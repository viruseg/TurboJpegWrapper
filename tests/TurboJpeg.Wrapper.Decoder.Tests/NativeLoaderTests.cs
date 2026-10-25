using System.Runtime.InteropServices;
using TurboJpeg.Wrapper.Decoder;
using TurboJpeg.Wrapper.Decoder.Internal.Native;

namespace TurboJpeg.Wrapper.Decoder.Tests;

public sealed class NativeLoaderTests
{
    [Theory]
    [InlineData("WINDOWS", Architecture.X64, "win-x64")]
    [InlineData("WINDOWS", Architecture.Arm64, "win-arm64")]
    [InlineData("LINUX", Architecture.X64, "linux-x64")]
    [InlineData("LINUX", Architecture.Arm64, "linux-arm64")]
    [InlineData("OSX", Architecture.X64, "osx-x64")]
    [InlineData("OSX", Architecture.Arm64, "osx-arm64")]
    public void GetRuntimeIdentifier_KnownPlatforms(string os, Architecture arch, string expected)
    {
        Assert.Equal(expected, TurboJpegNativeLoader.GetRuntimeIdentifier(OSPlatform.Create(os), arch));
    }

    [Theory]
    [InlineData("FREEBSD", Architecture.X64)]
    [InlineData("WINDOWS", Architecture.X86)]
    [InlineData("WINDOWS", Architecture.Arm)]
    [InlineData("LINUX", Architecture.X86)]
    [InlineData("OSX", Architecture.X86)]
    public void GetRuntimeIdentifier_Unsupported_ReturnsNull(string os, Architecture arch)
    {
        Assert.Null(TurboJpegNativeLoader.GetRuntimeIdentifier(OSPlatform.Create(os), arch));
    }

    [Theory]
    [InlineData("WINDOWS", "turbojpeg.dll")]
    [InlineData("LINUX", "libturbojpeg.so")]
    [InlineData("OSX", "libturbojpeg.dylib")]
    public void GetLibraryFileName_KnownPlatforms(string os, string expected)
    {
        Assert.Equal(expected, TurboJpegNativeLoader.GetLibraryFileName(OSPlatform.Create(os)));
    }

    [Fact]
    public void GetLibraryFileName_Unsupported_Throws()
    {
        Assert.Throws<PlatformNotSupportedException>(
            () => TurboJpegNativeLoader.GetLibraryFileName(OSPlatform.Create("FREEBSD")));
    }

    [Fact]
    public void GetCandidatePaths_ContainsCurrentRuntimeAsset()
    {
        var rid = TurboJpegNativeLoader.GetRuntimeIdentifier(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? OSPlatform.Windows :
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? OSPlatform.Linux : OSPlatform.OSX,
            RuntimeInformation.ProcessArchitecture)!;
        var currentOs = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? OSPlatform.Windows :
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? OSPlatform.Linux : OSPlatform.OSX;
        var file = TurboJpegNativeLoader.GetLibraryFileName(currentOs);
        var expected = Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native", file);

        Assert.Contains(expected, TurboJpegNativeLoader.GetCandidatePaths());
    }

    [Fact]
    public void Resolve_OverridePath_LoadsLibrary()
    {
        var libraryPath = TurboJpegNativeLoader.GetCandidatePaths().First(File.Exists);
        var handle = TurboJpegNativeLoader.Resolve(
            TurboJpegNative.LibraryName,
            typeof(TurboJpegNative).Assembly,
            null,
            libraryPath);

        try
        {
            Assert.NotEqual(nint.Zero, handle);
        }
        finally
        {
            if (handle != nint.Zero)
            {
                NativeLibrary.Free(handle);
            }
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Resolve_EmptyOrWhitespaceOverride_UsesFallback(string overridePath)
    {
        var handle = TurboJpegNativeLoader.Resolve(
            TurboJpegNative.LibraryName,
            typeof(TurboJpegNative).Assembly,
            null,
            overridePath);

        try
        {
            Assert.NotEqual(nint.Zero, handle);
        }
        finally
        {
            if (handle != nint.Zero)
            {
                NativeLibrary.Free(handle);
            }
        }
    }

    [Fact]
    public void Resolve_MissingOverride_ThrowsWithPathAndReason()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), "turbojpeg-missing", "turbojpeg.dll");

        var exception = Assert.Throws<TurboJpegException>(() =>
            TurboJpegNativeLoader.Resolve(
                TurboJpegNative.LibraryName,
                typeof(TurboJpegNative).Assembly,
                null,
                missingPath));

        Assert.Contains(missingPath, exception.Message);
        Assert.Contains(TurboJpegNativeLoader.OverrideEnvironmentVariable, exception.Message);
        Assert.Contains("not used", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_UnknownLibraryName_ReturnsZero()
    {
        var handle = TurboJpegNativeLoader.Resolve(
            "other-library",
            typeof(TurboJpegNative).Assembly,
            null,
            "ignored");

        Assert.Equal(nint.Zero, handle);
    }

    [Fact]
    public void CreateLoadException_NoOverride_MentionsOsSearchAndReason()
    {
        var inner = new DllNotFoundException("probe failure");

        var exception = TurboJpegNative.CreateLoadException(inner, overridePath: null);

        Assert.Contains("operating system search path", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("probe failure", exception.Message);
        Assert.Contains("3.2.0", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void CreateLoadException_WithOverride_MentionsOverrideOnly()
    {
        var path = Path.Combine(Path.GetTempPath(), "turbojpeg.dll");
        var inner = new BadImageFormatException("wrong architecture");

        var exception = TurboJpegNative.CreateLoadException(inner, path);

        Assert.Contains(path, exception.Message);
        Assert.Contains(TurboJpegNativeLoader.OverrideEnvironmentVariable, exception.Message);
        Assert.Contains("not used", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("wrong architecture", exception.Message);
    }

    [Fact]
    public void CreateLoadException_EntryPoint_MentionsRequiredVersion()
    {
        var inner = new EntryPointNotFoundException("tj3InitVersion");

        var exception = TurboJpegNative.CreateLoadException(inner, overridePath: null);

        Assert.Contains("entry point", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("3.2.0", exception.Message);
        Assert.Contains("tj3InitVersion", exception.Message);
    }
}
