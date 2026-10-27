namespace TurboJpegWrapper.Internal.Encoder.Bmp;

internal readonly struct BmpInfo
{
    internal BmpInfo(
        int width,
        int height,
        bool topDown,
        int bitCount,
        uint compression,
        int pixelOffset,
        int stride,
        int paletteOffset,
        int paletteCount,
        uint redMask,
        uint greenMask,
        uint blueMask,
        int densityX,
        int densityY)
    {
        Width = width;
        Height = height;
        TopDown = topDown;
        BitCount = bitCount;
        Compression = compression;
        PixelOffset = pixelOffset;
        Stride = stride;
        PaletteOffset = paletteOffset;
        PaletteCount = paletteCount;
        RedMask = redMask;
        GreenMask = greenMask;
        BlueMask = blueMask;
        DensityX = densityX;
        DensityY = densityY;
    }

    internal int Width { get; }

    internal int Height { get; }

    internal bool TopDown { get; }

    internal int BitCount { get; }

    internal uint Compression { get; }

    internal int PixelOffset { get; }

    internal int Stride { get; }

    internal int PaletteOffset { get; }

    internal int PaletteCount { get; }

    internal uint RedMask { get; }

    internal uint GreenMask { get; }

    internal uint BlueMask { get; }

    internal int DensityX { get; }

    internal int DensityY { get; }
}
