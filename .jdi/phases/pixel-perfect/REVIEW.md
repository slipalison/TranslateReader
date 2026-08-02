# Phase 22: Pixel-perfect da chrome vs mockups — Review (slug: pixel-perfect, iter 3, residual warning cleanup)

**Verdict:** APPROVED_WITH_WARNINGS

Review do micro-round de resíduo (W-2/W-3 do iter 2), diff real `06cb30f..HEAD` lido na íntegra
e conferido contra o SUMMARY § "Fix-round (iter 3)" — nada aceito só pela prosa. O verdict não é
APPROVED puro apenas porque os warnings deliberadamente NÃO endereçados (W-1/W-4/W-5 + 4 herdados
do iter 1) permanecem — todos residuais aceitos em reviews anteriores, nenhum é defeito de código.

## Iter-3 changes — verified

- **Escopo do diff — CONFIRMADO EXATO.** `git diff 06cb30f..HEAD -- src/TranslateReader` toca
  exatamente 2 arquivos: `SettingsOverlay.xaml` (1 linha: `Stroke`) e `SettingsOverlay.xaml.cs`
  (bloco de comentário apenas — todas as linhas alteradas são `//`, zero mudança
  funcional/lógica). Por commit: `be351b0` = os 2 arquivos de source + o apêndice do SUMMARY.md;
  `483ed6b` = só artefatos de processo do loop (LOOP.md/REVIEW.md), anterior ao fix. Nenhuma
  mudança não declarada. `src/TranslateReader.Core/**` e `Resources/Raw/**` intocados (DoD 11
  re-verificado abaixo).
- **W-3 (stroke do painel) — RESOLVIDO, conferido contra a spec.** `PanelBorder`
  (`SettingsOverlay.xaml:17-22`) agora usa `Stroke="{StaticResource Neutral500}"`. Cross-check
  independente: `design/PIXEL-SPEC.md:222` diz literalmente "bg ColorSurface, sombra ShadowLg +
  stroke Neutral500" na seção "Settings — painel desktop" — `Neutral500` é exatamente o que a
  spec pede. O recurso existe (`DesignTokens.xaml:35`, `#9397AB`) — verificação necessária porque
  `StaticResource` só resolve em runtime no MAUI; um build verde não provaria a chave. As rows de
  modelo continuam `Neutral800`, corretamente: a spec ali diz só "borda hairline" sem cor
  (registrado no iter 2 como não-divergência literal).
- **W-2 (precisão do comentário) — RESOLVIDO, agora factualmente correto.** O comentário novo
  (`SettingsOverlay.xaml.cs:213-217`) abandona a alegação falsa de que "`FontImageSource.Color`
  is not an observable bindable property" (`ColorProperty` É um `BindableProperty` real) e afirma
  o mecanismo correto: o handler de plataforma só re-rasteriza quando o `ImageSource` inteiro é
  substituído, não em mutação in-place de propriedade do source já renderizado — exatamente o gap
  que este reviewer descreveu no W-2 do iter 2. O hedge "does not reliably repaint" é apropriado.
  A citação `dotnet/maui#8826` foi suavizada para "same FontImageSource-color-not-applied family
  on another platform handler" — honesto e consistente com o que o fetch do issue no iter 2
  revelou (report de herança de cor no Android, closed/not-planned, não uma confirmação exata do
  cenário WinUI). Como comentário WHY de constraint não óbvia, é aceitável por csharp.md §7 (evita
  regressão futura para a mutação in-place). Código funcional em volta byte-a-byte intacto
  (`UpdateReadingModeButtonBorders`/`CloneSegmentIcon` inalterados).
- **Higiene:** `dotnet format whitespace --verify-no-changes` scoped no arquivo tocado = exit 0
  (zero drift novo). Commit `be351b0` segue Conventional Commits com scope da phase e tipo
  correto (`fix(pixel-perfect): ...`).

## DoD 1-11 re-verification (post iter-3)

Todos os 11 comandos `Verify:` literais do CONTEXT.md re-executados neste working tree
(HEAD = `be351b0`):

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
| 9 — Compila | PASS | exit 0 — `0 Error(s)`, 16 warnings pré-existentes (CS0618/CS0414), 10.4s |
| 10 — Suite verde, piso 375 | PASS | exit 0 — Total 375, Passed 373, Skipped 2, Failed 0 (idêntico ao iter 1/2 — nenhuma regressão; baseline 167 de D-2 amplamente coberta) |
| 11 — Core/Raw intocados | PASS | exit 0 — diff vazio contra BASELINE `82df842` |

Build e suite executados por este reviewer neste working tree — números NÃO copiados do SUMMARY;
coincidem com o que o SUMMARY declara.

## Blockers

Nenhum — verdict não é BLOCKED. O diff é exatamente o alegado (1 atributo XAML + 1 bloco de
comentário), ambos os fixes conferem contra spec/fatos, e nenhum gate ou DoD regrediu.

## Warnings

W-2 e W-3 do iter 2 estão resolvidos e saem da lista. Permanecem (deliberadamente não endereçados
neste round, residual aceito — não re-litigados, nada de novo encontrado):

- **W-1 (limitação estrutural, herdada do iter 1):** nem doer, nem reviewer, nem esta sessão têm
  screenshot/UI viva para confirmar o RESULTADO visual dos 8 fixes (hairline de 1px de fato
  renderizando, placeholder legível, glifos centrados em 36x36, fade perceptível). O código está
  correto por leitura estática e os fixes atacam causas raiz confirmadas, mas a paridade visual
  final permanece "Deferred to PR review" (CONTEXT.md) — mesmo precedente aceito pelo REVIEW do
  iter 1 para a phase inteira. Não é blocker por convenção já estabelecida. (Vale agora também
  para o stroke `Neutral500` do W-3: valor confere com a spec por leitura, rendering final é do
  PR review.)
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

N/A — verdict não é BLOCKED. (Micro-round exatamente como declarado: 2 fixes de resíduo
verificados contra spec e fatos; zero mudança funcional; DoD 1-11 íntegros; o residual restante é
estrutural/visual e já estava aceito em reviews anteriores.)
