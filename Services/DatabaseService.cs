using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using ShopeeVideoUploader.Helpers;
using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Services;

public class DatabaseService
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public string DatabasePath => _dbPath;

    public DatabaseService(string? customDbPath = null)
    {
        _dbPath = !string.IsNullOrWhiteSpace(customDbPath)
            ? customDbPath
            : GetDefaultDatabasePath();

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            DefaultTimeout = 15
        };
        _connectionString = builder.ToString();
    }

    public static string GetDefaultDatabasePath()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FlowPilot",
            "Data"
        );
        Directory.CreateDirectory(appDataDir);
        var targetDbPath = Path.Combine(appDataDir, "app.db");

        if (!File.Exists(targetDbPath))
        {
            var oldCandidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.db"),
                Path.Combine(Directory.GetCurrentDirectory(), "app.db")
            };

            foreach (var oldPath in oldCandidates)
            {
                if (File.Exists(oldPath) && !string.Equals(oldPath, targetDbPath, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        File.Copy(oldPath, targetDbPath, overwrite: false);
                        
                        var oldWal = oldPath + "-wal";
                        var targetWal = targetDbPath + "-wal";
                        if (File.Exists(oldWal) && !File.Exists(targetWal)) File.Copy(oldWal, targetWal, false);

                        var oldShm = oldPath + "-shm";
                        var targetShm = targetDbPath + "-shm";
                        if (File.Exists(oldShm) && !File.Exists(targetShm)) File.Copy(oldShm, targetShm, false);

                        Logger.Info($"Đã tự động chuyển dữ liệu CSDL từ {oldPath} sang {targetDbPath}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"Không thể sao chép CSDL cũ {oldPath}: {ex.Message}");
                    }
                }
            }
        }

        return targetDbPath;
    }

    private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

    public void InitializeDatabase()
    {
        using var connection = CreateConnection();
        connection.Open();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                PRAGMA journal_mode = WAL;
                PRAGMA synchronous = NORMAL;
                PRAGMA busy_timeout = 5000;
                PRAGMA cache_size = -64000;
                PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();
        }

        var createJobsTable = @"
            CREATE TABLE IF NOT EXISTS Jobs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FolderId INTEGER,
                VideoPath TEXT,
                ShopeeAffLink TEXT,
                Title TEXT,
                Status TEXT,
                ShopeeStatus TEXT,
                FbStatus TEXT,
                Log TEXT,
                DataJson TEXT
            );";
        connection.Execute(createJobsTable);

        var createFoldersTable = @"
            CREATE TABLE IF NOT EXISTS Folders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT
            );";
        connection.Execute(createFoldersTable);

        var columns = connection.Query<string>("SELECT name FROM pragma_table_info('Jobs')").ToHashSet(StringComparer.OrdinalIgnoreCase);
        EnsureColumn(connection, columns, "FolderId", "INTEGER");
        EnsureColumn(connection, columns, "VideoPath", "TEXT");
        EnsureColumn(connection, columns, "ShopeeAffLink", "TEXT");
        EnsureColumn(connection, columns, "Title", "TEXT");
        EnsureColumn(connection, columns, "Status", $"TEXT DEFAULT '{JobStatus.Waiting}'");
        EnsureColumn(connection, columns, "ShopeeStatus", $"TEXT DEFAULT '{JobStatus.ShopeePending}'");
        EnsureColumn(connection, columns, "FbStatus", $"TEXT DEFAULT '{JobStatus.FacebookPending}'");
        EnsureColumn(connection, columns, "Log", "TEXT");
        EnsureColumn(connection, columns, "DataJson", "TEXT");

        connection.Execute("UPDATE Jobs SET Status = @Waiting WHERE Status IS NULL OR TRIM(Status) = ''", new { Waiting = JobStatus.Waiting });
        connection.Execute("UPDATE Jobs SET ShopeeStatus = @Pending WHERE ShopeeStatus IS NULL OR TRIM(ShopeeStatus) = ''", new { Pending = JobStatus.ShopeePending });
        connection.Execute("UPDATE Jobs SET FbStatus = @Pending WHERE FbStatus IS NULL OR TRIM(FbStatus) = ''", new { Pending = JobStatus.FacebookPending });

        connection.Execute("CREATE INDEX IF NOT EXISTS IX_Jobs_FolderId ON Jobs(FolderId);");
        connection.Execute("CREATE INDEX IF NOT EXISTS IX_Jobs_Status ON Jobs(Status);");
        connection.Execute("CREATE INDEX IF NOT EXISTS IX_Jobs_ShopeeStatus ON Jobs(ShopeeStatus);");
        connection.Execute("CREATE INDEX IF NOT EXISTS IX_Jobs_FbStatus ON Jobs(FbStatus);");
    }

    private static void EnsureColumn(IDbConnection connection, ISet<string> columns, string name, string definition)
    {
        if (columns.Contains(name)) return;
        connection.Execute($"ALTER TABLE Jobs ADD COLUMN {name} {definition};");
        columns.Add(name);
    }

    public List<JobItem> GetAllJobs()
    {
        using var connection = CreateConnection();
        return connection.Query<JobItem>("SELECT * FROM Jobs ORDER BY Id").ToList();
    }

    public void SaveJob(JobItem job)
    {
        using var connection = CreateConnection();
        var sql = @"
            INSERT INTO Jobs (FolderId, VideoPath, ShopeeAffLink, Title, Status, ShopeeStatus, FbStatus, Log, DataJson)
            VALUES (@FolderId, @VideoPath, @ShopeeAffLink, @Title, @Status, @ShopeeStatus, @FbStatus, @Log, @DataJson);
            SELECT last_insert_rowid();";

        job.Id = connection.QuerySingle<int>(sql, job);
    }

    public void SaveJobs(IEnumerable<JobItem> jobs)
    {
        var jobList = jobs?.ToList();
        if (jobList == null || jobList.Count == 0) return;

        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var sqlWithId = @"
                INSERT INTO Jobs (Id, FolderId, VideoPath, ShopeeAffLink, Title, Status, ShopeeStatus, FbStatus, Log, DataJson)
                VALUES (@Id, @FolderId, @VideoPath, @ShopeeAffLink, @Title, @Status, @ShopeeStatus, @FbStatus, @Log, @DataJson);";

            var sqlAutoId = @"
                INSERT INTO Jobs (FolderId, VideoPath, ShopeeAffLink, Title, Status, ShopeeStatus, FbStatus, Log, DataJson)
                VALUES (@FolderId, @VideoPath, @ShopeeAffLink, @Title, @Status, @ShopeeStatus, @FbStatus, @Log, @DataJson);
                SELECT last_insert_rowid();";

            foreach (var job in jobList)
            {
                if (job.Id > 0)
                {
                    connection.Execute(sqlWithId, job, transaction: transaction);
                }
                else
                {
                    job.Id = connection.QuerySingle<int>(sqlAutoId, job, transaction: transaction);
                }
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void UpdateJob(JobItem job)
    {
        using var connection = CreateConnection();
        var sql = @"
            UPDATE Jobs 
            SET FolderId = @FolderId,
                VideoPath = @VideoPath, 
                ShopeeAffLink = @ShopeeAffLink, 
                Title = @Title, 
                Status = @Status, 
                ShopeeStatus = @ShopeeStatus, 
                FbStatus = @FbStatus, 
                Log = @Log, 
                DataJson = @DataJson
            WHERE Id = @Id";

        connection.Execute(sql, job);
    }

    public void UpdateJobs(IEnumerable<JobItem> jobs)
    {
        var jobList = jobs?.ToList();
        if (jobList == null || jobList.Count == 0) return;

        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var sql = @"
                UPDATE Jobs 
                SET FolderId = @FolderId,
                    VideoPath = @VideoPath, 
                    ShopeeAffLink = @ShopeeAffLink, 
                    Title = @Title, 
                    Status = @Status, 
                    ShopeeStatus = @ShopeeStatus, 
                    FbStatus = @FbStatus, 
                    Log = @Log, 
                    DataJson = @DataJson
                WHERE Id = @Id";

            connection.Execute(sql, jobList, transaction: transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void UpdateJobStatus(int id, string status, string log, string? shopeeStatus = null, string? fbStatus = null)
    {
        using var connection = CreateConnection();
        var sql = @"
            UPDATE Jobs 
            SET Status = @Status,
                Log = @Log,
                ShopeeStatus = COALESCE(@ShopeeStatus, ShopeeStatus),
                FbStatus = COALESCE(@FbStatus, FbStatus)
            WHERE Id = @Id";

        connection.Execute(sql, new { Id = id, Status = status, Log = log, ShopeeStatus = shopeeStatus, FbStatus = fbStatus });
    }

    public void ResetAllJobStatuses()
    {
        using var connection = CreateConnection();
        var sql = @"
            UPDATE Jobs 
            SET Status = @Waiting, 
                ShopeeStatus = @ShopeePending, 
                FbStatus = @FbPending, 
                Log = ''";

        connection.Execute(sql, new
        {
            Waiting = JobStatus.Waiting,
            ShopeePending = JobStatus.ShopeePending,
            FbPending = JobStatus.FacebookPending
        });
    }

    public void ResetJobStatuses(IEnumerable<int> ids)
    {
        var idList = ids?.Distinct().ToList();
        if (idList == null || idList.Count == 0) return;

        using var connection = CreateConnection();
        var sql = @"
            UPDATE Jobs 
            SET Status = @Waiting, 
                ShopeeStatus = @ShopeePending, 
                FbStatus = @FbPending, 
                Log = ''
            WHERE Id IN @Ids";

        connection.Execute(sql, new
        {
            Waiting = JobStatus.Waiting,
            ShopeePending = JobStatus.ShopeePending,
            FbPending = JobStatus.FacebookPending,
            Ids = idList
        });
    }

    public void DeleteJob(int id)
    {
        using var connection = CreateConnection();
        connection.Execute("DELETE FROM Jobs WHERE Id = @Id", new { Id = id });
    }

    public void DeleteJobs(IEnumerable<int> ids)
    {
        var idList = ids?.Distinct().ToList();
        if (idList == null || idList.Count == 0) return;

        using var connection = CreateConnection();
        connection.Execute("DELETE FROM Jobs WHERE Id IN @Ids", new { Ids = idList });
    }

    public void ClearAllJobs()
    {
        using var connection = CreateConnection();
        connection.Execute("DELETE FROM Jobs");
    }

    public void ReplaceAllJobs(IEnumerable<JobItem> jobs)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            connection.Execute("DELETE FROM Jobs", transaction: transaction);

            var sql = @"
                INSERT INTO Jobs (Id, FolderId, VideoPath, ShopeeAffLink, Title, Status, ShopeeStatus, FbStatus, Log, DataJson)
                VALUES (@Id, @FolderId, @VideoPath, @ShopeeAffLink, @Title, @Status, @ShopeeStatus, @FbStatus, @Log, @DataJson);";

            connection.Execute(sql, jobs, transaction: transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public List<FolderItem> GetAllFolders()
    {
        using var connection = CreateConnection();
        return connection.Query<FolderItem>("SELECT * FROM Folders ORDER BY Name").ToList();
    }

    public void SaveFolder(FolderItem folder)
    {
        using var connection = CreateConnection();
        var sql = @"
            INSERT INTO Folders (Name) VALUES (@Name);
            SELECT last_insert_rowid();";
        folder.Id = connection.QuerySingle<int>(sql, folder);
    }

    public void UpdateFolder(FolderItem folder)
    {
        using var connection = CreateConnection();
        connection.Execute("UPDATE Folders SET Name = @Name WHERE Id = @Id", folder);
    }

    public void DeleteFolder(int id)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            connection.Execute("UPDATE Jobs SET FolderId = NULL WHERE FolderId = @Id", new { Id = id }, transaction);
            connection.Execute("DELETE FROM Folders WHERE Id = @Id", new { Id = id }, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void BackupDatabase(string targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
            throw new ArgumentException("Đường dẫn sao lưu không hợp lệ", nameof(targetPath));

        var targetDir = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(targetDir)) Directory.CreateDirectory(targetDir);

        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "VACUUM INTO $targetPath;";
        cmd.Parameters.AddWithValue("$targetPath", targetPath);
        cmd.ExecuteNonQuery();

        Logger.Info($"Đã sao lưu CSDL an toàn vào: {targetPath}");
    }

    public void CreateDailyBackup()
    {
        try
        {
            var backupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FlowPilot",
                "Backups"
            );
            Directory.CreateDirectory(backupDir);

            var todayFile = Path.Combine(backupDir, $"app_backup_{DateTime.Now:yyyyMMdd}.db");
            if (!File.Exists(todayFile))
            {
                BackupDatabase(todayFile);
                Logger.Info($"Đã tạo bản sao lưu tự động hàng ngày: {todayFile}");
            }

            var cutoff = DateTime.Now.AddDays(-7);
            var backupFiles = Directory.GetFiles(backupDir, "app_backup_*.db");
            foreach (var file in backupFiles)
            {
                var fi = new FileInfo(file);
                if (fi.CreationTime < cutoff && fi.LastWriteTime < cutoff)
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Không thể tạo bản sao lưu tự động: {ex.Message}");
        }
    }

    public void RestoreDatabase(string sourcePath)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("File CSDL nguồn không tồn tại", sourcePath);

        SqliteConnection.ClearAllPools();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        var walPath = _dbPath + "-wal";
        var shmPath = _dbPath + "-shm";
        if (File.Exists(walPath)) try { File.Delete(walPath); } catch { }
        if (File.Exists(shmPath)) try { File.Delete(shmPath); } catch { }

        File.Copy(sourcePath, _dbPath, overwrite: true);
        Logger.Info($"Đã phục hồi CSDL từ {sourcePath} sang {_dbPath}");

        InitializeDatabase();
    }
}
