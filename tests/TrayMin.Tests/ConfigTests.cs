using TrayMin.Core;
using Xunit;

namespace TrayMin.Tests;

public class ConfigTests
{
    [Fact]
    public void Returns_defaults_when_file_missing()
    {
        var path = Path.Combine(Path.GetTempPath(), $"traymin-cfg-{Guid.NewGuid():N}.json");
        var cfg = Config.LoadOrDefault(path);

        Assert.Equal("Win+Shift+H", cfg.HideHotkey);
        Assert.Equal("Win+Shift+G", cfg.RestoreAllHotkey);
        Assert.Empty(cfg.BlockedExeNames);
    }

    [Fact]
    public void Reads_values_from_file()
    {
        var path = Path.Combine(Path.GetTempPath(), $"traymin-cfg-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """
        {
          "hideHotkey": "Ctrl+Alt+M",
          "restoreAllHotkey": "Ctrl+Alt+N",
          "blockedExeNames": ["discord.exe"]
        }
        """);
        try
        {
            var cfg = Config.LoadOrDefault(path);
            Assert.Equal("Ctrl+Alt+M", cfg.HideHotkey);
            Assert.Equal("Ctrl+Alt+N", cfg.RestoreAllHotkey);
            Assert.Equal(["discord.exe"], cfg.BlockedExeNames);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Normalizes_null_members_from_valid_json()
    {
        var path = Path.Combine(Path.GetTempPath(), $"traymin-cfg-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """
        {
          "hideHotkey": null,
          "restoreAllHotkey": null,
          "blockedExeNames": null
        }
        """);
        try
        {
            var cfg = Config.LoadOrDefault(path);
            Assert.Equal("Win+Shift+H", cfg.HideHotkey);
            Assert.Equal("Win+Shift+G", cfg.RestoreAllHotkey);
            Assert.Empty(cfg.BlockedExeNames);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Falls_back_to_defaults_when_file_is_corrupt()
    {
        var path = Path.Combine(Path.GetTempPath(), $"traymin-cfg-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{ this is not json");
        try
        {
            var cfg = Config.LoadOrDefault(path);
            Assert.Equal("Win+Shift+H", cfg.HideHotkey);
            Assert.True(File.Exists(path));
        }
        finally { File.Delete(path); }
    }
}
