using MiniExcelLibs;
using ShopeeVideoUploader.Helpers;
using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Services;

/// <summary>
/// Xử lý đọc/ghi file Excel bằng MiniExcel (streaming, nhẹ, không cần Office).
/// </summary>
public static class ExcelService
{
    /// <summary>Import file Excel thành danh sách JobItem</summary>
    public static List<JobItem> ImportExcel(string filePath)
    {
        Logger.Info($"Import Excel: {filePath}");
        var rows = MiniExcel.Query(filePath, useHeaderRow: true).ToList();
        var jobs = new List<JobItem>();
        int id = 1;

        if (rows.Count > 0)
        {
            var firstRow = (IDictionary<string, object>)rows[0];
            if (!HasColumn(firstRow, "VideoPath") && !HasColumn(firstRow, "ImagePath") && !HasColumn(firstRow, "Image") && !HasColumn(firstRow, "Anh"))
                throw new InvalidDataException("File Excel phải có cột VideoPath hoặc ImagePath để đẩy media lên điện thoại.");
        }

        foreach (var row in rows)
        {
            var dict = (IDictionary<string, object>)row;
            var data = dict.ToDictionary(
                pair => pair.Key,
                pair => pair.Value?.ToString() ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);

            var mediaPath = GetValue(dict, "VideoPath").Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(mediaPath))
                mediaPath = GetValue(dict, "ImagePath").Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(mediaPath))
                mediaPath = GetValue(dict, "Image").Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(mediaPath))
                mediaPath = GetValue(dict, "Anh").Trim().Trim('"');

            jobs.Add(new JobItem
            {
                Id = id++,
                VideoPath = mediaPath,
                ShopeeAffLink = GetValue(dict, "ShopeeAffLink"),
                Title = GetValue(dict, "Title"),
                Status = JobStatus.Waiting,
                ShopeeStatus = JobStatus.ShopeePending,
                FbStatus = JobStatus.FacebookPending,
                Log = "",
                Data = data
            });
        }

        Logger.Info($"Đã import {jobs.Count} jobs từ Excel");
        return jobs;
    }

    /// <summary>Xuất file Excel mẫu (template)</summary>
    public static void ExportTemplate(string filePath)
    {
        var template = new[]
        {
            new { VideoPath = @"C:\Videos\video1.mp4", ShopeeAffLink = "https://s.shopee.vn/abc123", Title = "Tiêu đề video mẫu 1" },
            new { VideoPath = @"C:\Videos\video2.mp4", ShopeeAffLink = "https://s.shopee.vn/def456", Title = "Tiêu đề video mẫu 2" },
        };
        MiniExcel.SaveAs(filePath, template);
        Logger.Info($"Template đã xuất: {filePath}");
    }

    /// <summary>Xuất kết quả (có Status & Log)</summary>
    public static void ExportResult(string filePath, List<JobItem> jobs)
    {
        MiniExcel.SaveAs(filePath, jobs);
        Logger.Info($"Kết quả đã xuất: {filePath} ({jobs.Count} jobs)");
    }

    private static string GetValue(IDictionary<string, object> dict, string key)
    {
        var normalizedKey = NormalizeHeader(key);
        var pair = dict.FirstOrDefault(item => NormalizeHeader(item.Key) == normalizedKey);
        return pair.Value?.ToString() ?? "";
    }

    private static bool HasColumn(IDictionary<string, object> dict, string key)
        => dict.Keys.Any(item => NormalizeHeader(item) == NormalizeHeader(key));

    private static string NormalizeHeader(string value)
        => new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
}
