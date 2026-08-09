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
