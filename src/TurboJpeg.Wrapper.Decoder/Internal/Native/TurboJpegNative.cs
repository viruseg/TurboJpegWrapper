using System.Runtime.InteropServices;
using TurboJpeg.Wrapper.Decoder.Internal;

namespace TurboJpeg.Wrapper.Decoder.Internal.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct TjScalingFactor
{
    internal int Numerator;
    internal int Denominator;
}

internal static unsafe partial class TurboJpegNative
{
    private static readonly RetryableCache<TurboJpegScalingFactor[]> s_supportedScalingFactors =
        new(LoadSupportedScalingFactors);

    internal const string LibraryName = "turbojpeg";

    internal const int InitDecompress = 1;
    internal const int ApiVersion = 3_002_000;
    internal const int PixelFormatBgr = 1;
    internal const int PixelFormatGray = 6;
    internal const int PixelFormatBgra = 8;

    internal const int ParamBottomUp = 1;
    internal const int ParamSubsamp = 4;
    internal const int ParamJpegWidth = 5;
    internal const int ParamJpegHeight = 6;
    internal const int ParamPrecision = 7;
    internal const int ParamColorspace = 8;
    internal const int ParamFastUpsample = 9;
    internal const int ParamFastDct = 10;
    internal const int ParamScanLimit = 13;
    internal const int ParamLossless = 15;
    internal const int ParamXDensity = 20;
    internal const int ParamYDensity = 21;
    internal const int ParamDensityUnits = 22;
    internal const int ParamMaxMemory = 23;
    internal const int ParamMaxPixels = 24;
    internal const int ParamSaveMarkers = 25;

    internal const int ErrorWarning = 0;
    internal const int ErrorFatal = 1;

    static TurboJpegNative()
    {
        TurboJpegNativeLoader.EnsureRegistered();
    }

    [LibraryImport(LibraryName, EntryPoint = "tj3InitVersion")]
    internal static partial nint InitVersion(int initType, int apiVersion);

    [LibraryImport(LibraryName, EntryPoint = "tj3Destroy")]
    internal static partial void Destroy(nint handle);

    [LibraryImport(LibraryName, EntryPoint = "tj3DecompressHeader")]
    internal static partial int DecompressHeader(nint handle, byte* jpegBuf, nuint jpegSize);

    [LibraryImport(LibraryName, EntryPoint = "tj3Decompress8")]
    internal static partial int Decompress8(
        nint handle,
        byte* jpegBuf,
        nuint jpegSize,
        byte* dstBuf,
        int pitch,
        int pixelFormat);

    [LibraryImport(LibraryName, EntryPoint = "tj3Get")]
    internal static partial int Get(nint handle, int param);

    [LibraryImport(LibraryName, EntryPoint = "tj3Set")]
    internal static partial int Set(nint handle, int param, int value);

    [LibraryImport(LibraryName, EntryPoint = "tj3GetErrorStr")]
    internal static partial nint GetErrorStr(nint handle);

    [LibraryImport(LibraryName, EntryPoint = "tj3GetErrorCode")]
    internal static partial int GetErrorCode(nint handle);

    [LibraryImport(LibraryName, EntryPoint = "tj3GetScalingFactors")]
    internal static partial TjScalingFactor* GetScalingFactors(out int count);

    [LibraryImport(LibraryName, EntryPoint = "tj3SetScalingFactor")]
    internal static partial int SetScalingFactor(nint handle, TjScalingFactor factor);

    internal static TurboJpegScalingFactor[] GetSupportedScalingFactors() =>
        s_supportedScalingFactors.GetOrCreate();

    private static TurboJpegScalingFactor[] LoadSupportedScalingFactors()
    {
        var nativeFactors = GetScalingFactors(out var count);
        if (nativeFactors == null || count <= 0)
        {
            ThrowHelper.Throw(CreateException(0));
        }

        var factors = new TurboJpegScalingFactor[count];
        for (var i = 0; i < count; i++)
        {
            var factor = nativeFactors[i];
            factors[i] = new TurboJpegScalingFactor(factor.Numerator, factor.Denominator);
        }

        return factors;
    }

    internal static SafeTurboJpegHandle CreateDecoderHandle()
    {
        nint handle = nint.Zero;
        try
        {
            handle = InitVersion(InitDecompress, ApiVersion);
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            ThrowHelper.Throw(CreateLoadException(ex));
        }

        if (handle == nint.Zero)
        {
            ThrowHelper.Throw(CreateException(0));
        }

        var safeHandle = new SafeTurboJpegHandle(handle);
        if (Set(handle, ParamSaveMarkers, 0) != 0)
        {
            var exception = CreateException(handle);
            safeHandle.Dispose();
            ThrowHelper.Throw(exception);
        }

        return safeHandle;
    }

    internal static TurboJpegException CreateException(nint handle)
    {
        if (handle == 0)
        {
            var globalMessage = Marshal.PtrToStringUTF8(GetErrorStr(0));
            return new TurboJpegException(
                string.IsNullOrWhiteSpace(globalMessage)
                    ? "TurboJPEG failed to initialize a decompression handle."
                    : globalMessage,
                isFatal: true);
        }

        var errorCode = GetErrorCode(handle);
        var message = Marshal.PtrToStringUTF8(GetErrorStr(handle));

        return new TurboJpegException(
            string.IsNullOrWhiteSpace(message) ? "TurboJPEG reported an unknown error." : message,
            isFatal: errorCode == ErrorFatal);
    }

    internal static TurboJpegException CreateLoadException(Exception innerException)
    {
        return CreateLoadException(
            innerException,
            Environment.GetEnvironmentVariable(TurboJpegNativeLoader.OverrideEnvironmentVariable));
    }

    internal static TurboJpegException CreateLoadException(Exception innerException, string? overridePath)
    {
        var version = $"libjpeg-turbo 3.2.0, TurboJPEG API version {ApiVersion}";

        if (innerException is EntryPointNotFoundException)
        {
            var foundAt = string.IsNullOrWhiteSpace(overridePath)
                ? "in one of the candidate locations or on the operating system search path"
                : $"at the configured override path '{overridePath}'";

            return new TurboJpegException(
                $"The TurboJPEG native library was found {foundAt}, but a required entry point is missing. " +
                $"This library requires {version}; install TurboJPEG 3.2.0 or newer. " +
                $"Reason: {innerException.Message}",
                innerException);
        }

        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return new TurboJpegException(
                $"Failed to load the TurboJPEG native library from '{overridePath}' specified by " +
                $"{TurboJpegNativeLoader.OverrideEnvironmentVariable} ({version}). " +
                $"The operating system search path was not used. Reason: {innerException.Message}",
                innerException);
        }

        return new TurboJpegException(
            $"Failed to load the TurboJPEG native library ({version}). " +
            $"Expected the library at one of: {string.Join(", ", TurboJpegNativeLoader.GetCandidatePaths())}, " +
            $"or on the operating system search path. Reason: {innerException.Message}",
            innerException);
    }
}
