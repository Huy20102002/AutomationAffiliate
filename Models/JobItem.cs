namespace ShopeeVideoUploader.Models;

/// <summary>
/// Đại diện cho một công việc (1 dòng trong Excel).
/// Mỗi JobItem chứa thông tin video cần upload lên Shopee.
/// </summary>
public class JobItem
{
    /// <summary>ID thứ tự</summary>
    public int Id { get; set; }

    /// <summary>ID thư mục (chiến dịch)</summary>
    public int? FolderId { get; set; }

    /// <summary>Đường dẫn file video trên PC (hoặc folder chứa video)</summary>
    public string VideoPath { get; set; } = string.Empty;

    /// <summary>Link Shopee Affiliate</summary>
    public string ShopeeAffLink { get; set; } = string.Empty;

    /// <summary>Tiêu đề video</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái xử lý: Chờ, Đang chạy, Thành công, Lỗi
    /// </summary>
    public string Status { get; set; } = "Chờ";

    /// <summary>Trạng thái sản phẩm trên Shopee.</summary>
    public string ShopeeStatus { get; set; } = "Chưa up Shopee";

    /// <summary>Trạng thái sản phẩm trên Facebook.</summary>
    public string FbStatus { get; set; } = "Chưa up Facebook";

    /// <summary>Log chi tiết quá trình xử lý</summary>
    public string Log { get; set; } = string.Empty;

    /// <summary>Dữ liệu các cột bổ sung được import từ Excel.</summary>
    public Dictionary<string, string> Data { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Dữ liệu JSON dùng để lưu vào CSDL</summary>
    public string DataJson
    {
        get => System.Text.Json.JsonSerializer.Serialize(Data);
        set
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            try
            {
                var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(value);
                if (dict != null) Data = new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
            }
            catch { /* Ignore invalid JSON */ }
        }
    }

    public string GetColumnValue(string columnName)
    {
        return TryGetColumnValue(columnName, out var value) ? value : string.Empty;
    }

    public bool HasColumn(string columnName)
        => TryGetColumnValue(columnName, out _);

    private bool TryGetColumnValue(string columnName, out string value)
    {
        value = string.Empty;
        if (string.IsNullOrWhiteSpace(columnName)) return false;

        var normalized = NormalizeColumnName(columnName);
        switch (normalized)
        {
            case "videopath": value = VideoPath; return true;
            case "title": value = Title; return true;
            case "shopeeafflink": value = ShopeeAffLink; return true;
            case "status": value = Status; return true;
            case "shopeestatus": value = ShopeeStatus; return true;
            case "fbstatus": value = FbStatus; return true;
            case "id": value = Id.ToString(); return true;
        }

        var customColumn = Data.FirstOrDefault(item => NormalizeColumnName(item.Key) == normalized);
        if (string.IsNullOrEmpty(customColumn.Key)) return false;
        value = customColumn.Value ?? string.Empty;
        return true;
    }

    private static string NormalizeColumnName(string value)
        => new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
}
