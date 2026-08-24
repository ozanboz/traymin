namespace TrayMin.Core;

public static class RecordValidator
{
    public static bool IsLive(HiddenWindowRecord record, IWindowProbe windows, IProcessProbe processes)
    {
        var hwnd = (nint)record.Hwnd;
        if (hwnd == 0 || !windows.IsWindow(hwnd)) return false;
        if (windows.GetProcessId(hwnd) != record.Pid) return false;

        var ticks = processes.GetStartTicks(record.Pid);
        return ticks is not null && ticks == record.ProcessStartTicks;
    }
}
