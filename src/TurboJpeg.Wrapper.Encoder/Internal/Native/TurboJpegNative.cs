using System.Runtime.InteropServices;
using TurboJpegWrapper.Encoder.Internal;
using TurboJpegWrapper.Internal.Native;

namespace TurboJpegWrapper.Encoder.Internal.Native;

/// <summary>Wraps the TurboJPEG compression calls used by the encoder.</summary>
internal static unsafe class TurboJpegNative
{
    internal static SafeTurboJpegHandle CreateEncoderHandle()
    {
        nint handle = nint.Zero;
        try
        {
            handle = TurboJpegNativeApi.InitVersion(
                TurboJpegNativeApi.InitCompress, TurboJpegNativeApi.ApiVersion);
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
        if (TurboJpegNativeApi.Set(handle, TurboJpegNativeApi.ParamNoRealloc, 1) != 0)
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
                    ? "TurboJPEG failed to initialize a compression handle."
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

    /// <summary>Compresses the source pixels and returns the number of JPEG bytes written.</summary>
    /// <exception cref="TurboJpegException">TurboJPEG reported an error.</exception>
    internal static int Compress(
        nint handle,
        ReadOnlySpan<byte> source,
        int width,
        int pitch,
        int height,
        int pixelFormat,
        Span<byte> destination)
    {
        fixed (byte* sourceBuffer = source)
        fixed (byte* destinationBuffer = destination)
        {
            var jpegBuffer = destinationBuffer;
            var jpegSize = (nuint)destination.Length;
            if (TurboJpegNativeApi.Compress8(
                    handle,
                    sourceBuffer,
                    width,
                    pitch,
                    height,
                    pixelFormat,
                    &jpegBuffer,
                    &jpegSize) != 0)
            {
                ThrowHelper.Throw(CreateException(handle));
            }

            return (int)jpegSize;
        }
    }
}
