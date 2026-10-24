using TurboJpegWrapper;
using TurboJpegWrapper.Internal.Native;

namespace TurboJpegWrapper.Tests;

public sealed class ScalingFactorTests
{
    [Fact]
    public void GetSupportedScalingFactors_ContainsOneAndDownscalesOnly()
    {
        var factors = TurboJpeg.GetSupportedScalingFactors();

        Assert.NotEmpty(factors.ToArray());
        Assert.Contains(factors.ToArray(), f => f == TurboJpegScalingFactor.One);
        Assert.All(factors.ToArray(), f => Assert.True(f.Numerator <= f.Denominator));
        Assert.Contains(factors.ToArray(), f => f == new TurboJpegScalingFactor(1, 2));
        Assert.DoesNotContain(factors.ToArray(), f => f == new TurboJpegScalingFactor(2, 1));
    }

    [Fact]
    public void GetSupportedScalingFactors_IsCached()
    {
        var first = TurboJpeg.GetSupportedScalingFactors().ToArray();
        var second = TurboJpeg.GetSupportedScalingFactors().ToArray();

        Assert.Equal(first, second);
    }

    [Fact]
    public void GetFilteredScalingFactors_ReturnsCachedInstance()
    {
        var first = TurboJpegNative.GetFilteredScalingFactors();
        var second = TurboJpegNative.GetFilteredScalingFactors();

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
