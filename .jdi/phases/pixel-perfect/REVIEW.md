# Phase 22: Pixel-perfect da chrome vs mockups — Review (slug: pixel-perfect, iter 4, TOC fix + test nit)

**Verdict:** APPROVED_WITH_WARNINGS

Review do fix-round iter 4 (commit `3335f59`), diff real `be351b0..HEAD` lido na íntegra e
conferido contra o SUMMARY § "Fix-round (iter 4)". O ponto central deste round — a alegação de
que `Setter.TargetName` existe no MAUI e reverte a "limitação" aceita no iter 1 — foi verificado
de forma independente contra a documentação oficial E contra o assembly 10.0.51 real deste repo,
e **a alegação é VERDADEIRA**. O verdict não é APPROVED puro apenas porque o resíduo
estrutural/deferido permanece (W-1 live-UI, W-4, W-5, arrow-left, Qwen/Phi) — nenhum é defeito
de código e nenhum é endereçável dentro das fronteiras desta phase.

## Iter-4 changes — verified

- **Escopo do diff — CONFIRMADO EXATO.** `git diff be351b0..HEAD -- src/TranslateReader
  test/TranslateReader.Tests` toca exatamente 2 arquivos: `ReaderPage.xaml` (comentário
  reescrito + `x:Name="ChapterTitleLabel"` + 2 setters `TargetName`) e `PixelSpecTests.cs`
  (1 assert trocado + 1 `using` removido). Por commit: `e334fbd` = só artefatos de processo
  (`LOOP.md`/`REVIEW.md`); `3335f59` = os 2 arquivos de source + apêndice do SUMMARY.md.
  Nenhuma mudança de lógica não declarada; working tree limpo fora dos artefatos do loop.
- **Alegação `Setter.TargetName` — VERIFICADA INDEPENDENTEMENTE, é REAL.** Duas provas:
  1. **Documentação oficial:** `learn.microsoft.com/dotnet/maui/user-interface/visual-states`,
     seção literal **"Set state on multiple elements"** (moniker range net-maui-8.0 até 11.0 —
     net-maui-10.0 coberto, sem caveat de versão): "The Setter type has a `TargetName` property,
     of type `string`, that represents the target object that the Setter for a visual state will
     manipulate". A doc exige `Property` totalmente qualificado — "to set the TextColor property
     on a Label, Property is specified as `Label.TextColor`" — e o exemplo da própria doc é
     byte-a-byte o shape usado no fix: `<Setter TargetName="label" Property="Label.TextColor"
     Value="Red" />`. Requisito de escopo: "set properties on other elements **within the same
     scope**". Único caveat documentado ("Property paths are unsupported" com TargetName) não se
     aplica — `Label.TextColor` é propriedade qualificada simples, não um path.
  2. **Assembly real do repo:** `microsoft.maui.controls.core/10.0.51/lib/net10.0/
     Microsoft.Maui.Controls.xml` contém `P:Microsoft.Maui.Controls.Setter.TargetName` ("Gets or
     sets the name of the element to which the setter applies") — a API existe na versão exata
     pinada aqui, e o build Release compilou o XAML com XamlC sem erro (prova de compile-time).

  O registro do iter 1 ("MAUI VisualState Setters só alcançam propriedades do próprio elemento,
  sem TargetName tipo WPF") estava **errado** — a doc suporta TargetName desde net-maui-8.0.
  Verificação do fix em si:
  - **(a) NameScope:** `ChapterTitleLabel` (`ReaderPage.xaml:226`) e o `Border` que declara
    `VisualStateManager.VisualStateGroups` (`:215-256`) estão dentro do MESMO `DataTemplate`
    (`:210`) — cada instanciação do template tem seu próprio NameScope, então o nome resolve por
    instância de row (exatamente o "same scope" da doc). O roteamento do estado `Selected` para o
    root do template já estava provado funcionando desde o iter 1 (o setter de `BackgroundColor`
    no mesmo `VisualState`); a única mecânica nova é a resolução do TargetName, documentada.
  - **(b) Sintaxe:** `Property="Label.TextColor"` — idêntico ao padrão documentado
    (fully-qualified obrigatório com TargetName). Confere.
  - **(c) Simetria:** ambos os estados têm o setter (`Normal` → `ColorText` em `:240-242`,
    `Selected` → `ColorAccent` em `:248-250`) — a cor reverte ao desselecionar. Redundância
    positiva: a doc diz que o VSM já desfaz os setters do estado anterior na troca, então o
    setter explícito no `Normal` torna a reversão duplamente garantida. Exatamente 2 ocorrências
    de `TargetName="ChapterTitleLabel"` no arquivo, 1 declaração do nome — sem duplicação.
  - O comentário inline novo (`:211-214`) agora afirma o mecanismo correto com a citação certa da
    doc — substituindo o comentário anterior que documentava a limitação inexistente.
- **Test nit — RESOLVIDO, inofensivo.** `LibraryPage_GridSpanIsAdaptive` trocou
  `Assert.Matches(new Regex("Span"), codeBehind)` por `Assert.Contains("Span", codeBehind,
  StringComparison.Ordinal)` — semanticamente idêntico para substring literal ("Span" não tem
  metacaracteres), remove a alocação de `Regex` sem propósito (alinha com csharp.md §2.1, ainda
  que em teste seja só higiene) e usa `Ordinal` explícito. O `using System.Text.RegularExpressions;`
  órfão saiu. Teste re-executado ISOLADO por este reviewer com filtro: 1/1 Passed.
- **Higiene:** `dotnet format whitespace --verify-no-changes` scoped em `PixelSpecTests.cs` =
  exit 0. Commit `3335f59` segue Conventional Commits com scope da phase e tipo correto
  (`fix(pixel-perfect): ...`). Nit de precedente: o commit agrupa os 2 fixes de resíduo num
  commit só — mesmo padrão do `be351b0` (iter 3), já aceito pelo review anterior para
  micro-rounds de resíduo; não re-litigado.

## DoD 1-11 re-verification (post iter-4)

Todos os 11 comandos `Verify:` literais do CONTEXT.md re-executados por este reviewer neste
working tree (HEAD = `3335f59`):

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
| 9 — Compila | PASS | exit 0 — `0 Error(s)`, 16 warnings pré-existentes (CS0618/CS0414), 9.3s — inclui o XAML novo com TargetName compilado por XamlC |
| 10 — Suite verde, piso 375 | PASS | exit 0 — Total 375, Passed 373, Skipped 2, Failed 0 (idêntico aos iters 1-3; `LibraryPage_GridSpanIsAdaptive` adicionalmente re-rodado isolado com a forma `Assert.Contains` nova: 1/1 Passed) |
| 11 — Core/Raw intocados | PASS | exit 0 — diff vazio contra BASELINE `82df842` |

Build e suite executados por este reviewer neste working tree — números NÃO copiados do SUMMARY;
coincidem com o que o SUMMARY declara.

## Blockers

Nenhum — verdict não é BLOCKED. O diff é exatamente o alegado, a alegação `Setter.TargetName` é
verdadeira e o fix segue o padrão documentado à risca, e nenhum gate ou DoD regrediu.

## Warnings

**Saem da lista neste round (resolvidos pelo iter 4):** o item herdado do iter 1 "TOC
`Setter.TargetName`" (o título ativo do TOC agora fica `ColorAccent` de verdade — era a própria
razão do warning) e o "W-7 `new Regex` em teste" (trocado por `Assert.Contains`). A lista
encolhe de 7 para 5.

Permanecem (deliberadamente não endereçados, com justificativa conferida — não re-litigados):

- **W-1 (limitação estrutural):** sem screenshot/UI viva neste ambiente para confirmar o
  RESULTADO visual — vale agora também para o repaint do título do TOC em runtime no WinUI: a
  verificação estática está completa (doc oficial + API presente no assembly + XamlC compila),
  mas a confirmação renderizada final permanece "Deferred to PR review" (CONTEXT.md), mesmo
  precedente sob o qual a phase inteira opera desde o iter 1. Nenhum código adicional fecha esse
  gap — só device/screenshot.
- **W-4 (nit, item H):** toggles rápidos de `IsTranslationModeActive` podem sobrepor
  `FadeToAsync`s. Cosmético, baixa frequência, dirigido por clique; o argumento novo do SUMMARY
  (a família `Animate`/`FadeTo` do MAUI keia animações concorrentes por target+handle, então a
  segunda chamada tende a superseder a primeira) é plausível e enfraquece ainda mais o warning,
  mas não foi verificado em runtime — segue registrado como resíduo aceito, não vale
  complexidade de cancelamento.
- **W-5 (lint, legado):** 3 WHITESPACE em `ThemeEngine.cs`/`ThemeEngineTests.cs` — arquivos
  Core/teste-de-Core fora da fronteira Client-only desta phase (D-...-1) e exentos por D-2.
  Vira BLOCK-on-new quando `baseline-de-estilo` shippar `.editorconfig`.
- **Arrow-left `E058` não aplicado (herdado do iter 1):** confirmado ainda ausente
  (`grep E058` = 0 hits; `Shell.BackButtonBehavior` nativo mantido em `ReaderPage.xaml:16-17`).
  A justificativa do SUMMARY confere: aplicar o glifo exigiria substituir o back button nativo
  por botão custom no `Shell.TitleView`, trocando affordances de plataforma (swipe-back iOS,
  Alt+Left Windows) por paridade cosmética de ícone — trade-off de arquitetura que merece
  decisão própria (D-XX), não um quick fix. Corretamente deixado de fora.
- **Qwen/Phi placeholder (herdado do iter 1):** o gap real está no `ModelRegistry` de
  `TranslationManager.cs` (Core) — fora da fronteira desta phase por D-...-1; as rows mostram
  só o nome, sem dados fabricados. Segue para uma phase de Core futura.

## Notes for next iteration

N/A — verdict não é BLOCKED. (O resíduo restante é 100% estrutural/deferido: 1 limitação de
ambiente (W-1), 1 nit cosmético aceito (W-4), 1 lint legado fora da fronteira (W-5) e 2 itens
que exigem decisão/phase própria (arrow-left, Qwen/Phi). Nenhum é endereçável por mais código
dentro desta phase — a lista não encolhe mais sem ship + PR review ou nova decisão.)
