using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TurboJpegWrapper.Decoder.Internal.Conversion;

internal static class PrecisionScale
{
    internal static byte[] GetLut(int precision)
    {
        var max = (1 << precision) - 1;
        var lut = new byte[256];

        for (var value = 0; value <= max; value++)
        {
            lut[value] = (byte)((value * 255 + max / 2) / max);
        }

        return lut;
    }

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
}
