# Phase 7: Pipeline CI/CD com seguranca + correcao do .slnx — Plan  (slug: ci-seguranca)

## Goal
Corrigir `TranslateReader.slnx` (refs a `/.idea/` gitignoradas) e entregar o pipeline GitHub Actions
rigoroso: CI (test Linux + build Windows), CodeQL, Dependabot, dependency review, OSSF Scorecard,
secret scan, SonarQube Cloud e release por tag.

**Specialist (todas as tasks):** `jdi-doer-translatereader` (single-stack, glob `**/*`).

## Locked decisions (de CONTEXT.md)
- **D-...-2:** fix do `.slnx` = remover SO o bloco `/.idea/`; ref a `.claude/settings.local.json` fica.
- **D-...-3:** seguranca = CodeQL + Dependabot + dependency-review + Scorecard + secret scan action.
- **D-...-4:** hardening obrigatorio (SHA pin, permissions, harden-runner, concurrency).
- **D-...-5:** CI = job `test` (ubuntu, Core+Tests, sem workload MAUI, coleta XPlat) + job `build`
  (windows, `net10.0-windows10.0.19041.0`). O GATE de 90% **nao** nasce aqui (fica em `cobertura-e-ci`).
- **D-...-6:** release por tag `v*` -> publish Windows -> GitHub Release; Sonar via `dotnet-sonarscanner`
  (`sonarcloud-github-action` deprecada); execucao real deferida ao PR review.
- **D-4:** conventional commits, scope `ci-seguranca`, 1 task = 1 commit, mensagem em ingles.

## Convencoes obrigatorias (valem para T-2..T-8, nao repetidas por task)
1. `permissions:` no topo com `contents: read`; cada job eleva so o que precisa.
2. `concurrency: { group: "${{ github.workflow }}-${{ github.ref }}", cancel-in-progress: true }`.
3. `step-security/harden-runner` como 1o step de todo job `ubuntu-latest` (`egress-policy: audit`).
4. TODA action de terceiro pinada por SHA de 40 hex + comentario da versao —
   `uses: actions/checkout@<sha40> # v5.0.0`. Resolver com
   `git ls-remote https://github.com/<owner>/<repo> refs/tags/<tag>^{}` (ou `gh api`). Nunca `@vN`.
5. `actions/checkout` com `persist-credentials: false` (requisito Scorecard).
6. .NET 10 via `actions/setup-dotnet` (`dotnet-version: 10.0.x`).
7. Validar YAML antes de commitar: `actionlint` se disponivel, senao
   `python -c "import yaml,sys;yaml.safe_load(open(sys.argv[1]))" <file>`.

Greps de aceite em pwsh: `sls '<regex>' <path>` (equivalente a `grep -E`).

## Tasks

### Wave 1 (paralelizavel — `files_modified` disjuntos)

#### T-1: Remover o bloco `/.idea/` do TranslateReader.slnx
- **Files:** `TranslateReader.slnx`
- **Acceptance:**
  - `sls '\.idea' TranslateReader.slnx` sem match (bloco `<Folder Name="/.idea/">` removido inteiro)
  - 3 `<Project Path=` preservados e bloco `/.claude/` intocado (`git diff` toca so as linhas 19-23)
  - `dotnet restore TranslateReader.slnx` conclui com sucesso
- **Deps:** none | **Test:** `dotnet restore` + greps | **DoD:** 1 | **Status:** pending

#### T-2: Workflow de CI (test Linux + build Windows)
- **Files:** `.github/workflows/ci.yml`
- **Como:** `on: push[main] + pull_request + workflow_dispatch`. Job `test` (ubuntu-latest):
  `dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --collect:"XPlat Code
  Coverage"` (esse csproj referencia SO o Core -> sem workload MAUI) + upload do `coverage.cobertura.xml`
  como artifact. Job `build` (windows-latest): `dotnet workload install maui` +
  `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0`.
- **Acceptance:**
  - `sls 'ubuntu-latest|windows-latest|XPlat Code Coverage|net10\.0-windows10\.0\.19041\.0' <file>` casa os 4
  - sem `dotnet workload` no job `test`; **nenhum** threshold/gate de cobertura (D-...-1)
  - YAML valida + convencoes 1-6 aplicadas
- **Deps:** none | **Test:** actionlint/parser YAML + greps | **DoD:** 2 | **Status:** pending

#### T-3: Workflow CodeQL (csharp, security-extended)
- **Files:** `.github/workflows/codeql.yml`
- **Como:** ubuntu-latest; `on: push[main] + pull_request + schedule` (cron semanal); job com
  `security-events: write`, `actions: read`, `contents: read`; `github/codeql-action/init` com
  `languages: csharp`, `build-mode: none` (C# analisa sem build desde 2024 — evita workload MAUI no
  runner), `queries: security-extended`; depois `github/codeql-action/analyze`.
- **Acceptance:**
  - `sls 'github/codeql-action|security-extended|csharp|build-mode: none' <file>` casa os 4
  - permissions do job least-privilege (sem `write-all`); harden-runner presente; YAML valida
- **Deps:** none | **Test:** actionlint/parser YAML + greps | **DoD:** 3 | **Status:** pending

#### T-4: Dependabot + dependency-review em PRs
- **Files:** `.github/dependabot.yml`, `.github/workflows/dependency-review.yml`
- **Como:** `dependabot.yml` `version: 2` com `package-ecosystem: nuget` (dir `/`, weekly) e
  `github-actions` (dir `/`, weekly — mantem os SHA pins atualizados). Workflow: `on: pull_request`,
  `contents: read` + `pull-requests: write`, harden-runner + checkout + `actions/dependency-review-action`
  com `fail-on-severity: moderate` e `comment-summary-in-pr: on-failure`.
- **Acceptance:**
  - `.github/dependabot.yml` existe e `sls 'nuget|github-actions' <file>` casa os 2
  - `sls 'actions/dependency-review-action|pull_request' <workflow>` casa os 2
  - YAML valida nos 2 arquivos
- **Deps:** none | **Test:** actionlint/parser YAML + greps | **DoD:** 4, 5 | **Status:** pending

#### T-5: OSSF Scorecard agendado + secret scan + badge no README
- **Files:** `.github/workflows/scorecard.yml`, `.github/workflows/secret-scan.yml`, `README.md`
- **Como:** `scorecard.yml`: `on: schedule` (cron semanal) + `push[main]` + `workflow_dispatch`; job com
  `security-events: write`, `id-token: write`, `contents: read`, `actions: read`; `ossf/scorecard-action`
  (`results_file: results.sarif`, `results_format: sarif`, `publish_results: true`) + `upload-artifact` +
  `github/codeql-action/upload-sarif`. `secret-scan.yml`: gitleaks (`gitleaks/gitleaks-action`, gratuito em
  repo publico pessoal; fallback `trufflesecurity/trufflehog`) em `push` + `pull_request` + schedule,
  `fetch-depth: 0`, `contents: read`. README: badge do Scorecard abaixo do titulo (linha 1), uri
  `github.com/slipalison/TranslateReader`.
- **Acceptance:**
  - `sls 'ossf/scorecard-action' <scorecard>` e `sls 'schedule:' <scorecard>` casam
  - `sls 'gitleaks|trufflehog' .github/workflows/*.yml` casa (case-insensitive)
  - `sls 'scorecard' README.md` casa; YAML valida nos 2 workflows
- **Deps:** none | **Test:** actionlint/parser YAML + greps | **DoD:** 6, 7 | **Status:** pending

#### T-6: Workflow de release por tag `v*`
- **Files:** `.github/workflows/release.yml`
- **Como:** `on: push: tags: ['v*']`; `contents: read` no topo e `contents: write` so no job; windows-latest;
  `dotnet workload install maui` + `dotnet publish src/TranslateReader/TranslateReader.csproj -c Release
  -f net10.0-windows10.0.19041.0 -p:WindowsPackageType=None -p:RuntimeIdentifier=win-x64` (unpackaged evita
  exigencia de assinatura MSIX), zip do diretorio publicado, `softprops/action-gh-release` com `files:` +
  `generate_release_notes: true`. Sem execucao real nesta phase (nao ha tag).
- **Acceptance:**
  - `sls 'v\*' <file>` e `sls 'softprops/action-gh-release' <file>` casam
  - `contents: write` aparece **apenas** no bloco do job de release; YAML valida
- **Deps:** none | **Test:** actionlint/parser YAML + greps | **DoD:** 9 | **Status:** pending

#### T-7: Workflow SonarQube Cloud (dotnet-sonarscanner)
- **Files:** `.github/workflows/sonarqube.yml`
- **Como:** ubuntu-latest, escopo Core+Tests (mesmo racional de D-...-5); `on: push[main] + pull_request +
  workflow_dispatch`; harden-runner; checkout `fetch-depth: 0` (blame do Sonar); `actions/setup-java`
  (temurin 17) + setup-dotnet; `dotnet tool install --global dotnet-sonarscanner`; `begin` (`/k:`, `/o:`,
  `/d:sonar.host.url=https://sonarcloud.io`, `/d:sonar.token=$SONAR_TOKEN`,
  `/d:sonar.cs.opencover.reportsPaths=**/coverage.opencover.xml`) -> `dotnet build` do Core ->
  `dotnet test -p:CoverletOutputFormat=opencover` -> `end`.
  **Guard:** `env: SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}` no job + `if: env.SONAR_TOKEN != ''` nos steps do
  scanner (o contexto `secrets` nao existe em `if:`) — sem token o workflow passa como no-op, ja que a
  execucao real esta deferida (D-...-6).
- **Acceptance:**
  - `sls 'sonarscanner|SONAR_TOKEN' <file>` casa os 2 (case-insensitive)
  - `sls 'sonarcloud-github-action' <file>` **sem** match (deprecada); YAML valida
- **Deps:** none | **Test:** actionlint/parser YAML + greps | **DoD:** 10 | **Status:** pending

### Wave 2

#### T-8: Auditoria de hardening + validacao de todos os workflows
- **Files:** `.github/workflows/*.yml`, `.github/dependabot.yml` (somente correcoes)
- **Acceptance:**
  - 100% pinado: `(sls 'uses:' .github/workflows/*.yml).Count` == `(sls 'uses:\s*\S+@[0-9a-f]{40}'
    .github/workflows/*.yml).Count` **e** `sls 'uses:\s*[\w.-]+/[\w.-]+@v[0-9]' .github/workflows/*.yml` sem match
  - todo workflow tem `permissions:` + `concurrency:`; todo job `ubuntu-latest` tem `step-security/harden-runner`
  - os 10 `Verify:` do CONTEXT.md executados e PASS (colar saida no SUMMARY.md)
  - actionlint (ou parser YAML) OK em todos os arquivos; comando usado registrado no SUMMARY.md
- **Deps:** T-2, T-3, T-4, T-5, T-6, T-7 | **Test:** bateria de greps do DoD + validacao YAML
- **DoD:** 8 (+ re-check de 2..10) | **Status:** pending
- **Nota:** se nada precisar de ajuste, nao ha commit — registrar a evidencia no SUMMARY.md.

## Execution
- **Total tasks:** 8
- **Waves:** 2
- Estimated parallel speedup: ~4x (7 de 8 tasks na wave 1)

| Wave | Tasks | Observacao |
|---|---|---|
| 1 | T-1, T-2, T-3, T-4, T-5, T-6, T-7 | paralelo — `files_modified` disjuntos, zero acoplamento |
| 2 | T-8 | gate final; depende de T-2..T-7 |

## Files modified (todas as tasks)
- `TranslateReader.slnx`, `README.md`, `.github/dependabot.yml`
- `.github/workflows/`: `ci.yml`, `codeql.yml`, `dependency-review.yml`, `scorecard.yml`,
  `secret-scan.yml`, `release.yml`, `sonarqube.yml`

## Test requirements
- Phase **infra-only**: nenhum `.cs` novo -> Gate 3 do reviewer (cobertura 90%, D-6) reporta
  **SKIPPED**. Esperado, nao e falha.
- Baseline: `dotnet test` segue com os 167 testes verdes (T-1 e o unico risco — validar com `dotnet restore`).
- Validacao por task: `actionlint` (ou parser YAML) + os greps de aceite listados acima.
- Nenhum gate de cobertura entra no pipeline nesta phase (D-...-1 / D-...-5).
