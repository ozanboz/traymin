using TrayMin.Core;
using TrayMin.Tests.Fakes;
using Xunit;

namespace TrayMin.Tests;

public class IconResolverTests
{
    private const string Exe = @"C:\Windows\System32\notepad.exe";

    [Fact]
    public void Stops_at_first_success_and_tries_small2_first()
    {
        var source = new RecordingIconSource { SucceedAt = "msg:2" };
        var icon = new IconResolver(source).Resolve(1, Exe);

        Assert.Equal(999, icon);
        Assert.Equal(["msg:2"], source.Calls);
    }

    [Fact]
    public void Falls_through_the_whole_chain_in_order()
    {
        var source = new RecordingIconSource { SucceedAt = "exe" };
        var icon = new IconResolver(source).Resolve(1, Exe);

        Assert.Equal(999, icon);
        Assert.Equal(["msg:2", "msg:1", "class:-34", "class:-14", "exe"], source.Calls);
    }

    [Fact]
    public void Uses_fallback_when_everything_fails()
    {
        var source = new RecordingIconSource { SucceedAt = null };
        var icon = new IconResolver(source).Resolve(1, Exe);

        Assert.Equal(1, icon);
        Assert.Equal(["msg:2", "msg:1", "class:-34", "class:-14", "exe", "fallback"], source.Calls);
    }

    [Fact]
    public void Skips_exe_step_when_path_is_unknown()
    {
        var source = new RecordingIconSource { SucceedAt = null };
        new IconResolver(source).Resolve(1, null);

        Assert.Equal(["msg:2", "msg:1", "class:-34", "class:-14", "fallback"], source.Calls);
    }
}
