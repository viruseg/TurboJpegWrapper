using TurboJpegWrapper.Decoder;

namespace TurboJpegWrapper.Decoder.Tests;

public sealed class WarningPolicyTests
{
    [Fact]
    public void DecodeToBmp_Truncated_DefaultStopOnWarning_ThrowsNonFatal()
    {
        var ex = Assert.Throws<TurboJpegException>(() =>
            new TurboJpegDecoder().DecodeToBmp(TestFixtures.Load("truncated.jpg")));
        Assert.False(ex.IsFatal);
    }

    [Fact]
    public void DecodeToBmp_Truncated_Tolerant_ReturnsBmpDeterministically()
    {
        var jpeg = TestFixtures.Load("truncated.jpg");
        var options = new TurboJpegDecodeOptions { StopOnWarning = false };
        var first = new byte[16 * 1024 * 1024];
        var second = new byte[16 * 1024 * 1024];
        first.AsSpan().Fill(0xAA);
        second.AsSpan().Fill(0x55);

        int size1 = new TurboJpegDecoder().DecodeToBmp(jpeg, options, first);
        int size2 = new TurboJpegDecoder().DecodeToBmp(jpeg, options, second);

        Assert.Equal(size1, size2);
        Assert.True(first.AsSpan(0, size1).SequenceEqual(second.AsSpan(0, size2)));
        Assert.NotEqual(0, BmpTestHelpers.Width(first));
    }

    [Fact]
    public void DecodeToBmp_InvalidBytes_Tolerant_ThrowsFatal()
    {
        using var decoder = new TurboJpegDecoder();
        var tolerant = new TurboJpegDecodeOptions { StopOnWarning = false };

        var ex = Assert.Throws<TurboJpegException>(() =>
            decoder.DecodeToBmp(new byte[] { 1, 2, 3, 4 }, tolerant));

        Assert.True(ex.IsFatal);
        Assert.Contains("Not a JPEG file", ex.Message);
    }

    [Fact]
    public void DecodeToBmp_MaxMemoryExceeded_Tolerant_StillThrowsFatal()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = TestFixtures.Load("progressive.jpg");
        JpegPatcher.SetDimensions(jpeg, 1024, 1024);
        var tolerant = new TurboJpegDecodeOptions
        {
            StopOnWarning = false,
            MaxMemoryMegabytes = 1,
        };

        var ex = Assert.Throws<TurboJpegException>(() => decoder.DecodeToBmp(jpeg, tolerant));

        Assert.True(ex.IsFatal);
        Assert.Contains("memory limit", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetImageInfo_HeaderWarning_StaysStrict()
    {
        Assert.Throws<TurboJpegException>(() =>
            new TurboJpegDecoder().GetImageInfo(TestFixtures.Load("extraneous.jpg")));
    }

    [Fact]
    public void GetImageInfo_ProgressiveAndArithmetic_AreReported()
    {
        Assert.True(new TurboJpegDecoder().GetImageInfo(TestFixtures.Load("progressive.jpg")).IsProgressive);
        Assert.True(new TurboJpegDecoder().GetImageInfo(TestFixtures.Load("arithmetic.jpg")).UsesArithmeticCoding);
        Assert.False(new TurboJpegDecoder().GetImageInfo(TestFixtures.Load("baseline-420.jpg")).IsProgressive);
    }

    [Fact]
    public void DecodeToBmp_ExtraneousHeader_Tolerant_ReturnsValidBmp()
    {
        var bmp = new TurboJpegDecoder().DecodeToBmp(
            TestFixtures.Load("extraneous.jpg"),
            new TurboJpegDecodeOptions { StopOnWarning = false });

        Assert.Equal((35, 23), (BmpTestHelpers.Width(bmp), BmpTestHelpers.Height(bmp)));
        Assert.Equal(32, BmpTestHelpers.BitsPerPixel(bmp));

        var (redB, redG, redR, redA) = BmpTestHelpers.Pixel(bmp, 5, 1);
        Assert.Equal(255, redA);
        Assert.True(redR > 200 && redG < 40 && redB < 40, $"Expected red, got ({redR},{redG},{redB}).");

        var (whiteB, whiteG, whiteR, _) = BmpTestHelpers.Pixel(bmp, 5, 10);
        Assert.True(whiteR > 200 && whiteG > 200 && whiteB > 200, "Expected white.");
    }

    [Fact]
    public void GetBmpSize_ExtraneousHeader_Tolerant_ReturnsSize()
    {
        using var decoder = new TurboJpegDecoder();
        var tolerant = new TurboJpegDecodeOptions { StopOnWarning = false };

        var size = decoder.GetBmpSize(TestFixtures.Load("extraneous.jpg"), tolerant);

        Assert.Equal(decoder.GetBmpSize(TestFixtures.Load("baseline-420.jpg")), size);
    }
}
