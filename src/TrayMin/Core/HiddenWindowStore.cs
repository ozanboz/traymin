using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrayMin.Core;

public sealed record HiddenWindowRecord
{
    public long Hwnd { get; init; }
    public uint Pid { get; init; }
    public long ProcessStartTicks { get; init; }
    public string ExePath { get; init; } = "";
    public string Title { get; init; } = "";
    public int ShowCmd { get; init; }
}

public sealed class HiddenWindowStore(string path, string backupPath)
{
    public IReadOnlyList<HiddenWindowRecord> Load()
    {
        if (!File.Exists(path)) return [];
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize(json, StoreJson.Default.ListHiddenWindowRecord) ?? [];
        }
        catch (JsonException ex)
        {
            Log.Write($"hidden.json corrupt, backing up: {ex.Message}");
            TryBackup();
            return [];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Write($"hidden.json unreadable: {ex.Message}");
            return [];
        }
    }

    public void Save(IReadOnlyCollection<HiddenWindowRecord> records)
    {
        var temp = path + ".tmp";
        var json = JsonSerializer.Serialize(new List<HiddenWindowRecord>(records),
            StoreJson.Default.ListHiddenWindowRecord);

        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(stream))
        {
            writer.Write(json);
            writer.Flush();
            stream.Flush(flushToDisk: true);
        }

        File.Move(temp, path, overwrite: true);
    }

    private void TryBackup()
    {
        try { File.Move(path, backupPath, overwrite: true); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Write($"hidden.json backup failed: {ex.Message}");
        }
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(List<HiddenWindowRecord>))]
internal sealed partial class StoreJson : JsonSerializerContext;
