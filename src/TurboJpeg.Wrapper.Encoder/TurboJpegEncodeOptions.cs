namespace TurboJpegWrapper.Encoder;

/// <summary>Immutable options that control JPEG encoding.</summary>
/// <remarks>
/// Range and enum validation runs when a value is assigned and is unconditional, including
/// values that lossless mode later ignores. Cross-property rules involving restart markers and
/// lossless mode are checked when encoding starts.
/// </remarks>
public sealed record TurboJpegEncodeOptions
{
    /// <summary>Gets the default encoding options.</summary>
    public static TurboJpegEncodeOptions Default { get; } = new();

    private readonly int _quality = 90;
    private readonly TurboJpegSubsampling _subsampling = TurboJpegSubsampling.Yuv420;
    private readonly TurboJpegColorspace _colorspace = TurboJpegColorspace.Default;
    private readonly int _losslessPredictor = 1;
    private readonly int _losslessPointTransform = 0;
    private readonly int _restartBlocks = 0;
    private readonly int _restartRows = 0;
    private readonly int _maxMemoryMegabytes = 0;
    private readonly TurboJpegDensity? _density = null;

    /// <summary>Gets the JPEG quality level, from 1 (lowest) to 100 (highest).</summary>
    /// <remarks>This option is ignored when <see cref="Lossless"/> is <see langword="true"/>.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 1 or greater than 100.</exception>
    public int Quality
    {
        get => _quality;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(Quality));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, nameof(Quality));
            _quality = value;
        }
    }

    /// <summary>Gets the chroma subsampling scheme of the encoded image.</summary>
    /// <remarks>This option is ignored when <see cref="Lossless"/> is <see langword="true"/>.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is <see cref="TurboJpegSubsampling.Unknown"/> or not a defined subsampling scheme.
    /// </exception>
    public TurboJpegSubsampling Subsampling
    {
        get => _subsampling;
        init
        {
            if (value is < TurboJpegSubsampling.Yuv444 or > TurboJpegSubsampling.Yuv24)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Subsampling),
                    value,
                    "The subsampling must be a defined value other than Unknown.");
            }

            _subsampling = value;
        }
    }

    /// <summary>Gets the color space of the encoded image.</summary>
    /// <remarks>This option is ignored when <see cref="Lossless"/> is <see langword="true"/>.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a defined color space.</exception>
    public TurboJpegColorspace Colorspace
    {
        get => _colorspace;
        init
        {
            if (value is not TurboJpegColorspace.Default
                and not TurboJpegColorspace.Rgb
                and not TurboJpegColorspace.YCbCr
                and not TurboJpegColorspace.Grayscale)
            {
                throw new ArgumentOutOfRangeException(nameof(Colorspace), value, null);
            }

            _colorspace = value;
        }
    }

    /// <summary>Gets a value indicating whether the fastest DCT/IDCT algorithm is used.</summary>
    /// <remarks>This option is ignored when <see cref="Lossless"/> is <see langword="true"/>.</remarks>
    public bool FastDct { get; init; } = false;

    /// <summary>Gets a value indicating whether optimal Huffman coding tables are generated.</summary>
    /// <remarks>This option is ignored when <see cref="Lossless"/> is <see langword="true"/>.</remarks>
    public bool OptimizeCoding { get; init; } = false;

    /// <summary>Gets a value indicating whether a progressive JPEG is produced.</summary>
    /// <remarks>This option is ignored when <see cref="Lossless"/> is <see langword="true"/>.</remarks>
    public bool Progressive { get; init; } = false;

    /// <summary>Gets a value indicating whether arithmetic entropy coding is used.</summary>
    /// <remarks>This option is ignored when <see cref="Lossless"/> is <see langword="true"/>.</remarks>
    public bool ArithmeticCoding { get; init; } = false;

    /// <summary>Gets a value indicating whether lossless JPEG compression is used.</summary>
    /// <remarks>
    /// When this option is <see langword="true"/>, only <see cref="LosslessPredictor"/>,
    /// <see cref="LosslessPointTransform"/>, <see cref="RestartRows"/>, <see cref="Density"/>,
    /// <see cref="MaxMemoryMegabytes"/>, and <see cref="StopOnWarning"/> are applied; the lossy
    /// options are ignored. A non-zero <see cref="RestartBlocks"/> is rejected when encoding
    /// starts.
    /// </remarks>
    public bool Lossless { get; init; } = false;

    /// <summary>Gets the lossless predictor selection value, from 1 through 7.</summary>
    /// <remarks>This option applies only when <see cref="Lossless"/> is <see langword="true"/>.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 1 or greater than 7.</exception>
    public int LosslessPredictor
    {
        get => _losslessPredictor;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(LosslessPredictor));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 7, nameof(LosslessPredictor));
            _losslessPredictor = value;
        }
    }

    /// <summary>Gets the lossless point transform, from 0 through 7.</summary>
    /// <remarks>
    /// This option applies only when <see cref="Lossless"/> is <see langword="true"/>. A value
    /// greater than zero right-shifts the samples, so the encoded image is not bit-exact.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 0 or greater than 7.</exception>
    public int LosslessPointTransform
    {
        get => _losslessPointTransform;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(LosslessPointTransform));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 7, nameof(LosslessPointTransform));
            _losslessPointTransform = value;
        }
    }

    /// <summary>Gets the number of MCUs between restart markers; 0 disables restart markers.</summary>
    /// <remarks>
    /// This option applies only to lossy compression. A non-zero value cannot be combined with a
    /// non-zero <see cref="RestartRows"/>; that combination is rejected when encoding starts.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int RestartBlocks
    {
        get => _restartBlocks;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(RestartBlocks));
            _restartBlocks = value;
        }
    }

    /// <summary>Gets the number of MCU rows between restart markers; 0 disables restart markers.</summary>
    /// <remarks>
    /// This option applies to lossy and lossless compression. A non-zero value cannot be combined
    /// with a non-zero <see cref="RestartBlocks"/>; that combination is rejected when encoding starts.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int RestartRows
    {
        get => _restartRows;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(RestartRows));
            _restartRows = value;
        }
    }

    /// <summary>Gets the maximum native intermediate memory in megabytes; 0 means no limit.</summary>
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

    /// <summary>Gets a value indicating whether encoding stops when TurboJPEG reports a warning.</summary>
    public bool StopOnWarning { get; init; } = false;

    /// <summary>Gets the pixel density written to the JPEG header.</summary>
    /// <remarks>
    /// When this option is <see langword="null"/>, the encoder uses the density stored in the BMP
    /// when it is non-zero and otherwise applies the native default density.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The assigned density has a non-positive dimension or undefined units.
    /// </exception>
    public TurboJpegDensity? Density
    {
        get => _density;
        init
        {
            value?.Validate();
            _density = value;
        }
    }
}
