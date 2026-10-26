namespace TurboJpegWrapper.Encoder;

/// <summary>Chroma subsampling schemes supported by the TurboJPEG encoder.</summary>
public enum TurboJpegSubsampling
{
    /// <summary>No subsampling scheme is selected; this value is not valid for encoding.</summary>
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

/// <summary>Color spaces supported by the TurboJPEG encoder.</summary>
public enum TurboJpegColorspace
{
    /// <summary>Use the color space implied by the input pixel format.</summary>
    Default = -1,

    /// <summary>RGB color space.</summary>
    Rgb = 0,

    /// <summary>YCbCr color space.</summary>
    YCbCr = 1,

    /// <summary>Grayscale color space.</summary>
    Grayscale = 2
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
