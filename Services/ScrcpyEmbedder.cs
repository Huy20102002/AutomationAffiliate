using System.Diagnostics;
using ShopeeVideoUploader.Helpers;

namespace ShopeeVideoUploader.Services;

/// <summary>
/// Nhúng cửa sổ scrcpy vào Panel WinForms qua Win32 API.
/// 
/// === KỸ THUẬT NHÚNG ===
/// 1. Chạy scrcpy.exe với --window-title để đặt tên cửa sổ duy nhất
/// 2. FindWindow() tìm HWND theo title
/// 3. SetParent() gắn scrcpy thành child của Panel
/// 4. SetWindowLong() xóa border/caption, thêm WS_CHILD
/// 5. MoveWindow() resize khớp Panel
/// 6. Hook Panel.SizeChanged để auto-resize theo
/// </summary>
public class ScrcpyEmbedder : IDisposable
{
    private Process? _scrcpyProcess;
    private IntPtr _scrcpyHwnd = IntPtr.Zero;
    private Panel? _hostPanel;
    private string _windowTitle = string.Empty;
    private string _deviceSerial = string.Empty;
    private System.Windows.Forms.Timer? _resizeTimer;

    public bool IsRunning => _scrcpyProcess != null && !_scrcpyProcess.HasExited && _scrcpyHwnd != IntPtr.Zero;
    public IntPtr Hwnd => _scrcpyHwnd;
    public string DeviceSerial => _deviceSerial;
    public Panel? HostPanel => _hostPanel;

    /// <summary>
    /// Khởi chạy scrcpy và nhúng vào Panel.
    /// </summary>
    public async Task<bool> StartAsync(string deviceSerial, Panel hostPanel, string? scrcpyPath = null)
    {
        try
        {
            _hostPanel = hostPanel;
            _deviceSerial = deviceSerial;
            _windowTitle = $"SCRCPY_{deviceSerial}";

            // Kill process cũ nếu có
            Stop();

            // Tìm scrcpy.exe
            var exePath = FindScrcpyPath(scrcpyPath);
            if (string.IsNullOrEmpty(exePath))
            {
                Logger.Info("Không tìm thấy scrcpy.exe, đang tiến hành tải tự động...");
                var progress = new Progress<string>(msg => Logger.Info(msg));
                exePath = await DownloadScrcpyAsync(progress);
                
                if (string.IsNullOrEmpty(exePath))
                {
                    Logger.Error("Tải scrcpy tự động thất bại!");
                    return false;
                }
            }

            Logger.Info($"Scrcpy path: {exePath}");
            var scrcpyDir = Path.GetDirectoryName(exePath) ?? AppDomain.CurrentDomain.BaseDirectory;

            // Tính toán kích thước ban đầu theo kích thước hostPanel
            int initWidth = _hostPanel.Width > 50 ? _hostPanel.Width : 250;
            int initHeight = _hostPanel.Height > 50 ? _hostPanel.Height : 528;

            // Khởi chạy scrcpy process với --window-width và --window-height để khởi tạo Direct3D swapchain đúng ngay từ đầu
            var args = $"--serial={deviceSerial} --window-title=\"{_windowTitle}\" --window-width={initWidth} --window-height={initHeight} --window-borderless --no-audio --stay-awake --max-fps=30";
            _scrcpyProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = args,
                    WorkingDirectory = scrcpyDir,
                    UseShellExecute = false,
                    CreateNoWindow = false
                }
            };
            _scrcpyProcess.Start();
            Logger.Info($"Scrcpy started (PID: {_scrcpyProcess.Id}) với kích thước ban đầu {initWidth}x{initHeight}");

            // Chờ cửa sổ scrcpy xuất hiện (retry tối đa 30 lần x 200ms = 6s)
            _scrcpyHwnd = IntPtr.Zero;
            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(200);
                _scrcpyHwnd = NativeMethods.FindWindow(null, _windowTitle);
                if (_scrcpyHwnd != IntPtr.Zero) break;

                if (_scrcpyProcess.HasExited)
                {
                    Logger.Error("Scrcpy đã thoát bất ngờ!");
                    return false;
                }
            }

            if (_scrcpyHwnd == IntPtr.Zero)
            {
                Logger.Error("Không tìm thấy cửa sổ scrcpy sau 6s!");
                Stop();
                return false;
            }

            // === NHÚNG SCRCPY VÀO PANEL ===

            // Bước 1: SetParent - gắn scrcpy thành child của Panel
            NativeMethods.SetParent(_scrcpyHwnd, _hostPanel.Handle);

            // Bước 2: Thay đổi Window Style
            // - Xóa WS_POPUP (cửa sổ popup độc lập)
            // - Xóa WS_CAPTION (title bar)
            // - QUAN TRỌNG: Giữ lại WS_THICKFRAME để SDL2 Direct3D11 swapchain nhận diện window resizable và tự động scale chuẩn
            // - Thêm WS_CHILD (child window), WS_VISIBLE, WS_CLIPCHILDREN, WS_CLIPSIBLINGS
            int style = NativeMethods.GetWindowLong(_scrcpyHwnd, NativeMethods.GWL_STYLE);
            style &= ~(NativeMethods.WS_POPUP | NativeMethods.WS_CAPTION |
                        NativeMethods.WS_MAXIMIZEBOX | NativeMethods.WS_MINIMIZEBOX | NativeMethods.WS_SYSMENU);
            style |= NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE |
                     NativeMethods.WS_CLIPCHILDREN | NativeMethods.WS_CLIPSIBLINGS |
                     NativeMethods.WS_THICKFRAME;
            NativeMethods.SetWindowLong(_scrcpyHwnd, NativeMethods.GWL_STYLE, style);

            // Bước 3: Resize khớp khít Panel
            NativeMethods.MoveWindow(_scrcpyHwnd, 0, 0, _hostPanel.Width, _hostPanel.Height, true);

            // Bước 4: Force redraw sau khi thay đổi style
            NativeMethods.SetWindowPos(_scrcpyHwnd, IntPtr.Zero, 0, 0,
                _hostPanel.Width, _hostPanel.Height,
                NativeMethods.SWP_NOZORDER | NativeMethods.SWP_FRAMECHANGED);

            // Bước 5: Đồng bộ thread input (xử lý keyboard focus)
            try
            {
                uint scrcpyThreadId = NativeMethods.GetWindowThreadProcessId(_scrcpyHwnd, out _);
                uint formThreadId = NativeMethods.GetWindowThreadProcessId(_hostPanel.FindForm()!.Handle, out _);
                if (scrcpyThreadId != formThreadId)
                    NativeMethods.AttachThreadInput(scrcpyThreadId, formThreadId, true);
            }
            catch { /* Không critical */ }

            // Hook resize event
            _hostPanel.SizeChanged += HostPanel_SizeChanged;

            // Timer kiểm tra định kỳ trong trường hợp cửa sổ scrcpy bị lệch kích thước
            _resizeTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _resizeTimer.Tick += (s, e) =>
            {
                if (_scrcpyHwnd != IntPtr.Zero && _hostPanel != null && _hostPanel.Width > 0 && _hostPanel.Height > 0)
                {
                    if (NativeMethods.GetClientRect(_scrcpyHwnd, out var rc))
                    {
                        if (rc.Right - rc.Left != _hostPanel.Width || rc.Bottom - rc.Top != _hostPanel.Height)
                        {
                            NativeMethods.MoveWindow(_scrcpyHwnd, 0, 0, _hostPanel.Width, _hostPanel.Height, true);
                            NativeMethods.SetWindowPos(_scrcpyHwnd, IntPtr.Zero, 0, 0,
                                _hostPanel.Width, _hostPanel.Height,
                                NativeMethods.SWP_NOZORDER | NativeMethods.SWP_FRAMECHANGED);
                        }
                    }
                }
            };
            _resizeTimer.Start();

            Logger.Info("Scrcpy đã nhúng thành công vào Panel!");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Lỗi khởi chạy scrcpy", ex);
            return false;
        }
    }

    private void HostPanel_SizeChanged(object? sender, EventArgs e)
    {
        if (_scrcpyHwnd != IntPtr.Zero && _hostPanel != null && _hostPanel.Width > 0 && _hostPanel.Height > 0)
        {
            NativeMethods.MoveWindow(_scrcpyHwnd, 0, 0, _hostPanel.Width, _hostPanel.Height, true);
            NativeMethods.SetWindowPos(_scrcpyHwnd, IntPtr.Zero, 0, 0,
                _hostPanel.Width, _hostPanel.Height,
                NativeMethods.SWP_NOZORDER | NativeMethods.SWP_FRAMECHANGED);
        }
    }

    /// <summary>
    /// Chuyển đổi cửa sổ scrcpy đang chạy sang một Panel mới mà không cần khởi động lại tiến trình.
    /// </summary>
    public bool Reparent(Panel newHostPanel)
    {
        if (_scrcpyHwnd == IntPtr.Zero || _scrcpyProcess == null || _scrcpyProcess.HasExited)
            return false;

        try
        {
            if (_hostPanel != null)
                _hostPanel.SizeChanged -= HostPanel_SizeChanged;

            _hostPanel = newHostPanel;
            _hostPanel.SizeChanged += HostPanel_SizeChanged;

            NativeMethods.SetParent(_scrcpyHwnd, _hostPanel.Handle);
            if (_hostPanel.Width > 0 && _hostPanel.Height > 0)
            {
                NativeMethods.MoveWindow(_scrcpyHwnd, 0, 0, _hostPanel.Width, _hostPanel.Height, true);
                NativeMethods.SetWindowPos(_scrcpyHwnd, IntPtr.Zero, 0, 0,
                    _hostPanel.Width, _hostPanel.Height,
                    NativeMethods.SWP_NOZORDER | NativeMethods.SWP_FRAMECHANGED);
            }
            NativeMethods.ShowWindow(_scrcpyHwnd, NativeMethods.SW_SHOW);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Lỗi reparent scrcpy", ex);
            return false;
        }
    }

    public void Stop()
    {
        if (_resizeTimer != null)
        {
            _resizeTimer.Stop();
            _resizeTimer.Dispose();
            _resizeTimer = null;
        }

        if (_hostPanel != null)
            _hostPanel.SizeChanged -= HostPanel_SizeChanged;

        if (_scrcpyProcess != null && !_scrcpyProcess.HasExited)
        {
            try
            {
                if (_scrcpyHwnd != IntPtr.Zero)
                    NativeMethods.SendMessage(_scrcpyHwnd, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                _scrcpyProcess.WaitForExit(2000);
                if (!_scrcpyProcess.HasExited)
                    _scrcpyProcess.Kill();
            }
            catch { }
        }
        _scrcpyProcess = null;
        _scrcpyHwnd = IntPtr.Zero;
    }

    private string FindScrcpyPath(string? custom)
    {
        if (!string.IsNullOrEmpty(custom) && File.Exists(custom)) return custom;

        var appDir = AppDomain.CurrentDomain.BaseDirectory;

        // 1. Thư mục ứng dụng
        var local = Path.Combine(appDir, "scrcpy.exe");
        if (File.Exists(local)) return local;
        var sub = Path.Combine(appDir, "scrcpy", "scrcpy.exe");
        if (File.Exists(sub)) return sub;

        // 2. Đường dẫn phổ biến
        var commonPaths = new[]
        {
            @"C:\scrcpy\scrcpy.exe",
            @"C:\Program Files\scrcpy\scrcpy.exe",
            @"C:\Program Files (x86)\scrcpy\scrcpy.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scrcpy", "scrcpy.exe"),
        };
        foreach (var p in commonPaths)
            if (File.Exists(p)) return p;

        // 3. PATH hệ thống
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(';'))
        {
            var p = Path.Combine(dir.Trim(), "scrcpy.exe");
            if (File.Exists(p)) return p;
        }
        return string.Empty;
    }

    /// <summary>
    /// Tải scrcpy tự động từ GitHub releases vào thư mục app/scrcpy/.
    /// </summary>
    public static async Task<string?> DownloadScrcpyAsync(IProgress<string>? progress = null)
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var scrcpyDir = Path.Combine(appDir, "scrcpy");
        var scrcpyExe = Path.Combine(scrcpyDir, "scrcpy.exe");

        if (File.Exists(scrcpyExe)) return scrcpyExe;

        try
        {
            progress?.Report("Đang tải scrcpy từ GitHub...");
            Logger.Info("Bắt đầu tải scrcpy...");

            var zipUrl = "https://github.com/Genymobile/scrcpy/releases/download/v3.1/scrcpy-win64-v3.1.zip";
            var zipPath = Path.Combine(appDir, "scrcpy_download.zip");

            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromMinutes(5);
            var response = await http.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = File.Create(zipPath);

            var buffer = new byte[81920];
            long downloaded = 0;
            int read;
            while ((read = await stream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read));
                downloaded += read;
                if (totalBytes > 0)
                    progress?.Report($"Đang tải scrcpy... {downloaded * 100 / totalBytes}%");
            }
            fileStream.Close();

            progress?.Report("Đang giải nén scrcpy...");
            if (Directory.Exists(scrcpyDir))
                Directory.Delete(scrcpyDir, true);

            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, appDir);

            // Scrcpy zip thường chứa folder con, tìm và rename
            var extractedDirs = Directory.GetDirectories(appDir, "scrcpy-win64*");
            if (extractedDirs.Length > 0 && !Directory.Exists(scrcpyDir))
                Directory.Move(extractedDirs[0], scrcpyDir);

            // Cleanup zip
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            if (File.Exists(scrcpyExe))
            {
                Logger.Info($"Scrcpy đã tải thành công: {scrcpyExe}");
                progress?.Report("Scrcpy đã tải xong!");
                return scrcpyExe;
            }

            Logger.Error("Giải nén xong nhưng không tìm thấy scrcpy.exe");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Error("Lỗi tải scrcpy", ex);
            progress?.Report($"Lỗi tải scrcpy: {ex.Message}");
            return null;
        }
    }

    public void Dispose() => Stop();
}
