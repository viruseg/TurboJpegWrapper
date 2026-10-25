namespace TurboJpeg.Wrapper.Decoder;

/// <summary>Color spaces reported by TurboJPEG for a JPEG image.</summary>
public enum TurboJpegColorspace
{
    /// <summary>RGB color space.</summary>
    Rgb = 0,

    /// <summary>YCbCr color space.</summary>
    YCbCr = 1,

    /// <summary>Grayscale color space.</summary>
    Grayscale = 2,

    /// <summary>CMYK color space.</summary>
    Cmyk = 3,

    /// <summary>YCCK color space.</summary>
    Ycck = 4
}

/// <summary>Chroma subsampling schemes reported by TurboJPEG for a JPEG image.</summary>
public enum TurboJpegSubsampling
{
    /// <summary>The subsampling scheme is unknown.</summary>
    Unknown = -1,

    /// <summary>4:4:4 chroma subsampling.</summary>
    Yuv444 = 0,

    /// <summary>4:2:2 chroma subsampling.</summary>
    Yuv422 = 1,

    /// <summary>4:2:0 chroma subsampling.</summary>
    Yuv420 = 2,

    /// <summary>Grayscale image without chroma components.</summary>
    Grayscale = 3,

    /// <summary>4:4:0 chroma subsampling.</summary>
    Yuv440 = 4,

    /// <summary>4:1:1 chroma subsampling.</summary>
    Yuv411 = 5,

    /// <summary>4:4:1 chroma subsampling.</summary>
    Yuv441 = 6,

    /// <summary>4:1:0 chroma subsampling.</summary>
    Yuv410 = 7,

    /// <summary>4:2:4 chroma subsampling.</summary>
    Yuv24 = 8
}

/// <summary>Units of the pixel density stored in the JPEG header.</summary>
public enum TurboJpegDensityUnits
{
    /// <summary>The pixel density units are unknown.</summary>
    Unknown = 0,

    /// <summary>The pixel density is expressed in pixels per inch.</summary>
    PixelsPerInch = 1,

    /// <summary>The pixel density is expressed in pixels per centimeter.</summary>
    PixelsPerCentimeter = 2
}

/// <summary>Bits per pixel of the BMP produced by decoding.</summary>
public enum TurboJpegBmpBitDepth
{
    /// <summary>8-bit grayscale with a 256-entry grayscale palette.</summary>
    Bpp8 = 8,

    /// <summary>24-bit BGR.</summary>
    Bpp24 = 24,

    /// <summary>32-bit BGRA with opaque alpha.</summary>
    Bpp32 = 32
}
