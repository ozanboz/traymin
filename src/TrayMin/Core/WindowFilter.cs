namespace TrayMin.Core;

public interface IWindowProbe
{
    bool IsWindow(nint hwnd);
    bool IsWindowVisible(nint hwnd);
    nint GetExStyle(nint hwnd);
    string GetClassName(nint hwnd);
    uint GetProcessId(nint hwnd);
    string? GetExePath(uint pid);
}

public interface IProcessProbe
{
    long? GetStartTicks(uint pid);
}

public enum FilterVerdict
{
    Ok,
    NotAWindow,
    NotVisible,
    ToolWindow,
    OwnWindow,
    ShellWindow,
    UwpUnsupported,
    Blocked,
}

public sealed class WindowFilter(IWindowProbe probe, uint selfPid, IReadOnlyList<string> blockedExeNames)
{
    private const nint WsExToolWindow = 0x00000080;

    private static readonly string[] ShellClasses =
    [
        "Shell_TrayWnd",
        "Shell_SecondaryTrayWnd",
        "Progman",
        "WorkerW",
        "NotifyIconOverflowWindow",
        "Windows.UI.Core.CoreWindow",
        "TrayMin.MessageWindow",
    ];

    public FilterVerdict Evaluate(nint hwnd)
    {
        if (hwnd == 0 || !probe.IsWindow(hwnd)) return FilterVerdict.NotAWindow;
        if (!probe.IsWindowVisible(hwnd)) return FilterVerdict.NotVisible;
        if ((probe.GetExStyle(hwnd) & WsExToolWindow) != 0) return FilterVerdict.ToolWindow;
        if (probe.GetProcessId(hwnd) == selfPid) return FilterVerdict.OwnWindow;

        var className = probe.GetClassName(hwnd);
        if (className == "ApplicationFrameWindow") return FilterVerdict.UwpUnsupported;
        foreach (var shell in ShellClasses)
            if (string.Equals(className, shell, StringComparison.Ordinal))
                return FilterVerdict.ShellWindow;

        if (blockedExeNames.Count > 0)
        {
            var exe = probe.GetExePath(probe.GetProcessId(hwnd));
            if (exe is not null)
            {
                var name = Path.GetFileName(exe);
                foreach (var blocked in blockedExeNames)
                    if (string.Equals(name, blocked, StringComparison.OrdinalIgnoreCase))
                        return FilterVerdict.Blocked;
            }
        }

        return FilterVerdict.Ok;
    }

    public static string Describe(FilterVerdict verdict) => verdict switch
    {
        FilterVerdict.Ok => "Sending window to the system tray.",
        FilterVerdict.NotAWindow => "No valid window was found.",
        FilterVerdict.NotVisible => "The window is not visible and cannot be hidden.",
        FilterVerdict.ToolWindow => "Tool windows do not have a taskbar entry.",
        FilterVerdict.OwnWindow => "This is TrayMin's own window.",
        FilterVerdict.ShellWindow => "Desktop and taskbar windows cannot be hidden.",
        FilterVerdict.UwpUnsupported => "Microsoft Store (UWP) applications are not supported.",
        FilterVerdict.Blocked => "This application is on the block list.",
        _ => "Unknown state.",
    };
}
