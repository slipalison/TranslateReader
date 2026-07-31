# Phase 15: Review  (slug: coverage-90) — iter 2, review FINAL da phase

**Verdict:** APPROVED_WITH_WARNINGS

Review regenerada do zero (a REVIEW da iter 1 foi deletada de proposito). Contexto: a iter 1
entregou as 8 tasks e foi derrubada pelo DoD critic por 3 gates ocos + 2 residuos — todos da mesma
familia: o `Verify:` lia artefato de medicao JA EM DISCO em vez de exigir sucesso da execucao
atual. A iter 2 alterou SOMENTE `.jdi/` (D-2026-07-31-coverage-90-8, append-only): zero linha de
`src/`, zero linha de `test/`. Esta review re-executou tudo com evidencia propria — nenhum numero
abaixo e auto-reportado pelo doer.

Diff revisado: `main` (`1af3a51`) ate `b52eca1`, branch `jdi/coverage-90`, 16 commits.

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `TranslateReader.csproj` Release `net10.0-windows10.0.19041.0`: **0 Erro(s)** (40 avisos `MVVMTK0045`, todos legados — `src/` intocado na phase inteira) |
| Tests | PASS | C#: **304 testes = 302 aprovados / 2 ignorados / 0 falhas** (baseline D-2 = 167; baseline da phase anterior = 256/254p — nada regrediu). JS: **60/60 pass** (`node --test`, tap) |
| Coverage | PASS | 0 arquivos novos de `src/*.cs` desde `4285f25` → gate formal por-arquivo-novo sem objeto; os pisos do DoD medidos por mim: JS **287/287 L (100%)** e 98/98 BR nos 4 scripts; `ModelAccess.cs` line-rate `1`/`1` (2 classes); `FileUtility.cs` `1`/`1`/`1` (3 classes); `HtmlUtility.cs` `1` |
| Lint | WARN | `dotnet format --verify-no-changes`: 9 erros WHITESPACE em **4 arquivos legados** (`ThemeEngine.cs`, `ReaderPage.xaml.cs`, `ThemeEngineTests.cs`, `TranslationManagerTests.cs`) — nenhum tocado pela phase; D-2 isenta (WARN ate `baseline-de-estilo`) |
| Security/Layer | PASS | 5.1/5.2/5.10-sync/5.17-mock-concreto = **0 hits**; 5.12 = so o static legado conhecido (`TranslationEngine.cs:16`); 5.15/OCE = so os hits legados ja inventariados em `.jdi/todos.md` (W-5 de `sonar-zero-issues`); `TranslationManager.cs:61` faz `throw;` (conforme §1). I/O real nos testes novos (`FileUtilityTests`, `ModelAccessTests`, `SettingsAccessTests`) coberto pelas excecoes locked nomeadas no PLAN (D-2026-07-30-the-method-refactor-3, D-2026-07-31-coverage-90-3, D-2026-07-30-regression-suite-3) |
| Consistency | PASS | 8 tasks = 8 commits atomicos 1:1 com os `files_modified` do PLAN; Conventional Commits scope `coverage-90`, tipos `test`/`ci`/`docs` apropriados; desvio `test/js/index.js` documentado no SUMMARY (Node >= 24 trata o positional de `--test` como glob) |
| UI Validation | SKIPPED | has_frontend=false (cliente MAUI nativo) — por design, nunca bloqueia |
| DoD | PASS | **5/5 auto PASS** (2 rodadas completas), 0 manual |

## Verificacao adversarial dos gates (o centro desta iter)

Os 5 `Verify:` foram extraidos **literalmente** do CONTEXT.md vigente (`sed` sobre o arquivo, nao
transcricao) e os antigos extraidos de `git show a81ed0b:...CONTEXT.md`. Matriz executada por mim,
exit codes reais:

| # | Cenario | ANTIGO | NOVO | Veredicto |
|---|---|---|---|---|
| A | `node` fora do PATH (`env PATH=/usr/bin:/bin`) + `js-lcov.info` valido de 5399 bytes em disco | item 2 = **0** (oco) | item 2 = **127** | furo fechado; o `rm -f` ainda destruiu o artefato velho — dupla protecao |
| B | 4 `.test.js` esvaziados | item 1 = **0** (oco) | item 1 = **1**, item 2 = **1** | Node reporta exatamente `# tests 1 / # pass 1 / # fail 0` (o agregador `index.js` conta como 1) — confirmei a alegacao do doer; `p>1` e o menor piso que discrimina |
| C | so `scroll.test.js` esvaziado | item 2 = **0** (lcov com **3** `SF:`, agregado dos 3 >= 85%) | item 2 = **1** (`n==4`) | defeito extra (e)(ii) confirmado e fechado |
| D | `throw` no topo de `paginated.test.js` | item 2 = **1** | itens 1 e 2 = **1** | ver "Julgamento da contestacao" abaixo |
| E | suite C# REPROVANDO (`FileUtilityTests.cs:81` `".epub"` -> `".MUTANT"`) | item 3 = **0** (oco — 10 relatorios velhos em disco nos GUIDs do `TestResults/` raiz) | itens 3 e 4 = **1** | furo central fechado; o blocker do critic era procedente |
| F | pisos elevados: item 2 `>=101`; item 1 `p>60`; item 3 apontado para `TranslationEngine.cs` (rates reais 0.2105/0.4/0.2) piso 0.90; item 4 piso 1.01 | — | **1 / 1 / 1 / 1** | os 4 gates sabem REPROVAR — a comparacao esta viva |
| G | `reportPaths` divergindo do destino do reporter (copia mutada do YAML); `setup-node@v7` sem SHA | item 5 = 0 (so grep de presenca) | item 5 = **1 / 1** | endurecimento real |
| H | repo limpo, 2 rodadas consecutivas dos 5 itens | — | **0,0,0,0,0** e **0,0,0,0,0** | zero falso positivo; `rm -rf`/`rm -f` nao quebram a 2a execucao |

Higiene: `git status --porcelain` voltou limpo apos CADA mutacao (verifiquei apos cada restore);
`TestResults/` inteiro (incl. `dod3`/`dod4`) e gitignored (`.gitignore:18` `**/TestResults/`) e
nao entra no denominador do Sonar — o scanner respeita exclusoes de SCM por padrao e, no CI, esses
diretorios nem existem (o job so cria `TestResults/` raiz + `js-lcov.info`).

### Julgamento da contestacao do doer (item c do dispatch)

**A contestacao procede.** Reproduzi o contra-exemplo literal do critic (`throw` num `.test.js`):
o reporter lcov do Node **trunca o destino para um stub de 4 bytes (`TN:`)** — medido por mim:
`wc -c` = 4, conteudo `TN:` — e o `awk` antigo saia exit 1 por `L==0`. Ou seja, o comando antigo
falhava ali por **acidente** (o reporter destruia o proprio artefato velho), nao por design.
**Isso NAO invalida o blocker**: o defeito estrutural ("le artefato velho em vez de exigir a
execucao atual") e real e esta provado pelos cenarios A (runner ausente -> antigo exit 0) e
C (suite parcial -> antigo exit 0), ambos executados por mim. O critic acertou a classe do defeito
e errou o exemplar; o doer foi honesto em registrar a distincao em vez de fingir reproducao.

### Endurecimentos por conta propria do doer (item d)

- **Item 1 `# pass > 1`:** honesto e correto. Confirmei empiricamente que a suite VAZIA reporta
  `# pass 1` (nao 0) — logo `> 0` seria vacuo e `> 1` e o piso minimo discriminante. NAO e ratchet
  (nao codifica os 60 desta phase); o ratchet numerico esta corretamente deferido para a virada da
  phase, coerente com o item `[PROCESSO/DoD]` pre-existente de `.jdi/todos.md`.
- **Item 5 `P = D`:** honesto, sem falso positivo (2 rodadas limpas) e estritamente mais forte em
  semantica que o par de greps de presenca (que aceitava `reportPaths` errado com a string certa
  num comentario). Residuo menor anotado em Warnings (W-3).

### Dois defeitos extras achados pelo doer (item e)

- **(i) `awk -v r="<multilinha>"` degradava para comparacao de STRING — confirmado.**
  `ModelAccess.cs` tem **2** elementos `<class>` no cobertura (classe + state machine de
  `DownloadModelAsync`), `FileUtility.cs` tem **3** — medi no relatorio real. Pior: executei o awk
  antigo com `r="1\n0.5"` (uma classe a **50%**) e ele saiu **exit 0** — na comparacao
  lexicografica só a primeira linha decide. O novo compara `$1+0` por classe, exige `n>0` matches
  e reprova se QUALQUER classe ficar abaixo do piso (provado no cenario F contra
  `TranslationEngine`). Correcao numerica de verdade.
- **(ii) agregado JS satisfeito por 3 dos 4 — confirmado** (cenario C: lcov com 3 `SF:`, antigo
  exit 0, novo exit 1 via `seen[f]`/`n==4`).

### Nenhum piso foi afrouxado (item f)

Comparei os comandos antigo/novo lado a lado (diff `a81ed0b..b52eca1` do CONTEXT.md):
JS agregado `>=85` -> `>=85`; `ModelAccess` `>=0.90` -> `<0.90` reprovando (mesmo piso, agora por
classe — mais estrito); `FileUtility`/`HtmlUtility` `>=0.99` -> `<0.99` por classe (idem); item 1
sem piso -> `p>1 && f==0` (mais estrito); item 5 presenca -> correspondencia + SHA-pin (mais
estrito). **Nenhum afrouxamento; todo delta e no sentido mais duro.**

### Caminho JDI-legal (item g)

- `.jdi/DECISIONS.md`: `git diff 1af3a51..HEAD -- .jdi/DECISIONS.md | grep -c '^-[^-]'` = **0** —
  175 insercoes, 0 remocoes; decisoes anteriores intactas; D-...-8 e append puro no fim.
- Ordem correta: `5d4209c` (registra D-...-8) **antes** de `3caf98a` (edita CONTEXT.md).
- CONTEXT.md: no diff da iter 2 mudaram SOMENTE as linhas `**Verify:**` e as anotacoes de
  `**Source:**` (registrando a supersessao). O texto dos criterios (`- [ ] ...`) e os pisos estao
  **byte-identicos** — conferido no diff bruto.

## Aritmetica de cobertura (conta minha, modelo Sonar: linhas + condicoes)

Ancora = numeros REMOTOS do SonarCloud em `main` (D-...-0): linhas 1099/1428 + condicoes 240/336 =
**1339/1764 = 75,90%**. `src/` intocado na phase -> o denominador C# nao muda.

Deltas que EU medi nos artefatos gerados nesta review:

| Delta | Medida minha | Efeito |
|---|---|---|
| JS lcov: 4 `SF:` distintos resolvendo em `src\...\wwwroot\js\`, **LH=LF=287, BRH=BRF=98** | 100% linha e branch | +195 no numerador (as 195 JS lines do baseline); +98 nos DOIS lados (condicoes JS novas no modelo) |
| `ModelAccess`/`SettingsAccess`/`FileUtility`/`HtmlUtility` 100% linha+branch (todas as classes) | cobertura plena | +39+12+3+7 = +61 |
| `ParsingEngine`: resta **1 linha (a 76) + 10 condicoes** (coverlet) das 88 unidades baseline | ganho +77 a +82 (modelos coverlet/Sonar divergem nas condicoes) | +81 (central) |
| `TranslationEngine` intocado (52L+30C descobertos), deferido D-...-4 | 0 | 0 |

**Projecao central: N = 1339+195+98+61+81 = 1774; D = 1764+98 = 1862 -> `1774/1862 = 95,27%`.**
Piso de 90% = 1676 unidades -> **margem +98**. Pessimista (ParsingEngine +77): 1770/1862 =
**95,06%**, margem +94. Numero do doer (proxy local 1336/1760): 95,32% — divergencia de 0,05pp da
minha ancora remota, mesma margem. Sensibilidade: sem import de branch do lcov JS: 1676/1764 =
95,01% — ainda passa. **Unico cenario que reprova: o lcov JS nao ser consumido pelo Sonar**
((1339+142)/1764 = **83,96%**) — e exatamente o cenario que o item 5 endurecido (P=D + SHA-pin +
flags) desarma localmente. T-8 e load-bearing e esta protegido.

## Amostragem por mutacao (item h)

A iter 2 nao tocou NENHUM teste (`git diff a81ed0b..b52eca1 -- src/ test/` vazio), entao vale a
amostra reduzida prevista no dispatch; ainda assim re-amostrei 4 mutacoes de PRODUCAO (restauradas
uma a uma, `git status` limpo apos cada):

| Mutacao (producao) | Teste esperado | Resultado |
|---|---|---|
| `bridge.js` `flushChunk` entrega `""` em vez do buffer | `bridge.test.js` | **MORTA** — `not ok 11 - appendChunk accumulates and flushChunk forwards the whole buffer` (59p/1f) |
| `ModelAccess.cs` `File.Move(..., overwrite: false)` | `ModelAccessTests` | **MORTA** — 14p/1f |
| `ParsingEngine.cs:304` fallback de capa por id `"cover"` -> `"coverX"` | `ParsingEngineEdgeCaseTests` | **MORTA** — 27p/1f |
| `ModelAccess.cs:41` degrau de progresso `\|\|` -> `&&` (so reporta o 1.0 final) | `ModelAccessTests` | **SOBREVIVEU** — 15/15 verdes (ver W-1) |

## Blockers

_(nenhum)_

## Warnings

- **W-1 (teste novo, qualidade de assercao):**
  `test/TranslateReader.Tests/ModelAccessTests.cs:146-160`
  (`DownloadModelAsync_ReportsHalfPercentStepsAndAlwaysTheFinalCompletion`) asserta teto
  (`reports.Count < 334`), monotonicidade e o `1.0` final, mas **nenhum piso de contagem** — o
  mutante que suprime TODOS os reports intermediarios (`ModelAccess.cs:41`, `||`->`&&`) passa
  15/15. O acceptance da T-5 nomeava "degrau de 0,5%" e essa metade nao e discriminada. Cobertura
  (o gate desta phase) esta integra; e defeito de forca de assercao, mesma classe da assercao
  frouxa ja registrada em `.jdi/todos.md` (`Assert.Empty(cover ?? [])`). Fix de 1 linha quando o
  arquivo for reaberto: ex. `Assert.InRange(reports.Count, 100, 333)` para payload de 100k/chunk
  300. Nao bloqueia: DoD nao exige adequacao mutacional e o piso de 90% nao depende disso.
- **W-2 (lint, legado):** 9 erros WHITESPACE em 4 arquivos nao tocados pela phase — D-2 isenta;
  vira BLOCK-on-new quando `baseline-de-estilo` shipar `.editorconfig`.
- **W-3 (residuo menor do item 5, registrado para a proxima phase que tocar o YAML):** `P = D`
  prova correspondencia mas nao pina o literal `TestResults/js-lcov.info`; uma mudanca COORDENADA
  dos dois lados no CI passaria o item 5 enquanto o item 2 continua medindo o caminho hardcoded
  local. Nao e gate oco (o criterio — "o Sonar le o que o reporter escreve" — segue provado; a
  cobertura continuaria fluindo no CI); e so a nota de que os itens 2 e 5 deixariam de aferir a
  mesma string. Endurecimento candidato: exigir tambem `P = "TestResults/js-lcov.info"`.

## DoD Checklist (gate 8)

Comandos extraidos literalmente do CONTEXT.md vigente; executados 2x cada no repo limpo.

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | Harness JS existe p/ os 4 scripts e todos os testes passam | CONTEXT (D-...-1/-2, superseded D-...-8) | Auto | PASS | exit 0, 0 (2 rodadas); `# tests 60 / # pass 60 / # fail 0`; reprova suite vazia (p=1) e piso p>60 |
| 2 | Cobertura agregada dos 4 JS >= 85% via lcov | CONTEXT (D-...-1/-5, superseded D-...-8) | Auto | PASS | exit 0, 0; medido 287/287 = **100,00%** sobre 4 `SF:` distintos; reprova runner ausente (127), 3/4 arquivos, piso 101 |
| 3 | `ModelAccess.cs` >= 90% local, handler fake sem rede | CONTEXT (D-...-3/-5, superseded D-...-8) | Auto | PASS | exit 0, 0; line-rate `1` nas 2 classes; reprova suite falhando (exit 1) e piso deslocado |
| 4 | `FileUtility.cs` e `HtmlUtility.cs` = 100% local | CONTEXT (D-...-5, superseded D-...-8) | Auto | PASS | exit 0, 0; line-rate `1 1 1` + `1`; reprova suite falhando e piso 1.01 |
| 5 | CI wiring: setup-node SHA-pinned, step lcov, `reportPaths` = destino do reporter | CONTEXT (D-...-2, superseded D-...-8) | Auto | PASS | exit 0, 0; `P = D = TestResults/js-lcov.info` (`sonarqube.yml:107` = `:137`); reprova divergencia e pin por tag |

**Totals:** 5 items | Auto: 5 (5 PASS, 0 FAIL) | Manual: 0 pending

## Estado final da phase

**Adicionado** (16 commits, 8 de task):
- Testes JS: harness `node:vm` com atribuicao ao arquivo real de producao (`vm.Script(code,
  {filename})` — os 4 `SF:` do lcov resolvem em `src/.../wwwroot/js/*.js`), agregador
  `test/js/index.js` (desvio documentado: Node >= 24 trata o positional de `--test` como glob),
  4 suites = **60 testes**, 287/287 L e 98/98 BR.
- Testes C#: `HtmlUtilityTests.cs` e `ParsingEngineEdgeCaseTests.cs` novos;
  `FileUtilityTests`/`SettingsAccessTests`/`ModelAccessTests` ampliados. Suite: 256 -> **304**
  (302p/2s), zero teste removido ou afrouxado.
- CI: `sonarqube.yml` ganha `actions/setup-node` SHA-pinned (gated em `SONAR_TOKEN`), step de
  lcov JS, `sonar.javascript.lcov.reportPaths=TestResults/js-lcov.info`,
  `sonar.exclusions=test/js/**` (exclui TESTE, nao producao — nao contradiz D-...-1).
- Processo (iter 2): D-2026-07-31-coverage-90-8 + os 5 `Verify:` reescritos para medir a execucao
  ATUAL; 2 achados de producao registrados em `.jdi/todos.md` com file:line e evidencia.

**NAO mudou:** nenhuma linha de `src/` (phase inteira) e nenhuma de `test/` na iter 2;
`TranslationEngine.cs` segue deferido (D-...-4); pisos do DoD identicos.

**Numeros finais:** build 0 erros · 304 C# (302p/2s) + 60 JS · projecao Sonar **95,27%**
(minha conta; doer 95,32% — delta 0,05pp) · margem sobre 90% = **+94 a +98 unidades**.

**Achados de producao da phase** (registrados, nao corrigidos — `src/` fechado por escopo):
handle de zip aberto no fallback de `ReadEpubSafeAsync` (`ParsingEngine.cs:138-190`) e `byte[0]`
em vez de `null` em `ExtractCoverImageAsync` (`ParsingEngine.cs:316`) — ambos conferidos por mim
nas linhas citadas de `.jdi/todos.md` § `De coverage-90`.

## Para o revisor humano do PR

O que o gate automatizado desta phase **NAO** prova — leia antes de aprovar:

1. **O numero de 90% e uma PROJECAO local** (95,27% na minha conta, modelo linhas+condicoes
   ancorado nos numeros remotos de `main`). O juiz final e o **painel do SonarCloud no PR** — os
   modelos de linha executavel do coverlet/lcov nao sao identicos aos do Sonar; a margem de
   ~+94-98 unidades absorve divergencia razoavel, mas so o scan remoto da o numero real.
2. **"Sem issues novas" NAO tem prova local** (D-...-6): os analisadores `csharpsquid`/
   `external_roslyn`/`javascript` do SonarCloud nao rodam em `dotnet build`/`node --test`. So o
   scan remoto do PR confirma — precedente real: a phase `sonar-zero-issues` teve issues que SO
   apareceram pos-push.
3. O Quality Gate verde mede **New Code**, nao o Overall de 90% (D-...-7) — nao aceite o check
   `sonarqube` como prova da meta.
4. O risco residual que reprovaria a meta e o lcov JS nao ser consumido pelo Sonar (83,96%) —
   confira no log do CI que o step "Test WebView scripts with lcov coverage" rodou e que o painel
   mostra cobertura > 0 nos 4 arquivos de `wwwroot/js/`.
5. Os 4 scripts JS de producao nao mudaram de comportamento por acao desta phase (diff de `src/`
   vazio), mas nao existe suite E2E de WebView — a confirmacao visual segue manual.

## Recommendation

Ship. Os 5 gates agora medem a execucao atual e sabem reprovar — provei por 8 cenarios
adversariais com exit codes reais, incluindo os 4 que o dispatch exigiu. O blocker da iter 1 era
procedente e esta fechado; a contestacao do doer sobre o contra-exemplo do `throw` e verdadeira e
nao muda o veredicto sobre o gate antigo (oco por 2 outros caminhos provados). Nenhum piso foi
afrouxado. W-1 (assercao sem piso de contagem no teste de progresso) e o unico item de codigo — 1
linha na proxima vez que `ModelAccessTests.cs` for aberto; nao justifica reter a phase. Apos o
merge, confirmar no SonarCloud os itens 1-4 da secao acima (`## Deferred to PR review` do
CONTEXT).

**Verdict:** APPROVED_WITH_WARNINGS

## DoD Critic (enhanced — forcado por /jdi-issue)

NOTA DE EXECUCAO: rodado inline pelo orquestrador. Os comandos foram extraidos do `CONTEXT.md`
vigente por parser e executados com captura do exit code REAL; repo restaurado e conferido
(`git status --porcelain`) depois de cada mutacao.

**CORRECAO DO PROPRIO REGISTRO DA ITER 1 (auditoria):** o harness de ataque que o orquestrador usou
na iter 1 extraia a linha do `Verify:` JUNTO com o sufixo `... || echo "exit 1"`, entao o `bash -c`
sempre retornava 0 — as duas "provas" de exit 0 daquela passagem eram artefato do teste, nao do
gate. O que sustenta o blocker da iter 1, e continua valendo, e: (1) o achado do
`find | sort | tail -1`, provado por comparacao direta de mtimes (o comando escolheu o relatorio de
07:49:59 enquanto o mais recente era 07:53:32 — sem harness envolvido); e (2) a reproducao
INDEPENDENTE do defeito estrutural pelo doer e pela reviewer, por dois outros caminhos (runner
ausente e suite C# falhando, ambos com o comando ANTIGO saindo 0). O blocker era valido; uma das
demonstracoes nao era.

**Matriz desta passagem (exit code real, comandos literais do CONTEXT.md vigente):**

| Cenario | Resultado |
|---|---|
| repo real, 5 itens, duas execucoes seguidas | **exit 0** nas 10 (sem falso positivo, `rm -rf` reentrante) |
| lcov VALIDO de 5399 bytes em disco + `node` fora do PATH | item 2 = **exit 127** (o furo original esta fechado) |
| suite C# com assercao invertida (`Assert.True`->`Assert.False` em `FileUtilityTests`) | itens 3 e 4 = **exit 1** |
| piso do item 2 elevado de 85 para 101 | **exit 1** (o comando sabe reprovar) |
| item 3 reapontado para `TranslationEngine` (23% real) | **exit 1** |
| `reportPaths` divergindo do destino do reporter no workflow | item 5 = **exit 1** |

Os tres gates que a iter 1 derrubou passaram a medir a execucao atual: diretorio de resultado limpo
por rodada (`rm -f`/`rm -rf`) encadeado com `&&`, de modo que falha do runner derruba o gate em vez
de cair no artefato anterior; e a selecao do relatorio deixou de ser heuristica (`find | sort |
tail -1` sobre GUIDs) para exigir exatamente 1 relatorio no diretorio dedicado. Nenhum piso foi
afrouxado (85 / 0.90 / 0.99 preservados), e os itens 1 e 5 ficaram mais duros do que o criterio
exigia (`# pass > 1` rejeita suite vazia; item 5 compara o caminho do `reportPaths` com o destino do
reporter em vez de so procurar as duas strings).

Nenhuma linha `Type=Auto`/`PASS` mostrou-se oca nesta passagem.

**Verdict:** APPROVED
