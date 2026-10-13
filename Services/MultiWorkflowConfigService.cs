using System;
using System.IO;
using System.Text.Json;
using ShopeeVideoUploader.Models;

namespace ShopeeVideoUploader.Services;

public sealed class MultiWorkflowConfigService
{
    private readonly string _configPath;

    public MultiWorkflowConfigService()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlowPilot");
        Directory.CreateDirectory(appData);
        _configPath = Path.Combine(appData, "multi_workflow_config.json");
    }

    public MultiWorkflowConfig Load()
    {
        if (!File.Exists(_configPath)) return new MultiWorkflowConfig();
        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<MultiWorkflowConfig>(json) ?? new MultiWorkflowConfig();
        }
        catch
        {
            return new MultiWorkflowConfig();
        }
    }

    public void Save(MultiWorkflowConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            var tempPath = _configPath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _configPath, overwrite: true);
        }
        catch { }
    }
}
