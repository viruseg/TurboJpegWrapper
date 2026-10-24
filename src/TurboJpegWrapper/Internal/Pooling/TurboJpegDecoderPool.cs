using System.Collections.Concurrent;

namespace TurboJpegWrapper.Internal.Pooling;

internal sealed class TurboJpegDecoderPool
{
    internal static TurboJpegDecoderPool Shared { get; } = new();

    private readonly ConcurrentQueue<TurboJpegDecoder> _decoders = new();
    private readonly int _cap = Math.Max(4, Environment.ProcessorCount * 2);
    private int _retained;

    internal int Count => _decoders.Count;

    internal TurboJpegDecoder Rent()
    {
        while (_decoders.TryDequeue(out var decoder))
        {
            Interlocked.Decrement(ref _retained);

            if (decoder.IsDisposed)
            {
                decoder.Dispose();
                continue;
            }

            return decoder;
        }

        return new TurboJpegDecoder();
    }

    internal void Return(TurboJpegDecoder decoder)
    {
        if (decoder.IsDisposed)
        {
            return;
        }

        if (Interlocked.Increment(ref _retained) <= _cap)
        {
            _decoders.Enqueue(decoder);
            return;
        }

        Interlocked.Decrement(ref _retained);
        decoder.Dispose();
    }
}
