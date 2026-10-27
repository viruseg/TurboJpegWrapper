using TurboJpegWrapper;
using DecoderOptions = TurboJpegWrapper.TurboJpegDecodeOptions;
using DecoderBitDepth = TurboJpegWrapper.TurboJpegBmpBitDepth;

namespace TurboJpegWrapper.Encoder.Tests;

public sealed class EncodeDefaultTests
{
    private static (byte B, byte G, byte R) Gradient(int x, int y) =>
        ((byte)(x * 7 + y * 3), (byte)(x * 3 + y * 11), (byte)(x * 5 + y * 13));

    private static (byte B, byte G, byte R, byte A) Gradient32(int x, int y)
    {
        var (b, g, r) = Gradient(x, y);
        return (b, g, r, (byte)(x * 5 + y * 9));
    }

    [Fact]
    public void EncodeToJpeg_DefaultOptions_ReportsSourceGeometryAndYuv420()
    {
        var bmp = BmpBuilder.Create24(35, 23, Gradient);
        using var encoder = new TurboJpegEncoder();

        var info = TurboJpeg.GetImageInfo(encoder.EncodeToJpeg(bmp));

        Assert.Equal(35, info.Width);
        Assert.Equal(23, info.Height);
        Assert.Equal((int)TurboJpegColorspace.YCbCr, (int)info.Colorspace);
        Assert.Equal((int)TurboJpegSubsampling.Yuv420, (int)info.Subsampling);
    }

    [Fact]
    public void EncodeToJpeg_DefaultOptions_DecodesToSourcePixels()
    {
        var bmp = BmpBuilder.Create24(32, 24, (x, y) =>
        {
            var left = x < 16;
            if (y < 12)
            {
                // Red in the top-left quadrant, green in the top-right.
                return left ? ((byte)0, (byte)0, (byte)255) : ((byte)0, (byte)255, (byte)0);
            }

            // Blue in the bottom-left quadrant, white in the bottom-right.
            return left ? ((byte)255, (byte)0, (byte)0) : ((byte)255, (byte)255, (byte)255);
        });
        using var encoder = new TurboJpegEncoder();

        var decoded = DecodedBmp.Decode(encoder.EncodeToJpeg(bmp));

        Assert.Equal(32, decoded.Width);
        Assert.Equal(24, decoded.Height);
        Assert.True(decoded.BottomUp);
        AssertPixel((0, 0, 255), decoded.GetPixel(8, 6));
        AssertPixel((0, 255, 0), decoded.GetPixel(24, 6));
        AssertPixel((255, 0, 0), decoded.GetPixel(8, 18));
        AssertPixel((255, 255, 255), decoded.GetPixel(24, 18));
    }

    [Fact]
    public void EncodeToJpeg_32BitAnd24BitSourcesWithSameRgb_ProduceIdenticalJpeg()
    {
        var bmp24 = BmpBuilder.Create24(35, 23, Gradient);
        var bmp32 = BmpBuilder.Create32(35, 23, (x, y) =>
        {
            var (b, g, r) = Gradient(x, y);
            return (b, g, r, (byte)(x * 5 + y * 9));
        });
        using var encoder = new TurboJpegEncoder();

        Assert.Equal(encoder.EncodeToJpeg(bmp24), encoder.EncodeToJpeg(bmp32));
    }

    [Fact]
    public void EncodeToJpeg_TopDownAndBottomUpSources_ProduceIdenticalJpeg()
    {
        var bottomUp = BmpBuilder.Create24(35, 23, Gradient);
        var topDown = BmpBuilder.Create24(35, 23, Gradient, topDown: true);
        using var encoder = new TurboJpegEncoder();

        Assert.Equal(encoder.EncodeToJpeg(bottomUp), encoder.EncodeToJpeg(topDown));
    }

    [Fact]
    public void EncodeToJpeg_32BitTopDownAndBottomUpSources_ProduceIdenticalJpeg()
    {
        var bottomUp = BmpBuilder.Create32(35, 23, Gradient32);
        var topDown = BmpBuilder.Create32(35, 23, Gradient32, topDown: true);
        using var encoder = new TurboJpegEncoder();

        Assert.Equal(encoder.EncodeToJpeg(bottomUp), encoder.EncodeToJpeg(topDown));
    }

    [Fact]
    public void EncodeToJpeg_BmpDensity_ConvertsPixelsPerMeterToDpi()
    {
        var bmp = BmpBuilder.Create24(8, 8, Gradient, ppmX: 3780, ppmY: 3780);
        using var encoder = new TurboJpegEncoder();

        var info = TurboJpeg.GetImageInfo(encoder.EncodeToJpeg(bmp));

        Assert.Equal(96, info.DensityX);
        Assert.Equal(96, info.DensityY);
        Assert.Equal((int)TurboJpegDensityUnits.PixelsPerInch, (int)info.DensityUnits);
    }

    [Fact]
    public void EncodeToJpeg_EmptyBmp_ThrowsArgumentException()
    {
        using var encoder = new TurboJpegEncoder();

        Assert.Throws<ArgumentException>(() => encoder.EncodeToJpeg(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void EncodeToJpeg_NullOptions_ThrowsArgumentNullException()
    {
        var bmp = BmpBuilder.Create24(2, 2, Gradient);
        using var encoder = new TurboJpegEncoder();

        Assert.Throws<ArgumentNullException>(() => encoder.EncodeToJpeg(bmp, null!));
    }

    [Fact]
    public void EncodeToJpeg_DisposedEncoder_ThrowsObjectDisposedException()
    {
        var bmp = BmpBuilder.Create24(2, 2, Gradient);
        var encoder = new TurboJpegEncoder();

        encoder.Dispose();

        Assert.Throws<ObjectDisposedException>(() => encoder.EncodeToJpeg(bmp));
    }

    private static void AssertPixel(
        (byte B, byte G, byte R) expected, (byte B, byte G, byte R) actual, int tolerance = 12)
    {
        Assert.InRange(actual.B, Math.Max(0, expected.B - tolerance), Math.Min(255, expected.B + tolerance));
        Assert.InRange(actual.G, Math.Max(0, expected.G - tolerance), Math.Min(255, expected.G + tolerance));
        Assert.InRange(actual.R, Math.Max(0, expected.R - tolerance), Math.Min(255, expected.R + tolerance));
    }
}
