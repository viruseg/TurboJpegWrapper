using TurboJpegWrapper;

namespace TurboJpegWrapper.Encoder.Tests;

public sealed class EncodeOptionsTests
{
    [Fact]
    public void Default_MatchesDesign()
    {
        var options = TurboJpegEncodeOptions.Default;

        Assert.Equal(90, options.Quality);
        Assert.Equal(TurboJpegSubsampling.Yuv420, options.Subsampling);
        Assert.Equal(TurboJpegColorspace.Default, options.Colorspace);
        Assert.False(options.FastDct);
        Assert.False(options.OptimizeCoding);
        Assert.False(options.Progressive);
        Assert.False(options.ArithmeticCoding);
        Assert.False(options.Lossless);
        Assert.Equal(1, options.LosslessPredictor);
        Assert.Equal(0, options.LosslessPointTransform);
        Assert.Equal(0, options.RestartBlocks);
        Assert.Equal(0, options.RestartRows);
        Assert.Equal(0, options.MaxMemoryMegabytes);
        Assert.False(options.StopOnWarning);
        Assert.Null(options.Density);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(101)]
    public void Quality_OutOfRange_Throws(int quality) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurboJpegEncodeOptions { Quality = quality });

    [Theory]
    [InlineData(1)]
    [InlineData(90)]
    [InlineData(100)]
    public void Quality_InRange_IsAccepted(int quality) =>
        Assert.Equal(quality, new TurboJpegEncodeOptions { Quality = quality }.Quality);

    [Fact]
    public void Quality_OutOfRange_ThrowsEvenWhenLossless() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurboJpegEncodeOptions { Lossless = true, Quality = 0 });

    [Fact]
    public void Subsampling_Unknown_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurboJpegEncodeOptions { Subsampling = TurboJpegSubsampling.Unknown });

    [Fact]
    public void Subsampling_Undefined_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurboJpegEncodeOptions { Subsampling = (TurboJpegSubsampling)9 });

    [Theory]
    [InlineData(TurboJpegSubsampling.Yuv444)]
    [InlineData(TurboJpegSubsampling.Yuv422)]
    [InlineData(TurboJpegSubsampling.Yuv420)]
    [InlineData(TurboJpegSubsampling.Grayscale)]
    [InlineData(TurboJpegSubsampling.Yuv440)]
    [InlineData(TurboJpegSubsampling.Yuv411)]
    [InlineData(TurboJpegSubsampling.Yuv441)]
    [InlineData(TurboJpegSubsampling.Yuv410)]
    [InlineData(TurboJpegSubsampling.Yuv24)]
    public void Subsampling_Defined_IsAccepted(TurboJpegSubsampling subsampling) =>
        Assert.Equal(subsampling, new TurboJpegEncodeOptions { Subsampling = subsampling }.Subsampling);

    [Theory]
    [InlineData(-2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Colorspace_Unsupported_Throws(int colorspace)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurboJpegEncodeOptions { Colorspace = (TurboJpegColorspace)colorspace });

        Assert.Contains(
            "Default, Rgb, YCbCr, or Grayscale",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(TurboJpegColorspace.Default)]
    [InlineData(TurboJpegColorspace.Rgb)]
    [InlineData(TurboJpegColorspace.YCbCr)]
    [InlineData(TurboJpegColorspace.Grayscale)]
    public void Colorspace_Defined_IsAccepted(TurboJpegColorspace colorspace) =>
        Assert.Equal(colorspace, new TurboJpegEncodeOptions { Colorspace = colorspace }.Colorspace);

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void LosslessPredictor_OutOfRange_Throws(int predictor) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurboJpegEncodeOptions { LosslessPredictor = predictor });

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    public void LosslessPredictor_InRange_IsAccepted(int predictor) =>
        Assert.Equal(
            predictor,
            new TurboJpegEncodeOptions { LosslessPredictor = predictor }.LosslessPredictor);

    [Theory]
    [InlineData(-1)]
    [InlineData(8)]
    public void LosslessPointTransform_OutOfRange_Throws(int pointTransform) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurboJpegEncodeOptions { LosslessPointTransform = pointTransform });

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void LosslessPointTransform_InRange_IsAccepted(int pointTransform) =>
        Assert.Equal(
            pointTransform,
            new TurboJpegEncodeOptions { LosslessPointTransform = pointTransform }.LosslessPointTransform);

    [Fact]
    public void RestartBlocks_Negative_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurboJpegEncodeOptions { RestartBlocks = -1 });

    [Fact]
    public void RestartRows_Negative_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurboJpegEncodeOptions { RestartRows = -1 });

    [Fact]
    public void RestartBlocks_SixteenBitMaximum_IsAccepted() =>
        Assert.Equal(65535, new TurboJpegEncodeOptions { RestartBlocks = 65535 }.RestartBlocks);

    [Fact]
    public void RestartRows_SixteenBitMaximum_IsAccepted() =>
        Assert.Equal(65535, new TurboJpegEncodeOptions { RestartRows = 65535 }.RestartRows);

    [Fact]
    public void RestartBlocks_AboveSixteenBitMaximum_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurboJpegEncodeOptions { RestartBlocks = 65536 });

    [Fact]
    public void RestartRows_AboveSixteenBitMaximum_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurboJpegEncodeOptions { RestartRows = 65536 });

    [Fact]
    public void MaxMemoryMegabytes_Negative_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurboJpegEncodeOptions { MaxMemoryMegabytes = -1 });

    [Fact]
    public void RestartMarkers_PositiveValues_AreAccepted()
    {
        var options = new TurboJpegEncodeOptions { RestartBlocks = 2, RestartRows = 3 };

        Assert.Equal(2, options.RestartBlocks);
        Assert.Equal(3, options.RestartRows);
    }

    [Fact]
    public void LosslessWithRestartBlocks_IsAcceptedByOptions()
    {
        var options = new TurboJpegEncodeOptions { Lossless = true, RestartBlocks = 1 };

        Assert.True(options.Lossless);
        Assert.Equal(1, options.RestartBlocks);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void Density_NonPositiveDimension_Throws(int x, int y) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurboJpegDensity(x, y, TurboJpegDensityUnits.PixelsPerInch));

    [Fact]
    public void Density_ZeroInitX_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new TurboJpegDensity { X = 0 });

    [Fact]
    public void Density_ZeroInitY_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new TurboJpegDensity { Y = 0 });

    [Fact]
    public void Density_SixteenBitMaximum_IsAccepted()
    {
        var density = new TurboJpegDensity(65535, 65535, TurboJpegDensityUnits.PixelsPerCentimeter);

        Assert.Equal(65535, density.X);
        Assert.Equal(65535, density.Y);
    }

    [Theory]
    [InlineData(65536, 1)]
    [InlineData(1, 65536)]
    public void Density_DimensionAboveSixteenBitMaximum_Throws(int x, int y) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurboJpegDensity(x, y, TurboJpegDensityUnits.PixelsPerInch));

    [Fact]
    public void Density_XInitAboveSixteenBitMaximum_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new TurboJpegDensity { X = 65536 });

    [Fact]
    public void Density_YInitAboveSixteenBitMaximum_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new TurboJpegDensity { Y = 65536 });

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void Density_UndefinedUnits_Throws(int units) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurboJpegDensity(72, 72, (TurboJpegDensityUnits)units));

    [Fact]
    public void Density_Valid_IsAccepted()
    {
        var density = new TurboJpegDensity(300, 300, TurboJpegDensityUnits.PixelsPerInch);

        var options = new TurboJpegEncodeOptions { Density = density };

        Assert.Equal(density, options.Density);
        Assert.Equal(300, options.Density!.Value.X);
        Assert.Equal(300, options.Density!.Value.Y);
        Assert.Equal(TurboJpegDensityUnits.PixelsPerInch, options.Density!.Value.Units);
    }

    [Fact]
    public void Density_Deconstruct_ReturnsComponents()
    {
        var (x, y, units) =
            new TurboJpegDensity(150, 75, TurboJpegDensityUnits.PixelsPerCentimeter);

        Assert.Equal(150, x);
        Assert.Equal(75, y);
        Assert.Equal(TurboJpegDensityUnits.PixelsPerCentimeter, units);
    }

    [Fact]
    public void Density_DefaultStruct_ThrowsWhenAssigned() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurboJpegEncodeOptions { Density = default(TurboJpegDensity) });

    [Fact]
    public void Density_PartiallyInitializedStruct_ThrowsWhenAssigned() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurboJpegEncodeOptions { Density = new TurboJpegDensity { X = 5 } });

    [Fact]
    public void With_PreservesValidatedValues()
    {
        var options = new TurboJpegEncodeOptions { Quality = 75 };

        var updated = options with { Progressive = true };

        Assert.Equal(75, updated.Quality);
        Assert.True(updated.Progressive);
        Assert.Equal(75, options.Quality);
        Assert.False(options.Progressive);
        Assert.Equal(90, TurboJpegEncodeOptions.Default.Quality);
    }
}
