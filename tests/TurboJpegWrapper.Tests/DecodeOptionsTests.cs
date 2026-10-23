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

        Assert.Throws<TurboJpegImageTooLargeException>(() => decoder.DecodeToBmp(jpeg, options));
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

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void Constructor_NonPositiveScalingFactor_ThrowsArgumentOutOfRangeException(
        int numerator, int denominator)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TurboJpegDecodeOptions(
            scalingFactor: new TurboJpegScalingFactor(numerator, denominator)));
    }
}
