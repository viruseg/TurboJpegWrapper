namespace TurboJpegWrapper;

/// <summary>Represents an error reported by the TurboJPEG native library.</summary>
public class TurboJpegException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="TurboJpegException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="isFatal">Whether the native error is fatal.</param>
    public TurboJpegException(string message, bool isFatal = true)
        : base(message)
    {
        IsFatal = isFatal;
    }

    /// <summary>Initializes a new instance of the <see cref="TurboJpegException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    /// <param name="isFatal">Whether the native error is fatal.</param>
    public TurboJpegException(string message, Exception innerException, bool isFatal = true)
        : base(message, innerException)
    {
        IsFatal = isFatal;
    }

    /// <summary>Gets a value indicating whether the native error is fatal.</summary>
    public bool IsFatal { get; }
}

/// <summary>Represents a JPEG that this library does not support decoding.</summary>
public sealed class TurboJpegUnsupportedException : TurboJpegException
{
    /// <summary>Initializes a new instance of the <see cref="TurboJpegUnsupportedException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public TurboJpegUnsupportedException(string message)
        : base(message, isFatal: false)
    {
    }
}

/// <summary>Represents a JPEG whose decoded output would exceed the configured limits.</summary>
public sealed class TurboJpegImageTooLargeException : TurboJpegException
{
    /// <summary>Initializes a new instance of the <see cref="TurboJpegImageTooLargeException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public TurboJpegImageTooLargeException(string message)
        : base(message, isFatal: false)
    {
    }
}
