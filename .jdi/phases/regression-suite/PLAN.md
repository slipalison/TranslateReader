# Phase 12: Rede de testes de regressao — Plan  (slug: regression-suite)

## Goal
Fixar em testes de caracterizacao o comportamento observavel de hoje, para que o refactor da phase
`the-method-refactor` quebre um teste em vez de quebrar o app.
**Fase SO-DE-TESTE: zero diff em `src/`. Diff em `src/` = desvio de escopo (Gate 5).**

## Locked decisions (CONTEXT.md / DECISIONS.md)
- `-2`: rede limitada a `src/TranslateReader.Core`; lacuna do app MAUI (1516 linhas) catalogada, nao fechada.
- `-3`: `BookTranslationJobAccess` (0 testes) ganha arquivo dedicado, padrao `InMemoryDatabase`.
- `-4`: pinar ordenacao de `FetchAllBooksAsync` + caso "progresso encontrado" de `LoadProgressAsync`.
- `-5`: 2 gaps ficam FORA (branch "imagens ja extraidas" via `Directory.Exists` real;
  `TranslationEngine` acoplado ao LLamaSharp). **Nao reabrir, nao fechar** — sao de phase 13.
- `-6`: test project segue single-TFM `net10.0`, 1 unico `.csproj` em `test/`.
- D-2 (boundary `4285f25`) + D-6 (90% em codigo novo). Specialist de todas as tasks:
  `jdi-doer-translatereader` (single-stack, glob `**/*`).

## Baseline medido nesta sessao (usar; nao re-derivar)
- **167** atributos `[Fact]`/`[Theory]` literais (metrica do DoD item 6), 16 arquivos; +2
  `[Fact(Skip=...)]` de integracao LLamaSharp que o grep do DoD nao conta -> 169 no arquivo.
- `dotnet test` -> **169 passed / 2 skipped / 171 total** = metrica de EXECUCAO. Comparar 169 com 167
  no Gate 2 deixaria passar regressao de 2 testes. Este piso deve SUBIR nesta fase.

## Regras validas para TODA task (nao repetidas abaixo)
1. `git diff --stat HEAD -- src/` vazio no commit; nenhum `.csproj` alterado.
2. Gate 5.17: proibido em codigo novo `new SqliteConnection`, `HttpClient`,
   `File.(Write|Read|Create)` — SQLite so via `InMemoryDatabase`; `Substitute.For<>` so em `I[A-Z]`.
3. **Prova de discriminacao** (learning `readme`: "DoD por grep nao mede claim falso"): antes do
   commit, mutar temporariamente o comportamento pinado em `src/`, confirmar que o teste novo FALHA,
   reverter com `git checkout -- src/`, registrar 1 linha por teste no SUMMARY. Teste que passa com
   a mutacao aplicada nao pina nada.
4. Timestamp de teste sempre `DateTime.UtcNow` (Kind=Utc): colunas sao TEXT `"O"` e o SQLite ordena
   lexicograficamente.
5. Codigo/commits em ingles; 1 task = 1 commit, scope `regression-suite`; `dotnet format
   --verify-no-changes` antes de commitar.

## Wave 1 — 5 tasks 100% independentes (arquivos disjuntos, sem deps): paralelismo real

#### T-1: cobrir `BookTranslationJobAccess`  (DoD 1, 2, 6)
- **Risco:** 107 linhas / 4 metodos publicos que persistem pause/resume da traducao de livro, com
  ZERO teste; mexer em `Status IN (...)` ou `LIMIT 1` hoje nao acusa nada.
- **Files modified:** `test/TranslateReader.Tests/BookTranslationJobAccessTests.cs` (novo)
- **Acceptance:**
  - `InMemoryDatabase` + `new BookTranslationJobAccess(_db.ConnectionString,
    initializeOnStartup: true)` + `IDisposable`, no padrao de `TranslationCacheAccessTests`.
  - **>= 8** atributos, cobrindo os 4 metodos E o significado das transicoes de `Status`:
    (a) fetch sem job -> `null`; (b) `SaveJobAsync` da `Id > 0` + round-trip de
    `SourceLanguage`/`TargetLanguage`/`LastCompletedChapterIndex`/`CreatedAt`/`UpdatedAt`;
    (c) `[Theory]` `Pending`/`InProgress`/`Paused` -> retornado como ativo; (d) status terminal
    (`Completed`) -> fetch `null`; (e) isolamento por `BookId`; (f) `UpdateJobProgressAsync`
    persiste indice + status; (g) update para status terminal torna o job INATIVO (par
    resume/encerramento); (h) `DeleteJobAsync` remove; (i) delete de id inexistente e no-op.
  - **Nao** pinar `ORDER BY UpdatedAt DESC` com 2 jobs ativos: os metodos estampam `DateTime.UtcNow`
    internamente e 2 inserts podem cair no mesmo tick -> flaky. Pinar o `LIMIT 1` (exatamente 1 job,
    e ativo) e registrar no SUMMARY que a ordem por `UpdatedAt` so e pinavel com seam de clock
    (producao, fora de escopo).
- **Test:** `dotnet test --filter FullyQualifiedName~BookTranslationJobAccessTests`
- **Commit:** `test(regression-suite): cover BookTranslationJobAccess pause/resume state`
- **Status:** pending

#### T-2: pinar ordenacao de `FetchAllBooksAsync`  (DoD 3, 6)
- **Risco:** `ORDER BY LastOpenedAt DESC, DateAdded DESC` (`BooksAccess.cs:52`) define a ordem da
  biblioteca na tela; hoje so ha assercao de CONTAGEM — remover a clausula passa verde.
- **Files modified:** `test/TranslateReader.Tests/BooksAccessTests.cs`
- **Acceptance:**
  - **+2** atributos (6 -> >= 8), nome contendo `Order` (o DoD faz `grep -i order`).
  - Teste 1: inserir na ordem A (`LastOpenedAt` -1d), B (-1h), C (null, `DateAdded` agora) ->
    sequencia exata de titulos `[B, A, C]`; pina `LastOpenedAt DESC` E NULL indo para o fim.
  - Teste 2: 2 livros com `LastOpenedAt` null e `DateAdded` distintos -> mais recente primeiro
    (pina o tiebreak `DateAdded DESC`, indistinguivel no teste 1).
  - Assercao de sequencia (indice ou `Assert.Equal` sobre a lista de titulos), nunca `Assert.Contains`.
- **Test:** `dotnet test --filter FullyQualifiedName~BooksAccessTests`
- **Commit:** `test(regression-suite): pin FetchAllBooksAsync ordering contract`
- **Status:** pending

#### T-3: `ReadingManager` — progresso encontrado + loop de imagens  (DoD 4, 6)
- **Risco:** (1) `LoadProgressAsync` so tem o caso nulo — retomada de leitura nunca verificada no
  Manager; (2) o `foreach` de `ExtractImagesIfNeededAsync` (path + `IFileUtility.WriteFileAsync`)
  NUNCA executa hoje (o unico teste passa dicionario vazio) e e alvo de phase 13 e de `epub-zip-slip`.
- **Files modified:** `test/TranslateReader.Tests/ReadingManagerTests.cs`
- **Acceptance:**
  - **+2** atributos (5 -> >= 7).
  - `LoadProgressAsync` com `ReadingProgress` do mock: assercao de `BookId`, `ChapterHRef`,
    `ScrollPosition`, `ProgressPercentage` (round-trip, nao `Assert.NotNull`).
  - Loop: `ExtractAllImagesAsync` devolve >= 2 entradas (uma com subpasta, ex. `images/cover.jpg`);
    `WriteFileAsync` recebido 1x por imagem com path composto esperado (separador normalizado) e
    `byte[]` correto; `IFileUtility` e mock -> nada em disco. Usar `ReadingManager` local com
    `booksDirectory` unico (`Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))`).
  - **NAO** cobrir o branch "imagens ja extraidas" (exige `Directory.Exists` true = I/O real): gap
    deliberado `D-...-5(1)`. Esta task cobre o branch OPOSTO, testavel sem seam de producao.
- **Test:** `dotnet test --filter FullyQualifiedName~ReadingManagerTests`
- **Commit:** `test(regression-suite): pin LoadProgressAsync hit and image extraction loop`
- **Status:** pending

#### T-4: pinar contratos de borda de `HtmlUtility`  (DoD 6; acima do piso, risco nomeado)
- **Risco:** alvo declarado de phase 13 (`Regex` -> `[GeneratedRegex]`, slice -> span). Os 11 testes
  atuais pegam o caminho comum; ficam soltos 4 branches — inclusive a de-duplicacao do `<base>`,
  cuja quebra faz TODA imagem do EPUB resolver URL errada em silencio.
- **Files modified:** `test/TranslateReader.Tests/HtmlInjectionTests.cs`
- **Acceptance:**
  - **+4** atributos, so em branch nao exercitado: (a) `InjectTags` em html que JA tem `<base ` ->
    exatamente 1 `<base` no resultado; (b) `InjectTags(html, null, null)` com html nao-vazio ->
    `Assert.Equal(html, result)`; (c) `ExtractBodyContent` com `<body>` sem `</body>` -> trecho do
    abre-body ate o fim; (d) `BuildContinuousScrollHtml([])` -> `string.Empty`.
  - Contar ocorrencias com `Regex.Matches` estatico como os testes irmaos; sem `new Regex(...)`.
- **Test:** `dotnet test --filter FullyQualifiedName~HtmlInjectionTests`
- **Commit:** `test(regression-suite): pin HtmlUtility edge contracts`
- **Status:** pending

#### T-5: `TranslationManager` — cancelamento, pause sem job, encoding  (DoD 6; acima do piso)
- **Risco:** dos 23 testes atuais NENHUM exercita cancelamento — §6 exige esse caminho, e
  `catch (OperationCanceledException) -> Update(..., "Paused") -> throw` E o contrato de resume do
  produto, dentro dos loops que phase 13 reestrutura. Alem disso `ReplaceTextBlocksInHtml` aplica
  `WebUtility.HtmlEncode` (fronteira de injecao: livro nao confiavel -> EPUB de saida) sem teste, e
  um refactor para span dropa isso calado.
- **Files modified:** `test/TranslateReader.Tests/TranslationManagerTests.cs`
- **Acceptance:**
  - **+5** atributos: (a) `TranslateBookAsync` cancelado no meio do loop (cancelar no callback do
    mock de `GenerateAsync`) -> `Assert.ThrowsAnyAsync<OperationCanceledException>` E
    `UpdateJobProgressAsync(jobId, <ultimo indice>, "Paused")` 1x E `DeleteJobAsync` NAO recebido E
    `CreateTranslatedEpubAsync` NAO recebido; (b) `TranslateChapterAsync` com token cancelado ->
    lanca ao iterar; (c) idem `TranslateParagraphsAsync`; (d) traducao com `<script>` e `&` -> o html
    entregue a `CreateTranslatedEpubAsync` contem `&lt;script&gt;`/`&amp;` e NAO o `<script>` cru;
    (e) `PauseTranslationAsync` sem job ativo -> `UpdateJobProgressAsync` nunca chamado.
  - `OperationCanceledException` nunca engolida. Reusar `SetupBookForTranslation`,
    `SetupCacheForRebuild`, `SynchronousProgress` — DRY.
- **Test:** `dotnet test --filter FullyQualifiedName~TranslationManagerTests`
- **Commit:** `test(regression-suite): pin translation cancellation and html encoding`
- **Status:** pending

## Wave 2 — gate final

#### T-6: guardrail estrutural + contagem agregada  (DoD 5, 6)
- **Risco:** as 2 metricas de contagem se confundem facil (167 atributos vs 169 executados) e o
  guardrail `-6` e estrutural — sem medicao explicita, regressao real passa pelo Gate 2.
- **Files modified:** `.jdi/phases/regression-suite/SUMMARY.md`
- **Dependencies:** T-1, T-2, T-3, T-4, T-5
- **Acceptance:** (e GATE: falhando, volta para T-1..T-5 — nunca se resolve com codigo de producao)
  - `grep -c "net10.0-windows" test/TranslateReader.Tests/TranslateReader.Tests.csproj` = 0;
    `find test -name "*.csproj" | wc -l` = 1; csproj sem diff na fase.
  - `grep -rhoE "\[Fact\]|\[Theory\]" test/TranslateReader.Tests --include=*.cs | wc -l` >= **188**
    (167 + 21 minimos; DoD exige >= 175 -> 13 de margem).
  - `dotnet test` -> **>= 190 passed, exatamente 2 skipped, 0 failed**; os 3 numeros no SUMMARY.
  - `git diff --stat 4285f25..HEAD -- src/` sem arquivo desta fase; Gate 5.17 limpo (regra 2).
  - SUMMARY registra: provas de discriminacao (regra 3), o gap de ordem por `UpdatedAt` (T-1) e que
    os 2 gaps de `D-...-5` seguem abertos por decisao.
- **Test:** `dotnet test` (suite completa, sem filtro)
- **Commit:** `docs(regression-suite): record regression baseline metrics`
- **Status:** pending

## Fora do plano, com motivo (nao e esquecimento)
- **`ParsingEngine`** (alvo de phase 13, 18 testes): todos dependem de 3 EPUBs reais em `TestData/`.
  Teste NOVO ali seria I/O de disco (proibido por §6) e fixture em memoria exigiria seam de producao
  (recebe path, nao stream) = refactor. Os 18 legados seguem sendo a rede do arquivo.
- **`TranslationEngine`** + branch "imagens ja extraidas": gaps deliberados `D-...-5`.
  **PageModels/Pages/MauiProgram:** travados fora pela opcao (c) de `D-...-2`.
- **Gate 3 (coverage) deve dar WARN, nao BLOCK:** o unico arquivo novo e de TESTE e coverlet exclui
  a test assembly do relatorio. **Nao adicionar codigo de producao para "consertar" isso.**

## Execution
- Tasks: 6 | Waves: 2 | Wave 1 = 5 tasks independentes -> speedup ~5x
- Atributos: +21 no piso -> 188 (DoD >= 175). Execucao: >= 190 passed / 2 skipped / 0 failed.

## Files modified (todas as tasks)
- `test/TranslateReader.Tests/BookTranslationJobAccessTests.cs` (novo)
- `test/TranslateReader.Tests/{BooksAccess,ReadingManager,HtmlInjection,TranslationManager}Tests.cs`
- `.jdi/phases/regression-suite/SUMMARY.md`  (nada em `src/`, nenhum `.csproj`)

## Test requirements
- Unit: `dotnet test` (xUnit 2.9.3 + NSubstitute 5.3.0, TFM `net10.0`); cobertura 90% (D-6) so em
  arquivos novos (ver nota do Gate 3); lint `dotnet format --verify-no-changes` por commit.
