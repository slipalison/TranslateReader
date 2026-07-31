# Phase 15: Review (slug: coverage-90)

**Verdict:** APPROVED_WITH_WARNINGS

Review FINAL da phase (iter 3 — re-verify unica da rodada de warnings do `/jdi-issue`).
Diff revisado: `main` (`1af3a51`) ate `e48a412`, branch `jdi/coverage-90`, 22 commits.
Historico: iter 1 entregou as 8 tasks (derrubada pelo DoD critic — 3 gates ocos); iter 2
consertou os gates (D-2026-07-31-coverage-90-8) e convergiu APPROVED_WITH_WARNINGS com 3
warnings; iter 3 fechou W-1 e W-3 e manteve W-2 aberto por regra. **Nenhuma linha de producao
mudou em iter alguma** — `git diff 1af3a51..HEAD -- src/` e vazio.

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `net10.0-windows10.0.19041.0` Release: **0 Erro(s)**, 8 avisos legados (CS0618/CS0414) |
| Tests | PASS | C#: **304 (302 aprovados / 2 ignorados / 0 falhas)** — baselines 167 (D-2), 256 (inicio da phase) e 304 (iter 2) preservados. JS: **60/60 pass** via `node --test test/js/` |
| Coverage | PASS | 0 arquivos novos de `src/` pos-`4285f25` (phase so adiciona teste). Alvos medidos (cobertura real, `TestResults/review-iter3`): `ModelAccess` LR=1/BR=1, `SettingsAccess` 1/1, `FileUtility` 1/1, `HtmlUtility` 1/1, `ParsingEngine` LR=1/BR=0.969. JS lcov: **287/287 L, 98/98 BR em 4 arquivos**. Agregado coverlet (contexto, inclui `TranslationEngine` deferido): line 92,56%, branch 79,7% |
| Lint | WARN | `dotnet format --verify-no-changes` exit 2: **9 erros WHITESPACE em 4 arquivos LEGADOS** (`ThemeEngine.cs`, `ReaderPage.xaml.cs`, `ThemeEngineTests.cs`, `TranslationManagerTests.cs`) — nenhum no diff da phase. W-2 mantido aberto por regra (D-2 + specialist proibe refactor de legado por estilo); dono: phase `baseline-de-estilo` |
| Security/Layer | PASS | Greps 5.1/5.2/5.3/5.10/5.15(catch-vazio-novo)/5.17(mock de concreto): zero hits novos. Hits existentes (OCE engolida em PageModels/ReaderPage/TranslationManager, `_nativeLibraryConfigured`, subscribe=5/unsubscribe=4) sao baseline de `main`, identicos byte a byte (src intocado). I/O real nos testes novos coberto por excecao locked nomeada: `ModelAccessTests` (temp+handler fake, D-...-3), `SettingsAccessTests` (SQLite in-memory, D-2026-07-30-regression-suite-3), `FileUtilityTests` (temp real, D-2026-07-30-the-method-refactor-3) |
| Consistency | PASS | 22/22 commits Conventional com scope `coverage-90`, tipos adequados (test/docs/ci/chore — nada cegamente `feat`). Todos os `files_modified` do PLAN presentes no log; 8/8 tasks completed com teste. Unica delecao da branch inteira (3078+/1-): uma linha de nota reformatada em `.jdi/todos.md` — **zero teste deletado ou afrouxado** |
| UI Validation | SKIPPED | has_frontend=false (cliente MAUI nativo) — por design, nunca bloqueia |
| DoD | PASS | **5/5 auto PASS em DUAS rodadas consecutivas** (exit codes reais: `0 0 0 0 0` / `0 0 0 0 0`), 0 itens manuais |

## Blockers
- _(nenhum)_

## Warnings
- **W-2 (herdado, aberto por regra):** 9 erros WHITESPACE do `dotnet format` em 4 arquivos
  legados, nenhum tocado pela phase. D-2 isenta legado e o specialist proibe refatorar legado por
  estilo; conserto pertence a `baseline-de-estilo` (que shipa `.editorconfig` e vira o gate para
  BLOCK-on-new). Nao e acionavel nesta phase.
- **Nota (nao acionavel aqui):** 2 achados de PRODUCAO registrados em `.jdi/todos.md`
  § `De coverage-90` (zip handle aberto em `ReadEpubSafeAsync`; `byte[0]` em vez de `null` em
  `FindCoverInManifest`) — deliberadamente NAO corrigidos: `src/` fechado por escopo da phase.

W-1 (mutante sobrevivente) e W-3 (residuo do item 5) da iter 2: **FECHADOS e verificados por
evidencia propria** (secao abaixo).

## DoD Checklist (gate 8)

Comandos extraidos LITERALMENTE do CONTEXT.md vigente (`sed` sobre `**Verify:**`), executados do
repo limpo, exit code real capturado com `; echo $?` (nunca `&& echo`).

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | Harness JS existe p/ 4 scripts, todos os testes passam | CONTEXT (D-...-1/-2, sup. D-...-8) | Auto | PASS | exit 0 / 0 (tap: 60 pass, 0 fail; piso `p>1` nega suite vazia) |
| 2 | Cobertura agregada dos 4 JS >= 85% via lcov | CONTEXT (D-...-1/-5, sup. D-...-8) | Auto | PASS | exit 0 / 0 (medido: 287/287 = 100,00%, 4 SF distintos exigidos) |
| 3 | `ModelAccess.cs` >= 90% local | CONTEXT (D-...-3/-5, sup. D-...-8) | Auto | PASS | exit 0 / 0 (line-rate `1` nas 2 classes, piso 0.90 intacto) |
| 4 | `FileUtility.cs`/`HtmlUtility.cs` = 100% | CONTEXT (D-...-5, sup. D-...-8) | Auto | PASS | exit 0 / 0 (line-rate `1` em todas as classes, piso 0.99 intacto) |
| 5 | CI wiring lcov (`setup-node` SHA + comando + `reportPaths`) | CONTEXT (D-...-2, sup. D-...-8 e D-...-9) | Auto | PASS | exit 0 / 0 (P=D=`TestResults/js-lcov.info` pinado; SHA 40-hex em `sonarqube.yml:76`) |

**Totals:** 5 items | Auto: 5 (5 PASS, 0 FAIL) | Manual: 0 pending

## Verificacao cetica (evidencia propria desta review)

**a) O mutante de W-1 morre agora — PROVADO.** Apliquei `||` -> `&&` em `ModelAccess.cs:41`,
rodei `ModelAccessTests`: **1 falha / 14 aprovados** — `Assert.InRange() Failure ... Range:
(100 - 333) / Actual: 1` (o "so o report final" exato que o mutante produz). Producao restaurada,
`git status --porcelain` limpo. **Assercao estritamente mais forte, sem teto afrouxado:** o teto
antigo `Assert.True(reports.Count < 334)` sobre int equivale a `<= 333`; `Assert.InRange(x, 100,
333)` e inclusivo nas duas pontas — teto LITERALMENTE identico, piso 100 e novo (e folgado:
esperado ~200 reports com step de 0,5% sobre 334 reads de 0,3% — sem flake).

**b) Item 5 sem regressao e sem furo — PROVADO com o comando ANTIGO (D-...-8) vs NOVO (D-...-9)
sobre copias mutadas do YAML:**

| Cenario | ANTIGO (D-8) | NOVO (D-9) |
|---|---|---|
| repo real | 0 | 0 (zero falso positivo) |
| rename COORDENADO dos 2 lados (`:107` + `:137`) | **0 (o furo W-3)** | **1 (fechado)** |
| divergencia unilateral (so `:107`) | 1 | 1 (preservado) |
| `setup-node` por tag em vez de SHA | 1 | 1 (preservado) |

O NOVO = ANTIGO + `&& test "$P" = "TestResults/js-lcov.info"` — puramente aditivo, conferido por
diff textual dos dois comandos.

**c) 5 `Verify:` no repo limpo, 2 rodadas, exit real:** `0 0 0 0 0` / `0 0 0 0 0`.

**d) Nenhum piso afrouxado nas 3 iters:** diff do texto integral das linhas de criterio `- [ ]`
entre o CONTEXT original (`055e7db`) e o vigente = **vazio (identicos)**. Pisos nos `Verify:`:
`>=85` (item 2), `0.90` (item 3), `0.99` x2 (item 4) — mesmos valores do original; as ocorrencias
extras desses numeros no arquivo atual estao so nas anotacoes `Source:` ("piso inalterado").

**e) Caminho JDI-legal:** `git diff 1af3a51..HEAD -- .jdi/DECISIONS.md | grep -c '^-[^-]'` = **0**
(append puro na phase inteira). Ordem iter 3: `ad9c30c` (teste) -> `a24f084` (**so**
`.jdi/DECISIONS.md`, +25/-0, registra D-...-9) -> `e3c01dc` (**so** CONTEXT.md, item 5) ->
`e48a412` (so SUMMARY.md). Decisao commitada ANTES da edicao do CONTEXT.

**f) Iter 3 fora do W-1:** `git diff a56b668 HEAD --stat -- src/ test/` = **so**
`test/TranslateReader.Tests/ModelAccessTests.cs | 3 (+2/-1)`. Producao e demais testes intocados.

**g) Aritmetica** — secao propria abaixo.

**h) Amostra de mutacao final — 3 mutantes, 3 MORTOS.** Iters 2/3 tocaram UMA linha de teste
(`ModelAccessTests.cs:157-158`), entao a amostra e reduzida por regra do dispatch; os 6 mutantes
de producao da iter 1 ja tinham morrido na review anterior. Nesta review: (1) C#
`ModelAccess.cs:41` `||`->`&&` — morto pelo teste novo (item a); (2) JS `scroll.js:11`
`&&`->`||` — `node --test` **exit 1**; (3) C# `ParsingEngine.NormalizePath` deixa de tratar
`..` — **9 falhas** em 76 testes de ParsingEngine (os edge cases de T-7 pegam). Producao
restaurada apos cada um; `git status --porcelain` limpo.

**i) "Sem issues novas" — varredura local dos padroes ja mordidos pelo Sonar neste repo**, sobre
os 5 arquivos de teste C# tocados + 4 suites JS: `.First()` em colecao indexavel (CA1826) =
**0**; `Regex.Matches().Count` (CA1875) = **0**; `Dispose()` sem `GC.SuppressFinalize` (CA1816) =
**0** (as 4 classes IDisposable novas chamam `GC.SuppressFinalize(this)`; helpers `sealed`);
teste sem assercao (S2699, heuristica awk) = **0**; `[Fact(Skip=)]` novo (xUnit1004) = **0**;
complexidade >15 (S3776) = nada proximo por inspecao. Unica observacao: literais de fixture EPUB
repetidos 3+ vezes em `ParsingEngineEdgeCaseTests.cs` (` href=`, ` media-type=`, etc.) — S1192
nao roda em test sources no profile C# do Sonar, e `test/js/**` esta excluido do scan
(`sonar.exclusions`, `sonarqube.yml:108`); risco residual baixo, sem acao local possivel (D-...-6).

## Aritmetica de cobertura

Modelo Sonar: `coverage = (lines_covered + conditions_covered) / (lines_to_cover +
conditions_to_cover)`; unidade = 1 linha OU 1 condicao. Refiz a conta com os artefatos de medicao
DESTA review (`TestResults/review-iter3/.../coverage.cobertura.xml` + `TestResults/js-lcov.info`):

- Baseline `main` remoto (D-...-0): 1339/1764 = 75,90%. Proxy local do doer: 1336/1760 = 75,91%
  (delta 0,01pp — proxy validado).
- Ganhos medidos agora: JS **+195 L** (lcov 287/287 = 100%) **+98 C** (BRH=BRF=98, que tambem
  entram no denominador); `ParsingEngine` +81 (residuo ~6 unidades — LR=1, BR=0.969);
  `ModelAccess` +39 (1/1); `SettingsAccess` +12 (BR=1); `HtmlUtility` +7 (1/1); `FileUtility` +3
  (1/1). Soma: **+435**.
- Ancorado no proxy local: (1336+435)/(1760+98) = **1771/1858 = 95,32%** — piso 90% = 1673
  unidades -> **margem +98 unidades (+5,32pp)**. Identico a conta do doer, divergencia 0,00pp.
- Ancorado no remoto: (1339+435)/(1764+98) = 1774/1862 = **95,27%**, margem +98 sobre 1676.
- Cenario unico que reprova, reproduzido na conta: lcov do JS nao consumido pelo Sonar ->
  1478/1760 = **83,98%**. E por isso que o item 5 do DoD (wiring) e load-bearing e foi endurecido
  duas vezes (D-...-8, D-...-9).
- Contexto: agregado coverlet bruto do Core = 92,56% L / 79,7% BR — unidade de contagem diferente
  da do Sonar (inclui `TranslationEngine` deferido e conta linhas que o Sonar nao conta); nao e o
  gate, so contexto.

O numero e **PROJECAO local**: o juiz final e o painel do SonarCloud pos-push (D-...-5/-6/-7,
`## Deferred to PR review` do CONTEXT).

## Estado final da phase

**Adicionado (3078 insercoes, 1 delecao — uma nota em `.jdi/todos.md`):**
- **Testes C#:** +48 (256 -> 304): `ParsingEngineEdgeCaseTests.cs` (novo, 451 linhas),
  `HtmlUtilityTests.cs` (novo, 90), extensoes em `ModelAccessTests.cs` (+199),
  `SettingsAccessTests.cs` (+64), `FileUtilityTests.cs` (+23).
- **Testes JS:** 60 (primeira suite JS do repo): `test/js/harness.js` (DOM falso + `node:vm` com
  `filename` real — atribuicao de cobertura provada pelos 4 `SF:` resolvendo em
  `src/.../wwwroot/js/*.js`), `index.js` (agregador), 4 suites `*.test.js`. **Zero dependencia
  nova** — sem `package.json`, roda com `node --test` nativo (Node >= 20; CI pina 24).
- **Wiring de CI** (`.github/workflows/sonarqube.yml`, +23): `actions/setup-node` SHA-pinned
  (`:76`), step de teste JS com lcov (`:137`), `sonar.javascript.lcov.reportPaths` (`:107`),
  `sonar.exclusions="test/js/**"` (`:108`, exclui TESTE do denominador, nunca producao).
- **Processo:** D-...-0 a D-...-9 em DECISIONS.md (append-only), CONTEXT/PLAN/SUMMARY/todos.

**O que NAO mudou:** `src/` — **zero linhas** em 22 commits (denominador C# do Sonar intacto);
nenhum teste pre-existente deletado ou afrouxado; `TranslationEngine` segue deferido (D-...-4).

**Numeros finais:** build 0 erros; 304 C# (302p/2s) + 60 JS; JS 287/287 L e 98/98 BR; alvos C#
da phase todos em line/branch-rate 1 (ParsingEngine BR 0.969); projecao Sonar **~95,3%**,
margem **+98 unidades** sobre o piso de 90%; DoD 5/5 x 2 rodadas; 9 mutantes provados mortos no
total da phase (6 iter 1 + 3 nesta review).

## Para o revisor humano do PR

1. **O 90% e PROJECAO local (~95,3%, margem +5,3pp).** O juiz final e o painel do SonarCloud no
   PR — cheque la a métrica **Overall** coverage (o Quality Gate padrao mede so New Code e NAO
   prova a meta, D-...-7). Se o painel mostrar ~84%, o lcov do JS nao foi consumido — olhe o step
   de teste JS e o `reportPaths` no `sonarqube.yml`.
2. **"Sem issues novas" so o scan remoto confirma** (analisadores do SonarCloud nao rodam local —
   D-...-6). Sinal local: varredura limpa dos padroes ja mordidos neste repo (item i acima).
3. **2 achados de producao ficaram REGISTRADOS e nao corrigidos por decisao** (escopo da phase
   fecha `src/`): zip handle aberto em `ReadEpubSafeAsync` e `byte[0]` em vez de `null` em
   `FindCoverInManifest` — file:line em `.jdi/todos.md` § `De coverage-90`.
4. **O harness JS e novo no repo** — primeira suite JS, roda via `node --test` nativo, **sem
   dependencia npm** (sem `package.json`). Os 4 scripts de producao do WebView NAO mudaram
   (diff de `src/` vazio); a unica prova de comportamento e o proprio harness, entao qualquer
   estranheza no reader em runtime merece olhar humano (sem E2E de WebView no repo).

## Recommendation

Phase pronta para `/jdi-ship`. W-2 fica para `baseline-de-estilo` (dono ja nomeado); os 2 achados
de producao de `.jdi/todos.md` devem virar item de phase futura (o zip handle toca o mesmo
arquivo da phase `epub-zip-slip`, ja pendente). Pos-push, confirmar no SonarCloud: Overall >= 90%
e zero issues novas — os dois unicos criterios do card que so existem remotamente.

## DoD Critic (enhanced — forcado por /jdi-issue, passe final antes do ship)

NOTA DE EXECUCAO: rodado inline pelo orquestrador, com captura do exit code REAL (a lição da iter 1:
extrair o `Verify:` junto com o sufixo `|| echo` faz o `bash -c` sempre retornar 0 e invalida a
prova). Comandos extraidos por parser do `CONTEXT.md` vigente; repo restaurado e conferido apos cada
mutacao.

Foco: o item 5, unico que mudou desde a passagem anterior (`D-2026-07-31-coverage-90-9`). Os itens
1-4 seguem com o julgamento da iter 2, onde foram atacados com runner ausente, suite quebrada, piso
elevado e artefato velho em disco.

| Cenario | Resultado |
|---|---|
| 5 itens no repo real, exit code real | **0 0 0 0 0** |
| item 5 com rename COORDENADO dos dois lados do YAML (o furo que W-3 apontou) | **exit 1 — fechado** |
| pisos ao longo das 3 iteracoes (85% JS, 0.90 ModelAccess, 0.99 FileUtility/HtmlUtility) | intactos |

O furo do W-3 era real e e o mesmo padrao que derrubou a phase anterior no `sonar.qualitygate.wait`:
o gate provava CORRESPONDENCIA entre duas strings do YAML, mas nao ancorava o valor esperado — as
duas podiam derivar juntas para um caminho que o scanner nao le. Agora o literal esta pinado.

Nenhuma linha `Type=Auto`/`PASS` mostrou-se oca nesta passagem.

**Verdict:** APPROVED
