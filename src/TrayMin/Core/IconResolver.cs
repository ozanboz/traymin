namespace TrayMin.Core;

public interface IIconSource
{
    nint FromWindowMessage(nint hwnd, int iconType);
    nint FromClass(nint hwnd, int gclpIndex);
    nint FromExe(string exePath);
    nint Fallback();
}

public sealed class IconResolver(IIconSource source)
{
    private const int IconBig = 1;
    private const int IconSmall2 = 2;
    private const int GclpHIconSm = -34;
    private const int GclpHIcon = -14;

    public nint Resolve(nint hwnd, string? exePath)
    {
        var icon = source.FromWindowMessage(hwnd, IconSmall2);
        if (icon != 0) return icon;

        icon = source.FromWindowMessage(hwnd, IconBig);
        if (icon != 0) return icon;

        icon = source.FromClass(hwnd, GclpHIconSm);
        if (icon != 0) return icon;

        icon = source.FromClass(hwnd, GclpHIcon);
        if (icon != 0) return icon;

        if (!string.IsNullOrEmpty(exePath))
        {
            icon = source.FromExe(exePath);
            if (icon != 0) return icon;
        }

        return source.Fallback();
    }
}
