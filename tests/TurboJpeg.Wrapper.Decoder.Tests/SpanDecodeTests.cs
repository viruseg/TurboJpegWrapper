using TurboJpegWrapper;

namespace TurboJpegWrapper.Decoder.Tests;

public sealed class SpanDecodeTests
{
    public static IEnumerable<object[]> DecodeCases()
    {
        yield return [TurboJpegBmpBitDepth.Bpp32, false, 0, 1, 1];
        yield return [TurboJpegBmpBitDepth.Bpp8, false, 0, 1, 1];
        yield return [TurboJpegBmpBitDepth.Bpp24, false, 0, 1, 1];
        yield return [TurboJpegBmpBitDepth.Bpp32, true, 6, 1, 1];
        yield return [TurboJpegBmpBitDepth.Bpp32, true, 3, 1, 1];
        yield return [TurboJpegBmpBitDepth.Bpp32, false, 0, 2, 1];
    }

    [Theory]
    [MemberData(nameof(DecodeCases))]
    public void GetBmpSize_MatchesDecodeLength(
        TurboJpegBmpBitDepth bitDepth,
        bool autoOrient,
        int exifOrientation,
        int numerator,
        int denominator)
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = LoadJpeg(exifOrientation);
        var options = CreateOptions(bitDepth, autoOrient, numerator, denominator);

        var expected = decoder.DecodeToBmp(jpeg, options);
        var size = decoder.GetBmpSize(jpeg, options);

        Assert.Equal(expected.Length, size);
    }

    [Theory]
    [MemberData(nameof(DecodeCases))]
    public void DecodeToBmp_Span_MatchesByteArray(
        TurboJpegBmpBitDepth bitDepth,
        bool autoOrient,
        int exifOrientation,
        int numerator,
        int denominator)
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = LoadJpeg(exifOrientation);
        var options = CreateOptions(bitDepth, autoOrient, numerator, denominator);
        var expected = decoder.DecodeToBmp(jpeg, options);
        var destination = new byte[expected.Length];

        var written = decoder.DecodeToBmp(jpeg, options, destination);

        Assert.Equal(expected.Length, written);
        Assert.Equal(expected, destination);
    }

    [Fact]
    public void DecodeToBmp_Span_TooSmall_ThrowsAndLeavesDestinationUntouched()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = LoadJpeg(6);
        var options = new TurboJpegDecodeOptions { AutoOrient = true };
        var size = decoder.GetBmpSize(jpeg, options);
        var destination = new byte[size - 1];
        Array.Fill(destination, (byte)0xEE);

        Assert.Throws<ArgumentException>(() => decoder.DecodeToBmp(jpeg, options, destination));
        Assert.All(destination, value => Assert.Equal(0xEE, value));
    }

    [Fact]
    public void DecodeToBmp_Span_ExactOverlap_ThrowsAndLeavesBufferUntouched()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = LoadJpeg(0);
        var options = new TurboJpegDecodeOptions { BmpBitDepth = TurboJpegBmpBitDepth.Bpp24 };
        var size = decoder.GetBmpSize(jpeg, options);
        var buffer = new byte[Math.Max(size, jpeg.Length)];
        jpeg.CopyTo(buffer, 0);
        var snapshot = buffer.ToArray();

        var exception = Assert.Throws<ArgumentException>(
            () => decoder.DecodeToBmp(
                buffer.AsSpan(0, jpeg.Length), options, buffer.AsSpan(0, size)));

        Assert.Equal("destination", exception.ParamName);
        Assert.Equal(snapshot, buffer);
    }

    [Fact]
    public void DecodeToBmp_Span_PartialOverlap_ThrowsArgumentException()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = LoadJpeg(0);
        var options = new TurboJpegDecodeOptions { BmpBitDepth = TurboJpegBmpBitDepth.Bpp24 };
        var size = decoder.GetBmpSize(jpeg, options);
        var buffer = new byte[jpeg.Length + size];
        jpeg.CopyTo(buffer, 0);

        var exception = Assert.Throws<ArgumentException>(
            () => decoder.DecodeToBmp(
                buffer.AsSpan(0, jpeg.Length), options, buffer.AsSpan(jpeg.Length - 1, size)));

        Assert.Equal("destination", exception.ParamName);
    }

    [Fact]
    public void DecodeToBmp_Span_AdjacentDestination_Decodes()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = LoadJpeg(0);
        var options = new TurboJpegDecodeOptions { BmpBitDepth = TurboJpegBmpBitDepth.Bpp24 };
        var expected = decoder.DecodeToBmp(jpeg, options);
        var buffer = new byte[jpeg.Length + expected.Length];
        jpeg.CopyTo(buffer, 0);

        var written = decoder.DecodeToBmp(
            buffer.AsSpan(0, jpeg.Length), options, buffer.AsSpan(jpeg.Length));

        Assert.Equal(expected.Length, written);
        Assert.Equal(expected, buffer.AsSpan(jpeg.Length).ToArray());
    }

    [Fact]
    public void DecodeToBmp_StaticSpan_Overlap_ThrowsArgumentException()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = LoadJpeg(0);
        var options = new TurboJpegDecodeOptions { BmpBitDepth = TurboJpegBmpBitDepth.Bpp24 };
        var size = decoder.GetBmpSize(jpeg, options);
        var buffer = new byte[Math.Max(size, jpeg.Length)];
        jpeg.CopyTo(buffer, 0);

        var exception = Assert.Throws<ArgumentException>(
            () => TurboJpeg.DecodeToBmp(
                buffer.AsSpan(0, jpeg.Length), options, buffer.AsSpan(0, size)));

        Assert.Equal("destination", exception.ParamName);
    }

    [Fact]
    public void StaticApi_GetBmpSizeAndSpanDecode_Match()
    {
        var jpeg = TestFixtures.Load("baseline-444.jpg");
        var options = new TurboJpegDecodeOptions
        {
            ScalingFactor = new TurboJpegScalingFactor(3, 2),
            BmpBitDepth = TurboJpegBmpBitDepth.Bpp24
        };
        var size = TurboJpeg.GetBmpSize(jpeg, options);
        var destination = new byte[size];

        var written = TurboJpeg.DecodeToBmp(jpeg, options, destination);

        Assert.Equal(size, written);
        Assert.Equal(3 * TestFixtures.Width / 2 + 1, BmpTestHelpers.Width(destination));
        Assert.Equal(3 * TestFixtures.Height / 2 + 1, BmpTestHelpers.Height(destination));
        Assert.Equal(24, BmpTestHelpers.BitsPerPixel(destination));
    }

    [Fact]
    public void GetBmpSize_Empty_ThrowsArgumentException()
    {
        using var decoder = new TurboJpegDecoder();

        Assert.Throws<ArgumentException>(() => decoder.GetBmpSize(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void DecodeToBmp_Span_Empty_ThrowsArgumentException()
    {
        using var decoder = new TurboJpegDecoder();

        Assert.Throws<ArgumentException>(
            () => decoder.DecodeToBmp(ReadOnlySpan<byte>.Empty, TurboJpegDecodeOptions.Default, new byte[1]));
    }

    [Fact]
    public void GetBmpSize_AfterDispose_ThrowsObjectDisposedException()
    {
        var decoder = new TurboJpegDecoder();
        var jpeg = TestFixtures.Load("baseline-444.jpg");
        decoder.Dispose();

        Assert.Throws<ObjectDisposedException>(() => decoder.GetBmpSize(jpeg));
        Assert.Throws<ObjectDisposedException>(
            () => decoder.DecodeToBmp(jpeg, TurboJpegDecodeOptions.Default, new byte[1]));
    }

    [Fact]
    public void GetBmpSize_LosslessWithScaling_ThrowsArgumentException()
    {
        using var decoder = new TurboJpegDecoder();
        var options = new TurboJpegDecodeOptions
        {
            ScalingFactor = new TurboJpegScalingFactor(1, 2)
        };

        Assert.Throws<ArgumentException>(
            () => decoder.GetBmpSize(TestFixtures.Load("lossless.jpg"), options));
    }

    [Fact]
    public void GetBmpSize_NullOptions_ThrowsArgumentNullException()
    {
        using var decoder = new TurboJpegDecoder();
        var jpeg = TestFixtures.Load("baseline-444.jpg");

        Assert.Throws<ArgumentNullException>(() => decoder.GetBmpSize(jpeg, null!));
        Assert.Throws<ArgumentNullException>(
            () => decoder.DecodeToBmp(jpeg, null!, new byte[1]));
    }

    private static byte[] LoadJpeg(int exifOrientation) =>
        exifOrientation == 0
            ? TestFixtures.Load("baseline-444.jpg")
            : JpegPatcher.WithExifOrientation(TestFixtures.Load("baseline-444.jpg"), (ushort)exifOrientation);

    private static TurboJpegDecodeOptions CreateOptions(
        TurboJpegBmpBitDepth bitDepth,
        bool autoOrient,
        int numerator,
        int denominator) =>
        new()
        {
            ScalingFactor = new TurboJpegScalingFactor(numerator, denominator),
            BmpBitDepth = bitDepth,
            AutoOrient = autoOrient
        };
}
