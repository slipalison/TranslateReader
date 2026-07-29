# Phase 9: Pipeline unificada (orquestrador reusable) — Plan  (slug: pipeline-unificada)

## Goal
Consolidar push/PR num `pipeline.yml` unico via `workflow_call` (run graph unico: ci, codeql,
semgrep, sca, secret-scan, sonarqube, dependency-review, sbom); `scorecard.yml`/`release.yml` seguem
isolados; branch protection re-mapeada pros novos check names.

## Locked decisions (D-2026-07-28-pipeline-unificada-1..6)
`-1` scorecard/release fora, hibrido schedule+workflow_call, `uses: ./` sem SHA, remap obrigatorio em
ordem travada, concurrency so no orquestrador · `-2` trigger surface arquivo por arquivo, sem rename ·
`-3` permissions por job CALLER (`contents: read` + elevacao pontual) · `-4` `secrets: inherit` so no
sonar + PR-context via inputs explicitos · `-5` `if:` no CALLER, nunca no reusable · `-6` artifact
names unicos + snapshot do branch protection ANTES de editar.

**Specialist (single-stack):** `jdi-doer-translatereader` em todas as tasks.

## Regras de execucao (obrigatorias)
1. Branch `jdi/pipeline-unificada` (main protegida). **Commit local por task; `git push` SO na T-7** —
   push intermediario dispara run meio-migrado (reusable sem `workflow_call` = run vermelho).
2. `scorecard.yml` e `release.yml`: diff obrigatoriamente VAZIO.
3. Nenhum step interno muda, exceto `Begin SonarQube analysis` (unica excecao, exigida por `-4`).
4. `secrets.GITHUB_TOKEN` chega ao reusable sem `secrets: inherit` — NAO herdar no caller do
   secret-scan (quebraria o DoD 5).
5. Zero `${{ }}` dentro de `run:` (learning W-3): inputs entram por `env:`. `pr-branch` vem de
   `pull_request.head.ref`, input atacavel — interpolar direto seria script injection.
6. Ordem de chaves no job caller: `name:` -> `if:` -> `permissions:` -> `uses:` -> `with:` ->
   `secrets:`. Os greps do DoD usam `-B5`/`-B10`; outra ordem reprova a verificacao.
7. Inferencia registrada (sem impacto em DoD): `sonarqube.yml` perde `workflow_dispatch`, mesma razao
   de `ci.yml` em `-2` (sem schedule, o orquestrador vira a entrada manual).
8. `-4`/`-5` sao corretos sob qualquer semantica de `github.event_name` em `workflow_call` (caller
   sempre ve o evento original). Decisao locked nao reaberta.

## Tasks

### Wave 1 (pre-edicao — bloqueia todo o resto)

#### T-1: snapshot do branch protection + branch de trabalho
- **Files modified:** `.jdi/phases/pipeline-unificada/branch-protection-before.json`
- **Comandos:** `git switch -c jdi/pipeline-unificada`; depois em **bash** (nao pwsh):
  `gh api repos/slipalison/TranslateReader/branches/main/protection > .jdi/phases/pipeline-unificada/branch-protection-before.json`
- **Acceptance:**
  - `test -s` + `python -c "import json;json.load(open(F,encoding='utf-8'))"` passa (DoD 10) —
    redirecionar em pwsh grava UTF-16 e reprova o teste
  - `git status --porcelain .github/workflows/` VAZIO no momento do snapshot (prova do "antes de qualquer edicao")
  - `git branch --show-current` = `jdi/pipeline-unificada`
- **Dependencies:** none
- **Test:** o `json.load` acima. `gh api` falhou = ABORTAR a phase (sem baseline nao ha remap auditavel).
- **Status:** pending

### Wave 2 (3 tasks paralelas — arquivos disjuntos)

#### T-2: `ci.yml` -> `workflow_call` puro
- **Files modified:** `.github/workflows/ci.yml`
- **Edit:** bloco `on:` (L3-7) vira `on:` + `workflow_call:`; remover bloco `concurrency:` (L12-14).
  `permissions: contents: read` e os jobs `test`/`build` intactos.
- **Acceptance:**
  - `grep -q "workflow_call:"` e `! grep -Eq "^\s*(push|pull_request|workflow_dispatch):"` (DoD 1,2,3)
  - `! grep -q "concurrency:"` (DoD 8)
  - diff nao toca step: `git diff main -- .github/workflows/ci.yml | grep -E "^[+-]" | grep -Eq "(uses:|run:)"` = FALSE
- **Dependencies:** T-1
- **Test:** `python -c "import yaml;yaml.safe_load(open('.github/workflows/ci.yml',encoding='utf-8'))"`
- **Status:** pending

#### T-3: 4 scanners semanais -> `workflow_call` + `schedule` [+ `workflow_dispatch`]
- **Files modified:** `.github/workflows/codeql.yml`, `semgrep.yml`, `sca.yml`, `secret-scan.yml`
- **Edit:** so o bloco `on:` + remocao de `concurrency:`. Permissions de job, harden-runner, SHA pins
  e steps intactos.

  | arquivo | `on:` final | remove |
  |---|---|---|
  | codeql.yml | workflow_call + `schedule: 26 7 * * 1` + workflow_dispatch (ADD) | push, pull_request, concurrency |
  | semgrep.yml | workflow_call + `schedule: 45 6 * * 1` + workflow_dispatch (ADD) | push, pull_request, concurrency |
  | sca.yml | workflow_call + workflow_dispatch (MANTEM) + `schedule: 50 5 * * 3` | push, pull_request, concurrency |
  | secret-scan.yml | workflow_call + `schedule: 15 4 * * 0` + workflow_dispatch (ADD) | push, pull_request, concurrency |
- **Acceptance:**
  - nos 4: `grep -q "workflow_call:"`, `grep -q "workflow_dispatch:"`, `grep -q "cron:"`,
    `! grep -Eq "^\s*(push|pull_request):"`, `! grep -q "concurrency:"` (DoD 1,2,3,8)
  - crons preservados: `git diff main -- <4 files> | grep -E "^[+-].*cron"` VAZIO
  - hardening intacto: o diff nao contem linha `uses:` nem `permissions:` de job
- **Dependencies:** T-1
- **Test:** `yaml.safe_load` nos 4
- **Status:** pending

#### T-4: `sonarqube.yml` (inputs de PR) + `dependency-review.yml` + `sbom.yml`
- **Files modified:** `.github/workflows/sonarqube.yml`, `dependency-review.yml`, `sbom.yml`
- **Edit A `sonarqube.yml`:** `on:` vira so `workflow_call:` com 3 inputs (`type: string`,
  `required: false`, `default: ""`): `pr-key`, `pr-branch`, `pr-base`. Remove push/pull_request/
  workflow_dispatch (regra 7) e `concurrency:`. `fetch-depth: 0`, `env: SONAR_TOKEN` e todos os
  `if: env.SONAR_TOKEN != ''` INTOCADOS. Step `Begin SonarQube analysis` passa a:

  ```yaml
        env:
          SONAR_PR_KEY: ${{ inputs.pr-key }}
          SONAR_PR_BRANCH: ${{ inputs.pr-branch }}
          SONAR_PR_BASE: ${{ inputs.pr-base }}
        run: |
          args=(/k:"slipalison_TranslateReader" /o:"slipalison"
                /d:sonar.host.url=https://sonarcloud.io /d:sonar.token="$SONAR_TOKEN"
                /d:sonar.cs.opencover.reportsPaths="**/TestResults/**/coverage.opencover.xml")
          if [ -n "$SONAR_PR_KEY" ]; then
            args+=(/d:sonar.pullrequest.key="$SONAR_PR_KEY"
                   /d:sonar.pullrequest.branch="$SONAR_PR_BRANCH"
                   /d:sonar.pullrequest.base="$SONAR_PR_BASE")
          fi
          dotnet-sonarscanner begin "${args[@]}"
  ```
- **Edit B `dependency-review.yml`:** `on:` vira so `workflow_call:` (remove `pull_request:`), remove
  `concurrency:`, NUNCA `workflow_dispatch` (a action exige base/head SHA reais de PR), zero
  `if:`/`event_name` no arquivo. Permissions do job intactas.
- **Edit C `sbom.yml`:** `on:` = workflow_call + workflow_dispatch (mantem) + `schedule: 20 3 * * 2`;
  remove `push:` e `concurrency:`; sem `event_name`; `contents: write` do job intocado.
- **Acceptance:**
  - `grep -A20 "workflow_call:" sonarqube.yml | grep -q "inputs:"` e `grep -q "fetch-depth: 0"` (DoD 6)
  - `grep -c "inputs\." sonarqube.yml` = **3**, todas em linha `SONAR_PR_(KEY|BRANCH|BASE):` (regra 5)
  - `! grep -q "workflow_dispatch:" dependency-review.yml` e `! grep -q "event_name" dependency-review.yml sbom.yml` (DoD 3,7)
  - nos 3: `grep -q "workflow_call:"`, `! grep -Eq "^\s*(push|pull_request):"`, `! grep -q "concurrency:"` (DoD 1,2,8)
- **Dependencies:** T-1
- **Test:** `yaml.safe_load` nos 3 + `bash -n` no bloco `run:` extraido (sintaxe do array)
- **Status:** pending

### Wave 3

#### T-5: criar `pipeline.yml` (orquestrador)
- **Files modified:** `.github/workflows/pipeline.yml` (novo — unico arquivo novo da phase)
- **Header exato:** `name: Pipeline`; `on:` = `push: branches: [main]` + `pull_request:` (bare) +
  `workflow_dispatch:`; `permissions: contents: read`; `concurrency:` group
  `${{ github.workflow }}-${{ github.ref }}` com `cancel-in-progress: true`.
- **8 jobs caller, SEM `needs:` entre eles** (paralelos = 1 run graph), `uses: ./.github/workflows/<x>.yml`:

  | job id / name | permissions | if | extra |
  |---|---|---|---|
  | ci / CI | contents: read | — | — |
  | codeql / CodeQL | contents: read, security-events: write, actions: read | — | — |
  | semgrep / Semgrep | contents: read, security-events: write | — | — |
  | sca / SCA | contents: read | — | — |
  | secret-scan / Secret Scan | contents: read | — | — |
  | sonarqube / SonarQube | contents: read | — | `with:` 3 inputs + `secrets: inherit` |
  | dependency-review / Dependency Review | contents: read, pull-requests: write | `github.event_name == 'pull_request'` | — |
  | sbom / SBOM | contents: write | `github.event_name == 'push'` | — |

  ```yaml
    sonarqube:
      name: SonarQube
      permissions:
        contents: read
      uses: ./.github/workflows/sonarqube.yml
      with:
        pr-key: ${{ github.event.pull_request.number }}
        pr-branch: ${{ github.event.pull_request.head.ref }}
        pr-base: ${{ github.event.pull_request.base.ref }}
      secrets: inherit
  ```
- **Acceptance:**
  - rodar literalmente os greps do CONTEXT de DoD 4 (`-B10` de cada `uses:` contem a elevacao),
    DoD 5 (`grep -c "secrets: inherit"` = 1 e `-B10` contem `sonarqube.yml`) e DoD 7 (`-B5` contem o `if:`)
  - `grep -Eq "^\s*(push|pull_request):"` e `grep -q "permissions:"` (DoD 1,9)
  - caller-only: `! grep -Eq "^\s+(runs-on|run):" pipeline.yml` — logo sem harden-runner aqui, o
    hardening vive dentro de cada reusable
- **Dependencies:** T-2, T-3, T-4
- **Test:** `yaml.safe_load('.github/workflows/pipeline.yml')`
- **Status:** pending

### Wave 4

#### T-6: bateria de validacao local + sweep de regressao de hardening
- **Files modified:** nenhum (corrige o que falhar; evidencia colada no SUMMARY.md)
- **Acceptance (tudo verde; actionlint indisponivel no ambiente):**
  - YAML: `for f in .github/workflows/*.yml; do python -c "import sys,yaml;yaml.safe_load(open(sys.argv[1],encoding='utf-8'))" "$f" || echo FAIL $f; done` sem FAIL; `ls .github/workflows/*.yml | wc -l` = **11**
  - os **10 comandos `Verify:` do CONTEXT.md** rodados literalmente, todos exit 0
  - anti-deadlock: `grep -oE "\./\.github/workflows/[a-z-]+\.yml" pipeline.yml | sed 's|^\./||' | sort -u`
    = 8 arquivos, todos existentes e com `workflow_call:`
  - hardening (contagens iguais as de antes): `harden-runner` = **10** = `runs-on: ubuntu-latest`;
    `actions/checkout@` = **12** = `persist-credentials: false`; `! grep -rEq "uses:\s*[^ ]+/[^ ]+@v[0-9]" .github/workflows/`
  - injection: `! grep -rEq "^\s+run:.*\\$\{\{" .github/workflows/` e nenhum `inputs.`/`github.` dentro de bloco `run:`
  - artifacts unicos (`-6a`): so `coverage` (ci) e `sbom-spdx` (sbom) entre os 8 migrados
  - intocados: `git diff --name-only main...HEAD -- .github/workflows/scorecard.yml .github/workflows/release.yml` **VAZIO**
- **Dependencies:** T-5
- **Test:** a propria bateria
- **Status:** pending

### Wave 5

#### T-7: abrir PR, capturar check names REAIS, preparar o remap (sem executar)
- **Files modified:** `.jdi/phases/pipeline-unificada/check-names-after.txt`, `branch-protection-remap.md`
- **Ordem travada (`-1d`; inverter foi o incidente de hoje):** push do branch -> `gh pr create --base main`
  -> `gh pr checks --watch` -> `SHA=$(gh pr view --json headRefOid -q .headRefOid)` ->
  `gh api repos/slipalison/TranslateReader/commits/$SHA/check-runs --jq '.check_runs[].name' | sort -u > check-names-after.txt`
  -> escrever em `branch-protection-remap.md` (SEM executar): contexts ANTES (do
  `branch-protection-before.json`), contexts DEPOIS (capturados) e o comando pronto
  `gh api -X PATCH repos/slipalison/TranslateReader/branches/main/protection/required_status_checks -f strict=true -f 'contexts[]=<novo>' ...`
- **Acceptance:**
  - PR aberta `jdi/pipeline-unificada` -> `main`; aba Actions mostra UM run (`Pipeline`) no lugar de ~8
  - `check-names-after.txt` com >= 8 nomes, nenhum escrito a mao
  - `branch-protection-remap.md` traz antes/depois + PATCH pronto + aviso de que os contexts antigos
    ficam "Expected" ate o PATCH (deadlock esperado, resolvido pelo remap)
  - **NAO executar** PATCH nem merge — ambos sao `Deferred to PR review`
- **Dependencies:** T-6
- **Test:** `gh pr checks` lista os checks do `Pipeline`; `test -s check-names-after.txt`
- **Status:** pending

## Execution
- Total tasks: 7
- Waves: 5
- Estimated parallel speedup: 1.4x (paralelismo real concentrado na Wave 2)
- Desvio do corte sugerido: o snapshot do branch protection virou **T-1** (era T-6), porque `-6b` e o
  DoD 10 exigem "ANTES de qualquer edicao" — na ordem sugerida o DoD nasceria falso. Logo 7 tasks, e
  a criacao do branch entra na T-1.
- README so tem badge OpenSSF Scorecard (nenhum badge de `ci.yml`) -> nada a trocar (YAGNI).

## Files modified (all tasks)
- `.github/workflows/`: `pipeline.yml` (novo), `ci.yml`, `codeql.yml`, `semgrep.yml`, `sca.yml`,
  `secret-scan.yml`, `sonarqube.yml`, `dependency-review.yml`, `sbom.yml`
- `.jdi/phases/pipeline-unificada/`: `branch-protection-before.json`, `check-names-after.txt`, `branch-protection-remap.md`
- INTOCADOS: `.github/workflows/scorecard.yml`, `.github/workflows/release.yml`

## Test requirements
- YAML: `python -c "import yaml;yaml.safe_load(open(F,encoding='utf-8'))"` por arquivo (actionlint indisponivel)
- Greps: os 10 `Verify:` do CONTEXT + sweep de hardening da T-6
- Cobertura: SKIPPED — infra-only, nenhum `.cs` novo/alterado (D-6 nao se aplica)

## DoD -> task
1 -> T-2,T-3,T-4,T-5 · 2 -> T-2..T-5 · 3 -> T-2,T-3,T-4 · 4 -> T-5 · 5 -> T-5 · 6 -> T-4 ·
7 -> T-4,T-5 · 8 -> T-2,T-3,T-4 · 9 -> T-5,T-6 · 10 -> **T-1**
