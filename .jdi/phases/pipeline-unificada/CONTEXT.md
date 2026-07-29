# Phase 9: Pipeline unificada (orquestrador reusable) — Context  (slug: pipeline-unificada)

## Goal
Consolidar os fluxos de push/PR num orquestrador unico `pipeline.yml` via `workflow_call` — um run
graph com build, testes, CodeQL, Semgrep, SCA, secrets, Sonar, dependency-review e SBOM;
`scorecard.yml` e `release.yml` permanecem isolados; branch protection re-mapeada pros novos nomes
de check.

## Locked decisions
(texto completo de cada uma em `.jdi/DECISIONS.md`)
- D-2026-07-28-pipeline-unificada-1: escopo geral (scorecard/release isolados, hibrido
  schedule+workflow_call, path local sem SHA, remap de check-name obrigatorio na fase, concurrency
  unica) — ja locked, nao redecidido aqui.
- D-2026-07-28-pipeline-unificada-2: trigger surface completo por arquivo — remove push/pull_request
  dos 8 reusables (fecha o double-run), adiciona `workflow_call` (nenhum tinha hoje), corrige
  `workflow_dispatch` por arquivo, sem rename.
- D-2026-07-28-pipeline-unificada-3: matriz de permissions por job caller em `pipeline.yml`
  (contents:read no top-level; elevacoes pontuais so onde o reusable ja precisa).
- D-2026-07-28-pipeline-unificada-4: secrets nao fluem implicito — `secrets: inherit` so no job do
  sonar; PR-context do SonarCloud via `workflow_call: inputs:` explicitos, nao via `github.event_name`
  implicito dentro do reusable.
- D-2026-07-28-pipeline-unificada-5: jobs condicionais (dependency-review, sbom) tem `if:` no job
  CALLER dentro de `pipeline.yml`, nunca dentro do proprio reusable.
- D-2026-07-28-pipeline-unificada-6: guardrail de nomes de artifact unicos (run_id compartilhado) +
  snapshot do branch protection atual ANTES de qualquer edicao.
- D-2026-07-28-pipeline-unificada-7: **supersede a clausula `secrets: inherit` da D-...-4** — o job
  caller do sonar passa `SONAR_TOKEN` explicitamente e `sonarqube.yml` declara o secret em
  `on.workflow_call.secrets`. O restante da D-...-4 (inputs explicitos de PR-context) segue valendo.
  Reescreve o DoD 5 abaixo.

## Canonical refs
- Card colado via `/jdi-issue` (sem URL/ID externo — 9 fatos tecnicos listados no dispatch, todos
  verificados contra os 10 arquivos reais abaixo antes de virar decisao).
- Os 10 workflows atuais, lidos por completo: `.github/workflows/{ci,codeql,dependency-review,
  scorecard,sonarqube,release,sbom,sca,secret-scan,semgrep}.yml`.
- `.jdi/phases/ci-seguranca/SUMMARY.md` e `.jdi/phases/sast-sca-sbom/SUMMARY.md` — SHA pins,
  convencao de hardening e comando de validacao YAML (`python -c "import yaml; yaml.safe_load(...)"`,
  actionlint indisponivel no ambiente) ja resolvidos, reutilizados aqui.
- GitHub Docs "Reuse workflows" (docs.github.com/en/actions/how-tos/reuse-automations/reuse-workflows)
  — pesquisa (1/2) confirmou que `secrets: inherit` funciona sem o reusable declarar
  `on.workflow_call.secrets`.
- Pesquisa (2/2, GitHub Actions runner issues/discussions) — `github.event_name` dentro de um job
  disparado por `workflow_call` resolve pra `"workflow_call"`, nao pro evento original do orquestrador.

## Out of scope
- `scorecard.yml` e `release.yml` — fora do orquestrador por limite tecnico real, ja locked em
  D-2026-07-28-pipeline-unificada-1(a).
- Renomear os 10 arquivos de workflow existentes — risco de novas categorias de code scanning.
- Novos scanners/validacoes de seguranca — ja entregues em `ci-seguranca`/`sast-sca-sbom`; aqui e so
  consolidacao estrutural (trigger/permissions/secrets/concurrency).
- `zizmor` — opcional, ja em `todos.md` desde a fase `ci-seguranca`.
- Logica interna de cada job (steps de build/test/scan) — so a superficie de trigger/permissions/
  secrets/concurrency muda.

## Definition of Done

### Auto-verifiable
- [ ] Double-run fechado: os 8 reusables perdem `push:`/`pull_request:`; `pipeline.yml` e o unico
      arquivo novo que os tem; `scorecard.yml` continua com o seu `push: branches: [main]` original
      **Verify:** `! grep -E "^\s*(push|pull_request):" .github/workflows/ci.yml .github/workflows/codeql.yml .github/workflows/dependency-review.yml .github/workflows/sonarqube.yml .github/workflows/sbom.yml .github/workflows/sca.yml .github/workflows/secret-scan.yml .github/workflows/semgrep.yml && grep -Eq "^\s*(push|pull_request):" .github/workflows/pipeline.yml && grep -q "branches: \[main\]" .github/workflows/scorecard.yml`
      **Source:** CONTEXT
- [ ] `workflow_call:` adicionado aos 8 reusables (nenhum tinha hoje); `pipeline.yml` existe como
      unico arquivo novo; os demais 3 (scorecard/release) continuam existindo com o mesmo nome
      **Verify:** `grep -q "workflow_call:" .github/workflows/ci.yml && grep -q "workflow_call:" .github/workflows/codeql.yml && grep -q "workflow_call:" .github/workflows/dependency-review.yml && grep -q "workflow_call:" .github/workflows/sonarqube.yml && grep -q "workflow_call:" .github/workflows/sbom.yml && grep -q "workflow_call:" .github/workflows/sca.yml && grep -q "workflow_call:" .github/workflows/secret-scan.yml && grep -q "workflow_call:" .github/workflows/semgrep.yml && test -f .github/workflows/pipeline.yml && test -f .github/workflows/scorecard.yml && test -f .github/workflows/release.yml`
      **Source:** CONTEXT
- [ ] `workflow_dispatch:` corrigido por arquivo: adicionado em codeql/semgrep/secret-scan (nao
      tinham), mantido em sca/sbom, removido de ci.yml, nunca adicionado em dependency-review.yml
      (a action precisa de base/head SHA real de PR, que nao existe num dispatch manual)
      **Verify:** `grep -q "workflow_dispatch:" .github/workflows/codeql.yml && grep -q "workflow_dispatch:" .github/workflows/semgrep.yml && grep -q "workflow_dispatch:" .github/workflows/secret-scan.yml && grep -q "workflow_dispatch:" .github/workflows/sca.yml && grep -q "workflow_dispatch:" .github/workflows/sbom.yml && ! grep -q "workflow_dispatch:" .github/workflows/ci.yml && ! grep -q "workflow_dispatch:" .github/workflows/dependency-review.yml`
      **Source:** CONTEXT
- [ ] Matriz de permissions por job caller em `pipeline.yml`: top-level `contents: read`; codeql e
      semgrep elevam `security-events: write`; sbom eleva `contents: write`; dependency-review eleva
      `pull-requests: write` — sem elevacao generica
      **Verify:** `grep -q "contents: read" .github/workflows/pipeline.yml && grep -B10 "codeql.yml" .github/workflows/pipeline.yml | grep -q "security-events: write" && grep -B10 "semgrep.yml" .github/workflows/pipeline.yml | grep -q "security-events: write" && grep -B10 "sbom.yml" .github/workflows/pipeline.yml | grep -q "contents: write" && grep -B10 "dependency-review.yml" .github/workflows/pipeline.yml | grep -q "pull-requests: write"`
      **Source:** CONTEXT
- [ ] Secrets nao fluem implicitamente (least privilege de verdade, nao por proxy): **zero**
      `secrets: inherit` em todo `.github/workflows/`; o job caller do sonar passa exclusivamente
      `SONAR_TOKEN`, de forma explicita; `sonarqube.yml` declara esse secret em
      `on.workflow_call.secrets` com `required: false` (para o no-op gracioso quando ausente)
      — emendado por D-2026-07-28-pipeline-unificada-7, que supersede a clausula `inherit` de
      D-2026-07-28-pipeline-unificada-4
      **Verify:** `! grep -rq "secrets: inherit" .github/workflows/ && test "$(grep -Fc 'SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}' .github/workflows/pipeline.yml)" = "1" && grep -F -B10 'SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}' .github/workflows/pipeline.yml | grep -q "sonarqube.yml" && python -c "import yaml,sys; d=yaml.safe_load(open('.github/workflows/sonarqube.yml',encoding='utf-8')); t=d.get('on', d.get(True)); sys.exit(0 if 'SONAR_TOKEN' in (t['workflow_call'].get('secrets') or {}) else 1)"`
      **Source:** CONTEXT
- [ ] Sonar: `on: workflow_call: inputs:` para contexto de PR explicito (nao depende de
      `github.event_name` implicito dentro do reusable); `fetch-depth: 0` do checkout intocado
      **Verify:** `grep -q "fetch-depth: 0" .github/workflows/sonarqube.yml && grep -A20 "workflow_call:" .github/workflows/sonarqube.yml | grep -q "inputs:"`
      **Source:** CONTEXT
- [ ] Jobs condicionais (dependency-review so em pull_request, sbom so em push) tem `if:
      github.event_name == ...` no job CALLER dentro de `pipeline.yml`; nunca dentro do reusable
      **Verify:** `grep -B5 "dependency-review.yml" .github/workflows/pipeline.yml | grep -q "if:.*pull_request" && grep -B5 "sbom.yml" .github/workflows/pipeline.yml | grep -q "if:.*push" && ! grep -q "event_name" .github/workflows/dependency-review.yml && ! grep -q "event_name" .github/workflows/sbom.yml`
      **Source:** CONTEXT
- [ ] `concurrency:` sai dos 8 reusables migrados; so `pipeline.yml`, `scorecard.yml` e
      `release.yml` mantem o bloco — sem risco de deadlock por colisao de group
      **Verify:** `! grep -l "concurrency:" .github/workflows/ci.yml .github/workflows/codeql.yml .github/workflows/dependency-review.yml .github/workflows/sonarqube.yml .github/workflows/sbom.yml .github/workflows/sca.yml .github/workflows/secret-scan.yml .github/workflows/semgrep.yml && grep -q "concurrency:" .github/workflows/pipeline.yml && grep -q "concurrency:" .github/workflows/scorecard.yml && grep -q "concurrency:" .github/workflows/release.yml`
      **Source:** CONTEXT
- [ ] Hardening (D-2026-07-28-ci-seguranca-4) intacto: 100% SHA pin em actions de terceiro (local
      `./` isento), harden-runner nos jobs ubuntu-latest, persist-credentials:false, permissions
      declarado em pipeline.yml
      **Verify:** `! grep -rEq "uses:\s*[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+@v[0-9]" .github/workflows/ && grep -rq "step-security/harden-runner" .github/workflows/ && grep -rq "persist-credentials: false" .github/workflows/ && grep -q "permissions:" .github/workflows/pipeline.yml`
      **Source:** CONTEXT
- [ ] Snapshot do branch protection de `main` capturado ANTES de qualquer edicao desta fase —
      baseline auditavel pro remap de check names (evita repetir o incidente de hoje)
      **Verify:** `test -s .jdi/phases/pipeline-unificada/branch-protection-before.json && python -c "import json; json.load(open('.jdi/phases/pipeline-unificada/branch-protection-before.json', encoding='utf-8'))"`
      **Source:** CONTEXT

### Manual
- _(none)_

## Deferred to PR review
- Confirmacao visual do run graph unico (todos os jobs de `pipeline.yml` numa unica execucao na aba
  Actions — hoje ~8 runs separados por push/PR).
- Execucao real do remap de branch protection: abrir a PR, capturar os check names reais via
  `gh api repos/.../commits/<sha>/check-runs`, so entao `PUT` os novos required contexts, so entao
  merge (ordem travada em D-2026-07-28-pipeline-unificada-1d — inverter essa ordem foi o incidente
  de hoje).
- PRs abertas do Dependabot com nomes de check antigos ficam stale ate rebase automatico — aceitavel.
- Primeiro run pos-merge em `main` verde de ponta a ponta (prova real de `workflow_call` em producao,
  nao testavel localmente).
- Decoracao de PR do SonarCloud usando os `sonar.pullrequest.*` explicitos aparecendo corretamente
  na UI do SonarCloud (so observavel num PR real).

## Notes
- Sem `.cs` novo/alterado nesta fase (infra-only, mesmo padrao de `ci-seguranca` e `sast-sca-sbom`)
  -> Gate 3 (cobertura 90%, D-6) do reviewer reporta SKIPPED, esperado.
- actionlint indisponivel no ambiente (confirmado nas 2 fases anteriores); validar YAML com
  `python -c "import yaml; yaml.safe_load(open(f, encoding='utf-8'))"` por arquivo.
- `npx jdi-cli` quebrado neste ambiente Windows — nenhum passo do doer/reviewer deve depender dele.
- Guardrail de nomes de artifact (D-2026-07-28-pipeline-unificada-6a): hoje `coverage` (ci.yml) e
  `sbom-spdx` (sbom.yml) sao os unicos nomes no escopo migrado e nao colidem; qualquer artifact novo
  deve prefixar com o nome do job, porque o `run_id` passa a ser compartilhado por todos os jobs.
  Nao virou DoD proprio por nao haver violacao hoje pra verificar.
- Ordem sugerida ao planner: (1) trigger surface + permissions + concurrency nos 8 reusables,
  arquivo por arquivo (mudanca mecanica); (2) snapshot do branch protection (DoD 10) antes de tocar
  qualquer arquivo; (3) criar `pipeline.yml` referenciando os 8 via `uses: ./...` com if:/
  permissions:/secrets: corretos por job; (4) abrir PR, capturar check names reais, remapear branch
  protection, so entao merge.
