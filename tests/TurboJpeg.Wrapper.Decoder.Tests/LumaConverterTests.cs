using TurboJpegWrapper.Decoder.Internal.Conversion;

namespace TurboJpegWrapper.Decoder.Tests;

public sealed class LumaConverterTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(33)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(4096)]
    public void BgrToGray_MatchesScalarReference(int pixelCount)
    {
        var bgr = CreateBgr(pixelCount);
        var expected = new byte[pixelCount];
        for (var i = 0; i < pixelCount; i++)
        {
            var offset = i * 3;
            expected[i] = (byte)(
                (77 * bgr[offset + 2] + 150 * bgr[offset + 1] + 29 * bgr[offset] + 128) >> 8);
        }

        var destination = new byte[expected.Length + 16];
        destination.AsSpan().Fill(0xEE);

        LumaConverter.BgrToGray(bgr, destination, pixelCount);

        Assert.Equal(expected, destination.AsSpan(0, expected.Length).ToArray());
        Assert.All(
            destination.AsSpan(expected.Length).ToArray(),
            value => Assert.Equal(0xEE, value));
    }

    [Fact]
    public void BgrToGray_OffsetIntoBuffers_MatchesScalarReference()
    {
        const int pixelCount = 37;
        var bgrBuffer = new byte[pixelCount * 3 + 5];
        for (var i = 0; i < bgrBuffer.Length; i++)
        {
            bgrBuffer[i] = (byte)(i * 41 + 3);
        }

        var bgr = bgrBuffer.AsSpan(5, pixelCount * 3);
        var destination = new byte[pixelCount + 21];
        destination.AsSpan().Fill(0xEE);

        LumaConverter.BgrToGray(bgr, destination.AsSpan(3), pixelCount);

        for (var i = 0; i < pixelCount; i++)
        {
            var offset = i * 3;
            Assert.Equal(
                (byte)((77 * bgr[offset + 2] + 150 * bgr[offset + 1] + 29 * bgr[offset] + 128) >> 8),
                destination[3 + i]);
        }

        Assert.All(destination.AsSpan(0, 3).ToArray(), value => Assert.Equal(0xEE, value));
        Assert.All(
            destination.AsSpan(3 + pixelCount).ToArray(),
            value => Assert.Equal(0xEE, value));
    }

    private static byte[] CreateBgr(int pixelCount)
    {
        var bgr = new byte[pixelCount * 3];
        for (var i = 0; i < bgr.Length; i++)
        {
            bgr[i] = (byte)(i * 31 + 7);
        }

        return bgr;
    }
}
