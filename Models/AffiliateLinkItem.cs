namespace ShopeeVideoUploader.Models;

/// <summary>Một liên kết tiếp thị kèm ảnh đại diện để tra cứu nhanh.</summary>
public sealed class AffiliateLinkItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string ImageHash { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public string SourceKey { get; set; } = string.Empty;

    public List<string> GetUrls() => ParseUrls(Url);

    public static List<string> ParseUrls(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];
        return System.Text.RegularExpressions.Regex.Matches(value, @"https?://[^\s,;]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            .Select(match => match.Value.Trim().TrimEnd('/'))
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
