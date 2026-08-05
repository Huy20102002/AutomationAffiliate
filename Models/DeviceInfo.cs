namespace ShopeeVideoUploader.Models;

/// <summary>
/// Wrapper chứa thông tin thiết bị Android kết nối qua USB.
/// </summary>
public class DeviceInfo
{
    /// <summary>Serial number thiết bị (VD: "R5CT12345")</summary>
    public string Serial { get; set; } = string.Empty;

    /// <summary>Tên model thiết bị (VD: "SM-G998B")</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Trạng thái kết nối: device, offline, unauthorized</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Phiên bản Android (VD: "13")</summary>
    public string AndroidVersion { get; set; } = string.Empty;

    /// <summary>Hiển thị trong ComboBox</summary>
    public override string ToString()
    {
        return $"{Model} ({Serial})";
    }
}
