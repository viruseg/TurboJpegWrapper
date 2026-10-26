using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace TurboJpegWrapper.Decoder.Internal.Conversion;

internal static class CmykConverter
{
    private static readonly Vector128<byte> s_lowFourBytes =
        Vector128.Create(0xFFFFFFFFu, 0u, 0u, 0u).AsByte();
    private static readonly Vector128<byte> s_cMask = Vector128.Create(
        (byte)0, 4, 8, 12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    private static readonly Vector128<byte> s_mMask = Vector128.Create(
        (byte)1, 5, 9, 13, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    private static readonly Vector128<byte> s_yMask = Vector128.Create(
        (byte)2, 6, 10, 14, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    private static readonly Vector128<byte> s_kMask = Vector128.Create(
        (byte)3, 7, 11, 15, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    private static readonly Vector128<byte> s_bgraLayout = Vector128.Create(
        (byte)0, 0, 0, 255,
        0, 0, 0, 255,
        0, 0, 0, 255,
        0, 0, 0, 255);
    private static readonly Vector128<byte> s_bLayout = Vector128.Create(
        (byte)0, 4, 4, 4,
        1, 4, 4, 4,
        2, 4, 4, 4,
        3, 4, 4, 4);
    private static readonly Vector128<byte> s_gLayout = Vector128.Create(
        (byte)4, 0, 4, 4,
        4, 1, 4, 4,
        4, 2, 4, 4,
        4, 3, 4, 4);
    private static readonly Vector128<byte> s_rLayout = Vector128.Create(
        (byte)4, 4, 0, 4,
        4, 4, 1, 4,
        4, 4, 2, 4,
        4, 4, 3, 4);
    private static readonly Vector128<byte> s_bgrPackLayout = Vector128.Create(
        (byte)0, 1, 2, 4, 5, 6, 8, 9, 10, 12, 13, 14, 15, 15, 15, 15);
    private static readonly Vector128<ushort> s_round = Vector128.Create((ushort)127);
    private static readonly Vector128<ushort> s_one = Vector128.Create((ushort)1);
    private static readonly Vector128<ushort> s_lumaR = Vector128.Create((ushort)77);
    private static readonly Vector128<ushort> s_lumaG = Vector128.Create((ushort)150);
    private static readonly Vector128<ushort> s_lumaB = Vector128.Create((ushort)29);
    private static readonly Vector128<ushort> s_lumaRound = Vector128.Create((ushort)128);

    internal static void ToBgra(ReadOnlySpan<byte> cmyk, Span<byte> bgra, int pixelCount)
    {
        ref var sourceRef = ref MemoryMarshal.GetReference(cmyk);
        ref var destinationRef = ref MemoryMarshal.GetReference(bgra);
        var i = 0;

        if (Vector128.IsHardwareAccelerated)
        {
            for (; i <= pixelCount - 4; i += 4)
            {
                var (r, g, b) = ConvertFourPixels(
                    Vector128.LoadUnsafe(ref sourceRef, (nuint)(i * 4)));

                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(ref destinationRef, i * 4), BuildBgra(r, g, b));
            }
        }

        for (; i < pixelCount; i++)
        {
            ConvertPixel(ref sourceRef, ref destinationRef, i);
        }
    }

    internal static void ToBgr(ReadOnlySpan<byte> cmyk, Span<byte> bgr, int pixelCount)
    {
        ref var sourceRef = ref MemoryMarshal.GetReference(cmyk);
        ref var destinationRef = ref MemoryMarshal.GetReference(bgr);
        var i = 0;

        if (Vector128.IsHardwareAccelerated)
        {
            for (; i <= pixelCount - 4; i += 4)
            {
                var (r, g, b) = ConvertFourPixels(
                    Vector128.LoadUnsafe(ref sourceRef, (nuint)(i * 4)));
                var packed = Vector128.Shuffle(BuildBgra(r, g, b), s_bgrPackLayout);
                ref var destination = ref Unsafe.Add(ref destinationRef, i * 3);

                Unsafe.WriteUnaligned(ref destination, Vector128.AsUInt64(packed).GetElement(0));
                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(ref destination, 8),
                    Vector128.AsUInt32(packed).GetElement(2));
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
            for (; i <= pixelCount - 4; i += 4)
            {
                var (r, g, b) = ConvertFourPixels(
                    Vector128.LoadUnsafe(ref sourceRef, (nuint)(i * 4)));
                var luma = (r * s_lumaR + g * s_lumaG + b * s_lumaB + s_lumaRound) >> 8;
                var bytes = Vector128.Narrow(luma, luma);

                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(ref destinationRef, i), Vector128.AsUInt32(bytes).GetElement(0));
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
    private static (Vector128<ushort> R, Vector128<ushort> G, Vector128<ushort> B)
        ConvertFourPixels(Vector128<byte> source)
    {
        var k = Vector128.WidenLower(Vector128.Shuffle(source, s_kMask));

        return (
            Divide(Vector128.WidenLower(Vector128.Shuffle(source, s_cMask)) * k),
            Divide(Vector128.WidenLower(Vector128.Shuffle(source, s_mMask)) * k),
            Divide(Vector128.WidenLower(Vector128.Shuffle(source, s_yMask)) * k));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<byte> BuildBgra(
        Vector128<ushort> r, Vector128<ushort> g, Vector128<ushort> b)
    {
        var r8 = Vector128.Narrow(r, r) & s_lowFourBytes;
        var g8 = Vector128.Narrow(g, g) & s_lowFourBytes;
        var b8 = Vector128.Narrow(b, b) & s_lowFourBytes;

        return Vector128.Shuffle(b8, s_bLayout)
            | Vector128.Shuffle(g8, s_gLayout)
            | Vector128.Shuffle(r8, s_rLayout)
            | s_bgraLayout;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<ushort> Divide(Vector128<ushort> product)
    {
        var t = product + s_round;

        return (t + s_one + Vector128.ShiftRightLogical(t, 8)) >> 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Divide(int product) => (byte)((product + 127) / 255);

    private static void ConvertPixel(ref byte sourceRef, ref byte destinationRef, int index)
    {
        var offset = index * 4;
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
