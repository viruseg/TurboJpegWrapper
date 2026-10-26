using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;
using TurboJpegWrapper.Internal.Native;

namespace TurboJpegWrapper.Encoder.Internal.Bmp;

internal static class BmpPixelConverter
{
    private const int PaletteEntries = 256;
    private const int PaletteEntrySize = 4;

    private const uint StandardRedMask = 0x00FF0000;
    private const uint StandardGreenMask = 0x0000FF00;
    private const uint StandardBlueMask = 0x000000FF;

    internal static BmpPixels Convert(
        ReadOnlySpan<byte> bmp, in BmpInfo info, bool grayscaleOutput)
    {
        if (info.BitCount == 24)
        {
            return Direct(bmp, info, TurboJpegNativeApi.PixelFormatBgr);
        }

        if (info.BitCount == 32 && IsDirectBgrx(info))
        {
            return Direct(bmp, info, TurboJpegNativeApi.PixelFormatBgrx);
        }

        if (grayscaleOutput &&
            info.BitCount == 8 &&
            info.Compression == 0 &&
            HasIdentityGrayscalePalette(bmp, info))
        {
            return Direct(bmp, info, TurboJpegNativeApi.PixelFormatGray);
        }

        return ConvertPixels(bmp, info, grayscaleOutput);
    }

    private static BmpPixels Direct(ReadOnlySpan<byte> bmp, in BmpInfo info, int pixelFormat) =>
        new(
            bmp[info.PixelOffset..],
            info.Stride,
            pixelFormat,
            !info.TopDown,
            rented: null);

    private static bool IsDirectBgrx(in BmpInfo info)
    {
        if (info.Compression == 0)
        {
            return true;
        }

        return info.Compression == 3 &&
            info.RedMask == StandardRedMask &&
            info.GreenMask == StandardGreenMask &&
            info.BlueMask == StandardBlueMask;
    }

    private static bool HasIdentityGrayscalePalette(ReadOnlySpan<byte> bmp, in BmpInfo info)
    {
        // All 256 possible indices must map to their own value for the packed indices to be
        // usable as GRAY bytes directly.
        if (info.PaletteCount < PaletteEntries)
        {
            return false;
        }

        var paletteOffset = info.PaletteOffset;
        for (var i = 0; i < PaletteEntries; i++)
        {
            var entry = paletteOffset + i * PaletteEntrySize;
            var value = (byte)i;
            if (bmp[entry] != value || bmp[entry + 1] != value || bmp[entry + 2] != value)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasGrayscalePalette(ReadOnlySpan<byte> bmp, in BmpInfo info)
    {
        // Only a neutral palette (all three channels equal per entry) carries luma
        // directly. A colour palette's red channel is not luma, and lossless mode ignores
        // the requested colorspace, so colour palettes must stay BGR and let TurboJPEG
        // perform the colour-to-gray conversion.
        var count = Math.Min(info.PaletteCount, PaletteEntries);
        for (var i = 0; i < count; i++)
        {
            var entry = info.PaletteOffset + i * PaletteEntrySize;
            if (bmp[entry] != bmp[entry + 1] || bmp[entry + 1] != bmp[entry + 2])
            {
                return false;
            }
        }

        return true;
    }

    private static BmpPixels ConvertPixels(
        ReadOnlySpan<byte> bmp, in BmpInfo info, bool grayscaleOutput)
    {
        var gray = grayscaleOutput && info.BitCount <= 8 && HasGrayscalePalette(bmp, info);
        var bytesPerPixel = gray ? 1 : 3;

        var pitch = (long)info.Width * bytesPerPixel;
        if (pitch > Array.MaxLength || info.Height > Array.MaxLength / pitch)
        {
            throw new TurboJpegImageTooLargeException(
                $"The converted pixel buffer for a {info.Width}x{info.Height} " +
                $"{info.BitCount}-bit BMP exceeds the maximum array length of " +
                $"{Array.MaxLength} bytes.");
        }

        var byteCount = checked((int)(pitch * info.Height));
        var pitchValue = (int)pitch;
        var rented = ArrayPool<byte>.Shared.Rent(byteCount);
        var destination = rented.AsSpan(0, byteCount);

        if (info.BitCount <= 8)
        {
            ConvertPalette(bmp, info, destination, pitchValue, gray);
        }
        else if (info.BitCount == 16)
        {
            Convert16(bmp, info, destination, pitchValue);
        }
        else
        {
            Convert32(bmp, info, destination, pitchValue);
        }

        var pixelFormat = gray
            ? TurboJpegNativeApi.PixelFormatGray
            : TurboJpegNativeApi.PixelFormatBgr;
        return new BmpPixels(destination, pitchValue, pixelFormat, !info.TopDown, rented);
    }

    private static void ConvertPalette(
        ReadOnlySpan<byte> bmp, in BmpInfo info, Span<byte> destination, int pitch, bool gray)
    {
        Span<byte> lut = stackalloc byte[PaletteEntries * 3];
        lut.Clear();

        var count = Math.Min(info.PaletteCount, PaletteEntries);
        var paletteOffset = info.PaletteOffset;
        for (var i = 0; i < count; i++)
        {
            var entry = paletteOffset + i * PaletteEntrySize;
            if (gray)
            {
                lut[i] = bmp[entry + 2];
            }
            else
            {
                var offset = i * 3;
                lut[offset] = bmp[entry];
                lut[offset + 1] = bmp[entry + 1];
                lut[offset + 2] = bmp[entry + 2];
            }
        }

        switch (info.BitCount)
        {
            case 8:
                ConvertPalette8(bmp, info, destination, pitch, lut, gray);
                break;
            case 4:
                ConvertPalette4(bmp, info, destination, pitch, lut, gray);
                break;
            default:
                ConvertPalette1(bmp, info, destination, pitch, lut, gray);
                break;
        }
    }

    private static void ConvertPalette8(
        ReadOnlySpan<byte> bmp,
        in BmpInfo info,
        Span<byte> destination,
        int pitch,
        ReadOnlySpan<byte> lut,
        bool gray)
    {
        for (var y = 0; y < info.Height; y++)
        {
            var source = bmp.Slice(info.PixelOffset + y * info.Stride, info.Stride);
            var target = destination.Slice(y * pitch, pitch);
            for (var x = 0; x < info.Width; x++)
            {
                StorePalettePixel(target, x, source[x], lut, gray);
            }
        }
    }

    private static void ConvertPalette4(
        ReadOnlySpan<byte> bmp,
        in BmpInfo info,
        Span<byte> destination,
        int pitch,
        ReadOnlySpan<byte> lut,
        bool gray)
    {
        for (var y = 0; y < info.Height; y++)
        {
            var source = bmp.Slice(info.PixelOffset + y * info.Stride, info.Stride);
            var target = destination.Slice(y * pitch, pitch);
            for (var x = 0; x < info.Width; x++)
            {
                var packed = source[x >> 1];
                var index = (x & 1) == 0 ? packed >> 4 : packed & 0x0F;
                StorePalettePixel(target, x, index, lut, gray);
            }
        }
    }

    private static void ConvertPalette1(
        ReadOnlySpan<byte> bmp,
        in BmpInfo info,
        Span<byte> destination,
        int pitch,
        ReadOnlySpan<byte> lut,
        bool gray)
    {
        for (var y = 0; y < info.Height; y++)
        {
            var source = bmp.Slice(info.PixelOffset + y * info.Stride, info.Stride);
            var target = destination.Slice(y * pitch, pitch);
            for (var x = 0; x < info.Width; x++)
            {
                var index = (source[x >> 3] >> (7 - (x & 7))) & 1;
                StorePalettePixel(target, x, index, lut, gray);
            }
        }
    }

    private static void StorePalettePixel(
        Span<byte> target, int x, int index, ReadOnlySpan<byte> lut, bool gray)
    {
        if (gray)
        {
            target[x] = lut[index];
            return;
        }

        var offset = x * 3;
        var lutOffset = index * 3;
        target[offset] = lut[lutOffset];
        target[offset + 1] = lut[lutOffset + 1];
        target[offset + 2] = lut[lutOffset + 2];
    }

    private static void Convert16(
        ReadOnlySpan<byte> bmp, in BmpInfo info, Span<byte> destination, int pitch)
    {
        var red = new ChannelField(info.RedMask);
        var green = new ChannelField(info.GreenMask);
        var blue = new ChannelField(info.BlueMask);

        for (var y = 0; y < info.Height; y++)
        {
            var source = bmp.Slice(info.PixelOffset + y * info.Stride, info.Stride);
            var target = destination.Slice(y * pitch, pitch);
            for (var x = 0; x < info.Width; x++)
            {
                var sample = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(x * 2, 2));
                StoreBgr(target, x, blue, green, red, sample);
            }
        }
    }

    private static void Convert32(
        ReadOnlySpan<byte> bmp, in BmpInfo info, Span<byte> destination, int pitch)
    {
        var red = new ChannelField(info.RedMask);
        var green = new ChannelField(info.GreenMask);
        var blue = new ChannelField(info.BlueMask);

        for (var y = 0; y < info.Height; y++)
        {
            var source = bmp.Slice(info.PixelOffset + y * info.Stride, info.Stride);
            var target = destination.Slice(y * pitch, pitch);
            for (var x = 0; x < info.Width; x++)
            {
                var sample = BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(x * 4, 4));
                StoreBgr(target, x, blue, green, red, sample);
            }
        }
    }

    private static void StoreBgr(
        Span<byte> target,
        int x,
        ChannelField blue,
        ChannelField green,
        ChannelField red,
        uint sample)
    {
        var offset = x * 3;
        target[offset] = blue.Extract(sample);
        target[offset + 1] = green.Extract(sample);
        target[offset + 2] = red.Extract(sample);
    }

    // BmpReader rejects zero and non-contiguous masks, so the field width is nonzero.
    private readonly struct ChannelField
    {
        private readonly uint _mask;
        private readonly int _shift;
        private readonly uint _max;
        private readonly byte[]? _lut;

        internal ChannelField(uint mask)
        {
            _mask = mask;
            _shift = BitOperations.TrailingZeroCount(mask);
            _max = mask >> _shift;
            _lut = _max <= ushort.MaxValue ? CreateLut(_max) : null;
        }

        internal byte Extract(uint sample)
        {
            var value = (sample & _mask) >> _shift;
            var lut = _lut;

            return lut is null
                ? (byte)(((ulong)value * 255 + (_max / 2)) / _max)
                : lut[(int)value];
        }

        private static byte[] CreateLut(uint max)
        {
            var lut = new byte[(int)max + 1];
            for (var value = 0; value <= (int)max; value++)
            {
                lut[value] = (byte)(((ulong)value * 255 + (max / 2)) / max);
            }

            return lut;
        }
    }
}
