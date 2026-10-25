namespace TurboJpegWrapper;

/// <summary>Immutable options that control JPEG decoding.</summary>
public sealed class TurboJpegDecodeOptions
{
    /// <summary>Gets the default decoding options.</summary>
    public static TurboJpegDecodeOptions Default { get; } = new();

    /// <summary>Initializes a new instance of the <see cref="TurboJpegDecodeOptions"/> class.</summary>
    /// <param name="scalingFactor">The output scaling factor; <see langword="null"/> means 1:1.</param>
    /// <param name="fastDct">Whether to use the fastest DCT/IDCT algorithm.</param>
    /// <param name="fastUpsampling">Whether to use the fastest chroma upsampling algorithm.</param>
    /// <param name="maxPixels">The maximum number of source pixels; 0 means no limit.</param>
    /// <param name="maxMemoryMegabytes">The maximum native intermediate memory in megabytes; 0 means no limit.</param>
    /// <param name="scanLimit">The maximum number of progressive scans; 0 means no limit.</param>
    /// <param name="bmpBitDepth">The BMP output bit depth.</param>
    /// <param name="autoOrient">
    /// Whether to apply the orientation stored in the JPEG EXIF metadata to the BMP; the default is
    /// <see langword="false"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A limit is negative, the scaling factor numerator or denominator is not positive,
    /// or the BMP bit depth is not 8, 24, or 32.
    /// </exception>
    public TurboJpegDecodeOptions(
        TurboJpegScalingFactor? scalingFactor = null,
        bool fastDct = false,
        bool fastUpsampling = false,
        int maxPixels = 100_000_000,
        int maxMemoryMegabytes = 0,
        int scanLimit = 500,
        TurboJpegBmpBitDepth bmpBitDepth = TurboJpegBmpBitDepth.Bpp32,
        bool autoOrient = false)
    {
        var scaling = scalingFactor ?? TurboJpegScalingFactor.One;

        if (scaling.Numerator <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scalingFactor), scaling, "The scaling factor numerator must be greater than zero.");
        }

        if (scaling.Denominator <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scalingFactor), scaling, "The scaling factor denominator must be greater than zero.");
        }

        if (maxPixels < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxPixels), maxPixels, "The maximum pixel count must not be negative.");
        }

        if (maxMemoryMegabytes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxMemoryMegabytes), maxMemoryMegabytes, "The maximum memory must not be negative.");
        }

        if (scanLimit < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scanLimit), scanLimit, "The scan limit must not be negative.");
        }

        if (bmpBitDepth is not TurboJpegBmpBitDepth.Bpp8
            and not TurboJpegBmpBitDepth.Bpp24
            and not TurboJpegBmpBitDepth.Bpp32)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bmpBitDepth), bmpBitDepth, "The BMP bit depth must be 8, 24, or 32.");
        }

        ScalingFactor = scaling;
        FastDct = fastDct;
        FastUpsampling = fastUpsampling;
        MaxPixels = maxPixels;
        MaxMemoryMegabytes = maxMemoryMegabytes;
        ScanLimit = scanLimit;
        BmpBitDepth = bmpBitDepth;
        AutoOrient = autoOrient;
    }

    /// <summary>Gets the output scaling factor.</summary>
    public TurboJpegScalingFactor ScalingFactor { get; }

    /// <summary>Gets a value indicating whether the fastest DCT/IDCT algorithm is used.</summary>
    public bool FastDct { get; }

    /// <summary>Gets a value indicating whether the fastest chroma upsampling algorithm is used.</summary>
    public bool FastUpsampling { get; }

    /// <summary>Gets the maximum number of source pixels; 0 means no limit.</summary>
    public int MaxPixels { get; }

    /// <summary>Gets the maximum native intermediate memory in megabytes; 0 means no limit.</summary>
    /// <remarks>
    /// The limit covers native TurboJPEG allocations. The returned BMP array and the auto-orient
    /// scratch buffer are managed and are not counted against it.
    /// </remarks>
    public int MaxMemoryMegabytes { get; }

    /// <summary>Gets the maximum number of progressive scans; 0 means no limit.</summary>
    public int ScanLimit { get; }

    /// <summary>Gets the BMP output bit depth.</summary>
    public TurboJpegBmpBitDepth BmpBitDepth { get; }

    /// <summary>Gets a value indicating whether the EXIF orientation is applied to the BMP.</summary>
    /// <remarks>
    /// The default is <see langword="false"/>. Missing, malformed, or out-of-range EXIF orientation
    /// leaves the image unchanged.
    /// </remarks>
    public bool AutoOrient { get; }
}
