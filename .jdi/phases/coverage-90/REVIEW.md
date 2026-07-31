# Phase 15: Review  (slug: coverage-90)

**Verdict:** APPROVED_WITH_WARNINGS

Iteracao 1 · Diff revisado: `main` (`1af3a51`) -> `jdi/coverage-90` (`09563ad`), 12 commits.
Toda evidencia abaixo foi produzida por esta revisao (builds, testes, cobertura, mutacoes,
worktree de baseline) — nada foi aceito do SUMMARY sem re-medicao.

## Gates

| Gate | Comando | Resultado real | Status |
|---|---|---|---|
| 1 Build | `dotnet restore && dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` | 0 erros, 8 warnings (CS0618/CS0414, todos em `src/` legado intocado) | PASS |
| 1b Build testes | `dotnet build test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release` | **0 warnings, 0 erros** (todo o codigo novo da phase) | PASS |
| 2 Tests C# | `dotnet test ... -c Release` | **304 testes: 302 aprovados, 2 ignorados, 0 falhas** (baseline D-2: 256 = 254p/2s; +48, os 2 skips sao os mesmos `TranslationEngineTests` de integracao legados) | PASS |
| 2b Tests JS | `node --test test/js/` (Node v24.14.0) | **60/60 pass, 0 fail** | PASS |
| 3 Coverage | Cobertura XML + lcov (re-medidos); novos arquivos = so testes; alvos C# da phase re-verificados por arquivo | `ModelAccess` 40/40 L + 24/24 C; `SettingsAccess` 65/65 + 30/30; `FileUtility` 20/20 + 6/6; `HtmlUtility` 73/73 + 46/46; `ParsingEngine` 198/199 + 97/102 (6 unidades residuais); JS 287/287 L + 98/98 BR | PASS |
| 4 Lint | `dotnet format TranslateReader.slnx --verify-no-changes` | exit 0 (unico output: MVVMTK0045 pre-existentes do build, `src/` legado) | PASS |
| 5 Security/Layer | bateria 5.1/5.2/5.10/5.12/5.15/5.16/5.17 + leitura manual dos testes novos | 5.1=0, 5.2=0, sync-over-async=0, static mutavel=1 (baseline `TranslationEngine._nativeLibraryConfigured`), empty catch=5 (todos `src/` legado, pre-existentes em `main`), TODO=0; I/O em teste novo 100% autorizado nomeadamente (ver (f)) | PASS |
| 6 Consistency | `git log`/`git diff 1af3a51..HEAD` vs PLAN | 12 commits Conventional (`test`/`ci`/`docs` + scope `coverage-90`); files_modified do PLAN todos presentes; 1 arquivo extra (`test/js/index.js`) = desvio declarado e justificado no SUMMARY; **0 arquivos de `src/`**; 0 linhas deletadas em `test/` | PASS |
| 7 UI Validation | — | SKIPPED (has_frontend=false, cliente MAUI nativo) | SKIPPED |
| 8 DoD | 5 itens Auto do CONTEXT.md rodados LITERALMENTE (PROJECT.md nao tem secao DoD; 0 itens Manual) | DoD1 exit=0 · DoD2 exit=0 · DoD3 exit=0 (rate=1) · DoD4 exit=0 (FU=1, HU=1) · DoD5 exit=0 | PASS |

## Veredito ponto a ponto (a)-(i)

### (a) A projecao de 95,32% se sustenta? — SIM, reproduzida de forma independente
Conta refeita do zero (parser proprio sobre o Cobertura, deduplicando linhas por arquivo e
excluindo `obj/` gerado; lcov agregado por `SF:`). Detalhe completo em `## Aritmetica de
cobertura`. Resultado: **1771/1858 = 95,32%** — identico ao do doer, divergencia 0,00pp.
O baseline tambem foi validado: worktree temporario em `1af3a51` re-executou a suite (256
testes, 254p/2s) e produziu **C# 1336/1565** -> com JS 0/195: **1336/1760 = 75,91%** vs
SonarCloud **1339/1764 = 75,90%** (delta 3 unidades cobertas / 4 no denominador ≈ 0,01pp).
A alegacao de fidelidade do proxy e verdadeira e foi provada por medicao propria, nao aceita.

### (b) Cobertura JS atribuida aos arquivos de producao? — SIM, com prova por mutacao
- `TestResults/js-lcov.info` contem exatamente 4 records `SF:`, todos resolvendo em
  `src\TranslateReader\Resources\Raw\wwwroot\js\{bridge,paginated,scroll,translation}.js`,
  com `LH=LF` > 0 (95/105/36/51). Nenhum `SF:` de `test/js/` (harness, index e `*.test.js`
  ficam fora do relatorio).
- O harness (`test/js/harness.js:395-399`) faz `fs.readFileSync` do caminho real +
  `new vm.Script(code, {filename: file})` — nenhum teste embute copia inline de producao.
- Prova definitiva: mutacoes aplicadas NO ARQUIVO DE PRODUCAO quebraram os testes
  (ver (g)) — os testes exercitam o arquivo real, nao uma copia.

### (c) `test/js/index.js` (desvio declarado) — CONFIRMADO nos 3 itens
1. O comando literal do CONTEXT funciona: `node --test test/js/` -> 60/60 pass.
2. A alegacao sobre Node >= 24 e verdadeira: reproduzi fora do repo (dir com `sample.test.js`
   e SEM `index.js`): `node --test sub/` tenta executar o diretorio como modulo e falha com
   0 testes reais rodados. O `index.js` funciona porque o require do diretorio resolve nele.
3. Omissao silenciosa impossivel: criei `test/js/zz_reviewer_probe.test.js` transitorio ->
   `node --test test/js/` passou a reportar **61** testes incluindo o probe (descoberta e
   dinamica via `readdirSync().filter(endsWith('.test.js'))`). Probe removido, arvore limpa.
4. (iii) `index.js` nao entra na cobertura: nenhum `SF:` dele no lcov (item (b)).

### (d) `sonar.exclusions="test/js/**"` (T-8) — CORRETO e necessario
- Esta dentro do bloco `args=(...)` do `begin` (`sonarqube.yml:108`), junto de
  `sonar.javascript.lcov.reportPaths` (linha 107).
- O glob cobre SOMENTE `test/js/**` — zero producao. O JS de producao vive em
  `src/TranslateReader/Resources/Raw/wwwroot/js/` e nao e alcancado.
- Sem a exclusao o denominador inflaria: o proprio mecanismo que faz o scanner .NET indexar
  o JS de `Resources/Raw` como fonte MAIN (arquivo fora de csproj — e o fato fundador do
  baseline desta phase, D-...-0) indexaria igualmente os ~1.244 novos LOC de `test/js/*` a 0%
  (o Node exclui test file do lcov por padrao, confirmado no relatorio gerado). A exclusao
  tambem impede o inverso (inflar o numerador com teste ~100%). Nao contradiz D-...-1.

### (e) "Sem issues nova" — nenhum sinal local de issue nova
- `dotnet build` Release: app 8 warnings (todos legados, `src/` intocado); **projeto de teste
  0 warnings** -> nenhum warning NOVO que o `external_roslyn` importaria.
- Greps dos padroes que ja morderam: `.First()`/`.Last()` em testes novos = 0 (CA1826);
  `Regex.Matches().Count` = 0 (CA1875); `Skip=` novo = 0 (xUnit1004); `Dispose()` das duas
  classes de teste descartaveis novas/alteradas chama `GC.SuppressFinalize` (CA1816);
  todos os testes novos amostrados tem assercao real (S2699 — ver (g)); JS novo usa
  `const`/`let`/`===` (0 hits de `var`/`==` em `test/js/`), mitigacao D-...-6 seguida.
- Limite estrutural inalterado (D-...-6): analisadores do SonarCloud so rodam pos-push;
  a confirmacao final permanece corretamente em `## Deferred to PR review`.

### (f) §6 — I/O real em teste novo, tudo com autorizacao nomeada
| Teste novo | I/O real | Autorizacao citada |
|---|---|---|
| `ModelAccessTests` (7 novos) | temp dir real; HTTP = `StubHttpMessageHandler` (URL `models.invalid`, **zero rede**) | D-2026-07-31-coverage-90-3 (citada no proprio PLAN T-5) |
| `ParsingEngineEdgeCaseTests` | EPUB sintetico em temp dir | D-...-3 + precedente `ParsingEngineTests.cs`, citados no header do arquivo (linhas 10-13) |
| `FileUtilityTests` (2 novos) | temp dir real | precedente D-2026-07-30-the-method-refactor-3 (PLAN T-2) |
| `SettingsAccessTests` (3 novos) | SQLite **in-memory** (`InMemoryDatabase`), sem disco | D-2026-07-30-regression-suite-3 (PLAN T-3) |
| `HtmlUtilityTests`, `test/js/*` | nenhum (harness le a PROPRIA fonte de producao, mecanismo exigido por D-...-2) | n/a |
Nenhum teste faz rede de verdade; nenhum I/O sem decisao nomeada.

### (g) Qualidade real — 6 mutacoes em producao, 6 mortas (16 falhas de teste)
Producao mutada transitoriamente, testes rodados, producao restaurada (`git status` limpo e
suites verdes re-confirmadas apos cada rodada):
| # | Mutacao (arquivo de producao) | Resultado |
|---|---|---|
| JS-1 | `paginated.js` `_applyLayout`: `colW = w - pad*2` -> `w - pad` | **1 falha** (`the layout uses the viewport width minus the horizontal padding`) |
| JS-2 | `bridge.js` `flushChunk`: buffer deixa de ser limpo | **3 falhas** (accumulate/forward, clears-on-throw, missing-target) |
| C#-1 | `ModelAccess.cs:50` `File.Move(..., overwrite: false)` | **1 falha** (`DownloadModelAsync_OverwritesAModelAlreadyOnDisk`) |
| C#-2 | `SettingsAccess.cs:49` fallback FontSize 18 -> 17 | **2 falhas** (ambos os testes de fallback) |
| C#-3 | `HtmlUtility.cs:102` `CanInjectIntoHead`: `\|\|` -> `&&` | **2 falhas** (InjectTags base-only e closing-head) |
| C#-4 | `ParsingEngine.cs:277` `NormalizePath`: `".."` -> `"..."` | **7 falhas** (NormalizePath, ResolvePath, Inlines/Rewrites/Svg) |
Zero teste "executa sem afirmar" encontrado; as assercoes pinam comportamento, nao so execucao.

### (h) Zero producao alterada, zero teste afrouxado — CONFIRMADO
- `git diff 1af3a51 HEAD --name-only -- src/` -> **vazio**.
- `git diff 1af3a51 HEAD --numstat -- test/` -> **0 linhas deletadas** em todos os 11 arquivos
  (5 novos, 4 so-adicao, `HtmlInjectionTests.cs` intocado como o PLAN T-2 exigia).
- Suite existente: 254 aprovados do baseline continuam presentes e aprovados (302 >= 254).

### (i) Achados de producao reportados e nao corrigidos — reais; registro parcial (WARN)
1. **`ExtractCoverImageAsync` devolve `byte[0]`**: CONFIRMADO no codigo —
   `FindCoverInManifest` (`ParsingEngine.cs:316`) retorna `imageFile?.Content` sem guarda
   `Length > 0`, ao contrario das duas fontes anteriores (`ParsingEngine.cs:72,75`), entao o
   placeholder vazio criado por `IgnoreMissingFileError=true` escapa como `byte[0]`.
   Comportamento fixado por teste (`ExtractCoverImageAsync_WithACoverImagePropertyPointingAtAMissingFile_ReturnsNoBytes`).
2. **Handle de zip aberto em `ReadEpubSafeAsync`**: PLAUSIVEL/consistente — quando a leitura
   estrita lanca `EpubPackageException` (`ParsingEngine.cs:164`), o descarte fica por conta do
   VersOne.Epub; a evidencia empirica e o guard `catch (IOException)` que o proprio teste
   precisou (`ParsingEngineEdgeCaseTests.cs:47-52`). Nao re-provei o lock em processo isolado
   (interno a lib de terceiro).
   **Lacuna**: nenhum dos dois esta em `.jdi/todos.md` — o SUMMARY diz "candidato a work item
   futuro" mas o registro nao foi feito (a secao `## De coverage-90` de todos.md tem apenas
   eslint e a contingencia de ParsingEngine). Ver Warnings. Corrigir aqui esta corretamente
   fora de escopo (`src/` off-limits por decisao da phase).

## Aritmetica de cobertura (conta propria, independente do SUMMARY)

Modelo: unidade = 1 linha coberta OU 1 condicao coberta (formato Sonar
`(lines_covered+conditions_covered)/(lines_to_cover+conditions_to_cover)`). C# do Cobertura
(coverlet) com dedup de linha por arquivo e exclusao de `obj/RegexGenerator.g.cs` (gerado, o
Sonar nao indexa); JS do lcov do `node:test`.

**Validacao do modelo no baseline (`main` @ `1af3a51`, worktree temporario, medido):**
- C# = 1099/1233 linhas + 237/332 condicoes = **1336/1565**
- - JS 0/195 (lines_to_cover remoto) = **1336/1760 = 75,91%** vs SonarCloud 1339/1764 =
  75,90% -> delta 0,01pp. Modelo fiel.
- Ancora extra: linhas C# cobertas local (1099) = exatamente o `covered` remoto de linha;
  denominador local de linhas C# (1233) = exatamente `lines_to_cover - JS` (1428-195).

**HEAD (`09563ad`, medido):**
- C# = 1173/1233 linhas + 305/332 condicoes = **1478/1565** (descoberto: `TranslationEngine`
  67 — deferido D-...-4; `ParsingEngine` 6; demais 14)
- JS (lcov) = 287/287 linhas, 98/98 branches — por arquivo: paginated 105/105+27/27,
  bridge 95/95+35/35, scroll 36/36+13/13, translation 51/51+23/23
- Projecao no modelo do doer (linhas JS no denominador remoto de 195, BRF do lcov como proxy
  das condicoes JS): `N = 1478 + 195 + 98 = 1771` · `D = 1565 + 195 + 98 = 1858` ->
  **1771/1858 = 95,32%** — bate com o SUMMARY com divergencia 0,00pp (<< 1pp).
- Sensibilidades: Sonar ignorando branch de JS -> 1673/1760 = **95,06%**; aplicando o delta
  remoto do baseline (+3/+4) -> 1774/1862 = **95,27%**; proxy 100% local (287 linhas JS no
  denominador) -> 1863/1950 = **95,54%**. Piso de 90% de 1858 = 1673 -> **margem +98 unidades**.
- Unico cenario reprovavel: lcov de JS nao consumido pelo Sonar -> 1478/1760 = **83,98%** —
  por isso T-8 (`lcov.reportPaths` + step de teste JS no CI) e load-bearing; o wiring esta
  correto (gate D acima), e a confirmacao final e pos-push (Deferred to PR review, D-...-5/-7).

## Blockers

Nenhum.

## Warnings

1. **Achados de producao sem work item** — `.jdi/todos.md` (secao `## De coverage-90`) nao
   registra os 2 achados do SUMMARY: handle de zip aberto no fallback de `ReadEpubSafeAsync`
   (`src/TranslateReader.Core/Business/Engines/ParsingEngine.cs:164-192`) e `byte[0]` em vez de
   `null` em `FindCoverInManifest` (`src/TranslateReader.Core/Business/Engines/ParsingEngine.cs:316`).
   Regra: `.claude/rules/csharp.md` §7 ("No TODO without a work item" — aqui o inverso: achado
   sem registro rastreavel fora do SUMMARY). Acao: adicionar os 2 itens a `todos.md` no fechamento
   da phase (1 linha cada); nao exige mexer em `src/`.
2. **Confirmacao remota pendente por design** — a meta ("90% no SonarQube" + "zero issues
   novas") so e provavel pos-push+CI (D-...-5/-6/-7; `## Deferred to PR review` do CONTEXT).
   Os pisos locais passaram com folga e o wiring esta correto, mas o numero remoto e o juiz
   final; quem faz o merge deve conferir o painel do SonarCloud no PR. Nao e defeito da
   execucao — e o limite estrutural ja decidido; registrado para nao virar PASS oco.
3. **`Assert.Empty(cover ?? [])`** (`test/TranslateReader.Tests/ParsingEngineEdgeCaseTests.cs:196`)
   aceita tanto `null` quanto `byte[0]` — assercao deliberadamente frouxa para nao fixar o
   defeito descrito no achado (i)(1) como contrato. Aceitavel enquanto caracterizacao, mas se o
   work item do warning 1 for corrigido um dia, este teste deve passar a exigir `null`.

## DoD Checklist (gate 8)

| # | Criterio | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | Harness JS existe p/ os 4 scripts e `node --test test/js/` passa | CONTEXT | Auto | PASS | exit 0 (60/60 pass) |
| 2 | Agregado lcov dos 4 JS >= 85% | CONTEXT | Auto | PASS | exit 0 — medido 287/287 = 100% |
| 3 | `ModelAccess.cs` >= 90% local | CONTEXT | Auto | PASS | exit 0 — `line-rate=1` (e `branch-rate=1`) |
| 4 | `FileUtility.cs` e `HtmlUtility.cs` = 100% | CONTEXT | Auto | PASS | exit 0 — ambos `line-rate=1` |
| 5 | CI wiring (setup-node + lcov + reportPaths) | CONTEXT | Auto | PASS | exit 0 — os 5 greps batem em `sonarqube.yml` |

**Totals:** 5 itens | Auto: 5 (5 PASS, 0 FAIL) | Manual: 0 pendentes
(PROJECT.md nao declara secao `## Definition of Done` — projeto adotado; apenas o DoD da phase se aplica.)

## Recommendation

Aprovar e seguir para `/jdi-ship coverage-90`. Antes do merge do PR: (1) registrar os 2 achados
de producao em `.jdi/todos.md` (warning 1 — 2 linhas, fora de `src/`); (2) apos o push, conferir
no SonarCloud a cobertura Overall (projecao propria: 95,3%, margem +98 unidades sobre o piso) e
a ausencia de issues novas (warning 2), que sao as duas condicoes do card e so existem remotas.
A qualidade dos testes foi provada por mutacao (6/6 mortas) — a cobertura entregue e real, nao
decorativa.

## DoD Critic (enhanced — forcado por /jdi-issue)

NOTA DE EXECUCAO: rodado inline pelo orquestrador (mesmo protocolo dos ciclos anteriores desta
sessao: contra-exemplo EXECUTADO, worktree restaurada e conferida depois de cada mutacao).

**Tres linhas ocas, todas com prova objetiva, todas da MESMA familia: o gate le um artefato de
medicao ANTIGO em vez de exigir que a medicao desta execucao tenha sucesso.**

- DoD row «Cobertura local agregada dos 4 scripts JS >= 85% via lcov»: **hollow=true,
  objective=true**. O comando e `node --test ... >/dev/null 2>&1; awk ... TestResults/js-lcov.info`
  — o `;` DESCARTA o exit code do runner e o `awk` le o arquivo que ja estava em disco.
  Contra-exemplo executado: acrescentei `throw new Error(...)` em `test/js/paginated.test.js`
  (harness quebrado, zero teste executado) e o `Verify:` continuou saindo **exit 0**, lendo o lcov
  da execucao anterior. Um harness quebrado, ou um `mkdir` que falhe, ou um reporter que nao escreva
  nada — todos passam enquanto sobrar um lcov velho no disco.

- DoD rows «`ModelAccess.cs` >= 90%» e «`FileUtility.cs`/`HtmlUtility.cs` = 100%»: **hollow=true,
  objective=true**, por DOIS motivos independentes.
  (1) Mesmo defeito do `;`: `dotnet test ... >/dev/null 2>&1; F=$(find ...)` ignora falha da suite.
  (2) Pior: a selecao do relatorio e `find TestResults -name "coverage.cobertura.xml" | sort |
  tail -1`. Os diretorios sao GUIDs do VSTest, entao `sort` e LEXICOGRAFICO, sem relacao nenhuma com
  o tempo. Medido agora neste repo, com 4 relatorios em disco: o comando escolheu
  `TestResults/9a248056-.../coverage.cobertura.xml` (mtime 07:49:59) enquanto o mais recente de fato
  era `TestResults/3e886ce2-.../coverage.cobertura.xml` (mtime 07:53:32). Ou seja, o gate afere um
  relatorio ARBITRARIO — pode aprovar cobertura que a execucao atual nunca produziu, e pode reprovar
  trabalho bom por ler um relatorio velho.

**Linhas com residuo declarado (nao ocas pela letra do criterio):**
- «Harness JS existe e todos os testes passam»: aceita suite VAZIA. Contra-exemplo executado:
  substitui `test/js/scroll.test.js` por um comentario e o `Verify:` saiu exit 0 (`node --test`
  aprova com 0 testes). Pela letra do criterio ("existe" + "passam") esta correto; quem prova a
  propriedade util e o item de cobertura — que por sua vez esta oco acima. O par nao sustenta a
  propriedade.
- «CI wiring»: greps de PRESENCA de string. Conferido manualmente que o caminho do
  `sonar.javascript.lcov.reportPaths=TestResults/js-lcov.info` (linha 107) casa com o destino do
  reporter (linha 137) e que o `setup-node` esta SHA-pinned — mas o comando nao PROVA essa
  correspondencia. E a mesma classe do defeito que quebrou a phase anterior
  (`sonar.qualitygate.wait` presente e invalido, verde local, exit 1 no runner).

Nada aqui questiona o TRABALHO: o reviewer reproduziu a aritmetica de forma independente (95,32%,
divergencia 0,00pp), a atribuicao de cobertura do JS aponta para os arquivos de producao e as 6
mutacoes amostradas morreram. O que nao presta e a PROVA: do jeito que os gates estao escritos, uma
regressao futura na medicao passa despercebida.

**Verdict:** BLOCKED
