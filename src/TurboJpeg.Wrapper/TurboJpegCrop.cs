namespace TurboJpegWrapper;

/// <summary>Defines a rectangular region of a JPEG image to decode.</summary>
/// <remarks>
/// Coordinates refer to the scaled, stored image: <see cref="TurboJpegDecodeOptions.ScalingFactor"/>
/// is applied first, and <see cref="TurboJpegDecodeOptions.AutoOrient"/> then transforms the cropped
/// pixels. The region must fit inside the scaled image, and <see cref="X"/> must be divisible by the
/// scaled iMCU width; <see cref="Y"/> is not required to be iMCU-aligned. Cropping is supported only
/// for lossy JPEGs with a known subsampling level.
/// </remarks>
public readonly record struct TurboJpegCrop
{
    /// <summary>Initializes a new instance of the <see cref="TurboJpegCrop"/> struct.</summary>
    /// <param name="X">The horizontal offset in the scaled image.</param>
    /// <param name="Y">The vertical offset in the scaled image.</param>
    /// <param name="Width">The region width in pixels.</param>
    /// <param name="Height">The region height in pixels.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="X"/> or <paramref name="Y"/> is negative, or
    /// <paramref name="Width"/> or <paramref name="Height"/> is zero or negative.
    /// </exception>
    public TurboJpegCrop(int X, int Y, int Width, int Height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(X);
        ArgumentOutOfRangeException.ThrowIfNegative(Y);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Height);
        this.X = X;
        this.Y = Y;
        this.Width = Width;
        this.Height = Height;
    }

    /// <summary>Gets the horizontal offset in the scaled image.</summary>
    public int X { get; }

    /// <summary>Gets the vertical offset in the scaled image.</summary>
    public int Y { get; }

    /// <summary>Gets the region width in pixels.</summary>
    public int Width { get; }

    /// <summary>Gets the region height in pixels.</summary>
    public int Height { get; }
}
