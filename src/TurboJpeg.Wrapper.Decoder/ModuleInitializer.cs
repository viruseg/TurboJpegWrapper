using System.Runtime.CompilerServices;
using TurboJpegWrapper.Decoder;
using TurboJpegWrapper.Internal.Native;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize() =>
        TurboJpegNativeLoader.EnsureRegistered(
            static (message, inner) => new TurboJpegException(message, inner!));
}
