using TurboJpegWrapper.Encoder;
using DecoderApi = TurboJpegWrapper.Decoder.TurboJpeg;

namespace TurboJpegWrapper.Encoder.Tests;

public sealed class StateIsolationTests
{
    [Fact]
    public void ReusedEncoder_EveryCallMatchesFreshInstance()
    {
        // No BMP density, so the final default encode must restore the explicit 1/1 fallback.
        var bmp = BmpBuilder.Create24(35, 29, Gradient);
        using var reused = new TurboJpegEncoder();

        var defaultOutput = AssertMatchesFresh(reused, bmp, TurboJpegEncodeOptions.Default);
        Assert.Equal(1, DecoderApi.GetImageInfo(defaultOutput).DensityX);

        var rgb444 = new TurboJpegEncodeOptions
        {
            Colorspace = TurboJpegColorspace.Rgb,
            Subsampling = TurboJpegSubsampling.Yuv444,
        };
        AssertMatchesFresh(reused, bmp, rgb444);

        var density300 = new TurboJpegEncodeOptions
        {
            Density = new TurboJpegDensity(300, 300, TurboJpegDensityUnits.PixelsPerInch),
        };
        var densityOutput = AssertMatchesFresh(reused, bmp, density300);
        Assert.Equal(300, DecoderApi.GetImageInfo(densityOutput).DensityX);

        AssertMatchesFresh(reused, bmp, new TurboJpegEncodeOptions { Progressive = true });
        AssertMatchesFresh(reused, bmp, new TurboJpegEncodeOptions { ArithmeticCoding = true });

        var losslessOutput = AssertMatchesFresh(
            reused, bmp, new TurboJpegEncodeOptions { Lossless = true });
        Assert.Equal(0xC3, JpegMarkers.FindSofMarker(losslessOutput));

        var finalOutput = AssertMatchesFresh(reused, bmp, TurboJpegEncodeOptions.Default);
        var finalInfo = DecoderApi.GetImageInfo(finalOutput);
        Assert.Equal(1, finalInfo.DensityX);
        Assert.Equal(1, finalInfo.DensityY);
        Assert.Equal((int)TurboJpegDensityUnits.Unknown, (int)finalInfo.DensityUnits);
    }

    [Fact]
    public void ReusedEncoder_DefaultAfterLossless_MatchesFreshInstance()
    {
        var bmp = BmpBuilder.Create24(35, 29, Gradient);
        using var reused = new TurboJpegEncoder();

        AssertMatchesFresh(reused, bmp, TurboJpegEncodeOptions.Default);
        reused.EncodeToJpeg(bmp, new TurboJpegEncodeOptions { Lossless = true });

        AssertMatchesFresh(reused, bmp, TurboJpegEncodeOptions.Default);
    }

    [Fact]
    public void ReusedEncoder_GrayscaleAfterLossless_MatchesFreshInstance()
    {
        var bmp = BmpBuilder.Create24(35, 29, Gradient);
        var grayscale = new TurboJpegEncodeOptions
        {
            Colorspace = TurboJpegColorspace.Grayscale,
        };
        using var reused = new TurboJpegEncoder();

        AssertMatchesFresh(reused, bmp, grayscale);
        reused.EncodeToJpeg(bmp, new TurboJpegEncodeOptions { Lossless = true });

        AssertMatchesFresh(reused, bmp, grayscale);
    }

    [Fact]
    public void ReusedEncoder_DefaultAfterRestartOptions_MatchesFreshInstance()
    {
        var bmp = BmpBuilder.Create24(64, 64, Gradient);
        using var reused = new TurboJpegEncoder();

        var withRestartRows = reused.EncodeToJpeg(
            bmp, new TurboJpegEncodeOptions { RestartRows = 1 });
        Assert.True(JpegMarkers.CountRestartMarkers(withRestartRows) > 0);

        AssertMatchesFresh(reused, bmp, TurboJpegEncodeOptions.Default);
    }

    private static byte[] AssertMatchesFresh(
        TurboJpegEncoder reused, byte[] bmp, TurboJpegEncodeOptions options)
    {
        var reusedOutput = reused.EncodeToJpeg(bmp, options);

        using var fresh = new TurboJpegEncoder();
        var freshOutput = fresh.EncodeToJpeg(bmp, options);

        Assert.Equal(freshOutput, reusedOutput);
        return reusedOutput;
    }

    private static (byte B, byte G, byte R) Gradient(int x, int y) =>
        ((byte)(x * 7 + y * 3), (byte)(x * 3 + y * 11), (byte)(x * 5 + y * 13));
}
