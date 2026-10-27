using System.Buffers.Binary;
using System.Numerics;

namespace TurboJpegWrapper.Internal.Encoder.Bmp;

internal static class BmpReader
{
    private const int FileHeaderSize = 14;
    private const int DibSizeFieldSize = 4;
    private const int MinimumHeaderSize = FileHeaderSize + DibSizeFieldSize;
    private const int BitfieldMaskOffset = FileHeaderSize + 40;
    private const int BitfieldMaskBytes = 12;

    internal static BmpInfo Read(ReadOnlySpan<byte> bmp)
    {
        if (bmp.Length < MinimumHeaderSize)
        {
            throw new ArgumentException(
                "The BMP buffer is too short to contain a BMP file header and the DIB header size.",
                nameof(bmp));
        }

        if (bmp[0] != (byte)'B' || bmp[1] != (byte)'M')
        {
            throw new ArgumentException("The BMP signature is missing.", nameof(bmp));
        }

        // The DIB size decides whether the file is an unsupported OS/2 core bitmap, so it
        // must be classified before the full DIB header can be required.
        var dibSizeValue = BinaryPrimitives.ReadUInt32LittleEndian(
            bmp.Slice(FileHeaderSize, DibSizeFieldSize));
        if (dibSizeValue != 40 &&
            dibSizeValue != 52 &&
            dibSizeValue != 56 &&
            dibSizeValue != 108 &&
            dibSizeValue != 124)
        {
            throw new TurboJpegUnsupportedException(
                dibSizeValue == 12
                    ? "OS/2 core BMPs (DIB header size 12) are not supported."
                    : $"BMP DIB header size {dibSizeValue} is not supported.");
        }

        var dibSize = (int)dibSizeValue;
        if (bmp.Length < FileHeaderSize + dibSize)
        {
            throw new ArgumentException(
                "The BMP buffer is too short to contain the DIB header.", nameof(bmp));
        }

        var width = BinaryPrimitives.ReadInt32LittleEndian(bmp.Slice(18, 4));
        if (width <= 0)
        {
            throw new ArgumentException("The BMP width must be positive.", nameof(bmp));
        }

        var storedHeight = BinaryPrimitives.ReadInt32LittleEndian(bmp.Slice(22, 4));
        if (storedHeight == 0 || storedHeight == int.MinValue)
        {
            throw new ArgumentException("The BMP height is invalid.", nameof(bmp));
        }

        var topDown = storedHeight < 0;
        var height = topDown ? -storedHeight : storedHeight;

        var planes = BinaryPrimitives.ReadUInt16LittleEndian(bmp.Slice(26, 2));
        if (planes != 1)
        {
            throw new ArgumentException(
                $"The BMP bit planes value {planes} is invalid; exactly one plane is required.",
                nameof(bmp));
        }

        var bitCount = BinaryPrimitives.ReadUInt16LittleEndian(bmp.Slice(28, 2));
        if (bitCount != 1 &&
            bitCount != 4 &&
            bitCount != 8 &&
            bitCount != 16 &&
            bitCount != 24 &&
            bitCount != 32)
        {
            throw new ArgumentException(
                $"The BMP bit count {bitCount} is not supported.", nameof(bmp));
        }

        var compression = BinaryPrimitives.ReadUInt32LittleEndian(bmp.Slice(30, 4));
        switch (compression)
        {
            case 0:
                break;
            case 3:
                if (bitCount != 16 && bitCount != 32)
                {
                    throw new TurboJpegUnsupportedException(
                        "BI_BITFIELDS is supported only for 16-bit and 32-bit BMPs.");
                }

                break;
            case 1:
                throw new TurboJpegUnsupportedException(
                    "RLE8-compressed BMPs are not supported.");
            case 2:
                throw new TurboJpegUnsupportedException(
                    "RLE4-compressed BMPs are not supported.");
            case 4:
                throw new TurboJpegUnsupportedException(
                    "JPEG-compressed BMPs are not supported.");
            case 5:
                throw new TurboJpegUnsupportedException(
                    "PNG-compressed BMPs are not supported.");
            default:
                throw new TurboJpegUnsupportedException(
                    $"BMP compression {compression} is not supported.");
        }

        var densityX = BinaryPrimitives.ReadInt32LittleEndian(bmp.Slice(38, 4));
        var densityY = BinaryPrimitives.ReadInt32LittleEndian(bmp.Slice(42, 4));
        var clrUsed = BinaryPrimitives.ReadUInt32LittleEndian(bmp.Slice(46, 4));

        var pixelOffsetValue = BinaryPrimitives.ReadUInt32LittleEndian(bmp.Slice(10, 4));
        var headerEnd = FileHeaderSize + dibSize;
        long minimumPixelOffset = headerEnd;
        if (compression == 3)
        {
            minimumPixelOffset = Math.Max(
                minimumPixelOffset, BitfieldMaskOffset + BitfieldMaskBytes);
        }

        if (pixelOffsetValue < minimumPixelOffset)
        {
            throw new ArgumentException(
                "The BMP pixel offset overlaps the file header, DIB header, or bitfield masks.",
                nameof(bmp));
        }

        if (pixelOffsetValue > bmp.Length)
        {
            throw new ArgumentException(
                "The BMP pixel offset is outside the buffer.", nameof(bmp));
        }

        var pixelOffset = (int)pixelOffsetValue;

        var paletteOffset = 0;
        var paletteCount = 0;
        if (bitCount <= 8)
        {
            long count = clrUsed != 0 ? clrUsed : 1u << bitCount;
            if (count > int.MaxValue)
            {
                throw new ArgumentException(
                    "The BMP palette entry count is too large.", nameof(bmp));
            }

            paletteOffset = headerEnd;
            var paletteEnd = paletteOffset + count * 4;
            if (paletteEnd > pixelOffset)
            {
                throw new ArgumentException(
                    "The BMP palette overlaps or extends past the pixel data.", nameof(bmp));
            }

            paletteCount = (int)count;
        }

        uint redMask = 0;
        uint greenMask = 0;
        uint blueMask = 0;
        if (compression == 3)
        {
            redMask = BinaryPrimitives.ReadUInt32LittleEndian(
                bmp.Slice(BitfieldMaskOffset, 4));
            greenMask = BinaryPrimitives.ReadUInt32LittleEndian(
                bmp.Slice(BitfieldMaskOffset + 4, 4));
            blueMask = BinaryPrimitives.ReadUInt32LittleEndian(
                bmp.Slice(BitfieldMaskOffset + 8, 4));

            ValidateMask(redMask, "red");
            ValidateMask(greenMask, "green");
            ValidateMask(blueMask, "blue");
            if ((redMask & greenMask) != 0 ||
                (redMask & blueMask) != 0 ||
                (greenMask & blueMask) != 0)
            {
                throw new TurboJpegUnsupportedException(
                    "BMP bitfield masks must not overlap.");
            }
        }
        else if (bitCount == 16)
        {
            redMask = 0x7C00;
            greenMask = 0x03E0;
            blueMask = 0x001F;
        }

        var strideValue = ((long)width * bitCount + 31) / 32 * 4;
        if (strideValue > int.MaxValue)
        {
            throw new ArgumentException("The BMP row stride is too large.", nameof(bmp));
        }

        var stride = (int)strideValue;
        var availableBytes = bmp.Length - pixelOffset;
        if (height > availableBytes / stride)
        {
            throw new ArgumentException("The BMP pixel data is truncated.", nameof(bmp));
        }

        return new BmpInfo(
            width,
            height,
            topDown,
            bitCount,
            compression,
            pixelOffset,
            stride,
            paletteOffset,
            paletteCount,
            redMask,
            greenMask,
            blueMask,
            densityX,
            densityY);
    }

    private static void ValidateMask(uint mask, string channel)
    {
        if (mask == 0)
        {
            throw new TurboJpegUnsupportedException(
                $"The BMP {channel} bitfield mask is empty.");
        }

        // The specified contiguity test (mask & (mask + 1)) == 0 applies to a run of ones
        // starting at bit 0; shift the field down so that standard masks such as 0xF800 pass.
        var field = mask >> BitOperations.TrailingZeroCount(mask);
        if ((field & unchecked(field + 1)) != 0)
        {
            throw new TurboJpegUnsupportedException(
                $"The BMP {channel} bitfield mask is not contiguous.");
        }
    }
}
