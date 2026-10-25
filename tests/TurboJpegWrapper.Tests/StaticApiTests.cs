using TurboJpegWrapper;
using TurboJpegWrapper.Internal.Pooling;

namespace TurboJpegWrapper.Tests;

public sealed class StaticApiTests
{
    [Fact]
    public void DecodeToBmp_Static_Works()
    {
        var bmp = TurboJpeg.DecodeToBmp(TestFixtures.Load("baseline-444.jpg"));

        Assert.Equal(35, BmpTestHelpers.Width(bmp));
        Assert.Equal(54 + 35 * 23 * 4, bmp.Length);
    }

    [Fact]
    public void DecodeToBmp_StaticAndInstance_AreByteIdentical()
    {
        var jpeg = TestFixtures.Load("baseline-420.jpg");

        var pooled = TurboJpeg.DecodeToBmp(jpeg);
        using var decoder = new TurboJpegDecoder();
        var instance = decoder.DecodeToBmp(jpeg);

        Assert.Equal(instance, pooled);
    }

    [Fact]
    public void DecodeToBmp_ConcurrentCalls_AreCorrect()
    {
        var jpeg = TestFixtures.Load("baseline-420.jpg");
        var failures = new System.Collections.Concurrent.ConcurrentBag<string>();

        Parallel.For(0, 64, i =>
        {
            try
            {
                var bmp = TurboJpeg.DecodeToBmp(jpeg);
                if (BmpTestHelpers.Width(bmp) != 35 || BmpTestHelpers.Height(bmp) != 23)
                    failures.Add($"bad dimensions at {i}");
            }
            catch (Exception ex)
            {
                failures.Add(ex.ToString());
            }
        });

        Assert.Empty(failures);
    }

    [Fact]
    public void DecodeToBmp_AfterFailure_PoolStillWorks()
    {
        Assert.Throws<TurboJpegException>(
            () => TurboJpeg.DecodeToBmp(new byte[] { 1, 2, 3 }));

        var bmp = TurboJpeg.DecodeToBmp(TestFixtures.Load("baseline-444.jpg"));
        Assert.Equal(35, BmpTestHelpers.Width(bmp));
    }

    [Fact]
    public void GetImageInfo_Static_Works()
    {
        var info = TurboJpeg.GetImageInfo(TestFixtures.Load("baseline-444.jpg"));
        Assert.Equal(35, info.Width);
        Assert.Equal(23, info.Height);
    }

    [Fact]
    public void DecodeToBmp_StaticWithOptions_UsesScaling()
    {
        var options = new TurboJpegDecodeOptions(
            scalingFactor: new TurboJpegScalingFactor(1, 2));

        var bmp = TurboJpeg.DecodeToBmp(TestFixtures.Load("baseline-444.jpg"), options);

        Assert.Equal(18, BmpTestHelpers.Width(bmp));
        Assert.Equal(12, BmpTestHelpers.Height(bmp));
    }

    [Fact]
    public void DecodeToBmp_Static_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => TurboJpeg.DecodeToBmp(TestFixtures.Load("baseline-444.jpg"), null!));
    }

    [Fact]
    public void DecodeToBmp_StaticWith24BitOptions_Writes24BitBmp()
    {
        var bmp = TurboJpeg.DecodeToBmp(
            TestFixtures.Load("baseline-444.jpg"),
            new TurboJpegDecodeOptions(bmpBitDepth: TurboJpegBmpBitDepth.Bpp24));

        Assert.Equal(24, BmpTestHelpers.BitsPerPixel(bmp));
        Assert.Equal(54 + 108 * 23, bmp.Length);
    }

    [Fact]
    public void Pool_Return_DoesNotEnqueueDisposedDecoder()
    {
        var pool = new TurboJpegDecoderPool();
        var disposed = new TurboJpegDecoder();
        disposed.Dispose();

        pool.Return(disposed);

        Assert.Equal(0, pool.Count);

        var rented = pool.Rent();
        try
        {
            Assert.False(rented.IsDisposed);
        }
        finally
        {
            rented.Dispose();
        }
    }
}
