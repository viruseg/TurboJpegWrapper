using System.Buffers.Binary;

namespace TurboJpeg.Wrapper.Decoder.Tests;

internal static class BmpTestHelpers
{
    internal const int HeaderSize = 54;

    internal static int Width(byte[] bmp) => BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(18));
    internal static int Height(byte[] bmp) => BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(22));

    internal static int BitsPerPixel(byte[] bmp) =>
        BinaryPrimitives.ReadUInt16LittleEndian(bmp.AsSpan(28));

    internal static int PixelOffset(byte[] bmp) =>
        BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(10));

    internal static int RowStride(byte[] bmp) =>
        (Width(bmp) * (BitsPerPixel(bmp) / 8) + 3) & ~3;

    internal static int PixelsPerMeterX(byte[] bmp) =>
        BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(38));

    internal static int PixelsPerMeterY(byte[] bmp) =>
        BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(42));

    internal static (byte B, byte G, byte R, byte A) Pixel(byte[] bmp, int x, int y)
    {
        var bitsPerPixel = BitsPerPixel(bmp);
        var offset = PixelOffset(bmp)
            + (Height(bmp) - 1 - y) * RowStride(bmp)
            + x * (bitsPerPixel / 8);

        if (bitsPerPixel == 8)
        {
            var value = bmp[offset];
            return (value, value, value, 255);
        }

        return (bmp[offset], bmp[offset + 1], bmp[offset + 2],
            bitsPerPixel == 32 ? bmp[offset + 3] : (byte)255);
    }
}
