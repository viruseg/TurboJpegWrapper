using System.Drawing;
using System.Drawing.Imaging;
using GdiEncoder = System.Drawing.Imaging.Encoder;

namespace TurboJpegWrapper.Encoder.Benchmark;

public static class ImgConverterGdi
{
    private const long JpegQuality = 90L;

    public static bool ConvertToJpeg(byte[] sourceBytes, out byte[] convertedBytes)
    {
        try
        {
            using var inMs = new MemoryStream(sourceBytes);
            using var image = Image.FromStream(inMs);

            using var encoderParameters = new EncoderParameters(1);
            using var qualityParameter = new EncoderParameter(GdiEncoder.Quality, JpegQuality);
            encoderParameters.Param[0] = qualityParameter;

            using var outMs = new MemoryStream();
            image.Save(outMs, GetJpegEncoder(), encoderParameters);

            convertedBytes = outMs.ToArray();
            return true;
        }
        catch
        {
            convertedBytes = [];
            return false;
        }
    }

    private static ImageCodecInfo GetJpegEncoder()
    {
        return ImageCodecInfo.GetImageEncoders()
            .First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
    }
}
