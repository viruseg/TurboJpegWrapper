using TurboJpeg.Wrapper.Decoder;
using TurboJpeg.Wrapper.Decoder.Internal.Native;

namespace TurboJpeg.Wrapper.Decoder.Tests;

public sealed class ScalingFactorTests
{
    [Fact]
    public void GetSupportedScalingFactors_ContainsDownscalesAndUpscales()
    {
        var factors = TurboJpeg.GetSupportedScalingFactors().ToArray();

        Assert.NotEmpty(factors);
        Assert.Contains(factors, f => f == TurboJpegScalingFactor.One);
        Assert.Contains(factors, f => f == new TurboJpegScalingFactor(1, 2));
        Assert.Contains(factors, f => f == new TurboJpegScalingFactor(2, 1));
        Assert.Contains(factors, f => f == new TurboJpegScalingFactor(9, 8));
        Assert.All(factors, f => Assert.True(f.Numerator > 0 && f.Denominator > 0));
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(9, 8)]
    public void DecodeToBmp_UpscalingFactor_ProducesCeiledDimensions(int numerator, int denominator)
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = TestFixtures.Load("baseline-444.jpg");

        var bmp = decoder.DecodeToBmp(
            jpeg,
            new TurboJpegDecodeOptions
            {
                ScalingFactor = new TurboJpegScalingFactor(numerator, denominator)
            });

        Assert.Equal(
            (TestFixtures.Width * numerator + denominator - 1) / denominator,
            BmpTestHelpers.Width(bmp));
        Assert.Equal(
            (TestFixtures.Height * numerator + denominator - 1) / denominator,
            BmpTestHelpers.Height(bmp));
        Assert.Equal(
            BmpTestHelpers.HeaderSize + BmpTestHelpers.Width(bmp) * BmpTestHelpers.Height(bmp) * 4,
            bmp.Length);
    }

    [Fact]
    public void GetSupportedScalingFactors_IsCached()
    {
        var first = TurboJpeg.GetSupportedScalingFactors().ToArray();
        var second = TurboJpeg.GetSupportedScalingFactors().ToArray();

        Assert.Equal(first, second);
    }

    [Fact]
    public void GetNativeScalingFactors_ReturnsCachedInstance()
    {
        var first = TurboJpegNative.GetSupportedScalingFactors();
        var second = TurboJpegNative.GetSupportedScalingFactors();

        Assert.Same(first, second);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void Constructor_NonPositiveValues_ThrowsArgumentOutOfRangeException(
        int numerator, int denominator)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TurboJpegScalingFactor(numerator, denominator));
    }

    [Fact]
    public void Value_ReturnsRatio()
    {
        Assert.Equal(0.5, new TurboJpegScalingFactor(1, 2).Value);
    }

    [Fact]
    public void Deconstruct_ReturnsComponents()
    {
        var (numerator, denominator) = new TurboJpegScalingFactor(3, 4);

        Assert.Equal(3, numerator);
        Assert.Equal(4, denominator);
    }

    [Fact]
    public void With_ValidComponent_PreservesOtherComponent()
    {
        var factor = new TurboJpegScalingFactor(1, 2) with { Numerator = 3 };

        Assert.Equal(3, factor.Numerator);
        Assert.Equal(2, factor.Denominator);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void With_NonPositiveNumerator_ThrowsArgumentOutOfRangeException(int numerator)
    {
        var factor = new TurboJpegScalingFactor(1, 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = factor with { Numerator = numerator });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void With_NonPositiveDenominator_ThrowsArgumentOutOfRangeException(int denominator)
    {
        var factor = new TurboJpegScalingFactor(1, 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = factor with { Denominator = denominator });
    }

    [Fact]
    public void ObjectInitializer_ValidComponents_CreatesFactor()
    {
        var factor = new TurboJpegScalingFactor { Numerator = 1, Denominator = 2 };

        Assert.Equal(0.5, factor.Value);
    }

    [Fact]
    public void Constructor_NamedArguments_CreatesFactor()
    {
        var factor = new TurboJpegScalingFactor(Numerator: 3, Denominator: 4);

        Assert.Equal(3, factor.Numerator);
        Assert.Equal(4, factor.Denominator);
    }
}
