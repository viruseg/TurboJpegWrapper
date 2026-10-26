using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TurboJpegWrapper.Decoder.Internal.Exif;

namespace TurboJpegWrapper.Decoder.Internal;

[StackTraceHidden]
internal static class ThrowHelper
{
    private const string BmpBitDepthMessage = "The BMP bit depth must be 8, 24, or 32.";

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowJpegEmpty() =>
        throw new ArgumentException("JPEG data must not be empty.", "jpeg");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowDestinationTooSmall(int requiredBytes) =>
        throw new ArgumentException(
            $"The destination must be at least {requiredBytes} bytes long.", "destination");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpBitDepthOutOfRange(
        string paramName, TurboJpegBmpBitDepth value, string? message = BmpBitDepthMessage) =>
        throw new ArgumentOutOfRangeException(paramName, value, message);

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowCannotDecodeToBmp(int precision, TurboJpegColorspace colorspace) =>
        throw new TurboJpegUnsupportedException(
            $"Cannot decode a JPEG with {precision}-bit precision and {colorspace} colorspace to BMP.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowUnsupportedPrecision(int precision) =>
        throw new TurboJpegUnsupportedException(
            $"The JPEG precision {precision} is not supported; supported precision is 2-16 bits.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowSourceImageTooLarge(int width, int height, int maxPixels) =>
        throw new TurboJpegImageTooLargeException(
            $"The source image ({width}x{height}) contains {(long)width * height} pixels, " +
            $"which exceeds the configured maximum of {maxPixels} pixels.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowDecodedImageTooLarge(int width, int height, long totalBytes) =>
        throw new TurboJpegImageTooLargeException(
            $"The decoded image ({width}x{height}) requires {totalBytes} bytes, " +
            $"which exceeds the maximum array length of {Array.MaxLength} bytes.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowDisposed() =>
        throw new ObjectDisposedException("TurboJpegDecoder");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowInvalidImageDimensions(int width, int height) =>
        throw new TurboJpegException(
            $"TurboJPEG reported invalid image dimensions {width}x{height}.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowScalingFactorNotPositive(int numerator, int denominator) =>
        throw new ArgumentException(
            $"The scaling factor {numerator}/{denominator} must have " +
            "a positive numerator and denominator.",
            "options");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowScalingFactorUnsupported(int numerator, int denominator) =>
        throw new ArgumentException(
            $"The scaling factor {numerator}/{denominator} is not supported by TurboJPEG.",
            "options");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowScalingFactorUnsupportedForLossless(int numerator, int denominator) =>
        throw new ArgumentException(
            $"The scaling factor {numerator}/{denominator} is not supported " +
            "for lossless JPEG images; use 1/1.",
            "options");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowCropSizeNotPositive(int width, int height) =>
        throw new ArgumentException(
            $"The crop region must have positive dimensions; got {width}x{height}.",
            "options");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowCropUnsupportedForLossless() =>
        throw new ArgumentException(
            "Cropping is not supported for lossless JPEG images.",
            "options");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowCropUnsupportedForUnknownSubsampling() =>
        throw new ArgumentException(
            "Cropping requires a JPEG with a known chroma subsampling scheme.",
            "options");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowCropOutsideImage(
        int x, int y, int width, int height, int imageWidth, int imageHeight) =>
        throw new ArgumentException(
            $"The crop region ({x},{y},{width}x{height}) does not fit inside " +
            $"the {imageWidth}x{imageHeight} scaled image.",
            "options");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowCropMisaligned(int x, int scaledImcuWidth) =>
        throw new ArgumentException(
            $"The crop X offset {x} must be divisible by the scaled iMCU width " +
            $"{scaledImcuWidth}.",
            "options");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowScaledDimensionTooLarge(long scaledDimension) =>
        throw new TurboJpegImageTooLargeException(
            $"The scaled image dimension {scaledDimension} exceeds the maximum supported dimension of {int.MaxValue}.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBytesPerPixelOutOfRange(int bytesPerPixel) =>
        throw new ArgumentOutOfRangeException("bytesPerPixel", bytesPerPixel, null);

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowOrientationOutOfRange(ExifOrientation orientation) =>
        throw new ArgumentOutOfRangeException("orientation", orientation, null);

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowWidthNotPositive(int width) =>
        throw new ArgumentOutOfRangeException(
            "width", width, "The width must be greater than zero.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowHeightNotPositive(int height) =>
        throw new ArgumentOutOfRangeException(
            "height", height, "The height must be greater than zero.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Throw(Exception exception) =>
        throw exception;
}
