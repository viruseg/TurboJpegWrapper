using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace TurboJpegWrapper.Internal.Decoder.Conversion;

internal static class LumaConverter
{
    private static readonly Vector128<byte> s_r0 = Vector128.Create(
        (byte)2, 5, 8, 11, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80);
    private static readonly Vector128<byte> s_g0 = Vector128.Create(
        (byte)1, 4, 7, 10, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80);
    private static readonly Vector128<byte> s_b0 = Vector128.Create(
        (byte)0, 3, 6, 9, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80);
    private static readonly Vector128<byte> s_r4 = Vector128.Create(
        (byte)6, 9, 12, 15, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80);
    private static readonly Vector128<byte> s_g4 = Vector128.Create(
        (byte)5, 8, 11, 14, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80);
    private static readonly Vector128<byte> s_b4 = Vector128.Create(
        (byte)4, 7, 10, 13, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80);
    private static readonly Vector128<ushort> s_weightR = Vector128.Create((ushort)77);
    private static readonly Vector128<ushort> s_weightG = Vector128.Create((ushort)150);
    private static readonly Vector128<ushort> s_weightB = Vector128.Create((ushort)29);
    private static readonly Vector128<ushort> s_round = Vector128.Create((ushort)128);
    private static readonly Vector128<ushort> s_zero = Vector128<ushort>.Zero;

    internal static void BgrToGray(ReadOnlySpan<byte> bgr, Span<byte> gray, int pixelCount)
    {
        ref var sourceRef = ref MemoryMarshal.GetReference(bgr);
        ref var destinationRef = ref MemoryMarshal.GetReference(gray);
        var i = 0;

        if (Vector128.IsHardwareAccelerated)
        {
            var r0 = s_r0;
            var g0 = s_g0;
            var b0 = s_b0;
            var r4 = s_r4;
            var g4 = s_g4;
            var b4 = s_b4;
            var weightR = s_weightR;
            var weightG = s_weightG;
            var weightB = s_weightB;
            var round = s_round;
            var zero = s_zero;

            for (; i <= pixelCount - 8; i += 8)
            {
                var offset = i * 3;
                var low = Vector128.LoadUnsafe(ref sourceRef, (nuint)offset);
                var high = Vector128.LoadUnsafe(ref sourceRef, (nuint)(offset + 8));

                var rLow = Vector128.WidenLower(Vector128.Shuffle(low, r0));
                var gLow = Vector128.WidenLower(Vector128.Shuffle(low, g0));
                var bLow = Vector128.WidenLower(Vector128.Shuffle(low, b0));
                var rHigh = Vector128.WidenLower(Vector128.Shuffle(high, r4));
                var gHigh = Vector128.WidenLower(Vector128.Shuffle(high, g4));
                var bHigh = Vector128.WidenLower(Vector128.Shuffle(high, b4));

                var lumaLow =
                    (rLow * weightR + gLow * weightG + bLow * weightB + round) >> 8;
                var lumaHigh =
                    (rHigh * weightR + gHigh * weightG + bHigh * weightB + round) >> 8;
                var packed = Vector128.Create(
                    Vector128.AsUInt32(Vector128.Narrow(lumaLow, zero)).GetElement(0),
                    Vector128.AsUInt32(Vector128.Narrow(lumaHigh, zero)).GetElement(0),
                    0u,
                    0u);

                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(ref destinationRef, i),
                    Vector128.AsUInt64(packed).GetElement(0));
            }
        }

        for (; i < pixelCount; i++)
        {
            var offset = i * 3;
            Unsafe.Add(ref destinationRef, i) = (byte)(
                (77 * Unsafe.Add(ref sourceRef, offset + 2)
                    + 150 * Unsafe.Add(ref sourceRef, offset + 1)
                    + 29 * Unsafe.Add(ref sourceRef, offset)
                    + 128) >> 8);
        }
    }
}
