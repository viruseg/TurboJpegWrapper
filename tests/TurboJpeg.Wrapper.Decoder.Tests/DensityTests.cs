using TurboJpegWrapper;
using TurboJpegWrapper.Internal.Decoder.Bmp;

namespace TurboJpegWrapper.Decoder.Tests;

public sealed class DensityTests
{
    [Theory]
    [InlineData((byte)1, (ushort)300, (ushort)200, 11811, 7874)]
    [InlineData((byte)2, (ushort)100, (ushort)50, 10000, 5000)]
    [InlineData((byte)0, (ushort)300, (ushort)200, 2835, 2835)]
    public void DecodeToBmp_WritesJfifDensity(
        byte units, ushort xDensity, ushort yDensity, int expectedX, int expectedY)
    {
        var jpeg = TestFixtures.Load("baseline-444.jpg");
        JpegPatcher.SetJfifDensity(jpeg, xDensity, yDensity, units);
        using var decoder = new TurboJpegDecoder();

        var info = decoder.GetImageInfo(jpeg);
        var bmp = decoder.DecodeToBmp(jpeg);

        Assert.Equal((TurboJpegDensityUnits)units, info.DensityUnits);
        Assert.Equal(xDensity, info.DensityX);
        Assert.Equal(yDensity, info.DensityY);
        Assert.Equal(expectedX, BmpTestHelpers.PixelsPerMeterX(bmp));
        Assert.Equal(expectedY, BmpTestHelpers.PixelsPerMeterY(bmp));
    }

    [Theory]
    [InlineData(TurboJpegDensityUnits.PixelsPerInch, 300, 11811)]
    [InlineData(TurboJpegDensityUnits.PixelsPerCentimeter, 100, 10000)]
    [InlineData(TurboJpegDensityUnits.Unknown, 300, BmpHeaderWriter.DefaultPixelsPerMeter)]
    [InlineData(TurboJpegDensityUnits.PixelsPerInch, 0, BmpHeaderWriter.DefaultPixelsPerMeter)]
    public void ToPixelsPerMeter_ConvertsKnownUnits(
        TurboJpegDensityUnits units, int density, int expected)
    {
        Assert.Equal(expected, BmpHeaderWriter.ToPixelsPerMeter(density, units));
    }
}
