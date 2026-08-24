using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrayMin.Core;

public sealed record Config
{
    public string HideHotkey { get; init; } = "Win+Shift+H";
    public string RestoreAllHotkey { get; init; } = "Win+Shift+G";
    public string[] BlockedExeNames { get; init; } = [];

    public static Config LoadOrDefault(string path)
    {
        try
        {
            if (!File.Exists(path)) return new Config();
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize(json, ConfigJson.Default.Config) ?? new Config();
            return config with
            {
                HideHotkey = string.IsNullOrWhiteSpace(config.HideHotkey)
                    ? "Win+Shift+H" : config.HideHotkey,
                RestoreAllHotkey = string.IsNullOrWhiteSpace(config.RestoreAllHotkey)
                    ? "Win+Shift+G" : config.RestoreAllHotkey,
                BlockedExeNames = config.BlockedExeNames?
                    .Where(static name => !string.IsNullOrWhiteSpace(name))
                    .ToArray() ?? [],
            };
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            Log.Write($"config read failed, using defaults: {ex.Message}");
            return new Config();
        }
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Config))]
internal sealed partial class ConfigJson : JsonSerializerContext;
