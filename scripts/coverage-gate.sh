#!/usr/bin/env bash
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

COVERAGE_MIN=${COVERAGE_MIN:-90}

# Boundary commit that separates legacy code (exempt, D-2) from new/changed code that must
# meet the floor. Literal constant on purpose -- never overridable via env (D-...-1).
BOUNDARY=4285f25
GATE_DIR=TestResults/coverage-gate
APP_PREFIX="src/TranslateReader/"

# Own directory, wiped at the start of every run so a stale report can never be read back
# as if it were this execution's measurement.
rm -rf "$GATE_DIR"
mkdir -p "$GATE_DIR"

# --- Scope: files added or modified after the boundary (D-...-2), test/** excluded ---
mapfile -t SCOPE_FILES < <(
  git log --diff-filter=AM --pretty=format: --name-only "${BOUNDARY}..HEAD" 2>/dev/null \
    | sort -u \
    | grep -E '\.cs$' \
    | grep -v '^test/' \
    | grep -vE '(^|/)obj/' \
    | grep -vE '(^|/)bin/' \
    || true
)

EXISTING_SCOPE_FILES=()
for f in "${SCOPE_FILES[@]}"; do
  [[ -f "$f" ]] && EXISTING_SCOPE_FILES+=("$f")
done
SCOPE_FILES=("${EXISTING_SCOPE_FILES[@]}")

# --- Measure now: run the suite with coverage collection into our own clean directory. ---
# Never read someone else's artifact -- this execution's report is the only one considered.
if ! dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release \
     --collect:"XPlat Code Coverage" \
     --results-directory "$GATE_DIR" \
     > "$GATE_DIR/dotnet-test.log" 2>&1
then
  echo "ERROR: dotnet test failed (red suite) -- see $GATE_DIR/dotnet-test.log" >&2
  tail -n 60 "$GATE_DIR/dotnet-test.log" >&2 || true
  exit 3
fi

shopt -s globstar nullglob
REPORTS=("$GATE_DIR"/**/coverage.cobertura.xml)
shopt -u globstar nullglob

if [[ ${#REPORTS[@]} -eq 0 ]]; then
  echo "ERROR: no coverage.cobertura.xml found under $GATE_DIR" >&2
  exit 3
fi

# --- Parse Cobertura report(s): normalize so each tag is on its own line, track the current
# <class filename="...">, collect filename|number|hits. No xmllint dependency (absent in Git
# Bash). Dedup by (filename, line number) keeping the max hits: a single .cs file can render
# several <class> blocks (partial classes, multiple types per file) and each <line> is also
# repeated at both method- and class-level -- counting every occurrence would inflate the
# denominator. ---
declare -A MAX_HITS

for report in "${REPORTS[@]}"; do
  current_filename=""
  while IFS= read -r tag; do
    if [[ "$tag" == '<class '* ]]; then
      if [[ "$tag" =~ filename=\"([^\"]*)\" ]]; then
        current_filename="${BASH_REMATCH[1]//\\//}"
      else
        current_filename=""
      fi
    elif [[ "$tag" == '<line '* && -n "$current_filename" ]]; then
      if [[ "$tag" =~ number=\"([0-9]+)\" ]]; then
        num="${BASH_REMATCH[1]}"
        if [[ "$tag" =~ hits=\"([0-9]+)\" ]]; then
          hits="${BASH_REMATCH[1]}"
          key="${current_filename}|${num}"
          prev="${MAX_HITS[$key]:-}"
          if [[ -z "$prev" || "$hits" -gt "$prev" ]]; then
            MAX_HITS["$key"]="$hits"
          fi
        fi
      fi
    fi
  done < <(sed 's/</\n</g' "$report" | grep -E '^<class |^<line ')
done

declare -A FILE_VALID
declare -A FILE_COVERED
for key in "${!MAX_HITS[@]}"; do
  fname="${key%|*}"
  hits="${MAX_HITS[$key]}"
  FILE_VALID["$fname"]=$(( ${FILE_VALID["$fname"]:-0} + 1 ))
  if [[ "$hits" != "0" ]]; then
    FILE_COVERED["$fname"]=$(( ${FILE_COVERED["$fname"]:-0} + 1 ))
  fi
done

# --- Emit per-file lines and accumulate the line-weighted total. App MAUI code is never part
# of the coverage run (test/TranslateReader.Tests only references TranslateReader.Core), so it
# is reported as an explicit skip rather than silently absent. ---
TOTAL_COVERED=0
TOTAL_VALID=0
MEASURED_FILES=0

for f in "${SCOPE_FILES[@]}"; do
  if [[ "$f" == "$APP_PREFIX"* ]]; then
    echo "COVERAGE_SKIP $f reason=app-maui-not-instrumented"
    continue
  fi

  match=""
  for fname in "${!FILE_VALID[@]}"; do
    if [[ "$f" == *"/$fname" || "$f" == "$fname" ]]; then
      match="$fname"
      break
    fi
  done

  if [[ -n "$match" && "${FILE_VALID[$match]:-0}" -gt 0 ]]; then
    c="${FILE_COVERED[$match]:-0}"
    v="${FILE_VALID[$match]}"
    echo "COVERAGE_FILE $f covered=$c valid=$v"
    TOTAL_COVERED=$((TOTAL_COVERED + c))
    TOTAL_VALID=$((TOTAL_VALID + v))
    MEASURED_FILES=$((MEASURED_FILES + 1))
  else
    echo "COVERAGE_SKIP $f reason=no-instrumented-lines"
  fi
done

if [[ "$TOTAL_VALID" -eq 0 ]]; then
  echo "ERROR: zero instrumented lines in scope -- nothing was measured" >&2
  exit 3
fi

PCT=$(awk -v c="$TOTAL_COVERED" -v v="$TOTAL_VALID" 'BEGIN{printf "%.2f", 100*c/v}')
echo "COVERAGE_SCOPE covered=$TOTAL_COVERED valid=$TOTAL_VALID pct=$PCT files=$MEASURED_FILES"

if awk -v p="$PCT" -v m="$COVERAGE_MIN" 'BEGIN{exit !(p+0 < m+0)}'; then
  echo "FAIL: scope coverage ${PCT}% below floor ${COVERAGE_MIN}%" >&2
  exit 1
fi

exit 0
