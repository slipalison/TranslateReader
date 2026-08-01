# Phase 16: Review  (slug: conversion-performance)

**Verdict:** APPROVED_WITH_WARNINGS

REVIEW FINAL da phase — iter 2 (re-verify unica da rodada de warnings do `/jdi-issue`).
Regenerada do zero, auto-suficiente. Diff revisado: `main` (`ad607ac`) ate HEAD (`6ce4f39`),
branch `jdi/conversion-performance`, 16 commits (7 da execucao + 3 da rodada de warnings + 6 de
processo). Historico: iter 1 = APPROVED_WITH_WARNINGS com 6 warnings (W-1..W-6); esta rodada
verificou o fechamento de W-1, W-2, W-3 (decisao de nao adicionar), W-5, W-6 e a confirmacao de
W-4 como legado nao-fechavel (D-2). Toda evidencia abaixo foi levantada e executada por esta
review, nao copiada do SUMMARY.

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `net10.0-windows10.0.19041.0` Release: **0 Error(s)**, 40 warnings (todos MVVMTK0045, pre-existentes do app MAUI) |
| Tests | PASS | **Failed: 0, Passed: 317, Skipped: 2, Total: 319** (baseline da phase 304, +15; os 2 skips sao os legados de `TranslationEngineTests`, identicos a baseline). JS: `node --test test/js/` = **60 pass, 0 fail** |
| Coverage | PASS | Linhas novas/alteradas da phase = **100%** (ver detalhamento D-6 abaixo). Agregado 92,56% (contexto, nao e o gate) |
| Lint | WARN | `dotnet format --verify-no-changes` exit 2 — erros WHITESPACE **apenas** em 4 arquivos legados (`ThemeEngine.cs`, `ReaderPage.xaml.cs`, `ThemeEngineTests.cs`, `TranslationManagerTests.cs`), nenhum entre os 9 arquivos da phase; D-2 exempt, WARN-only ate `baseline-de-estilo` |
| Security/Layer | PASS (warns legados) | 5.1/5.2/5.3/5.10/5.14/5.15(Core)/5.17 limpos; zip-slip do caminho novo REFUTADO por probe (abaixo); warns exclusivamente legados, todos ja nomeados em `todos.md § [LEGADO/D-2]` |
| Consistency | PASS | 16/16 commits Conventional com scope `conversion-performance`, tipos adequados (`perf`/`test`/`fix`/`docs`/`chore`); `files_modified` do PLAN = exatamente os 9 arquivos do diff src/test; `.gitignore` em 0 commits (segue so `M` na working tree) |
| UI Validation | SKIPPED | has_frontend=false (cliente MAUI nativo) — por design, nunca bloqueia |
| DoD | PASS | **7/7 auto PASS, rodados 2x cada** (exit codes reais, duas rodadas completas), 0 manual pendente |

## Blockers

- _(nenhum)_

## Warnings

1. **[Legado, registrado]** Achados do gate 5 no app MAUI e Core legados, todos pre-existentes em
   `main` e nomeados em `.jdi/todos.md:242` (`[LEGADO/D-2]`): `catch { }` em
   `ReaderPage.xaml.cs:326`/`:434`; `OperationCanceledException` engolida em
   `LibraryPageModel.cs:183`, `ReaderPageModel.cs:222`, `ReaderPage.xaml.cs:308`; static mutavel
   `TranslationEngine.cs:16`; eventos `+=/-=` = 5/4. Nenhum tocado pela phase (diff em
   `src/TranslateReader/` = 0 linhas — confirmado por `git diff ad607ac HEAD`). W-4 da iter 1:
   confirmado nao-fechavel nesta phase.
2. **[Legado, WARN gate 4]** Drift de whitespace em 4 arquivos legados fora da phase (lista acima).
3. **[Debito de design registrado, nao warning novo]** `ExtractAllImagesAsync` sem
   `CancellationToken` (decisao D-...-10c, verificada abaixo — o argumento se sustenta);
   `IParsingEngine` com 6 operacoes (acima do ideal 3-5 do CLAUDE.md, legado); handle de zip do
   fallback de `ReadEpubSafeAsync`; N+1 da biblioteca; reparse por capitulo. Todos nomeados em
   `todos.md`/DECISIONS — ver "Para o revisor humano do PR".

## Verificacao cetica da iter 2 (evidencia propria)

### (a) A decisao de NAO adicionar `CancellationToken` (W-3) se sustenta? — SIM, verificada em 3 pontos

O argumento do doer depende de: *"nada vaza no `break` — `using var bookRef` libera o handle"*.
Esta review nao aceitou o teste verde como prova; provou que o teste e **load-bearing**:

1. **O teste existe e passa isolado**:
   `ExtractAllImagesAsync_WhenTheConsumerBreaksEarly_ReleasesTheArchiveHandle`
   (`ParsingEngineEdgeCaseTests.cs:239`) — fixture de 2 imagens, `break` apos a 1a,
   `File.Delete(path)` + `Assert.False(File.Exists(path))`. Rodado isolado:
   `Passed! - Failed: 0, Passed: 1, Total: 1, Duration: 45 ms`.
2. **O teste nao e vacuo** — probe desta review (console app em `%TEMP%`, referenciando
   `TranslateReader.Core.csproj`, descartado apos o uso): um `EpubBookRef` de
   `EpubReader.OpenBookAsync` **NAO disposto** faz `File.Delete` lancar
   `IOException: The process cannot access the file ... because it is being used by another
   process` (2 execucoes, 2/2). Ou seja: em Windows, o `File.Delete` do teste **so passa** se o
   `using` do iterador tiver disposto o `EpubBookRef` no `break` do consumidor. Handle preso =
   teste vermelho. O teste prova exatamente o que o argumento precisa.
3. **A premissa "cadeia de leitura inteira e token-free" e verdadeira** — conferido nos contratos:
   `IReadingManager` (5 ops) e `IParsingEngine` (6 ops) nao declaram token; o unico CTS de
   `ReaderPageModel` (`:63`) serve o caminho de traducao. Um token so neste membro receberia
   `default` do unico chamador de producao (`ReadingManager.ExtractImagesIfNeededAsync`).
   O risco residual e de fato **latencia de cancelamento de UMA imagem**, nao vazamento de recurso.

Conclusao: decisao argumentada e consistente; achado permanece NOMEADO em `todos.md` amarrado ao
lazy-switch futuro (D-...-5b). Sem acao exigida nesta phase.

### (b) Regressao — iter 2 tocou SO `IParsingEngine.cs` em src/test — CONFIRMADO

`git diff 923a019 HEAD --stat`: `.jdi/DECISIONS.md` (+61), `.jdi/phases/.../SUMMARY.md` (+117),
`.jdi/todos.md` (+32), `src/.../IParsingEngine.cs` (+5) — **215 insercoes, 0 delecoes**. O unico
diff de codigo e o `<summary>` de 5 linhas no membro alterado pela phase (csharp.md §7).
Nenhum teste deletado/afrouxado desde `main`: as delecoes nos 3 arquivos de teste adaptados sao
(i) troca dicionario -> `await foreach` com assercoes equivalentes-ou-mais-fortes (`>= 100` do
Wardley preservado, `DidNotReceive` preservado sem `await`), (ii) o arrange do teste de capa
compactado em `CreateOrphanCoverEpub()` e (iii) `Assert.Empty(cover ?? [])` ->
`Assert.Null(cover)` — assercao **estritamente mais forte**.

### (c) Os 7 `Verify:` reais do `## Definition of Done` — 7/7 exit 0, DUAS rodadas

As 2 mencoes em prosa dentro de D-...-8/D-...-9 nao foram tratadas como gates; rodei exatamente
os 7 itens sob `## Definition of Done` do CONTEXT.md, como escritos. Nenhum ficou mais fraco na
iter 2 (nenhuma linha `**Verify:**` mudou desde `923a019` — diff da iter 2 nao toca CONTEXT.md).

| # | Item | Rodada 1 | Rodada 2 | Evidencia adicional |
|---|---|---|---|---|
| 1 | Conversao ponta-a-ponta curto vs grande | exit 0 | exit 0 | `Passed: 10`; 28 `Assert.` no arquivo (piso 20) |
| 2 | Contrato streama `IAsyncEnumerable<ExtractedImage>` | exit 0 | exit 0 | grep positivo + negativo no contrato |
| 3 | Lazy via `OpenBookAsync`, dois saltos, eager proibido nos dois | exit 0 | exit 0 | awk confirma `OpenEpubSafeAsync` no iterador e `EpubReader.OpenBookAsync` no helper |
| 4 | Pico de memoria MEDIDO | exit 0 | exit 0 | 4 literais no teste; `Passed: 1` (155 ms / 159 ms) |
| 5 | Capa orfa retorna `null` | exit 0 | exit 0 | guarda `Length > 0` confirmada em `FindCoverInManifest` (`ParsingEngine.cs:331`) |
| 6 | Download de modelo streamado | exit 0 | exit 0 | `ResponseHeadersRead` presente, `ReadAsByteArrayAsync/ReadAsStringAsync` ausentes, `Passed: 15` |
| 7 | Suite completa sem regressao | exit 0 | exit 0 | `Failed: 0`, `Total: 319 >= 316` nas duas rodadas |

### (d) Correcoes de auditoria (W-1/W-2) — append puro, afirmacoes verdadeiras

- `git diff 923a019 HEAD -- .jdi/DECISIONS.md | grep -c '^-[^-]'` = **0** — D-...-10 e append
  puro ao fim do arquivo; D-...-4 e D-...-9 nao foram reescritas (diff so adiciona apos a linha
  1546). Mesmo para `todos.md`: 0 remocoes.
- **"6 operacoes antes e depois" — contado por esta review**: `git show
  main:src/.../IParsingEngine.cs` = 6 membros (`ExtractMetadataAsync`, `ExtractChaptersAsync`,
  `ExtractChapterContentAsync`, `ExtractAllImagesAsync`, `ExtractCoverImageAsync`,
  `CreateTranslatedEpubAsync`); HEAD = os mesmos 6, so o tipo de retorno de um mudou. A correcao
  de D-...-10b e verdadeira; a propriedade "sem crescimento" de D-...-4 tambem.
- A correcao de W-1 (termo "impossivel" -> "insatisfazivel dentro do design locked pela T-2") e
  tecnicamente precisa: C# proibe `yield return` dentro de `try` **com** `catch`; a forma inline
  antes do laco seria legal em C# puro, e o que a elimina e a acceptance da T-2 (helper +
  builders compartilhados). A conclusao de D-...-9 permanece valida — o comando novo do item 3 e
  estritamente mais forte, e passou 2x nesta review.

### (e) Medicao de memoria continua valida e nao-flaky — ver secao propria abaixo

### (f) Cobertura D-6 — 100% nas linhas novas/alteradas; `<summary>` da iter 2 nao e executavel

### (g) Padroes Sonar — nenhuma ocorrencia nova encontrada (detalhe abaixo)

### (h) Card -> evidencia — ver secao propria abaixo

## Medicao de memoria (numeros desta review, nao do doer)

`ParsingEngineMemoryTests` rodado 2x via o `Verify:` do item 4 (Release, filtro proprio):
`Passed: 1` em **155 ms** e **159 ms** — e mais 3x dentro das suites completas desta review,
sempre verde. Para reportar os **bytes reais** (o teste so imprime o delta ao falhar), esta review
replicou a medicao exata do teste (mesmo warm-up no Practice, mesma baseline, mesma amostragem a
cada 32 imagens com `GC.GetTotalMemory(forceFullCollection: true)`) num probe fora do repo, sobre
o `ParsingEngine` de producao compilado do HEAD:

| Execucao | peakRetainedDelta | count | totalBytes |
|---|---|---|---|
| 1 | **313.216 bytes (0,30 MB)** | 256 | 43.986.604 (~42 MiB) |
| 2 | **313.240 bytes (0,30 MB)** | 256 | 43.986.604 (~42 MiB) |

Variacao entre execucoes: 24 bytes — medicao estavel, nao-flaky. Coerente com os 377.792 bytes
(0,36 MB) do probe do doer (processos distintos variam ~tens de KB). Folga ate o teto de 20 MB:
**~67x**. Contra o mutante eager registrado no SUMMARY (46 MB): **reducao de ~140x** no pico
retido, streamando os mesmos 256 arquivos e ~42 MiB de bytes. A prova por mutacao da T-7
(mutante eager -> teste FALHA com 46 MB; lazy restaurado -> passa) e o transcript de W-5
(guarda da capa removida -> `Assert.Null` FALHA; restaurada -> passa) constam no SUMMARY com
saidas reais e sao consistentes com tudo que esta review mediu.

## Cobertura D-6 por arquivo tocado (Cobertura XML desta review, agregado 92,56%)

| Arquivo | Cobertura | Observacao |
|---|---|---|
| `src/TranslateReader.Core/Models/ExtractedImage.cs` (novo) | **2/2 = 100%** | record de 1 linha |
| `src/TranslateReader.Core/Business/Managers/ReadingManager.cs` | **100%** (todas as classes compiladas, incl. `<ExtractImagesIfNeededAsync>` 12/12) | loop `await foreach` coberto |
| `src/TranslateReader.Core/Business/Engines/ParsingEngine.cs` | **100% em todos os metodos** exceto `<ExtractCoverImageAsync>` 12/14 (85,71%) | a unica linha descoberta (`:76`, branch `Content.Cover`) e **legada e intocada pela phase** — o diff `ad607ac..HEAD` nao passa pelas linhas 68-79; a linha alterada pela phase nesse fluxo (`FindCoverInManifest:331`) esta coberta (provado por mutacao em W-5) |
| `src/TranslateReader.Core/Contracts/Engines/IParsingEngine.cs` | n/a | interface — o `<summary>` da iter 2 nao gera linha executavel |
| 5 arquivos de teste | n/a | teste nao entra no denominador D-6 |

Linhas novas/alteradas da phase: **100%**. Gate D-6 (90%): PASS.

## Seguranca — verificacoes ativas desta review (alem dos greps)

- **Zip-slip no caminho novo (gate 5.6): REFUTADO por probe.** `ReadingManager` escreve
  `Path.Combine(imagesDir, relativePath)` com `relativePath` vindo do EPUB (untrusted). Probe com
  EPUB hostil (manifest `href="../../../evil.png"` + entry literal `OEBPS/../../../evil.png`):
  VersOne.Epub **normaliza** o path — `RelativePath` emitido = `evil.png`, resolvido DENTRO de
  `imagesDir` (`ESCAPES_IMAGESDIR=False`), enumeracao completa sem excecao. Sem escrita fora do
  diretorio por este caminho. (O padrao de escrita e o mesmo de `main`; a phase nao o alterou.)
- **XXE (5.7)**: o unico parse XML novo e o oraculo dos testes
  (`ParsingEngineFixtureValidationTests.SpineItemCount`) — `DtdProcessing.Prohibit` +
  `XmlResolver = null`. Conforme §4.
- **WebView (5.8)**: diff em `src/TranslateReader/` = 0 linhas; nada a reavaliar.
- **Secrets/PII (5.9)**: greps limpos nos arquivos da phase.
- **Sync-over-async (5.10)**: 0 hits em `src/`. O unico `catch (OperationCanceledException)` do
  Core (`TranslationManager.cs:61`) **rethrowa** (`throw;`) apos persistir estado — correto, e
  legado (fora do diff).

## Padroes Sonar (item g — unico sinal local possivel)

Varridos os 9 arquivos da phase + o codigo novo:

- **CA1826** (Enumerable em colecao indexavel): 0 — os `Count()`/`First()` novos recebem predicado
  ou operam pos-`Distinct()`; `Directory.GetFiles(...).Single()` segue o padrao ja em baseline
  (`ParsingEngineTests.cs:11`).
- **CA1874/CA1875** (regex): 0 — nenhum `new Regex(`; producao usa `[GeneratedRegex]`.
- **CA1816**: `ParsingEngineEdgeCaseTests.Dispose` chama `GC.SuppressFinalize(this)`. OK.
- **S2699** (teste sem assercao): 0 — todos os 17 `[Fact]`/`[Theory]` novos ou adaptados assertam
  (inclusive `RightingSoftware_..._NaoLancaExcecao`, que asserta dentro e fora do delegate).
- **S1192** (literal duplicado): literais repetidos existem nos ARQUIVOS DE TESTE
  (`"OEBPS/text/chapter1.xhtml"`, `"<html><body><p>a</p></body></html>"`, 6+ ocorrencias em
  `ParsingEngineEdgeCaseTests`), mas o scanner .NET classifica `TranslateReader.Tests.csproj`
  como test project e S1192/S3776 nao rodam em test sources; em `src/` (unico lugar onde a regra
  morde), as linhas novas nao repetem literal algum. Risco local: nenhum. Confirmacao final e do
  SonarCloud remoto (deferido, como registrado).
- **S3776**: metodos novos todos curtos (iterador de 4 linhas, helpers de 8).
- **xUnit1004**: nenhum `Skip=` novo; os 2 skips da suite sao os legados de
  `TranslationEngineTests` (baseline).

## Card -> evidencia (item h)

Card (colado via `/jdi-issue`, D-...-0): *"garanta que as funcionalidades esteja funcionado
corretamente, como a conversao de livros, as imagens, download de modelos, inclusive valide que a
conversao esteja funcionando tanto para livros curtos quanto para livros grandes, se necessario
ajuste para que tenhamos performance na conversao tanto na biblioteca quanto na leitura"*.

| Pedido do card | Evidencia | Status |
|---|---|---|
| Conversao de livros funciona corretamente | `ParsingEngineFixtureValidationTests` — 10 `[Fact]`, metadata/capitulos/conteudo/imagens, contagens contra **oraculo independente** (zip entries + `<itemref>` do `.opf`), 2 rodadas verdes nesta review | **COBERTO** |
| Imagens | Mesma suite + `ParsingEngineTests` (3 fixtures) + 4 edge cases novos (stream, fallback, vazio, `break`) + fix de perf medido (lazy `EpubBookRef`) | **COBERTO** |
| Download de modelos | `ModelAccessTests` (15 passed, 2 rodadas) + greps estruturais (`ResponseHeadersRead`, sem `ReadAsByteArrayAsync`) — suite pre-existente de `coverage-90` revalidada, nao estendida (o card pedia "funcionando", nao mais cobertura) | **COBERTO** |
| Livro CURTO e GRANDE | Practice (1,7 MB / 12 imgs) e Wardley (32 MB / 256 imgs) em todas as 5 propriedades; Righting (medio) via smoke pre-existente | **COBERTO** |
| Performance na CONVERSAO | Achado ancora corrigido e MEDIDO: pico retido 44 MB (eager) -> 0,30 MB (lazy, numeros desta review), teto 20 MB assertado em teste com prova por mutacao | **COBERTO** |
| Performance na BIBLIOTECA | N+1 de `ListBookSummariesAsync` **medido/raciocinado, sem fix** — "sem gargalo confirmado em escala realista" (D-...-5a), registrado em `todos.md` | **PARCIAL (decidido, nao corrigido)** |
| Performance na LEITURA | Reparse do EPUB inteiro por troca de capitulo (`ExtractChapterContentAsync`) — achado NOMEADO (D-...-5b), fix exige cache/lifecycle de `EpubBookRef`, **escopo maior que a phase** | **PARCIAL (nomeado, deferido)** |
| (implicito) validacao na UI real | UI MAUI, device Android/iOS, SonarCloud remoto | **NAO COBERTO — `## Deferred to PR review`** |

O card dizia "se necessario ajuste": para biblioteca a resposta registrada e "medimos e nao ha
gargalo confirmado"; para leitura o gargalo e real e esta nomeado com plano (lazy-switch), nao
silenciado. Coerente com o escopo finding-driven da phase.

## DoD Checklist (gate 8)

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | Conversao ponta-a-ponta nos fixtures CURTO e GRANDE | CONTEXT (D-...-0/-2, endurecido D-...-8) | Auto | PASS (2x) | exit 0; `Passed: 10`; 28 `Assert.` |
| 2 | Contrato streama `IAsyncEnumerable<ExtractedImage>` | CONTEXT (D-...-4) | Auto | PASS (2x) | exit 0; greps positivo+negativo |
| 3 | Lazy via `OpenBookAsync`, eager proibido nos 2 saltos | CONTEXT (D-...-3, corrigido D-...-9) | Auto | PASS (2x) | exit 0; awk 4 condicoes |
| 4 | Pico de memoria retida MEDIDO | CONTEXT (D-...-1/-3, endurecido D-...-8) | Auto | PASS (2x) | exit 0; `Passed: 1`; bytes reais medidos por esta review: 0,30 MB |
| 5 | Capa orfa retorna `null`, nao `byte[0]` | CONTEXT (D-...-6a) | Auto | PASS (2x) | exit 0; `Assert.Null(cover)` a 3 linhas do nome do teste |
| 6 | Download de modelo streamado, suite verde | CONTEXT (D-...-2, endurecido D-...-8) | Auto | PASS (2x) | exit 0; `Passed: 15` |
| 7 | Suite completa sem regressao | CONTEXT (guardrail, endurecido D-...-8) | Auto | PASS (2x) | exit 0; `Failed: 0`, `Total: 319 >= 316` |

**Totals:** 7 items | Auto: 7 (7 PASS, 0 FAIL) | Manual: 0 pending

## Estado final da phase

**Mudou em PRODUCAO (Core apenas — diff no app MAUI = 0 linhas):**
- `Contracts/Engines/IParsingEngine.cs` — `ExtractAllImagesAsync` de
  `Task<IReadOnlyDictionary<string, byte[]>>` para `IAsyncEnumerable<ExtractedImage>`
  (mudanca de contrato publico), com `<summary>` (iter 2).
- `Models/ExtractedImage.cs` (novo) — `record ExtractedImage(string RelativePath, byte[] Content)`.
- `Business/Engines/ParsingEngine.cs` — iterador lazy sobre `EpubReader.OpenBookAsync`
  (`using var bookRef`, 1 imagem por vez, sem colecao acumuladora); `OpenEpubSafeAsync` com o
  MESMO par strict/fallback de `ReadEpubSafeAsync` via builders compartilhados
  (`BuildStrictOptions`/`BuildFallbackOptions`); guarda `Length > 0` em `FindCoverInManifest`
  (bugfix `byte[0]` -> `null`).
- `Business/Managers/ReadingManager.cs` — `ExtractImagesIfNeededAsync` consome via `await foreach`.

**So em teste/doc:** 2 suites novas (`ParsingEngineMemoryTests`, 1 fact medido;
`ParsingEngineFixtureValidationTests`, 10 facts com oraculo independente), 4 edge cases novos e
1 assercao apertada em `ParsingEngineEdgeCaseTests`, 6 call sites adaptados em
`ParsingEngineTests`/`ReadingManagerTests`; DECISIONS D-...-0..10 (append-only),
CONTEXT/PLAN/SUMMARY, 3 blocos novos em `todos.md`.

**Numeros finais:** 319 testes C# (317p/2s, baseline 304) + 60 JS; build 0 erros; cobertura
agregada 92,56%, linhas novas/alteradas 100%; pico retido na extracao do fixture de 32 MB:
**0,30 MB** (teto 20 MB; eager mutante: 46 MB); suite completa em ~4 s.

## Para o revisor humano do PR (1 minuto)

- **O que os gates NAO provam:** comportamento da UI MAUI (import, leitura, temas, download) —
  o app esta fora da rede de testes; memoria/bateria em device real Android/iOS — sem harness
  local; e o quality gate do **SonarCloud remoto** — analisadores nao rodam local (a varredura
  local de padroes desta review deu 0 ocorrencias novas, mas o veredito e do CI).
- **Mudanca de contrato publico:** `IParsingEngine.ExtractAllImagesAsync` agora e
  `IAsyncEnumerable<ExtractedImage>` (era `Task<IReadOnlyDictionary<string, byte[]>>`). Breaking
  para qualquer implementador/consumidor externo; dentro do repo o unico consumidor de producao
  (`ReadingManager`) foi adaptado e o app MAUI nao chama o metodo diretamente (so registra o DI).
- **Achados registrados SEM correcao (decididos, nao esquecidos):** handle de zip aberto no
  fallback de `ReadEpubSafeAsync` (pre-existente, so no caminho de fallback); N+1 de
  `LibraryManager.ListBookSummariesAsync` (medido, sem gargalo confirmado em escala realista);
  reparse do EPUB inteiro a cada troca de capitulo na leitura (mesma causa raiz do achado ancora,
  fix exige cache com lifecycle — phase propria); `CancellationToken` ausente na cadeia de
  leitura (entrara quando a cadeia for tocada ponta a ponta); `IParsingEngine` com 6 operacoes
  (legado). Tudo em `.jdi/todos.md` com contexto.

## Recommendation

Aprovar e seguir para `/jdi-ship`. Os warnings sao exclusivamente legados (D-2) ou debitos ja
nomeados com plano; nada introduzido pela phase. A phase entregou o que o card pedia dentro do
escopo verificavel localmente, com a prova de memoria mais forte do repo ate aqui (mutacao +
medicao independente reproduzida por esta review com variacao de 24 bytes entre execucoes).
Pontos de atencao do PR: a mudanca de contrato publico e os 3 achados de performance deferidos.

**Verdict:** APPROVED_WITH_WARNINGS

## DoD Critic (enhanced — forcado por /jdi-issue, passe final antes do ship)

NOTA DE EXECUCAO: rodado inline pelo orquestrador, exit code REAL, gates extraidos por parser
restrito a secao `## Definition of Done` (o `CONTEXT.md` tem 9 ocorrencias da string `**Verify:**`,
mas 2 sao mencoes em prosa dentro das decisoes D-...-8/-9; contar as 9 como gates foi um erro de
extracao do proprio orquestrador na passagem anterior, corrigido aqui).

`git diff 923a019 HEAD -- .../CONTEXT.md` e **vazio**: nenhuma linha do DoD mudou desde a aprovacao
do critico na iter 1 — a rodada de warnings nao tocou gate nenhum (fechou W-1/W-2 por correcao de
registro append-only, W-3 por decisao argumentada registrada em `todos.md`, W-5 por transcript e W-6
por `<summary>` no membro alterado). Logo o julgamento anterior permanece valido e foi reconfirmado:
os **7 gates reais saem exit 0**.

O que sustentava o unico ponto de risco continua sustentando: o gate do item 3, reescrito pelo doer
durante a corrida (D-...-9), ficou ESTRITAMENTE mais forte — o caminho eager escondido atras da
mesma fachada `IAsyncEnumerable` e rejeitado no primeiro salto (contra-exemplo executado), e a
implementacao correta, que o comando anterior reprovava, passa. A complementaridade
estrutura+medicao segue sendo o que fecha o buraco: o grep nao pegaria uma implementacao que usa
`OpenBookAsync` e ainda assim acumula tudo numa lista; o teste de memoria pega (0,30-0,38 MB medidos
contra 46 MB do mutante eager).

Nenhuma linha `Type=Auto`/`PASS` mostrou-se oca.

**Verdict:** APPROVED
