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
