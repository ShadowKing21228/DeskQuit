using System;
using System.IO;
using System.Text.Json;
using DeskQuit.Models;
using DeskQuit.Services.Logging;

namespace DeskQuit.Services;

public class ConfigService
{
    private readonly string _configPath;

    public ConfigService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "DeskQuit");
        Directory.CreateDirectory(appFolder);
        _configPath = Path.Combine(appFolder, "settings.json");
    }

    public void SaveConfig(GlobalConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
            AppLogger.Info("Configuration saved successfully.", nameof(ConfigService));
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to save configuration: {ex.Message}", nameof(ConfigService));
        }
    }

    public GlobalConfig LoadConfig()
    {
        if (!File.Exists(_configPath))
        {
            AppLogger.Info("Configuration file not found. Loading default settings.", nameof(ConfigService));
            return new GlobalConfig();
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            var config = JsonSerializer.Deserialize<GlobalConfig>(json);
            AppLogger.Info("Configuration loaded successfully.", nameof(ConfigService));
            return config ?? new GlobalConfig();
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to load or parse configuration: {ex.Message}", nameof(ConfigService));
            return new GlobalConfig();
        }
    }
}
