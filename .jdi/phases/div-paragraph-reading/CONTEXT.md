# Phase 18: Traducao interativa cega a paragrafo em `<div>` (leitura) — Context (slug: div-paragraph-reading)

Gerado em modo `auto` via `/jdi-issue` (`mode=auto dod=auto_only`), brief = card colado pelo
usuario 2026-08-01 (sem interacao). Referente ja resolvido por evidencia em
`D-2026-08-01-div-paragraph-reading-1`. Diagnostico do brief CONFIRMADO por leitura direta do
codigo nesta sessao, com uma correcao registrada em `D-...-2`.

## Goal
Corrigir a traducao interativa por paragrafo visivel do ReaderPage para EPUBs de calibre
(paragrafos em `<div class="calibreN">`), mesmo defeito de classe que `div-paragraph-translation`
corrigiu para a traducao de livro completo — hoje devolve zero paragrafos e falha em silencio.

## Locked decisions
- **D-...-2** (escopo real): o defeito e 100% JavaScript. `TranslateParagraphsAsync`
  (`TranslationManager.cs:284-325`) nunca extrai HTML — recebe os paragrafos prontos do JS.
  `translation.js:7,26,45` faz `querySelectorAll('p')`/`'p[data-original]'` nas 3 funcoes
  (`getVisibleParagraphs`, `applyTranslations`, `clearTranslations`); zero `<p>` = `[]` =
  `ReaderPage.xaml.cs:296` retorna em silencio. Corrige o item 4 do brief: a paridade que importa
  e ENTRE as 3 funcoes JS, nao entre C# e JS.
- **D-...-3** (fix): `translation.js` ganha `_translatableCandidates(pg)` (helper interno, nao
  exportado) = `querySelectorAll('p,h1,h2,h3,h4,h5,h6,li,div')` filtrado — `div` so entra se for
  folha (`el.querySelector('div,p,h1..h6,li')` null) e tiver letra Unicode (`/\p{L}/u`); demais
  tags mantem o filtro de texto nao-vazio atual. As 3 funcoes passam a chamar SO esse helper —
  dessincronia de indice deixa de ser possivel por construcao. `test/js/harness.js` ganha suporte a
  selector groups por virgula (necessario pro `querySelectorAll` composto acima); nenhum outro
  recurso CSS novo. `:has()` REJEITADO (compat de WebView entre plataformas).
- **D-...-4** (caminho C# morto): `HtmlUtility.ExtractParagraphs`/`ParagraphRegex()` (mesmo defeito
  de classe, zero chamador de UI, so `TranslateChapterAsync`) e REMOVIDO; `TranslateChapterAsync`
  passa a chamar `ExtractTextBlocks` (ja corrigido). Compativel 1:1 com os 6 testes existentes
  (mesmo filtro no ramo p/h/li, corpos so `<p>`). REJEITADO remover o metodo do contrato
  `ITranslationManager` — alem do pedido do card.
- **D-...-5** (sinal + escopo Scroll): `getVisibleParagraphs` emite `console.warn` quando a pagina
  tem texto mas zero candidatos no capitulo inteiro (mesmo canal de `paginated.js`). So JS —
  `ReaderPage.xaml.cs`/`ReaderPageModel.cs` (fora da rede de testes, D-2026-07-30-regression-suite-2)
  ficam intocados. Aviso VISIVEL ao usuario -> `## Deferred to PR review`. Confirmado: modo Scroll
  nao tem traducao por paragrafo (bloqueado em `ReaderPage.xaml.cs:261-264` antes de chegar no
  paginado) — comportamento intacto.

## Canonical refs
- `.jdi/DECISIONS.md` / `.jdi/decisions/` D-2026-08-01-div-paragraph-reading-1..5
- `.jdi/decisions/` D-2026-08-01-div-paragraph-translation-1,7,8,9 (padrao de selecao/DoD que este
  fix espelha em JS, sem compartilhar codigo)
- `src/TranslateReader/Resources/Raw/wwwroot/js/translation.js`, `test/js/{harness,translation.test}.js`
- `src/TranslateReader.Core/Utilities/HtmlUtility.cs`, `Business/Managers/TranslationManager.cs`
- `.claude/rules/csharp.md` §1 (excecao so pra erro), §3 (UI thread), §4 (EPUB/HTML e input nao
  confiavel), §6 (bugfix comeca vermelho, sem I/O em teste novo)

## Out of scope
- Parser HTML real (AngleSharp/HtmlAgilityPack) — decisao ja fechada por
  `D-2026-08-01-div-paragraph-translation-1`.
- `:has()`/combinadores CSS no harness — so grupo por virgula, D-...-3.
- Remover `TranslateChapterAsync`/membro de `ITranslationManager` — D-...-4.
- EPUB do usuario — nunca referenciado nem commitado.

## Definition of Done

### Auto-verifiable
- [ ] `translation.js` tem UMA fonte de selecao (`_translatableCandidates`) reusada pelas 3
      funcoes; os seletores antigos `'p'`/`'p[data-original]'` somem
      **Verify:** `F=src/TranslateReader/Resources/Raw/wwwroot/js/translation.js; test $(grep -cE "function _translatableCandidates" "$F") -eq 1 && test $(grep -c "_translatableCandidates(" "$F") -ge 4 && test $(grep -cE "querySelectorAll\('p'\)|querySelectorAll\('p\[data-original\]'\)" "$F") -eq 0`
      **Source:** CONTEXT
- [ ] Paragrafo calibre (div-folha com letra) fica visivel, traduzivel e limpavel: round-trip
      get/apply/clear roda de verdade com >= 4 testes nomeados `calibre`
      **Verify:** `N=$(grep -cE "^test\('.*calibre" test/js/translation.test.js); test "$N" -ge 4 && mkdir -p TestResults && node --test --test-name-pattern="calibre" test/js/translation.test.js > TestResults/js-dod2.log 2>&1 && grep -qE "^# fail[[:space:]]+0$" TestResults/js-dod2.log && P=$(grep -oE "^# pass[[:space:]]+[0-9]+" TestResults/js-dod2.log | grep -oE "[0-9]+") && test "$P" -ge "$N"`
      **Source:** CONTEXT
- [ ] Suite inteira de `translation.js` passa, sem regressao dos testes ja existentes em `main`
      mais os >= 4 novos de calibre
      **Verify:** `B=$(git show main:test/js/translation.test.js | grep -cE "^test\("); mkdir -p TestResults && node --test test/js/translation.test.js > TestResults/js-dod3.log 2>&1 && grep -qE "^# fail[[:space:]]+0$" TestResults/js-dod3.log && P=$(grep -oE "^# pass[[:space:]]+[0-9]+" TestResults/js-dod3.log | grep -oE "[0-9]+") && test "$P" -ge "$((B + 4))"`
      **Source:** CONTEXT
- [ ] `HtmlUtility.ExtractParagraphs`/`ParagraphRegex` removidos; `TranslateChapterAsync` usa
      `ExtractTextBlocks`
      **Verify:** `F=src/TranslateReader.Core/Utilities/HtmlUtility.cs; test $(grep -c "ExtractParagraphs" "$F") -eq 0 && test $(grep -c "ParagraphRegex" "$F") -eq 0 && grep -q "HtmlUtility.ExtractTextBlocks" src/TranslateReader.Core/Business/Managers/TranslationManager.cs`
      **Source:** CONTEXT
- [ ] `TranslateChapterAsync` cobre corpo calibre (Fixture A reusada de `CalibreFixtures`) e os
      testes `TranslateChapterAsync_*` (existentes + 1 novo) passam de verdade, sem regressao
      **Verify:** `B=$(git show main:test/TranslateReader.Tests/TranslationManagerTests.cs | grep -c "public async Task TranslateChapterAsync_"); grep -q "TranslateChapterAsync_ForCalibreStyleBody_TranslatesLeafDivParagraphs" test/TranslateReader.Tests/TranslationManagerTests.cs && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~TranslateChapterAsync" > TestResults/dod5.log 2>&1 && grep -q "Passed!" TestResults/dod5.log && awk -v n=$((B+1)) '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (k&&f+0==0&&p+0>=n)?0:1}' TestResults/dod5.log`
      **Source:** CONTEXT
- [ ] Suite C# inteira passa, piso acima do baseline conhecido de `main`
      (`D-2026-08-01-div-paragraph-translation-9` item 8: Failed 0, Passed 320, Total 322) +1
      teste novo desta phase
      **Verify:** `mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release > TestResults/dod6.log 2>&1 && grep -q "Passed!" TestResults/dod6.log && awk -v pn=321 -v tn=323 '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1);if($i=="Total:")t=$(i+1)}} END{exit (k&&f+0==0&&p+0>=pn&&t+0>=tn)?0:1}' TestResults/dod6.log`
      **Source:** CONTEXT
- [ ] `src/TranslateReader/PageModels`/`Pages` (fora da rede de testes, D-2026-07-30-regression-suite-2)
      ficam intocados — o fix inteiro mora em `Resources/Raw/wwwroot/js` e no Core
      **Verify:** `test -z "$(git diff --name-only main -- src/TranslateReader/ ':(exclude)src/TranslateReader/Resources/Raw/wwwroot/js/')"`
      **Source:** CONTEXT

### Manual
- _(none — dod=auto_only; itens humanos foram para `## Deferred to PR review`)_

## Deferred to PR review
- Decisao de produto/UX: se/como avisar VISUALMENTE o usuario (toast/badge) quando o `console.warn`
  de `D-...-5` disparar — mesma classe de decisao ja deferida em
  `D-2026-08-01-div-paragraph-translation-4`.
- Leitura humana: o `_translatableCandidates` renderiza/pagina corretamente numa WebView real (o
  harness prova comportamento de funcao sobre DOM falso, nao layout/rendering real).
- Confirmacao de que o SonarCloud (analisador `javascript`) nao acusa issue nova nos arquivos
  tocados — so existe apos push+CI (D-2026-07-30-sonar-zero-issues-12).

## Notes
`_translatableCandidates` espelha o design de `HtmlUtility.IsTranslatableBlock`/`TextBlockRegex`
(p|h1-6|li uniao disjunta com div-folha+letra) por linguagem de design, sem compartilhar codigo —
runtimes diferentes (WebView JS vs C#), caminhos diferentes (leitura nunca chama o C#).
`ITranslationManager.TranslateChapterAsync` continua sem chamador de UI apos esta phase; so
`TranslateParagraphsAsync` esta ligado ao `ReaderPage`. Reusar `CalibreFixtures.PartiallyCoveredBody`
(`test/TranslateReader.Tests/CalibreFixtures.cs`) para o teste novo do item 5 do DoD — mesma forma
calibre ja fixada pela phase irma, nao inventar fixture nova.
