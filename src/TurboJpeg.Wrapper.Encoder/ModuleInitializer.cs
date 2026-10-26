using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TurboJpegWrapper.Encoder;
using TurboJpegWrapper.Internal.Native;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    [SuppressMessage("Usage", "CA2255", Justification = "The DllImport resolver must be registered before any P/Invoke; a module initializer is the load-time hook.")]
    internal static void Initialize() =>
        TurboJpegNativeLoader.EnsureRegistered(
            static (message, inner) => new TurboJpegException(message, inner!));
}
