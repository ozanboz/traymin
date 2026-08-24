using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TrayMin.Core;
using TrayMin.Native;

namespace TrayMin.App;

public sealed unsafe class MessageWindow : IDisposable
{
    public const string ClassName = "TrayMin.MessageWindow";

    private static MessageWindow? _instance;
    private static uint _taskbarCreated;
    private static uint _restoreAll;

    private nint _handle;

    public Func<uint, nint, nint, nint?>? OnMessage { get; set; }

    public nint Handle => _handle;

    public static uint TaskbarCreatedMessage
    {
        get
        {
            if (_taskbarCreated == 0)
            {
                fixed (char* name = "TaskbarCreated") _taskbarCreated = Win32.RegisterWindowMessage(name);
            }
            return _taskbarCreated;
        }
    }

    public static uint RestoreAllMessage
    {
        get
        {
            if (_restoreAll == 0)
            {
                fixed (char* name = "TrayMin.RestoreAll") _restoreAll = Win32.RegisterWindowMessage(name);
            }
            return _restoreAll;
        }
    }

    public void Create()
    {
        if (_instance is not null) throw new InvalidOperationException("MessageWindow is a singleton.");
        _instance = this;

        fixed (char* className = ClassName)
        {
            var wndClass = new Win32.WndClassEx
            {
                cbSize = (uint)sizeof(Win32.WndClassEx),
                lpfnWndProc = (nint)(delegate* unmanaged[Stdcall]<nint, uint, nint, nint, nint>)&Thunk,
                hInstance = Win32.GetModuleHandle(null),
                lpszClassName = className,
                hCursor = 0,
            };

            if (Win32.RegisterClassEx(ref wndClass) == 0)
                throw new InvalidOperationException($"RegisterClassEx failed: {Marshal.GetLastWin32Error()}");

            fixed (char* title = "TrayMin")
            {
                _handle = Win32.CreateWindowEx(
                    (uint)Win32.WsExToolWindow, className, title, Win32.WsPopup,
                    0, 0, 0, 0, 0, 0, Win32.GetModuleHandle(null), 0);
            }
        }

        if (_handle == 0)
            throw new InvalidOperationException($"CreateWindowEx failed: {Marshal.GetLastWin32Error()}");

        AllowMessagesFromLowerIntegrity();
    }

    private void AllowMessagesFromLowerIntegrity()
    {
        uint[] messages =
        [
            Win32.WmTrayCallback,
            TaskbarCreatedMessage,
            RestoreAllMessage,
        ];
        foreach (var message in messages)
        {
            if (!Win32.ChangeWindowMessageFilterEx(_handle, message, Win32.MsgFltAllow, null))
                Log.Write($"ChangeWindowMessageFilterEx failed for 0x{message:X}: {Marshal.GetLastWin32Error()}");
        }
    }

    public int RunMessageLoop()
    {
        while (true)
        {
            var result = Win32.GetMessage(out var msg, 0, 0, 0);
            if (result == 0) return 0;
            if (result == -1)
            {
                Log.Write($"GetMessage failed: {Marshal.GetLastWin32Error()}");
                return 1;
            }
            Win32.TranslateMessage(ref msg);
            Win32.DispatchMessage(ref msg);
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static nint Thunk(nint hwnd, uint msg, nint wParam, nint lParam)
    {
        var handled = _instance?.OnMessage?.Invoke(msg, wParam, lParam);
        return handled ?? Win32.DefWindowProc(hwnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_handle != 0) { Win32.DestroyWindow(_handle); _handle = 0; }
        _instance = null;
    }
}
