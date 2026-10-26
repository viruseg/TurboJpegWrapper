using TurboJpegWrapper.Decoder;

namespace TurboJpegWrapper.Decoder.Tests;

public sealed class CmykTests
{
    private static void AssertColorBands(byte[] bmp, bool ycckTransform = false)
    {
        Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 0, 0)); // white
        Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 1, 0));       // black
        Assert.Equal((0, 0, 255, 255), BmpTestHelpers.Pixel(bmp, 2, 0));     // red

        // The YCCK to CMYK transform is lossy: inverted cyan decodes with C' = 1.
        Assert.Equal(
            ycckTransform ? (255, 255, 1, 255) : (255, 255, 0, 255),
            BmpTestHelpers.Pixel(bmp, 3, 0));                                 // cyan
    }

    private static void AssertLumaBands(byte[] bmp, bool ycckTransform = false)
    {
        Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 0, 0)); // white
        Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 1, 0));       // black
        Assert.Equal((77, 77, 77, 255), BmpTestHelpers.Pixel(bmp, 2, 0));    // red

        var cyanLuma = (byte)(ycckTransform ? 179 : 178);
        Assert.Equal((cyanLuma, cyanLuma, cyanLuma, (byte)255),
            BmpTestHelpers.Pixel(bmp, 3, 0));                                 // cyan
    }

    [Theory]
    [InlineData("cmyk.jpg")]
    [InlineData("ycck.jpg")]
    public void DecodeToBmp_Cmyk_ProducesExpectedColors(string fixture)
    {
        var bmp = new TurboJpegDecoder().DecodeToBmp(TestFixtures.Load(fixture));

        Assert.Equal(32, BmpTestHelpers.BitsPerPixel(bmp));
        AssertColorBands(bmp, ycckTransform: fixture == "ycck.jpg");
    }

    [Theory]
    [InlineData("cmyk.jpg")]
    [InlineData("ycck.jpg")]
    public void DecodeToBmp_CmykBpp24_ProducesExpectedColors(string fixture)
    {
        var bmp = new TurboJpegDecoder().DecodeToBmp(
            TestFixtures.Load(fixture),
            new TurboJpegDecodeOptions { BmpBitDepth = TurboJpegBmpBitDepth.Bpp24 });

        Assert.Equal(24, BmpTestHelpers.BitsPerPixel(bmp));
        AssertColorBands(bmp, ycckTransform: fixture == "ycck.jpg");
    }

    [Theory]
    [InlineData("cmyk.jpg")]
    [InlineData("ycck.jpg")]
    public void DecodeToBmp_CmykBpp8_ProducesExpectedLuma(string fixture)
    {
        var bmp = new TurboJpegDecoder().DecodeToBmp(
            TestFixtures.Load(fixture),
            new TurboJpegDecodeOptions { BmpBitDepth = TurboJpegBmpBitDepth.Bpp8 });

        Assert.Equal(8, BmpTestHelpers.BitsPerPixel(bmp));
        AssertLumaBands(bmp, ycckTransform: fixture == "ycck.jpg");
    }

    [Theory]
    [InlineData(TurboJpegBmpBitDepth.Bpp32)]
    [InlineData(TurboJpegBmpBitDepth.Bpp24)]
    [InlineData(TurboJpegBmpBitDepth.Bpp8)]
    public void DecodeToBmp_CmykCroppedScalarTail_ProducesExpectedColors(
        TurboJpegBmpBitDepth bitDepth)
    {
        var bmp = new TurboJpegDecoder().DecodeToBmp(
            TestFixtures.Load("cmyk.jpg"),
            new TurboJpegDecodeOptions
            {
                BmpBitDepth = bitDepth,
                Crop = new TurboJpegCrop(0, 0, 3, 1)
            });

        Assert.Equal(3, BmpTestHelpers.Width(bmp));
        Assert.Equal((int)bitDepth, BmpTestHelpers.BitsPerPixel(bmp));
        Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 0, 0));
        Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 1, 0));

        if (bitDepth == TurboJpegBmpBitDepth.Bpp8)
        {
            Assert.Equal((77, 77, 77, 255), BmpTestHelpers.Pixel(bmp, 2, 0));
            return;
        }

        Assert.Equal((0, 0, 255, 255), BmpTestHelpers.Pixel(bmp, 2, 0));
    }

    [Fact]
    public void DecodeToBmp_CmykLowPrecision_ScalesBeforeConversion()
    {
        var bmp = new TurboJpegDecoder().DecodeToBmp(TestFixtures.Load("cmyk-p4.jpg"));

        Assert.Equal(4, BmpTestHelpers.Width(bmp));
        Assert.Equal(1, BmpTestHelpers.Height(bmp));
        AssertColorBands(bmp);
    }

    [Theory]
    [InlineData("cmyk12.jpg")]
    [InlineData("cmyk16.jpg")]
    public void DecodeToBmp_HighPrecisionCmyk_ProducesExpectedColors(string fixture)
    {
        var bmp = new TurboJpegDecoder().DecodeToBmp(TestFixtures.Load(fixture));

        Assert.Equal(4, BmpTestHelpers.Width(bmp));
        Assert.Equal(1, BmpTestHelpers.Height(bmp));
        Assert.Equal(32, BmpTestHelpers.BitsPerPixel(bmp));
        AssertColorBands(bmp);
    }

    [Theory]
    [InlineData("cmyk12.jpg", TurboJpegBmpBitDepth.Bpp24)]
    [InlineData("cmyk12.jpg", TurboJpegBmpBitDepth.Bpp8)]
    [InlineData("cmyk16.jpg", TurboJpegBmpBitDepth.Bpp24)]
    [InlineData("cmyk16.jpg", TurboJpegBmpBitDepth.Bpp8)]
    public void DecodeToBmp_HighPrecisionCmyk_DepthsProduceExpectedColors(
        string fixture, TurboJpegBmpBitDepth bitDepth)
    {
        var bmp = new TurboJpegDecoder().DecodeToBmp(
            TestFixtures.Load(fixture),
            new TurboJpegDecodeOptions { BmpBitDepth = bitDepth });

        Assert.Equal(4, BmpTestHelpers.Width(bmp));
        Assert.Equal(1, BmpTestHelpers.Height(bmp));
        Assert.Equal((int)bitDepth, BmpTestHelpers.BitsPerPixel(bmp));

        if (bitDepth == TurboJpegBmpBitDepth.Bpp8)
        {
            AssertLumaBands(bmp);
            return;
        }

        AssertColorBands(bmp);
    }

    [Fact]
    public void DecodeToBmp_HighPrecisionCmyk_InterleavedWithEightBit_DoesNotLeakState()
    {
        using var decoder = new TurboJpegDecoder();

        var first = decoder.DecodeToBmp(TestFixtures.Load("cmyk12.jpg"));
        AssertColorBands(first);

        var eightBit = decoder.DecodeToBmp(
            TestFixtures.Load("cmyk.jpg"),
            new TurboJpegDecodeOptions { BmpBitDepth = TurboJpegBmpBitDepth.Bpp8 });
        AssertLumaBands(eightBit);

        var last = decoder.DecodeToBmp(TestFixtures.Load("cmyk16.jpg"));
        AssertColorBands(last);
    }

    [Fact]
    public void DecodeToBmp_CmykLowPrecision_WithAutoOrient_ConvertsCorrectly()
    {
        var jpeg = JpegPatcher.WithExifOrientation(TestFixtures.Load("cmyk-p4.jpg"), 6);

        var bmp = new TurboJpegDecoder().DecodeToBmp(jpeg, new TurboJpegDecodeOptions
        {
            AutoOrient = true
        });

        Assert.Equal(1, BmpTestHelpers.Width(bmp));
        Assert.Equal(4, BmpTestHelpers.Height(bmp));
        Assert.Equal(32, BmpTestHelpers.BitsPerPixel(bmp));
        Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 0, 0));
        Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 0, 1));
        Assert.Equal((0, 0, 255, 255), BmpTestHelpers.Pixel(bmp, 0, 2));
        Assert.Equal((255, 255, 0, 255), BmpTestHelpers.Pixel(bmp, 0, 3));
    }

    [Theory]
    [InlineData("cmyk12.jpg")]
    [InlineData("cmyk16.jpg")]
    public void DecodeToBmp_HighPrecisionCmyk_WithAutoOrient_ProducesExpectedColors(string fixture)
    {
        var jpeg = JpegPatcher.WithExifOrientation(TestFixtures.Load(fixture), 6);

        var bmp = new TurboJpegDecoder().DecodeToBmp(jpeg, new TurboJpegDecodeOptions
        {
            AutoOrient = true
        });

        Assert.Equal(1, BmpTestHelpers.Width(bmp));
        Assert.Equal(4, BmpTestHelpers.Height(bmp));
        Assert.Equal(32, BmpTestHelpers.BitsPerPixel(bmp));
        Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 0, 0));
        Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 0, 1));
        Assert.Equal((0, 0, 255, 255), BmpTestHelpers.Pixel(bmp, 0, 2));
        Assert.Equal((255, 255, 0, 255), BmpTestHelpers.Pixel(bmp, 0, 3));
    }

    [Fact]
    public void DecodeToBmp_CmykWithAutoOrient_Works()
    {
        var jpeg = JpegPatcher.WithExifOrientation(TestFixtures.Load("cmyk.jpg"), 6);

        var bmp = new TurboJpegDecoder().DecodeToBmp(jpeg, new TurboJpegDecodeOptions
        {
            AutoOrient = true
        });

        Assert.Equal(1, BmpTestHelpers.Width(bmp));
        Assert.Equal(4, BmpTestHelpers.Height(bmp));
        Assert.Equal(32, BmpTestHelpers.BitsPerPixel(bmp));

        // Orientation 6 maps source (x, y) to output (height - 1 - y, x).
        Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 0, 0));
        Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 0, 1));
        Assert.Equal((0, 0, 255, 255), BmpTestHelpers.Pixel(bmp, 0, 2));
        Assert.Equal((255, 255, 0, 255), BmpTestHelpers.Pixel(bmp, 0, 3));
    }

    [Fact]
    public void DecodeToBmp_CmykTwiceWithDifferentOptions_DoesNotLeakState()
    {
        using var decoder = new TurboJpegDecoder();

        var lowPrecision = decoder.DecodeToBmp(
            TestFixtures.Load("cmyk-p4.jpg"),
            new TurboJpegDecodeOptions { BmpBitDepth = TurboJpegBmpBitDepth.Bpp8 });
        AssertLumaBands(lowPrecision);

        var fullPrecision = decoder.DecodeToBmp(TestFixtures.Load("cmyk.jpg"));
        AssertColorBands(fullPrecision);
    }

    [Fact]
    public void GetBmpSize_HugeDimensions_CmykScratchTooLarge_ThrowsImageTooLarge()
    {
        var jpeg = TestFixtures.Load("cmyk.jpg");
        JpegPatcher.SetDimensions(jpeg, 30000, 20000);
        using var decoder = new TurboJpegDecoder();

        var exception = Assert.Throws<TurboJpegImageTooLargeException>(
            () => decoder.GetBmpSize(jpeg, new TurboJpegDecodeOptions
            {
                BmpBitDepth = TurboJpegBmpBitDepth.Bpp8,
                MaxPixels = 0
            }));

        Assert.Contains("scratch", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetBmpSize_HugeDimensions_LosslessRgbToGrayScratchTooLarge_ThrowsImageTooLarge()
    {
        var jpeg = TestFixtures.Load("lossless-p8.jpg");
        JpegPatcher.SetDimensions(jpeg, 30000, 25000);
        using var decoder = new TurboJpegDecoder();

        var exception = Assert.Throws<TurboJpegImageTooLargeException>(
            () => decoder.GetBmpSize(jpeg, new TurboJpegDecodeOptions
            {
                BmpBitDepth = TurboJpegBmpBitDepth.Bpp8,
                MaxPixels = 0
            }));

        Assert.Contains("scratch", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
