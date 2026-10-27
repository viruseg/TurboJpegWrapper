using TurboJpegWrapper;

namespace TurboJpegWrapper.Decoder.Tests;

public sealed class TurboJpegCropTests
{
    [Theory]
    [InlineData(-1, 0, 1, 1)]
    [InlineData(0, -1, 1, 1)]
    [InlineData(0, 0, 0, 1)]
    [InlineData(0, 0, 1, 0)]
    public void Constructor_InvalidRegion_ThrowsArgumentOutOfRangeException(int x, int y, int w, int h)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TurboJpegCrop(x, y, w, h));
    }

    [Fact]
    public void Constructor_ValidRegion_ExposesComponents()
    {
        var crop = new TurboJpegCrop(3, 4, 5, 6);
        Assert.Equal((3, 4, 5, 6), (crop.X, crop.Y, crop.Width, crop.Height));
    }
}
