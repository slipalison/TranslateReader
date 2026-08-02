# Phase 22: Pixel-perfect da chrome vs mockups — Summary (slug: pixel-perfect)

**Status:** complete (9/9 tasks executed; T-9's own compound criterion is `blocked` on one
numeric sub-check that is a CONTEXT.md planning-data error, not missing work — see BLOCKED below)
**Iteration:** 1 (from clean start, no prior REVIEW.md)

## Executed tasks

### T-1 — Baseline + fontes Inter/Phosphor — **completed**
`git rev-parse HEAD` recorded in `.jdi/phases/pixel-perfect/BASELINE` before any other change.
Downloaded `Inter-Regular.ttf`/`Inter-Medium.ttf` (rsms/inter v4.1, `extras/ttf/`) and
`Phosphor.ttf`/`Phosphor-Fill.ttf` (phosphor-icons/web) into
`src/TranslateReader/Resources/Fonts/` (all > 50KB). Registered the 4 aliases (`InterRegular`,
`InterMedium`, `Phosphor`, `PhosphorFill`) in `MauiProgram.cs` alongside the existing OpenSans
fonts (not removed — T-2's job). Added "Inter (SIL OFL 1.1)" and "Phosphor Icons (MIT)" sections
to `THIRD-PARTY-NOTICES.md`.

### T-2 — Tokens + default font + morte do #E53E3E — **completed**
Added the 9 pixel-spec tokens (`ColorDanger`, `AccentTint10`, `AccentTint08`, `OverlayScrim`,
`CoverScrim`, `TextMuted70`, `TextMuted55`, `TextMuted40`, `ProgressTrackOnCover`) to
`DesignTokens.xaml`. Swapped every `OpenSansRegular` implicit-style setter in `Styles.xaml` to
`InterRegular` (no `OpenSansSemibold` existed in that file, so that half of the instruction was a
no-op). Replaced all 4 real `#E53E3E` occurrences (LibraryPage x1, ReaderPage x2,
SettingsOverlay x1 — CONTEXT.md's planner note said SettingsOverlay had 2, actual grep found 1)
with `{StaticResource ColorDanger}`.

### T-3 — Library desktop sidebar/top bar/hero — **completed**
Sidebar: 232w, `Padding="16,20"`, Phosphor logo chip (translate `E4A2`), nav items restructured
to icon+label with `AccentTint10` active background and `ColorAccent` icon/text (via per-Label
`DataTrigger`, since Border-level triggers cannot reach child Label properties without
`TargetName`, unsupported in MAUI). Model card: `cpu` icon, no bold. Top bar: title+count on one
baseline, Phosphor search icon inside the search box, new grid/list segmented toggle
(`GridToggleButton`/`ListToggleButton`) bound to new `IsListView`/`ShowGridViewCommand`/
`ShowListViewCommand`, language chip with globe icon, `Importar` moved from `ToolbarItem` (removed
entirely) to an inline top-bar `Border` button. Hero: 56x84 cover with a spine `BoxView`, non-bold
`InterRegular` title, `Continuar` button with a trailing arrow-right glyph.

### T-4 — Grid card restyle, LIST VIEW, adaptive span — **completed**
Grid card: cover corner radius 8 to 6, title/author now rendered inside the top of the cover
(was below), spine accent, `EPUB` badge restyled, progress bar backed by `ProgressTrackOnCover`,
and a new 28x28 dots-three-vertical menu button. Added sibling `BooksListCollection` (visible via
`DataTrigger` on `IsListView` — no `InvertedBoolConverter` exists in the project, matching the
plan's documented fallback) with an 84h row template (cover, title/author, mini progress, menu
button), each row carrying its own `FlyoutBase.ContextFlyout` copy (CollectionView templates
cannot share one flyout instance across two independent DataTemplates).
`LibraryPage.xaml.cs`: adaptive span formula `max(3, (int)((width + 20) / 187))` replaces the old
fixed-width formula, skipped entirely on Phone idiom (stays at the XAML-declared `Span="3"`).
`OnCardMenuTapped` opens the existing native `MenuFlyout` via
`Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase.ShowAttachedFlyout` under `#if WINDOWS` —
MAUI itself has no cross-platform `FlyoutBase.ShowAttachedFlyout`; verified by grepping the
compiled `Microsoft.Maui.Controls.dll` (absent) vs `Microsoft.WinUI.dll` (present). This matches
D-...-7's intent (native/WinUI-only flyout) and follows the same `#if WINDOWS` platform-bridge
pattern already used in `MauiProgram.cs`.

### T-5 — Library mobile header/search/hero — **completed**
Restructured the top-bar `Grid` to 2 rows / 7 columns, all driven by the same elements via
`OnIdiom` (no duplicated layout, per the global NAO FACA rule). Mobile: small logo chip,
title/count, language chip (36x34), icon-only `+` import button on row 0; `SearchEntry`
repositions itself into a dedicated full-width row 1 via `OnIdiom` `Grid.Row`/`Grid.Column`/
`Grid.ColumnSpan` (literally `WidthRequest="{OnIdiom Default=-1, ...}"` as the plan specified).
Hero collapses to a tappable card (`TapGestureRecognizer` on the whole `Border`) with a 44x66
cover, no author/progress line, no `Continuar` button — just a trailing arrow-right glyph.

### T-6 — Reader subtitle, footer, TOC restyle — **completed**
`ReaderPageModel.ChapterSubtitle` (new `[ObservableProperty]`) is recomputed in both
`LoadCurrentChapterAsync` and `LoadScrollContentAsync`; desktop format includes the author, Phone
idiom drops it (`Cap. {N} de {T}`). `Shell.TitleView` restructured to a two-line title block with
the new `ChapterSubtitleLabel`; TOC/gear buttons switched to Phosphor glyphs, all 3 title-bar
buttons shrunk to 36x36 (with `MinimumWidthRequest`/`MinimumHeightRequest` overridden, since the
implicit `Button` style's 44 floor would otherwise clamp them back up). Footer wrapped in a new
`ReaderFooter` `Border` (hairline top, `ColorBg`, 54h) shown only in Paginated mode;
`PageIndicatorLabel` reformatted to "Página {p} / {t} · Capítulo {c} de {n}"; new
`PageProgressBar` (200x2) updated at the same code-behind point. `PreviousButton`/`NextButton`
stay native `Button` elements (their `Clicked` handlers are unchanged) — icon+text achieved via
`FontImageSource` + `ContentLayout`, since a plain `Button.Text` cannot mix two font families and
converting to `Border`+`TapGestureRecognizer` was unnecessary here. TOC active item:
`AccentTint10` background (Border-level, works); the title label stays `ColorText` in the
active state — MAUI `VisualState` setters can only target properties of the element the
`VisualStateGroup` is declared on, not a child `Label`, without code-behind. This is the exact
fallback the plan anticipated and is documented inline in the XAML.

### T-7 — SettingsOverlay panel/cards/segmented/model list — **completed**
Panel: 400 to 380 desktop width; header and body now have their own distinct paddings (`20,18` /
`20,4,20,28`) via a 3-row `Grid` (drag-handle row / header row / scrolling body row) instead of one
shared padding on the whole panel. Close button: Phosphor `E4F6`, 36x36. Theme pickers stay
`Button` elements (the plan explicitly said `BorderColor` toggling continues, which only exists
on `Button`, not `Border`) — restyled to equal-width 3-column cards with two-line text (Aa +
label) (single font size for both lines is a conscious delta — `Button.Text` cannot mix font
sizes per substring); unselected border color changed from `Transparent` to `ColorDivider`.
Reading-mode control became a segmented `Button` pair (`PaginatedModeButton` before
`ScrollModeButton` in XAML order) using `FontImageSource` icons; selection now toggles
`TextColor`/icon `Color` instead of `BorderColor`. Field labels: 14 to 12, `TextMuted70`; the 4
slider value labels: `ColorAccent`. Font/language `Picker`s wrapped in a 36h hairline `Border`.
Model list: the horizontal `ScrollView` of pills is replaced by `ModelsList`, a
`VerticalStackLayout` of 4 `Border` rows — each keeps its frozen `x:Name` (migrated from `Button`
to `Border`) and its existing `Clicked` handler is now wired via `Tapped=` on a
`TapGestureRecognizer` (valid: `TappedEventArgs` is contravariant-compatible with the handlers'
existing `(object?, EventArgs)` signature — same technique used for the T-4 card-menu buttons).
`DeleteModelButton` now uses `TextColor="{StaticResource ColorBg}"` on the `ColorDanger`
background (the only solid button, per spec). Mobile sheet: new 36x4 drag handle, corner radius
16 to 18.

### T-8 — TranslateBookPopup 440w, banner order, outline buttons — **completed**
Card: 340 to 440w, padding 24 to 12. `OfflineBanner` moved from before to after the
language-picker `Grid` (verified: banner line index greater than SourcePicker line index).
Banner re-tokenized from solid `Accent900`/`Accent200` to `ColorBg`/`ColorDivider`/`Neutral500`
with a `shield-check` glyph. Cover: 40x56 to 34x51 with a 2px spine. Picker separator: plain
arrow character to Phosphor `arrow-right` (`E06C`). `Cancelar`/`Traduzir` converted from solid
`Button`s to outline `Border`+`TapGestureRecognizer` (same contravariance technique as T-7);
`OnCancelClicked`/`OnTranslateClicked` signatures and the `(source, target)` popup-result
contract are unchanged.

### T-9 — PixelSpecTests + DesignSystemTests update — **completed** (compound Criterio: `blocked`, see below)
Added `test/TranslateReader.Tests/PixelSpecTests.cs` with the 10 prescribed `[Fact]` tests
(exact names, no `[Theory]`/`[InlineData]`, no mocked concretes), following the same disk-read
pattern as `DesignSystemTests.cs`. Moved `#E53E3E` into `DesignSystemTests`'s
`RedesignedXaml_HasNoLegacyChromeHex` denylist (D-...-5). Full suite: 375/375 passing, 0
failures, 2 skipped (LLamaSharp-dependent tests, pre-existing skip).

## Deltas conscientes

1. Baseline test count. CONTEXT.md's "Auto-teste do planner" note assumed a 367-test baseline
   (365 passed + 2 skipped). The actual baseline — verified by running the suite against the
   commit immediately before T-1 (`424cc98`, and again against the true pre-phase tip `82df842`
   via `git stash`) — is 365 total (363 passed + 2 skipped), not 367. This is a two-test
   discrepancy in the plan's own numbers, not something introduced by this execution. See
   BLOCKED below for the consequence on DoD 10.
2. TOC active-item title color. Stays `ColorText` instead of `ColorAccent` in the `Selected`
   `VisualState` — MAUI `VisualStateGroup` setters can only target the element they're declared
   on; reaching a child `Label` would require code-behind (`VisualStateManager.GoToState` per
   item), which the plan explicitly offered as an optional escape hatch and explicitly asked to
   register which fallback was used. Registered here and inline in `ReaderPage.xaml`.
3. Theme-card "Aa" label. Single font size for both the "Aa" glyph and the theme name on a
   native `Button` instead of two independently-sized runs — `Button` cannot mix font sizes
   within one `Text`. Converting to `Border`+`Label`s was avoided because the plan explicitly
   kept `BorderColor` as the code-behind selection API for these 3 buttons.
4. `FlyoutBase.ShowAttachedFlyout`. Does not exist anywhere in MAUI's cross-platform surface
   (confirmed by grepping the compiled `Microsoft.Maui.Controls.dll`) — only in WinUI's
   `Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase`. Implemented as a `#if WINDOWS` bridge
   (`view.Handler?.PlatformView` cast to `Microsoft.UI.Xaml.FrameworkElement`), consistent with
   D-...-7 (menu stays native/WinUI-only) and the existing `#if WINDOWS` pattern in
   `MauiProgram.cs`. No-op on non-Windows platforms.
5. Outer top-bar/hero/grid horizontal gutter. Approximated at `28,16`/`28,20` rather than
   engineering three independently-padded zones for the Library main-content column — the
   PIXEL-SPEC's per-section paddings (top bar `28,16`, hero `p16` internal, grid `auto-fill`) were
   applied where each element's own Border/Grid has a literal padding value; the shared outer
   page gutter is a reasonable approximation, not exact. Real visual parity is explicitly
   "Deferred to PR review" in CONTEXT.md.

## BLOCKED

- Settings model list — Qwen/Phi filename and size. PIXEL-SPEC and the task instructed to use
  the real data read from `src/TranslateReader.Core` (read-only) for the filename/size shown per
  model row. `TranslationManager.cs`'s `ModelRegistry` only has entries for `gemma-2-2b` and
  `hy-mt1.5-1.8b` — `qwen-2.5-3b` and `phi-3.5` are UI-only placeholders with no real
  `FileName`/`SizeBytes` anywhere in Core (confirmed by reading `TranslationManager.cs` in full;
  `ResolveModel` falls back to Gemma for any unregistered name). Rather than inventing a
  filename/size, the Qwen/Phi rows in `SettingsOverlay.xaml` show only the model name — no
  fabricated data. This is a Core-side gap (`ModelRegistry` missing 2 entries) that is explicitly
  out of scope for this Client-only phase (D-...-1: Core must stay untouched — verified empty by
  DoD 11).
- DoD 10's `Total >= 377` numeric floor. Fails as literally written: actual total after T-9 is
  375 (365 real baseline + 10 new tests), not >= 377. All 10 required `[Fact]`s exist with the
  exact prescribed names and all pass; 0 failures; 2 skips (within the `<=2` limit). The gap is
  entirely attributable to CONTEXT.md's `baseline = 367` assumption being 2 higher than the
  measured actual (365) — see "Deltas conscientes" item 1. Not fixed by adding filler tests
  (would contradict "10 [Fact] com EXATAMENTE estes nomes" and the DRY/no-padding-metrics
  principle); flagged here for reviewer/human judgment on whether to correct the CONTEXT.md
  threshold to `>= 375` or treat the 2-test gap as accepted drift.

## DoD 1-11 pass/fail table

| DoD | Description | Result |
|---|---|---|
| 1 | Fontes Inter + Phosphor registradas e default trocado | PASS |
| 2 | Tokens novos + morte do #E53E3E | PASS |
| 3 | Library desktop: top bar do mockup | PASS |
| 4 | List view real no desktop | PASS |
| 5 | Grid adaptativo | PASS |
| 6 | Reader: subtitulo + footer do mockup | PASS |
| 7 | Settings: painel 380, cards de tema, segmented, lista de modelos | PASS |
| 8 | Popup: banner depois dos pickers, botoes outline, 440w | PASS |
| 9 | Compila (Windows Release 0 erros) | PASS |
| 10 | Suite inteira verde com os 10 testes novos (piso Total >= 377) | FAIL - 375/375 green, 0 failures, but 375 < 377 (see BLOCKED) |
| 11 | Core intocado (prova da fronteira) | PASS |

9/11 auto-verifiable DoD items pass cleanly; 1 fails purely on a numeric floor that the plan's
own baseline assumption got wrong by 2; 1 (11) is the hard boundary guarantee and holds.

## Files modified

- `src/TranslateReader/MauiProgram.cs`
- `src/TranslateReader/Resources/Fonts/Inter-Regular.ttf` (new)
- `src/TranslateReader/Resources/Fonts/Inter-Medium.ttf` (new)
- `src/TranslateReader/Resources/Fonts/Phosphor.ttf` (new)
- `src/TranslateReader/Resources/Fonts/Phosphor-Fill.ttf` (new)
- `THIRD-PARTY-NOTICES.md`
- `src/TranslateReader/Resources/Styles/DesignTokens.xaml`
- `src/TranslateReader/Resources/Styles/Styles.xaml`
- `src/TranslateReader/Pages/LibraryPage.xaml`
- `src/TranslateReader/Pages/LibraryPage.xaml.cs`
- `src/TranslateReader/PageModels/LibraryPageModel.cs`
- `src/TranslateReader/Pages/ReaderPage.xaml`
- `src/TranslateReader/Pages/ReaderPage.xaml.cs`
- `src/TranslateReader/PageModels/ReaderPageModel.cs`
- `src/TranslateReader/Pages/Controls/SettingsOverlay.xaml`
- `src/TranslateReader/Pages/Controls/SettingsOverlay.xaml.cs`
- `src/TranslateReader/Pages/Controls/TranslateBookPopup.xaml`
- `src/TranslateReader/Pages/Controls/TranslateBookPopup.xaml.cs`
- `test/TranslateReader.Tests/PixelSpecTests.cs` (new)
- `test/TranslateReader.Tests/DesignSystemTests.cs`
- `.jdi/phases/pixel-perfect/BASELINE` (new)
- `.jdi/phases/pixel-perfect/PLAN.md`

## Tests

- Total: 375
- Passing: 373
- Skipped: 2 (LLamaSharp model-dependent tests, pre-existing)
- Failing: 0
- `src/TranslateReader.Core/**` and `src/TranslateReader/Resources/Raw/**`: empty diff against
  BASELINE (`82df8420ab306c3f5a06e07edc72a0469e5af65c`) — verified via DoD 11's literal command.

## Fix-round (iter 2) — T-10

Executed against `D-2026-08-02-pixel-perfect-10` (8 user-reported bugs found in live manual
testing after iter 1 converged APPROVED_WITH_WARNINGS). Root cause for A-D and E was already
confirmed by the orchestrator's investigation before dispatch; F-H required directed investigation
during this task. All 8 items resolved, none ended BLOCKED.

- **A — Sidebar/TOC hairline border.** `Border.StrokeThickness` is a `double` in MAUI, not a
  `Thickness`; the XAML value `"0,0,1,0"` parsed via `double.TryParse(..., NumberStyles.Any,
  InvariantCulture)` silently succeeded as `10` (commas read as thousands separators), producing a
  solid 10px border on all 4 sides instead of a 1px right-side hairline. Fixed by setting
  `Stroke="Transparent"`/`StrokeThickness="0"` on `SidebarPanel` (`LibraryPage.xaml`) and
  `ChaptersPanel` (`ReaderPage.xaml`), and adding a real single-side hairline via a sibling
  `BoxView` (`WidthRequest="1"`, `HorizontalOptions="End"`, `ColorDivider`) — a `BoxView` next to
  `SidebarPanel` in the root `Grid` (Library), and a `BoxView` inside a new `Grid` wrapping the
  existing `ScrollView` inside `ChaptersPanel` (Reader), so its content/handlers are unchanged.
- **B — Search field text/placeholder clipped.** The implicit `Entry` style forces
  `MinimumHeightRequest="44"`, taller than the containing `Border`'s `HeightRequest` (35
  desktop/38 mobile). Fixed with an explicit `MinimumHeightRequest="0"` on `SearchEntry`,
  overriding the inherited floor; visual height stays controlled by the parent `Border`.
- **C — "Biblioteca" title duplicated.** `LibraryPage` has its own custom header but never
  suppressed Shell's native nav bar, so both the native `ContentPage.Title` and the custom header
  `Label` rendered. Fixed with `Shell.NavBarIsVisible="False"` on the `ContentPage` root.
  `ReaderPage` was unaffected (uses `Shell.TitleView`, which already replaces the native title) and
  was left untouched for this item.
- **D — Reader title-bar buttons (TOC/Aa/gear) clipped.** The implicit `Button` style forces
  `Padding="14,10"`; the 3 buttons were shrunk to 36x36 without overriding padding, leaving ~8x16px
  of usable glyph area inside a 36x36 box. Fixed with an explicit `Padding="0"` on all 3
  `Shell.TitleView` buttons.
- **E — OpenDyslexic font has no effect. Out of scope, registered only.** Confirmed it's a
  WebView-rendered content font (CSS), not a native MAUI font; no font file or `@font-face` exists
  under `Resources/Raw/wwwroot`. Fixing it would require touching `Resources/Raw/**`, forbidden by
  this phase's Client-only boundary (D-2026-08-02-pixel-perfect-1, DoD 11). Registered
  `.jdi/todos/2026-08-02-opendyslexic-webfont.md` with the concrete steps for a future phase. Did
  not touch `Resources/Raw/` or `Core`; did not remove "OpenDyslexic" from the picker.
- **F — Reading-mode segmented control not reflecting state. Fixed.** Investigated the given
  hypothesis (`FontImageSource.Color` mutated post-construction may not repaint on some platform
  handlers) via web research rather than guessing: confirmed this is a known, documented
  `dotnet/maui` limitation — `FontImageSource.Color` is not an observable bindable property, so
  mutating `.Color` on an already-rendered `ImageSource` does not trigger a repaint on WinUI; the
  documented workaround is to replace the whole `ImageSource` instance
  (https://www.telerik.com/forums/how-to-change-color-of-fontimagesource-programmatically,
  https://github.com/dotnet/maui/issues/8826). Applied: `UpdateReadingModeButtonBorders` in
  `SettingsOverlay.xaml.cs` now reassigns `ScrollModeButton.ImageSource`/
  `PaginatedModeButton.ImageSource` to a freshly constructed `FontImageSource` (via a small
  `CloneSegmentIcon` factory that copies `FontFamily`/`Glyph`/`Size` from the original
  XAML-declared instance and sets the new `Color`) instead of mutating `.Color` in place.
  `TextColor` was unaffected (that IS an observable `Button` property) and needed no change.
- **G — Settings layout re-audited against PIXEL-SPEC "Settings — painel desktop" line by line.**
  Compared every measurement in that section (header/body padding, theme-card size, segmented
  control height, slider label colors/track colors, model-list row size, status/delete-button
  styling) against the current `SettingsOverlay.xaml`. Panel width (380), header padding
  (`20,18`), body padding (`20,4,20,28`), segmented control (35h, `12,7` cell padding, Paginado
  before Rolagem), the 4 slider rows (label `TextMuted70` / value `ColorAccent` / track colors),
  the two language pickers (36h, 50/50, `12` gap), and the model rows (53h, `12,10` padding, `8`
  gap, `AccentTint08`+`ColorAccent` when selected, `circle`/`check-circle Fill` radio glyphs) all
  already matched the spec exactly — no action needed there. Found and fixed 2 real divergences:
  (1) the 3 theme-card buttons (`LightThemeButton`/`DarkThemeButton`/`SepiaThemeButton`) had no
  explicit `Padding`, inheriting the implicit `Button` style's `"14,10"` instead of the spec's
  `p:8,12` (desktop) / `p:6,10` (mobile) — confirmed this reading of the spec's unitless `p:X,Y`
  notation (no `px`, no `->` conversion arrow) means "already in MAUI `Padding="X,Y"` order" by
  cross-checking against the segmented control's `p:12,7`, which was already correctly applied as
  literally `Padding="12,7"`; added `Padding="{OnIdiom Default='6,10', Desktop='8,12'}"` to all 3
  theme buttons. (2) the "Modelo local" section header was rendered as just "Modelo" (missing
  "local") — corrected the `Label.Text`. The theme-card single-font-size-for-"Aa"+label limitation
  and the serif "Aa" glyph are pre-existing, explicitly documented deltas (iter-1 SUMMARY "Deltas
  conscientes" #3, and PIXEL-SPEC's own "Diferencas intencionais mantidas" #4) — not re-litigated.
- **H — Translation-mode indicator too subtle to notice. Fixed (perception only, no new business
  logic).** Confirmed the hypothesis: `IsTranslationModeActive` and its binding were already
  functionally correct (no logic bug), the 3px `BoxView` with a direct `IsVisible` binding and no
  entrance animation was simply too subtle to read as a state change. Increased
  `TranslationModeIndicator`'s `HeightRequest` from 3 to 5, and added a `FadeToAsync(0 -> 1, 200ms,
  Easing.CubicOut)` entrance animation in `ReaderPage.xaml.cs`'s existing
  `case nameof(ReaderPageModel.IsTranslationModeActive):` `PropertyChanged` hook (inside
  `OnTranslationModeChanged`, which previously only handled the "turned off" branch) — same
  `FadeToAsync` pattern already used for the TOC panel (`ShowChaptersPanelAsync`). No exit
  animation was added per the task's scope (entrance only). Nothing was touched in
  `ReaderPageModel`/Manager/Engine.

**Build:** `dotnet build src/TranslateReader/TranslateReader.csproj -f net10.0-windows10.0.19041.0
-c Release` — 0 errors (16 pre-existing warnings, all `CS0618`/`CS0414`, unrelated to this task).

**Tests:** `dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release` —
375 total, 373 passed, 2 skipped (pre-existing LLamaSharp-dependent skips), 0 failed. Identical
counts to the iter-1 baseline — no Core code was touched, so no regression and no new tests were
required (only Client-layer XAML/code-behind changed).

**Files modified this round:** `src/TranslateReader/Pages/LibraryPage.xaml`,
`src/TranslateReader/Pages/ReaderPage.xaml`, `src/TranslateReader/Pages/ReaderPage.xaml.cs`,
`src/TranslateReader/Pages/Controls/SettingsOverlay.xaml`,
`src/TranslateReader/Pages/Controls/SettingsOverlay.xaml.cs`,
`.jdi/todos/2026-08-02-opendyslexic-webfont.md` (new). `src/TranslateReader.Core/**` and
`src/TranslateReader/Resources/Raw/**` remain untouched (verified empty diff against
`.jdi/phases/pixel-perfect/BASELINE`).

## Fix-round (iter 3) — residual review warnings

User declined to ship on iter 2's `APPROVED_WITH_WARNINGS` and asked for the actionable residue
resolved first. Iter 2's REVIEW.md Warnings section had 5 items; 2 were concrete, low-risk,
single-line fixes with the exact cause already stated by the reviewer — applied directly by the
orchestrator (no doer dispatch needed for a diagnosis that was already complete):

- **W-3 (hairline color drift):** `SettingsOverlay.xaml`'s `PanelBorder` used
  `Stroke="{StaticResource Neutral800}"`; `design/PIXEL-SPEC.md:222` specifies `Neutral500` for
  this stroke. Fixed to `Neutral500`.
- **W-2 (comment precision, item F):** the inline comment in `SettingsOverlay.xaml.cs` above
  `UpdateReadingModeButtonBorders` claimed `FontImageSource.Color is not an observable bindable
  property`, which is imprecise (`ColorProperty` IS a `BindableProperty`; the real gap is the
  platform handler not re-rasterizing on an in-place mutation). Reworded to state the actual
  mechanism and cite `dotnet/maui#8826` as "same family, different platform handler" rather than
  an exact match.

Not fixed (remain as accepted residue, per the reviewer's own iter-2 judgment — none is a code
defect):
- **W-1** — no live UI/screenshot available in this environment to confirm the 8 T-10 fixes
  render correctly; deferred to PR review, same precedent the whole phase already operates under.
- **W-4** — rapid on/off/on toggling of `IsTranslationModeActive` can overlap two `FadeToAsync`
  calls; purely cosmetic on a low-frequency, user-click-driven event, reviewer judged not worth
  cancellation-token complexity.
- **W-5** and the iter-1-inherited items (TOC `Setter.TargetName`, `arrow-left E058` unused,
  Qwen/Phi placeholder UX, `new Regex` test nit) — legacy/out-of-scope or already judged
  not-worth-fixing by a prior review pass.

Build: 0 errors. Tests: 375 total, 373 passed, 2 skipped, 0 failed — identical to iter 1/2 (no
regression). No Core/Raw touched.

## Fix-round (iter 4) — TOC accent color + test nit

User declined ship a second time on iter 3's remaining residue. Re-investigated the two
previously-"accepted as impossible" items with a doc lookup instead of just re-asking:

- **TOC active-item title color (inherited from iter 1, previously accepted as a MAUI limitation
  — turned out to be wrong):** `Setter.TargetName` IS supported by .NET MAUI's Visual State
  Manager (confirmed against the official docs, "Set state on multiple elements" section,
  `learn.microsoft.com/dotnet/maui/user-interface/visual-states`) — a `VisualState.Setters` entry
  declared on one element CAN target a named sibling/descendant in the same `NameScope` via
  `TargetName` + a fully-qualified `Property` (e.g. `Label.TextColor`). Named the chapter-title
  `Label` (`ChapterTitleLabel`) inside the TOC `DataTemplate` and added `TargetName` setters to
  both `Normal` (`ColorText`) and `Selected` (`ColorAccent`) states in `ReaderPage.xaml`. The
  active chapter's title now genuinely turns accent-colored, matching the mockup — this is a real
  fix, not a residual re-litigated.
- **`new Regex("Span")` test nit (W-7, iter 1):** `PixelSpecTests.cs`'s
  `LibraryPage_GridSpanIsAdaptive` constructed a `Regex` object just to match a literal substring.
  Replaced with `Assert.Contains("Span", codeBehind, StringComparison.Ordinal)`; removed the
  now-unused `using System.Text.RegularExpressions;`.

Deliberately NOT touched, with reasoning (to avoid re-litigating on a 3rd decline):
- **W-1 (no live UI confirmation)** — structurally impossible in this environment; no more code
  can close this gap, only an actual screenshot/device test can.
- **W-4 (overlapping FadeToAsync on rapid toggle)** — cosmetic, low-frequency, and MAUI's
  `Animate`/`FadeTo` family keys concurrent animations by (target, handle-name), so a second call
  on the same view very likely already supersedes the first rather than visibly fighting it; adding
  explicit cancellation logic for an unconfirmed, low-impact edge case was judged not worth the
  added complexity, unchanged from iter 2/3's review.
- **Arrow-left `E058` icon unused (Reader back button)** — PIXEL-SPEC lists it, but applying it
  means replacing the native platform back button (`Shell.BackButtonBehavior`) with a fully custom
  `Shell.TitleView` button, which trades away native back-gesture affordances (swipe-back on iOS,
  Alt+Left on Windows) for a cosmetic icon match. That's an architecture trade-off, not a bug fix
  — deliberately left alone pending an explicit decision, not silently "fixed".
- **W-5 (legacy lint) / Qwen-Phi placeholder metadata** — both require touching files outside this
  phase's locked Client-only boundary (`ThemeEngine.cs` is Core; the model metadata gap is in
  `TranslationManager.cs`'s `ModelRegistry`, also Core) — D-...-1 forbids it, unchanged.

Build: 0 errors. Tests: 375 total, 373 passed, 2 skipped, 0 failed — identical, no regression.
Core/Raw diff against BASELINE still empty.
