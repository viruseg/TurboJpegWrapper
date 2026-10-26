namespace TurboJpegWrapper.Encoder;

/// <summary>Immutable options that control JPEG encoding.</summary>
/// <remarks>
/// Range and enum validation runs when a value is assigned and is unconditional, including
/// values that lossless mode later ignores. Cross-property rules involving restart markers,
/// lossless mode, RGB output, and density are checked when encoding starts.
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
    /// <remarks>
    /// This option is ignored when <see cref="Lossless"/> is <see langword="true"/> and when
    /// <see cref="Colorspace"/> is <see cref="TurboJpegColorspace.Rgb"/>. RGB output always uses
    /// 4:4:4; an explicit RGB colorspace takes precedence over
    /// <see cref="TurboJpegSubsampling.Grayscale"/>.
    /// </remarks>
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
    /// <remarks>
    /// <para>This option is ignored when <see cref="Lossless"/> is <see langword="true"/>.</para>
    /// <para>
    /// <see cref="TurboJpegColorspace.Rgb"/> output is always encoded with 4:4:4 subsampling
    /// regardless of <see cref="Subsampling"/>, and RGB output cannot store a pixel density.
    /// Specifying <see cref="Density"/> together with RGB output is rejected when encoding starts.
    /// </para>
    /// </remarks>
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
                throw new ArgumentOutOfRangeException(
                    nameof(Colorspace),
                    value,
                    "The color space must be Default, Rgb, YCbCr, or Grayscale.");
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
    /// <see cref="LosslessPointTransform"/>, <see cref="RestartRows"/>,
    /// <see cref="MaxMemoryMegabytes"/>, <see cref="StopOnWarning"/>, and the BMP row order are
    /// applied; the lossy options are ignored. Lossless output is RGB, so it cannot store a
    /// pixel density: <see cref="Density"/> must be <see langword="null"/>, and BMP-derived
    /// density is ignored. A non-zero <see cref="RestartBlocks"/> is rejected when encoding starts.
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is less than 0 or greater than 65535.
    /// </exception>
    public int RestartBlocks
    {
        get => _restartBlocks;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(RestartBlocks));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(
                value, 65535, nameof(RestartBlocks));
            _restartBlocks = value;
        }
    }

    /// <summary>Gets the number of MCU rows between restart markers; 0 disables restart markers.</summary>
    /// <remarks>
    /// This option applies to lossy and lossless compression. A non-zero value cannot be combined
    /// with a non-zero <see cref="RestartBlocks"/>; that combination is rejected when encoding starts.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is less than 0 or greater than 65535.
    /// </exception>
    public int RestartRows
    {
        get => _restartRows;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(RestartRows));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 65535, nameof(RestartRows));
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
    /// <para>
    /// Density is stored only in lossy YCbCr and grayscale JPEG files. When this option is
    /// <see langword="null"/>, those modes use the density stored in the BMP when it is non-zero
    /// and otherwise apply 1 x 1 with <see cref="TurboJpegDensityUnits.Unknown"/> units.
    /// </para>
    /// <para>
    /// Lossless and RGB output cannot store a density. Specifying this option together with
    /// <see cref="Lossless"/> = <see langword="true"/> or <see cref="Colorspace"/> =
    /// <see cref="TurboJpegColorspace.Rgb"/> throws <see cref="ArgumentException"/> with
    /// parameter name <c>options</c> when encoding or sizing starts. BMP-derived density is
    /// ignored in those modes; 1 x 1 with <see cref="TurboJpegDensityUnits.Unknown"/> units is
    /// applied instead.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The assigned density has a dimension outside 1 through 65535 or undefined units.
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
