using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using TurboJpegWrapper.Internal.Decoder;
using TurboJpegWrapper.Internal.Decoder.Exif;

namespace TurboJpegWrapper.Internal.Decoder.Bmp;

internal static class BmpOrientationWriter
{
    private const int TileSize = 32;
    private const byte Transpose4x4Shuffle = 0xD8;
    private static readonly Vector128<byte> s_transpose4x4 = Vector128.Create(
        (byte)0, 4, 8, 12,
        1, 5, 9, 13,
        2, 6, 10, 14,
        3, 7, 11, 15);
    private static readonly Vector128<byte> s_mirrorLeftToRight = Vector128.Create(
        (byte)9, 10, 11,
        6, 7, 8,
        3, 4, 5,
        0, 1, 2,
        12, 13, 14, 15);
    private static readonly Vector128<byte> s_mirrorRightToLeft = Vector128.Create(
        (byte)13, 14, 15,
        10, 11, 12,
        7, 8, 9,
        4, 5, 6,
        0, 1, 2, 3);
    private static readonly Vector128<uint> s_unpackLowIndices = Vector128.Create(0u, 0u, 1u, 1u);
    private static readonly Vector128<uint> s_unpackHighIndices = Vector128.Create(2u, 2u, 3u, 3u);
    private static readonly Vector128<uint> s_unpackSelect = Vector128.Create(
        uint.MaxValue, 0u, uint.MaxValue, 0u);
    private static readonly Vector128<uint> s_transposeLaneOrder = Vector128.Create(0u, 2u, 1u, 3u);

    private static Vector128<uint> UnpackLow(Vector128<uint> first, Vector128<uint> second) =>
        Vector128.ConditionalSelect(
            s_unpackSelect,
            Vector128.Shuffle(first, s_unpackLowIndices),
            Vector128.Shuffle(second, s_unpackLowIndices));

    private static Vector128<uint> UnpackHigh(Vector128<uint> first, Vector128<uint> second) =>
        Vector128.ConditionalSelect(
            s_unpackSelect,
            Vector128.Shuffle(first, s_unpackHighIndices),
            Vector128.Shuffle(second, s_unpackHighIndices));

    private static void Transpose4x4(
        Vector128<uint> r0,
        Vector128<uint> r1,
        Vector128<uint> r2,
        Vector128<uint> r3,
        out Vector128<uint> c0,
        out Vector128<uint> c1,
        out Vector128<uint> c2,
        out Vector128<uint> c3)
    {
        var t0 = UnpackLow(r0, r1);
        var t1 = UnpackHigh(r0, r1);
        var t2 = UnpackLow(r2, r3);
        var t3 = UnpackHigh(r2, r3);

        c0 = Vector128.Shuffle(UnpackLow(t0, t2), s_transposeLaneOrder);
        c1 = Vector128.Shuffle(UnpackHigh(t0, t2), s_transposeLaneOrder);
        c2 = Vector128.Shuffle(UnpackLow(t1, t3), s_transposeLaneOrder);
        c3 = Vector128.Shuffle(UnpackHigh(t1, t3), s_transposeLaneOrder);
    }

    internal static void Write(
        Span<byte> destination,
        ReadOnlySpan<byte> source,
        int width,
        int height,
        int bytesPerPixel,
        int destinationStride,
        ExifOrientation orientation)
    {
        ValidateOrientation(orientation);

        var bottomUpRow = orientation is ExifOrientation.Transverse or ExifOrientation.Rotate270;
        var leftToRightColumn = orientation is ExifOrientation.Transpose or ExifOrientation.Rotate270;

        switch (bytesPerPixel)
        {
            case 1:
                if (Vector128.IsHardwareAccelerated)
                {
                    WriteBlocked8(
                        destination,
                        source,
                        width,
                        height,
                        destinationStride,
                        bottomUpRow,
                        leftToRightColumn);
                }
                else
                {
                    WriteTiled8(
                        destination,
                        source,
                        width,
                        height,
                        destinationStride,
                        bottomUpRow,
                        leftToRightColumn);
                }

                break;

            case 3:
                if (Sse2.IsSupported)
                {
                    WriteSimd24(
                        destination,
                        source,
                        width,
                        height,
                        destinationStride,
                        bottomUpRow,
                        leftToRightColumn);
                }
                else if (Vector128.IsHardwareAccelerated)
                {
                    WriteVector24(
                        destination,
                        source,
                        width,
                        height,
                        destinationStride,
                        bottomUpRow,
                        leftToRightColumn);
                }
                else
                {
                    WriteTiled24(
                        destination,
                        source,
                        width,
                        height,
                        destinationStride,
                        bottomUpRow,
                        leftToRightColumn);
                }

                break;

            case 4:
                if (Sse2.IsSupported)
                {
                    WriteSimd32(
                        destination,
                        source,
                        width,
                        height,
                        destinationStride,
                        bottomUpRow,
                        leftToRightColumn);
                }
                else if (Vector128.IsHardwareAccelerated)
                {
                    WriteVector32(
                        destination,
                        source,
                        width,
                        height,
                        destinationStride,
                        bottomUpRow,
                        leftToRightColumn);
                }
                else
                {
                    WriteTiled32(
                        destination,
                        source,
                        width,
                        height,
                        destinationStride,
                        bottomUpRow,
                        leftToRightColumn);
                }

                break;

            default:
                ThrowHelper.ThrowBytesPerPixelOutOfRange(bytesPerPixel);
                break;
        }
    }

    private static void ValidateOrientation(ExifOrientation orientation)
    {
        if (orientation is not (ExifOrientation.Transpose
            or ExifOrientation.Rotate90
            or ExifOrientation.Transverse
            or ExifOrientation.Rotate270))
        {
            ThrowHelper.ThrowOrientationOutOfRange(orientation);
        }
    }

    internal static void WritePortable(
        Span<byte> destination,
        ReadOnlySpan<byte> source,
        int width,
        int height,
        int bytesPerPixel,
        int destinationStride,
        ExifOrientation orientation)
    {
        ValidateOrientation(orientation);

        var bottomUpRow = orientation is ExifOrientation.Transverse or ExifOrientation.Rotate270;
        var leftToRightColumn = orientation is ExifOrientation.Transpose or ExifOrientation.Rotate270;

        switch (bytesPerPixel)
        {
            case 1:
                WriteBlocked8(
                    destination,
                    source,
                    width,
                    height,
                    destinationStride,
                    bottomUpRow,
                    leftToRightColumn);
                break;
            case 3:
                WriteVector24(
                    destination,
                    source,
                    width,
                    height,
                    destinationStride,
                    bottomUpRow,
                    leftToRightColumn);
                break;
            case 4:
                WriteVector32(
                    destination,
                    source,
                    width,
                    height,
                    destinationStride,
                    bottomUpRow,
                    leftToRightColumn);
                break;
            default:
                ThrowHelper.ThrowBytesPerPixelOutOfRange(bytesPerPixel);
                break;
        }
    }

    internal static void MirrorRowsInPlace(
        Span<byte> pixels, int width, int height, int stride, int bytesPerPixel)
    {
        var rowBytes = width * bytesPerPixel;

        switch (bytesPerPixel)
        {
            case 1:
                for (var y = 0; y < height; y++)
                {
                    pixels.Slice(y * stride, rowBytes).Reverse();
                }

                break;

            case 3:
                if (rowBytes >= 16 && Vector128.IsHardwareAccelerated)
                {
                    Mirror24RowsBuffered(pixels, width, height, stride);
                }
                else
                {
                    for (var y = 0; y < height; y++)
                    {
                        Reverse24(pixels.Slice(y * stride, rowBytes));
                    }
                }

                break;

            case 4:
                for (var y = 0; y < height; y++)
                {
                    MemoryMarshal.Cast<byte, uint>(pixels.Slice(y * stride, rowBytes)).Reverse();
                }

                break;

            default:
                ThrowHelper.ThrowBytesPerPixelOutOfRange(bytesPerPixel);
                break;
        }
    }

    private static void WriteTiled8(
        Span<byte> destination,
        ReadOnlySpan<byte> source,
        int width,
        int height,
        int destinationStride,
        bool bottomUpRow,
        bool leftToRightColumn)
    {
        ref var sourceRef = ref MemoryMarshal.GetReference(source);
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        var sourceStride = width;

        for (var y0 = 0; y0 < height; y0 += TileSize)
        {
            var yEnd = Math.Min(y0 + TileSize, height);
            for (var x0 = 0; x0 < width; x0 += TileSize)
            {
                var xEnd = Math.Min(x0 + TileSize, width);
                for (var x = x0; x < xEnd; x++)
                {
                    var destinationRow = bottomUpRow ? x : width - 1 - x;
                    ref var destinationRowRef = ref Unsafe.Add(
                        ref destinationRef, destinationRow * destinationStride);

                    for (var y = y0; y < yEnd; y++)
                    {
                        var destinationColumn = leftToRightColumn ? y : height - 1 - y;
                        Unsafe.Add(ref destinationRowRef, destinationColumn) =
                            Unsafe.Add(ref sourceRef, y * sourceStride + x);
                    }
                }
            }
        }
    }

    private static void WriteTiled24(
        Span<byte> destination,
        ReadOnlySpan<byte> source,
        int width,
        int height,
        int destinationStride,
        bool bottomUpRow,
        bool leftToRightColumn)
    {
        ref var sourceRef = ref MemoryMarshal.GetReference(source);
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        var sourceStride = width * 3;

        for (var y0 = 0; y0 < height; y0 += TileSize)
        {
            var yEnd = Math.Min(y0 + TileSize, height);
            for (var x0 = 0; x0 < width; x0 += TileSize)
            {
                var xEnd = Math.Min(x0 + TileSize, width);
                for (var x = x0; x < xEnd; x++)
                {
                    var destinationRow = bottomUpRow ? x : width - 1 - x;
                    ref var destinationRowRef = ref Unsafe.Add(
                        ref destinationRef, destinationRow * destinationStride);

                    for (var y = y0; y < yEnd; y++)
                    {
                        var destinationColumn = leftToRightColumn ? y : height - 1 - y;
                        ref var sourcePixel = ref Unsafe.Add(
                            ref sourceRef, y * sourceStride + x * 3);
                        ref var destinationPixel = ref Unsafe.Add(
                            ref destinationRowRef, destinationColumn * 3);

                        destinationPixel = sourcePixel;
                        Unsafe.Add(ref destinationPixel, 1) = Unsafe.Add(ref sourcePixel, 1);
                        Unsafe.Add(ref destinationPixel, 2) = Unsafe.Add(ref sourcePixel, 2);
                    }
                }
            }
        }
    }

    private static void WriteTiled32(
        Span<byte> destination,
        ReadOnlySpan<byte> source,
        int width,
        int height,
        int destinationStride,
        bool bottomUpRow,
        bool leftToRightColumn)
    {
        ref var sourceRef = ref MemoryMarshal.GetReference(source);
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        var sourceStride = width * 4;

        for (var y0 = 0; y0 < height; y0 += TileSize)
        {
            var yEnd = Math.Min(y0 + TileSize, height);
            for (var x0 = 0; x0 < width; x0 += TileSize)
            {
                var xEnd = Math.Min(x0 + TileSize, width);
                for (var x = x0; x < xEnd; x++)
                {
                    var destinationRow = bottomUpRow ? x : width - 1 - x;
                    ref var destinationRowRef = ref Unsafe.Add(
                        ref destinationRef, destinationRow * destinationStride);

                    for (var y = y0; y < yEnd; y++)
                    {
                        var destinationColumn = leftToRightColumn ? y : height - 1 - y;
                        Unsafe.WriteUnaligned(
                            ref Unsafe.Add(ref destinationRowRef, destinationColumn * 4),
                            Unsafe.ReadUnaligned<uint>(
                                ref Unsafe.Add(ref sourceRef, y * sourceStride + x * 4)));
                    }
                }
            }
        }
    }

    private static void WriteSimd24(
        Span<byte> destination,
        ReadOnlySpan<byte> source,
        int width,
        int height,
        int destinationStride,
        bool bottomUpRow,
        bool leftToRightColumn)
    {
        ref var sourceRef = ref MemoryMarshal.GetReference(source);
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        var sourceStride = width * 3;
        // The vector load reads one byte past the last pixel; use the padded fast path when possible.
        var fullWidth = source.Length >= (long)width * height * 3 + 1
            ? width & ~3
            : (width - 1) & ~3;
        var fullHeight = height & ~3;

        for (var y0 = 0; y0 < fullHeight; y0 += 4)
        {
            for (var x0 = 0; x0 < fullWidth; x0 += 4)
            {
                var firstY = leftToRightColumn ? y0 : y0 + 3;
                var yStep = leftToRightColumn ? 1 : -1;

                var r0 = LoadRow24(ref sourceRef, firstY * sourceStride + x0 * 3);
                var r1 = LoadRow24(ref sourceRef, (firstY + yStep) * sourceStride + x0 * 3);
                var r2 = LoadRow24(ref sourceRef, (firstY + 2 * yStep) * sourceStride + x0 * 3);
                var r3 = LoadRow24(ref sourceRef, (firstY + 3 * yStep) * sourceStride + x0 * 3);

                var t0 = Sse2.UnpackLow(r0, r1);
                var t1 = Sse2.UnpackHigh(r0, r1);
                var t2 = Sse2.UnpackLow(r2, r3);
                var t3 = Sse2.UnpackHigh(r2, r3);

                var c0 = Sse2.Shuffle(Sse2.UnpackLow(t0, t2).AsInt32(), Transpose4x4Shuffle).AsUInt32();
                var c1 = Sse2.Shuffle(Sse2.UnpackHigh(t0, t2).AsInt32(), Transpose4x4Shuffle).AsUInt32();
                var c2 = Sse2.Shuffle(Sse2.UnpackLow(t1, t3).AsInt32(), Transpose4x4Shuffle).AsUInt32();
                var c3 = Sse2.Shuffle(Sse2.UnpackHigh(t1, t3).AsInt32(), Transpose4x4Shuffle).AsUInt32();

                var destinationColumn = leftToRightColumn ? y0 : height - 4 - y0;
                StoreColumn24(
                    ref destinationRef, c0, width, bottomUpRow, destinationColumn,
                    destinationStride, x0);
                StoreColumn24(
                    ref destinationRef, c1, width, bottomUpRow, destinationColumn,
                    destinationStride, x0 + 1);
                StoreColumn24(
                    ref destinationRef, c2, width, bottomUpRow, destinationColumn,
                    destinationStride, x0 + 2);
                StoreColumn24(
                    ref destinationRef, c3, width, bottomUpRow, destinationColumn,
                    destinationStride, x0 + 3);
            }
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = fullWidth; x < width; x++)
            {
                WritePixel24(
                    ref destinationRef,
                    ref sourceRef,
                    width,
                    height,
                    sourceStride,
                    destinationStride,
                    bottomUpRow,
                    leftToRightColumn,
                    x,
                    y);
            }
        }

        for (var y = fullHeight; y < height; y++)
        {
            for (var x = 0; x < fullWidth; x++)
            {
                WritePixel24(
                    ref destinationRef,
                    ref sourceRef,
                    width,
                    height,
                    sourceStride,
                    destinationStride,
                    bottomUpRow,
                    leftToRightColumn,
                    x,
                    y);
            }
        }
    }

    private static Vector128<uint> LoadRow24(ref byte sourceRef, int offset)
    {
        var a = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref sourceRef, offset));
        var b = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref sourceRef, offset + 3));
        var c = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref sourceRef, offset + 6));
        var d = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref sourceRef, offset + 9));
        return Vector128.Create(a, b, c, d);
    }

    private static void StoreColumn24(
        ref byte destinationRef,
        Vector128<uint> column,
        int width,
        bool bottomUpRow,
        int destinationColumn,
        int destinationStride,
        int x)
    {
        var destinationRow = bottomUpRow ? x : width - 1 - x;
        ref var destination = ref Unsafe.Add(
            ref destinationRef,
            destinationRow * destinationStride + destinationColumn * 3);

        Unsafe.WriteUnaligned(ref destination, column.GetElement(0));
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 3), column.GetElement(1));
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 6), column.GetElement(2));

        var last = column.GetElement(3);
        Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 9), (ushort)last);
        Unsafe.Add(ref destination, 11) = (byte)(last >> 16);
    }

    private static void WritePixel24(
        ref byte destinationRef,
        ref byte sourceRef,
        int width,
        int height,
        int sourceStride,
        int destinationStride,
        bool bottomUpRow,
        bool leftToRightColumn,
        int x,
        int y)
    {
        var destinationRow = bottomUpRow ? x : width - 1 - x;
        var destinationColumn = leftToRightColumn ? y : height - 1 - y;
        ref var destination = ref Unsafe.Add(
            ref destinationRef,
            destinationRow * destinationStride + destinationColumn * 3);
        ref var source = ref Unsafe.Add(ref sourceRef, y * sourceStride + x * 3);

        destination = source;
        Unsafe.Add(ref destination, 1) = Unsafe.Add(ref source, 1);
        Unsafe.Add(ref destination, 2) = Unsafe.Add(ref source, 2);
    }

    private static void WriteBlocked8(
        Span<byte> destination,
        ReadOnlySpan<byte> source,
        int width,
        int height,
        int destinationStride,
        bool bottomUpRow,
        bool leftToRightColumn)
    {
        ref var sourceRef = ref MemoryMarshal.GetReference(source);
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        var sourceStride = width;
        var fullWidth = width & ~3;
        var fullHeight = height & ~15;
        var rowStep = leftToRightColumn ? 1 : -1;

        for (var y0 = 0; y0 < fullHeight; y0 += 16)
        {
            var firstY = leftToRightColumn ? y0 : y0 + 15;
            var destinationColumn = leftToRightColumn ? y0 : height - 16 - y0;

            for (var x0 = 0; x0 < fullWidth; x0 += 4)
            {
                var g0 = LoadTransposed4x4(ref sourceRef, sourceStride, firstY, rowStep, x0);
                var g1 = LoadTransposed4x4(
                    ref sourceRef, sourceStride, firstY + 4 * rowStep, rowStep, x0);
                var g2 = LoadTransposed4x4(
                    ref sourceRef, sourceStride, firstY + 8 * rowStep, rowStep, x0);
                var g3 = LoadTransposed4x4(
                    ref sourceRef, sourceStride, firstY + 12 * rowStep, rowStep, x0);

                for (var column = 0; column < 4; column++)
                {
                    var values = Vector128.Create(
                        g0.GetElement(column),
                        g1.GetElement(column),
                        g2.GetElement(column),
                        g3.GetElement(column));
                    var destinationRow = bottomUpRow
                        ? x0 + column
                        : width - 1 - (x0 + column);
                    Unsafe.WriteUnaligned(
                        ref Unsafe.Add(
                            ref destinationRef,
                            destinationRow * destinationStride + destinationColumn),
                        values);
                }
            }
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = fullWidth; x < width; x++)
            {
                WritePixel8(
                    ref destinationRef,
                    ref sourceRef,
                    width,
                    height,
                    sourceStride,
                    destinationStride,
                    bottomUpRow,
                    leftToRightColumn,
                    x,
                    y);
            }
        }

        for (var y = fullHeight; y < height; y++)
        {
            for (var x = 0; x < fullWidth; x++)
            {
                WritePixel8(
                    ref destinationRef,
                    ref sourceRef,
                    width,
                    height,
                    sourceStride,
                    destinationStride,
                    bottomUpRow,
                    leftToRightColumn,
                    x,
                    y);
            }
        }
    }

    private static Vector128<uint> LoadTransposed4x4(
        ref byte sourceRef, int stride, int y, int rowStep, int x)
    {
        var r0 = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref sourceRef, y * stride + x));
        var r1 = Unsafe.ReadUnaligned<uint>(
            ref Unsafe.Add(ref sourceRef, (y + rowStep) * stride + x));
        var r2 = Unsafe.ReadUnaligned<uint>(
            ref Unsafe.Add(ref sourceRef, (y + 2 * rowStep) * stride + x));
        var r3 = Unsafe.ReadUnaligned<uint>(
            ref Unsafe.Add(ref sourceRef, (y + 3 * rowStep) * stride + x));

        return Vector128
            .Shuffle(Vector128.Create(r0, r1, r2, r3).AsByte(), s_transpose4x4)
            .AsUInt32();
    }

    private static void WriteSimd32(
        Span<byte> destination,
        ReadOnlySpan<byte> source,
        int width,
        int height,
        int destinationStride,
        bool bottomUpRow,
        bool leftToRightColumn)
    {
        ref var sourceRef = ref MemoryMarshal.GetReference(source);
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        var sourceStride = width * 4;
        var fullWidth = width & ~3;
        var fullHeight = height & ~3;

        for (var y0 = 0; y0 < fullHeight; y0 += 4)
        {
            for (var x0 = 0; x0 < fullWidth; x0 += 4)
            {
                var firstY = leftToRightColumn ? y0 : y0 + 3;
                var yStep = leftToRightColumn ? 1 : -1;

                var r0 = Unsafe.ReadUnaligned<Vector128<uint>>(
                    ref Unsafe.Add(ref sourceRef, firstY * sourceStride + x0 * 4));
                var r1 = Unsafe.ReadUnaligned<Vector128<uint>>(
                    ref Unsafe.Add(ref sourceRef, (firstY + yStep) * sourceStride + x0 * 4));
                var r2 = Unsafe.ReadUnaligned<Vector128<uint>>(
                    ref Unsafe.Add(ref sourceRef, (firstY + 2 * yStep) * sourceStride + x0 * 4));
                var r3 = Unsafe.ReadUnaligned<Vector128<uint>>(
                    ref Unsafe.Add(ref sourceRef, (firstY + 3 * yStep) * sourceStride + x0 * 4));

                var t0 = Sse2.UnpackLow(r0, r1);
                var t1 = Sse2.UnpackHigh(r0, r1);
                var t2 = Sse2.UnpackLow(r2, r3);
                var t3 = Sse2.UnpackHigh(r2, r3);

                var c0 = Sse2.Shuffle(Sse2.UnpackLow(t0, t2).AsInt32(), Transpose4x4Shuffle).AsUInt32();
                var c1 = Sse2.Shuffle(Sse2.UnpackHigh(t0, t2).AsInt32(), Transpose4x4Shuffle).AsUInt32();
                var c2 = Sse2.Shuffle(Sse2.UnpackLow(t1, t3).AsInt32(), Transpose4x4Shuffle).AsUInt32();
                var c3 = Sse2.Shuffle(Sse2.UnpackHigh(t1, t3).AsInt32(), Transpose4x4Shuffle).AsUInt32();

                var destinationColumn = leftToRightColumn ? y0 : height - 4 - y0;
                var destinationColumnOffset = destinationColumn * 4;
                var baseRow = bottomUpRow ? x0 : width - 1 - x0;
                var rowStep = bottomUpRow ? 1 : -1;

                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(
                        ref destinationRef,
                        baseRow * destinationStride + destinationColumnOffset),
                    c0);
                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(
                        ref destinationRef,
                        (baseRow + rowStep) * destinationStride + destinationColumnOffset),
                    c1);
                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(
                        ref destinationRef,
                        (baseRow + 2 * rowStep) * destinationStride + destinationColumnOffset),
                    c2);
                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(
                        ref destinationRef,
                        (baseRow + 3 * rowStep) * destinationStride + destinationColumnOffset),
                    c3);
            }
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = fullWidth; x < width; x++)
            {
                WritePixel32(
                    ref destinationRef,
                    ref sourceRef,
                    width,
                    height,
                    sourceStride,
                    destinationStride,
                    bottomUpRow,
                    leftToRightColumn,
                    x,
                    y);
            }
        }

        for (var y = fullHeight; y < height; y++)
        {
            for (var x = 0; x < fullWidth; x++)
            {
                WritePixel32(
                    ref destinationRef,
                    ref sourceRef,
                    width,
                    height,
                    sourceStride,
                    destinationStride,
                    bottomUpRow,
                    leftToRightColumn,
                    x,
                    y);
            }
        }
    }

    private static void WriteVector24(
        Span<byte> destination,
        ReadOnlySpan<byte> source,
        int width,
        int height,
        int destinationStride,
        bool bottomUpRow,
        bool leftToRightColumn)
    {
        ref var sourceRef = ref MemoryMarshal.GetReference(source);
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        var sourceStride = width * 3;
        var fullWidth = source.Length >= (long)width * height * 3 + 1
            ? width & ~3
            : (width - 1) & ~3;
        var fullHeight = height & ~3;

        for (var y0 = 0; y0 < fullHeight; y0 += 4)
        {
            for (var x0 = 0; x0 < fullWidth; x0 += 4)
            {
                var firstY = leftToRightColumn ? y0 : y0 + 3;
                var yStep = leftToRightColumn ? 1 : -1;

                var r0 = LoadRow24(ref sourceRef, firstY * sourceStride + x0 * 3);
                var r1 = LoadRow24(ref sourceRef, (firstY + yStep) * sourceStride + x0 * 3);
                var r2 = LoadRow24(ref sourceRef, (firstY + 2 * yStep) * sourceStride + x0 * 3);
                var r3 = LoadRow24(ref sourceRef, (firstY + 3 * yStep) * sourceStride + x0 * 3);

                Transpose4x4(r0, r1, r2, r3, out var c0, out var c1, out var c2, out var c3);

                var destinationColumn = leftToRightColumn ? y0 : height - 4 - y0;
                StoreColumn24(
                    ref destinationRef, c0, width, bottomUpRow, destinationColumn,
                    destinationStride, x0);
                StoreColumn24(
                    ref destinationRef, c1, width, bottomUpRow, destinationColumn,
                    destinationStride, x0 + 1);
                StoreColumn24(
                    ref destinationRef, c2, width, bottomUpRow, destinationColumn,
                    destinationStride, x0 + 2);
                StoreColumn24(
                    ref destinationRef, c3, width, bottomUpRow, destinationColumn,
                    destinationStride, x0 + 3);
            }
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = fullWidth; x < width; x++)
            {
                WritePixel24(
                    ref destinationRef,
                    ref sourceRef,
                    width,
                    height,
                    sourceStride,
                    destinationStride,
                    bottomUpRow,
                    leftToRightColumn,
                    x,
                    y);
            }
        }

        for (var y = fullHeight; y < height; y++)
        {
            for (var x = 0; x < fullWidth; x++)
            {
                WritePixel24(
                    ref destinationRef,
                    ref sourceRef,
                    width,
                    height,
                    sourceStride,
                    destinationStride,
                    bottomUpRow,
                    leftToRightColumn,
                    x,
                    y);
            }
        }
    }

    private static void WriteVector32(
        Span<byte> destination,
        ReadOnlySpan<byte> source,
        int width,
        int height,
        int destinationStride,
        bool bottomUpRow,
        bool leftToRightColumn)
    {
        ref var sourceRef = ref MemoryMarshal.GetReference(source);
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        var sourceStride = width * 4;
        var fullWidth = width & ~3;
        var fullHeight = height & ~3;

        for (var y0 = 0; y0 < fullHeight; y0 += 4)
        {
            for (var x0 = 0; x0 < fullWidth; x0 += 4)
            {
                var firstY = leftToRightColumn ? y0 : y0 + 3;
                var yStep = leftToRightColumn ? 1 : -1;

                var r0 = Unsafe.ReadUnaligned<Vector128<uint>>(
                    ref Unsafe.Add(ref sourceRef, firstY * sourceStride + x0 * 4));
                var r1 = Unsafe.ReadUnaligned<Vector128<uint>>(
                    ref Unsafe.Add(ref sourceRef, (firstY + yStep) * sourceStride + x0 * 4));
                var r2 = Unsafe.ReadUnaligned<Vector128<uint>>(
                    ref Unsafe.Add(ref sourceRef, (firstY + 2 * yStep) * sourceStride + x0 * 4));
                var r3 = Unsafe.ReadUnaligned<Vector128<uint>>(
                    ref Unsafe.Add(ref sourceRef, (firstY + 3 * yStep) * sourceStride + x0 * 4));

                Transpose4x4(r0, r1, r2, r3, out var c0, out var c1, out var c2, out var c3);

                var destinationColumn = leftToRightColumn ? y0 : height - 4 - y0;
                var destinationColumnOffset = destinationColumn * 4;
                var baseRow = bottomUpRow ? x0 : width - 1 - x0;
                var rowStep = bottomUpRow ? 1 : -1;

                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(
                        ref destinationRef,
                        baseRow * destinationStride + destinationColumnOffset),
                    c0);
                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(
                        ref destinationRef,
                        (baseRow + rowStep) * destinationStride + destinationColumnOffset),
                    c1);
                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(
                        ref destinationRef,
                        (baseRow + 2 * rowStep) * destinationStride + destinationColumnOffset),
                    c2);
                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(
                        ref destinationRef,
                        (baseRow + 3 * rowStep) * destinationStride + destinationColumnOffset),
                    c3);
            }
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = fullWidth; x < width; x++)
            {
                WritePixel32(
                    ref destinationRef,
                    ref sourceRef,
                    width,
                    height,
                    sourceStride,
                    destinationStride,
                    bottomUpRow,
                    leftToRightColumn,
                    x,
                    y);
            }
        }

        for (var y = fullHeight; y < height; y++)
        {
            for (var x = 0; x < fullWidth; x++)
            {
                WritePixel32(
                    ref destinationRef,
                    ref sourceRef,
                    width,
                    height,
                    sourceStride,
                    destinationStride,
                    bottomUpRow,
                    leftToRightColumn,
                    x,
                    y);
            }
        }
    }

    private static void WritePixel8(
        ref byte destinationRef,
        ref byte sourceRef,
        int width,
        int height,
        int sourceStride,
        int destinationStride,
        bool bottomUpRow,
        bool leftToRightColumn,
        int x,
        int y)
    {
        var destinationRow = bottomUpRow ? x : width - 1 - x;
        var destinationColumn = leftToRightColumn ? y : height - 1 - y;
        Unsafe.Add(
            ref destinationRef,
            destinationRow * destinationStride + destinationColumn) =
            Unsafe.Add(ref sourceRef, y * sourceStride + x);
    }

    private static void WritePixel32(
        ref byte destinationRef,
        ref byte sourceRef,
        int width,
        int height,
        int sourceStride,
        int destinationStride,
        bool bottomUpRow,
        bool leftToRightColumn,
        int x,
        int y)
    {
        var destinationRow = bottomUpRow ? x : width - 1 - x;
        var destinationColumn = leftToRightColumn ? y : height - 1 - y;
        Unsafe.WriteUnaligned(
            ref Unsafe.Add(
                ref destinationRef,
                destinationRow * destinationStride + destinationColumn * 4),
            Unsafe.ReadUnaligned<uint>(
                ref Unsafe.Add(ref sourceRef, y * sourceStride + x * 4)));
    }

    private static void Mirror24RowsBuffered(
        Span<byte> pixels, int width, int height, int stride)
    {
        var rowBytes = width * 3;
        var temp = ArrayPool<byte>.Shared.Rent(rowBytes + 16);

        try
        {
            var tempSpan = temp.AsSpan(0, rowBytes + 16);
            ref var tempRef = ref MemoryMarshal.GetReference(tempSpan);

            for (var y = 0; y < height; y++)
            {
                var source = pixels.Slice(y * stride, rowBytes);
                ref var sourceRef = ref MemoryMarshal.GetReference(source);

                var first = Vector128.Shuffle(
                    Unsafe.ReadUnaligned<Vector128<byte>>(
                        ref Unsafe.Add(ref sourceRef, rowBytes - 16)),
                    s_mirrorRightToLeft);
                Unsafe.WriteUnaligned(ref tempRef, first);

                var remaining = rowBytes - 12;
                var output = 12;

                while (remaining >= 12)
                {
                    var block = Vector128.Shuffle(
                        Unsafe.ReadUnaligned<Vector128<byte>>(
                            ref Unsafe.Add(ref sourceRef, remaining - 12)),
                        s_mirrorLeftToRight);
                    Unsafe.WriteUnaligned(ref Unsafe.Add(ref tempRef, output), block);
                    output += 12;
                    remaining -= 12;
                }

                for (var i = 0; i < remaining; i += 3)
                {
                    var tail = remaining - 3 - i;
                    temp[output + i] = source[tail];
                    temp[output + i + 1] = source[tail + 1];
                    temp[output + i + 2] = source[tail + 2];
                }

                tempSpan[..rowBytes].CopyTo(source);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(temp);
        }
    }

    private static void Reverse24(Span<byte> row)
    {
        ref var rowRef = ref MemoryMarshal.GetReference(row);
        var left = 0;
        var right = row.Length - 3;

        while (left < right)
        {
            ref var leftPixel = ref Unsafe.Add(ref rowRef, left);
            ref var rightPixel = ref Unsafe.Add(ref rowRef, right);

            var b0 = leftPixel;
            var b1 = Unsafe.Add(ref leftPixel, 1);
            var b2 = Unsafe.Add(ref leftPixel, 2);

            leftPixel = rightPixel;
            Unsafe.Add(ref leftPixel, 1) = Unsafe.Add(ref rightPixel, 1);
            Unsafe.Add(ref leftPixel, 2) = Unsafe.Add(ref rightPixel, 2);

            rightPixel = b0;
            Unsafe.Add(ref rightPixel, 1) = b1;
            Unsafe.Add(ref rightPixel, 2) = b2;

            left += 3;
            right -= 3;
        }
    }
}
