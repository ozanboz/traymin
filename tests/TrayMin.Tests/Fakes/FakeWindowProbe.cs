using TrayMin.Core;

namespace TrayMin.Tests.Fakes;

public sealed class FakeWindowProbe : IWindowProbe
{
    public bool Exists = true;
    public bool Visible = true;
    public nint ExStyle;
    public string Class = "Notepad";
    public uint Pid = 4242;
    public string? ExePath = @"C:\Windows\System32\notepad.exe";

    public bool IsWindow(nint hwnd) => Exists;
    public bool IsWindowVisible(nint hwnd) => Visible;
    public nint GetExStyle(nint hwnd) => ExStyle;
    public string GetClassName(nint hwnd) => Class;
    public uint GetProcessId(nint hwnd) => Pid;
    public string? GetExePath(uint pid) => ExePath;
}
