using System.Runtime.InteropServices;
using TrayMin.Core;
using TrayMin.Native;

namespace TrayMin.App;

public sealed class HotkeyRouter(nint hwnd) : IDisposable
{
    public const int HideId = 1;
    public const int RestoreAllId = 2;
    private const int ErrorHotkeyAlreadyRegistered = 1409;

    private readonly List<int> _registered = [];

    public bool Register(int id, HotkeySpec spec)
    {
        if (Win32.RegisterHotKey(hwnd, id, spec.Modifiers, spec.VirtualKey))
        {
            _registered.Add(id);
            Log.Write($"hotkey {id} registered (mods=0x{spec.Modifiers:X} vk=0x{spec.VirtualKey:X})");
            return true;
        }

        var error = Marshal.GetLastWin32Error();
        Log.Write(error == ErrorHotkeyAlreadyRegistered
            ? $"hotkey {id} already taken by another app"
            : $"RegisterHotKey({id}) failed: {error}");
        return false;
    }

    public void Dispose()
    {
        foreach (var id in _registered) Win32.UnregisterHotKey(hwnd, id);
        _registered.Clear();
    }
}
