using TurboJpegWrapper.Internal.Encoder;

namespace TurboJpegWrapper;

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

    /// <summary>Gets the JPEG quality level, from 1 (lowest) to 100 (highest).</summary>
    /// <remarks>
    /// The default is 90. This option is ignored when <see cref="Lossless"/> is <see langword="true"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 1 or greater than 100.</exception>
    public int Quality
    {
        get;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(Quality));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, nameof(Quality));
            field = value;
        }
    } = 90;

    /// <summary>Gets the chroma subsampling scheme of the encoded image.</summary>
    /// <remarks>
    /// The default is <see cref="TurboJpegSubsampling.Yuv420"/>. This option is ignored when
    /// <see cref="Lossless"/> is <see langword="true"/> and when
    /// <see cref="Colorspace"/> is <see cref="TurboJpegColorspace.Rgb"/>. RGB output always uses
    /// 4:4:4; an explicit RGB colorspace takes precedence over
    /// <see cref="TurboJpegSubsampling.Grayscale"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is <see cref="TurboJpegSubsampling.Unknown"/> or not a defined subsampling scheme.
    /// </exception>
    public TurboJpegSubsampling Subsampling
    {
        get;
        init
        {
            if (value is < TurboJpegSubsampling.Yuv444 or > TurboJpegSubsampling.Yuv24)
            {
                ThrowHelper.ThrowSubsamplingOutOfRange(value);
            }

            field = value;
        }
    } = TurboJpegSubsampling.Yuv420;

    /// <summary>Gets the color space of the encoded image.</summary>
    /// <remarks>
    /// <para>
    /// The default is <see cref="TurboJpegColorspace.Default"/>, which resolves to
    /// <see cref="TurboJpegColorspace.YCbCr"/> for color output. This option is ignored when
    /// <see cref="Lossless"/> is <see langword="true"/>.
    /// </para>
    /// <para>
    /// <see cref="TurboJpegColorspace.Rgb"/> output is always encoded with 4:4:4 subsampling
    /// regardless of <see cref="Subsampling"/>, and RGB output cannot store a pixel density.
    /// Specifying <see cref="Density"/> together with RGB output is rejected when encoding starts.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is not supported for encoding.</exception>
    public TurboJpegColorspace Colorspace
    {
        get;
        init
        {
            if (value is not TurboJpegColorspace.Default
                and not TurboJpegColorspace.Rgb
                and not TurboJpegColorspace.YCbCr
                and not TurboJpegColorspace.Grayscale)
            {
                ThrowHelper.ThrowColorspaceOutOfRange(value);
            }

            field = value;
        }
    } = TurboJpegColorspace.Default;

    /// <summary>Gets a value indicating whether the fastest DCT/IDCT algorithm is used.</summary>
    /// <remarks>
    /// The default is <see langword="false"/>. This option is ignored when <see cref="Lossless"/>
    /// is <see langword="true"/>.
    /// </remarks>
    public bool FastDct { get; init; } = false;

    /// <summary>Gets a value indicating whether optimal Huffman coding tables are generated.</summary>
    /// <remarks>
    /// The default is <see langword="false"/>. This option is ignored when <see cref="Lossless"/>
    /// is <see langword="true"/>.
    /// </remarks>
    public bool OptimizeCoding { get; init; } = false;

    /// <summary>Gets a value indicating whether a progressive JPEG is produced.</summary>
    /// <remarks>
    /// The default is <see langword="false"/>. This option is ignored when <see cref="Lossless"/>
    /// is <see langword="true"/>.
    /// </remarks>
    public bool Progressive { get; init; } = false;

    /// <summary>Gets a value indicating whether arithmetic entropy coding is used.</summary>
    /// <remarks>
    /// The default is <see langword="false"/>. This option is ignored when <see cref="Lossless"/>
    /// is <see langword="true"/>.
    /// </remarks>
    public bool ArithmeticCoding { get; init; } = false;

    /// <summary>Gets a value indicating whether lossless JPEG compression is used.</summary>
    /// <remarks>
    /// The default is <see langword="false"/>. When this option is <see langword="true"/>, only
    /// <see cref="LosslessPredictor"/>, <see cref="LosslessPointTransform"/>,
    /// <see cref="RestartRows"/>, <see cref="MaxMemoryMegabytes"/>, <see cref="StopOnWarning"/>,
    /// and the BMP row order are applied; the lossy options are ignored. Lossless output is RGB,
    /// so it cannot store a pixel density: <see cref="Density"/> must be <see langword="null"/>,
    /// and BMP-derived density is ignored. A non-zero <see cref="RestartBlocks"/> is rejected
    /// when encoding starts.
    /// </remarks>
    public bool Lossless { get; init; } = false;

    /// <summary>Gets the lossless predictor selection value, from 1 through 7.</summary>
    /// <remarks>
    /// The default is 1. This option applies only when <see cref="Lossless"/> is
    /// <see langword="true"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 1 or greater than 7.</exception>
    public int LosslessPredictor
    {
        get;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(LosslessPredictor));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 7, nameof(LosslessPredictor));
            field = value;
        }
    } = 1;

    /// <summary>Gets the lossless point transform, from 0 through 7.</summary>
    /// <remarks>
    /// The default is 0. This option applies only when <see cref="Lossless"/> is
    /// <see langword="true"/>. A value greater than zero right-shifts the samples, so the encoded
    /// image is not bit-exact.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 0 or greater than 7.</exception>
    public int LosslessPointTransform
    {
        get;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(LosslessPointTransform));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 7, nameof(LosslessPointTransform));
            field = value;
        }
    } = 0;

    /// <summary>Gets the number of MCUs between restart markers; 0 disables restart markers.</summary>
    /// <remarks>
    /// The default is 0. This option applies only to lossy compression. A non-zero value cannot be
    /// combined with a non-zero <see cref="RestartRows"/>; that combination is rejected when
    /// encoding starts.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is less than 0 or greater than 65535.
    /// </exception>
    public int RestartBlocks
    {
        get;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(RestartBlocks));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(
                value, 65535, nameof(RestartBlocks));
            field = value;
        }
    } = 0;

    /// <summary>Gets the number of MCU rows between restart markers; 0 disables restart markers.</summary>
    /// <remarks>
    /// The default is 0. This option applies to lossy and lossless compression. A non-zero value
    /// cannot be combined with a non-zero <see cref="RestartBlocks"/>; that combination is rejected
    /// when encoding starts.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is less than 0 or greater than 65535.
    /// </exception>
    public int RestartRows
    {
        get;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(RestartRows));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 65535, nameof(RestartRows));
            field = value;
        }
    } = 0;

    /// <summary>Gets the maximum native intermediate memory in megabytes; 0 means no limit.</summary>
    /// <remarks>The default is 0.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int MaxMemoryMegabytes
    {
        get;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(MaxMemoryMegabytes));
            field = value;
        }
    } = 0;

    /// <summary>Gets a value indicating whether encoding stops when TurboJPEG reports a warning.</summary>
    /// <remarks>The default is <see langword="false"/>.</remarks>
    public bool StopOnWarning { get; init; } = false;

    /// <summary>Gets the pixel density written to the JPEG header.</summary>
    /// <remarks>
    /// <para>
    /// The default is <see langword="null"/>. Density is stored only in lossy YCbCr and grayscale
    /// JPEG files; in those modes, <see langword="null"/> uses the density stored in the BMP when
    /// it is non-zero and otherwise applies 1 x 1 with <see cref="TurboJpegDensityUnits.Unknown"/>
    /// units.
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
        get;
        init
        {
            value?.Validate();
            field = value;
        }
    } = null;
}
