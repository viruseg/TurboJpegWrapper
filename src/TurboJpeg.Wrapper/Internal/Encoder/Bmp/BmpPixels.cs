using System.Buffers;

namespace TurboJpegWrapper.Internal.Encoder.Bmp;

internal ref struct BmpPixels
{
    private byte[]? _rented;

    internal BmpPixels(
        ReadOnlySpan<byte> data,
        int pitch,
        int pixelFormat,
        bool bottomUp,
        byte[]? rented)
    {
        Data = data;
        Pitch = pitch;
        PixelFormat = pixelFormat;
        BottomUp = bottomUp;
        _rented = rented;
    }

    internal ReadOnlySpan<byte> Data { get; }

    internal int Pitch { get; }

    internal int PixelFormat { get; }

    internal bool BottomUp { get; }

    internal void Release()
    {
        var rented = _rented;
        if (rented is not null)
        {
            _rented = null;
            ArrayPool<byte>.Shared.Return(rented);
        }
    }
}
