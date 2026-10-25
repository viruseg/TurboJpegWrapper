namespace TurboJpeg.Wrapper.Decoder.Tests;

public sealed class NativeAssetsTests
{
    [Theory]
    [InlineData("win-x64", "turbojpeg.dll")]
    [InlineData("win-arm64", "turbojpeg.dll")]
    [InlineData("linux-x64", "libturbojpeg.so")]
    [InlineData("linux-arm64", "libturbojpeg.so")]
    [InlineData("osx", "libturbojpeg.dylib")]
    public void BundledAsset_ExistsInOutput(string rid, string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native", fileName);
        Assert.True(File.Exists(path), $"Missing bundled native asset: {path}");
    }
}
