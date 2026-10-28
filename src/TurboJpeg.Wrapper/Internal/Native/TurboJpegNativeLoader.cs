using System.Reflection;
using System.Runtime.InteropServices;

namespace TurboJpegWrapper.Internal.Native;

internal static class TurboJpegNativeLoader
{
    internal const string OverrideEnvironmentVariable = "TURBOJPEG_NATIVE_LIBRARY_PATH";

    private static readonly object s_registrationLock = new();
    private static Func<string, Exception?, Exception>? s_exceptionFactory;
    private static int s_registered;

    internal static void EnsureRegistered(Func<string, Exception?, Exception> exceptionFactory)
    {
        ArgumentNullException.ThrowIfNull(exceptionFactory);

        if (Volatile.Read(ref s_registered) != 0)
        {
            return;
        }

        lock (s_registrationLock)
        {
            s_exceptionFactory ??= exceptionFactory;
            if (s_registered != 0)
            {
                return;
            }

            NativeLibrary.SetDllImportResolver(typeof(TurboJpegNativeApi).Assembly, Resolve);
            Volatile.Write(ref s_registered, 1);
        }
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
        if (!string.Equals(libraryName, TurboJpegNativeApi.LibraryName, StringComparison.Ordinal))
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
                throw CreateLoadException(ex, overridePath);
            }
        }

        var os = GetCurrentPlatform();
        if (GetRuntimeIdentifier(os, RuntimeInformation.ProcessArchitecture) is null)
        {
            throw new PlatformNotSupportedException(
                $"TurboJPEG native library loading is not supported on {os} with architecture " +
                $"{RuntimeInformation.ProcessArchitecture}. Set {OverrideEnvironmentVariable} to an explicit library path.");
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
            return NativeLibrary.Load(TurboJpegNativeApi.LibraryName, assembly, searchPath);
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException)
        {
            attempts.Add($"operating system search path for '{TurboJpegNativeApi.LibraryName}' ({ex.Message})");
            lastFailure = ex;
        }

        throw GetExceptionFactory()(
            $"Failed to load the TurboJPEG native library (libjpeg-turbo 3.2.1, " +
            $"TurboJPEG API version {TurboJpegNativeApi.ApiVersion}). " +
            $"Attempts: {string.Join("; ", attempts)}. " +
            $"Set {OverrideEnvironmentVariable} to an explicit library path.",
            lastFailure!);
    }

    internal static Exception CreateLoadException(Exception innerException, string? overridePath)
    {
        var version = $"libjpeg-turbo 3.2.1, TurboJPEG API version {TurboJpegNativeApi.ApiVersion}";
        var exceptionFactory = GetExceptionFactory();

        if (innerException is EntryPointNotFoundException)
        {
            var foundAt = string.IsNullOrWhiteSpace(overridePath)
                ? "in one of the candidate locations or on the operating system search path"
                : $"at the configured override path '{overridePath}'";

            return exceptionFactory(
                $"The TurboJPEG native library was found {foundAt}, but a required entry point is missing. " +
                $"This library requires {version}; install TurboJPEG 3.2.1 or newer. " +
                $"Reason: {innerException.Message}",
                innerException);
        }

        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return exceptionFactory(
                $"Failed to load the TurboJPEG native library from '{overridePath}' specified by " +
                $"{OverrideEnvironmentVariable} ({version}). " +
                $"The operating system search path was not used. Reason: {innerException.Message}",
                innerException);
        }

        return exceptionFactory(
            $"Failed to load the TurboJPEG native library ({version}). " +
            $"Expected the library at one of: {string.Join(", ", GetCandidatePaths())}, " +
            $"or on the operating system search path. Reason: {innerException.Message}",
            innerException);
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

        throw new PlatformNotSupportedException(
            $"The TurboJPEG native library file name is not known for OS '{os}'.");
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

    private static Func<string, Exception?, Exception> GetExceptionFactory() =>
        Volatile.Read(ref s_exceptionFactory)
        ?? throw new InvalidOperationException(
            "The TurboJPEG exception factory has not been registered. The managed package must call " +
            $"{nameof(TurboJpegNativeLoader)}.{nameof(EnsureRegistered)} from a module initializer.");

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
