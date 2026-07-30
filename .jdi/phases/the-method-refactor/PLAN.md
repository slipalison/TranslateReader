# Phase 13: Refactor The Method + memoria/CPU mobile — Plan  (slug: the-method-refactor)

## Goal
Eliminar violacoes concretas de The Method (CLAUDE.md) e de `.claude/rules/csharp.md`, cada mudanca
justificada por violacao nomeada e protegida pela rede de `regression-suite` (192 attrs, 196 passed /
2 skipped). 3 achados + 1 guardrail. Finding-driven, nao rewrite.

## Locked decisions (CONTEXT.md / DECISIONS.md)
- D-...-2: (A) escopo = `src/TranslateReader.Core` + `test/`; zero diff em `src/TranslateReader/`.
  (B) so conformidade provavel por inspecao — sem BenchmarkDotNet, sem otimizacao especulativa.
- D-...-3 (achado #1): `IFileUtility.DirectoryHasContent` + `ReadingManager` roteia por ele. EXCLUI
  `ReadingManager.cs:59-60` / `FileUtility.cs:31-32` (zip-slip = phase `epub-zip-slip`).
- D-...-4 (achado #2): MOVE puro dos 4 metodos HTML para `HtmlUtility`; sem teste novo dedicado.
- D-...-5 (achado #3): `ParsingEngine` vira `partial`, 7 regex inline viram `[GeneratedRegex]`.
- D-...-6: seam de interface do `TranslationEngine` DEFERIDO a `llm-mobile` — nao tocar.
- D-2 / D-6: 90% em codigo alterado pos-boundary `4285f25`; 192 attrs sao baseline intocavel.

## Regras de execucao (todas as tasks)
1. **1 achado = 1 commit atomico** (CONTEXT > Notes): producao + teste do mesmo achado no MESMO
   commit — seam alterado sem o teste que o prova nao e estado valido.
2. **Prova por mutacao** (learning de `regression-suite`): grep de DoD e satisfeito por teste que nao
   afirma nada. Cada task de codigo muta `src/`, confirma que um teste NOMEADO falha, reverte, e
   reporta o resultado na SUMMARY.
3. **Ajuste mecanico != mudanca de expectativa:** teste existente so muda por assinatura/stub, com a
   assercao original intacta e rotulo MECANICO explicito. Nenhum teste deletado ou afrouxado.
4. `dotnet format --verify-no-changes` JA falha no baseline (12 violacoes legadas, isentas por D-2) e
   exige `core.longpaths=true` — comparar contra o baseline, nunca reformatar legado.

## Tasks

### Wave 1 — achado #1 (fecha DoD 1 + DoD 2)

#### T-1: rotear a checagem de "imagens ja extraidas" por `IFileUtility`
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Contracts/Utilities/IFileUtility.cs`,
  `src/TranslateReader.Core/Utilities/FileUtility.cs`,
  `src/TranslateReader.Core/Business/Managers/ReadingManager.cs`,
  `test/TranslateReader.Tests/ReadingManagerTests.cs`, `test/TranslateReader.Tests/FileUtilityTests.cs`
- **Escopo exato:** contrato ganha `bool DirectoryHasContent(string directoryPath);` (7o metodo, 1
  contrato so — aceito em CONTEXT > Notes); `FileUtility` implementa com o predicado atual
  (`Directory.Exists(p) && Directory.GetFileSystemEntries(p).Length > 0`); `ReadingManager.cs:53-54`
  vira `if (fileUtility.DirectoryHasContent(imagesDir)) return;`. **NAO tocar `ReadingManager.cs:59-60`
  nem `FileUtility.WriteFileAsync`** — vetor de zip-slip, propriedade de `epub-zip-slip`.
- **Acceptance:**
  - DoD 1: `DirectoryHasContent` no contrato + impl; ZERO `Directory.(Exists|GetFileSystemEntries)`
    em `ReadingManager.cs`; chamada `fileUtility.DirectoryHasContent` presente.
  - DoD 2: `ReadingManagerTests.cs` >= 8 attrs e cita `DirectoryHasContent`; `FileUtilityTests.cs`
    cita `DirectoryHasContent`.
  - Cobertura (D-6): **sucesso** = dir com arquivo -> `true`; **edge** = dir vazio -> `false` e dir
    inexistente -> `false` (temp dir real, padrao ja usado no arquivo); **branch novo do Manager** =
    `DirectoryHasContent(...).Returns(true)` prova o skip (`ExtractAllImagesAsync`/`WriteFileAsync`
    `DidNotReceive`), sem I/O. **Excecao/cancelamento N/A declarado:** metodo sincrono sem
    `CancellationToken`; falha de FS propaga por fail-fast (§1), nao reproduzivel em unit test.
  - Mutacao: forcar `DirectoryHasContent` a retornar sempre `false` -> o teste de skip falha; a
    assercao `DidNotReceive` e load-bearing (mover o efeito, nao so remove-lo). Revertido.
  - Os 2 testes de `LoadChapterContentAsync` seguem verdes; `.Returns(false)` explicito neles
    (auto-value de `bool` ja e `false`) e ajuste **MECANICO**, assercoes intactas.
- **Dependencies:** none
- **Test:** `dotnet test` — `ReadingManagerTests` (7 -> >=8), `FileUtilityTests` (9 -> >=11)
- **Status:** pending

### Wave 2 — achado #3 (fecha DoD 4)

#### T-2: `ParsingEngine` -> `partial` + 7 `[GeneratedRegex]`
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Business/Engines/ParsingEngine.cs`
- **Escopo exato:** `public partial class ParsingEngine : IParsingEngine`; 7 padroes transcritos
  **verbatim** (mesma string, mesmos grupos e numeracao): L126 `(<dc:title...)` IgnoreCase|Singleline;
  L196 `<link\b([^>]*?)/?>`, L199 `\brel\s*=\s*"stylesheet"`, L202 `\bhref\s*=\s*"([^"]+)"`, L228
  img/src, L232 image/xlink:href, L236 image/href (esses 6, IgnoreCase). **Armadilha nomeada:** hoje o
  `RegexOptions` e o 4o argumento de `Regex.Replace(...)` — DEVE migrar para dentro do
  `[GeneratedRegex(...)]`; perde-lo muda comportamento em silencio. Nenhum metodo extraido, nenhuma
  logica alterada; `using System.Text.RegularExpressions` fica (tipos `Regex`/`Match` em uso).
- **Acceptance:**
  - DoD 4: zero `Regex.(Replace|Match|IsMatch)(`; `public partial class ParsingEngine` presente;
    `>= 7` `[GeneratedRegex`.
  - Equivalencia por inspecao (D-...-2 (B)): tabela de 7 linhas na SUMMARY `linha antiga -> metodo
    gerado -> pattern -> options`, provando transcricao byte-identica.
  - Nao-regressao: 19 `ParsingEngineTests` (3 EPUBs reais) verdes — executam `RewriteImagePaths` +
    `InlineCssLinks` (6 dos 7 padroes) via `ExtractChapterContentAsync`; caminho de falha coberto
    pelos fixtures problematicos (`RightingSoftware_*`), cancelamento N/A (API sem token). Mutacao:
    quebrar o padrao img/src -> `Practice_ExtractChapterContentAsync_RewritesImagePathsToVirtualHostUrl`
    falha -> reverter.
  - **Gap declarado, nao escondido:** `UpdateOpfTitleAsync` (padrao `<dc:title>`) nao e executado por
    teste algum — `CreateTranslatedEpubAsync` so aparece MOCKADO em `TranslationManagerTests`. Teste
    novo ali seria I/O de disco, proibido por §6 e ja recusado por precedente (`regression-suite`
    SUMMARY > Lacuna 4). Registrar em `.jdi/todos.md` na T-4; nao inventar fixture nem relaxar §6.
- **Dependencies:** none (arquivo disjunto de T-1/T-3)
- **Test:** `dotnet test` — `ParsingEngineTests` (19 attrs, 0 novos)
- **Status:** pending

### Wave 3 — achado #2 (fecha DoD 3)

#### T-3: MOVE dos 4 metodos de HTML de `TranslationManager` para `HtmlUtility`
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Business/Managers/TranslationManager.cs`,
  `src/TranslateReader.Core/Utilities/HtmlUtility.cs`
- **Escopo exato:** `HtmlUtility` vira `public static partial class` (obrigatorio para hospedar
  `[GeneratedRegex]`) e recebe `ExtractParagraphs`, `ExtractTextBlocks`, `ReplaceTextBlocksInHtml`,
  `StripHtmlTags` como `public static` (corpo/assinatura inalterados) + `ParagraphRegex`,
  `TextBlockRegex`, `HtmlTagRegex` como `private static partial`. `TranslationManager` remove as 4
  definicoes privadas e os 3 regex, roteia L141/L186/L189/L218 por `HtmlUtility.X(...)` (padrao ja
  usado com `ExtractBodyContent`), e perde `partial` e `using System.Text.RegularExpressions`
  (mantendo `System.Text` e `System.Security.Cryptography`). MOVE puro: zero mudanca de logica.
- **Acceptance:**
  - DoD 3: zero `private static ...(ExtractParagraphs|ExtractTextBlocks|ReplaceTextBlocksInHtml|StripHtmlTags)(`
    em `TranslationManager.cs` e **exatamente 4** `public static` desses nomes em `HtmlUtility.cs`
    (chamadas internas a `StripHtmlTags` nao contam — nao comecam com `public static`).
  - Cobertura (D-6): as linhas movidas seguem executadas pelas 48 chamadas indiretas existentes —
    `TranslateBookAsync_HtmlEncodesTranslatedTextBeforeBuildingTheEpub` (ReplaceTextBlocksInHtml),
    `TranslateBookAsync_TranslatesHeadingsAndListItems` (TextBlockRegex),
    `TranslateChapterAsync_SkipsEmptyParagraphs` (StripHtmlTags + filtro), `TranslateChapterAsync_*`
    (ParagraphRegex); **falha/cancelamento** pelo trio `*_WithCancelledToken_ThrowsWhileIterating` +
    `TranslateBookAsync_WhenCancelledMidLoop_...`. Sem teste novo dedicado (D-...-4).
  - Mutacao (prova que o MOVE esta no caminho vivo, nao e copia morta): quebrar
    `HtmlUtility.StripHtmlTags` -> `TranslationManagerTests` falha -> reverter.
  - 32 `TranslationManagerTests` + 15 `HtmlInjectionTests` verdes; sem colisao de nome com
    `ExtractBodyContent`/`InjectTags`/`BuildContinuousScrollHtml`.
- **Dependencies:** none (arquivos disjuntos de T-1/T-2)
- **Test:** `dotnet test` — `TranslationManagerTests` (32) + `HtmlInjectionTests` (15), 0 novos
- **Status:** pending

### Wave 4 — guardrail agregado (fecha DoD 5)

#### T-4: evidencia agregada + registro dos gaps declarados
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `.jdi/phases/the-method-refactor/SUMMARY.md`, `.jdi/todos.md`
- **Acceptance:**
  - DoD 5, 3 probes: `git diff --name-only $(git merge-base main HEAD) -- src/TranslateReader/` vazio;
    zero `BenchmarkDotNet` em `.csproj`; `[Fact]/[Theory]` >= 193 (esperado >= 195 = 192 + >=3 de T-1).
  - `dotnet build` + `dotnet test` verdes; `196 passed / 2 skipped` nao regride (esperado >= 199
    passed, 2 skipped — os `[Fact(Skip=...)]` de `TranslationEngineTests` intocados).
  - Os 5 `Verify:` do CONTEXT rodados, saida real colada, **escopo de cada medicao declarado**
    (learning: recorte parcial apresentado como total virou warning 2x).
  - `dotnet format` em escopo de SOLUCAO com `core.longpaths=true` vs baseline de 12 violacoes
    legadas — zero violacao NOVA nos 8 arquivos tocados.
  - `.jdi/todos.md` recebe o gap de T-2 (`UpdateOpfTitleAsync` sem teste que o execute) e o de
    `.claude/rules/csharp.md` §2 (infra de medicao nao criada, D-...-2 (B)).
- **Dependencies:** T-1, T-2, T-3
- **Test:** N/A (task de evidencia, nao altera `.cs`)
- **Status:** pending

## Execucao
- Total tasks: 4 | Waves: 4 | Speedup paralelo real: **1x**
- **Por que 4 waves:** `files_modified` de T-1/T-2/T-3 sao DISJUNTOS (T-2 e T-3 mexem em
  `[GeneratedRegex]` mas em arquivos diferentes, e `ParsingEngine` nao chama `HtmlUtility`). Mesmo
  assim nao ha paralelismo: o index do git e compartilhado e "1 achado = 1 commit atomico" exige
  serializacao. Waves = ordem de commit, nao dependencia de codigo.
- Ordem = a do CONTEXT (#1 -> #3 -> #2 -> guardrail), validada contra `files_modified`: **zero
  conflito real de arquivo**, mantida sem ajuste.

## Files modified (todas as tasks)
- T-1: `Core/Contracts/Utilities/IFileUtility.cs`, `Core/Utilities/FileUtility.cs`,
  `Core/Business/Managers/ReadingManager.cs`, `test/.../ReadingManagerTests.cs`, `.../FileUtilityTests.cs`
- T-2: `Core/Business/Engines/ParsingEngine.cs`
- T-3: `Core/Business/Managers/TranslationManager.cs`, `Core/Utilities/HtmlUtility.cs`
- T-4: `.jdi/phases/the-method-refactor/SUMMARY.md`, `.jdi/todos.md`

## Test requirements
- `dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj` (TFM unico `net10.0`, sem
  workload MAUI — D-...-regression-suite-6 proibe 2o test project / multi-target).
- Baseline intocavel: 192 attrs, 196 passed / 2 skipped. Cobertura minima 90% em codigo
  novo/alterado pos-boundary `4285f25` (D-6).
- Build do Core + Tests no doer; gate Windows (`-f net10.0-windows10.0.19041.0`) fica com o reviewer.
