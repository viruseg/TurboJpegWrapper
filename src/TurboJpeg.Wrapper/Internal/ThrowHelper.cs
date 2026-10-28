using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TurboJpegWrapper.Internal.Native;

namespace TurboJpegWrapper.Internal;

[StackTraceHidden]
internal static class ThrowHelper
{
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowDensityUnitsOutOfRange(TurboJpegDensityUnits units) =>
        throw new ArgumentOutOfRangeException(
            nameof(TurboJpegDensity.Units),
            units,
            "The density units must be Unknown, PixelsPerInch, or PixelsPerCentimeter.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowCacheFactoryReturnedNull() =>
        throw new InvalidOperationException("The cache factory returned null.");

    internal static PlatformNotSupportedException CreateNativeLibraryLoadUnsupported(
        OSPlatform os, Architecture architecture) =>
        new(
            $"TurboJPEG native library loading is not supported on {os} with architecture " +
            $"{architecture}. Set {TurboJpegNativeLoader.OverrideEnvironmentVariable} to an explicit library path.");

    internal static PlatformNotSupportedException CreateNativeLibraryFileNameUnsupported(OSPlatform os) =>
        new($"The TurboJPEG native library file name is not known for OS '{os}'.");

    internal static PlatformNotSupportedException CreateNativeLibraryPlatformUnsupported() =>
        new("TurboJPEG native library loading is supported only on Windows, Linux, and macOS.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowExceptionFactoryNotRegistered() =>
        throw new InvalidOperationException(
            "The TurboJPEG exception factory has not been registered. The managed package must call " +
            $"{nameof(TurboJpegNativeLoader)}.{nameof(TurboJpegNativeLoader.EnsureRegistered)} from a module initializer.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Throw(Exception exception) =>
        throw exception;
}
