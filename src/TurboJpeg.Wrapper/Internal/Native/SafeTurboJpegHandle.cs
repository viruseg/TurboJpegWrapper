using Microsoft.Win32.SafeHandles;

namespace TurboJpegWrapper.Internal.Native;

internal sealed class SafeTurboJpegHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal SafeTurboJpegHandle(nint existingHandle)
        : base(ownsHandle: true)
    {
        SetHandle(existingHandle);
    }

    protected override bool ReleaseHandle()
    {
        TurboJpegNativeApi.Destroy(handle);
        return true;
    }
}
