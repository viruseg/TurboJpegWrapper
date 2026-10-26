using System.Buffers;

namespace TurboJpegWrapper.Encoder.Internal.Bmp;

/// <summary>Describes the pixel buffer handed to the TurboJPEG compressor.</summary>
/// <remarks>
/// The direct paths return a view over the caller's BMP buffer and own no pooled memory.
/// Converted paths rent a buffer from <see cref="ArrayPool{T}.Shared"/>; callers must call
/// <see cref="Release"/> exactly once when they are done with the pixels.
/// </remarks>
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

    /// <summary>Gets the pixel bytes, starting at the first stored row.</summary>
    internal ReadOnlySpan<byte> Data { get; }

    /// <summary>Gets the distance in bytes between the starts of two consecutive rows.</summary>
    internal int Pitch { get; }

    /// <summary>Gets the TurboJPEG pixel format of <see cref="Data"/>.</summary>
    internal int PixelFormat { get; }

    /// <summary>Gets a value indicating whether the first stored row is the bottom row.</summary>
    internal bool BottomUp { get; }

    /// <summary>Returns the rented conversion buffer, if any, to the shared pool.</summary>
    /// <remarks>Calling this more than once on the same instance is a no-op.</remarks>
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
