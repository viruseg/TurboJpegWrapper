using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace TurboJpegWrapper.Decoder.Internal.Conversion;

internal static class PrecisionScale
{
    private static readonly byte[]?[] s_highPrecisionLuts = CreateHighPrecisionLuts();

    internal static byte[] GetLut(int precision) => CreateLut(precision);

    internal static void ScaleLowPrecisionInPlace(
        Span<byte> pixels, int pixelCount, int channels, bool alphaLast, byte[] lut)
    {
        var colorChannels = alphaLast ? channels - 1 : channels;
        ref var pixelsRef = ref MemoryMarshal.GetReference(pixels);

        for (var i = 0; i < pixelCount; i++)
        {
            var offset = i * channels;

            for (var channel = 0; channel < colorChannels; channel++)
            {
                ref var sample = ref Unsafe.Add(ref pixelsRef, offset + channel);
                sample = lut[sample];
            }

            if (alphaLast)
            {
                Unsafe.Add(ref pixelsRef, offset + channels - 1) = 255;
            }
        }
    }

    internal static unsafe void Downscale12(
        ReadOnlySpan<short> source, Span<byte> destination, int sampleCount, int precision)
    {
        if (precision != 12)
        {
            var lut = s_highPrecisionLuts[precision]!;
            var max = (1 << precision) - 1;

            for (var i = 0; i < sampleCount; i++)
            {
                destination[i] = lut[Math.Min(source[i], max)];
            }

            return;
        }

        fixed (short* sourcePtr = source)
        fixed (byte* destinationPtr = destination)
        {
            var offset = Vector128.Create((ushort)8);
            var maximum = Vector128.Create((ushort)255);
            var i = 0;

            for (; i <= sampleCount - 16; i += 16)
            {
                var low = Vector128.ShiftRightLogical(
                    Vector128.Load((ushort*)sourcePtr + i) + offset, 4);
                var high = Vector128.ShiftRightLogical(
                    Vector128.Load((ushort*)sourcePtr + i + 8) + offset, 4);
                Unsafe.WriteUnaligned(
                    destinationPtr + i,
                    Vector128.Narrow(Vector128.Min(low, maximum), Vector128.Min(high, maximum)));
            }

            for (; i < sampleCount; i++)
            {
                destinationPtr[i] = (byte)Math.Min(((ushort)sourcePtr[i] + 8) >> 4, 255);
            }
        }
    }

    internal static unsafe void Downscale16(
        ReadOnlySpan<ushort> source, Span<byte> destination, int sampleCount, int precision)
    {
        if (precision != 16)
        {
            var lut = s_highPrecisionLuts[precision]!;
            var max = (1 << precision) - 1;

            for (var i = 0; i < sampleCount; i++)
            {
                destination[i] = lut[Math.Min(source[i], max)];
            }

            return;
        }

        fixed (ushort* sourcePtr = source)
        fixed (byte* destinationPtr = destination)
        {
            var i = 0;

            for (; i <= sampleCount - 16; i += 16)
            {
                var low = Vector128.ShiftRightLogical(Vector128.Load(sourcePtr + i), 8);
                var high = Vector128.ShiftRightLogical(Vector128.Load(sourcePtr + i + 8), 8);
                Unsafe.WriteUnaligned(
                    destinationPtr + i, Vector128.Narrow(low, high));
            }

            for (; i < sampleCount; i++)
            {
                destinationPtr[i] = (byte)(sourcePtr[i] >> 8);
            }
        }
    }

    internal static void ForceOpaqueAlpha(Span<byte> bgra, int pixelCount)
    {
        ref var bgraRef = ref MemoryMarshal.GetReference(bgra);
        var i = 0;

        if (Vector128.IsHardwareAccelerated)
        {
            ref var pixelsRef = ref Unsafe.As<byte, uint>(ref bgraRef);
            var mask = Vector128.Create(0xFF000000u);

            for (; i <= pixelCount - 4; i += 4)
            {
                var pixels = Vector128.LoadUnsafe(ref pixelsRef, (nuint)i);
                Vector128.StoreUnsafe(pixels | mask, ref pixelsRef, (nuint)i);
            }
        }

        for (; i < pixelCount; i++)
        {
            Unsafe.Add(ref bgraRef, (i * 4) + 3) = 255;
        }
    }

    private static byte[]?[] CreateHighPrecisionLuts()
    {
        var luts = new byte[]?[17];
        ReadOnlySpan<int> precisions = [9, 10, 11, 13, 14, 15];

        foreach (var precision in precisions)
        {
            luts[precision] = CreateLut(precision);
        }

        return luts;
    }

    private static byte[] CreateLut(int precision)
    {
        var max = (1 << precision) - 1;
        var lut = new byte[precision >= 9 ? 1 << precision : 256];

        for (var value = 0; value <= max; value++)
        {
            lut[value] = (byte)((value * 255 + (max / 2)) / max);
        }

        return lut;
    }
}
