using System.Runtime.InteropServices;
using TurboJpegWrapper.Decoder.Internal;
using TurboJpegWrapper.Internal;
using TurboJpegWrapper.Internal.Native;

namespace TurboJpegWrapper.Decoder.Internal.Native;

internal static unsafe class TurboJpegNative
{
    private static readonly RetryableCache<TurboJpegScalingFactor[]> s_supportedScalingFactors =
        new(LoadSupportedScalingFactors);

    internal static SafeTurboJpegHandle CreateDecoderHandle()
    {
        nint handle = nint.Zero;
        try
        {
            handle = TurboJpegNativeApi.InitVersion(
                TurboJpegNativeApi.InitDecompress, TurboJpegNativeApi.ApiVersion);
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
        if (TurboJpegNativeApi.Set(handle, TurboJpegNativeApi.ParamSaveMarkers, 0) != 0)
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
            var globalMessage = Marshal.PtrToStringUTF8(TurboJpegNativeApi.GetErrorStr(0));
            return new TurboJpegException(
                string.IsNullOrWhiteSpace(globalMessage)
                    ? "TurboJPEG failed to initialize a decompression handle."
                    : globalMessage,
                isFatal: true);
        }

        var errorCode = TurboJpegNativeApi.GetErrorCode(handle);
        var message = Marshal.PtrToStringUTF8(TurboJpegNativeApi.GetErrorStr(handle));

        return new TurboJpegException(
            string.IsNullOrWhiteSpace(message) ? "TurboJPEG reported an unknown error." : message,
            isFatal: errorCode == TurboJpegNativeApi.ErrorFatal);
    }

    internal static TurboJpegException CreateLoadException(Exception innerException)
    {
        return CreateLoadException(
            innerException,
            Environment.GetEnvironmentVariable(TurboJpegNativeLoader.OverrideEnvironmentVariable));
    }

    internal static TurboJpegException CreateLoadException(Exception innerException, string? overridePath)
    {
        return (TurboJpegException)TurboJpegNativeLoader.CreateLoadException(
            innerException, overridePath);
    }

    internal static TurboJpegScalingFactor[] GetSupportedScalingFactors() =>
        s_supportedScalingFactors.GetOrCreate();

    private static TurboJpegScalingFactor[] LoadSupportedScalingFactors()
    {
        var nativeFactors = TurboJpegNativeApi.GetScalingFactors(out var count);
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
}
