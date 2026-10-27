using System.Buffers.Binary;
using TurboJpegWrapper.Internal.Decoder.Exif;

namespace TurboJpegWrapper.Decoder.Tests;

public sealed class ExifOrientationReaderTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void Read_ReturnsOrientationFromLittleEndianTiff(ushort orientation)
    {
        var jpeg = JpegPatcher.WithExifOrientation(TestFixtures.Load("baseline-444.jpg"), orientation);

        Assert.Equal((ExifOrientation)orientation, ExifOrientationReader.Read(jpeg));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(8)]
    public void Read_ReturnsOrientationFromBigEndianTiff(ushort orientation)
    {
        var jpeg = JpegPatcher.WithExifOrientation(
            TestFixtures.Load("baseline-444.jpg"), orientation, bigEndian: true);

        Assert.Equal((ExifOrientation)orientation, ExifOrientationReader.Read(jpeg));
    }

    [Fact]
    public void Read_WithoutExif_ReturnsNormal()
    {
        var jpeg = TestFixtures.Load("baseline-444.jpg");

        Assert.Equal(ExifOrientation.Normal, ExifOrientationReader.Read(jpeg));
    }

    [Fact]
    public void Read_WithXmpBeforeExif_ReturnsExifOrientation()
    {
        var fixture = TestFixtures.Load("baseline-444.jpg");
        var xmp = "http://ns.adobe.com/xap/1.0/\0<x:xmpmeta/>"u8.ToArray();

        var jpeg = JpegPatcher.WithExifOrientation(
            JpegPatcher.WithSegment(fixture, 0xE1, xmp), 6);

        Assert.Equal(ExifOrientation.Rotate90, ExifOrientationReader.Read(jpeg));
    }

    [Fact]
    public void Read_ExifWithoutOrientationTag_ReturnsNormal()
    {
        var payload = JpegPatcher.CreateExifPayload(6, tag: 0x010F);
        var jpeg = JpegPatcher.WithSegment(
            TestFixtures.Load("baseline-444.jpg"), 0xE1, payload);

        Assert.Equal(ExifOrientation.Normal, ExifOrientationReader.Read(jpeg));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(65535)]
    public void Read_OutOfRangeOrientation_ReturnsNormal(ushort orientation)
    {
        var jpeg = JpegPatcher.WithExifOrientation(
            TestFixtures.Load("baseline-444.jpg"), orientation);

        Assert.Equal(ExifOrientation.Normal, ExifOrientationReader.Read(jpeg));
    }

    [Fact]
    public void Read_OrientationWithWrongType_ReturnsNormal()
    {
        var payload = JpegPatcher.CreateExifPayload(6);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(6 + 12, 2), 4);

        var jpeg = JpegPatcher.WithSegment(
            TestFixtures.Load("baseline-444.jpg"), 0xE1, payload);

        Assert.Equal(ExifOrientation.Normal, ExifOrientationReader.Read(jpeg));
    }

    [Fact]
    public void Read_TruncatedExifPayload_ReturnsNormal()
    {
        var jpeg = JpegPatcher.WithSegment(
            TestFixtures.Load("baseline-444.jpg"), 0xE1, "Exif\0\0"u8);

        Assert.Equal(ExifOrientation.Normal, ExifOrientationReader.Read(jpeg));
    }

    [Fact]
    public void Read_TruncatedTiffHeader_ReturnsNormal()
    {
        var payload = JpegPatcher.CreateExifPayload(6);
        var jpeg = JpegPatcher.WithSegment(
            TestFixtures.Load("baseline-444.jpg"), 0xE1, payload.AsSpan(0, 10));

        Assert.Equal(ExifOrientation.Normal, ExifOrientationReader.Read(jpeg));
    }

    [Fact]
    public void Read_OffsetBeyondPayload_ReturnsNormal()
    {
        var payload = JpegPatcher.CreateExifPayload(6);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(6 + 4, 4), 0xFFFF_FF00u);

        var jpeg = JpegPatcher.WithSegment(
            TestFixtures.Load("baseline-444.jpg"), 0xE1, payload);

        Assert.Equal(ExifOrientation.Normal, ExifOrientationReader.Read(jpeg));
    }

    [Fact]
    public void Read_IfdOffsetInsideTiffHeader_ReturnsNormal()
    {
        var jpeg = JpegPatcher.WithSegment(
            TestFixtures.Load("baseline-444.jpg"),
            0xE1,
            JpegPatcher.CreateExifPayloadWithIfdInsideHeader());

        Assert.Equal(ExifOrientation.Normal, ExifOrientationReader.Read(jpeg));
    }

    [Fact]
    public void Read_InvalidIfdEntryCount_ReturnsNormal()
    {
        var payload = JpegPatcher.CreateExifPayload(6);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(6 + 8, 2), ushort.MaxValue);

        var jpeg = JpegPatcher.WithSegment(
            TestFixtures.Load("baseline-444.jpg"), 0xE1, payload);

        Assert.Equal(ExifOrientation.Normal, ExifOrientationReader.Read(jpeg));
    }

    [Fact]
    public void Read_NotAJpeg_ReturnsNormal()
    {
        Assert.Equal(ExifOrientation.Normal, ExifOrientationReader.Read(new byte[] { 1, 2, 3, 4 }));
        Assert.Equal(ExifOrientation.Normal, ExifOrientationReader.Read(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void Read_TruncatedJpeg_ReturnsNormal()
    {
        var jpeg = JpegPatcher.WithExifOrientation(TestFixtures.Load("baseline-444.jpg"), 6);

        Assert.Equal(ExifOrientation.Normal, ExifOrientationReader.Read(jpeg.AsSpan(0, 20)));
    }
}
