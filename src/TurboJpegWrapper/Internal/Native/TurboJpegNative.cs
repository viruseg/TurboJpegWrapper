using System.Runtime.InteropServices;

namespace TurboJpegWrapper.Internal.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct TjScalingFactor
{
    internal int Numerator;
    internal int Denominator;
}

internal static unsafe partial class TurboJpegNative
{
    private static readonly Lazy<TurboJpegScalingFactor[]> s_filteredScalingFactors =
        new(LoadFilteredScalingFactors);

    internal const string LibraryName = "turbojpeg";

    internal const int InitDecompress = 1;
    internal const int ApiVersion = 3_002_000;
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
    internal static partial int DecompressHeader(SafeTurboJpegHandle handle, byte* jpegBuf, nuint jpegSize);

    [LibraryImport(LibraryName, EntryPoint = "tj3Decompress8")]
    internal static partial int Decompress8(
        SafeTurboJpegHandle handle,
        byte* jpegBuf,
        nuint jpegSize,
        byte* dstBuf,
        int pitch,
        int pixelFormat);

    [LibraryImport(LibraryName, EntryPoint = "tj3Get")]
    internal static partial int Get(SafeTurboJpegHandle handle, int param);

    [LibraryImport(LibraryName, EntryPoint = "tj3Set")]
    internal static partial int Set(SafeTurboJpegHandle handle, int param, int value);

    [LibraryImport(LibraryName, EntryPoint = "tj3GetErrorStr")]
    internal static partial nint GetErrorStr(SafeTurboJpegHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "tj3GetErrorStr")]
    internal static partial nint GetErrorStr(nint handle);

    [LibraryImport(LibraryName, EntryPoint = "tj3GetErrorCode")]
    internal static partial int GetErrorCode(SafeTurboJpegHandle handle);

    [LibraryImport(LibraryName, EntryPoint = "tj3GetScalingFactors")]
    internal static partial TjScalingFactor* GetScalingFactors(out int count);

    [LibraryImport(LibraryName, EntryPoint = "tj3SetScalingFactor")]
    internal static partial int SetScalingFactor(SafeTurboJpegHandle handle, TjScalingFactor factor);

    internal static TurboJpegScalingFactor[] GetFilteredScalingFactors() =>
        s_filteredScalingFactors.Value;

    private static TurboJpegScalingFactor[] LoadFilteredScalingFactors()
    {
        var nativeFactors = GetScalingFactors(out var count);
        if (nativeFactors == null || count <= 0)
        {
            throw CreateException(null);
        }

        var factors = new TurboJpegScalingFactor[count];
        var filteredCount = 0;
        for (var i = 0; i < count; i++)
        {
            var factor = nativeFactors[i];
            if (factor.Numerator <= factor.Denominator)
            {
                factors[filteredCount++] = new TurboJpegScalingFactor(factor.Numerator, factor.Denominator);
            }
        }

        if (filteredCount != count)
        {
            Array.Resize(ref factors, filteredCount);
        }

        return factors;
    }

    internal static SafeTurboJpegHandle CreateDecoderHandle()
    {
        nint handle;
        try
        {
            handle = InitVersion(InitDecompress, ApiVersion);
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            throw CreateLoadException(ex);
        }

        if (handle == nint.Zero)
        {
            throw CreateException(null);
        }

        var safeHandle = new SafeTurboJpegHandle(handle);
        if (Set(safeHandle, ParamSaveMarkers, 0) != 0)
        {
            var exception = CreateException(safeHandle);
            safeHandle.Dispose();
            throw exception;
        }

        return safeHandle;
    }

    internal static TurboJpegException CreateException(SafeTurboJpegHandle? handle)
    {
        if (handle is null)
        {
            var globalMessage = Marshal.PtrToStringUTF8(GetErrorStr(nint.Zero));
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
