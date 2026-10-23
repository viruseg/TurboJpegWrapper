using TurboJpegWrapper.Internal.Bmp;
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

    /// <summary>Decodes a JPEG image to a complete 32-bit BMP file.</summary>
    /// <param name="jpeg">The complete JPEG file contents.</param>
    /// <returns>The complete BMP file contents.</returns>
    /// <exception cref="ArgumentException"><paramref name="jpeg"/> is empty.</exception>
    /// <exception cref="ObjectDisposedException">The decoder has been disposed.</exception>
    /// <exception cref="TurboJpegUnsupportedException">
    /// The image precision is not 8 bits or the image uses a CMYK or YCCK color space.
    /// </exception>
    /// <exception cref="TurboJpegImageTooLargeException">
    /// The decoded image would exceed the maximum array length.
    /// </exception>
    /// <exception cref="TurboJpegException">TurboJPEG could not decode the JPEG.</exception>
    public unsafe byte[] DecodeToBmp(ReadOnlySpan<byte> jpeg)
    {
        ThrowIfDisposed();

        if (jpeg.IsEmpty)
        {
            throw new ArgumentException("JPEG data must not be empty.", nameof(jpeg));
        }

        var info = ReadHeader(jpeg);
        if (!info.CanDecodeToBmp)
        {
            throw new TurboJpegUnsupportedException(
                $"Cannot decode a JPEG with {info.Precision}-bit precision and {info.Colorspace} colorspace to BMP.");
        }

        var width = info.Width;
        var height = info.Height;
        var total = BmpHeaderWriter.GetFileSize(width, height);
        if (total > Array.MaxLength)
        {
            throw new TurboJpegImageTooLargeException(
                $"The decoded image ({width}x{height}) requires {total} bytes, " +
                $"which exceeds the maximum array length of {Array.MaxLength} bytes.");
        }

        var handle = _handle!;
        SetParameter(handle, TurboJpegNative.ParamBottomUp, 1);
        SetParameter(handle, TurboJpegNative.ParamMaxPixels, _defaultOptions.MaxPixels);
        SetParameter(handle, TurboJpegNative.ParamScanLimit, _defaultOptions.ScanLimit);
        SetParameter(handle, TurboJpegNative.ParamFastDct, _defaultOptions.FastDct ? 1 : 0);
        SetParameter(handle, TurboJpegNative.ParamFastUpsample, _defaultOptions.FastUpsampling ? 1 : 0);

        var bmp = GC.AllocateUninitializedArray<byte>((int)total);
        BmpHeaderWriter.Write(bmp.AsSpan(0, BmpHeaderWriter.HeaderSize), width, height);

        var pitch = checked(width * 4);
        fixed (byte* jpegBuf = jpeg)
        fixed (byte* dstBuf = bmp.AsSpan(BmpHeaderWriter.HeaderSize))
        {
            if (TurboJpegNative.Decompress8(
                    handle,
                    jpegBuf,
                    (nuint)jpeg.Length,
                    dstBuf,
                    pitch,
                    TurboJpegNative.PixelFormatBgra) != 0)
            {
                throw TurboJpegNative.CreateException(handle);
            }
        }

        return bmp;
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

    private static void SetParameter(SafeTurboJpegHandle handle, int parameter, int value)
    {
        if (TurboJpegNative.Set(handle, parameter, value) != 0)
        {
            throw TurboJpegNative.CreateException(handle);
        }
    }
}
