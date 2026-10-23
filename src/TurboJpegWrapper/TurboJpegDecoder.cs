using TurboJpegWrapper.Internal.Native;

namespace TurboJpegWrapper;

/// <summary>Decodes JPEG images with libjpeg-turbo.</summary>
/// <remarks>
/// A decoder owns a single native handle. Instance methods are not thread-safe; use one decoder
/// per thread or synchronize access. After <see cref="Dispose"/> the instance cannot be reused.
/// </remarks>
public sealed class TurboJpegDecoder : IDisposable
{
    private readonly TurboJpegDecodeOptions _defaultOptions;
    private SafeTurboJpegHandle? _handle;

    /// <summary>Initializes a new instance of the <see cref="TurboJpegDecoder"/> class.</summary>
    /// <param name="defaultOptions">Options used when a method does not receive explicit options.</param>
    public TurboJpegDecoder(TurboJpegDecodeOptions? defaultOptions = null)
    {
        _defaultOptions = defaultOptions ?? TurboJpegDecodeOptions.Default;
        _handle = TurboJpegNative.CreateDecoderHandle();
    }

    internal bool IsDisposed => _handle is null;

    /// <summary>Reads the dimensions and encoding format of a JPEG image without decoding its pixels.</summary>
    /// <param name="jpeg">The complete JPEG file contents.</param>
    /// <returns>Information about the JPEG image.</returns>
    /// <exception cref="ArgumentException"><paramref name="jpeg"/> is empty.</exception>
    /// <exception cref="ObjectDisposedException">The decoder has been disposed.</exception>
    /// <exception cref="TurboJpegException">TurboJPEG could not parse the JPEG header.</exception>
    public TurboJpegImageInfo GetImageInfo(ReadOnlySpan<byte> jpeg)
    {
        ThrowIfDisposed();

        if (jpeg.IsEmpty)
        {
            throw new ArgumentException("JPEG data must not be empty.", nameof(jpeg));
        }

        return ReadHeader(jpeg);
    }

    /// <summary>Releases the native TurboJPEG handle.</summary>
    public void Dispose()
    {
        _handle?.Dispose();
        _handle = null;
    }

    private unsafe TurboJpegImageInfo ReadHeader(ReadOnlySpan<byte> jpeg)
    {
        var handle = _handle;
        if (handle is null)
        {
            throw new ObjectDisposedException(nameof(TurboJpegDecoder));
        }

        fixed (byte* jpegBuf = jpeg)
        {
            if (TurboJpegNative.DecompressHeader(handle, jpegBuf, (nuint)jpeg.Length) != 0)
            {
                throw TurboJpegNative.CreateException(handle);
            }
        }

        var width = TurboJpegNative.Get(handle, TurboJpegNative.ParamJpegWidth);
        var height = TurboJpegNative.Get(handle, TurboJpegNative.ParamJpegHeight);
        var precision = TurboJpegNative.Get(handle, TurboJpegNative.ParamPrecision);
        var colorspace = TurboJpegNative.Get(handle, TurboJpegNative.ParamColorspace);
        var subsampling = TurboJpegNative.Get(handle, TurboJpegNative.ParamSubsamp);
        var lossless = TurboJpegNative.Get(handle, TurboJpegNative.ParamLossless);

        if (width <= 0 || height <= 0)
        {
            throw new TurboJpegException($"TurboJPEG reported invalid image dimensions {width}x{height}.");
        }

        return new TurboJpegImageInfo(
            width,
            height,
            precision,
            (TurboJpegColorspace)colorspace,
            (TurboJpegSubsampling)subsampling,
            lossless != 0);
    }

    private void ThrowIfDisposed()
    {
        if (_handle is null)
        {
            throw new ObjectDisposedException(nameof(TurboJpegDecoder));
        }
    }
}
