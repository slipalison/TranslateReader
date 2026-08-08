# Phase 22: Pixel-perfect da chrome vs mockups — Review (slug: pixel-perfect, iter 5, IconOverride + animation guard + screenshot-verified cover fix)

**Verdict:** APPROVED_WITH_WARNINGS

Diff real `3335f59..HEAD` (commits `b033cfc` + `26d1a60`) lido na íntegra e conferido contra o
SUMMARY § "Fix-round (iter 5)" e § "Real live-UI verification". As duas alegações técnicas
centrais deste round — `BackButtonBehavior.IconOverride` existe e é puramente cosmética, e
`CancelAnimations()` é a API real do MAUI — foram verificadas de forma independente contra a
documentação oficial E contra o assembly `10.0.60` real deste repo (o mesmo pinado no `.csproj`,
mais preciso que o `10.0.51` checado no iter 4). Ambas são **VERDADEIRAS**. O fix de overlay de
capa foi verificado linha a linha contra o padrão pré-existente que ele espelha. O único item não
fechado neste round (o clique sintético no TOC não abriu o painel) foi documentado como
inconclusivo, não como corrigido nem como quebrado — julgamento correto, não conta como blocker.
Verdict não é APPROVED puro porque sobra resíduo genuinamente estrutural: W-1 agora restrito ao
interior do painel TOC, e W-5/Qwen-Phi presos à fronteira Core que esta phase não pode tocar
(D-...-1 / DoD 11).

## Iter-5 changes — verified

- **Escopo do diff — CONFIRMADO EXATO.** `git diff 3335f59..HEAD -- src/TranslateReader` toca
  exatamente 3 arquivos: `LibraryPage.xaml` (+15/-4), `ReaderPage.xaml` (+6/-1),
  `ReaderPage.xaml.cs` (+2). Por commit: `b033cfc` = `ReaderPage.xaml` +
  `ReaderPage.xaml.cs` (IconOverride + CancelAnimations); `26d1a60` = `LibraryPage.xaml` (overlay
  gate). Nenhum arquivo fora dessa lista, nenhuma lógica não declarada. Fora de `src/`, só
  artefatos de processo (`LOOP.md`/`REVIEW.md`/`SUMMARY.md`) mudaram — nada em
  `src/TranslateReader.Core/` ou `Resources/Raw/` (confirmado por `git diff --name-only
  3335f59..HEAD -- src/TranslateReader.Core/ src/TranslateReader/Resources/Raw/` = vazio).
- **`BackButtonBehavior.IconOverride` — VERIFICADA INDEPENDENTEMENTE, é REAL e é puramente
  cosmética.** Duas provas:
  1. **Documentação oficial** (`learn.microsoft.com/dotnet/api/microsoft.maui.controls.
     backbuttonbehavior.iconoverride`, moniker net-maui-10.0 coberto): "Gets or sets the icon to
     use instead of the default back button icon. This is a bindable property." Tipo
     `Microsoft.Maui.Controls.ImageSource` — `FontImageSource` (usado no fix) é subtipo de
     `ImageSource`, compatível. A doc de classe lista `IconOverride` como propriedade
     INDEPENDENTE de `Command`, `CommandParameter`, `IsEnabled`, `IsVisible`, `TextOverride` — sem
     nenhuma relação declarada entre elas.
  2. **Assembly real do repo, versão exata pinada:** `.csproj` fixa
     `PackageReference Include="Microsoft.Maui.Controls" Version="10.0.60"` — o cache NuGet local
     tem essa versão exata (`~/.nuget/packages/microsoft.maui.controls.core/10.0.60/lib/net10.0/
     Microsoft.Maui.Controls.xml`), e nela `P:Microsoft.Maui.Controls.BackButtonBehavior.
     IconOverride` existe com a mesma descrição, ao lado (mas distinta) de `CommandProperty`/
     `CommandParameterProperty`/`IsEnabledProperty`.
  - **Verificação do fix em si:** o diff em `ReaderPage.xaml` só adiciona o elemento filho
    `<BackButtonBehavior.IconOverride><FontImageSource .../></BackButtonBehavior.IconOverride>`
    dentro do `<BackButtonBehavior IsEnabled="True">` já existente — `IsEnabled="True"` não muda,
    nenhum `Command`/`CommandParameter` é adicionado. Ou seja, a navegação nativa (voltar/gestos)
    permanece 100% intocada; só o ícone visual muda. Sintaxe (`FontFamily="Phosphor"
    Glyph="&#xE058;" Size="18" Color="{StaticResource ColorText}"`) é idêntica ao padrão já usado
    em outros `FontImageSource` no mesmo arquivo (linhas 138, 174). Codepoint `E058` confere com
    `design/PIXEL-SPEC.md:75` (`arrow-left | E058 | Regular | voltar (reader)`). A alegação do
    iter 4/iter 5 de que aplicar o ícone exigiria trocar o back button nativo por um customizado —
    aceita nos iters 1-4 como trade-off de arquitetura — estava **errada**; `IconOverride` resolve
    sem trade-off nenhum.
- **`CancelAnimations()` — VERIFICADA, é REAL e está no ponto certo.** `Microsoft.Maui.Controls.
  ViewExtensions.CancelAnimations(VisualElement)` confere no mesmo assembly `10.0.60`: "Aborts all
  animations (e.g. LayoutTo, TranslateTo, ScaleTo, etc.) on the view element." `FadeToAsync`
  também é método real do MAUI (`ViewExtensions.FadeToAsync`, mesma classe) — não um wrapper
  customizado do projeto — então `CancelAnimations` mira o mesmo motor de animação que
  `FadeToAsync` usa, cancelamento é efetivo. Chamada em `ReaderPage.xaml.cs:197`, antes do reset
  `Opacity = 0` (linha 198) e do `await FadeToAsync(...)` (linha 199) — ordem correta para o guard
  funcionar (cancelar antes de reiniciar o fade).
- **Overlay de título/autor na capa real — VERIFICADO, espelha exatamente a condição
  pré-existente.** As duas novas `IsVisible="{Binding CoverImagePath, Converter=
  {StaticResource StringIsNullOrEmptyConverter}}"` (no `VerticalStackLayout` de título/autor e no
  `Label` "EPUB") são byte-a-byte idênticas à condição já usada no `Grid` do gradiente-placeholder
  pré-existente (`LibraryPage.xaml:577`) — mesma property, mesmo converter, mesmo StaticResource.
  Não é uma condição "parecida", é a MESMA regra reaplicada em 2 elementos novos — capas reais
  (com `CoverImagePath` setado) escondem os 3; capas placeholder mostram os 3. A legenda
  título/autor abaixo da capa (`Grid.Row="1"`/`"2"`, linhas 670-685) **não tem** `IsVisible`
  algum — permanece incondicional, então livros com capa real continuam com identificação
  textual legível, só perdem o overlay ilegível de cima da imagem.

## TOC-open item — judgment

Documentar o item como "não resolvido, nem corrigido nem confirmado quebrado" é a conduta CORRETA
aqui, e não deve contar contra o verdict como blocker. Justificativa:

1. **Consistente com a convenção do projeto.** É o mesmo padrão de honestidade já usado em
   D-...-9 (baseline de 367 vs 365 admitido como erro do planner, não escondido) e nas seções
   BLOCKED anteriores desta mesma phase (Qwen/Phi "sem dado fabricado" em vez de inventar
   filename/size). Reivindicar sucesso sem prova, ou simplesmente omitir a tentativa, seria pior
   nos dois casos — o primeiro é confiança não-conquistada, o segundo é informação escondida.
2. **Não há caminho de código que reduza essa incerteza especificamente.** Busquei por qualquer
   teste automatizado cobrindo `OnTocButtonClicked`/`IsTocVisible`/`ShowChaptersPanelAsync`
   (`grep -RnE "OnTocButtonClicked|IsTocVisible|ShowChaptersPanelAsync|TocButton"` no repo
   inteiro): o único hit em `test/` é `DesignSystemTests.cs:84`, que só afirma que o texto
   `x:Name="TocButton"` existe no XAML compilado — uma asserção estrutural estática, não um teste
   de comportamento. Não existe, em lugar nenhum da suite de 375 testes, um teste que instancie
   `ReaderPage` e dispare o evento `Clicked` para provar o toggle. Isso é consistente com o padrão
   já estabelecido no projeto: código-behind de Página que depende de `Handler`/runtime MAUI real
   não é unit-testado em lugar nenhum desta base (só Managers/Engines/Access via NSubstitute, e
   verificações estruturais de XAML via `DesignSystemTests`/`PixelSpecTests`) — não é uma lacuna
   nova introduzida aqui.
3. **Mesmo um teste novo não fecharia esta dúvida específica.** Ler o código
   (`OnTocButtonClicked` → `IsTocVisible = !IsTocVisible` → `PropertyChanged` → dispatch →
   `SyncChaptersPanelAsync` → `ShowChaptersPanelAsync`/`HideChaptersPanelAsync`) confirma lógica
   simples e corretamente encadeada, sem bug óbvio — e isso é exatamente o que um hipotético
   teste unitário chamando `OnTocButtonClicked(null, EventArgs.Empty)` diretamente provaria de
   novo. A incerteza real não é sobre a lógica C#; é sobre se o clique sintético via Win32
   (`mouse_event`/`SendInput`) chega ao botão dentro da região de title bar customizada do WinUI
   (`Shell.TitleView` roda dentro da faixa de título nativa, com regras de hit-test próprias do
   SO) — uma questão de infraestrutura de input, não de lógica de aplicação. Nenhum teste
   dentro deste ambiente fecha essa lacuna; só um clique real de mouse/touch ou execução em
   device fecharia.

Conclusão: mantenho como warning (residual, não blocker) — mas AGORA com escopo mais estreito que
o W-1 antigo (que cobria a phase inteira sem verificação viva nenhuma). Ver seção Warnings.

## DoD 1-11 re-verification (post iter-5)

Todos os 11 comandos `Verify:` literais do CONTEXT.md re-executados por este reviewer neste
working tree (HEAD = `26d1a60`):

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
| 9 — Compila | PASS | exit 0 — `0 Error(s)`; warnings pré-existentes CS0618 (7 sítios) + CS0414 (1 sítio) = 8 sítios distintos, mesmos de sempre (linhas confirmadas idênticas ao commit `3335f59`, nenhum novo); a contagem bruta no log varia por sítio-vezes-passe do MSBuild, não é regressão |
| 10 — Suite verde, piso 375 | PASS | exit 0 — Total 375, Passed 373, Skipped 2, Failed 0; os 10 nomes `[Fact]` prescritos confirmados presentes em `PixelSpecTests.cs` |
| 11 — Core/Raw intocados | PASS | exit 0 — diff vazio contra BASELINE (`.jdi/phases/pixel-perfect/BASELINE`), também vazio no range `3335f59..HEAD` isolado |

Build e suite executados por este reviewer neste working tree — números NÃO copiados do SUMMARY;
coincidem com o que o SUMMARY declara (build 0 erros, suite 375/373/2/0).

## Blockers

Nenhum. Diff é exatamente o alegado, as duas alegações de API (`IconOverride`, `CancelAnimations`)
são verdadeiras e verificadas contra a versão exata do assembly pinada no `.csproj`, o fix de
overlay de capa espelha corretamente a condição pré-existente sem quebrar legibilidade em nenhum
dos dois casos (capa real ou placeholder), nenhum DoD regrediu, e o item TOC-aberto foi tratado
com o nível de honestidade que este projeto exige — não é um defeito, é uma lacuna de verificação
declarada.

## Warnings

**Saem da lista neste round (resolvidos pelo iter 5):**
- **Arrow-left `E058` não aplicado** (herdado do iter 1, mantido nos iters 2-4 como trade-off de
  arquitetura aceito) — RESOLVIDO de verdade via `IconOverride`, sem trade-off nenhum. A premissa
  anterior (precisaria trocar o back button nativo) estava errada.
- **W-4 (fade sobreposto em toggle rápido)** — RESOLVIDO via `CancelAnimations()` antes do
  `FadeToAsync`, guard padrão do MAUI para exatamente essa classe de problema.

**W-1 SOBREVIVE, mas com escopo reescrito (não é mais o warning genérico de "sem UI viva").**
Este round fechou a maior parte do gap real: screenshots reais (Windows, app construído e
lançado, GDI+ scoped à janela) confirmaram visualmente sidebar (tint ativo, card de modelo),
barra superior (busca legível, toggle grid/list, hero card), grid layout, e o header/footer/
ícone-de-voltar do Reader — E encontraram + corrigiram um bug real que leitura de código não
achava (overlay ilegível em capa real). O que sobra, especificamente, é: o painel TOC nunca abriu
visivelmente sob clique sintético nesta sessão, então a borda-hairline (fix do T-10/iter 2) e a
cor de destaque do título ativo via `Setter.TargetName` (fix do iter 4) permanecem sem
confirmação de renderização real dentro do painel aberto — só revisão estática de código (sem bug
aparente) e a doc/assembly que provam a API existe. Nenhum teste automatizado cobre esse
code-behind (confirmado por grep nesta review), e um teste novo não fecharia esta lacuna
específica (ver seção "TOC-open item" acima) — só um clique real de mouse/touch ou execução em
device.

**Permanecem, genuinamente estruturais — fora do alcance de mais código Client-layer sem
reverter uma decisão locked:**
- **W-5 (lint, legado):** 3 WHITESPACE em `ThemeEngine.cs`/`ThemeEngineTests.cs` —
  `src/TranslateReader.Core/`, fora da fronteira Client-only desta phase (D-...-1), cuja anchor é
  justamente DoD 11 (diff vazio em Core). Corrigir aqui significaria violar o próprio DoD que este
  round acabou de reconfirmar PASS. Já coberto por uma phase futura dedicada
  (`baseline-de-estilo`), que virará BLOCK-on-new quando `.editorconfig` for adicionado.
- **Qwen/Phi placeholder (dados de modelo ausentes):** o gap é no `ModelRegistry` de
  `TranslationManager.cs` (Core) — mesma fronteira D-...-1, mesmo motivo. As rows da UI mostram só
  o nome, sem dado fabricado (correto). Fica para uma phase de Core futura.

Ambos os itens acima não são "preguiça" nem resíduo esquecido — são a fronteira que a própria
phase se propôs a respeitar (D-...-1, verificado a cada iteração por DoD 11). Endereçá-los aqui
seria uma regressão de escopo, não uma correção.

## Notes for next iteration

Verdict não é BLOCKED, então isto é opcional, não uma exigência de novo round. Se o objetivo
literal é "zero warnings" e não apenas "zero blockers": **dentro desta phase, com este ambiente,
não há mais nenhuma ação de código que feche os 2 itens estruturais (W-5, Qwen/Phi) sem violar
D-...-1/DoD 11** — eles só fecham com uma phase de Core dedicada, fora deste escopo. O único item
que ainda tem uma ação concreta e específica disponível é o resíduo estreito do W-1: um teste
manual de clique real (mouse ou touch, não script) no botão TOC do Reader, em Desktop, confirmando
visualmente (a) a borda hairline de 1px no painel e (b) o título do capítulo ativo em
`ColorAccent`. Essa é a única lacuna neste ponto que uma ação nova (não uma decisão de escopo)
poderia fechar — e ela exige interação humana real, não mais código.
