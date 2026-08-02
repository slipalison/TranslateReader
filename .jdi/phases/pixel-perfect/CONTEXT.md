# Phase 22: Pixel-perfect da chrome vs mockups — Context (slug: pixel-perfect)

Gerado em 2026-08-02, planejado por Fable 5 para execucao por LLM menor (D-...-8). Ground truth
produzido NESTA sessao de planejamento: `design/PIXEL-SPEC.md` (medidas via getComputedStyle de
ambos os mockups renderizados ao vivo), screenshot novo `design/screenshots/desktop-library-list.jpg`
(list view que a phase anterior acreditava nao existir), tabela de 24 icones Phosphor com
codepoints, e a descoberta de que o mockup define vermelho proprio (`#e08a8a`).

## Goal
Chrome do app bate pixel a pixel com os mockups: tipografia Inter, icones Phosphor, medidas/
cores/estados exatos do PIXEL-SPEC.md em Library (grid + list view), Reader (header/subtitulo/
footer/TOC) e Settings/TranslatePopup, desktop E mobile, sem tocar no Core.

## Locked decisions
- **D-...-1**: fonte de verdade = `design/PIXEL-SPEC.md`; escopo 100% Client Layer, Core com
  git diff VAZIO; sem exigencia nova de cobertura (nada de Core muda).
- **D-...-2**: Inter Regular(400)/Medium(500); `FontAttributes="Bold"` sai das superficies
  redesenhadas (mockup nao tem bold); "Aa" serifado vira InterRegular (delta consciente).
- **D-...-3**: icones = font Phosphor (Regular + Fill), codepoints tabelados no PIXEL-SPEC;
  chars improvisados (☰ ⚙ ✕ ☀ ☾ ☕ &#8594; emoji) saem das superficies redesenhadas.
- **D-...-4**: toggle grid/list ACEITO (reverte D-app-redesign-6 com evidencia nova); 2
  CollectionViews irmas + `IsListView`; SO desktop.
- **D-...-5**: token `ColorDanger` #E08A8A substitui TODOS os `#E53E3E`.
- **D-...-6**: grid desktop adaptativo — `span = max(3, (int)((W + 20) / 187))` em
  `SizeChanged`; mobile 3 fixo.
- **D-...-7**: MenuFlyout continua nativo; botao ⋮ visivel e obrigatorio
  (`FlyoutBase.ShowAttachedFlyout`).
- **D-...-8**: plano sequencial T-1..T-9, passos imperativos, Verify literal por task, "NAO
  FACA" por task, commit atomico por task, baseline de testes fixa (piso 377).

## Canonical refs
- `design/PIXEL-SPEC.md` — TODA medida vem daqui; `design/screenshots/*.jpg` (10 estados).
- `.jdi/decisions/D-2026-08-02-pixel-perfect-1..8.md`.
- Superficies: `src/TranslateReader/Pages/{LibraryPage,ReaderPage}.xaml(.cs)`,
  `Pages/Controls/{SettingsOverlay,TranslateBookPopup}.xaml(.cs)`,
  `PageModels/{LibraryPageModel,ReaderPageModel}.cs`, `Resources/Styles/{DesignTokens,Styles}.xaml`,
  `MauiProgram.cs`, `Resources/Fonts/`, `THIRD-PARTY-NOTICES.md`,
  `test/TranslateReader.Tests/{DesignSystemTests,PixelSpecTests}.cs`.
- Preservar da phase app-redesign: todos os `x:Name` prescritos la (DoD 3/4/5 daquela phase
  continuam passando), eventos publicos do SettingsOverlay, atribuicao Tencent,
  `ShowAsync/HideAsync` animados.

## Out of scope
- Estados de hover (mockup web; sem teste possivel e sem paridade nativa garantida).
- Visual interno do MenuFlyout aberto (D-...-7).
- Animacoes novas alem das existentes (`FadeToAsync`/`TranslateToAsync` ja shipped).
- Tema claro da chrome, i18n, warnings CS0618 de `DisplayAlert` (pre-existentes), ícones
  vetoriais por Path/SVG.
- Qualquer mudanca em `src/TranslateReader.Core/**`, `Resources/Raw/**`, `ThemeEngine`.

## Definition of Done

> `dod=auto_only`. Comandos em bash (Git Bash no Windows), executados da RAIZ do repo.
> `DOTNET_CLI_UI_LANGUAGE=en` porque o sumario local sai em pt-BR. Logs em `TestResults/`
> (gitignored). Piso de suite FIXO: baseline desta branch = 367 testes (365 passed + 2
> skipped) — D-...-8 proibe merge-base com origin/main aqui.

### Auto-verifiable
- [ ] **DoD 1 — Fontes Inter + Phosphor registradas e default trocado.** Os 4 TTF existem
      (>50KB cada), `MauiProgram` registra os 4 aliases, `Styles.xaml` nao referencia mais
      `OpenSansRegular`, e `THIRD-PARTY-NOTICES.md` cita Inter (OFL) e Phosphor (MIT)
      **Verify:** `F=src/TranslateReader/Resources/Fonts; for f in Inter-Regular.ttf Inter-Medium.ttf Phosphor.ttf Phosphor-Fill.ttf; do test -f "$F/$f" && test "$(wc -c < "$F/$f")" -gt 50000 || exit 1; done && for a in InterRegular InterMedium Phosphor PhosphorFill; do grep -q "\"$a\"" src/TranslateReader/MauiProgram.cs || exit 1; done && test "$(grep -c 'OpenSansRegular' src/TranslateReader/Resources/Styles/Styles.xaml)" -eq 0 && grep -qi "Inter" THIRD-PARTY-NOTICES.md && grep -qi "Phosphor" THIRD-PARTY-NOTICES.md`
      **Source:** D-...-2, D-...-3
- [ ] **DoD 2 — Tokens novos + morte do #E53E3E.** `DesignTokens.xaml` ganha os 10 tokens do
      PIXEL-SPEC (`ColorDanger`, `AccentTint10`, `AccentTint08`, `OverlayScrim`, `CoverScrim`,
      `TextMuted70`, `TextMuted55`, `TextMuted40`, `ProgressTrackOnCover` + brush opcional) e
      NENHUM `#E53E3E` sobra em `src/TranslateReader/`
      **Verify:** `T=src/TranslateReader/Resources/Styles/DesignTokens.xaml; for k in ColorDanger AccentTint10 AccentTint08 OverlayScrim CoverScrim TextMuted70 TextMuted55 TextMuted40 ProgressTrackOnCover; do grep -q "x:Key=\"$k\"" "$T" || exit 1; done && grep -qi '#E08A8A' "$T" && test "$(grep -ric '#E53E3E' src/TranslateReader/ | awk -F: '{s+=$NF} END{print s+0}')" -eq 0`
      **Source:** D-...-5, PIXEL-SPEC "Cores -> tokens"
- [ ] **DoD 3 — Library desktop: top bar do mockup.** Importar migrou pra top bar (zero
      `ToolbarItem`), toggle grid/list existe, busca com icone, e os `x:Name` novos prescritos
      existem (`ImportButton`, `GridToggleButton`, `ListToggleButton`)
      **Verify:** `X=src/TranslateReader/Pages/LibraryPage.xaml; test "$(grep -c '<ToolbarItem' "$X")" -eq 0 && for n in ImportButton GridToggleButton ListToggleButton SearchEntry SidebarPanel BookCountLabel TargetLanguageChip ModelStatusCard ContinueReadingHero RecentFilterButton; do grep -q "x:Name=\"$n\"" "$X" || exit 1; done && grep -q 'FontFamily="Phosphor"' "$X"`
      **Source:** D-...-3, PIXEL-SPEC "Library — top bar"
- [ ] **DoD 4 — List view real no desktop.** `BooksListCollection` existe ao lado de
      `BooksCollection` (preservada), `LibraryPageModel` tem `IsListView` +
      `ShowGridView`/`ShowListView`, e o botao ⋮ com `ShowAttachedFlyout` esta no code-behind
      **Verify:** `X=src/TranslateReader/Pages/LibraryPage.xaml; M=src/TranslateReader/PageModels/LibraryPageModel.cs; grep -q 'x:Name="BooksCollection"' "$X" && grep -q 'x:Name="BooksListCollection"' "$X" && grep -q 'IsListView' "$M" && grep -q 'ShowGridView' "$M" && grep -q 'ShowListView' "$M" && grep -q 'ShowAttachedFlyout' src/TranslateReader/Pages/LibraryPage.xaml.cs`
      **Source:** D-...-4, D-...-7, PIXEL-SPEC "LIST VIEW"
- [ ] **DoD 5 — Grid adaptativo.** `SizeChanged` recalcula o `Span` no code-behind da
      LibraryPage com a formula fechada (187 presente como constante)
      **Verify:** `C=src/TranslateReader/Pages/LibraryPage.xaml.cs; grep -q 'SizeChanged' "$C" && grep -q '187' "$C" && grep -qE 'Span' "$C"`
      **Source:** D-...-6
- [ ] **DoD 6 — Reader: subtitulo + footer do mockup.** `ChapterSubtitleLabel` existe e e
      alimentado por `ChapterSubtitle` no PageModel; footer tem `ReaderFooter` +
      `PageProgressBar`; TOC ativo usa `AccentTint10`
      **Verify:** `X=src/TranslateReader/Pages/ReaderPage.xaml; M=src/TranslateReader/PageModels/ReaderPageModel.cs; grep -q 'x:Name="ChapterSubtitleLabel"' "$X" && grep -q 'ChapterSubtitle' "$M" && grep -q 'x:Name="ReaderFooter"' "$X" && grep -q 'x:Name="PageProgressBar"' "$X" && grep -q 'AccentTint10' "$X"`
      **Source:** PIXEL-SPEC "Reader"
- [ ] **DoD 7 — Settings: painel 380, cards de tema, segmented, lista de modelos.** Largura
      380 no idiom Desktop, ordem Paginado->Rolagem no XAML, bloco de modelos e lista vertical
      (`ModelsList`) sem ScrollView horizontal, e TODOS os x:Name da phase anterior preservados
      **Verify:** `S=src/TranslateReader/Pages/Controls/SettingsOverlay.xaml; grep -q '380' "$S" && P=$(grep -n 'x:Name="PaginatedModeButton"' "$S" | cut -d: -f1) && R=$(grep -n 'x:Name="ScrollModeButton"' "$S" | cut -d: -f1) && test "$P" -lt "$R" && grep -q 'x:Name="ModelsList"' "$S" && test "$(grep -c 'Orientation="Horizontal"' "$S")" -eq 0 && for n in LightThemeButton DarkThemeButton SepiaThemeButton ScrollModeButton PaginatedModeButton FontPicker FontSizeSlider LineSpacingSlider LetterSpacingSlider WordSpacingSlider SourceLanguagePicker TargetLanguagePicker GemmaModelButton QwenModelButton PhiModelButton HyMtModelButton ModelStatusLabel DeleteModelButton; do grep -q "x:Name=\"$n\"" "$S" || exit 1; done && grep -qi "Powered by Tencent HY" "$S"`
      **Source:** PIXEL-SPEC "Settings", D-app-redesign-8
- [ ] **DoD 8 — Popup: banner DEPOIS dos pickers, botoes outline, 440w.** Ordem por numero de
      linha no XAML e largura nova
      **Verify:** `P=src/TranslateReader/Pages/Controls/TranslateBookPopup.xaml; B=$(grep -n 'x:Name="OfflineBanner"' "$P" | cut -d: -f1) && S=$(grep -n 'x:Name="SourcePicker"' "$P" | cut -d: -f1) && test "$B" -gt "$S" && grep -q '440' "$P" && grep -q 'x:Name="BookMetaLabel"' "$P"`
      **Source:** PIXEL-SPEC "modal Traduzir livro"
- [ ] **DoD 9 — Compila.** Windows Release 0 erros (Android segue coberto pelo job de CI da
      phase anterior, inalterado)
      **Verify:** `mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0 > TestResults/pp-build.log 2>&1 && grep -qE "^ *0 Error\(s\)" TestResults/pp-build.log`
      **Source:** D-app-redesign-10
- [ ] **DoD 10 — Suite inteira verde com os 10 testes novos.** `PixelSpecTests.cs` existe com
      os 10 `[Fact]` nomeados no PLAN, zero regressao, piso fixo `Total >= 377`,
      `Skipped <= 2`
      **Verify:** `D=test/TranslateReader.Tests/PixelSpecTests.cs; test -f "$D" && for n in DesignTokens_ExposeThePixelSpecExtensions Fonts_InterAndPhosphorAreRegistered Chrome_UsesNoLegacyDangerRed LibraryPage_HasTheListViewAndToggle LibraryPage_ImportButtonLivesInTheTopBar LibraryPage_GridSpanIsAdaptive ReaderPage_HasChapterSubtitleAndStyledFooter SettingsOverlay_ModelsAreAVerticalRadioList SettingsOverlay_UsesPhosphorGlyphsNotAsciiArt TranslateBookPopup_BannerFollowsThePickers; do grep -q "$n" "$D" || exit 1; done && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release > TestResults/pp-suite.log 2>&1 && grep -q "Passed!" TestResults/pp-suite.log && awk '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Skipped:")s=$(i+1);if($i=="Total:")t=$(i+1)}} END{exit (k&&f+0==0&&t+0>=377&&s+0<=2)?0:1}' TestResults/pp-suite.log`
      **Source:** D-...-8
- [ ] **DoD 11 — Core intocado (prova da fronteira).** Diff da phase inteira nao toca Core nem
      Raw. `PHASE_BASE` = commit registrado em `.jdi/phases/pixel-perfect/BASELINE` (criado no
      inicio da execucao, antes do primeiro commit da phase)
      **Verify:** `PHASE_BASE=$(cat .jdi/phases/pixel-perfect/BASELINE) && test -n "$PHASE_BASE" && test -z "$(git diff --name-only "$PHASE_BASE" -- src/TranslateReader.Core/ src/TranslateReader/Resources/Raw/)"`
      **Source:** D-...-1

### Manual
- _(none — `dod=auto_only`)_

## Deferred to PR review
- Paridade visual REAL contra `design/screenshots/*.jpg` (humano compara lado a lado).
- Sensacao das fontes/icones renderizados em DPI real (hinting do Inter no Windows).
- Smoke em device: Library grid<->list -> abrir livro -> TOC -> Settings -> modelos, em
  Windows E Android.
- Deltas conscientes: menu nativo (D-...-7), "Aa" sem serifa (D-...-2), hover states.

## Notes
- **Nomes prescritos novos** (o DoD depende deles, nao renomear): `ImportButton`,
  `GridToggleButton`, `ListToggleButton`, `BooksListCollection` (XAML Library);
  `IsListView`, `ShowGridView`/`ShowListView` (LibraryPageModel); `ChapterSubtitleLabel`,
  `ReaderFooter`, `PageProgressBar` (ReaderPage); `ChapterSubtitle` (ReaderPageModel);
  `ModelsList` (SettingsOverlay). Todos os x:Name/handlers/eventos PRE-existentes ficam.
- **BASELINE file**: T-1 comeca gravando `git rev-parse HEAD` em
  `.jdi/phases/pixel-perfect/BASELINE` (arquivo commitado) — e o ancora do DoD 11.
- **Auto-teste do planner**: nenhum dos nomes novos existe hoje no repo (grep confirmado nesta
  sessao); `#E53E3E` existe em 5 pontos hoje (LibraryPage 1, ReaderPage 2, SettingsOverlay 2 —
  DoD 2 nao passa vazio); `<ToolbarItem` existe hoje (DoD 3 nao passa vazio); suite atual =
  367 (DoD 10 exige +10).
