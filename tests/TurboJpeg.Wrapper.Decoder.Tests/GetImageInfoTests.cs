using TurboJpegWrapper;

namespace TurboJpegWrapper.Decoder.Tests;

public sealed class GetImageInfoTests
{
    [Fact]
    public void GetImageInfo_ReturnsDimensionsAndFormat()
    {
        using var decoder = new TurboJpegDecoder();

        var info = decoder.GetImageInfo(TestFixtures.Load("baseline-420.jpg"));

        Assert.Equal(35, info.Width);
        Assert.Equal(23, info.Height);
        Assert.Equal(8, info.Precision);
        Assert.Equal(TurboJpegColorspace.YCbCr, info.Colorspace);
        Assert.Equal(TurboJpegSubsampling.Yuv420, info.Subsampling);
        Assert.False(info.IsLossless);
        Assert.False(info.IsProgressive);
        Assert.False(info.UsesArithmeticCoding);
        Assert.True(info.CanDecodeToBmp);
    }

    [Fact]
    public void GetImageInfo_Grayscale_ReportsGray()
    {
        using var decoder = new TurboJpegDecoder();
        var info = decoder.GetImageInfo(TestFixtures.Load("grayscale.jpg"));

        Assert.Equal(TurboJpegColorspace.Grayscale, info.Colorspace);
        Assert.Equal(TurboJpegSubsampling.Grayscale, info.Subsampling);
    }

    [Fact]
    public void GetImageInfo_Precision12_IsReportedAndDecodable()
    {
        using var decoder = new TurboJpegDecoder();
        var info = decoder.GetImageInfo(TestFixtures.Load("precision12.jpg"));

        Assert.Equal(12, info.Precision);
        Assert.True(info.CanDecodeToBmp);
    }

    [Fact]
    public void GetImageInfo_Garbage_ThrowsTurboJpegException()
    {
        using var decoder = new TurboJpegDecoder();
        Assert.Throws<TurboJpegException>(() => decoder.GetImageInfo(new byte[] { 1, 2, 3, 4 }));
    }

    [Fact]
    public void GetImageInfo_AfterHeaderFailure_CanReadAgain()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = TestFixtures.Load("baseline-444.jpg");

        Assert.Throws<TurboJpegException>(
            () => decoder.GetImageInfo(jpeg.AsSpan(0, jpeg.Length / 2)));

        var info = decoder.GetImageInfo(jpeg);

        Assert.Equal(TestFixtures.Width, info.Width);
        Assert.Equal(TestFixtures.Height, info.Height);
    }

    [Fact]
    public void GetImageInfo_Empty_ThrowsArgumentException()
    {
        using var decoder = new TurboJpegDecoder();
        Assert.Throws<ArgumentException>(() => decoder.GetImageInfo(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void Decoder_AfterDispose_ThrowsObjectDisposedException()
    {
        var decoder = new TurboJpegDecoder();
        decoder.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () => decoder.GetImageInfo(TestFixtures.Load("baseline-444.jpg")));
    }

    [Theory]
    [InlineData(TurboJpegColorspace.YCbCr, 1, false)]
    [InlineData(TurboJpegColorspace.YCbCr, 2, true)]
    [InlineData(TurboJpegColorspace.Grayscale, 2, true)]
    [InlineData(TurboJpegColorspace.Rgb, 2, true)]
    [InlineData(TurboJpegColorspace.Cmyk, 2, true)]
    [InlineData(TurboJpegColorspace.Ycck, 2, true)]
    [InlineData(TurboJpegColorspace.YCbCr, 8, true)]
    [InlineData(TurboJpegColorspace.Grayscale, 8, true)]
    [InlineData(TurboJpegColorspace.Rgb, 8, true)]
    [InlineData(TurboJpegColorspace.Cmyk, 8, true)]
    [InlineData(TurboJpegColorspace.Ycck, 8, true)]
    [InlineData(TurboJpegColorspace.YCbCr, 12, true)]
    [InlineData(TurboJpegColorspace.Grayscale, 12, true)]
    [InlineData(TurboJpegColorspace.Rgb, 12, true)]
    [InlineData(TurboJpegColorspace.Cmyk, 12, true)]
    [InlineData(TurboJpegColorspace.Ycck, 12, true)]
    [InlineData(TurboJpegColorspace.YCbCr, 16, true)]
    [InlineData(TurboJpegColorspace.Grayscale, 16, true)]
    [InlineData(TurboJpegColorspace.Rgb, 16, true)]
    [InlineData(TurboJpegColorspace.Cmyk, 16, true)]
    [InlineData(TurboJpegColorspace.Ycck, 16, true)]
    [InlineData(TurboJpegColorspace.Cmyk, 17, false)]
    public void CanDecodeToBmp_ReflectsSupportedFormats(
        TurboJpegColorspace colorspace, int precision, bool expected)
    {
        var info = new TurboJpegImageInfo(
            35, 23, precision, colorspace, TurboJpegSubsampling.Yuv420, false);

        Assert.Equal(expected, info.CanDecodeToBmp);
    }
}
