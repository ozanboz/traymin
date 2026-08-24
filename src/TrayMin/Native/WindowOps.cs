using TrayMin.Core;

namespace TrayMin.Native;

public sealed unsafe class WindowOps : IWindowProbe
{
    public bool IsWindow(nint hwnd) => Win32.IsWindow(hwnd);

    public bool IsWindowVisible(nint hwnd) => Win32.IsWindowVisible(hwnd);

    public nint GetExStyle(nint hwnd) => Win32.GetWindowLongPtr(hwnd, Win32.GwlExStyle);

    public string GetClassName(nint hwnd)
    {
        Span<char> buffer = stackalloc char[256];
        fixed (char* p = buffer)
        {
            var length = Win32.GetClassName(hwnd, p, buffer.Length);
            return length <= 0 ? string.Empty : new string(buffer[..length]);
        }
    }

    public uint GetProcessId(nint hwnd)
    {
        Win32.GetWindowThreadProcessId(hwnd, out var pid);
        return pid;
    }

    public string? GetExePath(uint pid) => ProcessProbe.GetExePath(pid);

    public string GetTitle(nint hwnd)
    {
        Span<char> buffer = stackalloc char[512];
        fixed (char* p = buffer)
        {
            var length = Win32.GetWindowText(hwnd, p, buffer.Length);
            return length <= 0 ? string.Empty : new string(buffer[..length]);
        }
    }

    public nint GetForegroundTopLevel()
    {
        var hwnd = Win32.GetForegroundWindow();
        if (hwnd == 0) return 0;
        var root = Win32.GetAncestor(hwnd, Win32.GaRoot);
        return root == 0 ? hwnd : root;
    }

    public int GetShowCmd(nint hwnd)
    {
        var placement = new Win32.WindowPlacement { length = (uint)sizeof(Win32.WindowPlacement) };
        return Win32.GetWindowPlacement(hwnd, ref placement)
            ? (int)placement.showCmd
            : Win32.SwShowNormal;
    }

    public bool Hide(nint hwnd)
    {
        Win32.ShowWindow(hwnd, Win32.SwHide);
        return !Win32.IsWindowVisible(hwnd);
    }

    public bool ShowAndFocus(nint hwnd, int showCmd)
    {
        Win32.ShowWindow(hwnd, Win32.SwShow);

        Win32.ShowWindow(hwnd, showCmd == Win32.SwShowMaximized ? Win32.SwShowMaximized : Win32.SwRestore);

        var ourThread = Win32.GetCurrentThreadId();
        var targetThread = Win32.GetWindowThreadProcessId(hwnd, out _);
        var attached = ourThread != targetThread && Win32.AttachThreadInput(ourThread, targetThread, true);
        try
        {
            Win32.BringWindowToTop(hwnd);
            if (!Win32.SetForegroundWindow(hwnd))
                Log.Write($"SetForegroundWindow denied hwnd=0x{hwnd:X}");
        }
        finally
        {
            if (attached) Win32.AttachThreadInput(ourThread, targetThread, false);
        }
        return Win32.IsWindowVisible(hwnd);
    }
}
