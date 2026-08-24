using TrayMin.Core;

namespace TrayMin.Native;

public sealed unsafe class ProcessProbe : IProcessProbe
{
    public long? GetStartTicks(uint pid)
    {
        var handle = Win32.OpenProcess(Win32.ProcessQueryLimitedInformation, false, pid);
        if (handle == 0) return null;
        try
        {
            if (!Win32.GetProcessTimes(handle, out var creation, out _, out _, out _)) return null;
            return (long)(((ulong)creation.dwHighDateTime << 32) | creation.dwLowDateTime);
        }
        finally { Win32.CloseHandle(handle); }
    }

    public static string? GetExePath(uint pid)
    {
        var handle = Win32.OpenProcess(Win32.ProcessQueryLimitedInformation, false, pid);
        if (handle == 0) return null;
        try
        {
            Span<char> buffer = stackalloc char[1024];
            var size = (uint)buffer.Length;
            fixed (char* p = buffer)
            {
                if (!Win32.QueryFullProcessImageName(handle, 0, p, ref size)) return null;
                return new string(buffer[..(int)size]);
            }
        }
        finally { Win32.CloseHandle(handle); }
    }
}
