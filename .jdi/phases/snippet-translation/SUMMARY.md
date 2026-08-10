# Phase 23: Traducao de trechos por selecao de periodos — Summary (slug: snippet-translation)

**Status:** complete
**Tasks:** 8/8 complete, 0 blocked

## Executed tasks
- T-1 (`d9c92d9`): baseline commit gravado em `BASELINE` (`02a4c6c`); DoD 1 (PIXEL-SPEC + screenshots v0.2.0) verificado, ja existia da fase de planejamento.
- T-2 (`bdf914d`): harness JS ganhou `getClientRects`, `closest`, `elementFromPoint` (14 testes novos).
- T-3 (`84f3288`): `js/snippets.js` criado — nucleo puro: `_splitSentences`, `_runsOf`, `_snipHash`, `_blobPath`/`_blobFromEls`, `_snippetRoots` (19 testes); `coverage-gate.sh` passou a listar 5 arquivos JS.
- T-4 (`0f45028`): camada visual — CSS literal, spans de periodo, drag/tap de selecao, blob de vidro, pill, hint de primeira vez, fontes Inter/Phosphor embarcadas (16 testes).
- T-5 (`62e7ba9`): persistencia no JS — `restoreSnippets`/`applySnippetTranslation`/`setSnippetLoading`, chip de idioma, extracao de `sendRawMessage` em `bridge.js` (15 testes).
- T-6 (`5680f93`): tabela `SnippetTranslations`, `SnippetTranslation` model, `ISnippetTranslationAccess`/`SnippetTranslationAccess`, DI em `MauiProgram.cs` (11 testes).
- T-7 (`48fbaa2`): `ISnippetTranslationManager` implementado por `TranslationManager`, prompt contextual (trecho + paragrafo) em `PromptUtility`, cache via `TranslationCache`, `ResolveThemeColors` em `SettingsManager` (13 testes). Hash dourado `9d2a73a5` confirmado identico em JS (FNV-1a) e C#.
- T-8 (`0964c19`): `ReaderPage` — despacho de mensagem `snip|`, `ReaderJsonContext`, ciclo de vida da camada de snippets por capitulo, coexistencia com a traducao por paragrafo, limpeza de selecao na navegacao (5 testes).

## Blocked tasks
Nenhuma.

## Desvios documentados (nao sao scope creep — decorrem de decisoes locked das proprias tasks)
1. `test/TranslateReader.Tests/TranslationManagerTests.cs` ajustado: T-7 adiciona o 9o parametro de construtor a `TranslationManager` (novo substituto + arg no ctor de teste).
2. `src/TranslateReader.Core/Models/SnippetLabels.cs` + `SnippetTheme.cs` novos: payload `JsonTypeInfo` AOT-safe para `setSnippetLabels`, exigido pela propria instrucao literal da T-8 e pela regra "nunca reflection"; ficam em `Core/Models/` (nao em `src/TranslateReader/`).
3. `src/TranslateReader/Resources/Raw/wwwroot/js/snippets.js` retocado pela T-8: removida a chamada eager de `setSnippetLoading` em `_onTranslateClick` — o handler C# passa a ser o unico dono do estado de loading, por desenho explicito da T-8; 1 teste JS atualizado.

## Incompletude conhecida e sinalizada (nao e divida escondida)
Derivacao D (CONTEXT.md): paragrafo com markup inline (`<em>`/`<a>`/`<img>`) vira UM periodo unico
em vez de splitar dentro do markup — fora de escopo desta phase por decisao, registrado como todo
futuro em `.jdi/todos/2026-08-09-snippet-translation.md`.

## Files modified
- .jdi/phases/snippet-translation/BASELINE (novo)
- .jdi/phases/snippet-translation/PLAN.md
- scripts/coverage-gate.sh
- src/TranslateReader.Core/Access/SnippetTranslationAccess.cs (novo)
- src/TranslateReader.Core/Business/Managers/SettingsManager.cs
- src/TranslateReader.Core/Business/Managers/TranslationManager.cs
- src/TranslateReader.Core/Contracts/Access/ISnippetTranslationAccess.cs (novo)
- src/TranslateReader.Core/Contracts/Managers/ISettingsManager.cs
- src/TranslateReader.Core/Contracts/Managers/ISnippetTranslationManager.cs (novo)
- src/TranslateReader.Core/Contracts/Utilities/IPromptUtility.cs
- src/TranslateReader.Core/Models/SnippetLabels.cs (novo)
- src/TranslateReader.Core/Models/SnippetRemoveRequest.cs (novo)
- src/TranslateReader.Core/Models/SnippetRequest.cs (novo)
- src/TranslateReader.Core/Models/SnippetTheme.cs (novo)
- src/TranslateReader.Core/Models/SnippetToggleRequest.cs (novo)
- src/TranslateReader.Core/Models/SnippetTranslation.cs (novo)
- src/TranslateReader.Core/Utilities/PromptUtility.cs
- src/TranslateReader/MauiProgram.cs
- src/TranslateReader/PageModels/ReaderPageModel.cs
- src/TranslateReader/Pages/ReaderPage.xaml.cs
- src/TranslateReader/Resources/Raw/wwwroot/fonts/Inter-Medium.ttf (novo)
- src/TranslateReader/Resources/Raw/wwwroot/fonts/Inter-Regular.ttf (novo)
- src/TranslateReader/Resources/Raw/wwwroot/fonts/Phosphor.ttf (novo)
- src/TranslateReader/Resources/Raw/wwwroot/index.html
- src/TranslateReader/Resources/Raw/wwwroot/js/bridge.js
- src/TranslateReader/Resources/Raw/wwwroot/js/snippets.js (novo)
- src/TranslateReader/Serialization/ReaderJsonContext.cs
- test/TranslateReader.Tests/HybridWebViewContractTests.cs
- test/TranslateReader.Tests/PromptUtilityTests.cs
- test/TranslateReader.Tests/SettingsManagerTests.cs
- test/TranslateReader.Tests/SnippetTranslationAccessTests.cs (novo)
- test/TranslateReader.Tests/SnippetTranslationManagerTests.cs (novo)
- test/TranslateReader.Tests/TranslationManagerTests.cs
- test/js/bridge.test.js
- test/js/harness.js
- test/js/harness.test.js
- test/js/snippets.test.js (novo)

## Tests
- C#: 404 total (402 passed, 2 skipped pre-existentes GPU-only, 0 failed). Baseline `main` = 375; piso da phase (B+12..B+20) = 387..395 — entregue acima do teto.
- JS: 127 total (127 passed, 0 failed, 0 skipped). `comm -23` contra `main` confirma zero teste perdido em C# ou JS.
- Coverage (`bash scripts/coverage-gate.sh`, escopo AM pos-`4285f25`): C# 94.79% (1311/1383, floor 90%), JS 98.54% (1216/1234, floor 85%, files=5). `COVERAGE_GUARD new_app_cs=0` — nenhum `.cs` novo sem instrumentacao no app MAUI.
- Build: `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` — 0 Warning(s), 0 Error(s).
- Lint: `dotnet format whitespace --verify-no-changes` limpo em todo arquivo tocado pela phase.

## Iter 2 (ralph loop, pos-REVIEW.md BLOCKED) — fix de B-1 e W-1

Reviewer (iter 1, `b7df369`) bloqueou por B-1: o fluxo `snip|` nunca garantia a engine pronta e
engolia a falha sem estado de erro (primeiro uso em sessao nova falhava em silencio; modo rolagem
nao tinha caminho algum que inicializasse a engine — violando o requisito inegociavel 4; um segundo
`snip|` cancelando o primeiro deixava os placeholders do primeiro orfaos pulsando).

**Fix B-1** (`7c4b236`):
- `ReaderPageModel.TranslateSnippetsAsync` agora chama `EnsureModelDownloadedAsync(ct)` ele mesmo,
  antes de traduzir — independente de `ReadingMode` (ao contrario de `TranslateAsync`, que recusa
  Scroll). Isso cobre paginado E rolagem com o MESMO overlay visivel de download/carregamento
  (`IsModelDownloading`/`IsModelLoading`) ja usado pela traducao por paragrafo — decisao explicita
  de nao inventar um segundo overlay silencioso: reusar o existente ja satisfaz "nao e download
  silencioso de 2 GB" (progresso visivel + botao Cancelar).
- A fronteira UMA-so de conversao excecao->estado amigavel (csharp.md S1) passou a viver no
  PageModel: `TranslateSnippetsAsync` faz `catch (OperationCanceledException) { throw; }` (nunca
  convertida em erro, sempre flui) seguido de `catch (Exception ex)` que loga e mostra
  `DisplayAlert`, devolvendo lista vazia. Nao virou `[RelayCommand]` (o CommunityToolkit geraria
  cancelamento proprio, incompativel com o `_snippetCts` que a Page ja usa para "segundo `snip|`
  cancela o primeiro"); segue o MESMO padrao ja usado por `LoadCurrentChapterAsync`/`InitializeAsync`
  no mesmo arquivo — metodo de PageModel, entry point de um caso de uso, catch+log+DisplayAlert.
- `ReaderPage.HandleSnipRequestAsync`: na captura de `OperationCanceledException` (a corrida foi
  suplantada por um `snip|` mais novo, ou pelo Cancelar do overlay de download) e quando
  `results.Count == 0` (falha ja convertida em alerta pelo PageModel), chama o novo
  `clearSnippetLoading(keysJson)` antes de (re)lancar/retornar — o placeholder `.tr-loading` para
  de pulsar nos dois casos.
- `OnCancelDownloadClicked` agora tambem cancela `_snippetCts` (o overlay e compartilhado entre os
  dois fluxos desde este fix).
- `snippets.js`: `window.clearSnippetLoading(keys)` novo — inverso de `setSnippetLoading`, splica o
  texto original (capturado no `childNodes[0]` do placeholder, antes do blob) de volta em periodos
  individuais via `_spliceSpanBackToPeriods` (extraida de `_restoreSnipToPeriods`, que passou a
  chamar essa mesma funcao — DRY, comportamento identico ao anterior).
- Testes: 6 novos (+3 JS em `snippets.test.js`: `clearSnippetLoading` restaura periodos, nao toca
  snip ja traduzido, no-op se o paragrafo sumiu; +1 C# `HybridWebViewContractTests` confere
  `window.clearSnippetLoading` presente; ver tambem W-1 abaixo).

**Fix W-1** (`9c0bd44`): `LibraryManager` ganhou `ISnippetTranslationAccess` no ctor (7 params,
dentro do limite); `DeleteBookAsync` agora chama `RemoveSnippetsForBookAsync(bookId)` junto da
limpeza de `TranslationCache`/`ReadingState`. DI em `MauiProgram.cs` atualizado. +1 teste em
`LibraryManagerTests.cs` (`DeleteBookAsync_RemovesSnippetTranslationsForTheBook`).

**Nao alterado:** W-2..W-8 (fora de escopo desta iteracao por instrucao explicita — so B-1 e W-1).

**Verificacao pos-fix:**
- Build Windows Release: `0 Warning(s), 0 Error(s)`.
- C#: 406 total (404 passed, 2 skipped GPU-only pre-existentes, 0 failed) — +2 vs os 404 do iter 1
  (`DeleteBookAsync_RemovesSnippetTranslationsForTheBook`, `SnippetsJs_ExposesClearSnippetLoading`).
- JS: 130 total (130 passed, 0 failed, 0 skipped) — +3 vs os 127 do iter 1.
- `bash scripts/coverage-gate.sh`: exit 0. `COVERAGE_SCOPE covered=1313 valid=1385 pct=94.80
  files=26` (piso 90). `COVERAGE_JS covered=1249 valid=1266 pct=98.66 files=5` (piso 85).
  `COVERAGE_GUARD new_app_cs=0 waived=0`. `ReaderPageModel.cs`/`ReaderPage.xaml.cs` seguem
  `COVERAGE_SKIP reason=app-maui-not-instrumented` (o projeto de teste so referencia
  `TranslateReader.Core` — nao ha como testar unitariamente essas duas classes neste repo; a
  cobertura do fix nelas e estrutural, via `HybridWebViewContractTests` + build).
- `dotnet format whitespace --verify-no-changes`: exit 0, limpo.
- Reverti incidentalmente `Platforms/Android/MainActivity.cs`/`MainApplication.cs` (o `dotnet
  format` rodado sem escopo teria corrigido o FINALNEWLINE legado do W-7) — fora do escopo desta
  iteracao (D-2, W-7 e explicitamente "phase futura").

Commits: `7c4b236` (B-1), `9c0bd44` (W-1).

## Iter 3 (round 2 — fix de UX pos-feedback do usuario)

Loop reaberto (`total_resets: 1`) apos o usuario testar o app pos-convergencia (iter 2,
APPROVED_WITH_WARNINGS) e reprovar a experiencia visual do blob vs os mockups `design/v0.2.0`:
"O balao some depois de traduzido, ao selecionar esta deixando o selecionado opaco, a bolha na
selecao nao esta parecendo uma bolha de vidro e esta separando as linhas." Causa-raiz de cada
defeito ja diagnosticada pelo orquestrador antes desta iteracao (confirmada na fonte, nao
re-descoberta). Escopo 100% JS — nenhum `.cs` tocado.

### Causa -> Fix

1. **Blob some apos traducao.** So `setSnippetLoading` e a selecao criavam um blob;
   `applySnippetTranslation`/`restoreSnippets` -> `_replaceRangeWithSnip` -> `_buildSnipSpan`
   criavam o snip SEM blob. Fix: `_renderAllBlobs()` agora varre TRES fontes de blob a cada
   mutacao (`sel:`, `load:`, `snip:`), entao um snip pronto sempre ganha um blob permanente.
2. **Selecao deixa o texto opaco (vidro por cima do texto).** `_ensureSelBlob` fazia
   `p.appendChild(mask); p.appendChild(svg)` — blob entrava DEPOIS dos spans no DOM, entao o
   blur pintava por cima do texto. Fix: todo blob agora e criado via
   `owner.prepend(svg); owner.prepend(mask)`, sempre como os PRIMEIROS filhos do paragrafo — o
   texto pinta por cima do vidro, nao o contrario. Vale para selecao, loading e snip por igual
   (o bug tambem existia latente na selecao, so nao tinha sido nomeado pelo usuario ainda).
3. **Nao parece vidro / separa as linhas.** `_blobPath` antigo emitia um rounded-rect
   INDEPENDENTE por banda (linha), concatenado — visual de capsulas empilhadas com aresta na
   divisa entre linhas. Fix: `_blobPath` reescrito para tracar UM UNICO contorno (topo da
   primeira banda, desce a borda DIREITA de todas as bandas com juncoes em S, atravessa a base
   da ultima, sobe a borda ESQUERDA, fecha com Z) — algoritmo literal fornecido pelo
   orquestrador, adaptado ao estilo do arquivo mantendo a assinatura `_blobPath(bands, r)` e o
   ajuste de ponto-medio existente. Os 2 testes dourados de banda unica (linha simples, clamp de
   raio) saem BYTE-IDENTICOS ao algoritmo antigo (confirmado rodando a funcao em Node antes de
   editar — uma banda so reduz ao mesmo rounded-rect nos dois algoritmos); o teste de juncao no
   ponto medio (2 bandas) mantem as mesmas asserções de substring (contem `32.0`, nao contem
   `30.0`/`34.0`), tambem confirmado.
4. **Bonus (achado no diagnostico): blob do placeholder de loading deslocado** quando o trecho
   nao comecava no inicio do paragrafo — `setSnippetLoading` aninhava o blob DENTRO do proprio
   span (`position: relative` no span), mas a geometria de `_blobFromEls` e relativa ao
   PARAGRAFO. Fix: resolvido de tabela pela arquitetura nova — o blob nunca mais fica aninhado
   em nenhum span, sempre filho direto do paragrafo, coordenadas e container agora concordam.

### Arquitetura nova: registro unico + sweep declarativo

Substitui `_selBlob` (referencia unica, so selecao) e o blob-dentro-do-span do loading por um
Map `_blobs` (chave -> `{mask, svg, path}` vivos no DOM), varrido por `_renderAllBlobs()`:

- Chaves: `'sel:' + pi + ':' + runStart` (UM blob POR RUN CONTIGUO da selecao, via `_runsOf` —
  nao um blob para a uniao; corrige um bug latente onde uma selecao nao-contigua tipo `{0, 2}`
  desenhava uma banda atravessando o periodo 1, nao selecionado), `'load:' + snipKey` (a chave
  completa `chapterHRef:pi:a:b`, armazenada em `dataset.loadKey` — atributo NOVO, deliberadamente
  diferente de `data-snip` pra nao colidir com o parsing de `_unwrapParagraph`/`_onSnipClick`),
  `'snip:' + snipKey` (reusa `dataset.snip` que o snip ja carregava).
- `_blobDescriptors()`: coleta o estado DESEJADO a partir do DOM (runs de `_sel` via `_runsOf`,
  todo `.tr-loading`, todo `[data-snip]`). `_renderAllBlobs()`: cria o que falta (prepend
  mask+svg no paragrafo dono), remede tudo que existe (`_updateBlob` + `_blobFromEls`), remove
  do DOM e do Map tudo que nao esta mais na lista desejada.
- Kind -> animacao: `sel`/`snip` mantêm `.tr-blob` (`trGlassIn`, inalterado); `load` ganha a
  classe extra `.tr-blob-pulse` (CSS movido de `.tr-loading .tr-blob` para `.tr-blob-pulse`,
  porque o blob nao e mais filho do span `.tr-loading`).
- Chamado ao final de: `_renderSelection`, `applySnippetTranslation`, `restoreSnippets`,
  `setSnippetLoading`, `clearSnippetLoading`, `_renderSnipSpan` (toggle remede geometria),
  `_onSnipRemoveClick`, `_onResize` (agora SEMPRE, nao so com `_sel` ativo — loading/snip tambem
  precisam remedir no resize), `mountSnippetLayer`. `unmountSnippetLayer` limpa tudo
  (`_blobs.clear()` + remove os nos do DOM).
- `_blobPath` ganhou guarda `bands.length === 0 -> return ''` — necessaria porque
  `_renderAllBlobs` agora chama `_blobFromEls` bem mais vezes (todo restore/apply/toggle), e um
  elemento sem layout real (comum em teste, possivel em producao) gerava zero bandas; o algoritmo
  antigo tolerava isso via `.map().join()` vazio, o novo precisava do guard explicito.
- Codigo morto removido sem cadaver comentado: `_selBlob`, `_ensureSelBlob`, `_removeSelBlob`,
  a regra CSS `.tr-loading .tr-blob`.

### Invariantes do DoD re-conferidos (nao regrediram)

`_blobPath(bands, 10)` literal no call site, `OFF=8`/`padX=5`/`padY=1.5` intactos,
`_splitSentences`/`_snippetRoots` como fonte unica (contagem 1x cada) — todos re-grepados apos o
diff. Os 4 testes de geometria dourada mantêm os NOMES EXATOS exigidos; os valores dourados dos
testes de banda unica (linha simples, clamp de raio) permaneceram os MESMOS literais (confirmado
programaticamente antes de editar, nao apenas por analise); pelo menos um path dourado literal
segue comparado caractere a caractere (grep do DoD 4 `'M [0-9]+\.[0-9] .*Q .*Z'` continua
casando). `translation.js`/`paginated.js`/`scroll.js`: diff VAZIO vs BASELINE `02a4c6c`
re-confirmado. Aspas duplas em todo `querySelectorAll` de `snippets.js` (nenhuma nova ocorrencia
com aspas simples). `_translatableCandidates` continua a unica fonte de blocos. Zero string pt-BR
nova no JS.

### Testes novos (12, todos em `test/js/snippets.test.js`)

Z-order (blob antes do primeiro `[data-si]` no DOM do paragrafo); snip pronto tem blob
(`applySnippetTranslation` e `restoreSnippets`, cada um com teste dedicado, `clip-path` != `path('')`
apos setar um rect real); toggle remede o blob quando a geometria muda; selecao nao-contigua
`{0, 2}` gera DOIS blobs `sel:0:0`/`sel:0:2`, nenhum cobrindo o periodo 1; sweep remove orfaos em 2
cenarios (`clearSnippetSelection` e remocao de snip via chip); loading pulsa (classe
`tr-blob-pulse` presente) e `clearSnippetLoading` remove o blob do registro e do DOM; resize
remede mesmo sem `_sel` ativo (cenario so com loading); novo `_blobPath` com 2 bandas emite
exatamente UM `M` e UM `Z` (contorno unico); `_blobPath([], 10)` nao lanca, devolve `''`.

### Verificacao pos-fix

- JS: **142/142 passando** (era 130 antes desta iter), 0 skipped, 0 fail. `comm -23` contra a
  suite anterior desta mesma sessao: nenhum teste perdido (so adicoes).
- `bash scripts/coverage-gate.sh`: exit 0. `COVERAGE_JS covered=1331 valid=1346 pct=98.89
  files=5` (piso 85). `COVERAGE_SCOPE covered=1313 valid=1385 pct=94.80 files=26` (piso 90,
  **identico** ao iter 2 — confirma que nenhum `.cs` foi tocado). `COVERAGE_GUARD new_app_cs=0
  waived=0`.
- `dotnet test` (Release, prova de nao-regressao): **404 passed / 2 skipped (GPU-only
  pre-existentes) / 0 failed / 406 total** — identico ao iter 2, como esperado (mudanca 100% JS).
- `dotnet format whitespace --verify-no-changes`: exit 2 a nivel de SOLUCAO, mas pelas MESMAS 2
  violacoes FINALNEWLINE legadas ja registradas em W-7 (`Platforms/Android/MainActivity.cs`,
  `MainApplication.cs`) — arquivos INTOCADOS por esta iteracao (nenhum `.cs` no diff). Nao
  reincide na imprecisao apontada no iter 2 (aquele SUMMARY dizia "exit 0, limpo" a nivel de
  solucao): aqui o estado exato e reportado — os 2 arquivos tocados nesta iter (`snippets.js`,
  `snippets.test.js`) sao JS, fora do escopo do `dotnet format`.
- DoD 7 (fronteira) re-verificado: diff vazio de `translation.js`/`paginated.js`/`scroll.js`
  contra `BASELINE`; zero `querySelectorAll('...')` com aspas simples contendo p/h1/li/div; zero
  `window.(applyTranslations|clearTranslations|getVisibleParagraphs) =`.

Commit: `4d6d0a8` — `fix(snippet-translation): glass blob stays under text, persists on snips,
draws as one contour` (1 commit atomico, implementacao + testes juntos, mesmo padrao dos fixes
B-1/W-1 do iter 2).

## Iter 4 (ralph loop, pos-REVIEW.md BLOCKED — fix de B-2)

Reviewer (iter 3, revisao `48cae53`) bloqueou por B-2: `_unwrapParagraph` (usada por
`unmountSnippetLayer`) lia `node.className.indexOf('tr-blob')` para pular os nos do blob durante o
desembrulho do paragrafo. Isso funciona no mask (`<span>`, `className` string), mas o `<svg>` irmao
(criado via `_svgEl`/`createElementNS`) tem `className` como `SVGAnimatedString` em qualquer WebView
real (WebView2/Chromium, Android System WebView, WKWebView) — objeto truthy SEM `.indexOf` ->
TypeError no primeiro paragrafo com qualquer blob. Como a iter 3 tornou todo snip permanentemente
dono de um blob (o proprio objetivo do fix de UX daquela iteracao), o defeito passou a ser
alcancavel no caminho principal: traduzir um trecho -> ativar a traducao por paragrafo ->
`unmountSnippetLayer()` quebra em silencio (`EvaluateJavaScriptAsync` engole excecao JS), `_blobs`
nao e limpo, `_mounted` fica `true`, e o remonte seguinte pula paragrafos ainda com `data-pi` —
camada de snippets morta ate a proxima reinjecao de capitulo. Os 142 testes nao pegavam isso porque
`test/js/harness.js` nao tinha `createElementNS`: `_svgEl` caia no fallback `createElement`, cujo
`className` e string — o teste exercitava um tipo de elemento diferente do de producao.

### Fix B-2 (`a4aa004`)

Duas partes, como exigido pela revisao:

1. **Reordenacao em `unmountSnippetLayer`**: a limpeza dos blobs (varrer `_blobs`, `.remove()`
   mask+svg, `_blobs.clear()`) agora roda ANTES do loop de desembrulho dos paragrafos (era depois).
   Isso significa que `_unwrapParagraph` nunca mais encontra um no de blob no unico call site que
   existe hoje — a causa raiz fica inalcancavel na pratica.
2. **Checagem de classe string-safe** (cinto e suspensorio, exigido mesmo com o item 1 resolvendo o
   alcance): novo helper `_hasClass(node, cls)` — usa `node.className` diretamente quando e string,
   cai para `node.getAttribute('class')` quando nao e (SVG real ou o novo `FakeSvgElement` do
   harness). Preserva a semantica de substring que o codigo ja usava
   (`'tr-blob-svg'.indexOf('tr-blob') === 0` cobre mask, variante com pulse e svg com UMA checagem).
   Auditados TODOS os `className.indexOf` do arquivo (pedido explicito da revisao):
   `_updateSentClasses` e `_loadingSpanAt` tambem migraram para `_hasClass` por
   uniformidade/robustez futura, apesar de ambos serem PROVADAMENTE inalcancaveis por nos SVG hoje
   (operam so sobre spans `[data-si]`, que svg nunca carrega) — julgamento registrado em comentario
   no proprio codigo. `_svgEl` deixou de ter o fallback
   `document.createElementNS ? ... : document.createElement(...)` (existia so pra contornar a
   limitacao antiga do harness) e passou a chamar `createElementNS` sempre — API padrao suportada
   por WebView2/Android WebView/WKWebView ha muito tempo.

**Fechamento do ponto cego do harness** (`test/js/harness.js`): `FakeDocument.createElementNS(ns,
tag)` novo — devolve um `FakeSvgElement` (extends `FakeElement`) quando `ns` e o namespace SVG. O
`FakeSvgElement` inicializa `className` como um OBJETO (`{baseVal, animVal}`, nunca reescrito para
string mesmo apos `setAttribute('class', ...)`, via um hook `_setClassName` que a subclasse
sobrescreve) — reproduz de proposito o mesmo perigo de `SVGAnimatedString`. `getAttribute('class')`
continua devolvendo string normal em ambos os casos (mask e svg), exatamente como um DOM real.
Efeito colateral necessario: `matches()` (usada por `querySelectorAll`/`closest`) fazia
`element.className.split(' ')` sem guarda — com um `FakeSvgElement` em QUALQUER lugar da arvore,
isso quebraria toda checagem de selector no documento inteiro (nao so as que pedem uma classe SVG).
Extraido `classTokensOf(element)` (le a `class` refletida quando `className` nao e string) e usado
dentro de `matches()` — comportamento identico ao anterior para todo elemento HTML comum (`className`
ja e string, mesmo caminho), muda so para o `FakeSvgElement` novo.

### Testes novos (6)

- `test/js/harness.test.js` (+4): `createElementNS` no namespace SVG devolve elemento com
  `className` que nao e string; `getAttribute('class')` continua funcionando apos `setAttribute`;
  `createElementNS` fora do namespace SVG se comporta como `createElement`; `querySelectorAll` casa
  um elemento SVG por classe mesmo com `className` nao-string (prova o fix de `classTokensOf`).
- `test/js/snippets.test.js` (+2): `_unwrapParagraph` chamado diretamente sobre um paragrafo com um
  blob "orfao" ainda anexado (bypassa a reordenacao de proposito) nao lanca e descarta o blob —
  prova a Parte 2 isoladamente; `unmountSnippetLayer()` com um snip traduzido E uma selecao ativa
  simultaneos (o cenario exato da B-2: 2 blobs no paragrafo) completa sem lancar, limpa
  `[data-pi]`/`[data-si]`/`[data-snip]`/`.tr-blob`/`.tr-blob-svg`/`_blobs` por completo, e um
  `mountSnippetLayer()` seguinte re-envolve o paragrafo — a espiral da morte da B-2, agora coberta
  fim a fim.
- Teste de z-order existente (iter 3) ajustado: comparava `node.className === 'tr-blob-svg'`
  diretamente — passa a comparar via `getAttribute('class')`, ja que o svg real deixou de ter
  `className` string (o proprio comportamento que este fix introduz de proposito no harness).

**Nao alterado**: W-2..W-12 seguem fora de escopo desta iteracao (instrucao explicita — so B-2).

### Verificacao pos-fix

- JS: **148/148 passando** (142 + 6 novos), 0 fail, 0 skipped. `comm -23` contra a suite anterior a
  esta iter: **vazio** (zero teste perdido, so 6 adicoes, nomeadas acima).
- Golden geometry (DoD 4): os 4 testes de nome exato continuam passando, corpo do teste
  byte-identico (`git diff` confirma zero mudanca no bloco `blob geometry:`).
- Invariantes congelados re-confirmados: diff vazio de `translation.js`/`paginated.js`/`scroll.js`
  vs `BASELINE` (`02a4c6c`); zero `querySelectorAll('...')` com aspas simples contendo p/h1/li/div;
  `_blobPath(bands, 10)` literal + `OFF=8`/`padX=5`/`padY=1.5` intactos; `_splitSentences` com fonte
  e regex unicas (1x cada).
- `bash scripts/coverage-gate.sh`: exit 0. `COVERAGE_SCOPE covered=1313 valid=1385 pct=94.80
  files=26` (piso 90, **identico** ao iter 3 — confirma que nenhum `.cs` foi tocado).
  `COVERAGE_JS covered=1352 valid=1362 pct=99.27 files=5` (piso 85, subiu de 98.89% — os testes
  novos exercitam `_hasClass`/`_svgEl` que antes tinham cobertura parcial). `COVERAGE_GUARD
  new_app_cs=0 waived=0`.
- `dotnet test` (Release, prova de nao-regressao — iteracao 100% JS): **404 passed / 2 skipped
  (GPU-only pre-existentes) / 0 failed / 406 total** — identico ao iter 3.
- `dotnet format whitespace --verify-no-changes`: exit 2, mas pelas MESMAS 2 violacoes FINALNEWLINE
  legadas de sempre (`Platforms/Android/MainActivity.cs`, `MainApplication.cs`, W-7) — fora do diff
  desta iteracao (nenhum `.cs` tocado).
- `git status`/`git diff --name-only` confirmam escopo: so `snippets.js`, `harness.js`,
  `harness.test.js`, `snippets.test.js` (mais os arquivos de estado do `.jdi/` do orquestrador do
  loop, nao tocados por este specialist).

Commit: `a4aa004` — `fix(snippet-translation): unmount never hands a blob node to the unwrap loop,
and the class check is SVG-safe (B-2)` (1 commit atomico — producao + harness + testes juntos,
porque o fix de producao sozinho quebraria a suite existente sem o `createElementNS` do harness no
MESMO commit: seriam commits intermediarios com suite vermelha se divididos).

## Iter 5 (round 3 — fix de UX pos-screenshot do app real, Windows)

Loop reaberto (`total_resets: 2`) apos o usuario testar o app real pos-convergencia (round 2,
APPROVED_WITH_WARNINGS) e enviar screenshot: com 1 periodo selecionado, a pill virava um balao
disforme de ~6 linhas (a dica "· toque em outro período para estender" quebrava palavra por
palavra, "Traduzir trecho" quebrava em 2 linhas) e a pill/hint renderizavam com a fonte do livro
(monospace no screenshot), nao Inter. Causas-raiz ja diagnosticadas pelo orquestrador antes desta
iteracao (confirmadas na fonte, nao re-descobertas). Escopo 100% JS + doc — nenhum `.cs` tocado.

### Causa -> Fix

1. **Falta `white-space: nowrap`.** No `_SNIPPET_CSS` so `.tr-pill-count` tinha; `.tr-pill-tip`,
   `.tr-pill-only`, `.tr-pill-primary` e `.tr-hint` nao. Fix: nowrap adicionado nos 4 seletores.
2. **Shrink-to-fit de 50vw.** `.tr-pill`/`.tr-hint` usam `position: fixed; left: 50%;
   transform: translateX(-50%)` sem `width` — a largura disponivel para o calculo shrink-to-fit e
   `100vw − left` = 50vw do viewport REAL do WebView, nao da moldura de 1280px do mockup;
   `data-idiom="desktop"` no Windows nao implica janela larga. Fix: `max-width:
   calc(100vw - 24px)` como cinto de seguranca nos dois, MAIS degradacao por medicao (item novo,
   pedido pelo usuario — "mostrando ou nao de acordo com o tamanho da tela"): `_fitPill(pill)` mede
   `pill.scrollWidth` contra `document.documentElement.clientWidth - 24` depois da pill entrar no
   DOM e degrada NESTA ORDEM ate caber, re-medindo a cada passo: (1) remove a dica/`onlySentence`
   — o layout phone ja vive sem os dois por design; (2) remove o SPAN de texto do botao primario,
   deixando so o icone `ph-translate` (com `title`/`aria-label` = `_labels.translateSnip` para
   acessibilidade). Nunca permite quebra interna de linha. `_renderHint` usa a MESMA medicao mas
   sem fallback parcial — o hint e dispensavel, entao some por inteiro se nao couber.
   `_onResize` passou a reconstruir a selecao inteira (`_renderSelection` -> `_showPill` ->
   `_fitPill`) quando ha `_sel` ativo, em vez de so re-medir blobs, para que uma pill ja degradada
   se re-ajuste a um novo tamanho de janela.
3. **Fonte da pill/hint/chip.** `font-family: 'Inter', var(--font-body)` (pill/hint) e
   `font-family: var(--font-body)` (chip) eram INVALIDOS em silencio: `--font-body` e um token do
   design system do MOCKUP que nunca foi copiado para o `wwwroot` do app, entao a propriedade
   inteira falha no computed-value time (uma `var()` nao resolvida sem fallback invalida a
   declaracao TODA, nao so o token que falta) e o valor USADO vira o HERDADO — a regra
   `body { font-family: ...!important }` que `ThemeEngine.cs` gera para a fonte do livro
   (confirmado lendo o arquivo; `ThemeEngine.cs` ficou INTOCADO, conforme instrucao). Fix: todo
   `var(--font-body)` trocado por `'Inter', sans-serif !important` nos tres componentes (pill,
   hint, chip — chip manteve `font-size: 0.6em` herdado do paragrafo). O `!important` responde ao
   `!important` do proprio `ThemeEngine` no `body` (documentado em 1 linha WHY no codigo).

### Testes novos (8)

`test/js/snippets.test.js` (+7): 2 testes de CSS (nowrap presente nos 4 seletores; `_SNIPPET_CSS`
sem `var(--font-body)` e com `'Inter', sans-serif`); 3 de degradacao da pill via harness com
`document.documentElement.clientWidth` e `pill.scrollWidth` (getter) simulados — nada removido com
espaco sobrando, so a dica remove quando isso ja basta, dica E texto do botao removem quando ainda
nao basta (com `aria-label`/`title` verificados); 1 do hint (some por inteiro com viewport
zero-largura); 1 de resize com selecao ativa reconstruindo o elemento da pill (nao so remedindo).
`test/js/harness.test.js` (+1): default de `document.documentElement.clientWidth` em `createEnv`
(mesmo valor de `window.innerWidth`), necessario para que os testes de pill/hint PRE-EXISTENTES
continuem no-op para a nova medicao (scrollWidth default 0 sempre cabe em 800-24).

### Invariantes do DoD re-conferidos (nao regrediram)

`translation.js`/`paginated.js`/`scroll.js`: diff VAZIO vs `BASELINE` (`02a4c6c`). Aspas duplas em
todo `querySelectorAll`/`querySelector` novo. `_blobPath(bands, 10)` literal, `OFF=8`/`padX=5`/
`padY=1.5` intactos. `_splitSentences`/`_snippetRoots` como fonte unica (1x cada). Zero string
pt-BR nova em `snippets.js`. Os 14 literais visuais do DoD 10 (blur/border-radius/rgba/keyframes/
icones) permanecem no arquivo E na PIXEL-SPEC. Os 14 literais do DoD 1 permanecem na PIXEL-SPEC.

### Verificacao pos-fix

- JS: **156/156 passando** (era 148 antes desta iter — a suite completa `test/js/` inclui tambem
  `bridge`/`paginated`/`scroll`/`translation`, nao so `snippets`), 0 fail, 0 skipped. `comm -23`
  contra a suite anterior a esta iter (nome a nome, 6 arquivos): **vazio** — zero teste perdido, so
  as 8 adicoes listadas acima.
- `bash scripts/coverage-gate.sh`: exit 0. `COVERAGE_JS covered=1397 valid=1407 pct=99.29 files=5`
  (piso 85, subiu de 99.27). `COVERAGE_SCOPE covered=1313 valid=1385 pct=94.80 files=26` (piso 90,
  **identico** ao iter 4 — confirma que nenhum `.cs` foi tocado). `COVERAGE_GUARD new_app_cs=0
  waived=0`.
- `dotnet test` (Release, prova de nao-regressao — iteracao 100% JS+doc): **404 passed / 2 skipped
  (GPU-only pre-existentes) / 0 failed / 406 total** — identico ao iter 4.
- `dotnet format whitespace --verify-no-changes`: exit 2, pelas MESMAS 2 violacoes FINALNEWLINE
  legadas de sempre (`Platforms/Android/MainActivity.cs`, `MainApplication.cs`, W-7) — fora do diff
  desta iteracao (nenhum `.cs` tocado).
- `git status`/`git diff --name-only` confirmam escopo: so `snippets.js`, `harness.js`,
  `harness.test.js`, `snippets.test.js`, `design/v0.2.0/PIXEL-SPEC.md` (mais os arquivos de estado
  do `.jdi/` do orquestrador do loop, nao tocados por este specialist).

Commit: `b3005e1` — `fix(snippet-translation): pill/hint fit the real viewport and no longer
render in the book's font` (1 commit atomico — CSS + JS de medicao/degradacao + harness + testes +
sub-nota da PIXEL-SPEC juntos, coeso com o proprio fix).

## Iter 5 parte 2 (round 3 — fix de poluicao de contexto na traducao de trechos, pos-screenshot do app real)

Mesmo iter/round 3 do fix da pill (`b3005e1`); o reviewer roda depois cobrindo as duas partes.
Usuario reportou, com screenshot do app real: paragrafo com 3 periodos, traduziu o 1o e o 3o
corretamente (viraram snips), mas ao traduzir o 2o a traducao devolvida foi o PARAGRAFO INTEIRO —
repetindo o conteudo dos periodos 1 e 3 e com os literais "PT-BR" aparecendo DENTRO do texto
traduzido. Causa-raiz ja diagnosticada pelo orquestrador antes desta iteracao (confirmada na fonte,
nao re-descoberta).

### Causa -> Fix

`_onTranslateClick` (final de `snippets.js`) montava `paragraph = _sel.p.textContent`. Quando o
paragrafo ja continha snips, `textContent` nao e o paragrafo original: e a mistura do que esta
EXIBIDO — traducao PT dos snips (ou original, conforme o toggle de cada um) + os ROTULOS DOS CHIPS
("EN"/"PT-BR", spans de texto dentro do snip). Esse contexto poluido ia no campo `paragraph` do
payload `snip|`, `PromptUtility.BuildSnippetTranslationMessages` injetava no prompt como contexto, e
o modelo pequeno respondia traduzindo o "paragrafo" inteiro. O campo `text` (via `_rangeText`, so
periodos `[data-si]` do range) nunca foi afetado — o problema era exclusivamente o `paragraph`.

Fix em duas camadas, ambas obrigatorias por instrucao:

**A) JS.** Nova funcao pura `_originalParagraphText(p)`: percorre `p.childNodes` na ordem do
documento e reconstroi o texto ORIGINAL independente do que esta na tela — um snip contribui com
`dataset.orig` (nunca o texto exibido nem o chip, ambos dentro do mesmo span); um periodo ou
placeholder de loading contribui com o proprio texto (`childNodes[0].textContent`, ja que ambos so
tem um filho de texto); um blob de vidro (agora filho direto do paragrafo) e decoracao e e pulado
via `_hasClass(node, 'tr-blob')`; um text node (o espaco separador entre spans) e usado como esta —
a travessia em ordem preserva o espacamento correto mesmo quando um snip substitui varios periodos.
`_onTranslateClick` passou a chamar `_originalParagraphText(_sel.p)` em vez de ler `textContent`.

**B) C#.** `PromptUtility.BuildSnippetTranslationMessages` endurecido para modelo pequeno: o trecho
e o paragrafo agora sao delimitados por aspas triplas (`"""..."""`), o trecho e repetido dentro da
propria mensagem de sistema (alem de continuar chegando, inalterado, como mensagem de usuario), e a
instrucao passou a exigir EXCLUSIVAMENTE a traducao direta do trecho delimitado — sem delimitadores,
rotulos, comentarios, nem nada do resto do paragrafo (D-2026-08-09-snippet-translation-5).

**C) Entradas poluidas ja persistidas — escolha: SALT (nao heuristica).** A traducao poluida do
usuario ficou gravada no `TranslationCache` sob a chave `hash(trecho, src, dst)` (SHA-256, sem
mudanca de prompt na chave); re-traduzir o mesmo trecho devolveria o lixo do cache mesmo apos o fix
do prompt. Optamos pelo salt (mais limpo e deterministico que a heuristica de comprimento, e barato
de aplicar): `TranslationManager` ganhou `private const string SnippetCacheKeySalt =
"snippet-prompt-v2|"`, prefixado ao `text` SO na chamada de `ComputeHash` dentro de
`TranslateSnippetAsync` — a chave de cache do caminho de PARAGRAFO (`TranslateChapterAsync` /
`TranslateParagraphsAsync` / `TranslateSingleChapterAsync` / `FetchTranslationsFromCacheAsync`)
continua sem salt, entao nenhuma entrada de traducao por paragrafo e invalidada. Isso torna toda
entrada de cache de SNIPPET pre-existente inalcancavel (nova chave, nunca bate com a antiga) — a
proxima traducao do mesmo trecho gera uma chamada real ao engine com o prompt ja endurecido, sem
exigir wipe manual de banco. `ComputeSnippetHash` (o hash FNV-1a salvo em
`SnippetTranslation.OriginalHash`, reproduzido pelo `_snipHash` do JS para validar restore) foi
DELIBERADAMENTE deixado intacto: e um mecanismo diferente, para um proposito diferente (checar se a
ancora ainda bate com o texto na posicao, nao decidir se o cache de inferencia e reaproveitavel);
salga-lo nao invalidaria a entrada poluida do `TranslationCache` e quebraria a paridade de hash
JS/C# de que o restore depende.

### Testes novos (4)

JS (`test/js/snippets.test.js`, +2): `_originalParagraphText` testado diretamente contra um
paragrafo com um snip mostrando traducao + um snip mostrando original + um periodo comum — devolve
o texto original exato, sem rotulo de chip, sem texto traduzido; payload de `_onTranslateClick`
testado fim a fim com um snip ja traduzido presente no paragrafo (o cenario exato reportado) —
`paragraph` no JSON enviado por `sendRawMessage` bate com o original limpo, `text` continua correto.
O caso sem snips (teste pre-existente `translate: clicking the primary button sends a snip| message
with the selected run`) serve de regressao — passou inalterado, provando que o fix nao muda o
comportamento quando nao ha poluicao possivel.

C# (`test/TranslateReader.Tests`, +2): `PromptUtilityTests` ganhou
`BuildSnippetTranslationMessages_SystemMessageDemandsOnlyTheExcerptAndDelimitsItFromTheParagraph`
(prova "EXCLUSIVELY" na instrucao e os dois blocos delimitados por aspas triplas); o teste antigo
`BuildSnippetTranslationMessages_SystemMessageContainsTheParagraphAsContext` perdeu a asserção de
substring redundante ("only the translation of the excerpt", texto que nao existe mais) e manteve
seu escopo original (paragrafo aparece como contexto) — nome preservado, sem perda de teste.
`SnippetTranslationManagerTests` ganhou
`TranslateSnippetAsync_CacheKeyIsSaltedAwayFromTheLegacyParagraphHash`, que reproduz a formula
antiga (sem salt) localmente no teste e prova, via `Arg.Is<string>`, que a chave usada tanto no
`FetchTranslationAsync` quanto no `SaveTranslationAsync` do caminho de snippet diverge dela.

### Verificacao pos-fix

- JS: **158/158 passando** (era 156 antes desta parte — os 2 testes novos), 0 fail, 0 skipped.
  `comm -23` nome a nome contra a suite anterior a esta iteracao: vazio.
- C#: build Windows Release `0 Warning(s), 0 Error(s)`. `dotnet test`: **406 passed / 2 skipped
  (GPU-only pre-existentes) / 0 failed / 408 total** — +2 vs antes desta parte (os 2 testes novos).
- `bash scripts/coverage-gate.sh`: exit 0. `COVERAGE_SCOPE covered=1316 valid=1388 pct=94.81
  files=26` (piso 90 — agora COM `.cs` tocado nesta iteracao, ao contrario das partes anteriores do
  iter 5). `COVERAGE_JS covered=1422 valid=1432 pct=99.30 files=5` (piso 85). `COVERAGE_GUARD
  new_app_cs=0 waived=0`. Zero `COVERAGE_WAIVER_INVALID`.
- `dotnet format whitespace --verify-no-changes`: exit 2, mas pelas MESMAS 2 violacoes FINALNEWLINE
  legadas de sempre (`Platforms/Android/MainActivity.cs`, `MainApplication.cs`, W-7) — fora do diff
  desta parte; os arquivos `.cs` REALMENTE tocados (`TranslationManager.cs`, `PromptUtility.cs`,
  `PromptUtilityTests.cs`, `SnippetTranslationManagerTests.cs`) nao aparecem no log.
- Invariantes re-conferidos: diff vazio de `translation.js`/`paginated.js`/`scroll.js` vs
  `BASELINE` (`02a4c6c`); zero `querySelectorAll('...')` com aspas simples contendo p/h1/li/div em
  `snippets.js`; nenhum nome de teste perdido (JS e C#, `comm -23` vazio nos dois); golden geometry
  (`_blobPath`, `OFF`/`padX`/`padY`) intocado por este diff.
- `git status`/`git diff --name-only` confirmam escopo: `snippets.js`, `snippets.test.js`,
  `TranslationManager.cs`, `PromptUtility.cs`, `PromptUtilityTests.cs`,
  `SnippetTranslationManagerTests.cs` (mais os arquivos de estado do `.jdi/` do orquestrador do
  loop, nao tocados por este specialist).

Commit: `25ef3f3` — `fix(snippet-translation): snippet context no longer leaks sibling snips' shown
text and chip labels into the prompt` (1 commit atomico — JS + C# + testes juntos, pela mesma logica
dos fixes anteriores desta phase: as duas camadas sao partes complementares do mesmo defeito
reportado e um commit intermediario deixaria a poluicao de contexto sem o endurecimento do prompt
que a torna inofensiva, ou o cache salgado sem a fonte da poluicao corrigida).

## Iter 6 (fix round pos-loop, autorizado pelo usuario — 3o feedback com screenshots do app real)

Loop ja tinha convergido (`d947dad`, round 3 iter 1 APPROVED_WITH_WARNINGS) quando o usuario testou o
app real de novo e reportou DOIS defeitos novos com screenshots: **D-A** — o modelo devolvia o
paragrafo inteiro mesmo com o prompt endurecido do iter 5, E registros `SnippetTranslations`
envenenados de sessoes anteriores continuavam sendo restaurados (o salt do iter 5 so invalidou o
CACHE de inferencia, nao a tabela de persistencia); **D-B** — nenhum snip mostrava o vidro no app
real (so no harness Chrome), porque `SetupSnippetLayerAsync` media os blobs ANTES do
`Task.Delay(300)` + `GoToPageAsync`/`RestoreScrollPositionAsync` + carregamento assincrono das
fontes, cujo reflow subsequente deixava as coordenadas do clip-path mortas; agravado por paragrafo
fragmentado entre colunas do `_pager` (paginacao via CSS columns) gerando bandas absurdas.

Correcao prescrita em 4 partes, entregue em **2 commits atomicos por causa-raiz** (D-A = partes 1+2;
D-B = partes 3+4 — os arquivos tocados por cada causa sao quase inteiramente disjuntos, exceto
`snippets.js`, dividido por hunk entre os dois commits e verificado com `diff` contra o baseline e
contra o estado final antes de cada commit).

### D-A — guarda de proporcao + retry sem contexto + purga no restore

**Parte 1 (C#, `TranslationManager.TranslateSnippetAsync`):**
- `IsSnippetTranslationTooLong(text, translated)`: invalido se
  `translated.Length > text.Length * 3 + 120` (EN->PT raramente excede ~1.6x).
- Fluxo: cache hit invalido -> tratado como miss (sobrescreve ao salvar); inferencia 1 (prompt
  trecho+contexto, existente) -> invalida -> inferencia 2 SEM o paragrafo de contexto (overload novo
  `IPromptUtility.BuildSnippetTranslationMessages(snippet, sourceLanguage, targetLanguage, bookTitle,
  chapterTitle)`, 5 parametros, sem `paragraph`) -> ainda invalida -> `InvalidOperationException`
  propagada (fail fast, csharp.md S1) SEM persistir nada — nem `SaveSnippetAsync` nem
  `SaveTranslationAsync` sao chamados nesse caminho.
- `PromptUtility.BuildSnippetSystemMessage` ganhou `paragraph` nullable: o bloco de contexto so entra
  quando `paragraph` nao e nulo/vazio: o overload novo delega com `paragraph: null`.
- 3 testes novos em `SnippetTranslationManagerTests.cs` (cache podre sobrescrito; retry sem contexto
  acionado com `Received(1)` nas DUAS assinaturas do `BuildSnippetTranslationMessages`; falha total
  com `DidNotReceive()` em ambos os Save) + 4 em `PromptUtilityTests.cs` (overload sem paragrafo: sem
  bloco de contexto, ainda exige EXCLUSIVELY, inclui book title, contem o trecho).

**Parte 2 (JS, `restoreSnippets`):**
- `_isSnippetTranslationTooLong(originalText, translatedText)` — mesma formula, espelhando o C# (o
  JS nao tem digest assincrono para reusar o `ComputeSnippetHash`, entao a formula e reimplementada,
  nao chamada via RPC).
- `restoreSnippets`: apos a guarda de hash existente, roda a guarda de comprimento; reprovou -> NAO
  aplica o snip E `sendRawMessage('snip-remove|' + JSON com {chapterHRef, paragraphIndex,
  sentenceStart, sentenceEnd})` — reusa o handler `snip-remove` que ja existe no C#
  (`ReaderPage.HandleSnipRemoveAsync` -> `RemoveSnippetAsync`), entao a primeira reabertura do livro
  apos este fix limpa sozinha o estrago de sessoes anteriores. Hash divergente continua descarte
  silencioso SEM remove (comportamento intocado — pode ser paragrafo re-paginado).
- 5 testes novos em `snippets.test.js`: 2 unitarios de `_isSnippetTranslationTooLong`; poisoned ->
  nao aplica + `snip-remove|` com a ancora exata (`deepStrictEqual` no JSON); legitimo -> aplica E
  NAO manda `snip-remove`; hash divergente -> descarta SEM `snip-remove`.

### D-B — re-medicao confiavel + blob ciente de colunas

**Parte 3 (JS `mountSnippetLayer`/`unmountSnippetLayer` + C# `ReaderPage`):**
- `document.fonts.ready.then(_renderAllBlobs)` no mount, guardado com `if (document.fonts)` (fonte
  do livro + Inter carregam async; qualquer blob medido antes fica com coordenadas mortas).
- `ResizeObserver` reusado (nao recriado por mount) guardado com
  `typeof ResizeObserver !== 'undefined'`, observando cada paragrafo `[data-pi]` a cada mount;
  `disconnect()` + reobserva no INICIO de todo `mountSnippetLayer()` (nao so no primeiro) para nunca
  acumular observacoes de paragrafos de um capitulo anterior ja destacados do DOM; tambem
  desconectado em `unmountSnippetLayer()`. Callback coalescido via `_scheduleBlobRefresh`
  (`requestAnimationFrame`, fallback `setTimeout` quando ausente — e o caso do harness).
- `window.refreshSnippetBlobs = _renderAllBlobs` exposto; `ReaderPage` chama
  `refreshSnippetBlobs()` apos `GoToPageAsync`/`GoToLastPageAsync`/`NextPageAsync`/`PrevPageAsync`/
  `RestoreScrollPositionAsync` — cinto extra barato (sweep sem nada para fazer e um no-op).
  `translation.js`/`paginated.js`/`scroll.js` permanecem INTOCADOS (diff vazio vs BASELINE).
- `test/js/harness.js` ganhou stubs OPT-IN (`{ resizeObserver: true }`, `{ fonts: { ready } }`),
  ausentes por padrao — a maioria dos testes ja existentes continua exercitando de graca o caminho
  "host sem suporte" (guardas `if (document.fonts)` / `typeof ResizeObserver !== 'undefined'`).
- 9 testes novos: 4 em `harness.test.js` (default ausente dos dois; stub de `ResizeObserver` grava
  observe/unobserve/disconnect/callback; `document.fonts.ready` e um thenable); 5 em
  `snippets.test.js` (`refreshSnippetBlobs === _renderAllBlobs`; mount observa cada paragrafo;
  unmount desconecta; callback do observer re-mede via o timer de fallback — asserta o DELTA de
  timers pendentes, nao um total absoluto, porque o proprio `_sendReady` do `bridge.js` ja mantem 1
  timer pendente neste harness sem host; `document.fonts.ready` resolvendo re-mede um blob, teste
  `async` que se apoia na ordem FIFO de `.then()` no mesmo `Promise`); +1 prova que mount nao lanca
  sem nenhum dos dois suportado. `HybridWebViewContractTests.cs` ganhou
  `SnippetsJs_ExposesRefreshSnippetBlobs`.

**Parte 4 (JS `_blobFromEls`, blob ciente de colunas):**
- Causa raiz real: `_blobFromEls` ordenava `points` globalmente por `y1,x1` ANTES de agrupar em
  linhas — isso escondia o "salto para tras" que sinaliza wrap de coluna (a cauda da coluna N, em
  y grande, e a cabeca da coluna N+1, em y pequeno, ficavam intercaladas pela ordenacao em vez de
  preservar a ordem de leitura natural que `getClientRects()` ja devolve). Fix: **removida a
  ordenacao**; `points`/`lines` agora processados na ordem NATURAL de chegada.
- `_columnGroupsOf(lines)` nova: particiona as linhas (ja agrupadas por proximidade de `cy`, como
  antes) em grupos de coluna — novo grupo quando a linha N+1 tem `top` MENOR que o `top` da linha N
  (1 comentario WHY, criterio de 1 linha).
- `_blobFromEls` gera bandas POR GRUPO (variavel local `bands`, preservando o literal
  `_blobPath(bands, 10)` exigido pelo DoD 4) e concatena os `d` de cada grupo com espaco
  (`M...Z M...Z`, subpaths validos em `path()`/SVG).
- Goldens single-column (`_blobPath` chamado direto com bands literais) INTOCADOS — nao passam por
  `_blobFromEls`, byte-identicos por construcao; confirmado com `git diff` vazio no bloco
  `blob geometry:` dos testes existentes.
- 2 testes novos: 2 colunas simuladas (cauda em y=560-590, cabeca em y=16-46, x bem distantes) ->
  exatamente 2 `M` e 2 `Z` no `d` (nenhuma banda atravessando o vao); 2 linhas na MESMA coluna ainda
  tracam 1 `M`/1 `Z` (regressao de seguranca — o contorno unico do iter 3 continua funcionando
  quando NAO ha wrap de coluna).

### Verificacao pos-fix (repetida em cada um dos 2 commits antes de commitar, nao so no HEAD final)

- JS: **175/175 passando** (era 158 antes deste iter — 5 (D-A) + 12 (D-B) = 17 novos), 0 fail, 0
  skipped. `comm -23` nome a nome (sort + comm) contra a suite de `d947dad`, nos 2 arquivos tocados
  (`snippets.test.js`, `harness.test.js`): **vazio** nos dois — zero teste perdido, so adicoes.
  Estado intermediario (so D-A aplicado, D-B revertido para o baseline em memoria): **167/167**
  passando antes do 1o commit.
- C#: build Windows Release `0 Warning(s), 0 Error(s)`. `dotnet test`: **414 passed / 2 skipped
  (GPU-only pre-existentes) / 0 failed / 416 total** — +8 vs os 406 de `d947dad` (3+4 em
  `SnippetTranslationManagerTests`/`PromptUtilityTests` do D-A, 1 em `HybridWebViewContractTests` do
  D-B). `comm -23` nome a nome (public async Task/void) contra `d947dad`: vazio, +8 adicoes.
  Estado intermediario (so D-A, com `HybridWebViewContractTests.cs` temporariamente resetado ao
  baseline para nao acusar `SnippetsJs_ExposesRefreshSnippetBlobs` ausente): **413 passed / 2
  skipped / 0 failed / 415 total**, verificado ANTES do 1o commit.
- `bash scripts/coverage-gate.sh`: exit 0 nos DOIS commits. Final: `COVERAGE_SCOPE covered=1340
  valid=1411 pct=94.97 files=26` (piso 90 — `TranslationManager.cs` 257/257=100%,
  `PromptUtility.cs` 39/40=97.5%, ambos tocados). `COVERAGE_JS covered=1512 valid=1522 pct=99.34
  files=5` (piso 85). `COVERAGE_GUARD new_app_cs=0 waived=0`. Zero `COVERAGE_WAIVER_INVALID`.
- `dotnet format whitespace --verify-no-changes` restrito aos arquivos `.cs` tocados por este iter:
  exit 0, limpo (as 2 violacoes FINALNEWLINE legadas de `Platforms/Android/*` — W-7 — continuam fora
  do escopo, nenhum arquivo deste iter aparece no log de erro do `--verify-no-changes` a nivel de
  solucao).
- Invariantes: `translation.js`/`paginated.js`/`scroll.js` diff VAZIO vs `BASELINE` (`02a4c6c`);
  zero `querySelectorAll('...')` com aspas simples em `snippets.js`; `_blobPath(bands, 10)` literal +
  `OFF=8`/`padX=5`/`padY=1.5` + regex unica de `_splitSentences` + fonte unica de `_snippetRoots`
  todos re-grepados apos o diff (1x cada).
- `git status` limpo apos os 2 commits; escopo confere exatamente com os arquivos listados em cada
  mensagem de commit.

Commits: `044870b` — `fix(snippet-translation): guard snippet translations against implausibly long
responses and purge poisoned rows on restore (D-A)`; `daf11a7` — `fix(snippet-translation): keep
glass blobs measured after reflow and trace one contour per pagination column (D-B)`.

## Iter 7 (post-loop fix round, autorizado pelo usuario — 4o feedback com screenshots do app real)

Loop ja tinha convergido de novo (`8589c1e`, APPROVED_WITH_WARNINGS) quando o usuario testou o app
real e reportou DOIS defeitos ligados entre si: (1) ao REDIMENSIONAR a janela, o vidro some dos
snips justamente quando o paragrafo passa a fragmentar entre colunas/paginas; (2) ao mudar de
pagina, uma bolha de vidro fantasma aparece flutuando numa area vazia, sem selecao/traducao valida
por perto. Causa-raiz ja diagnosticada pelo orquestrador antes desta iteracao (confirmada na fonte).

### Causa-raiz

O blob (mask+svg) era `position: absolute` FILHO DO PARAGRAFO. Quando o paragrafo fragmenta entre
colunas do `_pager` (multi-column CSS, `paginated.js` — congelado, so lido), um elemento absoluto
ancora no PRIMEIRO box de fragmento gerado pelo navegador, mas `_blobFromEls` calculava as bandas
relativas ao `getBoundingClientRect()` da UNIAO dos fragmentos (top = min dos tops, que pode
pertencer ao SEGUNDO fragmento, no topo da coluna seguinte). Ancora (onde o elemento realmente
renderiza) e origem da geometria (de onde as coordenadas do path partem) descasavam: o resultado era
banda com Y negativo na pratica (vidro clipado/invisivel) ou deslocada (bolha fantasma numa area sem
relacao com o texto). A particao por coluna do iter 6 (`_columnGroupsOf`) ja particionava certo, mas
desenhava tudo no lugar errado pelo mesmo descasamento — o mockup nunca teve colunas, entao ancorar
por paragrafo era estruturalmente invalido no paginado real e so um bug latente ate o usuario
redimensionar a janela o suficiente para fragmentar um paragrafo.

### Fix — camada de blobs ancorada na RAIZ, nao mais no paragrafo

Toda a mudanca vive em `snippets.js` (mais o harness de teste e a PIXEL-SPEC); nenhum `.cs` tocado.

1. **Camada por raiz** (`_ensureLayerFor`/`_removeLayerFor`, `_snippetLayers` — `WeakMap`, nao `Map`:
   o `#_pager` paginado e um elemento NOVO a cada troca de capitulo, `paginated.js#initPagination`
   descarta o antigo; um registro forte acumularia uma entrada morta por capitulo virado para
   sempre). No mount (e sob demanda em `_renderAllBlobs`), cada raiz de `_snippetRoots()` ganha UM
   `<div class="tr-blob-layer">` como PRIMEIRO filho, `position: absolute; left: 0; top: 0; width: 0;
   height: 0; pointer-events: none`. Se `getComputedStyle(root).position === 'static'`, o codigo seta
   `root.style.position = 'relative'` E lembra que foi ELE quem setou (`ownedPosition`), restaurando
   `''` so nesse caso no unmount — nunca mexe numa posicao que o livro ou outro script ja tivesse.
2. **`_blobFromEls` mede relativo a RAIZ, nao ao paragrafo**: `_rootFor(el)` (novo) resolve a raiz via
   `root.contains(el)` percorrendo `_snippetRoots()` — sem repetir as strings `_pager`/
   `chapter-content` fora de `_snippetRoots` (DoD 6 continua valendo). Coordenadas passam a ser
   `rect - rootRect` em vez de `rect - parRect`. A raiz paginada cobre TODAS as paginas do capitulo
   de uma vez (CSS columns), entao a mascara/svg NAO e dimensionada ao tamanho da raiz inteira —
   `left`/`top`/`w`/`h` sao derivados do bounding box JUSTO em torno dos rects medidos (min/max das
   bandas +- `OFF`), e viram estilo INLINE por blob (`_updateBlob` agora seta `.style.left`/`.style.
   top`, que a CSS `.tr-blob`/`.tr-blob-svg` deixou de fixar em `-8px`). `_blobPath(bands, 10)` e
   `OFF`/`padX`/`padY` continuam LITERAIS e intocados — so a origem das coordenadas mudou.
3. **Blobs viram filhos do layer**: `_renderAllBlobs` resolve `root`/`layer` por entrada (via
   `_rootFor` + `_ensureLayerFor`) e faz `layer.appendChild(mask); layer.appendChild(svg)` em vez de
   `paragraph.prepend(...)`. `_blobDescriptors` perdeu o campo `owner` (paragrafo) — nao e mais
   necessario, a raiz e resolvida depois.
4. **Z-order**: como o layer e SEMPRE o primeiro filho da raiz (inserido antes de qualquer
   paragrafo), e os spans de periodo (`.tr-sent`/`[data-snip]`) continuam `position: relative` na
   propria CSS, o texto pinta por cima do vidro pela ordem do documento — sem depender do paragrafo
   em si ser posicionado. Prova por teste (`z-order: ...`).
5. **Fragmentacao**: com coordenadas root-relative, os grupos de `_columnGroupsOf` (iter 6, intocado)
   caem naturalmente na coluna correta cada um — o vidro aparece na parte visivel de CADA pagina que
   o periodo ocupa. A bolha fantasma morre: nao existe mais ancora descasada, so uma raiz que nunca
   fragmenta a si mesma.
6. **Limpeza**: `unmountSnippetLayer` chama `_removeLayerFor` por raiz — remover o `<div>` do layer
   leva TODOS os seus mask/svg filhos junto num unico `.remove()`, sem precisar mais varrer `_blobs`
   no-por-no; `_blobs.clear()` so reseta o registro. `_unwrapParagraph`/`_originalParagraphText`
   perderam o branch morto `_hasClass(node, 'tr-blob')` (nunca mais alcancavel: um blob nunca e filho
   do paragrafo) — sem cadaver comentado.
7. **`_wrapParagraph`**: `el.style.position = 'relative'` removido (e o reset simetrico em
   `_unwrapParagraph`) — a decisao literal do CONTEXT era "decida lendo": o paragrafo so precisava
   ser posicionado para servir de ancora ao blob antigo; os spans `.tr-sent` ja sao `position:
   relative` por si so e continuam garantindo a ordem de pintura sem ajuda do paragrafo, entao a
   linha ficou sem funcao (confirmado: nenhuma regra CSS depende de `[data-pi]` estar posicionado).

### Testes novos (11: 9 em `snippets.test.js`, 2 em `harness.test.js`) e 2 removidos

`harness.test.js` (+2, capacidades novas exigidas pelo fix): `contains` (Node.contains — usado por
`_rootFor`) prova nodo/descendente/fora-da-arvore; `getComputedStyle` prova o default `static` e o
reflexo do `style.position` inline (harness nao tem cascata CSS real, so reflete o inline, suficiente
para o unico uso que `snippets.js` faz dele).

`snippets.test.js` (+9, -2 renomeado/removido):
- `layer:` (5): camada criada como primeiro filho da raiz; CSS da `.tr-blob-layer` tem `pointer-events:
  none`; raiz `static` ganha `position: relative` e restaura no unmount; raiz que JA tinha posicao
  propria nunca e tocada; modo rolagem da UM layer por `.chapter-content`, layers distintos.
- `blob geometry: a paragraph fragmented across two columns is measured relative to the ROOT, never
  the paragraph` (1): raiz com rect NAO-zero e paragrafo SEM rect proprio (default zero) — prova por
  construcao que o paragrafo deixou de ser lido; `left`/`top`/`w`/`h` calculados a mao E validados
  contra `_blobPath` chamado independentemente com as bandas locais esperadas; asserta que nenhuma
  banda tem Y negativo (o defeito original) e que ha exatamente 2 contornos (`M`/`Z`) para as 2
  colunas simuladas.
- `sweep: the layer holds no orphaned blob after a selection is cleared` (1): prova "nenhum mask/svg
  orfao no layer" diretamente pelo `childNodes.length` do layer, nao so pela contagem global do
  documento (que as suites de sweep pre-existentes ja cobriam).
- `root: an element outside every snippet root resolves to no root` (1): cobre o branch defensivo de
  `_rootFor` (retorno `null`), unico trecho novo que ficou descoberto na 1a rodada de
  `--experimental-test-coverage` (os outros 4 gaps reportados sao pre-existentes, mesmas linhas
  relativas antes e depois do diff, confirmado com `git stash`).
- **Renomeado**: `z-order: the blob mask and svg become the first children of the paragraph...` vira
  `z-order: the blob layer is the first child of the root, so the glass paints before every
  paragraph` — comportamento mudou de fato (layer, nao paragrafo), nome atualizado para descrever a
  garantia real.
- **Removido** (1, documentado — nao e perda de cobertura): `unmount: a stray glass blob is skipped
  without throwing even though its outline is an SVG element (B-2 belt and suspenders)`. O cenario
  que o teste simulava (blob ainda anexado ao paragrafo quando `_unwrapParagraph` roda) ficou
  estruturalmente IMPOSSIVEL apos este fix — um blob nunca mais e filho de paragrafo, entao nao ha
  "orfao" para o unwrap encontrar. A garantia mais ampla que B-2 protegia (unmount nunca lanca mesmo
  com blobs vivos coexistindo com selecao/snip) continua coberta pelo teste holistico
  `unmount: completes without throwing and remains re-mountable with a snip blob and an active
  selection present (B-2 regression)`, inalterado por este diff e ainda verde.

### Invariantes re-conferidos (nao regrediram)

`translation.js`/`paginated.js`/`scroll.js`: diff VAZIO vs `BASELINE` (`02a4c6c`). Zero
`querySelectorAll('...')` com aspas simples contendo p/h1/li/div em `snippets.js`.
`_blobPath(bands, 10)` literal no call site (a variavel local do mapeamento foi deliberadamente
chamada `bands`, nao `local`, para preservar o grep do DoD 4); `OFF=8`/`padX=5`/`padY=1.5` intactos;
`_splitSentences` com fonte e regex unicas (1x cada); `_snippetRoots` como fonte unica de `_pager`/
`chapter-content` (contagem no arquivo inteiro == contagem dentro da propria funcao). Golden geometry
(DoD 4): os 4 testes de nome exato passam, `_blobPath` em si NAO foi tocado (so quem a chama mudou a
origem das coordenadas que passa pra ela). PIXEL-SPEC: os 14 literais do DoD 1 continuam presentes;
nova sub-nota "Ancoragem no app" adicionada apos a secao "Blob de vidro" documentando a divergencia
estrutural, sem remover nenhum literal medido do mockup.

### Verificacao pos-fix

- JS: **184/184 passando** (era 175 antes deste iter — 11 adicoes brutas, 2 remocoes, liquido +9),
  0 fail, 0 skipped. `comm -23` nome a nome contra a suite anterior a esta iteracao, nos 2 arquivos
  tocados (`snippets.test.js`, `harness.test.js`): so as 2 remocoes documentadas (`unmount: a stray
  glass blob...` e o nome antigo do z-order, substituido pelo novo). `comm -23` contra `main` (so
  `bridge`/`paginated`/`scroll`/`translation`, escopo do DoD 9): vazio.
- Cobertura de `snippets.js` isolada (`node --test --experimental-test-coverage
  --test-coverage-include=".../snippets.js"`): **99.20% linhas / 88.99% branches** — o UNICO gap novo
  introduzido pelo diff (`_rootFor` linha do `return null`) foi fechado pelo teste dedicado; os 4
  gaps remanescentes (`719-724`, `855-856`, `967`, `1213`) sao PRE-EXISTENTES, confirmados
  comparando com `git stash` antes do diff (mesmos branches defensivos, so deslocados pelas
  insercoes).
- `bash scripts/coverage-gate.sh`: exit 0. `COVERAGE_JS covered=1573 valid=1583 pct=99.37 files=5`
  (piso 85, subiu de 99.34% no fim do iter 6). `COVERAGE_SCOPE covered=1340 valid=1411 pct=94.97
  files=26` (piso 90, **identico** ao fim do iter 6 — confirma que nenhum `.cs` foi tocado).
  `COVERAGE_GUARD new_app_cs=0 waived=0`. Zero `COVERAGE_WAIVER_INVALID`.
- `dotnet build -c Release -f net10.0-windows10.0.19041.0`: `0 Warning(s), 0 Error(s)`.
- `dotnet test` (Release, prova de nao-regressao — iteracao 100% JS): **414 passed / 2 skipped
  (GPU-only pre-existentes) / 0 failed / 416 total** — identico ao fim do iter 6, como esperado
  (mudanca 100% JS + doc, nenhum `.cs` no diff).
- `dotnet format whitespace --verify-no-changes`: exit 2, pelas MESMAS 2 violacoes FINALNEWLINE
  legadas de sempre (`Platforms/Android/MainActivity.cs`, `MainApplication.cs`, W-7) — fora do diff
  desta iteracao (nenhum `.cs` tocado).
- `git status`/`git diff --name-only` confirmam escopo: `snippets.js`, `harness.js`,
  `harness.test.js`, `snippets.test.js`, `design/v0.2.0/PIXEL-SPEC.md` (mais os arquivos de estado do
  `.jdi/` do orquestrador do loop, nao tocados por este specialist).

Commit: `fix(snippet-translation): anchor glass blobs to a per-root layer so column-fragmented
periods keep their glow` (1 commit atomico — a arquitetura de layer-por-raiz e o unico fix que
resolve os DOIS defeitos reportados, que compartilham a MESMA causa raiz; harness + testes +
PIXEL-SPEC entram juntos porque o codigo de producao sozinho quebraria a suite existente sem as
capacidades novas do harness no MESMO commit).

## Iter 8 (fix round pos-loop, autorizado pelo usuario — 5o feedback com screenshot do app real)

Loop ja convergido (round pos-loop-2, `76e9dac`, APPROVED_WITH_WARNINGS) quando o usuario testou o
app real de novo e reportou DOIS defeitos novos: **D-A** — paragrafo com markup inline (`<em>`/`<a>`)
vira UM periodo unico em vez de splitar nos limites reais de sentenca ("nao consigo selecionar um
periodo, esta selecionando o paragrafo inteiro"); essa era a derivacao D aceita no `PLAN.md` original
("fora de escopo desta phase por decisao... a evolucao e phase futura, nao debito escondido") — o uso
real (EPUBs tem `<em>`/`<i>`/`<a>` em quase todo paragrafo) a derrubou. **D-B** — um `.tr-loading`
podia ficar pulsando pra sempre quando `applySnippetTranslation` recebia um item PRESENTE mas
inaplicavel (paragrafo/range nao encontrado): o placeholder nunca era devolvido aos periodos, so em
erro/excecao/resultado vazio. Causas ja diagnosticadas e fechadas pelo orquestrador antes desta
iteracao. Entregue em **2 commits atomicos, um por causa-raiz** (mesmo padrao do iter 6): D-A muda
`_wrapParagraph`/`_spliceSpanBackToPeriods`/`_originalParagraphText` + harness; D-B muda so
`applySnippetTranslation`. 0 `.cs` tocado nos dois.

### D-A — split preservando markup (derivacao D entregue)

**`_wrapParagraph` (`snippets.js`) deixou de colapsar QUALQUER paragrafo com filho-elemento num
periodo unico.** Algoritmo novo:
- `_SENTENCE_BOUNDARY_RE`: a regex de `_splitSentences` extraida pra uma `var` no topo do arquivo,
  lida por AMBAS `_splitSentences` (pedacos aparados) e a funcao irmã nova `_sentenceBoundaryMatches`
  (offsets `[start,end)` da propria fronteira no texto achatado, via `.source` — nunca um segundo
  literal; `grep -c` da regex confere 1x, re-verificado apos o diff).
- `_wrapMarkupParagraph(el)`: calcula o texto achatado (`el.textContent`), acha toda fronteira REAL
  via `_sentenceBoundaryMatches`, e DESCARTA qualquer fronteira cujo offset caia dentro do range de
  um elemento de primeiro nivel (`_topLevelElementRanges`) — um elemento inline e ATOMICO, entao a
  fronteira que cairia no meio dele simplesmente nao conta, e o periodo que a conteria continua ate a
  proxima fronteira em texto livre (1 linha WHY no codigo). `_consumeTextNode` percorre cada text node
  de primeiro nivel contra as fronteiras restantes, cortando com `Text.splitText` nativo (nunca
  serializa/reparseia HTML — csharp.md §4, conteudo do livro e input NAO confiavel): o pedaco antes da
  fronteira fecha o periodo CORRENTE (`span.tr-sent[data-si=j]`, via `appendChild` — move nodes, nao
  reconstroi), a propria fronteira (espaco) fica como no solto entre os spans (nunca dentro de um —
  `_unwrapParagraph` ja devolve um filho solto intocado), um span novo abre pro periodo seguinte. Um
  elemento de primeiro nivel e sempre movido inteiro (`appendChild`) pro span CORRENTE, nunca cortado.
  Confirmado a mao (2 casos do enunciado + 1 terceiro com `<a>` + 2 fronteiras reais, via script
  descartavel) e via os testes dourados abaixo: o mockup `onlySentence` (1 periodo so) continua
  alcancavel — e so o que sobra quando o paragrafo genuinamente nao tem NENHUMA fronteira real fora de
  markup (teste pre-existente da T-4, intocado, continua verde).
- **Undo com markup preservado.** `setSnippetLoading` agora chama `_captureRangeNodes` (via
  `_rangeNodeIndices`, extraida de `_spliceRange` — DRY, mesmo criterio de range nos dois) ANTES de
  substituir o range pelo placeholder, guardando os NODES originais (nao o texto) num `Map` novo,
  `_snipOriginalNodes`, chaveado pela mesma string `chapterHRef:pi:a:b` que um snip carrega em
  `dataset.snip`. `_spliceSpanBackToPeriods` (usada por `_restoreSnipToPeriods` — X do chip — e por
  `clearSnippetLoading`) ganhou um 4o parametro `key`: se o Map tem os nodes originais, eles sao
  splicados de volta VERBATIM (o `<em>` sobrevive); senao (fallback: snip vindo de `restoreSnippets`,
  que so tem texto persistido — sem o Map nunca populado) cai no re-split de texto plano de sempre
  (`_plainPeriodSpans`, extraida sem mudar comportamento). Toda leitura do Map deleta a entrada (uso
  unico); `unmountSnippetLayer` limpa o Map inteiro — mesmo bound de `_blobs`, nada sobrevive troca de
  capitulo (csharp.md §2.4).
- **Bonus (achado revisando o mesmo trecho): W-13 fechado.** `_originalParagraphText` lia
  `node.childNodes[0].textContent` pra um periodo — certo pra periodo/loading de 1 filho, mas
  truncava um periodo com markup (varios filhos) no PRIMEIRO node, cortando o contexto enviado ao
  modelo bem no meio de uma sentenca. Fix: `node.textContent` (achatado completo), 1 linha.
- **Ancoras antigas.** Paragrafo que era 1 periodo (`data-si=0`) e agora vira N: snips/loading
  persistidos com `[0..0]` terao hash divergente na proxima abertura — descarte silencioso JA
  EXISTENTE em `restoreSnippets` (SEM purge, ancora invalida != registro podre, mesma logica do D-A do
  iter 6). Documentado no `.jdi/todos/2026-08-09-snippet-translation.md` (item da derivacao D marcado
  RESOLVIDO, texto completo da entrega).
- `test/js/harness.js`: `FakeText.prototype.splitText(offset)` novo, espelhando `Node.splitText`
  nativo (trunca o node, insere a cauda como PROXIMO IRMAO, ainda anexada ao mesmo pai) — sem isso o
  algoritmo novo nao tem como cortar um text node no harness.

**Testes novos (13):** `snippets.test.js` (+7) — 3 periodos com `<em>` dentro do periodo 0 e
selecao do periodo 1 isolada; fronteira dentro de `<em>` adiada (periodo 0 engloba o elemento
inteiro); unwrap fiel (`innerHTML` byte-identico apos mount+unmount, com markup); `_originalParagraphText`
com periodo-markup entre um snip (W-13); traduzir periodo com markup -> chip ok -> remover -> `<em>`
original de volta (nodes, nao texto); `clearSnippetLoading` idem para o caso de falha; `_snipOriginalNodes`
limpo no unmount (sem leak). `harness.test.js` (+3) — `splitText` trunca e insere irmao; em node
destacado (sem pai) so trunca; encadeado 2x carve 3 pedacos.

**Verificacao pos-fix (so D-A, D-B nao commitado ainda neste ponto):**
- JS: **194/194 passando** (era 184 antes deste iter), 0 fail, 0 skipped. Zero teste de `main`
  perdido (`snippets.test.js`/`snippets.js` nao existem em `main` — feature inteira nova desde o
  merge-base `02a4c6c`; os 4 arquivos que main tem — `bridge`/`paginated`/`scroll`/`translation` — nao
  tocados por este diff, confirmado por `git diff --name-only`).
- `bash scripts/coverage-gate.sh`: exit 0. `COVERAGE_JS covered=1704 valid=1714 pct=99.42 files=5`
  (piso 85). `COVERAGE_SCOPE covered=1340 valid=1411 pct=94.97 files=26` (piso 90, **identico** ao
  fim do round pos-loop-2 — confirma que nenhum `.cs` foi tocado). `COVERAGE_GUARD new_app_cs=0
  waived=0`.
- `dotnet test` (Release, prova de nao-regressao — mudanca 100% JS): **414 passed / 2 skipped
  (GPU-only pre-existentes) / 0 failed / 416 total** — identico ao fim do round pos-loop-2.
- `dotnet build -c Release -f net10.0-windows10.0.19041.0`: `0 Warning(s), 0 Error(s)`.
- Invariantes: `translation.js`/`paginated.js`/`scroll.js` diff vazio vs `BASELINE`; aspas duplas em
  todo `querySelectorAll`; `_blobPath(bands, 10)` + `OFF=8`/`padX=5`/`padY=1.5` intactos; regex de
  `_splitSentences` 1x (re-contada apos o diff: `grep -cF` das duas substrings-ancora = 1 cada,
  `grep -cE '^function _splitSentences\('` = 1).

Commit: `fix(snippet-translation): split a paragraph into real sentence periods even when it carries
inline markup, preserving the element on undo` (D-A).

### D-B — loading nunca-orfao

`applySnippetTranslation` refatorada em `_applySnippetItem(item)` (a mesma logica de antes, agora
retornando se a splicagem realmente aconteceu — `_replaceRangeWithSnip`/`_spliceRange` ja retornavam
esse booleano, so ninguem lia) e `_clearOrphanedLoading(item)` (novo): quando um item NAO aplica
(paragrafo nao encontrado OU o range especifico dentro dele sumiu), procura no DOCUMENTO INTEIRO (nao
so onde `_findParagraph` teria olhado — e exatamente essa busca que falhou) um `.tr-loading` cuja
`dataset.loadKey` bate com a chave do proprio item, e devolve-o aos periodos via
`_spliceSpanBackToPeriods` (reaproveitando o `key` novo do fix D-A — se os nodes originais ainda
estao no `_snipOriginalNodes`, a restauracao ainda preserva markup). `applySnippetTranslation`
propriamente vira so `for (item) { if (!_applySnippetItem(item)) _clearOrphanedLoading(item); }`.

**Teste novo (1):** `applySnippetTranslation: an item whose paragraph can no longer be resolved
still clears its own loading placeholder...` — reproduz a race construindo o cenario minimo: chama
`setSnippetLoading`, depois torna o pager irresolvivel (`_pager.id = ''`, simulando uma navegacao que
derrubou a raiz que possuia o pedido) ANTES de `applySnippetTranslation` chegar com o MESMO
`chapterHRef/pi/a/b` — prova que zero `.tr-loading` sobra no DOM. O caminho feliz (item aplicavel)
continua coberto pelos testes pre-existentes `applySnippetTranslation replaces a loading
placeholder...`/`applySnippetTranslation gives the finished snip a permanent glass blob...`/
`applySnippetTranslation destructively replaces an overlapping existing snip`, todos intocados
(regressao).

**Verificacao pos-fix (D-A + D-B juntos, estado final):**
- JS: **195/195 passando** (194 + 1), 0 fail, 0 skipped. `comm -23` nome a nome contra a suite do
  commit D-A: vazio (so a 1 adicao).
- `bash scripts/coverage-gate.sh`: exit 0. `COVERAGE_JS covered=1727 valid=1737 pct=99.42 files=5`
  (piso 85). `COVERAGE_SCOPE covered=1340 valid=1411 pct=94.97 files=26` (piso 90, **identico** —
  confirma zero `.cs` tocado nos dois commits). `COVERAGE_GUARD new_app_cs=0 waived=0`.
- `dotnet test` (Release): **414 passed / 2 skipped (GPU-only pre-existentes) / 0 failed / 416
  total** — identico, como esperado (D-A+D-B sao 100% JS).
- `dotnet format whitespace --verify-no-changes`: exit 2, pelas MESMAS 2 violacoes FINALNEWLINE
  legadas de sempre (`Platforms/Android/MainActivity.cs`, `MainApplication.cs`, W-7) — fora do diff
  desta iteracao (nenhum `.cs` tocado por nenhum dos dois commits).
- `git status`/`git diff --name-only` confirmam escopo: `snippets.js`, `harness.js`,
  `harness.test.js`, `snippets.test.js`, `.jdi/todos/2026-08-09-snippet-translation.md` (mais os
  arquivos de estado do `.jdi/` do orquestrador, nao tocados por este specialist).

Commit: `fix(snippet-translation): never leave a snippet's loading placeholder stuck when its
translation result cannot be applied` (D-B).
