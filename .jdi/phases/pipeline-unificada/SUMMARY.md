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
| — | `PLAN.md` com as 7 tasks `completed` + primeira versao deste SUMMARY | `2c144a6` |
| T-7 (cont.) | Recaptura dos check names apos o run completo e correcao do remap (`CodeQL` nao muda de nome) | commit final |

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

## T-7 — run real e check names

Run graph unico confirmado: **um** run por evento (`gh run list --branch jdi/pipeline-unificada`
-> `Pipeline [pull_request]`), com os 13 checks apontando todos pro mesmo `runs/30445084653`.

Resultado do run (`gh pr checks 7`) — todos os jobs do Actions verdes:

```
CI / Test (Linux)                     pass  32s
CI / Build (Windows)                  pass  7m35s
CodeQL / Analyze C#                   pass  1m51s
Semgrep / Semgrep SAST                pass  39s
SCA / Dependency vulnerability gate   pass  1m1s
Secret Scan / Gitleaks                pass  16s
Secret Scan / TruffleHog              pass  20s
SonarQube / SonarQube Cloud scan      pass  1m23s
Dependency Review / Dependency review pass  14s
SBOM                                  skipping   (if: push - correto em PR)
CodeQL                    (app)       pass  3s
SonarCloud Code Analysis  (app)       pass  29s
Semgrep OSS               (app)       FAIL  4s   -> ver "Achados pro revisor"
```

Prova de que o `workflow_call` funciona de fato, nao so no YAML: o SonarQube rodou em modo PR com
os inputs explicitos e o `dependency-review` (que exige base/head SHA reais de PR) passou.

### Mapa de check names (capturado, nunca escrito a mao)

`check-names-after.txt` = saida literal de
`gh api repos/slipalison/TranslateReader/commits/<head>/check-runs --jq '.check_runs[].name' | sort -u`,
recapturada depois que TODOS os checks reportaram (13 nomes).

| context ANTES (required) | app_id | check name DEPOIS |
|---|---|---|
| `CodeQL` | 57789 (github-advanced-security) | `CodeQL` — **INALTERADO** |
| `Test (Linux)` | 15368 (github-actions) | `CI / Test (Linux)` |
| `Build (Windows)` | 15368 | `CI / Build (Windows)` |
| `Semgrep SAST` | 15368 | `Semgrep / Semgrep SAST` |
| `Dependency vulnerability gate` | 15368 | `SCA / Dependency vulnerability gate` |
| `Gitleaks` | 15368 | `Secret Scan / Gitleaks` |
| `TruffleHog` | 15368 | `Secret Scan / TruffleHog` |
| `SonarQube Cloud scan` | 15368 | `SonarQube / SonarQube Cloud scan` |
| `Dependency review` | 15368 | `Dependency Review / Dependency review` |
| — (nao required) | 15368 | `SBOM` (skipped em PR; em push vira `SBOM / Generate SBOM (Syft)`) |
| — (nao required) | 57789 / 12526 | `Semgrep OSS`, `SonarCloud Code Analysis` — checks de app, nome inalterado |

O prefixo `<caller> / <job>` vale so pros checks do Actions. Confirmado contra `main` pre-migracao
(`e5541f2`), onde os mesmos jobs reportavam como `Analyze C#`, `Test (Linux)`, `Build (Windows)`...

O PATCH pronto (variante que preserva `app_id`), a verificacao pos-remap e o rollback estao em
`branch-protection-remap.md`. **Nada foi executado:** remap e merge sao `Deferred to PR review`,
na ordem travada PR -> nomes reais -> PATCH -> merge (D-2026-07-28-pipeline-unificada-1d).

## Achados pro revisor

1. **`Semgrep OSS` vermelho: 1 alerta novo em `pipeline.yml:59`** —
   `yaml.github-actions.security.secrets-inherit`, regra de registry contra `secrets: inherit`,
   sugerindo `secrets: { SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }} }`.
   **Nao alterado de proposito:** `secrets: inherit` e decisao locked
   (D-2026-07-28-pipeline-unificada-4) e o DoD 5 exige literalmente
   `grep -c "secrets: inherit" == 1` — trocar reprovaria o DoD e reabriria decisao travada, fora
   da alcada do doer. Atenuantes: o reusable chamado e local (`./`), nao de terceiro; `Semgrep OSS`
   nao e required context; o job que gateia (`Semgrep / Semgrep SAST`, regras custom `.semgrep/`)
   passou verde. Pra adotar o map explicito: alterar `pipeline.yml` + emendar a decisao -4 +
   ajustar o DoD 5.
2. **`CodeQL` (required) nao precisa de remap.** Descoberto ao ler o `app_id` no snapshot: e o
   agregado de code scanning do app `github-advanced-security` (57789), nao um job do Actions. O
   job do Actions era `Analyze C#` (nunca required) e virou `CodeQL / Analyze C#`. Logo o remap
   mexe em 8 dos 9 contexts, nao nos 9 — o `branch-protection-remap.md` ja reflete isso.

## Desvios

1. **`-F strict=true` em vez de `-f strict=true`** no comando do remap (PLAN T-7 cita `-f`
   literalmente). Com `-f` o `gh` envia a string `"true"` e o endpoint responde 422, porque
   `strict` e boolean. Alem disso o `branch-protection-remap.md` traz como variante recomendada um
   `--input` JSON que preserva o `app_id` de cada context: `contexts[]` grava `app_id: null` e
   afrouxa o pin de app (regressao pequena de hardening, mas evitavel).
2. **T-6 sem commit proprio.** O PLAN define `files modified: nenhum` e nenhuma validacao falhou,
   entao nao houve o que corrigir/commitar — a evidencia vive neste SUMMARY.
3. **Tres `git push`, nenhum no meio da migracao.** O primeiro e o da T-7 (exigido pela ordem
   travada: push -> PR -> captura); os outros dois carregam commits documentais (`00ae5cd`,
   `2c144a6` e a correcao do remap). A regra 1 do PLAN — nao empurrar estado meio-migrado — foi
   respeitada: T-2..T-5 ficaram locais ate o pipeline estar completo. Efeito colateral conhecido:
   cada push dispara um run novo e o `concurrency` cancela o anterior (comportamento desejado).
4. **`check-names-after.txt` recapturado** depois que os checks de app reportaram — a primeira
   captura tinha so os 10 nomes do Actions, a final tem 13. Foi a recaptura que revelou o achado 2.
   As definicoes de workflow nao mudaram entre as duas capturas
   (`git diff --name-only 74b9347 HEAD -- .github/` vazio).
5. **`SBOM` aparece sem sufixo** na lista de check names. Nao e bug: o caller e
   `if: github.event_name == 'push'` e um job caller skipped reporta com o nome do caller. Nao e
   required context (nunca foi) e nao deve virar um, senao travaria toda PR.

## Notas

- Cobertura: **SKIPPED** — phase infra-only, nenhum `.cs` novo ou alterado, D-6 nao se aplica
  (mesmo padrao de `ci-seguranca` e `sast-sca-sbom`).
- `dotnet build` / `dotnet test` locais nao rodaram: nenhum arquivo de codigo foi tocado; o gate
  real rodou na propria PR #7 (`CI / Test (Linux)` e `CI / Build (Windows)` verdes).
- Estado transitorio esperado na PR #7: os 8 contexts antigos do Actions ficam "Expected — waiting
  for status to be reported" ate o PATCH do remap. Nao e falha. (`CodeQL` nao entra nesse limbo.)
- `npx jdi-cli` nao foi usado em nenhum passo (quebrado neste ambiente Windows).
