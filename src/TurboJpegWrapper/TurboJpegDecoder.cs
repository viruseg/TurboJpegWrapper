using System.Buffers;
using TurboJpegWrapper.Internal.Bmp;
using TurboJpegWrapper.Internal.Exif;
using TurboJpegWrapper.Internal.Native;

namespace TurboJpegWrapper;

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
    private int _appliedBottomUp = UnsetParameter;
    private int _appliedMaxPixels = UnsetParameter;
    private int _appliedMaxMemory = UnsetParameter;
    private int _appliedScanLimit = UnsetParameter;
    private int _appliedFastDct = UnsetParameter;
    private int _appliedFastUpsample = UnsetParameter;
    private TurboJpegScalingFactor? _appliedScalingFactor;

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
            throw new ArgumentException("JPEG data must not be empty.", nameof(jpeg));
        }

        return ReadHeader(jpeg);
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

        var plan = CreatePlan(jpeg, options);
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

        var plan = CreatePlan(jpeg, options);
        if (destination.Length < plan.TotalSize)
        {
            throw new ArgumentException(
                $"The destination must be at least {plan.TotalSize} bytes long.", nameof(destination));
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

        return CreatePlan(jpeg, options).TotalSize;
    }

    private DecodePlan CreatePlan(ReadOnlySpan<byte> jpeg, TurboJpegDecodeOptions options)
    {
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

        ValidateScalingFactor(info, options);

        var factor = options.ScalingFactor;
        var bitDepth = options.BmpBitDepth;
        var width = ScaleDimension(info.Width, factor);
        var height = ScaleDimension(info.Height, factor);

        var orientation = options.AutoOrient
            ? ExifOrientationReader.Read(jpeg)
            : ExifOrientation.Normal;
        var swapsAxes = orientation is >= ExifOrientation.Transpose and <= ExifOrientation.Rotate270;
        var outputWidth = swapsAxes ? height : width;
        var outputHeight = swapsAxes ? width : height;

        if (options.MaxPixels > 0 && (long)info.Width * info.Height > options.MaxPixels)
        {
            throw new TurboJpegImageTooLargeException(
                $"The source image ({info.Width}x{info.Height}) contains " +
                $"{(long)info.Width * info.Height} pixels, " +
                $"which exceeds the configured maximum of {options.MaxPixels} pixels.");
        }

        var total = BmpHeaderWriter.GetFileSize(outputWidth, outputHeight, bitDepth);
        if (total > Array.MaxLength)
        {
            throw new TurboJpegImageTooLargeException(
                $"The decoded image ({outputWidth}x{outputHeight}) requires {total} bytes, " +
                $"which exceeds the maximum array length of {Array.MaxLength} bytes.");
        }

        var pixelFormat = bitDepth switch
        {
            TurboJpegBmpBitDepth.Bpp8 => TurboJpegNative.PixelFormatGray,
            TurboJpegBmpBitDepth.Bpp24 => TurboJpegNative.PixelFormatBgr,
            TurboJpegBmpBitDepth.Bpp32 => TurboJpegNative.PixelFormatBgra,
            _ => throw new ArgumentOutOfRangeException(nameof(options), bitDepth, null)
        };

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
            (int)total,
            swapsAxes);
    }

    private unsafe void DecodeWithPlan(
        ReadOnlySpan<byte> jpeg,
        TurboJpegDecodeOptions options,
        in DecodePlan plan,
        Span<byte> bmp)
    {
        var handle = _handle!;
        ApplyParameters(handle, options, plan.Orientation);

        BmpHeaderWriter.Write(bmp, plan.OutputWidth, plan.OutputHeight, plan.BitDepth);
        BmpHeaderWriter.ClearPadding(bmp, plan.OutputWidth, plan.OutputHeight, plan.BitDepth);

        if (!plan.SwapsAxes)
        {
            DecompressInto(handle, jpeg, bmp[plan.PixelOffset..], plan.OutputStride, plan.PixelFormat);

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
        var scratch = ArrayPool<byte>.Shared.Rent(sourceByteCount);

        try
        {
            var sourcePixels = scratch.AsSpan(0, sourceByteCount);
            DecompressInto(handle, jpeg, sourcePixels, sourcePitch, plan.PixelFormat);

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
        SafeTurboJpegHandle handle, TurboJpegDecodeOptions options, ExifOrientation orientation)
    {
        var bottomUp = orientation is ExifOrientation.Normal or ExifOrientation.FlipHorizontal
            ? 1
            : 0;
        ApplyParameter(handle, TurboJpegNative.ParamBottomUp, bottomUp, ref _appliedBottomUp);
        ApplyParameter(handle, TurboJpegNative.ParamMaxPixels, options.MaxPixels, ref _appliedMaxPixels);
        ApplyParameter(handle, TurboJpegNative.ParamMaxMemory, options.MaxMemoryMegabytes, ref _appliedMaxMemory);
        ApplyParameter(handle, TurboJpegNative.ParamScanLimit, options.ScanLimit, ref _appliedScanLimit);
        ApplyParameter(handle, TurboJpegNative.ParamFastDct, options.FastDct ? 1 : 0, ref _appliedFastDct);
        ApplyParameter(
            handle,
            TurboJpegNative.ParamFastUpsample,
            options.FastUpsampling ? 1 : 0,
            ref _appliedFastUpsample);
        ApplyScalingFactor(handle, options.ScalingFactor);
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

        internal int TotalSize { get; }

        internal bool SwapsAxes { get; }
    }

    private static unsafe void DecompressInto(
        SafeTurboJpegHandle handle,
        ReadOnlySpan<byte> jpeg,
        Span<byte> destination,
        int pitch,
        int pixelFormat)
    {
        fixed (byte* jpegBuf = jpeg)
        fixed (byte* dstBuf = destination)
        {
            if (TurboJpegNative.Decompress8(
                    handle,
                    jpegBuf,
                    (nuint)jpeg.Length,
                    dstBuf,
                    pitch,
                    pixelFormat) != 0)
            {
                throw TurboJpegNative.CreateException(handle);
            }
        }
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

        try
        {
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
        _appliedBottomUp = UnsetParameter;
        _appliedMaxPixels = UnsetParameter;
        _appliedMaxMemory = UnsetParameter;
        _appliedScanLimit = UnsetParameter;
        _appliedFastDct = UnsetParameter;
        _appliedFastUpsample = UnsetParameter;
        _appliedScalingFactor = null;
        _handle = _handleFactory();
    }

    private void ThrowIfDisposed()
    {
        if (_handle is null)
        {
            throw new ObjectDisposedException(nameof(TurboJpegDecoder));
        }
    }

    private static void ValidateScalingFactor(TurboJpegImageInfo info, TurboJpegDecodeOptions options)
    {
        var factor = options.ScalingFactor;

        if (factor.Numerator <= 0 || factor.Denominator <= 0)
        {
            throw new ArgumentException(
                $"The scaling factor {factor.Numerator}/{factor.Denominator} must have " +
                "a positive numerator and denominator.",
                nameof(options));
        }

        if (Array.IndexOf(TurboJpegNative.GetSupportedScalingFactors(), factor) < 0)
        {
            throw new ArgumentException(
                $"The scaling factor {factor.Numerator}/{factor.Denominator} is not supported by TurboJPEG.",
                nameof(options));
        }

        if (info.IsLossless && factor != TurboJpegScalingFactor.One)
        {
            throw new ArgumentException(
                $"The scaling factor {factor.Numerator}/{factor.Denominator} is not supported " +
                "for lossless JPEG images; use 1/1.",
                nameof(options));
        }
    }

    private static int ScaleDimension(int dimension, TurboJpegScalingFactor factor)
    {
        var scaled = ((long)dimension * factor.Numerator + factor.Denominator - 1) / factor.Denominator;
        if (scaled > int.MaxValue)
        {
            throw new TurboJpegImageTooLargeException(
                $"The scaled image dimension {scaled} exceeds the maximum supported dimension of {int.MaxValue}.");
        }

        return (int)scaled;
    }

    private void ApplyParameter(
        SafeTurboJpegHandle handle, int parameter, int value, ref int appliedValue)
    {
        if (appliedValue == value)
        {
            return;
        }

        SetParameter(handle, parameter, value);
        appliedValue = value;
    }

    private void ApplyScalingFactor(SafeTurboJpegHandle handle, TurboJpegScalingFactor factor)
    {
        if (_appliedScalingFactor == factor)
        {
            return;
        }

        if (TurboJpegNative.SetScalingFactor(
                handle,
                new TjScalingFactor
                {
                    Numerator = factor.Numerator,
                    Denominator = factor.Denominator,
                }) != 0)
        {
            throw TurboJpegNative.CreateException(handle);
        }

        _appliedScalingFactor = factor;
    }

    private static void SetParameter(SafeTurboJpegHandle handle, int parameter, int value)
    {
        if (TurboJpegNative.Set(handle, parameter, value) != 0)
        {
            throw TurboJpegNative.CreateException(handle);
        }
    }
}
