# Phase 1: Baseline de estilo — Summary  (slug: baseline-de-estilo)

**Status:** complete (atualizado na iteracao 2 — ver `# Iteracao 2` no fim do arquivo)
**Tasks:** 7/7 completas, 0 blocked
**Ancora (BASELINE):** `cbf92481d996dab1dd22ffaac7d9f01972712f0d`
**Branch:** `feat/baseline-de-estilo` (nao pushada)

> As secoes abaixo sao o registro da **iteracao 1**, preservado como estava. O bloco T-5 continua
> descrevendo o BLOCKED que era verdade naquele momento; ele foi destravado por
> D-2026-08-08-baseline-de-estilo-6 e o resultado esta em `# Iteracao 2`, no fim do arquivo.

---

## T-1 — Ancora da phase (BASELINE) + arvore limpa

**Status:** completed
**Commit:** `657c6ca chore(baseline-de-estilo): anchor phase baseline commit`

- `.claude/scheduled_tasks.lock` estava modificado (lock vivo da sessao). Restaurado com
  `git restore --staged --worktree` antes de tudo, exatamente como o passo 1 manda, para nao ser
  varrido pelo `git add --renormalize .` de T-2.
- `git rev-parse HEAD` gravado em `.jdi/phases/baseline-de-estilo/BASELINE` antes de qualquer outra
  mudanca: `cbf92481d996dab1dd22ffaac7d9f01972712f0d`.
- Acceptance literal exit 0 (SHA valido, tracked, ancestral de HEAD, arvore limpa).

## T-2 — `.gitattributes` + `git add --renormalize .`

**Status:** completed
**Commit:** `a0cc8ab chore(baseline-de-estilo): normalize line endings` (mensagem literal de D-...-4(3))

- `.gitattributes` na raiz: `* text=auto eol=lf` como primeira regra, mais
  `*.ttf *.epub *.png *.jpg *.jpeg *.ico *.zip *.gguf *.pfx` declarados `binary`. `*.svg` declarado
  `text` de proposito (4 assets editaveis), conforme D-...-4(2).
- **O renormalize foi no-op de conteudo, como D-...-4(1) previu.** Medicao antes: o index ja estava
  100% LF (395 `i/lf`, 19 `i/-text`, 7 `i/none`, **zero** `i/crlf` ou `i/mixed`). O unico arquivo
  do commit e o proprio `.gitattributes`.
- Working tree convertida para LF (passo 4) com a arvore ja limpa: **266 arquivos passaram de CRLF
  para LF na working tree**. Depois: 396 `w/lf`, 19 `w/-text`, 7 `w/none`, zero CRLF.
- **O risco dos literais verbatim/raw nao se materializou.** Suite .NET pos-conversao:
  `Failed: 0, Passed: 373, Skipped: 2, Total: 375`. Suite JS: `79/79`. Nenhum BLOCKED.
- DoD 3 e DoD 5 literais: exit 0 ja neste ponto.

## T-3 — `.editorconfig` na raiz

**Status:** completed
**Commit:** `1229bb6 chore(baseline-de-estilo): add root .editorconfig`

- Linha 1 literal `root = true`, sem comentario antes e sem BOM (conferido com `od -c`: o arquivo
  comeca em `r o o t`).
- `[*]`: `end_of_line = lf`, `insert_final_newline`, `trim_trailing_whitespace`, `indent_style`.
  `[*.md]` com `trim_trailing_whitespace = false`. `[*.cs]` com `indent_size = 4` e as preferencias
  de style autorizadas por D-...-1(1) — todas com sufixo `:suggestion`, para valerem em codigo
  novo/tocado sem virar warning de build sobre legado.
- `dotnet_diagnostic.IDE0055.severity = suggestion`, com o motivo em comentario: D-...-1 tranca a
  normalizacao do legado em `dotnet format whitespace`, entao drift de formatacao completa em
  arquivo pre-`4285f25` nao pode quebrar build.
- Nao foi declarado `indent_size` para `[*.csproj]`/`[*.xml]`/`[*.xaml]` (fora de escopo).

## T-4 — `Directory.Build.props` (analyzers ligados) + inventario MEDIDO

**Status:** completed
**Commit:** `228669f chore(baseline-de-estilo): centralize analyzers in Directory.Build.props`

- `Directory.Build.props` na raiz com `EnableNETAnalyzers=true`, `AnalysisLevel=latest-recommended`,
  `EnforceCodeStyleInBuild=true` e `Meziantou.Analyzer` `PrivateAssets="all"`.
- **Versao do Meziantou re-confirmada na execucao**, nao herdada do plano:
  `dotnet package search Meziantou.Analyzer --exact-match` devolveu `3.0.141` como ultima estavel.
  Pinada exatamente essa.
- `TreatWarningsAsErrors` **nao** foi ligado aqui, de proposito (passo 2): liga-lo antes do `NoWarn`
  existir quebraria o build e destruiria o inventario.
- Acceptance (avaliacao MSBuild `-getProperty:`/`-getItem:` nos 3 projetos): exit 0.

### Inventario de warnings medido (entrada de T-5)

Fonte: `TestResults/bde-warning-ids.txt`, gerado dos dois builds Release (app Windows + Tests).
**24 IDs distintos, 0 deles `IDE*`** — nenhum pode ser desviado para o `.editorconfig` pela regra do
passo 1 de T-5; todos os 24 contam contra o teto de 12.

| Familia | Qtd | IDs |
|---|---|---|
| `CS` | 3 | `CS0414`, `CS0618`, `CS8602` |
| `CA` | 10 | `CA1001`, `CA1305`, `CA1707`, `CA1711`, `CA1805`, `CA1822`, `CA1826`, `CA1852`, `CA1859`, `CA1869` |
| `MA` | 11 | `MA0002`, `MA0004`, `MA0006`, `MA0009`, `MA0011`, `MA0016`, `MA0023`, `MA0046`, `MA0048`, `MA0051`, `MA0074` |
| `IDE` | 0 | — |
| `NU` | 0 | — |
| **Total contra o teto** | **24** | |

Onde os campeoes de volume batem (todos em codigo anterior a `4285f25`):

- `MA0004` (`ConfigureAwait`) — o mais espalhado: `ReaderPage.xaml.cs`, `TranslationManager.cs`,
  `ParsingEngine.cs`, `ReaderPageModel.cs`, `LibraryPageModel.cs`, todos os `*Access.cs`.
- `CA1707` (underscore em identificador) — praticamente toda a suite de testes, por causa da
  convencao `Metodo_Faz_Coisa` do xUnit.
- `MA0074` (`StringComparison` explicito) — `HtmlInjectionTests`, `HybridWebViewContractTests`,
  `ThemeEngineTests`, `PromptUtilityTests`.
- `CA1305`/`MA0011` (`IFormatProvider` explicito) — `SettingsAccess.cs` na frente.
- `CS0618` (`DisplayAlert` obsoleto) e `CS0414` (`_needsInjection`) — os dois que o CONTEXT.md ja
  antecipava.

## T-5 — `TreatWarningsAsErrors` + `NoWarn` fechado

**Status:** BLOCKED
**Commit:** `38864b6 docs(baseline-de-estilo): record T-5 blocked by the closed NoWarn cap`

**Motivo:** o inventario medido em T-4 tem **24 IDs `CS`/`CA`/`MA`** contra o **teto de 12** de
D-...-3(3b). Nem `CS` + `CA` sozinhos cabem (13). Nao existe particao legitima que feche a conta:
os 24 sao todos legado, e ha **zero `IDE*`** para rotear ao `.editorconfig`.

D-...-3(3b) trata o teto como regra de parada explicita — "passar disso significa que a decisao de
ligar `latest-recommended` precisa ser revisitada, nao que a lista cresce" — e o passo 4 de T-5
proibe nominalmente as tres saidas que existiriam:

1. curinga / prefixo de categoria no `NoWarn` — proibido por D-...-3(3a);
2. mover `CA*`/`MA*` para `.editorconfig` com `severity = none` so para driblar o teto — proibido
   por D-...-3(3b);
3. corrigir o warning no codigo legado — proibido por D-2 e por D-...-1(4).

Portanto `TreatWarningsAsErrors` **continua desligado** e nenhum elemento `<NoWarn>` foi escrito.
Isso e o comportamento mandado, nao uma falha de execucao — mas custa DoD 2 e DoD 6.

**Decisao que volta para o humano:** manter `AnalysisLevel=latest-recommended` + `Meziantou.Analyzer`
e conviver com uma lista `NoWarn` de 24 (exigiria reabrir D-...-3), ou baixar `AnalysisLevel` /
configurar o Meziantou por regra ate a lista caber em 12 (exigiria reabrir D-...-2). O CONTEXT.md ja
previa esta conversa em "Deferred to PR review": *"se chegar perto do teto de 12, isso e sinal de que
`latest-recommended` + Meziantou pesaram demais sobre o legado"*. A medicao diz que nao chegou
perto — passou do dobro.

**Passo 5 (risco do `build-android`):** nao verificavel nesta maquina. O workload `maui` esta
instalado, mas o csproj so inclui `net10.0-android` nas `TargetFrameworks` se um Android SDK existir
(`$(LocalAppData)\Android\Sdk` / `ANDROID_HOME` / `ANDROID_SDK_ROOT`); nao existe aqui, e o build
falha em `NETSDK1005` antes de compilar. **Efeito colateral do BLOCKED: o risco some por ora** — sem
`TreatWarningsAsErrors`, o job `build-android` do `ci.yml` nao muda de comportamento nenhum. Ele
volta a ser risco no dia em que T-5 for destravada.

## T-6 — `dotnet format whitespace` no repo inteiro

**Status:** completed
**Commits:** `6f1ba2b fix(baseline-de-estilo): drop the editorconfig charset key` +
`e5e5ada style(baseline-de-estilo): apply dotnet format whitespace repo-wide`

- `dotnet format whitespace` (subcomando, nunca o format completo) descobriu o `.slnx` sem
  fallback: os 3 projetos entraram.
- **Um obstaculo real apareceu e foi resolvido sem tocar codigo** — ver "Deltas conscientes": a
  chave `charset = utf-8` que T-3 tinha posto no `.editorconfig` fazia o formatter remover o BOM
  UTF-8 de 6 arquivos, o que **quebrava DoD 5** (git ve a remocao do BOM como mudanca de conteudo na
  linha 1, mesmo com `--ignore-all-space --ignore-blank-lines --ignore-cr-at-eol`). A chave foi
  removida em commit proprio, antes do format.
- O que o format de fato mudou (diff conferido linha a linha, nenhum token movido):
  - `ThemeEngine.cs:12,14` — alinhamento em coluna dos bracos do `switch` expression.
  - `ThemeEngineTests.cs:12` — alinhamento em coluna de um `[InlineData]`.
  - Newline final em 5 arquivos de `Platforms/` (Windows/App.xaml.cs, iOS/AppDelegate.cs,
    iOS/Program.cs, MacCatalyst/AppDelegate.cs, MacCatalyst/Program.cs).
- **`ReaderPage.xaml.cs:122,124` e `TranslationManagerTests.cs:528-529`, citados no CONTEXT.md e no
  `todos/LEGACY.md`, ja nao acusavam nada** com o `.editorconfig` desta phase. A lista original vinha
  do `dotnet format` com regra default.
- **Nenhum teste que le o proprio fonte quebrou.** `DesignSystemTests.cs` e `PixelSpecTests.cs` — os
  candidatos que o plano apontou — passaram sem tocar em assercao nenhuma, porque o format nao entrou
  nos arquivos que eles inspecionam.
- Suite .NET: `Failed: 0, Passed: 373, Skipped: 2, Total: 375`. Suite JS: `79/79`. DoD 4, DoD 5 e
  DoD 7 literais: exit 0.

## T-7 — Fechar a divida de processo (reviewer, registry, doer, todos)

**Status:** completed
**Commit:** `3547b8d docs(baseline-de-estilo): close the recurring lint debt in reviewer, registry and todos`

- **Gate 4 do reviewer** reescrito por D-...-5(1)(2): comando passa a ser
  `dotnet format whitespace --verify-no-changes`; violacao em arquivo tocado pela phase em review =
  **BLOCK**, violacao fora do diff = WARN e nunca blocker. "Tighten to BLOCK-on-new-files once the
  `baseline-de-estilo` phase ships..." removido.
- Gate 4 tambem registra a realidade do T-5 blocked: `TreatWarningsAsErrors` desligado, e warning
  `CS`/`CA`/`MA` **novo** em arquivo tocado pela phase e finding de BLOCK do gate, enquanto warning
  legado e ruido ignorado.
- **Armadilha do DoD 8 tratada como o plano avisou.** As 5 ocorrencias de `WARN.only` no arquivo do
  reviewer, com as 3 de fora do Gate 4 **reescritas preservando a severidade**, nunca promovidas:
  - ~175 build Android secundario -> "a missing workload is reported as WARN, never as BLOCK"
  - ~488 regra 5.11 (event handlers) -> "legacy: WARN, never BLOCK"
  - ~511 regra 5.12 (static mutavel) -> "legacy: WARN, never BLOCK — it is a one-shot native-init
    guard"
- `.jdi/registry/LEGACY.md:30`, `.jdi/registry/LEGACY-reviewers.md:33-34` e
  `.jdi/agents/jdi-doer-translatereader.md:71` reescritos para o toolchain real, com o Meziantou
  `3.0.141` pinado e o estado do `TreatWarningsAsErrors` dito com todas as letras.
- `.jdi/todos/LEGACY.md` marcado com o literal **`RESOLVIDO em baseline-de-estilo`**, citando o
  commit de T-6 e listando o que o format realmente mexeu.
- `npx jdi-cli render` nao foi rodado (passo 7). **`.github/` intocado.**

---

## Deltas conscientes

1. **`charset = utf-8` removido do `.editorconfig` (commit `6f1ba2b`).** T-3 passo 2 sugeria a
   chave; ela e incompativel com D-...-1(4). Com ela, `dotnet format whitespace` remove o BOM UTF-8
   de `Platforms/Windows/App.xaml.cs`, `Platforms/iOS/{AppDelegate,Program}.cs`,
   `Platforms/MacCatalyst/{AppDelegate,Program}.cs` e `test/.../HtmlInjectionTests.cs` — e o
   `git diff` semantico-zero **falha**, porque o BOM nao e espaco em branco aos olhos do git
   (verificado empiricamente: DoD 5 FAIL com a chave, PASS sem ela). Mantive a garantia locked
   (D-...-1(4), verificada por DoD 5) e larguei a chave, que nenhum DoD e nenhuma decisao exigem —
   DoD 1 pede `root=true`, `[*.cs]`, `end_of_line=lf` e `dotnet_diagnostic.`, nao `charset`. O motivo
   esta em comentario no proprio `.editorconfig`. **Consequencia:** o repo segue com 6 arquivos com
   BOM e sem politica de charset declarada. Se isso incomodar, e phase propria — remover BOM e
   mudanca de bytes que esta phase nao esta autorizada a fazer.
2. **Commit extra `fcb6f90 chore(baseline-de-estilo): track ralph loop artifact`.** O
   `.jdi/phases/baseline-de-estilo/LOOP.md` (artefato do orquestrador ralph) estava untracked e
   deixaria a arvore suja, quebrando a acceptance de T-1 (`test -z "$(git status --porcelain)"`).
   Commitado sozinho, depois do ancora e antes de qualquer conteudo, para nao contaminar nem o commit
   do ancora nem o do renormalize. Todas as phases anteriores tem `LOOP.md` tracked.
3. **T-5 gerou um commit apesar de BLOCKED** (`38864b6`), so com a mudanca de status no PLAN.md e o
   motivo no corpo. Nenhuma mudanca de config foi feita nesse commit.

## BLOCKED

**T-5 — `TreatWarningsAsErrors` + `NoWarn` fechado.**
24 IDs `CS`/`CA`/`MA` medidos contra o teto de 12 de D-...-3(3b), zero `IDE*` para desviar, e as tres
saidas possiveis (curinga, `severity = none`, corrigir legado) sao nominalmente proibidas. Regra de
parada acionada como o plano mandou. **Reabre D-...-2 (nivel de analise) ou D-...-3 (teto); nao e
decisao do executor.** Lista completa na secao de T-4.

Impacto: **DoD 2 e DoD 6 falham** e sao as unicas falhas da phase.

## DoD 1-8

| DoD | Resultado | Evidencia |
|---|---|---|
| 1 — `.editorconfig` na raiz com severidade explicita | **PASS** (exit 0) | `head -1` = `root = true`, `[*.cs]`, `end_of_line = lf`, `dotnet_diagnostic.IDE0055` |
| 2 — `Directory.Build.props` flui pros 3 projetos | **FAIL** (exit 1) | `EnableNETAnalyzers=true`, `EnforceCodeStyleInBuild=true`, `AnalysisLevel=latest-recommended` e `Meziantou.Analyzer` OK nos 3; **`TreatWarningsAsErrors=false`** — T-5 BLOCKED |
| 3 — `.gitattributes` + repo 100% LF | **PASS** (exit 0) | zero `i/crlf`/`i/mixed`; 9 extensoes binarias declaradas |
| 4 — `dotnet format whitespace` limpo | **PASS** (exit 0) | `TestResults/bde-format.log` sem violacao |
| 5 — phase semantico-zero em codigo | **PASS** (exit 0) | diff vs `cbf9248` em `*.cs`/`*.xaml`/`*.js` vazio sob `--ignore-all-space --ignore-blank-lines --ignore-cr-at-eol` |
| 6 — build com warnings-as-errors + `NoWarn` fechado | **FAIL** (exit 1) | `0 Error(s)` nos 2 builds, mas **nenhum `<NoWarn>` existe** e os warnings seguem vivos (app 359, tests 527) — T-5 BLOCKED |
| 7 — suite .NET verde + suite JS verde | **PASS** (exit 0) | `Failed: 0, Passed: 373, Skipped: 2, Total: 375`; `node --test test/js/` 79/79 |
| 8 — divida de processo fechada e CI intocado | **PASS** (exit 0) | 0 `WARN.only`, 0 `Tighten to BLOCK`, `RESOLVIDO em baseline-de-estilo` presente, `git diff --name-only cbf9248 -- .github/` vazio |

**6/8 PASS. As 2 falhas tem a mesma causa unica, mandada por decisao: T-5 BLOCKED pelo teto de 12.**

## Arquivos modificados

Codigo (semantico-zero, so whitespace/newline final):
- `src/TranslateReader.Core/Business/Engines/ThemeEngine.cs`
- `src/TranslateReader/Platforms/Windows/App.xaml.cs`
- `src/TranslateReader/Platforms/iOS/AppDelegate.cs`
- `src/TranslateReader/Platforms/iOS/Program.cs`
- `src/TranslateReader/Platforms/MacCatalyst/AppDelegate.cs`
- `src/TranslateReader/Platforms/MacCatalyst/Program.cs`
- `test/TranslateReader.Tests/ThemeEngineTests.cs`

Config (novos, raiz): `.editorconfig`, `.gitattributes`, `Directory.Build.props`

Processo: `.jdi/agents/jdi-reviewer-translatereader.md`, `.jdi/agents/jdi-doer-translatereader.md`,
`.jdi/registry/LEGACY.md`, `.jdi/registry/LEGACY-reviewers.md`, `.jdi/todos/LEGACY.md`,
`.jdi/phases/baseline-de-estilo/{BASELINE,PLAN.md,LOOP.md}`

**`.github/`: zero arquivos tocados.**

## Testes

- .NET: `Total 375`, `Passed 373`, `Failed 0`, `Skipped 2` (piso `>= 375 / 0 / <= 2` atendido).
- JS: `node --test test/js/` — 79 tests, 79 pass, 0 fail.
- Cobertura: **zero arquivo `.cs` novo nesta phase** -> Gate 3 reporta SKIPPED por D-2. Nao e falha e
  nao ha exigencia nova de cobertura.
- Build app Windows Release: `0 Error(s)`. Build Tests Release: `0 Error(s)`.

---

# Iteracao 2 (ralph) — T-5 destravado por D-...-6

**Escopo desta iteracao:** somente T-5. T-1..T-4, T-6 e T-7 ja estavam `completed` e nao foram
refeitos. Commit: `2e857a0 chore(baseline-de-estilo): treat warnings as errors with a closed NoWarn list`.

## O que mudou na regra do jogo

D-2026-08-08-baseline-de-estilo-6 emendou D-...-3: **o teto numerico de 12 esta revogado**. No lugar
dele, a lista `NoWarn` passa a ser exatamente o que a medicao deixou **apos calibrar** as rules cujo
pressuposto nao vale naquela pasta. Toda a validacao estrutural de D-...-3 continua valendo (elemento
`<NoWarn>` unico, so IDs concretos, nunca curinga, comentario por ID em linha propria), e o CONTEXT.md
ja saiu com DoD 6 sem a assercao `<= 12`.

O criterio de D-...-6(2) foi aplicado **item a item** aos 24 IDs, na forma literal em que esta escrito:
*se a rule fosse corrigida em vez de suprimida, o codigo ficaria pior?* Se sim -> calibracao, no escopo
mais estreito possivel, com o motivo tecnico em comentario acima da chave no `.editorconfig`. Se o
codigo ficaria melhor e so da trabalho -> divida -> `NoWarn`. **Empate conta como divida** — nao houve
nenhuma calibracao feita para encurtar a lista, que D-...-6(2) mantem proibida.

## Medicao usada como base

Os logs de T-4 (`TestResults/bde-inv-app.log`, `bde-inv-tests.log`) foram reprocessados por ID **e por
pasta**, que era o dado que faltava para decidir escopo. Nota metodologica: o MSBuild reporta cada
warning duas vezes (inline + sumario), entao as contagens abaixo sao **ocorrencias unicas por
`file:line:col`**, nao as brutas. Onde D-...-6 cita 688 (`CA1707`) e ~544 (`MA0004`), sao as brutas;
os unicos correspondentes sao 344 e 282.

## Decisao ID a ID (os 24)

| # | ID | Unicos | Onde | Decisao | Motivo (criterio de D-...-6(2)) |
|---|---|---|---|---|---|
| 1 | `CA1707` | 344 | so `test/` | **CALIBRAR** `[test/**.cs]` | Nome de teste xUnit e `Metodo_Cenario_Esperado`; o underscore e o que torna a falha legivel no runner. Renomear 344 testes para satisfazer uma guideline de API publica piora. Rule segue ligada em `src/`. |
| 2 | `CA1711` | 1 | so `test/` | **CALIBRAR** `[test/**.cs]` | `NonParallelCollection` e marcador `[CollectionDefinition]`; `<Nome>Collection` e o idioma do proprio xUnit. A rule protege consumidor de API publica contra tipo que parece `ICollection` — marcador de assembly de teste nao tem consumidor. Rule segue ligada em `src/`. |
| 3 | `MA0004` | 282 (107 app / 165 Core / 10 test) | os 3 | **CALIBRAR** `[src/TranslateReader/**.cs]` **+ `NoWarn`** | Na camada Client, `ConfigureAwait(false)` nao e estilo, e defeito: csharp.md secao 3 exige continuacao na main thread e todas essas continuacoes tocam page/PageModel/WebView. **Medido o Core como o orquestrador pediu: 165 ocorrencias, e la a rule esta CERTA** (biblioteca) -> essas 165 + as 10 de teste sao divida e vao para o `NoWarn`. Ver "Delta consciente 1". |
| 4 | `CA1805` | 2 | `Core/Models` | **CALIBRAR** `[src/TranslateReader.Core/Models/**.cs]` | As 11 propriedades de `ReadingSettings` declaram o default inline; a classe **e** a spec legivel dos defaults de leitura. Tirar `= 0` das duas que calham de ser zero deixa a tabela meio documentada, para poupar uma inicializacao de campo num objeto criado meia duzia de vezes. Rule segue ligada fora de `Models/`. |
| 5 | `CA1001` | 3 | app | **`NoWarn` RISCO** | Tipo dono de `CancellationTokenSource` sem `IDisposable`. Corrigir MELHORA (csharp.md secao 2.4). Bug potencial, nao estilo. |
| 6 | `CA1305` | 10 | Core | **`NoWarn` RISCO** | Conversao sensivel a cultura. `SettingsAccess` grava `double` como string: em pt-BR vira `"1,6"` e o seed invariante deixa de fazer round-trip. Corrigir MELHORA. |
| 7 | `MA0011` | 15 | Core | **`NoWarn` RISCO** | Mesmo defeito de `CA1305` visto pelo Meziantou (`ToString`/`TryParse`/`Append` sem `IFormatProvider`). |
| 8 | `MA0009` | 18 | `ParsingEngine` + test | **`NoWarn` RISCO** | Regex sem timeout sobre HTML de EPUB, que csharp.md secao 4 declara **entrada nao confiavel**. ReDoS. Seguranca e prioridade 1 do projeto. |
| 9 | `CS8602` | 7 | test | **`NoWarn` RISCO** | Deref possivelmente nulo; quando dispara, o teste morre com `NullReferenceException` em vez de reportar assercao. Nomeado por D-...-6(4). |
| 10 | `CS0414` | 1 | `ReaderPage.xaml.cs:21` | **`NoWarn` RISCO** | `_needsInjection` e escrito em 114 e 125 e **nunca lido**: guard de reinjecao do WebView que parou de guardar. Logica morta, nao estilo. |
| 11 | `CS0618` | 7 | app | `NoWarn` | `Page.DisplayAlert` obsoleto; `DisplayAlertAsync` e a troca. Corrigir MELHORA, e so trabalho. |
| 12 | `CA1822` | 1 | app | `NoWarn` | Membro nao toca estado de instancia e poderia ser `static`. Corrigir MELHORA. |
| 13 | `CA1826` | 1 | app | `NoWarn` | Metodo LINQ sobre colecao indexavel. Corrigir MELHORA (perf). |
| 14 | `CA1852` | 1 | app | `NoWarn` | Tipo interno selavel. Corrigir MELHORA. |
| 15 | `CA1859` | 2 | Core + app | `NoWarn` | Tentei o argumento "corrigir piora" (trocar `IReadOnlyDictionary` por `Dictionary` exporia static mutavel, contra csharp.md secao 2.4) e ele **nao se sustenta**: csharp.md secao 2.1 ja manda `FrozenDictionary`, que tambem e concreto e fecha a rule sem piorar nada. Logo, divida. |
| 16 | `CA1869` | 1 | app | `NoWarn` | `JsonSerializerOptions` novo por serializacao na ponte do WebView. csharp.md secao 2.2 manda cachear. Corrigir MELHORA. |
| 17 | `MA0002` | 11 | Core + test | `NoWarn` | Lookup de string sem `StringComparer`. csharp.md secao 2.1 quer `Ordinal`. Corrigir MELHORA. |
| 18 | `MA0006` | 15 | Core + app | `NoWarn` | Comparacao de string com operador em vez de `string.Equals` com `StringComparison` explicito. Corrigir MELHORA. |
| 19 | `MA0016` | 1 | `HtmlUtility` | `NoWarn` | Retorna `List<string>` em vez de abstracao. Argumento de hot path existe, mas o ganho e imperceptivel e devolver abstracao e melhor design: **empate tecnico conta como divida**, nao calibracao. Registrada a contradicao com `CA1859` (#15) — as duas rules mandam o oposto. |
| 20 | `MA0023` | 12 | `ParsingEngine` + test | `NoWarn` | Regex sem `ExplicitCapture`. Mesmos regexes de `MA0009`; corrigir junto. Corrigir MELHORA. |
| 21 | `MA0046` | 1 | app | `NoWarn` | Payload de `EventHandler<ReadingSettings>` nao deriva de `EventArgs`. Envolver num `EventArgs` e cerimonia, mas e a guideline do .NET: empate -> divida. |
| 22 | `MA0048` | 2 | app + test | `NoWarn` | Nome de arquivo != nome do tipo. Separar arquivos MELHORA a organizacao. So trabalho. |
| 23 | `MA0051` | 2 | app | `NoWarn` | Metodo com 91 e 76 linhas contra o limite de 60 do Meziantou e o de 20 de csharp.md secao 7. Corrigir MELHORA — e exatamente o que a regra do projeto ja pede. |
| 24 | `MA0074` | 138 | so test | `NoWarn` | Tentacao de calibrar ("assercao de teste otimiza legibilidade", csharp.md preambulo). **Recusada pela medicao**: entre as 138 ha `StartsWith` (9) e `EndsWith` (3), cujo default e **culture-sensitive**. Nao e cerimonia, e correcao. Divida. |

**Placar: 4 calibradas, 21 no `NoWarn`** (`MA0004` esta nos dois, por medicao — ver Delta 1).
24 - 3 que sairam inteiras da lista (`CA1707`, `CA1711`, `CA1805`) = 21.

## `NoWarn` final (21 IDs, elemento unico, sem curinga)

```
$(NoWarn);CA1001;CA1305;CA1822;CA1826;CA1852;CA1859;CA1869;CS0414;CS0618;CS8602;
MA0002;MA0004;MA0006;MA0009;MA0011;MA0016;MA0023;MA0046;MA0048;MA0051;MA0074
```

(no arquivo o valor esta numa linha unica — DoD 6 parseia entre `<NoWarn>` e `</NoWarn>`.)

**6 marcados `RISCO:`** por D-...-6(4): `CA1001`, `CA1305`, `CS0414`, `CS8602`, `MA0009`, `MA0011`.
D-...-6(4) exigia no minimo `CS8602` e `CA1001`; os outros quatro entraram com evidencia propria
(ReDoS sobre entrada nao confiavel, defeito de locale no round-trip de settings, guard morto).
Todos roteados para phase futura de correcao em
`.jdi/todos/2026-08-08-baseline-de-estilo.md`, secao "Warnings congelados no `NoWarn`", com
prioridade 1 (RISCO) e prioridade 2 (divida de qualidade) separadas.

## Deltas conscientes da iteracao 2

1. **`MA0004` aparece nos dois lugares, de proposito.** A medicao mandou: 107 ocorrencias na camada
   Client (calibracao — `ConfigureAwait(false)` la e defeito) e 175 fora dela (165 no Core + 10 em
   teste), que sao divida legitima porque **no Core a rule esta certa** (D-...-6(1)). Como DoD 6 exige
   um `<NoWarn>` **unico** no `Directory.Build.props`, a supressao da divida so existe repo-wide —
   entao hoje a chave do `.editorconfig` fica subsumida por ela. Ela foi escrita mesmo assim porque e
   o que sobra valendo **no dia em que a divida do Core for paga e `MA0004` sair do `NoWarn`**: sem
   ela, a rule voltaria a exigir `ConfigureAwait(false)` na UI, que csharp.md secao 3 proibe. O motivo
   esta escrito nos dois comentarios, e o item esta no todo. Nao e config morta: e a metade da
   decisao que sobrevive a outra.
2. **Nada de codigo foi tocado.** T-5 mexeu em `.editorconfig`, `Directory.Build.props`, no todo e no
   PLAN. Zero `.cs`/`.xaml`/`.js` — DoD 5 continua exit 0 pelo mesmo ancora `cbf9248`.
3. **`.github/` intocado**, como D-...-5(3) manda e DoD 8 confere.
4. **`PLAN.md` recebeu uma linha de EMENDA** em T-5 registrando que o teto de 12 do passo 4/acceptance
   foi revogado por D-...-6. O texto original do plano nao foi reescrito.
5. **Passo 5 de T-5 (risco do `build-android`) continua nao verificavel nesta maquina** — sem Android
   SDK, o csproj nem inclui `net10.0-android` nas `TargetFrameworks` (`NETSDK1005`). Diferenca em
   relacao a iteracao 1: agora `TreatWarningsAsErrors=true` **esta** ligado, entao o risco voltou a
   existir de verdade. O job `build-android` do `ci.yml` pode acender IDs que o TFM Windows nao
   acende, e eles viram erro. **Fica como risco a confirmar no PR**, nao como falha de task (fallback
   do specialist). `TestResults/bde-android-ids.txt` esta vazio por esse motivo.
6. **`LOOP.md` nao foi commitado nesta iteracao** — e artefato do orquestrador ralph, escrito por ele
   durante a execucao; entrar num commit de task quebraria a atomicidade.

## DoD 1-8 (rodados literais do CONTEXT.md, nesta iteracao, todos os 8)

| DoD | Resultado | Evidencia |
|---|---|---|
| 1 — `.editorconfig` na raiz com severidade explicita | **PASS** (exit 0) | `head -1` segue `root = true` (as 4 secoes calibradas entraram no fim do arquivo); `[*.cs]`, `end_of_line = lf`, `dotnet_diagnostic.` presentes |
| 2 — `Directory.Build.props` flui pros 3 projetos | **PASS** (exit 0) | avaliacao MSBuild nos 3: `"TreatWarningsAsErrors": "true"` em Core, Tests e app (era o unico item que faltava na iteracao 1) |
| 3 — `.gitattributes` + repo 100% LF | **PASS** (exit 0) | zero `i/crlf`/`i/mixed`; 9 extensoes binarias declaradas |
| 4 — `dotnet format whitespace` limpo | **PASS** (exit 0) | `TestResults/bde-format.log` vazio — as secoes novas do `.editorconfig` nao geraram churn |
| 5 — phase semantico-zero em codigo | **PASS** (exit 0) | diff vs `cbf9248` em `*.cs`/`*.xaml`/`*.js` vazio; T-5 nao tocou codigo |
| 6 — build com warnings-as-errors + `NoWarn` fechado | **PASS** (exit 0) | app Windows Release e Tests Release: **`0 Error(s)` e `0 Warning(s)`**; zero `: warning CS/CA/MA/IDE` nos 2 logs; `<NoWarn>` unico, 21 IDs concretos, nenhum curinga, cada ID em >= 2 linhas |
| 7 — suite .NET verde + suite JS verde | **PASS** (exit 0) | `Failed: 0, Passed: 373, Skipped: 2, Total: 375`; `node --test test/js/` 79/79 |
| 8 — divida de processo fechada e CI intocado | **PASS** (exit 0) | 0 `WARN.only`, 0 `Tighten to BLOCK`, `RESOLVIDO em baseline-de-estilo` presente, `git diff --name-only cbf9248 -- .github/` vazio |

**8/8 PASS.** As duas falhas da iteracao 1 (DoD 2 e DoD 6) tinham causa unica — T-5 travado — e
fecharam juntas.

## Testes (iteracao 2)

- .NET: `Total 375`, `Passed 373`, `Failed 0`, `Skipped 2` — identico a iteracao 1, sem regressao.
- JS: `node --test test/js/` — 79 tests, 79 pass, 0 fail.
- Build app Windows Release: `0 Error(s)`, `0 Warning(s)`. Build Tests Release: idem.
- Cobertura: zero `.cs` novo na phase -> Gate 3 SKIPPED por D-2, sem exigencia nova.
