using TurboJpegWrapper.Internal.Bmp;

namespace TurboJpegWrapper.Tests;

public sealed class MirrorRowsTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 0)]
    [InlineData(4, 5)]
    [InlineData(5, 0)]
    [InlineData(6, 0)]
    [InlineData(6, 5)]
    [InlineData(7, 5)]
    [InlineData(8, 0)]
    [InlineData(9, 5)]
    [InlineData(11, 0)]
    [InlineData(12, 5)]
    [InlineData(13, 0)]
    [InlineData(23, 5)]
    [InlineData(24, 0)]
    [InlineData(25, 5)]
    [InlineData(35, 0)]
    [InlineData(64, 5)]
    [InlineData(65, 0)]
    public void MirrorRowsInPlace_24Bit_MatchesReference(int width, int padding)
    {
        const int height = 3;
        var rowBytes = width * 3;
        var stride = rowBytes + padding;
        var pixels = new byte[stride * height];
        Array.Fill(pixels, (byte)0xEE);

        for (var y = 0; y < height; y++)
        {
            for (var i = 0; i < rowBytes; i++)
            {
                pixels[y * stride + i] = (byte)((y * rowBytes + i) * 37 + 11);
            }
        }

        var expected = (byte[])pixels.Clone();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var source = y * stride + (width - 1 - x) * 3;
                var destination = y * stride + x * 3;
                expected[destination] = pixels[source];
                expected[destination + 1] = pixels[source + 1];
                expected[destination + 2] = pixels[source + 2];
            }
        }

        BmpOrientationWriter.MirrorRowsInPlace(pixels, width, height, stride, 3);

        Assert.Equal(expected, pixels);
    }
}
