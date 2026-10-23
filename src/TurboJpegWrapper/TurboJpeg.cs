using TurboJpegWrapper.Internal.Native;

namespace TurboJpegWrapper;

/// <summary>Provides static access to TurboJPEG operations.</summary>
public static class TurboJpeg
{
    /// <summary>Gets the scaling factors supported by TurboJPEG, including 1:1 and downscaling factors only.</summary>
    /// <returns>A read-only span over the supported scaling factors in native order.</returns>
    /// <exception cref="TurboJpegException">The TurboJPEG native library could not be loaded.</exception>
    public static ReadOnlySpan<TurboJpegScalingFactor> GetSupportedScalingFactors()
    {
        try
        {
            return TurboJpegNative.GetFilteredScalingFactors();
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            throw TurboJpegNative.CreateLoadException(ex);
        }
    }
}
