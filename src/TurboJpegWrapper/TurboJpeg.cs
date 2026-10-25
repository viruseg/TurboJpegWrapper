using TurboJpegWrapper.Internal.Native;
using TurboJpegWrapper.Internal.Pooling;

namespace TurboJpegWrapper;

/// <summary>Provides static access to TurboJPEG operations.</summary>
public static class TurboJpeg
{
    /// <summary>Gets the scaling factors supported by TurboJPEG, including upscaling and downscaling factors.</summary>
    /// <remarks>The factors are read from the native library once and cached after the first successful query.</remarks>
    /// <returns>A read-only span over the supported scaling factors in native order.</returns>
    /// <exception cref="TurboJpegException">The native library could not be loaded or the native scaling-factor query failed.</exception>
    /// <exception cref="PlatformNotSupportedException">The current OS/architecture is not supported and no explicit native path is configured.</exception>
    public static ReadOnlySpan<TurboJpegScalingFactor> GetSupportedScalingFactors()
    {
        try
        {
            return TurboJpegNative.GetSupportedScalingFactors();
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            throw TurboJpegNative.CreateLoadException(ex);
        }
    }

    /// <summary>Decodes a JPEG image to a complete BMP file using default options.</summary>
    /// <param name="jpeg">The complete JPEG file contents.</param>
    /// <returns>The complete BMP file contents.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="jpeg"/> is empty or the default scaling factor is not supported.
    /// </exception>
    /// <exception cref="TurboJpegUnsupportedException">
    /// The image precision is not 8 bits or the image uses a CMYK or YCCK color space.
    /// </exception>
    /// <exception cref="TurboJpegImageTooLargeException">
    /// The decoded image would exceed the configured pixel limit or the maximum array length.
    /// </exception>
    /// <exception cref="TurboJpegException">TurboJPEG could not load or decode the JPEG.</exception>
    public static byte[] DecodeToBmp(ReadOnlySpan<byte> jpeg) =>
        DecodeToBmp(jpeg, TurboJpegDecodeOptions.Default);

    /// <summary>Decodes a JPEG image to a complete BMP file using the specified options.</summary>
    /// <param name="jpeg">The complete JPEG file contents.</param>
    /// <param name="options">The decoding options.</param>
    /// <returns>The complete BMP file contents.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="jpeg"/> is empty or the requested scaling factor is not supported.
    /// </exception>
    /// <exception cref="TurboJpegUnsupportedException">
    /// The image precision is not 8 bits or the image uses a CMYK or YCCK color space.
    /// </exception>
    /// <exception cref="TurboJpegImageTooLargeException">
    /// The decoded image would exceed the configured pixel limit or the maximum array length.
    /// </exception>
    /// <exception cref="TurboJpegException">TurboJPEG could not load or decode the JPEG.</exception>
    public static byte[] DecodeToBmp(ReadOnlySpan<byte> jpeg, TurboJpegDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        TurboJpegDecoder? decoder = null;
        try
        {
            decoder = TurboJpegDecoderPool.Shared.Rent();
            return decoder.DecodeToBmp(jpeg, options);
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            throw TurboJpegNative.CreateLoadException(ex);
        }
        finally
        {
            if (decoder is not null)
            {
                TurboJpegDecoderPool.Shared.Return(decoder);
            }
        }
    }

    /// <summary>Reads the dimensions and encoding format of a JPEG image without decoding its pixels.</summary>
    /// <param name="jpeg">The complete JPEG file contents.</param>
    /// <returns>Information about the JPEG image.</returns>
    /// <exception cref="ArgumentException"><paramref name="jpeg"/> is empty.</exception>
    /// <exception cref="TurboJpegException">TurboJPEG could not load the library or parse the JPEG header.</exception>
    public static TurboJpegImageInfo GetImageInfo(ReadOnlySpan<byte> jpeg)
    {
        TurboJpegDecoder? decoder = null;
        try
        {
            decoder = TurboJpegDecoderPool.Shared.Rent();
            return decoder.GetImageInfo(jpeg);
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            throw TurboJpegNative.CreateLoadException(ex);
        }
        finally
        {
            if (decoder is not null)
            {
                TurboJpegDecoderPool.Shared.Return(decoder);
            }
        }
    }
}
