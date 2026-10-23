using System.Buffers.Binary;
using TurboJpegWrapper;
using TurboJpegWrapper.Internal.Bmp;

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
        Assert.Equal(0, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(6)));
        Assert.Equal(54, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(10)));
        Assert.Equal(40, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(14)));
        Assert.Equal(35, BmpTestHelpers.Width(bmp));
        Assert.Equal(23, BmpTestHelpers.Height(bmp));
        Assert.Equal(1, BinaryPrimitives.ReadUInt16LittleEndian(bmp.AsSpan(26)));
        Assert.Equal(32, BinaryPrimitives.ReadUInt16LittleEndian(bmp.AsSpan(28)));
        Assert.Equal(0, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(30)));
        Assert.Equal(3220, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(34)));
        Assert.Equal(2835, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(38)));
        Assert.Equal(2835, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(42)));
        Assert.Equal(0, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(46)));
        Assert.Equal(0, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(50)));
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
    public void DecodeToBmp_NativeDecodeFailure_ThrowsTurboJpegException()
    {
        var jpeg = TestFixtures.Load("baseline-444.jpg");
        using var decoder = new TurboJpegDecoder(new TurboJpegDecodeOptions(maxPixels: 1));

        var info = decoder.GetImageInfo(jpeg);
        Assert.Equal(TestFixtures.Width, info.Width);
        Assert.Equal(TestFixtures.Height, info.Height);

        var exception = Assert.Throws<TurboJpegException>(() => decoder.DecodeToBmp(jpeg));
        Assert.Contains("large", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DecodeToBmp_TooLarge_ThrowsTurboJpegImageTooLargeException()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = PatchJpegDimensions(TestFixtures.Load("baseline-444.jpg"), 65500, 65500);

        var info = decoder.GetImageInfo(jpeg);
        Assert.Equal(65500, info.Width);
        Assert.Equal(65500, info.Height);

        Assert.Throws<TurboJpegImageTooLargeException>(() => decoder.DecodeToBmp(jpeg));
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

    [Fact]
    public void BmpHeaderWriter_Write_ShortDestination_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => BmpHeaderWriter.Write(new byte[53], 35, 23));
    }

    [Theory]
    [InlineData(0, 23)]
    [InlineData(-1, 23)]
    [InlineData(35, 0)]
    [InlineData(35, -1)]
    public void BmpHeaderWriter_Write_NonPositiveDimensions_ThrowsArgumentOutOfRangeException(
        int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BmpHeaderWriter.Write(new byte[BmpTestHelpers.HeaderSize], width, height));
    }

    private static byte[] PatchJpegDimensions(byte[] jpeg, int width, int height)
    {
        var patched = (byte[])jpeg.Clone();
        var offset = 2;

        while (offset + 4 <= patched.Length)
        {
            if (patched[offset] != 0xFF)
            {
                break;
            }

            var marker = patched[offset + 1];
            if (marker is 0xC0 or 0xC1 or 0xC2)
            {
                if (offset + 9 > patched.Length)
                {
                    break;
                }

                BinaryPrimitives.WriteUInt16BigEndian(patched.AsSpan(offset + 5, 2), checked((ushort)height));
                BinaryPrimitives.WriteUInt16BigEndian(patched.AsSpan(offset + 7, 2), checked((ushort)width));
                return patched;
            }

            if (marker is 0xD8 or 0xD9 or 0xDA or 0x01 ||
                (marker is >= 0xD0 and <= 0xD7))
            {
                break;
            }

            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(patched.AsSpan(offset + 2, 2));
            if (segmentLength < 2)
            {
                break;
            }

            offset += 2 + segmentLength;
        }

        throw new InvalidOperationException("The JPEG does not contain a supported SOF marker.");
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
