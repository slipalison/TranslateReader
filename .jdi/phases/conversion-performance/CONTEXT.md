# Phase 16: Validacao funcional e performance da conversao — Context (slug: conversion-performance)

## Goal
Provar por teste que conversao de livro, extracao de imagens e download de modelo funcionam de
ponta a ponta em livro CURTO e GRANDE (fixtures reais ja no repo), e corrigir os gargalos nomeados
que a validacao expuser — comecando pelo `ExtractAllImagesAsync`.

## Locked decisions
- D-2026-07-31-conversion-performance-0: registro da phase + evidencia medida (ja em DECISIONS.md).
- D-2026-07-31-conversion-performance-1: medicao = teste in-process deterministico via
  `GC.GetTotalMemory(forceFullCollection: true)` (nao BenchmarkDotNet, nao cronometro). Prova o
  PICO de memoria retida durante a extracao de imagens do fixture Wardley (32MB/229 imagens LOH)
  fica bem abaixo dos 44MB totais.
- D-2026-07-31-conversion-performance-2: escopo = Core (`ParsingEngine`, `ReadingManager`,
  `LibraryManager`, `ModelAccess`, `BooksAccess`/`ReadingStateAccess`) com os 3 fixtures reais. UI
  MAUI e device real (Android/iOS) -> `## Deferred to PR review` (espelha
  D-2026-07-30-regression-suite-2).
- D-2026-07-31-conversion-performance-3 (CORRECAO do diagnostico do achado ancora, pesquisado nesta
  sessao): `ReadEpubSafeAsync` usa `EpubReader.ReadBookAsync` — API EAGER que ja carrega TODO o
  conteudo (as 229 imagens, 44MB) na memoria durante o parse; `EpubBook` nao tem lazy-load
  (confirmado: vers-one/EpubReader docs, `ReadBookAsync` "reads all content into memory", vs
  `OpenBookAsync`/`EpubBookRef` que le sob demanda). Logo o `Dictionary<string, byte[]>` de
  `ExtractAllImagesAsync` NAO e a causa raiz por si — ele so copia REFERENCIAS (nao bytes); os 44MB
  ja estao residentes antes do dicionario existir. Trocar so Dictionary->IAsyncEnumerable MANTENDO
  `ReadBookAsync` nao reduziria o pico de memoria real — pareceria corrigido sem corrigir (mesma
  familia de proxy-que-nao-prova ja catalogada varias vezes em `.jdi/todos.md`). Fix real LOCKED:
  `ExtractAllImagesAsync` passa a usar `EpubReader.OpenBookAsync` (retorna `EpubBookRef`, leitura
  sob demanda), com as MESMAS opcoes de tolerancia hoje em `ReadEpubSafeAsync` (strict + fallback),
  lendo e emitindo UMA imagem por vez, descartando a referencia antes de ler a proxima, e
  descartando o `EpubBookRef` ao fim. Escopo do lazy-switch fica LIMITADO a este metodo — os outros
  5 metodos de `IParsingEngine` continuam em `ReadEpubSafeAsync`/eager (menor risco/diff; estender
  o lazy-switch a eles vira candidato futuro em `todos.md`, nao decidido aqui).
- D-2026-07-31-conversion-performance-4: contrato — `IParsingEngine.ExtractAllImagesAsync` passa a
  `IAsyncEnumerable<ExtractedImage> ExtractAllImagesAsync(string filePath)` (novo record
  `ExtractedImage(string RelativePath, byte[] Content)` em `Models/`), no lugar de
  `Task<IReadOnlyDictionary<string, byte[]>>`. Contagem de operacoes do contrato permanece 5 (sem
  crescimento). `ReadingManager.ExtractImagesIfNeededAsync` passa a `await foreach`, escrevendo e
  descartando cada imagem sem acumular seu proprio dicionario.
- D-2026-07-31-conversion-performance-5 (biblioteca/leitura — medido/raciocinado, sem fix nesta
  fase): (a) `LibraryManager.ListBookSummariesAsync` tem N+1 estrutural real (1 round-trip SQLite
  por livro via `FetchProgressAsync`) — forma imperfeita, mas sem evidencia de impacto perceptivel
  em escala realista (SQLite local, dezenas de livros); registrado em `todos.md` como candidato
  futuro se a biblioteca crescer — resposta honesta "medimos e nao ha gargalo confirmado" pedida
  pelo brief. (b) TODOS os metodos publicos de `ParsingEngine` reabrem+reparseiam o EPUB inteiro a
  cada chamada (`ReadEpubSafeAsync`/eager) — `ExtractChapterContentAsync` (1 por navegacao de
  capitulo) reparseia o livro TODO a cada troca de capitulo; achado real para "leitura", mesma
  causa raiz do achado ancora (D-...-3), mas corrigi-lo exige cache de `EpubBook`/`EpubBookRef` com
  gestao de ciclo de vida/disposal/concorrencia (`.claude/rules/csharp.md` §3) — escopo maior que
  esta fase comporta; registrado em `todos.md` como achado NOMEADO, nao descartado em silencio.
- D-2026-07-31-conversion-performance-6: dois defeitos ja registrados em `todos.md § coverage-90`.
  (a) `ExtractCoverImageAsync` byte[0] vs null — CORRIGIDO nesta fase (guarda `Length > 0`, mesmo
  arquivo ja tocado por D-...-3/-4; teste de caracterizacao existente aperta
  `Assert.Empty(cover ?? [])` -> `Assert.Null(cover)`). (b) handle de zip aberto no fallback de
  `ReadEpubSafeAsync` — DEFERIDO (exige investigar internals do VersOne.Epub alem do orcamento
  desta fase; so ocorre no caminho de fallback, nao exercitado pelos 3 fixtures reais usados na
  validacao desta fase); continua registrado em `todos.md`.
- D-2026-07-31-conversion-performance-7: `CreateTranslatedEpubAsync`'s `FirstOrDefault` O(entries×
  capitulos) confirmado NAO dominante (1.215-6.578 comparacoes de string nos 3 fixtures, per brief)
  — FORA de escopo integralmente (nem como higiene), registrado em `todos.md`.

## Canonical refs
- Card colado via `/jdi-issue` em 2026-07-31 (sem tracker/URL) — ver D-...-0.
- `test/TranslateReader.Tests/TestData/` (3 fixtures reais: Practice/Righting/Wardley).
- vers-one/EpubReader docs (`ReadBookAsync` vs `OpenBookAsync`, `EpubBook` vs `EpubBookRef`),
  pesquisado nesta sessao (2 lookups, orcamento do asker).
- `.claude/rules/csharp.md` §2 (LOH/alocacao), §3 (concorrencia), §6 (testes).
- `.jdi/todos.md` § `coverage-90` (os 2 defeitos de `ParsingEngine.cs`).

## Out of scope
- `CreateTranslatedEpubAsync` O(entries×capitulos) — nao dominante (D-...-7).
- Handle de zip aberto no fallback de `ReadEpubSafeAsync` — deferido (D-...-6b).
- N+1 de `ListBookSummariesAsync` — sem gargalo medido em escala realista (D-...-5a).
- Lazy-switch dos outros 5 metodos de `IParsingEngine` (metadata/chapters/chapter-content/
  cover/translated-epub) — mesma causa raiz do achado ancora, escopo maior, candidato futuro
  (D-...-5b, D-...-3).
- BenchmarkDotNet/`dotnet-counters` — infra nao introduzida (mesma escolha de
  D-2026-07-30-the-method-refactor-2B); consistente com D-...-1.

## Definition of Done

### Auto-verifiable
- [ ] Conversao valida ponta-a-ponta nos fixtures CURTO (Practice) e GRANDE (Wardley): metadata,
      capitulos, conteudo de capitulo e imagens extraem sem excecao, contagens batem com o EPUB.
      **Verify:** `dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~ParsingEngineFixtureValidationTests"`
      **Source:** CONTEXT (D-...-0, D-...-2)

- [ ] `IParsingEngine.ExtractAllImagesAsync` streama (`IAsyncEnumerable<ExtractedImage>`), nao
      materializa `IReadOnlyDictionary<string, byte[]>`.
      **Verify:** `grep -q "IAsyncEnumerable<ExtractedImage> ExtractAllImagesAsync" src/TranslateReader.Core/Contracts/Engines/IParsingEngine.cs && ! grep -q "IReadOnlyDictionary<string, byte\[\]>> ExtractAllImagesAsync" src/TranslateReader.Core/Contracts/Engines/IParsingEngine.cs`
      **Source:** CONTEXT (D-...-4)

- [ ] `ExtractAllImagesAsync` usa `OpenBookAsync` (lazy), nao herda o eager-load de
      `ReadEpubSafeAsync` — a causa raiz real do achado ancora.
      **Verify:** `awk "/ExtractAllImagesAsync/,/^    }/" src/TranslateReader.Core/Business/Engines/ParsingEngine.cs | grep -q "OpenBookAsync"`
      **Source:** CONTEXT (D-...-3)

- [ ] Pico de memoria retida ao extrair as 229 imagens do fixture Wardley (44MB totais) fica
      limitado (nao materializa o livro inteiro de uma vez) — prova MEDIDA, nao so estrutural.
      **Verify:** `dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~ParsingEngineMemoryTests"`
      **Source:** CONTEXT (D-...-1, D-...-3)

- [ ] `ExtractCoverImageAsync` retorna `null` (nao `byte[0]`) quando a capa do manifesto aponta pra
      arquivo ausente — defeito de `todos.md § coverage-90` fechado.
      **Verify:** `grep -q "ExtractCoverImageAsync_WithACoverImagePropertyPointingAtAMissingFile_ReturnsNoBytes" test/TranslateReader.Tests/ParsingEngineEdgeCaseTests.cs && grep -A3 "ExtractCoverImageAsync_WithACoverImagePropertyPointingAtAMissingFile_ReturnsNoBytes" test/TranslateReader.Tests/ParsingEngineEdgeCaseTests.cs | grep -q "Assert.Null(cover)"`
      **Source:** CONTEXT (D-...-6a)

- [ ] Download de modelo (`ModelAccess.DownloadModelAsync`) permanece validado (streaming
      bufferizado, sem materializar o arquivo inteiro) — suite existente de `coverage-90` continua
      verde.
      **Verify:** `grep -q "DownloadModelAsync" test/TranslateReader.Tests/ModelAccessTests.cs && dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~ModelAccessTests"`
      **Source:** CONTEXT (D-...-2; teste ja existe desde D-2026-07-31-coverage-90-3)

- [ ] Suite completa passa (nenhum teste existente regride ou e removido).
      **Verify:** `dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release`
      **Source:** CONTEXT (guardrail de baseline)

### Manual
- _(none)_

## Deferred to PR review
- Comportamento em device real Android/iOS (memoria/bateria) — sem harness/emulador neste
  ambiente (D-...-2).
- Confirmacao funcional/visual da UI MAUI (import, leitura, troca de tema, download de modelo) —
  sem harness (D-2026-07-30-regression-suite-2).
- Confirmacao remota do SonarCloud de "zero issue nova" — analisadores nao rodam local (mesmo
  limite de D-2026-07-30-sonar-zero-issues-6/D-2026-07-31-coverage-90-6).
- Responsividade percebida (UX) ao navegar capitulos em livro grande — achado D-...-5b e
  qualitativo/de produto, nao verificavel por comando.

## Notes
Achado ancora CORRIGIDO nesta sessao (D-...-3): o problema real e `ReadBookAsync` (eager), nao o
`Dictionary` em si — ver pesquisa em `## Canonical refs`. O teste de memoria (item 4 do DoD) existe
justamente para pegar uma implementacao que só troca Dictionary por IAsyncEnumerable mas mantém
`ReadBookAsync` por baixo — passaria nos greps estruturais e falharia na medição.
