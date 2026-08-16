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
      **Verify:** `F=src/TranslateReader/Resources/Raw/wwwroot/js/translation.js; C=$(sed -e 's://.*::' -e 's:/\*.*\*/::' "$F"); test "$(printf '%s\n' "$C" | grep -cE "^function _translatableCandidates\(")" -eq 1 && test "$(printf '%s\n' "$C" | grep -cE "querySelectorAll\('p'\)|querySelectorAll\('p\[data-original\]'\)")" -eq 0 && for fn in getVisibleParagraphs applyTranslations clearTranslations; do printf '%s\n' "$C" | awk -v f="window.$fn = function" 'index($0,f)==1,/^};/' | grep -q "_translatableCandidates(" || exit 1; done && H=$(printf '%s\n' "$C" | awk '/^function _translatableCandidates\(/,/^}$/') && printf '%s\n' "$H" | grep -qE "if \([^!]*querySelector\(" && printf '%s\n' "$H" | grep -qE "if \(![A-Za-z_]+\.test\("`
      **Source:** CONTEXT (comando substituido por `D-2026-08-01-div-paragraph-reading-6`: comentario
      removido antes do grep, helper amarrado ao CORPO das 3 funcoes e polaridade das 2 guardas do
      helper checada. Gate ESTRUTURAL de fonte unica — a prova de COMPORTAMENTO do corpo e dos itens
      2 e 3.)
- [ ] Paragrafo calibre (div-folha com letra) fica visivel, traduzivel e limpavel: round-trip
      get/apply/clear roda de verdade com >= 4 testes nomeados `calibre`
      **Verify:** `T=test/js/translation.test.js; N=$(grep -cE "^test\('.*calibre" "$T"); test "$N" -ge 4 && mkdir -p TestResults && node --test --test-reporter=tap --test-name-pattern="calibre" "$T" > TestResults/js-dod2.log 2>&1 && grep -qE "^# fail 0$" TestResults/js-dod2.log && P=$(awk '/^# pass /{print $3}' TestResults/js-dod2.log) && test "$P" -ge "$N" && for t in "getVisibleParagraphs returns every calibre leaf div that holds letters" "applyTranslations writes into the calibre div the reported index points at" "clearTranslations restores a translated calibre div and drops the marker"; do grep -qE "^ok [0-9]+ - $t$" TestResults/js-dod2.log || exit 1; done`
      **Source:** CONTEXT (comando substituido por `D-2026-08-01-div-paragraph-reading-6`: reporter
      TAP pinado — sem isso o comando NAO PODE sair 0 no Node 24 — e os 3 testes de round-trip
      get/apply/clear exigidos por NOME EXATO como `ok N - <nome>`, nao por contagem.)
- [ ] Suite inteira de `translation.js` passa, sem regressao dos testes ja existentes em `main`
      mais os >= 4 novos de calibre
      **Verify:** `mkdir -p TestResults && git show main:test/js/translation.test.js | awk -F"'" '/^test\(/{print $2}' | sort -u > TestResults/js-dod3-base.txt && node --test --test-reporter=tap test/js/translation.test.js > TestResults/js-dod3.log 2>&1 && grep -qE "^# fail 0$" TestResults/js-dod3.log && grep -qE "^# skipped 0$" TestResults/js-dod3.log && grep -E "^ok [0-9]+ - " TestResults/js-dod3.log | sed -E "s/^ok [0-9]+ - //" | sort -u > TestResults/js-dod3-head.txt && test -z "$(comm -23 TestResults/js-dod3-base.txt TestResults/js-dod3-head.txt)" && B=$(wc -l < TestResults/js-dod3-base.txt) && P=$(awk '/^# pass /{print $3}' TestResults/js-dod3.log) && test "$P" -ge "$((B + 4))"`
      **Source:** CONTEXT (comando substituido por `D-2026-08-01-div-paragraph-reading-6`: reporter
      TAP pinado, `# skipped 0` e — o que de fato prova nao-regressao — `comm -23` NOME A NOME entre
      os testes de `main` e os nomes VERDES do HEAD. O piso `B+4` sozinho passava com 3 testes de
      `main` deletados.)
- [ ] `HtmlUtility.ExtractParagraphs`/`ParagraphRegex` removidos; `TranslateChapterAsync` usa
      `ExtractTextBlocks`
      **Verify:** `M=src/TranslateReader.Core/Business/Managers/TranslationManager.cs; test -z "$(git grep -lE 'ExtractParagraphs|ParagraphRegex' -- 'src/*.cs' 'test/*.cs')" && B=$(sed 's://.*::' "$M" | awk '/IAsyncEnumerable<TranslatedParagraph> TranslateChapterAsync\(/,/^    }$/') && printf '%s\n' "$B" | grep -qE '=[[:space:]]*HtmlUtility\.ExtractTextBlocks\(bodyContent\)' && test "$(printf '%s\n' "$B" | grep -cE '=[[:space:]]*[A-Za-z0-9_.]+\(bodyContent\)')" -eq 1`
      **Source:** CONTEXT (comando substituido por `D-2026-08-01-div-paragraph-reading-6`: ausencia
      varrida no repo tracked inteiro via `git grep` — `grep -r` leria `obj/` e o codigo gerado por
      `[GeneratedRegex]` — e presenca amarrada ao CORPO de `TranslateChapterAsync`, com exatamente
      UMA atribuicao a partir de `bodyContent`. O `grep -q` antigo casava nas linhas 124/195.)
- [ ] `TranslateChapterAsync` cobre corpo calibre (Fixture A reusada de `CalibreFixtures`) e os
      testes `TranslateChapterAsync_*` (existentes + 1 novo) passam de verdade, sem regressao
      **Verify:** `B=$(git show main:test/TranslateReader.Tests/TranslationManagerTests.cs | grep -c "public async Task TranslateChapterAsync_"); grep -q "TranslateChapterAsync_ForCalibreStyleBody_TranslatesLeafDivParagraphs" test/TranslateReader.Tests/TranslationManagerTests.cs && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~TranslateChapterAsync" > TestResults/dod5.log 2>&1 && grep -q "Passed!" TestResults/dod5.log && awk -v n=$((B+1)) '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (k&&f+0==0&&p+0>=n)?0:1}' TestResults/dod5.log`
      **Source:** CONTEXT
- [ ] Suite C# inteira passa, piso DERIVADO de `main` no proprio comando (`[Fact]` + `[InlineData]`
      contados em `main`) +1 teste novo desta phase, e nenhum metodo de teste de `main` some
      **Verify:** `mkdir -p TestResults && B=$(( $(git grep -cE '^[[:space:]]*\[Fact' main -- 'test/TranslateReader.Tests/*.cs' | awk -F: '{s+=$NF} END{print s+0}') + $(git grep -cE '^[[:space:]]*\[InlineData' main -- 'test/TranslateReader.Tests/*.cs' | awk -F: '{s+=$NF} END{print s+0}') )) && S=$(git grep -cE 'Skip[[:space:]]*=' main -- 'test/TranslateReader.Tests/*.cs' | awk -F: '{s+=$NF} END{print s+0}') && git grep -hoE 'public (async Task|void) [A-Za-z0-9_]+\(' main -- 'test/TranslateReader.Tests/*.cs' | sed -E 's/^public (async Task|void) //; s/\($//' | sort -u > TestResults/dod6-base.txt && git grep -hoE 'public (async Task|void) [A-Za-z0-9_]+\(' -- 'test/TranslateReader.Tests/*.cs' | sed -E 's/^public (async Task|void) //; s/\($//' | sort -u > TestResults/dod6-head.txt && test -z "$(comm -23 TestResults/dod6-base.txt TestResults/dod6-head.txt)" && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release > TestResults/dod6.log 2>&1 && grep -q "Passed!" TestResults/dod6.log && awk -v tn=$((B+1)) -v sn=$S '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1);if($i=="Skipped:")s=$(i+1);if($i=="Total:")t=$(i+1)}} END{exit (k&&f+0==0&&t+0>=tn&&s+0<=sn&&p+0+s+0+f+0==t+0)?0:1}' TestResults/dod6.log`
      **Source:** CONTEXT (comando substituido por `D-2026-08-01-div-paragraph-reading-6`: o piso
      cravado `321/323` vinha de `D-2026-08-01-div-paragraph-translation-9` item 8 e esta 15 testes
      ABAIXO do baseline REAL de `main` — `Failed 0, Passed 335, Skipped 2, Total 337`, medido —
      logo aceitava regressao de ate 15 testes. Agora `B` sai de `main` no proprio comando (288+49 =
      337, bate 1:1 com o `Total` real), com `comm` nome a nome e coerencia do sumario.)
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
