using TurboJpegWrapper.Encoder;
using DecoderApi = TurboJpegWrapper.Decoder.TurboJpeg;

namespace TurboJpegWrapper.Encoder.Tests;

public sealed class LosslessOptionsTests
{
    private const byte LosslessSof = 0xC3;

    [Fact]
    public void Lossless_24BitBmp_ProducesSof3AndExactPixels()
    {
        var bmp = BmpBuilder.Create24(35, 23, Gradient);

        var jpeg = Encode(bmp, new TurboJpegEncodeOptions { Lossless = true });

        Assert.Equal(LosslessSof, JpegMarkers.FindSofMarker(jpeg));
        AssertPixelsDecodedFrom(bmp, jpeg, Gradient);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Lossless_EveryPredictor_RoundTripsExactly(int predictor)
    {
        var bmp = BmpBuilder.Create24(35, 23, Gradient);

        var jpeg = Encode(
            bmp, new TurboJpegEncodeOptions { Lossless = true, LosslessPredictor = predictor });

        Assert.Equal(LosslessSof, JpegMarkers.FindSofMarker(jpeg));
        Assert.Equal((byte)predictor, JpegMarkers.ReadScanParameters(jpeg).Predictor);
        AssertPixelsDecodedFrom(bmp, jpeg, Gradient);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    public void Lossless_Predictor_IsWrittenToSos(int predictor)
    {
        var bmp = BmpBuilder.Create24(35, 23, Gradient);

        var jpeg = Encode(
            bmp, new TurboJpegEncodeOptions { Lossless = true, LosslessPredictor = predictor });

        Assert.Equal((byte)predictor, JpegMarkers.ReadScanParameters(jpeg).Predictor);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Lossless_PointTransform_IsWrittenToSos(int pointTransform)
    {
        var bmp = BmpBuilder.Create24(35, 23, Gradient);

        var jpeg = Encode(
            bmp,
            new TurboJpegEncodeOptions { Lossless = true, LosslessPointTransform = pointTransform });

        Assert.Equal(LosslessSof, JpegMarkers.FindSofMarker(jpeg));
        Assert.Equal((byte)pointTransform, JpegMarkers.ReadScanParameters(jpeg).SuccessiveLow);
        var decoded = DecodedBmp.Decode(jpeg);
        Assert.Equal(35, decoded.Width);
        Assert.Equal(23, decoded.Height);
    }

    [Fact]
    public void Lossless_PointTransformZero_RoundTripsExactly()
    {
        var bmp = BmpBuilder.Create24(35, 23, Gradient);

        var jpeg = Encode(
            bmp, new TurboJpegEncodeOptions { Lossless = true, LosslessPointTransform = 0 });

        Assert.Equal(LosslessSof, JpegMarkers.FindSofMarker(jpeg));
        AssertPixelsDecodedFrom(bmp, jpeg, Gradient);
    }

    [Fact]
    public void Lossless_PointTransformOne_Decodes()
    {
        var bmp = BmpBuilder.Create24(35, 23, Gradient);

        var jpeg = Encode(
            bmp, new TurboJpegEncodeOptions { Lossless = true, LosslessPointTransform = 1 });

        Assert.Equal(LosslessSof, JpegMarkers.FindSofMarker(jpeg));
        var decoded = DecodedBmp.Decode(jpeg);
        Assert.Equal(35, decoded.Width);
        Assert.Equal(23, decoded.Height);
    }

    [Fact]
    public void Lossless_WithEveryLossyFlag_IgnoresThemAndStaysPixelExact()
    {
        var bmp = BmpBuilder.Create24(35, 23, Gradient);
        var options = new TurboJpegEncodeOptions
        {
            Lossless = true,
            Progressive = true,
            ArithmeticCoding = true,
            OptimizeCoding = true,
            FastDct = true,
        };

        var jpeg = Encode(bmp, options);

        Assert.Equal(LosslessSof, JpegMarkers.FindSofMarker(jpeg));
        AssertPixelsDecodedFrom(bmp, jpeg, Gradient);
    }

    [Fact]
    public void Lossless_RestartRows_ProducesRestartMarkersAndExactPixels()
    {
        var bmp = BmpBuilder.Create24(64, 64, Gradient);

        var jpeg = Encode(
            bmp, new TurboJpegEncodeOptions { Lossless = true, RestartRows = 1 });

        Assert.Equal(LosslessSof, JpegMarkers.FindSofMarker(jpeg));
        Assert.True(
            JpegMarkers.CountRestartMarkers(jpeg) > 0,
            "The lossless JPEG contains no restart markers.");
        AssertPixelsDecodedFrom(bmp, jpeg, Gradient);
    }

    [Fact]
    public void Lossless_RestartBlocks_ThrowsAtEncodeAndSize()
    {
        var bmp = BmpBuilder.Create24(35, 23, Gradient);
        var options = new TurboJpegEncodeOptions { Lossless = true, RestartBlocks = 1 };
        using var encoder = new TurboJpegEncoder();

        var encodeError = Assert.Throws<ArgumentException>(
            () => encoder.EncodeToJpeg(bmp, options));
        var sizeError = Assert.Throws<ArgumentException>(
            () => encoder.GetMaxJpegSize(bmp, options));

        Assert.Equal("options", encodeError.ParamName);
        Assert.Equal("options", sizeError.ParamName);
    }

    [Fact]
    public void Lossy_RestartRows_ProducesRestartMarkers()
    {
        var bmp = BmpBuilder.Create24(64, 64, Gradient);

        var jpeg = Encode(bmp, new TurboJpegEncodeOptions { RestartRows = 1 });

        Assert.True(
            JpegMarkers.CountRestartMarkers(jpeg) > 0,
            "The lossy JPEG contains no restart markers.");
        Assert.Equal(64, DecodedBmp.Decode(jpeg).Width);
    }

    [Fact]
    public void Lossy_RestartBlocks_ProducesRestartMarkers()
    {
        var bmp = BmpBuilder.Create24(64, 64, Gradient);

        var jpeg = Encode(bmp, new TurboJpegEncodeOptions { RestartBlocks = 1 });

        Assert.True(
            JpegMarkers.CountRestartMarkers(jpeg) > 0,
            "The lossy JPEG contains no restart markers.");
        Assert.Equal(64, DecodedBmp.Decode(jpeg).Width);
    }

    [Fact]
    public void RestartRowsAndBlocks_BothNonZero_ThrowsAtEncodeAndSize()
    {
        var bmp = BmpBuilder.Create24(35, 23, Gradient);
        var rowsThenBlocks = new TurboJpegEncodeOptions { RestartRows = 2 } with { RestartBlocks = 3 };
        var blocksThenRows = new TurboJpegEncodeOptions { RestartBlocks = 3 } with { RestartRows = 2 };
        using var encoder = new TurboJpegEncoder();

        foreach (var options in new[] { rowsThenBlocks, blocksThenRows })
        {
            var encodeError = Assert.Throws<ArgumentException>(
                () => encoder.EncodeToJpeg(bmp, options));
            var sizeError = Assert.Throws<ArgumentException>(
                () => encoder.GetMaxJpegSize(bmp, options));

            Assert.Equal("options", encodeError.ParamName);
            Assert.Equal("options", sizeError.ParamName);
        }
    }

    [Fact]
    public void Density_Option_OverridesBmpDensity()
    {
        // 3780 ppm is 96 dpi, so a 300 dpi override must be visible in the output.
        var bmp = BmpBuilder.Create24(35, 23, Gradient, ppmX: 3780, ppmY: 3780);
        var options = new TurboJpegEncodeOptions
        {
            Density = new TurboJpegDensity(300, 300, TurboJpegDensityUnits.PixelsPerInch),
        };

        var jpeg = Encode(bmp, options);
        var info = DecoderApi.GetImageInfo(jpeg);

        Assert.Equal(300, info.DensityX);
        Assert.Equal(300, info.DensityY);
        Assert.Equal((int)TurboJpegDensityUnits.PixelsPerInch, (int)info.DensityUnits);
    }

    [Fact]
    public void MaxMemoryAndStopOnWarning_AreAccepted()
    {
        var bmp = BmpBuilder.Create24(64, 64, Gradient);
        var options = new TurboJpegEncodeOptions
        {
            MaxMemoryMegabytes = 1,
            StopOnWarning = true,
        };

        var jpeg = Encode(bmp, options);

        Assert.Equal(64, DecodedBmp.Decode(jpeg).Width);
    }

    [Fact]
    public void Lossless_32BitBmp_RoundTripsExactly()
    {
        var bmp = BmpBuilder.Create32(35, 23, (x, y) =>
        {
            var (b, g, r) = Gradient(x, y);
            return (b, g, r, (byte)(x * 5 + y * 9));
        });

        var jpeg = Encode(bmp, new TurboJpegEncodeOptions { Lossless = true });

        Assert.Equal(LosslessSof, JpegMarkers.FindSofMarker(jpeg));
        AssertPixelsDecodedFrom(bmp, jpeg, Gradient);
    }

    [Fact]
    public void Lossless_ColourPalette8_RoundTripsExactly()
    {
        var palette = CreateColourPalette(256);
        var bmp = BmpBuilder.CreatePalette8(
            35, 23, (x, y) => (byte)((x * 13 + y * 29) & 0xFF), palette);

        var jpeg = Encode(bmp, new TurboJpegEncodeOptions { Lossless = true });

        Assert.Equal(LosslessSof, JpegMarkers.FindSofMarker(jpeg));
        AssertPixelsDecodedFrom(
            bmp, jpeg, (x, y) => palette[(x * 13 + y * 29) & 0xFF]);
    }

    [Fact]
    public void Lossless_GrayscalePalette8_RoundTripsExactly()
    {
        var palette = CreateGrayscalePalette(256);
        var bmp = BmpBuilder.CreatePalette8(
            35, 23, (x, y) => (byte)((x * 13 + y * 29) & 0xFF), palette);

        var jpeg = Encode(bmp, new TurboJpegEncodeOptions { Lossless = true });

        Assert.Equal(LosslessSof, JpegMarkers.FindSofMarker(jpeg));
        AssertPixelsDecodedFrom(
            bmp, jpeg, (x, y) => palette[(x * 13 + y * 29) & 0xFF]);
    }

    [Fact]
    public void Lossless_16Bit565_RoundTripsExactly()
    {
        var bmp = BmpBuilder.Create16(
            35,
            23,
            (x, y) => (ushort)(x * 1234 + y * 567),
            redMask: 0xF800,
            greenMask: 0x07E0,
            blueMask: 0x001F,
            bitfields: true);

        var jpeg = Encode(bmp, new TurboJpegEncodeOptions { Lossless = true });

        Assert.Equal(LosslessSof, JpegMarkers.FindSofMarker(jpeg));
        AssertPixelsDecodedFrom(
            bmp, jpeg, (x, y) => From565((ushort)(x * 1234 + y * 567)));
    }

    [Fact]
    public void Lossless_1BitPalette_RoundTripsExactly()
    {
        var palette = new List<(byte B, byte G, byte R)>
        {
            (0, 0, 0),
            (0, 128, 255),
        };
        var bmp = BmpBuilder.CreatePalette1(35, 23, (x, y) => (byte)((x + y) & 1), palette);

        var jpeg = Encode(bmp, new TurboJpegEncodeOptions { Lossless = true });

        Assert.Equal(LosslessSof, JpegMarkers.FindSofMarker(jpeg));
        AssertPixelsDecodedFrom(bmp, jpeg, (x, y) => palette[(x + y) & 1]);
    }

    [Fact]
    public void Lossless_4BitPalette_RoundTripsExactly()
    {
        var palette = CreateColourPalette(16);
        var bmp = BmpBuilder.CreatePalette4(
            35, 23, (x, y) => (byte)((x * 3 + y * 5) & 0x0F), palette);

        var jpeg = Encode(bmp, new TurboJpegEncodeOptions { Lossless = true });

        Assert.Equal(LosslessSof, JpegMarkers.FindSofMarker(jpeg));
        AssertPixelsDecodedFrom(bmp, jpeg, (x, y) => palette[(x * 3 + y * 5) & 0x0F]);
    }

    [Fact]
    public void Lossless_MaxSize_UsesFourFourFourSizing()
    {
        var bmp = BmpBuilder.Create24(64, 64, Noise);
        var losslessOptions = new TurboJpegEncodeOptions { Lossless = true };
        using var encoder = new TurboJpegEncoder();

        var actual = encoder.EncodeToJpeg(bmp, losslessOptions);
        var losslessMax = encoder.GetMaxJpegSize(bmp, losslessOptions);
        var lossyMax = encoder.GetMaxJpegSize(bmp, TurboJpegEncodeOptions.Default);

        Assert.True(
            losslessMax >= actual.Length,
            $"Lossless maximum size {losslessMax} is smaller than the encoded {actual.Length} bytes.");
        Assert.True(
            losslessMax > lossyMax,
            $"Lossless maximum size {losslessMax} is not strictly larger than the 4:2:0 lossy maximum {lossyMax}.");
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public void Lossless_GrayscaleRequest_PreservesColour(bool usePalette, bool useColorspace)
    {
        var palette = CreateColourPalette(256);
        var bmp = usePalette
            ? BmpBuilder.CreatePalette8(
                35, 23, (x, y) => (byte)((x * 13 + y * 29) & 0xFF), palette)
            : BmpBuilder.Create24(35, 23, Gradient);
        var expected = usePalette
            ? (Func<int, int, (byte B, byte G, byte R)>)((x, y) =>
                palette[(x * 13 + y * 29) & 0xFF])
            : Gradient;
        var options = useColorspace
            ? new TurboJpegEncodeOptions
            {
                Lossless = true,
                Colorspace = TurboJpegColorspace.Grayscale,
            }
            : new TurboJpegEncodeOptions
            {
                Lossless = true,
                Subsampling = TurboJpegSubsampling.Grayscale,
            };

        var jpeg = Encode(bmp, options);

        Assert.Equal(LosslessSof, JpegMarkers.FindSofMarker(jpeg));
        AssertPixelsDecodedFrom(bmp, jpeg, expected);
    }

    private static byte[] Encode(byte[] bmp, TurboJpegEncodeOptions options)
    {
        using var encoder = new TurboJpegEncoder();
        return encoder.EncodeToJpeg(bmp, options);
    }

    private static void AssertPixelsDecodedFrom(
        byte[] bmp,
        byte[] jpeg,
        Func<int, int, (byte B, byte G, byte R)> expected)
    {
        var decoded = DecodedBmp.Decode(jpeg);
        var width = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
            bmp.AsSpan(18));
        var height = Math.Abs(
            System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(22)));

        Assert.Equal(width, decoded.Width);
        Assert.Equal(height, decoded.Height);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                Assert.Equal(expected(x, y), decoded.GetPixel(x, y));
            }
        }
    }

    private static List<(byte B, byte G, byte R)> CreateColourPalette(int count)
    {
        var palette = new List<(byte B, byte G, byte R)>(count);
        for (var i = 0; i < count; i++)
        {
            palette.Add(((byte)i, (byte)(255 - i), (byte)((i * 7) & 0xFF)));
        }

        return palette;
    }

    private static List<(byte B, byte G, byte R)> CreateGrayscalePalette(int count)
    {
        var palette = new List<(byte B, byte G, byte R)>(count);
        for (var i = 0; i < count; i++)
        {
            palette.Add(((byte)i, (byte)i, (byte)i));
        }

        return palette;
    }

    private static (byte B, byte G, byte R) From565(ushort sample)
    {
        var red = (sample >> 11) & 0x1F;
        var green = (sample >> 5) & 0x3F;
        var blue = sample & 0x1F;
        return (ScaleToByte(blue, 31), ScaleToByte(green, 63), ScaleToByte(red, 31));
    }

    private static byte ScaleToByte(int value, int max) =>
        (byte)((value * 255 + (max / 2)) / max);

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
