# Phase 15: Cobertura de 90% no SonarQube sem issues novas — Plan  (slug: coverage-90)

## Goal
Levar a metrica `coverage` do SonarQube em `main` de 75,9% para >=90% escrevendo SO testes, sem
issue nova. Specialist unico em todas as tasks: `jdi-doer-translatereader` (single-stack, `**/*`).

## Correcao aritmetica (redimensiona D-...-5, que contou so linhas)
`coverage` do Sonar nao e cobertura de linha:
`(lines_covered + conditions_covered) / (lines_to_cover + conditions_to_cover)`.
Baseline `main`: linhas 1428/329 descobertas, condicoes 336/96 descobertas ->
(1099+240)/(1428+336) = **1339/1764 = 75,9%**. Meta: covered >= 0,90*1764 = 1588 ->
**faltam +249 unidades** (unidade = 1 linha OU 1 condicao), nao +187.

- Plano literal de D-...-5 (166+20+5 = 191): 1530/1764 = **86,7%** — nao fecha.
- Leitura generosa do mesmo plano (JS 166 + ModelAccess 35 + utils 30 = 231): **89,0%** — nao fecha.
- Plano abaixo SEM `ParsingEngine` (242): 1581/1764 = **89,6%** — nao fecha.
=> `ParsingEngine.cs` e task de primeira classe, nao "reserva de contingencia".

| Alvo | descob. (L/C) | meta do plano | ganho |
|---|---|---|---|
| 4 JS do WebView | 195 (195/0) | >=95% agregado (piso DoD: 85%) | +185 |
| `ParsingEngine.cs` | 88 (45/43) | fechar >=55 unidades | +55 |
| `ModelAccess.cs` | 39 (25/14) | >=95% linha (DoD: >=90%) | +35 |
| `SettingsAccess.cs` | 12 (0/12) | 100% (so condicoes) | +12 |
| `HtmlUtility.cs` | 7 (2/5) | 100% | +7 |
| `FileUtility.cs` | 3 (3/0) | 100% | +3 |
| `TranslationEngine.cs` | 67 (52/15) | DEFERIDO (D-...-4), nao reaberto | 0 |
| models + ThemeEngine + TranslationManager | ~14 | fora do plano | 0 |
| **soma** | **425** | | **+297** |

- Projecao A (lcov sem dados de branch): 1636/1764 = **92,7%**, margem **+48** sobre 1588.
- Projecao B (lcov com branch: +BRF~90 no denominador, cobertos a q=0,85):
  (1339+185+76+112)/(1764+90) = 1712/1854 = **92,4%**, margem **+44**.
  Custo liquido do branch JS = BRF*(0,9-q) ~ 5 unidades — segunda ordem; T-8 recalcula com
  `sum(BRF)`/`sum(BRH)` medidos no lcov real.

**Cenario declarado em que 90% NAO fecha:** JS parando no piso de 85% (+166) E `ParsingEngine`
rendendo <40 -> ~1667/1854 = **89,9%**. O unico estoque restante seria `TranslationEngine`
(67 unidades), locked como deferido. O doer NAO reabre D-...-4 e NAO mexe em `src/` para
manipular denominador: T-8 registra o numero e escala a decisao para o revisor humano no PR.

Nenhum arquivo de `src/` e tocado nesta phase — o denominador de C# nao muda. A unica mudanca de
denominador prevista e o BRF do lcov JS.

## Decisoes aplicadas
D-...-1 (rota A) · D-...-2 (layout `test/js/`, comando lcov, wiring, risco de atribuicao `vm`) ·
D-...-3 (ModelAccess, excecao de I/O) · D-...-4 (TranslationEngine deferido) · D-...-6 (sem gate
local de "issue nova"; teste novo usa indexador, `const/let/===`) · D-...-7 (Quality Gate nao prova
a meta) · D-6 · D-2 (256 testes = baseline intocavel).
Excecoes a `.claude/rules/csharp.md` §6, cada uma citada na task que a usa: T-2 (temp real,
D-2026-07-30-the-method-refactor-3), T-3 (SQLite in-memory, D-2026-07-30-regression-suite-3),
T-5 (temp real + handler fake, D-...-3), T-7 (fixture EPUB, D-...-3 + precedente
`ParsingEngineTests.cs`).

## Tasks
Valido para TODAS: baseline de 256 testes (254p/2s) nao regride, nenhum teste existente deletado ou
afrouxado, 1 task = 1 commit atomico com scope `coverage-90`.

### Wave 1 (paralelo)

#### T-1: harness JS + `paginated.js` (prova de atribuicao de cobertura)
- **Files:** `test/js/harness.js`, `test/js/paginated.test.js`
- **Acceptance:**
  - `TestResults/js-lcov.info` tem record `SF:` resolvendo em
    `src/TranslateReader/Resources/Raw/wwwroot/js/paginated.js` com `LH>0` — prova exigida por
    D-...-2 (`fs.readFileSync` + `new vm.Script(code,{filename:<caminho real>})`; codigo como
    string literal quebra a atribuicao EM SILENCIO). Fallback nomeado se a atribuicao falhar:
    `require()` do arquivo real com `globalThis.window` stub.
  - harness carrega N scripts no MESMO `vm.createContext` (sandbox com `window` === o proprio
    global) e da contexto novo por teste; `paginated.js` >=95% linhas no lcov.
  - `node --test test/js/` verde.
- **Dependencies:** none | **Test:** `test/js/paginated.test.js` | **Status:** completed

#### T-2: `FileUtility.cs` + `HtmlUtility.cs` a 100% (DoD 4)
- **Files:** `test/TranslateReader.Tests/FileUtilityTests.cs`,
  `test/TranslateReader.Tests/HtmlUtilityTests.cs` (novo)
- **Acceptance:**
  - `WriteFileAsync` coberto (as 3 linhas descobertas), temp dir real pelo precedente
    D-2026-07-30-the-method-refactor-3; ramos restantes de `HtmlUtility` fechados.
  - cobertura: `line-rate` = 1 E `branch-rate` = 1 nos dois arquivos (+10 unidades).
  - `HtmlInjectionTests.cs` nao e editado (esta sujo na worktree da phase anterior).
- **Dependencies:** none | **Test:** `FileUtilityTests`, `HtmlUtilityTests` | **Status:** completed

#### T-3: `SettingsAccess.cs` — 12 condicoes descobertas (0 linhas)
- **Files:** `test/TranslateReader.Tests/SettingsAccessTests.cs`
- **Acceptance:**
  - fetch com a tabela contendo SO uma chave desconhecida cobre os 11 fallbacks
    (`Enum.TryParse`/`double.TryParse` false + `??` de string); ctor `initializeOnStartup: false`
    coberto.
  - `branch-rate` de `SettingsAccess.cs` = 1 (`line-rate` ja e 1) — +12 unidades.
  - usa o `InMemoryDatabase` existente, sem disco (D-2026-07-30-regression-suite-3).
- **Dependencies:** none | **Test:** `SettingsAccessTests` | **Status:** completed

### Wave 2

#### T-4: `bridge.js`
- **Files:** `test/js/bridge.test.js`
- **Acceptance:**
  - `bridge.js` >=95% linhas no lcov; cobre as 4 deteccoes de host de `_sendReady` + retry via
    `setTimeout` stub + o `catch`, e os DOIS ramos de `document.readyState` (script recarregado em
    contexto novo), `flushChunk` com funcao ausente e com funcao que lanca.
  - `node --test test/js/` verde.
- **Dependencies:** T-1 | **Test:** `test/js/bridge.test.js` | **Status:** completed

#### T-5: `ModelAccess.cs` >= 90% (DoD 3)
- **Files:** `test/TranslateReader.Tests/ModelAccessTests.cs`
- **Acceptance:**
  - `DownloadModelAsync` com `HttpMessageHandler` fake, sem rede (D-...-3), cobrindo: sucesso com e
    sem `Content-Length`; `progress` null e nao-null (degrau de 0,5% e report final); status !=2xx
    (`EnsureSuccessStatusCode`); cancelamento (`OperationCanceledException` propaga, §1); swap
    `.tmp` -> final com overwrite.
  - cobertura de `ModelAccess.cs`: `line-rate` >=0,95 e `branch-rate` >=0,90 (+~35 unidades).
- **Dependencies:** none | **Test:** `ModelAccessTests` | **Status:** completed

### Wave 3

#### T-6: `scroll.js` + `translation.js` (fecha DoD 1 e DoD 2)
- **Files:** `test/js/scroll.test.js`, `test/js/translation.test.js`
- **Acceptance:**
  - os 4 arquivos `test/js/*.test.js` existem e `node --test test/js/` passa (DoD 1).
  - `translation.js` carregado no MESMO contexto de `paginated.js` (usa `_stepW`/`_currentPage`
    como variaveis livres); cada arquivo >=95% linhas.
  - agregado dos 4 scripts >=95% (piso do DoD 2 = 85%); SUMMARY.md registra LH/LF e
    `sum(BRF)`/`sum(BRH)` agregados — entrada da reconciliacao de T-8.
- **Dependencies:** T-1, T-4 | **Test:** `test/js/{scroll,translation}.test.js` | **Status:** completed

### Wave 4

#### T-7: `ParsingEngine.cs` — fechar >=55 das 88 unidades
- **Files:** `test/TranslateReader.Tests/ParsingEngineEdgeCaseTests.cs` (novo)
- **Acceptance:**
  - linhas com `hits="0"` caem de 45 para <=12 E condicoes descobertas de 43 para <=21.
  - alvos nomeados: fallback `catch (EpubPackageException)` de `ReadEpubSafeAsync` (~22 linhas);
    os 2 `throw` de `ExtractChapterContentAsync`; ramos de `InlineCssLinks` (sem rel / sem href /
    css ausente / css inlinado); `ReplaceImageRef` com `data:` e `http`; arms de `FindCss` e
    `FindImage`; `NormalizePath` com `..`, `.` e segmento vazio; `GetDirectoryPath` sem barra;
    os 3 fallbacks de `FindCoverInManifest`; `ResolveChapterTitle` sem navegacao.
  - preferir reflexao sobre estaticos privados (padrao ja usado em `ParsingEngineRegexTests.cs`,
    zero I/O); EPUB sintetico montado em temp SO onde a entrada publica exige arquivo (D-...-3 +
    precedente `ParsingEngineTests.cs`); nenhum binario novo commitado.
- **Dependencies:** T-5, T-6 (a medicao delas dimensiona o alvo real) | **Test:**
  `ParsingEngineEdgeCaseTests` | **Status:** completed

### Wave 5

#### T-8: wiring de CI + reconciliacao aritmetica (DoD 5)
- **Files:** `.github/workflows/sonarqube.yml`
- **Acceptance:**
  - `actions/setup-node` pinada por SHA (D-2026-07-28-ci-seguranca-4), `node-version: 24`, gated
    `if: env.SONAR_TOKEN != ''`, antes do `begin`; step de teste JS com o comando exato de D-...-2
    entre `begin` e `end`, mesmo gate; `/d:sonar.javascript.lcov.reportPaths=TestResults/js-lcov.info`
    dentro do `args=(...)` ja existente. O `Verify:` do DoD 5 passa.
  - `/d:sonar.exclusions="test/js/**"`: o scanner indexa arquivo fora de csproj como fonte MAIN
    (e por isso que o JS de `Resources/Raw` mede coverage sem o projeto MAUI ser buildado); sem a
    exclusao, ~300 linhas de teste JS entram no denominador a 0% (o Node exclui test file da
    cobertura por padrao) e a meta fica inalcancavel. Nao contradiz D-...-1, que rejeitou excluir o
    JS de PRODUCAO; e tambem impede o inverso (inflar a metrica com teste a ~100%).
  - reconciliacao no SUMMARY.md: `D_final = 1764 + sum(BRF)`, `N_final = 1339 + ganhos medidos`,
    veredito `N_final >= 0,9*D_final`. Projecao <90% => registra o numero e escala para o PR; nao
    reabre D-...-4, nao edita `src/`.
  - gating/hardening existentes intactos; nenhum step novo fora do `if: env.SONAR_TOKEN != ''`
    (o "Assert the scan is not silently skipped" ja cobre o no-op silencioso).
- **Dependencies:** T-1..T-7 | **Test:** `node --test test/js/` + grep do DoD 5 | **Status:** completed

## Riscos
- Proxy local != metrica remota (cobertura/lcov contam diferente do Sonar) — a margem de +44/+48
  unidades absorve divergencia; o numero real so existe pos-push (D-...-5, Deferred to PR review).
- Atribuicao `vm` silenciosa (D-...-2) — unica prova local e o `SF:`/`LH` de T-1.
- "Zero issue nova" nao tem gate local (D-...-6); mitigacao esta na escrita das tasks.

## Execution
- Total tasks: 8 | Waves: 5 | speedup paralelo estimado: 1,6x
- Ordem de risco crescente; T-1 vem primeiro por ser pre-requisito de infra dos outros 3 JS.
- `.github/workflows/sonarqube.yml` e ponto de serializacao — so T-8 o toca, na ultima wave.

## Files modified (todas as tasks)
- `test/js/harness.js`, `test/js/{paginated,bridge,scroll,translation}.test.js`
- `test/TranslateReader.Tests/{FileUtilityTests,HtmlUtilityTests,SettingsAccessTests,ModelAccessTests,ParsingEngineEdgeCaseTests}.cs`
- `.github/workflows/sonarqube.yml`
- Zero arquivos em `src/`.

## Test requirements
- C#: `dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --collect:"XPlat Code Coverage;Format=cobertura" --results-directory TestResults`
- JS: `node --test --experimental-test-coverage --test-reporter=lcov --test-reporter-destination=TestResults/js-lcov.info test/js/`
- Baseline: 256 testes (254 pass / 2 skip) nao regride. Gate de 90% (D-6) vale para codigo
  novo/alterado — nesta phase so ha codigo de teste.
