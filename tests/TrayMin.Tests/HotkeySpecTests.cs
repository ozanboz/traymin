using TrayMin.Core;
using Xunit;

namespace TrayMin.Tests;

public class HotkeySpecTests
{
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private const uint ModNoRepeat = 0x4000;

    [Fact]
    public void Parses_win_shift_letter()
    {
        Assert.True(HotkeySpec.TryParse("Win+Shift+H", out var spec));
        Assert.Equal(ModWin | ModShift | ModNoRepeat, spec.Modifiers);
        Assert.Equal((uint)'H', spec.VirtualKey);
    }

    [Fact]
    public void Parses_all_modifiers_and_is_case_insensitive()
    {
        Assert.True(HotkeySpec.TryParse("ctrl+ALT+shift+win+g", out var spec));
        Assert.Equal(ModControl | ModAlt | ModShift | ModWin | ModNoRepeat, spec.Modifiers);
        Assert.Equal((uint)'G', spec.VirtualKey);
    }

    [Fact]
    public void Parses_function_keys()
    {
        Assert.True(HotkeySpec.TryParse("Alt+F12", out var spec));
        Assert.Equal(ModAlt | ModNoRepeat, spec.Modifiers);
        Assert.Equal(0x7Bu, spec.VirtualKey);
    }

    [Fact]
    public void Rejects_missing_modifier()
    {
        Assert.False(HotkeySpec.TryParse("H", out _));
    }

    [Fact]
    public void Rejects_unknown_key_and_empty()
    {
        Assert.False(HotkeySpec.TryParse("Win+Shift+HH", out _));
        Assert.False(HotkeySpec.TryParse("", out _));
        Assert.False(HotkeySpec.TryParse("Win+Shift+F25", out _));
    }
}
