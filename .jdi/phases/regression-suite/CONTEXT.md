# Phase 12: Rede de testes de regressao — Context (slug: regression-suite)

Gerado em modo `auto` via `/jdi-issue` (dispatch `mode=auto dod=auto_only`, sem interacao —
todas as decisoes abaixo foram tomadas e justificadas pelo asker, nao pelo usuario ao vivo).

## Goal
Fixar o comportamento observavel de hoje em testes de caracterizacao, para que qualquer
alteracao futura (em especial o refactor da phase `the-method-refactor`) quebre um teste em vez
de quebrar o app — cobrindo os caminhos do Core hoje sem teste, e decidindo explicitamente o
que fazer com as 1516 linhas do projeto MAUI que o test project atual nao alcanca.

## Locked decisions
(texto completo de cada uma em `.jdi/DECISIONS.md`)
- D-2026-07-30-regression-suite-1: origem da fase (card colado), achado estrutural — o test
  project (`net10.0`) so referencia `TranslateReader.Core`; o app MAUI (1516 linhas) nao tem
  caminho de teste. Baseline a preservar: **167** testes (D-2; confirmado por grep nesta sessao:
  167 `[Fact]`/`[Theory]` em 16 arquivos — nao 169/17 como o card presumia, numero batendo com
  `PROJECT.md`/D-2, entao 167 e o numero oficial).
- **D-2026-07-30-regression-suite-2 (decisao central da fase):** opcao **(c)** escolhida — a rede
  desta fase fica limitada a `src/TranslateReader.Core`; a lacuna do app MAUI e aceita e
  catalogada, nao fechada. Opcao (a) (2o test project multi-TFM) exigiria infraestrutura nova
  (csproj MAUI, seam pra `[QueryProperty]`/Shell) que e trabalho formatado como producao — risco
  de virar o refactor que esta fase esta proibida de fazer — e so rodaria no job `build`
  (windows-latest) do CI, que nunca foi escopado pra `dotnet test` (D-2026-07-28-ci-seguranca-5).
  Opcao (b) (extrair logica dos PageModels) e refactor comportamental — ja alocado em
  `the-method-refactor`, que DEPENDE desta fase rodar primeiro; fazer aqui inverteria a
  dependencia. Cruzando com os alvos que `the-method-refactor` ja declara (ParsingEngine,
  HtmlUtility, TranslationEngine, loops, LOH) — todos vivem em Core, alcancaveis hoje. Inventario
  do que fica desprotegido: `ReaderPage.xaml.cs` (488), `ReaderPageModel.cs` (303),
  `LibraryPageModel.cs` (236), `SettingsOverlay.xaml.cs` (221), `TranslateBookPopup.xaml.cs`,
  `MauiProgram.cs`, `AppShell.xaml.cs`, `Utilities/*Converter.cs` — nenhum teste automatizado
  pega regressao ali; so revisao manual, ate uma fase futura fechar isso deliberadamente.
- D-2026-07-30-regression-suite-3: maior lacuna objetiva do Core = `BookTranslationJobAccess.cs`
  (107 linhas, 4 metodos publicos, persiste pause/resume da traducao de livro) tem **zero** teste
  hoje. As 5 classes `*Access` irmas ja seguem um padrao reusavel (`InMemoryDatabase.cs` +
  `(connectionString, initializeOnStartup: true)`) — esta fase cria
  `BookTranslationJobAccessTests.cs` seguindo esse padrao, sem infra nova.
- D-2026-07-30-regression-suite-4: dois gaps mais estreitos no Core — (1)
  `BooksAccess.FetchAllBooksAsync` ordena por `LastOpenedAt DESC, DateAdded DESC` mas so tem
  asserção de contagem, nunca de ordem; (2) `ReadingManager.LoadProgressAsync` so tem o caso
  nulo testado, falta o caso "progresso encontrado" (mock puro, sem I/O).
- D-2026-07-30-regression-suite-5: dois gaps ficam DELIBERADAMENTE fora, motivo tecnico nomeado
  (nao silenciados — registrados em `.jdi/todos.md` para `the-method-refactor`): (1) o branch
  "imagens ja extraidas" de `ReadingManager` toca `Directory.Exists` real (nao via
  `IFileUtility`) — testa-lo exigiria I/O de disco num teste NOVO, proibido por
  `.claude/rules/csharp.md` §6; fechar exige seam de producao (refactor, fora de escopo); (2)
  `TranslationEngine` acopla a tipos concretos do LLamaSharp (`LLamaWeights`, `StatelessExecutor`)
  sem seam de interface — os 2 testes de integracao `[Fact(Skip=...)]` ja existentes continuam
  sendo o unico jeito de exercitar carregamento real de modelo.
- D-2026-07-30-regression-suite-6 (guardrail): o test project permanece single-TFM `net10.0` —
  nenhum segundo test project ou multi-target nesta fase, checado no DoD.

## Canonical refs
- Card colado via `/jdi-issue` — texto integral em D-2026-07-30-regression-suite-1.
- `CLAUDE.md` § "Arquitetura: The Method" (regras de camada) e `.claude/rules/csharp.md` §6
  (regra de teste: 90% em codigo novo, sem I/O real em teste unitario, sucesso+falha+cancelamento).
- `.jdi/agents/jdi-reviewer-translatereader.md` — Gate 2 (baseline 167), Gate 3 (cobertura
  90% so em arquivos novos), Gate 8 (DoD).
- Codigo lido nesta sessao: `ReadingManager.cs`, `BooksAccess.cs`, `BookTranslationJobAccess.cs`,
  `TranslationEngine.cs`, `TranslationManager.cs`, `HtmlUtility.cs`, `SettingsManager.cs`.
- Testes lidos: `ReadingManagerTests.cs`, `BooksAccessTests.cs`, `TranslationCacheAccessTests.cs`
  (padrao de referencia pra `BookTranslationJobAccessTests.cs`), `TranslationEngineTests.cs`,
  `HtmlInjectionTests.cs`, `ParsingEngineTests.cs`, `InMemoryDatabase.cs`.

## Out of scope
- Qualquer refactor ou mudanca de comportamento em codigo de producao — esta fase so ADICIONA
  testes. Pertence a `the-method-refactor` (D-2026-07-30-the-method-refactor-1, depende desta
  fase estar em `main` primeiro).
- Otimizacao de memoria/CPU/bateria pedida no card — segunda metade do card, pertence a
  `the-method-refactor`, nao entra no DoD desta fase.
- Segundo test project MAUI-TFM ou qualquer teste de `src/TranslateReader/Pages`/`PageModels` —
  decisao (c) trava isso fora (D-2026-07-30-regression-suite-2).
- Pin do reviewer em Fable 5/xhigh (D-7) — ja implementado, nao e escopo desta fase.

## Definition of Done

### Auto-verifiable
- [ ] `BookTranslationJobAccess` (0 testes hoje, 107 linhas, estado de pause/resume) ganha
      arquivo de teste dedicado cobrindo os 4 metodos publicos, seguindo o padrao
      `InMemoryDatabase` das classes `*Access` irmas (sem I/O real de disco/rede)
      **Verify:** `test -f test/TranslateReader.Tests/BookTranslationJobAccessTests.cs && grep -q "InMemoryDatabase" test/TranslateReader.Tests/BookTranslationJobAccessTests.cs && for m in FetchActiveJobAsync SaveJobAsync UpdateJobProgressAsync DeleteJobAsync; do grep -q "$m" test/TranslateReader.Tests/BookTranslationJobAccessTests.cs || exit 1; done`
      **Source:** CONTEXT
- [ ] `BookTranslationJobAccessTests.cs` tem no minimo 6 casos de teste (mesma ordem de grandeza
      da irma `TranslationCacheAccessTests.cs`, que precisou de 6 pra um CRUD de 4 metodos com
      round-trip, not-found e isolamento entre registros)
      **Verify:** `test $(grep -cE "\[Fact\]|\[Theory\]" test/TranslateReader.Tests/BookTranslationJobAccessTests.cs) -ge 6`
      **Source:** CONTEXT
- [ ] `BooksAccessTests.cs` ganha um teste caracterizando o contrato de ordenacao de
      `FetchAllBooksAsync` (`ORDER BY LastOpenedAt DESC, DateAdded DESC`), nao so contagem
      **Verify:** `grep -qi "order" test/TranslateReader.Tests/BooksAccessTests.cs && test $(grep -cE "\[Fact\]|\[Theory\]" test/TranslateReader.Tests/BooksAccessTests.cs) -ge 7`
      **Source:** CONTEXT
- [ ] `ReadingManagerTests.cs` ganha um teste para o caso "progresso encontrado" de
      `LoadProgressAsync` (hoje so o caso nulo esta coberto)
      **Verify:** `test $(grep -cE "\[Fact\]|\[Theory\]" test/TranslateReader.Tests/ReadingManagerTests.cs) -ge 6`
      **Source:** CONTEXT
- [ ] Guardrail D-2026-07-30-regression-suite-6: decisao (c) honrada estruturalmente — nenhum
      segundo test project ou multi-target MAUI introduzido nesta fase
      **Verify:** `test $(grep -c "net10.0-windows" test/TranslateReader.Tests/TranslateReader.Tests.csproj) -eq 0 && test $(find test -name "*.csproj" | wc -l) -eq 1`
      **Source:** CONTEXT
- [ ] Contagem total de `[Fact]`/`[Theory]` sobe do baseline 167 (D-2) para no minimo 175 —
      soma dos minimos comprometidos acima (+6 BookTranslationJobAccess, +1 BooksAccess,
      +1 ReadingManager = +8)
      **Verify:** `test $(grep -rhoE "\[Fact\]|\[Theory\]" test/TranslateReader.Tests --include=*.cs | wc -l) -ge 175`
      **Source:** CONTEXT

### Manual
- _(none — dod=auto_only; itens inerentemente humanos foram para `## Deferred to PR review`,
  nao viraram linha Manual)_

## Deferred to PR review
- Leitura humana de que os novos cenarios de `BookTranslationJobAccessTests.cs` caracterizam de
  fato o significado de negocio das transicoes de `Status` (`Pending`/`InProgress`/`Paused`), nao
  so o mecanismo CRUD — grep nao mede isso.
- Confirmar que o naming/estrutura dos novos testes segue a convencao legivel dos arquivos
  `*Tests.cs` irmaos (julgamento subjetivo de estilo).
- Validar que a triagem dos 2 achados deliberadamente deixados de fora (D-2026-07-30-
  regression-suite-5) esta correta — ou seja, que nenhum deles e na verdade fechavel sem
  refactor e deveria ter entrado no DoD desta fase.

## Notes
- Fase so-de-teste: sem mudanca de codigo de producao -> Gate 5 (layer/security) do reviewer
  nao deve achar diff em `src/`; qualquer diff em `src/` nesta fase e desvio de escopo.
- `TranslationEngine` e `ReadingManager` ficam com gaps deliberados — nao sao "esquecimento do
  planner", sao decisao registrada (D-2026-07-30-regression-suite-5) com causa tecnica nomeada.
  O planner nao deve tentar fecha-los sem antes reabrir essa decisao.
- Achados de `the-method-refactor` gerados nesta sessao (seam de `IFileUtility` faltando em
  `ReadingManager`; acoplamento concreto do `TranslationEngine` ao LLamaSharp) estao em
  `.jdi/todos.md` secao "De `regression-suite`" — o planner de `the-method-refactor` deve ler
  essa secao antes de comecar.
- Ordem sugerida ao planner: (1) `BookTranslationJobAccessTests.cs` primeiro (maior gap,
  independente dos outros); (2) `BooksAccessTests`/`ReadingManagerTests` (gaps pequenos,
  paralelizaveis entre si); (3) checagem final do guardrail de csproj e da contagem agregada.
- **Duas metricas distintas, medidas nesta sessao — nao confundir no Gate 2.** (a) contagem de
  ATRIBUTOS `[Fact]`/`[Theory]` literais = **167**, a metrica que o DoD item 6 usa (comando
  `grep -rhoE "\[Fact\]|\[Theory\]"`), e a que bate com D-2/PROJECT.md; existem ainda 2
  `[Fact(Skip=...)]` de integracao LLamaSharp que esse padrao deliberadamente NAO conta, entao
  o total de atributos no arquivo e 169. (b) contagem de CASOS EXECUTADOS por `dotnet test` =
  **169 aprovados, 2 ignorados, 171 total** (os `[Theory]` com `InlineData` expandem 1:N).
  Consequencia pro reviewer: o "baseline 167" do Gate 2 esta expresso na metrica (a); ao rodar
  `dotnet test` o numero que aparece e 169 aprovados. Comparar 169 contra 167 e comparar coisas
  diferentes — uma regressao de 2 testes reais passaria por esse gate. O baseline de execucao
  correto a preservar nesta fase e **169 aprovados / 171 total**, e ele deve SUBIR junto com os
  +8 atributos comprometidos.
