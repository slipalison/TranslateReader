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

## Iter 2 — rodada de warnings (/jdi-issue)

O loop ja convergiu em `APPROVED_WITH_WARNINGS` na iter 1. Esta rodada trata os 6 warnings da
REVIEW. Nenhum item do DoD foi enfraquecido, nenhum `Verify:` mudou, nenhum teste foi deletado ou
afrouxado, e o diff em `src/TranslateReader/` continua vazio.

### W-1 — justificativa "impossivel" em D-...-9 — **FECHADO** (correcao de auditoria)
`D-2026-07-31-conversion-performance-10 (a)` anexada. C# so proibe `yield return` dentro de um `try`
**com `catch`** — um `try/catch` inline ANTES do laco de `yield` seria legal, entao o termo preciso e
**"insatisfazivel dentro do design locked pela T-2"**, nao "impossivel": o que elimina a forma inline
e a acceptance da T-2 (helper `OpenEpubSafeAsync` + builders compartilhados), justamente a estrutura
que prova "MESMAS opcoes de tolerancia" de D-...-3. D-...-9 **nao foi reescrita** — caminho
append-only, precedente `D-2026-07-30-sonar-zero-issues-13` corrigindo a `-12`. Conclusao de D-...-9
intacta: o comando novo segue estritamente mais forte (proibe `ReadEpubSafeAsync` no iterador e
`ReadBookAsync` no helper). Evidencia: `git diff .jdi/DECISIONS.md` = 61 insercoes, **0 remocoes**.

### W-2 — "contagem permanece 5" em D-...-4 — **FECHADO** (correcao de auditoria)
`D-...-10 (b)`. Medido: `IParsingEngine` tem **6 operacoes antes e 6 depois** — a propriedade
afirmada ("sem crescimento") esta CORRETA, so o numero estava errado; origem provavel, "os OUTROS 5
metodos" de D-...-3. As 6 excedem o "3-5 ideal" do CLAUDE.md como forma LEGADA (pre-`4285f25`,
coberta por D-2), agora NOMEADA em `.jdi/todos.md` para revisao junto com o lazy-switch (D-...-5b).
Sem acao de codigo: dividir contrato legado seria o rewrite amplo que o escopo finding-driven proibe.

### W-3 — `ExtractAllImagesAsync` sem `CancellationToken` — **FECHADO como decisao de NAO adicionar**
Avaliado, nao ignorado. Decisao: a assinatura locked por D-...-4 **fica como esta**; achado NOMEADO
em `.jdi/todos.md` + `D-...-10 (c)`. Argumento medido, nao reflexo:

1. `.claude/rules/csharp.md` §3 exige que o token **FLUA** `PageModel -> Manager -> Engine -> Access`.
   A cadeia de LEITURA e inteira token-free: `IReadingManager` (5 ops) e `IParsingEngine` (6 ops) nao
   declaram token, e os 3 call sites de producao (`ReaderPageModel.cs:112,134,154` ->
   `LoadChapterContentAsync(BookId, chapter.HRef)`) nao tem CTS no caminho de leitura — o unico CTS
   do PageModel (`:63`) serve o caminho de TRADUCAO.
2. Logo, adicionar o token **so neste membro** faria o unico consumidor de producao
   (`ReadingManager.ExtractImagesIfNeededAsync`) passar `default`: um parametro que anuncia
   cancelamento e **nao cancela nada**. Pior que a ausencia — e a mesma familia
   proxy-que-nao-prova que esta phase catalogou em D-...-3, agora aplicada a um contrato.
3. Fazer o token fluir de verdade exige mudar `IReadingManager` + `ReadingManager` + os 3 call sites
   em `src/TranslateReader/` — **diff proibido nesta phase** — com ciclo de vida de CTS no PageModel
   (§2.4). E fase propria, junto com o lazy-switch que ja vai tocar a cadeia de leitura inteira.
4. O contraste esta no proprio repo: `ITranslationManager`/`ITranslationEngine` mostram um fluxo §3
   correto — token em TODOS os niveis, com `[EnumeratorCancellation]` em `GenerateStreamingAsync` e
   `TranslateChapterAsync`. Um contrato meio-token/meio-nao seria inconsistencia sem ganho funcional.
5. Risco residual **medido e limitado**: o `using var bookRef` do iterador libera o handle do arquivo
   no `break` do consumidor e na propagacao de excecao — provado por
   `ExtractAllImagesAsync_WhenTheConsumerBreaksEarly_ReleasesTheArchiveHandle` (`File.Delete` apos o
   `break` nao lanca). A lacuna e de **latencia de cancelamento** (no maximo a leitura de uma
   imagem), nao de seguranca de recurso. Nada vaza hoje.

Como NAO adicionei o token, nenhum contrato locked mudou: sem supersedencia de D-...-4, sem alteracao
do `Verify:` do item 2, sem teste de cancelamento novo (nao ha caminho de cancelamento a cobrir).

### W-4 — catches legados no app MAUI — **NAO FECHAVEL** (confirmado registrado)
Fora do diff da phase (`src/TranslateReader/` = 0 linhas), anterior a `4285f25`, coberto por **D-2** e
pelo estatuto finding-driven. Ja registrado em `.jdi/todos.md`, bloco `[LEGADO/D-2]`, com os 5 pontos
exatos da REVIEW: `ReaderPage.xaml.cs:326` e `:434` (`catch { }`), `LibraryPageModel.cs:183`,
`ReaderPageModel.cs:222` e `ReaderPage.xaml.cs:308` (`OperationCanceledException` engolida).
Confirmado nesta rodada — nenhuma acao; segue candidato a phase de higiene do head MAUI.

### W-5 — transcript da prova por mutacao da T-6 — **FECHADO** (rodado agora, saida real)
Mutante aplicado em `FindCoverInManifest` (`ParsingEngine.cs:331`), guarda `Length > 0` removida
(`return imageFile?.Content;`):

```
[xUnit.net] ...ExtractCoverImageAsync_WithACoverImagePropertyPointingAtAMissingFile_ReturnsNoBytes [FAIL]
  Error Message:
   Assert.Null() Failure: Value is not null
Expected: null
Actual:   []
  Stack Trace:
     at ...ParsingEngineEdgeCaseTests.cs:line 278
Failed!  - Failed: 1, Passed: 0, Skipped: 0, Total: 1, Duration: 61 ms
```

Restaurado (`git checkout` do arquivo, guarda de volta na linha 331) e re-rodado:

```
Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 63 ms
```

O aperto `Assert.Empty(cover ?? [])` -> `Assert.Null(cover)` e load-bearing: sem a guarda, vermelho.

### W-6 — `<summary>` no membro de contrato alterado — **FECHADO**
`<summary>` adicionado **so** em `ExtractAllImagesAsync` (`IParsingEngine.cs:11-15`), o unico membro
que esta phase alterou e o unico que e codigo novo (pos-`4285f25`), como csharp.md §7 pede para
`Contracts/`. Julgamento sobre a inconsistencia do arquivo: documentar os outros 5 seria refatorar
legado por estilo, proibido por D-2 — e a alternativa (nao documentar nenhum) deixaria codigo NOVO
fora da regra. 1 de 6 documentado e estado transitorio normal de repo brownfield, e o membro
documentado e exatamente o que a phase possui. Sem `GenerateDocumentationFile` no csproj nao ha
CS1591 nem warning novo (build segue com os mesmos 64 MVVMTK0045 pre-existentes).

### Gates re-rodados nesta iteracao (numeros reais)

| Gate | Resultado |
|---|---|
| `dotnet build TranslateReader.slnx -c Release` | **0 Error(s)**, 64 Warning(s) — todos MVVMTK0045 pre-existentes do app MAUI |
| `dotnet test ... -c Release` | **Failed: 0, Passed: 317, Skipped: 2, Total: 319** — baseline mantida |
| `node --test test/js/` | **pass 60, fail 0** — baseline mantida |
| `dotnet format --verify-no-changes` (escopo `IParsingEngine.cs`) | exit **0** |
| Os 7 `Verify:` do `## Definition of Done` | **7/7 exit 0** — item 1 `Passed: 10` + 28 `Assert.`; 2 contrato; 3 dois saltos; 4 quatro literais + `Passed: 1`; 5 capa; 6 `Passed: 15`; 7 `Failed: 0` / `Total: 319 >= 316` |
| Diff em `src/TranslateReader/` | **vazio** |
| `.jdi/DECISIONS.md` append-only | 0 linhas removidas |
| `.gitignore` | segue `M` na working tree, **fora de todo commit** desta rodada |

Nota de escopo: o PLAN listava `.jdi/todos.md` em "NAO tocar" (era do asker). A edicao desta rodada
foi autorizada pelo dispatch da iter 2 e e **puramente aditiva** (0 linhas removidas) — nada do que o
asker escreveu foi alterado. `dotnet format --verify-no-changes` na solucao inteira acusa desvios de
espaco em branco pre-existentes em `ThemeEngine.cs`, `ReaderPage.xaml.cs`, `ThemeEngineTests.cs` e
`TranslationManagerTests.cs` — **nenhum deles entre os 9 arquivos da phase**, legado, nao corrigido
aqui por D-2.

### Commits da iter 2
| sha | subject |
|---|---|
| `848dd6e` | docs(conversion-performance): correct the audit record of two locked decisions |
| `91ccb24` | docs(conversion-performance): name the missing cancellation token as a finding |
| `e1722a2` | docs(conversion-performance): document the streaming member of the parsing contract |
