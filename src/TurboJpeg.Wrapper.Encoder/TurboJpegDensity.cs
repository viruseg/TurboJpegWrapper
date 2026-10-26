namespace TurboJpegWrapper.Encoder;

/// <summary>Represents the pixel density stored in the JPEG header.</summary>
/// <remarks>Both dimensions must be positive; <see langword="default"/> (0 x 0) is not a valid density.</remarks>
public readonly record struct TurboJpegDensity
{
    private readonly int _x;
    private readonly int _y;
    private readonly TurboJpegDensityUnits _units;

    /// <summary>Initializes a new instance of the <see cref="TurboJpegDensity"/> struct.</summary>
    /// <param name="X">The horizontal pixel density.</param>
    /// <param name="Y">The vertical pixel density.</param>
    /// <param name="Units">The units of the pixel density.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="X"/> or <paramref name="Y"/> is zero or negative, or
    /// <paramref name="Units"/> is not a defined value.
    /// </exception>
    public TurboJpegDensity(int X, int Y, TurboJpegDensityUnits Units)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(X);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Y);
        ValidateUnits(Units);
        _x = X;
        _y = Y;
        _units = Units;
    }

    /// <summary>Gets the horizontal pixel density.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
    public int X
    {
        get => _x;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _x = value;
        }
    }

    /// <summary>Gets the vertical pixel density.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
    public int Y
    {
        get => _y;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _y = value;
        }
    }

    /// <summary>Gets the units of the pixel density.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a defined density unit.</exception>
    public TurboJpegDensityUnits Units
    {
        get => _units;
        init
        {
            ValidateUnits(value);
            _units = value;
        }
    }

    /// <summary>Deconstructs the density into its horizontal density, vertical density, and units.</summary>
    /// <param name="X">The horizontal pixel density.</param>
    /// <param name="Y">The vertical pixel density.</param>
    /// <param name="Units">The units of the pixel density.</param>
    public void Deconstruct(out int X, out int Y, out TurboJpegDensityUnits Units)
    {
        X = _x;
        Y = _y;
        Units = _units;
    }

    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_x, nameof(X));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_y, nameof(Y));
        ValidateUnits(_units);
    }

    private static void ValidateUnits(TurboJpegDensityUnits units)
    {
        if (units is < TurboJpegDensityUnits.Unknown or > TurboJpegDensityUnits.PixelsPerCentimeter)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Units),
                units,
                "The density units must be Unknown, PixelsPerInch, or PixelsPerCentimeter.");
        }
    }
}
