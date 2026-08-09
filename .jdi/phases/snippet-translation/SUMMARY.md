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
