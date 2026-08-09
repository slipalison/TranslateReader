# Phase 23: Review  (slug: snippet-translation)

**Verdict:** APPROVED_WITH_WARNINGS

**Iter:** 2 (ralph loop, re-verify pos-fix de B-1/W-1) · Reviewer: `jdi-reviewer-translatereader` (mode=verify + dod-critic fold-in, D-5 enhanced) · Data: 2026-08-09
**HEAD revisado:** `deb5864` · Fixes revisados: `7c4b236` (B-1), `9c0bd44` (W-1) · Baseline da phase: `02a4c6c`

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `net10.0-windows10.0.19041.0` Release: exit 0, `0 Aviso(s), 0 Erro(s)` (log lido). Phase nao tocou `Platforms/` -> build mobile secundario nao exigido |
| Tests | PASS | Suite verde: **404 passed / 0 failed / 2 skipped (GPU-only pre-existentes) / 406 total** >= baseline 167 (D-2), +2 vs iter 1. JS: **130/130, 0 skipped**, +3 vs iter 1. Zero teste perdido nome a nome vs `main` (`comm -23` vazio, C# e JS — re-executado por este review) |
| Coverage | PASS | `bash scripts/coverage-gate.sh` **exit 0 capturado diretamente** (execucao propria, 2x): `COVERAGE_SCOPE covered=1313 valid=1385 pct=94.80 files=26` (piso 90, D-6); `COVERAGE_JS covered=1249 valid=1266 pct=98.66 files=5` (piso 85, D-...-4); `COVERAGE_GUARD new_app_cs=0 waived=0`; zero `COVERAGE_WAIVER_INVALID` |
| Lint | WARN | `dotnet format whitespace --verify-no-changes` **exit 2** — as MESMAS 2 violacoes FINALNEWLINE legadas do iter 1 (`Platforms/Android/MainActivity.cs`, `MainApplication.cs`), fora do diff da phase (o doer reverteu de proposito o fix incidental, D-2/W-7). Arquivos tocados pela phase: limpos. Legado = WARN, nunca BLOCK. Nota: o SUMMARY declara "exit 0, limpo" — impreciso no escopo solution (ver Warnings) |
| Security/Layer | PASS (warn) | 5.1-5.17 re-executados: nenhum achado BLOCK-class. **B-1 do iter 1 verificado como CORRIGIDO na fonte, cenario a cenario (detalhe abaixo)**. W-1 corrigido (Manager -> Access, direcao legal). 3 warnings novos de baixa severidade (W-9/W-10/W-11) |
| Consistency | PASS | 13 commits conventional, escopo `snippet-translation`, tipos adequados (`fix` para os 2 fixes, `docs` para o SUMMARY). Fixes tocam exatamente os arquivos que o B-1/W-1 pediam + testes; SUMMARY iter 2 apendado sem apagar iter 1 |
| UI Validation | SKIPPED | has_frontend=false (native MAUI client) — por design, nunca bloqueia |
| DoD | PASS | **10/10 auto PASS, 0 manual** (`dod=auto_only`; PROJECT.md nao declara secao DoD). Todos os `Verify:` re-executados integralmente por este review em iter 2 — nada herdado do iter 1 nem do self-report do doer |

## Blockers

Nenhum.

### B-1 (iter 1) — verificacao cetica do fix, elo por elo (nao foi rubber stamp)

O blocker do iter 1 tinha 3 exigencias. Cada uma foi verificada na fonte em HEAD, nao no diff:

1. **Engine garantida antes de gerar, nos DOIS modos.** `ReaderPageModel.TranslateSnippetsAsync`
   (`src/TranslateReader/PageModels/ReaderPageModel.cs:344-362`) agora chama
   `EnsureModelDownloadedAsync(ct)` incondicionalmente antes de `RunSnippetTranslationsAsync` — nao
   ha NENHUM guard de `ReadingMode` em todo o caminho (confirmado por leitura do metodo e do
   `EnsureModelDownloadedAsync:286-311`, que baixa via `DownloadModelIfNeededAsync` e inicializa via
   `InitializeEngineIfNeededAsync` -> `TranslationEngine.InitializeAsync`). Em rolagem, este e agora
   o unico caminho do reader que inicializa a engine — o requisito inegociavel 4 volta a se sustentar.
   O download frio NAO e silencioso: reusa o overlay modal full-page (`Grid.RowSpan="4"`,
   `InputTransparent="False"`, progresso + botao Cancelar — `ReaderPage.xaml:272-310`), e
   `OnCancelDownloadClicked` passou a cancelar tambem `_snippetCts` (`ReaderPage.xaml.cs:585-592`).
2. **Excecao nao vaza mais em silencio.** A fronteira excecao->estado amigavel vive no PageModel:
   `catch (OperationCanceledException) { throw; }` (OCE sempre flui, nunca vira erro) seguido de
   `catch (Exception)` -> log + `DisplayAlert` + retorno `[]` (`ReaderPageModel.cs:352-361`).
   Nao e `[RelayCommand]`, mas segue o MESMO padrao de fronteira ja estabelecido no arquivo
   (`InitializeAsync:98-102`, `LoadCurrentChapterAsync:130-134`) — e a justificativa do doer
   (AsyncRelayCommand geraria cancelamento proprio conflitando com o `_snippetCts` da Page) e
   tecnicamente correta. Julgamento: compativel com a intencao do `csharp.md` §1 (UMA fronteira, na
   camada PageModel). `HandleSnipRequestAsync` re-lanca a OCE apos limpar (`:491-499`) e ela so e
   absorvida na fronteira terminal do event handler (`OnHybridMessageReceived:115`), padrao baseline.
3. **Placeholder limpo em falha E em cancelamento, JS e C# conferidos ponta a ponta.**
   `window.clearSnippetLoading` (`snippets.js:904-914`) casa EXATAMENTE com a estrutura que
   `setSnippetLoading` (`:870-889`) constroi: o placeholder carrega `data-si = a` e o texto original
   e o `childNodes[0]` (text node criado por `span.textContent = original` ANTES do append do
   blob/mask) — a leitura `span.childNodes[0].textContent` e correta. `_spliceSpanBackToPeriods`
   (extraida de `_restoreSnipToPeriods`, mesma semantica de iter 1) devolve os periodos com os
   listeners `pointerdown` re-anexados — o usuario consegue tentar de novo. Os DOIS call sites C#
   existem e usam `keysJson` pre-serializado via `ReaderJsonContext` (gate 5.8 ok): catch de OCE
   (`ReaderPage.xaml.cs:497`, corrida suplantada/Cancelar do overlay) e `results.Count == 0`
   (`:506`, falha ja convertida em alerta pelo PageModel — sentinel nao ambiguo: `requests` vazio
   retorna cedo e sucesso com >=1 request sempre produz >=1 resultado). 3 testes JS novos passam
   por nome exato (restaura periodos / nao toca snip aplicado / no-op sem paragrafo) + 1 teste C#
   de contrato (`SnippetsJs_ExposesClearSnippetLoading`) — re-executados por este review.

Os 3 cenarios de falha do iter 1 foram re-simulados na fonte: (a) sessao nova paginada com cache
miss -> download visivel -> engine inicializa -> traduz; falha de rede -> alerta amigavel +
placeholder restaurado; (b) rolagem -> mesmo caminho, sem guard de modo; (c) `snip|` concorrente ->
OCE do primeiro -> `clearSnippetLoading(keys1)` -> re-lanca -> absorvida no topo; chaves iguais sao
inalcancaveis porque os periodos do primeiro range estao consumidos no placeholder.

Limite honesto da verificacao: `ReaderPage.xaml.cs`/`ReaderPageModel.cs` sao
`app-maui-not-instrumented` (modelo de teste locked do repo) — a cobertura do fix nelas e
estrutural (contract test + build + leitura), e a paridade visual/device real ja esta
explicitamente em `## Deferred to PR review` no CONTEXT. Tudo que e mecanicamente verificavel foi
verificado.

### W-1 (iter 1) — verificado como corrigido

`LibraryManager.DeleteBookAsync` agora chama `snippetTranslationAccess.RemoveSnippetsForBookAsync(bookId)`
junto da limpeza de `TranslationCache`/`ReadingState` (`LibraryManager.cs:74`); DI atualizado em
`MauiProgram.cs`; ctor com 7 params (dentro do limite). Direcao de camada legal (Manager -> Access).
Teste novo real: `DeleteBookAsync_RemovesSnippetTranslationsForTheBook` asserta
`Received(1).RemoveSnippetsForBookAsync(7)` contra interface (NSubstitute so em contrato).

## Warnings

Abertos do iter 1 (W-2..W-8, intocados por instrucao explicita do orquestrador — re-verificados
como ainda aplicaveis em HEAD):

- **W-2 — Hint pode ressuscitar com a camada desmontada (JS).** Ainda aplicavel: listeners de
  documento continuam ativos apos `unmountSnippetLayer()`; `_clearSelection` -> `_renderHint`
  re-anexa `.tr-hint` se `_hintDismissed=false` (`snippets.js:433-458`).
- **W-3 — `SnippetRequest.ChapterHRef` tipado `string` nao-anulavel mas carrega `null` do WebView**
  no modo paginado. Ainda aplicavel (`SnippetRequest.cs:4`; tratado em `ReaderPage.FillCurrentChapter:455-464`).
- **W-4 — `_APP_ACCENT = '#9184d9'` hardcoded** (`snippets.js:133`) duplica o accent dos tokens XAML
  (DRY). Ainda aplicavel.
- **W-5 — Linhas novas 0% cobertas em `SnippetLabels.cs` (0/15) e `SnippetTheme.cs` (0/1)** — ainda
  aplicavel (confirmado no stdout do gate deste review); gate agregado passa com folga.
- **W-6 — Legado: `dotnet test` a nivel de solution sai 1** (CA1711 em TFMs iOS/MacCatalyst de
  `Platforms/*/AppDelegate.cs`; calibracao cobre so `test/**`). Nenhum arquivo relevante mudou na
  iter 2 — segue pre-existente de `main`, rota = phase futura.
- **W-7 — Legado: FINALNEWLINE em `Platforms/Android/MainActivity.cs` e `MainApplication.cs`**
  (`dotnet format whitespace` exit 2 — re-confirmado nesta iter; o doer reverteu o fix incidental de
  proposito, coerente com D-2/escopo). Nota de precisao: o SUMMARY iter 2 declara o comando "exit 0,
  limpo" — a nivel de solution isso nao e verdade; os arquivos DA PHASE estao limpos.
- **W-8 — Nota de julgamento (aceito, sem acao):** OCE absorvida so na fronteira terminal de event
  handler (`ReaderPage.xaml.cs:115`), padrao baseline. A iter 2 MELHOROU este quadro: os catches
  novos de OCE re-lancam (`ReaderPageModel.cs:352`, `ReaderPage.xaml.cs:491`). `catch { }` de
  `:442`/`:699` seguem legado.

Novos desta iteracao (decorrentes do proprio fix — baixa severidade, nenhum bloqueia):

- **W-9 — Fluxo de snippet sem o guard de reentrancia que `TranslateAsync` tem.**
  `TranslateSnippetsAsync` nao checa `IsModelDownloading || IsModelLoading` antes de
  `EnsureModelDownloadedAsync`, e nem `TranslationManager.DownloadModelIfNeededAsync`/
  `InitializeEngineIfNeededAsync` tem `SemaphoreSlim` (csharp.md §3 pede semaforo/Lazy para init
  caro unico). Na pratica a corrida e quase inalcancavel (o overlay modal full-page bloqueia input
  durante download/load, e a selecao JS e limpa antes do `sendRawMessage`) e o pior caso falha
  ALTO, nao corrompe (`ModelAccess` baixa para `.tmp` com `FileShare.None` + `File.Move` atomico ->
  IOException -> alerta amigavel + placeholder limpo). Defense-in-depth barato: guard de 1 linha no
  PageModel ou semaforo no Manager. Nao exigido nesta phase.
- **W-10 — Afinidade de thread das propriedades observaveis no caminho novo.** A iter 2 fez o
  handler de mensagem hybrid (`OnHybridMessageReceived` -> `TranslateSnippetsAsync` ->
  `EnsureModelDownloadedAsync`) passar a mutar `IsModelDownloading`/`IsModelLoading`/
  `ModelDownloadProgress` (`[ObservableProperty]` -> `PropertyChanged` -> bindings). No Windows
  (alvo de verificacao locked; WebView2 entrega `WebMessageReceived` na thread do dispatcher) esta
  correto. O contrato do HybridWebView NAO garante UI thread em Android/iOS — quando traducao mobile
  existir (LLamaSharp hoje e Windows-only), marshalar via `MainThread`/`Dispatcher` (csharp.md §3).
  Forward-looking, sem acao nesta phase.
- **W-11 — Falha parcial em selecao multi-trecho diverge UI/banco ate o proximo reload.** Um
  `snip|` pode carregar N ranges (`_runsOf`); `TranslateSnippetAsync` salva cada snippet no SQLite
  conforme conclui. Se o trecho k falhar, o PageModel descarta o batch inteiro (`return []`) e a
  Page limpa TODOS os placeholders — mas os k-1 ja persistidos reaparecem traduzidos no proximo
  `restoreSnippets` (re-selecao vira cache hit; nada quebra, so surpreende). Minor; resolucao
  natural se um dia o fluxo aplicar resultados parciais.

## Gate 5 — detalhe das checagens (todas re-executadas neste review, iter 2)

| Check | Resultado |
|---|---|
| 5.1 Client -> Access/Engine | limpo (grep sem output) |
| 5.2 Storage em `Contracts/Access` | limpo |
| 5.3 Manager -> Manager | limpo — hits sao cada Manager implementando o proprio contrato; `LibraryManager` novo injeta ACCESS, nao Manager |
| 5.4 PageModel >1 Manager por caso de uso | ok — inalterado vs iter 1; caso de uso de snippet usa so `ISnippetTranslationManager` (a orquestracao de engine-readiness usa `ITranslationManager` via `EnsureModelDownloadedAsync` pre-existente — mesmo PageModel, caso de uso de preparo de modelo compartilhado com o fluxo legado, mesmo desenho de `TranslateAsync`) |
| 5.5 Regra de negocio em Manager/PageModel | ok — `DeleteBookAsync` segue sequencia pura de Access; `TranslateSnippetsAsync` orquestra + fronteira de erro, sem regra de negocio |
| 5.6 Zip-slip | so baseline (`ParsingEngine`, intocado) |
| 5.7 XXE | zero hits |
| 5.8 WebView JS injection | limpo — os 2 call sites novos (`clearSnippetLoading({keysJson})` em `:497`/`:506`) usam `keysJson` pre-serializado (`ReaderJsonContext.Default.ListString`); zero interpolacao crua |
| 5.9 Secrets/PII em log | limpo |
| 5.10 Sync-over-async | zero hits; `ct` flui Page -> PageModel -> Manager -> Engine; OCE dos catches novos re-lanca (ver W-8) |
| 5.11 Eventos +=/-= | subscribe=5 / unsubscribe=4 — IDENTICO ao baseline do bootstrap; iter 2 nao adicionou evento C# |
| 5.12 Static mutavel | nenhum novo (baseline `_nativeLibraryConfigured` + expression-bodied de `SettingsOverlay`) |
| 5.13 Cache in-memory sem bound | nenhum novo (hits so em `obj/` gerado) |
| 5.14 Alocacao em hot path | limpo |
| 5.15 Fail fast | **B-1 resolvido** (fronteira no PageModel, OCE flui, placeholder limpo); `catch { }` restantes (`:442`,`:699`) sao legado |
| 5.16 TODO sem ticket | zero |
| 5.17 Disciplina de teste D-2 | NSubstitute so contra interfaces (novo mock: `ISnippetTranslationAccess`); teste W-1 asserta chamada real; I/O real em teste so nos padroes baseline abencoados (FileUtility/ModelAccess/contract tests) |

## DoD Checklist (gate 8)

Todos os `Verify:` re-executados integralmente por este review na iter 2 (nada herdado).

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | Ground truth v0.2.0 existe e e medida (PIXEL-SPEC + >= 4 screenshots) | CONTEXT (D-...-4) | Auto | PASS | exit 0; 14 literais presentes; 8 screenshots |
| 2 | Tabela/Model/Access novos, storage invisivel no contrato, round-trip testado | CONTEXT (D-...-1) | Auto | PASS | exit 0; filtro `~Snippet`: **30 passed / 0 failed** (piso 12; +2 vs iter 1) |
| 3 | Split de periodos: UMA funcao, regex literal, fronteiras testadas | CONTEXT (D-...-4) | Auto | PASS | exit 0; regex 1x; testes `splitSentences` pass |
| 4 | Geometria dourada do blob (4 testes de nome exato) | CONTEXT (D-...-4) | Auto | PASS | exit 0; 4 nomes exatos ok no TAP |
| 5 | Persistencia: restaura, respeita toggle, descarta hash divergente | CONTEXT (D-...-1) | Auto | PASS | exit 0; 4 testes de nome exato pass |
| 6 | Independente do modo: `_snippetRoots` unica fonte, 2 modos testados | CONTEXT (D-...-3) | Auto | PASS | exit 0; contagem corpo==arquivo; 2 testes de raiz pass |
| 7 | 3 JS congelados intocados, seletor nao duplicado, ordem de carga | CONTEXT (D-...-2,-3) | Auto | PASS | exit 0; diff vazio vs BASELINE `02a4c6c` re-verificado em HEAD |
| 8 | Gate de cobertura com 5 JS e sem `.cs` novo no app MAUI | CONTEXT (D-...-2) | Auto | PASS | exit 0; `files=5`; `GUARD new_app_cs=0 waived=0`; pisos 90/85 inalterados |
| 9 | Build limpo + 2 suites verdes, piso derivado de `main`, zero teste perdido | CONTEXT (D-...-6) | Auto | PASS | exit 0; build `0 Error(s)`; C# 406 total (B=375, piso 387); JS 130/130, `# skipped 0`; `comm -23` vazio (C# e JS) |
| 10 | Literais visuais na spec E no JS; zero pt-BR no JS; rotulos via `setSnippetLabels` | CONTEXT (D-...-2,-4) | Auto | PASS | exit 0; 15 literais nos 2 arquivos; grep pt-BR no JS = 0; 9 strings pt-BR no ReaderPage |

**Totals:** 10 items | Auto: 10 (10 PASS, 0 FAIL) | Manual: 0 pending

Nota: `.jdi/PROJECT.md` nao possui secao `## Definition of Done`; o conjunto acima (CONTEXT.md,
`dod=auto_only`) e o DoD integral. Itens humanos (paridade visual, blur/drag em device, qualidade
linguistica, posicao da pill) permanecem em `## Deferred to PR review` por decisao do CONTEXT.

## DoD-critic (D-5 enhanced, segunda passada — mesmo reviewer, mode=dod-critic)

Re-inspecao adversarial dos 10 rows Auto/PASS contra os ARTEFATOS em HEAD (nao contra os comandos):

```json
[
  {"row": 1, "hollow": false, "evidence": "design/v0.2.0 intocado pelos commits da iter 2; spec e 8 JPGs identicos ao verificado no iter 1"},
  {"row": 2, "hollow": false, "evidence": "SnippetTranslationAccess.cs intocado na iter 2 (9c0bd44 toca LibraryManager, nao o Access); DDL+UNIQUE re-grepados; 30 testes ~Snippet reais (28 do iter 1 + W-1 + contrato clearSnippetLoading)"},
  {"row": 3, "hollow": false, "evidence": "regex 1x em snippets.js (re-contado apos o diff da iter 2, que adicionou _spliceSpanBackToPeriods sem duplicar o split — _splitSentences segue fonte unica, chamada nos 2 consumidores)"},
  {"row": 4, "hollow": false, "evidence": "paths dourados intocados; testes por nome exato re-executados nesta iter"},
  {"row": 5, "hollow": false, "evidence": "harness real, DOM mutado; hash divergente deixa paragrafo intacto — re-executado nesta iter"},
  {"row": 6, "hollow": false, "evidence": "_snippetRoots segue o unico lugar com _pager/chapter-content (contagem corpo==arquivo re-feita em HEAD, pos-diff da iter 2)"},
  {"row": 7, "hollow": false, "evidence": "git diff vazio nos 3 JS congelados re-verificado contra BASELINE real em HEAD (a iter 2 tocou so snippets.js entre os JS)"},
  {"row": 8, "hollow": false, "evidence": "gate re-executado 2x por este review com exit code capturado direto (0): SCOPE 94.80, JS 98.66 files=5, GUARD 0/0"},
  {"row": 9, "hollow": false, "evidence": "406 C# reais / 130 JS reais rodados por este review; comm -23 vazio nos 2 lados; piso B+12=387 superado com margem"},
  {"row": 10, "hollow": false, "evidence": "clearSnippetLoading/_spliceSpanBackToPeriods novos lidos na integra: comentarios em ingles, zero pt-BR novo; literais visuais intactos"}
]
```

**Resultado do critic: nenhum row hollow.** Nenhum Auto/PASS rebaixado; verdito inalterado pela
segunda passada (o critic so aperta, nunca afrouxa).

## Recommendation

**B-1 esta genuinamente corrigido** — verificado elo a elo na fonte (engine readiness incondicional
e mode-independent; fronteira de erro no PageModel com OCE fluindo; limpeza de placeholder nos dois
call sites, com o JS casando exatamente a estrutura do `setSnippetLoading`) e coberto por 4 testes
novos que este review re-executou por nome. **W-1 idem** (1 linha de fiacao + DI + teste real).
Nenhum blocker novo emergiu do fix; os 3 warnings novos (W-9 guard de reentrancia, W-10 afinidade
de thread fora do Windows, W-11 divergencia transitoria em falha parcial) sao baixa severidade,
mitigados por construcao (overlay modal, `.tmp`+`FileShare.None`, restore no reload) e nao exigem
acao nesta phase. Gates mecanicos todos verdes com folga (94.80/98.66, 406+130 testes, DoD 10/10
sem PASS oco).

Recomendo **ship**. Na PR humana, alem dos itens ja deferidos pelo CONTEXT, vale um smoke manual do
caminho de demo (sessao fria -> selecionar trecho -> overlay de download -> traduzir; e o Cancelar
no meio) — e o unico elo nao mecanizavel do B-1. W-2..W-11 ficam como candidatos a uma phase de
higiene futura (`.jdi/todos/`), destacando W-9 (guard barato) e a correcao da nota do SUMMARY sobre
o lint (W-7).
