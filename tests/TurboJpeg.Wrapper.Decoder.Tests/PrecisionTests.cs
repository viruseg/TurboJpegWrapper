using TurboJpegWrapper.Decoder;

namespace TurboJpegWrapper.Decoder.Tests;

public sealed class PrecisionTests
{
    private static byte[] Decode(string fixture, TurboJpegBmpBitDepth bitDepth) =>
        new TurboJpegDecoder().DecodeToBmp(
            TestFixtures.Load(fixture),
            new TurboJpegDecodeOptions { BmpBitDepth = bitDepth });

    [Theory]
    [InlineData("lossless-p2.jpg")]
    [InlineData("lossless-p4.jpg")]
    [InlineData("lossless-p7.jpg")]
    public void DecodeToBmp_LowPrecisionRgb_ScalesColorBands(string fixture)
    {
        var bmp = Decode(fixture, TurboJpegBmpBitDepth.Bpp32);

        Assert.Equal((0, 0, 255, 255), BmpTestHelpers.Pixel(bmp, 5, 1));
        Assert.Equal((0, 255, 0, 255), BmpTestHelpers.Pixel(bmp, 5, 4));
        Assert.Equal((255, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 5, 7));
        Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 5, 10));
        Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 5, 13));
    }

    [Theory]
    [InlineData("lossless-p2.jpg")]
    [InlineData("lossless-p4.jpg")]
    [InlineData("lossless-p7.jpg")]
    public void DecodeToBmp_LowPrecisionRgb24_ScalesColorBands(string fixture)
    {
        var bmp = Decode(fixture, TurboJpegBmpBitDepth.Bpp24);

        Assert.Equal((0, 0, 255, 255), BmpTestHelpers.Pixel(bmp, 5, 1));
        Assert.Equal((0, 255, 0, 255), BmpTestHelpers.Pixel(bmp, 5, 4));
        Assert.Equal((255, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 5, 7));
        Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 5, 10));
        Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 5, 13));
    }

    [Theory]
    [InlineData("gray-lossless-p2.jpg")]
    [InlineData("gray-lossless-p4.jpg")]
    [InlineData("gray-lossless-p7.jpg")]
    public void DecodeToBmp_LowPrecisionGray_ScalesToEightBpp(string fixture)
    {
        var bmp = Decode(fixture, TurboJpegBmpBitDepth.Bpp8);

        Assert.Equal(8, BmpTestHelpers.BitsPerPixel(bmp));
        Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 5, 10));
        Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 5, 13));
    }

    [Theory]
    [InlineData("lossless-p2.jpg")]
    [InlineData("lossless-p4.jpg")]
    [InlineData("lossless-p7.jpg")]
    public void DecodeToBmp_LowPrecisionBgra_AlphaIs255(string fixture)
    {
        var bmp = Decode(fixture, TurboJpegBmpBitDepth.Bpp32);
        var pixels = bmp.AsSpan(BmpTestHelpers.PixelOffset(bmp));

        for (var i = 3; i < pixels.Length; i += 4)
        {
            Assert.Equal(255, pixels[i]);
        }
    }
}
