using TurboJpegWrapper.Internal.Native;

namespace TurboJpegWrapper.Internal.Encoder;

internal static class SizeCalculator
{
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
