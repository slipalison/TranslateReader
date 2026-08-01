# Phase 18: Imagem quebrada em livro traduzido — Plan  (slug: translated-epub-images)

## Goal
Corrigir a GERACAO do EPUB traduzido para que nenhuma URL do app (`https://epub-images/...`) vaze
para dentro do artefato, provando a propriedade no ARTEFATO, sem tocar o app MAUI.

## Locked decisions (CONTEXT.md)
- **D-...-2/-3**: enum `ChapterContentPurpose { Display, Export }` (`Models/`) vira parametro
  OBRIGATORIO (sem default) de `ExtractChapterContentAsync`. `Export` devolve `item.Content` cru (sem
  `RewriteImagePaths`/`InlineCssLinks`); `Display` = hoje + guarda (`imagesDirectory` vazio ->
  `InvalidOperationException`). Helpers privados intocados.
- **D-...-4**: `ReadingManager.cs:30` -> `Display`; `TranslationManager.cs:123,193,242` -> `Export`
  (reconferido no HEAD pos-PR #17: linhas exatas, os 3 passam `string.Empty`). Diff fechado, 5 arquivos.
- **D-...-6/-7**: regexes de forma, percent-encoding e migracao de livros quebrados fora de escopo.

## Achados do planner (medidos nesta sessao; sem tool de shell — por isso nada e cravado)
1. **[BLOQUEANTE, DoD 5] o piso `304` esta 34 testes ABAIXO do real.** Contei com o MESMO grep do
   gate sobre `test/TranslateReader.Tests/*.cs` do HEAD (== `main`, ja com PR #17): `[Fact` = **289**
   + `[InlineData` = **49** = **338**, `Skip =` = **2** — bate 1:1 com a corrida real (`Failed 0,
   Passed 336, Skipped 2, Total 338`) e nao ha `MemberData`/`ClassData` (verificado), logo a formula
   e exata. `304` aceita perder 34 testes. **T-1** corrige append-only DERIVANDO `B` de `main` no
   proprio comando + `comm` NOME A NOME (learning: contagem aceita stub e delecao compensada).
2. **[DoD 3] risco de gate que reprova codigo correto:** a prosa exige "nenhuma entrada do zip com
   `https://`", mas o EPUB-fonte pode ter `https://` NATIVO (o fixture Practice tem paginas de
   anuncio no spine). Sem shell nao medi -> **T-1** sonda ANTES de qualquer codigo; so se houver
   ocorrencia nativa a prosa vira DIFERENCIAL. `epub-images` == 0 segue absoluto nos 2 cenarios.
3. **[compilacao] o churn do 4o argumento atinge 6 arquivos de teste, nao 2** (CONTEXT citou 2):
   `ParsingEngineTests` (5 calls), `ParsingEngineEdgeCaseTests` (5), `ParsingEngineFixtureValidationTests`
   (2), `ExtractTextBlocksBaselineTests` (1), `TranslationManagerTests` (12), `ReadingManagerTests` (4).
   **ARMADILHA:** `ExtractTextBlocksBaselineTests.cs:62` chama o engine REAL com `string.Empty` — com
   `Display` a guarda nova EXPLODE; tem de receber `Export`. Bonus: os 4 literais de caracterizacao
   (`6102`/`239075`/`1329`/`292254`) sobreviverem sob `Export` e prova independente de que pular as 2
   mutacoes nao altera o texto extraido. **Se um mudar, a premissa de D-...-3 esta errada —
   investigar, NUNCA relaxar o literal.**
4. **[contrato/The Method]** `IParsingEngine` tem **6 operacoes**; PARAMETRO novo mantem 6 (exigencia
   de D-...-2). Enum em `Models/` (zero dependencia, igual `ReadingMode.cs`). Nada sobe de camada.
5. **[§6 x DoD 3 — conflito RESOLVIDO aqui]** o teste do artefato precisa abrir zip em disco. A
   excecao a `csharp.md` §6 ja esta REGISTRADA e nomeada para este arquivo em
   **D-2026-07-31-coverage-90-3** (fixture `.epub` real "autorizada nomeadamente no PLAN de
   `sonar-zero-issues`, T-6"), e `ParsingEngineTests.cs:249-329` ja grava EPUB traduzido em temp dir.
   Logo teste novo NESSE arquivo, MESMO padrao (temp dir + `Guid`, `ZipFile.OpenRead`, helpers
   `ReadEntry`/`ReadOpf`, cleanup em `finally`) — sem decisao nova. `MemoryStream`/`ZipArchive`
   REJEITADO: a assinatura de `CreateTranslatedEpubAsync` e path-based e muda-la estoura o diff
   fechado de D-...-4 (DoD 6).
6. **[livros ja quebrados]** ZERO codigo (D-...-7) e o registro **JA EXISTE**:
   `.jdi/todos/2026-08-01-translated-epub-images.md`, item `[PRODUTO/UX, decisao humana]`, + `##
   Deferred to PR review`. Nenhuma task nova; quem prova que nada de migracao entrou e o DoD 6.
7. **[guarda fail-fast: nenhum caminho legitimo passa vazio hoje]** `ReadingManager.cs:28` monta
   `Path.Combine(booksDirectory, "images", bookId)` e `MauiProgram.cs:65,88,94` injeta
   `Path.Combine(FileSystem.AppDataDirectory, "books")` — nunca vazio. PROVAM:
   `ReadingManagerTests.LoadChapterContentAsync_ExtractsImagesThenParsesContent` (assere
   `Arg.Is<string>(s => s.Contains("images"))`) e `..._WritesEveryExtractedImageBelowTheBookImagesDirectory`.
   Os unicos `string.Empty` do repo sao os 3 call sites que viram `Export` + o achado 3.

## Tasks
Specialist de TODAS as tasks: `jdi-doer-translatereader` (single-stack, `.jdi/specialists.md`).

### Wave 1
#### T-1: corrigir os pisos ocos do DoD por MEDICAO (append-only) — **DoD 5 e 3**
- **Files modified:** `.jdi/decisions/D-2026-08-01-translated-epub-images-9.md` (NOVO),
  `.jdi/phases/translated-epub-images/CONTEXT.md` (so as linhas dos itens 5 e, se preciso, 3)
- **Acceptance:**
  - Corrida LIMPA da suite antes de qualquer mudanca; `Total/Passed/Failed/Skipped` reais transcritos
    na decisao (esperado 338/336/0/2; se divergir, vale o medido).
  - Sonda registrada: quantas entradas do Practice `.epub` tem `https://` NATIVO (`pwsh` +
    `[IO.Compression.ZipFile]::OpenRead` + `StreamReader` por entrada + `-match 'https://'`).
  - `D-...-9` (arquivo NOVO; `-1..-8` intocados) substitui **so** o comando do item 5: `B` = `[Fact` +
    `[InlineData` em `main`, `S` = `Skip =` em `main`, `comm -23` de nomes de metodo `main` vs HEAD
    (nenhum some), `Failed: 0 && Total >= B+5 && Skipped <= S && Passed+Skipped+Failed == Total`.
    `+5` = os 5 testes novos da fase (3 em `ParsingEngineTests`, 2 em `TranslationManagerTests`).
  - Se houver `https://` nativo, a MESMA `D-...-9` troca o item 3 para a forma DIFERENCIAL (nenhuma
    entrada GANHA `https://` que a mesma entrada do original nao tinha); senao registra a medicao que
    autoriza a forma absoluta.
  - `npx -y jdi-cli render` rodado, nenhuma view editada a mao, `.gitignore` fora do commit.
- **Dependencies:** none | **Test:** rodar so a derivacao do comando novo e conferir `B=338`, `S=2`,
  `comm` vazio | **Commit:** `docs(translated-epub-images): derive the suite floor from main (D-...-9)`
- **Status:** pending

### Wave 2
#### T-2: RED — teste que prova a imagem quebrada NO ARTEFATO (tem de FALHAR) — **DoD 3 (vermelho)**
- **Files modified:** `test/TranslateReader.Tests/ParsingEngineTests.cs`
- **Acceptance:**
  - `Practice_TranslatedEpubArtifact_ForExportedChapters_NoEntryContainsTheAppHost` escrito com a
    assinatura de HOJE (3 args, `string.Empty`), espelhando `RebuildAllTranslatedChaptersAsync` +
    `CreateTranslatedEpubAsync`: html por capitulo -> `HtmlUtility.ReplaceTextBlocksInHtml(html,
    blocosOriginais)` (cache vazio = identidade) -> grava em temp dir -> reabre com `ZipFile.OpenRead`.
  - Assercao final IGUAL antes e depois do fix (T-3 so muda o 4o argumento): nenhuma entrada com
    `epub-images`; `https://` na forma decidida em T-1.
  - **Transcript obrigatorio no SUMMARY:** `--filter "FullyQualifiedName~Practice_TranslatedEpubArtifact"
    > TestResults/red-artifact.log` com `Failed: 1` e a URL `https://epub-images/...` na mensagem.
  - Nenhum arquivo de `src/` tocado.
- **Dependencies:** T-1 (a forma da assercao vem da medicao) | **Test:** ele mesmo — RED aqui, GREEN
  em T-3 (`csharp.md` §6: bugfix comeca vermelho) | **Commit:** `test(translated-epub-images): prove
  the app host leaks into the translated epub`
- **Status:** pending

### Wave 3
#### T-3: fix — `ChapterContentPurpose`, Export sem mutacao, guarda no Display — **DoD 1, 2, 3**
- **Files modified:** em `src/TranslateReader.Core/`: `Models/ChapterContentPurpose.cs` (NOVO),
  `Contracts/Engines/IParsingEngine.cs`, `Business/Engines/ParsingEngine.cs`,
  `Business/Managers/TranslationManager.cs`, `Business/Managers/ReadingManager.cs`; em
  `test/TranslateReader.Tests/`: `ParsingEngineTests.cs`, `ParsingEngineEdgeCaseTests.cs`,
  `ParsingEngineFixtureValidationTests.cs`, `ExtractTextBlocksBaselineTests.cs`,
  `TranslationManagerTests.cs`, `ReadingManagerTests.cs`
- **Acceptance:**
  - `Export` devolve `item.Content` sem as 2 mutacoes; `Display` inalterado + guarda
    `string.IsNullOrWhiteSpace(imagesDirectory)` -> `InvalidOperationException`, ao lado das 2 guardas
    de `ParsingEngine.cs:50-53`. Contrato segue com 6 operacoes, parametro sem default.
  - Call sites: `TranslationManager` 3x `Export` / 0x `Display`; `ReadingManager` 1x `Display` / 0x `Export`.
  - 2 testes NOVOS: `Practice_ExtractChapterContentAsync_ForExport_MatchesRawZipEntryForEveryChapter`
    (compara com a entrada crua via `ReadEntry`, TODOS os capitulos) e
    `Practice_ExtractChapterContentAsync_DisplayWithEmptyImagesDirectory_ThrowsInvalidOperationException`.
  - T-2 fica GREEN so pela troca do 4o argumento para `Export` (assercao intocada); transcript em
    `TestResults/green-artifact.log`.
  - Churn: `Display` onde ja se passa diretorio real; **`Export` em `ExtractTextBlocksBaselineTests.cs:62`**;
    `Arg.Any<ChapterContentPurpose>()` nos setups NSubstitute; os 4 literais de baseline nao mudam.
  - `dotnet format` limpo; zero arquivo em `src/TranslateReader/`.
- **Dependencies:** T-2 | **Test:** DoD 1, DoD 2 (guarda + `..._RewritesImagePathsToVirtualHostUrl`
  existente), DoD 3 verde | **Commit:** `fix(translated-epub-images): extract chapters for export
  without app-host rewrites`
- **Status:** pending

### Wave 4
#### T-4: pinar o purpose que cada call site de producao passa — **DoD 4**
- **Files modified:** `test/TranslateReader.Tests/TranslationManagerTests.cs`,
  `test/TranslateReader.Tests/ReadingManagerTests.cs`
- **Acceptance:**
  - `TranslateBookAsync_UsesExportPurposeForCacheExtractionAndRebuild`: `Received(2)` com `Export`
    (extracao de cache + rebuild) **e** `DidNotReceive()` com `Display`.
  - `TranslateChapterAsync_UsesExportPurposeToReadChapterHtml`: `Received(1)` com `Export`.
  - `LoadChapterContentAsync_ExtractsImagesThenParsesContent` (`ReadingManagerTests.cs:62`) assere
    `ChapterContentPurpose.Display` mantendo `Arg.Is<string>(s => s.Contains("images"))` — e isso que
    prova a guarda inalcancavel em producao. Nenhum arquivo de `src/` tocado.
- **Dependencies:** T-3 | **Test:** os 3 nomes acima (filtro do DoD 4, piso `n=3`) | **Commit:**
  `test(translated-epub-images): pin the chapter content purpose of every call site`
- **Status:** pending

### Wave 5
#### T-5: nao-regressao, escopo de diff e prova por mutacao — **DoD 5, 6**
- **Files modified:** `.jdi/phases/translated-epub-images/SUMMARY.md`
- **Acceptance:**
  - Os 6 `Verify:` (item 5 ja corrigido por T-1) rodados em sequencia, exit 0, logs em
    `TestResults/dod1..6.log`; `Total >= B+5`, `comm` sem nome perdido.
  - `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0`
    com `0 Error(s)`; `git diff --name-only main -- src/TranslateReader/` VAZIO.
  - **Mutacao** (learning: gate textual nao prova comportamento), transcrita: (a) `Export` caindo no
    caminho de `Display` -> `..._ForExport_MatchesRawZipEntry...` e `..._NoEntryContainsTheAppHost`
    FALHAM; (b) guarda removida -> o teste de `InvalidOperationException` FALHA; ambas revertidas.
  - SUMMARY registra baseline medido, transcript RED->GREEN de T-2/T-3 e que livros ja traduzidos
    seguem quebrados por decisao (D-...-7, ja registrado em `.jdi/todos/`). `.gitignore` fora do commit.
- **Dependencies:** T-4 | **Test:** suite inteira (DoD 5) + build do app (DoD 6) | **Commit:**
  `docs(translated-epub-images): record gate evidence and mutation proof`
- **Status:** pending

## Execution
- Tasks: 5 | Waves: 5 sequenciais | speedup 1x **por desenho**: RED-first (T-2 antes de T-3) e o 4o
  argumento tocando os mesmos 6 arquivos de teste tornam qualquer paralelismo conflito garantido.
- DoD: 1->T-3 | 2->T-3 | 3->T-2 (red) + T-3 (green) | 4->T-4 | 5->T-1 (corrige) + T-5 (executa) |
  6->T-5. **6/6.**
- Fora de todo commit: `.gitignore` (alteracao local do usuario).

## Test requirements
- `DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release`
- Piso: `Failed: 0`, `Total >= B+5` com `B` derivado de `main` no proprio comando, `Skipped <= 2`,
  nenhum nome de teste de `main` ausente no HEAD.
- Cobertura >= 90% (D-6) sobre o codigo ALTERADO: branch `Export`, guarda de `Display`, 4 call sites.
- I/O de disco autorizado SO em `ParsingEngineTests.cs` (D-2026-07-31-coverage-90-3); rede e SQLite
  reais seguem banidos.
