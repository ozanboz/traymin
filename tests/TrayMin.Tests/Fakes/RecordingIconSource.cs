using TrayMin.Core;

namespace TrayMin.Tests.Fakes;

public sealed class RecordingIconSource : IIconSource
{
    public List<string> Calls { get; } = [];
    public string? SucceedAt;

    private nint Result(string key)
    {
        Calls.Add(key);
        return key == SucceedAt ? 999 : 0;
    }

    public nint FromWindowMessage(nint hwnd, int iconType) => Result($"msg:{iconType}");
    public nint FromClass(nint hwnd, int gclpIndex) => Result($"class:{gclpIndex}");
    public nint FromExe(string exePath) => Result("exe");
    public nint Fallback() { Calls.Add("fallback"); return 1; }
}
