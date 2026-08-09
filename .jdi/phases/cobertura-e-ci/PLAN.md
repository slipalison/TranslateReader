# Phase 2: Cobertura e CI — Plan  (slug: cobertura-e-ci)

## Leia antes de comecar (obrigatorio)

1. **7 tasks EM ORDEM (T-1 -> T-7). Nao reordene, nao junte, nao paralelize.** A ordem de commits
   e locked por D-...-6 (medidor -> CI -> docs -> branch protection -> Android) e T-1..T-3 escrevem
   o MESMO arquivo. 1 task = 1 commit atomico, mensagem literal dada em cada task.
2. "DoD N literal" = copie o comando `Verify:` do CONTEXT.md **sem editar**. Licoes de `coverage-90`
   e `app-redesign`: reescrever um Verify pra ele passar (ou deixar um `|| echo` que zera o exit
   code) e o modo classico de falhar a phase.
3. **Escopo cabe em 8 tasks -> a linha de corte de D-...-6 NAO foi aplicada.** Itens 1-5 ficam todos
   nesta fase; nenhuma phase nova, nenhuma decisao de divisao a registrar.
4. **NAO FACA em nenhuma task:** adicionar `coverlet.msbuild` ou qualquer `Threshold`/`ThresholdType`
   (D-...-1); criar twin PowerShell do script; escrever em `TestResults/js-lcov.info` ou tocar
   `sonarqube.yml` (pin de D-2026-07-31-coverage-90-9); definir `COVERAGE_MIN`/`COVERAGE_JS_MIN` em
   qualquer YAML; dar `PATCH` na branch protection (deferred); criar segundo test project ou
   multi-target (D-2026-07-30-regression-suite-6); tocar `.cs`/`.xaml` de producao; mexer em warning
   legado ou no `NoWarn` fora do que T-7 autoriza.
5. **Esta phase nao adiciona nenhum `.cs`** — o escopo do proprio gate e o que ja existe hoje. O
   numero de partida sai de T-1, medido, nunca estimado.

## Goal
O piso de 90% em codigo novo/alterado (D-6) vira medidor versionado que falha local e no CI, com
job de cobertura proprio no pipeline.

## Locked decisions (from CONTEXT.md)
D-...-1 gate = `scripts/coverage-gate.sh` (bash, mede a propria execucao, ponderado por linha) +
SonarCloud como 2a camada; D-...-2 escopo `--diff-filter=AM` pos-`4285f25`, `test/**` fora;
D-...-3 bloqueia local E no CI, e `ci.yml` perde `--collect` e o artifact; D-...-4 denominador =
C# do Core + 4 scripts JS (piso 85%) + guarda dura do app MAUI com waiver auditavel;
D-...-5 `coverage.yml` `workflow_call` puro + caller `Coverage` + reportgenerator + W-2 Android +
protocolo de remap; D-...-6 ordem obrigatoria e linha de corte (nao usada).

## Ruling do planner (conflito real entre D-...-2 e D-...-4, resolvido aqui)
`.cs` do app **modificado** pos-boundary esta no escopo `AM` e nao aparece no Cobertura. D-...-2
chama ausencia sob `src/TranslateReader/` de "falha dura", mas delega o caso ("tratado em D-...-4"),
e D-...-4(c) escopa a falha dura a arquivo **NOVO**. Vale o especifico: **modificado -> sai do
denominador como `COVERAGE_SKIP <path> reason=app-maui-not-instrumented`** (visivel, nunca
silencioso); **novo -> exit 2**. Se o doer discordar, e BLOCKED com decisao nova — nao patch.

## Execucao
7 tasks, **1 wave, cadeia estritamente sequencial, 0 paralelismo** — nao e falta de esforco: a
ordem de commits e locked (D-...-6) e T-1..T-3 sao o mesmo arquivo. Specialist de TODAS as tasks:
`jdi-doer-translatereader` (single-stack, glob `**/*`).

---

## T-1: `scripts/coverage-gate.sh` — nucleo de medicao C# + numero de partida MEDIDO
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `scripts/coverage-gate.sh` (novo)
- **Passos:**
  1. `#!/usr/bin/env bash` + `set -euo pipefail` + `cd "$(git rev-parse --show-toplevel)"`.
     **`COVERAGE_MIN=${COVERAGE_MIN:-90}` na COLUNA 0** (DoD 1 casa `^COVERAGE_MIN=\$\{...`; um
     espaco ou um `readonly` na frente reprova). Boundary como constante literal `4285f25` (DoD 2
     faz grep do literal), sem override por env.
  2. Diretorio proprio, **apagado no inicio de TODA execucao**: `rm -rf TestResults/coverage-gate`
     + `mkdir -p`. Isso e o teste do sentinel de DoD 1.
  3. Escopo: `git log --diff-filter=AM --pretty=format: --name-only 4285f25..HEAD | sort -u`
     filtrado por `\.cs$`, **menos** `test/`, `obj/`, `bin/`, e menos arquivo que nao existe mais
     (`test -f`) — deletado depois nao entra em denominador.
  4. Medir AGORA, nao ler artefato de ninguem (learning `coverage-90` 2):
     `dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --collect:"XPlat Code Coverage" --results-directory TestResults/coverage-gate`
     encadeado com `&&`/`if`, **nunca `;`**. Suite vermelha ou relatorio ausente -> **exit 3**.
     Localizar o `coverage.cobertura.xml` **so dentro** de `TestResults/coverage-gate`; proibido
     `ls -t` / `find | sort | tail -1` (learning `coverage-90` 3) — o diretorio esta limpo, entao
     0 relatorio = exit 3 e >1 = agregue todos, nunca "escolha um".
  5. Parser sem dependencia externa (`xmllint` nao existe no Git Bash): normalize com
     `sed 's/</\n</g'`, siga o `<class filename=...>` corrente, colete `filename|number|hits`.
     **Dedup obrigatorio por `filename + number`** com `hits = max` (classe parcial rende varios
     `<class>` e infla o denominador — D-...-1). Normalize `\` -> `/` e case por **sufixo** de path
     (a raiz do `<source>` e `D:\REPO\...` no Windows e `/home/runner/...` no CI).
  6. Saida, contrato do CONTEXT: `COVERAGE_FILE <path> covered=<int> valid=<int>` por arquivo
     medido; `COVERAGE_SKIP <path> reason=no-instrumented-lines` (interface/record/enum) e
     `reason=app-maui-not-instrumented` (ruling acima); e
     `COVERAGE_SCOPE covered= valid= pct= files=` com `printf '%.2f'` (DoD 2 exige
     `|pct - 100*covered/valid| <= 0.06` — **soma ponderada, nunca media de `line-rate`**).
     `valid == 0` -> exit 3 (medir nada nao e passar).
  7. `pct < COVERAGE_MIN` -> exit 1; senao exit 0.
  8. **Bit de execucao (armadilha Windows, `core.filemode=false`):**
     `git add --chmod=+x scripts/coverage-gate.sh`. DoD 1 exige `100755` no index ja em T-2.
  9. Commit: `feat(cobertura-e-ci): add line-weighted coverage gate script`.
- **REGRA DE PARADA (o motivo de esta task vir primeiro):** rode e **reporte o numero antes de
  qualquer bloqueio existir**. Se `pct < 90`: proibido baixar o piso, proibido waiver de arquivo do
  Core por conta propria. Waiver so onde uma decisao JA aceita a lacuna (cite a decisao na linha).
  Caso contrario: `BLOCKED: scope coverage <X>% (<covered>/<valid>)` no SUMMARY.md e pare.
- **Acceptance:** **DoD 2 literal** exit 0; `bash scripts/coverage-gate.sh` exit 0 com o numero
  impresso; sentinel plantado em `TestResults/coverage-gate/` nao sobrevive a rodada; zero `ls -t`,
  zero `| sort | tail`, zero `;` entre medicao e avaliacao no script.
- **Dependencies:** none
- **Test:** a propria suite roda dentro do script — `Failed: 0`, `Total >= 375`
- **Status:** pending

## T-2: bloco JS no gate (4 scripts do WebView, piso 85%)
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `scripts/coverage-gate.sh`
- **Passos:**
  1. `COVERAGE_JS_MIN=${COVERAGE_JS_MIN:-85}` **na coluna 0** (mesma armadilha de T-1).
  2. Comando literal de D-...-4(b), destino no diretorio do gate:
     `node --test --experimental-test-coverage --test-reporter=lcov --test-reporter-destination=TestResults/coverage-gate/js-lcov.info test/js/`.
     Node ausente ou suite JS vermelha -> exit 3.
  3. Parse do lcov por `SF:` + `DA:<line>,<hits>`, filtrando **so** os 4 arquivos de producao em
     `src/TranslateReader/Resources/Raw/wwwroot/js/` (`bridge`, `paginated`, `scroll`,
     `translation`) — `test/js/harness.js` e `index.js` ficam fora. `files != 4` -> exit 3 (drift
     de path tem de gritar, nao encolher o denominador).
  4. `COVERAGE_JS covered= valid= pct= files=4`; `pct < COVERAGE_JS_MIN` -> exit 1.
  5. **Avalie os dois pisos antes de sair**: imprima C# e JS e so entao escolha o exit — senao uma
     falha de C# esconde o numero do JS.
  6. Commit: `feat(cobertura-e-ci): measure the four WebView scripts in the coverage gate`.
- **NAO FACA:** escrever em `TestResults/js-lcov.info`; tocar `sonarqube.yml` (DoD 4 confere que as
  2 ocorrencias do literal continuam la).
- **Acceptance:** **DoD 4 literal** exit 0; **DoD 1 literal** exit 0 (os dois defaults pinados,
  `100755`, verde no repo real, **vermelho com `COVERAGE_MIN=101`**, sentinel apagado).
- **Dependencies:** T-1
- **Test:** `node --test test/js/` verde + `bash scripts/coverage-gate.sh` exit 0
- **Status:** pending

## T-3: guarda do app MAUI + `.jdi/coverage-waivers.txt`
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `scripts/coverage-gate.sh`, `.jdi/coverage-waivers.txt` (novo)
- **Passos:**
  1. A guarda roda **antes** do `dotnet test` (falha barata, 2 segundos em vez de 3 minutos).
  2. `.cs` novo no app = `git log --diff-filter=A --pretty=format: --name-only 4285f25..HEAD` sob
     `src/TranslateReader/` **uniao** `git ls-files --others --exclude-standard -- 'src/TranslateReader/**.cs'`
     (sinal antes do commit, D-...-4c). Exclua `obj/` e `bin/` explicitamente — `obj/**/*.g.cs`
     existe no disco e nao pode contar.
  3. `.jdi/coverage-waivers.txt`: uma linha por path, **exige** referencia `# D-...`; linha sem
     referencia nao e waiver — imprima `COVERAGE_WAIVER_INVALID <path>` e mantenha a violacao.
     Linhas `#`/vazias ignoradas. Arquivo commitado com cabecalho explicando o formato (esperado
     hoje: **zero entradas vivas**, o app nao ganhou `.cs` novo desde o boundary).
  4. `COVERAGE_GUARD new_app_cs=<int> waived=<int>`; `new_app_cs > waived` -> **exit 2**.
  5. Waiver valido tambem tira o path do denominador C# (valvula (i) de D-...-2), impresso como
     `COVERAGE_SKIP <path> reason=waived`.
  6. Commit: `feat(cobertura-e-ci): fail the gate on new untested MAUI app code`.
- **Acceptance:** **DoD 3 literal** exit 0 — red (exit 2) com o probe, green (exit 0) com o waiver,
  e **arvore limpa no fim** (waiver restaurado, probe removido).
- **Dependencies:** T-2
- **Test:** o proprio red-then-green de DoD 3 + `bash scripts/coverage-gate.sh` exit 0 sem probe
- **Status:** pending

## T-4: `coverage.yml` + caller no `pipeline.yml` + `ci.yml` perde a coleta
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `.github/workflows/coverage.yml` (novo), `.github/workflows/pipeline.yml`,
  `.github/workflows/ci.yml`
- **Passos:**
  1. `coverage.yml`: `on: workflow_call:` **puro** (sem `push`/`pull_request`/`workflow_dispatch`/
     `schedule`), **sem `concurrency:`** (vive so no orquestrador — learning `pipeline-unificada` 4),
     `permissions: contents: read` no topo, job `ubuntu-latest` com `name: Coverage gate`.
  2. Steps: `step-security/harden-runner` (`egress-policy: audit`) -> `actions/checkout` com
     **`fetch-depth: 0`** e `persist-credentials: false` -> `actions/setup-dotnet` 10.0.x ->
     `actions/setup-node` 24 -> `bash scripts/coverage-gate.sh`.
     **`fetch-depth: 0` e critico:** com o default 1 o commit `4285f25` nao existe no clone e o
     gate sai 3. Reaproveite os SHAs de 40 hex ja pinados em `ci.yml`/`sonarqube.yml`; 100% das
     actions de terceiro pinadas (DoD 5 conta).
  3. Relatorio (DoD 6), `if: ${{ !cancelled() }}` para o numero aparecer TAMBEM quando o gate
     reprova: `dotnet tool install --global dotnet-reportgenerator-globaltool` ->
     `reportgenerator -reports:TestResults/coverage-gate/**/coverage.cobertura.xml -targetdir:TestResults/coverage-gate/report -reporttypes:'MarkdownSummaryGithub;Html'`
     -> `cat .../SummaryGithub.md >> "$GITHUB_STEP_SUMMARY"` -> `actions/upload-artifact` com
     `name: coverage-html-report` (unico entre TODOS os workflows — hoje existem `SARIF file` e
     `sbom-spdx`; `coverage` some no passo 5) e `if-no-files-found: error`.
  4. `pipeline.yml`, job novo com `name:` como **primeira** chave (DoD 9 le o primeiro `name:` do
     bloco): `coverage:` / `name: Coverage` / `permissions: contents: read` /
     `uses: ./.github/workflows/coverage.yml`. **Sem `secrets:`** e **sem `if:`** (roda em push e
     em PR, D-...-3).
  5. `ci.yml`: tirar `--collect:"XPlat Code Coverage"` do job `test` e **remover inteiro** o step
     `Upload coverage artifact`. O job continua rodando a suite (`Pipeline / CI` inalterado).
  6. Commit: `ci(cobertura-e-ci): add dedicated coverage job and drop duplicate collection`.
- **NAO FACA:** citar `COVERAGE_MIN`/`COVERAGE_JS_MIN` em `coverage.yml`, nem em comentario (DoD 5
  faz grep no arquivo inteiro) — o piso mora no script, senao um commit de workflow o afrouxa.
- **Acceptance:** **DoD 5 literal** exit 0 e **DoD 6 literal** exit 0; os 3 arquivos num unico
  commit (separar entregaria PR que mede duas vezes ou nenhuma).
- **Dependencies:** T-3
- **Test:** n/a local (YAML) — a prova real e o 1o run da PR, coberto em T-7
- **Status:** pending

## T-5: uma regra, um lugar — reviewer chama o script e os docs param de mentir a versao
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `.jdi/agents/jdi-reviewer-translatereader.md`,
  `.jdi/agents/jdi-doer-translatereader.md`, `.jdi/registry/LEGACY.md`,
  `.jdi/registry/LEGACY-reviewers.md`, `.jdi/PROJECT.md`
- **Passos:**
  1. Gate 3 do reviewer (200-278): substituir as DUAS implementacoes por `bash scripts/coverage-gate.sh`
     + contrato de saida + exit codes (0/1/2/3). O bloco PowerShell com `Measure-Object -Average`
     **sai inteiro** (DoD 8 confere a ausencia do literal) e o enquadramento `--diff-filter=A` /
     "so arquivos novos" vira `AM`, ponderado por linha.
  2. Mesmo arquivo, linhas 64, 76, 746 e 787-798: versao `10.0.1`, escopo `AM`, e o fim do
     "SKIPPED enquanto nao houver arquivo novo" (agora sempre ha escopo). **Nao mexa na severidade
     de nenhum outro gate** — nada nesta phase autoriza isso.
  3. `jdi-doer-translatereader.md:68,70`: `coverlet.collector 10.0.1` + a obrigacao de rodar
     `bash scripts/coverage-gate.sh` antes de declarar task pronta.
  4. `.jdi/registry/LEGACY.md:26,29,37-38` e `LEGACY-reviewers.md:19,36`: mesma correcao.
  5. `.jdi/PROJECT.md:27` -> `coverlet.collector 10.0.1` (DoD 8 exige o literal) e `:84` -> o gate
     agora existe e onde ele mora.
  6. **Nao rodar `npx jdi-cli render`** (quebra no Windows; as views sao gitignored e geradas).
  7. Commit: `docs(cobertura-e-ci): point the reviewer coverage gate at the script`.
- **Acceptance:** **DoD 8 literal** exit 0; `git diff` so com linhas de cobertura/versao — zero
  mudanca de severidade em Gates 1/2/4/5.
- **Dependencies:** T-4
- **Test:** n/a (documentacao de processo)
- **Status:** pending

## T-6: baseline da branch protection + documento de remap (mutacao NAO executada)
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `.jdi/phases/cobertura-e-ci/branch-protection-before.json` (novo),
  `.jdi/phases/cobertura-e-ci/branch-protection-remap.md` (novo)
- **Passos:**
  1. `gh api repos/slipalison/TranslateReader/branches/main/protection > .jdi/phases/cobertura-e-ci/branch-protection-before.json`.
     Nada em T-1..T-5 mutou o objeto de protection, entao este ainda e o "antes de qualquer edicao"
     de D-...-5(4a). `gh` sem auth/admin -> **BLOCKED**; nunca escreva o JSON a mao.
  2. `branch-protection-remap.md` no molde do de `pipeline-unificada`, com cabecalho
     **`Status: NAO EXECUTADO`** e a ordem travada: `push -> abrir PR -> capturar nomes REAIS via
     gh api .../check-runs -> PATCH -> merge`.
  3. **O literal, e o conflito que o doer NAO pode esconder.** Registre os dois candidatos:
     - derivacao de D-...-5(4b) (`name:` do orquestrador + ` / ` + `name:` do job caller) =
       **`Pipeline / Coverage`** — literal exigido pelo DoD 9;
     - evidencia empirica deste repo (`.jdi/archive/pipeline-unificada/branch-protection-remap.md`:
       `Test (Linux)` -> `CI / Test (Linux)`; e o `before.json` pre-migracao prova que o `name:` do
       workflow **nao** prefixa nada) = **`Coverage / Coverage gate`**.
     Os dois nao podem estar certos. Por isso o `PATCH` so usa o nome **capturado** apos o 1o run —
     digitar qualquer um dos dois de memoria e literalmente o incidente
     D-2026-07-28-pipeline-unificada-1(d) (100% das PRs travadas).
  4. Comando `PATCH` pronto na variante `checks[]` com `app_id` e `-F strict=true` (nunca
     `contexts[]`, que grava `app_id: null`, e nunca `-f strict` -> 422), montado como os **9
     contexts do `before.json` + 1 novo = 10**, mais verificacao pos-PATCH e rollback identico ao
     `before.json`.
  5. Commit: `docs(cobertura-e-ci): record branch protection baseline and coverage check remap`.
- **NAO FACA:** rodar o `PATCH` (esta em `## Deferred to PR review`); adicionar required context
  que ainda nao reportou — e assim que se tranca o repositorio.
- **Acceptance:** **DoD 9 literal** exit 0; o PATCH documentado preserva os 9 `app_id` do
  `before.json` e acrescenta exatamente 1 context.
- **Dependencies:** T-5
- **Test:** n/a (artefato de operacao)
- **Status:** pending

## T-7: W-2 do Android — medir no CI e registrar (por ultimo, so na PR)
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `.jdi/phases/cobertura-e-ci/android-warnings.md` (novo), e **so se a medicao
  exigir** `Directory.Build.props`
- **Passos:**
  1. Push do branch + PR. A medicao e o log do job `Build (Android)` do `ci.yml` — nao ha SDK
     Android local, entao **nao invente resultado**: sem run, e BLOCKED.
  2. Zero IDs novos -> o arquivo contem o literal **`RESULTADO: zero IDs novos`** + link do run.
  3. IDs novos -> liste um por linha no formato `- CA1416 ...` / `- XA1234 ...` (DoD 7 casa
     `^- (CA|CS|MA|XA|NETSDK)[0-9]{3,5}`) e aplique a ordem de preferencia de D-...-5(3):
     (i) ID no `<NoWarn>` unico com **linha de comentario propria**, marca `RISCO:` quando for bug
     potencial, roteado para `.jdi/todos/`; (ii) `TreatWarningsAsErrors=false` **so** com
     `Condition` no TFM `net10.0-android` + comentario citando `D-2026-08-08-cobertura-e-ci-5`, e
     apenas se os IDs vierem do toolchain Android e nao forem enumeraveis.
     Desligar amplamente e **PROIBIDO** (desfaria a phase anterior pra nao ler um log).
  4. Invariantes de `baseline-de-estilo` preservadas: um unico elemento `<NoWarn>`, sem curinga,
     cada ID aparecendo em >= 2 linhas do arquivo, `TreatWarningsAsErrors` segue `true` na raiz.
  5. Commit: `chore(cobertura-e-ci): record the Android warning measurement (W-2)`.
- **Acceptance:** **DoD 7 literal** exit 0; `Build (Android)` verde no run citado.
- **Dependencies:** T-6
- **Test:** job `Build (Android)` do pipeline (evidencia = URL do run no arquivo)
- **Status:** pending

---

## Test requirements
- .NET: roda dentro do gate — `dotnet test ... -c Release`, `Failed: 0`, `Total >= 375`.
- JS: `node --test test/js/` verde (T-2 em diante roda dentro do gate).
- Gate: `bash scripts/coverage-gate.sh` exit 0 na raiz, e exit 1 com `COVERAGE_MIN=101`.
- Cobertura: **zero `.cs` novo/alterado nesta phase** -> o escopo do gate nao muda; o piso de 90%
  incide sobre o que T-1 mediu.

## Mapa DoD -> task
DoD 1 -> T-2 (defaults e `100755` nascem em T-1) | DoD 2 -> T-1 | DoD 3 -> T-3 | DoD 4 -> T-2 |
DoD 5 -> T-4 | DoD 6 -> T-4 | DoD 7 -> T-7 | DoD 8 -> T-5 | DoD 9 -> T-6

## Encerramento da phase (apos T-7)
1. Rodar os 9 DoD do CONTEXT.md em sequencia, literais, todos exit 0.
2. `SUMMARY.md`: numero de partida medido em T-1 (`covered/valid/pct`), lista de `COVERAGE_SKIP`
   com motivo, conteudo final do waiver, resultado do Android e qualquer BLOCKED.
3. Re-sincronizar T-5 se alguma decisao tiver sido emendada no meio da phase (learning de
   `baseline-de-estilo` 3: doc de processo commitado antes do desbloqueio descreve o mundo errado).
4. PR `feat/cobertura-e-ci` -> `main` com os 9 DoD e a secao `## Deferred to PR review` copiada:
   Quality Gate do SonarCloud 80% -> 90%, `PATCH` da protection **com o nome capturado do run**,
   resultado real do `build-android`, e conferencia de que a duracao da PR nao dobrou (a coleta
   saiu do `ci.yml`, o esperado e empate).
