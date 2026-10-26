using TurboJpegWrapper.Decoder;

namespace TurboJpegWrapper.Decoder.Tests;

public sealed class CropTests
{
    [Fact]
    public void DecodeToBmp_AlignedCrop_MatchesFullDecodeSubregion()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = TestFixtures.Load("baseline-420.jpg");
        var full = decoder.DecodeToBmp(jpeg);
        var cropped = decoder.DecodeToBmp(jpeg, new TurboJpegDecodeOptions
        {
            Crop = new TurboJpegCrop(0, 0, 16, 16)
        });

        Assert.Equal((16, 16), (BmpTestHelpers.Width(cropped), BmpTestHelpers.Height(cropped)));
        Assert.Equal(BmpTestHelpers.Pixel(full, 5, 4), BmpTestHelpers.Pixel(cropped, 5, 4));
        Assert.Equal(BmpTestHelpers.Pixel(full, 12, 11), BmpTestHelpers.Pixel(cropped, 12, 11));
    }

    [Fact]
    public void DecodeToBmp_UnalignedY_IsAccepted()
    {
        // X=0 is aligned to the 16-pixel 4:2:0 iMCU; Y=8 is intentionally not.
        var bmp = new TurboJpegDecoder().DecodeToBmp(
            TestFixtures.Load("baseline-420.jpg"),
            new TurboJpegDecodeOptions { Crop = new TurboJpegCrop(0, 8, 16, 8) });
        Assert.Equal((16, 8), (BmpTestHelpers.Width(bmp), BmpTestHelpers.Height(bmp)));
    }

    [Fact]
    public void DecodeToBmp_MisalignedX_ThrowsArgumentException()
    {
        using var decoder = new TurboJpegDecoder();

        Assert.Throws<ArgumentException>(() => decoder.DecodeToBmp(
            TestFixtures.Load("baseline-420.jpg"),
            new TurboJpegDecodeOptions { Crop = new TurboJpegCrop(8, 0, 16, 8) }));
    }

    [Fact]
    public void DecodeToBmp_DefaultCropStruct_ThrowsArgumentException()
    {
        using var decoder = new TurboJpegDecoder();

        Assert.Throws<ArgumentException>(() => decoder.DecodeToBmp(
            TestFixtures.Load("baseline-420.jpg"),
            new TurboJpegDecodeOptions { Crop = default(TurboJpegCrop) }));
    }

    [Fact]
    public void DecodeToBmp_CropOutsideScaledImage_ThrowsArgumentException()
    {
        using var decoder = new TurboJpegDecoder();

        Assert.Throws<ArgumentException>(() => decoder.DecodeToBmp(
            TestFixtures.Load("baseline-420.jpg"),
            new TurboJpegDecodeOptions { Crop = new TurboJpegCrop(16, 0, 24, 8) }));
    }

    [Fact]
    public void DecodeToBmp_CropWithHalfScale_UsesScaledImcu()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = TestFixtures.Load("baseline-420.jpg");
        var full = decoder.DecodeToBmp(jpeg, new TurboJpegDecodeOptions
        {
            ScalingFactor = new TurboJpegScalingFactor(1, 2)
        });
        var cropped = decoder.DecodeToBmp(jpeg, new TurboJpegDecodeOptions
        {
            ScalingFactor = new TurboJpegScalingFactor(1, 2),
            Crop = new TurboJpegCrop(8, 0, 10, 12)
        });

        Assert.Equal((10, 12), (BmpTestHelpers.Width(cropped), BmpTestHelpers.Height(cropped)));
        Assert.Equal((18, 12), (BmpTestHelpers.Width(full), BmpTestHelpers.Height(full)));
        Assert.Equal(BmpTestHelpers.Pixel(full, 10, 3), BmpTestHelpers.Pixel(cropped, 2, 3));
    }

    [Fact]
    public void DecodeToBmp_CropWithUpscaling_Works()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = TestFixtures.Load("baseline-420.jpg");
        var full = decoder.DecodeToBmp(jpeg, new TurboJpegDecodeOptions
        {
            ScalingFactor = new TurboJpegScalingFactor(2, 1)
        });
        var cropped = decoder.DecodeToBmp(jpeg, new TurboJpegDecodeOptions
        {
            ScalingFactor = new TurboJpegScalingFactor(2, 1),
            Crop = new TurboJpegCrop(0, 0, 32, 32)
        });

        Assert.Equal((32, 32), (BmpTestHelpers.Width(cropped), BmpTestHelpers.Height(cropped)));
        Assert.Equal(BmpTestHelpers.Pixel(full, 5, 5), BmpTestHelpers.Pixel(cropped, 5, 5));
        Assert.Equal(BmpTestHelpers.Pixel(full, 5, 12), BmpTestHelpers.Pixel(cropped, 5, 12));
        Assert.Equal(BmpTestHelpers.Pixel(full, 5, 28), BmpTestHelpers.Pixel(cropped, 5, 28));
        AssertColor(cropped, 5, 5, 255, 0, 0, tolerance: 32);
        AssertColor(cropped, 5, 12, 0, 255, 0, tolerance: 32);
        AssertColor(cropped, 5, 20, 0, 0, 255, tolerance: 32);
        AssertColor(cropped, 5, 28, 255, 255, 255, tolerance: 32);
    }

    [Fact]
    public void DecodeToBmp_12BitLossyCrop_Works()
    {
        var bmp = new TurboJpegDecoder().DecodeToBmp(
            TestFixtures.Load("precision12.jpg"),
            new TurboJpegDecodeOptions { Crop = new TurboJpegCrop(0, 0, 16, 16) });

        Assert.Equal((16, 16), (BmpTestHelpers.Width(bmp), BmpTestHelpers.Height(bmp)));
        Assert.Equal(32, BmpTestHelpers.BitsPerPixel(bmp));
    }

    [Fact]
    public void DecodeToBmp_LosslessCrop_ThrowsArgumentException()
    {
        using var decoder = new TurboJpegDecoder();

        Assert.Throws<ArgumentException>(() => decoder.DecodeToBmp(
            TestFixtures.Load("lossless.jpg"),
            new TurboJpegDecodeOptions { Crop = new TurboJpegCrop(0, 0, 16, 8) }));
    }

    [Fact]
    public void DecodeToBmp_UnknownSubsamplingCrop_ThrowsArgumentException()
    {
        using var decoder = new TurboJpegDecoder();

        Assert.Throws<ArgumentException>(() => decoder.DecodeToBmp(
            TestFixtures.Load("s-unknown.jpg"),
            new TurboJpegDecodeOptions { Crop = new TurboJpegCrop(0, 0, 16, 8) }));
    }

    [Fact]
    public void GetBmpSize_WithCrop_MatchesDecodeLength()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = TestFixtures.Load("baseline-420.jpg");
        var options = new TurboJpegDecodeOptions { Crop = new TurboJpegCrop(0, 0, 16, 16) };

        var bmp = decoder.DecodeToBmp(jpeg, options);

        Assert.Equal(bmp.Length, decoder.GetBmpSize(jpeg, options));
        Assert.Equal((16, 16), (BmpTestHelpers.Width(bmp), BmpTestHelpers.Height(bmp)));
    }

    [Fact]
    public void DecodeToBmp_SpanWithCrop_WritesExactSize()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = TestFixtures.Load("baseline-420.jpg");
        var options = new TurboJpegDecodeOptions { Crop = new TurboJpegCrop(0, 8, 16, 8) };
        var expected = decoder.DecodeToBmp(jpeg, options);
        var destination = new byte[decoder.GetBmpSize(jpeg, options)];

        var written = decoder.DecodeToBmp(jpeg, options, destination);

        Assert.Equal(destination.Length, written);
        Assert.Equal(expected, destination);
        Assert.Equal((16, 8), (BmpTestHelpers.Width(destination), BmpTestHelpers.Height(destination)));
    }

    [Fact]
    public void DecodeToBmp_CropWithAutoOrient_AppliesCropBeforeOrientation()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = JpegPatcher.WithExifOrientation(TestFixtures.Load("baseline-420.jpg"), 6);
        var full = decoder.DecodeToBmp(jpeg, new TurboJpegDecodeOptions { AutoOrient = true });
        var cropped = decoder.DecodeToBmp(jpeg, new TurboJpegDecodeOptions
        {
            AutoOrient = true,
            Crop = new TurboJpegCrop(0, 0, 16, 8)
        });

        Assert.Equal((8, 16), (BmpTestHelpers.Width(cropped), BmpTestHelpers.Height(cropped)));
        Assert.Equal(BmpTestHelpers.Pixel(full, 18, 10), BmpTestHelpers.Pixel(cropped, 3, 10));
        Assert.Equal(BmpTestHelpers.Pixel(full, 21, 14), BmpTestHelpers.Pixel(cropped, 6, 14));
    }

    private static void AssertColor(
        byte[] bmp, int x, int y, byte r, byte g, byte b, int tolerance = 24)
    {
        var (actualB, actualG, actualR, actualA) = BmpTestHelpers.Pixel(bmp, x, y);

        Assert.Equal(255, actualA);
        Assert.InRange((int)actualR, r - tolerance, r + tolerance);
        Assert.InRange((int)actualG, g - tolerance, g + tolerance);
        Assert.InRange((int)actualB, b - tolerance, b + tolerance);
    }
}
