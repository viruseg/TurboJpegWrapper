using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using TurboJpegWrapper;

namespace TurboJpegWrapper.Tests;

public sealed class AutoOrientTests
{
    private const int Red = 0xFF0000;
    private const int Green = 0x00FF00;
    private const int Blue = 0x0000FF;
    private const int Yellow = 0xFFFF00;
    private static readonly Lazy<byte[]> AsymmetricJpeg = new(CreateAsymmetricJpeg);

    public static IEnumerable<object[]> AsymmetricOrientations()
    {
        yield return [(ushort)1, TestFixtures.Width, TestFixtures.Height, Red, Green, Blue, Yellow];
        yield return [(ushort)2, TestFixtures.Width, TestFixtures.Height, Green, Red, Yellow, Blue];
        yield return [(ushort)3, TestFixtures.Width, TestFixtures.Height, Yellow, Blue, Green, Red];
        yield return [(ushort)4, TestFixtures.Width, TestFixtures.Height, Blue, Yellow, Red, Green];
        yield return [(ushort)5, TestFixtures.Height, TestFixtures.Width, Red, Blue, Green, Yellow];
        yield return [(ushort)6, TestFixtures.Height, TestFixtures.Width, Blue, Red, Yellow, Green];
        yield return [(ushort)7, TestFixtures.Height, TestFixtures.Width, Yellow, Green, Blue, Red];
        yield return [(ushort)8, TestFixtures.Height, TestFixtures.Width, Green, Yellow, Red, Blue];
    }

    public static IEnumerable<object[]> InPlaceOrientations()
    {
        yield return [(ushort)2, TestFixtures.Width, TestFixtures.Height, Green, Red, Yellow, Blue];
        yield return [(ushort)3, TestFixtures.Width, TestFixtures.Height, Yellow, Blue, Green, Red];
        yield return [(ushort)4, TestFixtures.Width, TestFixtures.Height, Blue, Yellow, Red, Green];
    }

    [Fact]
    public void Options_AutoOrient_DefaultsToFalse()
    {
        Assert.False(new TurboJpegDecodeOptions().AutoOrient);
        Assert.False(TurboJpegDecodeOptions.Default.AutoOrient);
    }

    [Fact]
    public void DecodeToBmp_WithoutAutoOrient_IgnoresExifOrientation()
    {
        using var decoder = new TurboJpegDecoder();
        var bmp = decoder.DecodeToBmp(AsymmetricFixture(6));

        AssertLayout(bmp, TestFixtures.Width, TestFixtures.Height, 32);
        AssertOrientation(bmp, TestFixtures.Width, TestFixtures.Height, Red, Green, Blue, Yellow);
    }

    [Fact]
    public void DecodeToBmp_AutoOrientWithoutExif_MatchesDefaultOutput()
    {
        var jpeg = TestFixtures.Load("baseline-444.jpg");

        using var decoder = new TurboJpegDecoder();
        var plain = decoder.DecodeToBmp(jpeg);
        var autoOrient = decoder.DecodeToBmp(jpeg, new TurboJpegDecodeOptions { AutoOrient = true });

        Assert.Equal(plain, autoOrient);
    }

    [Fact]
    public void DecodeToBmp_AutoOrientNormalOrientation_MatchesDefaultOutput()
    {
        var jpeg = TestFixtures.Load("baseline-444.jpg");
        var oriented = JpegPatcher.WithExifOrientation(jpeg, 1);

        using var decoder = new TurboJpegDecoder();
        var plain = decoder.DecodeToBmp(jpeg);
        var autoOrient = decoder.DecodeToBmp(
            oriented, new TurboJpegDecodeOptions { AutoOrient = true });

        Assert.Equal(plain, autoOrient);
    }

    [Fact]
    public void DecodeToBmp_AutoOrientMalformedIfdOffset_MatchesDefaultOutput()
    {
        var jpeg = JpegPatcher.WithSegment(
            TestFixtures.Load("baseline-444.jpg"),
            0xE1,
            JpegPatcher.CreateExifPayloadWithIfdInsideHeader());

        using var decoder = new TurboJpegDecoder();
        var plain = decoder.DecodeToBmp(jpeg);
        var autoOrient = decoder.DecodeToBmp(jpeg, new TurboJpegDecodeOptions { AutoOrient = true });

        Assert.Equal(plain, autoOrient);
    }

    [Theory]
    [MemberData(nameof(AsymmetricOrientations))]
    public void DecodeToBmp_AutoOrient_AppliesOrientation(
        ushort orientation,
        int expectedWidth,
        int expectedHeight,
        int topLeft,
        int topRight,
        int bottomLeft,
        int bottomRight)
    {
        using var decoder = new TurboJpegDecoder();
        var bmp = decoder.DecodeToBmp(
            AsymmetricFixture(orientation),
            new TurboJpegDecodeOptions { AutoOrient = true });

        AssertLayout(bmp, expectedWidth, expectedHeight, 32);
        AssertOrientation(bmp, expectedWidth, expectedHeight, topLeft, topRight, bottomLeft, bottomRight);
    }

    [Theory]
    [InlineData("progressive.jpg")]
    [InlineData("lossless.jpg")]
    [InlineData("baseline-420.jpg")]
    public void DecodeToBmp_AutoOrient_WorksWithSupportedEncodings(string fixture)
    {
        var jpeg = JpegPatcher.WithExifOrientation(TestFixtures.Load(fixture), 6);

        using var decoder = new TurboJpegDecoder();
        var bmp = decoder.DecodeToBmp(jpeg, new TurboJpegDecodeOptions { AutoOrient = true });

        AssertLayout(bmp, TestFixtures.Height, TestFixtures.Width, 32);
    }

    [Theory]
    [InlineData(TurboJpegBmpBitDepth.Bpp8)]
    [InlineData(TurboJpegBmpBitDepth.Bpp24)]
    [InlineData(TurboJpegBmpBitDepth.Bpp32)]
    public void DecodeToBmp_AutoOrientWithHalfScale_WritesExpectedLayout(
        TurboJpegBmpBitDepth bitDepth)
    {
        var options = new TurboJpegDecodeOptions
        {
            ScalingFactor = new TurboJpegScalingFactor(1, 2),
            BmpBitDepth = bitDepth,
            AutoOrient = true
        };

        using var decoder = new TurboJpegDecoder();
        var bmp = decoder.DecodeToBmp(OrientedFixture(6), options);

        AssertLayout(bmp, 12, 18, (int)bitDepth);
        AssertPaddingZero(bmp);

        if (bitDepth == TurboJpegBmpBitDepth.Bpp32)
        {
            AssertColor(bmp, 10, 5, 255, 0, 0, tolerance: 16);
        }
    }

    [Theory]
    [MemberData(nameof(InPlaceOrientations))]
    public void DecodeToBmp_AutoOrient24Bit_MirrorsInPlace(
        ushort orientation,
        int expectedWidth,
        int expectedHeight,
        int topLeft,
        int topRight,
        int bottomLeft,
        int bottomRight)
    {
        var options = new TurboJpegDecodeOptions
        {
            BmpBitDepth = TurboJpegBmpBitDepth.Bpp24,
            AutoOrient = true
        };

        using var decoder = new TurboJpegDecoder();
        var bmp = decoder.DecodeToBmp(AsymmetricFixture(orientation), options);

        AssertLayout(bmp, expectedWidth, expectedHeight, 24);
        AssertOrientation(bmp, expectedWidth, expectedHeight, topLeft, topRight, bottomLeft, bottomRight);
        AssertPaddingZero(bmp);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void DecodeToBmp_AutoOrient8BitInPlace_WritesExpectedLayout(ushort orientation)
    {
        var jpeg = JpegPatcher.WithExifOrientation(TestFixtures.Load("grayscale.jpg"), orientation);

        using var decoder = new TurboJpegDecoder();
        var bmp = decoder.DecodeToBmp(jpeg, new TurboJpegDecodeOptions
        {
            BmpBitDepth = TurboJpegBmpBitDepth.Bpp8,
            AutoOrient = true
        });

        AssertLayout(bmp, TestFixtures.Width, TestFixtures.Height, 8);
        AssertPaddingZero(bmp);
    }

    [Fact]
    public void DecodeToBmp_AutoOrient24Bit_WritesOrientedBmp()
    {
        using var decoder = new TurboJpegDecoder();
        var bmp = decoder.DecodeToBmp(
            AsymmetricFixture(6),
            new TurboJpegDecodeOptions
            {
                BmpBitDepth = TurboJpegBmpBitDepth.Bpp24,
                AutoOrient = true
            });

        AssertLayout(bmp, TestFixtures.Height, TestFixtures.Width, 24);
        AssertOrientation(
            bmp,
            TestFixtures.Height,
            TestFixtures.Width,
            Blue,
            Red,
            Yellow,
            Green);
        AssertPaddingZero(bmp);
    }

    [Fact]
    public void DecodeToBmp_AutoOrient8Bit_RotatesGrayscale()
    {
        var jpeg = JpegPatcher.WithExifOrientation(TestFixtures.Load("grayscale.jpg"), 6);

        using var decoder = new TurboJpegDecoder();
        var bmp = decoder.DecodeToBmp(jpeg, new TurboJpegDecodeOptions
        {
            BmpBitDepth = TurboJpegBmpBitDepth.Bpp8,
            AutoOrient = true
        });

        AssertLayout(bmp, TestFixtures.Height, TestFixtures.Width, 8);
        AssertPaddingZero(bmp);

        var (b, g, r, a) = BmpTestHelpers.Pixel(bmp, 20, 10);
        Assert.Equal(r, g);
        Assert.Equal(g, b);
        Assert.Equal(255, a);
        Assert.InRange((int)r, 60, 92);
    }

    [Fact]
    public void DecodeToBmp_RepeatedCalls_ToggleOrientationCorrectly()
    {
        var jpeg = AsymmetricJpeg.Value;

        using var decoder = new TurboJpegDecoder();
        var options = new TurboJpegDecodeOptions { AutoOrient = true };

        var flipHorizontal = decoder.DecodeToBmp(
            JpegPatcher.WithExifOrientation(jpeg, 2), options);
        AssertOrientation(
            flipHorizontal,
            TestFixtures.Width,
            TestFixtures.Height,
            Green,
            Red,
            Yellow,
            Blue);

        var rotate90 = decoder.DecodeToBmp(
            JpegPatcher.WithExifOrientation(jpeg, 6), options);
        AssertOrientation(
            rotate90,
            TestFixtures.Height,
            TestFixtures.Width,
            Blue,
            Red,
            Yellow,
            Green);

        var rotate180 = decoder.DecodeToBmp(
            JpegPatcher.WithExifOrientation(jpeg, 3), options);
        AssertOrientation(
            rotate180,
            TestFixtures.Width,
            TestFixtures.Height,
            Yellow,
            Blue,
            Green,
            Red);

        var flipVertical = decoder.DecodeToBmp(
            JpegPatcher.WithExifOrientation(jpeg, 4), options);
        AssertOrientation(
            flipVertical,
            TestFixtures.Width,
            TestFixtures.Height,
            Blue,
            Yellow,
            Red,
            Green);

        var normal = decoder.DecodeToBmp(jpeg);
        AssertOrientation(
            normal,
            TestFixtures.Width,
            TestFixtures.Height,
            Red,
            Green,
            Blue,
            Yellow);
    }

    [Fact]
    public void DecodeToBmp_StaticWithAutoOrient_Works()
    {
        var bmp = TurboJpeg.DecodeToBmp(
            AsymmetricFixture(6), new TurboJpegDecodeOptions { AutoOrient = true });

        AssertOrientation(
            bmp,
            TestFixtures.Height,
            TestFixtures.Width,
            Blue,
            Red,
            Yellow,
            Green);
    }

    private static byte[] AsymmetricFixture(ushort orientation) =>
        JpegPatcher.WithExifOrientation(AsymmetricJpeg.Value, orientation);

    private static byte[] OrientedFixture(ushort orientation) =>
        JpegPatcher.WithExifOrientation(TestFixtures.Load("baseline-444.jpg"), orientation);

    private static void AssertOrientation(
        byte[] bmp,
        int expectedWidth,
        int expectedHeight,
        int topLeft,
        int topRight,
        int bottomLeft,
        int bottomRight)
    {
        AssertColor(bmp, 5, 5, topLeft, tolerance: 24);
        AssertColor(bmp, expectedWidth - 6, 5, topRight, tolerance: 24);
        AssertColor(bmp, 5, expectedHeight - 6, bottomLeft, tolerance: 24);
        AssertColor(
            bmp,
            expectedWidth - 6,
            expectedHeight - 6,
            bottomRight,
            tolerance: 24);
    }

    private static void AssertLayout(
        byte[] bmp, int expectedWidth, int expectedHeight, int bitsPerPixel)
    {
        Assert.Equal(expectedWidth, BmpTestHelpers.Width(bmp));
        Assert.Equal(expectedHeight, BmpTestHelpers.Height(bmp));
        Assert.Equal(bitsPerPixel, BmpTestHelpers.BitsPerPixel(bmp));
        Assert.Equal(
            BmpTestHelpers.PixelOffset(bmp) + BmpTestHelpers.RowStride(bmp) * expectedHeight,
            bmp.Length);
    }

    private static void AssertPaddingZero(byte[] bmp)
    {
        var rowBytes = BmpTestHelpers.Width(bmp) * (BmpTestHelpers.BitsPerPixel(bmp) / 8);
        var stride = BmpTestHelpers.RowStride(bmp);
        if (stride == rowBytes)
        {
            return;
        }

        var pixelOffset = BmpTestHelpers.PixelOffset(bmp);
        for (var y = 0; y < BmpTestHelpers.Height(bmp); y++)
        {
            for (var i = rowBytes; i < stride; i++)
            {
                Assert.Equal(0, bmp[pixelOffset + y * stride + i]);
            }
        }
    }

    private static void AssertColor(byte[] bmp, int x, int y, int rgb, int tolerance)
    {
        var r = (byte)((rgb >> 16) & 0xFF);
        var g = (byte)((rgb >> 8) & 0xFF);
        var b = (byte)(rgb & 0xFF);
        AssertColor(bmp, x, y, r, g, b, tolerance);
    }

    private static void AssertColor(
        byte[] bmp, int x, int y, int r, int g, int b, int tolerance)
    {
        var (actualB, actualG, actualR, actualA) = BmpTestHelpers.Pixel(bmp, x, y);

        Assert.Equal(255, actualA);
        Assert.InRange((int)actualR, r - tolerance, r + tolerance);
        Assert.InRange((int)actualG, g - tolerance, g + tolerance);
        Assert.InRange((int)actualB, b - tolerance, b + tolerance);
    }

    private static byte[] CreateAsymmetricJpeg()
    {
        using var image = new Image<Rgb24>(
            TestFixtures.Width, TestFixtures.Height, new Rgb24(255, 0, 0));

        var halfWidth = TestFixtures.Width / 2;
        var halfHeight = TestFixtures.Height / 2;

        image.ProcessPixelRows(accessor =>
        {
            Fill(
                accessor,
                halfWidth,
                0,
                TestFixtures.Width - halfWidth,
                halfHeight,
                new Rgb24(0, 255, 0));
            Fill(
                accessor,
                0,
                halfHeight,
                halfWidth,
                TestFixtures.Height - halfHeight,
                new Rgb24(0, 0, 255));
            Fill(
                accessor,
                halfWidth,
                halfHeight,
                TestFixtures.Width - halfWidth,
                TestFixtures.Height - halfHeight,
                new Rgb24(255, 255, 0));
        });

        using var stream = new MemoryStream();
        image.SaveAsJpeg(stream, new JpegEncoder
        {
            Quality = 100,
            ColorType = JpegEncodingColor.YCbCrRatio444
        });
        return stream.ToArray();

        static void Fill(PixelAccessor<Rgb24> rows, int x, int y, int width, int height, Rgb24 color)
        {
            for (var row = y; row < y + height; row++)
            {
                rows.GetRowSpan(row).Slice(x, width).Fill(color);
            }
        }
    }
}
