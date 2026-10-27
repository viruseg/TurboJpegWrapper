# TurboJpeg.Wrapper

.NET 10 wrappers around libjpeg-turbo 3.2.0 and its TurboJPEG API. The repository provides one managed package and five shared native runtime packages:

| Managed package | Namespace | Purpose |
| --- | --- | --- |
| `TurboJpeg.Wrapper` | `TurboJpegWrapper` | Decodes 2- to 16-bit RGB, YCbCr, grayscale, CMYK, and YCCK JPEG files to complete 8-, 24-, or 32-bit BMP files (scaling, cropping, EXIF auto-orientation), and encodes uncompressed 1- to 32-bit BMP files to JPEG with quality, subsampling, colorspace, lossless, progressive, arithmetic coding, restart markers, and density options. |

The managed package contains no native code. Add exactly one native package that matches the deployment runtime.

> **Version `3.2.0-pre.2` is a prerelease.** All packages in this repository use that version. Pass `--prerelease` when installing from NuGet and pin exact versions for reproducible builds.

## Native packages

| Runtime | Native package | Bundled asset |
| --- | --- | --- |
| Windows x64 | `TurboJpeg.Wrapper.Native.win-x64` | `runtimes/win-x64/native/turbojpeg.dll` |
| Windows ARM64 | `TurboJpeg.Wrapper.Native.win-arm64` | `runtimes/win-arm64/native/turbojpeg.dll` |
| Linux x64 | `TurboJpeg.Wrapper.Native.linux-x64` | `runtimes/linux-x64/native/libturbojpeg.so` |
| Linux ARM64 | `TurboJpeg.Wrapper.Native.linux-arm64` | `runtimes/linux-arm64/native/libturbojpeg.so` |
| macOS x64 and ARM64 | `TurboJpeg.Wrapper.Native.osx-universal` | `runtimes/osx/native/libturbojpeg.dylib` |

The macOS package contains one universal binary for x86_64 and arm64. Linux assets are glibc builds; musl/Alpine requires a musl libjpeg-turbo build supplied through `TURBOJPEG_NATIVE_LIBRARY_PATH`.

## Installation

```bash
dotnet add package TurboJpeg.Wrapper --prerelease
dotnet add package TurboJpeg.Wrapper.Native.win-x64 --prerelease
```

Replace `win-x64` with the package for the target runtime from the table above.

The package exposes a single `TurboJpeg` facade in the `TurboJpegWrapper` namespace, covering both decoding and encoding.

## Requirements

- .NET 10 (`net10.0`).
- Windows x64/ARM64, Linux x64/ARM64, or macOS x64/ARM64. x86 is not supported.
- The bundled native library is libjpeg-turbo 3.2.0.
- Linux binaries require glibc 2.17 or later and do not support musl/Alpine. Supply a musl build through `TURBOJPEG_NATIVE_LIBRARY_PATH` when targeting Alpine.
- macOS requires 10.7 or later on x64 and 11 or later on ARM64. The `osx-universal` package serves both architectures.
- If the matching native package is missing, the first operation throws `TurboJpegException` with the expected native library paths.

## Benchmarks

`Ratio` and `Alloc Ratio` are relative to TurboJpeg.

### Encoding

`TurboJpeg.EncodeToJpeg` with default options (quality 90, 4:2:0), GDI+ JPEG quality 90, and ImageSharp `JpegEncoder { Quality = 90 }`.

| Method           | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated  | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|--------:|---------:|---------:|---------:|-----------:|------------:|
| EncodeTurboJpeg  |  5.390 ms | 0.0613 ms | 0.0656 ms |  1.00 |    0.02 |        - |        - |        - |  343.75 KB |        1.00 |
| EncodeGdi        |  8.665 ms | 0.1143 ms | 0.1013 ms |  1.61 |    0.03 | 218.7500 | 203.1250 | 203.1250 | 1294.16 KB |        3.76 |
| EncodeImageSharp | 27.392 ms | 0.2689 ms | 0.2384 ms |  5.08 |    0.07 | 500.0000 | 468.7500 | 468.7500 | 2991.58 KB |        8.70 |

### Decoding

`TurboJpeg.DecodeToBmp` with `AutoOrient = true` and `Bpp24`; GDI+ and ImageSharp auto-orient from EXIF and produce a 24-bit BMP as well.

| Method           | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|----------:|------:|--------:|---------:|---------:|---------:|----------:|------------:|
| DecodeTurboJpeg  |  6.341 ms | 0.1250 ms | 0.2053 ms |  6.227 ms |  1.00 |    0.04 | 500.0000 | 500.0000 | 500.0000 |   4.42 MB |        1.00 |
| DecodeGdi        | 14.172 ms | 0.2785 ms | 0.2980 ms | 14.187 ms |  2.24 |    0.08 | 875.0000 | 875.0000 | 875.0000 |   8.85 MB |        2.00 |
| DecodeImageSharp | 15.002 ms | 0.2241 ms | 0.1872 ms | 14.986 ms |  2.37 |    0.08 | 765.6250 | 656.2500 | 656.2500 |   9.29 MB |        2.10 |

GDI+ and ImageSharp do not expose the same chroma-subsampling and TurboJPEG-specific controls, so these results are throughput and allocation indicators, not a byte-identical output comparison.

The harnesses are in [bench/TurboJpeg.Wrapper.Encoder.Benchmark](bench/TurboJpeg.Wrapper.Encoder.Benchmark) and [bench/TurboJpeg.Wrapper.Decoder.Benchmark](bench/TurboJpeg.Wrapper.Decoder.Benchmark).

## Package documentation

- [TurboJpeg.Wrapper README](src/TurboJpeg.Wrapper/README.md)

The managed package embeds its README. Native packages contain the native asset, `LICENSE`, and `THIRD-PARTY-NOTICES.md` only.

## License and third-party notices

The source code in this repository is licensed under the MIT License. See [LICENSE](LICENSE).

The bundled native libraries are libjpeg-turbo 3.2.0, distributed under the BSD-3-Clause license and the Independent JPEG Group / zlib notices. See [native/THIRD-PARTY-NOTICES.md](native/THIRD-PARTY-NOTICES.md). Every managed package includes its README, `LICENSE`, and `THIRD-PARTY-NOTICES.md`; native packages include `LICENSE` and `THIRD-PARTY-NOTICES.md`.
