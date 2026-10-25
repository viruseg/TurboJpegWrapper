namespace TurboJpegWrapper;

/// <summary>Immutable options that control JPEG decoding.</summary>
public sealed record TurboJpegDecodeOptions
{
    /// <summary>Gets the default decoding options.</summary>
    public static TurboJpegDecodeOptions Default { get; } = new();

    private readonly TurboJpegScalingFactor _scalingFactor = TurboJpegScalingFactor.One;
    private readonly int _maxPixels = 100_000_000;
    private readonly int _maxMemoryMegabytes = 0;
    private readonly int _scanLimit = 500;
    private readonly TurboJpegBmpBitDepth _bmpBitDepth = TurboJpegBmpBitDepth.Bpp32;

    /// <summary>Gets the output scaling factor.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The numerator or denominator is zero or negative.
    /// </exception>
    public TurboJpegScalingFactor ScalingFactor
    {
        get => _scalingFactor;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value.Numerator, nameof(ScalingFactor));
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value.Denominator, nameof(ScalingFactor));
            _scalingFactor = value;
        }
    }

    /// <summary>Gets a value indicating whether the fastest DCT/IDCT algorithm is used.</summary>
    public bool FastDct { get; init; } = false;

    /// <summary>Gets a value indicating whether the fastest chroma upsampling algorithm is used.</summary>
    public bool FastUpsampling { get; init; } = false;

    /// <summary>Gets the maximum number of source pixels; 0 means no limit.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int MaxPixels
    {
        get => _maxPixels;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(MaxPixels));
            _maxPixels = value;
        }
    }

    /// <summary>Gets the maximum native intermediate memory in megabytes; 0 means no limit.</summary>
    /// <remarks>
    /// The limit covers native TurboJPEG allocations. The returned BMP array and the auto-orient
    /// scratch buffer are managed and are not counted against it.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int MaxMemoryMegabytes
    {
        get => _maxMemoryMegabytes;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(MaxMemoryMegabytes));
            _maxMemoryMegabytes = value;
        }
    }

    /// <summary>Gets the maximum number of progressive scans; 0 means no limit.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int ScanLimit
    {
        get => _scanLimit;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(ScanLimit));
            _scanLimit = value;
        }
    }

    /// <summary>Gets the BMP output bit depth.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not 8, 24, or 32 bits per pixel.</exception>
    public TurboJpegBmpBitDepth BmpBitDepth
    {
        get => _bmpBitDepth;
        init
        {
            if (value is not TurboJpegBmpBitDepth.Bpp8
                and not TurboJpegBmpBitDepth.Bpp24
                and not TurboJpegBmpBitDepth.Bpp32)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(BmpBitDepth), value, "The BMP bit depth must be 8, 24, or 32.");
            }

            _bmpBitDepth = value;
        }
    }

    /// <summary>Gets a value indicating whether the EXIF orientation is applied to the BMP.</summary>
    /// <remarks>
    /// The default is <see langword="false"/>. Missing, malformed, or out-of-range EXIF orientation
    /// leaves the image unchanged.
    /// </remarks>
    public bool AutoOrient { get; init; } = false;
}
