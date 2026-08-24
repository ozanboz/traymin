using TrayMin.Core;
using TrayMin.Native;

namespace TrayMin.App;

public sealed class HideController(
    WindowOps windows,
    ProcessProbe processes,
    WindowFilter filter,
    IconResolver icons,
    HiddenWindowStore store,
    TrayIcons tray,
    uint managerIconId)
{
    private readonly Dictionary<uint, HiddenWindowRecord> _hidden = [];

    public int Count => _hidden.Count;

    public IReadOnlyList<(uint IconId, string Title)> List()
        => _hidden.Select(pair => (pair.Key, Label(pair.Value))).ToArray();

    public bool TryGetTitle(uint iconId, out string title)
    {
        if (_hidden.TryGetValue(iconId, out var record)) { title = Label(record); return true; }
        title = string.Empty;
        return false;
    }

    public void HideForeground()
    {
        var hwnd = windows.GetForegroundTopLevel();
        var verdict = filter.Evaluate(hwnd);
        if (verdict != FilterVerdict.Ok)
        {
            tray.Balloon(managerIconId, "TrayMin", WindowFilter.Describe(verdict));
            Log.Write($"hide rejected: {verdict} hwnd=0x{hwnd:X}");
            return;
        }

        var pid = windows.GetProcessId(hwnd);
        var processStartTicks = processes.GetStartTicks(pid);
        if (processStartTicks is null)
        {
            tray.Balloon(managerIconId, "TrayMin",
                "Window identity could not be verified; it cannot be hidden safely.");
            Log.Write($"hide rejected: process start time unavailable pid={pid} hwnd=0x{hwnd:X}");
            return;
        }
        var record = new HiddenWindowRecord
        {
            Hwnd = hwnd,
            Pid = pid,
            ProcessStartTicks = processStartTicks.Value,
            ExePath = ProcessProbe.GetExePath(pid) ?? string.Empty,
            Title = windows.GetTitle(hwnd),
            ShowCmd = windows.GetShowCmd(hwnd),
        };

        var snapshot = _hidden.Values.Append(record).ToArray();
        store.Save(snapshot);

        if (!windows.Hide(hwnd))
        {
            store.Save(_hidden.Values.ToArray());
            tray.Balloon(managerIconId, "TrayMin",
                "Window could not be hidden. Administrator privileges may be required.");
            Log.Write($"ShowWindow(SW_HIDE) failed hwnd=0x{hwnd:X}");
            return;
        }

        var icon = icons.Resolve(hwnd, string.IsNullOrEmpty(record.ExePath) ? null : record.ExePath);
        var iconId = tray.Add(icon, Label(record));
        _hidden[iconId] = record;

        UpdateManagerTooltip();
        Log.Write($"hidden hwnd=0x{hwnd:X} icon={iconId} title={record.Title}");
    }

    public bool RestoreByIconId(uint iconId)
    {
        if (!_hidden.TryGetValue(iconId, out var record)) return false;

        var hwnd = (nint)record.Hwnd;
        if (windows.IsWindow(hwnd) && !windows.ShowAndFocus(hwnd, record.ShowCmd))
        {
            tray.Balloon(managerIconId, "TrayMin",
                "Window could not be restored. Recovery state was retained; try again.");
            Log.Write($"restore failed, state retained hwnd=0x{hwnd:X} icon={iconId}");
            return false;
        }
        tray.Remove(iconId);
        _hidden.Remove(iconId);
        store.Save(_hidden.Values.ToArray());
        UpdateManagerTooltip();
        Log.Write($"restored hwnd=0x{hwnd:X} icon={iconId}");
        return true;
    }

    public bool RestoreAll()
    {
        foreach (var iconId in _hidden.Keys.ToArray()) RestoreByIconId(iconId);
        return _hidden.Count == 0;
    }

    public void SweepDead()
    {
        var removed = false;
        foreach (var (iconId, record) in _hidden.ToArray())
        {
            if (RecordValidator.IsLive(record, windows, processes)) continue;
            tray.Remove(iconId);
            _hidden.Remove(iconId);
            removed = true;
            Log.Write($"swept dead icon={iconId} hwnd=0x{record.Hwnd:X}");
        }

        if (!removed) return;
        store.Save(_hidden.Values.ToArray());
        UpdateManagerTooltip();
    }

    public void RecoverFromDisk()
    {
        var survivors = new List<HiddenWindowRecord>();
        foreach (var record in store.Load())
        {
            if (!RecordValidator.IsLive(record, windows, processes))
            {
                Log.Write($"dropping stale record hwnd=0x{record.Hwnd:X} pid={record.Pid}");
                continue;
            }

            var hwnd = (nint)record.Hwnd;
            var icon = icons.Resolve(hwnd, string.IsNullOrEmpty(record.ExePath) ? null : record.ExePath);
            _hidden[tray.Add(icon, Label(record))] = record;
            survivors.Add(record);
        }

        store.Save(survivors);
        UpdateManagerTooltip();
        if (survivors.Count > 0)
        {
            Log.Write($"recovered {survivors.Count} hidden window(s) from disk");
            tray.Balloon(managerIconId, "TrayMin", $"{survivors.Count} hidden window(s) recovered.");
        }
    }

    private void UpdateManagerTooltip()
        => tray.Modify(managerIconId, icons.Resolve(0, null),
            _hidden.Count == 0 ? "TrayMin — no hidden windows" : $"TrayMin — {_hidden.Count} hidden window(s)");

    private static string Label(HiddenWindowRecord record)
    {
        var app = string.IsNullOrEmpty(record.ExePath) ? "unknown" : Path.GetFileNameWithoutExtension(record.ExePath);
        var title = string.IsNullOrWhiteSpace(record.Title) ? app : record.Title;
        return title.Length > 100 ? title[..100] : title;
    }
}
