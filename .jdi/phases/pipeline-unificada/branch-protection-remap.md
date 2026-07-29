# Remap do branch protection de `main` — pipeline-unificada

**Status: NAO EXECUTADO.** Este arquivo so prepara o comando. A execucao do `PATCH` e o merge
estao em `## Deferred to PR review` do CONTEXT.md e seguem a ordem travada em
D-2026-07-28-pipeline-unificada-1(d):

```
push do branch -> abrir PR -> capturar check names REAIS -> PATCH protection -> merge
```

Inverter essa ordem foi o incidente de 2026-07-28 (4 contexts com nome errado travaram todos os PRs).

- PR: https://github.com/slipalison/TranslateReader/pull/7 (`jdi/pipeline-unificada` -> `main`)
- Baseline (antes de qualquer edicao): `branch-protection-before.json`
- Nomes reais capturados via API, com todos os 13 checks ja reportados: `check-names-after.txt`

## Por que os nomes mudam

Com `workflow_call`, o check de um job aninhado passa a se chamar
`<nome do job caller no pipeline.yml> / <nome do job dentro do reusable>`.
Nenhum job interno foi renomeado — so ganhou o prefixo do caller.

**Isso vale so para checks do GitHub Actions (`app_id: 15368`).** Checks reportados por GitHub Apps
nao mudam de nome, porque nao sao jobs do Actions:

| check | app | muda? |
|---|---|---|
| `CodeQL` | `github-advanced-security` (57789) | **NAO** — agregado de code scanning, nao e job |
| `Semgrep OSS` | `github-advanced-security` (57789) | NAO — agregado do SARIF do Semgrep |
| `SonarCloud Code Analysis` | `sonarqubecloud` (12526) | NAO — check do proprio SonarCloud |

Confirmado empiricamente: em `main` pre-migracao (`e5541f2`) os checks do Actions eram
`Analyze C#`, `Test (Linux)`, `Build (Windows)`, ... (nomes crus dos jobs); na PR #7 sao
`CodeQL / Analyze C#`, `CI / Test (Linux)`, `CI / Build (Windows)`, ... enquanto `CodeQL` (app)
continua identico e verde.

## Contexts ANTES (9) -> DEPOIS

Fonte: `branch-protection-before.json` (`required_status_checks.checks`, com `app_id`) e
`check-names-after.txt`.

| # | context ANTES (required hoje) | app_id | check name DEPOIS |
|---|---|---|---|
| 1 | `CodeQL` | 57789 | `CodeQL` — **INALTERADO** (nao remapear) |
| 2 | `Test (Linux)` | 15368 | `CI / Test (Linux)` |
| 3 | `Build (Windows)` | 15368 | `CI / Build (Windows)` |
| 4 | `Semgrep SAST` | 15368 | `Semgrep / Semgrep SAST` |
| 5 | `Dependency vulnerability gate` | 15368 | `SCA / Dependency vulnerability gate` |
| 6 | `Gitleaks` | 15368 | `Secret Scan / Gitleaks` |
| 7 | `TruffleHog` | 15368 | `Secret Scan / TruffleHog` |
| 8 | `SonarQube Cloud scan` | 15368 | `SonarQube / SonarQube Cloud scan` |
| 9 | `Dependency review` | 15368 | `Dependency Review / Dependency review` |

Continuam 9 required contexts: 8 renomeados + `CodeQL` intocado. O resto da protection
(`strict: true`, `enforce_admins: false`, reviews, linear history, force pushes) nao e tocado —
o `PATCH` abaixo atinge so `required_status_checks`.

### Checks capturados que NAO devem virar required

- `SBOM` — o caller e `if: github.event_name == 'push'`, entao em PR reporta skipped com o nome do
  caller (sem sufixo). Em push pra `main` vira `SBOM / Generate SBOM (Syft)`. Exigir um check que
  so existe em `push` travaria toda PR.
- `CodeQL / Analyze C#` — o job do Actions. O gate real de code scanning ja e o context `CodeQL`
  (app), que continua required. Adicionar tambem o job e possivel, mas e endurecimento novo, fora
  do escopo desta phase.
- `Semgrep OSS`, `SonarCloud Code Analysis` — checks de app, nunca foram required. Ver a nota sobre
  `Semgrep OSS` abaixo.

## Estado transitorio esperado (nao e falha)

Ate o `PATCH` rodar, a PR #7 mostra os 8 contexts antigos do Actions como
**"Expected — waiting for status to be reported"**, porque nenhum workflow reporta mais com
aqueles nomes. Os checks novos rodam normalmente ao lado. O deadlock e esperado e some assim que
o remap for aplicado. (`CodeQL` nao entra nesse limbo: continua sendo reportado.)

## Comando pronto (executar SO na revisao da PR)

Variante recomendada — preserva o pin de `app_id` de cada context (so o app dono pode reportar
aquele check; sem isso qualquer app poderia satisfazer o gate):

```bash
gh api -X PATCH repos/slipalison/TranslateReader/branches/main/protection/required_status_checks \
  --input - <<'JSON'
{
  "strict": true,
  "checks": [
    { "context": "CodeQL",                                "app_id": 57789 },
    { "context": "CI / Test (Linux)",                     "app_id": 15368 },
    { "context": "CI / Build (Windows)",                  "app_id": 15368 },
    { "context": "Semgrep / Semgrep SAST",                "app_id": 15368 },
    { "context": "SCA / Dependency vulnerability gate",   "app_id": 15368 },
    { "context": "Secret Scan / Gitleaks",                "app_id": 15368 },
    { "context": "Secret Scan / TruffleHog",              "app_id": 15368 },
    { "context": "SonarQube / SonarQube Cloud scan",      "app_id": 15368 },
    { "context": "Dependency Review / Dependency review", "app_id": 15368 }
  ]
}
JSON
```

Variante simples (a do PLAN, via `contexts[]`) — funciona, mas grava `app_id: null` em todos,
afrouxando o pin de app:

```bash
gh api -X PATCH repos/slipalison/TranslateReader/branches/main/protection/required_status_checks \
  -F strict=true \
  -f 'contexts[]=CodeQL' \
  -f 'contexts[]=CI / Test (Linux)' \
  -f 'contexts[]=CI / Build (Windows)' \
  -f 'contexts[]=Semgrep / Semgrep SAST' \
  -f 'contexts[]=SCA / Dependency vulnerability gate' \
  -f 'contexts[]=Secret Scan / Gitleaks' \
  -f 'contexts[]=Secret Scan / TruffleHog' \
  -f 'contexts[]=SonarQube / SonarQube Cloud scan' \
  -f 'contexts[]=Dependency Review / Dependency review'
```

`-F strict=true` (typed) e nao `-f strict=true`: com `-f` o gh manda a string `"true"` e o endpoint
responde 422, porque `strict` e boolean.

### Verificacao pos-PATCH

```bash
gh api repos/slipalison/TranslateReader/branches/main/protection/required_status_checks \
  --jq '.checks[] | "\(.context) :: \(.app_id)"' | sort
```

Esperado: as mesmas 9 linhas da tabela acima (8 renomeadas + `CodeQL` com app 57789).

### Rollback

```bash
gh api -X PATCH repos/slipalison/TranslateReader/branches/main/protection/required_status_checks \
  --input - <<'JSON'
{
  "strict": true,
  "checks": [
    { "context": "CodeQL",                        "app_id": 57789 },
    { "context": "Semgrep SAST",                  "app_id": 15368 },
    { "context": "SonarQube Cloud scan",          "app_id": 15368 },
    { "context": "Dependency review",             "app_id": 15368 },
    { "context": "Gitleaks",                      "app_id": 15368 },
    { "context": "TruffleHog",                    "app_id": 15368 },
    { "context": "Test (Linux)",                  "app_id": 15368 },
    { "context": "Build (Windows)",               "app_id": 15368 },
    { "context": "Dependency vulnerability gate", "app_id": 15368 }
  ]
}
JSON
```

Restaura exatamente o `required_status_checks.checks` de `branch-protection-before.json`. So faz
sentido junto com um revert da PR #7 — com o pipeline unificado em `main`, os nomes crus dos jobs
nunca mais sao reportados.

## Nota: `Semgrep OSS` vermelho na PR #7 (1 alerta novo)

`yaml.github-actions.security.secrets-inherit` em `.github/workflows/pipeline.yml:59` —
regra de registry do Semgrep contra `secrets: inherit`, sugerindo o map explicito
`secrets: { SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }} }`.

Nao foi alterado nesta phase, de proposito: `secrets: inherit` no job do sonar e **decisao locked**
(D-2026-07-28-pipeline-unificada-4) e o DoD 5 exige literalmente
`grep -c "secrets: inherit" pipeline.yml == 1`. Trocar pelo map explicito reprovaria o DoD e
reabriria uma decisao travada — fora da alcada do doer.

Contexto pro revisor decidir: o reusable chamado e local (`./.github/workflows/sonarqube.yml`), nao
de terceiro, o que enfraquece o modelo de ameaca da regra; mesmo assim o map explicito seria
estritamente menos privilegiado. `Semgrep OSS` nao e required context e o job que gateia
(`Semgrep / Semgrep SAST`, regras custom `.semgrep/`) passou verde. Se o revisor preferir o map
explicito, e alterar `pipeline.yml` + emendar D-2026-07-28-pipeline-unificada-4 + ajustar o DoD 5.
