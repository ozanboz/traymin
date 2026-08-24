using TrayMin.Core;

namespace TrayMin.Tests.Fakes;

public sealed class FakeProcessProbe : IProcessProbe
{
    public long? Ticks = 130000000000000000;
    public long? GetStartTicks(uint pid) => Ticks;
}
