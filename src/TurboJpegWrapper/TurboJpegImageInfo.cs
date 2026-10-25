namespace TurboJpegWrapper;

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
        Precision == 8 &&
        Colorspace is not TurboJpegColorspace.Cmyk and not TurboJpegColorspace.Ycck;
}
