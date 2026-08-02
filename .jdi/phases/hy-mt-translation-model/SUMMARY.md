# Phase 20: Modelo de traducao hy-mt1.5-1.8b - Summary (slug: hy-mt-translation-model)

## Status
All 6 tasks completed. All 6 DoD `Verify:` commands from CONTEXT.md ran verbatim and pass. Full
regression suite green (346 passed, 2 skipped [pre-existing, real-model-required], 0 failed).

## Pre-flight (before T-1)
```
BASE=$(git merge-base origin/main HEAD); git diff --name-only "$BASE" -- src/TranslateReader.Core/ src/TranslateReader/
```
Output: empty. Proceeded with T-1..T-6 in order.

## Tasks X/Y complete
6/6 complete. 0 blocked.

## Executed tasks

### T-1 (commit 167f21d) - RED test for the "any *.gguf" bug
Added `IsModelAvailable_ReturnsFalseWhenADifferentGgufFileExists` and
`GetModelPath_ThrowsWhenOnlyADifferentGgufFileExists` to `ModelAccessTests.cs`, written against
today's parameterless `IsModelAvailable()`/`GetModelPath()`. RED transcript captured before any
production change:

```
DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~ModelAccessTests"
...
[xUnit.net 00:00:00.21]     TranslateReader.Tests.ModelAccessTests.IsModelAvailable_ReturnsFalseWhenADifferentGgufFileExists [FAIL]
  Error Message:
   Assert.False() Failure
Expected: False
Actual:   True
[xUnit.net 00:00:00.24]     TranslateReader.Tests.ModelAccessTests.GetModelPath_ThrowsWhenOnlyADifferentGgufFileExists [FAIL]
  Error Message:
   Assert.Throws() Failure: No exception was thrown
Expected: typeof(System.IO.FileNotFoundException)

Failed!  - Failed:     2, Passed:    15, Skipped:     0, Total:    17, Duration: 95 ms - TranslateReader.Tests.dll (net10.0)
```
Both new names visible, the 6 pre-existing `IsModelAvailable`/`GetModelPath` tests untouched and
still passing (15/17 = 6 pre-existing model-access tests + 9 download/delete tests).

### T-2 (commit ea13ec6) - THIRD-PARTY-NOTICES.md
Created `THIRD-PARTY-NOTICES.md` at repo root with the real Tencent HY Community License Agreement
clauses (territorial exclusion EU/UK/South Korea, "Powered by Tencent HY" attribution, non-affiliation
disclaimer, license-copy/notice-file obligation). Wording confirmed via a fresh WebFetch of
https://huggingface.co/tencent/HY-MT1.5-1.8B-GGUF/blob/main/License.txt this iteration (1 of the
2-lookup budget used), not invented. Scoped to the Tencent HY entry only, per acceptance.

### T-3 (commit 882107b) - HY-MT button + attribution in SettingsOverlay
Added `HyMtModelButton` (`Text="HY-MT 1.8B"`, `Clicked="OnHyMtClicked"`) cloning the 3 existing model
buttons' attributes. Wrapped the model-button `HorizontalStackLayout` in
`<ScrollView Orientation="Horizontal" HorizontalScrollBarVisibility="Never">` so the 4th button stays
reachable on a narrow viewport (R5). Added an attribution `<Label>` below the button group containing
"Powered by Tencent HY", non-affiliation, and a pointer to `THIRD-PARTY-NOTICES.md`. Code-behind gained
`OnHyMtClicked` (mirrors `OnQwenClicked`, sets `"hy-mt1.5-1.8b"`) and `UpdateModelButtonBorders` gained
the `HyMtModelButton` line. No other file under `src/TranslateReader/` touched; `MauiProgram.cs`
untouched (DI resolves the new Manager ctor parameter automatically).
Build gate: `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0`
resulted in `0 Error(s)` (40 pre-existing warnings, none new). The 2 T-1 red tests were still red at
this point, as expected - this task's gate is the build, not the suite.

### T-4 (commit 9c9a09d) - filename-aware IModelAccess/ModelAccess
`IModelAccess.IsModelAvailable(string fileName)` / `GetModelPath(string fileName)` (exact contract
text, 1-line `<summary>` each; `DownloadModelAsync`/`DeleteModelAsync` untouched). `ModelAccess` now
does `File.Exists(Path.Combine(_modelsDirectory, fileName))` and an exact-path `GetModelPath`, dropping
`Directory.EnumerateFiles(..., "*.gguf")` entirely - this closes the T-1 bug. `GetModelPath` still
fails fast with `FileNotFoundException`, message carries no user-profile path. `TranslationManager`
call sites adapted minimally: `IsModelAvailable(DefaultModel.FileName)` /
`GetModelPath(DefaultModel.FileName)` (registry/settings wiring deferred to T-5, as planned). The 6
existing `ModelAccessTests` now create/check `ModelFileName` instead of the old `model.gguf` literal;
the 2 T-1 tests went green by adding `(ModelFileName)`. `TranslationManagerTests` 3
`IsModelAvailable`/`GetModelPath` stubs mechanically switched to `Arg.Any<string>()`.
Gate: `--filter "FullyQualifiedName~ModelAccessTests.IsModelAvailable|FullyQualifiedName~ModelAccessTests.GetModelPath"`
resulted in Passed:8, Failed:0, Total:8. Coverage on both changed methods: 100% line, 100% branch.

### T-5 (commit 4f5d90c) - ModelRegistry + ResolveModel + settings-driven selection
`DefaultModel` renamed to `GemmaModel`; new `HyMtModel` (Name `hy-mt1.5-1.8b`, FileName
`HY-MT1.5-1.8B-Q4_K_M.gguf`, DownloadUrl to the `tencent/HY-MT1.5-1.8B-GGUF` resolve URL, SizeBytes
`1_133_080_512`, the measured value - the card's `1_213_000_000` does not appear anywhere). Both live
in `private static readonly IReadOnlyDictionary<string, ModelInfo> ModelRegistry` keyed by `Name`,
`StringComparer.Ordinal`. `ResolveModel(string modelName)` does `TryGetValue` with an explicit fallback
to `GemmaModel` (1-line WHY comment: Qwen/Phi have no real URL yet, D-...-4). `ISettingsAccess
settingsAccess` added as the constructor's 8th/last parameter (Manager to ResourceAccess, permitted).
`DownloadModelIfNeededAsync` and `InitializeEngineIfNeededAsync` now call `FetchSettingsAsync()`,
resolve the model, and use its `FileName`/`DownloadUrl`; `InitializeEngineIfNeededAsync` keeps the
`IsReady` guard-clause check before touching settings, so the already-ready path does zero I/O
(verified by the pre-existing `InitializeEngineIfNeededAsync_WhenEngineAlreadyReady_SkipsInitialization`
test, which still passes unmodified - it never stubs `_settingsAccess`).

RED-first transcript, captured with the constructor already wired to accept `ISettingsAccess` but the
method bodies still hard-coded to `GemmaModel` (pre-ResolveModel):
```
DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~DownloadModelIfNeededAsync_WhenSettingsSelectHyMt_DownloadsTheHyMtUrl|FullyQualifiedName~DownloadModelIfNeededAsync_WhenSettingsSelectAnUnregisteredModel_FallsBackToGemma|FullyQualifiedName~InitializeEngineIfNeededAsync_WhenSettingsSelectHyMt_UsesTheHyMtFileName"
...
Failed TranslateReader.Tests.TranslationManagerTests.InitializeEngineIfNeededAsync_WhenSettingsSelectHyMt_UsesTheHyMtFileName
  NSubstitute.Exceptions.ReceivedCallsException : Expected to receive exactly 1 call matching: FetchSettingsAsync() - Actually received no matching calls.
Failed TranslateReader.Tests.TranslationManagerTests.DownloadModelIfNeededAsync_WhenSettingsSelectHyMt_DownloadsTheHyMtUrl
  NSubstitute.Exceptions.ReceivedCallsException : Expected to receive exactly 1 call matching: FetchSettingsAsync() - Actually received no matching calls.
Failed TranslateReader.Tests.TranslationManagerTests.DownloadModelIfNeededAsync_WhenSettingsSelectAnUnregisteredModel_FallsBackToGemma
  NSubstitute.Exceptions.ReceivedCallsException : Expected to receive exactly 1 call matching: FetchSettingsAsync() - Actually received no matching calls.

Failed!  - Failed:     3, Passed:     0, Skipped:     0, Total:     3, Duration: 138 ms - TranslateReader.Tests.dll (net10.0)
```
All 3 assertions verify the literal HY-MT/Gemma URL or filename (never Arg.Any in the assertion),
plus an explicit `Received(1).FetchSettingsAsync()` - chosen precisely because, without that call
check, the "unregistered model falls back to Gemma" test would have coincidentally passed even before
`ResolveModel` existed (old code always used Gemma), defeating the RED requirement. After implementing
`ResolveModel` and wiring both methods: same filter resulted in Passed:7, Failed:0, Total:7. Coverage
on `DownloadModelIfNeededAsync`/`InitializeEngineIfNeededAsync`/`ResolveModel`: 100% line, 100% branch
(all folded into the top-level `TranslationManager` class coverage entry since `ResolveModel` is not
an async state machine).

R3 - known exposure for PR review: `TranslationManager` primary constructor now takes 8
parameters, over the csharp.md section 7 / SonarS107 ceiling of 7. This is a forced consequence of locked
decisions D-...-4 + DoD 6: a parameter object would require either a new file in
`TranslateReader.Core` (not in T-5's files_modified, and DoD 6's exact-diff-scope check would fail)
or a change to `MauiProgram.cs` (explicitly prohibited by T-3's acceptance and DoD 6). Not refactored
around; flagged here as intentional debt for the PR reviewer to accept or route to a future phase.

### T-6 (no commit - no versioned diff) - final DoD run + scope closure
Ran all 6 CONTEXT `Verify:` commands verbatim via Bash. Results below. No file under version control
was touched by this task (only `TestResults/*.log`, already covered by `.gitignore:18`).

## DoD verification (all 6, run verbatim this iteration)

| DoD | Command (verbatim from CONTEXT.md) | Result |
|---|---|---|
| 1 - registry literals | full grep chain over TranslationManager.cs, incl. IReadOnlyDictionary regex | PASS (exit 0), re-verified after dotnet format per R4 |
| 2 - settings resolution | filter DownloadModelIfNeededAsync / InitializeEngineIfNeededAsync + awk floor n=7 | PASS - Passed: 7, Failed: 0, Total: 7 (floor met exactly) |
| 3 - filename-aware ModelAccess | filter ModelAccessTests.IsModelAvailable / ModelAccessTests.GetModelPath + awk floor n=8 | PASS - Passed: 8, Failed: 0, Total: 8 (floor met exactly) |
| 4 - license + UI + build | grep chain over THIRD-PARTY-NOTICES.md/SettingsOverlay.xaml(.cs) + Windows TFM build | PASS - 0 Error(s) |
| 5 - full suite regression | awk floor tn=B+5, name-diff via comm -23 against origin/main | PASS - B=343, floor=348, actual Passed:346, Skipped:2, Failed:0, Total:348; no base test name missing from HEAD |
| 6 - diff scope | exact Core diff list + excluded files empty + app-side diff restricted to SettingsOverlay | PASS - Core diff is exactly Access/ModelAccess.cs,Business/Managers/TranslationManager.cs,Contracts/Access/IModelAccess.cs,; TranslationEngine.cs/PromptUtility.cs/ITranslationManager.cs/ModelInfo.cs diff empty |

All 6 commands were executed exactly as printed in CONTEXT.md (no substitutions), each read the real
`Passed!`/floor via awk, never the bare `dotnet test` exit code.

## Files modified
- `THIRD-PARTY-NOTICES.md` (new)
- `src/TranslateReader.Core/Contracts/Access/IModelAccess.cs`
- `src/TranslateReader.Core/Access/ModelAccess.cs`
- `src/TranslateReader.Core/Business/Managers/TranslationManager.cs`
- `src/TranslateReader/Pages/Controls/SettingsOverlay.xaml`
- `src/TranslateReader/Pages/Controls/SettingsOverlay.xaml.cs`
- `test/TranslateReader.Tests/ModelAccessTests.cs`
- `test/TranslateReader.Tests/TranslationManagerTests.cs`

Matches DoD 6's exact-scope requirement; no other file touched. `.gitignore` and
`.jdi/phases/div-paragraph-reading/REVIEW.md` were left alone as instructed (no local diff on them
was present at execution time, and neither is in this phase's files_modified).

## Tests
- Total (full suite, -c Release): 348 - Passed 346, Skipped 2 (pre-existing, real-GGUF-model gated,
  unrelated to this phase), Failed 0.
- New tests this phase: 5 exactly - 2 in `ModelAccessTests.cs` (T-1), 3 in `TranslationManagerTests.cs`
  (T-5). Base (origin/main) test count: 343. 343 + 5 = 348 = actual total. No base test name missing
  from HEAD (comm -23 empty).
- Coverage on changed/new production code (`ModelAccess.IsModelAvailable`/`GetModelPath`,
  `TranslationManager.DownloadModelIfNeededAsync`/`InitializeEngineIfNeededAsync`/`ResolveModel`):
  100% line, 100% branch (Cobertura line-rate=1, branch-rate=1 on every touched class/state
  machine) - exceeds the 90% D-6 floor.

## Blocked tasks
None.

## Notes for PR review (from CONTEXT.md Deferred to PR review section, restated here for visibility)
- Legal/product: no geo-gating for EU/UK/South Korea users selecting hy-mt1.5-1.8b (D-...-3, YAGNI -
  no location infra exists). Tracked in `.jdi/todos/2026-08-01-hy-mt-translation-model.md`.
- Quality: hy-mt runs at the uniform Temperature=0.1 instead of the vendor-recommended 0.7
  (D-...-5, deliberate, not measured this phase).
- R3 - TranslationManager now has an 8-parameter constructor, above the csharp.md section 7 / S107
  ceiling. Forced by D-...-4 + DoD 6's exact-diff-scope gate (a parameter-object fix would need a new
  Core file or a MauiProgram.cs change, both out of this task's files_modified). Not a doer
  oversight - accept or schedule a follow-up phase.
- No on-device confirmation that hy-mt1.5-1.8b actually downloads/translates coherently - no harness
  in this environment (same limitation as prior phases).
- SonarCloud has not run against this branch yet (only available after push+CI).