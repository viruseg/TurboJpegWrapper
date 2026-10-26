using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TurboJpegWrapper.Encoder.Internal;

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
    internal static void Throw(Exception exception) =>
        throw exception;
}
