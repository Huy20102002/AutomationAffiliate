namespace ShopeeVideoUploader.Helpers;

/// <summary>
/// Thread-safe logger ghi log ra file và fire event để UI nhận realtime.
/// </summary>
public static class Logger
{
    private static readonly object _lock = new();
    private static readonly string _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
    private static string _logFile = string.Empty;

    /// <summary>
    /// Event fired khi có log mới. UI subscribe để hiển thị realtime.
    /// </summary>
    public static event Action<string>? OnLog;

    /// <summary>
    /// Khởi tạo logger, tạo thư mục và file log theo ngày.
    /// </summary>
    public static void Initialize()
    {
        if (!Directory.Exists(_logDir))
            Directory.CreateDirectory(_logDir);

        _logFile = Path.Combine(_logDir, $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
    }

    /// <summary>
    /// Ghi log info.
    /// </summary>
    public static void Info(string message)
    {
        WriteLog("INFO", message);
    }

    /// <summary>
    /// Ghi log warning.
    /// </summary>
    public static void Warn(string message)
    {
        WriteLog("WARN", message);
    }

    /// <summary>
    /// Ghi log error.
    /// </summary>
    public static void Error(string message, Exception? ex = null)
    {
        var msg = ex != null ? $"{message} | {ex.Message}" : message;
        WriteLog("ERROR", msg);
    }

    private static void WriteLog(string level, string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}";

        lock (_lock)
        {
            try
            {
                if (!string.IsNullOrEmpty(_logFile))
                    File.AppendAllText(_logFile, line + Environment.NewLine);
            }
            catch
            {
                // Silently ignore file write errors
            }
        }

        // Fire event cho UI (thread-safe vì UI sẽ dùng Invoke)
        OnLog?.Invoke(line);
    }
}
