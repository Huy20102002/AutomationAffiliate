using System.Text.Json;
using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Services;

public class TelegramConfigService
{
    private readonly string _configPath;

    public TelegramConfigService()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlowPilot");
        Directory.CreateDirectory(appData);
        _configPath = Path.Combine(appData, "telegramconfig.json");
    }

    public TelegramConfig Load()
    {
        if (!File.Exists(_configPath)) return new TelegramConfig();
        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<TelegramConfig>(json) ?? new TelegramConfig();
        }
        catch
        {
            return new TelegramConfig();
        }
    }

    public void Save(TelegramConfig config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        var tempPath = _configPath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _configPath, overwrite: true);
    }
}
