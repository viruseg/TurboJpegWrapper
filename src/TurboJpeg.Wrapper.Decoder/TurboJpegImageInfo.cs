namespace TurboJpegWrapper.Decoder;

/// <summary>Describes the dimensions and encoding format of a JPEG image.</summary>
/// <param name="Width">Image width in pixels.</param>
/// <param name="Height">Image height in pixels.</param>
/// <param name="Precision">Sample precision in bits.</param>
/// <param name="Colorspace">Color space of the image.</param>
/// <param name="Subsampling">Chroma subsampling scheme of the image.</param>
/// <param name="IsLossless">Whether the image was encoded with lossless JPEG.</param>
public readonly record struct TurboJpegImageInfo(
    int Width,
    int Height,
    int Precision,
    TurboJpegColorspace Colorspace,
    TurboJpegSubsampling Subsampling,
    bool IsLossless)
{
    /// <summary>
    /// Gets a value indicating whether the image can be decoded to a BMP by this library.
    /// </summary>
    public bool CanDecodeToBmp =>
        Precision is >= 2 and <= 8 &&
        Colorspace is not TurboJpegColorspace.Cmyk and not TurboJpegColorspace.Ycck;

    /// <summary>Gets the horizontal pixel density from the JPEG header.</summary>
    public int DensityX { get; init; }

    /// <summary>Gets the vertical pixel density from the JPEG header.</summary>
    public int DensityY { get; init; }

    /// <summary>Gets the units of <see cref="DensityX"/> and <see cref="DensityY"/>.</summary>
    public TurboJpegDensityUnits DensityUnits { get; init; }

    /// <summary>Gets a value indicating whether the image was encoded with progressive JPEG.</summary>
    public bool IsProgressive { get; init; }

    /// <summary>Gets a value indicating whether the image uses arithmetic entropy coding.</summary>
    public bool UsesArithmeticCoding { get; init; }
}
