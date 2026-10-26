using System.Buffers;
using TurboJpegWrapper.Encoder.Internal;
using TurboJpegWrapper.Encoder.Internal.Bmp;
using TurboJpegWrapper.Encoder.Internal.Native;
using TurboJpegWrapper.Internal.Native;

namespace TurboJpegWrapper.Encoder;

/// <summary>Encodes BMP images to JPEG with libjpeg-turbo.</summary>
/// <remarks>
/// An encoder owns a single native handle. Instance methods are not thread-safe; use one encoder
/// per thread or synchronize access. After <see cref="Dispose"/> the instance cannot be reused.
/// A failed native encode recreates the native handle so the encoder remains usable. Recovery is
/// best-effort: if recreation fails, the encoder becomes disposed and the original error is
/// rethrown.
/// </remarks>
public sealed class TurboJpegEncoder : IDisposable
{
    private const int UnsetParameter = int.MinValue;
    private const double PixelsPerMeterToInch = 0.0254;

    private readonly TurboJpegEncodeOptions _defaultOptions;
    private readonly Func<SafeTurboJpegHandle> _handleFactory;
    private SafeTurboJpegHandle? _handle;
    private int _appliedStopOnWarning = UnsetParameter;
    private int _appliedBottomUp = UnsetParameter;
    private int _appliedLossless = UnsetParameter;
    private int _appliedQuality = UnsetParameter;
    private int _appliedSubsampling = UnsetParameter;
    private int _appliedColorspace = UnsetParameter;
    private int _appliedRestartBlocks = UnsetParameter;
    private int _appliedRestartRows = UnsetParameter;
    private int _appliedMaxMemory = UnsetParameter;
    private int _appliedDensityX = UnsetParameter;
    private int _appliedDensityY = UnsetParameter;
    private int _appliedDensityUnits = UnsetParameter;

    /// <summary>Initializes a new instance of the <see cref="TurboJpegEncoder"/> class.</summary>
    /// <param name="defaultOptions">Options used when a method does not receive explicit options.</param>
    public TurboJpegEncoder(TurboJpegEncodeOptions? defaultOptions = null)
        : this(defaultOptions, TurboJpegNative.CreateEncoderHandle)
    {
    }

    internal TurboJpegEncoder(
        TurboJpegEncodeOptions? defaultOptions, Func<SafeTurboJpegHandle> handleFactory)
    {
        ArgumentNullException.ThrowIfNull(handleFactory);

        _defaultOptions = defaultOptions ?? TurboJpegEncodeOptions.Default;
        _handleFactory = handleFactory;
        _handle = handleFactory();
    }

    /// <summary>Encodes a BMP image to a JPEG image using the encoder's default options.</summary>
    /// <param name="bmp">The complete BMP file contents.</param>
    /// <returns>The complete JPEG file contents.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="bmp"/> is empty or is not a supported BMP file.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The encoder has been disposed.</exception>
    /// <exception cref="TurboJpegUnsupportedException">
    /// The BMP uses an encoding that this library does not support.
    /// </exception>
    /// <exception cref="TurboJpegImageTooLargeException">
    /// The worst-case JPEG size exceeds the maximum array length.
    /// </exception>
    /// <exception cref="TurboJpegException">TurboJPEG could not encode the image.</exception>
    public byte[] EncodeToJpeg(ReadOnlySpan<byte> bmp) => EncodeToJpeg(bmp, _defaultOptions);

    /// <summary>Encodes a BMP image to a JPEG image using the specified options.</summary>
    /// <param name="bmp">The complete BMP file contents.</param>
    /// <param name="options">The encoding options.</param>
    /// <returns>The complete JPEG file contents.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="bmp"/> is empty or is not a supported BMP file.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The encoder has been disposed.</exception>
    /// <exception cref="TurboJpegUnsupportedException">
    /// The BMP uses an encoding that this library does not support.
    /// </exception>
    /// <exception cref="TurboJpegImageTooLargeException">
    /// The worst-case JPEG size exceeds the maximum array length.
    /// </exception>
    /// <exception cref="TurboJpegException">TurboJPEG could not encode the image.</exception>
    public byte[] EncodeToJpeg(ReadOnlySpan<byte> bmp, TurboJpegEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ThrowIfDisposed();

        if (bmp.IsEmpty)
        {
            ThrowHelper.ThrowBmpEmpty();
        }

        var info = BmpReader.Read(bmp);
        var grayscaleOutput = options.Colorspace == TurboJpegColorspace.Grayscale
            || options.Subsampling == TurboJpegSubsampling.Grayscale;
        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput);
        try
        {
            var handle = _handle;
            if (handle is null)
            {
                ThrowHelper.ThrowDisposed();
            }

            var nativeHandle = handle.DangerousGetHandle();
            var effectiveSubsampling = grayscaleOutput
                ? (int)TurboJpegSubsampling.Grayscale
                : (int)options.Subsampling;
            var colorspace = ResolveColorspace(options.Colorspace, grayscaleOutput);
            var density = ResolveDensity(in info, options);

            // The worst-case size is computed before any native state changes; an over-limit
            // image is a caller error, not a native failure that should recreate the handle.
            var maxSize = TurboJpegNative.GetMaxJpegSize(
                info.Width, info.Height, effectiveSubsampling);
            var worstCase = ArrayPool<byte>.Shared.Rent(maxSize);
            try
            {
                try
                {
                    ApplyParameters(
                        nativeHandle,
                        options,
                        pixels.BottomUp,
                        effectiveSubsampling,
                        colorspace,
                        density);

                    var written = TurboJpegNative.Compress(
                        nativeHandle,
                        pixels.Data,
                        info.Width,
                        pixels.Pitch,
                        info.Height,
                        pixels.PixelFormat,
                        worstCase);

                    var result = GC.AllocateUninitializedArray<byte>(written);
                    worstCase.AsSpan(0, written).CopyTo(result);
                    return result;
                }
                catch (TurboJpegException)
                {
                    RecoverAfterNativeFailure();
                    throw;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(worstCase);
            }
        }
        finally
        {
            pixels.Release();
        }
    }

    /// <summary>Releases the native TurboJPEG handle.</summary>
    public void Dispose()
    {
        _handle?.Dispose();
        _handle = null;
    }

    private static int ResolveColorspace(TurboJpegColorspace colorspace, bool grayscaleOutput)
    {
        if (grayscaleOutput)
        {
            return (int)TurboJpegColorspace.Grayscale;
        }

        return colorspace == TurboJpegColorspace.Default
            ? (int)TurboJpegColorspace.YCbCr
            : (int)colorspace;
    }

    private static TurboJpegDensity ResolveDensity(
        in BmpInfo info, TurboJpegEncodeOptions options)
    {
        if (options.Density is { } density)
        {
            return density;
        }

        if (info.DensityX == 0 && info.DensityY == 0)
        {
            // Apply the native default explicitly so a reused handle cannot keep the density
            // of a previous encode.
            return new TurboJpegDensity(1, 1, TurboJpegDensityUnits.Unknown);
        }

        var pixelsPerInchX = ToPixelsPerInch(info.DensityX);
        var pixelsPerInchY = ToPixelsPerInch(info.DensityY);
        if (info.DensityX == 0)
        {
            pixelsPerInchX = pixelsPerInchY;
        }
        else if (info.DensityY == 0)
        {
            pixelsPerInchY = pixelsPerInchX;
        }

        return new TurboJpegDensity(
            pixelsPerInchX, pixelsPerInchY, TurboJpegDensityUnits.PixelsPerInch);
    }

    private static int ToPixelsPerInch(int pixelsPerMeter) =>
        Math.Max(
            1,
            (int)Math.Round(
                pixelsPerMeter * PixelsPerMeterToInch, MidpointRounding.AwayFromZero));

    private void ApplyParameters(
        nint handle,
        TurboJpegEncodeOptions options,
        bool bottomUp,
        int effectiveSubsampling,
        int colorspace,
        TurboJpegDensity density)
    {
        ApplyParameter(
            handle, TurboJpegNativeApi.ParamStopOnWarning,
            options.StopOnWarning ? 1 : 0, ref _appliedStopOnWarning);
        ApplyParameter(
            handle, TurboJpegNativeApi.ParamBottomUp,
            bottomUp ? 1 : 0, ref _appliedBottomUp);
        ApplyParameter(handle, TurboJpegNativeApi.ParamLossless, 0, ref _appliedLossless);
        ApplyParameter(handle, TurboJpegNativeApi.ParamQuality, options.Quality, ref _appliedQuality);
        ApplyParameter(
            handle, TurboJpegNativeApi.ParamSubsamp, effectiveSubsampling, ref _appliedSubsampling);
        ApplyParameter(
            handle, TurboJpegNativeApi.ParamColorspace, colorspace, ref _appliedColorspace);

        // Setting either restart interval clears the other, so apply rows before blocks when
        // rows are zero (the Task 6 default) to leave both at zero deterministically.
        ApplyParameter(handle, TurboJpegNativeApi.ParamRestartRows, 0, ref _appliedRestartRows);
        ApplyParameter(
            handle, TurboJpegNativeApi.ParamRestartBlocks, 0, ref _appliedRestartBlocks);

        ApplyParameter(handle, TurboJpegNativeApi.ParamMaxMemory, 0, ref _appliedMaxMemory);

        var (densityX, densityY, densityUnits) = density;
        ApplyParameter(handle, TurboJpegNativeApi.ParamXDensity, densityX, ref _appliedDensityX);
        ApplyParameter(handle, TurboJpegNativeApi.ParamYDensity, densityY, ref _appliedDensityY);
        ApplyParameter(
            handle, TurboJpegNativeApi.ParamDensityUnits,
            (int)densityUnits, ref _appliedDensityUnits);
    }

    private void ApplyParameter(
        nint handle, int parameter, int value, ref int appliedValue)
    {
        if (appliedValue == value)
        {
            return;
        }

        if (TurboJpegNativeApi.Set(handle, parameter, value) != 0)
        {
            ThrowHelper.Throw(TurboJpegNative.CreateException(handle));
        }

        appliedValue = value;
    }

    private void RecoverAfterNativeFailure()
    {
        try
        {
            _handle?.Dispose();
            _handle = null;
            ResetAppliedParameters();
            _handle = _handleFactory();
        }
        catch
        {
            // WHY: recovery is best-effort; the original encode error is rethrown by the caller.
            _handle = null;
        }
    }

    private void ResetAppliedParameters()
    {
        _appliedStopOnWarning = UnsetParameter;
        _appliedBottomUp = UnsetParameter;
        _appliedLossless = UnsetParameter;
        _appliedQuality = UnsetParameter;
        _appliedSubsampling = UnsetParameter;
        _appliedColorspace = UnsetParameter;
        _appliedRestartBlocks = UnsetParameter;
        _appliedRestartRows = UnsetParameter;
        _appliedMaxMemory = UnsetParameter;
        _appliedDensityX = UnsetParameter;
        _appliedDensityY = UnsetParameter;
        _appliedDensityUnits = UnsetParameter;
    }

    private void ThrowIfDisposed()
    {
        if (_handle is null)
        {
            ThrowHelper.ThrowDisposed();
        }
    }
}
