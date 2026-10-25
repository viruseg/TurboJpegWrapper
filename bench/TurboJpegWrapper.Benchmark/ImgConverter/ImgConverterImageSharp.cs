using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Processing;

namespace TurboJpegWrapper.Benchmark;

public static class ImgConverterImageSharp
{
    public static bool ConvertToBmp(ReadOnlySpan<byte> sourceBytes, out byte[] convertedBytes)
    {
        try
        {
            using var image = Image.Load(sourceBytes);

            image.Mutate(x => x.AutoOrient().BackgroundColor(Color.White));

            var width = image.Width;
            var height = image.Height;

            var rowSize = (width * 3 + 3) & ~3;
            var estimatedSize = 54 + rowSize * height + 1024;

            using var outMs = new MemoryStream(estimatedSize);

            var encoder = new BmpEncoder
            {
                BitsPerPixel = BmpBitsPerPixel.Bit24,
                SupportTransparency = false
            };

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