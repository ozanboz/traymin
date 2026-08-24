using System.Runtime.InteropServices;
using TrayMin.Core;
using TrayMin.Native;

namespace TrayMin.App;

public sealed unsafe class TrayIcons(nint hwnd) : IDisposable
{
    private readonly Dictionary<uint, (nint Icon, string Tip)> _icons = [];
    private uint _nextId = 1;

    public uint Add(nint icon, string tip)
    {
        var id = _nextId++;
        _icons[id] = (icon, tip);
        if (Send(Win32.NimAdd, id, icon, tip))
            Log.Write($"tray icon {id} added, tip='{tip}'");
        else
            Log.Write($"NIM_ADD failed for id {id}: {Marshal.GetLastWin32Error()}");
        return id;
    }

    public void Modify(uint id, nint icon, string tip)
    {
        if (!_icons.TryGetValue(id, out var previous))
        {
            if (icon != 0) Win32.DestroyIcon(icon);
            return;
        }

        if (!Send(Win32.NimModify, id, icon, tip))
        {
            Log.Write($"NIM_MODIFY failed for id {id}: {Marshal.GetLastWin32Error()}");
            if (icon != 0) Win32.DestroyIcon(icon);
            return;
        }

        _icons[id] = (icon, tip);
        if (previous.Icon != 0) Win32.DestroyIcon(previous.Icon);
    }

    public void Remove(uint id)
    {
        if (!_icons.Remove(id, out var entry)) return;
        var data = new Win32.NotifyIconData
        {
            cbSize = (uint)sizeof(Win32.NotifyIconData),
            hWnd = hwnd,
            uID = id,
        };
        if (!Win32.ShellNotifyIcon(Win32.NimDelete, &data))
            Log.Write($"NIM_DELETE failed for id {id}: {Marshal.GetLastWin32Error()}");
        if (entry.Icon != 0) Win32.DestroyIcon(entry.Icon);
    }

    public void ReAddAll()
    {
        foreach (var (id, entry) in _icons)
        {
            if (Send(Win32.NimAdd, id, entry.Icon, entry.Tip)) continue;
            if (Send(Win32.NimModify, id, entry.Icon, entry.Tip)) continue;
            Log.Write($"re-add failed for id {id}: {Marshal.GetLastWin32Error()}");
        }
    }

    public void Balloon(uint id, string title, string text)
    {
        if (!_icons.TryGetValue(id, out var entry)) return;

        var data = new Win32.NotifyIconData
        {
            cbSize = (uint)sizeof(Win32.NotifyIconData),
            hWnd = hwnd,
            uID = id,
            uFlags = Win32.NifInfo | Win32.NifIcon | Win32.NifTip | Win32.NifMessage,
            uCallbackMessage = Win32.WmTrayCallback,
            hIcon = entry.Icon,
        };
        Copy(data.szTip, 128, entry.Tip);
        Copy(data.szInfoTitle, 64, title);
        Copy(data.szInfo, 256, text);

        Win32.ShellNotifyIcon(Win32.NimModify, &data);
    }

    public int ShowMenu(IReadOnlyList<(int Id, string Text)> items)
    {
        var menu = Win32.CreatePopupMenu();
        if (menu == 0) return 0;
        try
        {
            foreach (var (id, text) in items)
            {
                if (id == 0)
                {
                    Win32.AppendMenu(menu, Win32.MfSeparator, 0, null);
                    continue;
                }
                fixed (char* p = text) Win32.AppendMenu(menu, Win32.MfString, (nuint)id, p);
            }

            Win32.GetCursorPos(out var point);
            Win32.SetForegroundWindow(hwnd);
            var chosen = Win32.TrackPopupMenu(menu, Win32.TpmRightButton | Win32.TpmReturnCmd,
                point.X, point.Y, 0, hwnd, 0);
            Win32.PostMessage(hwnd, Win32.WmNull, 0, 0);
            return chosen;
        }
        finally { Win32.DestroyMenu(menu); }
    }

    private bool Send(uint message, uint id, nint icon, string tip)
    {
        var data = new Win32.NotifyIconData
        {
            cbSize = (uint)sizeof(Win32.NotifyIconData),
            hWnd = hwnd,
            uID = id,
            uFlags = Win32.NifMessage | Win32.NifIcon | Win32.NifTip,
            uCallbackMessage = Win32.WmTrayCallback,
            hIcon = icon,
        };
        Copy(data.szTip, 128, tip);
        return Win32.ShellNotifyIcon(message, &data);
    }

    private static void Copy(char* destination, int capacity, string value)
    {
        var length = Math.Min(value.Length, capacity - 1);
        value.AsSpan(0, length).CopyTo(new Span<char>(destination, capacity));
        destination[length] = '\0';
    }

    public void Dispose()
    {
        foreach (var id in _icons.Keys.ToArray()) Remove(id);
    }
}
