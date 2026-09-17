namespace ShopeeVideoUploader.Models;

/// <summary>
/// Các trạng thái dùng chung cho job. Giữ tên trạng thái ở một nơi để UI và
/// hai workflow engine không tự tạo ra các biến thể khác nhau.
/// </summary>
public static class JobStatus
{
    public const string Waiting = "Chờ";
    public const string Running = "Đang chạy";
    public const string Retrying = "Đang thử lại";
    public const string RunningAgain = "Đang chạy lại";
    public const string Succeeded = "Thành công";
    public const string Failed = "Lỗi";
    public const string Stopped = "Đã dừng";

    public const string ShopeePending = "Chưa up Shopee";
    public const string ShopeeDone = "Đã up Shopee";
    public const string FacebookPending = "Chưa up Facebook";
    public const string FacebookDone = "Đã up Facebook";

    public static bool IsCompleted(JobItem job, string platform)
        => string.Equals(GetPlatformStatus(job, platform),
            platform.Equals("Facebook", StringComparison.OrdinalIgnoreCase) ? FacebookDone : ShopeeDone,
            StringComparison.Ordinal);

    public static bool IsTerminal(string status)
        => status is Succeeded or Failed or Stopped;

    public static string GetPlatformStatus(JobItem job, string platform)
        => platform.Equals("Facebook", StringComparison.OrdinalIgnoreCase)
            ? job.FbStatus
            : job.ShopeeStatus;

    public static void MarkCompleted(JobItem job, string platform)
    {
        if (platform.Equals("Facebook", StringComparison.OrdinalIgnoreCase))
            job.FbStatus = FacebookDone;
        else
            job.ShopeeStatus = ShopeeDone;
    }

    public static void Reset(JobItem job)
    {
        job.Status = Waiting;
        job.ShopeeStatus = ShopeePending;
        job.FbStatus = FacebookPending;
        job.Log = string.Empty;
    }
}
