using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Services.Interfaces;

public enum DevicePlatform
{
    Android,
    iOS
}

public interface IDeviceController
{
    DevicePlatform Platform { get; }
    
    // Core actions
    Task ClickAsync(string deviceId, int x, int y, CancellationToken ct = default);
    Task SwipeAsync(string deviceId, int x1, int y1, int x2, int y2, int durationMs = 300, CancellationToken ct = default);
    Task InputTextAsync(string deviceId, string text, CancellationToken ct = default);
    
    // Lifecycle
    Task OpenAppAsync(string deviceId, string packageName, CancellationToken ct = default);
    Task ForceStopAppAsync(string deviceId, string packageName, CancellationToken ct = default);
    Task<string> GetCurrentForegroundAppAsync(string deviceId, CancellationToken ct = default);
    
    // File Transfer
    Task PushFileAsync(string deviceId, string localPath, string remotePath, CancellationToken ct = default);
    Task RemoveFileAsync(string deviceId, string remotePath, CancellationToken ct = default);
}
