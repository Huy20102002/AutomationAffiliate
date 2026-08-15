using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string dbPath = "app.db")
    {
        _connectionString = $"Data Source={dbPath}";
    }

    private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

    public void InitializeDatabase()
    {
        using var connection = CreateConnection();
        connection.Open();
        
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
        
        try
        {
            connection.Execute("ALTER TABLE Jobs ADD COLUMN FbStatus TEXT DEFAULT 'Chưa up Facebook';");
        }
        catch { /* Bỏ qua nếu cột đã tồn tại */ }
        
        try
        {
            connection.Execute("ALTER TABLE Jobs ADD COLUMN FolderId INTEGER;");
        }
        catch { /* Bỏ qua nếu cột đã tồn tại */ }
    }

    public List<JobItem> GetAllJobs()
    {
        using var connection = CreateConnection();
        var jobs = connection.Query<JobItem>("SELECT * FROM Jobs").ToList();
        return jobs;
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

    public void DeleteJob(int id)
    {
        using var connection = CreateConnection();
        connection.Execute("DELETE FROM Jobs WHERE Id = @Id", new { Id = id });
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
        connection.Execute("DELETE FROM Folders WHERE Id = @Id", new { Id = id });
        connection.Execute("UPDATE Jobs SET FolderId = NULL WHERE FolderId = @Id", new { Id = id });
    }
}
