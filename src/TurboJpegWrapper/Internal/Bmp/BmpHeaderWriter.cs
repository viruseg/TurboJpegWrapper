using System.Buffers.Binary;

namespace TurboJpegWrapper.Internal.Bmp;

internal static class BmpHeaderWriter
{
    internal const int HeaderSize = 54;

    internal static long GetFileSize(int width, int height) =>
        checked(HeaderSize + (long)width * height * 4);

    internal static void Write(Span<byte> destination, int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width), width, "The width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height), height, "The height must be greater than zero.");
        }

        if (destination.Length < HeaderSize)
        {
            throw new ArgumentException(
                $"The destination must be at least {HeaderSize} bytes long.", nameof(destination));
        }

        var fileSize = checked((int)GetFileSize(width, height));
        var imageSize = fileSize - HeaderSize;

        destination[0] = (byte)'B';
        destination[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(destination[2..], fileSize);
        BinaryPrimitives.WriteInt32LittleEndian(destination[6..], 0);
        BinaryPrimitives.WriteInt32LittleEndian(destination[10..], HeaderSize);
        BinaryPrimitives.WriteInt32LittleEndian(destination[14..], 40);
        BinaryPrimitives.WriteInt32LittleEndian(destination[18..], width);
        BinaryPrimitives.WriteInt32LittleEndian(destination[22..], height);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[26..], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[28..], 32);
        BinaryPrimitives.WriteInt32LittleEndian(destination[30..], 0);
        BinaryPrimitives.WriteInt32LittleEndian(destination[34..], imageSize);
        BinaryPrimitives.WriteInt32LittleEndian(destination[38..], 2835);
        BinaryPrimitives.WriteInt32LittleEndian(destination[42..], 2835);
        BinaryPrimitives.WriteInt32LittleEndian(destination[46..], 0);
        BinaryPrimitives.WriteInt32LittleEndian(destination[50..], 0);
    }
}
