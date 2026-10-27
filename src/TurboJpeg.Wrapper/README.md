# TurboJpeg.Wrapper

Version `3.2.0-pre.2` (prerelease)

A .NET 10 wrapper around the libjpeg-turbo 3.2.0 TurboJPEG API. It decodes JPEG images to complete 8-, 24-, or 32-bit BMP files and encodes uncompressed BMP images to JPEG.

All public types are in the `TurboJpegWrapper` namespace. `TurboJpeg` is a static facade; `TurboJpegDecoder` and `TurboJpegEncoder` are single-handle instances. The managed assembly contains no native code; add exactly one native package for the deployment runtime.

## Requirements

- .NET 10 (`net10.0`).
- One native package matching the target runtime. Native packages bundle libjpeg-turbo 3.2.0 for Windows x64/ARM64, Linux x64/ARM64, and macOS x64/ARM64.
- Linux: glibc 2.17 or later. musl/Alpine requires a musl libjpeg-turbo build supplied through `TURBOJPEG_NATIVE_LIBRARY_PATH`.
- macOS: 10.7 or later on x64, 11 or later on ARM64.
- x86 is not supported.

## Installation

```bash
dotnet add package TurboJpeg.Wrapper --prerelease
dotnet add package TurboJpeg.Wrapper.Native.win-x64 --prerelease
```

Replace `win-x64` with the package for the target runtime.

| Runtime | Native package |
| --- | --- |
| `win-x64` | `TurboJpeg.Wrapper.Native.win-x64` |
| `win-arm64` | `TurboJpeg.Wrapper.Native.win-arm64` |
| `linux-x64` | `TurboJpeg.Wrapper.Native.linux-x64` |
| `linux-arm64` | `TurboJpeg.Wrapper.Native.linux-arm64` |
| `osx-x64`, `osx-arm64` | `TurboJpeg.Wrapper.Native.osx-universal` |

If the matching native package is missing, the first decode, encode, or native-handle creation throws `TurboJpegException` with the expected native library paths.

## Quick start

```csharp
using TurboJpegWrapper;

byte[] jpeg = File.ReadAllBytes("photo.jpg");
byte[] bmp = TurboJpeg.DecodeToBmp(jpeg);
File.WriteAllBytes("photo.bmp", bmp);

byte[] reencoded = TurboJpeg.EncodeToJpeg(bmp);
File.WriteAllBytes("photo.jpg", reencoded);
```

Decoding returns a complete BMP file, not raw pixels. Encoding accepts a complete BMP file, not raw pixel data, and returns a complete JPEG file.

## Decoding

The decoder handles 2- to 16-bit RGB, YCbCr, grayscale, CMYK, and YCCK JPEGs: lossy, lossless, progressive, and arithmetic-coded images with any chroma subsampling scheme TurboJPEG reports. BMP samples are always 8 bits per channel.

### Static facade

`TurboJpeg` is thread-safe.

```csharp
byte[] bmp = TurboJpeg.DecodeToBmp(jpeg);
byte[] scaled = TurboJpeg.DecodeToBmp(
    jpeg,
    new TurboJpegDecodeOptions { ScalingFactor = new TurboJpegScalingFactor(1, 2) });

TurboJpegImageInfo info = TurboJpeg.GetImageInfo(jpeg);
ReadOnlySpan<TurboJpegScalingFactor> factors = TurboJpeg.GetSupportedScalingFactors();

int size = TurboJpeg.GetBmpSize(jpeg);
byte[] buffer = new byte[size];
int written = TurboJpeg.DecodeToBmp(jpeg, TurboJpegDecodeOptions.Default, buffer);
```

`GetBmpSize` performs the same validation as decoding and returns the exact BMP size. The span overload returns the number of bytes written; the destination must not overlap the JPEG source, and overlap throws `ArgumentException`.

### Instance decoder

Use an instance to keep one native handle on a thread. Instances are not thread-safe.

```csharp
using var decoder = new TurboJpegDecoder();
byte[] bmp = decoder.DecodeToBmp(jpeg);
TurboJpegImageInfo info = decoder.GetImageInfo(jpeg);

int size = decoder.GetBmpSize(jpeg);
byte[] buffer = new byte[size];
int written = decoder.DecodeToBmp(jpeg, TurboJpegDecodeOptions.Default, buffer);
```

All decode and size methods throw `ObjectDisposedException` after the decoder is disposed.

### Options

`TurboJpegDecodeOptions` is an immutable record; use `with` to derive modified options, for example `TurboJpegDecodeOptions.Default with { AutoOrient = true }`. All limits are disabled by `0`.

| Option | Default | Meaning |
| --- | --- | --- |
| `ScalingFactor` | `1/1` | Output scaling. Accepts every factor returned by `GetSupportedScalingFactors()`, including upscaling factors such as `2/1`. Unsupported factors throw `ArgumentException`. Lossless JPEG requires `1/1`. |
| `Crop` | `null` | Region of the scaled stored image to decode. See [Cropping](#cropping). |
| `FastDct` | `false` | Uses the fastest DCT/IDCT algorithm. |
| `FastUpsampling` | `false` | Uses the fastest chroma upsampling algorithm. |
| `MaxPixels` | `100_000_000` | Maximum number of source pixels; `0` means no limit. |
| `MaxMemoryMegabytes` | `0` | Maximum native intermediate memory in megabytes; `0` means no limit. Violations throw `TurboJpegException`. Managed buffers are not counted. |
| `ScanLimit` | `500` | Maximum number of progressive scans; `0` means no limit. |
| `BmpBitDepth` | `Bpp32` | BMP output depth: `Bpp8` (grayscale plus 256-entry palette), `Bpp24` (BGR), or `Bpp32` (BGRA). |
| `AutoOrient` | `false` | Applies the EXIF orientation stored in the JPEG, so the BMP is upright. Orientations that swap the axes also transpose the BMP width and height. |
| `StopOnWarning` | `true` | Treats a native warning as an error. See [Warnings](#warnings). |

### Cropping

`TurboJpegDecodeOptions.Crop` decodes a rectangular region instead of the full image. Coordinates refer to the scaled, stored image: scaling is applied first, then the crop, then `AutoOrient`.

- `X` must be divisible by the scaled iMCU width: 8 for 4:4:4, 4:4:0, 4:4:1, and grayscale; 16 for 4:2:2, 4:2:0, and 2x4; 32 for 4:1:1 and 4:1:0. `Y` does not need to be aligned.
- The region must fit inside the scaled image and have positive width and height.
- Only lossy JPEGs with a known chroma subsampling scheme can be cropped.

Orientations 5-8 transpose the crop width and height. `GetBmpSize` returns the cropped BMP size. Invalid crops throw `ArgumentException`.

### Precision and CMYK

BMP output always uses 8-bit channels. JPEG precision 2-16 is accepted; precisions 2-7, 9-11, and 13-15 are mapped exactly to 8 bits, and 12- and 16-bit output is within 1 LSB of exact rounding.

Lossless RGB to `Bpp8` is converted to luma. Lossless GRAY to `Bpp24` or `Bpp32` is expanded to `B = G = R` with alpha 255. Both work at every precision and with `AutoOrient`.

CMYK and YCCK are decoded as four channels and converted best-effort to BGR, BGRA, or luma. Stored samples are treated as inverted Adobe CMYK regardless of the Adobe APP14 marker, so a producer that stores non-inverted CMYK renders with inverted colors. `Bpp8` output is the luma of the converted BGR values.

The wrapper does not perform ICC color management.

### Warnings

Truncated entropy data and extraneous bytes can produce a native warning instead of a fatal error.

- With `StopOnWarning = true` (default), a native warning throws a non-fatal `TurboJpegException` (`IsFatal = false`) and no BMP is returned.
- With `StopOnWarning = false`, warnings are ignored and a best-effort BMP is returned.
- `GetImageInfo` always treats warnings as errors.

### BMP output

- Complete BMP file with a 54-byte `BITMAPINFOHEADER`; 8-bit output adds a 1024-byte palette.
- `Bpp8`: grayscale with a 256-entry palette; color is discarded. `Bpp24`: BGR. `Bpp32`: BGRA with alpha 255.
- Bottom-up rows (`biHeight` is positive), `BI_RGB` compression, rows padded to 4 bytes with zeros.
- Pixel data starts at 54 bytes (24/32 bpp) or 1078 bytes (8 bpp); `biSizeImage` is `stride * height`.
- `biXPelsPerMeter` and `biYPelsPerMeter` are derived from the JFIF density when present, otherwise 2835 (72 DPI). `TurboJpegImageInfo` exposes `DensityX`, `DensityY`, and `DensityUnits`.
- With `AutoOrient` or `Crop`, the header describes the resulting image; with `AutoOrient`, orientations 5-8 transpose width and height.

### Supported inputs

- JPEG precision 2-16, including 12-bit lossy extended sequential.
- RGB, YCbCr, grayscale, CMYK, and YCCK.
- Baseline, extended sequential, progressive, arithmetic, and lossless entropy coding.
- Any chroma subsampling scheme TurboJPEG reports; cropping requires a known scheme.
- Lossless JPEG at `1/1` scaling.

A precision outside 2-16 throws `TurboJpegUnsupportedException`.

Without `AutoOrient`, decoded dimensions are the stored JPEG dimensions; with `Crop`, the crop dimensions. With `AutoOrient`, the orientation tag (`0x0112`) from the first EXIF APP1 segment is applied; all eight values are supported, so the result may be rotated or mirrored. Missing, malformed, or out-of-range orientation is ignored, and malformed metadata never fails decoding.

## Encoding

The encoder turns uncompressed 1-, 4-, 8-, 16-, 24-, and 32-bit BMP files into JPEG.

### Static facade

`TurboJpeg` is thread-safe.

```csharp
byte[] jpeg = TurboJpeg.EncodeToJpeg(bmp);
byte[] tuned = TurboJpeg.EncodeToJpeg(
    bmp,
    new TurboJpegEncodeOptions { Quality = 95, Subsampling = TurboJpegSubsampling.Yuv444 });

int maxSize = TurboJpeg.GetMaxJpegSize(bmp);
byte[] buffer = new byte[maxSize];
int written = TurboJpeg.EncodeToJpeg(bmp, TurboJpegEncodeOptions.Default, buffer);
File.WriteAllBytes("photo.jpg", buffer[..written]);
```

`GetMaxJpegSize` validates the BMP and returns the worst-case JPEG size for the image and effective subsampling; the actual JPEG is usually smaller. The span overload requires a destination of at least that size, writes the JPEG into it, and returns the number of bytes written. The destination must not overlap the BMP source; overlap throws `ArgumentException`.

### Instance encoder

Use an instance to keep one native handle on a thread. Optional default options apply to the parameterless overloads; explicit options replace them for that call. Instances are not thread-safe.

```csharp
using var encoder = new TurboJpegEncoder();
byte[] jpeg = encoder.EncodeToJpeg(bmp);

using var tuned = new TurboJpegEncoder(
    new TurboJpegEncodeOptions { Quality = 85, Progressive = true });
byte[] progressive = tuned.EncodeToJpeg(bmp);
byte[] oneOff = tuned.EncodeToJpeg(
    bmp,
    new TurboJpegEncodeOptions { Quality = 70 });

int maxSize = encoder.GetMaxJpegSize(bmp);
byte[] buffer = new byte[maxSize];
int written = encoder.EncodeToJpeg(bmp, TurboJpegEncodeOptions.Default, buffer);
```

All methods throw `ObjectDisposedException` after `Dispose`.

### Options

`TurboJpegEncodeOptions` is an immutable record. Use `with` to derive modified options, for example `TurboJpegEncodeOptions.Default with { Quality = 75 }`.

| Option | Default | Meaning |
| --- | --- | --- |
| `Quality` | `90` | JPEG quality from 1 (lowest) to 100 (highest). Ignored when `Lossless` is `true`. |
| `Subsampling` | `Yuv420` | `Yuv444`, `Yuv422`, `Yuv420`, `Grayscale`, `Yuv440`, `Yuv411`, `Yuv441`, `Yuv410`, or `Yuv24`; `Unknown` is invalid. Ignored when `Lossless` is `true` or `Colorspace` is `Rgb`. `Colorspace = Grayscale` or `Subsampling = Grayscale` produces grayscale output unless `Colorspace` is explicitly `Rgb`. |
| `Colorspace` | `Default` | `Default` resolves to `YCbCr` for color input; `Rgb`, `YCbCr`, and `Grayscale` can be requested. `Cmyk` and `Ycck` are decoding-only and rejected. `Rgb` forces 4:4:4. Ignored when `Lossless` is `true`. |
| `FastDct` | `false` | Uses the fastest DCT/IDCT algorithm. Ignored when `Lossless` is `true`. |
| `OptimizeCoding` | `false` | Generates optimal Huffman tables. Ignored when `Lossless` is `true`. |
| `Progressive` | `false` | Produces a progressive JPEG. Ignored when `Lossless` is `true`. |
| `ArithmeticCoding` | `false` | Uses arithmetic entropy coding. Ignored when `Lossless` is `true`. |
| `Lossless` | `false` | Enables lossless JPEG compression. Only `LosslessPredictor`, `LosslessPointTransform`, `RestartRows`, `MaxMemoryMegabytes`, and `StopOnWarning` apply. A non-zero `RestartBlocks` is rejected, and `Density` must be `null`. |
| `LosslessPredictor` | `1` | Lossless predictor from 1 through 7. Lossless only. |
| `LosslessPointTransform` | `0` | Lossless point transform from 0 through 7. Lossless only. A value above 0 right-shifts the samples, so the output is not bit-exact. |
| `RestartBlocks` | `0` | MCUs between restart markers, 0-65535; `0` disables them. Lossy only; must be `0` with `Lossless`, and cannot be combined with a non-zero `RestartRows`. |
| `RestartRows` | `0` | MCU rows between restart markers, 0-65535; `0` disables them. Lossy and lossless; cannot be combined with a non-zero `RestartBlocks`. |
| `MaxMemoryMegabytes` | `0` | Maximum native intermediate memory in megabytes; `0` means no limit. Violations throw `TurboJpegException`. |
| `StopOnWarning` | `false` | Treats a TurboJPEG warning as an encoding failure instead of continuing. |
| `Density` | `null` | Pixel density written to the JPEG header. `null` derives the density from the BMP as described in [Density](#density). Lossy YCbCr and grayscale output only; rejected with lossless or RGB output. |

Range and enum validation runs when a value is assigned, including values that `Lossless` later ignores. Cross-property validation runs when encoding or sizing starts: `RestartBlocks` and `RestartRows` cannot both be non-zero, `RestartBlocks` must be zero with `Lossless = true`, and `Density` requires lossy YCbCr or grayscale output. Violations throw `ArgumentException` with parameter name `options`.

`Colorspace = Rgb` forces 4:4:4 and ignores `Subsampling`. Other invalid combinations are rejected by libjpeg-turbo and surface as `TurboJpegException`.

### BMP input support

The encoder accepts complete BMP files with DIB header sizes 40, 52, 56, 108, and 124, bottom-up (positive height) or top-down (negative height), with 1, 4, 8, 16, 24, or 32 bits per pixel.

| BMP layout | Handling |
| --- | --- |
| 24-bit `BI_RGB` | BGR |
| 32-bit `BI_RGB` | BGRX; the fourth byte is ignored |
| 32-bit `BI_BITFIELDS` with masks `00FF0000`/`0000FF00`/`000000FF` | BGRX; alpha is ignored |
| 8-bit `BI_RGB` with a full 256-entry identity grayscale palette and grayscale output requested | Palette indices used as gray samples |
| 8-bit grayscale palette with grayscale output requested | Palette lookup to 8-bit gray |
| 8-bit color palette, or grayscale output not requested | Palette lookup to BGR |
| 1- or 4-bit grayscale palette with grayscale output requested | Expanded to 8-bit gray |
| 1- or 4-bit palette otherwise | Expanded to BGR |
| 16-bit `BI_RGB` (X1R5G5B5) or `BI_BITFIELDS` (including RGB565) | Scaled to BGR |
| 32-bit `BI_BITFIELDS` with other masks | Scaled to BGR; alpha is ignored |

"Grayscale output requested" means `Colorspace = Grayscale`, or `Subsampling = Grayscale` when `Colorspace` is not explicitly `Rgb`. Explicit `Rgb` wins and forces 4:4:4. It applies only to lossy encoding; lossless mode does not convert color to grayscale.

Palette images use `clrUsed` entries when non-zero, otherwise `1 << bitCount` entries. Indices beyond the declared entries are treated as black. Alpha and color management are not applied.

The following BMP variants are rejected:

- OS/2 core bitmaps (DIB header size 12) and any DIB header size other than 40, 52, 56, 108, or 124: `TurboJpegUnsupportedException`.
- RLE4/RLE8, `BI_JPEG`, `BI_PNG`, and compression other than `BI_RGB`/`BI_BITFIELDS`: `TurboJpegUnsupportedException`.
- `BI_BITFIELDS` on 24-bit bitmaps, and empty, non-contiguous, or overlapping masks: `TurboJpegUnsupportedException`.
- Malformed or truncated files, including a missing `BM` signature, non-positive width, zero or `int.MinValue` height, invalid planes or bit counts, offsets outside the buffer, and truncated pixel data: `ArgumentException`.

### Density

`Density` is a `TurboJpegDensity` with `X` and `Y` from 1 to 65535 and `PixelsPerInch`, `PixelsPerCentimeter`, or `Unknown` units. The default struct (0 x 0) is invalid.

Density is written only for lossy YCbCr and grayscale output. Lossless and RGB output reject an explicit density and ignore the BMP density, writing 1 x 1 with `Unknown` units.

With `Density = null` and lossy YCbCr or grayscale output, the encoder derives density from the BMP `biXPelsPerMeter` and `biYPelsPerMeter`:

- A non-zero value is converted to DPI and clamped to 1-65535.
- If only one axis is non-zero, its value is used for both axes.
- With no BMP density, the encoder writes 1 x 1 with `Unknown` units.

An explicit `Density` overrides the BMP values.

## Errors

- `ArgumentException`: empty input; invalid options (unsupported scaling factor, invalid crop, invalid restart or density combinations, invalid colorspace for encoding); malformed or truncated BMP; destination too small; destination overlapping the source.
- `ArgumentNullException`: a null options object.
- `ArgumentOutOfRangeException`: negative crop offset, non-positive crop dimension, invalid BMP bit depth, or an option or density value outside its allowed range or not supported for the requested direction.
- `TurboJpegUnsupportedException`: image precision outside 2-16 bits, or a BMP encoding the library does not support.
- `TurboJpegImageTooLargeException`: `MaxPixels` is exceeded, or the BMP, worst-case JPEG, or a conversion buffer would exceed `Array.MaxLength`.
- `TurboJpegException`: native error, native-library load failure, or a native warning with the direction's `StopOnWarning` enabled.
- `ObjectDisposedException`: an instance method was called after `Dispose`.
- `PlatformNotSupportedException`: no known runtime identifier for the current OS/architecture and no explicit native path.

## Native library loading

Native libraries are distributed as separate runtime packages, one asset each:

| Runtime | Asset |
| --- | --- |
| `win-x64` | `runtimes/win-x64/native/turbojpeg.dll` |
| `win-arm64` | `runtimes/win-arm64/native/turbojpeg.dll` |
| `linux-x64` | `runtimes/linux-x64/native/libturbojpeg.so` |
| `linux-arm64` | `runtimes/linux-arm64/native/libturbojpeg.so` |
| `osx-x64`, `osx-arm64` | `runtimes/osx/native/libturbojpeg.dylib` |

The macOS library is a universal binary (x86_64 + arm64).

Set `TURBOJPEG_NATIVE_LIBRARY_PATH` to the full path of a native library file to override discovery. A non-blank value is the only location used; an invalid path fails immediately.

Otherwise the resolver tries, in order:

1. `runtimes/{rid}/native/{library}` under `AppContext.BaseDirectory`.
2. On macOS only, `runtimes/osx/native/{library}` under `AppContext.BaseDirectory`.
3. `{library}` in `AppContext.BaseDirectory`.
4. The operating system's normal library search.

## Threading

- `TurboJpeg` is safe for concurrent use.
- `TurboJpegDecoder` and `TurboJpegEncoder` are not thread-safe. Use one instance per thread or synchronize access.
- `TurboJpegDecodeOptions` and `TurboJpegEncodeOptions` are immutable and can be shared across threads and calls.

## Package contents and license

The package contains the managed `net10.0` assembly, its XML documentation file, `README.md`, `LICENSE`, and `THIRD-PARTY-NOTICES.md`. It contains no native assets and has no package dependencies.

The source code is licensed under the MIT License (`LICENSE`). The bundled native libraries are libjpeg-turbo 3.2.0, licensed under BSD-3-Clause with the Independent JPEG Group and zlib notices (`THIRD-PARTY-NOTICES.md`).
