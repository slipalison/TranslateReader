# Remap do branch protection de `main` — pipeline-unificada

**Status: NAO EXECUTADO.** Este arquivo so prepara o comando. A execucao do `PATCH` e o merge
estao em `## Deferred to PR review` do CONTEXT.md e seguem a ordem travada em
D-2026-07-28-pipeline-unificada-1(d):

```
push do branch -> abrir PR -> capturar check names REAIS -> PATCH protection -> merge
```

Inverter essa ordem foi o incidente de 2026-07-28 (4 contexts com nome errado travaram todos os PRs).

- PR: https://github.com/slipalison/TranslateReader/pull/7 (`jdi/pipeline-unificada` -> `main`)
- Head SHA usado na captura: `74b9347c8151075a16ee1b30211e79d63f3577e3`
- Baseline (antes de qualquer edicao): `branch-protection-before.json`
- Nomes reais capturados via API (nenhum escrito a mao): `check-names-after.txt`

## Por que os nomes mudam

Com `workflow_call`, o check de um job aninhado passa a se chamar
`<nome do job caller no pipeline.yml> / <nome do job dentro do reusable>`.
Nenhum job interno foi renomeado — so ganhou o prefixo do caller.

## Contexts ANTES (9) -> DEPOIS

Fonte: `branch-protection-before.json` (`required_status_checks.contexts`) e `check-names-after.txt`.

| # | context ANTES (required hoje) | check name DEPOIS | app_id |
|---|---|---|---|
| 1 | `Test (Linux)` | `CI / Test (Linux)` | 15368 |
| 2 | `Build (Windows)` | `CI / Build (Windows)` | 15368 |
| 3 | `CodeQL` | `CodeQL / Analyze C#` | 57789 |
| 4 | `Semgrep SAST` | `Semgrep / Semgrep SAST` | 15368 |
| 5 | `Dependency vulnerability gate` | `SCA / Dependency vulnerability gate` | 15368 |
| 6 | `Gitleaks` | `Secret Scan / Gitleaks` | 15368 |
| 7 | `TruffleHog` | `Secret Scan / TruffleHog` | 15368 |
| 8 | `SonarQube Cloud scan` | `SonarQube / SonarQube Cloud scan` | 15368 |
| 9 | `Dependency review` | `Dependency Review / Dependency review` | 15368 |

Outros valores do bloco preservados: `strict: true`, `enforce_admins: false` — o `PATCH` abaixo
mexe SO em `required_status_checks`, o resto da protection (reviews, linear history, force pushes)
fica intacto.

### Check names capturados que NAO viram required

- `SBOM` — o job caller `sbom` e `if: github.event_name == 'push'`, entao numa PR ele reporta
  como skipped com o nome do caller (`SBOM`), sem o sufixo do job interno. Num push pra `main` o
  nome sera `SBOM / Generate SBOM (Syft)`. Nunca foi required (nao esta nos 9 de antes) e deve
  continuar assim — exigir um check que so existe em `push` travaria toda PR.

## Estado transitorio esperado (nao e falha)

Ate o `PATCH` rodar, a PR #7 mostra os 9 contexts antigos como
**"Expected — waiting for status to be reported"**, porque nenhum workflow reporta mais com
aqueles nomes. Os 10 checks novos rodam normalmente ao lado. O deadlock e esperado e some
assim que o remap for aplicado.

## Comando pronto (executar SO na revisao da PR)

```bash
gh api -X PATCH repos/slipalison/TranslateReader/branches/main/protection/required_status_checks \
  -F strict=true \
  -f 'contexts[]=CI / Test (Linux)' \
  -f 'contexts[]=CI / Build (Windows)' \
  -f 'contexts[]=CodeQL / Analyze C#' \
  -f 'contexts[]=Semgrep / Semgrep SAST' \
  -f 'contexts[]=SCA / Dependency vulnerability gate' \
  -f 'contexts[]=Secret Scan / Gitleaks' \
  -f 'contexts[]=Secret Scan / TruffleHog' \
  -f 'contexts[]=SonarQube / SonarQube Cloud scan' \
  -f 'contexts[]=Dependency Review / Dependency review'
```

`-F strict=true` (typed) e nao `-f strict=true`: com `-f` o gh manda a string `"true"` e o
endpoint responde 422 (`strict` e boolean). Unico desvio em relacao ao literal do PLAN.

### Verificacao pos-PATCH

```bash
gh api repos/slipalison/TranslateReader/branches/main/protection/required_status_checks \
  --jq '.contexts[]' | sort -u > /tmp/contexts-after.txt
diff <(sort -u /tmp/contexts-after.txt) \
     <(grep -v '^SBOM$' .jdi/phases/pipeline-unificada/check-names-after.txt) && echo "REMAP OK"
```

Espera-se diff vazio: os 9 required novos == os 10 checks capturados menos `SBOM`.

### Rollback

```bash
gh api -X PATCH repos/slipalison/TranslateReader/branches/main/protection/required_status_checks \
  -F strict=true \
  -f 'contexts[]=CodeQL' \
  -f 'contexts[]=Semgrep SAST' \
  -f 'contexts[]=SonarQube Cloud scan' \
  -f 'contexts[]=Dependency review' \
  -f 'contexts[]=Gitleaks' \
  -f 'contexts[]=TruffleHog' \
  -f 'contexts[]=Test (Linux)' \
  -f 'contexts[]=Build (Windows)' \
  -f 'contexts[]=Dependency vulnerability gate'
```

Restaura exatamente os contexts do `branch-protection-before.json`. So faz sentido junto com um
revert da PR #7 — com o pipeline unificado no `main`, esses nomes nunca mais sao reportados.
