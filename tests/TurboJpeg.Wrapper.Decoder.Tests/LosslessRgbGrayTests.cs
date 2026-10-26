using TurboJpegWrapper.Decoder;

namespace TurboJpegWrapper.Decoder.Tests;

public sealed class LosslessRgbGrayTests
{
    private static readonly int[] GrayBandRows = [1, 5, 9, 13, 17, 21];

    private static byte[] Decode(string fixture, TurboJpegDecodeOptions options) =>
        new TurboJpegDecoder().DecodeToBmp(TestFixtures.Load(fixture), options);

    private static void AssertGrayBands(
        byte[] bmp, int band0, int band1, int band2, int band3, int band4, int band5)
    {
        int[] values = [band0, band1, band2, band3, band4, band5];

        for (var i = 0; i < GrayBandRows.Length; i++)
        {
            var value = (byte)values[i];
            Assert.Equal(
                (value, value, value, (byte)255),
                BmpTestHelpers.Pixel(bmp, 5, GrayBandRows[i]));
        }
    }

    [Theory]
    [InlineData("lossless-p2.jpg")]
    [InlineData("lossless-p4.jpg")]
    [InlineData("lossless-p7.jpg")]
    [InlineData("lossless-p8.jpg")]
    public void DecodeToBmp_LosslessRgbToBpp8_UsesManagedLuma(string fixture)
    {
        var bmp = Decode(fixture, new TurboJpegDecodeOptions
        {
            BmpBitDepth = TurboJpegBmpBitDepth.Bpp8
        });

        Assert.Equal((77, 77, 77, 255), BmpTestHelpers.Pixel(bmp, 5, 1));
        Assert.Equal((149, 149, 149, 255), BmpTestHelpers.Pixel(bmp, 5, 4));
        Assert.Equal((29, 29, 29, 255), BmpTestHelpers.Pixel(bmp, 5, 7));
        Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 5, 10));
        Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 5, 13));
    }

    [Fact]
    public void DecodeToBmp_LosslessRgbToBpp8_WithAutoOrient_Works()
    {
        var jpeg = JpegPatcher.WithExifOrientation(TestFixtures.Load("lossless-p8.jpg"), 6);

        var bmp = new TurboJpegDecoder().DecodeToBmp(jpeg, new TurboJpegDecodeOptions
        {
            BmpBitDepth = TurboJpegBmpBitDepth.Bpp8,
            AutoOrient = true
        });

        Assert.Equal(TestFixtures.Height, BmpTestHelpers.Width(bmp));
        Assert.Equal(TestFixtures.Width, BmpTestHelpers.Height(bmp));
        Assert.Equal(8, BmpTestHelpers.BitsPerPixel(bmp));

        // Orientation 6 maps source (x, y) to output (height - 1 - y, x).
        Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 12, 5));
        Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 9, 5));
    }

    [Theory]
    [InlineData(TurboJpegBmpBitDepth.Bpp24)]
    [InlineData(TurboJpegBmpBitDepth.Bpp32)]
    public void DecodeToBmp_LosslessGray_WithAutoOrient_Works(TurboJpegBmpBitDepth bitDepth)
    {
        var jpeg = JpegPatcher.WithExifOrientation(TestFixtures.Load("gray-lossless-p8.jpg"), 6);

        var bmp = new TurboJpegDecoder().DecodeToBmp(jpeg, new TurboJpegDecodeOptions
        {
            BmpBitDepth = bitDepth,
            AutoOrient = true
        });

        Assert.Equal(TestFixtures.Height, BmpTestHelpers.Width(bmp));
        Assert.Equal(TestFixtures.Width, BmpTestHelpers.Height(bmp));
        Assert.Equal((int)bitDepth, BmpTestHelpers.BitsPerPixel(bmp));

        Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 1, 5));
        Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 21, 5));
    }

    [Theory]
    [InlineData("gray-lossless-p2.jpg", 0, 0, 85, 170, 170, 255)]
    [InlineData("gray-lossless-p4.jpg", 0, 34, 68, 136, 187, 255)]
    [InlineData("gray-lossless-p7.jpg", 0, 32, 64, 129, 191, 255)]
    [InlineData("gray-lossless-p8.jpg", 0, 32, 64, 128, 191, 255)]
    public void DecodeToBmp_LosslessGrayToBpp32_ExpandsChannels(
        string fixture, int band0, int band1, int band2, int band3, int band4, int band5)
    {
        var bmp = Decode(fixture, new TurboJpegDecodeOptions
        {
            BmpBitDepth = TurboJpegBmpBitDepth.Bpp32
        });

        Assert.Equal(32, BmpTestHelpers.BitsPerPixel(bmp));
        AssertGrayBands(bmp, band0, band1, band2, band3, band4, band5);
    }

    [Theory]
    [InlineData("gray-lossless-p2.jpg", 0, 0, 85, 170, 170, 255)]
    [InlineData("gray-lossless-p4.jpg", 0, 34, 68, 136, 187, 255)]
    [InlineData("gray-lossless-p7.jpg", 0, 32, 64, 129, 191, 255)]
    [InlineData("gray-lossless-p8.jpg", 0, 32, 64, 128, 191, 255)]
    public void DecodeToBmp_LosslessGrayToBpp24_ExpandsChannels(
        string fixture, int band0, int band1, int band2, int band3, int band4, int band5)
    {
        var bmp = Decode(fixture, new TurboJpegDecodeOptions
        {
            BmpBitDepth = TurboJpegBmpBitDepth.Bpp24
        });

        Assert.Equal(24, BmpTestHelpers.BitsPerPixel(bmp));
        AssertGrayBands(bmp, band0, band1, band2, band3, band4, band5);
    }

    [Theory]
    [InlineData("gray-lossless-p2.jpg", 0, 0, 85, 170, 170, 255)]
    [InlineData("gray-lossless-p4.jpg", 0, 34, 68, 136, 187, 255)]
    [InlineData("gray-lossless-p7.jpg", 0, 32, 64, 129, 191, 255)]
    [InlineData("gray-lossless-p8.jpg", 0, 32, 64, 128, 191, 255)]
    public void DecodeToBmp_LosslessGrayToBpp8_UsesNativeGray(
        string fixture, int band0, int band1, int band2, int band3, int band4, int band5)
    {
        var bmp = Decode(fixture, new TurboJpegDecodeOptions
        {
            BmpBitDepth = TurboJpegBmpBitDepth.Bpp8
        });

        Assert.Equal(8, BmpTestHelpers.BitsPerPixel(bmp));
        AssertGrayBands(bmp, band0, band1, band2, band3, band4, band5);
    }
}
