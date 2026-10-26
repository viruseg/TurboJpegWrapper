using TurboJpegWrapper.Encoder;
using TurboJpegWrapper.Encoder.Internal.Bmp;
using TurboJpegWrapper.Internal.Native;

namespace TurboJpegWrapper.Encoder.Tests;

public sealed class BmpPixelConverterTests
{
    [Fact]
    public void Convert_24BitBottomUp_ReturnsDirectBgr()
    {
        var bmp = BmpBuilder.Create24(
            3,
            2,
            (x, y) => ((byte)(10 + x), (byte)(20 + y), (byte)(30 + x + y)));
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: false);
        try
        {
            Assert.Equal(TurboJpegNativeApi.PixelFormatBgr, pixels.PixelFormat);
            Assert.True(pixels.BottomUp);
            Assert.Equal(info.Stride, pixels.Pitch);
            Assert.Equal(12, pixels.Pitch);
            Assert.True(pixels.Data.SequenceEqual(bmp.AsSpan(info.PixelOffset)));

            // Bottom-up files store the image's lower row first.
            Assert.Equal((byte)10, pixels.Data[0]);
            Assert.Equal((byte)21, pixels.Data[1]);
            Assert.Equal((byte)31, pixels.Data[2]);

            Assert.Equal(((byte)10, (byte)20, (byte)30), PixelBgr(pixels, info.Height, 0, 0));
            Assert.Equal(((byte)12, (byte)21, (byte)33), PixelBgr(pixels, info.Height, 2, 1));

            bmp[info.PixelOffset] = 99;
            Assert.Equal((byte)99, pixels.Data[0]);
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Convert_24BitTopDown_ReturnsDirectBgrWithTopDownRows()
    {
        var bmp = BmpBuilder.Create24(
            2,
            2,
            (x, y) => ((byte)(1 + x), (byte)(3 + y), (byte)(5 + x + y)),
            topDown: true);
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: false);
        try
        {
            Assert.Equal(TurboJpegNativeApi.PixelFormatBgr, pixels.PixelFormat);
            Assert.False(pixels.BottomUp);
            Assert.Equal(8, pixels.Pitch);
            Assert.Equal((byte)1, pixels.Data[0]);
            Assert.Equal((byte)3, pixels.Data[1]);
            Assert.Equal((byte)5, pixels.Data[2]);

            Assert.Equal(((byte)1, (byte)3, (byte)5), PixelBgr(pixels, info.Height, 0, 0));
            Assert.Equal(((byte)2, (byte)4, (byte)7), PixelBgr(pixels, info.Height, 1, 1));
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Convert_32BitTopDown_ReturnsDirectBgrx()
    {
        var bmp = BmpBuilder.Create32(
            2,
            2,
            (x, y) => ((byte)(10 + x + y), (byte)(20 + x + y), (byte)(30 + x + y), (byte)(40 + x + y)),
            topDown: true);
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: true);
        try
        {
            Assert.Equal(TurboJpegNativeApi.PixelFormatBgrx, pixels.PixelFormat);
            Assert.False(pixels.BottomUp);
            Assert.Equal(8, pixels.Pitch);
            Assert.True(pixels.Data.SequenceEqual(bmp.AsSpan(info.PixelOffset)));

            Assert.Equal(((byte)10, (byte)20, (byte)30, (byte)40), PixelBgrx(pixels, info.Height, 0, 0));
            Assert.Equal(((byte)12, (byte)22, (byte)32, (byte)42), PixelBgrx(pixels, info.Height, 1, 1));
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Convert_32BitBottomUp_ReportsBottomUp()
    {
        var bmp = BmpBuilder.Create32(
            1,
            2,
            (_, y) => ((byte)(10 + y), (byte)(20 + y), (byte)(30 + y), (byte)(40 + y)));
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: false);
        try
        {
            Assert.Equal(TurboJpegNativeApi.PixelFormatBgrx, pixels.PixelFormat);
            Assert.True(pixels.BottomUp);
            Assert.Equal(4, pixels.Pitch);

            // The first stored row is the image's bottom row.
            Assert.Equal((byte)11, pixels.Data[0]);
            Assert.Equal(((byte)10, (byte)20, (byte)30, (byte)40), PixelBgrx(pixels, info.Height, 0, 0));
            Assert.Equal(((byte)11, (byte)21, (byte)31, (byte)41), PixelBgrx(pixels, info.Height, 0, 1));
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Convert_32BitStandardBitfieldMasks_ReturnsDirectBgrx()
    {
        var samples = new uint[] { 0xFF3C2814, 0x80112233 };
        var bmp = BmpBuilder.Create32Bitfields(
            2,
            1,
            (x, _) => samples[x],
            redMask: 0x00FF0000,
            greenMask: 0x0000FF00,
            blueMask: 0x000000FF);
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: false);
        try
        {
            Assert.Equal(3u, info.Compression);
            Assert.Equal(66, info.PixelOffset);
            Assert.Equal(TurboJpegNativeApi.PixelFormatBgrx, pixels.PixelFormat);
            Assert.True(pixels.BottomUp);
            Assert.Equal(8, pixels.Pitch);
            Assert.True(pixels.Data.SequenceEqual(bmp.AsSpan(info.PixelOffset)));
            Assert.Equal(((byte)0x14, (byte)0x28, (byte)0x3C, (byte)0xFF), PixelBgrx(pixels, info.Height, 0, 0));
            Assert.Equal(((byte)0x33, (byte)0x22, (byte)0x11, (byte)0x80), PixelBgrx(pixels, info.Height, 1, 0));
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Convert_8BitIdentityGrayscalePalette_GrayscaleRequested_ReturnsDirectGray()
    {
        var palette = CreateIdentityPalette();
        var bmp = BmpBuilder.CreatePalette8(3, 2, (x, y) => (byte)(x * 80 + y * 5), palette);
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: true);
        try
        {
            Assert.Equal(TurboJpegNativeApi.PixelFormatGray, pixels.PixelFormat);
            Assert.True(pixels.BottomUp);
            Assert.Equal(info.Stride, pixels.Pitch);
            Assert.Equal(4, pixels.Pitch);
            Assert.True(pixels.Data.SequenceEqual(bmp.AsSpan(info.PixelOffset)));

            Assert.Equal((byte)0, PixelGray(pixels, info.Height, 0, 0));
            Assert.Equal((byte)80, PixelGray(pixels, info.Height, 1, 0));
            Assert.Equal((byte)160, PixelGray(pixels, info.Height, 2, 0));
            Assert.Equal((byte)5, PixelGray(pixels, info.Height, 0, 1));
            Assert.Equal((byte)165, PixelGray(pixels, info.Height, 2, 1));

            bmp[info.PixelOffset] = 255;
            Assert.Equal((byte)255, pixels.Data[0]);
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Convert_8BitIdentityGrayscalePalette_ColorRequested_ExpandsToBgr()
    {
        var palette = CreateIdentityPalette();
        var bmp = BmpBuilder.CreatePalette8(3, 2, (x, y) => (byte)(x * 80 + y * 5), palette);
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: false);
        try
        {
            Assert.Equal(TurboJpegNativeApi.PixelFormatBgr, pixels.PixelFormat);
            Assert.True(pixels.BottomUp);
            Assert.Equal(9, pixels.Pitch);
            Assert.Equal(18, pixels.Data.Length);

            Assert.Equal(((byte)80, (byte)80, (byte)80), PixelBgr(pixels, info.Height, 1, 0));
            Assert.Equal(((byte)165, (byte)165, (byte)165), PixelBgr(pixels, info.Height, 2, 1));

            bmp[info.PixelOffset] = 255;
            Assert.Equal(((byte)5, (byte)5, (byte)5), PixelBgr(pixels, info.Height, 0, 1));
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Convert_8BitColourPalette_ExpandsExactBgr()
    {
        var palette = new (byte B, byte G, byte R)[]
        {
            (10, 20, 30),
            (40, 50, 60),
            (70, 80, 90),
            (100, 110, 120),
        };
        var bmp = BmpBuilder.CreatePalette8(
            3,
            2,
            (x, y) => (byte)((x + y * 3) % 4),
            palette,
            topDown: true,
            clrUsed: 4);
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: false);
        try
        {
            Assert.Equal(TurboJpegNativeApi.PixelFormatBgr, pixels.PixelFormat);
            Assert.False(pixels.BottomUp);
            Assert.Equal(9, pixels.Pitch);
            Assert.Equal(18, pixels.Data.Length);

            Assert.Equal(((byte)10, (byte)20, (byte)30), PixelBgr(pixels, info.Height, 0, 0));
            Assert.Equal(((byte)70, (byte)80, (byte)90), PixelBgr(pixels, info.Height, 2, 0));
            Assert.Equal(((byte)100, (byte)110, (byte)120), PixelBgr(pixels, info.Height, 0, 1));
            Assert.Equal(((byte)10, (byte)20, (byte)30), PixelBgr(pixels, info.Height, 1, 1));
            Assert.Equal(((byte)40, (byte)50, (byte)60), PixelBgr(pixels, info.Height, 2, 1));

            bmp[info.PixelOffset] = 200;
            Assert.Equal(((byte)10, (byte)20, (byte)30), PixelBgr(pixels, info.Height, 0, 0));
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Convert_8BitColourPalette_GrayscaleRequested_KeepsExactBgr()
    {
        var palette = new (byte B, byte G, byte R)[]
        {
            (10, 20, 30),
            (40, 50, 60),
            (70, 80, 90),
            (100, 110, 120),
        };
        var bmp = BmpBuilder.CreatePalette8(
            3,
            2,
            (x, y) => (byte)((x + y * 3) % 4),
            palette,
            clrUsed: 4);
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: true);
        try
        {
            Assert.Equal(TurboJpegNativeApi.PixelFormatBgr, pixels.PixelFormat);
            Assert.True(pixels.BottomUp);
            Assert.Equal(9, pixels.Pitch);
            Assert.Equal(18, pixels.Data.Length);

            // A colour palette carries no luma, so the requested gray output stays BGR.
            // Rows are stored bottom-up: image row 1 (indices 3, 0, 1), then row 0 (0, 1, 2).
            var expected = new byte[]
            {
                100, 110, 120, 10, 20, 30, 40, 50, 60,
                10, 20, 30, 40, 50, 60, 70, 80, 90,
            };
            Assert.True(pixels.Data.SequenceEqual(expected));
            Assert.Equal(((byte)10, (byte)20, (byte)30), PixelBgr(pixels, info.Height, 0, 0));
            Assert.Equal(((byte)100, (byte)110, (byte)120), PixelBgr(pixels, info.Height, 0, 1));
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Convert_8BitPaletteIndicesBeyondClrUsed_BecomeBlack()
    {
        var palette = new (byte B, byte G, byte R)[]
        {
            (10, 20, 30),
            (40, 50, 60),
            (70, 80, 90),
        };
        var bmp = BmpBuilder.CreatePalette8(
            4,
            1,
            (x, _) => (byte)(x == 3 ? 9 : x),
            palette,
            clrUsed: 3);
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: false);
        try
        {
            Assert.Equal(3, info.PaletteCount);
            Assert.Equal(TurboJpegNativeApi.PixelFormatBgr, pixels.PixelFormat);
            Assert.Equal(12, pixels.Pitch);
            Assert.Equal(12, pixels.Data.Length);

            Assert.Equal(((byte)10, (byte)20, (byte)30), PixelBgr(pixels, info.Height, 0, 0));
            Assert.Equal(((byte)40, (byte)50, (byte)60), PixelBgr(pixels, info.Height, 1, 0));
            Assert.Equal(((byte)70, (byte)80, (byte)90), PixelBgr(pixels, info.Height, 2, 0));
            Assert.Equal(((byte)0, (byte)0, (byte)0), PixelBgr(pixels, info.Height, 3, 0));
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Convert_8BitNonIdentityGrayscalePalette_GrayscaleRequested_UsesPaletteValues()
    {
        var palette = new (byte B, byte G, byte R)[256];
        for (var i = 0; i < palette.Length; i++)
        {
            var value = (byte)(255 - i);
            palette[i] = (value, value, value);
        }

        var bmp = BmpBuilder.CreatePalette8(4, 1, (x, _) => x switch
        {
            0 => 0,
            1 => 1,
            2 => 127,
            _ => 255,
        }, palette);
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: true);
        try
        {
            Assert.Equal(256, info.PaletteCount);
            Assert.Equal(TurboJpegNativeApi.PixelFormatGray, pixels.PixelFormat);
            Assert.Equal(4, pixels.Pitch);
            Assert.Equal(4, pixels.Data.Length);

            Assert.Equal((byte)255, PixelGray(pixels, info.Height, 0, 0));
            Assert.Equal((byte)254, PixelGray(pixels, info.Height, 1, 0));
            Assert.Equal((byte)128, PixelGray(pixels, info.Height, 2, 0));
            Assert.Equal((byte)0, PixelGray(pixels, info.Height, 3, 0));
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Convert_4BitPalette_ExpandsBgrAndUnpacksHighNibbleFirst()
    {
        var palette = new (byte B, byte G, byte R)[16];
        for (var i = 0; i < palette.Length; i++)
        {
            palette[i] = ((byte)(i * 10), (byte)(i * 10 + 1), (byte)(i * 10 + 2));
        }

        var bmp = BmpBuilder.CreatePalette4(5, 2, (x, y) => (byte)((x + y * 5) % 16), palette);
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: false);
        try
        {
            Assert.Equal(TurboJpegNativeApi.PixelFormatBgr, pixels.PixelFormat);
            Assert.True(pixels.BottomUp);
            Assert.Equal(15, pixels.Pitch);
            Assert.Equal(30, pixels.Data.Length);

            Assert.Equal(((byte)0, (byte)1, (byte)2), PixelBgr(pixels, info.Height, 0, 0));
            Assert.Equal(((byte)10, (byte)11, (byte)12), PixelBgr(pixels, info.Height, 1, 0));
            Assert.Equal(((byte)40, (byte)41, (byte)42), PixelBgr(pixels, info.Height, 4, 0));
            Assert.Equal(((byte)50, (byte)51, (byte)52), PixelBgr(pixels, info.Height, 0, 1));
            Assert.Equal(((byte)90, (byte)91, (byte)92), PixelBgr(pixels, info.Height, 4, 1));

            // The first output row comes from the first stored (bottom) row.
            Assert.Equal((byte)50, pixels.Data[0]);
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Convert_4BitPalette_GrayscaleRequested_ExpandsGray()
    {
        var palette = new (byte B, byte G, byte R)[16];
        for (var i = 0; i < palette.Length; i++)
        {
            var value = (byte)(i * 16);
            palette[i] = (value, value, value);
        }

        var bmp = BmpBuilder.CreatePalette4(5, 1, (x, _) => (byte)(x == 4 ? 15 : x), palette);
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: true);
        try
        {
            Assert.Equal(TurboJpegNativeApi.PixelFormatGray, pixels.PixelFormat);
            Assert.Equal(5, pixels.Pitch);
            Assert.Equal(5, pixels.Data.Length);
            Assert.Equal((byte)0, PixelGray(pixels, info.Height, 0, 0));
            Assert.Equal((byte)16, PixelGray(pixels, info.Height, 1, 0));
            Assert.Equal((byte)32, PixelGray(pixels, info.Height, 2, 0));
            Assert.Equal((byte)48, PixelGray(pixels, info.Height, 3, 0));
            Assert.Equal((byte)240, PixelGray(pixels, info.Height, 4, 0));
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Convert_1BitPalette_ExpandsBgr()
    {
        var palette = new (byte B, byte G, byte R)[]
        {
            (1, 2, 3),
            (4, 5, 6),
        };
        var bmp = BmpBuilder.CreatePalette1(10, 2, (x, y) => (byte)((x + y) & 1), palette);
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: false);
        try
        {
            Assert.Equal(TurboJpegNativeApi.PixelFormatBgr, pixels.PixelFormat);
            Assert.True(pixels.BottomUp);
            Assert.Equal(30, pixels.Pitch);
            Assert.Equal(60, pixels.Data.Length);

            Assert.Equal(((byte)1, (byte)2, (byte)3), PixelBgr(pixels, info.Height, 0, 0));
            Assert.Equal(((byte)4, (byte)5, (byte)6), PixelBgr(pixels, info.Height, 1, 0));
            Assert.Equal(((byte)1, (byte)2, (byte)3), PixelBgr(pixels, info.Height, 9, 1));

            // The first stored row is the bottom row starting at index 1.
            Assert.Equal((byte)4, pixels.Data[0]);
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Convert_1BitPalette_GrayscaleRequested_ExpandsGray()
    {
        var palette = new (byte B, byte G, byte R)[]
        {
            (10, 10, 10),
            (250, 250, 250),
        };
        var bmp = BmpBuilder.CreatePalette1(10, 1, (x, _) => (byte)(x % 3 == 0 ? 1 : 0), palette);
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: true);
        try
        {
            Assert.Equal(TurboJpegNativeApi.PixelFormatGray, pixels.PixelFormat);
            Assert.Equal(10, pixels.Pitch);
            Assert.Equal(10, pixels.Data.Length);
            for (var x = 0; x < 10; x++)
            {
                Assert.Equal(x % 3 == 0 ? (byte)250 : (byte)10, PixelGray(pixels, info.Height, x, 0));
            }
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Convert_16Bit555WithoutMasks_ProducesBgr()
    {
        var samples = new ushort[] { 0x7C00, 0x03E0, 0x001F, 0x7FFF, 0x0000, 0x4210 };
        var bmp = BmpBuilder.Create16(
            6,
            1,
            (x, _) => samples[x],
            redMask: 0,
            greenMask: 0,
            blueMask: 0,
            bitfields: false);
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: false);
        try
        {
            Assert.Equal(TurboJpegNativeApi.PixelFormatBgr, pixels.PixelFormat);
            Assert.Equal(18, pixels.Pitch);
            Assert.Equal(18, pixels.Data.Length);

            var expected = new (byte B, byte G, byte R)[]
            {
                (0, 0, 255),
                (0, 255, 0),
                (255, 0, 0),
                (255, 255, 255),
                (0, 0, 0),
                (132, 132, 132),
            };
            for (var x = 0; x < expected.Length; x++)
            {
                Assert.Equal(expected[x], PixelBgr(pixels, info.Height, x, 0));
            }
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Convert_16Bit565WithMasks_ProducesBgr()
    {
        var samples = new ushort[] { 0xF800, 0x07E0, 0x001F, 0xFFFF, 0x8410 };
        var bmp = BmpBuilder.Create16(
            5,
            1,
            (x, _) => samples[x],
            redMask: 0xF800,
            greenMask: 0x07E0,
            blueMask: 0x001F,
            bitfields: true);
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: false);
        try
        {
            Assert.Equal(TurboJpegNativeApi.PixelFormatBgr, pixels.PixelFormat);
            Assert.Equal(15, pixels.Pitch);
            Assert.Equal(15, pixels.Data.Length);

            Assert.Equal(((byte)0, (byte)0, (byte)255), PixelBgr(pixels, info.Height, 0, 0));
            Assert.Equal(((byte)0, (byte)255, (byte)0), PixelBgr(pixels, info.Height, 1, 0));
            Assert.Equal(((byte)255, (byte)0, (byte)0), PixelBgr(pixels, info.Height, 2, 0));
            Assert.Equal(((byte)255, (byte)255, (byte)255), PixelBgr(pixels, info.Height, 3, 0));
            Assert.Equal(((byte)132, (byte)130, (byte)132), PixelBgr(pixels, info.Height, 4, 0));
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Convert_32BitNonStandardMasks_ProducesBgrAndIgnoresAlpha()
    {
        var samples = new uint[]
        {
            0x03020100, // B=3 G=2 R=1, alpha 0
            0xFF8000FF, // B=255 G=128 R=0, alpha 255
            0x000000AA, // black, alpha 170
            0x11223355, // B=17 G=34 R=51, alpha 85
            0x030201FF, // B=3 G=2 R=1, alpha 255
        };
        var bmp = BmpBuilder.Create32Bitfields(
            5,
            1,
            (x, _) => samples[x],
            redMask: 0x0000FF00,
            greenMask: 0x00FF0000,
            blueMask: 0xFF000000);
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: false);
        try
        {
            Assert.Equal(TurboJpegNativeApi.PixelFormatBgr, pixels.PixelFormat);
            Assert.Equal(15, pixels.Pitch);
            Assert.Equal(15, pixels.Data.Length);

            Assert.Equal(((byte)3, (byte)2, (byte)1), PixelBgr(pixels, info.Height, 0, 0));
            Assert.Equal(((byte)255, (byte)128, (byte)0), PixelBgr(pixels, info.Height, 1, 0));
            Assert.Equal(((byte)0, (byte)0, (byte)0), PixelBgr(pixels, info.Height, 2, 0));
            Assert.Equal(((byte)17, (byte)34, (byte)51), PixelBgr(pixels, info.Height, 3, 0));
            Assert.Equal(((byte)3, (byte)2, (byte)1), PixelBgr(pixels, info.Height, 4, 0));
            Assert.Equal(PixelBgr(pixels, info.Height, 0, 0), PixelBgr(pixels, info.Height, 4, 0));
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Convert_32BitTenBitMasks_ScalesToBgrAndIgnoresAlpha()
    {
        var samples = new uint[]
        {
            0xFFF80000, // alpha 3, R=1023, G=512, B=0
            0x001003FF, // alpha 0, R=1, G=0, B=1023
        };
        var bmp = BmpBuilder.Create32Bitfields(
            2,
            1,
            (x, _) => samples[x],
            redMask: 0x3FF00000,
            greenMask: 0x000FFC00,
            blueMask: 0x000003FF);
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: false);
        try
        {
            Assert.Equal(TurboJpegNativeApi.PixelFormatBgr, pixels.PixelFormat);
            Assert.Equal(6, pixels.Pitch);
            Assert.Equal(6, pixels.Data.Length);
            Assert.Equal(((byte)0, (byte)128, (byte)255), PixelBgr(pixels, info.Height, 0, 0));
            Assert.Equal(((byte)255, (byte)0, (byte)0), PixelBgr(pixels, info.Height, 1, 0));
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Convert_32BitSixteenBitRedMask_ScalesThroughLookupTable()
    {
        var samples = new uint[] { 0xFFFF1234, 0x0000FF00, 0x000000FF, 0x80008080 };
        var bmp = BmpBuilder.Create32Bitfields(
            4,
            1,
            (x, _) => samples[x],
            redMask: 0xFFFF0000,
            greenMask: 0x0000FF00,
            blueMask: 0x000000FF);
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: false);
        try
        {
            Assert.Equal(TurboJpegNativeApi.PixelFormatBgr, pixels.PixelFormat);
            Assert.Equal(((byte)0x34, (byte)0x12, (byte)255), PixelBgr(pixels, info.Height, 0, 0));
            Assert.Equal(((byte)0, (byte)255, (byte)0), PixelBgr(pixels, info.Height, 1, 0));
            Assert.Equal(((byte)255, (byte)0, (byte)0), PixelBgr(pixels, info.Height, 2, 0));
            Assert.Equal(((byte)0x80, (byte)0x80, (byte)128), PixelBgr(pixels, info.Height, 3, 0));
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Convert_32BitWideMask_FallsBackToExactDivision()
    {
        var samples = new uint[] { 0xFFFFF0F0, 0x0000FF00, 0x00001010 };
        var bmp = BmpBuilder.Create32Bitfields(
            3,
            1,
            (x, _) => samples[x],
            redMask: 0xFFFFF000,
            greenMask: 0x00000F00,
            blueMask: 0x000000F0);
        var info = BmpReader.Read(bmp);

        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: false);
        try
        {
            Assert.Equal(((byte)255, (byte)0, (byte)255), PixelBgr(pixels, info.Height, 0, 0));
            Assert.Equal(((byte)0, (byte)255, (byte)0), PixelBgr(pixels, info.Height, 1, 0));
            Assert.Equal(((byte)17, (byte)0, (byte)0), PixelBgr(pixels, info.Height, 2, 0));
        }
        finally
        {
            pixels.Release();
        }
    }

    [Fact]
    public void Release_DirectPath_CanBeCalledTwice()
    {
        var bmp = BmpBuilder.Create24(2, 2, (_, _) => ((byte)1, (byte)2, (byte)3));
        var info = BmpReader.Read(bmp);
        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: false);

        pixels.Release();
        pixels.Release();
    }

    [Fact]
    public void Release_ConvertedPath_CanBeCalledTwice()
    {
        var palette = CreateIdentityPalette();
        var bmp = BmpBuilder.CreatePalette8(2, 2, (_, _) => 1, palette);
        var info = BmpReader.Read(bmp);
        var pixels = BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: false);

        pixels.Release();
        pixels.Release();
    }

    [Theory]
    [InlineData(int.MaxValue, 1)]
    [InlineData(1_000_000, 1_000_000)]
    public void Convert_ConvertedBufferExceedingMaxLength_ThrowsImageTooLarge(int width, int height)
    {
        var info = new BmpInfo(
            width: width,
            height: height,
            topDown: false,
            bitCount: 8,
            compression: 0,
            pixelOffset: 54,
            stride: width,
            paletteOffset: 0,
            paletteCount: 0,
            redMask: 0,
            greenMask: 0,
            blueMask: 0,
            densityX: 0,
            densityY: 0);
        var bmp = new byte[54];

        var exception = Record.Exception(() =>
        {
            BmpPixelConverter.Convert(bmp, in info, grayscaleOutput: false);
        });

        Assert.IsType<TurboJpegImageTooLargeException>(exception);
    }

    private static (byte B, byte G, byte R)[] CreateIdentityPalette()
    {
        var palette = new (byte B, byte G, byte R)[256];
        for (var i = 0; i < palette.Length; i++)
        {
            palette[i] = ((byte)i, (byte)i, (byte)i);
        }

        return palette;
    }

    private static (byte B, byte G, byte R) PixelBgr(BmpPixels pixels, int height, int x, int y)
    {
        var row = pixels.BottomUp ? height - 1 - y : y;
        var offset = row * pixels.Pitch + x * 3;
        return (pixels.Data[offset], pixels.Data[offset + 1], pixels.Data[offset + 2]);
    }

    private static (byte B, byte G, byte R, byte A) PixelBgrx(BmpPixels pixels, int height, int x, int y)
    {
        var row = pixels.BottomUp ? height - 1 - y : y;
        var offset = row * pixels.Pitch + x * 4;
        return (pixels.Data[offset], pixels.Data[offset + 1], pixels.Data[offset + 2], pixels.Data[offset + 3]);
    }

    private static byte PixelGray(BmpPixels pixels, int height, int x, int y)
    {
        var row = pixels.BottomUp ? height - 1 - y : y;
        return pixels.Data[row * pixels.Pitch + x];
    }
}
