#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"
output="${1:-$repo_root/artifacts/packages}"
version="$(dotnet msbuild src/TurboJpeg.Wrapper/TurboJpeg.Wrapper.csproj -getProperty:Version -nologo)"

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

check_managed() {
  local id="$1"
  local package
  package="$(nupkg "$id")"
  [[ -f "$package" ]] || fail "missing $package"
  local package_entries
  package_entries="$(entries "$package")"
  grep -qxF "lib/net10.0/$id.dll" <<<"$package_entries" || fail "$id has no lib/net10.0/$id.dll"
  grep -qxF "lib/net10.0/$id.xml" <<<"$package_entries" || fail "$id has no lib/net10.0/$id.xml"
  local lib_count
  lib_count="$(grep -c '^lib/' <<<"$package_entries" || true)"
  [[ "$lib_count" -eq 2 ]] || fail "$id has $lib_count lib assets instead of 2"
  grep -qxF "README.md" <<<"$package_entries" || fail "$id has no README.md"
  grep -qxF "LICENSE" <<<"$package_entries" || fail "$id has no LICENSE"
  grep -qxF "THIRD-PARTY-NOTICES.md" <<<"$package_entries" || fail "$id has no THIRD-PARTY-NOTICES.md"
  if grep -q '^runtimes/' <<<"$package_entries"; then
    fail "$id contains runtime assets"
  fi
  local package_readme
  package_readme="$(unzip -p "$package" README.md)"
  if ! grep -qxF "# $id" <<<"$package_readme"; then
    fail "$id README title is not '# $id'"
  fi
  local package_nuspec
  package_nuspec="$(unzip -p "$package" "$(grep '\.nuspec$' <<<"$package_entries")")"
  if ! grep -q '<license type="file">LICENSE</license>' <<<"$package_nuspec"; then
    fail "$id has no file-based LICENSE metadata"
  fi
  if grep -q '<dependency ' <<<"$package_nuspec"; then
    fail "$id declares package dependencies"
  fi
}

check_managed TurboJpeg.Wrapper

check_native() {
  local id="$1"
  local asset="$2"
  local package
  package="$(nupkg "$id")"
  [[ -f "$package" ]] || fail "missing $package"
  local package_entries
  package_entries="$(entries "$package")"
  grep -qxF "$asset" <<<"$package_entries" || fail "$id is missing $asset"
  grep -qxF "LICENSE" <<<"$package_entries" || fail "$id is missing LICENSE"
  grep -qxF "THIRD-PARTY-NOTICES.md" <<<"$package_entries" || fail "$id is missing THIRD-PARTY-NOTICES.md"
  if grep -q '^lib/' <<<"$package_entries"; then
    fail "$id contains managed lib assets"
  fi
  local runtime_count
  runtime_count="$(grep -c '^runtimes/.*/native/' <<<"$package_entries" || true)"
  [[ "$runtime_count" -eq 1 ]] || fail "$id has $runtime_count runtime assets"
  local package_nuspec
  package_nuspec="$(unzip -p "$package" "$(grep '\.nuspec$' <<<"$package_entries")")"
  if ! grep -q '<license type="file">LICENSE</license>' <<<"$package_nuspec"; then
    fail "$id has no file-based LICENSE metadata"
  fi
  if grep -q '<dependency ' <<<"$package_nuspec"; then
    fail "$id declares dependencies"
  fi
}

check_native TurboJpeg.Wrapper.Native.win-x64 runtimes/win-x64/native/turbojpeg.dll
check_native TurboJpeg.Wrapper.Native.win-arm64 runtimes/win-arm64/native/turbojpeg.dll
check_native TurboJpeg.Wrapper.Native.linux-x64 runtimes/linux-x64/native/libturbojpeg.so
check_native TurboJpeg.Wrapper.Native.linux-arm64 runtimes/linux-arm64/native/libturbojpeg.so
check_native TurboJpeg.Wrapper.Native.osx-universal runtimes/osx/native/libturbojpeg.dylib

echo "verify-packages: ok ($version)"
