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
}
