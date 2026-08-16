#!/usr/bin/env bash
# Fetches the pinned llama.cpp release's iOS XCFramework, verifies its SHA-256 before ever
# extracting anything, and leaves the ios-arm64 llama.framework slice at a stable, predictable
# path that TranslateReader.csproj's NativeReference points to literally
# (D-2026-08-16-llm-mobile-9). Never committed to git -- see .gitignore.
#
# Usage:
#   fetch-llama-xcframework.sh <tag> <sha256>                 download + verify + extract
#   fetch-llama-xcframework.sh --verify-only <file> <sha256>  verify only, no network, testable
#
# The --verify-only mode is what lets the checksum guarantee be PROVEN rather than just grepped:
# run it against a known-good file/hash pair (expect exit 0) and against a wrong hash (expect
# exit != 0) -- both without downloading a single byte.
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

CACHE_ROOT=".cache/llama-xcframework"
RELEASE_URL_BASE="https://github.com/ggml-org/llama.cpp/releases/download"

sha256_of() {
  if command -v sha256sum >/dev/null 2>&1; then
    sha256sum "$1" | cut -d' ' -f1
  elif command -v shasum >/dev/null 2>&1; then
    shasum -a 256 "$1" | cut -d' ' -f1
  else
    echo "ERROR: neither sha256sum nor shasum is available to verify a checksum" >&2
    return 1
  fi
}

verify_checksum() {
  local file="$1" expected="$2" actual
  if [[ ! -f "$file" ]]; then
    echo "ERROR: file not found for checksum verification: $file" >&2
    return 1
  fi
  actual=$(sha256_of "$file")
  if [[ "$actual" != "$expected" ]]; then
    echo "ERROR: checksum mismatch for $file: expected $expected, got $actual" >&2
    return 1
  fi
  echo "CHECKSUM_OK $file"
}

if [[ "${1:-}" == "--verify-only" ]]; then
  test $# -eq 3 || { echo "ERROR: --verify-only requires <file> <sha256>" >&2; exit 1; }
  verify_checksum "$2" "$3"
  exit 0
fi

test $# -eq 2 || { echo "ERROR: usage: fetch-llama-xcframework.sh <tag> <sha256>" >&2; exit 1; }
TAG="$1"
EXPECTED_SHA256="$2"

if [[ ! "$TAG" =~ ^b[0-9]+$ ]]; then
  echo "ERROR: tag must be a pinned llama.cpp release like bNNNN, never latest/master/main: $TAG" >&2
  exit 1
fi

ASSET_NAME="llama-${TAG}-xcframework.zip"
TAG_DIR="$CACHE_ROOT/$TAG"
ZIP_PATH="$TAG_DIR/$ASSET_NAME"
EXTRACT_DIR="$TAG_DIR/extracted"
FRAMEWORK_DEST="$TAG_DIR/llama.framework"

mkdir -p "$TAG_DIR"

# --- Cache layer 1: skip re-extraction entirely if a previous run of this exact script already
# produced the framework here (it only ever does so from a checksum-verified zip). ---
if [[ -d "$FRAMEWORK_DEST" ]]; then
  echo "CACHE_HIT $FRAMEWORK_DEST"
  exit 0
fi

# --- Cache layer 2: skip the network round-trip if a zip is already here AND still matches the
# pinned checksum (a corrupted or stale cached zip is never trusted, always re-downloaded). ---
if [[ -f "$ZIP_PATH" ]] && verify_checksum "$ZIP_PATH" "$EXPECTED_SHA256" >/dev/null 2>&1; then
  echo "ZIP_CACHE_HIT $ZIP_PATH"
else
  DOWNLOAD_URL="$RELEASE_URL_BASE/$TAG/$ASSET_NAME"
  echo "DOWNLOADING $DOWNLOAD_URL"
  curl -sL --fail --max-time 600 -o "$ZIP_PATH.tmp" "$DOWNLOAD_URL"
  mv "$ZIP_PATH.tmp" "$ZIP_PATH"

  # --- Verify BEFORE extracting anything: an unverified download is never trusted enough to
  # unpack, let alone link into the app. ---
  verify_checksum "$ZIP_PATH" "$EXPECTED_SHA256"
fi

rm -rf "$EXTRACT_DIR" "$FRAMEWORK_DEST"
mkdir -p "$EXTRACT_DIR"
if command -v unzip >/dev/null 2>&1; then
  unzip -q "$ZIP_PATH" -d "$EXTRACT_DIR"
else
  ps=$(command -v pwsh || command -v powershell.exe || true)
  if [[ -z "$ps" ]]; then
    echo "ERROR: neither unzip nor PowerShell is available to extract $ZIP_PATH" >&2
    exit 1
  fi
  "$ps" -NoProfile -NonInteractive -Command "Expand-Archive -Path '$ZIP_PATH' -DestinationPath '$EXTRACT_DIR' -Force"
fi

SOURCE_FRAMEWORK=$(find "$EXTRACT_DIR" -type d -ipath '*llama.xcframework/ios-arm64/llama.framework' | head -1)
if [[ -z "$SOURCE_FRAMEWORK" ]]; then
  echo "ERROR: no ios-arm64/llama.framework slice found inside $ASSET_NAME" >&2
  exit 1
fi

cp -R "$SOURCE_FRAMEWORK" "$FRAMEWORK_DEST"
echo "EXTRACTED $FRAMEWORK_DEST"
exit 0
