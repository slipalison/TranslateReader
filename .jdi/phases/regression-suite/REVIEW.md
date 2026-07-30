# Phase 12: Review  (slug: regression-suite)

**Verdict:** APPROVED_WITH_WARNINGS

**Round 2** da cadeia autonoma do `/jdi-issue`. Revisao executada por `jdi-reviewer-translatereader`
(Fable 5, xhigh — D-7), mode=verify. Commits revisados: `299f150..cb43e71` — 6 commits da fase
(`2510828..1992fcb`, ja aprovados no round 1) + **3 commits da rodada de correcao de warnings**
(`137cd1d`, `41a9f0b`, `cb43e71`). Nenhum claim do doer foi aceito sem medicao — todos os numeros
e provas abaixo foram re-derivados nesta sessao, incluindo a replicacao adversarial da prova por
mutacao B vs C.

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `src/TranslateReader/TranslateReader.csproj` -c Release -f net10.0-windows10.0.19041.0 — 0 erros, 40 warnings MVVMTK0045 (legados, identicos ao round 1) |
| Tests | PASS | **196 aprovados / 2 ignorados / 198 total / 0 falhas** (re-medido apos a rodada de correcao). Atributos `[Fact]`/`[Theory]` literais: **192** (baseline 167, D-2) — a correcao nao adicionou testes, so 4 linhas de stub. Execucao: baseline 169/171 preservado e elevado. Os 2 skips seguem sendo os 2 `[Fact(Skip=...)]` de integracao LLamaSharp |
| Coverage | WARN | Inalterado: unico `.cs` novo desde `4285f25` e `BookTranslationJobAccessTests.cs` (arquivo de TESTE), ausente do Cobertura porque coverlet exclui a test assembly. Agregado 82,72% (contexto apenas, identico ao round 1). Nao mensuravel por design em fase so-de-teste |
| Lint | WARN | `dotnet format --verify-no-changes` (escopo solucao): **12** violacoes WHITESPACE, todas legadas (D-2), **zero novas** — enumeracao re-medida e conferida contra a tabela corrigida do SUMMARY (`41a9f0b`): bate arquivo a arquivo, linha a linha (5 em `src/`, 7 no test project). Permanece WARN ate `baseline-de-estilo` |
| Security/Layer | PASS | `git diff --name-only 299f150..HEAD -- src/` **vazio** E `git diff -- src/` **vazio** — toda mutacao da prova foi revertida; `src/` bit-identico a `299f150`. Greps 5.1/5.2/5.10/5.12/5.15/5.17 identicos ao baseline do bootstrap. As 4 linhas novas da correcao sao stubs sobre `ITranslationCacheAccess` (mock de interface, Gate 5.17 limpo) |
| Consistency | PASS | 9 commits atomicos, Conventional Commits com scope `regression-suite`, tipos corretos (`test` x6, `docs` x3). `137cd1d` = `test` (muda codigo de teste), `41a9f0b`/`cb43e71` = `docs` — tipagem apropriada |
| UI Validation | SKIPPED | has_frontend=false (cliente MAUI nativo) |
| DoD | PASS | 6/6 auto PASS (re-executados neste round), 0 manual (dod=auto_only). CONTEXT.md **nao foi editado** na fase inteira (`git diff 299f150..HEAD -- CONTEXT.md` vazio) — DoD locked respeitado |

## Blockers

_(nenhum)_

## Warnings

Mapeamento contra o round 1 — o que foi resolvido vs o que permanece:

1. **[STANDING = W1 do round 1] Gate 3 — cobertura nao mensuravel no escopo da fase (by design).**
   Corretamente triado como nao-corrigivel numa fase so-de-teste: consertar exigiria codigo de
   producao, o que o PLAN proibe explicitamente. Sem acao; o gate volta a morder quando uma fase
   criar arquivo novo em `src/`.
2. **[STANDING = W2 do round 1] Gate 4 — 12 violacoes WHITESPACE legadas** (7 no test project +
   5 em `src/`). Isentas por D-2; nao-corrigiveis aqui (5 exigem tocar `src/`). Apertar para
   BLOCK-em-arquivo-novo quando `baseline-de-estilo` entregar `.editorconfig` + analyzers.
3. **[RESOLVIDO — era o W3 do round 1] Precisao do SUMMARY sobre o `dotnet format`.** O commit
   `41a9f0b` corrigiu a enumeracao de 7 (escopo test project) para 12 (escopo solucao), com a
   distincao de escopo explicita. Verifiquei por medicao propria: a lista corrigida esta EXATA
   (12 hits, mesmos arquivos/linhas) e a afirmacao central preservada ("identica ao baseline,
   zero novas") continua valida — `src/` bit-identico a `299f150` e as 4 linhas de stub da
   correcao (682-683/703-704) nao tocam nenhuma linha flagada (25/42/12/528/529, todas antes dos
   pontos de insercao).
4. **[STANDING = W4 do round 1] Legado inalterado** (ja conhecido do bootstrap): static mutavel
   `TranslationEngine._nativeLibraryConfigured`; `catch { }` em `ReaderPage.xaml.cs:326,434`;
   OCE engolida em 3 pontos do Client layer; desequilibrio historico subscribe/unsubscribe (5/4).
   Corretamente triado como fora do alcance de uma fase so-de-teste — nada disso era corrigivel
   sem tocar `src/`.
5. **[NOVO — relato, nao merito] Rodada C da prova de mutacao: o desfecho do teste nao foi
   declarado, e o resumo repassado pela cadeia ("passa vacuamente") esta errado na letra.**
   Medicao minha (tabela abaixo): sob a mutacao B com o codigo de teste PRE-correcao, os 2 testes
   **NAO passam** — falham 2/2 na assercao PRIMARIA (`Assert.ThrowsAny() Failure: No exception
   was thrown`), porque o falso cache hit pula tanto `GenerateAsync` quanto o throw movido.
   A alegacao SUBSTANTIVA do doer esta correta e foi confirmada (a engine e inalcancavel atras
   do falso cache hit, logo a assercao secundaria pre-correcao nao podia falhar sob NENHUMA
   mutacao do comportamento de cancelamento = vacua; e nunca houve falso verde, nem pre nem pos).
   Mas a linha C da tabela do SUMMARY omite o desfecho do teste (as linhas A e B declaram os
   seus: "2 falhas / 2") e diz "a assercao secundaria e satisfeita" — contrafactual, ja que a
   execucao aborta na primaria antes de avalia-la. Mesma classe do W3 do round 1: calibrar
   confianca em auto-relatos da cadeia autonoma; a prova que sustenta o fix e a B (medida e
   confirmada), nao a C.

## Verificacao da rodada de correcao (commits `1992fcb..cb43e71`) — claims vs medicao

### 1. `137cd1d` — stub explicito nos 2 testes de cancelamento — **CONFIRMADO (com a ressalva do warning 5)**

Diff conferido: exatamente 4 linhas de teste (2 stubs `FetchTranslationAsync(...).Returns((string?)null)`,
um por teste) + documentacao no SUMMARY. Nenhum teste novo, nenhum atributo novo (192 mantidos).

Replicacao adversarial da prova por mutacao — eu mesmo apliquei as mutacoes em
`src/TranslateReader.Core/Business/Managers/TranslationManager.cs`, rodei
`--filter FullyQualifiedName~WithCancelledToken` e reverti com `git checkout -- src/` entre rodadas:

| # | Mutacao em src/ | Codigo de teste | Resultado MEDIDO por mim | Bate com o claim? |
|---|---|---|---|---|
| A | `ct.ThrowIfCancellationRequested()` removido dos loops de `TranslateChapterAsync` (:226) e `TranslateParagraphsAsync` (:274) | corrigido (HEAD) | **2 falhas / 2** na PRIMARIA: `Assert.ThrowsAny() Failure: No exception was thrown` | SIM — identico ao SUMMARY linha A |
| B | throw movido para DEPOIS de `GenerateAsync` (:248/:291) — OCE ainda e lancada, mas a engine roda antes | corrigido (HEAD) | **2 falhas / 2** na SECUNDARIA: `ReceivedCallsException: Expected to receive no calls ... Actually received 1 matching call: GenerateAsync(...)` | SIM — **a claim load-bearing**: a assercao secundaria agora carrega peso |
| C | mesma mutacao B | pre-correcao (`git checkout 1992fcb -- <teste>`) | **2 falhas / 2 na PRIMARIA** (`No exception was thrown`) — a engine nunca e chamada (o falso cache hit `string.Empty` curto-circuita o loop e pula tambem o throw movido) | PARCIAL — a vacuidade da secundaria esta comprovada (ela e inalcancavel), mas o teste pre-correcao NAO "passa vacuamente": ele tambem fica vermelho, so que pela primaria. Ver warning 5 |

Apos as rodadas: `git checkout -- src/`, suite completa re-rodada **verde (196/2/198)**,
`git diff -- src/` vazio, arvore limpa. Nenhuma mutacao commitada.

Leitura de merito: o fix e correto e valioso independente da imprecisao da linha C — com o stub,
os testes exercitam o caminho REAL de cancelamento (cache miss), e a dupla assercao
primaria+secundaria discrimina tanto a remocao (A) quanto o reordenamento (B) do throw. Antes do
fix, a secundaria era infalsificavel.

### 2. `41a9f0b` — enumeracao do `dotnet format` corrigida para escopo solucao — **CONFIRMADO**

Re-executei `dotnet format --verify-no-changes` no escopo da solucao neste round: **12** violacoes
WHITESPACE — `src/`: `ThemeEngine.cs(12,24)`, `(14,11)`, `ReadingManager.cs(55,1)`,
`ReaderPage.xaml.cs(122,103)`, `(124,72)`; `test/`: `HtmlInjectionTests.cs(25,1)`, `(42,1)`,
`ThemeEngineTests.cs(12,33)`, `TranslationManagerTests.cs(528,21)`, `(528,33)`, `(528,61)`,
`(529,31)`. Identico, item a item, a tabela corrigida do SUMMARY. A afirmacao central preservada
("identica ao baseline `299f150`, zero novas") segue valida: a comparacao com o baseline foi
estabelecida no round 1 por worktree, `src/` continua bit-identico, e a unica mudanca de teste
da rodada (4 linhas em 682-683/703-704) nao desloca nem toca as linhas flagadas. **W3 do round 1
resolvido.**

### 3. `cb43e71` — nota em `.jdi/todos.md` sobre o grep estreito do DoD 5 — **CONFIRMADO**

- A nota existe (`.jdi/todos.md` § "De `regression-suite`", item [PROCESSO/DoD]) e esta
  tecnicamente correta: o `Verify:` do DoD 5 procura o literal `net10.0-windows`, entao um
  `<TargetFrameworks>net10.0;net10.0-android</TargetFrameworks>` hipotetico passaria (nenhuma
  substring `net10.0-windows`). A recomendacao para phases futuras (probar `<TargetFrameworks`
  plural e `UseMaui`) fecha exatamente esse buraco.
- **CONTEXT.md NAO foi editado:** `git diff 299f150..HEAD -- .jdi/phases/regression-suite/CONTEXT.md`
  vazio. O DoD locked ficou intacto; a nota foi para o lugar certo (todos.md, nao o DoD).
- Reforco independente: re-inspecionei o csproj de teste — `<TargetFramework>net10.0</TargetFramework>`
  singular, sem `UseMaui`, exatamente 1 `.csproj` sob `test/`. O guardrail da decisao (c) esta
  de fato honrado, pelo criterio forte (inspecao), nao so pelo grep fraco.

## Invariantes re-medidos neste round

- **Fase so-de-teste:** `git diff --name-only 299f150..HEAD -- src/` vazio E `git diff -- src/`
  vazio (arvore de trabalho). `src/` bit-identico a `299f150`.
- **Contagens:** 192 atributos (>= 192 exigido; DoD >= 175; piso do PLAN 188) e
  196 passed / **exatamente 2** skipped / 0 failed (>= 196 exigido).
- **Nenhum `.csproj` tocado** na fase inteira (`git diff --name-only 299f150..HEAD -- '*.csproj'`
  vazio); test project single-TFM `net10.0`.
- Working tree ao final: limpo (unico untracked: este REVIEW.md, que o orquestrador commita).

## DoD Checklist (gate 8)

Todos re-executados neste round (nao herdados do round 1):

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | `BookTranslationJobAccessTests.cs` existe, usa `InMemoryDatabase`, cobre os 4 metodos publicos | CONTEXT | Auto | PASS | exit 0 (arquivo + 4 greps de metodo) |
| 2 | `BookTranslationJobAccessTests.cs` com >= 6 casos | CONTEXT | Auto | PASS | 12 atributos (>= 6) |
| 3 | `BooksAccessTests.cs` caracteriza ordenacao + >= 7 atributos | CONTEXT | Auto | PASS | `grep -i order` ok; 8 atributos |
| 4 | `ReadingManagerTests.cs` >= 6 atributos (caso "progresso encontrado") | CONTEXT | Auto | PASS | 7 atributos |
| 5 | Guardrail: sem 2o test project / multi-target MAUI | CONTEXT | Auto | PASS | 0 hits `net10.0-windows`; 1 csproj; inspecao: TFM singular, sem `UseMaui` |
| 6 | Atributos `[Fact]`/`[Theory]` >= 175 (baseline 167) | CONTEXT | Auto | PASS | **192** >= 175 |

**Totals:** 6 items | Auto: 6 (6 PASS, 0 FAIL) | Manual: 0 pending

_(PROJECT.md nao declara `## Definition of Done`; itens vieram integralmente do CONTEXT.md — nao e
INCONCLUSIVE porque o CONTEXT declara DoD valido com `Verify:`/`Source:`. O dod-critic do round 1
ja havia varrido as 6 linhas Auto PASS: zero hollow.)_

## Recommendation

Aprovar e seguir para `/jdi-ship regression-suite` + PR. A rodada de correcao entregou o que
prometeu: warning 3 do round 1 esta **resolvido** (enumeracao corrigida e verificada exata), a
assercao secundaria dos 2 testes de cancelamento agora e **comprovadamente load-bearing** (mutacao
B medida por mim: falha exatamente nela), e a divida do grep estreito foi registrada no lugar
certo sem tocar o DoD locked. Warnings 1, 2 e 4 permanecem como standing — corretamente triados
como impossiveis de corrigir numa fase so-de-teste (exigem `src/`); nao representam omissao da
rodada. O unico achado novo (warning 5) e de precisao de RELATO na linha C da prova de mutacao —
nao afeta o codigo shipped, que e estritamente mais forte que o do round 1. Para o PR humano:
alem dos 3 itens "deferred" ja com parecer favoravel no round 1, vale 1 linha de atencao ao
warning 5 como historico de calibracao de auto-relatos da cadeia autonoma.
