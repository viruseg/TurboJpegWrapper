using TurboJpegWrapper.Internal;

namespace TurboJpegWrapper.Tests;

public sealed class RetryableCacheTests
{
    [Fact]
    public void GetOrCreate_AfterFactoryFailure_RetriesAndCachesSuccess()
    {
        var attempts = 0;
        var cache = new RetryableCache<object>(() =>
        {
            if (Interlocked.Increment(ref attempts) == 1)
            {
                throw new InvalidOperationException("transient");
            }

            return new object();
        });

        Assert.Throws<InvalidOperationException>(() => cache.GetOrCreate());
        var value = cache.GetOrCreate();

        Assert.Same(value, cache.GetOrCreate());
        Assert.Equal(2, attempts);
    }

    [Fact]
    public void GetOrCreate_ConcurrentCalls_CreateValueOnce()
    {
        var attempts = 0;
        var cache = new RetryableCache<object>(() =>
        {
            Interlocked.Increment(ref attempts);
            return new object();
        });
        var values = new object[64];

        Parallel.For(0, values.Length, i => values[i] = cache.GetOrCreate());

        Assert.Equal(1, attempts);
        Assert.All(values, value => Assert.Same(values[0], value));
    }

    [Fact]
    public void GetOrCreate_NullFactoryResult_ThrowsInvalidOperationException()
    {
        var cache = new RetryableCache<object>(() => null!);

        Assert.Throws<InvalidOperationException>(() => cache.GetOrCreate());
    }
}
