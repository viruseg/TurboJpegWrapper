namespace TurboJpegWrapper;

/// <summary>Represents a TurboJPEG scaling factor as a numerator/denominator pair.</summary>
/// <remarks>Both components must be positive; <see langword="default"/> (0/0) is not a valid scaling factor.</remarks>
public readonly record struct TurboJpegScalingFactor
{
    private readonly int _numerator;
    private readonly int _denominator;

    /// <summary>Initializes a new instance of the <see cref="TurboJpegScalingFactor"/> struct.</summary>
    /// <param name="Numerator">The scaling factor numerator.</param>
    /// <param name="Denominator">The scaling factor denominator.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="Numerator"/> or <paramref name="Denominator"/> is zero or negative.
    /// </exception>
    public TurboJpegScalingFactor(int Numerator, int Denominator)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Numerator);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Denominator);
        _numerator = Numerator;
        _denominator = Denominator;
    }

    /// <summary>Gets the scaling factor numerator.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
    public int Numerator
    {
        get => _numerator;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _numerator = value;
        }
    }

    /// <summary>Gets the scaling factor denominator.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
    public int Denominator
    {
        get => _denominator;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _denominator = value;
        }
    }

    /// <summary>Gets the 1:1 scaling factor.</summary>
    public static TurboJpegScalingFactor One => new(1, 1);

    /// <summary>Gets the scaling factor as a floating-point value.</summary>
    public double Value => (double)Numerator / Denominator;

    /// <summary>Deconstructs the scaling factor into its numerator and denominator.</summary>
    /// <param name="Numerator">The scaling factor numerator.</param>
    /// <param name="Denominator">The scaling factor denominator.</param>
    public void Deconstruct(out int Numerator, out int Denominator)
    {
        Numerator = _numerator;
        Denominator = _denominator;
    }
}
