#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 2 ]]; then
  echo "usage: $0 <rid> <native-suffix>" >&2
  exit 2
fi

rid="$1"
native_suffix="$2"
case "$rid" in
  win-x64|win-arm64) native_file="turbojpeg.dll" ;;
  linux-x64|linux-arm64) native_file="libturbojpeg.so" ;;
  osx-x64|osx-arm64) native_file="libturbojpeg.dylib" ;;
  *) echo "unsupported rid: $rid" >&2; exit 2 ;;
esac
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

feed="$repo_root/artifacts/packages"
[[ -d "$feed" ]] || { echo "missing package feed: $feed" >&2; exit 1; }
version="$(dotnet msbuild src/TurboJpeg.Wrapper.Decoder/TurboJpeg.Wrapper.Decoder.csproj -getProperty:Version -nologo)"
smoke_dir="$repo_root/artifacts/smoke"

rm -rf "$smoke_dir"
mkdir -p "$smoke_dir"

cleanup() {
  rm -rf "$smoke_dir"
}
trap cleanup EXIT

feed_source="$feed"
packages_source="$smoke_dir/packages"
if command -v cygpath >/dev/null 2>&1; then
  feed_source="$(cygpath -m "$feed")"
  packages_source="$(cygpath -m "$smoke_dir/packages")"
fi

cat > "$smoke_dir/NuGet.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$feed_source" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF

cat > "$smoke_dir/Smoke.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="TurboJpeg.Wrapper.Decoder" Version="$version" />
    <PackageReference Include="TurboJpeg.Wrapper.Encoder" Version="$version" />
    <PackageReference Include="TurboJpeg.Wrapper.Native.$native_suffix" Version="$version" />
  </ItemGroup>
</Project>
EOF

cat > "$smoke_dir/Program.cs" <<'CS'
using DecoderApi = TurboJpegWrapper.Decoder.TurboJpeg;
using EncoderApi = TurboJpegWrapper.Encoder.TurboJpeg;

var jpeg = File.ReadAllBytes(args[0]);
var bmp = DecoderApi.DecodeToBmp(jpeg);
var encoded = EncoderApi.EncodeToJpeg(bmp);
var roundTrip = DecoderApi.DecodeToBmp(encoded);
var info = DecoderApi.GetImageInfo(encoded);
return info.Width > 0 && info.Height > 0 && roundTrip.Length > 54 ? 0 : 1;
CS

NUGET_PACKAGES="$packages_source" dotnet publish artifacts/smoke/Smoke.csproj -c Release -r "$rid" --self-contained false -o artifacts/smoke/publish --nologo
native_path="$repo_root/artifacts/smoke/publish/$native_file"
[[ -f "$native_path" ]] || { echo "native asset was not published: $native_path" >&2; exit 1; }
native_override="$native_path"
if command -v cygpath >/dev/null 2>&1; then
  native_override="$(cygpath -m "$native_path")"
fi
TURBOJPEG_NATIVE_LIBRARY_PATH="$native_override" dotnet artifacts/smoke/publish/Smoke.dll tests/TurboJpeg.Wrapper.Decoder.Tests/Fixtures/baseline-420.jpg
echo "smoke-package: ok ($rid, $native_suffix)"
