# Phase 16: Validacao funcional e performance da conversao — Summary  (slug: conversion-performance)

**Status:** complete
**Tasks:** 7/7 completas, 0 blocked
**Branch:** `jdi/conversion-performance` (base `main` @ `ad607ac`)

## O que foi feito, por task

- **T-1** — `D-2026-07-31-conversion-performance-8` anexada (append-only) endurecendo os `Verify:`
  dos itens 1, 4, 6 e 7 do DoD: piso de `Passed:`, literais obrigatorios no arquivo de teste e
  ratchet `Total >= 316` (baseline 304 + 12), fixado ANTES da corrida, com `Failed: 0`. As 4 linhas
  refletidas no CONTEXT.md; itens 2, 3 e 5 deixados intocados conforme o PLAN.
- **T-2** — `ExtractAllImagesAsync` passou a `IAsyncEnumerable<ExtractedImage>` sobre
  `EpubReader.OpenBookAsync` (`EpubBookRef`, `using` dentro do iterador). Fallback num
  `OpenEpubSafeAsync` separado (C# proibe `yield return` em `try/catch`) e as opcoes de tolerancia
  viraram `BuildStrictOptions()`/`BuildFallbackOptions()` compartilhados com `ReadEpubSafeAsync` —
  "mesmas opcoes" provado por estrutura. `ReadingManager` usa `await foreach`. Novo record
  `Models/ExtractedImage.cs`. 6 call sites de teste adaptados, nenhum deletado ou afrouxado.
- **T-3** — 4 `[Fact]` novos em `ParsingEngineEdgeCaseTests`: stream feliz, **fallback lenient do
  `OpenEpubSafeAsync`**, livro so-texto, e `break` do consumidor + `File.Delete` (prova o disposal).
- **T-4** — `ParsingEngineMemoryTests.cs` (novo): mede o pico de memoria retida no fixture grande.
- **T-5** — `ParsingEngineFixtureValidationTests.cs` (novo): 10 `[Fact]`, 2 fixtures x 5
  propriedades, contagens conferidas por **oraculo independente** (entries do zip + `<itemref>` do
  `.opf` lido com `XmlReader` + `DtdProcessing.Prohibit`, secao 4 / XXE). 28 linhas com `Assert.`.
- **T-6** — bugfix `byte[0]` -> `null` em `FindCoverInManifest`, comecando pelo teste vermelho.
- **T-7** — prova por mutacao + rodada final dos 7 `Verify:` + este SUMMARY.

## Commits (7 desta execucao)

| sha | subject |
|---|---|
| `ff4a735` | docs(conversion-performance): harden the four weak DoD Verify commands |
| `a7d0533` | perf(conversion-performance): stream book images off the lazy EpubBookRef |
| `0d4a6f7` | test(conversion-performance): cover the lazy image stream and its fallback |
| `7890004` | test(conversion-performance): measure the retained peak of the image stream |
| `eb526b6` | test(conversion-performance): validate conversion end to end, short book vs large |
| `742a93c` | fix(conversion-performance): return null, not byte[0], for an orphan manifest cover |
| `4d01006` | docs(conversion-performance): fix the unsatisfiable Verify of DoD item 3 |

## PROVA POR MUTACAO (T-7) — o teste de memoria mede algo

Mutante aplicado em `ExtractAllImagesAsync`: caminho **eager** (`ReadEpubSafeAsync` + `Dictionary`)
atras da MESMA fachada `IAsyncEnumerable` — exatamente o "fix falso" que passaria em qualquer grep
estrutural. Transcript:

```
MUTANT APPLIED: eager ReadBookAsync + Dictionary behind the same IAsyncEnumerable facade
  Pico de memoria retida durante a extracao: 46 MB (teto 20 MB). O caminho eager retinha ~44 MB.
Failed!  - Failed: 1, Passed: 0, Skipped: 0, Total: 1
```

Restaurado (`git checkout`) e re-rodado:

```
Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 182 ms
```

Numero real do caminho lazy (obtido baixando temporariamente o teto para 1 byte e imprimindo
bytes — probe descartada, nao commitada):

```
Pico de memoria retida: 377792 bytes (0,36 MB) | count=256 | totalBytes=43986604 (~42 MiB)
```

Antes 46 MB retidos, depois 0,36 MB, streamando os mesmos 256 arquivos e 42 MiB de bytes.
**Reducao de ~127x no pico retido**; a folga ate o teto de 20 MB e de ~54x.

Prova extra de nao-vacuidade do fallback (T-3): trocando o corpo do `catch (EpubPackageException)`
de `OpenEpubSafeAsync` por `throw;`, o teste `..._UsesTheLenientFallback` fica **vermelho** — a
linha nova nao esta a 0%.

## Gates (numeros reais)

| Gate | Resultado |
|---|---|
| `dotnet build TranslateReader.slnx -c Release` | **Build succeeded. 64 Warning(s), 0 Error(s)** (todos MVVMTK0045 pre-existentes do app MAUI) |
| `dotnet test ... -c Release` | **Failed: 0, Passed: 317, Skipped: 2, Total: 319** (baseline 304, +15) |
| Tempo total da suite | **7 s** de relogio de parede (runner reporta `Duration: 4 s`). O PLAN estimava +20-40 s; o custo real dos ~8 full-GC e das ~6 enumeracoes de Wardley ficou em ~3 s |
| Cobertura de linha do projeto | **92,56%** linha / 79,78% branch |
| Cobertura das linhas NOVAS/ALTERADAS (D-6) | **100%** — `ExtractedImage.cs` 1/1, `ReadingManager.cs` 32/32, `ParsingEngine.cs` 199/200. A unica linha descoberta (`ParsingEngine.cs:76`, `return epub.CoverImage;`) e legada e nao foi tocada nesta phase |
| `dotnet format --verify-no-changes` | limpo em todos os arquivos tocados |
| Diff em `src/TranslateReader/` (MAUI) | **vazio**, como o PLAN previa (o app so registra `IParsingEngine` no DI) |
| `.gitignore` | contagem de aparicoes em `git log --name-only main..HEAD` = **0** — a alteracao local do usuario nao entrou em commit algum |

## Os 7 `Verify:` do DoD, rodados como escritos

| # | Item | exit |
|---|---|---|
| 1 | Conversao ponta-a-ponta curto vs grande | **0** (`Passed: 10`, 28 `Assert.`) |
| 2 | Contrato streama `IAsyncEnumerable<ExtractedImage>` | **0** |
| 3 | `ExtractAllImagesAsync` usa `OpenBookAsync` (lazy) | **0** (comando corrigido, ver desvio 2) |
| 4 | Pico de memoria MEDIDO | **0** (grep = 4 literais, `Passed: 1`) |
| 5 | `ExtractCoverImageAsync` retorna `null` | **0** |
| 6 | Download de modelo streamado | **0** (`Passed: 15`) |
| 7 | Suite completa sem regressao | **0** (`Failed: 0`, `Total: 319 >= 316`) |

Auto-teste registrado antes de codar: os itens 1 e 4 retornavam exit 1 e 2 (arquivos ainda
inexistentes) — prova de que nao passam vazio.

## Desvios do PLAN (com justificativa)

1. **`DOTNET_CLI_UI_LANGUAGE=en` nos 4 `Verify:` endurecidos.** O `dotnet test` desta maquina emite
   o sumario em pt-BR ("Aprovado!  - Com falha: 0, Aprovado: 302"), entao `grep -q "Passed!"` daria
   FALSO NEGATIVO local e o gate so valeria num CI em ingles. Registrado em D-...-8.
2. **`Verify:` do item 3 corrigido (D-...-9, append-only).** O comando original exigia
   `OpenBookAsync` literalmente dentro do corpo de `ExtractAllImagesAsync` — impossivel dada a
   armadilha 1 do proprio PLAN (`yield return` nao pode ficar em `try/catch`, logo o fallback vive
   em `OpenEpubSafeAsync`). Como estava, **reprovaria a implementacao correta e aprovaria uma sem
   fallback**. O comando novo prova a cadeia em 2 saltos e proibe o caminho eager nos dois;
   contra-exemplo rodado: apontar o iterador de volta para `ReadEpubSafeAsync` faz o comando sair 1.
3. **Bloco arrange do teste de capa compactado.** O `Verify:` do item 5 usa
   `grep -A3 <nome>` seguido de `grep -q "Assert.Null(cover)"`, e o arranjo de 6 linhas empurrava a
   assercao para fora da janela — o gate falharia mesmo com a correcao aplicada. O fixture virou o
   helper `CreateOrphanCoverEpub()`; **nome do teste e assercao preservados**, nada afrouxado.
4. **`ReadContentAsBytesAsync()`** confirmada como o nome real da API (o PLAN ja acertara);
   `EpubLocalByteContentFileRef.ReadContentAsync` existe como sinonimo e nao foi usada.
5. **Fixture de 2 imagens novo** (`CreateTwoImageEpub`) para o teste de `break` — com uma imagem so,
   o `break` nao demonstraria interrupcao real do stream.

## O que ficou de fora (ja decidido, nao esquecido)

- Lazy-switch dos outros 5 metodos de `IParsingEngine` (D-...-5b / D-...-3) —
  `ExtractChapterContentAsync` ainda reparseia o livro inteiro por troca de capitulo; achado
  NOMEADO em `todos.md`.
- Handle de zip vazado no fallback de `ReadEpubSafeAsync` (D-...-6b) — deferido.
- N+1 de `LibraryManager.ListBookSummariesAsync` (D-...-5a) — medido, sem gargalo confirmado.
- `FirstOrDefault` O(entries x capitulos) de `CreateTranslatedEpubAsync` (D-...-7) — nao dominante.
- Device real Android/iOS, UI MAUI e confirmacao remota do SonarCloud — `## Deferred to PR review`.
