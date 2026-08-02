# Phase 22: Pixel-perfect da chrome vs mockups — Review (slug: pixel-perfect, iter 2, fix-round)

**Verdict:** APPROVED_WITH_WARNINGS

Review do fix-round T-10 (D-2026-08-02-pixel-perfect-10), diff real `7d00da8..06cb30f` lido na
íntegra e conferido contra cada claim do SUMMARY — nada aceito só pela prosa do doer. Escopo do
diff: 5 arquivos em `src/TranslateReader/Pages/**` + 1 todo novo em `.jdi/todos/` — exatamente o
que o SUMMARY declara, nada além.

## T-10 items A-H — verified

- **A — CONFIRMADO.** `StrokeThickness="0,0,1,0"` sumiu dos dois arquivos (grep count = 0). LibraryPage: `SidebarPanel` agora `Stroke="Transparent"` + `StrokeThickness="0"` (`LibraryPage.xaml:22-23`) e BoxView irmão no mesmo `Grid.Column="0"`, `WidthRequest="1"`, `HorizontalOptions="End"`, `ColorDivider`, `IsVisible` OnIdiom idêntico ao da sidebar (`LibraryPage.xaml:191-198`) — declarado DEPOIS do Border no mesmo cell, logo renderiza por cima (z-order de Grid), na borda direita da coluna de 232px: visível e no lugar certo. ReaderPage: `ChaptersPanel` mesmo fix de stroke (`ReaderPage.xaml:186-187`); o novo `Grid` envolve o `ScrollView` original intacto + BoxView overlay em `End` (`ReaderPage.xaml:189-258`); as animações show/hide (`ShowChaptersPanelAsync`/`HideChaptersPanelAsync`, `ReaderPage.xaml.cs:259-276`) miram o próprio Border `ChaptersPanel` e não foram tocadas — filhos (Grid/BoxView) animam junto, nada quebra. O doer foi além do PLAN (zerou também o StrokeThickness) — melhoria, não desvio.
- **B — CONFIRMADO e SUFICIENTE.** `MinimumHeightRequest="0"` está no `SearchEntry` especificamente (`LibraryPage.xaml:275-276`), não em outro Entry. A preocupação de FontSize residual não se aplica: `SearchEntry` já sobrescreve `FontSize="13"` explícito (`LibraryPage.xaml:282`) — fonte 13 num container de 35/38px é confortável; só o piso de 44 do estilo implícito era o problema, e foi removido.
- **C — CONFIRMADO.** `Shell.NavBarIsVisible="False"` no elemento raiz `ContentPage` de `LibraryPage.xaml:9` (attached property válida; DoD 9 compila 0 erros). `LibraryPage` é o ÚNICO `ShellContent` raiz do `AppShell.xaml` — nunca teve back button para perder. `ReaderPage.xaml` mantém `Shell.TitleView` + `Shell.BackButtonBehavior` (`ReaderPage.xaml:16-18`) intactos; foi tocado pelo diff só para os itens A/D/H, não para C.
- **D — CONFIRMADO.** `Padding="0"` nos 3 botões do `Shell.TitleView`: TocButton (`ReaderPage.xaml:29`), botão Aa/translate (`:56`), engrenagem (`:67`). Centralização: `Button` MAUI centra conteúdo por padrão; glifo 18/texto 15 numa caixa 36x36 com padding 0 centra com folga (~9px por lado) — risco de "cramped" é baixo; confirmação final só visual (warning geral W-1).
- **E — CONFIRMADO.** `.jdi/todos/2026-08-02-opendyslexic-webfont.md` existe, segue o formato dos todos existentes (header `## De \`{phase}\` ({data})` + bullet com tag em negrito, igual `2026-08-02-app-redesign.md`), com causa raiz e passos futuros concretos. Critério literal (E) exit 0; diff do commit T-10 toca só 5 arquivos de `Pages/**` + o todo; `git diff BASELINE -- Core/ Raw/` vazio (DoD 11 PASS).
- **F — CONFIRMADO como padrão seguro; teoria externa parcialmente conferida.** `UpdateReadingModeButtonBorders` agora reatribui `Button.ImageSource` com instância nova via `CloneSegmentIcon` (`SettingsOverlay.xaml.cs:217-227`) — factory `static`, lê dos templates x:Name'd nunca mutados (`SettingsOverlay.xaml:149,165`, ambos pré-existentes do T-7), cores de campos `static readonly` (`:26,:203`), sem closure, idempotente em chamadas repetidas. **Worst case é inofensivo-ou-melhor:** mesmo se a teoria WinUI estiver imprecisa, a instância nova com a cor certa renderiza certo — nunca pior que a mutação anterior. Ressalvas em W-2 (citação adjacente, comentário inline tecnicamente impreciso).
- **G — CONFIRMADO; auditoria independente refeita.** As 2 divergências corrigidas são reais e batem com o texto literal da spec: (1) `Padding="{OnIdiom Default='6,10', Desktop='8,12'}"` nos 3 theme buttons (`SettingsOverlay.xaml:89,102,115`) = spec `p:8,12` desktop (PIXEL-SPEC:228) / `p:6,10` mobile (PIXEL-SPEC:281); (2) "Modelo local" (`SettingsOverlay.xaml:293`) = PIXEL-SPEC:227. Reli a seção "Settings — painel desktop" inteira contra o XAML atual: 380w, header `20,18`, corpo `20,4,20,28` gap 22, section headers 13/InterMedium/cs1/Neutral600, cards 67/59h r10 hairline `ColorDivider` não-selecionado, segmented 35h células `12,7` Paginado→Rolagem, labels 12 `TextMuted70` / valores 12 `ColorAccent`, pickers 36h 50/50 gap 12, rows de modelo 53h `12,10` gap 12 radio 16, status `E602` 14 + 12 Neutral600, delete `ColorDanger`/`ColorBg`, Tencent 12 Neutral500, handle 36x4 r2 + corner 18 — tudo confere como o doer afirmou. Um ponto que a auditoria não citou: PIXEL-SPEC:222 pede stroke do painel `Neutral500`, XAML usa `Neutral800` (`SettingsOverlay.xaml:22`) — mesma família de drift de hairline já aceita como W-3 no iter 1 (ver W-3 abaixo); fora da lista de foco que o PLAN deu ao item G, então claim não é falso, é residual.
- **H — CONFIRMADO, animação real.** `TranslationModeIndicator` (x:Name novo) 3px→5px (`ReaderPage.xaml:82-86`); `OnTranslationModeChanged` (`ReaderPage.xaml.cs:189-201`) faz `Opacity = 0` + `await FadeToAsync(1, 200ms, CubicOut)` — não é flip de `IsVisible`; disparado do `case nameof(IsTranslationModeActive)` PRÉ-existente (`:98-99`), nenhum `+=` novo (pares OnAppearing `+=` / OnDisappearing `-=` intactos, 4/4, `:35-38`/`:59-62`); duração em `const` (`:14`); branch de desligar (CancelPageTranslation + ClearTranslationsAsync) preservado com guard invertido. Higiene csharp.md limpa (sem closure capturada, sem static mutável novo, `async void` é padrão pré-existente desse dispatch). Nit não-bloqueante em W-4.

## DoD 1-11 re-verification (post fix-round)

Todos os 11 comandos `Verify:` literais do CONTEXT.md re-executados neste working tree (HEAD = `06cb30f`):

| DoD | PASS/FAIL | Evidence |
|---|---|---|
| 1 — Fontes Inter + Phosphor | PASS | exit 0 (4 TTF >50KB, 4 aliases, 0 OpenSansRegular, notices OK) |
| 2 — Tokens + morte do #E53E3E | PASS | exit 0 (9 tokens, #E08A8A presente, 0 hits #E53E3E) |
| 3 — Library top bar | PASS | exit 0 (0 ToolbarItem, 10 x:Name, Phosphor presente) |
| 4 — List view real | PASS | exit 0 (BooksListCollection, IsListView, Show*, ShowAttachedFlyout) |
| 5 — Grid adaptativo | PASS | exit 0 (SizeChanged, 187, Span no code-behind) |
| 6 — Reader subtítulo + footer | PASS | exit 0 (ChapterSubtitleLabel, ReaderFooter, PageProgressBar, AccentTint10) |
| 7 — Settings painel/segmented/modelos | PASS | exit 0 (380, Paginado<Rolagem, ModelsList, 0 Horizontal, 18 x:Name, Tencent) |
| 8 — Popup banner/440w | PASS | exit 0 (OfflineBanner depois de SourcePicker, 440, BookMetaLabel) |
| 9 — Compila | PASS | exit 0 — `0 Error(s)`, 16 warnings pré-existentes (CS0618/CS0414) |
| 10 — Suite verde, piso 375 | PASS | exit 0 — Total 375, Passed 373, Skipped 2, Failed 0 (idêntico ao iter 1 — nenhuma regressão) |
| 11 — Core/Raw intocados | PASS | exit 0 — diff vazio contra BASELINE `82df842` |

Critérios literais do T-10 também re-executados: **(itens A-D)** exit 0; **(item E)** exit 0.
Gate 4 (lint, WARN-only) re-rodado: mesmos 3 WHITESPACE legados do iter 1 (`ThemeEngine.cs:12,14`,
`ThemeEngineTests.cs:12` — Core, comprovadamente não tocado pela phase); zero drift novo nos
arquivos tocados pelo T-10. Commits do round seguem Conventional Commits com scope da phase
(`fix(pixel-perfect): T-10 ...`, `chore(pixel-perfect): ...`).

## Blockers

Nenhum — verdict não é BLOCKED. Todos os claims A-H do doer conferem contra o diff real; nenhum
gate regrediu; Core/Raw intocados; nenhuma claim FALSA encontrada.

## Warnings

- **W-1 (limitação estrutural, herdada do iter 1):** nem doer, nem reviewer, nem esta sessão têm
  screenshot/UI viva para confirmar o RESULTADO visual dos 8 fixes (hairline de 1px de fato
  renderizando, placeholder legível, glifos centrados em 36x36, fade perceptível). O código está
  correto por leitura estática e os fixes atacam causas raiz confirmadas, mas a paridade visual
  final permanece "Deferred to PR review" (CONTEXT.md) — mesmo precedente aceito pelo REVIEW do
  iter 1 para a phase inteira. Não é blocker por convenção já estabelecida.
- **W-2 (item F, precisão da justificativa):** a citação `dotnet/maui#8826` é real e da família
  certa (FontImageSource.Color não aplicado em Button), mas é um report de HERANÇA de cor no
  Android (closed/not-planned), não uma confirmação exata de "mutação pós-render não repinta no
  WinUI" (verificado via fetch do issue — 1 lookup do budget). O comentário inline
  "`FontImageSource.Color` is not an observable bindable property" é tecnicamente impreciso
  (`ColorProperty` É um BindableProperty; o gap real é o handler de plataforma não re-rasterizar
  em mutação interna do source). O FIX em si independe da teoria: substituir a instância é o
  workaround canônico dessa família de bugs e é inofensivo-ou-melhor no pior caso. Sugestão: só
  ajustar a redação do comentário se o arquivo for tocado de novo; não vale um commit próprio.
- **W-3 (hairline drift remanescente, família do W-3 do iter 1):** `PanelBorder` do
  SettingsOverlay usa `Stroke="{StaticResource Neutral800}"` (`SettingsOverlay.xaml:22`) onde
  PIXEL-SPEC:222 pede `stroke Neutral500`; rows de modelo usam `Neutral800` onde a spec diz só
  "borda hairline" (sem cor — não é divergência literal). A auditoria do item G não cobriu essa
  linha (o PLAN focou o G em paddings/tamanhos). Julgamento final na comparação lado a lado do PR,
  como todo o resto do W-3 original.
- **W-4 (nit, item H):** toggles rápidos on→off→on de `IsTranslationModeActive` podem sobrepor
  `FadeToAsync`s (a animação anterior não é cancelada). Efeito puramente cosmético num evento de
  baixa frequência dirigido por clique de usuário — não vale complexidade de cancelamento.
- **W-5 (lint, legado — persistente do iter 1):** 3 WHITESPACE em `ThemeEngine.cs`/
  `ThemeEngineTests.cs`, arquivos fora do diff da phase (D-2 exime). Vira BLOCK-on-new quando
  `baseline-de-estilo` shippar `.editorconfig`.
- **Herdados do iter 1, ainda válidos e não re-litigados:** W-1 (TOC `Setter.TargetName`),
  W-4 (ícone `arrow-left E058` não aplicado), W-6 (UX dos modelos placeholder Qwen/Phi),
  W-7 (nit `new Regex` em teste).

## Notes for next iteration

N/A — verdict não é BLOCKED. (Fix-round honesto e completo: 8/8 itens com causa raiz real
atacada ou registro justificado; DoD 1-11 íntegros; residual é exclusivamente visual/cosmético,
listado acima.)
