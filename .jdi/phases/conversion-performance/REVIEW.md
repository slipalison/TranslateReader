# Phase 16: Review  (slug: conversion-performance)

**Verdict:** APPROVED_WITH_WARNINGS

Reviewer: `jdi-reviewer-translatereader` | mode=verify | iter=1
Diff revisado: `main` (`ad607ac`) -> HEAD (`8e2c088`), branch `jdi/conversion-performance`, 11 commits (7 de execucao + 4 docs).
Toda evidencia abaixo foi produzida NESTA revisao (builds, testes, cobertura, 3 mutacoes e 3 medicoes de memoria rodadas pela reviewer) — nada foi aceito por auto-relato do SUMMARY.

## Gates

| Gate | Comando | Resultado real | Status |
|---|---|---|---|
| 1. Build | `dotnet restore && dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` | exit 0, **0 Erro(s)** (8 avisos pre-existentes: CS0414/MVVMTK0045, todos no app MAUI intocado) | PASS |
| 2. Tests | `dotnet test test/.../TranslateReader.Tests.csproj -c Release` | **Failed: 0, Passed: 317, Skipped: 2, Total: 319** (baseline 304; piso ratchet 316 batido). JS: `node --test test/js/` = **60/60 pass** (baseline 60) | PASS |
| 3. Coverage | `--collect:"XPlat Code Coverage"` + parse do Cobertura | Agregado 92,56% (contexto). **Linhas novas/alteradas: 100%** — `ExtractedImage.cs` 2/2, `ReadingManager.cs` 100% em todas as classes, `ParsingEngine.cs` 100% em tudo que a phase tocou (iterador 8/8, `OpenEpubSafeAsync` 6/6, builders, guarda de capa). Unica linha descoberta: `ParsingEngine.cs:76` (`return coverContent;`) — **legada, nao tocada** (D-2 exime) | PASS |
| 4. Lint | `dotnet format --verify-no-changes` | exit 0 — limpo | PASS |
| 5. Security/Layer | bateria 5.1-5.17 (abaixo) | 0 achados novos; achados legados relatados como WARN (W-4) | PASS c/ WARN |
| 6. Consistency | commits vs PLAN `files_modified` | 9 arquivos src/test dos commits = exatamente os do PLAN; Conventional Commits com scope `conversion-performance` e types adequados (docs/perf/test/fix, nao tudo `feat`); 1 lacuna documental (W-5) | PASS c/ WARN |
| 7. UI Validation | — | SKIPPED (has_frontend=false, MAUI nativo — por design do reviewer) | SKIPPED |
| 8. DoD | 7 itens Auto do CONTEXT.md rodados como escritos | **7/7 exit 0** (tabela abaixo); PROJECT.md nao tem secao DoD propria; 0 itens Manual | PASS |

Detalhe do gate 5 (todos rodados nesta revisao):
- 5.1 Client->Access/Engine: limpo. 5.2 storage em contratos: limpo. 5.3 Manager->Manager: so auto-interface (ok).
- 5.6 zip: unico arquivo com `ZipArchive/ZipFile` segue `ParsingEngine.cs` (baseline); nenhuma escrita de entry em disco nova. 5.7 XXE: o unico `XmlReader` novo esta em TESTE (`ParsingEngineFixtureValidationTests.cs:180-186`) e usa `DtdProcessing.Prohibit` + `XmlResolver = null` — correto. 5.8 WebView: nenhum call site novo (diff em `src/TranslateReader/` = 0 linhas). 5.9 segredos/PII: limpo.
- 5.10 sync-over-async: limpo. 5.11 eventos: subscribe=5/unsubscribe=4 = baseline exata do bootstrap (nenhum `+=` novo). 5.12 static mutavel: 1 hit = o baseline legado (`TranslationEngine.cs:16`). 5.14 hot-path: sem `new Regex`, sem `ReadAll*`, sem `Substring/ToLower==` em Engines/Utilities. 5.15 Result-pattern: limpo. 5.17 mocks: so interfaces; I/O real em teste coberto pela excecao NOMEADA no cabecalho dos proprios arquivos (precedente D-2026-07-31-coverage-90-3).

## Veredito ponto a ponto (a)-(j) do dispatch

**a) O fix e real? — CONFIRMADO, com prova por mutacao propria.**
Codigo lido: `ParsingEngine.cs:61-66` — o iterador usa `using var bookRef = await OpenEpubSafeAsync(...)` (descarte garantido, inclusive em `break` do consumidor), le UMA imagem por vez via `await image.ReadContentAsBytesAsync()` e nao acumula colecao; `OpenEpubSafeAsync` (`:152-162`) chama `EpubReader.OpenBookAsync` nos dois ramos. **Mutacao (rodada por mim):** recoloquei o caminho eager (`ReadEpubSafeAsync` + `Dictionary`) atras da MESMA fachada `IAsyncEnumerable` — exatamente o "fix falso" que o CONTEXT (D-...-3) preve — e `ParsingEngineMemoryTests` **FALHOU com 45 MB de pico retido (teto 20 MB)**. Restaurado o lazy, voltou a passar. Imagens nao ficam retidas entre iteracoes: pico medido de 0,36 MB streamando 42 MiB (secao de medicao abaixo).

**b) O teste de memoria e flaky? — NAO.**
3 medicoes consecutivas (probe com impressao de bytes): **377.800 / 377.960 / 378.160 bytes** — variancia de ~360 bytes (0,0003 MB) entre corridas. Distancia ao teto de 20 MB: **~55x**. Nao ha nenhuma assercao de TEMPO no arquivo (sem `Stopwatch`, sem duracao). `[CollectionDefinition("NonParallel", DisableParallelization = true)]` + `[Collection("NonParallel")]` presentes (`ParsingEngineMemoryTests.cs:5,14`) — e o teste passou dentro da suite completa paralela em 2 rodadas independentes desta revisao. Gate de memoria estavel, nao vira ruido.

**c) "Mesmas opcoes de tolerancia" — CONFIRMADO literalmente.**
O diff de `ReadEpubSafeAsync` mostra que `BuildStrictOptions()`/`BuildFallbackOptions()` (`ParsingEngine.cs:164-205`) contem exatamente os mesmos 7+10 flags que estavam inline antes da phase (strict: 3+3+1; fallback: +`IgnoreMissingMetadataNode`, +`IgnoreRemoteContentFileError`, +`IgnoreFileIsTooLargeError`) — zero divergencia — e que os DOIS metodos (`ReadEpubSafeAsync:142/146`, `OpenEpubSafeAsync:156/160`) consomem os MESMOS builders. "Mesmas opcoes" provado por estrutura, como D-...-3 exigia.

**d) `catch (EpubPackageException)` novo coberto? — CONFIRMADO por cobertura E mutacao.**
Teste sintetico existe (`ExtractAllImagesAsync_WithAPackageMissingItsMetadataNode_UsesTheLenientFallback`, EdgeCaseTests); Cobertura: `OpenEpubSafeAsync` **6/6 linhas (100%)**. **Mutacao (rodada por mim):** troquei o corpo do catch por `throw;` -> o teste ficou **vermelho** (`EpubPackageException: metadata not found in the package`). Restaurado, verde.

**e) Desvio #1 (D-...-9, Verify do item 3) — LEGITIMO; gate ficou MAIS forte, nao mais fraco.**
(i) Append puro: `git diff ad607ac..HEAD -- .jdi/DECISIONS.md | grep -c '^-[^-]'` = **0**; o commit `4d01006` so trocou a linha `**Verify:**`/`**Source:**` do item 3 no CONTEXT + adicionou o bullet — e o bullet corresponde ao que D-...-9 autoriza. (ii) Ataque com contra-exemplo (rodado por mim): contra o mutante eager-atras-da-fachada, o comando NOVO sai **exit 1** (o salto 1 proibe `ReadEpubSafeAsync` no corpo do iterador); contra uma implementacao sem fallback (sem `OpenEpubSafeAsync`), o salto 1 tambem falha. O comando ANTIGO, conferido no diff do `4d01006`, de fato reprovaria a implementacao correta (o corpo do iterador nao contem o literal `OpenBookAsync`) e aprovaria uma sem fallback — e **nunca proibia** `ReadBookAsync` em lugar algum, coisa que o novo proibe nos dois saltos. **Nuance de honestidade** (nao muda o veredito): a palavra "impossivel" em D-...-9 e um overstatement tecnico — C# permitiria `try/catch` inline ANTES do laco de `yield` (`yield` so e proibido dentro do proprio try/catch) — mas essa forma violaria a acceptance LOCKED da T-2 do PLAN (helper `OpenEpubSafeAsync` a parte + builders compartilhados, que e o que prova o item (c)); dentro do design locked, o Verify antigo era de fato insatisfazivel. Nenhuma auto-indulgencia: a prova principal continua sendo o item 4 (medida), que o mutante reprova de qualquer forma.

**f) Desvio #2 (`DOTNET_CLI_UI_LANGUAGE=en`) — NAO mascara nada.**
O prefixo apenas fixa o idioma do sumario do runner; os parsers do Verify (`grep "Passed!"`, `sed 's/.*Passed:...'`, `Failed:`, `Total:`) DEPENDEM de saida em ingles, entao sem o prefixo o gate quebraria nesta maquina pt-BR por falso negativo — e num CI en-US o prefixo e no-op. E endurecimento de portabilidade, registrado em D-...-8 com o motivo. Confirmei rodando os 7 comandos como escritos: todos exit 0 com contadores reais (`Passed: 10`, `Passed: 1`, `Passed: 15`, `Total: 319`).

**g) Contrato e camadas — OK, com 1 correcao de numero (W-2).**
`IParsingEngine` tem **6 operacoes antes e 6 depois** — zero crescimento — mas D-...-4 afirma "permanece 5": e um erro de contagem no texto da decisao (o proprio D-...-3 fala em "os OUTROS 5 metodos", ou seja 6 no total). 6 excede o "ideal 3-5" do CLAUDE.md, porem e forma legada nao alterada pela phase — WARN documental, nao estrutural. `ExtractedImage` e `record` em `Models/` (`src/TranslateReader.Core/Models/ExtractedImage.cs`) ✓. `ReadingManager` segue chamando apenas `IParsingEngine`/`IFileUtility`/Access via interface — `await foreach` e a unica mudanca (`ReadingManager.cs:56`), sem tocar Resource direto, sem regra de negocio nova ✓. Bateria 5.1-5.3 limpa; nenhuma violacao de The Method introduzida.

**h) Regressao de testes — NENHUMA.**
`git diff ad607ac..HEAD -- test/`: **zero teste deletado**; contagem 304 -> 319 (+15). Os 6 call sites adaptados mantiveram ou aumentaram a forca: `Practice_...RetornaImagensDoEpub` mantem as mesmas assercoes por item + contagem; `Wardley_...Retorna256Imagens` preserva o piso `>= 100`; `RightingSoftware_...NaoLancaExcecao` agora ENUMERA o stream inteiro dentro do `Record.ExceptionAsync` (mais forte que so await do Task); `DidNotReceive()` sem `await` tem a mesma semantica de verificacao do NSubstitute; as 3 assercoes de `WriteFileAsync` do `ReadingManagerTests` estao intactas. `CreateOrphanCoverEpub()` passa argumentos IDENTICOS ao bloco inline antigo (mesmo nome, mesmo coverItem, `imageHref/imageBytes` null) — a compactacao foi so para caber na janela do `grep -A3` do Verify; a assercao APERTOU (`Assert.Empty(cover ?? [])` -> `Assert.Null(cover)`). **Prova extra por mutacao (minha):** revertendo a guarda de `FindCoverInManifest` para `return imageFile?.Content;`, o teste falha com `Assert.Null() Failure` — o aperto e load-bearing.

**i) Cobertura D-6 e padroes Sonar — PASS.**
Cobertura medida por mim (Cobertura XML): 100% nas linhas novas/alteradas (numeros no gate 3). Padroes que o Sonar ja mordeu neste repo, varridos nos arquivos novos/alterados: S2699 (10 facts/28 asserts e 1 fact/3 asserts — nenhum teste sem assercao), xUnit1004 (nenhum `Skip=` novo), CA1816 (`Dispose` com `GC.SuppressFinalize` presente), CA1826/CA1861 (LINQ so com predicado; `FrozenSet` em `static readonly`), `[GeneratedRegex]` mantido, sem `new Regex`. S1192: os literais repetidos em `ParsingEngineEdgeCaseTests` ("OEBPS/text/chapter1.xhtml" 5->7 ocorrencias) seguem o padrao de fixture ja existente no arquivo desde a baseline; os 2 arquivos NOVOS estao limpos. Confirmacao remota do SonarCloud permanece em `## Deferred to PR review` (mesma fronteira das phases 14/15).

**j) Livro curto E grande — CONFIRMADO e load-bearing.**
`ParsingEngineFixtureValidationTests` = 2 fixtures (Practice 1,7 MB / Wardley 32 MB) x 5 propriedades, 10 facts, 28 `Assert.`. As assercoes NAO sao "nao lancou": contagem de capitulos == `<itemref>` do `.opf` lido por oraculo independente (`XmlReader` + `DtdProcessing.Prohibit`); contagem de imagens == entries de imagem do zip com **igualdade exata** + piso `>= 200` no oraculo do Wardley; conteudo de TODO capitulo nao-vazio e sem `src="../"`; bytes `> 0` com `RelativePath` unico. Rodado por mim: `Passed: 10`.

## Blockers

- _(nenhum)_

## Warnings

- **W-1** `.jdi/DECISIONS.md:1535` (D-...-9) — justificativa "o que a propria decisao de design torna impossivel / C# proibe yield return em try/catch" e tecnicamente overstated: um `try/catch` inline antes do laco de `yield` satisfaria o comando antigo. A correcao continua legitima (era insatisfazivel DENTRO do design locked pela T-2 do PLAN, e o comando novo e estritamente mais forte — contra-exemplos rodados nesta revisao). Regra: precisao de decisao/auditoria (JDI append-only). Sem acao de codigo; opcional registrar a nuance em `todos.md`.
- **W-2** `.jdi/DECISIONS.md:1455-1456` (D-...-4) vs `src/TranslateReader.Core/Contracts/Engines/IParsingEngine.cs:7-16` — a decisao diz "contagem permanece 5", mas o contrato tem **6 operacoes** (antes e depois; sem crescimento na phase). CLAUDE.md pede 3-5 ideal — forma legada, candidata a `todos.md` quando o lazy-switch dos outros metodos (D-...-5b) for atacado. Regra: CLAUDE.md § Contratos.
- **W-3** `src/TranslateReader.Core/Contracts/Engines/IParsingEngine.cs:10` — novo caminho async sem `CancellationToken`/`[EnumeratorCancellation]` (um livro de 200+ imagens agora e enumerado incrementalmente e um consumidor cancelado so para no `break`). Assinatura foi LOCKED assim por D-...-4 e o contrato inteiro e token-free (padrao legado), entao nao bloqueia — mas `.claude/rules/csharp.md` §3 pede token em caminho novo; candidato nomeado para a phase futura do lazy-switch. Regra: csharp.md §3.
- **W-4** (legado, fora do diff da phase; report-only por adopted=true) — `src/TranslateReader/Pages/ReaderPage.xaml.cs:326,434` `catch { }` vazios; `LibraryPageModel.cs:183`, `ReaderPageModel.cs:222`, `ReaderPage.xaml.cs:308` engolem `OperationCanceledException`. Regra: csharp.md §1. Baseline pre-existente (diff da phase em `src/TranslateReader/` = 0 linhas).
- **W-5** `PLAN.md` T-6 acceptance pedia "transcript das duas rodadas no SUMMARY" e o SUMMARY so afirma "comecando pelo teste vermelho" sem transcript. Lacuna DOCUMENTAL apenas — eu reproduzi a prova: mutante `return imageFile?.Content;` deixa o teste vermelho (`Assert.Null() Failure`), restaurado fica verde. Regra: gate 6 (consistencia PLAN/SUMMARY).
- **W-6** `src/TranslateReader.Core/Contracts/Engines/IParsingEngine.cs:10` — membro de contrato alterado segue sem `<summary>` (o arquivo inteiro nunca teve — legado). Regra: csharp.md §7. WARN estilo, nao bloqueia por D-2.

## Medicao de memoria (numeros DESTA revisao, nao do SUMMARY)

Maquina local, `-c Release`, fixture Wardley (32 MB zip / 256 imagens / ~42 MiB de bytes de imagem).

| Corrida | Caminho | Pico retido (delta) | count | totalBytes | Resultado |
|---|---|---|---|---|---|
| Probe 1 | lazy (committed) | **377.800 bytes (0,36 MB)** | 256 | 43.986.604 | (probe: teto 1 byte so p/ imprimir) |
| Probe 2 | lazy (committed) | **377.960 bytes (0,36 MB)** | 256 | 43.986.604 | idem |
| Probe 3 | lazy (committed) | **378.160 bytes (0,36 MB)** | 256 | 43.986.604 | idem |
| Mutante eager | `ReadEpubSafeAsync`+Dictionary atras da mesma fachada | **45 MB** | 256 | idem | **Failed** (teto 20 MB) |
| Committed, como escrito | lazy | < 20 MB | 256 | idem | **Passed** em 4 rodadas independentes (suite c/ coverage, combo pos-restauracao, DoD item 4, DoD item 7) |

Variancia entre as 3 probes: ~360 bytes. Confirma o claim central da phase: **~45 MB (eager) -> ~0,37 MB (lazy)**, reducao de ~120x, folga de ~55x ate o teto — numeros compativeis com os 46 MB/0,36 MB reportados pelo doer.

## DoD Checklist (gate 8)

| # | Criterio | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | Conversao ponta-a-ponta curto vs grande | CONTEXT (D-...-0/-2, endurecido D-...-8) | Auto | PASS | exit 0; `Passed: 10`; 28 `Assert.` no arquivo |
| 2 | Contrato streama `IAsyncEnumerable<ExtractedImage>` | CONTEXT (D-...-4) | Auto | PASS | exit 0 |
| 3 | `ExtractAllImagesAsync` lazy via `OpenBookAsync` em 2 saltos, eager proibido nos dois | CONTEXT (D-...-3, corrigido D-...-9) | Auto | PASS | exit 0; contra-mutante: exit 1 |
| 4 | Pico de memoria MEDIDO limitado | CONTEXT (D-...-1/-3, endurecido D-...-8) | Auto | PASS | exit 0; grep de literais = 4; `Passed: 1`; mutante eager reprova com 45 MB |
| 5 | Capa orfa retorna `null`, nao `byte[0]` | CONTEXT (D-...-6a) | Auto | PASS | exit 0; mutante da guarda deixa o teste vermelho |
| 6 | Download de modelo streamado segue validado | CONTEXT (D-...-2, endurecido D-...-8) | Auto | PASS | exit 0; `Passed: 15` |
| 7 | Suite completa sem regressao (piso 316) | CONTEXT (guardrail, endurecido D-...-8) | Auto | PASS | exit 0; `Failed: 0`, `Total: 319` |

**Totals:** 7 itens | Auto: 7 (7 PASS, 0 FAIL) | Manual: 0 pendentes
(PROJECT.md nao declara secao `## Definition of Done` propria; DoD da phase esta integralmente no CONTEXT.md — nada de INCONCLUSIVE.)

## Recommendation

Aprovar e seguir para `/jdi-ship conversion-performance`. O achado ancora foi corrigido de verdade (provado por mutacao e por medida propria, nao por grep), o gate de memoria e estavel (variancia de bytes), nenhum teste regrediu e a cobertura das linhas novas e 100%. Os warnings sao documentais/legados: W-1/W-2 merecem uma linha em `todos.md` na proxima passada de docs; W-3 (CancellationToken) deve entrar NOMEADO na phase futura do lazy-switch (D-...-5b) para nao se perder; W-4 e baseline legado ja conhecido. Nada exige nova rodada do doer.

**Verdict:** APPROVED_WITH_WARNINGS

## DoD Critic (enhanced — forcado por /jdi-issue)

NOTA DE EXECUCAO: rodado inline pelo orquestrador, com captura do exit code REAL e comandos
extraidos por parser do `CONTEXT.md` vigente. Registro de honestidade: a primeira extracao do
orquestrador capturou como se fossem gates duas MENCOES EM PROSA a `**Verify:**` dentro das decisoes
D-...-8 e D-...-9 (as decisoes citam o termo entre crases), produzindo dois falsos "exit 127/2". Com
o filtro corrigido, os **7 gates reais saem exit 0**, batendo com doer e reviewer.

**Ponto de maior risco desta phase — o gate do item 3 foi reescrito PELO PROPRIO doer no meio da
corrida (D-...-9), o que e a situacao classica de acomodar o gate a implementacao. Atacado
diretamente:**

O comando novo e uma checagem de DOIS saltos: (1) o corpo do iterador precisa referenciar
`OpenEpubSafeAsync` e NAO pode referenciar `ReadEpubSafeAsync`; (2) `OpenEpubSafeAsync` precisa usar
`EpubReader.OpenBookAsync` e NAO pode usar `EpubReader.ReadBookAsync`. Contra-exemplo executado em
copia (`OpenEpubSafeAsync` -> `ReadEpubSafeAsync`, isto e, o caminho eager escondido atras da mesma
fachada `IAsyncEnumerable`): **exit 1, pego no primeiro salto**. O comando ANTIGO exigia
`OpenBookAsync` literalmente dentro do corpo do iterador — impossivel de satisfazer com o fallback
strict+fallback exigido pela propria decisao locked, e ao mesmo tempo cego para `ReadBookAsync`.
Ou seja: o gate ficou ESTRITAMENTE mais forte, nao mais permissivo. Nao houve auto-indulgencia.

Limite honesto do gate estrutural: ele nao pegaria uma implementacao que usa `OpenBookAsync` e
mesmo assim acumula todas as imagens numa lista antes de emitir. Quem cobre esse caso e o item de
MEDICAO (pico retido de 0,36 MB contra teto de 20 MB, e 46 MB quando a reviewer plantou o mutante
eager) — os dois itens sao complementares por construcao, e e por isso que a phase precisa dos dois.

Nenhuma linha `Type=Auto`/`PASS` mostrou-se oca nesta passagem.

**Verdict:** APPROVED
