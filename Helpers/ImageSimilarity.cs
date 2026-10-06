using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Numerics;

namespace ShopeeVideoUploader.Helpers;

/// <summary>Các tiện ích lưu ảnh và tạo dấu vân tay ảnh 64-bit.</summary>
public static class ImageSimilarity
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp"
    };

    public static string ImageLibraryPath
    {
        get
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FlowPilot",
                "LinkAffImages");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string SaveToLibrary(string sourcePath)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Không tìm thấy ảnh đã chọn.", sourcePath);

        var sourceFullPath = Path.GetFullPath(sourcePath);
        var libraryFullPath = Path.GetFullPath(ImageLibraryPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (sourceFullPath.StartsWith(libraryFullPath, StringComparison.OrdinalIgnoreCase))
            return sourceFullPath;

        var extension = Path.GetExtension(sourceFullPath);
        if (!SupportedExtensions.Contains(extension)) extension = ".png";
        var destination = Path.Combine(ImageLibraryPath, $"aff_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{extension}");
        File.Copy(sourceFullPath, destination, overwrite: false);
        return destination;
    }

    public static string ComputeDifferenceHash(string imagePath)
    {
        using var source = LoadUnlocked(imagePath);
        using var resized = new Bitmap(9, 8, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(resized))
        {
            graphics.Clear(Color.White);
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.DrawImage(source, new Rectangle(0, 0, 9, 8));
        }

        ulong hash = 0;
        var bit = 0;
        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 8; x++)
            {
                var left = resized.GetPixel(x, y);
                var right = resized.GetPixel(x + 1, y);
                var leftGray = (left.R * 299 + left.G * 587 + left.B * 114) / 1000;
                var rightGray = (right.R * 299 + right.G * 587 + right.B * 114) / 1000;
                if (leftGray > rightGray) hash |= 1UL << bit;
                bit++;
            }
        }

        return hash.ToString("X16");
    }

    public static int HammingDistance(string firstHash, string secondHash)
    {
        if (!ulong.TryParse(firstHash, System.Globalization.NumberStyles.HexNumber, null, out var first) ||
            !ulong.TryParse(secondHash, System.Globalization.NumberStyles.HexNumber, null, out var second))
            return int.MaxValue;

        return BitOperations.PopCount(first ^ second);
    }

    public static Bitmap CreateThumbnail(string imagePath, int width, int height)
    {
        using var source = LoadUnlocked(imagePath);
        var target = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(target);
        graphics.Clear(Color.FromArgb(248, 250, 252));
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;

        var ratio = Math.Min((double)width / source.Width, (double)height / source.Height);
        var drawWidth = Math.Max(1, (int)Math.Round(source.Width * ratio));
        var drawHeight = Math.Max(1, (int)Math.Round(source.Height * ratio));
        var x = (width - drawWidth) / 2;
        var y = (height - drawHeight) / 2;
        graphics.DrawImage(source, new Rectangle(x, y, drawWidth, drawHeight));
        return target;
    }

    public static Bitmap LoadUnlocked(string imagePath)
    {
        using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var image = Image.FromStream(stream);
        return new Bitmap(image);
    }
}
