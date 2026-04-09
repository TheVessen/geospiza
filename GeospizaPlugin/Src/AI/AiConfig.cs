using System;
using System.IO;
using Newtonsoft.Json;

namespace GeospizaPlugin.AI;

public class AiConfig
{
    public string SelectedModel { get; set; } = "gemma4:e4b";
    public bool SetupComplete { get; set; } = false;

    private static string ConfigPath
    {
        get
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "Geospiza");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "ai-config.json");
        }
    }

    public static AiConfig Load()
    {
        try
        {
            var path = ConfigPath;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<AiConfig>(json) ?? new AiConfig();
            }
        }
        catch
        {
            // Return defaults if config is unreadable
        }

        return new AiConfig();
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        catch
        {
            // Best-effort save
        }
    }
}
