using System.Text.Json;
using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Services;

public class AiConfigService
{
    private readonly string _configPath;

    public AiConfigService()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlowPilot");
        Directory.CreateDirectory(appData);
        _configPath = Path.Combine(appData, "aiconfig.json");
    }

    public AiConfig Load()
    {
        if (!File.Exists(_configPath)) return new AiConfig();
        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<AiConfig>(json) ?? new AiConfig();
        }
        catch
        {
            return new AiConfig();
        }
    }

    public void Save(AiConfig config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}
