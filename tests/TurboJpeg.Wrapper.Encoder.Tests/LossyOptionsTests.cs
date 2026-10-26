using TurboJpegWrapper.Encoder;
using DecoderApi = TurboJpegWrapper.Decoder.TurboJpeg;

namespace TurboJpegWrapper.Encoder.Tests;

public sealed class LossyOptionsTests
{
    private const byte BaselineSof = 0xC0;
    private const byte ProgressiveSof = 0xC2;
    private const byte ArithmeticSof = 0xC9;
    private const byte ProgressiveArithmeticSof = 0xCA;

    [Fact]
    public void Quality_HigherQuality_ProducesLargerOutputAndBothDecode()
    {
        var bmp = BmpBuilder.Create24(64, 64, Noise);
        using var encoder = new TurboJpegEncoder();

        var low = encoder.EncodeToJpeg(bmp, new TurboJpegEncodeOptions { Quality = 50 });
        var high = encoder.EncodeToJpeg(bmp, new TurboJpegEncodeOptions { Quality = 95 });

        Assert.True(
            high.Length > low.Length,
            $"Quality 95 produced {high.Length} bytes, which is not larger than the {low.Length} bytes of quality 50.");
        Assert.Equal(64, DecodedBmp.Decode(low).Width);
        Assert.Equal(64, DecodedBmp.Decode(high).Height);
    }

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
    public void Subsampling_EveryValue_EncodesDecodesAndIsReported(TurboJpegSubsampling subsampling)
    {
        var bmp = BmpBuilder.Create24(35, 29, Gradient);
        var options = new TurboJpegEncodeOptions { Subsampling = subsampling };
        using var encoder = new TurboJpegEncoder();

        var jpeg = encoder.EncodeToJpeg(bmp, options);
        var maxSize = encoder.GetMaxJpegSize(bmp, options);
        var info = DecoderApi.GetImageInfo(jpeg);
        var decoded = DecodedBmp.Decode(jpeg);

        Assert.True(
            maxSize >= jpeg.Length,
            $"The maximum size {maxSize} is smaller than the encoded {jpeg.Length} bytes.");
        Assert.Equal((int)subsampling, (int)info.Subsampling);
        Assert.Equal(35, decoded.Width);
        Assert.Equal(29, decoded.Height);
    }

    [Fact]
    public void Colorspace_RgbWithDefaultSubsampling_IsReportedAsRgbAndFourFourFour()
    {
        var bmp = BmpBuilder.Create24(35, 29, Gradient);
        var options = new TurboJpegEncodeOptions { Colorspace = TurboJpegColorspace.Rgb };
        using var encoder = new TurboJpegEncoder();

        var jpeg = encoder.EncodeToJpeg(bmp, options);
        var info = DecoderApi.GetImageInfo(jpeg);

        Assert.Equal((int)TurboJpegColorspace.Rgb, (int)info.Colorspace);
        Assert.Equal((int)TurboJpegSubsampling.Yuv444, (int)info.Subsampling);
        Assert.Equal(35, DecodedBmp.Decode(jpeg).Width);
    }

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
    public void Colorspace_Rgb_EverySubsamplingValue_EncodesAsFourFourFour(
        TurboJpegSubsampling subsampling)
    {
        var bmp = BmpBuilder.Create24(35, 29, Gradient);
        var options = new TurboJpegEncodeOptions
        {
            Colorspace = TurboJpegColorspace.Rgb,
            Subsampling = subsampling,
        };
        var fourFourFourOptions = new TurboJpegEncodeOptions
        {
            Colorspace = TurboJpegColorspace.Rgb,
            Subsampling = TurboJpegSubsampling.Yuv444,
        };
        using var encoder = new TurboJpegEncoder();

        var jpeg = encoder.EncodeToJpeg(bmp, options);
        var info = DecoderApi.GetImageInfo(jpeg);

        Assert.Equal((int)TurboJpegColorspace.Rgb, (int)info.Colorspace);
        Assert.Equal((int)TurboJpegSubsampling.Yuv444, (int)info.Subsampling);
        Assert.Equal(
            encoder.GetMaxJpegSize(bmp, fourFourFourOptions),
            encoder.GetMaxJpegSize(bmp, options));
        Assert.Equal(35, DecodedBmp.Decode(jpeg).Width);
    }

    [Fact]
    public void Colorspace_Grayscale_ConvertsColourBmpToGrayPixels()
    {
        var bmp = BmpBuilder.Create24(32, 24, (x, y) =>
        {
            var left = x < 16;
            if (y < 12)
            {
                return left ? ((byte)0, (byte)0, (byte)255) : ((byte)0, (byte)255, (byte)0);
            }

            return left ? ((byte)255, (byte)0, (byte)0) : ((byte)255, (byte)255, (byte)255);
        });
        using var encoder = new TurboJpegEncoder();

        var jpeg = encoder.EncodeToJpeg(
            bmp, new TurboJpegEncodeOptions { Colorspace = TurboJpegColorspace.Grayscale });
        var info = DecoderApi.GetImageInfo(jpeg);
        var decoded = DecodedBmp.Decode(jpeg);

        Assert.Equal((int)TurboJpegColorspace.Grayscale, (int)info.Colorspace);
        for (var y = 0; y < decoded.Height; y++)
        {
            for (var x = 0; x < decoded.Width; x++)
            {
                var pixel = decoded.GetPixel(x, y);
                Assert.True(
                    pixel.R == pixel.G && pixel.G == pixel.B,
                    $"Pixel ({x}, {y}) is ({pixel.R}, {pixel.G}, {pixel.B}), not grayscale.");
            }
        }
    }

    [Fact]
    public void FastDct_ChangesEncodedBytesAndProducesBaselineJpeg()
    {
        var bmp = BmpBuilder.Create24(64, 64, Noise);
        using var encoder = new TurboJpegEncoder();

        var defaultJpeg = encoder.EncodeToJpeg(bmp, TurboJpegEncodeOptions.Default);
        var fastJpeg = encoder.EncodeToJpeg(bmp, new TurboJpegEncodeOptions { FastDct = true });

        Assert.NotEqual(defaultJpeg, fastJpeg);
        Assert.Equal(BaselineSof, JpegMarkers.FindSofMarker(fastJpeg));
        Assert.Equal(64, DecodedBmp.Decode(fastJpeg).Width);
    }

    [Fact]
    public void OptimizeCoding_ProducesStrictlySmallerOutput()
    {
        var bmp = BmpBuilder.Create24(64, 64, Noise);
        using var encoder = new TurboJpegEncoder();

        var defaultJpeg = encoder.EncodeToJpeg(bmp, TurboJpegEncodeOptions.Default);
        var optimized = encoder.EncodeToJpeg(
            bmp, new TurboJpegEncodeOptions { OptimizeCoding = true });

        Assert.True(
            optimized.Length < defaultJpeg.Length,
            $"Optimized output is {optimized.Length} bytes, which is not smaller than the {defaultJpeg.Length}-byte default output.");
        Assert.Equal(64, DecodedBmp.Decode(optimized).Width);
    }

    [Fact]
    public void Progressive_ProducesProgressiveSof()
    {
        var bmp = BmpBuilder.Create24(37, 23, Gradient);
        using var encoder = new TurboJpegEncoder();

        var jpeg = encoder.EncodeToJpeg(bmp, new TurboJpegEncodeOptions { Progressive = true });

        Assert.Equal(ProgressiveSof, JpegMarkers.FindSofMarker(jpeg));
        Assert.Equal(37, DecodedBmp.Decode(jpeg).Width);
    }

    [Theory]
    [InlineData(false, ArithmeticSof)]
    [InlineData(true, ProgressiveArithmeticSof)]
    public void ArithmeticCoding_ProducesArithmeticSof(bool progressive, byte expectedSof)
    {
        var bmp = BmpBuilder.Create24(37, 23, Gradient);
        var options = new TurboJpegEncodeOptions
        {
            ArithmeticCoding = true,
            Progressive = progressive,
        };
        using var encoder = new TurboJpegEncoder();

        var jpeg = encoder.EncodeToJpeg(bmp, options);

        Assert.Equal(expectedSof, JpegMarkers.FindSofMarker(jpeg));
    }

    [Fact]
    public void ReusedInstance_ReturnsToByteIdenticalDefaultOutput()
    {
        var bmp = BmpBuilder.Create24(35, 29, Gradient);
        using var reused = new TurboJpegEncoder();

        reused.EncodeToJpeg(bmp, TurboJpegEncodeOptions.Default);
        reused.EncodeToJpeg(
            bmp,
            new TurboJpegEncodeOptions
            {
                Progressive = true,
                FastDct = true,
                OptimizeCoding = true,
            });
        reused.EncodeToJpeg(bmp, new TurboJpegEncodeOptions { ArithmeticCoding = true });
        var finalOutput = reused.EncodeToJpeg(bmp, TurboJpegEncodeOptions.Default);

        using var fresh = new TurboJpegEncoder();
        var freshOutput = fresh.EncodeToJpeg(bmp, TurboJpegEncodeOptions.Default);

        Assert.Equal(freshOutput, finalOutput);
    }

    private static (byte B, byte G, byte R) Gradient(int x, int y) =>
        ((byte)(x * 7 + y * 3), (byte)(x * 3 + y * 11), (byte)(x * 5 + y * 13));

    private static (byte B, byte G, byte R) Noise(int x, int y)
    {
        var value = (uint)(x * 73856093 ^ y * 19349663);
        value ^= value << 13;
        value ^= value >> 17;
        value ^= value << 5;
        return ((byte)value, (byte)(value >> 8), (byte)(value >> 16));
    }
}
