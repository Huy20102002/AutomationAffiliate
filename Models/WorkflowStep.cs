using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ShopeeVideoUploader.Models;

/// <summary>
/// Loại hành động trong Workflow.
/// Mỗi StepType tương ứng với một thao tác ADB cụ thể.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum StepType
{
    /// <summary>Điểm bắt đầu workflow</summary>
    Start,

    /// <summary>Điểm kết thúc workflow</summary>
    End,

    /// <summary>Chạm vào điểm (X, Y) trên màn hình</summary>
    Tap,

    /// <summary>Gõ văn bản (hỗ trợ Tiếng Việt qua ADBKeyBoard)</summary>
    InputText,

    /// <summary>Push video từ PC vào điện thoại qua ADB</summary>
    PushVideo,

    /// <summary>Chờ một khoảng thời gian (ms)</summary>
    Delay,

    /// <summary>Vuốt màn hình từ (X1,Y1) đến (X2,Y2)</summary>
    Swipe,

    /// <summary>Mở ứng dụng bằng package name</summary>
    OpenApp,

    /// <summary>Gửi phím (Back, Home, Enter, v.v.)</summary>
    KeyEvent,

    /// <summary>Quét media để Shopee nhận file mới push vào</summary>
    MediaScan,

    /// <summary>Chạy một lệnh adb shell tùy chỉnh</summary>
    AdbShell
}

[JsonConverter(typeof(StringEnumConverter))]
public enum VideoSourceMode
{
    ExcelPath,
    FolderAndExcelFileName,
    FixedFile
}

public enum TapMode
{
    Coordinates,
    XPath,
    Image
}

/// <summary>
/// Đại diện cho một bước trong Workflow No-Code.
/// Mỗi bước chứa đầy đủ thông số để thực thi hành động tương ứng.
/// </summary>
public class WorkflowStep
{
    public static string GetTypeLabel(StepType type) => type switch
    {
        StepType.Start => "Bắt đầu",
        StepType.End => "Kết thúc",
        StepType.Tap => "Chạm",
        StepType.InputText => "Nhập văn bản",
        StepType.PushVideo => "Đẩy video",
        StepType.Delay => "Chờ",
        StepType.Swipe => "Vuốt",
        StepType.OpenApp => "Mở ứng dụng",
        StepType.KeyEvent => "Phím hệ thống",
        StepType.MediaScan => "Quét thư viện",
        StepType.AdbShell => "Lệnh ADB",
        _ => type.ToString()
    };

    /// <summary>Loại hành động</summary>
    public StepType Type { get; set; }

    /// <summary>Tọa độ X cho Tap</summary>
    public int X { get; set; }

    /// <summary>Tọa độ Y cho Tap</summary>
    public int Y { get; set; }

    public TapMode TapMode { get; set; } = TapMode.Coordinates;
    public string TapXPath { get; set; } = string.Empty;
    public string TapImagePath { get; set; } = string.Empty;
    public double TapImageThreshold { get; set; } = 0.88;
    public int TapImageTimeoutMs { get; set; } = 5000;

    /// <summary>Tọa độ X kết thúc cho Swipe</summary>
    public int X2 { get; set; }

    /// <summary>Tọa độ Y kết thúc cho Swipe</summary>
    public int Y2 { get; set; }

    /// <summary>
    /// Văn bản để gõ, package name cho OpenApp, keycode cho KeyEvent,
    /// hoặc đường dẫn remote cho MediaScan.
    /// Hỗ trợ binding từ Excel: {Title}, {ShopeeAffLink}, {VideoPath}
    /// </summary>
    public string TextValue { get; set; } = string.Empty;

    /// <summary>
    /// Tên cột Excel để binding dữ liệu động.
    /// VD: "Title" sẽ thay {Title} trong TextValue bằng giá trị từ Excel.
    /// Nếu để trống, TextValue sẽ được dùng nguyên bản.
    /// </summary>
    public string BindingColumn { get; set; } = string.Empty;

    /// <summary>Nguồn file cho bước Đẩy video.</summary>
    public VideoSourceMode VideoSource { get; set; } = VideoSourceMode.ExcelPath;

    /// <summary>Thư mục video khi dùng FolderAndExcelFileName.</summary>
    public string VideoFolderPath { get; set; } = string.Empty;

    /// <summary>File video cố định khi dùng FixedFile.</summary>
    public string VideoFilePath { get; set; } = string.Empty;

    /// <summary>Clear FlowPilot-managed videos on the device before uploading a new video.</summary>
    public bool ClearDeviceVideosBeforeUpload { get; set; } = true;

    /// <summary>Chỉ xóa file nguồn trên PC sau khi toàn bộ workflow của sản phẩm chạy thành công.</summary>
    public bool DeleteLocalVideoAfterSuccess { get; set; }

    /// <summary>Thời gian chờ sau khi thực hiện bước (ms)</summary>
    public int DelayAfterMs { get; set; } = 500;

    /// <summary>Thời gian chờ tối đa (ms) để random</summary>
    public int? DelayMaxMs { get; set; }

    /// <summary>Thời gian vuốt cho Swipe (ms). Mặc định 300ms.</summary>
    public int SwipeDurationMs { get; set; } = 300;

    /// <summary>Mô tả bước hiển thị trên UI (tùy chọn)</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Sử dụng AI để xử lý lại văn bản trước khi nhập (áp dụng cho InputText)</summary>
    public bool UseAiForText { get; set; }

    /// <summary>
    /// Vị trí node trên canvas. Giá trị âm nghĩa là canvas tự xếp layout.
    /// Được lưu cùng workflow để người dùng tự sắp xếp flow.
    /// </summary>
    public int CanvasX { get; set; } = -1;
    public int CanvasY { get; set; } = -1;

    /// <summary>
    /// Tạo mô tả ngắn gọn cho hiển thị trên danh sách bước.
    /// </summary>
    public string GetDisplayText()
    {
        if (!string.IsNullOrEmpty(Description))
            return $"[{GetTypeLabel(Type)}] {Description}";

        return Type switch
        {
            StepType.Tap => TapMode switch
            {
                TapMode.XPath => $"[Chạm XPath] {TapXPath}",
                TapMode.Image => $"[Chạm ảnh] {Path.GetFileName(TapImagePath)}",
                _ => $"[Chạm] ({X}, {Y})"
            },
            StepType.Start => "[Bắt đầu] Bắt đầu quy trình",
            StepType.End => "[Kết thúc] Kết thúc quy trình",
            StepType.InputText => $"[Nhập văn bản] \"{(TextValue.Length > 30 ? TextValue[..30] + "..." : TextValue)}\"",
            StepType.PushVideo => "[Đẩy video] → /sdcard/DCIM/Camera/",
            StepType.Delay => DelayMaxMs.HasValue ? $"[Chờ] {DelayAfterMs}-{DelayMaxMs} ms" : $"[Chờ] {DelayAfterMs} ms",
            StepType.Swipe => $"[Vuốt] ({X},{Y}) → ({X2},{Y2})",
            StepType.OpenApp => $"[Mở ứng dụng] {TextValue}",
            StepType.KeyEvent => $"[Phím hệ thống] {TextValue}",
            StepType.MediaScan => "[Quét thư viện] Quét media",
            StepType.AdbShell => $"[Lệnh ADB] {TextValue}",
            _ => $"[{GetTypeLabel(Type)}]"
        };
    }
}
