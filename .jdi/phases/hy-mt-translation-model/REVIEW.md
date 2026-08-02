# Phase 20: Review  (slug: hy-mt-translation-model)

**Verdict:** APPROVED_WITH_WARNINGS

> Re-verify pass after warning-fix commit `d5162d3` (wraps the settings sheet's
> `VerticalStackLayout` in a vertical `ScrollView` in `SettingsOverlay.xaml` — closes the previous
> iteration's W4 reachability doubt). Full gate review re-run from scratch; nothing carried over
> from the prior REVIEW.

## Gates
| Gate | Status | Details |
|---|---|---|
| Build | PASS | `dotnet restore` + `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` — exit 0, `0 Error(s)`, 40 warnings (all pre-existing MVVMTK0045-class, none new, none in phase-touched files). Android/iOS not built: phase did not touch `Platforms/`. |
| Tests | PASS | Full suite `-c Release`: **Failed: 0, Passed: 346, Skipped: 2, Total: 348**. Pre-phase baseline derived from `git merge-base origin/main HEAD` (= `20f0328`) = 343 ([Fact]+[InlineData]); 348 = 343 + 5 new tests. The 2 skips are the pre-existing real-GGUF-gated `TranslationEngineTests`. No regression; name-by-name diff vs base empty (`comm -23`). |
| Coverage | PASS | Cobertura report `TestResults/e028c11a-.../coverage.cobertura.xml` (aggregate line-rate 0.9331, context only). New .cs files since boundary `4285f25` present in report: `BookTranslationResult.cs` (1.0), `ExtractedImage.cs` (1.0); `ChapterContentPurpose.cs` has no coverable lines (enum). Gate average over new files = **100% >= 90%**. Changed-code claim independently re-verified from the XML: `ModelAccess` class 1.0 line/1.0 branch; `TranslationManager` class + `<DownloadModelIfNeededAsync>d__15` + `<InitializeEngineIfNeededAsync>d__16` all 1.0/1.0. Only sub-1.0 branch entries (`TranslateChapterAsync`/`TranslateParagraphsAsync` at 0.833) are legacy methods untouched by this phase. |
| Lint | WARN | `dotnet format --verify-no-changes` exit 2. **Correction to the reviewer agent's claim**: the orchestrator independently re-ran the command and the 5 pre-existing WHITESPACE errors (`ThemeEngine.cs:12,14`, `ReaderPage.xaml.cs:122,124`, `ThemeEngineTests.cs:12`) are still present — the reviewer's "no longer present at all" was wrong (likely misread a truncated log). None of the 5 are in a file this phase touches (`ModelAccess.cs`, `TranslationManager.cs`, `IModelAccess.cs`, `SettingsOverlay.xaml(.cs)`, `THIRD-PARTY-NOTICES.md`) — confirmed via `git stash`/diff, so it's legacy drift, not a regression. Gate 4 is WARN-only per the reviewer spec regardless, so this correction does not change the verdict. |
| Security/Layer | PASS (warnings) | 5.1 client->Access/Engines: clean — only hits are `MauiProgram.cs` (composition root, allowed). 5.2 storage tech in Contracts: clean. 5.3 Manager->Manager: clean — every `I*Manager` hit is a Manager implementing its OWN interface; `TranslationManager` ctor injects `ISettingsAccess` (ResourceAccess contract), NOT `ISettingsManager`, exactly as D-...-4 locked (re-confirmed at `TranslationManager.cs:21`). 5.10 sync-over-async: clean — the two `.Result` hits in `LibraryPageModel.cs` are `popupResult.Result` (popup record property, not `Task.Result`). 5.11: this round's diff touches only `SettingsOverlay.xaml`; `SettingsOverlay.xaml.cs` has zero `+=` subscriptions (it only declares/invokes its own events) — no new imbalance. 5.12: `ModelRegistry` still `private static readonly IReadOnlyDictionary<string, ModelInfo>` with `StringComparer.Ordinal` (`TranslationManager.cs:35-40`); `GemmaModel`/`HyMtModel` `static readonly`; only mutable static in src is the known legacy `TranslationEngine._nativeLibraryConfigured:16` baseline (not re-flagged). 5.15: no empty catch or Result-pattern in phase code; `GetModelPath` still throws `FileNotFoundException` without user-profile path in the message. 5.16: 8-param ctor -> W1 (known/accepted/locked, not re-flagged as new). 5.17: all 11 `Substitute.For<>` targets in tests are interfaces (Contracts only); `ModelAccessTests` download tests use `StubHttpMessageHandler` (no real network); temp-dir fixture is the pre-phase pattern. XAML spot-check of `d5162d3`: new vertical `<ScrollView>` (default orientation) wraps the sheet at lines 24/261; the horizontal `ScrollView` around the 4 model buttons (lines 193-236) is byte-intact inside it; `HyMtModelButton` (line 225) and the "Powered by Tencent HY" attribution label (line 237) unchanged — nothing the DoD 4 grep depends on moved. |
| Consistency | PASS | Files across the last 10 commits match PLAN's files_modified exactly (7 files, nothing else). New commit `d5162d3` is a warning-fix round commit: type `fix`, scope = phase slug (`hy-mt-translation-model`), single-file diff (+2 lines in `SettingsOverlay.xaml`) matching its message; targets the prior W4 exactly, no scope creep. Earlier task commits (`167f21d`, `ea13ec6`, `882107b`, `9c9a09d`, `4f5d90c`) unchanged from the previous review. |
| UI Validation | SKIPPED | has_frontend=false |
| DoD | PASS | 6/6 auto, 0 manual — every `Verify:` re-run VERBATIM from CONTEXT.md this iteration, all exit 0 (table below). |

## Blockers
None.

## Warnings
1. **W1 — 8-parameter `TranslationManager` primary constructor** (`src/TranslateReader.Core/Business/Managers/TranslationManager.cs:13-21`), one over the csharp.md §7 / Sonar S107 ceiling of 7. KNOWN, ACCEPTED, LOCKED exposure forced by decision D-...-4 + the DoD 6 exact-diff-scope gate (a parameter object would need a new Core file or a `MauiProgram.cs` change, both prohibited). Permanent for this phase; reported for PR-review visibility only — NOT a blocker.
2. **W2 — legacy fail-fast drift (pre-existing, untouched this phase, adopted=true -> non-blocking):** `catch { }` at `src/TranslateReader/Pages/ReaderPage.xaml.cs:326` and `:434`; swallowed `OperationCanceledException` at `src/TranslateReader/PageModels/LibraryPageModel.cs:183`, `src/TranslateReader/PageModels/ReaderPageModel.cs:222`, `src/TranslateReader/Pages/ReaderPage.xaml.cs:308` (csharp.md §1 says OCE always flows). All pre-date the phase (DoD 6 proves the app-side diff is limited to `SettingsOverlay.xaml(.cs)`).
3. **W3 — legacy PageModels inject >1 Manager** (`LibraryPageModel` 2, `ReaderPageModel` 3 — vs CLAUDE.md rule "max 1 Manager per use case"). Pre-existing, not touched by this phase.

**Resolved this round:** the previous iteration's **W4** (license/attribution reachability in a non-scrolling bottom sheet) is CLOSED by `d5162d3` — the whole settings sheet now sits inside a vertical `ScrollView`, so the HY-MT button and the attribution label are reachable by scroll on any viewport height regardless of section count. The generic on-device smoke test (download hy-mt for real, translate a paragraph) remains a CONTEXT "Deferred to PR review" item, not a warning against this phase.

## DoD Checklist (gate 8)
| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | Registry has both real entries with session-validated values; wrong card number absent; typed dictionary | CONTEXT (D-...-2, D-...-4) | Auto | PASS | Verbatim grep chain over `TranslationManager.cs` exit 0: `Name: "hy-mt1.5-1.8b"`, `FileName: "HY-MT1.5-1.8B-Q4_K_M.gguf"`, exact resolve URL, `SizeBytes: 1_133_080_512`, `Name: "gemma-2-2b"`, `SizeBytes: 1_629_413_888`, zero `1_213_000_000`, `IReadOnlyDictionary<string, ModelInfo>` matched |
| 2 | Settings-driven resolution real: hy-mt selected uses hy-mt URL/file, unknown name falls back to gemma; 7 filter tests pass | CONTEXT (D-...-4) | Auto | PASS | Verbatim command exit 0 — `TestResults/dod2.log`: `Passed! - Failed: 0, Passed: 7, Total: 7` (floor n=7 met) |
| 3 | `IsModelAvailable`/`GetModelPath` filename-aware; 8 filter tests incl. the 2 foreign-gguf difference tests | CONTEXT (D-...-4) | Auto | PASS | Verbatim command exit 0 — contract signatures matched in `IModelAccess.cs`; `TestResults/dod3.log`: `Passed! - Failed: 0, Passed: 8, Total: 8` (floor n=8 met) |
| 4 | License visible and reachable: `THIRD-PARTY-NOTICES.md` with real clauses; 4th button + attribution in `SettingsOverlay`; app builds | CONTEXT (D-...-3) | Auto | PASS | Verbatim command exit 0 AFTER the `d5162d3` structural change — all 6 notice strings + `HyMtModelButton` + `OnHyMtClicked` + `"hy-mt1.5-1.8b"` matched; `TestResults/dod4.log`: `0 Error(s)`. Reachability now structurally honored on BOTH axes: horizontal `ScrollView` for the button row (R5) + vertical `ScrollView` for the whole sheet (`d5162d3`); spot-check confirmed the wrap moved nothing the grep depends on |
| 5 | Full suite no regression: Failed 0, Total >= B+5, Skipped <= S, sum coherent, no base test name missing | CONTEXT | Auto | PASS | Verbatim command exit 0 — B=343, S=2, floor=348; `TestResults/dod5.log`: `Failed: 0, Passed: 346, Skipped: 2, Total: 348`; `comm -23 base head` empty |
| 6 | Diff scope closed: Engine/PromptUtility/ITranslationManager/ModelInfo untouched; app diff = SettingsOverlay only; Core diff = exactly 3 files; notices file exists | CONTEXT (D-...-4/5/6) | Auto | PASS | Verbatim command exit 0 — Core diff is exactly `Access/ModelAccess.cs,Business/Managers/TranslationManager.cs,Contracts/Access/IModelAccess.cs,`; excluded-file diffs empty (`d5162d3` touches only `SettingsOverlay.xaml`, which the scope check explicitly permits) |

**Totals:** 6/6 auto PASS, 0 manual pending, 0 FAIL.

## Recommendation
Ship it. This re-verify confirms the warning-fix round did exactly one thing and did it safely:
`d5162d3` adds a vertical `ScrollView` around the settings sheet (+2 lines, one file), closing the
DoD critic's W4 reachability doubt without disturbing the horizontal button-row `ScrollView`, the
grep anchors, the build, the suite (348/348 accounted, 0 failed), or the DoD 6 diff-scope contract.
All blocking gates green, all 6 DoD `Verify:` commands re-run verbatim and pass, coverage
independently re-confirmed at 100% on new/changed code from the Cobertura XML, and every locked
decision (ISettingsAccess not ISettingsManager, static readonly Ordinal registry, explicit gemma
fallback, untouched Engine/PromptUtility/ITranslationManager/ModelInfo) remains honored. The
remaining warnings are W1 (permanent, decision-forced 8-param ctor) and W2/W3 (legacy-only,
adopted=true) — expected and non-blocking. CONTEXT "Deferred to PR review" items stay live for the
human reviewer: EU/UK/South Korea residual license risk (no geo-gating, D-...-3), hy-mt at uniform
Temperature=0.1 vs vendor-recommended 0.7 (D-...-5), on-device download/translation smoke test,
SonarCloud pending push+CI, and legal wording unreviewed by counsel.
