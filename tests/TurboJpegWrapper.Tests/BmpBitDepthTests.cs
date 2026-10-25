using System.Buffers.Binary;
using TurboJpegWrapper;
using TurboJpegWrapper.Internal.Bmp;

namespace TurboJpegWrapper.Tests;

public sealed class BmpBitDepthTests
{
    [Theory]
    [InlineData(35, TurboJpegBmpBitDepth.Bpp8, 36)]
    [InlineData(35, TurboJpegBmpBitDepth.Bpp24, 108)]
    [InlineData(18, TurboJpegBmpBitDepth.Bpp24, 56)]
    [InlineData(5, TurboJpegBmpBitDepth.Bpp24, 16)]
    [InlineData(35, TurboJpegBmpBitDepth.Bpp32, 140)]
    public void GetRowStride_ReturnsAlignedStride(
        int width, TurboJpegBmpBitDepth bitDepth, int expected)
    {
        Assert.Equal(expected, BmpHeaderWriter.GetRowStride(width, bitDepth));
    }

    [Theory]
    [InlineData(35, 23, TurboJpegBmpBitDepth.Bpp8, 1906)]
    [InlineData(35, 23, TurboJpegBmpBitDepth.Bpp24, 2538)]
    [InlineData(35, 23, TurboJpegBmpBitDepth.Bpp32, 3274)]
    public void GetFileSize_IncludesHeaderStrideAndPalette(
        int width, int height, TurboJpegBmpBitDepth bitDepth, long expected)
    {
        Assert.Equal(expected, BmpHeaderWriter.GetFileSize(width, height, bitDepth));
    }

    [Fact]
    public void Write_24Bit_WritesAlignedHeader()
    {
        var bmp = new byte[2538];

        BmpHeaderWriter.Write(bmp, 35, 23, TurboJpegBmpBitDepth.Bpp24);

        Assert.Equal(2538, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(2)));
        Assert.Equal(54, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(10)));
        Assert.Equal(35, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(18)));
        Assert.Equal(23, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(22)));
        Assert.Equal(24, BinaryPrimitives.ReadUInt16LittleEndian(bmp.AsSpan(28)));
        Assert.Equal(0, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(30)));
        Assert.Equal(2484, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(34)));
        Assert.Equal(0, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(46)));
    }

    [Fact]
    public void Write_8Bit_WritesHeaderAndGrayscalePalette()
    {
        var bmp = new byte[1906];

        BmpHeaderWriter.Write(bmp, 35, 23, TurboJpegBmpBitDepth.Bpp8);

        Assert.Equal(1906, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(2)));
        Assert.Equal(1078, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(10)));
        Assert.Equal(8, BinaryPrimitives.ReadUInt16LittleEndian(bmp.AsSpan(28)));
        Assert.Equal(828, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(34)));
        Assert.Equal(256, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(46)));

        for (var i = 0; i < 256; i++)
        {
            var offset = BmpHeaderWriter.HeaderSize + i * 4;
            Assert.Equal((byte)i, bmp[offset]);
            Assert.Equal((byte)i, bmp[offset + 1]);
            Assert.Equal((byte)i, bmp[offset + 2]);
            Assert.Equal(0, bmp[offset + 3]);
        }
    }

    [Fact]
    public void Write_8Bit_ShortDestination_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => BmpHeaderWriter.Write(new byte[1077], 35, 23, TurboJpegBmpBitDepth.Bpp8));
    }

    [Fact]
    public void ClearPadding_24Bit_ZeroesOnlyPadding()
    {
        var buffer = new byte[BmpHeaderWriter.GetPixelOffset(TurboJpegBmpBitDepth.Bpp24) + 16 * 2];
        Array.Fill(buffer, (byte)0xFF);

        BmpHeaderWriter.ClearPadding(buffer, 5, 2, TurboJpegBmpBitDepth.Bpp24);

        var offset = BmpHeaderWriter.GetPixelOffset(TurboJpegBmpBitDepth.Bpp24);
        for (var y = 0; y < 2; y++)
        {
            for (var i = 0; i < 15; i++)
            {
                Assert.Equal(0xFF, buffer[offset + y * 16 + i]);
            }

            Assert.Equal(0, buffer[offset + y * 16 + 15]);
        }
    }

    private const int FixtureWidth = 35;
    private const int FixtureHeight = 23;

    private static byte[] Decode(string fixture, TurboJpegBmpBitDepth bitDepth)
    {
        using var decoder = new TurboJpegDecoder();
        return decoder.DecodeToBmp(
            TestFixtures.Load(fixture),
            new TurboJpegDecodeOptions(bmpBitDepth: bitDepth));
    }

    [Fact]
    public void DecodeToBmp_24Bit_WritesHeaderAndSize()
    {
        var bmp = Decode("baseline-444.jpg", TurboJpegBmpBitDepth.Bpp24);

        Assert.Equal(2538, bmp.Length);
        Assert.Equal(2538, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(2)));
        Assert.Equal(54, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(10)));
        Assert.Equal(24, BinaryPrimitives.ReadUInt16LittleEndian(bmp.AsSpan(28)));
        Assert.Equal(0, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(30)));
        Assert.Equal(2484, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(34)));
        Assert.Equal(0, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(46)));
    }

    [Fact]
    public void DecodeToBmp_24Bit_PaddingIsZero()
    {
        var bmp = Decode("baseline-444.jpg", TurboJpegBmpBitDepth.Bpp24);
        var stride = BmpTestHelpers.RowStride(bmp);
        var rowBytes = FixtureWidth * 3;
        var offset = BmpTestHelpers.PixelOffset(bmp);

        for (var y = 0; y < FixtureHeight; y++)
        {
            for (var i = rowBytes; i < stride; i++)
            {
                Assert.Equal(0, bmp[offset + y * stride + i]);
            }
        }
    }

    [Fact]
    public void DecodeToBmp_24Bit_ColorsAndBottomUp()
    {
        var bmp = Decode("baseline-444.jpg", TurboJpegBmpBitDepth.Bpp24);

        Assert.InRange(bmp[54], 120, 136);
        Assert.InRange(bmp[55], 120, 136);
        Assert.InRange(bmp[56], 120, 136);

        var (b, g, r, a) = BmpTestHelpers.Pixel(bmp, 10, 2);
        Assert.Equal(255, a);
        Assert.InRange((int)r, 247, 255);
        Assert.InRange((int)g, 0, 8);
        Assert.InRange((int)b, 0, 8);
    }

    [Fact]
    public void DecodeToBmp_24Bit_ChannelsMatch32Bit()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = TestFixtures.Load("baseline-420.jpg");
        var bmp32 = decoder.DecodeToBmp(jpeg);
        var bmp24 = decoder.DecodeToBmp(
            jpeg, new TurboJpegDecodeOptions(bmpBitDepth: TurboJpegBmpBitDepth.Bpp24));

        for (var y = 0; y < FixtureHeight; y++)
        {
            for (var x = 0; x < FixtureWidth; x++)
            {
                var p32 = BmpTestHelpers.Pixel(bmp32, x, y);
                var p24 = BmpTestHelpers.Pixel(bmp24, x, y);

                Assert.InRange((int)p24.B, p32.B - 1, p32.B + 1);
                Assert.InRange((int)p24.G, p32.G - 1, p32.G + 1);
                Assert.InRange((int)p24.R, p32.R - 1, p32.R + 1);
                Assert.Equal(255, p24.A);
            }
        }
    }

    [Fact]
    public void DecodeToBmp_24Bit_OpensWithSystemDrawing()
    {
        var bmp = Decode("baseline-444.jpg", TurboJpegBmpBitDepth.Bpp24);

        using var bitmap = new System.Drawing.Bitmap(new MemoryStream(bmp));
        Assert.Equal(System.Drawing.Imaging.PixelFormat.Format24bppRgb, bitmap.PixelFormat);
        Assert.Equal(FixtureWidth, bitmap.Width);
        Assert.Equal(FixtureHeight, bitmap.Height);

        var pixel = bitmap.GetPixel(10, 2);
        Assert.Equal(255, pixel.A);
        Assert.InRange(pixel.R, 247, 255);
        Assert.InRange(pixel.G, 0, 8);
        Assert.InRange(pixel.B, 0, 8);
    }

    [Fact]
    public void DecodeToBmp_8Bit_WritesHeaderAndSize()
    {
        var bmp = Decode("grayscale.jpg", TurboJpegBmpBitDepth.Bpp8);

        Assert.Equal(1906, bmp.Length);
        Assert.Equal(1906, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(2)));
        Assert.Equal(1078, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(10)));
        Assert.Equal(8, BinaryPrimitives.ReadUInt16LittleEndian(bmp.AsSpan(28)));
        Assert.Equal(828, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(34)));
        Assert.Equal(256, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(46)));
    }

    [Fact]
    public void DecodeToBmp_8Bit_OpensAsIndexedAndIsGrayscale()
    {
        var bmp = Decode("baseline-444.jpg", TurboJpegBmpBitDepth.Bpp8);

        using var bitmap = new System.Drawing.Bitmap(new MemoryStream(bmp));
        Assert.Equal(System.Drawing.Imaging.PixelFormat.Format8bppIndexed, bitmap.PixelFormat);
        Assert.Equal(FixtureWidth, bitmap.Width);
        Assert.Equal(FixtureHeight, bitmap.Height);

        for (var y = 0; y < FixtureHeight; y++)
        {
            for (var x = 0; x < FixtureWidth; x++)
            {
                var (b, g, r, a) = BmpTestHelpers.Pixel(bmp, x, y);
                Assert.Equal(r, g);
                Assert.Equal(g, b);
                Assert.Equal(255, a);
            }
        }

        var redBand = bitmap.GetPixel(10, 2);
        Assert.InRange(redBand.R, 68, 84);
    }

    [Fact]
    public void DecodeToBmp_SwitchingDepths_OnSameDecoder_ProducesCorrectHeaders()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = TestFixtures.Load("baseline-444.jpg");

        var bmp32 = decoder.DecodeToBmp(jpeg);
        var bmp24 = decoder.DecodeToBmp(
            jpeg, new TurboJpegDecodeOptions(bmpBitDepth: TurboJpegBmpBitDepth.Bpp24));
        var bmp8 = decoder.DecodeToBmp(
            jpeg, new TurboJpegDecodeOptions(bmpBitDepth: TurboJpegBmpBitDepth.Bpp8));
        var bmp32Again = decoder.DecodeToBmp(jpeg);

        Assert.Equal(32, BmpTestHelpers.BitsPerPixel(bmp32));
        Assert.Equal(24, BmpTestHelpers.BitsPerPixel(bmp24));
        Assert.Equal(8, BmpTestHelpers.BitsPerPixel(bmp8));
        Assert.Equal(32, BmpTestHelpers.BitsPerPixel(bmp32Again));
        Assert.Equal(bmp32, bmp32Again);
    }

    [Fact]
    public void DecodeToBmp_24Bit_HugeDimensions_ThrowsImageTooLarge()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = TestFixtures.Load("baseline-444.jpg");
        JpegPatcher.SetDimensions(jpeg, 65500, 65500);

        var exception = Assert.Throws<TurboJpegImageTooLargeException>(
            () => decoder.DecodeToBmp(
                jpeg,
                new TurboJpegDecodeOptions(
                    maxPixels: 0,
                    bmpBitDepth: TurboJpegBmpBitDepth.Bpp24)));

        Assert.Contains(
            "maximum array length", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
