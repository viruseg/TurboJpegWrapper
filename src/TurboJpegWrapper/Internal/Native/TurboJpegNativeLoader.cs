using System.Reflection;
using System.Runtime.InteropServices;

namespace TurboJpegWrapper.Internal.Native;

internal static class TurboJpegNativeLoader
{
    internal const string OverrideEnvironmentVariable = "TURBOJPEG_NATIVE_LIBRARY_PATH";

    private static int s_registered;

    internal static void EnsureRegistered()
    {
        if (Interlocked.Exchange(ref s_registered, 1) != 0)
        {
            return;
        }

        NativeLibrary.SetDllImportResolver(typeof(TurboJpegNative).Assembly, Resolve);
    }

    internal static nint Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, TurboJpegNative.LibraryName, StringComparison.Ordinal))
        {
            return nint.Zero;
        }

        var overridePath = Environment.GetEnvironmentVariable(OverrideEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            try
            {
                return NativeLibrary.Load(overridePath);
            }
            catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException)
            {
                throw new TurboJpegException(
                    $"Failed to load the TurboJPEG native library from '{overridePath}' " +
                    $"specified by {OverrideEnvironmentVariable}: {ex.Message}",
                    ex);
            }
        }

        var os = GetCurrentPlatform();
        if (GetRuntimeIdentifier(os, RuntimeInformation.ProcessArchitecture) is null)
        {
            throw new PlatformNotSupportedException(
                $"TurboJPEG native library loading is not supported on {os} with architecture " +
                $"{RuntimeInformation.ProcessArchitecture}. Set {OverrideEnvironmentVariable} to an explicit library path.");
        }

        foreach (var candidate in GetCandidatePaths())
        {
            if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out var candidateHandle))
            {
                return candidateHandle;
            }
        }

        if (NativeLibrary.TryLoad(TurboJpegNative.LibraryName, assembly, searchPath, out var systemHandle))
        {
            return systemHandle;
        }

        return nint.Zero;
    }

    internal static string? GetRuntimeIdentifier(OSPlatform os, Architecture architecture)
    {
        if (os == OSPlatform.Windows)
        {
            return architecture switch
            {
                Architecture.X64 => "win-x64",
                Architecture.X86 => "win-x86",
                Architecture.Arm64 => "win-arm64",
                _ => null
            };
        }

        if (os == OSPlatform.Linux)
        {
            return architecture switch
            {
                Architecture.X64 => "linux-x64",
                Architecture.Arm64 => "linux-arm64",
                _ => null
            };
        }

        if (os == OSPlatform.OSX)
        {
            return architecture switch
            {
                Architecture.X64 => "osx-x64",
                Architecture.Arm64 => "osx-arm64",
                _ => null
            };
        }

        return null;
    }

    internal static string GetLibraryFileName(OSPlatform os)
    {
        if (os == OSPlatform.Windows)
        {
            return "turbojpeg.dll";
        }

        if (os == OSPlatform.Linux)
        {
            return "libturbojpeg.so";
        }

        if (os == OSPlatform.OSX)
        {
            return "libturbojpeg.dylib";
        }

        throw new PlatformNotSupportedException($"The TurboJPEG native library file name is not known for OS '{os}'.");
    }

    internal static IReadOnlyList<string> GetCandidatePaths()
    {
        var os = GetCurrentPlatform();
        var rid = GetRuntimeIdentifier(os, RuntimeInformation.ProcessArchitecture);
        var fileName = GetLibraryFileName(os);
        var paths = new List<string>(2);

        if (rid is not null)
        {
            paths.Add(Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native", fileName));
        }

        paths.Add(Path.Combine(AppContext.BaseDirectory, fileName));

        return paths;
    }

    private static OSPlatform GetCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return OSPlatform.Windows;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return OSPlatform.Linux;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return OSPlatform.OSX;
        }

        throw new PlatformNotSupportedException(
            "TurboJPEG native library loading is supported only on Windows, Linux, and macOS.");
    }
}
