namespace ShopeeVideoUploader.Models;

/// <summary>
/// Cấu hình Telegram Bot phục vụ thông báo và điều khiển từ xa.
/// </summary>
public class TelegramConfig
{
    /// <summary>
    /// Token của Telegram Bot do @BotFather cấp.
    /// </summary>
    public string BotToken { get; set; } = string.Empty;

    /// <summary>
    /// Danh sách Chat ID của Admin được phép nhận thông báo và điều khiển tool (ngăn cách bởi dấu phẩy).
    /// </summary>
    public string AdminChatIds { get; set; } = string.Empty;

    /// <summary>
    /// Bật/Tắt toàn bộ hệ thống thông báo Telegram.
    /// </summary>
    public bool EnableNotifications { get; set; } = true;

    /// <summary>
    /// Gửi thông báo khi bắt đầu phiên chạy.
    /// </summary>
    public bool NotifyOnStart { get; set; } = true;

    /// <summary>
    /// Gửi thông báo sau khi mỗi video được up thành công.
    /// </summary>
    public bool NotifyOnSuccess { get; set; } = true;

    /// <summary>
    /// Gửi thông báo khi video bị lỗi trong quá trình xử lý.
    /// </summary>
    public bool NotifyOnError { get; set; } = true;

    /// <summary>
    /// Tự động chụp và gửi ảnh màn hình thiết bị khi gặp lỗi.
    /// </summary>
    public bool SendScreenshotOnError { get; set; } = true;

    /// <summary>
    /// Cho phép nhận lệnh điều khiển từ xa (Bắt đầu, Dừng, Chụp màn hình,...) từ Telegram.
    /// </summary>
    public bool EnableRemoteControl { get; set; } = true;

    /// <summary>
    /// Nền tảng mặc định khi kích hoạt chạy từ Telegram (Shopee / Facebook).
    /// </summary>
    public string DefaultPlatform { get; set; } = "Shopee";

    /// <summary>
    /// Mặc định bật/tắt AI SEO tiêu đề khi chạy từ Telegram.
    /// </summary>
    public bool UseAiTitle { get; set; } = false;

    /// <summary>
    /// Thời gian delay tối thiểu (phút) giữa các video.
    /// </summary>
    public int DelayMinMinutes { get; set; } = 0;

    /// <summary>
    /// Thời gian delay tối đa (phút) giữa các video.
    /// </summary>
    public int DelayMaxMinutes { get; set; } = 0;

    /// <summary>
    /// ID thư mục/chiến dịch đang chọn để chạy (-1: Tất cả chiến dịch).
    /// </summary>
    public int SelectedFolderId { get; set; } = -1;

    /// <summary>
    /// File quy trình workflow .json đang được chọn.
    /// </summary>
    public string SelectedWorkflowFile { get; set; } = "Shopee_Upload.json";

    /// <summary>
    /// Chỉ up các video có link tiếp thị (bỏ qua video không link).
    /// </summary>
    public bool OnlyRunWithLink { get; set; } = false;

    /// <summary>
    /// Kiểm tra xem một Chat ID có thuộc danh sách Admin hợp lệ hay không.
    /// </summary>
    public bool IsAdmin(long chatId)
    {
        if (string.IsNullOrWhiteSpace(AdminChatIds))
            return false;

        var strId = chatId.ToString();
        var ids = AdminChatIds.Split([',', ';', ' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return ids.Any(id => id.Trim().Equals(strId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Lấy danh sách tất cả các Chat ID của Admin hợp lệ.
    /// </summary>
    public List<long> GetAdminChatIdList()
    {
        if (string.IsNullOrWhiteSpace(AdminChatIds))
            return [];

        var result = new List<long>();
        var ids = AdminChatIds.Split([',', ';', ' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var id in ids)
        {
            if (long.TryParse(id.Trim(), out var parsedId))
            {
                result.Add(parsedId);
            }
        }
        return result;
    }
}
