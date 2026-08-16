#!/usr/bin/env bash
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

COVERAGE_MIN=${COVERAGE_MIN:-90}
COVERAGE_JS_MIN=${COVERAGE_JS_MIN:-85}

# Boundary commit that separates legacy code (exempt, D-2) from new/changed code that must
# meet the floor. Literal constant on purpose -- never overridable via env (D-...-1).
BOUNDARY=4285f25
GATE_DIR=TestResults/coverage-gate
APP_PREFIX="src/TranslateReader/"
JS_DIR="src/TranslateReader/Resources/Raw/wwwroot/js"

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

# --- Waivers: one path per line, comment MUST cite a decision (# D-...). A line without that
# reference is not a valid waiver -- it is reported and still counts as a violation. ---
WAIVERS_FILE=.jdi/coverage-waivers.txt
declare -A WAIVED_PATHS
WAIVED_COUNT=0
if [[ -f "$WAIVERS_FILE" ]]; then
  while IFS= read -r line || [[ -n "$line" ]]; do
    trimmed="${line#"${line%%[![:space:]]*}"}"
    [[ -z "$trimmed" ]] && continue
    [[ "$trimmed" == \#* ]] && continue
    waiver_path="${trimmed%%[[:space:]]*}"
    comment="${trimmed#*#}"
    if [[ "$trimmed" == *"#"* && "$comment" == *"D-"* ]]; then
      WAIVED_PATHS["$waiver_path"]=1
      WAIVED_COUNT=$((WAIVED_COUNT + 1))
    else
      echo "COVERAGE_WAIVER_INVALID $waiver_path"
    fi
  done < "$WAIVERS_FILE"
fi

# --- Guard: new .cs files under the MAUI app never get instrumented by this run (the test
# project only references TranslateReader.Core), so a NEW file there must ship pre-tested
# elsewhere or be waived -- never silently uncounted. Runs before the expensive dotnet test
# call so an unwaived new file fails cheap (git only) instead of after a full suite run. ---
mapfile -t NEW_APP_TRACKED < <(
  git log --diff-filter=A --pretty=format: --name-only "${BOUNDARY}..HEAD" 2>/dev/null \
    | sort -u \
    | grep -E '\.cs$' \
    | grep -E "^${APP_PREFIX}" \
    | grep -vE '(^|/)obj/' \
    | grep -vE '(^|/)bin/' \
    || true
)
mapfile -t NEW_APP_UNTRACKED < <(
  git ls-files --others --exclude-standard -- "${APP_PREFIX}**.cs" 2>/dev/null || true
)
mapfile -t NEW_APP_CS < <(
  printf '%s\n' "${NEW_APP_TRACKED[@]}" "${NEW_APP_UNTRACKED[@]}" | sed '/^$/d' | sort -u
)
NEW_APP_CS_COUNT=${#NEW_APP_CS[@]}

echo "COVERAGE_GUARD new_app_cs=$NEW_APP_CS_COUNT waived=$WAIVED_COUNT"
if [[ "$NEW_APP_CS_COUNT" -gt "$WAIVED_COUNT" ]]; then
  exit 2
fi

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
    if [[ -n "${WAIVED_PATHS[$f]:-}" ]]; then
      echo "COVERAGE_SKIP $f reason=waived"
    else
      echo "COVERAGE_SKIP $f reason=app-maui-not-instrumented"
    fi
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

GATE_FAILED=0
if awk -v p="$PCT" -v m="$COVERAGE_MIN" 'BEGIN{exit !(p+0 < m+0)}'; then
  echo "FAIL: scope coverage ${PCT}% below floor ${COVERAGE_MIN}%" >&2
  GATE_FAILED=1
fi

# --- JS: the four production WebView scripts, measured by this execution too (not just by
# Sonar). Node's own coverage engine already dedups DA lines per file, so no extra pass is
# needed here. ---
if ! command -v node >/dev/null 2>&1; then
  echo "ERROR: node not found -- cannot measure JS coverage" >&2
  exit 3
fi

JS_LCOV="$GATE_DIR/js-lcov.info"
if ! node --test --experimental-test-coverage --test-reporter=lcov \
     --test-reporter-destination="$JS_LCOV" test/js/ \
     > "$GATE_DIR/node-test.log" 2>&1
then
  echo "ERROR: node --test failed (red JS suite) -- see $GATE_DIR/node-test.log" >&2
  tail -n 60 "$GATE_DIR/node-test.log" >&2 || true
  exit 3
fi

declare -A JS_VALID
declare -A JS_COVERED
current_sf=""
while IFS= read -r line; do
  case "$line" in
    SF:*)
      current_sf="${line#SF:}"
      current_sf="${current_sf//\\//}"
      ;;
    DA:*)
      if [[ -n "$current_sf" ]]; then
        rest="${line#DA:}"
        da_hits="${rest#*,}"
        JS_VALID["$current_sf"]=$(( ${JS_VALID["$current_sf"]:-0} + 1 ))
        if [[ "$da_hits" != "0" ]]; then
          JS_COVERED["$current_sf"]=$(( ${JS_COVERED["$current_sf"]:-0} + 1 ))
        fi
      fi
      ;;
    end_of_record)
      current_sf=""
      ;;
  esac
done < "$JS_LCOV"

JS_TOTAL_COVERED=0
JS_TOTAL_VALID=0
JS_FILES=0
for name in bridge paginated scroll snippets translation; do
  expected="$JS_DIR/$name.js"
  match=""
  for sf in "${!JS_VALID[@]}"; do
    if [[ "$sf" == *"$expected" ]]; then
      match="$sf"
      break
    fi
  done
  if [[ -n "$match" ]]; then
    JS_TOTAL_COVERED=$((JS_TOTAL_COVERED + ${JS_COVERED["$match"]:-0}))
    JS_TOTAL_VALID=$((JS_TOTAL_VALID + ${JS_VALID["$match"]}))
    JS_FILES=$((JS_FILES + 1))
  fi
done

if [[ "$JS_FILES" -ne 5 ]]; then
  echo "ERROR: expected exactly 5 production JS files in lcov, found $JS_FILES" >&2
  exit 3
fi

JS_PCT=$(awk -v c="$JS_TOTAL_COVERED" -v v="$JS_TOTAL_VALID" 'BEGIN{printf "%.2f", (v>0) ? 100*c/v : 0}')
echo "COVERAGE_JS covered=$JS_TOTAL_COVERED valid=$JS_TOTAL_VALID pct=$JS_PCT files=$JS_FILES"

if awk -v p="$JS_PCT" -v m="$COVERAGE_JS_MIN" 'BEGIN{exit !(p+0 < m+0)}'; then
  echo "FAIL: JS coverage ${JS_PCT}% below floor ${COVERAGE_JS_MIN}%" >&2
  GATE_FAILED=1
fi

if [[ "$GATE_FAILED" -eq 1 ]]; then
  exit 1
fi

exit 0
