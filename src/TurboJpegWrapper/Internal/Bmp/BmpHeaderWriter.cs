using System.Buffers.Binary;

namespace TurboJpegWrapper.Internal.Bmp;

internal static class BmpHeaderWriter
{
    internal const int HeaderSize = 54;
    internal const int GrayscalePaletteSize = 256 * 4;

    internal static int GetBytesPerPixel(TurboJpegBmpBitDepth bitDepth) =>
        bitDepth switch
        {
            TurboJpegBmpBitDepth.Bpp8 => 1,
            TurboJpegBmpBitDepth.Bpp24 => 3,
            TurboJpegBmpBitDepth.Bpp32 => 4,
            _ => throw new ArgumentOutOfRangeException(
                nameof(bitDepth), bitDepth, "The BMP bit depth must be 8, 24, or 32.")
        };

    internal static int GetPixelOffset(TurboJpegBmpBitDepth bitDepth) =>
        bitDepth == TurboJpegBmpBitDepth.Bpp8
            ? HeaderSize + GrayscalePaletteSize
            : HeaderSize;

    internal static int GetRowStride(int width, TurboJpegBmpBitDepth bitDepth) =>
        checked((width * GetBytesPerPixel(bitDepth) + 3) & ~3);

    internal static long GetFileSize(int width, int height, TurboJpegBmpBitDepth bitDepth) =>
        checked(GetPixelOffset(bitDepth) + (long)GetRowStride(width, bitDepth) * height);

    internal static void Write(
        Span<byte> destination, int width, int height, TurboJpegBmpBitDepth bitDepth)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width), width, "The width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height), height, "The height must be greater than zero.");
        }

        var pixelOffset = GetPixelOffset(bitDepth);
        if (destination.Length < pixelOffset)
        {
            throw new ArgumentException(
                $"The destination must be at least {pixelOffset} bytes long.", nameof(destination));
        }

        var stride = GetRowStride(width, bitDepth);
        var fileSize = checked((int)GetFileSize(width, height, bitDepth));
        var imageSize = fileSize - pixelOffset;
        var bitsPerPixel = GetBytesPerPixel(bitDepth) * 8;

        destination[0] = (byte)'B';
        destination[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(destination[2..], fileSize);
        BinaryPrimitives.WriteInt32LittleEndian(destination[6..], 0);
        BinaryPrimitives.WriteInt32LittleEndian(destination[10..], pixelOffset);
        BinaryPrimitives.WriteInt32LittleEndian(destination[14..], 40);
        BinaryPrimitives.WriteInt32LittleEndian(destination[18..], width);
        BinaryPrimitives.WriteInt32LittleEndian(destination[22..], height);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[26..], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[28..], (ushort)bitsPerPixel);
        BinaryPrimitives.WriteInt32LittleEndian(destination[30..], 0);
        BinaryPrimitives.WriteInt32LittleEndian(destination[34..], imageSize);
        BinaryPrimitives.WriteInt32LittleEndian(destination[38..], 2835);
        BinaryPrimitives.WriteInt32LittleEndian(destination[42..], 2835);
        BinaryPrimitives.WriteInt32LittleEndian(
            destination[46..], bitDepth == TurboJpegBmpBitDepth.Bpp8 ? 256 : 0);
        BinaryPrimitives.WriteInt32LittleEndian(destination[50..], 0);

        if (bitDepth == TurboJpegBmpBitDepth.Bpp8)
        {
            var palette = destination.Slice(HeaderSize, GrayscalePaletteSize);
            for (var i = 0; i < 256; i++)
            {
                var entry = palette.Slice(i * 4, 4);
                entry[0] = (byte)i;
                entry[1] = (byte)i;
                entry[2] = (byte)i;
                entry[3] = 0;
            }
        }
    }

    internal static void ClearPadding(
        Span<byte> destination, int width, int height, TurboJpegBmpBitDepth bitDepth)
    {
        var rowBytes = checked(width * GetBytesPerPixel(bitDepth));
        var stride = GetRowStride(width, bitDepth);
        if (stride == rowBytes)
        {
            return;
        }

        var pixelOffset = GetPixelOffset(bitDepth);
        var padding = stride - rowBytes;
        for (var y = 0; y < height; y++)
        {
            var rowOffset = checked(pixelOffset + y * stride + rowBytes);
            destination.Slice(rowOffset, padding).Clear();
        }
    }
}
