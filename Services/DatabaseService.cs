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
                VideoPath TEXT,
                ShopeeAffLink TEXT,
                Title TEXT,
                Status TEXT,
                ShopeeStatus TEXT,
                Log TEXT,
                DataJson TEXT
            );";
            
        connection.Execute(createJobsTable);
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
            INSERT INTO Jobs (VideoPath, ShopeeAffLink, Title, Status, ShopeeStatus, Log, DataJson)
            VALUES (@VideoPath, @ShopeeAffLink, @Title, @Status, @ShopeeStatus, @Log, @DataJson);
            SELECT last_insert_rowid();";
            
        job.Id = connection.QuerySingle<int>(sql, job);
    }

    public void UpdateJob(JobItem job)
    {
        using var connection = CreateConnection();
        var sql = @"
            UPDATE Jobs 
            SET VideoPath = @VideoPath, 
                ShopeeAffLink = @ShopeeAffLink, 
                Title = @Title, 
                Status = @Status, 
                ShopeeStatus = @ShopeeStatus, 
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
                INSERT INTO Jobs (Id, VideoPath, ShopeeAffLink, Title, Status, ShopeeStatus, Log, DataJson)
                VALUES (@Id, @VideoPath, @ShopeeAffLink, @Title, @Status, @ShopeeStatus, @Log, @DataJson);";
            
            connection.Execute(sql, jobs, transaction: transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
