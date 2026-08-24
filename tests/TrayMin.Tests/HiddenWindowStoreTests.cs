using TrayMin.Core;
using Xunit;

namespace TrayMin.Tests;

public class HiddenWindowStoreTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("traymin-store").FullName;

    private HiddenWindowStore NewStore(out string path, out string backup)
    {
        path = Path.Combine(_dir, "hidden.json");
        backup = path + ".bak";
        return new HiddenWindowStore(path, backup);
    }

    private static HiddenWindowRecord Sample(long hwnd = 66830) => new()
    {
        Hwnd = hwnd,
        Pid = 4242,
        ProcessStartTicks = 130000000000000000,
        ExePath = @"C:\Windows\System32\notepad.exe",
        Title = "Untitled - Notepad",
        ShowCmd = 1,
    };

    [Fact]
    public void Load_returns_empty_when_file_missing()
    {
        var store = NewStore(out _, out _);
        Assert.Empty(store.Load());
    }

    [Fact]
    public void Save_then_load_round_trips_every_field()
    {
        var store = NewStore(out _, out _);
        store.Save([Sample()]);

        var loaded = Assert.Single(store.Load());
        Assert.Equal(Sample(), loaded);
    }

    [Fact]
    public void Save_overwrites_previous_content()
    {
        var store = NewStore(out _, out _);
        store.Save([Sample(1), Sample(2)]);
        store.Save([Sample(3)]);

        var loaded = store.Load();
        Assert.Equal(3, Assert.Single(loaded).Hwnd);
    }

    [Fact]
    public void Corrupt_file_is_moved_to_backup_and_load_returns_empty()
    {
        var store = NewStore(out var path, out var backup);
        File.WriteAllText(path, "{ not json at all");

        Assert.Empty(store.Load());
        Assert.False(File.Exists(path));
        Assert.True(File.Exists(backup));
        Assert.Contains("not json", File.ReadAllText(backup));
    }

    [Fact]
    public void Save_leaves_no_temp_file_behind()
    {
        var store = NewStore(out var path, out _);
        store.Save([Sample()]);

        Assert.False(File.Exists(path + ".tmp"));
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);
}
