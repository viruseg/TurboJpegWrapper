# TurboJpeg.Wrapper

.NET 10 wrappers around libjpeg-turbo 3.2.0 and its TurboJPEG API. The repository provides two managed packages and five shared native runtime packages:

| Managed package | Namespace | Purpose |
| --- | --- | --- |
| `TurboJpeg.Wrapper.Decoder` | `TurboJpegWrapper.Decoder` | Decodes 8-bit lossy, progressive, and lossless JPEG files to complete 8-, 24-, or 32-bit BMP files, with optional scaling and EXIF auto-orientation. |
| `TurboJpeg.Wrapper.Encoder` | `TurboJpegWrapper.Encoder` | Encodes uncompressed 1-, 4-, 8-, 16-, 24-, and 32-bit BMP files to JPEG, with quality, chroma subsampling, lossless, progressive, arithmetic coding, restart markers, and density options. |

The managed packages contain no native code. Add exactly one native package that matches the deployment runtime; the same native package serves both managed packages.

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

### Decoder

```bash
dotnet add package TurboJpeg.Wrapper.Decoder --prerelease
dotnet add package TurboJpeg.Wrapper.Native.win-x64 --prerelease
```

### Encoder

```bash
dotnet add package TurboJpeg.Wrapper.Encoder --prerelease
dotnet add package TurboJpeg.Wrapper.Native.win-x64 --prerelease
```

Replace `win-x64` with the package for the target runtime from the table above. An application that uses both managed packages still references only one native package.

Both packages expose a `TurboJpeg` facade and `TurboJpegException` types in their own namespaces. When importing both namespaces, use type or namespace aliases to avoid ambiguous references.

## Requirements

- .NET 10 (`net10.0`).
- Windows x64/ARM64, Linux x64/ARM64, or macOS x64/ARM64. x86 is not supported.
- The bundled native library is libjpeg-turbo 3.2.0.
- Linux binaries require glibc 2.17 or later and do not support musl/Alpine. Supply a musl build through `TURBOJPEG_NATIVE_LIBRARY_PATH` when targeting Alpine.
- macOS requires 10.7 or later on x64 and 11 or later on ARM64. The `osx-universal` package serves both architectures.
- If the matching native package is missing, the first operation throws `TurboJpegException` with the expected native library paths.

## Package documentation

- [TurboJpeg.Wrapper.Decoder README](src/TurboJpeg.Wrapper.Decoder/README.md)
- [TurboJpeg.Wrapper.Encoder README](src/TurboJpeg.Wrapper.Encoder/README.md)

Each managed package embeds its own README. Native packages contain the native asset, `LICENSE`, and `THIRD-PARTY-NOTICES.md` only.

## License and third-party notices

The source code in this repository is licensed under the MIT License. See [LICENSE](LICENSE).

The bundled native libraries are libjpeg-turbo 3.2.0, distributed under the BSD-3-Clause license and the Independent JPEG Group / zlib notices. See [native/THIRD-PARTY-NOTICES.md](native/THIRD-PARTY-NOTICES.md). Every managed package includes its README, `LICENSE`, and `THIRD-PARTY-NOTICES.md`; native packages include `LICENSE` and `THIRD-PARTY-NOTICES.md`.
