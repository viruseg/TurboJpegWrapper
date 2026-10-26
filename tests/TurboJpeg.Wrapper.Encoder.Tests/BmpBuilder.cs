using System.Buffers.Binary;

namespace TurboJpegWrapper.Encoder.Tests;

/// <summary>Builds BMP files for the encoder tests.</summary>
/// <remarks>
/// Pixel callbacks receive top-down image coordinates: (0, 0) is the top-left pixel.
/// Bottom-up files, the default, store the rows in reverse order.
/// </remarks>
internal static class BmpBuilder
{
    private const int FileHeaderSize = 14;
    private const int CoreDibHeaderSize = 40;
    private const int BitfieldMaskOffset = 54;

    internal static byte[] Create24(
        int width,
        int height,
        Func<int, int, (byte B, byte G, byte R)> pixel,
        bool topDown = false,
        int ppmX = 0,
        int ppmY = 0,
        int dibSize = 40)
    {
        var stride = GetStride(width, 24);
        var pixelOffset = FileHeaderSize + dibSize;
        var buffer = new byte[checked(pixelOffset + stride * height)];

        WriteHeader(
            buffer,
            width,
            topDown ? -height : height,
            bitCount: 24,
            compression: 0,
            pixelOffset,
            dibSize,
            planes: 1,
            clrUsed: 0,
            ppmX,
            ppmY,
            redMask: 0,
            greenMask: 0,
            blueMask: 0,
            validSignature: true);

        for (var y = 0; y < height; y++)
        {
            var row = pixelOffset + GetFileRow(y, height, topDown) * stride;
            for (var x = 0; x < width; x++)
            {
                var (b, g, r) = pixel(x, y);
                var offset = row + x * 3;
                buffer[offset] = b;
                buffer[offset + 1] = g;
                buffer[offset + 2] = r;
            }
        }

        return buffer;
    }

    internal static byte[] Create32(
        int width,
        int height,
        Func<int, int, (byte B, byte G, byte R, byte A)> pixel,
        bool topDown = false,
        int ppmX = 0,
        int ppmY = 0)
    {
        var stride = GetStride(width, 32);
        const int pixelOffset = FileHeaderSize + CoreDibHeaderSize;
        var buffer = new byte[checked(pixelOffset + stride * height)];

        WriteHeader(
            buffer,
            width,
            topDown ? -height : height,
            bitCount: 32,
            compression: 0,
            pixelOffset,
            CoreDibHeaderSize,
            planes: 1,
            clrUsed: 0,
            ppmX,
            ppmY,
            redMask: 0,
            greenMask: 0,
            blueMask: 0,
            validSignature: true);

        for (var y = 0; y < height; y++)
        {
            var row = pixelOffset + GetFileRow(y, height, topDown) * stride;
            for (var x = 0; x < width; x++)
            {
                var (b, g, r, a) = pixel(x, y);
                var offset = row + x * 4;
                buffer[offset] = b;
                buffer[offset + 1] = g;
                buffer[offset + 2] = r;
                buffer[offset + 3] = a;
            }
        }

        return buffer;
    }

    internal static byte[] CreatePalette8(
        int width,
        int height,
        Func<int, int, byte> index,
        IReadOnlyList<(byte B, byte G, byte R)> palette,
        bool topDown = false,
        int clrUsed = 0)
    {
        var paletteCount = clrUsed != 0 ? clrUsed : 1 << 8;
        var stride = GetStride(width, 8);
        var pixelOffset = FileHeaderSize + CoreDibHeaderSize + paletteCount * 4;
        var buffer = new byte[checked(pixelOffset + stride * height)];

        WriteHeader(
            buffer,
            width,
            topDown ? -height : height,
            bitCount: 8,
            compression: 0,
            pixelOffset,
            CoreDibHeaderSize,
            planes: 1,
            clrUsed: (uint)clrUsed,
            ppmX: 0,
            ppmY: 0,
            redMask: 0,
            greenMask: 0,
            blueMask: 0,
            validSignature: true);
        WritePalette(buffer, palette, paletteCount);

        for (var y = 0; y < height; y++)
        {
            var row = pixelOffset + GetFileRow(y, height, topDown) * stride;
            for (var x = 0; x < width; x++)
            {
                buffer[row + x] = index(x, y);
            }
        }

        return buffer;
    }

    internal static byte[] CreatePalette4(
        int width,
        int height,
        Func<int, int, byte> index,
        IReadOnlyList<(byte B, byte G, byte R)> palette,
        bool topDown = false)
    {
        const int paletteCount = 16;
        var stride = GetStride(width, 4);
        var pixelOffset = FileHeaderSize + CoreDibHeaderSize + paletteCount * 4;
        var buffer = new byte[checked(pixelOffset + stride * height)];

        WriteHeader(
            buffer,
            width,
            topDown ? -height : height,
            bitCount: 4,
            compression: 0,
            pixelOffset,
            CoreDibHeaderSize,
            planes: 1,
            clrUsed: 0,
            ppmX: 0,
            ppmY: 0,
            redMask: 0,
            greenMask: 0,
            blueMask: 0,
            validSignature: true);
        WritePalette(buffer, palette, paletteCount);

        for (var y = 0; y < height; y++)
        {
            var row = pixelOffset + GetFileRow(y, height, topDown) * stride;
            for (var x = 0; x < width; x++)
            {
                var value = (byte)(index(x, y) & 0x0F);
                var offset = row + x / 2;
                buffer[offset] |= (x & 1) == 0 ? (byte)(value << 4) : value;
            }
        }

        return buffer;
    }

    internal static byte[] CreatePalette1(
        int width,
        int height,
        Func<int, int, byte> index,
        IReadOnlyList<(byte B, byte G, byte R)> palette,
        bool topDown = false)
    {
        const int paletteCount = 2;
        var stride = GetStride(width, 1);
        var pixelOffset = FileHeaderSize + CoreDibHeaderSize + paletteCount * 4;
        var buffer = new byte[checked(pixelOffset + stride * height)];

        WriteHeader(
            buffer,
            width,
            topDown ? -height : height,
            bitCount: 1,
            compression: 0,
            pixelOffset,
            CoreDibHeaderSize,
            planes: 1,
            clrUsed: 0,
            ppmX: 0,
            ppmY: 0,
            redMask: 0,
            greenMask: 0,
            blueMask: 0,
            validSignature: true);
        WritePalette(buffer, palette, paletteCount);

        for (var y = 0; y < height; y++)
        {
            var row = pixelOffset + GetFileRow(y, height, topDown) * stride;
            for (var x = 0; x < width; x++)
            {
                if ((index(x, y) & 1) != 0)
                {
                    buffer[row + x / 8] |= (byte)(1 << (7 - (x & 7)));
                }
            }
        }

        return buffer;
    }

    internal static byte[] Create16(
        int width,
        int height,
        Func<int, int, ushort> sample,
        ushort redMask,
        ushort greenMask,
        ushort blueMask,
        bool bitfields,
        bool topDown = false)
    {
        var stride = GetStride(width, 16);
        var compression = bitfields ? 3u : 0u;
        var pixelOffset = bitfields
            ? FileHeaderSize + CoreDibHeaderSize + 12
            : FileHeaderSize + CoreDibHeaderSize;
        var buffer = new byte[checked(pixelOffset + stride * height)];

        WriteHeader(
            buffer,
            width,
            topDown ? -height : height,
            bitCount: 16,
            compression,
            pixelOffset,
            CoreDibHeaderSize,
            planes: 1,
            clrUsed: 0,
            ppmX: 0,
            ppmY: 0,
            redMask,
            greenMask,
            blueMask,
            validSignature: true);

        for (var y = 0; y < height; y++)
        {
            var row = pixelOffset + GetFileRow(y, height, topDown) * stride;
            for (var x = 0; x < width; x++)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(
                    buffer.AsSpan(row + x * 2), sample(x, y));
            }
        }

        return buffer;
    }

    internal static byte[] CreateRle8(int width, int height)
    {
        const int paletteCount = 256;
        const int pixelOffset = FileHeaderSize + CoreDibHeaderSize + paletteCount * 4;
        var buffer = new byte[pixelOffset + 2];

        WriteHeader(
            buffer,
            width,
            height,
            bitCount: 8,
            compression: 1,
            pixelOffset,
            CoreDibHeaderSize,
            planes: 1,
            clrUsed: paletteCount,
            ppmX: 0,
            ppmY: 0,
            redMask: 0,
            greenMask: 0,
            blueMask: 0,
            validSignature: true);

        buffer[pixelOffset] = 0;
        buffer[pixelOffset + 1] = 1;
        return buffer;
    }

    /// <summary>Builds a structurally valid OS/2 core-header (DIB size 12) 24-bit BMP.</summary>
    internal static byte[] CreateOs2Core(int width, int height)
    {
        const int coreDibHeaderSize = 12;
        var stride = GetStride(width, 24);
        var pixelOffset = FileHeaderSize + coreDibHeaderSize;
        var buffer = new byte[checked(pixelOffset + stride * height)];

        buffer[0] = (byte)'B';
        buffer[1] = (byte)'M';
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(2), (uint)buffer.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(10), (uint)pixelOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(14), coreDibHeaderSize);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(18), checked((ushort)width));
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(20), checked((ushort)height));
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(22), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(24), 24);
        return buffer;
    }

    /// <summary>Builds a raw BMP header with caller-controlled fields for malformed cases.</summary>
    internal static byte[] CreateRaw(
        int width = 2,
        int height = 2,
        int bitCount = 24,
        uint compression = 0,
        int dibSize = 40,
        int planes = 1,
        int pixelOffset = 54,
        int totalLength = 100,
        uint clrUsed = 0,
        uint redMask = 0,
        uint greenMask = 0,
        uint blueMask = 0,
        int ppmX = 0,
        int ppmY = 0,
        bool validSignature = true)
    {
        if (totalLength < FileHeaderSize + CoreDibHeaderSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalLength), "A raw BMP header needs at least 54 bytes.");
        }

        var buffer = new byte[totalLength];
        WriteHeader(
            buffer,
            width,
            height,
            bitCount,
            compression,
            pixelOffset,
            dibSize,
            planes,
            clrUsed,
            ppmX,
            ppmY,
            redMask,
            greenMask,
            blueMask,
            validSignature);
        return buffer;
    }

    private static void WriteHeader(
        byte[] buffer,
        int width,
        int height,
        int bitCount,
        uint compression,
        int pixelOffset,
        int dibSize,
        int planes,
        uint clrUsed,
        int ppmX,
        int ppmY,
        uint redMask,
        uint greenMask,
        uint blueMask,
        bool validSignature)
    {
        buffer[0] = validSignature ? (byte)'B' : (byte)'X';
        buffer[1] = validSignature ? (byte)'M' : (byte)'Z';
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(2), (uint)buffer.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(10), (uint)pixelOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(14), (uint)dibSize);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(18), width);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(22), height);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(26), (ushort)planes);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(28), (ushort)bitCount);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(30), compression);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(38), ppmX);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(42), ppmY);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(46), clrUsed);

        if (buffer.Length >= BitfieldMaskOffset + 12 && (dibSize >= 52 || compression == 3))
        {
            BinaryPrimitives.WriteUInt32LittleEndian(
                buffer.AsSpan(BitfieldMaskOffset), redMask);
            BinaryPrimitives.WriteUInt32LittleEndian(
                buffer.AsSpan(BitfieldMaskOffset + 4), greenMask);
            BinaryPrimitives.WriteUInt32LittleEndian(
                buffer.AsSpan(BitfieldMaskOffset + 8), blueMask);
        }
    }

    private static void WritePalette(
        byte[] buffer, IReadOnlyList<(byte B, byte G, byte R)> palette, int count)
    {
        var written = Math.Min(palette.Count, count);
        for (var i = 0; i < written; i++)
        {
            var (b, g, r) = palette[i];
            var offset = FileHeaderSize + CoreDibHeaderSize + i * 4;
            buffer[offset] = b;
            buffer[offset + 1] = g;
            buffer[offset + 2] = r;
            buffer[offset + 3] = 0;
        }
    }

    private static int GetStride(int width, int bitCount) =>
        checked(((width * bitCount + 31) / 32) * 4);

    private static int GetFileRow(int y, int height, bool topDown) =>
        topDown ? y : height - 1 - y;
}
