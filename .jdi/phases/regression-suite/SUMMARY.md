# Phase 12: Rede de testes de regressao — Summary  (slug: regression-suite)

**Status:** complete
**Tasks:** 6/6 completas, 0 blocked
**Escopo honrado:** fase SO-DE-TESTE — `git diff --name-only 299f150..HEAD -- src/` = **vazio** (0 arquivos).
Nenhum `.csproj` alterado.

## Executed tasks

- **T-1** (`2510828`): `BookTranslationJobAccessTests.cs` criado — **12 atributos / 14 casos**, padrao
  `InMemoryDatabase` + `new BookTranslationJobAccess(_db.ConnectionString, initializeOnStartup: true)` +
  `IDisposable`, igual a `TranslationCacheAccessTests`. Cobre os 4 metodos publicos e o SIGNIFICADO das
  transicoes de `Status`: um `[Theory]` prova que Pending/InProgress/Paused sao retomaveis; Completed
  fica invisivel para `FetchActiveJobAsync`; update para status terminal encerra o job; job terminal ao
  lado de um ativo nao esconde o ativo; isolamento por `BookId`; `SaveJobAsync` devolve `Id > 0` e
  estampa `CreatedAt == UpdatedAt`; update e delete atingem so o job alvo.
- **T-2** (`5ab285c`): `BooksAccessTests.cs` **6 -> 8 atributos**. Duas assercoes de SEQUENCIA exata de
  titulos (nao `Assert.Contains`): uma pina `LastOpenedAt DESC` com nunca-abertos no fim, a outra pina o
  tiebreak `DateAdded DESC` (indistinguivel no primeiro teste). `MakeBook` ganhou dois parametros
  opcionais (`lastOpenedAt`, `dateAdded`); os 6 testes legados nao mudaram de comportamento.
- **T-3** (`cd24848`): `ReadingManagerTests.cs` **5 -> 7 atributos**. (1) `LoadProgressAsync` com
  progresso encontrado: round-trip de `BookId`/`ChapterHRef`/`ScrollPosition`/`ProgressPercentage`.
  (2) loop de `ExtractImagesIfNeededAsync` executado de verdade (2 imagens, uma em subpasta
  `images/fig1.png`): `WriteFileAsync` recebido 1x por imagem com o path composto e separador
  normalizado, `byte[]` correto. `IFileUtility` e mock -> zero I/O; `booksDirectory` unico via
  `Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))`.
- **T-4** (`c4375c8`): `HtmlInjectionTests.cs` **11 -> 15 atributos**, so em branch nao exercitada:
  (a) html que ja tem um `<base ` -> exatamente 1 `<base` no resultado (contagem via `Regex.Matches`
  estatico, como os testes irmaos); (b) `InjectTags(html, null, null)` -> `Assert.Equal(html, result)`;
  (c) `ExtractBodyContent` com `<body>` sem `</body>` -> trecho do abre-body ate o fim;
  (d) `BuildContinuousScrollHtml([])` -> `string.Empty`.
- **T-5** (`2816fcd`): `TranslationManagerTests.cs` **27 -> 32 atributos**. (a) `TranslateBookAsync`
  cancelado no meio do loop (cancelamento disparado no callback do mock de `GenerateAsync`) ->
  `ThrowsAnyAsync<OperationCanceledException>` + `UpdateJobProgressAsync(42, 0, Paused)` 1x +
  `DeleteJobAsync` NAO recebido + `CreateTranslatedEpubAsync` NAO recebido; (b) e (c)
  `TranslateChapterAsync` / `TranslateParagraphsAsync` com token cancelado -> lancam ao iterar e nao
  chamam a engine; (d) traducao hostil (`<script>alert(1)</script> Tom & Jerry`) -> o html entregue a
  `CreateTranslatedEpubAsync` contem `&lt;script&gt;` e `&amp;` e NAO o `<script>` cru;
  (e) `PauseTranslationAsync` sem job ativo -> `UpdateJobProgressAsync` nunca chamado.
  Reusa `SetupBookForTranslation`, `SetupBook`, `SetupBookAndChapter`, `SetupCacheForRebuild`.
- **T-6** (este commit): gate estrutural + contagem agregada (abaixo). PLAN.md com as 6 tasks marcadas
  `completed`.

## Blocked tasks

_(nenhuma)_

## Files modified

- `test/TranslateReader.Tests/BookTranslationJobAccessTests.cs` (novo)
- `test/TranslateReader.Tests/BooksAccessTests.cs`
- `test/TranslateReader.Tests/ReadingManagerTests.cs`
- `test/TranslateReader.Tests/HtmlInjectionTests.cs`
- `test/TranslateReader.Tests/TranslationManagerTests.cs`
- `.jdi/phases/regression-suite/PLAN.md` (status das tasks), `.jdi/phases/regression-suite/SUMMARY.md`

**Nada em `src/`. Nenhum `.csproj`.**

## Tests — as duas metricas, sem confundir

| Metrica | Baseline | Agora | Piso do PLAN |
|---|---|---|---|
| (a) atributos `[Fact]`/`[Theory]` literais (`grep -rhoE`) | 167 | **192** (+25) | >= 188 OK (DoD >= 175 OK) |
| (b) casos executados por `dotnet test` | 169 passed / 2 skipped / 171 total | **196 passed / 2 skipped / 198 total** | >= 190 passed, exatamente 2 skipped, 0 failed OK |

Os +25 atributos rendem +27 casos executados porque o `[Theory]` de T-1 expande em 3 `InlineData`.
Os 2 skipped continuam sendo exatamente os 2 `[Fact(Skip=...)]` de integracao LLamaSharp — nenhum
teste novo foi marcado como skip. Duracao da suite: ~4 s.

Cobertura: o unico arquivo novo e de TESTE e coverlet exclui a test assembly do relatorio — Gate 3
deve dar WARN, nao BLOCK. Nenhum codigo de producao foi adicionado para consertar isso (PLAN, secao
"Fora do plano").

## Guardrail estrutural (D-2026-07-30-regression-suite-6)

- `grep -c "net10.0-windows" test/TranslateReader.Tests/TranslateReader.Tests.csproj` = **0** OK
- `find test -name "*.csproj" | wc -l` = **1** OK (single-TFM `net10.0`, nenhum segundo test project)
- `git diff --name-only 299f150..HEAD -- src/` = **vazio** OK
- Gate 5.17: nas linhas ADICIONADAS nao existe `new SqliteConnection`, `HttpClient` nem
  `File.Write/Read/Create`; os testes novos nao criaram nenhum `Substitute.For<>` proprio — reusam os
  campos existentes, todos sobre interfaces `I[A-Z]` de `Contracts/` OK
- `dotnet format --verify-no-changes`: a lista de violacoes e **identica** a do baseline
  (`HtmlInjectionTests.cs(25,1)`, `(42,1)`, `ThemeEngineTests.cs(12,33)`,
  `TranslationManagerTests.cs(528,21/33/61)`, `(529,31)`) — todas em linhas LEGADAS, isentas por D-2.
  Nenhuma linha nova aparece na lista. Uma violacao introduzida por codigo novo (inicializador de
  objeto multi-declarador copiado do estilo vizinho) foi corrigida antes do commit de T-5.

## Provas de discriminacao (regra 3 do PLAN)

Metodo: mutar o comportamento pinado em `src/`, rodar o filtro, confirmar que o teste novo FALHA,
`git checkout -- src/`, reconfirmar verde. **Nenhuma mutacao foi commitada** — `git status` limpo de
`src/` antes de cada commit; conferido no fim (`git diff --name-only 299f150..HEAD -- src/` vazio).
Em cada rodada, o conjunto de testes que falhou foi EXATAMENTE o previsto — nenhum teste novo
sobreviveu a mutacao do comportamento que ele afirma pinar.

### T-1 — `BookTranslationJobAccess.cs` (12/12 atributos discriminados)

| Mutacao | Testes que FALHARAM |
|---|---|
| A: `if (!reader.ReadAsync()) return null` -> `return new BookTranslationJob()` | 5: ReturnsNullWhenNoJobExists, IgnoresJobInTerminalStatus, IsolatesJobsByBookId, UpdateJobProgressAsync_ToTerminalStatus_MakesJobInactive, DeleteJobAsync_RemovesJob |
| B: filtro de status perde Paused (`IN (Pending, InProgress)`) | 3: AssignsGeneratedIdAndRoundTripsJob, ReturnsJobWhoseStatusIsResumable(Paused), WithTerminalJobsAroundActiveOne_ReturnsOnlyTheActiveJob |
| C: `SELECT last_insert_rowid()` -> `SELECT 0` | 6: AssignsGeneratedIdAndRoundTripsJob, WithTerminalJobsAroundActiveOne, UpdateJobProgressAsync_PersistsChapterIndexAndStatus, _ToTerminalStatus_MakesJobInactive, DeleteJobAsync_RemovesJob, DeleteJobAsync_WithUnknownId_LeavesExistingJobsIntact |
| D (2 alvos disjuntos): `$created` -> `DateTime.UnixEpoch` **e** UPDATE `WHERE Id = $jobId` -> `WHERE Id >= 0` | 2: SaveJobAsync_StampsCreatedAtEqualToUpdatedAt, UpdateJobProgressAsync_TouchesOnlyTargetJob |
| E: filtro de status reduzido a `IN (Paused)` | 7, incluindo as linhas Pending e InProgress do `[Theory]` |

### T-2 — `BooksAccess.cs` (2/2)

| Mutacao | Falhou |
|---|---|
| `ORDER BY LastOpenedAt DESC, DateAdded DESC` -> `ORDER BY DateAdded DESC` | OrdersByLastOpenedAtDescending_WithNeverOpenedLast (so ele; os 7 outros verdes) |
| tiebreak `DateAdded DESC` -> `DateAdded ASC` | OrdersNeverOpenedBooksByDateAddedDescending (so ele) |

### T-3 — `ReadingManager.cs` (2/2)

| Mutacao | Falhou |
|---|---|
| 2 alvos disjuntos: `FetchProgressAsync(bookId)` -> `FetchProgressAsync(bookId + 1)` **e** a normalizacao de separador do path da imagem removida | LoadProgressAsync_ReturnsStoredProgressForRequestedBook e LoadChapterContentAsync_WritesEveryExtractedImageBelowTheBookImagesDirectory |
| `foreach` sobre as imagens limitado a primeira (`images.Take(1)`) | ..._WritesEveryExtractedImage... (prova a assercao "1x por imagem", nao so o path) |

### T-4 — `HtmlUtility.cs` (4/4, mutacoes com alvos disjuntos numa rodada)

| Mutacao | Falhou |
|---|---|
| `hasBase` deixa de checar se o html ja tem `<base ` | InjectTags_WhenHtmlAlreadyHasBaseTag_KeepsExactlyOneBase |
| early-return "nada a injetar" -> `BuildFallbackHtml(html, baseTag, css)` | InjectTags_WithNothingToInject_ReturnsHtmlUntouched |
| `if (bodyEndIndex < 0) return html[bodyStart..]` -> `return html` | ExtractBodyContent_WithUnclosedBody_ReturnsEverythingAfterTheOpenTag |
| `new StringBuilder()` -> `new StringBuilder("<section>")` | BuildContinuousScrollHtml_WithNoChapters_ReturnsEmptyString |

Falharam exatamente os 4 testes novos, 1 por mutacao, e **os 11 legados seguiram verdes** —
confirmacao direta de que essas 4 branches nao eram exercitadas por ninguem antes.

### T-5 — `TranslationManager.cs` (5/5)

| Mutacao | Falhou |
|---|---|
| rodada 1, 3 alvos disjuntos: status do catch de `TranslateBookAsync` Paused -> Cancelled; guarda `job is not null` invertida em `PauseTranslationAsync`; `WebUtility.HtmlEncode` removido de `ReplaceTextBlocksInHtml` | WhenCancelledMidLoop_PausesJobAndSkipsEpubCreation, PauseTranslationAsync_WithoutActiveJob_DoesNotUpdateAnyJob, HtmlEncodesTranslatedTextBeforeBuildingTheEpub (+ o legado PauseTranslationAsync_UpdatesJobStatus, esperado: guarda invertida quebra os dois lados) |
| rodada 2: remover `ct.ThrowIfCancellationRequested()` SO dentro de `TranslateChapterAsync` e `TranslateParagraphsAsync` | TranslateChapterAsync_WithCancelledToken_ThrowsWhileIterating, TranslateParagraphsAsync_WithCancelledToken_ThrowsWhileIterating |

## Lacunas registradas (decisao, nao esquecimento)

1. **O `ORDER BY UpdatedAt DESC` de `FetchActiveJobAsync` (T-1) segue NAO pinado.** Com 2 jobs ativos o
   resultado depende de `UpdatedAt`, mas `SaveJobAsync` e `UpdateJobProgressAsync` estampam
   `DateTime.UtcNow` internamente e dois inserts podem cair no mesmo tick -> teste flaky. So e pinavel
   com seam de clock (mudanca de producao, fora do escopo desta fase). O que ESTA pinado e o efeito
   observavel do `LIMIT 1` + filtro de status: com jobs terminais em volta do ativo, o retornado e o
   ativo (WithTerminalJobsAroundActiveOne...). Candidato para `the-method-refactor`.
2. **Os 2 gaps de `D-2026-07-30-regression-suite-5` seguem ABERTOS por decisao** — nao foram fechados
   nem reabertos: (1) branch "imagens ja extraidas" de `ReadingManager` le `Directory.Exists` direto,
   fecha-lo exigiria I/O real num teste novo (proibido por `.claude/rules/csharp.md` secao 6) ou seam de
   producao; (2) `TranslationEngine` acopla a tipos concretos do LLamaSharp (`LLamaWeights`,
   `StatelessExecutor`) sem seam de interface — os 2 `[Fact(Skip=...)]` de integracao continuam sendo o
   unico jeito de exercitar carregamento real de modelo. T-3 cobriu deliberadamente o branch OPOSTO
   (imagens ainda nao extraidas), testavel sem tocar producao.
3. **Lacuna do app MAUI (1516 linhas) permanece catalogada, nao fechada** — opcao (c) de
   `D-2026-07-30-regression-suite-2`. `ReaderPage.xaml.cs`, `ReaderPageModel.cs`, `LibraryPageModel.cs`,
   `SettingsOverlay.xaml.cs`, `TranslateBookPopup.xaml.cs`, `MauiProgram.cs`, `AppShell.xaml.cs` e
   `Utilities/*Converter.cs` seguem sem teste automatizado.
4. **`ParsingEngine`** ficou fora por motivo tecnico: os 18 testes existentes dependem de 3 EPUBs reais
   em `TestData/`; teste NOVO ali seria I/O de disco (secao 6) e fixture em memoria exigiria seam de
   producao (a API recebe path, nao stream). Os 18 legados seguem sendo a rede daquele arquivo.

## Achado desta execucao (util para quem escrever teste novo aqui)

Substituto NSubstitute de metodo que devolve `Task<string?>` **nao** devolve null por default: o
auto-value de string e `string.Empty`, que o `TranslationManager` le como CACHE HIT e por isso pula a
chamada a engine. O teste de cancelamento de `TranslateBookAsync` precisou de
`_cacheAccess.FetchTranslationAsync(...).Returns((string?)null)` explicito — sem isso ele passava
vazio (falso verde), porque a engine nunca era chamada e o cancelamento nunca era disparado.
