using System.Buffers.Binary;

namespace TurboJpegWrapper.Decoder.Tests;

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

    internal static byte[] WithExifOrientation(
        byte[] jpeg, ushort orientation, bool bigEndian = false) =>
        WithSegment(jpeg, 0xE1, CreateExifPayload(orientation, bigEndian));

    internal static void SetJfifDensity(
        byte[] jpeg, ushort xDensity, ushort yDensity, byte units)
    {
        ArgumentNullException.ThrowIfNull(jpeg);

        if (jpeg.Length < 20
            || jpeg[0] != 0xFF
            || jpeg[1] != 0xD8
            || jpeg[2] != 0xFF
            || jpeg[3] != 0xE0
            || !jpeg.AsSpan(6, 4).SequenceEqual("JFIF"u8))
        {
            throw new ArgumentException("The JPEG must start with a JFIF APP0 segment.", nameof(jpeg));
        }

        jpeg[13] = units;
        BinaryPrimitives.WriteUInt16BigEndian(jpeg.AsSpan(14, 2), xDensity);
        BinaryPrimitives.WriteUInt16BigEndian(jpeg.AsSpan(16, 2), yDensity);
    }

    internal static byte[] CreateExifPayload(
        ushort orientation, bool bigEndian = false, ushort tag = 0x0112)
    {
        var payload = new byte[6 + 26];
        "Exif\0\0"u8.CopyTo(payload);
        var tiff = payload.AsSpan(6);

        tiff[0] = bigEndian ? (byte)'M' : (byte)'I';
        tiff[1] = tiff[0];

        WriteUInt16(tiff[2..], 42, bigEndian);
        WriteUInt32(tiff[4..], 8, bigEndian);
        WriteUInt16(tiff[8..], 1, bigEndian);
        WriteUInt16(tiff[10..], tag, bigEndian);
        WriteUInt16(tiff[12..], 3, bigEndian);
        WriteUInt32(tiff[14..], 1, bigEndian);
        WriteUInt16(tiff[18..], orientation, bigEndian);
        WriteUInt32(tiff[22..], 0, bigEndian);

        return payload;
    }

    internal static byte[] CreateExifPayloadWithIfdInsideHeader(ushort orientation = 6)
    {
        var payload = new byte[6 + 508];
        "Exif\0\0"u8.CopyTo(payload);
        var tiff = payload.AsSpan(6);

        tiff[0] = (byte)'I';
        tiff[1] = (byte)'I';
        WriteUInt16(tiff[2..], 42, bigEndian: false);
        WriteUInt32(tiff[4..], 2, bigEndian: false);

        var entry = tiff.Slice(16, 12);
        WriteUInt16(entry, 0x0112, bigEndian: false);
        WriteUInt16(entry[2..], 3, bigEndian: false);
        WriteUInt32(entry[4..], 1, bigEndian: false);
        WriteUInt16(entry[8..], orientation, bigEndian: false);

        return payload;
    }

    internal static byte[] WithSegment(byte[] jpeg, byte marker, ReadOnlySpan<byte> payload)
    {
        ArgumentNullException.ThrowIfNull(jpeg);

        if (jpeg.Length < 2 || jpeg[0] != 0xFF || jpeg[1] != 0xD8)
        {
            throw new ArgumentException("The JPEG must start with an SOI marker.", nameof(jpeg));
        }

        if (payload.Length + 2 > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(payload), "The segment payload is too large.");
        }

        var segment = new byte[payload.Length + 4];
        segment[0] = 0xFF;
        segment[1] = marker;
        BinaryPrimitives.WriteUInt16BigEndian(segment.AsSpan(2), (ushort)(payload.Length + 2));
        payload.CopyTo(segment.AsSpan(4));

        var result = new byte[jpeg.Length + segment.Length];
        jpeg.AsSpan(0, 2).CopyTo(result);
        segment.CopyTo(result.AsSpan(2));
        jpeg.AsSpan(2).CopyTo(result.AsSpan(2 + segment.Length));
        return result;
    }

    private static void WriteUInt16(Span<byte> destination, ushort value, bool bigEndian)
    {
        if (bigEndian)
        {
            BinaryPrimitives.WriteUInt16BigEndian(destination, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt16LittleEndian(destination, value);
        }
    }

    private static void WriteUInt32(Span<byte> destination, uint value, bool bigEndian)
    {
        if (bigEndian)
        {
            BinaryPrimitives.WriteUInt32BigEndian(destination, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt32LittleEndian(destination, value);
        }
    }
}
