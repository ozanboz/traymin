<div align="center">
  <img src="docs/assets/traymin-icon.png" width="128" height="128" alt="TrayMin icon">
  <h1>TrayMin</h1>
  <p>Minimize almost any Windows application to the system tray with a global hotkey.</p>

  <p>
    <a href="https://github.com/ozanboz/traymin/actions/workflows/build.yml"><img src="https://github.com/ozanboz/traymin/actions/workflows/build.yml/badge.svg" alt="Build status"></a>
    <a href="https://github.com/ozanboz/traymin/releases/latest"><img src="https://img.shields.io/github/v/release/ozanboz/traymin" alt="Latest release"></a>
    <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-green.svg" alt="MIT license"></a>
    <img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows" alt="Windows 10 and 11">
  </p>
</div>

TrayMin is a small, portable Windows utility that hides the currently focused
window from the taskbar and gives it its own system tray icon. Click the icon to
restore the window, or restore everything with a second global hotkey.

The release is a self-contained NativeAOT executable. It does not require the
.NET runtime, an installer, a service, a browser, or DLL injection.

## Features

- Hide the focused window with `Win+Shift+H`
- Restore all hidden windows with `Win+Shift+G`
- Preserve each application's original icon in the system tray
- Restore a window by clicking its tray icon
- Manage all hidden windows from a permanent TrayMin icon
- Recover hidden windows after an unexpected TrayMin termination
- Recreate tray icons after Windows Explorer restarts
- Remove stale tray icons when their application exits
- Support elevated applications when TrayMin runs with administrator privileges
- Start automatically at sign-in through Windows Task Scheduler
- Run as a single, self-contained executable
- Store no telemetry and make no network requests

## Download

Download `TrayMin.exe` from the
[latest GitHub release](https://github.com/ozanboz/traymin/releases/latest).

The binary is currently unsigned, so Windows SmartScreen may show a warning on
first launch. You can verify the file against the SHA-256 checksum included with
the release.

## Quick start

1. Download `TrayMin.exe`.
2. Run it once.
3. Focus a window and press `Win+Shift+H`.
4. Click the new tray icon to restore that window.

TrayMin has no visible main window. Its permanent icon may initially appear
inside the notification-area overflow menu.

## Controls

| Action | Default control |
|---|---|
| Hide the focused window | `Win+Shift+H` |
| Restore all hidden windows | `Win+Shift+G` |
| Restore one window | Left-click its tray icon |
| Open the management menu | Click the permanent TrayMin icon |
| Restore from the management menu | Select the window title |
| Exit safely | Management menu → **Exit** |

Exiting TrayMin restores every hidden window before the process terminates. If
a window cannot be restored, TrayMin stays open and retains its recovery state.

## Command line

```text
TrayMin.exe --install
TrayMin.exe --uninstall
TrayMin.exe --restore-all
```

| Command | Purpose |
|---|---|
| `--install` | Create an elevated sign-in task and start TrayMin automatically |
| `--uninstall` | Restore hidden windows, stop TrayMin, and remove the sign-in task |
| `--restore-all` | Emergency recovery command for all persisted hidden windows |

`--install` and `--uninstall` request elevation through UAC when required.

## Configuration

Open the TrayMin management menu and select **Open settings**, or edit:

```text
%LOCALAPPDATA%\TrayMin\config.json
```

Example:

```json
{
  "hideHotkey": "Win+Shift+H",
  "restoreAllHotkey": "Win+Shift+G",
  "blockedExeNames": ["discord.exe"]
}
```

Supported hotkey modifiers are `Win`, `Ctrl`, `Alt`, and `Shift`. The main key
can be a letter, a digit, or `F1` through `F24`.

Add applications with built-in tray support to `blockedExeNames` to avoid
creating duplicate tray icons.

## Recovery and safety

Before hiding a window, TrayMin records its native window handle, process ID,
process start time, executable path, title, and previous display state in:

```text
%LOCALAPPDATA%\TrayMin\hidden.json
```

The record is flushed to disk before the window is hidden. On restart, TrayMin
accepts a record only when the window handle, process ID, and process start time
still match. This prevents recycled Windows identifiers from restoring the
wrong window.

If a restore attempt fails, TrayMin keeps both the tray icon and persisted
record so the operation can be retried. Normal exit and uninstall are refused
until every live hidden window has been restored.

## Compatibility

- Windows 10 version 19045 or newer
- Windows 11
- x64 processors

Legacy Microsoft Store UWP windows hosted by `ApplicationFrameHost` are not
supported because hiding them can suspend the application or replace its native
window. Modern Win32 and WinUI 3 applications generally work normally.

A normally launched TrayMin cannot control a window running at a higher
integrity level. Use `TrayMin.exe --install` when you need to hide elevated
terminals, Task Manager, Registry Editor, or similar applications.

## Build from source

Requirements:

- .NET 9 SDK
- Visual Studio Build Tools with the C++ workload for NativeAOT linking
- Python 3 and Pillow only when regenerating the icon asset

```powershell
git clone https://github.com/ozanboz/traymin.git
cd traymin
dotnet test tests/TrayMin.Tests -c Release
dotnet publish src/TrayMin -c Release
```

Published executable:

```text
src\TrayMin\bin\Release\net9.0-windows\win-x64\publish\traymin.exe
```

Regenerate the application icon:

```powershell
python scripts/generate-icon.py
```

## Project structure

```text
src/TrayMin/                 Application source and Win32 interop
tests/TrayMin.Tests/         Unit and asset-contract tests
scripts/generate-icon.py     Deterministic multi-resolution icon generator
scripts/smoke-window-state.ps1  Window visibility smoke helper
```

## License

TrayMin is available under the [MIT License](LICENSE).
