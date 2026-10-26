# TurboJpeg.Wrapper.Encoder

Version `3.2.0-pre.2` (prerelease)

A production-oriented .NET 10 wrapper around the libjpeg-turbo 3.2.0 TurboJPEG API for encoding uncompressed BMP images to JPEG.

The package is encode-only. For JPEG-to-BMP decoding, use the separate `TurboJpeg.Wrapper.Decoder` package. The managed assembly contains no native code; add exactly one native package that matches the deployment runtime.

## Requirements

- .NET 10 (`net10.0`).
- The managed package plus exactly one native package for the target runtime. The native packages bundle libjpeg-turbo 3.2.0 for Windows x64/ARM64, Linux x64/ARM64, and macOS x64/ARM64. Linux binaries require glibc 2.17+ and do not support musl/Alpine; macOS requires 10.7+ on x64 and 11+ on ARM64.
- x86 is not supported.

## Installation

The managed package contains no native code. Add it together with exactly one native package that matches the deployment runtime:

```bash
dotnet add package TurboJpeg.Wrapper.Encoder --prerelease
dotnet add package TurboJpeg.Wrapper.Native.win-x64 --prerelease
```

| Runtime | Native package |
| --- | --- |
| `win-x64` | `TurboJpeg.Wrapper.Native.win-x64` |
| `win-arm64` | `TurboJpeg.Wrapper.Native.win-arm64` |
| `linux-x64` | `TurboJpeg.Wrapper.Native.linux-x64` |
| `linux-arm64` | `TurboJpeg.Wrapper.Native.linux-arm64` |
| `osx-x64`, `osx-arm64` | `TurboJpeg.Wrapper.Native.osx-universal` |

Packages are published as `3.2.0-pre.2` prereleases, so `--prerelease` is required. The package and assembly names are `TurboJpeg.Wrapper.Encoder`; the C# namespace is `TurboJpegWrapper.Encoder`.

If the matching native package is missing, the first encoder creation throws `TurboJpegException` with the expected native library paths.

## Quick start

```csharp
using TurboJpegWrapper.Encoder;

byte[] jpeg = TurboJpeg.EncodeToJpeg(File.ReadAllBytes("photo.bmp"));
File.WriteAllBytes("photo.jpg", jpeg);
```

The input is a complete BMP file, not raw pixel data, and the result is a complete JPEG file.

Use options for quality, chroma subsampling, lossless output, and the other settings described below:

```csharp
byte[] smaller = TurboJpeg.EncodeToJpeg(
    File.ReadAllBytes("photo.bmp"),
    new TurboJpegEncodeOptions { Quality = 80 });

byte[] lossless = TurboJpeg.EncodeToJpeg(
    File.ReadAllBytes("photo.bmp"),
    new TurboJpegEncodeOptions { Lossless = true });
```

## API

### Static facade

`TurboJpeg` is thread-safe. Each call rents an encoder from an internal pool and returns it when the call completes.

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

`GetMaxJpegSize` parses and validates the BMP and returns the worst-case JPEG size that TurboJPEG can produce for the image and effective subsampling. It uses the same validation as encoding and is the correct way to size a destination buffer; the actual JPEG is usually smaller.

The span overload requires `destination.Length` to be at least `GetMaxJpegSize` and throws `ArgumentException` otherwise. It writes the JPEG directly into the caller's buffer and returns the number of bytes written. The required destination length is the worst-case maximum, not the final encoded length. The destination must not overlap the BMP source; the zero-copy pixel path may read the source while the native encoder writes JPEG bytes, so an overlapping destination is rejected with `ArgumentException`.

### Instance encoder

Use an instance when you want to keep one native handle on a thread. `TurboJpegEncoder` accepts optional default options for the parameterless overloads. Instances are not thread-safe.

```csharp
using TurboJpegWrapper.Encoder;

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

Explicit options replace the instance defaults for that call. After `Dispose`, all instance methods throw `ObjectDisposedException`.

If a native encode fails, the instance recreates its native handle so it remains usable. Recovery is best-effort: if recreation also fails, the instance becomes disposed and the original exception is rethrown.

### Options

`TurboJpegEncodeOptions` is an immutable record. Use `with` to derive modified options, for example `TurboJpegEncodeOptions.Default with { Quality = 75 }`.

| Option | Default | Meaning |
| --- | --- | --- |
| `Quality` | `90` | JPEG quality from 1 (lowest) to 100 (highest). Ignored when `Lossless` is `true`. |
| `Subsampling` | `Yuv420` | Chroma subsampling: `Yuv444`, `Yuv422`, `Yuv420`, `Grayscale`, `Yuv440`, `Yuv411`, `Yuv441`, `Yuv410`, or `Yuv24`. `Unknown` is invalid. Ignored when `Lossless` is `true` or `Colorspace` is `Rgb`; RGB output always uses 4:4:4. An explicit RGB colorspace takes precedence over `Subsampling = Grayscale`. |
| `Colorspace` | `Default` | `Default` resolves to `YCbCr` for colour input; `Rgb`, `YCbCr`, and `Grayscale` can be requested explicitly. `Rgb` output always uses 4:4:4 regardless of `Subsampling`. `Grayscale`, or `Subsampling = Grayscale` when `Colorspace` is not explicitly `Rgb`, produces grayscale output; explicit `Rgb` wins. Ignored when `Lossless` is `true`. |
| `FastDct` | `false` | Uses the fastest DCT/IDCT algorithm. Ignored when `Lossless` is `true`. |
| `OptimizeCoding` | `false` | Generates optimal Huffman coding tables. Ignored when `Lossless` is `true`. |
| `Progressive` | `false` | Produces a progressive JPEG. Ignored when `Lossless` is `true`. |
| `ArithmeticCoding` | `false` | Uses arithmetic entropy coding. Ignored when `Lossless` is `true`. |
| `Lossless` | `false` | Enables lossless JPEG compression. Only `LosslessPredictor`, `LosslessPointTransform`, `RestartRows`, `MaxMemoryMegabytes`, and `StopOnWarning` apply; the lossy options are ignored. A non-zero `RestartBlocks` is rejected, and `Density` must be `null`. |
| `LosslessPredictor` | `1` | Lossless predictor selector from 1 through 7. Applies only when `Lossless` is `true`. |
| `LosslessPointTransform` | `0` | Lossless point transform from 0 through 7. Applies only when `Lossless` is `true`. A value greater than zero right-shifts the samples, so the encoded image is no longer bit-exact. |
| `RestartBlocks` | `0` | Number of MCUs between restart markers, from 0 through 65535; `0` disables restart markers. Applies only to lossy compression. Must be `0` with `Lossless`, and cannot be combined with a non-zero `RestartRows`. |
| `RestartRows` | `0` | Number of MCU rows between restart markers, from 0 through 65535; `0` disables restart markers. Applies to lossy and lossless compression and cannot be combined with a non-zero `RestartBlocks`. |
| `MaxMemoryMegabytes` | `0` | Maximum native intermediate memory in megabytes; `0` means no limit. Violations surface as `TurboJpegException`. |
| `StopOnWarning` | `false` | Treats a TurboJPEG warning as an encoding failure instead of continuing. |
| `Density` | `null` | Pixel density written to the JPEG header. `null` derives the density from the BMP as described below. Applies only to lossy YCbCr and grayscale output; explicit density is rejected with lossless or RGB output. |

Range and enum validation runs when a value is assigned, including values that `Lossless` later ignores. For example, `Quality = 0` throws even when `Lossless = true`.

Cross-property validation runs when encoding or sizing starts: `RestartBlocks` and `RestartRows` cannot both be non-zero, `RestartBlocks` must be zero with `Lossless = true`, and `Density` cannot be specified with `Lossless = true` or `Colorspace = Rgb`. All violations throw `ArgumentException` with parameter name `options`.

RGB output is never subsampled: `Colorspace = Rgb` forces 4:4:4 and ignores `Subsampling`. Combinations that depend on other libjpeg-turbo semantics are rejected by the native library and surface as `TurboJpegException`.

## BMP input support

The encoder accepts complete BMP files with DIB header sizes 40, 52, 56, 108, or 124, in bottom-up form (positive height) or top-down form (negative height). Supported bit counts are 1, 4, 8, 16, 24, and 32.

| BMP layout | Handling | Path |
| --- | --- | --- |
| 24-bit `BI_RGB` | Passed to TurboJPEG as BGR | Zero-copy |
| 32-bit `BI_RGB` | Passed to TurboJPEG as BGRX; the fourth byte is ignored | Zero-copy |
| 32-bit `BI_BITFIELDS` with masks `00FF0000`/`0000FF00`/`000000FF` | Passed to TurboJPEG as BGRX; alpha is ignored | Zero-copy |
| 8-bit `BI_RGB` with a full 256-entry identity grayscale palette and grayscale output requested | Palette indices used directly as gray samples | Zero-copy |
| 8-bit grayscale palette with grayscale output requested | Palette lookup to 8-bit gray | Pooled conversion |
| 8-bit colour palette, or grayscale output not requested | Palette lookup to BGR | Pooled conversion |
| 1- or 4-bit grayscale palette with grayscale output requested | Expand to 8-bit gray | Pooled conversion |
| 1- or 4-bit palette otherwise | Expand to BGR | Pooled conversion |
| 16-bit `BI_RGB` (X1R5G5B5) or `BI_BITFIELDS` (including RGB565) | Extract and scale channels to BGR | Pooled conversion |
| 32-bit `BI_BITFIELDS` with other masks | Extract and scale channels to BGR; alpha is ignored | Pooled conversion |

"Grayscale output requested" means `Colorspace = Grayscale`, or `Subsampling = Grayscale` when `Colorspace` is not explicitly `Rgb` (explicit `Rgb` wins and forces 4:4:4), and it applies only to lossy encoding. Lossless mode ignores both options and does not subsample or convert colour to grayscale. In lossy mode, colour palettes always expand to BGR even when grayscale output is requested; native TurboJPEG performs the colour-to-gray conversion.

Palette images use `clrUsed` entries when it is non-zero, otherwise `1 << bitCount` entries. Palette indices beyond the declared entries are treated as black. Alpha and colour management are not applied.

The following BMP variants are rejected:

- OS/2 core bitmaps (DIB header size 12) and any DIB header size other than 40, 52, 56, 108, or 124, with `TurboJpegUnsupportedException`.
- RLE4- or RLE8-compressed bitmaps, `BI_JPEG`, `BI_PNG`, and any compression other than `BI_RGB`/`BI_BITFIELDS`, with `TurboJpegUnsupportedException`.
- `BI_BITFIELDS` on a 24-bit bitmap, or bitfield masks that are empty, non-contiguous, or overlapping, with `TurboJpegUnsupportedException`.
- Malformed or truncated files, including a missing `BM` signature, a non-positive width, a zero or `int.MinValue` height, invalid planes or bit counts, palette or pixel offsets outside the buffer, and truncated pixel data, with `ArgumentException`.

## Density

`Density` is a `TurboJpegDensity` value with `X` and `Y` between 1 and 65535 (the unsigned 16-bit range of the JPEG density fields) and `PixelsPerInch`, `PixelsPerCentimeter`, or `Unknown` units. The `default` struct (0 x 0) is not a valid density.

JPEG density is stored only in lossy YCbCr and grayscale files. Lossless JPEG output is RGB, and `Colorspace = Rgb` produces RGB output; neither can store a density. In those modes an explicit `Density` is rejected when encoding or sizing starts with `ArgumentException` (parameter name `options`), and BMP density is ignored: the encoder applies 1 x 1 with `Unknown` units.

When `Density` is `null` and the output is lossy YCbCr or grayscale, the encoder derives the density from the BMP's `biXPelsPerMeter` and `biYPelsPerMeter` values:

- A non-zero BMP density is converted to pixels per inch with `dpi = clamp(round(ppm * 0.0254), 1, 65535)` and written with `PixelsPerInch` units.
- If only one BMP axis is non-zero, the same value is used for both axes.
- If the BMP stores no density, the encoder applies 1 x 1 with `Unknown` units explicitly. This is not a 72 DPI value.

In lossy YCbCr and grayscale modes, an explicit density overrides the BMP values and is applied verbatim:

```csharp
var options = new TurboJpegEncodeOptions
{
    Density = new TurboJpegDensity(300, 300, TurboJpegDensityUnits.PixelsPerInch),
};
```

Lossless and RGB outputs ignore the BMP density, so the same BMP can encode with an explicit 96 DPI density in YCbCr mode and with 1 x 1 `Unknown` density after switching to lossless or RGB.

## Performance and allocations

- Uncompressed 24-bit and 32-bit BMPs in the layouts marked zero-copy above are passed directly to TurboJPEG. No managed pixel buffer is allocated and no pixel copy is made.
- Other BMP layouts are converted once into a buffer rented from `ArrayPool<byte>.Shared`; the buffer is returned after encoding.
- The `byte[]`-returning overloads compress into a pooled worst-case JPEG buffer, allocate the exact-size result array, and copy the encoded bytes once. This is the "one copy" output path.
- The span overload writes the JPEG directly into the caller's destination and makes no output copy. Combined with a zero-copy BMP layout, neither the input pixels nor the output JPEG are copied.
- `GetMaxJpegSize` lets callers allocate or reuse a destination without allocating a worst-case temporary buffer. The returned size is a worst case and can be considerably larger than the final JPEG.
- The static facade pools native encoder handles. Converted pixel buffers and the temporary worst-case output buffer are pooled as well, so repeated encodes avoid repeated large allocations.

## Errors

- `ArgumentException`: empty input, a malformed or truncated BMP, invalid option combinations (both restart options non-zero, a non-zero `RestartBlocks` with lossless encoding, or an explicit `Density` with lossless or RGB output), a destination buffer that is too small, or a destination buffer that overlaps the BMP source.
- `ArgumentNullException`: a null options object passed to an options overload.
- `ArgumentOutOfRangeException`: an option or density value is outside its allowed range or is not a defined enum value.
- `TurboJpegUnsupportedException`: a valid BMP uses an encoding this library does not support.
- `TurboJpegImageTooLargeException`: the worst-case JPEG size, or a converted pixel buffer, would exceed `Array.MaxLength`.
- `TurboJpegException`: a native TurboJPEG error or a native-library load failure. `IsFatal` reports whether the native error is fatal.
- `ObjectDisposedException`: an instance method was called after `Dispose`.
- `PlatformNotSupportedException`: the current OS/architecture has no known runtime identifier and no explicit native path is configured.

## Threading and lifetime

- `TurboJpeg` is safe for concurrent use. Each operation rents an exclusive encoder and returns it afterward, and a failed call does not poison the pool.
- `TurboJpegEncoder` owns one native handle and is not thread-safe. Use one instance per thread or synchronize access.
- `TurboJpegEncodeOptions` is immutable, so one options instance can be shared across threads and calls.
- An instance becomes unusable after `Dispose`; disposal releases its native handle. The static facade never exposes disposed instances.
- Native failures recreate the instance handle when possible, preserving the original exception if recovery fails.

## Native library loading

Native libraries are distributed as separate runtime packages. The same five packages serve `TurboJpeg.Wrapper.Decoder` and `TurboJpeg.Wrapper.Encoder`:

| Runtime | Native package | Asset |
| --- | --- | --- |
| `win-x64` | `TurboJpeg.Wrapper.Native.win-x64` | `runtimes/win-x64/native/turbojpeg.dll` |
| `win-arm64` | `TurboJpeg.Wrapper.Native.win-arm64` | `runtimes/win-arm64/native/turbojpeg.dll` |
| `linux-x64` | `TurboJpeg.Wrapper.Native.linux-x64` | `runtimes/linux-x64/native/libturbojpeg.so` |
| `linux-arm64` | `TurboJpeg.Wrapper.Native.linux-arm64` | `runtimes/linux-arm64/native/libturbojpeg.so` |
| `osx-x64`, `osx-arm64` | `TurboJpeg.Wrapper.Native.osx-universal` | `runtimes/osx/native/libturbojpeg.dylib` |

The macOS library is a universal binary (x86_64 + arm64) supplied once by the `osx-universal` package. The loader resolves the base `osx` runtime folder on both macOS architectures.

The bundled Linux libraries are glibc builds. For musl/Alpine, supply a musl libjpeg-turbo build through `TURBOJPEG_NATIVE_LIBRARY_PATH`.

Set `TURBOJPEG_NATIVE_LIBRARY_PATH` to the full path of a native library file to override discovery. When the variable is set to a non-blank value, it is the only location used; an invalid path fails immediately instead of falling back.

Otherwise the resolver tries, in order:

1. `runtimes/{rid}/native/{library}` under `AppContext.BaseDirectory`.
2. On macOS only, `runtimes/osx/native/{library}` under `AppContext.BaseDirectory`.
3. `{library}` in `AppContext.BaseDirectory`.
4. The operating system's normal library search.

The resolver knows `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, and `osx-arm64`. x86 is not supported. The native library is loaded when an encoder handle is created, either by constructing `TurboJpegEncoder` or by the first static call. A load failure is reported as `TurboJpegException` with the attempted paths; an unsupported OS/architecture without an explicit path throws `PlatformNotSupportedException`.

## Benchmark note

The repository includes an encoder benchmark that compares `TurboJpeg.EncodeToJpeg` with GDI+ and ImageSharp. All three use quality 90: the wrapper uses `TurboJpegEncodeOptions.Default` (quality 90, 4:2:0), GDI+ saves with JPEG quality 90, and ImageSharp uses `JpegEncoder { Quality = 90 }`.

GDI+ and ImageSharp do not expose the same chroma-subsampling and TurboJPEG-specific controls, so the benchmark compares each library's normal quality-90 output rather than identical encoder settings or byte-identical files. Treat the results as throughput and allocation indicators, not as a controlled codec comparison. The benchmark project targets Windows (`net10.0-windows`) because of its GDI+ dependency.

## Using both wrapper packages

`TurboJpegWrapper.Encoder` and `TurboJpegWrapper.Decoder` both define `TurboJpeg`, `TurboJpegException`, `TurboJpegUnsupportedException`, and `TurboJpegImageTooLargeException`. Importing both namespaces without aliases makes those names ambiguous. Use type or namespace aliases:

```csharp
using EncoderTurboJpeg = TurboJpegWrapper.Encoder.TurboJpeg;
using DecoderTurboJpeg = TurboJpegWrapper.Decoder.TurboJpeg;

byte[] jpeg = EncoderTurboJpeg.EncodeToJpeg(File.ReadAllBytes("photo.bmp"));
byte[] bmp = DecoderTurboJpeg.DecodeToBmp(jpeg);
```

If both namespaces are imported, alias the exception types as well, for example `using EncoderException = TurboJpegWrapper.Encoder.TurboJpegException;`.

## Package contents and license

The package contains the managed `net10.0` assembly, `README.md`, `LICENSE`, and `THIRD-PARTY-NOTICES.md`. It contains no native assets and has no package dependencies; add exactly one native package from the table above.

The source code is licensed under the MIT License (`LICENSE`). The native libraries distributed through the companion native packages are libjpeg-turbo 3.2.0, licensed under BSD-3-Clause with the Independent JPEG Group and zlib notices (`THIRD-PARTY-NOTICES.md`). Both files are included in this package.
