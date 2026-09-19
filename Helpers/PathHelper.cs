using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Helpers;

/// <summary>
/// Helper kiểm tra trùng đường dẫn video/ảnh xuyên chiến dịch.
/// </summary>
public static class PathHelper
{
    /// <summary>
    /// Chuẩn hóa path: trim, lowercase, chuẩn hóa separator.
    /// Hỗ trợ path chứa nhiều file phân cách bằng "; "
    /// </summary>
    public static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        var trimmed = path.Trim().Trim('"');
        // Nếu là multi-image path (phân cách bằng ;), normalize từng phần
        if (trimmed.Contains(';'))
        {
            var parts = trimmed.Split(';')
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(p => NormalizeSingle(p));
            return string.Join("; ", parts);
        }
        return NormalizeSingle(trimmed);
    }

    private static string NormalizeSingle(string path)
    {
        try
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return path.Replace('/', '\\').TrimEnd('\\');
        }
    }

    /// <summary>
    /// Tìm các sản phẩm trùng đường dẫn với path cho trước (cross-campaign).
    /// Trả về danh sách JobItem có cùng VideoPath, kèm tên folder.
    /// </summary>
    public static List<DuplicateInfo> FindDuplicates(
        string videoPath,
        IEnumerable<JobItem> allProducts,
        IReadOnlyList<FolderItem> folders)
    {
        if (string.IsNullOrWhiteSpace(videoPath))
            return [];

        var normalized = NormalizePath(videoPath);
        var folderMap = folders.ToDictionary(f => f.Id, f => f.Name);

        return allProducts
            .Where(j => string.Equals(NormalizePath(j.VideoPath), normalized, StringComparison.OrdinalIgnoreCase))
            .Select(j => new DuplicateInfo
            {
                JobId = j.Id,
                VideoPath = j.VideoPath,
                FolderId = j.FolderId,
                FolderName = GetFolderName(j.FolderId, folderMap),
                Title = j.Title
            })
            .ToList();
    }

    /// <summary>
    /// Build dictionary: normalized path → list of DuplicateInfo cho toàn bộ allProducts.
    /// Dùng để check nhanh khi cần highlight nhiều dòng.
    /// </summary>
    public static Dictionary<string, List<DuplicateInfo>> BuildDuplicateMap(
        IEnumerable<JobItem> allProducts,
        IReadOnlyList<FolderItem> folders)
    {
        var folderMap = folders.ToDictionary(f => f.Id, f => f.Name);
        var map = new Dictionary<string, List<DuplicateInfo>>(StringComparer.OrdinalIgnoreCase);

        foreach (var j in allProducts)
        {
            if (string.IsNullOrWhiteSpace(j.VideoPath)) continue;
            var key = NormalizePath(j.VideoPath);
            if (!map.TryGetValue(key, out var list))
            {
                list = [];
                map[key] = list;
            }
            list.Add(new DuplicateInfo
            {
                JobId = j.Id,
                VideoPath = j.VideoPath,
                FolderId = j.FolderId,
                FolderName = GetFolderName(j.FolderId, folderMap),
                Title = j.Title
            });
        }

        return map;
    }

    private static string GetFolderName(int? folderId, Dictionary<int, string> folderMap)
    {
        if (folderId == null || folderId == 0) return "Chưa phân loại";
        return folderMap.TryGetValue(folderId.Value, out var name) ? name : $"Chiến dịch #{folderId}";
    }
}

/// <summary>
/// Thông tin một bản trùng lặp.
/// </summary>
public class DuplicateInfo
{
    public int JobId { get; set; }
    public string VideoPath { get; set; } = string.Empty;
    public int? FolderId { get; set; }
    public string FolderName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}
