using System.Diagnostics;
using TrayMin.Core;

namespace TrayMin.Install;

public static class TaskInstaller
{
    public const string TaskName = "TrayMin";

    public static int Install(string exePath, bool allowElevation = true)
    {
        var exitCode = CreateTask(exePath);
        if (exitCode == 0 || !allowElevation) return exitCode;

        Log.Write("install failed unelevated, relaunching with UAC prompt");
        return RelaunchElevated(exePath, "--install-elevated", "install");
    }

    public static int Uninstall(string exePath, bool allowElevation = true)
    {
        if (allowElevation)
            return RelaunchElevated(exePath, "--uninstall-elevated", "uninstall");

        var query = Run("schtasks", $"/Query /TN {TaskName}");
        if (query.ExitCode != 0)
        {
            var fileState = GetTaskFileState();
            if (fileState == TaskFileState.Absent)
            {
                Log.Write("uninstall: scheduled task is positively absent");
                return 0;
            }

            Log.Write($"uninstall query failed; backing-file state={fileState}: {query.Output}");
            return query.ExitCode;
        }

        return Run("schtasks", $"/Delete /TN {TaskName} /F").ExitCode;
    }

    private enum TaskFileState { Present, Absent, Unknown }

    private static TaskFileState GetTaskFileState()
    {
        var taskFile = Path.Combine(Environment.SystemDirectory, "Tasks", TaskName);
        try
        {
            _ = File.GetAttributes(taskFile);
            return TaskFileState.Present;
        }
        catch (FileNotFoundException)
        {
            return TaskFileState.Absent;
        }
        catch (DirectoryNotFoundException)
        {
            return TaskFileState.Absent;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            Log.Write($"scheduled-task backing-file probe failed: {ex.Message}");
            return TaskFileState.Unknown;
        }
    }

    private static int RelaunchElevated(string exePath, string argument, string operation)
    {
        try
        {
            var info = new ProcessStartInfo(exePath, argument)
            {
                UseShellExecute = true,
                Verb = "runas",
            };
            using var elevated = Process.Start(info);
            if (elevated is null) { Log.Write("elevated relaunch returned no process"); return 1; }
            elevated.WaitForExit();
            Log.Write($"elevated {operation} exited with {elevated.ExitCode}");
            return elevated.ExitCode;
        }
        catch (Exception ex)
        {
            Log.Write($"elevated relaunch failed: {ex.Message}");
            return 1;
        }
    }

    private static int CreateTask(string exePath)
    {
        var escapedExePath = System.Security.SecurityElement.Escape(exePath) ?? exePath;
        var rawUserId = $"{Environment.UserDomainName}\\{Environment.UserName}";
        var escapedUserId = System.Security.SecurityElement.Escape(rawUserId) ?? rawUserId;
        var xml = $"""
        <?xml version="1.0" encoding="UTF-16"?>
        <Task version="1.4" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
          <RegistrationInfo>
            <Description>TrayMin - minimize windows to the system tray</Description>
          </RegistrationInfo>
          <Triggers>
            <LogonTrigger>
              <Enabled>true</Enabled>
              <UserId>{escapedUserId}</UserId>
            </LogonTrigger>
          </Triggers>
          <Principals>
            <Principal id="Author">
              <UserId>{escapedUserId}</UserId>
              <LogonType>InteractiveToken</LogonType>
              <RunLevel>HighestAvailable</RunLevel>
            </Principal>
          </Principals>
          <Settings>
            <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
            <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
            <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
            <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
            <StartWhenAvailable>true</StartWhenAvailable>
          </Settings>
          <Actions Context="Author">
            <Exec>
              <Command>{escapedExePath}</Command>
            </Exec>
          </Actions>
        </Task>
        """;

        var xmlPath = Path.Combine(Path.GetTempPath(), "traymin-task.xml");
        File.WriteAllText(xmlPath, xml, System.Text.Encoding.Unicode);
        try
        {
            return Run("schtasks", $"/Create /TN {TaskName} /XML \"{xmlPath}\" /F").ExitCode;
        }
        finally
        {
            try { File.Delete(xmlPath); } catch (IOException) { }
        }
    }

    private readonly record struct CommandResult(int ExitCode, string Output);

    private static CommandResult Run(string file, string arguments)
    {
        var info = new ProcessStartInfo(file, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var process = Process.Start(info);
        if (process is null)
        {
            Log.Write($"failed to start {file}");
            return new CommandResult(1, "process start returned null");
        }

        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();
        output = output.Trim();
        Log.Write($"{file} {arguments} -> exit {process.ExitCode}: {output}");
        return new CommandResult(process.ExitCode, output);
    }
}
