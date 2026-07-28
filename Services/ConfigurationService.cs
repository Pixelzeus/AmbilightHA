using System;
using System.IO;
using System.Text.Json;
using AmbilightHA.Models;

namespace AmbilightHA.Services;

public static class ConfigurationService
{
    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config != null) return config;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigurationService] Failed to load config: {ex.Message}");
        }

        return new AppConfig();
    }

    public static void Save(AppConfig config)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigurationService] Failed to save config: {ex.Message}");
        }
    }
}
