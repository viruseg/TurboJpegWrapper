using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TurboJpegWrapper.Internal.Encoder;

[StackTraceHidden]
internal static class ThrowHelper
{
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpEmpty() =>
        throw new ArgumentException("BMP data must not be empty.", "bmp");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowDisposed() =>
        throw new ObjectDisposedException("TurboJpegEncoder");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowDestinationTooSmall(int requiredBytes) =>
        throw new ArgumentException(
            $"The destination must be at least {requiredBytes} bytes long.", "destination");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowDestinationOverlapsSource() =>
        throw new ArgumentException(
            "The destination must not overlap the BMP source buffer.", "destination");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowJpegSizeZero(int width, int height) =>
        throw new TurboJpegImageTooLargeException(
            $"TurboJPEG reported a zero worst-case JPEG size for a {width}x{height} image.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowJpegTooLarge(int width, int height, nuint size) =>
        throw new TurboJpegImageTooLargeException(
            $"The worst-case JPEG size for a {width}x{height} image is {size} bytes, " +
            $"which exceeds the maximum array length of {Array.MaxLength} bytes.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowRestartOptionsConflict() =>
        throw new ArgumentException(
            "RestartBlocks and RestartRows cannot both be non-zero.", "options");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowLosslessRestartBlocks() =>
        throw new ArgumentException(
            "RestartBlocks must be zero when lossless compression is enabled.", "options");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowDensityWithLossless() =>
        throw new ArgumentException(
            "Density cannot be specified for lossless JPEG output, which does not store density.",
            "options");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowDensityWithRgb() =>
        throw new ArgumentException(
            "Density cannot be specified for RGB JPEG output; only YCbCr and grayscale JPEG files store density.",
            "options");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Throw(Exception exception) =>
        throw exception;
}
