#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"
output="${1:-$repo_root/artifacts/packages}"
version="$(dotnet msbuild src/TurboJpeg.Wrapper.Decoder/TurboJpeg.Wrapper.Decoder.csproj -getProperty:Version -nologo)"

fail() {
  echo "verify-packages: $*" >&2
  exit 1
}

entries() {
  unzip -Z1 "$1"
}

nupkg() {
  echo "$output/$1.$version.nupkg"
}

package_count="$(find "$output" -maxdepth 1 -type f -name '*.nupkg' | wc -l | tr -d ' ')"
[[ "$package_count" -eq 6 ]] || fail "expected 6 packages, found $package_count"

main="$(nupkg TurboJpeg.Wrapper.Decoder)"
[[ -f "$main" ]] || fail "missing $main"
main_entries="$(entries "$main")"
grep -qxF "lib/net10.0/TurboJpeg.Wrapper.Decoder.dll" <<<"$main_entries" || fail "managed package has no lib/net10.0/TurboJpeg.Wrapper.Decoder.dll"
grep -qxF "README.md" <<<"$main_entries" || fail "managed package has no README.md"
grep -qxF "THIRD-PARTY-NOTICES.md" <<<"$main_entries" || fail "managed package has no THIRD-PARTY-NOTICES.md"
if grep -q '^runtimes/' <<<"$main_entries"; then
  fail "managed package contains runtime assets"
fi
main_nuspec="$(unzip -p "$main" "$(grep '\.nuspec$' <<<"$main_entries")")"
if grep -q 'dependency id="TurboJpeg.Wrapper.Decoder.Native.' <<<"$main_nuspec"; then
  fail "managed package depends on a native package"
fi

check_native() {
  local id="$1"
  local asset="$2"
  local package
  package="$(nupkg "$id")"
  [[ -f "$package" ]] || fail "missing $package"
  local package_entries
  package_entries="$(entries "$package")"
  grep -qxF "$asset" <<<"$package_entries" || fail "$id is missing $asset"
  grep -qxF "THIRD-PARTY-NOTICES.md" <<<"$package_entries" || fail "$id is missing THIRD-PARTY-NOTICES.md"
  if grep -q '^lib/' <<<"$package_entries"; then
    fail "$id contains managed lib assets"
  fi
  local runtime_count
  runtime_count="$(grep -c '^runtimes/.*/native/' <<<"$package_entries" || true)"
  [[ "$runtime_count" -eq 1 ]] || fail "$id has $runtime_count runtime assets"
  local package_nuspec
  package_nuspec="$(unzip -p "$package" "$(grep '\.nuspec$' <<<"$package_entries")")"
  if grep -q '<dependency ' <<<"$package_nuspec"; then
    fail "$id declares dependencies"
  fi
}

check_native TurboJpeg.Wrapper.Decoder.Native.win-x64 runtimes/win-x64/native/turbojpeg.dll
check_native TurboJpeg.Wrapper.Decoder.Native.win-arm64 runtimes/win-arm64/native/turbojpeg.dll
check_native TurboJpeg.Wrapper.Decoder.Native.linux-x64 runtimes/linux-x64/native/libturbojpeg.so
check_native TurboJpeg.Wrapper.Decoder.Native.linux-arm64 runtimes/linux-arm64/native/libturbojpeg.so
check_native TurboJpeg.Wrapper.Decoder.Native.osx-universal runtimes/osx/native/libturbojpeg.dylib

echo "verify-packages: ok ($version)"
