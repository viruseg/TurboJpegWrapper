using System.Buffers.Binary;

namespace TurboJpegWrapper.Tests;

internal static class BmpTestHelpers
{
    internal const int HeaderSize = 54;

    internal static int Width(byte[] bmp) => BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(18));
    internal static int Height(byte[] bmp) => BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(22));

    internal static (byte B, byte G, byte R, byte A) Pixel(byte[] bmp, int x, int y)
    {
        int width = Width(bmp);
        int height = Height(bmp);
        int rowFromBottom = height - 1 - y;
        int offset = HeaderSize + (rowFromBottom * width + x) * 4;
        return (bmp[offset], bmp[offset + 1], bmp[offset + 2], bmp[offset + 3]);
    }
}
