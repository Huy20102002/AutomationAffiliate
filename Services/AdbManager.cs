using System.Drawing;
using System.Diagnostics;
using System.Xml.Linq;
using System.Xml.XPath;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.Receivers;
using ShopeeVideoUploader.Helpers;
using ShopeeVideoUploader.Models;
namespace ShopeeVideoUploader.Services;

/// <summary>
/// Quản lý kết nối ADB và thực thi các lệnh điều khiển Android.
/// </summary>
public class AdbManager : IDisposable
{
    private AdbClient? _client;
    private AdbServer? _server;
    private bool _isInitialized;
    private string _adbPath = string.Empty;

    /// <summary>Đường dẫn adb.exe đang được dùng bởi ứng dụng.</summary>
    public string AdbPath => _adbPath;

    public async Task<bool> InitializeAsync(string? customAdbPath = null)
    {
        try
        {
            _adbPath = FindAdbPath(customAdbPath);
            if (string.IsNullOrEmpty(_adbPath))
            {
                Logger.Error("Không tìm thấy adb.exe!");
                return false;
            }
            Logger.Info($"ADB path: {_adbPath}");
            _server = new AdbServer();
            var result = await _server.StartServerAsync(_adbPath, false, CancellationToken.None);
            Logger.Info($"ADB Server: {result}");
            _client = new AdbClient();
            _isInitialized = true;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Lỗi khởi tạo ADB", ex);
            return false;
        }
    }

    public async Task<List<DeviceInfo>> GetConnectedDevicesAsync()
    {
        if (!_isInitialized || _client == null) return [];
        try
        {
            var devices = await _client.GetDevicesAsync();
            var result = new List<DeviceInfo>();
            foreach (var d in devices)
            {
                result.Add(new DeviceInfo
                {
                    Serial = d.Serial,
                    Model = d.Model ?? "Unknown",
                    State = d.State.ToString(),
                    AndroidVersion = await GetPropAsync(d, "ro.build.version.release")
                });
            }
            return result;
        }
        catch (Exception ex)
        {
            Logger.Error("Lỗi lấy devices", ex);
            return [];
        }
    }

    public async Task<DeviceData?> GetDeviceBySerialAsync(string serial)
    {
        if (_client == null) return null;
        var devices = await _client.GetDevicesAsync();
        return devices.FirstOrDefault(d => d.Serial == serial);
    }

    /// <summary>Lấy danh sách package ứng dụng bên thứ ba đang cài trên thiết bị.</summary>
    public async Task<List<string>> GetInstalledPackagesAsync(DeviceData device)
    {
        var output = await ShellAsync(device, "pm list packages");
        return output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim().Replace("package:", string.Empty, StringComparison.OrdinalIgnoreCase))
            .Where(packageName => !string.IsNullOrWhiteSpace(packageName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(packageName => packageName)
            .ToList();
    }

    /// <summary>
    /// Lấy package của ứng dụng đang hiển thị trên điện thoại.
    /// Người dùng có thể mở app bằng scrcpy rồi gọi hàm này để chọn app mà không cần tự nhớ package name.
    /// </summary>
    public async Task<string> GetForegroundPackageAsync(DeviceData device)
    {
        var output = await ShellAsync(device, "dumpsys window windows");
        var packageName = ExtractPackageName(output);
        if (!string.IsNullOrWhiteSpace(packageName)) return packageName;

        output = await ShellAsync(device, "dumpsys activity activities");
        return ExtractPackageName(output);
    }

    /// <summary>Tìm tâm của node Android theo XPath trong UI Automator XML.</summary>
    public async Task<Point?> FindUiNodeCenterAsync(
        DeviceData device,
        string xpath,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(xpath))
            throw new InvalidOperationException("Chưa cấu hình XPath cho bước Chạm.");

        const int maxAttempts = 12;
        const int retryDelayMs = 500;
        var idleStateFailures = 0;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            // Do not reuse an old hierarchy if UI Automator cannot dump the current screen.
            await ShellAsync(device, "rm -f /sdcard/flowpilot-ui.xml");
            var dumpResult = await ShellAsync(device, "uiautomator dump --compressed /sdcard/flowpilot-ui.xml");
            if (dumpResult.Contains("ERROR", StringComparison.OrdinalIgnoreCase) ||
                dumpResult.Contains("could not get idle state", StringComparison.OrdinalIgnoreCase))
            {
                idleStateFailures++;
                Logger.Warn($"[ADB] UI dump attempt {attempt + 1}/{maxAttempts} is not ready: {dumpResult}");
                
                if (idleStateFailures >= 3)
                {
                    throw new UiAutomationNotReadyException(
                        "UI Automator chưa sẵn sàng sau 3 lần thử (lỗi idle state). Cần thoát app và chạy lại workflow.");
                }

                if (attempt < maxAttempts - 1)
                    await Task.Delay(retryDelayMs, ct);
                continue;
            }
            var xml = await ShellAsync(device, "cat /sdcard/flowpilot-ui.xml");
            try
            {
                var document = XDocument.Parse(xml);
                var node = document.XPathSelectElement(xpath);
                if (node != null &&
                    !string.Equals(node.Attribute("enabled")?.Value, "false", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(node.Attribute("visible-to-user")?.Value, "false", StringComparison.OrdinalIgnoreCase))
                {
                    var bounds = node.Attribute("bounds")?.Value ?? string.Empty;
                    var match = System.Text.RegularExpressions.Regex.Match(
                        bounds, @"\[(\d+),(\d+)\]\[(\d+),(\d+)\]");
                    if (match.Success)
                    {
                        var left = int.Parse(match.Groups[1].Value);
                        var top = int.Parse(match.Groups[2].Value);
                        var right = int.Parse(match.Groups[3].Value);
                        var bottom = int.Parse(match.Groups[4].Value);
                        var center = new Point((left + right) / 2, (top + bottom) / 2);
                        Logger.Info($"[ADB] XPath tìm thấy node tại ({center.X},{center.Y}): {xpath}");
                        return center;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Không đọc được UI XML lần {attempt + 1}: {ex.Message}");
            }

            if (attempt < maxAttempts - 1)
                await Task.Delay(retryDelayMs, ct);
        }

        Logger.Warn($"[ADB] Không tìm thấy node theo XPath: {xpath}");
        return null;
    }

    /// <summary>Tìm tâm ảnh mẫu trên screenshot Android và retry đến timeout.</summary>
    public async Task<Point?> FindUiNodeCenterStrictAsync(
        DeviceData device,
        string xpath,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(xpath))
            throw new InvalidOperationException("Chưa cấu hình XPath cho bước Chạm.");

        const int maxAttempts = 12;
        const int retryDelayMs = 500;
        var idleStateFailures = 0;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            await ShellAsync(device, "rm -f /sdcard/flowpilot-ui.xml");
            var dumpResult = await ShellAsync(device, "uiautomator dump --compressed /sdcard/flowpilot-ui.xml");
            if (dumpResult.Contains("ERROR", StringComparison.OrdinalIgnoreCase) ||
                dumpResult.Contains("could not get idle state", StringComparison.OrdinalIgnoreCase))
            {
                idleStateFailures++;
                Logger.Warn($"[ADB] UI dump attempt {attempt + 1}/{maxAttempts} is not ready: {dumpResult}");
                
                if (idleStateFailures >= 3)
                {
                    throw new UiAutomationNotReadyException(
                        "UI Automator chưa sẵn sàng sau 3 lần thử (lỗi idle state). Cần thoát app và chạy lại workflow.");
                }

                if (attempt < maxAttempts - 1)
                    await Task.Delay(retryDelayMs, ct);
                continue;
            }

            var xml = await ShellAsync(device, "cat /sdcard/flowpilot-ui.xml");
            try
            {
                var document = XDocument.Parse(xml);
                var node = document.XPathSelectElement(xpath);
                if (node != null &&
                    !string.Equals(node.Attribute("enabled")?.Value, "false", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(node.Attribute("visible-to-user")?.Value, "false", StringComparison.OrdinalIgnoreCase))
                {
                    var bounds = node.Attribute("bounds")?.Value ?? string.Empty;
                    var match = System.Text.RegularExpressions.Regex.Match(bounds, @"\[(\d+),(\d+)\]\[(\d+),(\d+)\]");
                    if (match.Success)
                    {
                        var left = int.Parse(match.Groups[1].Value);
                        var top = int.Parse(match.Groups[2].Value);
                        var right = int.Parse(match.Groups[3].Value);
                        var bottom = int.Parse(match.Groups[4].Value);
                        var center = new Point((left + right) / 2, (top + bottom) / 2);
                        Logger.Info($"[ADB] XPath tÃ¬m tháº¥y node táº¡i ({center.X},{center.Y}): {xpath}");
                        return center;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"KhÃ´ng Ä‘á»c Ä‘Æ°á»£c UI XML láº§n {attempt + 1}: {ex.Message}");
            }

            if (attempt < maxAttempts - 1)
                await Task.Delay(retryDelayMs, ct);
        }

        if (idleStateFailures > 0)
            throw new UiAutomationNotReadyException(
                "UI Automator chưa sẵn sàng sau nhiều lần dump. Hãy thoát app và chạy lại workflow.");

        Logger.Warn($"[ADB] KhÃ´ng tÃ¬m tháº¥y node theo XPath: {xpath}");
        return null;
    }

    /// <summary>TÃ¬m tÃ¢m áº£nh máº«u trÃªn screenshot Android vÃ  retry Ä‘áº¿n timeout.</summary>
    public async Task<Point?> FindImageCenterAsync(
        DeviceData device,
        string imagePath,
        double threshold = 0.88,
        int timeoutMs = 5000,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            throw new FileNotFoundException($"Không tìm thấy ảnh mẫu chạm: {imagePath}");

        threshold = Math.Clamp(threshold, 0.5, 0.99);
        using var template = new Bitmap(imagePath);
        var deadline = DateTime.UtcNow.AddMilliseconds(Math.Max(0, timeoutMs));
        while (DateTime.UtcNow <= deadline)
        {
            ct.ThrowIfCancellationRequested();
            var png = await CaptureScreenshotPngAsync(device, ct);
            using var screenshotStream = new MemoryStream(png);
            using var screenshot = new Bitmap(screenshotStream);
            var result = FindTemplateCenter(screenshot, template, threshold);
            if (result != null)
            {
                Logger.Info($"[ADB] Ảnh mẫu tìm thấy tại ({result.Value.X},{result.Value.Y}): {Path.GetFileName(imagePath)}");
                return result;
            }

            if (timeoutMs <= 0) break;
            await Task.Delay(250, ct);
        }

        Logger.Warn($"[ADB] Không tìm thấy ảnh mẫu: {imagePath}");
        return null;
    }

    private async Task<byte[]> CaptureScreenshotPngAsync(DeviceData device, CancellationToken ct)
    {
        EnsureInit();
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _adbPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        process.StartInfo.ArgumentList.Add("-s");
        process.StartInfo.ArgumentList.Add(device.Serial);
        process.StartInfo.ArgumentList.Add("exec-out");
        process.StartInfo.ArgumentList.Add("screencap");
        process.StartInfo.ArgumentList.Add("-p");
        process.Start();
        using var output = new MemoryStream();
        await process.StandardOutput.BaseStream.CopyToAsync(output, ct);
        await process.WaitForExitAsync(ct);
        if (process.ExitCode != 0 || output.Length == 0)
            throw new InvalidOperationException("Không chụp được màn hình Android qua ADB.");
        return output.ToArray();
    }

    private static Point? FindTemplateCenter(Bitmap screen, Bitmap template, double threshold)
    {
        if (template.Width > screen.Width || template.Height > screen.Height)
            return null;

        var candidateStep = 5;
        var sampleStepX = Math.Max(1, template.Width / 16);
        var sampleStepY = Math.Max(1, template.Height / 16);
        var bestScore = 0d;
        Point? bestPoint = null;
        var maxDifference = (1d - threshold) * 255d * 3d;

        for (var y = 0; y <= screen.Height - template.Height; y += candidateStep)
        {
            for (var x = 0; x <= screen.Width - template.Width; x += candidateStep)
            {
                var difference = 0d;
                var samples = 0;
                for (var ty = 0; ty < template.Height; ty += sampleStepY)
                {
                    for (var tx = 0; tx < template.Width; tx += sampleStepX)
                    {
                        var a = screen.GetPixel(x + tx, y + ty);
                        var b = template.GetPixel(tx, ty);
                        difference += Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
                        samples++;
                    }
                }

                var score = 1d - difference / (samples * 255d * 3d);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPoint = new Point(x + template.Width / 2, y + template.Height / 2);
                }
                if (score >= threshold && difference / samples <= maxDifference)
                    return bestPoint;
            }
        }

        return bestScore >= threshold ? bestPoint : null;
    }

    private static string ExtractPackageName(string output)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            output ?? string.Empty,
            @"(?<package>[A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z0-9_]+)+)/");
        return match.Success ? match.Groups["package"].Value : string.Empty;
    }

    /// <summary>Tap tại (x,y) — dùng DeviceExtensions.ClickAsync</summary>
    public async Task TapAsync(DeviceData device, int x, int y)
    {
        EnsureInit();
        Logger.Info($"[ADB] Tap ({x},{y})");
        // DeviceExtensions.ClickAsync nhận Point thay vì int x, int y riêng lẻ
        await _client!.ClickAsync(device, new Point(x, y));
    }

    /// <summary>
    /// Gõ text hỗ trợ Tiếng Việt qua ADBKeyBoard.
    /// 
    /// === XỬ LÝ TIẾNG VIỆT & KÝ TỰ ĐẶC BIỆT ===
    /// "adb shell input text" KHÔNG hỗ trợ Unicode vì chỉ xử lý ASCII.
    /// Các ký tự có dấu (ă, ơ, ữ...) sẽ bị lỗi hoặc mất dấu.
    /// 
    /// GIẢI PHÁP: ADBKeyBoard (github.com/senzhk/ADBKeyBoard)
    /// - Virtual keyboard Android nhận text qua Broadcast Intent
    /// - Hỗ trợ TOÀN BỘ Unicode: Tiếng Việt, emoji, ký tự đặc biệt
    /// 
    /// FLOW:
    /// 1. am broadcast -a ADB_INPUT_TEXT --es msg "Tiếng Việt"
    /// 2. Fallback Base64: am broadcast -a ADB_INPUT_B64 --es msg {base64}
    /// 3. Fallback cuối: SendText (chỉ ASCII)
    /// </summary>
    public async Task InputTextAsync(DeviceData device, string text)
    {
        EnsureInit();
        if (string.IsNullOrEmpty(text)) return;
        Logger.Info($"[ADB] InputText: \"{(text.Length > 40 ? text[..40] + "..." : text)}\"");
        
        var keyboardPackage = await ShellAsync(device, "pm path com.android.adbkeyboard");
        if (!string.IsNullOrWhiteSpace(keyboardPackage))
        {
            try
            {
                var b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));
                var broadcastResult = await ShellAsync(device, $"am broadcast -a ADB_INPUT_B64 --es msg {b64}");
                Logger.Info($"[ADB] ADB_INPUT_B64: {broadcastResult}");
                return;
            }
            catch { Logger.Warn("Lỗi khi gửi lệnh ADBKeyBoard broadcast"); }
        }
        else
        {
            Logger.Warn("Thiết bị chưa cài ADBKeyboard, fallback sang SendTextAsync (có thể lỗi khoảng trắng hoặc Unicode)");
        }

        // Fallback: DeviceExtensions.SendTextAsync (chỉ hỗ trợ ASCII)
        // Thay thế khoảng trắng thành %s để tránh ngắt lệnh ADB shell input text
        var safeText = text.Replace(" ", "%s");
        await _client!.SendTextAsync(device, safeText);
    }

    /// <summary>Push file từ PC vào thiết bị qua SyncService</summary>
    public async Task PushFileAsync(DeviceData device, string localPath, string remotePath)
    {
        EnsureInit();
        Logger.Info($"[ADB] Push: {Path.GetFileName(localPath)} → {remotePath}");
        // SyncService.PushAsync signature:
        // PushAsync(Stream, string remotePath, UnixFileStatus, DateTimeOffset, Action<SyncProgressChangedEventArgs>?, bool, CancellationToken)
        using var syncService = new SyncService(device);
        using var stream = File.OpenRead(localPath);
        await syncService.PushAsync(
            stream,
            remotePath,
            UnixFileStatus.DefaultFileMode,  // permission 644
            DateTimeOffset.Now,
            null,                             // progress callback
            false,                            // isCancelled
            CancellationToken.None);
    }

    /// <summary>Kích hoạt Media Scanner để Shopee nhận file video mới</summary>
    /// <summary>Remove only FlowPilot-managed videos from the Android upload folder.</summary>
    public async Task<int> ClearFlowPilotVideosAsync(DeviceData device)
    {
        EnsureInit();
        const string command = "rm -f /sdcard/DCIM/Camera/*.mp4 /sdcard/DCIM/Camera/*.mov /sdcard/DCIM/Camera/*.mkv /sdcard/DCIM/Camera/*.avi";
        await ShellAsync(device, command);
        Logger.Info("[VIDEO] Cleared all video(s) from the device.");
        return 1;
    }

    public async Task TriggerMediaScanAsync(DeviceData device, string remotePath)
    {
        EnsureInit();
        // Broadcast scan file (Android cũ)
        await ShellAsync(device, $"am broadcast -a android.intent.action.MEDIA_SCANNER_SCAN_FILE -d file://{remotePath}");
        // Content provider scan (Android 10+)
        await ShellAsync(device, "content call --method scan_volume --uri content://media --arg external_primary");
    }

    /// <summary>Vuốt màn hình — dùng DeviceExtensions.SwipeAsync với Point</summary>
    public async Task SwipeAsync(DeviceData device, int x1, int y1, int x2, int y2, int durationMs = 300)
    {
        EnsureInit();
        Logger.Info($"[ADB] Swipe ({x1},{y1})→({x2},{y2}) {durationMs}ms");
        // DeviceExtensions.SwipeAsync nhận Point start, Point end, long duration
        await _client!.SwipeAsync(device, new Point(x1, y1), new Point(x2, y2), (long)durationMs);
    }

    /// <summary>Mở app bằng package name</summary>
    public async Task OpenAppAsync(DeviceData device, string packageName)
    {
        EnsureInit();
        Logger.Info($"[ADB] OpenApp: {packageName}");
        await ShellAsync(device, $"monkey -p {packageName} -c android.intent.category.LAUNCHER 1");
    }

    /// <summary>Gửi key event (BACK=4, HOME=3, ENTER=66)</summary>
    public async Task SendKeyEventAsync(DeviceData device, string keycode)
    {
        EnsureInit();
        await ShellAsync(device, $"input keyevent {keycode}");
    }

    /// <summary>Setup ADBKeyBoard làm IME mặc định cho gõ Unicode</summary>
    public async Task SetupAdbKeyboardAsync(DeviceData device)
    {
        var keyboardPackage = await ShellAsync(device, "pm path com.android.adbkeyboard");
        if (string.IsNullOrWhiteSpace(keyboardPackage))
            throw new InvalidOperationException("Thiết bị chưa cài ADBKeyboard.");

        await ShellAsync(device, "ime enable com.android.adbkeyboard/.AdbIME");
        await ShellAsync(device, "ime set com.android.adbkeyboard/.AdbIME");

        // Android đổi IME bất đồng bộ; gửi broadcast ngay lập tức thường bị rơi
        // vào IME cũ và không nhập gì. Chờ đến khi hệ thống xác nhận IME mới.
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var currentIme = await ShellAsync(device, "settings get secure default_input_method");
            if (currentIme.Contains("com.android.adbkeyboard/.AdbIME", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Info("[ADB] ADBKeyBoard đã sẵn sàng làm IME mặc định");
                return;
            }

            await Task.Delay(100);
        }

        throw new InvalidOperationException("Android chưa chuyển sang ADBKeyboard sau 3 giây.");
    }

    /// <summary>Chạy shell command và trả về output</summary>
    public async Task<string> ShellAsync(DeviceData device, string command)
    {
        EnsureInit();
        var receiver = new ConsoleOutputReceiver();
        await _client!.ExecuteRemoteCommandAsync(command, device, receiver);
        return receiver.ToString().Trim();
    }

    /// <summary>
    /// Đọc luồng sự kiện input của Android. Dùng để ghi thao tác chạm/phím
    /// từ thiết bị đang điều khiển bằng Scrcpy thành các bước workflow.
    /// </summary>
    public async Task StreamInputEventsAsync(DeviceData device, Action<string> onLine, CancellationToken ct)
    {
        EnsureInit();
        await _client!.ExecuteRemoteCommandAsync(
            "getevent -lt",
            device,
            line =>
            {
                onLine(line);
                return !ct.IsCancellationRequested;
            },
            ct);
    }

    /// <summary>Buộc ứng dụng hiện tại thoát ra để workflow có thể chạy lại từ đầu.</summary>
    public async Task ForceStopAppAsync(DeviceData device, string packageName)
    {
        EnsureInit();
        if (string.IsNullOrWhiteSpace(packageName))
            return;

        Logger.Info($"[ADB] Force-stop: {packageName}");
        await ShellAsync(device, $"am force-stop {packageName}");
    }

    private async Task<string> GetPropAsync(DeviceData device, string prop)
    {
        try
        {
            var r = new ConsoleOutputReceiver();
            await _client!.ExecuteRemoteCommandAsync($"getprop {prop}", device, r);
            return r.ToString().Trim();
        }
        catch { return "?"; }
    }

    private string FindAdbPath(string? custom)
    {
        if (!string.IsNullOrEmpty(custom) && File.Exists(custom)) return custom;

        // 1. Thư mục ứng dụng
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var local = Path.Combine(appDir, "adb.exe");
        if (File.Exists(local)) return local;
        var pt = Path.Combine(appDir, "platform-tools", "adb.exe");
        if (File.Exists(pt)) return pt;

        // 2. Các đường dẫn phổ biến trên máy Windows
        var commonPaths = new[]
        {
            @"C:\Program Files (x86)\xiaowei_android\tools\adb.exe",    // Xiaowei (数卫)
            @"C:\Program Files\xiaowei_android\tools\adb.exe",
            @"C:\Program Files\i4Tools9\files\adb\adb.exe",             // i4Tools
            @"C:\platform-tools\adb.exe",                               // Google platform-tools
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Android", "Sdk", "platform-tools", "adb.exe"),         // Android SDK
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "scrcpy", "adb.exe"),                                   // Scrcpy bundle
        };
        foreach (var p in commonPaths)
            if (File.Exists(p)) return p;

        // 3. PATH hệ thống
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(';'))
        {
            var p = Path.Combine(dir.Trim(), "adb.exe");
            if (File.Exists(p)) return p;
        }
        return string.Empty;
    }




    private void EnsureInit()
    {
        if (!_isInitialized || _client == null)
            throw new InvalidOperationException("ADB chưa khởi tạo.");
    }

    public void Dispose()
    {
        _client = null;
        _server = null;
        _isInitialized = false;
    }
}
