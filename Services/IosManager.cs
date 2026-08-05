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
public class IosManager : IDeviceController
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

    /// <summary>
    /// Tìm kiếm các thiết bị WDA đang chạy trên các cổng từ 8100 đến 8105.
    /// </summary>
    public async Task<List<string>> GetConnectedWdaPortsAsync(CancellationToken ct = default)
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

    public async Task PushFileAsync(string deviceId, string localPath, string remotePath, CancellationToken ct = default)
    {
        Logger.Warn($"[WDA] KHÔNG HỖ TRỢ đẩy file trực tiếp vào iOS. Vui lòng tự copy file hoặc cài đặt tidevice.");
        throw new NotSupportedException("Chưa hỗ trợ đẩy video tự động trên iPhone. Vui lòng dùng Ái Tư Trợ Thủ copy vào thư viện ảnh trước.");
    }

    public async Task RemoveFileAsync(string deviceId, string remotePath, CancellationToken ct = default)
    {
        Logger.Warn($"[WDA] KHÔNG HỖ TRỢ xóa file trên iOS.");
        await Task.CompletedTask;
    }
}
