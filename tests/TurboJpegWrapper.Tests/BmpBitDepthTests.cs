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
}
