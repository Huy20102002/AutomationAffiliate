using System.Diagnostics;

namespace ShopeeVideoUploader.Helpers;

public static class MediaHelper
{
    /// <summary>
    /// Mở/xem file video hoặc ảnh bằng ứng dụng mặc định của hệ điều hành.
    /// Hỗ trợ chuẩn hóa đường dẫn, gỡ dấu ngoặc kép, xử lý danh sách ảnh/video phân cách bởi ';'.
    /// </summary>
    public static bool PlayOrOpenMedia(string? rawPath, IWin32Window? owner = null)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            MessageBox.Show(owner, "Đường dẫn file video / ảnh đang trống.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        // Tách trường hợp nhiều ảnh phân cách bằng ; hoặc |
        var parts = rawPath.Split(new[] { ';', '|', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var targetPath = parts.FirstOrDefault()?.Trim().Trim('"');

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            MessageBox.Show(owner, "Đường dẫn file không hợp lệ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        // Tìm file đầu tiên còn tồn tại nếu là danh sách nhiều ảnh
        if (!File.Exists(targetPath) && parts.Length > 1)
        {
            foreach (var part in parts)
            {
                var clean = part.Trim().Trim('"');
                if (File.Exists(clean))
                {
                    targetPath = clean;
                    break;
                }
            }
        }

        if (!File.Exists(targetPath))
        {
            string? dir = null;
            try { dir = Path.GetDirectoryName(targetPath); } catch { }

            var dirExists = !string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir);
            var msg = $"Không tìm thấy file video/ảnh trên máy:\n{targetPath}\n\nFile có thể đã bị di chuyển hoặc đổi tên.";
            if (dirExists)
            {
                msg += "\n\nBạn có muốn mở thư mục chứa không?";
                if (MessageBox.Show(owner, msg, "Không tìm thấy file", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"/select,\"{targetPath}\"",
                            UseShellExecute = true
                        });
                        return true;
                    }
                    catch { }
                }
                return false;
            }

            MessageBox.Show(owner, msg, "Không tìm thấy file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = targetPath,
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(owner, $"Không thể mở file video: {ex.Message}", "Lỗi mở file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }
}
