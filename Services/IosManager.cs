using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ShopeeVideoUploader.Models;
using ShopeeVideoUploader.Services.Interfaces;
using ShopeeVideoUploader.Helpers;

namespace ShopeeVideoUploader.Services;

/// <summary>
/// Quản lý thiết bị iOS qua WebDriverAgent (WDA) do Ái Tư Trợ Thủ/Panda Helper cung cấp.
/// Kết nối qua cổng HTTP cục bộ (ví dụ: http://localhost:8100).
/// </summary>
public class IosManager : IDeviceController, IDisposable
{
    public DevicePlatform Platform => DevicePlatform.iOS;
    private static readonly HttpClient _httpClient = new HttpClient();
    
    // Lưu trữ SessionID hiện tại cho mỗi cổng để tránh tạo lại nhiều lần
    private readonly Dictionary<string, string> _wdaSessions = new();

    private async Task<string> GetSessionIdAsync(string portStr, CancellationToken ct)
    {
        if (_wdaSessions.TryGetValue(portStr, out var existingSession))
        {
            return existingSession;
        }

        var url = $"http://127.0.0.1:{portStr}/session";
        var payload = new { capabilities = new { } };
        var json = JsonConvert.SerializeObject(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync(ct);
            var result = JObject.Parse(responseString);
            var sessionId = result["value"]?["sessionId"]?.ToString();
            
            if (string.IsNullOrEmpty(sessionId))
            {
                // Fallback cho một số bản WDA cũ trả về sessionId ở root
                sessionId = result["sessionId"]?.ToString();
            }

            if (!string.IsNullOrEmpty(sessionId))
            {
                _wdaSessions[portStr] = sessionId;
                return sessionId;
            }
            throw new Exception("Không thể lấy sessionId từ WDA");
        }
        catch (Exception ex)
        {
            Logger.Error($"[WDA] Failed to get session on port {portStr}: {ex.Message}");
            throw;
        }
    }

    private async Task SendWdaCommandAsync(string portStr, string endpoint, object payload, CancellationToken ct)
    {
        var sessionId = await GetSessionIdAsync(portStr, ct);
        var url = $"http://127.0.0.1:{portStr}/session/{sessionId}{endpoint}";
        var json = JsonConvert.SerializeObject(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Logger.Error($"[WDA] Lỗi khi gửi lệnh {endpoint} tới cổng {portStr}: {ex.Message}");
            // Clear session to force reconnect next time
            _wdaSessions.Remove(portStr);
            throw;
        }
    }

    // ── Auto-start tidevice ──
    private static string? _tideviceExePath;
    private System.Diagnostics.Process? _tideviceProcess;

    private static string FindTideviceExe()
    {
        if (!string.IsNullOrEmpty(_tideviceExePath) && File.Exists(_tideviceExePath))
            return _tideviceExePath;

        var candidates = new List<string>();

        // Python Store app (Windows Store)
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var packagesDir = Path.Combine(localAppData, "Packages");
        if (Directory.Exists(packagesDir))
        {
            try
            {
                foreach (var pythonDir in Directory.GetDirectories(packagesDir, "PythonSoftwareFoundation*"))
                {
                    var scriptsDir = Path.Combine(pythonDir, "LocalCache", "local-packages");
                    if (Directory.Exists(scriptsDir))
                    {
                        foreach (var versionDir in Directory.GetDirectories(scriptsDir, "Python*"))
                        {
                            candidates.Add(Path.Combine(versionDir, "Scripts", "tidevice.exe"));
                        }
                    }
                }
            }
            catch { /* ignore scan errors */ }
        }

        // Standard pip install locations
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        candidates.Add(Path.Combine(userProfile, "AppData", "Roaming", "Python", "Python312", "Scripts", "tidevice.exe"));
        candidates.Add(Path.Combine(userProfile, "AppData", "Roaming", "Python", "Python311", "Scripts", "tidevice.exe"));
        candidates.Add(Path.Combine(userProfile, "AppData", "Local", "Programs", "Python", "Python312", "Scripts", "tidevice.exe"));
        candidates.Add(Path.Combine(userProfile, "AppData", "Local", "Programs", "Python", "Python311", "Scripts", "tidevice.exe"));

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                _tideviceExePath = path;
                Logger.Info($"[iOS] Tìm thấy tidevice: {path}");
                return path;
            }
        }

        return string.Empty;
    }

    private void StartTideviceProxyIfNeed()
    {
        if (_tideviceProcess != null && !_tideviceProcess.HasExited)
            return; // Đang chạy rồi

        var exePath = FindTideviceExe();
        if (string.IsNullOrEmpty(exePath))
        {
            Logger.Warn("[iOS] Không tìm thấy tidevice.exe. Tính năng auto-start WDA bị bỏ qua.");
            return;
        }

        Logger.Info("[iOS] Đang tự động khởi chạy tidevice wdaproxy...");
        try
        {
            _tideviceProcess = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = "wdaproxy -B com.facebook.WebDriverAgentRunner.xctrunner -p 8100",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            
            // Bỏ qua output để tránh block process
            _tideviceProcess.OutputDataReceived += (s, e) => { };
            _tideviceProcess.ErrorDataReceived += (s, e) => { };
            
            _tideviceProcess.Start();
            _tideviceProcess.BeginOutputReadLine();
            _tideviceProcess.BeginErrorReadLine();
            
            Logger.Info("[iOS] Đã khởi chạy tidevice ngầm thành công.");
        }
        catch (Exception ex)
        {
            Logger.Error($"[iOS] Lỗi khởi chạy tidevice: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            if (_tideviceProcess != null && !_tideviceProcess.HasExited)
            {
                _tideviceProcess.Kill();
                _tideviceProcess.Dispose();
                _tideviceProcess = null;
                Logger.Info("[iOS] Đã tắt tidevice wdaproxy.");
            }
        }
        catch { }
    }

    /// <summary>
    /// Tìm kiếm các thiết bị WDA đang chạy trên các cổng từ 8100 đến 8105.
    /// </summary>
    public async Task<List<string>> GetConnectedWdaPortsAsync(CancellationToken ct = default)
    {
        var activePorts = await ScanPortsAsync(ct);
        
        if (activePorts.Count == 0)
        {
            // Nếu không thấy port nào, thử tự động bật tidevice
            StartTideviceProxyIfNeed();
            if (_tideviceProcess != null && !_tideviceProcess.HasExited)
            {
                Logger.Info("[iOS] Đang chờ WDA khởi động...");
                // Chờ một chút để WDA kịp khởi động
                for (int i = 0; i < 5; i++)
                {
                    await Task.Delay(2000, ct);
                    activePorts = await ScanPortsAsync(ct);
                    if (activePorts.Count > 0)
                        break;
                }
            }
        }
        
        return activePorts;
    }

    private async Task<List<string>> ScanPortsAsync(CancellationToken ct)
    {
        var activePorts = new List<string>();
        for (int port = 8100; port <= 8105; port++)
        {
            try
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(500); // Timeout 500ms cho mỗi cổng
                
                var response = await _httpClient.GetAsync($"http://127.0.0.1:{port}/status", cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    activePorts.Add(port.ToString());
                    Logger.Info($"[WDA] Tìm thấy thiết bị iOS tại cổng {port}");
                }
            }
            catch
            {
                // Bỏ qua các cổng không phản hồi
            }
        }
        return activePorts;
    }

    public async Task ClickAsync(string deviceId, int x, int y, CancellationToken ct = default)
    {
        Logger.Info($"[WDA] Tap at {x}, {y} on {deviceId}");
        var payload = new { x = x, y = y };
        await SendWdaCommandAsync(deviceId, "/wda/tap/0", payload, ct);
    }

    public async Task SwipeAsync(string deviceId, int x1, int y1, int x2, int y2, int durationMs = 300, CancellationToken ct = default)
    {
        Logger.Info($"[WDA] Swipe from {x1},{y1} to {x2},{y2} duration {durationMs}ms on {deviceId}");
        // Lưu ý: WDA yêu cầu duration tính bằng giây dưới dạng float
        var payload = new { fromX = x1, fromY = y1, toX = x2, toY = y2, duration = durationMs / 1000.0 };
        await SendWdaCommandAsync(deviceId, "/wda/dragfromtoforduration", payload, ct);
    }

    public async Task InputTextAsync(string deviceId, string text, CancellationToken ct = default)
    {
        Logger.Info($"[WDA] Type text: {text} on {deviceId}");
        var payload = new { value = text.ToCharArray().Select(c => c.ToString()).ToArray() };
        await SendWdaCommandAsync(deviceId, "/wda/keys", payload, ct);
    }

    public async Task OpenAppAsync(string deviceId, string packageName, CancellationToken ct = default)
    {
        Logger.Info($"[WDA] Open App: {packageName} on {deviceId}");
        var payload = new { bundleId = packageName };
        await SendWdaCommandAsync(deviceId, "/wda/apps/launch", payload, ct);
    }

    public async Task ForceStopAppAsync(string deviceId, string packageName, CancellationToken ct = default)
    {
        Logger.Info($"[WDA] Kill App: {packageName} on {deviceId}");
        var payload = new { bundleId = packageName };
        
        try 
        {
            await SendWdaCommandAsync(deviceId, "/wda/apps/terminate", payload, ct);
        }
        catch (Exception ex)
        {
            Logger.Warn($"[WDA] Không thể kill app {packageName}: {ex.Message}. Bỏ qua.");
        }
    }

    public async Task<string> GetCurrentForegroundAppAsync(string deviceId, CancellationToken ct = default)
    {
        try
        {
            var sessionId = await GetSessionIdAsync(deviceId, ct);
            var response = await _httpClient.GetAsync($"http://127.0.0.1:{deviceId}/session/{sessionId}/wda/activeAppInfo", ct);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                var result = JObject.Parse(content);
                return result["value"]?["bundleId"]?.ToString() ?? "";
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"[WDA] Lỗi lấy app đang chạy: {ex.Message}");
        }
        return "";
    }

    // ── pymobiledevice3 AFC integration ──

    private static string? _pmdExePath;

    private static string FindPymobiledevice3Exe()
    {
        if (!string.IsNullOrEmpty(_pmdExePath) && File.Exists(_pmdExePath))
            return _pmdExePath;

        // Tìm pymobiledevice3.exe trong các thư mục Python Scripts phổ biến
        var candidates = new List<string>();

        // Python Store app (Windows Store)
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var packagesDir = Path.Combine(localAppData, "Packages");
        if (Directory.Exists(packagesDir))
        {
            try
            {
                foreach (var pythonDir in Directory.GetDirectories(packagesDir, "PythonSoftwareFoundation*"))
                {
                    var scriptsDir = Path.Combine(pythonDir, "LocalCache", "local-packages");
                    if (Directory.Exists(scriptsDir))
                    {
                        foreach (var versionDir in Directory.GetDirectories(scriptsDir, "Python*"))
                        {
                            candidates.Add(Path.Combine(versionDir, "Scripts", "pymobiledevice3.exe"));
                        }
                    }
                }
            }
            catch { /* ignore scan errors */ }
        }

        // Standard pip install locations
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        candidates.Add(Path.Combine(userProfile, "AppData", "Roaming", "Python", "Python312", "Scripts", "pymobiledevice3.exe"));
        candidates.Add(Path.Combine(userProfile, "AppData", "Roaming", "Python", "Python311", "Scripts", "pymobiledevice3.exe"));
        candidates.Add(Path.Combine(userProfile, "AppData", "Local", "Programs", "Python", "Python312", "Scripts", "pymobiledevice3.exe"));
        candidates.Add(Path.Combine(userProfile, "AppData", "Local", "Programs", "Python", "Python311", "Scripts", "pymobiledevice3.exe"));

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                _pmdExePath = path;
                Logger.Info($"[iOS] Tìm thấy pymobiledevice3: {path}");
                return path;
            }
        }

        throw new FileNotFoundException(
            "Không tìm thấy pymobiledevice3.exe. Hãy cài đặt bằng lệnh: pip install pymobiledevice3");
    }

    private static async Task<string> RunPmdCommandAsync(string arguments, CancellationToken ct, int timeoutMs = 60000)
    {
        var exePath = FindPymobiledevice3Exe();
        Logger.Info($"[iOS] pymobiledevice3 {arguments}");

        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync(ct);
        var errorTask = process.StandardError.ReadToEndAsync(ct);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeoutMs);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            try { process.Kill(); } catch { }
            throw new TimeoutException($"pymobiledevice3 timed out sau {timeoutMs / 1000}s");
        }

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            var errorMsg = string.IsNullOrWhiteSpace(error) ? output : error;
            Logger.Error($"[iOS] pymobiledevice3 lỗi (exit {process.ExitCode}): {errorMsg.Trim()}");
            throw new InvalidOperationException($"pymobiledevice3 lỗi: {errorMsg.Trim()}");
        }

        return output;
    }

    public async Task PushFileAsync(string deviceId, string localPath, string remotePath, CancellationToken ct = default)
    {
        if (!File.Exists(localPath))
            throw new FileNotFoundException($"File không tồn tại: {localPath}");

        // Đẩy file vào /var/mobile/Media/DCIM/100APPLE/ (Camera Roll)
        var remoteDir = "DCIM/100APPLE";
        Logger.Info($"[iOS] Đẩy file {Path.GetFileName(localPath)} → {remoteDir}/");

        await RunPmdCommandAsync($"afc push \"{localPath}\" \"{remoteDir}\"", ct, timeoutMs: 120000);

        Logger.Info($"[iOS] Đã đẩy file thành công vào Camera Roll");
    }

    public async Task RemoveFileAsync(string deviceId, string remotePath, CancellationToken ct = default)
    {
        Logger.Info($"[iOS] Xóa file: {remotePath}");

        try
        {
            await RunPmdCommandAsync($"afc rm \"{remotePath}\"", ct);
            Logger.Info($"[iOS] Đã xóa: {remotePath}");
        }
        catch (Exception ex)
        {
            Logger.Warn($"[iOS] Không thể xóa {remotePath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Liệt kê các file trong thư mục DCIM/100APPLE trên thiết bị iOS.
    /// </summary>
    public async Task<List<string>> ListDcimFilesAsync(CancellationToken ct = default)
    {
        try
        {
            var output = await RunPmdCommandAsync("afc ls \"DCIM/100APPLE\"", ct);
            return output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        catch (Exception ex)
        {
            Logger.Warn($"[iOS] Không thể liệt kê file DCIM: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Xóa các video flowpilot_ đã đẩy trước đó khỏi Camera Roll trên iOS.
    /// </summary>
    public async Task ClearFlowPilotVideosAsync(CancellationToken ct = default)
    {
        Logger.Info("[iOS] Đang xóa các video flowpilot_ cũ trên thiết bị iOS...");
        var files = await ListDcimFilesAsync(ct);
        var flowpilotFiles = files.Where(f => f.StartsWith("flowpilot_", StringComparison.OrdinalIgnoreCase)).ToList();

        if (flowpilotFiles.Count == 0)
        {
            Logger.Info("[iOS] Không có video flowpilot_ cũ để xóa.");
            return;
        }

        foreach (var file in flowpilotFiles)
        {
            try
            {
                await RunPmdCommandAsync($"afc rm \"DCIM/100APPLE/{file}\"", ct);
                Logger.Info($"[iOS] Đã xóa: {file}");
            }
            catch (Exception ex)
            {
                Logger.Warn($"[iOS] Không xóa được {file}: {ex.Message}");
            }
        }

        Logger.Info($"[iOS] Đã xóa {flowpilotFiles.Count} video cũ.");
    }

    /// <summary>
    /// Chụp ảnh màn hình thiết bị iOS qua WebDriverAgent (WDA).
    /// </summary>
    public async Task<byte[]?> TakeScreenshotBytesAsync(string portStr = "8100", CancellationToken ct = default)
    {
        try
        {
            var url = $"http://127.0.0.1:{portStr}/screenshot";
            using var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;
            var responseString = await response.Content.ReadAsStringAsync(ct);
            var result = JObject.Parse(responseString);
            var base64 = result["value"]?.ToString();
            if (!string.IsNullOrEmpty(base64))
            {
                return Convert.FromBase64String(base64);
            }
            return null;
        }
        catch (Exception ex)
        {
            Logger.Warn($"[iOS] Lỗi chụp màn hình: {ex.Message}");
            return null;
        }
    }
}
