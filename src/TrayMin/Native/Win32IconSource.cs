using TrayMin.Core;

namespace TrayMin.Native;

public sealed unsafe class Win32IconSource : IIconSource
{
    private const uint TimeoutMs = 200;

    public nint FromWindowMessage(nint hwnd, int iconType)
    {
        var sent = Win32.SendMessageTimeout(hwnd, Win32.WmGetIcon, iconType, 0,
            Win32.SmtoAbortIfHung, TimeoutMs, out var result);
        return sent == 0 || result == 0 ? 0 : Win32.CopyIcon(result);
    }

    public nint FromClass(nint hwnd, int gclpIndex)
    {
        var borrowed = Win32.GetClassLongPtr(hwnd, gclpIndex);
        return borrowed == 0 ? 0 : Win32.CopyIcon(borrowed);
    }

    public nint FromExe(string exePath)
    {
        nint small = 0;
        fixed (char* path = exePath)
        {
            var extracted = Win32.ExtractIconEx(path, 0, null, &small, 1);
            return extracted == 0 ? 0 : small;
        }
    }

    public nint Fallback()
    {
        var module = Win32.GetModuleHandle(null);
        var width = Win32.GetSystemMetrics(Win32.SmCxSmIcon);
        var height = Win32.GetSystemMetrics(Win32.SmCySmIcon);
        var owned = Win32.LoadImage(module, 32512, Win32.ImageIcon, width, height, 0);
        if (owned != 0) return owned;

        Log.Write("embedded application icon unavailable; using system fallback");
        var borrowed = Win32.LoadIcon(0, 32512);
        return borrowed == 0 ? 0 : Win32.CopyIcon(borrowed);
    }
}
