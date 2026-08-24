using System.Buffers.Binary;
using Xunit;

namespace TrayMin.Tests;

public class IconAssetTests
{
    [Fact]
    public void Ico_contains_exact_required_png_frames()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "traymin.ico");
        var data = File.ReadAllBytes(path);

        Assert.Equal(0, BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(0, 2)));
        Assert.Equal(1, BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(2, 2)));
        var count = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(4, 2));
        Assert.Equal(6, count);

        var sizes = new List<int>();
        ReadOnlySpan<byte> pngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

        for (var index = 0; index < count; index++)
        {
            var entry = data.AsSpan(6 + index * 16, 16);
            var width = entry[0] == 0 ? 256 : entry[0];
            var height = entry[1] == 0 ? 256 : entry[1];
            Assert.Equal(width, height);
            Assert.Equal(1, BinaryPrimitives.ReadUInt16LittleEndian(entry[4..6]));
            Assert.Equal(32, BinaryPrimitives.ReadUInt16LittleEndian(entry[6..8]));

            var length = BinaryPrimitives.ReadUInt32LittleEndian(entry[8..12]);
            var offset = BinaryPrimitives.ReadUInt32LittleEndian(entry[12..16]);
            Assert.True(offset + length <= data.Length);
            Assert.True(data.AsSpan((int)offset, 8).SequenceEqual(pngSignature));
            sizes.Add(width);
        }

        Assert.Equal([16, 20, 24, 32, 48, 256], sizes);
    }
}
