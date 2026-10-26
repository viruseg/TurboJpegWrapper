using System.Globalization;
using TurboJpegWrapper.Encoder;
using TurboJpegWrapper.Encoder.Internal;

namespace TurboJpegWrapper.Encoder.Tests;

public sealed class BufferApiTests
{
    private static readonly (byte B, byte G, byte R)[] Palette8 =
        Enumerable.Range(0, 256)
            .Select(i => ((byte)i, (byte)(255 - i), (byte)((i * 3) & 0xFF)))
            .ToArray();

    public static TheoryData<string> Formats { get; } =
        new() { "24-bit", "32-bit", "8-bit palette", "16-bit", "1-bit" };

    [Theory]
    [MemberData(nameof(Formats))]
    public void GetMaxJpegSize_IsAtLeastEncodedLength(string format)
    {
        var bmp = CreateBmp(format);
        using var encoder = new TurboJpegEncoder();

        var maxSize = encoder.GetMaxJpegSize(bmp);
        var encoded = encoder.EncodeToJpeg(bmp);

        Assert.True(
            maxSize >= encoded.Length,
            $"{format}: the maximum size {maxSize} is smaller than the encoded {encoded.Length} bytes.");
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void EncodeToJpeg_ExactMaxDestination_WritesByteIdenticalJpeg(string format)
    {
        var bmp = CreateBmp(format);
        using var encoder = new TurboJpegEncoder();
        var expected = encoder.EncodeToJpeg(bmp);
        var destination = new byte[encoder.GetMaxJpegSize(bmp)];

        var written = encoder.EncodeToJpeg(bmp, TurboJpegEncodeOptions.Default, destination);

        Assert.Equal(expected.Length, written);
        Assert.True(destination.AsSpan(0, written).SequenceEqual(expected));
    }

    [Fact]
    public void GetMaxJpegSize_WithOptions_IsAtLeastEncodedLength()
    {
        var bmp = BmpBuilder.Create24(37, 29, Gradient);
        var options = new TurboJpegEncodeOptions
        {
            Quality = 95,
            Subsampling = TurboJpegSubsampling.Yuv444,
        };
        using var encoder = new TurboJpegEncoder();

        var maxSize = encoder.GetMaxJpegSize(bmp, options);
        var encoded = encoder.EncodeToJpeg(bmp, options);

        Assert.True(maxSize >= encoded.Length);
    }

    [Fact]
    public void GetMaxJpegSize_GrayscaleColorspace_UsesGrayscaleSizing()
    {
        var bmp = BmpBuilder.Create24(37, 29, Gradient);
        var options = new TurboJpegEncodeOptions { Colorspace = TurboJpegColorspace.Grayscale };
        using var encoder = new TurboJpegEncoder();

        var maxSize = encoder.GetMaxJpegSize(bmp, options);
        var encoded = encoder.EncodeToJpeg(bmp, options);

        Assert.True(maxSize >= encoded.Length);
    }

    [Fact]
    public void EncodeToJpeg_DestinationOneByteShort_ThrowsArgumentExceptionNamingRequiredSize()
    {
        var bmp = BmpBuilder.Create24(23, 17, Gradient);
        using var encoder = new TurboJpegEncoder();
        var maxSize = encoder.GetMaxJpegSize(bmp);
        var destination = new byte[maxSize - 1];

        var exception = Assert.Throws<ArgumentException>(
            () => encoder.EncodeToJpeg(bmp, TurboJpegEncodeOptions.Default, destination));

        Assert.Equal("destination", exception.ParamName);
        Assert.Contains(
            maxSize.ToString(CultureInfo.InvariantCulture),
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void GetMaxJpegSize_EmptyBmp_ThrowsArgumentException()
    {
        using var encoder = new TurboJpegEncoder();

        var exception = Assert.Throws<ArgumentException>(
            () => encoder.GetMaxJpegSize(ReadOnlySpan<byte>.Empty));

        Assert.Equal("bmp", exception.ParamName);
    }

    [Fact]
    public void GetMaxJpegSize_NullOptions_ThrowsArgumentNullException()
    {
        var bmp = BmpBuilder.Create24(2, 2, Gradient);
        using var encoder = new TurboJpegEncoder();

        Assert.Throws<ArgumentNullException>(() => encoder.GetMaxJpegSize(bmp, null!));
    }

    [Fact]
    public void GetMaxJpegSize_DisposedEncoder_ThrowsObjectDisposedException()
    {
        var bmp = BmpBuilder.Create24(2, 2, Gradient);
        var encoder = new TurboJpegEncoder();

        encoder.Dispose();

        Assert.Throws<ObjectDisposedException>(() => encoder.GetMaxJpegSize(bmp));
    }

    [Fact]
    public void EncodeToJpeg_DestinationEmptyBmp_ThrowsArgumentException()
    {
        using var encoder = new TurboJpegEncoder();

        Assert.Throws<ArgumentException>(() =>
            encoder.EncodeToJpeg(
                ReadOnlySpan<byte>.Empty, TurboJpegEncodeOptions.Default, new byte[1024]));
    }

    [Fact]
    public void EncodeToJpeg_DestinationNullOptions_ThrowsArgumentNullException()
    {
        var bmp = BmpBuilder.Create24(2, 2, Gradient);
        using var encoder = new TurboJpegEncoder();

        Assert.Throws<ArgumentNullException>(() =>
            encoder.EncodeToJpeg(bmp, null!, new byte[1024]));
    }

    [Fact]
    public void EncodeToJpeg_DestinationDisposedEncoder_ThrowsObjectDisposedException()
    {
        var bmp = BmpBuilder.Create24(2, 2, Gradient);
        var encoder = new TurboJpegEncoder();

        encoder.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            encoder.EncodeToJpeg(bmp, TurboJpegEncodeOptions.Default, new byte[1024]));
    }

    [Fact]
    public void SizeCalculator_HugeDimensions_ExceedArrayLimit_ThrowsWithoutAllocating()
    {
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();

        // libjpeg-turbo reports zero when the dimensions overflow its 32-bit size
        // computation, so use the largest area that still yields a nonzero result
        // above Array.MaxLength. Verified: 30000x30000 -> 2,700,002,048 bytes.
        var exception = Assert.Throws<TurboJpegImageTooLargeException>(
            () => SizeCalculator.GetMaxJpegSize(
                30_000, 30_000, (int)TurboJpegSubsampling.Yuv420));

        Assert.Contains("maximum array length", exception.Message, StringComparison.Ordinal);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Assert.True(
            allocated < 1024 * 1024,
            $"GetMaxJpegSize allocated {allocated} bytes while rejecting huge dimensions.");
    }

    [Fact]
    public void SizeCalculator_InvalidSubsampling_ThrowsImageTooLargeException()
    {
        var exception = Assert.Throws<TurboJpegImageTooLargeException>(
            () => SizeCalculator.GetMaxJpegSize(1024, 1024, 420));

        Assert.Contains("zero worst-case JPEG size", exception.Message, StringComparison.Ordinal);
    }

    private static (byte B, byte G, byte R) Gradient(int x, int y) =>
        ((byte)(x * 7 + y * 3), (byte)(x * 3 + y * 11), (byte)(x * 5 + y * 13));

    private static byte[] CreateBmp(string format) => format switch
    {
        "24-bit" => BmpBuilder.Create24(23, 17, Gradient),
        "32-bit" => BmpBuilder.Create32(23, 17, (x, y) =>
        {
            var (b, g, r) = Gradient(x, y);
            return (b, g, r, (byte)(x * 5 + y * 9));
        }),
        "8-bit palette" => BmpBuilder.CreatePalette8(
            23, 17, (x, y) => (byte)(x * 5 + y * 3), Palette8),
        "16-bit" => BmpBuilder.Create16(
            23,
            17,
            (x, y) =>
            {
                var red = (x * 11 + y * 7) & 0x1F;
                var green = (x * 5 + y * 3) & 0x1F;
                var blue = (x * 7 + y * 11) & 0x1F;
                return (ushort)(red << 10 | green << 5 | blue);
            },
            redMask: 0x7C00,
            greenMask: 0x03E0,
            blueMask: 0x001F,
            bitfields: false),
        "1-bit" => BmpBuilder.CreatePalette1(
            23,
            17,
            (x, y) => (byte)((x ^ y) & 1),
            new[] { ((byte)0, (byte)0, (byte)0), ((byte)255, (byte)255, (byte)255) }),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
    };
}
