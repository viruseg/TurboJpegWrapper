using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TurboJpegWrapper.Decoder.Internal.Conversion;

internal static class GrayConverter
{
    internal static void ExpandToBgr(ReadOnlySpan<byte> gray, Span<byte> bgr, int pixelCount)
    {
        ref var grayRef = ref MemoryMarshal.GetReference(gray);
        ref var bgrRef = ref MemoryMarshal.GetReference(bgr);

        for (var i = 0; i < pixelCount; i++)
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

        for (var i = 0; i < pixelCount; i++)
        {
            var value = Unsafe.Add(ref grayRef, i);
            var offset = i * 4;
            Unsafe.Add(ref bgraRef, offset) = value;
            Unsafe.Add(ref bgraRef, offset + 1) = value;
            Unsafe.Add(ref bgraRef, offset + 2) = value;
            Unsafe.Add(ref bgraRef, offset + 3) = 255;
        }
    }
}
