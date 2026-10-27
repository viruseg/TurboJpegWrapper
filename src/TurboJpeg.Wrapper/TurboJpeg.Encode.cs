using TurboJpegWrapper.Internal.Encoder;
using TurboJpegWrapper.Internal.Encoder.Native;
using TurboJpegWrapper.Internal.Encoder.Pooling;

namespace TurboJpegWrapper;

public static partial class TurboJpeg
{
    /// <summary>Encodes a BMP image to a JPEG image using default options.</summary>
    /// <param name="bmp">The complete BMP file contents.</param>
    /// <returns>The complete JPEG file contents.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="bmp"/> is empty or is not a supported BMP file.
    /// </exception>
    /// <exception cref="TurboJpegUnsupportedException">
    /// The BMP uses an encoding that this library does not support.
    /// </exception>
    /// <exception cref="TurboJpegImageTooLargeException">
    /// The worst-case JPEG size exceeds the maximum array length.
    /// </exception>
    /// <exception cref="TurboJpegException">TurboJPEG could not load or encode the image.</exception>
    /// <exception cref="PlatformNotSupportedException">
    /// The current OS/architecture is not supported and no explicit native library path is configured.
    /// </exception>
    public static byte[] EncodeToJpeg(ReadOnlySpan<byte> bmp) =>
        EncodeToJpeg(bmp, TurboJpegEncodeOptions.Default);

    /// <summary>Encodes a BMP image to a JPEG image using the specified options.</summary>
    /// <param name="bmp">The complete BMP file contents.</param>
    /// <param name="options">The encoding options.</param>
    /// <returns>The complete JPEG file contents.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="bmp"/> is empty or is not a supported BMP file, or <paramref name="options"/>
    /// contain an invalid combination such as both restart intervals being non-zero,
    /// <see cref="TurboJpegEncodeOptions.RestartBlocks"/> with lossless mode, or
    /// <see cref="TurboJpegEncodeOptions.Density"/> with lossless or RGB output.
    /// </exception>
    /// <exception cref="TurboJpegUnsupportedException">
    /// The BMP uses an encoding that this library does not support.
    /// </exception>
    /// <exception cref="TurboJpegImageTooLargeException">
    /// The worst-case JPEG size exceeds the maximum array length.
    /// </exception>
    /// <exception cref="TurboJpegException">TurboJPEG could not load or encode the image.</exception>
    /// <exception cref="PlatformNotSupportedException">
    /// The current OS/architecture is not supported and no explicit native library path is configured.
    /// </exception>
    public static byte[] EncodeToJpeg(ReadOnlySpan<byte> bmp, TurboJpegEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        TurboJpegEncoder.ValidateOptions(options);

        TurboJpegEncoder? encoder = null;
        try
        {
            encoder = TurboJpegEncoderPool.Shared.Rent();
            return encoder.EncodeToJpeg(bmp, options);
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            ThrowHelper.Throw(TurboJpegNative.CreateLoadException(ex));
            return null!;
        }
        finally
        {
            if (encoder is not null)
            {
                TurboJpegEncoderPool.Shared.Return(encoder);
            }
        }
    }

    /// <summary>Encodes a BMP image into a caller-provided JPEG buffer using the specified options.</summary>
    /// <param name="bmp">The complete BMP file contents.</param>
    /// <param name="options">The encoding options.</param>
    /// <param name="destination">The destination buffer for the complete JPEG file contents.</param>
    /// <returns>The number of bytes written to <paramref name="destination"/>.</returns>
    /// <remarks>
    /// The required buffer size can be obtained with
    /// <see cref="GetMaxJpegSize(ReadOnlySpan{byte}, TurboJpegEncodeOptions)"/>.
    /// <paramref name="destination"/> must not overlap <paramref name="bmp"/>, because the
    /// encoder may read BMP pixels directly from the source while writing JPEG bytes.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="bmp"/> is empty or is not a supported BMP file, <paramref name="options"/>
    /// contain an invalid combination such as both restart intervals being non-zero,
    /// <see cref="TurboJpegEncodeOptions.RestartBlocks"/> with lossless mode, or
    /// <see cref="TurboJpegEncodeOptions.Density"/> with lossless or RGB output,
    /// <paramref name="destination"/> is too small, or <paramref name="destination"/> overlaps
    /// <paramref name="bmp"/>.
    /// </exception>
    /// <exception cref="TurboJpegUnsupportedException">
    /// The BMP uses an encoding that this library does not support.
    /// </exception>
    /// <exception cref="TurboJpegImageTooLargeException">
    /// The worst-case JPEG size exceeds the maximum array length.
    /// </exception>
    /// <exception cref="TurboJpegException">TurboJPEG could not load or encode the image.</exception>
    /// <exception cref="PlatformNotSupportedException">
    /// The current OS/architecture is not supported and no explicit native library path is configured.
    /// </exception>
    public static int EncodeToJpeg(
        ReadOnlySpan<byte> bmp, TurboJpegEncodeOptions options, Span<byte> destination)
    {
        ArgumentNullException.ThrowIfNull(options);
        TurboJpegEncoder.ValidateOptions(options);

        TurboJpegEncoder? encoder = null;
        try
        {
            encoder = TurboJpegEncoderPool.Shared.Rent();
            return encoder.EncodeToJpeg(bmp, options, destination);
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            ThrowHelper.Throw(TurboJpegNative.CreateLoadException(ex));
            return 0;
        }
        finally
        {
            if (encoder is not null)
            {
                TurboJpegEncoderPool.Shared.Return(encoder);
            }
        }
    }

    /// <summary>Gets the maximum JPEG size that <see cref="EncodeToJpeg(ReadOnlySpan{byte})"/> could produce using default options.</summary>
    /// <param name="bmp">The complete BMP file contents.</param>
    /// <returns>The maximum possible JPEG file size in bytes.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="bmp"/> is empty or is not a supported BMP file.
    /// </exception>
    /// <exception cref="TurboJpegUnsupportedException">
    /// The BMP uses an encoding that this library does not support.
    /// </exception>
    /// <exception cref="TurboJpegImageTooLargeException">
    /// TurboJPEG reports a zero worst-case size, or the size exceeds the maximum array length.
    /// </exception>
    /// <exception cref="TurboJpegException">TurboJPEG could not load the library.</exception>
    /// <exception cref="PlatformNotSupportedException">
    /// The current OS/architecture is not supported and no explicit native library path is configured.
    /// </exception>
    public static int GetMaxJpegSize(ReadOnlySpan<byte> bmp) =>
        GetMaxJpegSize(bmp, TurboJpegEncodeOptions.Default);

    /// <summary>Gets the maximum JPEG size that <see cref="EncodeToJpeg(ReadOnlySpan{byte}, TurboJpegEncodeOptions)"/> could produce using the specified options.</summary>
    /// <param name="bmp">The complete BMP file contents.</param>
    /// <param name="options">The encoding options.</param>
    /// <returns>The maximum possible JPEG file size in bytes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="bmp"/> is empty or is not a supported BMP file, or <paramref name="options"/>
    /// contain an invalid combination such as both restart intervals being non-zero,
    /// <see cref="TurboJpegEncodeOptions.RestartBlocks"/> with lossless mode, or
    /// <see cref="TurboJpegEncodeOptions.Density"/> with lossless or RGB output.
    /// </exception>
    /// <exception cref="TurboJpegUnsupportedException">
    /// The BMP uses an encoding that this library does not support.
    /// </exception>
    /// <exception cref="TurboJpegImageTooLargeException">
    /// TurboJPEG reports a zero worst-case size, or the size exceeds the maximum array length.
    /// </exception>
    /// <exception cref="TurboJpegException">TurboJPEG could not load the library.</exception>
    /// <exception cref="PlatformNotSupportedException">
    /// The current OS/architecture is not supported and no explicit native library path is configured.
    /// </exception>
    public static int GetMaxJpegSize(ReadOnlySpan<byte> bmp, TurboJpegEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        TurboJpegEncoder.ValidateOptions(options);

        TurboJpegEncoder? encoder = null;
        try
        {
            encoder = TurboJpegEncoderPool.Shared.Rent();
            return encoder.GetMaxJpegSize(bmp, options);
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            ThrowHelper.Throw(TurboJpegNative.CreateLoadException(ex));
            return 0;
        }
        finally
        {
            if (encoder is not null)
            {
                TurboJpegEncoderPool.Shared.Return(encoder);
            }
        }
    }
}
