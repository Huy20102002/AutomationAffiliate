namespace ShopeeVideoUploader.Models;

public sealed class TikTokVideoItem
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string DurationText { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public bool Selected { get; set; } = true;
    public string Status { get; set; } = "Chưa tải";
}
