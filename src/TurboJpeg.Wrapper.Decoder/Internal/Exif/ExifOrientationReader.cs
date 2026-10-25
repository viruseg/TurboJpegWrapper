using System.Buffers.Binary;

namespace TurboJpegWrapper.Decoder.Internal.Exif;

internal static class ExifOrientationReader
{
    private const ushort OrientationTag = 0x0112;
    private const ushort ShortType = 3;
    private const ushort TiffMagic = 42;

    private static ReadOnlySpan<byte> ExifIdentifier => "Exif\0\0"u8;

    internal static ExifOrientation Read(ReadOnlySpan<byte> jpeg)
    {
        if (jpeg.Length < 4 || jpeg[0] != 0xFF || jpeg[1] != 0xD8)
        {
            return ExifOrientation.Normal;
        }

        var offset = 2;
        while (offset < jpeg.Length - 1)
        {
            if (jpeg[offset] != 0xFF)
            {
                offset++;
                continue;
            }

            while (offset < jpeg.Length && jpeg[offset] == 0xFF)
            {
                offset++;
            }

            if (offset >= jpeg.Length)
            {
                break;
            }

            var marker = jpeg[offset++];

            if (marker is 0xDA or 0xD9)
            {
                break;
            }

            if (marker == 0x00
                || marker == 0x01
                || (marker >= 0xD0 && marker <= 0xD7))
            {
                continue;
            }

            if (jpeg.Length - offset < 2)
            {
                break;
            }

            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(jpeg.Slice(offset, 2));
            if (segmentLength < 2 || (long)offset + segmentLength > jpeg.Length)
            {
                break;
            }

            if (marker == 0xE1)
            {
                var payload = jpeg.Slice(offset + 2, segmentLength - 2);
                if (payload.StartsWith(ExifIdentifier))
                {
                    return ReadTiff(payload[ExifIdentifier.Length..]);
                }
            }

            offset += segmentLength;
        }

        return ExifOrientation.Normal;
    }

    private static ExifOrientation ReadTiff(ReadOnlySpan<byte> tiff)
    {
        if (tiff.Length < 8)
        {
            return ExifOrientation.Normal;
        }

        bool littleEndian;
        if (tiff[0] == (byte)'I' && tiff[1] == (byte)'I')
        {
            littleEndian = true;
        }
        else if (tiff[0] == (byte)'M' && tiff[1] == (byte)'M')
        {
            littleEndian = false;
        }
        else
        {
            return ExifOrientation.Normal;
        }

        if (ReadUInt16(tiff[2..], littleEndian) != TiffMagic)
        {
            return ExifOrientation.Normal;
        }

        var ifdOffset = ReadUInt32(tiff[4..], littleEndian);
        if (ifdOffset < 8 || ifdOffset > (uint)(tiff.Length - 2))
        {
            return ExifOrientation.Normal;
        }

        var ifd = (int)ifdOffset;
        var entryCount = ReadUInt16(tiff[ifd..], littleEndian);
        var entriesStart = ifd + 2L;
        if (entriesStart + entryCount * 12L > tiff.Length)
        {
            return ExifOrientation.Normal;
        }

        for (var i = 0; i < entryCount; i++)
        {
            var entry = tiff.Slice((int)(entriesStart + i * 12L), 12);
            if (ReadUInt16(entry, littleEndian) != OrientationTag)
            {
                continue;
            }

            var type = ReadUInt16(entry[2..], littleEndian);
            var count = ReadUInt32(entry[4..], littleEndian);
            if (type != ShortType || count != 1)
            {
                return ExifOrientation.Normal;
            }

            var value = ReadUInt16(entry[8..], littleEndian);
            return value is >= (ushort)ExifOrientation.Normal and <= (ushort)ExifOrientation.Rotate270
                ? (ExifOrientation)value
                : ExifOrientation.Normal;
        }

        return ExifOrientation.Normal;
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> source, bool littleEndian) =>
        littleEndian
            ? BinaryPrimitives.ReadUInt16LittleEndian(source)
            : BinaryPrimitives.ReadUInt16BigEndian(source);

    private static uint ReadUInt32(ReadOnlySpan<byte> source, bool littleEndian) =>
        littleEndian
            ? BinaryPrimitives.ReadUInt32LittleEndian(source)
            : BinaryPrimitives.ReadUInt32BigEndian(source);
}
