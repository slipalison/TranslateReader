# Remap do branch protection de `main` — cobertura-e-ci

**Status: NAO EXECUTADO.** Este arquivo so prepara o comando. A mutacao real e o `PATCH` estao em
`## Deferred to PR review` do CONTEXT.md e seguem a ordem travada por
D-2026-07-28-pipeline-unificada-1(d), reafirmada aqui por D-2026-08-08-cobertura-e-ci-5:

```
push do branch -> abrir PR -> capturar o check name REAL via `gh api .../check-runs` -> PATCH -> merge
```

Inverter essa ordem foi o incidente de 2026-07-28 (4 contexts com nome errado travaram todos os
PRs, arquivado em `.jdi/archive/pipeline-unificada/branch-protection-remap.md`). Nao se repete
aqui: nenhum dos dois candidatos abaixo entra no `PATCH` sem antes ser confirmado contra o run
real.

- Baseline (antes de qualquer edicao desta phase): `branch-protection-before.json`, capturado em
  2026-08-09 via `gh api repos/slipalison/TranslateReader/branches/main/protection` — nada em
  T-1..T-5 mutou o objeto de protection, entao este snapshot ainda vale como "antes".
- Nomes reais pos-1o-run: a capturar depois que a PR desta phase rodar (`gh api
  repos/slipalison/TranslateReader/commits/<sha>/check-runs`), ainda nao existe.

## O conflito que este doer nao pode esconder

O novo job de cobertura (`coverage:` em `pipeline.yml`, chamando `coverage.yml`) produz DOIS
literais candidatos para o required context, dependendo de qual regra de derivacao se aplica —
**e os dois nao podem estar certos ao mesmo tempo**:

### Candidato A — derivacao de D-2026-08-08-cobertura-e-ci-5(4b)

`name:` do workflow orquestrador (`pipeline.yml`, topo do arquivo) + ` / ` + `name:` do job
caller (`coverage:` dentro de `pipeline.yml`):

```
Pipeline / Coverage
```

Este e o literal exigido pelo DoD 9 desta phase (`grep -qF 'Pipeline / Coverage'` neste arquivo).

### Candidato B — evidencia empirica deste MESMO repo (`pipeline-unificada`)

`.jdi/archive/pipeline-unificada/branch-protection-remap.md` documenta, com prova em produção
(PR #7, `main` pre-migracao `e5541f2`), que o GitHub Actions NAO usa o `name:` do topo do
workflow orquestrador como prefixo. A regra observada e:

```
<name: do job CALLER no orquestrador> / <name: do job DENTRO do reusable>
```

Exemplo real ja em `main`: o job caller `ci:` tem `name: CI` em `pipeline.yml`; o job dentro de
`ci.yml` tem `name: Test (Linux)`; o check reportado e `CI / Test (Linux)` — o `name: Pipeline`
do topo do orquestrador **nao aparece em lugar nenhum**. O `before.json` capturado antes da
migracao de `pipeline-unificada` e a prova: os contexts eram `Test (Linux)`, `Build (Windows)`,
etc. — nomes crus dos jobs, sem NENHUM prefixo de workflow orquestrador.

Aplicando a mesma regra ao job de cobertura desta phase — job caller `coverage:` com
`name: Coverage` em `pipeline.yml`, job dentro de `coverage.yml` com `name: Coverage gate` —
o candidato empirico e:

```
Coverage / Coverage gate
```

### Por que nao dá pra decidir aqui

Candidato A vem de uma leitura literal de D-...-5(4b) que a PR original *pediu* explicitamente
(e por isso o DoD 9 desta phase testa exatamente esse literal contra a ESTRUTURA do YAML — nao
contra o runtime do GitHub). Candidato B vem de observar o que o GitHub Actions realmente faz,
com prova em produção neste mesmo repositorio. Um workflow `name:` de topo nunca aparecer em
nenhum check reportado (todos os exemplos capturados em `pipeline-unificada` confirmam isso) é
motivo forte para esperar Candidato B, nao A, quando a PR desta phase rodar de verdade.

**A resposta correta e nao adivinhar.** O `PATCH` abaixo so roda depois que
`gh api repos/slipalison/TranslateReader/commits/<sha>/check-runs` mostrar qual dos dois (ou um
terceiro, se a regra tiver mais uma nuance) o GitHub realmente reportou. Digitar qualquer um dos
dois de memoria no `PATCH` e literalmente o incidente D-2026-07-28-pipeline-unificada-1(d).

## Contexts ANTES (9) -> DEPOIS (10)

Fonte: `branch-protection-before.json` (`required_status_checks.checks`, com `app_id`). Nenhum
dos 9 contexts existentes muda — este job e uma ADICAO, nao um remap dos outros 9.

| # | context (inalterado) | app_id |
|---|---|---|
| 1 | `CodeQL` | 57789 |
| 2 | `CI / Test (Linux)` | 15368 |
| 3 | `CI / Build (Windows)` | 15368 |
| 4 | `Semgrep / Semgrep SAST` | 15368 |
| 5 | `SCA / Dependency vulnerability gate` | 15368 |
| 6 | `Secret Scan / Gitleaks` | 15368 |
| 7 | `Secret Scan / TruffleHog` | 15368 |
| 8 | `SonarQube / SonarQube Cloud scan` | 15368 |
| 9 | `Dependency Review / Dependency review` | 15368 |
| 10 (**NOVO**, nome a confirmar) | `Pipeline / Coverage` OU `Coverage / Coverage gate` — ver conflito acima | 15368 (GitHub Actions, mesmo app dos outros 8 jobs de Actions) |

Resto da protection (`strict: true`, `enforce_admins: false`, reviews, linear history, force
pushes) nao e tocado — o `PATCH` abaixo atinge so `required_status_checks`.

## Comando pronto (executar SO na revisao da PR, apos capturar o nome real)

Variante `checks[]` com `app_id` (preserva o pin de app — a variante `contexts[]}` grava
`app_id: null` em todos e afrouxa o pin, D-2026-07-28-pipeline-unificada-1). `-F strict=true`
tipado, nunca `-f strict=true` (com `-f` o gh manda a string `"true"` e o endpoint responde 422,
`strict` e boolean):

```bash
# 1. Capturar o nome real DEPOIS que o 1o run da PR terminar:
gh api repos/slipalison/TranslateReader/commits/<PR_HEAD_SHA>/check-runs \
  --jq '.check_runs[] | select(.name | contains("Coverage")) | .name'

# 2. Substituir <CAPTURED_NAME> abaixo pelo valor exato retornado acima (nao adivinhar).
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
    { "context": "Secret Scan / TruffleHog",               "app_id": 15368 },
    { "context": "SonarQube / SonarQube Cloud scan",      "app_id": 15368 },
    { "context": "Dependency Review / Dependency review", "app_id": 15368 },
    { "context": "<CAPTURED_NAME>",                        "app_id": 15368 }
  ]
}
JSON
```

### Verificacao pos-PATCH

```bash
gh api repos/slipalison/TranslateReader/branches/main/protection/required_status_checks \
  --jq '.checks[] | "\(.context) :: \(.app_id)"' | sort
```

Esperado: as mesmas 9 linhas de `before.json` (inalteradas) + 1 linha nova com o nome capturado
no passo 1 e `app_id 15368`.

### Rollback

Restaura exatamente `required_status_checks.checks` de `branch-protection-before.json` — os
mesmos 9 contexts, remove o 10o:

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
    { "context": "Secret Scan / TruffleHog",               "app_id": 15368 },
    { "context": "SonarQube / SonarQube Cloud scan",      "app_id": 15368 },
    { "context": "Dependency Review / Dependency review", "app_id": 15368 }
  ]
}
JSON
```

So faz sentido junto com um revert da PR desta phase — enquanto `coverage.yml` continuar em
`main`, o job de cobertura roda de qualquer forma; so deixa de ser *required*.
