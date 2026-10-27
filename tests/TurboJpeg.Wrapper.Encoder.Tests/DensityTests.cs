using TurboJpegWrapper;

namespace TurboJpegWrapper.Encoder.Tests;

public sealed class DensityTests
{
    private const byte LosslessSof = 0xC3;

    private static readonly TurboJpegDensity Dpi300 =
        new(300, 300, TurboJpegDensityUnits.PixelsPerInch);

    [Fact]
    public void Lossless_ExplicitDensity_ThrowsFromEveryEncodeOverloadAndSizing()
    {
        var bmp = BmpBuilder.Create24(35, 23, Gradient);
        var options = new TurboJpegEncodeOptions { Lossless = true, Density = Dpi300 };
        using var encoder = new TurboJpegEncoder();
        var destination = new byte[1 << 20];

        var arrayError = Assert.Throws<ArgumentException>(
            () => encoder.EncodeToJpeg(bmp, options));
        var spanError = Assert.Throws<ArgumentException>(
            () => encoder.EncodeToJpeg(bmp, options, destination));
        var sizeError = Assert.Throws<ArgumentException>(
            () => encoder.GetMaxJpegSize(bmp, options));

        Assert.Equal("options", arrayError.ParamName);
        Assert.Equal("options", spanError.ParamName);
        Assert.Equal("options", sizeError.ParamName);
    }

    [Fact]
    public void Lossless_DefaultOptionsWithDensity_ThrowsFromOptionlessOverloads()
    {
        var bmp = BmpBuilder.Create24(35, 23, Gradient);
        using var encoder = new TurboJpegEncoder(
            new TurboJpegEncodeOptions { Lossless = true, Density = Dpi300 });

        var encodeError = Assert.Throws<ArgumentException>(() => encoder.EncodeToJpeg(bmp));
        var sizeError = Assert.Throws<ArgumentException>(() => encoder.GetMaxJpegSize(bmp));

        Assert.Equal("options", encodeError.ParamName);
        Assert.Equal("options", sizeError.ParamName);
    }

    [Fact]
    public void Rgb_ExplicitDensity_ThrowsFromEveryEncodeOverloadAndSizing()
    {
        var bmp = BmpBuilder.Create24(35, 23, Gradient);
        var options = new TurboJpegEncodeOptions
        {
            Colorspace = TurboJpegColorspace.Rgb,
            Density = Dpi300,
        };
        using var encoder = new TurboJpegEncoder();
        var destination = new byte[1 << 20];

        var arrayError = Assert.Throws<ArgumentException>(
            () => encoder.EncodeToJpeg(bmp, options));
        var spanError = Assert.Throws<ArgumentException>(
            () => encoder.EncodeToJpeg(bmp, options, destination));
        var sizeError = Assert.Throws<ArgumentException>(
            () => encoder.GetMaxJpegSize(bmp, options));

        Assert.Equal("options", arrayError.ParamName);
        Assert.Equal("options", spanError.ParamName);
        Assert.Equal("options", sizeError.ParamName);
    }

    [Fact]
    public void Lossless_BmpDensity_IsIgnoredAndDecodesAsOneToOneUnknown()
    {
        var bmp = BmpBuilder.Create24(8, 8, Gradient, ppmX: 3780, ppmY: 3780);

        var jpeg = Encode(bmp, new TurboJpegEncodeOptions { Lossless = true });
        var info = TurboJpeg.GetImageInfo(jpeg);

        Assert.Equal(LosslessSof, JpegMarkers.FindSofMarker(jpeg));
        Assert.Equal(1, info.DensityX);
        Assert.Equal(1, info.DensityY);
        Assert.Equal((int)TurboJpegDensityUnits.Unknown, (int)info.DensityUnits);
    }

    [Fact]
    public void Rgb_BmpDensity_IsIgnoredAndDecodesAsOneToOneUnknown()
    {
        var bmp = BmpBuilder.Create24(8, 8, Gradient, ppmX: 3780, ppmY: 3780);

        var jpeg = Encode(bmp, new TurboJpegEncodeOptions { Colorspace = TurboJpegColorspace.Rgb });
        var info = TurboJpeg.GetImageInfo(jpeg);

        Assert.Equal((int)TurboJpegColorspace.Rgb, (int)info.Colorspace);
        Assert.Equal(1, info.DensityX);
        Assert.Equal(1, info.DensityY);
        Assert.Equal((int)TurboJpegDensityUnits.Unknown, (int)info.DensityUnits);
    }

    [Fact]
    public void Lossy_YCbCrBmpDensity_StillDecodesAs96Dpi()
    {
        var bmp = BmpBuilder.Create24(8, 8, Gradient, ppmX: 3780, ppmY: 3780);

        var jpeg = Encode(
            bmp,
            new TurboJpegEncodeOptions
            {
                Colorspace = TurboJpegColorspace.YCbCr,
                Subsampling = TurboJpegSubsampling.Yuv444,
            });
        var info = TurboJpeg.GetImageInfo(jpeg);

        Assert.Equal(96, info.DensityX);
        Assert.Equal(96, info.DensityY);
        Assert.Equal((int)TurboJpegDensityUnits.PixelsPerInch, (int)info.DensityUnits);
    }

    [Fact]
    public void Grayscale_ExplicitDensity_IsApplied()
    {
        var bmp = BmpBuilder.Create24(8, 8, Gradient);

        var jpeg = Encode(
            bmp,
            new TurboJpegEncodeOptions
            {
                Colorspace = TurboJpegColorspace.Grayscale,
                Density = Dpi300,
            });
        var info = TurboJpeg.GetImageInfo(jpeg);

        Assert.Equal((int)TurboJpegColorspace.Grayscale, (int)info.Colorspace);
        Assert.Equal(300, info.DensityX);
        Assert.Equal(300, info.DensityY);
        Assert.Equal((int)TurboJpegDensityUnits.PixelsPerInch, (int)info.DensityUnits);
    }

    [Fact]
    public void BmpHugeDensity_IsClampedToNativeSixteenBitMaximum()
    {
        var bmp = BmpBuilder.Create24(8, 8, Gradient, ppmX: int.MaxValue, ppmY: int.MaxValue);

        var jpeg = Encode(bmp, TurboJpegEncodeOptions.Default);
        var info = TurboJpeg.GetImageInfo(jpeg);

        Assert.Equal(65535, info.DensityX);
        Assert.Equal(65535, info.DensityY);
        Assert.Equal((int)TurboJpegDensityUnits.PixelsPerInch, (int)info.DensityUnits);
    }

    private static byte[] Encode(byte[] bmp, TurboJpegEncodeOptions options)
    {
        using var encoder = new TurboJpegEncoder();
        return encoder.EncodeToJpeg(bmp, options);
    }

    private static (byte B, byte G, byte R) Gradient(int x, int y) =>
        ((byte)(x * 7 + y * 3), (byte)(x * 3 + y * 11), (byte)(x * 5 + y * 13));
}
