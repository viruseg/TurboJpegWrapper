using System.Collections.Concurrent;

namespace TurboJpegWrapper.Internal.Pooling;

internal static class TurboJpegDecoderPool
{
    private static readonly ConcurrentQueue<TurboJpegDecoder> s_decoders = new();
    private static readonly int s_cap = Math.Max(4, Environment.ProcessorCount * 2);
    private static int s_retained;

    internal static TurboJpegDecoder Rent()
    {
        while (s_decoders.TryDequeue(out var decoder))
        {
            Interlocked.Decrement(ref s_retained);

            if (decoder.IsDisposed)
            {
                decoder.Dispose();
                continue;
            }

            return decoder;
        }

        return new TurboJpegDecoder();
    }

    internal static void Return(TurboJpegDecoder decoder)
    {
        if (decoder.IsDisposed)
        {
            return;
        }

        if (Interlocked.Increment(ref s_retained) <= s_cap)
        {
            s_decoders.Enqueue(decoder);
            return;
        }

        Interlocked.Decrement(ref s_retained);
        decoder.Dispose();
    }
}
