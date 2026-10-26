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
        Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 5, 1));
        Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 5, 21));
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

    [Theory]
    [InlineData("lossless-p9.jpg")]
    [InlineData("lossless-p10.jpg")]
    [InlineData("lossless-p11.jpg")]
    [InlineData("lossless-p12.jpg")]
    [InlineData("lossless-p13.jpg")]
    [InlineData("lossless-p14.jpg")]
    [InlineData("lossless-p15.jpg")]
    [InlineData("lossless-p16.jpg")]
    public void DecodeToBmp_HighPrecisionRgb_ProducesFullRangeBands(string fixture)
    {
        var bmp = new TurboJpegDecoder().DecodeToBmp(TestFixtures.Load(fixture));

        Assert.Equal((0, 0, 255, 255), BmpTestHelpers.Pixel(bmp, 5, 1));
        Assert.Equal((0, 255, 0, 255), BmpTestHelpers.Pixel(bmp, 5, 4));
        Assert.Equal((255, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 5, 7));
        Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 5, 10));
        Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 5, 13));
    }

    [Theory]
    [InlineData("lossless-p9.jpg", TurboJpegBmpBitDepth.Bpp8)]
    [InlineData("lossless-p10.jpg", TurboJpegBmpBitDepth.Bpp8)]
    [InlineData("lossless-p11.jpg", TurboJpegBmpBitDepth.Bpp8)]
    [InlineData("lossless-p12.jpg", TurboJpegBmpBitDepth.Bpp8)]
    [InlineData("lossless-p13.jpg", TurboJpegBmpBitDepth.Bpp8)]
    [InlineData("lossless-p14.jpg", TurboJpegBmpBitDepth.Bpp8)]
    [InlineData("lossless-p15.jpg", TurboJpegBmpBitDepth.Bpp8)]
    [InlineData("lossless-p16.jpg", TurboJpegBmpBitDepth.Bpp8)]
    [InlineData("lossless-p9.jpg", TurboJpegBmpBitDepth.Bpp24)]
    [InlineData("lossless-p10.jpg", TurboJpegBmpBitDepth.Bpp24)]
    [InlineData("lossless-p11.jpg", TurboJpegBmpBitDepth.Bpp24)]
    [InlineData("lossless-p12.jpg", TurboJpegBmpBitDepth.Bpp24)]
    [InlineData("lossless-p13.jpg", TurboJpegBmpBitDepth.Bpp24)]
    [InlineData("lossless-p14.jpg", TurboJpegBmpBitDepth.Bpp24)]
    [InlineData("lossless-p15.jpg", TurboJpegBmpBitDepth.Bpp24)]
    [InlineData("lossless-p16.jpg", TurboJpegBmpBitDepth.Bpp24)]
    public void DecodeToBmp_HighPrecisionRgb_Bpp8AndBpp24(
        string fixture, TurboJpegBmpBitDepth bitDepth)
    {
        var bmp = Decode(fixture, bitDepth);

        Assert.Equal((int)bitDepth, BmpTestHelpers.BitsPerPixel(bmp));

        if (bitDepth == TurboJpegBmpBitDepth.Bpp8)
        {
            Assert.Equal((77, 77, 77, 255), BmpTestHelpers.Pixel(bmp, 5, 1));
            Assert.Equal((149, 149, 149, 255), BmpTestHelpers.Pixel(bmp, 5, 4));
            Assert.Equal((29, 29, 29, 255), BmpTestHelpers.Pixel(bmp, 5, 7));
            Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 5, 10));
            Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 5, 13));
            return;
        }

        Assert.Equal((0, 0, 255, 255), BmpTestHelpers.Pixel(bmp, 5, 1));
        Assert.Equal((0, 255, 0, 255), BmpTestHelpers.Pixel(bmp, 5, 4));
        Assert.Equal((255, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 5, 7));
        Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 5, 10));
        Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 5, 13));
    }

    [Fact]
    public void DecodeToBmp_LowPrecisionRgb_WithAutoOrient_ScalesAndSwaps()
    {
        var jpeg = JpegPatcher.WithExifOrientation(TestFixtures.Load("lossless-p4.jpg"), 6);

        var bmp = new TurboJpegDecoder().DecodeToBmp(jpeg, new TurboJpegDecodeOptions
        {
            AutoOrient = true
        });

        Assert.Equal(
            (TestFixtures.Height, TestFixtures.Width),
            (BmpTestHelpers.Width(bmp), BmpTestHelpers.Height(bmp)));
        Assert.Equal(32, BmpTestHelpers.BitsPerPixel(bmp));

        Assert.Equal((0, 0, 255, 255), BmpTestHelpers.Pixel(bmp, 21, 5));
        Assert.Equal((0, 255, 0, 255), BmpTestHelpers.Pixel(bmp, 18, 5));
        Assert.Equal((255, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 15, 5));
        Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 12, 5));
        Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 9, 5));
    }

    [Theory]
    [InlineData("lossless-p12.jpg", TurboJpegBmpBitDepth.Bpp32)]
    [InlineData("lossless-p12.jpg", TurboJpegBmpBitDepth.Bpp8)]
    [InlineData("gray-lossless-p16.jpg", TurboJpegBmpBitDepth.Bpp32)]
    public void DecodeToBmp_HighPrecision_WithAutoOrient_Works(
        string fixture, TurboJpegBmpBitDepth bitDepth)
    {
        var jpeg = JpegPatcher.WithExifOrientation(TestFixtures.Load(fixture), 6);

        var bmp = new TurboJpegDecoder().DecodeToBmp(jpeg, new TurboJpegDecodeOptions
        {
            BmpBitDepth = bitDepth,
            AutoOrient = true
        });

        Assert.Equal(
            (TestFixtures.Height, TestFixtures.Width),
            (BmpTestHelpers.Width(bmp), BmpTestHelpers.Height(bmp)));
        Assert.Equal((int)bitDepth, BmpTestHelpers.BitsPerPixel(bmp));

        if (fixture.StartsWith("gray", StringComparison.Ordinal))
        {
            Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 21, 5));
            Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 1, 5));
            return;
        }

        if (bitDepth == TurboJpegBmpBitDepth.Bpp8)
        {
            Assert.Equal((77, 77, 77, 255), BmpTestHelpers.Pixel(bmp, 21, 5));
            Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 12, 5));
            Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 9, 5));
            return;
        }

        Assert.Equal((0, 0, 255, 255), BmpTestHelpers.Pixel(bmp, 21, 5));
        Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 12, 5));
        Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 9, 5));
    }

    [Theory]
    [InlineData("gray-lossless-p9.jpg")]
    [InlineData("gray-lossless-p12.jpg")]
    [InlineData("gray-lossless-p16.jpg")]
    public void DecodeToBmp_HighPrecisionGray_Bpp8(string fixture)
    {
        var bmp = Decode(fixture, TurboJpegBmpBitDepth.Bpp8);

        Assert.Equal(8, BmpTestHelpers.BitsPerPixel(bmp));
        Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 5, 1));
        Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 5, 21));
    }

    [Theory]
    [InlineData("lossless-p9.jpg")]
    [InlineData("lossless-p10.jpg")]
    [InlineData("lossless-p11.jpg")]
    [InlineData("lossless-p12.jpg")]
    [InlineData("lossless-p13.jpg")]
    [InlineData("lossless-p14.jpg")]
    [InlineData("lossless-p15.jpg")]
    [InlineData("lossless-p16.jpg")]
    public void DecodeToBmp_HighPrecisionBgra_AlphaIs255(string fixture)
    {
        var bmp = new TurboJpegDecoder().DecodeToBmp(TestFixtures.Load(fixture));
        var pixels = bmp.AsSpan(BmpTestHelpers.PixelOffset(bmp));

        for (var i = 3; i < pixels.Length; i += 4)
        {
            Assert.Equal(255, pixels[i]);
        }
    }

    [Fact]
    public void DecodeToBmp_Precision12MidGray_WithinOneLsb()
    {
        var bmp = new TurboJpegDecoder().DecodeToBmp(TestFixtures.Load("precision12.jpg"));
        var (b, g, r, a) = BmpTestHelpers.Pixel(bmp, 5, 21);

        Assert.True(Math.Abs(b - 128) <= 1, $"b={b}");
        Assert.True(Math.Abs(g - 128) <= 1, $"g={g}");
        Assert.True(Math.Abs(r - 128) <= 1, $"r={r}");
        Assert.Equal(255, a);
    }

    [Fact]
    public void DecodeToBmp_Precision16MidGray_WithinOneLsb()
    {
        var bmp = Decode("gray-lossless-p16.jpg", TurboJpegBmpBitDepth.Bpp32);
        var (b, g, r, a) = BmpTestHelpers.Pixel(bmp, 5, 13);

        Assert.True(Math.Abs(b - 128) <= 1, $"b={b}");
        Assert.True(Math.Abs(g - 128) <= 1, $"g={g}");
        Assert.True(Math.Abs(r - 128) <= 1, $"r={r}");
        Assert.Equal(255, a);
    }
}
