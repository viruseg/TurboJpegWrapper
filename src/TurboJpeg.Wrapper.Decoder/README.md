# TurboJpeg.Wrapper.Decoder

Version `3.2.0-pre.2` (prerelease)

A production-oriented .NET 10 wrapper around the libjpeg-turbo TurboJPEG API for decoding JPEG images to complete 8-, 24-, or 32-bit BMP files.

The package decodes 2- to 16-bit RGB, YCbCr, grayscale, CMYK, and YCCK JPEGs, including lossy, lossless, progressive, and arithmetic-coded images with every chroma subsampling scheme TurboJPEG reports. BMP samples are always 8 bits per channel. The package is decode-only. It does not encode JPEGs or expose YUV buffers. For BMP-to-JPEG encoding, use the separate `TurboJpeg.Wrapper.Encoder` package. Where TurboJPEG supports the requested conversion, it writes grayscale, BGR, or BGRA pixels directly into the final BMP allocation; the lossless RGB to `Bpp8` and lossless GRAY to `Bpp24`/`Bpp32` fallbacks use managed scratch buffers. On the direct path, EXIF mirror and 180-degree orientations avoid a full decoded-size buffer; only the four axis-swapping orientations need one.

## Requirements

- Package version `3.2.0-pre.2` (prerelease).
- .NET 10.
- The managed package plus exactly one native package for the target runtime. The native packages bundle libjpeg-turbo 3.2.0 for Windows x64/ARM64, Linux x64/ARM64, and macOS x64/ARM64. Linux binaries require glibc 2.17+ and do not support musl/Alpine; macOS requires 10.7+ on x64 and 11+ on ARM64.
- x86 is not supported.

## Installation

The managed package contains no native code. Add it together with exactly one native package that matches the deployment runtime:

```bash
dotnet add package TurboJpeg.Wrapper.Decoder --prerelease
dotnet add package TurboJpeg.Wrapper.Native.win-x64 --prerelease
```

| Runtime | Native package |
| --- | --- |
| `win-x64` | `TurboJpeg.Wrapper.Native.win-x64` |
| `win-arm64` | `TurboJpeg.Wrapper.Native.win-arm64` |
| `linux-x64` | `TurboJpeg.Wrapper.Native.linux-x64` |
| `linux-arm64` | `TurboJpeg.Wrapper.Native.linux-arm64` |
| `osx-x64`, `osx-arm64` | `TurboJpeg.Wrapper.Native.osx-universal` |

Packages are published as `3.2.0-pre.2` prereleases, so `--prerelease` is required. The package and assembly names are `TurboJpeg.Wrapper.Decoder`; the C# namespace is `TurboJpegWrapper.Decoder`.

If the matching native package is missing, the first decode throws `TurboJpegException` with the expected native library paths.

## Quick start

```csharp
using TurboJpegWrapper.Decoder;

byte[] jpeg = File.ReadAllBytes("photo.jpg");
byte[] bmp = TurboJpeg.DecodeToBmp(jpeg);
File.WriteAllBytes("photo.bmp", bmp);

byte[] bmp24 = TurboJpeg.DecodeToBmp(
    jpeg,
    new TurboJpegDecodeOptions { BmpBitDepth = TurboJpegBmpBitDepth.Bpp24 });
File.WriteAllBytes("photo-24.bmp", bmp24);

byte[] upright = TurboJpeg.DecodeToBmp(
    jpeg,
    new TurboJpegDecodeOptions { AutoOrient = true });
File.WriteAllBytes("photo-upright.bmp", upright);

byte[] cropped = TurboJpeg.DecodeToBmp(
    jpeg,
    new TurboJpegDecodeOptions { Crop = new TurboJpegCrop(0, 0, 320, 200) });
File.WriteAllBytes("photo-cropped.bmp", cropped);
```

The result is a complete BMP file, not raw pixels.

## API

### Static facade

`TurboJpeg` is thread-safe and rents decoders from an internal pool.

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

`GetBmpSize` performs the same header, EXIF, scaling, crop, and limit validation as decoding, so the destination buffer can be sized exactly. The span overloads return the number of bytes written and avoid allocating a new BMP array per call; pool and reuse the buffer when decoding many images. The destination must not overlap the JPEG source, because the decoder reads the JPEG while writing BMP bytes; an overlapping destination throws `ArgumentException`.

### Instance decoder

Use an instance when you want to keep one native handle on a thread. Instances are not thread-safe.

```csharp
using TurboJpegWrapper.Decoder;

using var decoder = new TurboJpegDecoder();
byte[] bmp = decoder.DecodeToBmp(jpeg);
TurboJpegImageInfo info = decoder.GetImageInfo(jpeg);

int size = decoder.GetBmpSize(jpeg);
byte[] buffer = new byte[size];
int written = decoder.DecodeToBmp(jpeg, TurboJpegDecodeOptions.Default, buffer);
```

All decode and size methods throw `ObjectDisposedException` after the decoder is disposed.

If a JPEG header parse fails, the decoder recreates its native handle. If recreation fails,
the decoder becomes disposed and the original exception is rethrown.

### Options

`TurboJpegDecodeOptions` is an immutable record; use `with` to derive modified options, for example
`TurboJpegDecodeOptions.Default with { AutoOrient = true }`. All limits are disabled by `0`.

| Option | Default | Meaning |
| --- | --- | --- |
| `ScalingFactor` | `1/1` | Output scaling. Every factor returned by `GetSupportedScalingFactors()` is accepted, including upscaling factors such as `2/1`. Unsupported factors throw `ArgumentException`. Lossless JPEG requires `1/1`. |
| `Crop` | `null` | Region of the scaled stored image to decode. See [Cropping](#cropping). |
| `FastDct` | `false` | Uses the fastest DCT/IDCT algorithm. |
| `FastUpsampling` | `false` | Uses the fastest chroma upsampling algorithm. |
| `MaxPixels` | `100_000_000` | Maximum number of source pixels; `0` means no limit. |
| `MaxMemoryMegabytes` | `0` | Maximum native intermediate memory in megabytes; `0` means no limit. Violations surface as `TurboJpegException`. |
| `ScanLimit` | `500` | Maximum number of progressive scans; `0` means no limit. |
| `BmpBitDepth` | `Bpp32` | BMP output depth: `Bpp8` (grayscale plus 256-entry palette), `Bpp24` (BGR), or `Bpp32` (BGRA). |
| `AutoOrient` | `false` | Applies the EXIF orientation stored in the JPEG, so the BMP is upright. Orientations that swap the axes also transpose the BMP width and height. |
| `StopOnWarning` | `true` | Treats a native warning as an error. See [Warnings](#warnings). |

`MaxPixels` and the maximum array length are checked before the BMP buffer is allocated. A smaller scaling factor does not relax the source-pixel limit.

### Cropping

`TurboJpegDecodeOptions.Crop` decodes a rectangular region instead of the full image. Coordinates refer to the scaled, stored image, not to the auto-oriented output:

- `ScalingFactor` is applied first, then the crop region is applied to the scaled image.
- `AutoOrient` transforms the cropped pixels afterward. Orientations 5-8 swap the BMP width and height of the crop.
- `X` must be divisible by the scaled iMCU width; `Y` is not required to be iMCU-aligned.
- The region must fit inside the scaled image and must have a positive width and height.
- Cropping is supported only for lossy JPEGs with a known chroma subsampling scheme.

The native iMCU width is 8 for 4:4:4, 4:4:0, 4:4:1, and grayscale; 16 for 4:2:2, 4:2:0, and 2x4; and 32 for 4:1:1 and 4:1:0. The scaled iMCU width is `ceil(native iMCU width * scaling numerator / scaling denominator)`.

Invalid crops throw `ArgumentException`: a region outside the scaled image, a horizontally misaligned region, a lossless JPEG, or an unknown subsampling scheme. `GetBmpSize` returns the cropped BMP size.

### Precision and CMYK

BMP output always uses 8-bit channels. JPEG precision 2-16 is accepted:

- Precision 8 samples pass through unchanged.
- Precisions 2-7, 9-11, and 13-15 use exact lookup tables that map the native sample range to 0-255.
- Precisions 12 and 16 use SIMD narrowing. The result is within 1 LSB of exact rounding for every sample.

TurboJPEG cannot convert some lossless colorspaces directly, so the wrapper provides two managed fallbacks:

- Lossless RGB to `Bpp8`: the image is decoded as BGR, downconverted to 8 bits, and converted to luma with `(77 R + 150 G + 29 B + 128) >> 8`.
- Lossless GRAY to `Bpp24` or `Bpp32`: the image is decoded as GRAY, downconverted, and expanded to `B = G = R = sample` with alpha 255.

Both fallbacks work at every precision from 2 to 16 and with `AutoOrient`.

CMYK and YCCK are decoded as four-channel CMYK and converted best-effort to BGR, BGRA, or luma. Stored samples are always treated as inverted Adobe CMYK, regardless of the Adobe APP14 marker: `R = round(C * K / 255)`, `G = round(M * K / 255)`, and `B = round(Y * K / 255)`, where `C`, `M`, `Y`, and `K` are the stored samples. A non-Adobe producer that stores non-inverted CMYK will render with inverted colors. `Bpp8` output is the luma of the converted BGR values.

The wrapper does not perform ICC color management. CMYK-to-RGB is arithmetic best-effort conversion, not colorimetric conversion.

### Warnings

Truncated entropy data and extraneous bytes can make TurboJPEG report a warning instead of a fatal error.

- With the default `StopOnWarning = true`, a warning is reported as a non-fatal `TurboJpegException` (`IsFatal = false`) and no BMP is returned.
- With `StopOnWarning = false`, native warnings are ignored and a best-effort BMP is returned. The native destination is zero-filled before decoding, so bytes the decoder does not write are deterministic zeros instead of stale pool or caller memory.
- Fatal errors throw in both modes.
- `GetImageInfo` always treats warnings as errors.

### Memory

`MaxMemoryMegabytes` limits native TurboJPEG allocations only. The returned BMP, the EXIF auto-orient scratch buffer, and the precision and CMYK conversion scratch buffers are managed and are not counted against it.

Managed scratch is checked before allocation. A 16-bit sample scratch requires `2 * samples` bytes, and byte scratch requires 1, 3, or 4 bytes per pixel depending on the pipeline. If the BMP or any scratch buffer would exceed `Array.MaxLength`, the call throws `TurboJpegImageTooLargeException` before allocating it.

## BMP output

- Complete BMP file with a 54-byte `BITMAPINFOHEADER`; 8-bit output adds a 1024-byte palette.
- 8 bits per pixel: one grayscale byte per pixel with a 256-entry grayscale palette. Color is discarded, so the result is always grayscale.
- 24 bits per pixel: BGR byte order.
- 32 bits per pixel: BGRA byte order with alpha always 255.
- Bottom-up rows (`biHeight` is positive), `BI_RGB` compression.
- Rows are padded to a 4-byte boundary: `stride = (width * bytesPerPixel + 3) & ~3`. Padding bytes are zero; 32-bit rows need no padding.
- Pixel data starts at 54 bytes for 24/32 bpp and at 1078 bytes for 8 bpp.
- `biSizeImage` is `stride * height`; the file size is `pixelOffset + stride * height`.
- `biXPelsPerMeter` and `biYPelsPerMeter` are derived from the JFIF density when the JPEG stores it in dots per inch or dots per centimeter; otherwise they default to 2835 (72 DPI). `TurboJpegImageInfo` exposes the raw `DensityX`, `DensityY`, and `DensityUnits` values.
- With `AutoOrient`, the header describes the upright image; orientations 5-8 transpose the width and height.
- With `Crop`, the header describes the cropped image; orientations 5-8 transpose the crop dimensions.

## Supported inputs

- JPEG precision from 2 to 16 bits, including 12-bit lossy extended sequential.
- RGB, YCbCr, grayscale, CMYK, and YCCK color spaces.
- Baseline, extended sequential, progressive, arithmetic, and lossless entropy coding.
- Any chroma subsampling scheme reported by TurboJPEG; cropping requires a known scheme.
- Lossless JPEG at `1/1` scaling.

A JPEG whose precision is outside 2-16 bits is rejected with `TurboJpegUnsupportedException`.

The wrapper does not perform ICC color management. Without `AutoOrient`, decoded dimensions are the stored JPEG dimensions; with `Crop`, they are the crop dimensions.

With `AutoOrient`, the orientation tag (`0x0112`) from the first EXIF APP1 segment is applied. All eight EXIF orientation values are supported, so the result may be rotated or mirrored. Missing, malformed, or out-of-range EXIF orientation is ignored and the image is decoded unchanged; malformed metadata never fails decoding. EXIF is read from the JPEG bytes before decoding, so no additional native metadata API is used.

## Errors

- `ArgumentException`: empty input, invalid options, an unsupported scaling factor, an invalid crop region, a destination buffer that is too small, or a destination buffer that overlaps the JPEG source.
- `ArgumentNullException`: a null options object passed to the static or instance options overload.
- `ArgumentOutOfRangeException`: a negative crop offset, a non-positive crop dimension, or an invalid BMP bit depth.
- `TurboJpegUnsupportedException`: the image precision is outside 2-16 bits.
- `TurboJpegImageTooLargeException`: `MaxPixels` is exceeded, or the BMP or a decode scratch buffer would exceed `Array.MaxLength`.
- `TurboJpegException`: a native TurboJPEG error, a native-library load failure, or a native warning while `StopOnWarning` is `true`.
- `ObjectDisposedException`: an instance method was called after `Dispose`.
- `PlatformNotSupportedException`: the current OS/architecture has no known runtime identifier and no explicit native path is configured.

## Native library loading

Native libraries are distributed as separate runtime packages. Each native package contains one asset:

| Runtime | Asset |
| --- | --- |
| `win-x64` | `runtimes/win-x64/native/turbojpeg.dll` |
| `win-arm64` | `runtimes/win-arm64/native/turbojpeg.dll` |
| `linux-x64` | `runtimes/linux-x64/native/libturbojpeg.so` |
| `linux-arm64` | `runtimes/linux-arm64/native/libturbojpeg.so` |
| `osx-x64`, `osx-arm64` | `runtimes/osx/native/libturbojpeg.dylib` |

The macOS library is a universal binary (x86_64 + arm64) supplied once by the `TurboJpeg.Wrapper.Native.osx-universal` package. The loader resolves the base `osx` runtime folder on both macOS architectures.

The bundled Linux libraries are glibc builds; for musl/Alpine, supply a musl build through `TURBOJPEG_NATIVE_LIBRARY_PATH`.

Set `TURBOJPEG_NATIVE_LIBRARY_PATH` to the full path of a native library file to override discovery. When the variable is set to a non-blank value, it is the only location used; an invalid path fails immediately instead of falling back.

Otherwise the resolver tries, in order:

1. `runtimes/{rid}/native/{library}` under `AppContext.BaseDirectory`.
2. On macOS only, `runtimes/osx/native/{library}` under `AppContext.BaseDirectory`.
3. `{library}` in `AppContext.BaseDirectory`.
4. The operating system's normal library search.

The resolver knows these runtime identifiers on Windows, Linux, and macOS: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, and `osx-arm64`. x86 is not supported. To add a different runtime, place the matching library under `runtimes/{rid}/native/` and include it in a native package.

## Threading and performance

- `TurboJpeg` is safe for concurrent use; each operation rents an exclusive decoder and returns it afterward.
- `TurboJpegDecoder` owns one native handle and is not thread-safe. Use one instance per thread or synchronize access.
- The lossy 8-bit non-CMYK path, and lossless inputs whose BMP depth matches the native pixel format, writes directly into the final BMP buffer. High-precision, CMYK, and lossless fallback paths decode into managed scratch buffers and convert into the BMP, so they are slower than the direct 8-bit path.
- On the direct path with `AutoOrient`, orientations 1-4 write directly into the BMP; 24-bit mirroring uses one pooled row buffer. Orientations 5-8 rent a decoded-size scratch buffer from the shared array pool and transpose it into the BMP.
- The 24-bit row mirror uses a portable `Vector128` implementation on both architectures. Axis-swapping transposes use SSE2/SSSE3 implementations on x64 and portable `Vector128` implementations on ARM64. 12/16-bit downconversion, CMYK conversion, grayscale expansion, and the managed luma fallback use SIMD kernels with scalar tails.
- Cropping reduces decode and conversion work in proportion to the cropped output size.
- The span decode overloads let callers reuse a buffer and avoid allocating a BMP array per operation.
- Supported scaling factors are cached after the first successful query.

## Package contents and license

The package contains the managed `net10.0` assembly, `README.md`, `LICENSE`, and `THIRD-PARTY-NOTICES.md`. It contains no native assets and has no package dependencies; add exactly one native package from the table above.

The source code is licensed under the MIT License (`LICENSE`). The native libraries distributed through the companion native packages are libjpeg-turbo 3.2.0, licensed under BSD-3-Clause with the Independent JPEG Group and zlib notices (`THIRD-PARTY-NOTICES.md`). Both files are included in this package.
