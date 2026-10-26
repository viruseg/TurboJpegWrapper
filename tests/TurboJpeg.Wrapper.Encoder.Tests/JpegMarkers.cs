using System.Buffers.Binary;

namespace TurboJpegWrapper.Encoder.Tests;

/// <summary>Reads structural markers from JPEG files in tests.</summary>
internal static class JpegMarkers
{
    private const byte MarkerPrefix = 0xFF;
    private const byte StartOfScan = 0xDA;

    /// <summary>
    /// Returns the first start-of-frame marker byte (<c>0xC0</c>, <c>0xC2</c>, <c>0xC9</c>,
    /// <c>0xCA</c>, ...) that appears before the start-of-scan segment.
    /// </summary>
    /// <exception cref="InvalidDataException">The JPEG contains no start-of-frame marker.</exception>
    internal static byte FindSofMarker(ReadOnlySpan<byte> jpeg)
    {
        // Skip the SOI marker; every following marker starts on an 0xFF prefix.
        var offset = 2;
        while (offset + 1 < jpeg.Length)
        {
            if (jpeg[offset] != MarkerPrefix)
            {
                offset++;
                continue;
            }

            var marker = jpeg[offset + 1];
            if (marker == MarkerPrefix)
            {
                // Fill byte between marker prefixes; the real marker starts one byte later.
                offset++;
                continue;
            }

            if (marker == StartOfScan)
            {
                break;
            }

            if (IsStartOfFrame(marker))
            {
                return marker;
            }

            if (IsStandalone(marker))
            {
                offset += 2;
                continue;
            }

            if (offset + 4 > jpeg.Length)
            {
                break;
            }

            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(jpeg.Slice(offset + 2, 2));
            if (segmentLength < 2)
            {
                break;
            }

            offset += 2 + segmentLength;
        }

        throw new InvalidDataException("The JPEG contains no SOF marker before the SOS marker.");
    }

    /// <summary>Counts restart markers (<c>0xFFD0</c> through <c>0xFFD7</c>).</summary>
    internal static int CountRestartMarkers(ReadOnlySpan<byte> jpeg)
    {
        var count = 0;
        for (var i = 0; i + 1 < jpeg.Length; i++)
        {
            if (jpeg[i] == MarkerPrefix && jpeg[i + 1] is >= 0xD0 and <= 0xD7)
            {
                count++;
                i++;
            }
        }

        return count;
    }

    private static bool IsStartOfFrame(byte marker) =>
        marker is >= 0xC0 and <= 0xCF
        and not 0xC4 // DHT
        and not 0xC8 // JPG
        and not 0xCC; // DAC

    private static bool IsStandalone(byte marker) =>
        marker == 0x01 || marker is >= 0xD0 and <= 0xD9;
}
