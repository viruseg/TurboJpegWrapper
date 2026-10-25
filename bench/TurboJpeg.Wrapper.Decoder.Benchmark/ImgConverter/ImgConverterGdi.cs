using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace TurboJpegWrapper.Decoder.Benchmark;

public static class ImgConverterGdi
{
    public static bool ConvertToBmp(byte[] sourceBytes, out byte[] convertedBytes)
    {
        try
        {
            using var inMs = new MemoryStream(sourceBytes);
            using var originalImage = Image.FromStream(inMs);

            RotateImageIfRequired(originalImage);

            var width = originalImage.Width;
            var height = originalImage.Height;

            using var bmp24 = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            bmp24.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);

            using (var g = Graphics.FromImage(bmp24))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.Clear(Color.White);
                g.DrawImage(originalImage, 0, 0, width, height);
            }

            var rowSize = (width * 3 + 3) & ~3;
            var estimatedSize = 54 + rowSize * height + 1024;

            using var outMs = new MemoryStream(estimatedSize);
            bmp24.Save(outMs, ImageFormat.Bmp);

            convertedBytes = outMs.ToArray();
            return true;
        }
        catch
        {
            convertedBytes = [];
            return false;
        }
    }

    private static void RotateImageIfRequired(Image img)
    {
        const int ExifOrientationTagId = 0x0112;

        if (Array.IndexOf(img.PropertyIdList, ExifOrientationTagId) < 0)
        {
            return;
        }

        var prop = img.GetPropertyItem(ExifOrientationTagId);
        if (prop?.Value == null || prop.Value.Length == 0)
        {
            return;
        }

        int orientation = prop.Value.Length >= 2
            ? BitConverter.ToUInt16(prop.Value, 0)
            : prop.Value[0];

        var rotateFlipType = orientation switch
        {
            2 => RotateFlipType.RotateNoneFlipX,
            3 => RotateFlipType.Rotate180FlipNone,
            4 => RotateFlipType.Rotate180FlipX,
            5 => RotateFlipType.Rotate90FlipX,
            6 => RotateFlipType.Rotate90FlipNone,
            7 => RotateFlipType.Rotate270FlipX,
            8 => RotateFlipType.Rotate270FlipNone,
            _ => RotateFlipType.RotateNoneFlipNone
        };

        if (rotateFlipType != RotateFlipType.RotateNoneFlipNone)
        {
            img.RotateFlip(rotateFlipType);
            try
            {
                img.RemovePropertyItem(ExifOrientationTagId);
            }
            catch
            {
                // ignored
            }
        }
    }
}