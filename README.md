# TurboJpegWrapper

A production-oriented .NET 10 wrapper around the libjpeg-turbo TurboJPEG API for decoding JPEG images to complete 8-, 24-, or 32-bit BMP files.

The library is decode-only. It does not encode JPEGs or expose YUV buffers. TurboJPEG writes grayscale, BGR, or BGRA pixels directly into the final BMP allocation. With EXIF auto-orientation, mirror and 180-degree orientations are applied in place; only the four axis-swapping orientations need a temporary decoded-size buffer.

## Requirements

- .NET 10.
- Windows x64 for the bundled native binary (`turbojpeg.dll` 3.2.0).
- The resolver also recognizes Linux and macOS runtime identifiers; see [Native library loading](#native-library-loading).

## Quick start

```csharp
using TurboJpegWrapper;

byte[] jpeg = File.ReadAllBytes("photo.jpg");
byte[] bmp = TurboJpeg.DecodeToBmp(jpeg);
File.WriteAllBytes("photo.bmp", bmp);

byte[] bmp24 = TurboJpeg.DecodeToBmp(
    jpeg,
    new TurboJpegDecodeOptions(bmpBitDepth: TurboJpegBmpBitDepth.Bpp24));
File.WriteAllBytes("photo-24.bmp", bmp24);

byte[] upright = TurboJpeg.DecodeToBmp(
    jpeg,
    new TurboJpegDecodeOptions(autoOrient: true));
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
    new TurboJpegDecodeOptions(
        scalingFactor: new TurboJpegScalingFactor(1, 2)));

TurboJpegImageInfo info = TurboJpeg.GetImageInfo(jpeg);
ReadOnlySpan<TurboJpegScalingFactor> factors = TurboJpeg.GetSupportedScalingFactors();
```

### Instance decoder

Use an instance when you want to keep one native handle on a thread. Instances are not thread-safe.

```csharp
using TurboJpegWrapper;

using var decoder = new TurboJpegDecoder();
byte[] bmp = decoder.DecodeToBmp(jpeg);
TurboJpegImageInfo info = decoder.GetImageInfo(jpeg);
```

Both `DecodeToBmp` overloads and `GetImageInfo` throw `ObjectDisposedException` after the decoder is disposed.

If a JPEG header parse fails, the decoder recreates its native handle. If recreation fails,
the decoder becomes disposed and the original exception is rethrown.

### Options

`TurboJpegDecodeOptions` is immutable. All limits are disabled by `0`.

| Option | Default | Meaning |
| --- | --- | --- |
| `ScalingFactor` | `1/1` | Output scaling. Only exact factors returned by `GetSupportedScalingFactors()` are accepted. Upscaling and unsupported factors throw `ArgumentException`. Lossless JPEG requires `1/1`. |
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

- `ArgumentException`: empty input, invalid options, or an unsupported scaling factor.
- `ArgumentNullException`: a null options object passed to the static or instance options overload.
- `TurboJpegUnsupportedException`: precision or color space cannot be converted to BMP.
- `TurboJpegImageTooLargeException`: `MaxPixels` is exceeded or the BMP would exceed `Array.MaxLength`.
- `TurboJpegException`: a native TurboJPEG error or a native-library load failure.
- `ObjectDisposedException`: an instance method was called after `Dispose`.
- `PlatformNotSupportedException`: the current OS/architecture has no known runtime identifier and no explicit native path is configured.

## Native library loading

The package bundles `runtimes/win-x64/native/turbojpeg.dll`.

Set `TURBOJPEG_NATIVE_LIBRARY_PATH` to the full path of a native library file to override discovery. When the variable is set to a non-blank value, it is the only location used; an invalid path fails immediately instead of falling back.

Otherwise the resolver tries, in order:

1. `runtimes/{rid}/native/{library}` under `AppContext.BaseDirectory`.
2. `{library}` in `AppContext.BaseDirectory`.
3. The operating system's normal library search.

The resolver knows these x64 runtime identifiers: `win-x64`, `linux-x64`, and `osx-x64`. Only `win-x64` is bundled today. To add another platform, place the matching library under `runtimes/{rid}/native/` and include it in the package. x86 and ARM64 are not supported.

## Threading and performance

- `TurboJpeg` is safe for concurrent use; each operation rents an exclusive decoder and returns it afterward.
- `TurboJpegDecoder` owns one native handle and is not thread-safe. Use one instance per thread or synchronize access.
- Decoding writes directly into the final BMP buffer for the selected depth. With `AutoOrient`, orientations 1-4 decode directly into the BMP and are mirrored in place when needed; orientations 5-8 rent a decoded-size scratch buffer from the shared array pool and transpose it into the BMP.
- Supported scaling factors are cached after the first successful query.

## Packaging

Build a package with:

```bash
dotnet pack src/TurboJpegWrapper/TurboJpegWrapper.csproj -c Release -o artifacts
```

The package contains `runtimes/win-x64/native/turbojpeg.dll` and `THIRD-PARTY-NOTICES.md`.

## License and notices

The bundled native library is libjpeg-turbo 3.2.0, distributed under the BSD-3-Clause license and the Independent JPEG Group / zlib notices. See [native/THIRD-PARTY-NOTICES.md](native/THIRD-PARTY-NOTICES.md); the same file is included in the NuGet package.
