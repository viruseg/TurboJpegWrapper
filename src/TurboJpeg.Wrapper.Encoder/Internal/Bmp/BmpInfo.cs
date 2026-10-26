namespace TurboJpegWrapper.Encoder.Internal.Bmp;

/// <summary>Describes the layout of a validated BMP file.</summary>
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

    /// <summary>Gets the horizontal density in pixels per metre, as stored in the file.</summary>
    internal int DensityX { get; }

    /// <summary>Gets the vertical density in pixels per metre, as stored in the file.</summary>
    internal int DensityY { get; }
}
