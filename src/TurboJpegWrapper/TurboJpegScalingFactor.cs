namespace TurboJpegWrapper;

/// <summary>Represents a TurboJPEG scaling factor as a numerator/denominator pair.</summary>
/// <remarks>Both components must be positive; <see langword="default"/> (0/0) is not a valid scaling factor.</remarks>
public readonly record struct TurboJpegScalingFactor
{
    /// <summary>Initializes a new instance of the <see cref="TurboJpegScalingFactor"/> struct.</summary>
    /// <param name="numerator">The scaling factor numerator.</param>
    /// <param name="denominator">The scaling factor denominator.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="numerator"/> or <paramref name="denominator"/> is zero or negative.
    /// </exception>
    public TurboJpegScalingFactor(int numerator, int denominator)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numerator);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(denominator);
        Numerator = numerator;
        Denominator = denominator;
    }

    /// <summary>Gets the scaling factor numerator.</summary>
    public int Numerator { get; }

    /// <summary>Gets the scaling factor denominator.</summary>
    public int Denominator { get; }

    /// <summary>Gets the 1:1 scaling factor.</summary>
    public static TurboJpegScalingFactor One => new(1, 1);

    /// <summary>Gets the scaling factor as a floating-point value.</summary>
    public double Value => (double)Numerator / Denominator;

    /// <summary>Deconstructs the scaling factor into its numerator and denominator.</summary>
    /// <param name="numerator">The scaling factor numerator.</param>
    /// <param name="denominator">The scaling factor denominator.</param>
    public void Deconstruct(out int numerator, out int denominator)
    {
        numerator = Numerator;
        denominator = Denominator;
    }
}
