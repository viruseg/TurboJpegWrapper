using TurboJpegWrapper.Internal.Decoder.Conversion;

namespace TurboJpegWrapper.Decoder.Tests;

public sealed class GrayConverterTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(33)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(4096)]
    public void ExpandToBgr_MatchesScalarReference(int pixelCount)
    {
        var gray = CreateGray(pixelCount);
        var expected = new byte[pixelCount * 3];
        for (var i = 0; i < pixelCount; i++)
        {
            var value = gray[i];
            var offset = i * 3;
            expected[offset] = value;
            expected[offset + 1] = value;
            expected[offset + 2] = value;
        }

        var destination = new byte[expected.Length + 16];
        destination.AsSpan().Fill(0xEE);

        GrayConverter.ExpandToBgr(gray, destination, pixelCount);

        Assert.Equal(expected, destination.AsSpan(0, expected.Length).ToArray());
        Assert.All(
            destination.AsSpan(expected.Length).ToArray(),
            value => Assert.Equal(0xEE, value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(33)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(4096)]
    public void ExpandToBgra_MatchesScalarReference(int pixelCount)
    {
        var gray = CreateGray(pixelCount);
        var expected = new byte[pixelCount * 4];
        for (var i = 0; i < pixelCount; i++)
        {
            var value = gray[i];
            var offset = i * 4;
            expected[offset] = value;
            expected[offset + 1] = value;
            expected[offset + 2] = value;
            expected[offset + 3] = 255;
        }

        var destination = new byte[expected.Length + 16];
        destination.AsSpan().Fill(0xEE);

        GrayConverter.ExpandToBgra(gray, destination, pixelCount);

        Assert.Equal(expected, destination.AsSpan(0, expected.Length).ToArray());
        Assert.All(
            destination.AsSpan(expected.Length).ToArray(),
            value => Assert.Equal(0xEE, value));
    }

    [Fact]
    public void Expand_OffsetsIntoBuffers_MatchesScalarReference()
    {
        const int pixelCount = 37;
        var grayBuffer = new byte[pixelCount + 3];
        for (var i = 0; i < grayBuffer.Length; i++)
        {
            grayBuffer[i] = (byte)(i * 29 + 5);
        }

        var gray = grayBuffer.AsSpan(3, pixelCount);
        var bgr = new byte[pixelCount * 3 + 21];
        var bgra = new byte[pixelCount * 4 + 21];
        bgr.AsSpan().Fill(0xEE);
        bgra.AsSpan().Fill(0xEE);

        GrayConverter.ExpandToBgr(gray, bgr.AsSpan(5), pixelCount);
        GrayConverter.ExpandToBgra(gray, bgra.AsSpan(5), pixelCount);

        for (var i = 0; i < pixelCount; i++)
        {
            var value = gray[i];
            Assert.Equal(value, bgr[5 + i * 3]);
            Assert.Equal(value, bgr[5 + i * 3 + 1]);
            Assert.Equal(value, bgr[5 + i * 3 + 2]);
            Assert.Equal(value, bgra[5 + i * 4]);
            Assert.Equal(value, bgra[5 + i * 4 + 1]);
            Assert.Equal(value, bgra[5 + i * 4 + 2]);
            Assert.Equal(255, bgra[5 + i * 4 + 3]);
        }

        Assert.All(bgr.AsSpan(0, 5).ToArray(), value => Assert.Equal(0xEE, value));
        Assert.All(bgra.AsSpan(0, 5).ToArray(), value => Assert.Equal(0xEE, value));
        Assert.All(
            bgr.AsSpan(5 + pixelCount * 3).ToArray(),
            value => Assert.Equal(0xEE, value));
        Assert.All(
            bgra.AsSpan(5 + pixelCount * 4).ToArray(),
            value => Assert.Equal(0xEE, value));
    }

    private static byte[] CreateGray(int pixelCount)
    {
        var gray = new byte[pixelCount];
        for (var i = 0; i < gray.Length; i++)
        {
            gray[i] = (byte)(i * 37 + 11);
        }

        return gray;
    }
}
