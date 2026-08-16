#!/usr/bin/env bash
# Proves that the llama/ggml native backend actually ships inside the built Android package, and
# measures the 16 KB page-size alignment of every such .so (D-2026-08-16-llm-mobile-4). Fails
# closed: no APK found, zero matching .so found, or (in --check-doc mode) a stale/adulterated
# recorded alignment are all hard failures, never a quiet empty success.
#
# Usage:
#   check-android-so.sh [--check-doc <doc-file>] [<search-dir>]
#
# Output contract (tokens the Definition of Done greps for -- do not rename):
#   SO_FOUND <path-inside-apk>            one line per llama/ggml .so found
#   SO_ALIGN <path-inside-apk> align=<n>  alignment of that .so's largest PT_LOAD segment
#   SO_COUNT <n>                          total matching .so found; 0 is always a failure
#
# --check-doc <doc-file>: after measuring, every SO_ALIGN line above must appear verbatim in
# <doc-file> (a changed number = stale doc = failure), and every .so measured below 16384 must be
# named in a line starting with "MITIGATION:" in that same file (an unrecorded limitation is a
# failure too, not a silently-passing gate).
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

DEFAULT_SEARCH_DIR="src/TranslateReader/bin/Debug/net10.0-android"
REQUIRED_ALIGN=16384

# --- Argument parsing: one optional --check-doc <file>, one optional positional search dir. ---
CHECK_DOC=""
SEARCH_DIR=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --check-doc)
      test $# -ge 2 || { echo "ERROR: --check-doc requires a file argument" >&2; exit 1; }
      CHECK_DOC="$2"
      shift 2
      ;;
    *)
      SEARCH_DIR="$1"
      shift
      ;;
  esac
done
SEARCH_DIR="${SEARCH_DIR:-$DEFAULT_SEARCH_DIR}"

if [[ -n "$CHECK_DOC" && ! -f "$CHECK_DOC" ]]; then
  echo "ERROR: --check-doc file not found: $CHECK_DOC" >&2
  exit 1
fi

if [[ ! -d "$SEARCH_DIR" ]]; then
  echo "ERROR: search directory not found: $SEARCH_DIR" >&2
  exit 1
fi

mapfile -t APKS < <(find "$SEARCH_DIR" -iname "*.apk" 2>/dev/null | sort)
if [[ ${#APKS[@]} -eq 0 ]]; then
  echo "ERROR: no .apk found under $SEARCH_DIR -- build net10.0-android first" >&2
  exit 1
fi

WORKDIR=$(mktemp -d)
trap 'rm -rf "$WORKDIR"' EXIT

# --- Zip access: unzip first, PowerShell/.NET ZipFile as fallback (unzip is not guaranteed on
# every Git Bash install) -- D-2026-08-16-llm-mobile-4. ---
list_zip_entries() {
  local apk="$1"
  if command -v unzip >/dev/null 2>&1; then
    unzip -l "$apk" 2>/dev/null | tr -d '\r' | awk '{print $NF}' | grep -E '^lib/'
    return 0
  fi

  local ps
  ps=$(command -v pwsh || command -v powershell.exe || true)
  if [[ -z "$ps" ]]; then
    echo "ERROR: neither unzip nor PowerShell is available to read $apk" >&2
    return 1
  fi
  "$ps" -NoProfile -NonInteractive -Command \
    "Add-Type -AssemblyName System.IO.Compression.FileSystem; ([System.IO.Compression.ZipFile]::OpenRead('$apk')).Entries | ForEach-Object { \$_.FullName }" \
    | tr -d '\r' | grep -E '^lib/'
}

extract_zip_entry() {
  local apk="$1" entry="$2" dest="$3"
  if command -v unzip >/dev/null 2>&1; then
    unzip -p "$apk" "$entry" > "$dest" 2>/dev/null
    return 0
  fi

  local ps
  ps=$(command -v pwsh || command -v powershell.exe || true)
  if [[ -z "$ps" ]]; then
    echo "ERROR: neither unzip nor PowerShell is available to read $apk" >&2
    return 1
  fi
  "$ps" -NoProfile -NonInteractive -Command \
    "Add-Type -AssemblyName System.IO.Compression.FileSystem; \$z = [System.IO.Compression.ZipFile]::OpenRead('$apk'); \$e = \$z.GetEntry('$entry'); \$s = \$e.Open(); \$o = [System.IO.File]::Create('$dest'); \$s.CopyTo(\$o); \$o.Dispose(); \$s.Dispose(); \$z.Dispose();"
}

# --- ELF64 program-header reader: no readelf/NDK dependency, pure `od` byte reads (D-...-4). ---
read_uint() {
  # read_uint <file> <byte-offset> <byte-width>
  od -An -t"u$3" --endian=little -j "$2" -N "$3" "$1" | tr -d ' '
}

elf_largest_load_align() {
  local file="$1"
  local magic class phoff phentsize phnum
  magic=$(od -An -tx1 -N 4 "$file" | tr -d ' ')
  class=$(read_uint "$file" 4 1)
  if [[ "$magic" != "7f454c46" || "$class" != "2" ]]; then
    echo "ERROR: $file is not a 64-bit ELF (magic=$magic class=$class)" >&2
    return 1
  fi

  phoff=$(read_uint "$file" 32 8)
  phentsize=$(read_uint "$file" 54 2)
  phnum=$(read_uint "$file" 56 2)

  local i entry_off type memsz align best_align=0 best_memsz=-1
  for ((i = 0; i < phnum; i++)); do
    entry_off=$((phoff + i * phentsize))
    type=$(read_uint "$file" "$entry_off" 4)
    if [[ "$type" == "1" ]]; then
      memsz=$(read_uint "$file" "$((entry_off + 40))" 8)
      align=$(read_uint "$file" "$((entry_off + 48))" 8)
      if (( memsz > best_memsz )); then
        best_memsz=$memsz
        best_align=$align
      fi
    fi
  done

  if (( best_memsz < 0 )); then
    echo "ERROR: $file has no PT_LOAD segment" >&2
    return 1
  fi
  echo "$best_align"
}

# --- Measure every llama/ggml .so across every APK found, de-duplicated by in-archive path. ---
declare -A SEEN
FOUND_COUNT=0
ALIGN_LINES=()
MITIGATION_NEEDED=()

for apk in "${APKS[@]}"; do
  while IFS= read -r entry; do
    [[ -z "$entry" ]] && continue
    case "$entry" in
      lib/*/libllama.so | lib/*/libggml.so | lib/*/libggml-base.so | lib/*/libggml-cpu.so | lib/*/libmtmd.so) ;;
      *) continue ;;
    esac
    [[ -n "${SEEN[$entry]:-}" ]] && continue
    SEEN["$entry"]=1

    dest="$WORKDIR/$(basename "$entry")-$FOUND_COUNT"
    extract_zip_entry "$apk" "$entry" "$dest"
    if [[ ! -s "$dest" ]]; then
      echo "ERROR: failed to extract $entry from $apk" >&2
      exit 1
    fi

    align=$(elf_largest_load_align "$dest")
    echo "SO_FOUND $entry"
    echo "SO_ALIGN $entry align=$align"
    ALIGN_LINES+=("SO_ALIGN $entry align=$align")
    if (( align < REQUIRED_ALIGN )); then
      MITIGATION_NEEDED+=("$(basename "$entry")")
    fi
    FOUND_COUNT=$((FOUND_COUNT + 1))
  done < <(list_zip_entries "$apk")
done

echo "SO_COUNT $FOUND_COUNT"

if [[ "$FOUND_COUNT" -eq 0 ]]; then
  echo "ERROR: zero llama/ggml .so found in $SEARCH_DIR -- native backend missing from the package" >&2
  exit 1
fi

if [[ -n "$CHECK_DOC" ]]; then
  for line in "${ALIGN_LINES[@]}"; do
    if ! grep -qF "$line" "$CHECK_DOC"; then
      echo "ERROR: $CHECK_DOC is out of date or wrong, missing measured line: $line" >&2
      exit 1
    fi
  done

  for so_name in "${MITIGATION_NEEDED[@]}"; do
    if ! grep -E '^MITIGATION:' "$CHECK_DOC" 2>/dev/null | grep -qF "$so_name"; then
      echo "ERROR: $so_name measured below $REQUIRED_ALIGN-byte alignment but $CHECK_DOC has no MITIGATION line naming it" >&2
      exit 1
    fi
  done
fi

exit 0
