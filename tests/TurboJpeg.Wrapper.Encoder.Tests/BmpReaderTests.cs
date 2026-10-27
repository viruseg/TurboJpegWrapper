using System.Buffers.Binary;
using TurboJpegWrapper;
using TurboJpegWrapper.Internal.Encoder.Bmp;

namespace TurboJpegWrapper.Encoder.Tests;

public sealed class BmpReaderTests
{
    [Fact]
    public void Read_24BitBottomUp_ParsesHeader()
    {
        var bmp = BmpBuilder.Create24(
            5,
            3,
            (x, y) => ((byte)x, (byte)y, (byte)(x + y)),
            topDown: false,
            ppmX: 2835,
            ppmY: 1181);

        var info = BmpReader.Read(bmp);

        Assert.Equal(5, info.Width);
        Assert.Equal(3, info.Height);
        Assert.False(info.TopDown);
        Assert.Equal(24, info.BitCount);
        Assert.Equal(0u, info.Compression);
        Assert.Equal(54, info.PixelOffset);
        Assert.Equal(16, info.Stride);
        Assert.Equal(0, info.PaletteOffset);
        Assert.Equal(0, info.PaletteCount);
        Assert.Equal(2835, info.DensityX);
        Assert.Equal(1181, info.DensityY);
    }

    [Fact]
    public void Read_32BitTopDown_ParsesNegativeHeight()
    {
        var bmp = BmpBuilder.Create32(2, 3, (_, _) => ((byte)1, (byte)2, (byte)3, (byte)4), topDown: true);

        Assert.Equal(-3, BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(22)));

        var info = BmpReader.Read(bmp);

        Assert.Equal(2, info.Width);
        Assert.Equal(3, info.Height);
        Assert.True(info.TopDown);
        Assert.Equal(32, info.BitCount);
        Assert.Equal(0u, info.Compression);
        Assert.Equal(54, info.PixelOffset);
        Assert.Equal(8, info.Stride);
        Assert.Equal(0u, info.RedMask);
        Assert.Equal(0u, info.GreenMask);
        Assert.Equal(0u, info.BlueMask);
    }

    [Fact]
    public void Read_32BitBottomUp_RecomputesPositiveHeight()
    {
        var bmp = BmpBuilder.Create32(2, 3, (_, _) => ((byte)1, (byte)2, (byte)3, (byte)4));

        var info = BmpReader.Read(bmp);

        Assert.Equal(3, info.Height);
        Assert.False(info.TopDown);
        Assert.Equal(8, info.Stride);
    }

    [Fact]
    public void Read_Palette8WithoutClrUsed_UsesFullPalette()
    {
        var palette = new (byte B, byte G, byte R)[] { (1, 2, 3), (4, 5, 6), (7, 8, 9) };
        var bmp = BmpBuilder.CreatePalette8(3, 2, (x, y) => (byte)((x + y) % 3), palette);

        var info = BmpReader.Read(bmp);

        Assert.Equal(8, info.BitCount);
        Assert.Equal(0u, info.Compression);
        Assert.Equal(54, info.PaletteOffset);
        Assert.Equal(256, info.PaletteCount);
        Assert.Equal(54 + 256 * 4, info.PixelOffset);
        Assert.Equal(4, info.Stride);
    }

    [Fact]
    public void Read_Palette8WithClrUsed_UsesDeclaredCount()
    {
        var palette = new (byte B, byte G, byte R)[7];
        var bmp = BmpBuilder.CreatePalette8(3, 2, (x, _) => (byte)x, palette, clrUsed: 7);

        var info = BmpReader.Read(bmp);

        Assert.Equal(54, info.PaletteOffset);
        Assert.Equal(7, info.PaletteCount);
        Assert.Equal(54 + 7 * 4, info.PixelOffset);
        Assert.Equal(4, info.Stride);
    }

    [Fact]
    public void Read_16BitBitfields565_ReadsMasks()
    {
        var bmp = BmpBuilder.Create16(
            2,
            2,
            (x, y) => (ushort)(x + y),
            redMask: 0xF800,
            greenMask: 0x07E0,
            blueMask: 0x001F,
            bitfields: true);

        var info = BmpReader.Read(bmp);

        Assert.Equal(16, info.BitCount);
        Assert.Equal(3u, info.Compression);
        Assert.Equal(0xF800u, info.RedMask);
        Assert.Equal(0x07E0u, info.GreenMask);
        Assert.Equal(0x001Fu, info.BlueMask);
        Assert.Equal(66, info.PixelOffset);
        Assert.Equal(4, info.Stride);
    }

    [Fact]
    public void Read_16BitRgb_DefaultsTo555Masks()
    {
        var bmp = BmpBuilder.Create16(
            2,
            2,
            (_, _) => (ushort)0,
            redMask: 0,
            greenMask: 0,
            blueMask: 0,
            bitfields: false);

        var info = BmpReader.Read(bmp);

        Assert.Equal(16, info.BitCount);
        Assert.Equal(0u, info.Compression);
        Assert.Equal(0x7C00u, info.RedMask);
        Assert.Equal(0x03E0u, info.GreenMask);
        Assert.Equal(0x001Fu, info.BlueMask);
        Assert.Equal(54, info.PixelOffset);
        Assert.Equal(4, info.Stride);
    }

    [Fact]
    public void Read_32BitBitfieldsStandardMasks_ReadsMasks()
    {
        var bmp = BmpBuilder.CreateRaw(
            bitCount: 32,
            compression: 3,
            pixelOffset: 66,
            redMask: 0x00FF0000,
            greenMask: 0x0000FF00,
            blueMask: 0x000000FF);

        var info = BmpReader.Read(bmp);

        Assert.Equal(32, info.BitCount);
        Assert.Equal(3u, info.Compression);
        Assert.Equal(0x00FF0000u, info.RedMask);
        Assert.Equal(0x0000FF00u, info.GreenMask);
        Assert.Equal(0x000000FFu, info.BlueMask);
        Assert.Equal(66, info.PixelOffset);
        Assert.Equal(8, info.Stride);
    }

    [Theory]
    [InlineData(4, 16)]
    [InlineData(1, 2)]
    public void Read_SmallPaletteBitCounts_ParsePaletteLayout(int bitCount, int expectedPaletteCount)
    {
        var palette = new (byte B, byte G, byte R)[expectedPaletteCount];
        var bmp = bitCount == 4
            ? BmpBuilder.CreatePalette4(3, 2, (x, y) => (byte)(x + y), palette)
            : BmpBuilder.CreatePalette1(3, 2, (x, y) => (byte)(x + y), palette);

        var info = BmpReader.Read(bmp);

        Assert.Equal(bitCount, info.BitCount);
        Assert.Equal(0u, info.Compression);
        Assert.Equal(54, info.PaletteOffset);
        Assert.Equal(expectedPaletteCount, info.PaletteCount);
        Assert.Equal(54 + expectedPaletteCount * 4, info.PixelOffset);
        Assert.Equal(4, info.Stride);
    }

    [Theory]
    [InlineData(52)]
    [InlineData(56)]
    [InlineData(108)]
    [InlineData(124)]
    public void Read_ExtendedDibHeaderSizes_AreAccepted(int dibSize)
    {
        var bmp = BmpBuilder.Create24(2, 2, (_, _) => ((byte)0, (byte)0, (byte)0), dibSize: dibSize);

        var info = BmpReader.Read(bmp);

        Assert.Equal(2, info.Width);
        Assert.Equal(2, info.Height);
        Assert.Equal(24, info.BitCount);
        Assert.Equal(14 + dibSize, info.PixelOffset);
        Assert.Equal(8, info.Stride);
    }

    [Theory]
    [InlineData(52, 16)]
    [InlineData(56, 16)]
    [InlineData(108, 32)]
    [InlineData(124, 32)]
    public void Read_ExtendedDibWithBitfields_ReadsMasksAndLayout(int dibSize, int bitCount)
    {
        const int width = 2;
        const int height = 2;
        var stride = bitCount == 16 ? 4 : 8;
        var pixelOffset = 14 + dibSize;
        var totalLength = pixelOffset + stride * height;
        var redMask = bitCount == 16 ? 0xF800u : 0x00FF0000u;
        var greenMask = bitCount == 16 ? 0x07E0u : 0x0000FF00u;
        var blueMask = bitCount == 16 ? 0x001Fu : 0x000000FFu;
        var bmp = BmpBuilder.CreateRaw(
            width: width,
            height: height,
            bitCount: bitCount,
            compression: 3,
            dibSize: dibSize,
            pixelOffset: pixelOffset,
            totalLength: totalLength,
            redMask: redMask,
            greenMask: greenMask,
            blueMask: blueMask);

        var info = BmpReader.Read(bmp);

        Assert.Equal(width, info.Width);
        Assert.Equal(height, info.Height);
        Assert.Equal(bitCount, info.BitCount);
        Assert.Equal(3u, info.Compression);
        Assert.Equal(pixelOffset, info.PixelOffset);
        Assert.Equal(stride, info.Stride);
        Assert.Equal(redMask, info.RedMask);
        Assert.Equal(greenMask, info.GreenMask);
        Assert.Equal(blueMask, info.BlueMask);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(53)]
    public void Read_EmptyOrShortBuffer_Throws(int length)
    {
        var valid = BmpBuilder.Create24(2, 2, (_, _) => ((byte)0, (byte)0, (byte)0));

        Assert.Throws<ArgumentException>(() => BmpReader.Read(valid[..length]));
    }

    [Fact]
    public void Read_MissingSignature_Throws()
    {
        var bmp = BmpBuilder.CreateRaw(validSignature: false);

        Assert.Throws<ArgumentException>(() => BmpReader.Read(bmp));
    }

    [Fact]
    public void Read_ZeroWidth_Throws()
    {
        var bmp = BmpBuilder.CreateRaw(width: 0);

        Assert.Throws<ArgumentException>(() => BmpReader.Read(bmp));
    }

    [Fact]
    public void Read_ZeroHeight_Throws()
    {
        var bmp = BmpBuilder.CreateRaw(height: 0);

        Assert.Throws<ArgumentException>(() => BmpReader.Read(bmp));
    }

    [Fact]
    public void Read_MinimumHeight_Throws()
    {
        var bmp = BmpBuilder.CreateRaw(height: int.MinValue);

        Assert.Throws<ArgumentException>(() => BmpReader.Read(bmp));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Read_InvalidPlanes_Throws(int planes)
    {
        var bmp = BmpBuilder.CreateRaw(planes: planes);

        Assert.Throws<ArgumentException>(() => BmpReader.Read(bmp));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(12)]
    public void Read_UnsupportedBitCount_Throws(int bitCount)
    {
        var bmp = BmpBuilder.CreateRaw(bitCount: bitCount);

        Assert.Throws<ArgumentException>(() => BmpReader.Read(bmp));
    }

    [Theory]
    [InlineData(50)]
    [InlineData(500)]
    public void Read_PixelOffsetOutsideHeaderOrBuffer_Throws(int pixelOffset)
    {
        var bmp = BmpBuilder.CreateRaw(pixelOffset: pixelOffset, totalLength: 100);

        Assert.Throws<ArgumentException>(() => BmpReader.Read(bmp));
    }

    [Fact]
    public void Read_PixelDataBeyondBuffer_Throws()
    {
        var bmp = BmpBuilder.CreateRaw(width: 4, height: 4, totalLength: 66);

        Assert.Throws<ArgumentException>(() => BmpReader.Read(bmp));
    }

    [Fact]
    public void Read_HostileWidth_Throws()
    {
        var bmp = BmpBuilder.CreateRaw(width: int.MaxValue, height: 1);

        Assert.Throws<ArgumentException>(() => BmpReader.Read(bmp));
    }

    [Fact]
    public void Read_HostileHeight_Throws()
    {
        var bmp = BmpBuilder.CreateRaw(width: 2, height: int.MaxValue);

        Assert.Throws<ArgumentException>(() => BmpReader.Read(bmp));
    }

    [Fact]
    public void Read_PaletteBeyondPixelOffset_Throws()
    {
        var bmp = BmpBuilder.CreateRaw(
            bitCount: 8,
            clrUsed: 256,
            pixelOffset: 54,
            totalLength: 200);

        Assert.Throws<ArgumentException>(() => BmpReader.Read(bmp));
    }

    [Fact]
    public void Read_Rle8_ThrowsUnsupported()
    {
        var bmp = BmpBuilder.CreateRle8(4, 4);

        Assert.Throws<TurboJpegUnsupportedException>(() => BmpReader.Read(bmp));
    }

    [Theory]
    [InlineData(4, 2u)]
    [InlineData(24, 4u)]
    [InlineData(24, 5u)]
    [InlineData(24, 6u)]
    public void Read_UnsupportedCompression_Throws(int bitCount, uint compression)
    {
        var bmp = BmpBuilder.CreateRaw(bitCount: bitCount, compression: compression);

        Assert.Throws<TurboJpegUnsupportedException>(() => BmpReader.Read(bmp));
    }

    [Fact]
    public void Read_BitfieldsAt24Bit_ThrowsUnsupported()
    {
        var bmp = BmpBuilder.CreateRaw(bitCount: 24, compression: 3);

        Assert.Throws<TurboJpegUnsupportedException>(() => BmpReader.Read(bmp));
    }

    [Fact]
    public void Read_Os2CoreHeader_ThrowsUnsupported()
    {
        var bmp = BmpBuilder.CreateOs2Core(1, 7);

        Assert.Equal(54, bmp.Length);
        Assert.Throws<TurboJpegUnsupportedException>(() => BmpReader.Read(bmp));
    }

    [Fact]
    public void Read_Sub54ByteOs2Core_ThrowsUnsupported()
    {
        var bmp = BmpBuilder.CreateOs2Core(1, 1);

        Assert.Equal(30, bmp.Length);
        Assert.True(bmp.Length < 54);
        Assert.Throws<TurboJpegUnsupportedException>(() => BmpReader.Read(bmp));
    }

    [Fact]
    public void Read_NonContiguousMask_ThrowsUnsupported()
    {
        var bmp = BmpBuilder.Create16(
            2,
            2,
            (_, _) => (ushort)0,
            redMask: 0xF800,
            greenMask: 0x07E0,
            blueMask: 0x0011,
            bitfields: true);

        Assert.Throws<TurboJpegUnsupportedException>(() => BmpReader.Read(bmp));
    }

    [Fact]
    public void Read_OverlappingMasks_ThrowsUnsupported()
    {
        var bmp = BmpBuilder.Create16(
            2,
            2,
            (_, _) => (ushort)0,
            redMask: 0xFF00,
            greenMask: 0x0F00,
            blueMask: 0x00FF,
            bitfields: true);

        Assert.Throws<TurboJpegUnsupportedException>(() => BmpReader.Read(bmp));
    }
}
