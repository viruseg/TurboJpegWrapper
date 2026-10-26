using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace TurboJpegWrapper.Encoder.Benchmark;

public static class ImgConverterImageSharp
{
    public static bool ConvertToJpeg(ReadOnlySpan<byte> sourceBytes, out byte[] convertedBytes)
    {
        try
        {
            using var image = Image.Load(sourceBytes);
            using var outMs = new MemoryStream();

            var encoder = new JpegEncoder { Quality = 90 };

            image.Save(outMs, encoder);

            convertedBytes = outMs.ToArray();
            return true;
        }
        catch
        {
            convertedBytes = [];
            return false;
        }
    }
}
