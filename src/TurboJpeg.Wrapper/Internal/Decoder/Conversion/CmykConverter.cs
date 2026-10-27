using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace TurboJpegWrapper.Internal.Decoder.Conversion;

internal static class CmykConverter
{
    private static readonly Vector128<byte> s_cMask = Vector128.Create(
        (byte)0, 4, 8, 12, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80);
    private static readonly Vector128<byte> s_mMask = Vector128.Create(
        (byte)1, 5, 9, 13, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80);
    private static readonly Vector128<byte> s_yMask = Vector128.Create(
        (byte)2, 6, 10, 14, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80);
    private static readonly Vector128<byte> s_kMask = Vector128.Create(
        (byte)3, 7, 11, 15, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80);
    private static readonly Vector128<byte> s_alpha = Vector128.Create(
        (byte)0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255);
    private static readonly Vector128<byte> s_bLow = Vector128.Create(
        (byte)0, 0x80, 0x80, 0x80, 1, 0x80, 0x80, 0x80, 2, 0x80, 0x80, 0x80, 3, 0x80, 0x80, 0x80);
    private static readonly Vector128<byte> s_gLow = Vector128.Create(
        (byte)0x80, 0, 0x80, 0x80, 0x80, 1, 0x80, 0x80, 0x80, 2, 0x80, 0x80, 0x80, 3, 0x80, 0x80);
    private static readonly Vector128<byte> s_rLow = Vector128.Create(
        (byte)0x80, 0x80, 0, 0x80, 0x80, 0x80, 1, 0x80, 0x80, 0x80, 2, 0x80, 0x80, 0x80, 3, 0x80);
    private static readonly Vector128<byte> s_bHigh = Vector128.Create(
        (byte)4, 0x80, 0x80, 0x80, 5, 0x80, 0x80, 0x80, 6, 0x80, 0x80, 0x80, 7, 0x80, 0x80, 0x80);
    private static readonly Vector128<byte> s_gHigh = Vector128.Create(
        (byte)0x80, 4, 0x80, 0x80, 0x80, 5, 0x80, 0x80, 0x80, 6, 0x80, 0x80, 0x80, 7, 0x80, 0x80);
    private static readonly Vector128<byte> s_rHigh = Vector128.Create(
        (byte)0x80, 0x80, 4, 0x80, 0x80, 0x80, 5, 0x80, 0x80, 0x80, 6, 0x80, 0x80, 0x80, 7, 0x80);
    private static readonly Vector128<byte> s_bgrPack = Vector128.Create(
        (byte)0, 1, 2, 4, 5, 6, 8, 9, 10, 12, 13, 14, 0x80, 0x80, 0x80, 0x80);
    private static readonly Vector128<ushort> s_round = Vector128.Create((ushort)128);
    private static readonly Vector128<ushort> s_lumaR = Vector128.Create((ushort)77);
    private static readonly Vector128<ushort> s_lumaG = Vector128.Create((ushort)150);
    private static readonly Vector128<ushort> s_lumaB = Vector128.Create((ushort)29);
    private static readonly Vector128<ushort> s_zero = Vector128<ushort>.Zero;

    internal static void ToBgra(ReadOnlySpan<byte> cmyk, Span<byte> bgra, int pixelCount)
    {
        ref var sourceRef = ref MemoryMarshal.GetReference(cmyk);
        ref var destinationRef = ref MemoryMarshal.GetReference(bgra);
        var i = 0;

        if (Vector128.IsHardwareAccelerated)
        {
            for (; i <= pixelCount - 8; i += 8)
            {
                var (r, g, b) = ConvertEightPixels(ref sourceRef, i);

                var b8 = Vector128.Narrow(b, s_zero);
                var g8 = Vector128.Narrow(g, s_zero);
                var r8 = Vector128.Narrow(r, s_zero);

                var low = Vector128.Shuffle(b8, s_bLow)
                    | Vector128.Shuffle(g8, s_gLow)
                    | Vector128.Shuffle(r8, s_rLow)
                    | s_alpha;
                var high = Vector128.Shuffle(b8, s_bHigh)
                    | Vector128.Shuffle(g8, s_gHigh)
                    | Vector128.Shuffle(r8, s_rHigh)
                    | s_alpha;

                Unsafe.WriteUnaligned(ref Unsafe.Add(ref destinationRef, i * 4), low);
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref destinationRef, (i * 4) + 16), high);
            }
        }

        for (; i < pixelCount; i++)
        {
            var offset = i * 4;
            var k = Unsafe.Add(ref sourceRef, offset + 3);
            var c = Divide(Unsafe.Add(ref sourceRef, offset) * k);
            var m = Divide(Unsafe.Add(ref sourceRef, offset + 1) * k);
            var y = Divide(Unsafe.Add(ref sourceRef, offset + 2) * k);
            ref var destination = ref Unsafe.Add(ref destinationRef, offset);

            destination = y;
            Unsafe.Add(ref destination, 1) = m;
            Unsafe.Add(ref destination, 2) = c;
            Unsafe.Add(ref destination, 3) = 255;
        }
    }

    internal static void ToBgr(ReadOnlySpan<byte> cmyk, Span<byte> bgr, int pixelCount)
    {
        ref var sourceRef = ref MemoryMarshal.GetReference(cmyk);
        ref var destinationRef = ref MemoryMarshal.GetReference(bgr);
        var i = 0;

        if (Vector128.IsHardwareAccelerated)
        {
            for (; i <= pixelCount - 8; i += 8)
            {
                var (r, g, b) = ConvertEightPixels(ref sourceRef, i);

                var b8 = Vector128.Narrow(b, s_zero);
                var g8 = Vector128.Narrow(g, s_zero);
                var r8 = Vector128.Narrow(r, s_zero);

                var low = Vector128.Shuffle(
                    Vector128.Shuffle(b8, s_bLow)
                    | Vector128.Shuffle(g8, s_gLow)
                    | Vector128.Shuffle(r8, s_rLow),
                    s_bgrPack);
                var high = Vector128.Shuffle(
                    Vector128.Shuffle(b8, s_bHigh)
                    | Vector128.Shuffle(g8, s_gHigh)
                    | Vector128.Shuffle(r8, s_rHigh),
                    s_bgrPack);

                ref var destination = ref Unsafe.Add(ref destinationRef, i * 3);
                Unsafe.WriteUnaligned(ref destination, Vector128.AsUInt64(low).GetElement(0));
                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(ref destination, 8), Vector128.AsUInt32(low).GetElement(2));
                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(ref destination, 12), Vector128.AsUInt64(high).GetElement(0));
                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(ref destination, 20), Vector128.AsUInt32(high).GetElement(2));
            }
        }

        for (; i < pixelCount; i++)
        {
            var offset = i * 4;
            var k = Unsafe.Add(ref sourceRef, offset + 3);
            var c = Divide(Unsafe.Add(ref sourceRef, offset) * k);
            var m = Divide(Unsafe.Add(ref sourceRef, offset + 1) * k);
            var y = Divide(Unsafe.Add(ref sourceRef, offset + 2) * k);
            ref var destination = ref Unsafe.Add(ref destinationRef, i * 3);

            destination = y;
            Unsafe.Add(ref destination, 1) = m;
            Unsafe.Add(ref destination, 2) = c;
        }
    }

    internal static void ToGray(ReadOnlySpan<byte> cmyk, Span<byte> gray, int pixelCount)
    {
        ref var sourceRef = ref MemoryMarshal.GetReference(cmyk);
        ref var destinationRef = ref MemoryMarshal.GetReference(gray);
        var i = 0;

        if (Vector128.IsHardwareAccelerated)
        {
            for (; i <= pixelCount - 8; i += 8)
            {
                var (r, g, b) = ConvertEightPixels(ref sourceRef, i);
                var luma = (r * s_lumaR + g * s_lumaG + b * s_lumaB + s_round) >> 8;
                var bytes = Vector128.Narrow(luma, s_zero);

                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(ref destinationRef, i), Vector128.AsUInt64(bytes).GetElement(0));
            }
        }

        for (; i < pixelCount; i++)
        {
            var offset = i * 4;
            var k = Unsafe.Add(ref sourceRef, offset + 3);
            var r = Divide(Unsafe.Add(ref sourceRef, offset) * k);
            var g = Divide(Unsafe.Add(ref sourceRef, offset + 1) * k);
            var b = Divide(Unsafe.Add(ref sourceRef, offset + 2) * k);

            Unsafe.Add(ref destinationRef, i) = (byte)((77 * r + 150 * g + 29 * b + 128) >> 8);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (Vector128<ushort> R, Vector128<ushort> G, Vector128<ushort> B) ConvertEightPixels(
        ref byte sourceRef, int index)
    {
        var low = Vector128.LoadUnsafe(ref sourceRef, (nuint)(index * 4));
        var high = Vector128.LoadUnsafe(ref sourceRef, (nuint)((index * 4) + 16));
        var k = WidenEight(low, high, s_kMask);

        return (
            Divide(WidenEight(low, high, s_cMask) * k),
            Divide(WidenEight(low, high, s_mMask) * k),
            Divide(WidenEight(low, high, s_yMask) * k));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<ushort> WidenEight(
        Vector128<byte> low, Vector128<byte> high, Vector128<byte> mask)
    {
        var bytes = Vector128.Shuffle(low, mask)
            | Vector128.ShiftLeft(Vector128.Shuffle(high, mask).AsUInt64(), 32).AsByte();

        return Vector128.WidenLower(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<ushort> Divide(Vector128<ushort> product)
    {
        var rounded = product + s_round;

        return (rounded + Vector128.ShiftRightLogical(rounded, 8)) >> 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Divide(int product) => (byte)((product + 127) / 255);
}
