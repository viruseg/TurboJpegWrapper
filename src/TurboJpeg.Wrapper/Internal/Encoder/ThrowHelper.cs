using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TurboJpegWrapper.Internal.Encoder;

[StackTraceHidden]
internal static class ThrowHelper
{
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpEmpty() =>
        throw new ArgumentException("BMP data must not be empty.", "bmp");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowDisposed() =>
        throw new ObjectDisposedException("TurboJpegEncoder");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowDestinationTooSmall(int requiredBytes) =>
        throw new ArgumentException(
            $"The destination must be at least {requiredBytes} bytes long.", "destination");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowDestinationOverlapsSource() =>
        throw new ArgumentException(
            "The destination must not overlap the BMP source buffer.", "destination");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowJpegSizeZero(int width, int height) =>
        throw new TurboJpegImageTooLargeException(
            $"TurboJPEG reported a zero worst-case JPEG size for a {width}x{height} image.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowJpegTooLarge(int width, int height, nuint size) =>
        throw new TurboJpegImageTooLargeException(
            $"The worst-case JPEG size for a {width}x{height} image is {size} bytes, " +
            $"which exceeds the maximum array length of {Array.MaxLength} bytes.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowRestartOptionsConflict() =>
        throw new ArgumentException(
            "RestartBlocks and RestartRows cannot both be non-zero.", "options");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowLosslessRestartBlocks() =>
        throw new ArgumentException(
            "RestartBlocks must be zero when lossless compression is enabled.", "options");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowDensityWithLossless() =>
        throw new ArgumentException(
            "Density cannot be specified for lossless JPEG output, which does not store density.",
            "options");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowDensityWithRgb() =>
        throw new ArgumentException(
            "Density cannot be specified for RGB JPEG output; only YCbCr and grayscale JPEG files store density.",
            "options");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpFileHeaderTooShort() =>
        throw new ArgumentException(
            "The BMP buffer is too short to contain a BMP file header and the DIB header size.",
            "bmp");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpSignatureMissing() =>
        throw new ArgumentException("The BMP signature is missing.", "bmp");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpDibHeaderSizeUnsupported(uint dibSize) =>
        throw new TurboJpegUnsupportedException(
            dibSize == 12
                ? "OS/2 core BMPs (DIB header size 12) are not supported."
                : $"BMP DIB header size {dibSize} is not supported.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpDibHeaderTooShort() =>
        throw new ArgumentException(
            "The BMP buffer is too short to contain the DIB header.", "bmp");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpWidthNotPositive() =>
        throw new ArgumentException("The BMP width must be positive.", "bmp");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpHeightInvalid() =>
        throw new ArgumentException("The BMP height is invalid.", "bmp");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpPlaneCountInvalid(ushort planes) =>
        throw new ArgumentException(
            $"The BMP bit planes value {planes} is invalid; exactly one plane is required.",
            "bmp");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpBitCountUnsupported(ushort bitCount) =>
        throw new ArgumentException(
            $"The BMP bit count {bitCount} is not supported.", "bmp");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpBitfieldsBitCountUnsupported() =>
        throw new TurboJpegUnsupportedException(
            "BI_BITFIELDS is supported only for 16-bit and 32-bit BMPs.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpRle8Unsupported() =>
        throw new TurboJpegUnsupportedException("RLE8-compressed BMPs are not supported.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpRle4Unsupported() =>
        throw new TurboJpegUnsupportedException("RLE4-compressed BMPs are not supported.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpJpegCompressionUnsupported() =>
        throw new TurboJpegUnsupportedException("JPEG-compressed BMPs are not supported.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpPngCompressionUnsupported() =>
        throw new TurboJpegUnsupportedException("PNG-compressed BMPs are not supported.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpCompressionUnsupported(uint compression) =>
        throw new TurboJpegUnsupportedException(
            $"BMP compression {compression} is not supported.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpPixelOffsetOverlapsHeader() =>
        throw new ArgumentException(
            "The BMP pixel offset overlaps the file header, DIB header, or bitfield masks.",
            "bmp");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpPixelOffsetOutsideBuffer() =>
        throw new ArgumentException("The BMP pixel offset is outside the buffer.", "bmp");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpPaletteTooLarge() =>
        throw new ArgumentException("The BMP palette entry count is too large.", "bmp");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpPaletteOverlapsPixels() =>
        throw new ArgumentException(
            "The BMP palette overlaps or extends past the pixel data.", "bmp");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpBitfieldMasksOverlap() =>
        throw new TurboJpegUnsupportedException("BMP bitfield masks must not overlap.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpRowStrideTooLarge() =>
        throw new ArgumentException("The BMP row stride is too large.", "bmp");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpPixelDataTruncated() =>
        throw new ArgumentException("The BMP pixel data is truncated.", "bmp");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpBitfieldMaskEmpty(string channel) =>
        throw new TurboJpegUnsupportedException(
            $"The BMP {channel} bitfield mask is empty.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpBitfieldMaskNotContiguous(string channel) =>
        throw new TurboJpegUnsupportedException(
            $"The BMP {channel} bitfield mask is not contiguous.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowBmpPixelsTooLarge(int width, int height, int bitCount) =>
        throw new TurboJpegImageTooLargeException(
            $"The converted pixel buffer for a {width}x{height} " +
            $"{bitCount}-bit BMP exceeds the maximum array length of " +
            $"{Array.MaxLength} bytes.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowSubsamplingOutOfRange(TurboJpegSubsampling value) =>
        throw new ArgumentOutOfRangeException(
            nameof(TurboJpegEncodeOptions.Subsampling),
            value,
            "The subsampling must be a defined value other than Unknown.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowColorspaceOutOfRange(TurboJpegColorspace value) =>
        throw new ArgumentOutOfRangeException(
            nameof(TurboJpegEncodeOptions.Colorspace),
            value,
            "The color space must be Default, Rgb, YCbCr, or Grayscale.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Throw(Exception exception) =>
        throw exception;
}
