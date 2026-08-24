namespace TrayMin.Core;

public static class Log
{
    private static readonly object Gate = new();

    public static void Write(string message)
    {
        try
        {
            Paths.EnsureDir();
            lock (Gate)
            {
                File.AppendAllText(Paths.Log,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
