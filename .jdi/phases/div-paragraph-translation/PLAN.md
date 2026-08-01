# Phase 17: Traducao cega a paragrafo em `<div>` (EPUB de calibre) — Plan  (slug: div-paragraph-translation)

## Goal
Traduzir paragrafos em `<div>` (EPUBs calibre; hoje `ExtractTextBlocks` so ve `p|h1-6|li`) e parar
de reportar sucesso quando parte relevante do texto ficou de fora, em silencio.

## Correcao bloqueante — o gatilho de `D-...-1` entrega o livro 56% intraduzido

Medicao (D-...-0 + tabela do usuario; palavras de corpo do livro-origem, 53 documentos):

| Simbolo | Fonte | Palavras |
|---|---|---|
| B | blocos `p\|h1-6\|li` | 11.114 |
| D | divs-folha | 88.042 |
| C | corpo total | 88.107 |

1. **B + D = 99.156 > C = 88.107 → sobreposicao >= 11.049 palavras**: 99,4% do texto de `p|h|li`
   ja vive DENTRO de um div-folha. Uniao ingenua = 11.049 palavras traduzidas 2x (+12,6% de
   chamadas ao LLM) e — pior — a lista de extracao deixa de casar 1:1 com a varredura de
   `ReplaceTextBlocksInHtml` (`translations[index++]`): traducao escrita no bloco errado.
2. O "11,2%" de `D-...-0` usou denominador 99.156 (ja com a dupla contagem). Com o corpo real
   (88.107) a cobertura de hoje e **12,6%** — bate com a tabela do usuario. A aritmetica dos dois
   lados fecha; a sobreposicao ja estava latente nos numeros originais.
3. Tetos por regra: gatilho locked (fallback so com ZERO blocos) = 39.051 = **44,3%** (33/53
   documentos seguem em ingles → o usuario reabre o mesmo bug); (a) max por corpo = 88.042 =
   99,93%; (b) uniao disjunta <= 88.107 = 100%. Delta (b)−(a) = 65 palavras = 0,07%.

**Escolha: (b) — uniao disjunta, implementada como UMA regex, nao como dedup pos-hoc.** O branch
de div so casa div-folha que NAO contem `p|h[1-6]|li`; as duas fontes ficam disjuntas por
construcao, em ordem de documento, num unico `Matches`. (a) foi rejeitada por dois motivos, nao
pelos 0,07%: exigiria recomputar a decisao "quem rende mais texto" dentro de
`ReplaceTextBlocksInHtml` (que recebe o html inteiro, nao o body) — divergencia entre as duas
passagens = traducao no paragrafo errado, falha silenciosa pior que a original; e um
`<div class="section">` com varios `<p>` dentro (forma dos 3 fixtures reais) venceria a comparacao
e viraria UM bloco gigante, regredindo granularidade, cache e tamanho de prompt.

**Segundo defeito estrutural (nao estava no CONTEXT):** `ReplaceTextBlocksInHtml` usa
`TextBlockRegex` sozinha. Corrigir so a extracao faz o motor traduzir e cachear os divs e **nunca
escrever nada no EPUB** — a phase entregaria o livro igualmente em ingles. Extracao e substituicao
tem de compartilhar selecao E predicado de filtro. Vira `D-...-8`.

## Locked decisions
- `D-...-1` (div-folha via lookahead, letter guard `char.IsLetter`, `RegexTimeoutMilliseconds`,
  parser HTML REJEITADO) — vale integralmente, **exceto o gatilho "zero blocos"**, supersedido por
  `D-...-7` (T-1, caminho append-only).
- `D-...-2` caracterizacao dos 3 fixtures reais · `D-...-3` `BookTranslationResult` +
  `CoveredTextRatio` · `D-...-4` 1 ajuste mecanico em `LibraryPageModel` · `D-...-5` fixture =
  string HTML literal, sem I/O, EPUB do usuario nunca referenciado · `D-...-6` bugfix comeca
  vermelho · `D-1`/`D-2`/`D-6` The Method, boundary `4285f25`, 90% em codigo novo.

## Tasks

### Wave 1

#### T-1: Anexar `D-...-7`/`D-...-8` e realinhar o CONTEXT.md (append-only)
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `.jdi/DECISIONS.md`, `.jdi/phases/div-paragraph-translation/CONTEXT.md`
- **Acceptance:**
  - `D-...-7` anexada com a aritmetica acima: regra de selecao = uniao disjunta por alternacao
    unica; supersede SO o gatilho de `D-...-1`; lookahead, letter guard, timeout e a rejeicao de
    AngleSharp/HtmlAgilityPack seguem valendo.
  - `D-...-8` anexada: `ReplaceTextBlocksInHtml` compartilha selecao e predicado com
    `ExtractTextBlocks`, senao a traducao e cacheada e nunca chega ao EPUB.
  - `git diff .jdi/DECISIONS.md | grep -c '^-[^-]'` == 0 (D-...-1..-6 intactas — auditoria da
    truncagem registrada em STATE.md).
  - CONTEXT.md: item 3 do DoD reescrito para o invariante real ("div que contem `p|h#|li` nunca
    vira bloco — zero dupla contagem"), **mantendo o nome de teste**
    `ExtractTextBlocks_WhenParagraphTagsPresent_IgnoresLeafDivs`; `D-...-1` ganha nota de
    supersessao. Nenhum dos 7 `Verify:` muda de comando ou de nome de teste.
  - `.gitignore` (alteracao local do usuario) fora do commit.
- **Dependencies:** none
- **Test:** n/a (doc) — provado por diff sem delecao + `Verify:` dos 7 itens ainda validos
- **Status:** pending

### Wave 2 (parallel-eligible)

#### T-2: Testes RED da selecao (`HtmlUtilityTests.cs`, zero I/O)
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `test/TranslateReader.Tests/HtmlUtilityTests.cs`
- **Acceptance:**
  - `ExtractTextBlocks_ForCalibreStyleBody_ExtractsLeafDivsWithLetters`: Fixture A → exatamente 3
    blocos na ordem; container `calibre1`, div de `<img>` e div so com `&#8226;` ficam de fora.
  - `ExtractTextBlocks_WhenParagraphTagsPresent_IgnoresLeafDivs`: `<div class="s"><p>A</p><p>B</p>
    </div>` → 2 blocos (`A`,`B`); assercao explicita de que nenhum texto aparece 2x.
  - Round-trip (`D-...-8`): `ReplaceTextBlocksInHtml(FixtureA, ["T1","T2","T3"])` escreve as 3
    traducoes nos 3 divs certos, na ordem, e deixa o div de imagem e o do bullet byte-identicos.
  - ReDoS (csharp.md §4): corpo com >= 5.000 div-folha (~250 KB, acima do maior capitulo real
    ~5.300 palavras) extrai correto em < 1s; corpo degenerado com milhares de `<div` sem
    fechamento retorna ou lanca `RegexMatchTimeoutException` em < 5s — nunca pendura.
  - Edge: `<div/>`/`<br/>` no corpo nao geram bloco que engula os divs seguintes.
  - Transcript VERMELHO no SUMMARY (nomes + `Failed:` >= 5) antes de T-4 (`D-...-6`); nenhum teste
    existente editado, renomeado ou removido.
- **Dependencies:** T-1
- **Test:** os proprios (xUnit, strings literais — `D-...-5`)
- **Status:** pending

#### T-3: Caracterizacao dos 3 fixtures reais (baseline, verde desde o inicio)
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `test/TranslateReader.Tests/ExtractTextBlocksBaselineTests.cs` (novo)
- **Acceptance:**
  - 3 testes `*_PreservesBaselineBlockCount`, 1 por fixture (`Wardley Maps`, `Righting software`,
    `Practice Makes Perfect`), padrao `FindEpub` de `ParsingEngineTests.cs` (unica excecao de I/O
    ja autorizada); contagem MEDIDA pelo doer e fixada como literal.
  - Cada teste fixa tambem a soma de chars nao-espaco extraidos, nao so a contagem de blocos —
    contagem igual com texto diferente nao pode passar despercebida.
  - Verdes antes de T-4; o EPUB do usuario nao e citado em nenhum arquivo.
- **Dependencies:** T-1
- **Test:** os proprios
- **Status:** pending

### Wave 3

#### T-4: Selecao unica em `HtmlUtility` — extracao e substituicao simetricas
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Utilities/HtmlUtility.cs`,
  `test/TranslateReader.Tests/HtmlInjectionTests.cs` (so se o numero de fabricas de `Regex` mudar)
- **Acceptance:**
  - `TextBlockRegex` ganha alternativa de div-folha: `<div\b[^>]*>` cujo conteudo nao contem
    `<div`, `<p`, `<h[1-6]` nem `<li` (lookahead negativo por caractere), `IgnoreCase|Singleline`
    + `RegexTimeoutMilliseconds`; branch `p|h|li` PRIMEIRO; grupos nomeados para o Replace resolver
    a tag de fechamento.
  - Predicado de filtro unico, compartilhado por `ExtractTextBlocks` e `ReplaceTextBlocksInHtml`:
    branch de div exige >= 1 `char.IsLetter` apos `StripHtmlTags`; branch `p|h|li` mantem o filtro
    de whitespace ATUAL (endurece-lo mudaria a baseline de T-3). Filtro assimetrico entre as duas
    passagens desalinha `index++` — e a falha que T-2 (round-trip) mata.
  - T-2 fica 100% verde; T-3 mantem exatamente os mesmos numeros. Se algum fixture mudar, task
    `blocked` + reporte — nunca afrouxar a caracterizacao.
  - Zero `[GeneratedRegex]` sem `RegexTimeoutMilliseconds` (DoD 6);
    `EveryHtmlUtilityRegex_IsBoundedByAMatchTimeout` continua com contagem EXATA (8 → 9 se surgir
    fabrica nova; jamais trocar `Assert.Equal` por `>=`).
  - Sem dependencia nova (`D-...-1`); sem warning de analisador novo alem do `SYSLIB1044` ja
    waivered (backreference `\1` mantida; comentario do waiver atualizado).
- **Dependencies:** T-2, T-3
- **Test:** T-2 + T-3 verdes na mesma corrida
- **Status:** pending

### Wave 4

#### T-5: `BookTranslationResult` + `CoveredTextRatio` (`D-...-3`)
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Models/BookTranslationResult.cs` (novo),
  `src/TranslateReader.Core/Contracts/Managers/ITranslationManager.cs`,
  `src/TranslateReader.Core/Business/Managers/TranslationManager.cs`,
  `src/TranslateReader.Core/Utilities/HtmlUtility.cs`,
  `test/TranslateReader.Tests/TranslationManagerTests.cs`
- **Acceptance:**
  - `record BookTranslationResult(string EpubPath, double CoveredTextRatio)`;
    `Task<BookTranslationResult> TranslateBookAsync(...)` no contrato (com `<summary>`) e no Manager.
  - Agregacao dentro de `RebuildAllTranslatedChaptersAsync` (ja itera todo capitulo — zero I/O
    novo): `covered += chars nao-espaco dos blocos`, `total += chars nao-espaco do corpo`;
    `total == 0 → 1.0`.
  - A contagem de chars vive em `HtmlUtility` (`CountTextChars`, HTML e responsabilidade dela —
    CLAUDE.md); o Manager so acumula e divide, sem if/else de regra de negocio.
  - NUNCA lanca por cobertura baixa (csharp.md §1); `IProgress`, cache e job seguem intocados.
  - Unico assert de retorno existente (`TranslationManagerTests.cs:334`) passa a ler
    `result.EpubPath`; os outros 11 `TranslateBookAsync` nao leem retorno e ficam intocados.
- **Dependencies:** T-4
- **Test:** suite existente compila e passa; ramos do ratio cobertos em T-6
- **Status:** pending

### Wave 5 (parallel-eligible)

#### T-6: Testes do sinal de cobertura (DoD 4 e 5)
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `test/TranslateReader.Tests/TranslationManagerTests.cs`
- **Acceptance:**
  - `TranslateBookAsync_CoveredTextRatio_IsBelowOneWhenTextEscapesEveryBlock` (Fixture A como
    conteudo de capitulo → `< 1.0`) e `..._CoveredTextRatio_IsOneWhenEveryCharacterIsCovered`
    (Fixture B → `== 1.0`).
  - `TranslateBookAsync_WithZeroCoverageChapter_CompletesWithoutThrowing`: capitulo so com `<img>`
    → retorna normalmente, `DeleteJobAsync` chamado, ratio `0.0`, zero excecao.
  - Ramo corpo vazio → `1.0` coberto por teste proprio.
  - >= 90% de linha e branch no codigo novo de T-5 (D-6), medido com
    `--collect:"XPlat Code Coverage"`.
- **Dependencies:** T-5
- **Test:** os proprios (NSubstitute sobre `Contracts/`, zero I/O)
- **Status:** pending

#### T-7: Ajuste mecanico do consumidor MAUI + corrida final (DoD 7)
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader/PageModels/LibraryPageModel.cs`
- **Acceptance:**
  - Unico call site (`LibraryPageModel.cs:171`) le `result.EpubPath` nas linhas 175/180; zero
    `DisplayAlert`/`ShowPopupAsync`/`Popup` novo (UX de cobertura baixa = `## Deferred to PR review`).
  - `git diff --name-only main -- src/TranslateReader/` == exatamente esse arquivo.
  - Gate 1: `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f
    net10.0-windows10.0.19041.0` verde.
  - Corrida final: `Total >= 331`, `Failed: 0` (piso fixado ANTES da execucao: 319 baseline + >= 12
    novos), 60 testes JS intocados.
- **Dependencies:** T-5
- **Test:** suite completa + build do TFM Windows
- **Status:** pending

## Riscos nomeados
- **R1 — consumidor nao mapeado (D-...-4).** `grep -rn "TranslateBookAsync" src/` = 3 hits:
  contrato, Manager e `LibraryPageModel.cs:171`; `MauiProgram.cs` so registra o DI. `src/
  TranslateReader/` nao tem teste algum (D-2026-07-30-regression-suite-2), entao o UNICO detector
  de um consumidor esquecido e o Gate 1 (build `net10.0-windows`). Entre T-5 e T-7 o head MAUI fica
  deliberadamente quebrado — nao fazer push nessa janela.
- **R2 — fixtures reais com div-folha legitimo.** Se T-4 mudar a contagem de T-3, e texto real hoje
  intraduzido, nao ruido: task `blocked`, decisao humana, teste de caracterizacao nunca afrouxado.
- **R3 — custo/tempo de traducao.** O livro-origem passa de 360 para ~2,2k blocos (~8x palavras);
  a corrida sai de 3min56s para dezenas de minutos. Consequencia esperada da correcao, nao defeito;
  sem mudanca de UI nesta phase.
- **R4 — ReDoS.** Lookahead por caractere e O(n·m); o `RegexTimeoutMilliseconds` de 1s ja existente
  e a rede. Coberto pelas duas assercoes de T-2 (corpo grande + corpo degenerado).

## Learnings aplicados (phases anteriores)
- `Verify:`/acceptance medem propriedade POR ITEM, nunca so total agregado (the-method-refactor).
- Mudar DoD locked pelo caminho append-only: decisao nova supersedendo, depois a linha do
  CONTEXT.md — nunca reescrevendo a anterior (the-method-refactor) → T-1.
- Piso de contagem de teste fixado ANTES da corrida, no valor fechado da phase anterior
  (coverage-90/todos.md) → `Total >= 331` em T-7.
- Waiver so vale no sistema que levanta a issue; `#pragma` nao waiva Sonar (sonar-zero-issues) →
  T-4 nao inventa pragma novo.
- Cobertura do Sonar conta linhas + condicoes (coverage-90) → T-6 exige 90% de linha E branch.

## Execution
- Total tasks: 7 · Waves: 5 · Speedup paralelo: ~1,4x (T-2/T-3 e T-6/T-7)
- DoD: 7/7 itens cobertos → 1:T-3 · 2:T-2+T-4 · 3:T-2+T-4 (texto realinhado em T-1) · 4:T-5+T-6 ·
  5:T-6 · 6:T-4 · 7:T-7

## Files modified (all tasks)
- `.jdi/DECISIONS.md`, `.jdi/phases/div-paragraph-translation/CONTEXT.md`
- `src/TranslateReader.Core/Utilities/HtmlUtility.cs`
- `src/TranslateReader.Core/Models/BookTranslationResult.cs` (novo)
- `src/TranslateReader.Core/Contracts/Managers/ITranslationManager.cs`
- `src/TranslateReader.Core/Business/Managers/TranslationManager.cs`
- `src/TranslateReader/PageModels/LibraryPageModel.cs`
- `test/TranslateReader.Tests/HtmlUtilityTests.cs`, `HtmlInjectionTests.cs` (condicional),
  `TranslationManagerTests.cs`, `ExtractTextBlocksBaselineTests.cs` (novo)

## Test requirements
- Unit: `dotnet test` — piso `Total >= 331`, `Failed: 0`; 319 C# + 60 JS sao baseline intocavel
- Coverage: `dotnet test --collect:"XPlat Code Coverage"` — >= 90% (linha + branch) no codigo novo
  pos-`4285f25` (D-6)
- Build: `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f
  net10.0-windows10.0.19041.0`
- Sem I/O em teste novo, exceto os 3 de T-3 (fixtures `.epub` reais, excecao ja autorizada)
