# Phase 2: Cobertura e CI — Summary (slug: cobertura-e-ci)

**Status:** complete
**Tasks:** 7/7 complete, 0 blocked

## Executed tasks
- T-1: `scripts/coverage-gate.sh` created (bash, line-weighted, `AM` scope post-`4285f25`).
  Measured the starting number BEFORE any block existed, per the stop rule: **95.28%** C#
  (1090/1144 lines) — above the 90% floor, so no BLOCKED. Commit `0ac1f1d`.
- T-2: JS block added (`COVERAGE_JS_MIN` default 85%, the 4 WebView production scripts).
  Measured **100%** JS (318/318 lines). Both floors printed before the exit decision. Commit
  `0bf9ae3`.
- T-3: MAUI app guard (new `.cs` under `src/TranslateReader/` fails exit 2 unless waived) +
  `.jdi/coverage-waivers.txt` created with zero live entries. DoD 3 red-then-green probe verified
  (exit 2 -> waived -> exit 0, tree restored). Commit `9f9eb63`.
- T-4: `.github/workflows/coverage.yml` (new, `workflow_call`-only, hardened, reportgenerator
  summary + HTML artifact), `pipeline.yml` gained the `coverage` caller job, `ci.yml` dropped
  `--collect:"XPlat Code Coverage"` and its artifact upload. Commit `1351642`.
- T-5: Reviewer Gate 3 rewritten to call `scripts/coverage-gate.sh` (removed the
  `Measure-Object -Average` PowerShell implementation and the `--diff-filter=A`/"SKIPPED when no
  new file" framing). `coverlet.collector` version corrected 8.0.1 -> 10.0.1 across
  `PROJECT.md`, `LEGACY.md`, `jdi-doer-translatereader.md`, `jdi-reviewer-translatereader.md`.
  Zero severity change on gates 1/2/4/5. Commit `8ebb1d0`.
- T-6: `branch-protection-before.json` snapshot (9 required contexts, captured 2026-08-09,
  nothing in T-1..T-5 mutated protection) + `branch-protection-remap.md` documenting the naming
  conflict between the two derivation candidates (**not executed**, `PATCH` deferred). Commit
  `337e10a`.
- T-7: Pushed the branch, opened PR #28, waited for the real `Build (Android)` CI run, and
  recorded the measured result (see below — one warning ID found, added to `Directory.Build.props`
  `<NoWarn>`). Commit `53b58fc`.

## Blocked tasks
- none

## Starting number (T-1, measured 2026-08-09 — never estimated)
```
COVERAGE_SCOPE covered=1090 valid=1144 pct=95.28 files=17
COVERAGE_JS    covered=318  valid=318  pct=100.00 files=4
COVERAGE_GUARD new_app_cs=0 waived=0
```
Both above their floors (90% C#, 85% JS) on the very first measurement — no waiver of Core files
was needed, no floor was lowered.

## COVERAGE_SKIP (18 files, all explained)
**Core, `reason=no-instrumented-lines`** (interfaces/enum, nothing for the compiler to instrument):
- `src/TranslateReader.Core/Contracts/Access/IModelAccess.cs`
- `src/TranslateReader.Core/Contracts/Engines/IParsingEngine.cs`
- `src/TranslateReader.Core/Contracts/Managers/ILibraryManager.cs`
- `src/TranslateReader.Core/Contracts/Managers/ITranslationManager.cs`
- `src/TranslateReader.Core/Contracts/Utilities/IFileUtility.cs`
- `src/TranslateReader.Core/Models/ChapterContentPurpose.cs`

**App MAUI, `reason=app-maui-not-instrumented`** (ruling in PLAN.md: modified app `.cs` is out of
the coverage run because `test/TranslateReader.Tests` only references `TranslateReader.Core`;
none of these 12 files are new since the boundary, so the T-3 guard does not fire on them):
- `src/TranslateReader/App.xaml.cs`, `MauiProgram.cs`
- `src/TranslateReader/PageModels/LibraryPageModel.cs`, `ReaderPageModel.cs`
- `src/TranslateReader/Pages/Controls/SettingsOverlay.xaml.cs`, `TranslateBookPopup.xaml.cs`
- `src/TranslateReader/Pages/LibraryPage.xaml.cs`, `ReaderPage.xaml.cs`
- `src/TranslateReader/Platforms/MacCatalyst/AppDelegate.cs`, `Program.cs`
- `src/TranslateReader/Platforms/Windows/App.xaml.cs`
- `src/TranslateReader/Platforms/iOS/AppDelegate.cs`, `Program.cs`

No `COVERAGE_WAIVER_INVALID` lines — the waiver file has zero entries to validate.

## Waiver file (final content)
`.jdi/coverage-waivers.txt` — header/format documentation only, **zero live waiver entries**. The
app has not gained a new `.cs` file since the boundary commit `4285f25`, so nothing needed a
waiver in this phase. The DoD 3 probe added and removed one temporary line during verification;
the tracked file is unchanged (`git diff --quiet` confirmed after every DoD 3 run).

## Android result (W-2, T-7 — real measurement, not guessed)
- PR: https://github.com/slipalison/TranslateReader/pull/28
- Run: https://github.com/slipalison/TranslateReader/actions/runs/31317291066 (job
  `Build (Android)`: https://github.com/slipalison/TranslateReader/actions/runs/31317291066/job/93254482136)
- Head SHA measured: `337e10ae32ad9f75037c062a5cf6d180fa61c8f9`
- **The job passed, but that alone was not proof of zero warnings** — `TreatWarningsAsErrors=true`
  governs the C# compiler, not every MSBuild task in the Android packaging pipeline. Reading the
  actual job log surfaced one warning a green build had hidden:
  - `XA4301` (2x, `arm64-v8a` + `x86_64`) — "APK already contains the item
    lib/<abi>/libe_sqlite3.so; ignoring." Benign: `Microsoft.Data.Sqlite.Core` and the explicit
    `SQLitePCLRaw.bundle_green` reference both ship the same native binary; the Android linker
    de-dupes it at pack time. Not RISCO.
- Resolution: `XA4301` added to the existing single, closed, per-ID `<NoWarn>` in
  `Directory.Build.props` with its own comment (preference order D-...-5(3), option (i)).
  `TreatWarningsAsErrors` stays `true`, unconditioned, at the root — option (ii) was not needed.
  `.jdi/phases/cobertura-e-ci/android-warnings.md` records the full detail.
- Verified after the change: `dotnet build -f net10.0-windows10.0.19041.0` still 0 warnings/0
  errors, `dotnet test` still 373 passed / 2 skipped / 0 failed (375 total, baseline preserved).

**Note for the coordinator:** the earlier read of "all CI checks green" as "zero IDs novos" was an
oversimplification — a green `Build (Android)` job does not by itself prove zero warning IDs fired
(XA-prefixed warnings from the Android packaging step are not subject to
`TreatWarningsAsErrors`). The actual log was read and the real result (one ID) is what got
recorded, per the phase's own "nao invente resultado" rule.

## DoD results (9/9, all literal, all exit 0)
| DoD | Verify | Result |
|---|---|---|
| 1 | Gate exists, executable (`100755`), fails on `COVERAGE_MIN=101`, sentinel wiped every run | PASS |
| 2 | `AM` scope, line-weighted (not averaged), `pct == 100*covered/valid` within tolerance | PASS |
| 3 | Red-then-green app guard probe, tree clean after | PASS |
| 4 | JS measured by the gate itself, 4 files, 85% floor, Sonar's `js-lcov.info` path untouched | PASS |
| 5 | `coverage.yml` pure `workflow_call`, hardened, pinned SHAs, `pipeline.yml` caller, `ci.yml` decollection | PASS |
| 6 | reportgenerator summary + artifact, no duplicate artifact name across all workflows | PASS |
| 7 | Android W-2 measured and recorded, `NoWarn`/`TreatWarningsAsErrors` invariants preserved | PASS |
| 8 | Reviewer calls the script, no `Measure-Object -Average`, `coverlet.collector` version fixed everywhere | PASS |
| 9 | Branch protection baseline + remap doc, naming conflict documented, literal present | PASS |

## Files modified (all commits)
- `scripts/coverage-gate.sh` (new)
- `.jdi/coverage-waivers.txt` (new)
- `.github/workflows/coverage.yml` (new)
- `.github/workflows/pipeline.yml`, `.github/workflows/ci.yml`
- `.jdi/agents/jdi-reviewer-translatereader.md`, `.jdi/agents/jdi-doer-translatereader.md`
- `.jdi/registry/LEGACY.md`, `.jdi/registry/LEGACY-reviewers.md`, `.jdi/PROJECT.md`
- `.jdi/phases/cobertura-e-ci/branch-protection-before.json` (new)
- `.jdi/phases/cobertura-e-ci/branch-protection-remap.md` (new)
- `.jdi/phases/cobertura-e-ci/android-warnings.md` (new)
- `Directory.Build.props` (T-7, measurement-driven — anticipated by the PLAN as conditional)

## Tests
- Total: 375
- Passing: 373 (2 skipped, pre-existing `TranslationEngineTests` model-dependent skips — baseline
  unchanged, no regression from the 167-test floor)
- Failing: 0
- Coverage: 95.28% C# (`AM` scope) / 100% JS — both above the 90%/85% floors

## PR and CI
- PR #28 (`feat/cobertura-e-ci` -> `main`): https://github.com/slipalison/TranslateReader/pull/28
- CI run (head `53b58fc` includes T-7; the measured run for W-2 was `337e10a`, one commit
  earlier — the T-7 commit only touches docs/`Directory.Build.props`, not `.cs`, so it does not
  invalidate the Android measurement) — all checks reported green on the `337e10a` run: `CI /
  Test (Linux)`, `CI / Build (Windows)`, `CI / Build (Android)`, `Coverage / Coverage gate`,
  `CodeQL`, `Semgrep`, `SCA`, `Secret Scan` (Gitleaks + TruffleHog), `SonarQube`, `Dependency
  Review`. `SBOM` reported `skipping` as expected (its caller is `if: github.event_name ==
  'push'`, PRs do not trigger it).
- No unrelated pre-existing failures observed — nothing to report as "broken before this phase."
- **Confirmed the `Coverage / Coverage gate` check name empirically**: this matches "Candidate B"
  in `branch-protection-remap.md` (derived from the `pipeline-unificada` precedent: `<caller job
  name> / <inner job name>`), not "Candidate A" (`Pipeline / Coverage`, the literal the DoD 9
  Verify script tests against the YAML structure). The remap doc conflict section already
  anticipated this — the `PATCH` was never written with either guess; it stays deferred to PR
  review with the real captured name (`Coverage / Coverage gate`).

## Deferred to PR review (copied into the PR #28 body)
- Raise the SonarCloud Quality Gate `new_coverage` threshold 80% -> 90% (D-...-1(b)) — lives in
  the SonarCloud UI, not a repo file; confirm/apply after merge.
- Real branch protection mutation: add `Coverage / Coverage gate` (the name actually captured
  from the PR #28 run) as a required context. Needs an admin token and must run only after the
  check has reported at least once — it has now, so this is unblocked for the next
  admin-authenticated session, but still intentionally not executed by this doer run (D-...-5(4a)
  keeps mutation out of the doer hands).
- Real `build-android` result (W-2): recorded above — one benign warning (`XA4301`), suppressed
  via `<NoWarn>` with `TreatWarningsAsErrors` unchanged.
- Confirm PR duration did not double (coverage collection moved out of `ci.yml` into its own
  parallel job) — `CI / Test (Linux)` ran in 44s on this PR vs. historically comparable runs;
  `Coverage / Coverage gate` ran in parallel (55s), so the wall-clock PR gate time is a wash, not
  additive.
