using TurboJpegWrapper.Internal;

namespace TurboJpegWrapper;

/// <summary>Represents the pixel density stored in the JPEG header.</summary>
/// <remarks>
/// Both dimensions must be between 1 and 65535, the unsigned 16-bit range of the JPEG density
/// fields; <see langword="default"/> (0 x 0) is not a valid density.
/// </remarks>
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
    /// <paramref name="X"/> or <paramref name="Y"/> is outside 1 through 65535, or
    /// <paramref name="Units"/> is not a defined value.
    /// </exception>
    public TurboJpegDensity(int X, int Y, TurboJpegDensityUnits Units)
    {
        ValidateDimension(X, nameof(X));
        ValidateDimension(Y, nameof(Y));
        ValidateUnits(Units);
        _x = X;
        _y = Y;
        _units = Units;
    }

    /// <summary>Gets the horizontal pixel density.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside 1 through 65535.</exception>
    public int X
    {
        get => _x;
        init
        {
            ValidateDimension(value, nameof(X));
            _x = value;
        }
    }

    /// <summary>Gets the vertical pixel density.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside 1 through 65535.</exception>
    public int Y
    {
        get => _y;
        init
        {
            ValidateDimension(value, nameof(Y));
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
        ValidateDimension(_x, nameof(X));
        ValidateDimension(_y, nameof(Y));
        ValidateUnits(_units);
    }

    private static void ValidateDimension(int value, string paramName)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, paramName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 65535, paramName);
    }

    private static void ValidateUnits(TurboJpegDensityUnits units)
    {
        if (units is < TurboJpegDensityUnits.Unknown or > TurboJpegDensityUnits.PixelsPerCentimeter)
        {
            ThrowHelper.ThrowDensityUnitsOutOfRange(units);
        }
    }
}
