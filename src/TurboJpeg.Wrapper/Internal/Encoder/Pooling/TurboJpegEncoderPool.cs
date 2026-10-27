using System.Collections.Concurrent;

namespace TurboJpegWrapper.Internal.Encoder.Pooling;

internal sealed class TurboJpegEncoderPool
{
    internal static TurboJpegEncoderPool Shared { get; } = new();

    private readonly ConcurrentQueue<TurboJpegEncoder> _encoders = new();
    private readonly int _cap = Math.Max(4, Environment.ProcessorCount * 2);
    private int _retained;

    internal int Count => _encoders.Count;

    internal TurboJpegEncoder Rent()
    {
        while (_encoders.TryDequeue(out var encoder))
        {
            Interlocked.Decrement(ref _retained);

            if (encoder.IsDisposed)
            {
                encoder.Dispose();
                continue;
            }

            return encoder;
        }

        return new TurboJpegEncoder();
    }

    internal void Return(TurboJpegEncoder encoder)
    {
        if (encoder.IsDisposed)
        {
            return;
        }

        if (Interlocked.Increment(ref _retained) <= _cap)
        {
            _encoders.Enqueue(encoder);
            return;
        }

        Interlocked.Decrement(ref _retained);
        encoder.Dispose();
    }
}
