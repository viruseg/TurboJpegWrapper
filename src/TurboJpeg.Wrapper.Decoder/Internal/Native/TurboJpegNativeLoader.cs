using System.Reflection;
using System.Runtime.InteropServices;
using TurboJpeg.Wrapper.Decoder.Internal;

namespace TurboJpeg.Wrapper.Decoder.Internal.Native;

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
        return Resolve(
            libraryName,
            assembly,
            searchPath,
            Environment.GetEnvironmentVariable(OverrideEnvironmentVariable));
    }

    internal static nint Resolve(
        string libraryName,
        Assembly assembly,
        DllImportSearchPath? searchPath,
        string? overridePath)
    {
        if (!string.Equals(libraryName, TurboJpegNative.LibraryName, StringComparison.Ordinal))
        {
            return nint.Zero;
        }

        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            try
            {
                return NativeLibrary.Load(overridePath);
            }
            catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException)
            {
                ThrowHelper.Throw(TurboJpegNative.CreateLoadException(ex, overridePath));
            }
        }

        var os = GetCurrentPlatform();
        if (GetRuntimeIdentifier(os, RuntimeInformation.ProcessArchitecture) is null)
        {
            ThrowHelper.ThrowNativeLoadingUnsupported(os, RuntimeInformation.ProcessArchitecture);
        }

        var attempts = new List<string>();
        Exception? lastFailure = null;

        foreach (var candidate in GetCandidatePaths())
        {
            if (!File.Exists(candidate))
            {
                attempts.Add($"{candidate} (not found)");
                continue;
            }

            try
            {
                return NativeLibrary.Load(candidate);
            }
            catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException)
            {
                attempts.Add($"{candidate} ({ex.Message})");
                lastFailure = ex;
            }
        }

        try
        {
            return NativeLibrary.Load(TurboJpegNative.LibraryName, assembly, searchPath);
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException)
        {
            attempts.Add($"operating system search path for '{TurboJpegNative.LibraryName}' ({ex.Message})");
            lastFailure = ex;
        }

        ThrowHelper.ThrowNativeLoadFailed(attempts, lastFailure!);
        return nint.Zero;
    }

    internal static string? GetRuntimeIdentifier(OSPlatform os, Architecture architecture)
    {
        if (os == OSPlatform.Windows)
        {
            return architecture switch
            {
                Architecture.X64 => "win-x64",
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

        ThrowHelper.ThrowLibraryFileNameUnknown(os);
        return null!;
    }

    internal static IReadOnlyList<string> GetCandidatePaths() =>
        GetCandidatePaths(
            GetCurrentPlatform(),
            RuntimeInformation.ProcessArchitecture,
            AppContext.BaseDirectory);

    internal static IReadOnlyList<string> GetCandidatePaths(
        OSPlatform os,
        Architecture architecture,
        string baseDirectory)
    {
        var rid = GetRuntimeIdentifier(os, architecture);
        var fileName = GetLibraryFileName(os);
        var paths = new List<string>(3);

        if (rid is not null)
        {
            paths.Add(Path.Combine(baseDirectory, "runtimes", rid, "native", fileName));
        }

        if (os == OSPlatform.OSX)
        {
            paths.Add(Path.Combine(baseDirectory, "runtimes", "osx", "native", fileName));
        }

        paths.Add(Path.Combine(baseDirectory, fileName));

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

        ThrowHelper.ThrowPlatformUnsupported();
        return default;
    }
}
