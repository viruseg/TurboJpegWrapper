using System.Runtime.InteropServices;
using TurboJpegWrapper.Internal.Native;

namespace TurboJpegWrapper.Tests;

public sealed class NativeLoaderTests
{
    [Theory]
    [InlineData("WINDOWS", Architecture.X64, "win-x64")]
    [InlineData("WINDOWS", Architecture.X86, "win-x86")]
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
    [InlineData("WINDOWS", Architecture.Arm)]
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
}
