# Phase 22: Pixel-perfect da chrome vs mockups — Review (slug: pixel-perfect, iter 1)

**Verdict:** APPROVED_WITH_WARNINGS

Revisao independente (reviewer `jdi-reviewer-translatereader`, mode=verify). Todos os 11 itens
do DoD foram re-executados com os comandos LITERAIS do CONTEXT.md (pos-correcao D-...-9, que e o
piso vigente: `Total >= 375`), da raiz do repo, em Git Bash — resultados abaixo sao medidos, nao
copiados do SUMMARY.md do doer. Gates complementares: build PASS (DoD 9), testes PASS (DoD 10,
375 >= piso 375 e >> baseline legada de 167 — zero regressao), coverage SKIPPED (nenhum `.cs` de
produto novo nesta phase; D-...-1 dispensa exigencia nova de cobertura), lint WARN (3 achados
WHITESPACE, todos em arquivos legados NAO tocados pela phase), Security/Layer PASS (nenhuma
violacao nova), Consistency PASS (commits `style|feat|test|docs|chore(pixel-perfect)` atomicos
por task, D-4), UI Validation SKIPPED (has_frontend=false), DoD PASS (11/11 auto, 0 manual —
`dod=auto_only`).

## DoD 1-11 results

| DoD | Description | Result | Evidence |
|---|---|---|---|
| 1 | Fontes Inter + Phosphor registradas e default trocado | PASS | comando literal, exit 0. 4 TTF > 50KB em `src/TranslateReader/Resources/Fonts/` (Bin 411-488KB no diff); 4 aliases em `MauiProgram.cs:33-36`; 0 `OpenSansRegular` em `Styles.xaml`; Inter (OFL) + Phosphor (MIT) em `THIRD-PARTY-NOTICES.md` |
| 2 | Tokens novos + morte do #E53E3E | PASS | comando literal, exit 0. 9 tokens + `#E08A8A` em `DesignTokens.xaml:86-95`; 0 ocorrencias de `#E53E3E` em `src/TranslateReader/` |
| 3 | Library desktop: top bar do mockup | PASS | comando literal, exit 0. 0 `<ToolbarItem`; 10 x:Name exigidos presentes (`ImportButton` LibraryPage.xaml:378, `GridToggleButton`:286, `ListToggleButton`:317); `FontFamily="Phosphor"` presente |
| 4 | List view real no desktop | PASS | comando literal, exit 0. `BooksCollection`:519 + `BooksListCollection`:675 irmas; `IsListView` LibraryPageModel.cs:28, `ShowGridView`:122/`ShowListView`:125; `ShowAttachedFlyout` LibraryPage.xaml.cs:44 |
| 5 | Grid adaptativo | PASS | comando literal, exit 0. `SizeChanged` LibraryPage.xaml.cs:16, const 187 (:8), `grid.Span = span` (:34), formula `max(3, (W+20)/187)` (:32), Phone idiom pulado (:29) |
| 6 | Reader: subtitulo + footer do mockup | PASS | comando literal, exit 0. `ChapterSubtitleLabel` ReaderPage.xaml:42, `ChapterSubtitle` ReaderPageModel.cs (novo `[ObservableProperty]` + `UpdateChapterSubtitle()` chamado nos 2 load paths), `ReaderFooter`:98, `PageProgressBar`:141, `AccentTint10`:236 |
| 7 | Settings: painel 380, cards de tema, segmented, lista de modelos | PASS | comando literal, exit 0. 380 em SettingsOverlay.xaml:20; `PaginatedModeButton`:133 < `ScrollModeButton`:149; `ModelsList`:296; 0 `Orientation="Horizontal"`; os 18 x:Name congelados presentes; atribuicao Tencent:397 |
| 8 | Popup: banner DEPOIS dos pickers, botoes outline, 440w | PASS | comando literal, exit 0. `OfflineBanner`:93 > `SourcePicker`:65; 440 em TranslateBookPopup.xaml:12; `BookMetaLabel`:40 |
| 9 | Compila (Windows Release 0 erros) | PASS | comando literal, exit 0 (`TestResults/pp-build.log`, `0 Error(s)`) |
| 10 | Suite verde com 10 testes novos, piso `Total >= 375` (D-...-9) | PASS | comando literal, exit 0. `Passed! - Failed: 0, Passed: 373, Skipped: 2, Total: 375` (`TestResults/pp-suite.log`); os 10 `[Fact]` com nomes exatos existem em `PixelSpecTests.cs` e tem assercoes reais (nao hollow) |
| 11 | Core intocado (prova da fronteira) | PASS | comando literal, exit 0. `BASELINE=82df8420...`; `git diff --name-only` vazio para `src/TranslateReader.Core/` e `Resources/Raw/` |

Nota sobre DoD 10: o "FAIL" reportado no SUMMARY.md era contra o piso ANTIGO (377). A correcao
D-...-9 (arquivo `.jdi/decisions/D-2026-08-02-pixel-perfect-9.md`, commit `3964ed0`) fixou o piso
em 375 apos root-cause do erro de contagem do planner (baseline real 365 = 316 `[Fact]` + 49
`[InlineData]`, confirmado independentemente). Com o CONTEXT.md como esta HOJE escrito, DoD 10
passa limpo. A recusa do doer de inflar a suite com testes de preenchimento foi a decisao correta.

## Deltas conscientes — verified

1. **Baseline 367 -> 365 / piso 377 -> 375** — CONCORDO. Verifiquei o decision file D-...-9 e
   re-executei a suite: 375 total, 0 falhas, 2 skips. O erro era do planejamento (CONTEXT.md),
   nao da execucao; corrigido na raiz (artefato errado), com precedente identico na phase
   app-redesign. Nao e discrepancia — e o piso vigente.
2. **TOC ativo: titulo fica ColorText (nao ColorAccent)** — CONCORDO COM RESSALVA (ver Warning
   W-1). O fallback esta implementado e documentado inline (`ReaderPage.xaml:203-207` + VisualState
   `Selected` com bg `AccentTint10` em :234-238), exatamente o que o PLAN pre-autorizou e mandou
   registrar. POREM a justificativa tecnica absoluta ("MAUI VisualState setters so alcancam o
   proprio elemento") e imprecisa: `Setter.TargetName` EXISTE no MAUI (confirmado por inspecao de
   `Microsoft.Maui.Controls.dll` 10.0.60 — `TargetName` presente; docs "Visual states: set state
   on multiple elements"), entao ha caminho XAML-only para o titulo ColorAccent. Nao e shortcut de
   ma-fe — o proprio PLAN afirmou que TargetName nao era suportado — mas e melhoravel sem
   code-behind. Warning, nao blocker.
3. **`FlyoutBase.ShowAttachedFlyout` via `#if WINDOWS`** — CONCORDO. `LibraryPage.xaml.cs:40-46`:
   handler definido FORA do `#if` (a assinatura existe em todos os TFMs, o wiring
   `Tapped="OnCardMenuTapped"` compila em Android/iOS; corpo no-op fora do Windows), build Windows
   0 erros. Verificacao independente do risco runtime: o WinUI `ShowAttachedFlyout` le a attached
   property `AttachedFlyout` (nao `ContextFlyout`) — inspecionei `Microsoft.Maui.Controls.dll`
   (net10.0-windows) e ele contem MemberRefs a `GetAttachedFlyout` E `SetAttachedFlyout`, ou seja,
   o mapper Windows do MAUI popula o slot que `ShowAttachedFlyout` consulta. Cada botao ⋮ carrega
   seu proprio `FlyoutBase.ContextFlyout` no MESMO elemento tocado (LibraryPage.xaml:628-637,
   :757-766), entao o sender passado ao bridge e o dono do flyout. Padrao consistente com o
   `#if WINDOWS` ja existente em `MauiProgram.cs` e com D-...-7. Smoke em device continua
   "Deferred to PR review" (CONTEXT), corretamente.
4. **`Tapped=` reutilizando handlers `(object?, EventArgs)`** — CONCORDO. `Tapped` e
   `EventHandler<TappedEventArgs>`; um metodo `(object?, EventArgs)` e conversivel por
   contravariancia de method group (C# spec), e o build Release com 0 erros e a prova empirica de
   que o XamlC aceita. Handlers nao usam `e`, comportamento identico. Verificado em
   SettingsOverlay.xaml:305/335/357/379 -> `OnGemmaClicked` etc. (SettingsOverlay.xaml.cs:260-286,
   assinaturas intactas) e TranslateBookPopup.xaml:122/138 -> `OnCancelClicked`/`OnTranslateClicked`
   (TranslateBookPopup.xaml.cs:66/73, contrato `(source, target)` inalterado). Os 3 eventos
   publicos do SettingsOverlay (`CloseRequested`/`SettingsChanged`/`DeleteModelRequested`) e
   `ShowAsync`/`HideAsync` animados preservados.
5. **Gutter externo aproximado (delta #5)** — CONCORDO. `LibraryPage.xaml:193` (`Padding="24,20"`
   na coluna principal) + :205 (top bar `OnIdiom Desktop='28,16'`, igual ao spec `p:16px 28px`) +
   hero `p16` (:419). O PIXEL-SPEC define paddings por secao (aplicados) mas nao um gutter unico
   da coluna; a aproximacao e razoavel e a paridade visual real e explicitamente "Deferred to PR
   review" no CONTEXT.
6. **(BLOCKED do doer) Qwen/Phi sem filename/tamanho** — RESOLUCAO CORRETA, NAO E BLOCKER.
   Li `TranslationManager.cs` (somente leitura): `ModelRegistry` (linhas 23-39) tem SO
   `gemma-2-2b` e `hy-mt1.5-1.8b`; `ResolveModel` (:47-48) faz fallback para Gemma em nome
   desconhecido — o proprio Core comenta "Qwen/Phi are offered in the UI but have no real download
   URL yet". Inventar filename/tamanho seria dado fabricado; adicionar entradas ao registry
   violaria D-...-1 (Core intocado, provado vazio pelo DoD 11). As rows renderizam sao
   (SettingsOverlay.xaml:327-369): labels estaticos, nenhum binding quebrado, nenhuma excecao
   possivel, selecao/radio funcionam via code-behind como antes, e ha comentario inline apontando o
   SUMMARY. Gap e do Core, para phase futura (ver W-6).

Verificacoes adicionais de higiene do diff completo (`82df842..HEAD`), alem do que o SUMMARY citou:
zero `FontAttributes="Bold"` ADICIONADO (0 linhas `+` no diff; os Bold remanescentes em
LibraryPage.xaml:798 e ReaderPage.xaml:267/307 sao overlays pre-existentes que o PIXEL-SPEC
"Diferencas intencionais mantidas" #3 manda nao redesenhar); zero hex solto adicionado fora de
`DesignTokens.xaml` (unico local com hex novo = os 9 tokens); todo `x:Name` removido no diff
reaparece adicionado (nenhum nome perdido; novos: BooksListCollection, ChapterSubtitleLabel,
GridToggleButton, ImportButton, ListToggleButton, ModelsList, PageProgressBar, ReaderFooter,
SearchBoxBorder + 4 RadioIcons + 2 segmented icons); todo `Clicked=` removido foi religado
(`Tapped=` ou `Clicked=` novo); todos os codepoints Phosphor usados batem com a tabela do
PIXEL-SPEC (E4A2, E758-Fill, E1A0, E610, E30C, E464, E5A2, E28C, E3D4, E06C, E2F0, E272, E138,
E13A, E4F6, E0E6, EB04, E184-Fill, E18A, E602, E208, E40C — conferidos um a um nos 4 XAML).
Regras csharp.md no codigo novo: sem sync-over-async (grep limpo); pareamento de eventos
INALTERADO vs baseline (5 `+=` / 4 `-=`, identico a `82df842` — o unico despareado e
`SizeChanged += OnPageSizeChanged` da propria pagina em si mesma, self-subscription pre-existente
que nao enraiza nada; a phase so trocou o corpo do handler); statics novos sao todos
`static readonly` imutaveis (SettingsOverlay.xaml.cs:124/203/233-235 — leituras de resource, em
conformidade com 5.12); CQS respeitado nos membros novos (`ShowGridView`/`ShowListView` so mutam,
`ChapterSubtitle`/`IsListView` sao propriedades geradas; `UpdateChapterSubtitle` void);
`PageProgressBar.Progress` atualizado dentro do mesmo dispatch de UI thread do
`PageIndicatorLabel` (ReaderPage.xaml.cs:468-477).

## Blockers

Nenhum.

## Warnings

- **W-1 (paridade TOC):** o titulo do item ativo do TOC pode chegar ao `ColorAccent` do mockup
  SEM code-behind via `Setter.TargetName` em VisualState (suportado pelo MAUI, ao contrario do que
  PLAN/SUMMARY assumiram — evidencia: `TargetName` presente em `Microsoft.Maui.Controls.dll`
  10.0.60 e documentado em "Visual states: set state on multiple elements"). Sugestao de follow-up
  em `ReaderPage.xaml:203-241`: `x:Name` no Label do titulo + `<Setter TargetName=...
  Property="Label.TextColor" Value="{StaticResource ColorAccent}">` no estado `Selected`.
- **W-2 (legado, pre-existente em superficie redesenhada):** `StrokeThickness="0,0,1,0"` em
  `LibraryPage.xaml:22` e `ReaderPage.xaml:183` — `Border.StrokeThickness` e `double`; o parse
  invariant com AllowThousands avalia essa string como **10** (verificado:
  `[double]::Parse('0,0,1,0', InvariantCulture)` = 10), nao "borda so na direita". Pre-existente
  no BASELINE `82df842` (fora do escopo desta phase, shipped na app-redesign), mas afeta a
  paridade visual que o humano vai julgar no PR: recomenda-se trocar por `StrokeThickness="1"` ou
  um BoxView de 1px na borda desejada.
- **W-3 (cor de hairline):** em LibraryPage, sidebar/busca/toggle/model-card usam
  `Stroke="{StaticResource Neutral800}"` onde o PIXEL-SPEC pede hairline `ColorDivider`
  (`#29E9E9ED`) para varios desses elementos (ex.: spec "Library — top bar" busca "borda hairline
  ColorDivider"; "sidebar ... Borda direita: hairline ColorDivider"). Substituicao consistente e
  visualmente proxima, mas nao e o token da spec. Julgamento final na comparacao lado a lado do PR.
- **W-4 (icone nao aplicado):** `arrow-left E058` ("voltar (reader)") da tabela do PIXEL-SPEC nao
  foi aplicado — o back button continua o nativo do Shell (`Shell.BackButtonBehavior`,
  ReaderPage.xaml:16-18). Nenhuma task/DoD o exigia explicitamente; registrado como pendencia de
  paridade para o PR review.
- **W-5 (lint, legado):** `dotnet format --verify-no-changes` acusa 3 WHITESPACE em
  `ThemeEngine.cs:12/14` e `ThemeEngineTests.cs:12` — arquivos NAO tocados pela phase (Core provado
  intocado pelo DoD 11). WARN legado por D-2; vira BLOCK-on-new quando a phase
  `baseline-de-estilo` shippar `.editorconfig`.
- **W-6 (UX dos modelos placeholder):** o usuario ainda PODE selecionar Qwen/Phi na UI e o Core
  silenciosamente resolve para Gemma (`ResolveModel`, comportamento pre-existente e comentado no
  Core). Com as rows agora mostrando so o nome (sem filename/size), a assimetria ficou visivel.
  Follow-up sugerido (phase futura, fora desta fronteira): ou adicionar as entradas reais ao
  `ModelRegistry` do Core, ou desabilitar/ocultar as rows sem registro.
- **W-7 (nit de teste):** `PixelSpecTests.cs:119` usa `Assert.Matches(new Regex("Span"), ...)`
  onde `Assert.Contains("Span", ...)` bastaria — `new Regex(...)` por chamada e contra o espirito
  de csharp.md §2.1 (irrelevante em teste, mas gratuito).

## Notes for next iteration

N/A — verdict nao e BLOCKED. (Follow-ups nao-bloqueantes listados em Warnings; paridade visual
real, hinting de fonte e smoke em device permanecem "Deferred to PR review" conforme CONTEXT.md.)
