# Phase 9: Pipeline unificada (orquestrador reusable) — Summary  (slug: pipeline-unificada)

**Status:** executed
**Tasks:** 7/7 completas, 0 blocked
**Branch:** `jdi/pipeline-unificada` (base `main` = `e5541f2`)
**PR:** https://github.com/slipalison/TranslateReader/pull/7

Resultado: um push/PR passa a gerar **1 run** (`Pipeline`) com 8 jobs caller em paralelo, no lugar
dos ~8 runs independentes de antes. `scorecard.yml` e `release.yml` seguem isolados, com diff vazio.

## Tasks executadas

| Task | O que fez | Commit |
|---|---|---|
| T-1 | Branch `jdi/pipeline-unificada` + snapshot do branch protection ANTES de qualquer edicao (`branch-protection-before.json`, 9 required contexts) | `628ea97` |
| T-2 | `ci.yml` -> `workflow_call` puro (perde push/pull_request/workflow_dispatch + concurrency) | `87919bf` |
| T-3 | `codeql.yml`, `semgrep.yml`, `sca.yml`, `secret-scan.yml` -> hibridos `workflow_call` + `schedule` + `workflow_dispatch` | `834c6f4` |
| T-4 | `sonarqube.yml` (3 inputs de PR + `run:` via `env:`), `dependency-review.yml` (`workflow_call` puro), `sbom.yml` (call + dispatch + cron) | `7b4674b` |
| T-5 | Novo `pipeline.yml` — 8 jobs caller sem `needs:`, permissions por job, `secrets: inherit` so no sonar, `if:` de evento so no caller, concurrency unica | `74b9347` |
| T-6 | Bateria de validacao local (10 DoD + sweeps de hardening/injection/artifact). Zero correcoes necessarias -> sem commit proprio (PLAN: `files modified: nenhum`) | — |
| T-7 | Push do branch, PR #7, captura dos check names reais via API, `branch-protection-remap.md` com o PATCH pronto (NAO executado) | `00ae5cd` |
| — | Atualizacao de `PLAN.md` (7x `completed`) + este SUMMARY | commit final |

## Arquivos modificados

- Novo: `.github/workflows/pipeline.yml`
- Migrados (8): `ci.yml`, `codeql.yml`, `semgrep.yml`, `sca.yml`, `secret-scan.yml`, `sonarqube.yml`,
  `dependency-review.yml`, `sbom.yml`
- Artefatos da phase: `branch-protection-before.json`, `check-names-after.txt`, `branch-protection-remap.md`
- **INTOCADOS (diff vazio, confirmado):** `.github/workflows/scorecard.yml`, `.github/workflows/release.yml`

`git diff --name-only main...HEAD` traz exatamente esses 9 workflows + os artefatos `.jdi/`, nada mais.

## T-6 — evidencia de validacao (saidas reais)

### YAML (actionlint indisponivel; validador = `python -c "import yaml; yaml.safe_load(...)"`)

```
for f in .github/workflows/*.yml; do python -c "import sys,yaml;yaml.safe_load(open(sys.argv[1],encoding='utf-8'))" "$f" || echo FAIL $f; done
# (nenhum FAIL)
ls .github/workflows/*.yml | wc -l
11
```

### Os 10 comandos `Verify:` do CONTEXT.md, rodados literalmente

```
DoD 1: PASS      DoD 2: PASS      DoD 3: PASS      DoD 4: PASS      DoD 5: PASS
DoD 6: PASS      DoD 7: PASS      DoD 8: PASS      DoD 9: PASS      DoD 10: PASS
```

### Anti-deadlock (todo `uses: ./` resolve pra arquivo com `workflow_call:`)

```
refs: 8
.github/workflows/ci.yml                      exists=yes workflow_call=yes
.github/workflows/codeql.yml                  exists=yes workflow_call=yes
.github/workflows/dependency-review.yml       exists=yes workflow_call=yes
.github/workflows/sbom.yml                    exists=yes workflow_call=yes
.github/workflows/sca.yml                     exists=yes workflow_call=yes
.github/workflows/secret-scan.yml             exists=yes workflow_call=yes
.github/workflows/semgrep.yml                 exists=yes workflow_call=yes
.github/workflows/sonarqube.yml               exists=yes workflow_call=yes
```

### Hardening (contagens identicas as de antes da migracao)

```
harden-runner              = 10 (esperado 10)
runs-on: ubuntu-latest     = 10 (esperado 10)
actions/checkout@          = 12 (esperado 12)
persist-credentials: false = 12 (esperado 12)
no mutable @vN pins OK
```

### Injection (learning W-3)

```
no ${{ on run: lines OK
run blocks containing ${{ }}: NONE      # varredura via yaml.safe_load em todos os 11 arquivos
grep -c "inputs\." .github/workflows/sonarqube.yml -> 3
60:          SONAR_PR_KEY: ${{ inputs.pr-key }}
61:          SONAR_PR_BRANCH: ${{ inputs.pr-branch }}
62:          SONAR_PR_BASE: ${{ inputs.pr-base }}
```

O bloco `run:` novo do `Begin SonarQube analysis` passou por `bash -n` (SYNTAX OK) e por dry-run
com stub nos dois modos: com `SONAR_PR_KEY` preenchido emite os 3 `/d:sonar.pullrequest.*`; vazio,
emite so os 5 argumentos de branch — nenhum glob expandido, token so via `$SONAR_TOKEN`.

### Artifacts unicos (D-2026-07-28-pipeline-unificada-6a)

```
.github/workflows/ci.yml-35-          name: coverage
.github/workflows/sbom.yml-45-          name: sbom-spdx
```

Dois nomes, sem colisao sob o `run_id` agora compartilhado.

### Intocados

```
git diff --name-only main...HEAD -- .github/workflows/scorecard.yml .github/workflows/release.yml
# (vazio)
```

## Check names capturados (T-7)

Um unico run criado pela PR: `30444846178 Pipeline [pull_request]`. Nomes lidos de
`gh api repos/slipalison/TranslateReader/commits/74b9347c8151075a16ee1b30211e79d63f3577e3/check-runs`
(nenhum escrito a mao) — 10 linhas em `check-names-after.txt`.

| context ANTES (required) | check name DEPOIS |
|---|---|
| `Test (Linux)` | `CI / Test (Linux)` |
| `Build (Windows)` | `CI / Build (Windows)` |
| `CodeQL` | `CodeQL / Analyze C#` |
| `Semgrep SAST` | `Semgrep / Semgrep SAST` |
| `Dependency vulnerability gate` | `SCA / Dependency vulnerability gate` |
| `Gitleaks` | `Secret Scan / Gitleaks` |
| `TruffleHog` | `Secret Scan / TruffleHog` |
| `SonarQube Cloud scan` | `SonarQube / SonarQube Cloud scan` |
| `Dependency review` | `Dependency Review / Dependency review` |
| — (nao required) | `SBOM` (caller skipped em PR; em push vira `SBOM / Generate SBOM (Syft)`) |

O PATCH pronto, a verificacao pos-remap e o rollback estao em `branch-protection-remap.md`.
**Nada foi executado:** o remap e o merge sao `Deferred to PR review`, na ordem travada
PR -> nomes reais -> PATCH -> merge (D-2026-07-28-pipeline-unificada-1d).

## Desvios

1. **`-F strict=true` em vez de `-f strict=true`** no comando do remap (PLAN T-7 cita `-f`
   literalmente). Com `-f` o `gh` envia a string `"true"` e o endpoint responde 422, porque
   `strict` e boolean. Desvio de 1 caractere, corrige um comando que falharia; anotado tambem
   dentro do proprio `branch-protection-remap.md`.
2. **T-6 sem commit proprio.** O PLAN define `files modified: nenhum` e nenhuma validacao falhou,
   entao nao houve o que corrigir/commitar — a evidencia vive neste SUMMARY.
3. **Dois `git push`, nenhum no meio da migracao.** O primeiro e o da T-7 (exigido pela ordem
   travada: push -> PR -> captura). O segundo carrega os commits documentais posteriores
   (`00ae5cd` + PLAN/SUMMARY). A regra 1 do PLAN — nao empurrar estado meio-migrado — foi
   respeitada: T-2..T-5 ficaram locais ate o pipeline estar completo.
4. **`SBOM` aparece sem sufixo** na lista de check names. Nao e bug: o caller e
   `if: github.event_name == 'push'` e um job caller skipped reporta com o nome do caller. Nao e
   required context (nunca foi) e nao deve virar um, senao travaria toda PR.

## Notas

- Cobertura: **SKIPPED** — phase infra-only, nenhum `.cs` novo ou alterado, D-6 nao se aplica
  (mesmo padrao de `ci-seguranca` e `sast-sca-sbom`).
- `dotnet build` / `dotnet test` nao rodaram: nenhum arquivo de codigo foi tocado; o gate real de
  build/test roda dentro da propria PR #7 via `Pipeline / CI`.
- Estado transitorio esperado na PR #7: os 9 contexts antigos ficam "Expected — waiting for status
  to be reported" ate o PATCH do remap. Nao e falha.
- `npx jdi-cli` nao foi usado em nenhum passo (quebrado neste ambiente Windows).
