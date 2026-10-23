using System.Buffers.Binary;

namespace TurboJpegWrapper.Tests;

internal static class JpegPatcher
{
    internal static void SetDimensions(byte[] jpeg, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(jpeg);

        if (width is < 1 or > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width), width, "The width must be between 1 and 65535.");
        }

        if (height is < 1 or > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height), height, "The height must be between 1 and 65535.");
        }

        var offset = 0;
        while (offset + 1 < jpeg.Length)
        {
            if (jpeg[offset] != 0xFF)
            {
                offset++;
                continue;
            }

            var markerStart = offset;
            var markerCode = markerStart + 1;
            while (markerCode < jpeg.Length && jpeg[markerCode] == 0xFF)
            {
                markerStart = markerCode;
                markerCode++;
            }

            if (markerCode >= jpeg.Length)
            {
                break;
            }

            var marker = jpeg[markerCode];
            if (marker == 0x00)
            {
                offset = markerCode + 1;
                continue;
            }

            if (marker is 0xD9 or 0xDA)
            {
                break;
            }

            if (marker == 0x01 || marker is 0xD8 || marker is >= 0xD0 and <= 0xD7)
            {
                offset = markerCode + 1;
                continue;
            }

            var segment = markerStart + 2;
            if (segment + 2 > jpeg.Length)
            {
                break;
            }

            var length = BinaryPrimitives.ReadUInt16BigEndian(jpeg.AsSpan(segment, 2));
            if (length < 2 || segment + length > jpeg.Length)
            {
                break;
            }

            if (marker is 0xC0 or 0xC1 or 0xC2)
            {
                if (length < 8)
                {
                    break;
                }

                BinaryPrimitives.WriteUInt16BigEndian(
                    jpeg.AsSpan(markerStart + 5, 2), (ushort)height);
                BinaryPrimitives.WriteUInt16BigEndian(
                    jpeg.AsSpan(markerStart + 7, 2), (ushort)width);
                return;
            }

            offset = segment + length;
        }

        throw new InvalidOperationException("The JPEG does not contain a supported SOF marker.");
    }
}
