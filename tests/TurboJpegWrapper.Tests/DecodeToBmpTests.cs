using System.Buffers.Binary;
using TurboJpegWrapper;

namespace TurboJpegWrapper.Tests;

public sealed class DecodeToBmpTests
{
    [Fact]
    public void DecodeToBmp_WritesValidBmpHeader()
    {
        using var decoder = new TurboJpegDecoder();
        var bmp = decoder.DecodeToBmp(TestFixtures.Load("baseline-444.jpg"));

        Assert.Equal((byte)'B', bmp[0]);
        Assert.Equal((byte)'M', bmp[1]);
        Assert.Equal(bmp.Length, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(2)));
        Assert.Equal(54, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(10)));
        Assert.Equal(40, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(14)));
        Assert.Equal(35, BmpTestHelpers.Width(bmp));
        Assert.Equal(23, BmpTestHelpers.Height(bmp));
        Assert.Equal(1, BinaryPrimitives.ReadUInt16LittleEndian(bmp.AsSpan(26)));
        Assert.Equal(32, BinaryPrimitives.ReadUInt16LittleEndian(bmp.AsSpan(28)));
        Assert.Equal(0, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(30)));
        Assert.Equal(54 + 35 * 23 * 4, bmp.Length);
    }

    [Fact]
    public void DecodeToBmp_OpensWithSystemDrawing()
    {
        using var decoder = new TurboJpegDecoder();
        var bmp = decoder.DecodeToBmp(TestFixtures.Load("baseline-444.jpg"));

        using var bitmap = new System.Drawing.Bitmap(new MemoryStream(bmp));
        Assert.Equal(35, bitmap.Width);
        Assert.Equal(23, bitmap.Height);
        Assert.Equal(255, bitmap.GetPixel(10, 2).A);
    }

    [Fact]
    public void DecodeToBmp_Colors_AreCorrect()
    {
        using var decoder = new TurboJpegDecoder();
        var bmp = decoder.DecodeToBmp(TestFixtures.Load("baseline-444.jpg"));

        AssertBandColors(bmp, tolerance: 8);
    }

    [Fact]
    public void DecodeToBmp_IsBottomUp()
    {
        using var decoder = new TurboJpegDecoder();
        var bmp = decoder.DecodeToBmp(TestFixtures.Load("baseline-444.jpg"));

        Assert.Equal(255, bmp[57]);
        Assert.InRange((int)bmp[54], 120, 136);
        Assert.InRange((int)bmp[55], 120, 136);
        Assert.InRange((int)bmp[56], 120, 136);

        AssertColor(bmp, 10, 2, 255, 0, 0, tolerance: 8);
    }

    [Fact]
    public void DecodeToBmp_420_TolerantColors()
    {
        using var decoder = new TurboJpegDecoder();
        var bmp = decoder.DecodeToBmp(TestFixtures.Load("baseline-420.jpg"));

        AssertBandColors(bmp, tolerance: 16);
    }

    [Fact]
    public void DecodeToBmp_Grayscale_IsOpaqueGray()
    {
        using var decoder = new TurboJpegDecoder();
        var bmp = decoder.DecodeToBmp(TestFixtures.Load("grayscale.jpg"));

        int width = BmpTestHelpers.Width(bmp);
        int height = BmpTestHelpers.Height(bmp);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var (b, g, r, a) = BmpTestHelpers.Pixel(bmp, x, y);
                Assert.Equal(r, g);
                Assert.Equal(g, b);
                Assert.Equal(255, a);
            }
        }
    }

    [Fact]
    public void DecodeToBmp_Progressive_Works()
    {
        using var decoder = new TurboJpegDecoder();
        var bmp = decoder.DecodeToBmp(TestFixtures.Load("progressive.jpg"));

        AssertValidBmp(bmp);
    }

    [Fact]
    public void DecodeToBmp_Precision12_ThrowsUnsupported()
    {
        using var decoder = new TurboJpegDecoder();

        var exception = Assert.Throws<TurboJpegUnsupportedException>(
            () => decoder.DecodeToBmp(TestFixtures.Load("precision12.jpg")));

        Assert.Contains("precision", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("colorspace", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DecodeToBmp_Lossless_Works()
    {
        using var decoder = new TurboJpegDecoder();
        var bmp = decoder.DecodeToBmp(TestFixtures.Load("lossless.jpg"));

        AssertValidBmp(bmp);
    }

    [Fact]
    public void DecodeToBmp_Truncated_ThrowsTurboJpegException()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = TestFixtures.Load("baseline-444.jpg");

        Assert.Throws<TurboJpegException>(() => decoder.DecodeToBmp(jpeg.AsSpan(0, jpeg.Length / 2)));
    }

    [Fact]
    public void DecodeToBmp_Garbage_ThrowsTurboJpegException()
    {
        using var decoder = new TurboJpegDecoder();

        Assert.Throws<TurboJpegException>(() => decoder.DecodeToBmp(new byte[] { 1, 2, 3, 4 }));
    }

    [Fact]
    public void DecodeToBmp_Empty_ThrowsArgumentException()
    {
        using var decoder = new TurboJpegDecoder();

        Assert.Throws<ArgumentException>(() => decoder.DecodeToBmp(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void DecodeToBmp_AfterDispose_ThrowsObjectDisposedException()
    {
        var decoder = new TurboJpegDecoder();
        decoder.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () => decoder.DecodeToBmp(TestFixtures.Load("baseline-444.jpg")));
    }

    private static void AssertValidBmp(byte[] bmp)
    {
        Assert.Equal((byte)'B', bmp[0]);
        Assert.Equal((byte)'M', bmp[1]);
        Assert.Equal(bmp.Length, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(2)));
        Assert.Equal(TestFixtures.Width, BmpTestHelpers.Width(bmp));
        Assert.Equal(TestFixtures.Height, BmpTestHelpers.Height(bmp));
        Assert.Equal(
            BmpTestHelpers.HeaderSize + TestFixtures.Width * TestFixtures.Height * 4,
            bmp.Length);
    }

    private static void AssertBandColors(byte[] bmp, int tolerance)
    {
        AssertColor(bmp, 10, 2, 255, 0, 0, tolerance);
        AssertColor(bmp, 10, 6, 0, 255, 0, tolerance);
        AssertColor(bmp, 10, 10, 0, 0, 255, tolerance);
        AssertColor(bmp, 10, 14, 255, 255, 255, tolerance);
        AssertColor(bmp, 10, 18, 0, 0, 0, tolerance);
        AssertColor(bmp, 10, 21, 128, 128, 128, tolerance);
    }

    private static void AssertColor(byte[] bmp, int x, int y, int r, int g, int b, int tolerance)
    {
        var (actualB, actualG, actualR, actualA) = BmpTestHelpers.Pixel(bmp, x, y);

        Assert.Equal(255, actualA);
        Assert.InRange((int)actualR, r - tolerance, r + tolerance);
        Assert.InRange((int)actualG, g - tolerance, g + tolerance);
        Assert.InRange((int)actualB, b - tolerance, b + tolerance);
    }
}
