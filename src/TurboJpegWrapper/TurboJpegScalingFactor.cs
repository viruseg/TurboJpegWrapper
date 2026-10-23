namespace TurboJpegWrapper;

/// <summary>Represents a TurboJPEG scaling factor as a numerator/denominator pair.</summary>
/// <param name="Numerator">The scaling factor numerator.</param>
/// <param name="Denominator">The scaling factor denominator.</param>
public readonly record struct TurboJpegScalingFactor(int Numerator, int Denominator)
{
    /// <summary>Gets the 1:1 scaling factor.</summary>
    public static TurboJpegScalingFactor One => new(1, 1);

    /// <summary>Gets the scaling factor as a floating-point value.</summary>
    public double Value => (double)Numerator / Denominator;
}
