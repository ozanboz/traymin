using TrayMin.Core;
using TrayMin.Tests.Fakes;
using Xunit;

namespace TrayMin.Tests;

public class RecordValidatorTests
{
    private static HiddenWindowRecord Record => new()
    {
        Hwnd = 66830,
        Pid = 4242,
        ProcessStartTicks = 130000000000000000,
        ExePath = @"C:\Windows\System32\notepad.exe",
        Title = "Untitled - Notepad",
        ShowCmd = 1,
    };

    [Fact]
    public void Live_when_window_exists_pid_and_start_time_match()
    {
        var windows = new FakeWindowProbe { Pid = 4242 };
        var processes = new FakeProcessProbe { Ticks = 130000000000000000 };

        Assert.True(RecordValidator.IsLive(Record, windows, processes));
    }

    [Fact]
    public void Dead_when_handle_no_longer_a_window()
    {
        var windows = new FakeWindowProbe { Exists = false, Pid = 4242 };
        Assert.False(RecordValidator.IsLive(Record, windows, new FakeProcessProbe()));
    }

    [Fact]
    public void Dead_when_handle_was_recycled_by_another_process()
    {
        var windows = new FakeWindowProbe { Pid = 9999 };
        Assert.False(RecordValidator.IsLive(Record, windows, new FakeProcessProbe()));
    }

    [Fact]
    public void Dead_when_pid_was_recycled_by_a_newer_process()
    {
        var windows = new FakeWindowProbe { Pid = 4242 };
        var processes = new FakeProcessProbe { Ticks = 130999999999999999 };

        Assert.False(RecordValidator.IsLive(Record, windows, processes));
    }

    [Fact]
    public void Dead_when_process_start_time_cannot_be_read()
    {
        var windows = new FakeWindowProbe { Pid = 4242 };
        var processes = new FakeProcessProbe { Ticks = null };

        Assert.False(RecordValidator.IsLive(Record, windows, processes));
    }
}
