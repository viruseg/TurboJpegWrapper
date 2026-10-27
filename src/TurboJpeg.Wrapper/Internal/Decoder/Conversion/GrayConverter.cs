using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace TurboJpegWrapper.Internal.Decoder.Conversion;

internal static class GrayConverter
{
    private static readonly Vector128<byte> s_bgr0 = Vector128.Create(
        (byte)0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 0x80, 0x80, 0x80, 0x80);
    private static readonly Vector128<byte> s_bgr4 = Vector128.Create(
        (byte)4, 4, 4, 5, 5, 5, 6, 6, 6, 7, 7, 7, 0x80, 0x80, 0x80, 0x80);
    private static readonly Vector128<byte> s_bgr8 = Vector128.Create(
        (byte)8, 8, 8, 9, 9, 9, 10, 10, 10, 11, 11, 11, 0x80, 0x80, 0x80, 0x80);
    private static readonly Vector128<byte> s_bgr12 = Vector128.Create(
        (byte)12, 12, 12, 13, 13, 13, 14, 14, 14, 15, 15, 15, 0x80, 0x80, 0x80, 0x80);
    private static readonly Vector128<byte> s_bgra0 = Vector128.Create(
        (byte)0, 0, 0, 0x80, 1, 1, 1, 0x80, 2, 2, 2, 0x80, 3, 3, 3, 0x80);
    private static readonly Vector128<byte> s_bgra4 = Vector128.Create(
        (byte)4, 4, 4, 0x80, 5, 5, 5, 0x80, 6, 6, 6, 0x80, 7, 7, 7, 0x80);
    private static readonly Vector128<byte> s_bgra8 = Vector128.Create(
        (byte)8, 8, 8, 0x80, 9, 9, 9, 0x80, 10, 10, 10, 0x80, 11, 11, 11, 0x80);
    private static readonly Vector128<byte> s_bgra12 = Vector128.Create(
        (byte)12, 12, 12, 0x80, 13, 13, 13, 0x80, 14, 14, 14, 0x80, 15, 15, 15, 0x80);
    private static readonly Vector128<byte> s_alpha = Vector128.Create(
        (byte)0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255);

    internal static void ExpandToBgr(ReadOnlySpan<byte> gray, Span<byte> bgr, int pixelCount)
    {
        ref var grayRef = ref MemoryMarshal.GetReference(gray);
        ref var bgrRef = ref MemoryMarshal.GetReference(bgr);
        var i = 0;

        if (Vector128.IsHardwareAccelerated)
        {
            for (; i <= pixelCount - 16; i += 16)
            {
                var source = Vector128.LoadUnsafe(ref grayRef, (nuint)i);
                var destination = i * 3;
                StoreBgr(ref bgrRef, destination, Vector128.Shuffle(source, s_bgr0));
                StoreBgr(ref bgrRef, destination + 12, Vector128.Shuffle(source, s_bgr4));
                StoreBgr(ref bgrRef, destination + 24, Vector128.Shuffle(source, s_bgr8));
                StoreBgr(ref bgrRef, destination + 36, Vector128.Shuffle(source, s_bgr12));
            }

            for (; i <= pixelCount - 4; i += 4)
            {
                var packed = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref grayRef, i));
                var source = Vector128.CreateScalar(packed).AsByte();
                StoreBgr(ref bgrRef, i * 3, Vector128.Shuffle(source, s_bgr0));
            }
        }

        for (; i < pixelCount; i++)
        {
            var value = Unsafe.Add(ref grayRef, i);
            var offset = i * 3;
            Unsafe.Add(ref bgrRef, offset) = value;
            Unsafe.Add(ref bgrRef, offset + 1) = value;
            Unsafe.Add(ref bgrRef, offset + 2) = value;
        }
    }

    internal static void ExpandToBgra(ReadOnlySpan<byte> gray, Span<byte> bgra, int pixelCount)
    {
        ref var grayRef = ref MemoryMarshal.GetReference(gray);
        ref var bgraRef = ref MemoryMarshal.GetReference(bgra);
        var i = 0;

        if (Vector128.IsHardwareAccelerated)
        {
            for (; i <= pixelCount - 16; i += 16)
            {
                var source = Vector128.LoadUnsafe(ref grayRef, (nuint)i);
                var destination = i * 4;
                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(ref bgraRef, destination),
                    Vector128.Shuffle(source, s_bgra0) | s_alpha);
                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(ref bgraRef, destination + 16),
                    Vector128.Shuffle(source, s_bgra4) | s_alpha);
                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(ref bgraRef, destination + 32),
                    Vector128.Shuffle(source, s_bgra8) | s_alpha);
                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(ref bgraRef, destination + 48),
                    Vector128.Shuffle(source, s_bgra12) | s_alpha);
            }

            for (; i <= pixelCount - 4; i += 4)
            {
                var packed = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref grayRef, i));
                var source = Vector128.CreateScalar(packed).AsByte();
                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(ref bgraRef, i * 4),
                    Vector128.Shuffle(source, s_bgra0) | s_alpha);
            }
        }

        for (; i < pixelCount; i++)
        {
            var value = Unsafe.Add(ref grayRef, i);
            var offset = i * 4;
            Unsafe.Add(ref bgraRef, offset) = value;
            Unsafe.Add(ref bgraRef, offset + 1) = value;
            Unsafe.Add(ref bgraRef, offset + 2) = value;
            Unsafe.Add(ref bgraRef, offset + 3) = 255;
        }
    }

    private static void StoreBgr(ref byte destination, int offset, Vector128<byte> values)
    {
        Unsafe.WriteUnaligned(
            ref Unsafe.Add(ref destination, offset),
            Vector128.AsUInt64(values).GetElement(0));
        Unsafe.WriteUnaligned(
            ref Unsafe.Add(ref destination, offset + 8),
            Vector128.AsUInt32(values).GetElement(2));
    }
}
