using TurboJpegWrapper.Decoder;

namespace TurboJpegWrapper.Decoder.Tests;

public sealed class CodingCoverageTests
{
    [Theory]
    [InlineData("arithmetic.jpg", TurboJpegBmpBitDepth.Bpp8)]
    [InlineData("arithmetic.jpg", TurboJpegBmpBitDepth.Bpp24)]
    [InlineData("arithmetic.jpg", TurboJpegBmpBitDepth.Bpp32)]
    [InlineData("s-unknown.jpg", TurboJpegBmpBitDepth.Bpp8)]
    [InlineData("s-unknown.jpg", TurboJpegBmpBitDepth.Bpp24)]
    [InlineData("s-unknown.jpg", TurboJpegBmpBitDepth.Bpp32)]
    public void DecodeToBmp_SupportedEntropyAndSubsampling_ProducesExpectedBands(
        string fixture, TurboJpegBmpBitDepth bitDepth)
    {
        var jpeg = TestFixtures.Load(fixture);
        var options = new TurboJpegDecodeOptions { BmpBitDepth = bitDepth };
        using var decoder = new TurboJpegDecoder();

        var size = decoder.GetBmpSize(jpeg, options);
        var bmp = decoder.DecodeToBmp(jpeg, options);

        Assert.Equal(size, bmp.Length);
        Assert.Equal(
            (TestFixtures.Width, TestFixtures.Height),
            (BmpTestHelpers.Width(bmp), BmpTestHelpers.Height(bmp)));
        Assert.Equal((int)bitDepth, BmpTestHelpers.BitsPerPixel(bmp));

        if (bitDepth == TurboJpegBmpBitDepth.Bpp8)
        {
            AssertLuma(bmp, 5, 1, 77);
            AssertLuma(bmp, 5, 4, 149);
            AssertLuma(bmp, 5, 7, 29);
            AssertLuma(bmp, 5, 10, 255);
            AssertLuma(bmp, 5, 13, 0);
            return;
        }

        if (fixture == "arithmetic.jpg")
        {
            // The arithmetic fixture uses darker bands than s-unknown.jpg.
            AssertColor(bmp, 5, 1, 0, 23, 212);
            AssertColor(bmp, 5, 4, 8, 233, 41);
            AssertColor(bmp, 5, 7, 226, 4, 2);
            AssertColor(bmp, 5, 10, 255, 251, 252);
            AssertColor(bmp, 5, 13, 2, 0, 0);
            return;
        }

        AssertColor(bmp, 5, 1, 0, 0, 255);
        AssertColor(bmp, 5, 4, 0, 255, 0);
        AssertColor(bmp, 5, 7, 255, 0, 0);
        AssertColor(bmp, 5, 10, 255, 255, 255);
        AssertColor(bmp, 5, 13, 0, 0, 0);
    }    [Theory]
    [InlineData("arithmetic.jpg")]
    [InlineData("s-unknown.jpg")]
    public void DecodeToBmp_SupportedEntropyAndSubsampling_SupportsScalingAndOrientation(
        string fixture)
    {
        var jpeg = TestFixtures.Load(fixture);
        using var decoder = new TurboJpegDecoder();

        var half = decoder.DecodeToBmp(jpeg, new TurboJpegDecodeOptions
        {
            ScalingFactor = new TurboJpegScalingFactor(1, 2),
            BmpBitDepth = TurboJpegBmpBitDepth.Bpp24,
        });
        Assert.Equal((18, 12), (BmpTestHelpers.Width(half), BmpTestHelpers.Height(half)));

        var oriented = decoder.DecodeToBmp(
            JpegPatcher.WithExifOrientation(jpeg, 6),
            new TurboJpegDecodeOptions
            {
                AutoOrient = true,
                BmpBitDepth = TurboJpegBmpBitDepth.Bpp24,
            });
        Assert.Equal(
            (TestFixtures.Height, TestFixtures.Width),
            (BmpTestHelpers.Width(oriented), BmpTestHelpers.Height(oriented)));
    }

    private static void AssertLuma(byte[] bmp, int x, int y, int expected)
    {
        const int tolerance = 24;
        var (b, g, r, a) = BmpTestHelpers.Pixel(bmp, x, y);

        Assert.Equal(255, a);
        Assert.Equal(r, g);
        Assert.Equal(g, b);
        Assert.InRange((int)b, Math.Max(0, expected - tolerance), Math.Min(255, expected + tolerance));
    }

    private static void AssertColor(
        byte[] bmp, int x, int y, byte expectedB, byte expectedG, byte expectedR)
    {
        const int tolerance = 24;
        var (b, g, r, a) = BmpTestHelpers.Pixel(bmp, x, y);

        Assert.Equal(255, a);
        Assert.InRange((int)b, Math.Max(0, expectedB - tolerance), Math.Min(255, expectedB + tolerance));
        Assert.InRange((int)g, Math.Max(0, expectedG - tolerance), Math.Min(255, expectedG + tolerance));
        Assert.InRange((int)r, Math.Max(0, expectedR - tolerance), Math.Min(255, expectedR + tolerance));
    }
}
