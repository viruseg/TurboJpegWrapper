#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
output="$repo_root/artifacts/packages"

rm -rf "$output"
mkdir -p "$output"

projects=(
  "src/TurboJpeg.Wrapper.Decoder/TurboJpeg.Wrapper.Decoder.csproj"
  "src/TurboJpeg.Wrapper.Encoder/TurboJpeg.Wrapper.Encoder.csproj"
  "src/native/TurboJpeg.Wrapper.Native.win-x64/TurboJpeg.Wrapper.Native.win-x64.csproj"
  "src/native/TurboJpeg.Wrapper.Native.win-arm64/TurboJpeg.Wrapper.Native.win-arm64.csproj"
  "src/native/TurboJpeg.Wrapper.Native.linux-x64/TurboJpeg.Wrapper.Native.linux-x64.csproj"
  "src/native/TurboJpeg.Wrapper.Native.linux-arm64/TurboJpeg.Wrapper.Native.linux-arm64.csproj"
  "src/native/TurboJpeg.Wrapper.Native.osx-universal/TurboJpeg.Wrapper.Native.osx-universal.csproj"
)

for project in "${projects[@]}"; do
  dotnet pack "$repo_root/$project" -c Release -o "$output" --nologo
done
