using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TurboJpegWrapper.Decoder.Internal;

namespace TurboJpegWrapper.Decoder.Internal.Bmp;

internal static class BmpHeaderWriter
{
    internal const int HeaderSize = 54;
    internal const int GrayscalePaletteSize = 256 * 4;
    internal const int DefaultPixelsPerMeter = 2835;

    private static readonly byte[] s_template = CreateTemplate();

    internal static int ToPixelsPerMeter(int density, TurboJpegDensityUnits units)
    {
        if (density <= 0)
        {
            return DefaultPixelsPerMeter;
        }

        return units switch
        {
            TurboJpegDensityUnits.PixelsPerInch =>
                checked((int)((density * 10_000L + 127) / 254)),
            TurboJpegDensityUnits.PixelsPerCentimeter => checked(density * 100),
            _ => DefaultPixelsPerMeter
        };
    }

    internal static int GetBytesPerPixel(TurboJpegBmpBitDepth bitDepth)
    {
        switch (bitDepth)
        {
            case TurboJpegBmpBitDepth.Bpp8:
                return 1;
            case TurboJpegBmpBitDepth.Bpp24:
                return 3;
            case TurboJpegBmpBitDepth.Bpp32:
                return 4;
            default:
                ThrowHelper.ThrowBmpBitDepthOutOfRange(nameof(bitDepth), bitDepth);
                return 0;
        }
    }

    internal static int GetPixelOffset(TurboJpegBmpBitDepth bitDepth) =>
        bitDepth == TurboJpegBmpBitDepth.Bpp8
            ? HeaderSize + GrayscalePaletteSize
            : HeaderSize;

    internal static int GetRowStride(int width, TurboJpegBmpBitDepth bitDepth) =>
        checked((width * GetBytesPerPixel(bitDepth) + 3) & ~3);

    internal static long GetFileSize(int width, int height, TurboJpegBmpBitDepth bitDepth) =>
        checked(GetPixelOffset(bitDepth) + (long)GetRowStride(width, bitDepth) * height);

    internal static void Write(
        Span<byte> destination,
        int width,
        int height,
        TurboJpegBmpBitDepth bitDepth,
        int pixelsPerMeterX = DefaultPixelsPerMeter,
        int pixelsPerMeterY = DefaultPixelsPerMeter)
    {
        if (width <= 0)
        {
            ThrowHelper.ThrowWidthNotPositive(width);
        }

        if (height <= 0)
        {
            ThrowHelper.ThrowHeightNotPositive(height);
        }

        var pixelOffset = GetPixelOffset(bitDepth);
        if (destination.Length < pixelOffset)
        {
            ThrowHelper.ThrowDestinationTooSmall(pixelOffset);
        }

        var fileSize = checked((int)GetFileSize(width, height, bitDepth));
        var imageSize = fileSize - pixelOffset;
        var bitsPerPixel = GetBytesPerPixel(bitDepth) * 8;
        var templateLength = bitDepth == TurboJpegBmpBitDepth.Bpp8 ? pixelOffset : HeaderSize;

        s_template.AsSpan(0, templateLength).CopyTo(destination);

        BinaryPrimitives.WriteInt32LittleEndian(destination[2..], fileSize);
        BinaryPrimitives.WriteInt32LittleEndian(destination[10..], pixelOffset);
        BinaryPrimitives.WriteInt32LittleEndian(destination[18..], width);
        BinaryPrimitives.WriteInt32LittleEndian(destination[22..], height);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[28..], (ushort)bitsPerPixel);
        BinaryPrimitives.WriteInt32LittleEndian(destination[34..], imageSize);
        BinaryPrimitives.WriteInt32LittleEndian(destination[38..], pixelsPerMeterX);
        BinaryPrimitives.WriteInt32LittleEndian(destination[42..], pixelsPerMeterY);
        BinaryPrimitives.WriteInt32LittleEndian(
            destination[46..], bitDepth == TurboJpegBmpBitDepth.Bpp8 ? 256 : 0);
    }

    private static byte[] CreateTemplate()
    {
        var template = new byte[HeaderSize + GrayscalePaletteSize];
        template[0] = (byte)'B';
        template[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(template.AsSpan(14), 40);
        BinaryPrimitives.WriteUInt16LittleEndian(template.AsSpan(26), 1);
        BinaryPrimitives.WriteInt32LittleEndian(template.AsSpan(38), DefaultPixelsPerMeter);
        BinaryPrimitives.WriteInt32LittleEndian(template.AsSpan(42), DefaultPixelsPerMeter);
        BinaryPrimitives.WriteInt32LittleEndian(template.AsSpan(46), 256);

        var palette = template.AsSpan(HeaderSize, GrayscalePaletteSize);
        for (var i = 0; i < 256; i++)
        {
            var entry = palette.Slice(i * 4, 4);
            entry[0] = (byte)i;
            entry[1] = (byte)i;
            entry[2] = (byte)i;
            entry[3] = 0;
        }

        return template;
    }

    internal static unsafe void ClearPadding(
        Span<byte> destination, int width, int height, TurboJpegBmpBitDepth bitDepth)
    {
        var rowBytes = checked(width * GetBytesPerPixel(bitDepth));
        var stride = GetRowStride(width, bitDepth);
        if (stride == rowBytes)
        {
            return;
        }

        var padding = stride - rowBytes;
        var offset = checked(GetPixelOffset(bitDepth) + rowBytes);
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);

        if (padding == 3)
        {
            for (var y = 0; y < height; y++)
            {
                ref var target = ref Unsafe.Add(ref destinationRef, offset);
                Unsafe.WriteUnaligned(ref target, (ushort)0);
                Unsafe.Add(ref target, 2) = 0;
                offset += stride;
            }
        }
        else if (padding == 2)
        {
            for (var y = 0; y < height; y++)
            {
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref destinationRef, offset), (ushort)0);
                offset += stride;
            }
        }
        else
        {
            for (var y = 0; y < height; y++)
            {
                Unsafe.Add(ref destinationRef, offset) = 0;
                offset += stride;
            }
        }
    }
}
