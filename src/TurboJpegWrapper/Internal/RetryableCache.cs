namespace TurboJpegWrapper.Internal;

internal sealed class RetryableCache<T> where T : class
{
    private readonly object _lock = new();
    private readonly Func<T> _factory;
    private T? _value;

    internal RetryableCache(Func<T> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    internal T GetOrCreate()
    {
        var value = Volatile.Read(ref _value);
        if (value is not null)
        {
            return value;
        }

        lock (_lock)
        {
            value = _value;
            if (value is null)
            {
                value = _factory() ??
                    throw new InvalidOperationException("The cache factory returned null.");
                Volatile.Write(ref _value, value);
            }

            return value;
        }
    }
}
