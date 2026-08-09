# Phase 1: Baseline de estilo — Review (slug: baseline-de-estilo, iter 2)

**Verdict:** APPROVED_WITH_WARNINGS

Reviewed on branch `feat/baseline-de-estilo` (not pushed, nothing committed by this review).
Anchor `BASELINE = cbf92481d996dab1dd22ffaac7d9f01972712f0d`, 13 commits `cbf9248..c2d83b4`.
Every `Verify:` below was copied literally from `CONTEXT.md` and re-executed by the reviewer —
none of the results are taken from `SUMMARY.md`.

## DoD 1-8 results

| DoD | Result | Evidence (re-run by the reviewer) |
|---|---|---|
| 1 — `.editorconfig` at root, explicit severity | **PASS** (exit 0) | `head -1` = `root = true` (no BOM, no leading comment); `[*.cs]`, `end_of_line = lf`, `dotnet_diagnostic.IDE0055.severity = suggestion` all present; file tracked |
| 2 — `Directory.Build.props` really flows to the 3 projects | **PASS** (exit 0) | MSBuild evaluation (`-getProperty:`/`-getItem:`) on Core, Tests and app (`-p:TargetFramework=net10.0-windows10.0.19041.0`): `EnableNETAnalyzers=true`, `EnforceCodeStyleInBuild=true`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended`, `Meziantou.Analyzer` 3.0.141 in all three |
| 3 — `.gitattributes` + repo 100% LF | **PASS** (exit 0) | `* text=auto eol=lf` is the first rule; 9 binary extensions declared; `git ls-files --eol` has zero `i/crlf` and zero `i/mixed` |
| 4 — `dotnet format whitespace` clean repo-wide | **PASS** (exit 0) | `dotnet format whitespace --verify-no-changes` → exit 0, `TestResults/bde-format.log` empty. Discovered the `.slnx` without the csproj fallback |
| 5 — whole phase is semantics-zero in code | **PASS** (exit 0) | `git diff --ignore-all-space --ignore-blank-lines --ignore-cr-at-eol cbf9248 -- '*.cs' '*.xaml' '*.js'` → empty |
| 6 — build green WITH warnings-as-errors + closed `NoWarn` | **PASS** (exit 0) | app (Windows Release) and Tests (Release): **`0 Error(s)` and `0 Warning(s)` in both**; zero `: warning (CS\|CA\|MA\|IDE)` lines in either log; exactly **1** `<NoWarn>` element, **21** concrete IDs, no wildcard, every ID on ≥ 2 lines of the file |
| 7 — .NET suite green + JS suite green | **PASS** (exit 0) | `Failed: 0, Passed: 373, Skipped: 2, Total: 375` (floor `≥375 / 0 / ≤2` met, no drop from the 167-test D-2 baseline); `node --test test/js/` → `tests 79, pass 79, fail 0` |
| 8 — process debt closed and CI untouched | **PASS** (exit 0) | 0 `WARN.only` and 0 `Tighten to BLOCK` in the reviewer; 0 `WARN.only` in both registries; 0 `quando a phase` in `LEGACY-reviewers.md`; 0 `custom analyzers exist yet` in the doer; literal `RESOLVIDO em baseline-de-estilo` present in `todos/LEGACY.md`; `git diff --name-only cbf9248 -- .github/` empty |

**8/8 auto PASS, 0 manual items.** The `Verify:` strings were not edited by the doer: the only
change to `CONTEXT.md` in this phase is commit `aa23502` (orchestrator, same commit as D-...-6),
which removed exactly one clause — `test "$(echo "$IDS" | wc -l)" -le 12` — from DoD 6. The rest of
that command is byte-identical to the original, and DoD 6's structural assertions (single element,
no wildcard, one comment line per ID) survived intact, as D-...-6(5) required.

**Diff audit — nothing undisclosed.** 22 files, 762 insertions / 40 deletions. The 7 `.cs` files in
the diff are whitespace/final-newline only (proven by DoD 5). Everything else is `.editorconfig`,
`.gitattributes`, `Directory.Build.props`, and `.jdi/` process artifacts. `.github/` is untouched.
The two conscious deltas from iteration 1 are both real and both correctly reasoned:
- `charset = utf-8` removed in its own commit `6f1ba2b` — with it, `dotnet format whitespace` strips
  the UTF-8 BOM from 6 files and git sees a content change on line 1, which breaks DoD 5. No
  decision and no DoD required `charset`. The reason is written into `.editorconfig` itself. Correct
  call: D-...-1(4) is locked, the key was not.
- Extra commit `fcb6f90` tracking `LOOP.md` — orchestrator artifact, committed alone so it does not
  contaminate the anchor or the renormalize commit. Consistent with every prior phase.

Commit hygiene (Gate 6): 13 commits, all `<type>(baseline-de-estilo): ...`, types correctly varied
(`chore`/`style`/`docs`/`fix`), one subject per commit, and the locked D-...-1(3) order
(gitattributes → editorconfig → props → format → docs) is respected in the log.

## Calibration vs NoWarn — judged against D-6

D-...-6(2)'s objective test: *if the rule were fixed instead of suppressed, would the code get
WORSE?* Yes → calibration. Better-but-laborious → debt → `NoWarn`. I applied it independently to
each of the 4, and I checked scope narrowness and the presence of the technical justification
comment above each key.

| ID | Scope in `.editorconfig` | D-6 test | Verdict |
|---|---|---|---|
| `CA1707` | `[test/**.cs]` | Renaming 344 xUnit tests off `Method_Scenario_Expected` destroys the readability of runner output; CA1707 is a *public-API* naming guideline and a non-shipped test assembly has no public API. Fixing = worse. | **Legitimate calibration** |
| `CA1711` | `[test/**.cs]` | Verified in source: `test/TranslateReader.Tests/ParsingEngineMemoryTests.cs:5-6` is `[CollectionDefinition("NonParallel", DisableParallelization = true)] public class NonParallelCollection;`. `<Name>Collection` is xUnit's own idiom for the marker; the rule exists to stop a consumer mistaking the type for an `ICollection`, and this type has no consumers. **Weakest of the four** — the class name is functionally arbitrary, so "fixing" it is cheap, and the honest answer is closer to "not worse, just pointless". But the *premise* of the rule genuinely does not hold here, which is the standard D-...-6(2) actually sets, and the scope is 1 hit inside `test/`. | **Accepted** (borderline, see W-4) |
| `MA0004` | `[src/TranslateReader/**.cs]` | Strongest of the four and not a judgment call: `.claude/rules/csharp.md` §3 states UI state changes only on the main thread, so `ConfigureAwait(false)` on continuations that touch pages, `[ObservableProperty]` setters and the WebView is a **defect**, not a style miss. The scope is the app project = the whole Client Layer, and it is literally one of the two example scopes D-...-6(2) names. Deliberately NOT extended to `Core/`. | **Legitimate calibration** |
| `CA1805` | `[src/TranslateReader.Core/Models/**.cs]` | Verified in source: `ReadingSettings` has 11 properties, all stating their default inline; `LetterSpacing` and `WordSpacing` are the 2 hits precisely because their default happens to be `0`. Removing `= 0` leaves the defaults table half-documented to save two field initialisations on an object built a handful of times. CA1805 is a perf rule whose premise (wasted work) does not hold on a POCO that *is* the readable spec. Scope is `Models/` only. | **Legitimate calibration** |

**No dodges found.** Three independent signals support this:

1. **Empirical proof the calibrations are load-bearing and correctly scoped.** `CA1707`, `CA1711`
   and `CA1805` are **not** in `NoWarn`. With `TreatWarningsAsErrors=true`, their 344 / 1 / 2 hits
   would be hard build *errors* if the `.editorconfig` sections did not match. Both builds report
   `0 Error(s)` and `0 Warning(s)` → the sections match exactly the folders claimed, and nothing
   broader (a wider glob would have been indistinguishable, but a narrower or wrong one would have
   failed the build).
2. **Calibration did not shorten the list to hit a target.** The cap was already revoked before the
   split was made; there was no number to reach. The doer explicitly *refused* three tempting
   candidates and wrote the losing argument down — `MA0074` ("recusada pela medicao": among the 138
   hits are `StartsWith`/`EndsWith`, whose defaults are culture-sensitive, so it is correctness not
   ceremony), `CA1859` (it retracted its own "concrete type would expose mutable static" argument
   after noticing csharp.md §2.1 already mandates `FrozenDictionary`, which closes the rule without
   harm), and `MA0016`/`MA0046` (declared technical ties, and "empate conta como divida"). A doer
   gaming the list does not argue itself out of three easy wins.
3. **Every calibration carries its technical justification** in a comment block directly above the
   key, naming the rule, the hit count, the folder, why the premise fails, and that the rule stays
   enforced elsewhere. D-...-6(2)'s documentation requirement is met in full.

**On the `MA0004` double-listing — honest engineering, not dead config.** The situation is real:
the `.editorconfig` key (Client layer) is subsumed today by the repo-wide `NoWarn` entry, because
DoD 6 mandates a *single* `<NoWarn>` element and MSBuild has no folder scoping for it. So the key
changes nothing about today's build. I scrutinised this specifically and it clears the bar:

- The measurement genuinely splits the ID in two populations with **opposite** verdicts — 107 unique
  hits in the Client layer where fixing is a defect (calibration), 175 outside it (165 Core + 10
  test) where the rule is *right* and not fixing is debt (`NoWarn`). Collapsing that into one
  answer would have been the dishonest move; keeping both is what the measurement said.
- It is disclosed in **all four** places a future reader could land: the `.editorconfig` comment
  ("This key is what keeps the Client layer correct on the day that debt is paid and MA0004 leaves
  NoWarn"), the `Directory.Build.props` comment ("the Client layer is calibrated in .editorconfig
  instead, and that calibration outlives this entry"), the todo, and SUMMARY "Delta consciente 1".
  The cross-reference runs both directions, so neither file can be read into a wrong conclusion.
- It fails safe. If a future engineer deletes the `.editorconfig` key as "dead", nothing breaks
  today; the cost only lands the day MA0004 leaves `NoWarn`, which is exactly the moment the todo
  file tells them to look. If instead the key had been omitted, paying the Core debt would silently
  re-arm a rule that csharp.md §3 forbids in the UI.

The one thing it is not is *provable* by build output the way the other three are. That is a
property of D-3's single-element `NoWarn` design, not of the doer's choice.

## T-7 process changes — verified

**Gate 4 scoping is honest.** The diff of `.jdi/agents/jdi-reviewer-translatereader.md` shows the
old text (`Failure = **WARN only** for now ... Tighten to BLOCK-on-new-files once the
baseline-de-estilo phase ships ...`) replaced by exactly what D-...-5(1)(2) authorised and nothing
more: command becomes the `whitespace` subcommand; **BLOCK only on files touched by the phase under
review**; "Violation in a file outside that diff = WARN, never a blocker" is stated explicitly, with
D-2 cited. No repo-wide promotion, no new gate, no widened scope. Confirmed against D-...-5 line by
line.

**The 3 non-Gate-4 `WARN.only` occurrences were reworded with severity preserved.** Diff evidence:

| Location | Before | After | Severity |
|---|---|---|---|
| ~172 (Gate 1 secondary, Android build) | "WARN-only if the workload is missing rather than BLOCK" | "a missing workload is reported as WARN, never as BLOCK" | unchanged |
| ~496 (5.11 unpaired event subscription) | "(pre-existing imbalance — legacy, WARN only)" | "(pre-existing imbalance — legacy: WARN, never BLOCK)" | unchanged |
| ~520 (5.12 mutable static state) | "(legacy, WARN only — it is a one-shot native-init guard)" | "(legacy: WARN, never BLOCK — it is a one-shot native-init guard)" | unchanged |

None was deleted, none was promoted to BLOCK. The DoD 8 grep trap (`grep -ci 'WARN.only'` runs over
the *whole* file) was defused by rephrasing rather than by silently hardening rules no decision
authorised. This is the specific failure mode I was asked to hunt for and it did not occur — the
phase did not make the reviewer stricter than the human approved.

`registry/LEGACY.md`, `registry/LEGACY-reviewers.md` and `jdi-doer-translatereader.md:71` were
realigned per D-...-5(4), and `todos/LEGACY.md:367-378` carries the literal
`RESOLVIDO em baseline-de-estilo` with the T-6 commit named as evidence and an accurate account of
what the format actually changed. `.github/` untouched.

## `RISCO:` markers and follow-up routing

Six IDs are marked `RISCO:` in `Directory.Build.props`. D-...-6(4) required **at minimum** `CS8602`
and `CA1001` — both present. I assessed the four extras independently; none is padding:

| ID | Claim | Assessment |
|---|---|---|
| `CS8602` (7x, test) | Null deref kills the test with `NullReferenceException` instead of reporting an assertion | **Real.** Mandated by D-...-6(4) anyway |
| `CA1001` (3x, PageModels + Pages) | Type owns a `CancellationTokenSource` and is not `IDisposable` | **Real.** Direct csharp.md §2.4 violation; an undisposed PageModel is MAUI's #1 leak. Mandated by D-...-6(4) |
| `MA0009` (18x, `ParsingEngine` + test) | Regex with no timeout over EPUB HTML | **Real, and the most serious of the six.** csharp.md §4 classifies EPUB files as untrusted input, security is the project's priority 1, and ReDoS on a parsing hot path is a genuine availability bug — not style |
| `CA1305` + `MA0011` (10x + 15x, Core) | `SettingsAccess` round-trips `double` through `ToString()`/`TryParse` with no `IFormatProvider` | **Real and concrete, not theoretical.** On a pt-BR box `1.6` is written as `"1,6"`; an invariant seed then fails to round-trip and the setting falls back to its default silently. The user of this repo is on pt-BR |
| `CS0414` (1x, `ReaderPage.xaml.cs:21`) | `_needsInjection` written at 114 and 125, never read | **Real.** A WebView re-injection guard that stopped guarding is dead logic with a live intent, not an unused-field nit |

`.jdi/todos/2026-08-08-baseline-de-estilo.md` exists, is tracked, and routes them properly:
a "Warnings congelados no `NoWarn`" section split into **Priority 1 (`RISCO:`)** — all six, each
with file/line and the concrete failure mode — and **Priority 2 (quality debt)** covering the
remaining 15, including the note that `CA1859` and `MA0016` contradict each other and that whoever
takes the fix phase must pick one by decision. It also records that the `NoWarn` list may only ever
shrink. Routing is genuine, not a stub.

## Blockers

None.

## Warnings

**W-1 (fix before merge — one line per file).** Three process files still assert
`TreatWarningsAsErrors` is **OFF**, which iteration 2 made false. T-7 ran while T-5 was BLOCKED and
was never re-synced after `2e857a0` flipped the flag:
- `.jdi/agents/jdi-reviewer-translatereader.md`, Gate 4: *"`TreatWarningsAsErrors` is deliberately
  NOT enabled. `baseline-de-estilo` measured 24 distinct `CS`/`CA`/`MA` IDs ... against the 12-ID
  `NoWarn` cap of D-2026-08-08-baseline-de-estilo-3 and stopped at that cap instead of growing the
  list. Until a human revisits `AnalysisLevel=latest-recommended` ..."* — every clause is now wrong:
  the flag is on, the cap was revoked by D-...-6, and the list exists with 21 IDs.
- `.jdi/agents/jdi-doer-translatereader.md:71`: *"`TreatWarningsAsErrors` is OFF ... so it awaits a
  human call on `latest-recommended`"*.
- `.jdi/registry/LEGACY.md`: *"`TreatWarningsAsErrors` continua desligado: o inventario medido deu
  24 IDs `CS/CA/MA` contra o teto de 12"*.

This is **not** a DoD 8 failure: DoD 8's `Verify:` asserts the absence of `WARN.only`,
`Tighten to BLOCK`, `quando a phase` and `custom analyzers exist yet`, and the presence of `BLOCK`
and the `RESOLVIDO` marker — all of which genuinely hold. But DoD 8's stated intent is *"divida de
processo fechada"*, and D-...-5(4) exists precisely because these files "hoje afirmam o contrario".
They affirm the contrary again. No operational harm today (a new warning is now caught earlier, by
Gate 1, as a build error), so this is a documentation defect rather than a regression — but it is
the phase's own deliverable and should not reach `main` stating the opposite of what the phase
shipped.

**W-2 (live CI exposure, cannot be verified on this machine).** `TreatWarningsAsErrors=true` lives
in the root `Directory.Build.props`, so it applies to **every** TFM, including the `build-android`
job in `.github/workflows/ci.yml:60-82` (`ubuntu-latest`, `dotnet build ... -f net10.0-android -c
Release`). No DoD covers that target, and D-...-5(3) forbids touching `.github/` to relax it. The
measured 24-ID inventory came from the Windows TFM + Tests only. The Android TFM compiles a
different file set (`Platforms/Android/MainActivity.cs`, `MainApplication.cs` — both reviewed, tiny
and clean) plus an Android-specific toolchain, so IDs the Windows build never lights up (`CA1416`
platform-compatibility is the obvious candidate, plus `XA*` from the Android SDK) would now be hard
**errors**, not warnings, and are absent from `NoWarn`. This machine has no Android SDK
(`ANDROID_HOME`/`ANDROID_SDK_ROOT` unset, `%LOCALAPPDATA%\Android\Sdk` absent) and the app csproj
conditionally omits `net10.0-android` without one, so it is unverifiable locally — exactly the
fallback PLAN T-5 step 5 prescribes. **Severity for the PR: medium-high.** It is loud and cheap to
detect (the PR's own CI run answers it), but if it fires, the fix needs its own decision, since
adding IDs to `NoWarn` requires one per D-...-3(2)/D-...-6(3). Recommend opening the PR and letting
`build-android` answer the question before merge. Scanned the other workflows: only `sonarqube.yml`
also builds (Core + Tests, `net10.0`, same file set as the local measurement) — low risk.

**W-3 (structural consequence, worth writing down somewhere).** The `NoWarn` list is repo-wide and
MSBuild offers no new-vs-legacy scoping, so D-...-3(5) — *"Codigo NOVO nao usa `NoWarn`"* — is now
enforced **only by human review**, not by the compiler. Concretely: a new regex without a timeout in
`ParsingEngine` (`MA0009`, untrusted EPUB input) or a new undisposed `CancellationTokenSource`
(`CA1001`) will compile silently. The `Directory.Build.props` comment states the intent ("a NEW
warning in NEW code is a review blocker, not a candidate for this list"), but the place that rule
has to be *operationalised* is Gate 4 of the reviewer — which is the stale text in W-1. Fixing W-1
should fold this in.

**W-4 (judgment, non-blocking).** `CA1711` is the thinnest of the four calibrations. Its premise
argument (public-API guideline, no consumers in a test assembly) is sound and the scope is 1 hit in
`test/`, so it stands — but had it gone to `NoWarn` instead, nothing would have been lost. Noted so
that a future reader does not treat it as precedent for calibrating on convenience.

**W-5 (nit).** The hit counts in `.editorconfig` comments are **raw** MSBuild counts (`CA1707` 688,
`MA0004` 214/330 — MSBuild reports each warning twice, inline + summary), while
`Directory.Build.props` comments use **unique** `file:line:col` counts (`MA0004` 175). SUMMARY
documents the methodology, but the two config files disagree with each other on the same rule and a
reader consulting only them will be confused. One convention, stated once, would fix it.

**W-6 (nit).** `.jdi/todos/2026-08-08-baseline-de-estilo.md` was created *inside* the T-5 commit
`2e857a0`, and its first ~28 lines are discuss-session out-of-scope items that predate T-5 and
belong to the `/jdi-discuss` step. SUMMARY discloses that T-5 wrote to the todo, but not that the
file did not previously exist in git. Harmless, and the file itself is good; flagged only because
"1 task = 1 subject" is a standing project directive.

**W-7 (carried, unchanged by this phase).** The repo keeps 6 files with a UTF-8 BOM and no declared
charset policy, because `charset = utf-8` was dropped to protect DoD 5. Correctly reasoned and
correctly disclosed by the doer; it just remains open. It needs its own phase — removing a BOM is a
byte change this phase was not authorised to make.

## Gates (reviewer 1-8)

| Gate | Status | Details |
|---|---|---|
| 1 Build | **PASS** | `src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` → `0 Error(s)`, `0 Warning(s)`. Android secondary: not runnable here (no SDK) → WARN, never BLOCK (see W-2) |
| 2 Tests | **PASS** | 375 total / 373 passed / 0 failed / 2 skipped — above the 167-test D-2 baseline, identical to iteration 1, no regression. JS: 79/79 |
| 3 Coverage | **SKIPPED** | Zero new `.cs` files after `4285f25` in this phase (the 7 touched files are whitespace-only). SKIPPED by D-2, not a failure |
| 4 Lint/Format | **PASS** | `dotnet format whitespace --verify-no-changes` → exit 0 repo-wide. Note: this is the first review run under the gate's own new BLOCK-on-phase-files rule, and it is clean |
| 5 Security/Layer/Concurrency/Memory | **PASS** | DoD 5 proves the phase is semantics-zero across `*.cs`/`*.xaml`/`*.js`, so no layer, concurrency, memory or injection surface changed. The security-adjacent finding is a *visibility* one, not a new defect: `MA0009` (ReDoS on untrusted EPUB HTML) is now suppressed rather than fixed — authorised by D-...-6(4), marked `RISCO:`, and routed to the todo. See W-3 |
| 6 Consistency | **PASS** | 13 commits, Conventional Commits with scope `baseline-de-estilo`, types correctly varied, atomic, in the D-...-1(3) locked order. All 7 PLAN tasks `completed`; all `files_modified` appear in the log; PLAN's T-5 amendment records the D-...-6 revocation without rewriting the original text |
| 7 UI Validation | **SKIPPED** | `has_frontend=false` — native MAUI client, by design, never a failure |
| 8 DoD | **PASS** | 8/8 auto PASS, 0 manual pending |

## Recommendation

Ship it, with W-1 fixed first. Iteration 2 did the hard thing correctly: it took D-...-6's objective
test literally, applied it per ID, refused three calibrations it could easily have taken, and left
the losing arguments written down. The result is a build that goes from 886 warnings to a hard
`0 Error(s) / 0 Warning(s)` on both targets without a single line of legacy code changed, and the
process rules it rewrote were not quietly hardened beyond what the human approved.

Before merging:
1. Fix **W-1** — three stale sentences claiming `TreatWarningsAsErrors` is off, and fold **W-3**
   into the Gate 4 rewrite while there.
2. Open the PR and let `build-android` answer **W-2**. If it fails, that is a new decision, not a
   patch.
3. Carry the CONTEXT.md "Deferred to PR review" items into the PR body: LF phantom-diff check in
   Visual Studio/Rider on Windows, one Windows smoke run post-format, and the human judgment call on
   a 21-ID `NoWarn` (the answer the measurement gives is: `latest-recommended` + Meziantou on a
   codebase that never had analyzers has a long tail, and 21 frozen IDs is the honest price — but
   only 6 of them are risk, and all 21 are now tracked).
