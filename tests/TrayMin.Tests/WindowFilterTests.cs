using TrayMin.Core;
using TrayMin.Tests.Fakes;
using Xunit;

namespace TrayMin.Tests;

public class WindowFilterTests
{
    private const nint WsExToolWindow = 0x00000080;
    private const uint SelfPid = 1000;

    private static WindowFilter Build(FakeWindowProbe probe, params string[] blocked)
        => new(probe, SelfPid, blocked);

    [Fact]
    public void Accepts_ordinary_visible_window()
    {
        Assert.Equal(FilterVerdict.Ok, Build(new FakeWindowProbe()).Evaluate(1));
    }

    [Fact]
    public void Rejects_zero_handle_and_dead_window()
    {
        Assert.Equal(FilterVerdict.NotAWindow, Build(new FakeWindowProbe()).Evaluate(0));
        Assert.Equal(FilterVerdict.NotAWindow, Build(new FakeWindowProbe { Exists = false }).Evaluate(1));
    }

    [Fact]
    public void Rejects_invisible_window()
    {
        Assert.Equal(FilterVerdict.NotVisible, Build(new FakeWindowProbe { Visible = false }).Evaluate(1));
    }

    [Fact]
    public void Rejects_tool_window()
    {
        Assert.Equal(FilterVerdict.ToolWindow,
            Build(new FakeWindowProbe { ExStyle = WsExToolWindow }).Evaluate(1));
    }

    [Fact]
    public void Rejects_our_own_window()
    {
        Assert.Equal(FilterVerdict.OwnWindow, Build(new FakeWindowProbe { Pid = SelfPid }).Evaluate(1));
    }

    [Theory]
    [InlineData("Shell_TrayWnd")]
    [InlineData("Progman")]
    [InlineData("WorkerW")]
    [InlineData("NotifyIconOverflowWindow")]
    [InlineData("Windows.UI.Core.CoreWindow")]
    public void Rejects_shell_windows(string className)
    {
        Assert.Equal(FilterVerdict.ShellWindow,
            Build(new FakeWindowProbe { Class = className }).Evaluate(1));
    }

    [Fact]
    public void Rejects_uwp_frame()
    {
        Assert.Equal(FilterVerdict.UwpUnsupported,
            Build(new FakeWindowProbe { Class = "ApplicationFrameWindow" }).Evaluate(1));
    }

    [Fact]
    public void Rejects_blocklisted_exe_case_insensitively_by_file_name()
    {
        var probe = new FakeWindowProbe { ExePath = @"D:\Apps\Discord\Discord.exe" };
        Assert.Equal(FilterVerdict.Blocked, Build(probe, "discord.exe").Evaluate(1));
    }

    [Fact]
    public void Accepts_when_exe_path_is_unavailable()
    {
        Assert.Equal(FilterVerdict.Ok,
            Build(new FakeWindowProbe { ExePath = null }, "discord.exe").Evaluate(1));
    }

    [Fact]
    public void Describe_returns_turkish_message_for_every_verdict()
    {
        foreach (FilterVerdict v in Enum.GetValues<FilterVerdict>())
            Assert.False(string.IsNullOrWhiteSpace(WindowFilter.Describe(v)));
    }
}
