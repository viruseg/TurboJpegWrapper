using TurboJpegWrapper.Decoder.Internal.Bmp;
using TurboJpegWrapper.Decoder.Internal.Exif;

namespace TurboJpegWrapper.Decoder.Tests;

public sealed class BmpOrientationWriterTests
{
    public static IEnumerable<object[]> Orientations()
    {
        yield return new object[] { 5, 2, 3, new byte[] { 1, 4, 2, 5, 3, 6 } };
        yield return new object[] { 6, 2, 3, new byte[] { 4, 1, 5, 2, 6, 3 } };
        yield return new object[] { 7, 2, 3, new byte[] { 6, 3, 5, 2, 4, 1 } };
        yield return new object[] { 8, 2, 3, new byte[] { 3, 6, 2, 5, 1, 4 } };
    }

    public static IEnumerable<object[]> AxesSwappedCases()
    {
        foreach (var orientation in new[] { 5, 6, 7, 8 })
        {
            foreach (var bytesPerPixel in new[] { 1, 3, 4 })
            {
                yield return [orientation, bytesPerPixel];
            }
        }
    }

    public static IEnumerable<object[]> PortableCases()
    {
        var sizes = new[] { (4, 7), (8, 7), (9, 5), (16, 7), (17, 3), (35, 23), (64, 5) };
        foreach (var orientation in new[] { 5, 6, 7, 8 })
        {
            foreach (var bytesPerPixel in new[] { 1, 3, 4 })
            {
                foreach (var (width, height) in sizes)
                {
                    yield return [orientation, bytesPerPixel, width, height];
                }
            }
        }
    }

    public static IEnumerable<object[]> Blocked8Sizes()
    {
        yield return [1, 1];
        yield return [2, 16];
        yield return [3, 17];
        yield return [4, 15];
        yield return [4, 16];
        yield return [5, 17];
        yield return [15, 16];
        yield return [16, 15];
        yield return [16, 16];
        yield return [17, 31];
        yield return [31, 17];
        yield return [35, 23];
        yield return [64, 16];
        yield return [65, 33];
    }

    [Theory]
    [MemberData(nameof(Blocked8Sizes))]
    public void Write8_MatchesReference_ForBlockedTransposes(int width, int height)
    {
        var source = new byte[width * height];
        for (var i = 0; i < source.Length; i++)
        {
            source[i] = (byte)((i * 31) + 7);
        }

        var destinationStride = (height + 3) & ~3;
        foreach (var orientationValue in new[] { 5, 6, 7, 8 })
        {
            var orientation = (ExifOrientation)orientationValue;
            var expected = new byte[destinationStride * width];
            WriteReference(
                expected, source, width, height, 1, destinationStride, orientation);

            var actual = new byte[expected.Length];
            BmpOrientationWriter.Write(
                actual, source, width, height, 1, destinationStride, orientation);

            Assert.Equal(expected, actual);
        }
    }

    [Theory]
    [MemberData(nameof(PortableCases))]
    public void WritePortable_MatchesReference(
        int orientationValue, int bytesPerPixel, int width, int height)
    {
        var source = new byte[width * height * bytesPerPixel];
        for (var i = 0; i < source.Length; i++)
        {
            source[i] = (byte)((i * 31) + 7);
        }

        var destinationStride = ((height * bytesPerPixel) + 3) & ~3;
        var expected = new byte[destinationStride * width];
        var orientation = (ExifOrientation)orientationValue;
        WriteReference(
            expected, source, width, height, bytesPerPixel, destinationStride, orientation);

        var actual = new byte[expected.Length];
        BmpOrientationWriter.WritePortable(
            actual, source, width, height, bytesPerPixel, destinationStride, orientation);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(4, 5)]
    [InlineData(8, 7)]
    [InlineData(12, 3)]
    [InlineData(16, 9)]
    [InlineData(32, 5)]
    [InlineData(64, 7)]
    public void WritePortable24_PaddedSource_MatchesReference(int width, int height)
    {
        var sourceByteCount = width * height * 3;
        var source = new byte[sourceByteCount + 4];
        for (var i = 0; i < sourceByteCount; i++)
        {
            source[i] = (byte)((i * 31) + 7);
        }

        var destinationStride = ((height * 3) + 3) & ~3;
        var expected = new byte[destinationStride * width];
        WriteReference(
            expected, source, width, height, 3, destinationStride, ExifOrientation.Rotate90);

        var actual = new byte[expected.Length];
        BmpOrientationWriter.WritePortable(
            actual, source, width, height, 3, destinationStride, ExifOrientation.Rotate90);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(4, 5, 0)]
    [InlineData(4, 5, 4)]
    [InlineData(8, 7, 0)]
    [InlineData(8, 7, 4)]
    [InlineData(12, 3, 0)]
    [InlineData(12, 3, 4)]
    [InlineData(16, 9, 0)]
    [InlineData(16, 9, 4)]
    [InlineData(32, 5, 0)]
    [InlineData(32, 5, 4)]
    [InlineData(64, 7, 0)]
    [InlineData(64, 7, 4)]
    public void Write24_PaddedAndUnpaddedSources_MatchReference(
        int width, int height, int padding)
    {
        var sourceByteCount = width * height * 3;
        var source = new byte[sourceByteCount + padding];
        for (var i = 0; i < sourceByteCount; i++)
        {
            source[i] = (byte)((i * 31) + 7);
        }

        var destinationStride = ((height * 3) + 3) & ~3;
        var expected = new byte[destinationStride * width];
        WriteReference(
            expected, source, width, height, 3, destinationStride, ExifOrientation.Rotate90);

        var actual = new byte[expected.Length];
        BmpOrientationWriter.Write(
            actual,
            source,
            width,
            height,
            3,
            destinationStride,
            ExifOrientation.Rotate90);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(AxesSwappedCases))]
    public void Write_MatchesReference_WithPaddingAndEdges(
        int orientationValue, int bytesPerPixel)
    {
        const int width = 35;
        const int height = 23;
        var source = new byte[width * height * bytesPerPixel];
        for (var i = 0; i < source.Length; i++)
        {
            source[i] = (byte)((i * 31) + 7);
        }

        var destinationStride = ((height * bytesPerPixel) + 3) & ~3;
        var expected = new byte[destinationStride * width];
        var orientation = (ExifOrientation)orientationValue;
        WriteReference(
            expected, source, width, height, bytesPerPixel, destinationStride, orientation);

        var actual = new byte[expected.Length];
        BmpOrientationWriter.Write(
            actual, source, width, height, bytesPerPixel, destinationStride, orientation);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(Orientations))]
    public void Write_MapsTopDownPixelsToBottomUpBmp(
        int orientationValue, int outputWidth, int outputHeight, byte[] expectedTopDown)
    {
        const int width = 3;
        const int height = 2;
        var source = new byte[] { 1, 2, 3, 4, 5, 6 };
        var stride = (outputWidth + 3) & ~3;
        var destination = new byte[stride * outputHeight];

        BmpOrientationWriter.Write(
            destination,
            source,
            width,
            height,
            1,
            stride,
            (ExifOrientation)orientationValue);

        for (var y = 0; y < outputHeight; y++)
        {
            for (var x = 0; x < outputWidth; x++)
            {
                Assert.Equal(
                    expectedTopDown[y * outputWidth + x],
                    destination[(outputHeight - 1 - y) * stride + x]);
            }
        }
    }

    [Fact]
    public void Write_Rotate90_CopiesAllPixelBytes()
    {
        const int width = 3;
        const int height = 2;
        const int bytesPerPixel = 3;
        var source = Enumerable.Range(0, width * height * bytesPerPixel).Select(i => (byte)i).ToArray();
        var stride = 8;
        var destination = new byte[stride * 3];

        BmpOrientationWriter.Write(
            destination, source, width, height, bytesPerPixel, stride, ExifOrientation.Rotate90);

        var expectedPixels = new[] { 4, 1, 5, 2, 6, 3 };
        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < 2; x++)
            {
                var expectedPixel = expectedPixels[y * 2 + x];
                var offset = (2 - y) * stride + x * bytesPerPixel;
                for (var i = 0; i < bytesPerPixel; i++)
                {
                    Assert.Equal(
                        (byte)((expectedPixel - 1) * bytesPerPixel + i),
                        destination[offset + i]);
                }
            }
        }
    }

    [Fact]
    public void Write_DoesNotTouchRowPadding()
    {
        const int width = 3;
        const int height = 2;
        const int bytesPerPixel = 3;
        const int stride = 8;
        var source = new byte[width * height * bytesPerPixel];
        var destination = new byte[stride * 3];
        Array.Fill(destination, (byte)0xEE);

        BmpOrientationWriter.Write(
            destination, source, width, height, bytesPerPixel, stride, ExifOrientation.Rotate270);

        for (var y = 0; y < 3; y++)
        {
            for (var i = 6; i < stride; i++)
            {
                Assert.Equal(0xEE, destination[y * stride + i]);
            }
        }
    }

    private static void WriteReference(
        byte[] destination,
        byte[] source,
        int width,
        int height,
        int bytesPerPixel,
        int destinationStride,
        ExifOrientation orientation)
    {
        var sourceStride = width * bytesPerPixel;
        var bottomUpRow = orientation is ExifOrientation.Transverse or ExifOrientation.Rotate270;
        var leftToRightColumn = orientation is ExifOrientation.Transpose or ExifOrientation.Rotate270;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var destinationRow = bottomUpRow ? x : width - 1 - x;
                var destinationColumn = leftToRightColumn ? y : height - 1 - y;
                Array.Copy(
                    source,
                    y * sourceStride + x * bytesPerPixel,
                    destination,
                    destinationRow * destinationStride + destinationColumn * bytesPerPixel,
                    bytesPerPixel);
            }
        }
    }
}
