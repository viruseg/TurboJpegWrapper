using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace TurboJpegWrapper.Decoder.Internal.Conversion;

internal static class PrecisionScale
{
    private const int NonTemporalSampleThreshold = 8192;

    private static readonly byte[]?[] s_luts = CreateLuts();

    internal static byte[] GetLut(int precision) => s_luts[precision]!;

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
        ReadOnlySpan<short> source,
        Span<byte> destination,
        int sampleCount,
        int precision,
        bool opaqueAlpha = false)
    {
        if (precision != 12)
        {
            DownscaleExact(source, destination, sampleCount, precision, opaqueAlpha);
            return;
        }

        fixed (short* sourcePtr = source)
        fixed (byte* destinationPtr = destination)
        {
            var i = 0;

            if (Vector256.IsHardwareAccelerated)
            {
                var offset = Vector256.Create((ushort)8);
                var maximum = Vector256.Create((ushort)255);
                var nonTemporal = opaqueAlpha
                    && Avx.IsSupported
                    && sampleCount >= NonTemporalSampleThreshold;

                if (nonTemporal)
                {
                    for (; i < sampleCount && ((nuint)(destinationPtr + i) & 31) != 0; i++)
                    {
                        destinationPtr[i] = (byte)Math.Min(((ushort)sourcePtr[i] + 8) >> 4, 255);

                        if ((i & 3) == 3)
                        {
                            destinationPtr[i] = 255;
                        }
                    }
                }

                var alphaMask = GetAlphaMask256(i & 3);

                for (; i <= sampleCount - 64; i += 64)
                {
                    var a = Vector256.Min(
                        Vector256.ShiftRightLogical(Vector256.Load((ushort*)sourcePtr + i) + offset, 4),
                        maximum);
                    var b = Vector256.Min(
                        Vector256.ShiftRightLogical(Vector256.Load((ushort*)sourcePtr + i + 16) + offset, 4),
                        maximum);
                    var c = Vector256.Min(
                        Vector256.ShiftRightLogical(Vector256.Load((ushort*)sourcePtr + i + 32) + offset, 4),
                        maximum);
                    var d = Vector256.Min(
                        Vector256.ShiftRightLogical(Vector256.Load((ushort*)sourcePtr + i + 48) + offset, 4),
                        maximum);
                    var low = Vector256.Narrow(a, b);
                    var high = Vector256.Narrow(c, d);

                    if (opaqueAlpha)
                    {
                        low |= alphaMask;
                        high |= alphaMask;
                    }

                    if (nonTemporal)
                    {
                        Avx.StoreAlignedNonTemporal(destinationPtr + i, low);
                        Avx.StoreAlignedNonTemporal(destinationPtr + i + 32, high);
                    }
                    else
                    {
                        Unsafe.WriteUnaligned(destinationPtr + i, low);
                        Unsafe.WriteUnaligned(destinationPtr + i + 32, high);
                    }
                }
            }

            var offset128 = Vector128.Create((ushort)8);
            var maximum128 = Vector128.Create((ushort)255);
            var alphaMask128 = GetAlphaMask128(i & 3);

            for (; i <= sampleCount - 16; i += 16)
            {
                var low = Vector128.ShiftRightLogical(
                    Vector128.Load((ushort*)sourcePtr + i) + offset128, 4);
                var high = Vector128.ShiftRightLogical(
                    Vector128.Load((ushort*)sourcePtr + i + 8) + offset128, 4);
                var packed = Vector128.Narrow(
                    Vector128.Min(low, maximum128), Vector128.Min(high, maximum128));

                if (opaqueAlpha)
                {
                    packed |= alphaMask128;
                }

                Unsafe.WriteUnaligned(destinationPtr + i, packed);
            }

            for (; i < sampleCount; i++)
            {
                destinationPtr[i] = (byte)Math.Min(((ushort)sourcePtr[i] + 8) >> 4, 255);

                if (opaqueAlpha && (i & 3) == 3)
                {
                    destinationPtr[i] = 255;
                }
            }
        }
    }

    internal static unsafe void Downscale16(
        ReadOnlySpan<ushort> source,
        Span<byte> destination,
        int sampleCount,
        int precision,
        bool opaqueAlpha = false)
    {
        if (precision != 16)
        {
            DownscaleExact(source, destination, sampleCount, precision, opaqueAlpha);
            return;
        }

        fixed (ushort* sourcePtr = source)
        fixed (byte* destinationPtr = destination)
        {
            var i = 0;

            if (Vector256.IsHardwareAccelerated)
            {
                var alphaMask = Vector256.Create(0xFF000000u);

                for (; i <= sampleCount - 64; i += 64)
                {
                    var a = Vector256.ShiftRightLogical(Vector256.Load(sourcePtr + i), 8);
                    var b = Vector256.ShiftRightLogical(Vector256.Load(sourcePtr + i + 16), 8);
                    var c = Vector256.ShiftRightLogical(Vector256.Load(sourcePtr + i + 32), 8);
                    var d = Vector256.ShiftRightLogical(Vector256.Load(sourcePtr + i + 48), 8);
                    var low = Vector256.Narrow(a, b);
                    var high = Vector256.Narrow(c, d);

                    if (opaqueAlpha)
                    {
                        low = Vector256.AsByte(Vector256.AsUInt32(low) | alphaMask);
                        high = Vector256.AsByte(Vector256.AsUInt32(high) | alphaMask);
                    }

                    Unsafe.WriteUnaligned(destinationPtr + i, low);
                    Unsafe.WriteUnaligned(destinationPtr + i + 32, high);
                }
            }

            var alphaMask128 = Vector128.Create(0xFF000000u);

            for (; i <= sampleCount - 16; i += 16)
            {
                var low = Vector128.ShiftRightLogical(Vector128.Load(sourcePtr + i), 8);
                var high = Vector128.ShiftRightLogical(Vector128.Load(sourcePtr + i + 8), 8);
                var packed = Vector128.Narrow(low, high);

                if (opaqueAlpha)
                {
                    packed = Vector128.AsByte(Vector128.AsUInt32(packed) | alphaMask128);
                }

                Unsafe.WriteUnaligned(destinationPtr + i, packed);
            }

            for (; i < sampleCount; i++)
            {
                destinationPtr[i] = (byte)(sourcePtr[i] >> 8);

                if (opaqueAlpha && (i & 3) == 3)
                {
                    destinationPtr[i] = 255;
                }
            }
        }
    }

    private static void DownscaleExact(
        ReadOnlySpan<short> source,
        Span<byte> destination,
        int sampleCount,
        int precision,
        bool opaqueAlpha)
    {
        var lut = s_luts[precision]!;
        var max = (1 << precision) - 1;
        ref var sourceRef = ref Unsafe.As<short, ushort>(ref MemoryMarshal.GetReference(source));
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        ref var lutRef = ref MemoryMarshal.GetReference(lut.AsSpan());
        var i = 0;

        for (; i <= sampleCount - 8; i += 8)
        {
            Unsafe.Add(ref destinationRef, i) =
                Unsafe.Add(ref lutRef, Math.Min(Unsafe.Add(ref sourceRef, i), max));
            Unsafe.Add(ref destinationRef, i + 1) =
                Unsafe.Add(ref lutRef, Math.Min(Unsafe.Add(ref sourceRef, i + 1), max));
            Unsafe.Add(ref destinationRef, i + 2) =
                Unsafe.Add(ref lutRef, Math.Min(Unsafe.Add(ref sourceRef, i + 2), max));
            Unsafe.Add(ref destinationRef, i + 3) =
                Unsafe.Add(ref lutRef, Math.Min(Unsafe.Add(ref sourceRef, i + 3), max));
            Unsafe.Add(ref destinationRef, i + 4) =
                Unsafe.Add(ref lutRef, Math.Min(Unsafe.Add(ref sourceRef, i + 4), max));
            Unsafe.Add(ref destinationRef, i + 5) =
                Unsafe.Add(ref lutRef, Math.Min(Unsafe.Add(ref sourceRef, i + 5), max));
            Unsafe.Add(ref destinationRef, i + 6) =
                Unsafe.Add(ref lutRef, Math.Min(Unsafe.Add(ref sourceRef, i + 6), max));
            Unsafe.Add(ref destinationRef, i + 7) =
                Unsafe.Add(ref lutRef, Math.Min(Unsafe.Add(ref sourceRef, i + 7), max));

            if (opaqueAlpha)
            {
                Unsafe.Add(ref destinationRef, i + 3) = 255;
                Unsafe.Add(ref destinationRef, i + 7) = 255;
            }
        }

        for (; i < sampleCount; i++)
        {
            Unsafe.Add(ref destinationRef, i) =
                Unsafe.Add(ref lutRef, Math.Min(Unsafe.Add(ref sourceRef, i), max));

            if (opaqueAlpha && (i & 3) == 3)
            {
                Unsafe.Add(ref destinationRef, i) = 255;
            }
        }
    }

    private static void DownscaleExact(
        ReadOnlySpan<ushort> source,
        Span<byte> destination,
        int sampleCount,
        int precision,
        bool opaqueAlpha)
    {
        var lut = s_luts[precision]!;
        var max = (1 << precision) - 1;
        ref var sourceRef = ref MemoryMarshal.GetReference(source);
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        ref var lutRef = ref MemoryMarshal.GetReference(lut.AsSpan());
        var i = 0;

        for (; i <= sampleCount - 8; i += 8)
        {
            Unsafe.Add(ref destinationRef, i) =
                Unsafe.Add(ref lutRef, Math.Min(Unsafe.Add(ref sourceRef, i), max));
            Unsafe.Add(ref destinationRef, i + 1) =
                Unsafe.Add(ref lutRef, Math.Min(Unsafe.Add(ref sourceRef, i + 1), max));
            Unsafe.Add(ref destinationRef, i + 2) =
                Unsafe.Add(ref lutRef, Math.Min(Unsafe.Add(ref sourceRef, i + 2), max));
            Unsafe.Add(ref destinationRef, i + 3) =
                Unsafe.Add(ref lutRef, Math.Min(Unsafe.Add(ref sourceRef, i + 3), max));
            Unsafe.Add(ref destinationRef, i + 4) =
                Unsafe.Add(ref lutRef, Math.Min(Unsafe.Add(ref sourceRef, i + 4), max));
            Unsafe.Add(ref destinationRef, i + 5) =
                Unsafe.Add(ref lutRef, Math.Min(Unsafe.Add(ref sourceRef, i + 5), max));
            Unsafe.Add(ref destinationRef, i + 6) =
                Unsafe.Add(ref lutRef, Math.Min(Unsafe.Add(ref sourceRef, i + 6), max));
            Unsafe.Add(ref destinationRef, i + 7) =
                Unsafe.Add(ref lutRef, Math.Min(Unsafe.Add(ref sourceRef, i + 7), max));

            if (opaqueAlpha)
            {
                Unsafe.Add(ref destinationRef, i + 3) = 255;
                Unsafe.Add(ref destinationRef, i + 7) = 255;
            }
        }

        for (; i < sampleCount; i++)
        {
            Unsafe.Add(ref destinationRef, i) =
                Unsafe.Add(ref lutRef, Math.Min(Unsafe.Add(ref sourceRef, i), max));

            if (opaqueAlpha && (i & 3) == 3)
            {
                Unsafe.Add(ref destinationRef, i) = 255;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<byte> GetAlphaMask256(int phase) => phase switch
    {
        0 => Vector256.Create(0xFF000000u).AsByte(),
        1 => Vector256.Create(0x00FF0000u).AsByte(),
        2 => Vector256.Create(0x0000FF00u).AsByte(),
        _ => Vector256.Create(0x000000FFu).AsByte(),
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<byte> GetAlphaMask128(int phase) => phase switch
    {
        0 => Vector128.Create(0xFF000000u).AsByte(),
        1 => Vector128.Create(0x00FF0000u).AsByte(),
        2 => Vector128.Create(0x0000FF00u).AsByte(),
        _ => Vector128.Create(0x000000FFu).AsByte(),
    };

    private static byte[]?[] CreateLuts()
    {
        var luts = new byte[]?[17];

        for (var precision = 2; precision <= 15; precision++)
        {
            if (precision is not (12 or 16))
            {
                luts[precision] = CreateLut(precision);
            }
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
