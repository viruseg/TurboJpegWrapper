# TurboJpeg.Wrapper.Decoder

A production-oriented .NET 10 wrapper around the libjpeg-turbo TurboJPEG API for decoding JPEG images to complete 8-, 24-, or 32-bit BMP files.

The library is decode-only. It does not encode JPEGs or expose YUV buffers. TurboJPEG writes grayscale, BGR, or BGRA pixels directly into the final BMP allocation. With EXIF auto-orientation, mirror and 180-degree orientations avoid a full decoded-size buffer; only the four axis-swapping orientations need one.

## Requirements

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

Packages are published as prereleases, so `--prerelease` is required. The package and assembly names are `TurboJpeg.Wrapper.Decoder`; the C# namespace is `TurboJpegWrapper.Decoder`.

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

`GetBmpSize` performs the same header, EXIF, scaling, and limit validation as decoding, so the destination buffer can be sized exactly. The span overloads return the number of bytes written and avoid allocating a new BMP array per call; pool and reuse the buffer when decoding many images.

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
| `FastDct` | `false` | Uses the fastest DCT/IDCT algorithm. |
| `FastUpsampling` | `false` | Uses the fastest chroma upsampling algorithm. |
| `MaxPixels` | `100_000_000` | Maximum number of source pixels; `0` means no limit. |
| `MaxMemoryMegabytes` | `0` | Maximum native intermediate memory in megabytes; `0` means no limit. Violations surface as `TurboJpegException`. |
| `ScanLimit` | `500` | Maximum number of progressive scans; `0` means no limit. |
| `BmpBitDepth` | `Bpp32` | BMP output depth: `Bpp8` (grayscale plus 256-entry palette), `Bpp24` (BGR), or `Bpp32` (BGRA). |
| `AutoOrient` | `false` | Applies the EXIF orientation stored in the JPEG, so the BMP is upright. Orientations that swap the axes also transpose the BMP width and height. |

`MaxPixels` and the maximum array length are checked before the BMP buffer is allocated. A smaller scaling factor does not relax the source-pixel limit.

`MaxMemoryMegabytes` limits native TurboJPEG allocations only. The returned BMP array and, for orientations 5-8, the managed auto-orient scratch buffer are not counted against it.

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

## Supported inputs

- 8-bit lossy JPEG, including progressive JPEG.
- 8-bit lossless JPEG at `1/1` scaling.
- Grayscale JPEG; channels are expanded to equal BGR/BGRA values, or used directly at 8 bpp.
- Images with any supported TurboJPEG chroma subsampling.

The following are rejected with `TurboJpegUnsupportedException`:

- 12-bit and other non-8-bit JPEG precision.
- CMYK and YCCK JPEG.

The wrapper does not perform ICC color management. Without `AutoOrient`, decoded dimensions are the stored JPEG dimensions.

With `AutoOrient`, the orientation tag (`0x0112`) from the first EXIF APP1 segment is applied. All eight EXIF orientation values are supported, so the result may be rotated or mirrored. Missing, malformed, or out-of-range EXIF orientation is ignored and the image is decoded unchanged; malformed metadata never fails decoding. EXIF is read from the JPEG bytes before decoding, so no additional native metadata API is used.

## Errors

- `ArgumentException`: empty input, invalid options, an unsupported scaling factor, or a destination buffer that is too small.
- `ArgumentNullException`: a null options object passed to the static or instance options overload.
- `TurboJpegUnsupportedException`: precision or color space cannot be converted to BMP.
- `TurboJpegImageTooLargeException`: `MaxPixels` is exceeded or the BMP would exceed `Array.MaxLength`.
- `TurboJpegException`: a native TurboJPEG error or a native-library load failure.
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
- Decoding writes directly into the final BMP buffer for the selected depth. With `AutoOrient`, orientations 1-4 write directly into the BMP; 24-bit mirroring uses one pooled row buffer. Orientations 5-8 rent a decoded-size scratch buffer from the shared array pool and transpose it into the BMP.
- The 24-bit row mirror uses a portable `Vector128` implementation on both architectures. Axis-swapping transposes use SSE2/SSSE3 implementations on x64 and portable `Vector128` implementations on ARM64.
- The span decode overloads let callers reuse a buffer; a 12 MP 32-bit decode measured 0 managed bytes per operation versus 48 MB for the `byte[]` overload in the external harness.
- Supported scaling factors are cached after the first successful query.

## Packaging

Build all six packages with:

```bash
bash scripts/pack-packages.sh
```

The script writes to `artifacts/packages` and produces version `3.2.0-pre.2`, which comes from `Directory.Build.props`:

- `TurboJpeg.Wrapper.Decoder.3.2.0-pre.2.nupkg`: managed assembly, `README.md`, `LICENSE`, and `THIRD-PARTY-NOTICES.md`; no native assets and no native package dependencies.
- `TurboJpeg.Wrapper.Native.win-x64.3.2.0-pre.2.nupkg`
- `TurboJpeg.Wrapper.Native.win-arm64.3.2.0-pre.2.nupkg`
- `TurboJpeg.Wrapper.Native.linux-x64.3.2.0-pre.2.nupkg`
- `TurboJpeg.Wrapper.Native.linux-arm64.3.2.0-pre.2.nupkg`
- `TurboJpeg.Wrapper.Native.osx-universal.3.2.0-pre.2.nupkg`

## License and notices

The source code in this repository is licensed under the MIT License. See [LICENSE](LICENSE); the same file is included in every package.

The bundled native libraries are libjpeg-turbo 3.2.0, distributed under the BSD-3-Clause license and the Independent JPEG Group / zlib notices. See [native/THIRD-PARTY-NOTICES.md](native/THIRD-PARTY-NOTICES.md); the same file is included in the managed package and in every native package.
