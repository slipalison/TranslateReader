# Phase 18: Traducao interativa cega a paragrafo em `<div>` (leitura) — Plan  (slug: div-paragraph-reading)

## Goal
Traduzir paragrafo visivel de EPUB de calibre (`<div class="calibreN">`) na leitura interativa. Hoje
as 3 funcoes de `translation.js` fazem `querySelectorAll('p')` → zero paragrafo → o botao falha em
silencio (`ReaderPage.xaml.cs:296`). Mesmo defeito de classe de `div-paragraph-translation`, outro
runtime.

## Aritmetica do DoD item 6 — MEDIDA, o piso FECHA (armadilha desarmada)

Risco levantado: remover `ExtractParagraphs` (D-...-4) tiraria testes do total e tornaria o piso
`Passed >= 321 / Total >= 323` inalcancavel. **Medicao desta sessao:**

| Fonte | Valor |
|---|---|
| `grep -rn "ExtractParagraphs\|ParagraphRegex" test/` | **0 hits** — nenhum teste chama a API direto |
| Baseline `main` (D-2026-08-01-div-paragraph-translation-9 item 8) | Failed 0, Passed 320, Total 322 |
| Testes REMOVIDOS pela remocao da API | **0** |
| Testes ADICIONADOS (T-4, nome fixado pelo `Verify:` do item 5) | **+1** |
| Projecao | Passed **321**, Total **323** |

`ExtractParagraphs` tem 1 chamador so (`TranslationManager.cs:244`) e e coberto INDIRETAMENTE pelos
6 `TranslateChapterAsync_*` ([Fact], todos com corpo so `<p>`), que seguem existindo e passando com
`ExtractTextBlocks` (ramo `p|h|li` = mesmo filtro `!IsNullOrWhiteSpace`). O unico teste que QUEBRA e
`HtmlInjectionTests.cs:304` (`Assert.Equal(8, factories.Count)`), que e ajuste de literal, nao
delecao — contagem de testes inalterada.

**Consequencia 1:** NAO existe decisao nova nesta phase. O doer NAO cria
`D-2026-08-01-div-paragraph-reading-6` nem toca no CONTEXT.md. Se achar que precisa, e sinal de que
algo mais fundo mudou → task `blocked`, nao decisao improvisada.
**Consequencia 2:** a margem e ZERO (321/323 = exatamente o piso). **Deletar, renomear ou pular
qualquer teste existente reprova o item 6.** So e permitido SOMAR.
**Contingencia (so se a corrida real de T-6 vier abaixo do piso):** caminho append-only — arquivo
NOVO `.jdi/decisions/D-2026-08-01-div-paragraph-reading-6.md` supersedendo o piso, depois a linha do
CONTEXT.md, depois `npx -y jdi-cli render`. NUNCA reescrever decisao existente, NUNCA editar
`.jdi/DECISIONS.md` nem `.jdi/todos.md` (views geradas).

## Locked decisions
- `D-...-2` defeito 100% JS; a paridade que importa e entre as 3 funcoes JS, nao C#↔JS.
- `D-...-3` `_translatableCandidates(pg)` interno, `querySelectorAll('p,h1..h6,li,div')`, div so se
  folha + `/\p{L}/u`; harness ganha selector group por virgula e nada mais (`:has()` REJEITADO).
- `D-...-4` `ExtractParagraphs`/`ParagraphRegex` REMOVIDOS, `TranslateChapterAsync` usa
  `ExtractTextBlocks`; membro do contrato `ITranslationManager` PERMANECE.
- `D-...-5` `console.warn` quando ha texto e zero candidatos; `ReaderPage.xaml.cs`/`ReaderPageModel.cs`
  e o modo Scroll intocados.
- `D-1`/`D-2`/`D-6` The Method, boundary `4285f25`, 90% em codigo alterado · `csharp.md` §6 (bugfix
  comeca VERMELHO) manda na ordem das tasks.

## Tasks

### Wave 1 (parallel-eligible — cadeia JS e cadeia C# tocam arquivos disjuntos)

#### T-1: Selector group por virgula no harness, em ORDEM DE DOCUMENTO
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `test/js/harness.js`, `test/js/harness.test.js` (novo)
- **Acceptance:**
  - `parseSelector` vira lista; `matchDescendants` parseia a lista UMA vez e faz UMA passada por
    `descendantElements(root)`, empurrando o elemento se casar QUALQUER parte. **Proibido** o laco
    externo por seletor com concatenacao — devolveria todos os `p` e depois todos os `div`,
    quebrando o pareamento indice↔elemento de que as 3 funcoes de producao dependem.
  - Teste que PROVA ordem: `querySelectorAll('div, p')` sobre `<p>a</p><div>b</div><p>c</p>` retorna
    `[P, DIV, P]` (nao `[P, P, DIV]`); elemento que casa 2 partes aparece 1x so.
  - Split de virgula ignora virgula DENTRO de `[...]`: `[data-chapter-href="a,b.xhtml"]` continua 1
    seletor. `scroll.js:32` monta esse seletor com href vindo do EPUB — input nao confiavel
    (`csharp.md` §4); split ingenuo por `,` vira selecao errada silenciosa.
  - Seletor sem virgula com comportamento byte-identico ao atual; zero recurso CSS novo
    (`:not`/`:has`/combinadores seguem fora); zero dependencia npm.
  - >= 3 testes novos em `harness.test.js` (auto-descoberto por `test/js/index.js`).
  - `node --test test/js/` inteiro VERDE — `bridge`/`paginated`/`scroll`/`translation` sem 1 edicao.
    Baseline `B_js` (`# pass` da suite JS em `main`) MEDIDO e registrado no SUMMARY.
- **Dependencies:** none
- **Test:** `test/js/harness.test.js` + suite JS completa como regressao
- **Commit:** `test(div-paragraph-reading): support selector groups in the JS DOM harness`
- **Status:** completed
- **DoD:** pre-requisito dos itens 2 e 3

#### T-4: Teste VERMELHO do caminho C# (`TranslateChapterAsync` em corpo calibre)
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `test/TranslateReader.Tests/TranslationManagerTests.cs`
- **Acceptance:**
  - `TranslateChapterAsync_ForCalibreStyleBody_TranslatesLeafDivParagraphs` (nome EXATO — o
    `Verify:` do item 5 faz `grep -q` dele), reusando `CalibreFixtures.PartiallyCoveredBody` (Notes
    do CONTEXT): espera **3** paragrafos na ordem; wrapper `calibre1` (nao-folha), div so com
    `<img>` e div so com `&#8226;` (sem letra) ficam de fora.
  - NSubstitute sobre `Contracts/`, padrao dos 6 `TranslateChapterAsync_*` ja no arquivo; zero I/O,
    zero SQLite real (`csharp.md` §6).
  - Roda VERMELHO agora: com `ExtractParagraphs` o corpo calibre rende 0 paragrafos e o
    `IAsyncEnumerable` nao itera nada. Transcript do `dotnet test --filter
    "FullyQualifiedName~TranslateChapterAsync"` com `Failed: 1` no SUMMARY ANTES de T-5.
  - Os 6 `TranslateChapterAsync_*` existentes ficam intocados (margem zero).
- **Dependencies:** none
- **Test:** o proprio (xUnit, fixture literal)
- **Commit:** `test(div-paragraph-reading): cover calibre body in TranslateChapterAsync`
- **Status:** completed
- **DoD:** item 5

### Wave 2 (parallel-eligible — `test/js/translation.test.js` vs Core + `HtmlInjectionTests.cs`)

#### T-2: Testes VERMELHOS de calibre em `translation.test.js`
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `test/js/translation.test.js`
- **Acceptance:**
  - >= 6 testes novos, >= 5 com a palavra `calibre` no nome e declarados com `test('` na COLUNA 0
    (o `Verify:` do item 2 e `grep -cE "^test\('.*calibre"`). Baseline de `main` = 13 testes → o
    item 3 exige `# pass >= 17`.
  - Cobrem, sobre corpo estilo `CalibreFixtures.PartiallyCoveredBody` paginado por `paginate()`:
    (a) `getVisibleParagraphs` devolve os 3 div-folha com letra; (b) **ordem de documento com `p` e
    div-folha INTERCALADOS** — `<p>um</p><div>dois</div><p>tres</p>` → indices 0,1,2 na ordem do
    DOM; (c) `applyTranslations` escreve no div-folha certo pelo indice devolvido em (b);
    (d) round-trip apply→clear num div-folha: `textContent` volta ao original E
    `dataset.original === undefined`; (e) wrapper nao-folha e div so com `<img>`/`&#8226;` nunca
    viram candidato; (f) `console.warn` de `D-...-5` dispara com pager `<span>so span</span>`
    (texto sim, candidato nao) e NAO dispara quando ha candidato — o harness ja captura console
    (`env.logged('warn', ...)`), nenhuma capacidade nova necessaria.
  - Transcript VERMELHO no SUMMARY (`node --test test/js/translation.test.js`, `# fail >= 6`, nomes
    visiveis) ANTES de T-3 — `csharp.md` §6 vale para o JS desta phase.
  - Os 13 testes existentes ficam intocados (o item 3 compara com `git show main:`).
- **Dependencies:** T-1
- **Test:** os proprios (`node --test`, zero dependencia)
- **Commit:** `test(div-paragraph-reading): add failing calibre cases for paragraph selection`
- **Status:** completed
- **DoD:** itens 2 e 3 (metade vermelha)

#### T-5: Remover o caminho C# morto (`ExtractParagraphs`/`ParagraphRegex`)
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Utilities/HtmlUtility.cs`,
  `src/TranslateReader.Core/Business/Managers/TranslationManager.cs`,
  `test/TranslateReader.Tests/HtmlInjectionTests.cs`
- **Acceptance:**
  - `ExtractParagraphs` e a fabrica `[GeneratedRegex] ParagraphRegex()` DELETADAS (zero ocorrencia
    das duas strings em `HtmlUtility.cs`); `TranslationManager.cs:244` passa a
    `HtmlUtility.ExtractTextBlocks(bodyContent)` — 1 linha, resto do metodo intocado.
  - **Remocao ACOMPANHADA da API, nao afrouxamento de teste:** zero teste deletado ou renomeado
    (`grep -rn "ExtractParagraphs\|ParagraphRegex" test/` = 0 hits hoje, e a razao de a remocao nao
    custar teste nenhum). Se aparecer teste referenciando as duas APIs, task `blocked`.
  - **Acoplamento medido:** `HtmlInjectionTests.cs:304` faz `Assert.Equal(8, factories.Count)`
    (reflexao sobre as fabricas de `Regex` de `HtmlUtility`) e QUEBRA com a remocao — ajustar o
    literal para `7` no MESMO commit; trocar `Assert.Equal` por `>=` e PROIBIDO. Segue 1 teste, a
    aritmetica 321/323 nao muda.
  - T-4 fica VERDE; os 6 `TranslateChapterAsync_*` continuam verdes sem edicao.
  - Cobertura D-6: a mudanca e DELECAO + 1 linha alterada, exercida pelos 7 testes do filtro
    `~TranslateChapterAsync`; provar com `dotnet test --collect:"XPlat Code Coverage"` e reportar no
    SUMMARY o % de `TranslateChapterAsync` (>= 90% linha+branch).
  - Zero warning novo de analisador; `dotnet format` antes do commit.
- **Dependencies:** T-4
- **Test:** T-4 + os 6 existentes + `EveryHtmlUtilityRegex_IsBoundedByAMatchTimeout` na mesma corrida
- **Commit:** `fix(div-paragraph-reading): extract text blocks in TranslateChapterAsync`
- **Status:** completed
- **DoD:** itens 4 e 5

### Wave 3

#### T-3: `_translatableCandidates` — fonte unica de selecao em `translation.js`
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader/Resources/Raw/wwwroot/js/translation.js`
- **Acceptance:**
  - `function _translatableCandidates(pg)` interna (NAO em `window`, padrao de `_stepW`):
    `pg.querySelectorAll('p, h1, h2, h3, h4, h5, h6, li, div')` preservando a ordem devolvida; `DIV`
    entra so se `el.querySelector('div, p, h1, h2, h3, h4, h5, h6, li')` for `null` E
    `(el.dataset.original ?? el.textContent)` casar `/\p{L}/u`; demais tags mantem o filtro de texto
    nao-vazio atual.
  - As 3 funcoes chamam SO esse helper. `clearTranslations` deixa de usar
    `querySelectorAll('p[data-original]')` e passa a filtrar `_translatableCandidates(pg)` por
    `dataset.original !== undefined` — dessincronia de indice deixa de ser possivel por construcao.
  - Zero ocorrencia de `querySelectorAll('p')` / `querySelectorAll('p[data-original]')`;
    `_translatableCandidates(` aparece >= 4x (item 1).
  - `console.warn` (`D-...-5`) so quando `pg` existe, `pg.textContent.trim()` nao e vazio e a lista
    vem vazia — mesmo canal de `paginated.js`; nada de `alert`/UI.
  - T-2 fica 100% VERDE e `node --test test/js/` inteiro tambem (`# fail 0`, `# pass >= B_js + 9`).
  - Cobertura D-6 fora do coverlet: `node --test --experimental-test-coverage test/js/`, reportando
    no SUMMARY a linha de `translation.js` com >= 90% de linha e branch.
  - Sem `:has()`, sem combinador, sem dependencia — o arquivo segue compativel com WebView2/
    WKWebView/Android WebView como o resto de `wwwroot/js`.
- **Dependencies:** T-2
- **Test:** `test/js/translation.test.js` (T-2) + suite JS completa
- **Commit:** `fix(div-paragraph-reading): select calibre leaf divs as translatable paragraphs`
- **Status:** completed
- **DoD:** itens 1, 2 e 3

### Wave 4

#### T-6: Corrida final dos gates + escopo de diff
- **Specialist:** jdi-doer-translatereader
- **Files modified:** nenhum arquivo de codigo (so `TestResults/*.log` e o SUMMARY)
- **Acceptance:**
  - `node --test test/js/`: `# fail 0`, `# pass >= B_js + 9` (>= 3 de T-1 + >= 6 de T-2).
  - `dotnet test ... -c Release` completo: `Failed: 0`, `Passed >= 321`, `Total >= 323`. Abaixo
    disso → task `blocked` + contingencia append-only; NUNCA deletar teste para fechar conta.
  - `git diff --name-only main -- src/TranslateReader/ ':(exclude)src/TranslateReader/Resources/Raw/wwwroot/js/'`
    VAZIO: `Pages/`, `PageModels/`, `Platforms/` intocados (item 7). O Core muda e nao casa esse
    pathspec.
  - `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f
    net10.0-windows10.0.19041.0` verde (unico detector de consumidor esquecido — `src/TranslateReader/`
    nao tem teste, D-2026-07-30-regression-suite-2).
  - Os 7 `Verify:` do CONTEXT rodados um a um, saida colada no SUMMARY.
  - `.gitignore` tem alteracao local NAO commitada do usuario — fora de todo commit da phase.
- **Dependencies:** T-3, T-5
- **Test:** suite JS + suite C# + build do TFM Windows
- **Commit:** nenhum se nao houver diff versionado (`TestResults/` e ignorado)
- **Status:** completed
- **DoD:** itens 3, 6 e 7

## Riscos nomeados
- **R1 — ordem do `querySelectorAll` composto.** No DOM real a ordem e de DOCUMENTO, nao a dos
  seletores; e isso que mantem o indice pareado entre as 3 funcoes. Se o harness nao reproduzir
  isso (T-1), o teste (b) de T-2 passa verde por acidente e a WebView real quebra.
- **R2 — margem zero no item 6.** 321/323 e o piso exato; qualquer teste perdido reprova. So somar.
- **R3 — `<div>` nao-folha virando candidato.** `_pager` recebe os filhos de `chapter-container`
  (`paginated.js:35`) e `BuildContinuousScrollHtml` embrulha em `div.chapter-content`: sem a guarda
  de folha o capitulo inteiro viraria 1 paragrafo gigante. Coberto por T-2 (e).
- **R4 — regressao silenciosa no harness.** `parseSelector` afeta `bridge`/`paginated`/`scroll`/
  `translation`; por isso o gate e `node --test test/js/` inteiro, nunca so um arquivo.

## Learnings aplicados
- Gate que so descreve a FORMA do arquivo aprova codigo que nao roda → 5 dos 7 itens executam suite;
  T-6 le `# fail`/`Passed!`, nao exit code.
- `dotnet test --filter` que casa ZERO teste sai 0 → item 5 exige piso de testes CASADOS (>= 7).
- Quem seleciona para ler e quem seleciona para escrever tem que ser a MESMA regra → as 3 funcoes JS
  compartilham `_translatableCandidates` (T-3), nao 3 seletores parecidos.
- Piso fixado ANTES da corrida → `B_js` medido em T-1, `321/323` calculado neste PLAN.

## Execution
- Total tasks: 6 · Waves: 4 · Speedup paralelo: ~1,5x (T-1∥T-4 e T-2∥T-5)
- DoD 7/7: 1:T-3 · 2:T-1+T-2+T-3 · 3:T-1+T-2+T-3+T-6 · 4:T-5 · 5:T-4+T-5 · 6:T-6 · 7:T-6
- Decisao nova: NENHUMA. `.jdi/decisions/` e `.jdi/todos/` so mudam se surgir scope creep → arquivo
  NOVO em `.jdi/todos/` + `npx -y jdi-cli render`.

## Files modified (all tasks)
- `test/js/harness.js`, `test/js/harness.test.js` (novo), `test/js/translation.test.js`
- `src/TranslateReader/Resources/Raw/wwwroot/js/translation.js`
- `src/TranslateReader.Core/Utilities/HtmlUtility.cs`,
  `src/TranslateReader.Core/Business/Managers/TranslationManager.cs`
- `test/TranslateReader.Tests/TranslationManagerTests.cs`, `HtmlInjectionTests.cs`

## Test requirements
- JS: `node --test test/js/` — `# fail 0`, `# pass >= B_js + 9`; cobertura com
  `--experimental-test-coverage` (>= 90% em `translation.js`, D-6 fora do coverlet)
- C#: `DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj
  -c Release` — `Failed: 0`, `Passed >= 321`, `Total >= 323`; `--collect:"XPlat Code Coverage"`
  (>= 90% linha+branch no codigo alterado)
- Build: `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0`
- Zero I/O em teste novo (JS e C#); EPUB do usuario nunca referenciado
