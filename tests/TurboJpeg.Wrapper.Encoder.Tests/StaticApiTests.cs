using System.Collections.Concurrent;
using TurboJpegWrapper.Encoder;
using TurboJpegWrapper.Encoder.Internal.Pooling;
using DecoderApi = TurboJpegWrapper.Decoder.TurboJpeg;

namespace TurboJpegWrapper.Encoder.Tests;

public sealed class StaticApiTests
{
    private static (byte B, byte G, byte R) Gradient(int x, int y) =>
        ((byte)(x * 7 + y * 3), (byte)(x * 3 + y * 11), (byte)(x * 5 + y * 13));

    [Fact]
    public void EncodeToJpeg_DefaultOptions_MatchesInstanceOutput()
    {
        var bmp = BmpBuilder.Create24(35, 23, Gradient);
        using var encoder = new TurboJpegEncoder();

        var instance = encoder.EncodeToJpeg(bmp);
        var pooled = TurboJpeg.EncodeToJpeg(bmp);

        Assert.Equal(instance, pooled);
    }

    [Fact]
    public void EncodeToJpeg_LosslessOptions_MatchesInstanceOutput()
    {
        var bmp = BmpBuilder.Create24(35, 23, Gradient);
        var options = new TurboJpegEncodeOptions { Lossless = true };
        using var encoder = new TurboJpegEncoder();

        var instance = encoder.EncodeToJpeg(bmp, options);
        var pooled = TurboJpeg.EncodeToJpeg(bmp, options);

        Assert.Equal(instance, pooled);
    }

    [Fact]
    public void EncodeToJpeg_StaticWithOptions_WritesCorrectDimensions()
    {
        var bmp = BmpBuilder.Create24(35, 23, Gradient);
        var options = new TurboJpegEncodeOptions { Quality = 75 };

        var jpeg = TurboJpeg.EncodeToJpeg(bmp, options);
        var info = DecoderApi.GetImageInfo(jpeg);

        Assert.Equal(35, info.Width);
        Assert.Equal(23, info.Height);
    }

    [Fact]
    public void EncodeToJpeg_ConcurrentCalls_AreCorrect()
    {
        var bmp = BmpBuilder.Create24(35, 23, Gradient);
        var failures = new ConcurrentBag<string>();

        Parallel.For(0, 64, i =>
        {
            try
            {
                var info = DecoderApi.GetImageInfo(TurboJpeg.EncodeToJpeg(bmp));
                if (info.Width != 35 || info.Height != 23)
                {
                    failures.Add(
                        $"bad dimensions {info.Width}x{info.Height} at iteration {i}");
                }
            }
            catch (Exception ex)
            {
                failures.Add(ex.ToString());
            }
        });

        Assert.Empty(failures);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EncodeToJpeg_AfterFailure_PoolStillWorks(bool emptyBmp)
    {
        var invalid = emptyBmp
            ? Array.Empty<byte>()
            : BmpBuilder.CreateRaw(validSignature: false);

        Assert.Throws<ArgumentException>(() => TurboJpeg.EncodeToJpeg(invalid));

        var jpeg = TurboJpeg.EncodeToJpeg(BmpBuilder.Create24(35, 23, Gradient));
        Assert.Equal(35, DecoderApi.GetImageInfo(jpeg).Width);
    }

    [Fact]
    public void EncodeToJpeg_StaticDestination_MatchesInstanceOutput()
    {
        var bmp = BmpBuilder.Create24(23, 17, Gradient);
        using var encoder = new TurboJpegEncoder();

        var expected = encoder.EncodeToJpeg(bmp);
        var destination = new byte[TurboJpeg.GetMaxJpegSize(bmp)];
        var written = TurboJpeg.EncodeToJpeg(
            bmp, TurboJpegEncodeOptions.Default, destination);

        Assert.Equal(expected.Length, written);
        Assert.True(destination.AsSpan(0, written).SequenceEqual(expected));
    }

    [Fact]
    public void EncodeToJpeg_NullOptions_ThrowsArgumentNullException()
    {
        var bmp = BmpBuilder.Create24(2, 2, Gradient);

        Assert.Throws<ArgumentNullException>(() => TurboJpeg.EncodeToJpeg(bmp, null!));
        Assert.Throws<ArgumentNullException>(
            () => TurboJpeg.EncodeToJpeg(bmp, null!, new byte[1024]));
    }

    [Fact]
    public void InvalidCrossPropertyOptions_ThrowFromStaticEncodeAndSizing()
    {
        var bmp = BmpBuilder.Create24(2, 2, Gradient);
        var options = new TurboJpegEncodeOptions { RestartBlocks = 1, RestartRows = 1 };

        var encode = Assert.Throws<ArgumentException>(
            () => TurboJpeg.EncodeToJpeg(bmp, options));
        var sizing = Assert.Throws<ArgumentException>(
            () => TurboJpeg.GetMaxJpegSize(bmp, options));

        Assert.Equal("options", encode.ParamName);
        Assert.Equal("options", sizing.ParamName);
    }

    [Fact]
    public void GetMaxJpegSize_Static_MatchesInstanceResult()
    {
        var bmp = BmpBuilder.Create24(37, 29, Gradient);
        var options = new TurboJpegEncodeOptions
        {
            Quality = 95,
            Subsampling = TurboJpegSubsampling.Yuv444,
        };
        using var encoder = new TurboJpegEncoder();

        Assert.Equal(encoder.GetMaxJpegSize(bmp), TurboJpeg.GetMaxJpegSize(bmp));
        Assert.Equal(
            encoder.GetMaxJpegSize(bmp, options),
            TurboJpeg.GetMaxJpegSize(bmp, options));
    }

    [Fact]
    public void Pool_Return_DoesNotEnqueueDisposedEncoder()
    {
        var pool = new TurboJpegEncoderPool();
        var disposed = new TurboJpegEncoder();
        disposed.Dispose();

        pool.Return(disposed);

        Assert.Equal(0, pool.Count);

        var rented = pool.Rent();
        try
        {
            Assert.False(rented.IsDisposed);
            var jpeg = rented.EncodeToJpeg(BmpBuilder.Create24(4, 3, Gradient));
            Assert.Equal(4, DecoderApi.GetImageInfo(jpeg).Width);
        }
        finally
        {
            rented.Dispose();
        }
    }
}
