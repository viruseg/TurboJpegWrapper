using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TurboJpegWrapper.Internal.Exif;
using TurboJpegWrapper.Internal.Native;

namespace TurboJpegWrapper.Internal;

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
    internal static void ThrowCacheFactoryReturnedNull() =>
        throw new InvalidOperationException("The cache factory returned null.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Throw(Exception exception) =>
        throw exception;

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowNativeLoadingUnsupported(OSPlatform os, Architecture architecture) =>
        throw new PlatformNotSupportedException(
            $"TurboJPEG native library loading is not supported on {os} with architecture " +
            $"{architecture}. Set {TurboJpegNativeLoader.OverrideEnvironmentVariable} to an explicit library path.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowNativeLoadFailed(
        IReadOnlyList<string> attempts, Exception lastFailure) =>
        throw new TurboJpegException(
            $"Failed to load the TurboJPEG native library (libjpeg-turbo 3.2.0, " +
            $"TurboJPEG API version {TurboJpegNative.ApiVersion}). " +
            $"Attempts: {string.Join("; ", attempts)}. " +
            $"Set {TurboJpegNativeLoader.OverrideEnvironmentVariable} to an explicit library path.",
            lastFailure);

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowLibraryFileNameUnknown(OSPlatform os) =>
        throw new PlatformNotSupportedException(
            $"The TurboJPEG native library file name is not known for OS '{os}'.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowPlatformUnsupported() =>
        throw new PlatformNotSupportedException(
            "TurboJPEG native library loading is supported only on Windows, Linux, and macOS.");
}
