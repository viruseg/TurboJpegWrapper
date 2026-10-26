using TurboJpegWrapper.Internal.Native;

namespace TurboJpegWrapper.Encoder.Internal;

/// <summary>Computes the worst-case JPEG buffer size reported by TurboJPEG.</summary>
internal static class SizeCalculator
{
    /// <summary>Returns the worst-case JPEG size in bytes for an image and subsampling scheme.</summary>
    /// <exception cref="TurboJpegImageTooLargeException">
    /// The reported size is zero or exceeds the maximum supported array length.
    /// </exception>
    internal static int GetMaxJpegSize(int width, int height, int subsampling)
    {
        var size = TurboJpegNativeApi.JPEGBufSize(width, height, subsampling);
        if (size == 0)
        {
            ThrowHelper.ThrowJpegSizeZero(width, height);
        }

        if (size > int.MaxValue || size > (nuint)Array.MaxLength)
        {
            ThrowHelper.ThrowJpegTooLarge(width, height, size);
        }

        return (int)size;
    }
}
