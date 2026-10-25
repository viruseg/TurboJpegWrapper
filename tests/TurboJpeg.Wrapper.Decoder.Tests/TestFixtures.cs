namespace TurboJpegWrapper.Decoder.Tests;

internal static class TestFixtures
{
    internal const int Width = 35;
    internal const int Height = 23;

    internal static byte[] Load(string name) =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
}
