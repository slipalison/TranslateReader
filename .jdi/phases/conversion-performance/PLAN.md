# Phase 16: Validacao funcional e performance da conversao — Plan  (slug: conversion-performance)

## Goal
Provar por teste que conversao, extracao de imagens e download de modelo funcionam ponta-a-ponta em
livro CURTO (Practice) e GRANDE (Wardley), e corrigir o achado ancora: `ExtractAllImagesAsync` passa
a streamar via `EpubReader.OpenBookAsync` (lazy), com prova MEDIDA de memoria.

## Locked decisions (CONTEXT.md / DECISIONS.md)
- D-...-1: medicao = `GC.GetTotalMemory(forceFullCollection: true)` in-process. Sem BenchmarkDotNet, sem cronometro.
- D-...-3: fix real = `OpenBookAsync`/`EpubBookRef` com as MESMAS opcoes de tolerancia (strict + fallback); lazy-switch LIMITADO a `ExtractAllImagesAsync`.
- D-...-4: `IAsyncEnumerable<ExtractedImage> ExtractAllImagesAsync(string filePath)` + record `ExtractedImage(string RelativePath, byte[] Content)`; `ReadingManager` usa `await foreach`.
- D-...-6a: `ExtractCoverImageAsync` `byte[0]` -> `null`. D-...-5a/5b/6b/7: fora de escopo, ja em `todos.md`.
- D-1/D-2/D-6: The Method; boundary `4285f25`; 90% em linha nova/alterada.

## Riscos nomeados (tratar, nao descobrir)
1. **DoD com `Verify:` fraco (itens 1, 4, 6, 7).** `dotnet test --filter X` sozinho nao prova nada:
   filtro que casa ZERO teste pode sair 0, e "o runner terminou" nao prova que existe assercao de
   memoria. Endurecido em **T-1** (decisao NOVA D-...-8 append-only, depois a linha do CONTEXT.md).
2. **Call sites de `ExtractAllImagesAsync`** (grep completo): producao = `ReadingManager.cs:56`
   APENAS; testes = `ReadingManagerTests.cs:54,120,149` + `ParsingEngineTests.cs:102,157,196`.
   `MauiProgram.cs:79` so registra `IParsingEngine` no DI — **nenhum consumidor do app MAUI chama o
   metodo**, entao a mudanca de contrato nao gera diff em `src/TranslateReader/`. Se o build MAUI
   quebrar, e sinal de consumidor nao mapeado: parar e reportar.
3. `await _parsingEngine.DidNotReceive().ExtractAllImagesAsync(...)` **nao compila** (IAsyncEnumerable
   nao e awaitable); `Record.ExceptionAsync(() => ...)` idem. Adaptar, nao remover.
4. **Custo de tempo.** Wardley (32MB / 45MB descomprimido) ja e parseado ~5x na suite; a phase soma
   ~6 enumeracoes + ~8 full-GC forcados: **+20-40s**, suite total na casa de 2-3 min. Testes novos
   sobre Wardley levam `[Trait("Category","Slow")]` so para observabilidade — **sem filtro de
   exclusao no CI** (excluir enfraqueceria o gate).
5. **Teste de memoria flaky e pior que nenhum** (vira ruido que alguem desliga). Mitigacao integral
   obrigatoria em T-4.

## Tasks

### Wave 1 (parallel-eligible)

#### T-1: Endurecer os 4 `Verify:` fracos do DoD (itens 1, 4, 6, 7)
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `.jdi/DECISIONS.md`, `.jdi/phases/conversion-performance/CONTEXT.md`
- **Acceptance:**
  - `D-2026-07-31-conversion-performance-8` **anexado ao fim** de DECISIONS.md (D-...-0..7 intocadas):
    supersede so as linhas `**Verify:**` dos itens 1, 4, 6, 7 — motivo: filtro que casa zero teste
    passa vazio; rodar teste nao prova assercao. Declara tambem o ratchet pendente de
    `todos.md § [PROCESSO/DoD]`: piso `Total >= 316` (baseline publicada 304 + 12), fixado ANTES da corrida.
  - Append-only provado: `git diff .jdi/DECISIONS.md | grep -c '^-[^-]'` retorna `0`.
  - CONTEXT.md: bullet de D-...-8 em `## Locked decisions` + as 4 linhas abaixo. Itens 2, 3, 5 ficam
    **intocados** (ja provam propriedade estrutural). `$P` =
    `test/TranslateReader.Tests/TranslateReader.Tests.csproj` e `PASS(f)` =
    `sed -n 's/.*Passed: *\([0-9]*\).*/\1/p' f | head -1` sao expandidos INLINE (Verify roda sem setup).
  - Item 1: `dotnet test $P -c Release --filter "FullyQualifiedName~ParsingEngineFixtureValidationTests" >/tmp/tr1.log 2>&1 && grep -q "Passed!" /tmp/tr1.log && test "$(PASS /tmp/tr1.log)" -ge 10 && test "$(grep -c 'Assert\.' test/TranslateReader.Tests/ParsingEngineFixtureValidationTests.cs)" -ge 20`
  - Item 4: `test "$(grep -c -e 'GC.GetTotalMemory(forceFullCollection: true)' -e 'MaxRetainedBytes = 20L \* 1024 \* 1024' -e 'peakRetainedDelta < MaxRetainedBytes' test/TranslateReader.Tests/ParsingEngineMemoryTests.cs)" -ge 3 && dotnet test $P -c Release --filter "FullyQualifiedName~ParsingEngineMemoryTests" >/tmp/tr4.log 2>&1 && grep -q "Passed!" /tmp/tr4.log && test "$(PASS /tmp/tr4.log)" -ge 1`
  - Item 6: `grep -q "HttpCompletionOption.ResponseHeadersRead" src/TranslateReader.Core/Access/ModelAccess.cs && ! grep -qE "ReadAsByteArrayAsync|ReadAsStringAsync" src/TranslateReader.Core/Access/ModelAccess.cs && dotnet test $P -c Release --filter "FullyQualifiedName~ModelAccessTests" >/tmp/tr6.log 2>&1 && grep -q "Passed!" /tmp/tr6.log && test "$(PASS /tmp/tr6.log)" -ge 15`
  - Item 7: `dotnet test $P -c Release >/tmp/tr7.log 2>&1 && grep -q "Passed!" /tmp/tr7.log && test "$(sed -n 's/.*Failed: *\([0-9]*\).*/\1/p' /tmp/tr7.log | head -1)" -eq 0 && test "$(sed -n 's/.*Total: *\([0-9]*\).*/\1/p' /tmp/tr7.log | head -1)" -ge 316`
- **Dependencies:** none
- **Test:** n/a (docs). Auto-teste: hoje os 4 comandos retornam != 0 (arquivos ainda nao existem) —
  prova que nao passam vazio.
- **Status:** pending

#### T-2: `ExtractAllImagesAsync` lazy + contrato streaming
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Models/ExtractedImage.cs` (novo),
  `src/TranslateReader.Core/Contracts/Engines/IParsingEngine.cs`,
  `src/TranslateReader.Core/Business/Engines/ParsingEngine.cs`,
  `src/TranslateReader.Core/Business/Managers/ReadingManager.cs`,
  `test/TranslateReader.Tests/ParsingEngineTests.cs`, `test/TranslateReader.Tests/ReadingManagerTests.cs`
- **Acceptance:**
  - `record ExtractedImage(string RelativePath, byte[] Content)` em `Models/`; contrato passa a
    `IAsyncEnumerable<ExtractedImage> ExtractAllImagesAsync(string filePath)` (5 operacoes, sem `Task<`).
  - Implementacao = iterador `async IAsyncEnumerable<...>`: `using var bookRef = await
    OpenEpubSafeAsync(filePath);` e, por item de `bookRef.Content.Images.Local`
    (`EpubLocalByteContentFileRef`), `await imgRef.ReadContentAsBytesAsync()` -> `yield return`.
    **Sem colecao acumuladora.** `EpubBookRef` e `IDisposable` e detem o handle do zip (§2.4) — o
    `using` no iterador tambem cobre `break` do consumidor.
  - `try/catch` nao pode envolver `yield return`: `OpenEpubSafeAsync` e `private static async
    Task<EpubBookRef>` a parte, espelhando `ReadEpubSafeAsync` (strict -> `catch
    (EpubPackageException)` -> fallback). As opcoes viram builders compartilhados
    (`BuildStrictOptions()`/`BuildFallbackOptions()`) usados pelos DOIS metodos — e assim que "MESMAS
    opcoes de tolerancia" (D-...-3) fica provado por estrutura, nao por copia/cola.
  - `ReadingManager.ExtractImagesIfNeededAsync`: `await foreach (var (relativePath, content) in
    parsingEngine.ExtractAllImagesAsync(epubPath))`, escreve e descarta; guarda `DirectoryHasContent`
    preservada.
  - Os 3 testes de `ParsingEngineTests` (`:100`, `:155`, `:194`) adaptados para `await foreach`, com
    assercoes equivalentes ou mais fortes (`>= 100` do Wardley preservado). Nada deletado/afrouxado.
  - `ReadingManagerTests`: helper `private static async IAsyncEnumerable<ExtractedImage> AsAsync(
    params ExtractedImage[] items)` (com `await Task.Yield()` para nao emitir CS1998) nos stubs
    `:54`/`:120`; `:149` vira chamada **sem `await`**. As 3 assercoes de `WriteFileAsync` intactas.
  - `dotnet build` limpo em Core + nos 4 TFMs do app (contrato publico mudou).
- **Dependencies:** none
- **Test:** `ParsingEngineTests` (3 adaptados) + `ReadingManagerTests` (3 adaptados).
- **Status:** pending

### Wave 2

#### T-3: Cobertura do caminho lazy novo (edge cases sinteticos)
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `test/TranslateReader.Tests/ParsingEngineEdgeCaseTests.cs`
- **Acceptance:**
  - `..._StreamsEachLocalImageWithItsRelativePath` sobre `CreateRichEpub()`: exatamente 1 item,
    `RelativePath` = `OEBPS/images/pic.png`, `Content` == `PngBytes`.
  - `..._WithAPackageMissingItsMetadataNode_UsesTheLenientFallback`: fixture sem `<metadata>` (padrao
    de `:58`) + 1 imagem no manifesto -> cobre o `catch (EpubPackageException)` de
    `OpenEpubSafeAsync`. **Sem este teste a linha nova do fallback fica a 0% e o gate D-6/Sonar New
    Code reprova** — os 3 fixtures reais nunca entram no fallback.
  - `..._WithoutAnyImage_YieldsNothing` (livro so-texto).
  - `..._WhenTheConsumerBreaksEarly_ReleasesTheArchiveHandle`: `break` apos o 1o item e depois
    `File.Delete(path)` sem excecao (prova o `using` do `EpubBookRef`).
  - I/O de disco em teste: excecao NOMEADA vigente (cabecalho do proprio arquivo, linhas 11-13 —
    D-2026-07-31-coverage-90-3 + precedente `ParsingEngineTests`); citar no comentario do bloco novo.
- **Dependencies:** T-2
- **Test:** os 4 `[Fact]` acima (strict + fallback + disposal de `OpenEpubSafeAsync`).
- **Status:** pending

#### T-4: `ParsingEngineMemoryTests` — prova MEDIDA do pico retido
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `test/TranslateReader.Tests/ParsingEngineMemoryTests.cs` (novo)
- **Acceptance:**
  - `const long MaxRetainedBytes = 20L * 1024 * 1024;` (44MB medidos no defeito, ~5MB esperados no
    lazy: 2,2x abaixo do defeito e ~4x acima do esperado) + assercao literal
    `Assert.True(peakRetainedDelta < MaxRetainedBytes, $"... {peakRetainedDelta / 1024 / 1024} MB ...")`.
  - Anti-flakiness, todos obrigatorios: (a) **warm-up** enumerando Practice inteiro antes de medir
    (JIT, statics, buffers da lib); (b) baseline `GC.Collect(); GC.WaitForPendingFinalizers();
    GC.GetTotalMemory(forceFullCollection: true)` imediatamente antes do laco; (c) pico = MAIOR
    `GC.GetTotalMemory(forceFullCollection: true)` amostrado DENTRO do laco a cada 32 imagens (~8
    amostras — full-GC deixa so o retido, nao o lixo); (d) o teste **nao retem** as imagens (acumula
    so `count` e `long totalBytes`); (e) classe sob `[CollectionDefinition("NonParallel",
    DisableParallelization = true)]` + `[Collection("NonParallel")]` — `GetTotalMemory` e do PROCESSO
    e outro teste parseando Wardley em paralelo poluiria a medida; (f) **zero assercao de tempo**.
  - Anti-vacuidade no mesmo teste: `Assert.True(count >= 200)` e
    `Assert.True(totalBytes > 35L * 1024 * 1024)` — implementacao que nao emite nada nao passa so por
    ser economica.
  - `[Trait("Category","Slow")]`; custo alvo < 30s.
- **Dependencies:** T-2
- **Test:** ele proprio (item 4 do DoD, com o `Verify:` endurecido de T-1) + mutacao em T-7.
- **Status:** pending

#### T-5: `ParsingEngineFixtureValidationTests` — ponta-a-ponta curto vs grande
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `test/TranslateReader.Tests/ParsingEngineFixtureValidationTests.cs` (novo)
- **Acceptance:**
  - 10 `[Fact]` = 2 fixtures (Practice, Wardley) x 5 propriedades: metadata (titulo/autor/idioma nao
    vazios, `TotalChapters > 0`); contagem de capitulos; conteudo de TODO capitulo (nao vazio, sem
    `src="../"`); contagem de imagens; bytes de imagem (`Length > 0`, `RelativePath` unico).
  - **Contagens batem com o EPUB** por oraculo INDEPENDENTE, nao pelo proprio engine:
    `ZipFile.OpenRead(fixture)` -> nº de imagens = entries com extensao de imagem; nº de capitulos =
    `<itemref>` do `.opf` lido com `XmlReader` + `DtdProcessing.Prohibit` (§4, XXE). Igualdade
    exata em imagens e `chapters.Count == spineCount`.
  - >= 20 `Assert.` no arquivo (piso do Verify endurecido). Wardley -> `[Trait("Category","Slow")]`.
  - Usa os 3 fixtures ja existentes em `TestData/` — **nenhum fixture novo** (Righting ja tem smoke
    em `ParsingEngineTests`).
- **Dependencies:** T-2
- **Test:** os 10 `[Fact]` acima (item 1 do DoD).
- **Status:** pending

### Wave 3

#### T-6: `ExtractCoverImageAsync` — `byte[0]` vira `null` (D-...-6a)
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Business/Engines/ParsingEngine.cs`,
  `test/TranslateReader.Tests/ParsingEngineEdgeCaseTests.cs`
- **Acceptance:**
  - **Ordem obrigatoria (§6: bugfix comeca por teste que falha):** 1º apertar
    `ParsingEngineEdgeCaseTests.cs:196` de `Assert.Empty(cover ?? [])` para `Assert.Null(cover)` e
    rodar -> **FALHA** (o teste frouxo de hoje documenta o defeito; o aperto E a reproducao);
    2º aplicar em `FindCoverInManifest` (`ParsingEngine.cs:316`) a guarda irma das linhas 72/75
    (`return imageFile?.Content is { Length: > 0 } bytes ? bytes : null;`); 3º rodar -> PASSA.
    Transcript das duas rodadas no SUMMARY.
  - `..._WithAnEmptyDeclaredCover_...` (`:172`) e os 2 testes de capa dos fixtures reais seguem
    verdes — a guarda nao pode matar capa valida.
  - Nome do teste preservado (o `Verify:` do item 5 do DoD faz grep pelo nome); so a assercao muda.
- **Dependencies:** T-2 (mesmo arquivo de producao), T-3 (mesmo arquivo de teste)
- **Test:** `ExtractCoverImageAsync_WithACoverImagePropertyPointingAtAMissingFile_ReturnsNoBytes`.
- **Status:** pending

### Wave 4

#### T-7: Prova por mutacao + rodada final dos 7 `Verify:`
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `.jdi/phases/conversion-performance/SUMMARY.md`
- **Acceptance:**
  - **Mutacao (prova que o teste de memoria mede algo):** reverter TEMPORARIAMENTE
    `ExtractAllImagesAsync` para o eager (`ReadEpubSafeAsync` + dicionario), rodar
    `ParsingEngineMemoryTests` -> tem que **FALHAR** com delta na casa de 40+ MB; restaurar o lazy
    (`git checkout` do arquivo) -> passa. Transcript das duas rodadas, com os MB observados, no
    SUMMARY. Sem isso, "prova MEDIDA" volta a ser "o runner terminou".
  - Os 7 `Verify:` do DoD (4 endurecidos + 3 originais) rodados como escritos, saida colada no
    SUMMARY, todos exit 0; `Total:` real da suite registrado (piso 316).
  - `git status` confirma que `.gitignore` (alteracao local do usuario) nao entrou em commit algum.
- **Dependencies:** T-1, T-2, T-3, T-4, T-5, T-6
- **Test:** suite completa `-c Release` + mutacao documentada.
- **Status:** pending

## Execution
- Total tasks: 7 | Waves: 4 (W1: T-1, T-2 | W2: T-3, T-4, T-5 | W3: T-6 | W4: T-7)
- Speedup paralelo estimado: ~1,8x | 7 commits atomicos, escopo `conversion-performance`.

## Files modified (todas as tasks)
- Core: `Models/ExtractedImage.cs` (novo), `Contracts/Engines/IParsingEngine.cs`,
  `Business/Engines/ParsingEngine.cs`, `Business/Managers/ReadingManager.cs`
- Testes: `ParsingEngineTests.cs`, `ReadingManagerTests.cs`, `ParsingEngineEdgeCaseTests.cs`,
  `ParsingEngineMemoryTests.cs` (novo), `ParsingEngineFixtureValidationTests.cs` (novo)
- JDI: `.jdi/DECISIONS.md`, `.jdi/phases/conversion-performance/CONTEXT.md`, `.../SUMMARY.md`
- NAO tocar: `src/TranslateReader/**` (MAUI), `.gitignore`, `.jdi/todos.md` (ja anotado pelo asker)

## Test requirements
- `dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release`
- Baseline intocavel: 304 testes C# (302p/2s) + 60 JS. Piso pos-phase: `Total >= 316`, `Failed: 0`.
- Cobertura minima **90%** em linha nova/alterada (D-6). Mapa: iterador + `OpenEpubSafeAsync` strict
  -> T-2/T-3/T-5; fallback -> T-3; `ExtractedImage` -> T-2/T-3; loop do `ReadingManager` -> T-2;
  guarda de capa -> T-6. Nenhuma linha de producao nova sem teste nomeado.
