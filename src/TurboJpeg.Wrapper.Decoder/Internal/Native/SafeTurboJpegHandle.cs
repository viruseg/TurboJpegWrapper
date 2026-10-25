using Microsoft.Win32.SafeHandles;

namespace TurboJpeg.Wrapper.Decoder.Internal.Native;

internal sealed class SafeTurboJpegHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal SafeTurboJpegHandle(nint existingHandle)
        : base(ownsHandle: true)
    {
        SetHandle(existingHandle);
    }

    protected override bool ReleaseHandle()
    {
        TurboJpegNative.Destroy(handle);
        return true;
    }
}
