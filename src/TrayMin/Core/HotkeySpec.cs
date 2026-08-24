namespace TrayMin.Core;

public readonly record struct HotkeySpec(uint Modifiers, uint VirtualKey)
{
    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;
    public const uint ModShift = 0x0004;
    public const uint ModWin = 0x0008;
    public const uint ModNoRepeat = 0x4000;

    public static bool TryParse(string text, out HotkeySpec spec)
    {
        spec = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        uint mods = 0;
        uint vk = 0;

        foreach (var raw in text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var part = raw.ToUpperInvariant();
            switch (part)
            {
                case "WIN": mods |= ModWin; continue;
                case "CTRL": case "CONTROL": mods |= ModControl; continue;
                case "ALT": mods |= ModAlt; continue;
                case "SHIFT": mods |= ModShift; continue;
            }

            if (vk != 0) return false;

            if (part.Length == 1 && part[0] is >= 'A' and <= 'Z')
            {
                vk = part[0];
                continue;
            }
            if (part.Length == 1 && part[0] is >= '0' and <= '9')
            {
                vk = part[0];
                continue;
            }
            if (part.Length is 2 or 3 && part[0] == 'F'
                && int.TryParse(part.AsSpan(1), out var n) && n is >= 1 and <= 24)
            {
                vk = (uint)(0x70 + n - 1);
                continue;
            }
            return false;
        }

        if (vk == 0 || mods == 0) return false;

        spec = new HotkeySpec(mods | ModNoRepeat, vk);
        return true;
    }
}
