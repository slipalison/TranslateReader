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
