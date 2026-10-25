using TurboJpegWrapper;

namespace TurboJpegWrapper.Tests;

public sealed class DecodeOptionsTests
{
    [Theory]
    [InlineData(1, 2, 18, 12)]
    [InlineData(3, 4, 27, 18)]
    [InlineData(1, 8, 5, 3)]
    public void DecodeToBmp_Scaling_UsesCeilDimensions(
        int num, int den, int expectedWidth, int expectedHeight)
    {
        using var decoder = new TurboJpegDecoder();
        var options = new TurboJpegDecodeOptions(
            scalingFactor: new TurboJpegScalingFactor(num, den));

        var bmp = decoder.DecodeToBmp(TestFixtures.Load("baseline-444.jpg"), options);

        Assert.Equal(expectedWidth, BmpTestHelpers.Width(bmp));
        Assert.Equal(expectedHeight, BmpTestHelpers.Height(bmp));
    }

    [Fact]
    public void DecodeToBmp_HalfScale_WritesExactSizeAndContent()
    {
        using var decoder = new TurboJpegDecoder();
        var options = new TurboJpegDecodeOptions(
            scalingFactor: new TurboJpegScalingFactor(1, 2));

        var bmp = decoder.DecodeToBmp(TestFixtures.Load("baseline-444.jpg"), options);

        Assert.Equal(BmpTestHelpers.HeaderSize + 18 * 12 * 4, bmp.Length);

        var (b, g, r, a) = BmpTestHelpers.Pixel(bmp, 5, 1);
        Assert.Equal(255, a);
        Assert.InRange((int)r, 231, 255);
        Assert.InRange((int)g, 0, 24);
        Assert.InRange((int)b, 0, 24);
    }

    [Fact]
    public void DecodeToBmp_UpscaleFactor_ThrowsArgumentException()
    {
        using var decoder = new TurboJpegDecoder();
        var options = new TurboJpegDecodeOptions(
            scalingFactor: new TurboJpegScalingFactor(2, 1));

        Assert.Throws<ArgumentException>(
            () => decoder.DecodeToBmp(TestFixtures.Load("baseline-444.jpg"), options));
    }

    [Fact]
    public void DecodeToBmp_UnsupportedFactor_ThrowsArgumentException()
    {
        using var decoder = new TurboJpegDecoder();
        var options = new TurboJpegDecodeOptions(
            scalingFactor: new TurboJpegScalingFactor(13, 17));

        Assert.Throws<ArgumentException>(
            () => decoder.DecodeToBmp(TestFixtures.Load("baseline-444.jpg"), options));
    }

    [Fact]
    public void DecodeToBmp_LosslessWithScaling_ThrowsArgumentException()
    {
        using var decoder = new TurboJpegDecoder();
        var options = new TurboJpegDecodeOptions(
            scalingFactor: new TurboJpegScalingFactor(1, 2));

        Assert.Throws<ArgumentException>(
            () => decoder.DecodeToBmp(TestFixtures.Load("lossless.jpg"), options));
    }

    [Fact]
    public void DecodeToBmp_MaxPixelsExceeded_ThrowsImageTooLarge()
    {
        using var decoder = new TurboJpegDecoder();
        var options = new TurboJpegDecodeOptions(maxPixels: 100);

        Assert.Throws<TurboJpegImageTooLargeException>(
            () => decoder.DecodeToBmp(TestFixtures.Load("baseline-444.jpg"), options));
    }

    [Fact]
    public void DecodeToBmp_ScanLimitExceeded_ThrowsTurboJpegException()
    {
        using var decoder = new TurboJpegDecoder();
        var options = new TurboJpegDecodeOptions(scanLimit: 1);

        Assert.Throws<TurboJpegException>(
            () => decoder.DecodeToBmp(TestFixtures.Load("progressive.jpg"), options));
    }

    [Fact]
    public void DecodeToBmp_MaxMemoryExceeded_ThrowsTurboJpegException()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = TestFixtures.Load("progressive.jpg");
        JpegPatcher.SetDimensions(jpeg, 1024, 1024);

        var exception = Assert.Throws<TurboJpegException>(
            () => decoder.DecodeToBmp(
                jpeg,
                new TurboJpegDecodeOptions(maxMemoryMegabytes: 1)));

        Assert.Contains("memory limit", exception.Message, StringComparison.OrdinalIgnoreCase);

        var recovered = decoder.DecodeToBmp(TestFixtures.Load("baseline-444.jpg"));
        Assert.Equal(35, BmpTestHelpers.Width(recovered));
    }

    [Fact]
    public void DecodeToBmp_AfterNativeFailure_CanDecodeAgain()
    {
        using var decoder = new TurboJpegDecoder();

        Assert.Throws<TurboJpegException>(() => decoder.DecodeToBmp(
            TestFixtures.Load("progressive.jpg"),
            new TurboJpegDecodeOptions(scanLimit: 1)));

        var bmp = decoder.DecodeToBmp(TestFixtures.Load("baseline-444.jpg"));

        Assert.Equal(35, BmpTestHelpers.Width(bmp));
        Assert.Equal(23, BmpTestHelpers.Height(bmp));
    }

    [Fact]
    public void DecodeToBmp_FastFlags_StillProduceValidBmp()
    {
        using var decoder = new TurboJpegDecoder();
        var options = new TurboJpegDecodeOptions(fastDct: true, fastUpsampling: true);

        var bmp = decoder.DecodeToBmp(TestFixtures.Load("baseline-420.jpg"), options);

        Assert.Equal(35, BmpTestHelpers.Width(bmp));
        Assert.Equal(23, BmpTestHelpers.Height(bmp));
    }

    [Fact]
    public void DecodeToBmp_RepeatedCalls_DoNotLeakOptions()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = TestFixtures.Load("baseline-444.jpg");

        var full = decoder.DecodeToBmp(jpeg);
        var half = decoder.DecodeToBmp(jpeg, new TurboJpegDecodeOptions(
            scalingFactor: new TurboJpegScalingFactor(1, 2)));
        var fullAgain = decoder.DecodeToBmp(jpeg);

        Assert.Equal(35, BmpTestHelpers.Width(full));
        Assert.Equal(18, BmpTestHelpers.Width(half));
        Assert.Equal(35, BmpTestHelpers.Width(fullAgain));

        Assert.Throws<TurboJpegImageTooLargeException>(() => decoder.DecodeToBmp(
            jpeg, new TurboJpegDecodeOptions(maxPixels: 100)));
        Assert.Equal(35, BmpTestHelpers.Width(decoder.DecodeToBmp(jpeg)));
    }

    [Fact]
    public void DecodeToBmp_HugeClaimedDimensions_ThrowsImageTooLarge()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = TestFixtures.Load("baseline-444.jpg");
        JpegPatcher.SetDimensions(jpeg, 65500, 65500);
        var options = new TurboJpegDecodeOptions(maxPixels: 0);

        var exception = Assert.Throws<TurboJpegImageTooLargeException>(
            () => decoder.DecodeToBmp(jpeg, options));
        Assert.Contains("maximum array length", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DecodeToBmp_NullOptions_ThrowsArgumentNullException()
    {
        using var decoder = new TurboJpegDecoder();

        Assert.Throws<ArgumentNullException>(
            () => decoder.DecodeToBmp(TestFixtures.Load("baseline-444.jpg"), null!));
    }

    [Fact]
    public void DecodeToBmp_ConstructorDefaultOptions_AreUsed()
    {
        using var decoder = new TurboJpegDecoder(
            new TurboJpegDecodeOptions(scalingFactor: new TurboJpegScalingFactor(1, 2)));

        var bmp = decoder.DecodeToBmp(TestFixtures.Load("baseline-444.jpg"));

        Assert.Equal(18, BmpTestHelpers.Width(bmp));
        Assert.Equal(12, BmpTestHelpers.Height(bmp));
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public void Constructor_NegativeLimit_ThrowsArgumentOutOfRangeException(
        int maxPixels, int maxMemoryMegabytes, int scanLimit)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TurboJpegDecodeOptions(
            maxPixels: maxPixels,
            maxMemoryMegabytes: maxMemoryMegabytes,
            scanLimit: scanLimit));
    }

    [Fact]
    public void Constructor_DefaultScalingFactor_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TurboJpegDecodeOptions(scalingFactor: default(TurboJpegScalingFactor)));
    }

    [Fact]
    public void Constructor_BmpBitDepth_DefaultsTo32()
    {
        Assert.Equal(TurboJpegBmpBitDepth.Bpp32, new TurboJpegDecodeOptions().BmpBitDepth);
        Assert.Equal(TurboJpegBmpBitDepth.Bpp32, TurboJpegDecodeOptions.Default.BmpBitDepth);
    }

    [Theory]
    [InlineData(TurboJpegBmpBitDepth.Bpp8)]
    [InlineData(TurboJpegBmpBitDepth.Bpp24)]
    [InlineData(TurboJpegBmpBitDepth.Bpp32)]
    public void Constructor_BmpBitDepth_IsExposed(TurboJpegBmpBitDepth bitDepth)
    {
        Assert.Equal(bitDepth, new TurboJpegDecodeOptions(bmpBitDepth: bitDepth).BmpBitDepth);
    }

    [Fact]
    public void Constructor_UndefinedBmpBitDepth_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TurboJpegDecodeOptions(bmpBitDepth: (TurboJpegBmpBitDepth)12));
    }
}
