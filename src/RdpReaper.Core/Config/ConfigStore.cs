using System.IO;
using System.Text.Json;
using RdpReaper.Core.Infrastructure;

namespace RdpReaper.Core.Config;

public static class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static AppConfig LoadOrCreate()
    {
        var path = AppPaths.ConfigPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        if (!File.Exists(path))
        {
            var created = CreateDefault();
            Save(created);
            return created;
        }

        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? CreateDefault();
        Normalize(config);
        return config;
    }

    public static void Save(AppConfig config)
    {
        Normalize(config);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(AppPaths.ConfigPath, json);
    }

    private static AppConfig CreateDefault()
    {
        return new AppConfig
        {
            DatabasePath = AppPaths.DatabasePath
        };
    }

    private static void Normalize(AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.DatabasePath))
        {
            config.DatabasePath = AppPaths.DatabasePath;
        }

        config.AllowIpList = NormalizeList(config.AllowIpList);
        config.BlockIpList = NormalizeList(config.BlockIpList);
        config.AllowSubnetList = NormalizeList(config.AllowSubnetList);
        config.BlockSubnetList = NormalizeList(config.BlockSubnetList);
    }

    private static List<string> NormalizeList(List<string>? list)
    {
        if (list == null || list.Count == 0)
        {
            return new List<string>();
        }

        return list
            .Select(entry => entry.Trim())
            .Where(entry => !string.IsNullOrWhiteSpace(entry))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
