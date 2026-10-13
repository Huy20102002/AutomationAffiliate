using System;

namespace ShopeeVideoUploader.Models;

public enum MultiWorkflowExecutionMode
{
    PerVideo = 0,        // Mỗi video: Xong Quy trình 1 -> chuyển ngay sang Quy trình 2 cho video đó
    BatchByWorkflow = 1  // Theo lượt danh sách: Chạy hết Quy trình 1 cho cả danh sách -> sau đó mới sang Quy trình 2
}

public sealed class MultiWorkflowConfig
{
    public bool Enabled { get; set; } = false;

    // Quy trình 1
    public string WorkflowFile1 { get; set; } = string.Empty;
    public string Platform1 { get; set; } = "Facebook";

    // Quy trình 2
    public string WorkflowFile2 { get; set; } = string.Empty;
    public string Platform2 { get; set; } = "Shopee";

    // Chế độ thực thi
    public MultiWorkflowExecutionMode Mode { get; set; } = MultiWorkflowExecutionMode.PerVideo;

    // Tự động bỏ qua nếu video đã hoàn thành nền tảng đó trước đó
    public bool SkipIfPlatformAlreadyDone { get; set; } = true;

    // Nếu quy trình 1 bị lỗi, có tiếp tục chạy quy trình 2 cho video đó không
    public bool ContinueNextWorkflowOnFailure { get; set; } = false;

    // Thời gian nghỉ giữa 2 quy trình/nền tảng (giây)
    public int DelayBetweenWorkflowsSeconds { get; set; } = 15;

    // Thời gian chờ tải lên / upload hoàn tất sau mỗi quy trình (giây)
    public int UploadWaitTimeoutSeconds { get; set; } = 10;
}
