using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TurboJpegWrapper.Decoder.Internal.Conversion;

internal static class LumaConverter
{
    internal static void BgrToGray(ReadOnlySpan<byte> bgr, Span<byte> gray, int pixelCount)
    {
        ref var sourceRef = ref MemoryMarshal.GetReference(bgr);
        ref var destinationRef = ref MemoryMarshal.GetReference(gray);

        for (var i = 0; i < pixelCount; i++)
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
