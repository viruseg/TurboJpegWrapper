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
    private int _appliedFastDct = UnsetParameter;
    private int _appliedOptimize = UnsetParameter;
    private int _appliedProgressive = UnsetParameter;
    private int _appliedArithmetic = UnsetParameter;
    private int _appliedLosslessPsv = UnsetParameter;
    private int _appliedLosslessPt = UnsetParameter;
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

        var info = ReadBmp(bmp);

        // The worst-case size is computed before any native state changes; an over-limit
        // image is a caller error, not a native failure that should recreate the handle.
        var maxSize = CalculateMaxJpegSize(in info, options);
        var worstCase = ArrayPool<byte>.Shared.Rent(maxSize);
        try
        {
            // The rented array may be larger than requested; its full capacity is the
            // destination size, so NOREALLOC can never reallocate.
            var written = EncodeCore(bmp, options, in info, worstCase);

            var result = GC.AllocateUninitializedArray<byte>(written);
            worstCase.AsSpan(0, written).CopyTo(result);
            return result;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(worstCase);
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
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="bmp"/> is empty, is not a supported BMP file, or
    /// <paramref name="destination"/> is too small.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The encoder has been disposed.</exception>
    /// <exception cref="TurboJpegUnsupportedException">
    /// The BMP uses an encoding that this library does not support.
    /// </exception>
    /// <exception cref="TurboJpegImageTooLargeException">
    /// The worst-case JPEG size exceeds the maximum array length.
    /// </exception>
    /// <exception cref="TurboJpegException">TurboJPEG could not encode the image.</exception>
    public int EncodeToJpeg(
        ReadOnlySpan<byte> bmp, TurboJpegEncodeOptions options, Span<byte> destination)
    {
        ArgumentNullException.ThrowIfNull(options);
        ThrowIfDisposed();

        var info = ReadBmp(bmp);
        var maxSize = CalculateMaxJpegSize(in info, options);
        if (destination.Length < maxSize)
        {
            ThrowHelper.ThrowDestinationTooSmall(maxSize);
        }

        return EncodeCore(bmp, options, in info, destination);
    }

    /// <summary>Gets the maximum JPEG size that <see cref="EncodeToJpeg(ReadOnlySpan{byte})"/> could produce using the encoder's default options.</summary>
    /// <param name="bmp">The complete BMP file contents.</param>
    /// <returns>The maximum possible JPEG file size in bytes.</returns>
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
    public int GetMaxJpegSize(ReadOnlySpan<byte> bmp) => GetMaxJpegSize(bmp, _defaultOptions);

    /// <summary>Gets the maximum JPEG size that <see cref="EncodeToJpeg(ReadOnlySpan{byte}, TurboJpegEncodeOptions)"/> could produce using the specified options.</summary>
    /// <param name="bmp">The complete BMP file contents.</param>
    /// <param name="options">The encoding options.</param>
    /// <returns>The maximum possible JPEG file size in bytes.</returns>
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
    public int GetMaxJpegSize(ReadOnlySpan<byte> bmp, TurboJpegEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ThrowIfDisposed();

        var info = ReadBmp(bmp);
        return CalculateMaxJpegSize(in info, options);
    }

    internal bool IsDisposed => _handle is null;

    /// <summary>Releases the native TurboJPEG handle.</summary>
    public void Dispose()
    {
        _handle?.Dispose();
        _handle = null;
    }

    private static BmpInfo ReadBmp(ReadOnlySpan<byte> bmp)
    {
        if (bmp.IsEmpty)
        {
            ThrowHelper.ThrowBmpEmpty();
        }

        return BmpReader.Read(bmp);
    }

    private static bool IsGrayscaleOutput(TurboJpegEncodeOptions options) =>
        options.Colorspace == TurboJpegColorspace.Grayscale
        || options.Subsampling == TurboJpegSubsampling.Grayscale;

    private static int ResolveEffectiveSubsampling(TurboJpegEncodeOptions options)
    {
        if (options.Lossless)
        {
            // Lossless compression never subsamples, so size the buffer for 4:4:4.
            return (int)TurboJpegSubsampling.Yuv444;
        }

        return IsGrayscaleOutput(options)
            ? (int)TurboJpegSubsampling.Grayscale
            : (int)options.Subsampling;
    }

    private static int CalculateMaxJpegSize(
        in BmpInfo info, TurboJpegEncodeOptions options)
    {
        ValidateOptions(options);
        return SizeCalculator.GetMaxJpegSize(
            info.Width, info.Height, ResolveEffectiveSubsampling(options));
    }

    private static void ValidateOptions(TurboJpegEncodeOptions options)
    {
        if (options.RestartBlocks != 0 && options.RestartRows != 0)
        {
            ThrowHelper.ThrowRestartOptionsConflict();
        }

        if (options.Lossless && options.RestartBlocks != 0)
        {
            ThrowHelper.ThrowLosslessRestartBlocks();
        }
    }

    /// <summary>
    /// Compresses validated BMP pixels directly into <paramref name="destination"/>. The caller
    /// has already parsed the BMP and verified that the destination is large enough.
    /// </summary>
    private int EncodeCore(
        ReadOnlySpan<byte> bmp,
        TurboJpegEncodeOptions options,
        in BmpInfo info,
        Span<byte> destination)
    {
        var grayscaleOutput = !options.Lossless && IsGrayscaleOutput(options);
        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput);
        try
        {
            var handle = _handle;
            if (handle is null)
            {
                ThrowHelper.ThrowDisposed();
            }

            var nativeHandle = handle.DangerousGetHandle();
            var effectiveSubsampling = ResolveEffectiveSubsampling(options);
            var colorspace = ResolveColorspace(options.Colorspace, grayscaleOutput);
            var density = ResolveDensity(in info, options);

            try
            {
                ApplyParameters(
                    nativeHandle,
                    options,
                    pixels.BottomUp,
                    effectiveSubsampling,
                    colorspace,
                    density);

                return TurboJpegNative.Compress(
                    nativeHandle,
                    pixels.Data,
                    info.Width,
                    pixels.Pitch,
                    info.Height,
                    pixels.PixelFormat,
                    destination);
            }
            catch (TurboJpegException)
            {
                RecoverAfterNativeFailure();
                throw;
            }
        }
        finally
        {
            pixels.Release();
        }
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

        if (!options.Lossless)
        {
            // Start from lossy mode, then apply every effective lossy parameter.
            ApplyParameter(handle, TurboJpegNativeApi.ParamLossless, 0, ref _appliedLossless);
            ApplyParameter(
                handle, TurboJpegNativeApi.ParamQuality, options.Quality, ref _appliedQuality);
            ApplyParameter(
                handle, TurboJpegNativeApi.ParamSubsamp,
                effectiveSubsampling, ref _appliedSubsampling);
            ApplyParameter(
                handle, TurboJpegNativeApi.ParamColorspace,
                colorspace, ref _appliedColorspace);
            ApplyParameter(
                handle, TurboJpegNativeApi.ParamFastDct,
                options.FastDct ? 1 : 0, ref _appliedFastDct);
            ApplyParameter(
                handle, TurboJpegNativeApi.ParamOptimize,
                options.OptimizeCoding ? 1 : 0, ref _appliedOptimize);
            ApplyParameter(
                handle, TurboJpegNativeApi.ParamProgressive,
                options.Progressive ? 1 : 0, ref _appliedProgressive);
            ApplyParameter(
                handle, TurboJpegNativeApi.ParamArithmetic,
                options.ArithmeticCoding ? 1 : 0, ref _appliedArithmetic);
        }
        else
        {
            // Stale lossy flags make tj3Set(LOSSLESS, 1) fail, so clear them first.
            ApplyParameter(handle, TurboJpegNativeApi.ParamProgressive, 0, ref _appliedProgressive);
            ApplyParameter(handle, TurboJpegNativeApi.ParamArithmetic, 0, ref _appliedArithmetic);
            ApplyParameter(handle, TurboJpegNativeApi.ParamOptimize, 0, ref _appliedOptimize);
            ApplyParameter(handle, TurboJpegNativeApi.ParamFastDct, 0, ref _appliedFastDct);
            ApplyParameter(handle, TurboJpegNativeApi.ParamLossless, 1, ref _appliedLossless);
            ApplyParameter(
                handle, TurboJpegNativeApi.ParamLosslessPsv,
                options.LosslessPredictor, ref _appliedLosslessPsv);
            ApplyParameter(
                handle, TurboJpegNativeApi.ParamLosslessPt,
                options.LosslessPointTransform, ref _appliedLosslessPt);
        }

        if (options.RestartRows > 0)
        {
            // Setting a non-zero restart interval clears the other one, so set the zero
            // value first and the requested interval last.
            ApplyParameter(
                handle, TurboJpegNativeApi.ParamRestartBlocks, 0, ref _appliedRestartBlocks);
            ApplyParameter(
                handle, TurboJpegNativeApi.ParamRestartRows,
                options.RestartRows, ref _appliedRestartRows);
        }
        else
        {
            ApplyParameter(
                handle, TurboJpegNativeApi.ParamRestartRows, 0, ref _appliedRestartRows);
            ApplyParameter(
                handle, TurboJpegNativeApi.ParamRestartBlocks,
                options.RestartBlocks, ref _appliedRestartBlocks);
        }

        ApplyParameter(
            handle, TurboJpegNativeApi.ParamMaxMemory,
            options.MaxMemoryMegabytes, ref _appliedMaxMemory);

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
        _appliedFastDct = UnsetParameter;
        _appliedOptimize = UnsetParameter;
        _appliedProgressive = UnsetParameter;
        _appliedArithmetic = UnsetParameter;
        _appliedLosslessPsv = UnsetParameter;
        _appliedLosslessPt = UnsetParameter;
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
