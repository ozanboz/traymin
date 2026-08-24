namespace TrayMin.Core;

public static class Paths
{
    public static string Dir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TrayMin");

    public static string Config => Path.Combine(Dir, "config.json");
    public static string Hidden => Path.Combine(Dir, "hidden.json");
    public static string HiddenBackup => Path.Combine(Dir, "hidden.json.bak");
    public static string Log => Path.Combine(Dir, "traymin.log");

    public static void EnsureDir() => Directory.CreateDirectory(Dir);
}
