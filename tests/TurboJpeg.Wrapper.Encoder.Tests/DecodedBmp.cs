using System.Buffers.Binary;
using DecoderOptions = TurboJpegWrapper.TurboJpegDecodeOptions;
using DecoderBitDepth = TurboJpegWrapper.TurboJpegBmpBitDepth;

namespace TurboJpegWrapper.Encoder.Tests;

/// <summary>Wraps the decoder's 24-bit BMP output and exposes top-down BGR pixels.</summary>
internal sealed class DecodedBmp
{
    private readonly byte[] _bmp;
    private readonly int _pixelOffset;
    private readonly int _stride;
    private readonly int _storedRowCount;

    private DecodedBmp(byte[] bmp)
    {
        _bmp = bmp;
        _pixelOffset = BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(10));
        Width = BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(18));

        var storedHeight = BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(22));
        BottomUp = storedHeight > 0;
        Height = Math.Abs(storedHeight);
        _storedRowCount = Height;

        var bitCount = BinaryPrimitives.ReadUInt16LittleEndian(bmp.AsSpan(28));
        if (bitCount != 24)
        {
            throw new InvalidOperationException(
                $"DecodedBmp requires 24-bit BMP output but the decoder produced {bitCount} bits per pixel.");
        }

        _stride = (Width * 3 + 3) & ~3;
    }

    internal int Width { get; }

    internal int Height { get; }

    /// <summary>Gets a value indicating whether the BMP stores its bottom row first.</summary>
    internal bool BottomUp { get; }

    internal static DecodedBmp Decode(byte[] jpeg) =>
        new(TurboJpeg.DecodeToBmp(
            jpeg, new DecoderOptions { BmpBitDepth = DecoderBitDepth.Bpp24 }));

    /// <summary>Returns the BGR pixel at top-down image coordinates.</summary>
    internal (byte B, byte G, byte R) GetPixel(int x, int y)
    {
        if ((uint)x >= (uint)Width)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        if ((uint)y >= (uint)_storedRowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(y));
        }

        var row = BottomUp ? _storedRowCount - 1 - y : y;
        var offset = _pixelOffset + row * _stride + x * 3;
        return (_bmp[offset], _bmp[offset + 1], _bmp[offset + 2]);
    }
}
