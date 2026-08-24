using TrayMin.App;
using TrayMin.Core;
using TrayMin.Install;
using TrayMin.Native;

namespace TrayMin;

internal static class Program
{
    private const int MenuRestoreAll = 9001;
    private const int MenuOpenSettings = 9002;
    private const int MenuExit = 9003;
    private const int MenuRestoreBase = 10000;

    private static int Main(string[] args)
    {
        Paths.EnsureDir();

        if (args.Length > 0)
        {
            switch (args[0].ToLowerInvariant())
            {
                case "--install":
                    return TaskInstaller.Install(ExePath());
                case "--install-elevated":
                    return TaskInstaller.Install(ExePath(), allowElevation: false);
                case "--uninstall":
                    return TaskInstaller.Uninstall(ExePath());
                case "--uninstall-elevated":
                    if (!CloseRunningInstance()) return 1;
                    return TaskInstaller.Uninstall(ExePath(), allowElevation: false);
                case "--restore-all":
                    return RestoreAllInRunningInstance();
                default:
                    Log.Write($"unknown argument: {args[0]}");
                    return 2;
            }
        }

        using var single = new Mutex(initiallyOwned: true, "Local\\TrayMin.SingleInstance", out var isFirst);
        if (!isFirst)
        {
            Log.Write("another instance is already running, exiting");
            return 3;
        }

        var config = Config.LoadOrDefault(Paths.Config);
        var windows = new WindowOps();
        var icons = new IconResolver(new Win32IconSource());

        using var window = new MessageWindow();
        window.Create();

        using var tray = new TrayIcons(window.Handle);
        using var hotkeys = new HotkeyRouter(window.Handle);

        var managerIconId = tray.Add(new Win32IconSource().Fallback(), "TrayMin — no hidden windows");

        var store = new HiddenWindowStore(Paths.Hidden, Paths.HiddenBackup);
        var processes = new ProcessProbe();
        var filter = new WindowFilter(windows, Win32.GetCurrentProcessId(), config.BlockedExeNames);
        var controller = new HideController(windows, processes, filter, icons, store, tray, managerIconId);

        if (HotkeySpec.TryParse(config.HideHotkey, out var hideSpec))
        {
            if (!hotkeys.Register(HotkeyRouter.HideId, hideSpec))
                tray.Balloon(managerIconId, "TrayMin",
                    $"Hotkey could not be registered: {config.HideHotkey}. Change it in config.json.");
        }
        else
        {
            tray.Balloon(managerIconId, "TrayMin", $"Invalid hotkey: {config.HideHotkey}");
        }

        if (HotkeySpec.TryParse(config.RestoreAllHotkey, out var restoreSpec))
        {
            if (!hotkeys.Register(HotkeyRouter.RestoreAllId, restoreSpec))
                tray.Balloon(managerIconId, "TrayMin",
                    $"Restore hotkey could not be registered: {config.RestoreAllHotkey}. Change it in config.json.");
        }
        else
        {
            tray.Balloon(managerIconId, "TrayMin",
                $"Invalid restore hotkey: {config.RestoreAllHotkey}");
        }

        const nuint LivenessTimerId = 1;
        Win32.SetTimer(window.Handle, LivenessTimerId, 2000, 0);

        controller.RecoverFromDisk();

        window.OnMessage = (msg, wParam, lParam) =>
        {
            if (msg == Win32.WmTrayCallback)
            {
                var iconId = (uint)wParam;
                var mouse = Win32.LoWord(lParam);

                if (iconId == managerIconId)
                {
                    if (mouse is Win32.WmRButtonUp or Win32.WmLButtonUp)
                    {
                        var items = new List<(int, string)>();
                        foreach (var (id, label) in controller.List())
                            items.Add(((int)(MenuRestoreBase + id), label));
                        if (items.Count > 0) items.Add((0, ""));
                        items.Add((MenuRestoreAll, $"Restore all ({controller.Count})"));
                        items.Add((MenuOpenSettings, "Open settings"));
                        items.Add((0, ""));
                        items.Add((MenuExit, "Exit"));
                        HandleCommand(tray.ShowMenu(items));
                    }
                    return 0;
                }

                if (mouse == Win32.WmLButtonUp)
                {
                    controller.RestoreByIconId(iconId);
                    return 0;
                }

                if (mouse == Win32.WmRButtonUp && controller.TryGetTitle(iconId, out var title))
                {
                    var chosen = tray.ShowMenu([
                        ((int)(MenuRestoreBase + iconId), $"Geri getir: {title}"),
                    ]);
                    HandleCommand(chosen);
                    return 0;
                }

                return 0;
            }

            if (msg == Win32.WmHotkey && (int)wParam == HotkeyRouter.HideId)
            {
                controller.HideForeground();
                return 0;
            }

            if (msg == Win32.WmHotkey && (int)wParam == HotkeyRouter.RestoreAllId)
            {
                controller.RestoreAll();
                return 0;
            }

            if (msg == Win32.WmTimer && (nuint)wParam == LivenessTimerId)
            {
                controller.SweepDead();
                return 0;
            }

            if (msg == MessageWindow.TaskbarCreatedMessage)
            {
                tray.ReAddAll();
                Log.Write("taskbar recreated, icons re-added");
                return 0;
            }

            if (msg == MessageWindow.RestoreAllMessage)
            {
                controller.RestoreAll();
                return 0;
            }

            if (msg == Win32.WmCommand)
            {
                HandleCommand((int)Win32.LoWord(wParam));
                return 0;
            }

            if (msg == Win32.WmClose)
            {
                if (!controller.RestoreAll())
                {
                    Log.Write("shutdown refused: one or more hidden windows could not be restored");
                    return 0;
                }
                Win32.PostQuitMessage(0);
                return 0;
            }

            if (msg == Win32.WmDestroy)
            {
                Win32.PostQuitMessage(0);
                return 0;
            }

            return null;
        };

        Log.Write("running");
        int exitCode;
        try
        {
            exitCode = window.RunMessageLoop();
        }
        finally
        {
            Win32.KillTimer(window.Handle, LivenessTimerId);
            if (!controller.RestoreAll())
                Log.Write("final restore incomplete; recovery state retained");
        }
        Log.Write($"exiting with {exitCode}");
        return exitCode;

        void HandleCommand(int command)
        {
            if (command >= MenuRestoreBase)
            {
                controller.RestoreByIconId((uint)(command - MenuRestoreBase));
                return;
            }

            switch (command)
            {
                case MenuRestoreAll:
                    controller.RestoreAll();
                    break;
                case MenuOpenSettings:
                    OpenSettings();
                    break;
                case MenuExit:
                    Win32.PostMessage(window.Handle, Win32.WmClose, 0, 0);
                    break;
            }
        }
    }

    private static string ExePath() => Environment.ProcessPath
        ?? throw new InvalidOperationException("ProcessPath unavailable");

    private static unsafe bool CloseRunningInstance()
    {
        nint target;
        fixed (char* className = MessageWindow.ClassName)
            target = Win32.FindWindow(className, null);

        if (target == 0) return true;
        if (!Win32.PostMessage(target, Win32.WmClose, 0, 0))
        {
            Log.Write($"failed to request running-instance shutdown: {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
            return false;
        }

        for (var attempt = 0; attempt < 100 && Win32.IsWindow(target); attempt++)
            Thread.Sleep(100);

        if (!Win32.IsWindow(target))
        {
            Log.Write("running instance closed for uninstall");
            return true;
        }

        Log.Write("uninstall aborted: running instance did not exit within 10 seconds");
        return false;
    }

    private static unsafe int RestoreAllInRunningInstance()
    {
        nint target;
        fixed (char* className = MessageWindow.ClassName)
            target = Win32.FindWindow(className, null);

        if (target != 0)
        {
            if (!Win32.PostMessage(target, MessageWindow.RestoreAllMessage, 0, 0))
            {
                Log.Write($"restore-all PostMessage failed: {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
                return 1;
            }
            Log.Write("restore-all posted to running instance");
            return 0;
        }

        var windows = new WindowOps();
        var processes = new ProcessProbe();
        var store = new HiddenWindowStore(Paths.Hidden, Paths.HiddenBackup);
        var failed = new List<HiddenWindowRecord>();
        var restored = 0;

        foreach (var record in store.Load())
        {
            if (!RecordValidator.IsLive(record, windows, processes)) continue;
            if (windows.ShowAndFocus((nint)record.Hwnd, record.ShowCmd))
                restored++;
            else
                failed.Add(record);
        }

        store.Save(failed);
        Log.Write($"restore-all standalone restored {restored}, retained {failed.Count} failed record(s)");
        return failed.Count == 0 ? 0 : 1;
    }

    private static void OpenSettings()
    {
        try
        {
            if (!File.Exists(Paths.Config))
                File.WriteAllText(Paths.Config, """
                {
                  "hideHotkey": "Win+Shift+H",
                  "restoreAllHotkey": "Win+Shift+G",
                  "blockedExeNames": []
                }
                """);

            using var process = System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(Paths.Config) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Write($"opening settings failed: {ex.Message}");
        }
    }
}
