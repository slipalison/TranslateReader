# Phase 17: Traducao cega a paragrafo em `<div>` (EPUB de calibre) — Context (slug: div-paragraph-translation)

Gerado em modo `auto` via `/jdi-issue` (dispatch `mode=auto dod=auto_only`, brief = bug report do
usuario 2026-08-01, sem interacao — decisoes tomadas e justificadas pelo asker). Diagnostico medido
integral em `.jdi/DECISIONS.md` D-2026-08-01-div-paragraph-translation-0. Decisoes -1..-6 abaixo
**pendentes de append** em DECISIONS.md (ver aviso operacional no fim da sessao do asker) — o texto
completo delas ja esta aqui, autocontido.

## Goal
Traduzir paragrafos em `<div>` (EPUBs calibre, hoje `ExtractTextBlocks` so ve `p|h1-6|li`) e parar
de reportar sucesso quando parte relevante do texto ficou de fora, em silencio.

## Locked decisions
- **D-...-1 (extracao)** — _gatilho SUPERSEDIDO por `D-2026-08-01-div-paragraph-translation-7`
  (uniao disjunta numa unica regex; o resto da decisao — lookahead, guarda de letra, timeout,
  rejeicao de parser HTML — continua valendo). Texto original preservado abaixo:_
  `ExtractTextBlocks` tenta `p|h1-6|li` (regex atual, intocada). SO quando
  isso devolve zero blocos PARA AQUELE CORPO, cai num fallback de **div-folha** (div sem `<div>`
  aninhado antes do fechamento — lookahead negativo por caractere, com o `RegexTimeoutMilliseconds`
  que toda regex de `HtmlUtility` ja carrega, csharp.md §4/ReDoS). Bloco de div so conta se tiver
  >= 1 letra Unicode (`char.IsLetter`) apos `StripHtmlTags` — filtra imagem/bullet/numero isolado
  sem dependencia nova. REJEITADO: parser de HTML real (AngleSharp/HtmlAgilityPack) — mudaria a
  arquitetura 100%-regex de `HtmlUtility` de bugfix pra rewrite de Utility inteira (mesmo racional
  de `coverage-90`: zero dependencia nova quando da pra resolver sem); nesting real medido no livro
  do usuario e raso (`calibreN` direto), lookahead basta. Fallback e por CHAMADA (=por capitulo),
  sem heuristica de "livro inteiro" — evita tocar capitulos que ja funcionam.
- **D-...-2 (baseline):** os 3 fixtures reais (`Wardley Maps`, `Righting software`, `Practice Makes
  Perfect`) nao tem `<div>` fora de `p|h1-6|li` hoje, entao o fallback nunca ativa neles — provado
  por teste de CARACTERIZACAO (fixa a contagem ATUAL antes da mudanca), nao so "codigo intocado".
- **D-...-3 (sinal de cobertura):** `TranslateBookAsync` passa a devolver
  `BookTranslationResult(string EpubPath, double CoveredTextRatio)` em vez de `string` cru.
  `CoveredTextRatio` = caracteres NAO-espaco extraidos em blocos / caracteres NAO-espaco do corpo
  inteiro (`StripHtmlTags` + `char.IsWhiteSpace`), agregado por capitulo dentro de
  `RebuildAllTranslatedChaptersAsync` (ja itera todo capitulo — zero I/O novo); 1.0 se o corpo for
  vazio. NUNCA lanca excecao por cobertura baixa (csharp.md §1: formato inesperado e fluxo
  esperado, nao erro). `ILogger` REJEITADO como veiculo: nenhum Manager/Engine do Core injeta
  logger hoje — seria infra nova fora de escopo de bugfix. `IProgress<BookTranslationProgress>`
  REJEITADO como veiculo unico: o parametro pode ser `null`, e o ponto inteiro do defeito e nunca
  ficar em silencio — sinal condicionado a parametro opcional repetiria a mesma classe de falha.
- **D-...-4 (impacto em src/TranslateReader/):** mudar o retorno obriga 1 ajuste MECANICO em
  `LibraryPageModel.TranslateBookAsync` (ler `result.EpubPath` em vez do `string` cru) — nao e UI
  nova. Decidir SE/COMO avisar visualmente o usuario sobre `CoveredTextRatio` baixo fica em
  `## Deferred to PR review` (decisao de produto/UX humana, fora do DoD automatizavel).
- **D-...-5 (fixture de teste):** nem o EPUB do usuario (protegido, caminho pessoal, obra com
  direitos) nem um `.epub` sintetico novo tipo `CreateOrphanCoverEpub` — o defeito vive inteiro em
  `HtmlUtility.ExtractTextBlocks(string bodyContent)`, que nao toca arquivo. Teste usa STRING HTML
  literal reproduzindo a forma calibre — sem I/O, sem EPUB, sem questao de copyright, mais estreito
  que o precedente do brief. Corpos sinteticos fixados em `## Notes`.
- **D-...-6 (bugfix comeca vermelho):** os testes de `## Notes` (Fixture A/B) e a caracterizacao
  dos 3 fixtures reais sao escritos ANTES do fallback existir — o de Fixture A fica vermelho (0
  blocos) ate o fallback ser implementado.
- **D-...-7 (selecao):** uniao disjunta numa UNICA regex com alternacao (branch `p|h[1-6]|li`
  primeiro, branch de div-folha depois, so casando div que NAO contem `p|h[1-6]|li`). Supersede SO
  o gatilho "zero blocos" de `D-...-1`. Teto 100% do corpo contra 44,3% do gatilho antigo.
- **D-...-8 (simetria):** `ReplaceTextBlocksInHtml` compartilha selecao E predicado com
  `ExtractTextBlocks`; sem isso a traducao dos divs seria cacheada e nunca escrita no EPUB.
- **D-...-9 (o DoD prova COMPORTAMENTO):** supersede os 7 comandos `Verify:` de
  `## Definition of Done` (os CRITERIOS nao mudam). Cada comando novo comeca pelo comando ANTIGO
  literal e segue, encadeado com `&&`, numa execucao real da suite com piso de testes casados.
  Item 8 e novo. Motivo: o DoD critic provou que os 7 antigos aprovavam um mutante que reintroduz
  o defeito da phase (`dotnet test` 9 falhas) e aprovavam ate fonte que nao compila.

## Canonical refs
- `.jdi/DECISIONS.md` D-2026-08-01-div-paragraph-translation-0 (diagnostico medido, numeros)
- `src/TranslateReader.Core/Utilities/HtmlUtility.cs`, `Business/Managers/TranslationManager.cs`,
  `Contracts/Managers/ITranslationManager.cs`
- `.claude/rules/csharp.md` §1 (excecao so pra erro), §2.1/§4 (regex com timeout, EPUB e input nao
  confiavel), §6 (bugfix comeca com teste vermelho, sem I/O em teste novo)

## Out of scope
- `HtmlUtility.ExtractParagraphs`/`TranslationManager.TranslateChapterAsync` (traducao interativa
  por paragrafo visivel) — MESMO defeito de classe (so `<p>`), fora do escopo confirmado pelo
  usuario nesta invocacao ("os DOIS defeitos": extracao de `TranslateBookAsync` + sinal). Registrado
  em `.jdi/todos.md`.
- Parser de HTML real (AngleSharp/HtmlAgilityPack) — D-...-1, decisao desta fase, nao "nunca".
- Aviso visual de cobertura baixa ao usuario — `## Deferred to PR review`.
- EPUB do usuario — nunca referenciado nem commitado, em teste ou em qualquer arquivo.

## Definition of Done

> **Endurecido na iter 2 por `D-2026-08-01-div-paragraph-translation-9`.** Os 7 `Verify:`
> originais mediam so FORMA (nome de teste presente, literal presente, contagem de atributo,
> escopo de diff) e o DoD critic provou que aprovavam um mutante que reintroduz o defeito da phase
> (`dotnet build` 0 erros, `dotnet test` 9 falhas, 7/7 gates exit 0) e aprovavam ate fonte que nao
> compila. Cada comando abaixo COMECA pelo comando antigo, literal — nenhuma checagem estrutural
> foi perdida — e continua, encadeado com `&&` (nunca `;`), numa execucao REAL da suite com piso
> de testes casados fixado antes da corrida. Filtro que casa zero teste reprova por construcao: o
> VSTest sai com exit 0 e sem a linha `Passed!`, por isso todo item exige o `grep -q "Passed!"`
> alem do encadeamento. `DOTNET_CLI_UI_LANGUAGE=en` e obrigatorio (a maquina imprime o sumario em
> pt-BR). Logs vao para `TestResults/`, ja em `.gitignore`.

### Auto-verifiable
- [ ] Teste de caracterizacao por fixture real fixa a contagem ATUAL de `ExtractTextBlocks` para
      `Wardley Maps`, `Righting software` e `Practice Makes Perfect` (nome contem
      `PreservesBaselineBlockCount`, 1 por fixture, 3 no total) — e os 3 passam de verdade
      **Verify:** `test $(grep -rho "PreservesBaselineBlockCount" test/TranslateReader.Tests/*.cs | wc -l) -eq 3 && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~PreservesBaselineBlockCount" > TestResults/dod1.log 2>&1 && grep -q "Passed!" TestResults/dod1.log && awk -v n=3 '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (k&&f+0==0&&p+0==n)?0:1}' TestResults/dod1.log`
      **Source:** CONTEXT
- [ ] Fallback de div-folha extrai a Fixture A de `## Notes` corretamente (ignora container, div de
      imagem e div sem letra) usando guarda de letra Unicode — provado rodando os 3 testes de
      selecao de div-folha, nao so pela presenca do nome
      **Verify:** `grep -q "ExtractTextBlocks_ForCalibreStyleBody_ExtractsLeafDivsWithLetters" test/TranslateReader.Tests/HtmlUtilityTests.cs && grep -q "IsLetter" src/TranslateReader.Core/Utilities/HtmlUtility.cs && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~ExtractTextBlocks_ForCalibreStyleBody_ExtractsLeafDivsWithLetters|FullyQualifiedName~ExtractTextBlocks_ForFullyCoveredCalibreBody_ExtractsTheSingleLeafDiv|FullyQualifiedName~ExtractTextBlocks_ForLeafDivWithoutAnyLetter_SkipsTheBlock" > TestResults/dod2.log 2>&1 && grep -q "Passed!" TestResults/dod2.log && awk -v n=3 '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (k&&f+0==0&&p+0>=n)?0:1}' TestResults/dod2.log`
      **Source:** CONTEXT
- [ ] Invariante estrutural (`D-...-7`): div que CONTEM `<p>`/`<h#>`/`<li>` nunca vira bloco — as
      duas fontes sao disjuntas por construcao, zero dupla contagem, zero regressao dos livros que
      hoje funcionam; e a simetria de `D-...-8` (round-trip extracao/substituicao) roda junto
      **Verify:** `grep -q "ExtractTextBlocks_WhenParagraphTagsPresent_IgnoresLeafDivs" test/TranslateReader.Tests/HtmlUtilityTests.cs && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~ExtractTextBlocks_WhenParagraphTagsPresent_IgnoresLeafDivs|FullyQualifiedName~ReplaceTextBlocksInHtml_ForCalibreStyleBody_WritesEachTranslationIntoItsOwnDiv|FullyQualifiedName~ReplaceTextBlocksInHtml_ForCalibreStyleBody_LeavesLetterlessDivsByteIdentical" > TestResults/dod3.log 2>&1 && grep -q "Passed!" TestResults/dod3.log && awk -v n=3 '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (k&&f+0==0&&p+0>=n)?0:1}' TestResults/dod3.log`
      **Source:** CONTEXT
- [ ] `TranslateBookAsync` devolve `BookTranslationResult.CoveredTextRatio` refletindo texto fora de
      qualquer bloco reconhecido (Fixture A < 1.0, Fixture B == 1.0 em `## Notes`) e NUNCA acima de
      1.0 nem com HTML malformado (W-1) — os 4 testes de ratio rodam e passam
      **Verify:** `grep -q "record BookTranslationResult" src/TranslateReader.Core/Models/BookTranslationResult.cs && grep -q "Task<BookTranslationResult> TranslateBookAsync" src/TranslateReader.Core/Contracts/Managers/ITranslationManager.cs && grep -q "TranslateBookAsync_CoveredTextRatio" test/TranslateReader.Tests/TranslationManagerTests.cs && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~TranslateBookAsync_CoveredTextRatio" > TestResults/dod4.log 2>&1 && grep -q "Passed!" TestResults/dod4.log && awk -v n=4 '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (k&&f+0==0&&p+0>=n)?0:1}' TestResults/dod4.log`
      **Source:** CONTEXT
- [ ] Cobertura zero/baixa nao lanca excecao — `TranslateBookAsync` completa e devolve resultado
      normalmente (csharp.md §1), verificado executando o teste
      **Verify:** `grep -q "TranslateBookAsync_WithZeroCoverageChapter_CompletesWithoutThrowing" test/TranslateReader.Tests/TranslationManagerTests.cs && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~TranslateBookAsync_WithZeroCoverageChapter_CompletesWithoutThrowing" > TestResults/dod5.log 2>&1 && grep -q "Passed!" TestResults/dod5.log && awk -v n=1 '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (k&&f+0==0&&p+0>=n)?0:1}' TestResults/dod5.log`
      **Source:** CONTEXT
- [ ] Toda `[GeneratedRegex]` nova/alterada em `HtmlUtility.cs` carrega `RegexTimeoutMilliseconds`
      (ReDoS, csharp.md §4 — EPUB e input nao confiavel) — conferido por reflexao em runtime e
      pelos 2 testes de corpo adversarial, alem da aritmetica do arquivo
      **Verify:** `F=src/TranslateReader.Core/Utilities/HtmlUtility.cs; N=$(grep -c "\[GeneratedRegex" "$F"); T=$(grep -c "RegexTimeoutMilliseconds" "$F"); D=$(grep -c "private const int RegexTimeoutMilliseconds" "$F"); test $((T-D)) -ge "$N" && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~EveryHtmlUtilityRegex_IsBoundedByAMatchTimeout|FullyQualifiedName~ExtractTextBlocks_ForLargeCalibreBody_ExtractsEveryBlockUnderOneSecond|FullyQualifiedName~ExtractTextBlocks_ForDegenerateUnclosedDivs_ReturnsOrTimesOutWithoutHanging" > TestResults/dod6.log 2>&1 && grep -q "Passed!" TestResults/dod6.log && awk -v n=3 '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (k&&f+0==0&&p+0>=n)?0:1}' TestResults/dod6.log`
      **Source:** CONTEXT
- [ ] `src/TranslateReader/` so muda em `LibraryPageModel.cs` (ajuste mecanico do retorno), sem
      popup/alert/UI nova (D-...-4) — e o projeto MAUI compila com o novo tipo de retorno
      **Verify:** `test "$(git diff --name-only main -- src/TranslateReader/ | tr '\n' ',')" = "src/TranslateReader/PageModels/LibraryPageModel.cs," && test $(git diff main -- src/TranslateReader/PageModels/LibraryPageModel.cs | grep -cE "^\+.*(DisplayAlert\(|ShowPopupAsync|new .*Popup)") -eq 0 && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0 > TestResults/dod7.log 2>&1 && grep -qE "^ *0 Error\(s\)" TestResults/dod7.log`
      **Source:** CONTEXT
- [ ] A suite INTEIRA (sem filtro) passa com piso de testes casados fixado antes da corrida:
      `Failed: 0`, `Passed: >= 320`, `Total: >= 322` (baseline `9c56c36` era 319/321; +1 com o teste
      de clamp do W-1). E o item que reprova o mutante do critico e qualquer regressao futura do
      seletor (`D-...-9`)
      **Verify:** `mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release > TestResults/dod8.log 2>&1 && grep -q "Passed!" TestResults/dod8.log && awk -v pn=320 -v tn=322 '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1);if($i=="Total:")t=$(i+1)}} END{exit (k&&f+0==0&&p+0>=pn&&t+0>=tn)?0:1}' TestResults/dod8.log`
      **Source:** CONTEXT (D-2026-08-01-div-paragraph-translation-9)

### Manual
- _(none — dod=auto_only; itens humanos foram para `## Deferred to PR review`, nao viraram linha
  Manual)_

## Deferred to PR review
- Decisao de produto/UX: se/como avisar visualmente o usuario quando `CoveredTextRatio` for baixo
  (toast, badge, texto no popup de traducao) — D-...-4, decisao humana de wording/threshold.
- Confirmacao de que o Quality Gate do SonarCloud nao acusa issue nova nos arquivos tocados — so
  existe apos push+CI (mesmo limite ja documentado em D-2026-07-30-sonar-zero-issues-6 e
  D-2026-07-31-coverage-90-6).
- Leitura humana: as Fixtures A/B de `## Notes` reproduzem fielmente a forma calibre real (nao so
  uma aproximacao conveniente pro teste passar).

## Notes
**Fixture A** (leaf-div + guarda de letra + cobertura parcial), corpo passado a `ExtractTextBlocks`:
```html
<div class="calibre1">
<div class="calibre2">First calibre paragraph with real text.</div>
<div class="calibre2">Second calibre paragraph with more text.</div>
<div class="calibre3"><img src="fig1.png"/></div>
<div class="calibre2">&#8226;</div>
<div class="calibre2">Third paragraph, letters only matter here.</div>
</div>
```
Esperado: 3 blocos (so os `calibre2` com letra). `calibre1` nunca casa sozinho (contem `<div>`
aninhado); `calibre3` casa mas nao tem letra (so `<img>`) — filtrado; o bullet isolado (`&#8226;`)
casa mas nao tem letra — filtrado, e conta como caractere nao-espaco NAO coberto, entao
`CoveredTextRatio` < 1.0 para este corpo.

**Fixture B** (cobertura total): `<div class="calibre2">Only paragraph, fully covered by the leaf
div.</div>` — 1 bloco, `CoveredTextRatio` == 1.0 (nada de nao-espaco fora do bloco).

Residuo conhecido e aceito: `StripHtmlTags` nao remove TEXTO dentro de `<style>` no body (so as
tags) — impacto nulo nos 3 fixtures reais e no corpo de EPUB tipico (`<style>` fica no `<head>`,
ja excluido por `ExtractBodyContent`).

`ExtractBodyContent`/`StripHtmlTags` ja sao `public static` — reusar direto, sem duplicar logica.
Caracterizacao dos 3 fixtures reais fica ao lado do padrao ja usado por `ParsingEngineTests.cs`
(`FindEpub`, I/O real ja autorizado so pra fixture de EPUB); `HtmlUtilityTests.cs` continua 100%
sem I/O (strings literais).

Churn mecanico esperado (nao e item de DoD, e consequencia previsivel de D-...-3): os ~13 testes
`TranslateBookAsync_*` ja existentes em `TranslationManagerTests.cs` passam a ler `result.EpubPath`
em vez de `result` (mudanca de tipo de retorno).

Ordem sugerida ao planner: (1) fallback de div-folha + guarda de letra + testes de
`HtmlUtilityTests.cs` (Fixture A/B, teste anti-regressao p/h/li); (2) caracterizacao dos 3 fixtures
reais; (3) `BookTranslationResult` + `CoveredTextRatio` em `TranslationManager`/`ITranslationManager`
+ atualizacao dos ~13 testes existentes + 2 testes novos; (4) ajuste mecanico de
`LibraryPageModel.cs` por ultimo, sozinho, pra manter o diff de `src/TranslateReader/` auditavel
num commit so.
