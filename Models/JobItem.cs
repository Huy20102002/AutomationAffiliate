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
    public string Status { get; set; } = JobStatus.Waiting;

    /// <summary>Trạng thái sản phẩm trên Shopee.</summary>
    public string ShopeeStatus { get; set; } = JobStatus.ShopeePending;


    /// <summary>Trạng thái sản phẩm trên Facebook.</summary>
    public string FbStatus { get; set; } = JobStatus.FacebookPending;

    /// <summary>Log chi tiết quá trình xử lý</summary>
    public string Log { get; set; } = string.Empty;

    /// <summary>Dữ liệu các cột bổ sung được import từ Excel.</summary>
    public Dictionary<string, string> Data { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Đánh dấu video đã được tạo tiêu đề bằng AI (tránh gọi API lặp lại tốn token)</summary>
    public bool IsAiTitleGenerated
    {
        get => Data.TryGetValue("AiTitleGenerated", out var v) && (v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase));
        set => Data["AiTitleGenerated"] = value ? "1" : "0";
    }

    /// <summary>Dữ liệu JSON dùng để lưu vào CSDL</summary>
    public string DataJson
    {
        get => System.Text.Json.JsonSerializer.Serialize(Data);
        set
        {
            Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(value)) return;
            try
            {
                var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(value);
                if (dict != null) Data = new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
            }
            catch { /* Ignore invalid JSON */ }
        }
    }

    public string GetColumnValue(string columnName, bool takeAllLinks = true)
    {
        return TryGetColumnValue(columnName, out var value, takeAllLinks) ? value : string.Empty;
    }

    public bool HasColumn(string columnName)
        => TryGetColumnValue(columnName, out _);

    public List<string> GetShopeeAffLinks()
    {
        if (string.IsNullOrWhiteSpace(ShopeeAffLink)) return [];
        var parts = ShopeeAffLink.Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        var list = new List<string>();
        foreach (var p in parts)
        {
            var match = System.Text.RegularExpressions.Regex.Match(p, @"https?://[^\s,;]+");
            var link = match.Success ? match.Value : p.Trim();
            if (!string.IsNullOrWhiteSpace(link))
                list.Add(link);
        }
        return list;
    }

    private bool TryGetColumnValue(string columnName, out string value, bool takeAllLinks = true)
    {
        value = string.Empty;
        if (string.IsNullOrWhiteSpace(columnName)) return false;

        var normalized = NormalizeColumnName(columnName);
        var links = GetShopeeAffLinks();

        switch (normalized)
        {
            case "videopath": value = VideoPath; return true;
            case "imagepath":
            case "image":
            case "anh":
                var customImg = Data.FirstOrDefault(item => NormalizeColumnName(item.Key) == normalized);
                value = !string.IsNullOrEmpty(customImg.Key) && !string.IsNullOrWhiteSpace(customImg.Value) ? customImg.Value : VideoPath;
                return true;
            case "title": value = Title; return true;
            case "shopeeafflink": 
                value = links.Count > 0 ? (takeAllLinks ? string.Join("\n", links) : links[0]) : ShopeeAffLink; 
                return true;
            case "shopeeafflinkall":
                value = links.Count > 0 ? string.Join("\n", links) : ShopeeAffLink;
                return true;
            case "shopeeafflink_first":
            case "shopeeafflink1":
            case "shopeeafflink0":
                value = links.Count > 0 ? links[0] : "";
                return true;
            case "shopeeafflink2":
                value = links.Count > 1 ? links[1] : "";
                return true;
            case "shopeeafflink3":
                value = links.Count > 2 ? links[2] : "";
                return true;
            case "shopeeafflink4":
                value = links.Count > 3 ? links[3] : "";
                return true;
            case "shopeeafflink5":
                value = links.Count > 4 ? links[4] : "";
                return true;
            case "shopeeafflink6":
                value = links.Count > 5 ? links[5] : "";
                return true;
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
