using TurboJpegWrapper.Decoder;

namespace TurboJpegWrapper.Decoder.Tests;

public sealed class LosslessRgbGrayTests
{
    private static byte[] Decode(string fixture, TurboJpegDecodeOptions options) =>
        new TurboJpegDecoder().DecodeToBmp(TestFixtures.Load(fixture), options);

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

    [Fact]
    public void DecodeToBmp_LosslessGrayToBpp32_ReplicatesChannels()
    {
        var bmp = Decode(
            "gray-lossless-p8.jpg",
            new TurboJpegDecodeOptions { BmpBitDepth = TurboJpegBmpBitDepth.Bpp32 });

        Assert.Equal((255, 255, 255, 255), BmpTestHelpers.Pixel(bmp, 5, 10));
        Assert.Equal((0, 0, 0, 255), BmpTestHelpers.Pixel(bmp, 5, 13));
    }
}
