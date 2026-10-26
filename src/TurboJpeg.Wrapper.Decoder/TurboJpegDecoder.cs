using System.Buffers;
using TurboJpegWrapper.Decoder.Internal;
using TurboJpegWrapper.Decoder.Internal.Bmp;
using TurboJpegWrapper.Decoder.Internal.Exif;
using TurboJpegWrapper.Decoder.Internal.Native;
using TurboJpegWrapper.Internal.Native;

namespace TurboJpegWrapper.Decoder;

/// <summary>Decodes JPEG images with libjpeg-turbo.</summary>
/// <remarks>
/// A decoder owns a single native handle. Instance methods are not thread-safe; use one decoder
/// per thread or synchronize access. After <see cref="Dispose"/> the instance cannot be reused.
/// A failed JPEG header parse recreates the native handle so the decoder remains usable. Recovery
/// is best-effort: if recreation fails, the decoder becomes disposed and the original error is
/// rethrown.
/// </remarks>
public sealed class TurboJpegDecoder : IDisposable
{
    private const int UnsetParameter = int.MinValue;

    private readonly TurboJpegDecodeOptions _defaultOptions;
    private readonly Func<SafeTurboJpegHandle> _handleFactory;
    private SafeTurboJpegHandle? _handle;
    private int _appliedStopOnWarning = UnsetParameter;
    private int _appliedBottomUp = UnsetParameter;
    private int _appliedMaxPixels = UnsetParameter;
    private int _appliedMaxMemory = UnsetParameter;
    private int _appliedScanLimit = UnsetParameter;
    private int _appliedFastDct = UnsetParameter;
    private int _appliedFastUpsample = UnsetParameter;
    private TurboJpegScalingFactor? _appliedScalingFactor;
    private TurboJpegCrop? _appliedCrop;

    /// <summary>Initializes a new instance of the <see cref="TurboJpegDecoder"/> class.</summary>
    /// <param name="defaultOptions">Options used when a method does not receive explicit options.</param>
    public TurboJpegDecoder(TurboJpegDecodeOptions? defaultOptions = null)
        : this(defaultOptions, TurboJpegNative.CreateDecoderHandle)
    {
    }

    internal TurboJpegDecoder(
        TurboJpegDecodeOptions? defaultOptions, Func<SafeTurboJpegHandle> handleFactory)
    {
        ArgumentNullException.ThrowIfNull(handleFactory);

        _defaultOptions = defaultOptions ?? TurboJpegDecodeOptions.Default;
        _handleFactory = handleFactory;
        _handle = handleFactory();
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
            ThrowHelper.ThrowJpegEmpty();
        }

        var handle = _handle!.DangerousGetHandle();
        ApplyWarningPolicy(handle, stopOnWarning: true);

        return ReadHeader(
            jpeg,
            tolerateWarnings: false,
            includeDensity: true,
            includeSubsampling: true,
            includeLossless: true);
    }

    /// <summary>Decodes a JPEG image to a complete BMP file using the decoder's default options.</summary>
    /// <param name="jpeg">The complete JPEG file contents.</param>
    /// <returns>The complete BMP file contents.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="jpeg"/> is empty or the decoder's default scaling factor is not supported.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The decoder has been disposed.</exception>
    /// <exception cref="TurboJpegUnsupportedException">
    /// The image precision is not 8 bits or the image uses a CMYK or YCCK color space.
    /// </exception>
    /// <exception cref="TurboJpegImageTooLargeException">
    /// The decoded image would exceed the configured pixel limit or the maximum array length.
    /// </exception>
    /// <exception cref="TurboJpegException">TurboJPEG could not decode the JPEG.</exception>
    public byte[] DecodeToBmp(ReadOnlySpan<byte> jpeg) => DecodeToBmp(jpeg, _defaultOptions);

    /// <summary>Decodes a JPEG image to a complete BMP file using the specified options.</summary>
    /// <param name="jpeg">The complete JPEG file contents.</param>
    /// <param name="options">The decoding options.</param>
    /// <returns>The complete BMP file contents.</returns>
    /// <remarks>
    /// When <see cref="TurboJpegDecodeOptions.AutoOrient"/> is enabled, the EXIF orientation is
    /// applied and the BMP dimensions may be transposed.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="jpeg"/> is empty or the requested scaling factor is not supported.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The decoder has been disposed.</exception>
    /// <exception cref="TurboJpegUnsupportedException">
    /// The image precision is not 8 bits or the image uses a CMYK or YCCK color space.
    /// </exception>
    /// <exception cref="TurboJpegImageTooLargeException">
    /// The decoded image would exceed the configured pixel limit or the maximum array length.
    /// </exception>
    /// <exception cref="TurboJpegException">TurboJPEG could not decode the JPEG.</exception>
    public byte[] DecodeToBmp(ReadOnlySpan<byte> jpeg, TurboJpegDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ThrowIfDisposed();

        var plan = CreatePlan(jpeg, options, includeDensity: true);
        var bmp = GC.AllocateUninitializedArray<byte>(plan.TotalSize);
        DecodeWithPlan(jpeg, options, in plan, bmp);
        return bmp;
    }

    /// <summary>Decodes a JPEG image into a caller-provided BMP buffer using the specified options.</summary>
    /// <param name="jpeg">The complete JPEG file contents.</param>
    /// <param name="options">The decoding options.</param>
    /// <param name="destination">The destination buffer for the complete BMP file.</param>
    /// <returns>The number of bytes written to <paramref name="destination"/>.</returns>
    /// <remarks>
    /// The required buffer size can be obtained with <see cref="GetBmpSize(ReadOnlySpan{byte}, TurboJpegDecodeOptions)"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="jpeg"/> is empty, the requested scaling factor is not supported, or
    /// <paramref name="destination"/> is too small.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The decoder has been disposed.</exception>
    /// <exception cref="TurboJpegUnsupportedException">
    /// The image precision is not 8 bits or the image uses a CMYK or YCCK color space.
    /// </exception>
    /// <exception cref="TurboJpegImageTooLargeException">
    /// The decoded image would exceed the configured pixel limit or the maximum array length.
    /// </exception>
    /// <exception cref="TurboJpegException">TurboJPEG could not decode the JPEG.</exception>
    public int DecodeToBmp(
        ReadOnlySpan<byte> jpeg,
        TurboJpegDecodeOptions options,
        Span<byte> destination)
    {
        ArgumentNullException.ThrowIfNull(options);
        ThrowIfDisposed();

        var plan = CreatePlan(jpeg, options, includeDensity: true);
        if (destination.Length < plan.TotalSize)
        {
            ThrowHelper.ThrowDestinationTooSmall(plan.TotalSize);
        }

        DecodeWithPlan(jpeg, options, in plan, destination[..plan.TotalSize]);
        return plan.TotalSize;
    }

    /// <summary>Gets the size of the BMP that <see cref="DecodeToBmp(ReadOnlySpan{byte})"/> would produce using the default options.</summary>
    /// <param name="jpeg">The complete JPEG file contents.</param>
    /// <returns>The required BMP file size in bytes.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="jpeg"/> is empty or the default scaling factor is not supported.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The decoder has been disposed.</exception>
    /// <exception cref="TurboJpegUnsupportedException">
    /// The image precision is not 8 bits or the image uses a CMYK or YCCK color space.
    /// </exception>
    /// <exception cref="TurboJpegImageTooLargeException">
    /// The decoded image would exceed the configured pixel limit or the maximum array length.
    /// </exception>
    /// <exception cref="TurboJpegException">TurboJPEG could not parse the JPEG header.</exception>
    public int GetBmpSize(ReadOnlySpan<byte> jpeg) => GetBmpSize(jpeg, _defaultOptions);

    /// <summary>Gets the size of the BMP that <see cref="DecodeToBmp(ReadOnlySpan{byte}, TurboJpegDecodeOptions)"/> would produce.</summary>
    /// <param name="jpeg">The complete JPEG file contents.</param>
    /// <param name="options">The decoding options.</param>
    /// <returns>The required BMP file size in bytes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="jpeg"/> is empty or the requested scaling factor is not supported.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The decoder has been disposed.</exception>
    /// <exception cref="TurboJpegUnsupportedException">
    /// The image precision is not 8 bits or the image uses a CMYK or YCCK color space.
    /// </exception>
    /// <exception cref="TurboJpegImageTooLargeException">
    /// The decoded image would exceed the configured pixel limit or the maximum array length.
    /// </exception>
    /// <exception cref="TurboJpegException">TurboJPEG could not parse the JPEG header.</exception>
    public int GetBmpSize(ReadOnlySpan<byte> jpeg, TurboJpegDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ThrowIfDisposed();

        return CreatePlan(jpeg, options, includeDensity: false).TotalSize;
    }

    private DecodePlan CreatePlan(
        ReadOnlySpan<byte> jpeg, TurboJpegDecodeOptions options, bool includeDensity)
    {
        if (jpeg.IsEmpty)
        {
            ThrowHelper.ThrowJpegEmpty();
        }

        var handle = _handle!.DangerousGetHandle();
        ApplyWarningPolicy(handle, options.StopOnWarning);

        var includeSubsampling = options.Crop is not null;
        var includeLossless =
            options.Crop is not null || options.ScalingFactor != TurboJpegScalingFactor.One;
        var info = ReadHeader(
            jpeg,
            tolerateWarnings: !options.StopOnWarning,
            includeDensity,
            includeSubsampling,
            includeLossless);
        if (!info.CanDecodeToBmp)
        {
            ThrowHelper.ThrowCannotDecodeToBmp(info.Precision, info.Colorspace);
        }

        ValidateScalingFactor(info, options);

        var factor = options.ScalingFactor;
        var bitDepth = options.BmpBitDepth;
        var scaledWidth = ScaleDimension(info.Width, factor);
        var scaledHeight = ScaleDimension(info.Height, factor);
        ValidateCrop(info, options, scaledWidth, scaledHeight);

        var width = options.Crop?.Width ?? scaledWidth;
        var height = options.Crop?.Height ?? scaledHeight;

        var orientation = options.AutoOrient
            ? ExifOrientationReader.Read(jpeg)
            : ExifOrientation.Normal;
        var swapsAxes = orientation is >= ExifOrientation.Transpose and <= ExifOrientation.Rotate270;
        var outputWidth = swapsAxes ? height : width;
        var outputHeight = swapsAxes ? width : height;

        if (options.MaxPixels > 0 && (long)info.Width * info.Height > options.MaxPixels)
        {
            ThrowHelper.ThrowSourceImageTooLarge(info.Width, info.Height, options.MaxPixels);
        }

        var total = BmpHeaderWriter.GetFileSize(outputWidth, outputHeight, bitDepth);
        if (total > Array.MaxLength)
        {
            ThrowHelper.ThrowDecodedImageTooLarge(outputWidth, outputHeight, total);
        }

        int pixelFormat;
        switch (bitDepth)
        {
            case TurboJpegBmpBitDepth.Bpp8:
                pixelFormat = TurboJpegNativeApi.PixelFormatGray;
                break;
            case TurboJpegBmpBitDepth.Bpp24:
                pixelFormat = TurboJpegNativeApi.PixelFormatBgr;
                break;
            case TurboJpegBmpBitDepth.Bpp32:
                pixelFormat = TurboJpegNativeApi.PixelFormatBgra;
                break;
            default:
                ThrowHelper.ThrowBmpBitDepthOutOfRange(nameof(options), bitDepth, null);
                pixelFormat = default;
                break;
        }

        var pixelsPerMeterX = BmpHeaderWriter.ToPixelsPerMeter(info.DensityX, info.DensityUnits);
        var pixelsPerMeterY = BmpHeaderWriter.ToPixelsPerMeter(info.DensityY, info.DensityUnits);

        return new DecodePlan(
            width,
            height,
            outputWidth,
            outputHeight,
            bitDepth,
            orientation,
            pixelFormat,
            BmpHeaderWriter.GetPixelOffset(bitDepth),
            BmpHeaderWriter.GetBytesPerPixel(bitDepth),
            BmpHeaderWriter.GetRowStride(outputWidth, bitDepth),
            pixelsPerMeterX,
            pixelsPerMeterY,
            (int)total,
            swapsAxes);
    }

    private unsafe void DecodeWithPlan(
        ReadOnlySpan<byte> jpeg,
        TurboJpegDecodeOptions options,
        in DecodePlan plan,
        Span<byte> bmp)
    {
        var handle = _handle!.DangerousGetHandle();
        ApplyParameters(handle, options, plan.Orientation);

        BmpHeaderWriter.Write(
            bmp,
            plan.OutputWidth,
            plan.OutputHeight,
            plan.BitDepth,
            plan.PixelsPerMeterX,
            plan.PixelsPerMeterY);
        BmpHeaderWriter.ClearPadding(bmp, plan.OutputWidth, plan.OutputHeight, plan.BitDepth);

        if (!plan.SwapsAxes)
        {
            if (!options.StopOnWarning)
            {
                bmp[plan.PixelOffset..].Clear();
            }

            Decompress8Into(
                handle,
                jpeg,
                bmp[plan.PixelOffset..],
                plan.OutputStride,
                plan.PixelFormat,
                tolerateWarnings: !options.StopOnWarning);

            if (plan.Orientation is ExifOrientation.FlipHorizontal or ExifOrientation.Rotate180)
            {
                BmpOrientationWriter.MirrorRowsInPlace(
                    bmp[plan.PixelOffset..],
                    plan.Width,
                    plan.Height,
                    plan.OutputStride,
                    plan.BytesPerPixel);
            }

            return;
        }

        var sourcePitch = checked(plan.Width * plan.BytesPerPixel);
        var sourceByteCount = checked(sourcePitch * plan.Height);
        var sourceWithPadding = checked(sourceByteCount + 4);
        var scratch = ArrayPool<byte>.Shared.Rent(sourceWithPadding);

        try
        {
            var sourcePixels = scratch.AsSpan(0, sourceWithPadding);
            if (!options.StopOnWarning)
            {
                sourcePixels.Clear();
            }

            Decompress8Into(
                handle,
                jpeg,
                sourcePixels[..sourceByteCount],
                sourcePitch,
                plan.PixelFormat,
                tolerateWarnings: !options.StopOnWarning);

            BmpOrientationWriter.Write(
                bmp[plan.PixelOffset..],
                sourcePixels,
                plan.Width,
                plan.Height,
                plan.BytesPerPixel,
                plan.OutputStride,
                plan.Orientation);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(scratch);
        }
    }

    private void ApplyParameters(
        nint handle, TurboJpegDecodeOptions options, ExifOrientation orientation)
    {
        var bottomUp = orientation is ExifOrientation.Normal or ExifOrientation.FlipHorizontal
            ? 1
            : 0;
        ApplyParameter(handle, TurboJpegNativeApi.ParamBottomUp, bottomUp, ref _appliedBottomUp);
        ApplyParameter(handle, TurboJpegNativeApi.ParamMaxPixels, options.MaxPixels, ref _appliedMaxPixels);
        ApplyParameter(handle, TurboJpegNativeApi.ParamMaxMemory, options.MaxMemoryMegabytes, ref _appliedMaxMemory);
        ApplyParameter(handle, TurboJpegNativeApi.ParamScanLimit, options.ScanLimit, ref _appliedScanLimit);
        ApplyParameter(handle, TurboJpegNativeApi.ParamFastDct, options.FastDct ? 1 : 0, ref _appliedFastDct);
        ApplyParameter(
            handle,
            TurboJpegNativeApi.ParamFastUpsample,
            options.FastUpsampling ? 1 : 0,
            ref _appliedFastUpsample);
        ApplyScalingFactor(handle, options.ScalingFactor);
        ApplyCrop(handle, options.Crop);
    }

    private void ApplyWarningPolicy(nint handle, bool stopOnWarning)
    {
        ApplyParameter(
            handle,
            TurboJpegNativeApi.ParamStopOnWarning,
            stopOnWarning ? 1 : 0,
            ref _appliedStopOnWarning);
    }

    private readonly struct DecodePlan
    {
        internal DecodePlan(
            int width,
            int height,
            int outputWidth,
            int outputHeight,
            TurboJpegBmpBitDepth bitDepth,
            ExifOrientation orientation,
            int pixelFormat,
            int pixelOffset,
            int bytesPerPixel,
            int outputStride,
            int pixelsPerMeterX,
            int pixelsPerMeterY,
            int totalSize,
            bool swapsAxes)
        {
            Width = width;
            Height = height;
            OutputWidth = outputWidth;
            OutputHeight = outputHeight;
            BitDepth = bitDepth;
            Orientation = orientation;
            PixelFormat = pixelFormat;
            PixelOffset = pixelOffset;
            BytesPerPixel = bytesPerPixel;
            OutputStride = outputStride;
            PixelsPerMeterX = pixelsPerMeterX;
            PixelsPerMeterY = pixelsPerMeterY;
            TotalSize = totalSize;
            SwapsAxes = swapsAxes;
        }

        internal int Width { get; }

        internal int Height { get; }

        internal int OutputWidth { get; }

        internal int OutputHeight { get; }

        internal TurboJpegBmpBitDepth BitDepth { get; }

        internal ExifOrientation Orientation { get; }

        internal int PixelFormat { get; }

        internal int PixelOffset { get; }

        internal int BytesPerPixel { get; }

        internal int OutputStride { get; }

        internal int PixelsPerMeterX { get; }

        internal int PixelsPerMeterY { get; }

        internal int TotalSize { get; }

        internal bool SwapsAxes { get; }
    }

    private static unsafe void Decompress8Into(
        nint handle,
        ReadOnlySpan<byte> jpeg,
        Span<byte> destination,
        int pitch,
        int pixelFormat,
        bool tolerateWarnings)
    {
        fixed (byte* jpegBuf = jpeg)
        fixed (byte* dstBuf = destination)
        {
            if (TurboJpegNativeApi.Decompress8(
                    handle,
                    jpegBuf,
                    (nuint)jpeg.Length,
                    dstBuf,
                    pitch,
                    pixelFormat) != 0
                && (!tolerateWarnings
                    || TurboJpegNativeApi.GetErrorCode(handle) != TurboJpegNativeApi.ErrorWarning))
            {
                ThrowHelper.Throw(TurboJpegNative.CreateException(handle));
            }
        }
    }

    /// <summary>Releases the native TurboJPEG handle.</summary>
    public void Dispose()
    {
        _handle?.Dispose();
        _handle = null;
    }

    private unsafe TurboJpegImageInfo ReadHeader(
        ReadOnlySpan<byte> jpeg,
        bool tolerateWarnings,
        bool includeDensity,
        bool includeSubsampling,
        bool includeLossless)
    {
        var safeHandle = _handle;
        if (safeHandle is null)
        {
            ThrowHelper.ThrowDisposed();
        }

        var handle = safeHandle.DangerousGetHandle();

        try
        {
            fixed (byte* jpegBuf = jpeg)
            {
                if (TurboJpegNativeApi.DecompressHeader(handle, jpegBuf, (nuint)jpeg.Length) != 0
                    && (!tolerateWarnings
                        || TurboJpegNativeApi.GetErrorCode(handle) != TurboJpegNativeApi.ErrorWarning))
                {
                    ThrowHelper.Throw(TurboJpegNative.CreateException(handle));
                }
            }

            var width = TurboJpegNativeApi.Get(handle, TurboJpegNativeApi.ParamJpegWidth);
            var height = TurboJpegNativeApi.Get(handle, TurboJpegNativeApi.ParamJpegHeight);
            var precision = TurboJpegNativeApi.Get(handle, TurboJpegNativeApi.ParamPrecision);
            var colorspace = TurboJpegNativeApi.Get(handle, TurboJpegNativeApi.ParamColorspace);
            var progressive = TurboJpegNativeApi.Get(handle, TurboJpegNativeApi.ParamProgressive);
            var arithmetic = TurboJpegNativeApi.Get(handle, TurboJpegNativeApi.ParamArithmetic);
            var subsampling = includeSubsampling
                ? TurboJpegNativeApi.Get(handle, TurboJpegNativeApi.ParamSubsamp)
                : (int)TurboJpegSubsampling.Unknown;
            var lossless = includeLossless
                ? TurboJpegNativeApi.Get(handle, TurboJpegNativeApi.ParamLossless)
                : 0;
            var densityX = includeDensity
                ? TurboJpegNativeApi.Get(handle, TurboJpegNativeApi.ParamXDensity)
                : 0;
            var densityY = includeDensity
                ? TurboJpegNativeApi.Get(handle, TurboJpegNativeApi.ParamYDensity)
                : 0;
            var densityUnits = includeDensity
                ? TurboJpegNativeApi.Get(handle, TurboJpegNativeApi.ParamDensityUnits)
                : 0;

            if (width <= 0 || height <= 0)
            {
                ThrowHelper.ThrowInvalidImageDimensions(width, height);
            }

            return new TurboJpegImageInfo(
                width,
                height,
                precision,
                (TurboJpegColorspace)colorspace,
                (TurboJpegSubsampling)subsampling,
                lossless != 0)
            {
                DensityX = densityX,
                DensityY = densityY,
                DensityUnits = (TurboJpegDensityUnits)densityUnits,
                IsProgressive = progressive != 0,
                UsesArithmeticCoding = arithmetic != 0
            };
        }
        catch (TurboJpegException)
        {
            try
            {
                RecreateHandleAfterFailure();
            }
            catch
            {
                // WHY: recovery is best-effort; the original header error is rethrown below.
                _handle = null;
            }

            throw;
        }
    }

    private void RecreateHandleAfterFailure()
    {
        _handle?.Dispose();
        _handle = null;
        _appliedStopOnWarning = UnsetParameter;
        _appliedBottomUp = UnsetParameter;
        _appliedMaxPixels = UnsetParameter;
        _appliedMaxMemory = UnsetParameter;
        _appliedScanLimit = UnsetParameter;
        _appliedFastDct = UnsetParameter;
        _appliedFastUpsample = UnsetParameter;
        _appliedScalingFactor = null;
        _appliedCrop = null;
        _handle = _handleFactory();
    }

    private void ThrowIfDisposed()
    {
        if (_handle is null)
        {
            ThrowHelper.ThrowDisposed();
        }
    }

    private static void ValidateScalingFactor(TurboJpegImageInfo info, TurboJpegDecodeOptions options)
    {
        var factor = options.ScalingFactor;

        if (factor.Numerator <= 0 || factor.Denominator <= 0)
        {
            ThrowHelper.ThrowScalingFactorNotPositive(factor.Numerator, factor.Denominator);
        }

        if (Array.IndexOf(TurboJpegNative.GetSupportedScalingFactors(), factor) < 0)
        {
            ThrowHelper.ThrowScalingFactorUnsupported(factor.Numerator, factor.Denominator);
        }

        if (info.IsLossless && factor != TurboJpegScalingFactor.One)
        {
            ThrowHelper.ThrowScalingFactorUnsupportedForLossless(
                factor.Numerator, factor.Denominator);
        }
    }

    private static void ValidateCrop(
        TurboJpegImageInfo info,
        TurboJpegDecodeOptions options,
        int scaledWidth,
        int scaledHeight)
    {
        if (options.Crop is not { } crop)
        {
            return;
        }

        if (crop.Width <= 0 || crop.Height <= 0)
        {
            ThrowHelper.ThrowCropSizeNotPositive(crop.Width, crop.Height);
        }

        if (info.IsLossless)
        {
            ThrowHelper.ThrowCropUnsupportedForLossless();
        }

        if (info.Subsampling == TurboJpegSubsampling.Unknown)
        {
            ThrowHelper.ThrowCropUnsupportedForUnknownSubsampling();
        }

        if ((long)crop.X + crop.Width > scaledWidth
            || (long)crop.Y + crop.Height > scaledHeight)
        {
            ThrowHelper.ThrowCropOutsideImage(
                crop.X, crop.Y, crop.Width, crop.Height, scaledWidth, scaledHeight);
        }

        var factor = options.ScalingFactor;
        var scaledImcuWidth =
            (GetImcuWidth(info.Subsampling) * factor.Numerator + factor.Denominator - 1)
            / factor.Denominator;
        if (crop.X % scaledImcuWidth != 0)
        {
            ThrowHelper.ThrowCropMisaligned(crop.X, scaledImcuWidth);
        }
    }

    private static int GetImcuWidth(TurboJpegSubsampling subsampling) => subsampling switch
    {
        TurboJpegSubsampling.Yuv444 => 8,
        TurboJpegSubsampling.Yuv422 => 16,
        TurboJpegSubsampling.Yuv420 => 16,
        TurboJpegSubsampling.Grayscale => 8,
        TurboJpegSubsampling.Yuv440 => 8,
        TurboJpegSubsampling.Yuv411 => 32,
        TurboJpegSubsampling.Yuv441 => 8,
        TurboJpegSubsampling.Yuv410 => 32,
        TurboJpegSubsampling.Yuv24 => 16,
        _ => 0,
    };

    private static int ScaleDimension(int dimension, TurboJpegScalingFactor factor)
    {
        var scaled = ((long)dimension * factor.Numerator + factor.Denominator - 1) / factor.Denominator;
        if (scaled > int.MaxValue)
        {
            ThrowHelper.ThrowScaledDimensionTooLarge(scaled);
        }

        return (int)scaled;
    }

    private void ApplyParameter(
        nint handle, int parameter, int value, ref int appliedValue)
    {
        if (appliedValue == value)
        {
            return;
        }

        SetParameter(handle, parameter, value);
        appliedValue = value;
    }

    private void ApplyScalingFactor(nint handle, TurboJpegScalingFactor factor)
    {
        if (_appliedScalingFactor == factor)
        {
            return;
        }

        if (TurboJpegNativeApi.SetScalingFactor(
                handle,
                new TjScalingFactor
                {
                    Numerator = factor.Numerator,
                    Denominator = factor.Denominator,
                }) != 0)
        {
            ThrowHelper.Throw(TurboJpegNative.CreateException(handle));
        }

        _appliedScalingFactor = factor;
    }

    private void ApplyCrop(nint handle, TurboJpegCrop? crop)
    {
        if (_appliedCrop == crop)
        {
            return;
        }

        var region = crop is { } value
            ? new TjRegion
            {
                X = value.X,
                Y = value.Y,
                W = value.Width,
                H = value.Height,
            }
            : new TjRegion();

        if (TurboJpegNativeApi.SetCroppingRegion(handle, region) != 0)
        {
            ThrowHelper.Throw(TurboJpegNative.CreateException(handle));
        }

        _appliedCrop = crop;
    }

    private static void SetParameter(nint handle, int parameter, int value)
    {
        if (TurboJpegNativeApi.Set(handle, parameter, value) != 0)
        {
            ThrowHelper.Throw(TurboJpegNative.CreateException(handle));
        }
    }
}
