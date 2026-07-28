# Phase 7: Pipeline CI/CD com seguranca + correcao do .slnx — Context  (slug: ci-seguranca)

## Goal
Corrigir o `TranslateReader.slnx` (referencias a `.idea/` gitignoradas) e entregar um pipeline
GitHub Actions rigoroso: CodeQL, dependency review, secret scanning, OSSF Scorecard, build +
testes, SonarQube e release automatizado.

## Locked decisions
(texto completo de cada uma em `.jdi/DECISIONS.md`)
- D-2026-07-28-ci-seguranca-1: phase criada via /jdi-issue; overlap com `cobertura-e-ci` e
  consciente (CI nasce aqui, gate de cobertura 90% fica la).
- D-2026-07-28-ci-seguranca-2: fix do `.slnx` = so remover `/.idea/`; `.claude/settings.local.json` fica como esta.
- D-2026-07-28-ci-seguranca-3: "seguranca" = CodeQL + Dependabot + dependency-review-action +
  OSSF Scorecard + secret scan action; toggles nativos de Settings vao pra Deferred.
- D-2026-07-28-ci-seguranca-4: hardening obrigatorio — actions pinadas por full SHA,
  `permissions` least-privilege, `step-security/harden-runner`, `concurrency`.
- D-2026-07-28-ci-seguranca-5: CI = job test (ubuntu-latest, sem workload MAUI) + job build
  (windows-latest, TFM `net10.0-windows10.0.19041.0`); gate de 90% fica pra `cobertura-e-ci`.
- D-2026-07-28-ci-seguranca-6: "releases" = tag `v*` -> publish Windows -> GitHub Release;
  "SonarQube" = SonarQube Cloud via `dotnet-sonarscanner`, execucao real deferida.

## Canonical refs
- Card colado via `/jdi-issue` (sem URL/ID externo — texto abaixo).
- Repo `github.com/slipalison/TranslateReader`, publico, branch `main` (CodeQL/Scorecard/Dependabot gratuitos).
- `.gitignore:29` + `TranslateReader.slnx` bloco `/.idea/` (linhas 19-23) — defeito diagnosticado.

> "ajuste o arquivo .slnx pois esta errado, crie a pipeline para o github actions que contenha
> todas as validacoes de seguranca disponivel hoje no open source do github tenha testes e
> releases, sonarqube, seja criterioso com a seguranca e rigido!"

## Out of scope
- Build/testes de Android e iOS no pipeline de CI (workload MAUI mobile) — `todos.md`.
- Assinatura/publicacao em loja (Google Play, App Store/TestFlight) no release — `todos.md`.
- `zizmor` (linter estatico de workflows) — reforco opcional, nao obrigatorio — `todos.md`.
- SonarQube self-hosted — descartado a favor do SonarQube Cloud (D-2026-07-28-ci-seguranca-6).

## Definition of Done

### Auto-verifiable
- [ ] `TranslateReader.slnx` nao referencia mais `/.idea/`; projetos preservados
      **Verify:** `! grep -qi "\.idea" TranslateReader.slnx && grep -q "src/TranslateReader.Core/TranslateReader.Core.csproj" TranslateReader.slnx`
      **Source:** CONTEXT
- [ ] CI: job de build (Windows TFM) + job de test/coverage (Linux, sem workload MAUI)
      **Verify:** `grep -rq "windows-latest" .github/workflows/ && grep -rq "net10.0-windows10.0.19041.0" .github/workflows/ && grep -rq "ubuntu-latest" .github/workflows/ && grep -rq "XPlat Code Coverage" .github/workflows/`
      **Source:** CONTEXT
- [ ] CodeQL para C# com queries security-extended
      **Verify:** `grep -rq "github/codeql-action" .github/workflows/ && grep -rq "security-extended" .github/workflows/ && grep -riq "csharp" .github/workflows/`
      **Source:** CONTEXT
- [ ] Dependabot configurado (ecosystems nuget + github-actions)
      **Verify:** `test -f .github/dependabot.yml && grep -q "nuget" .github/dependabot.yml && grep -q "github-actions" .github/dependabot.yml`
      **Source:** CONTEXT
- [ ] `dependency-review-action` rodando em pull requests
      **Verify:** `grep -rq "actions/dependency-review-action" .github/workflows/ && grep -rq "pull_request" .github/workflows/`
      **Source:** CONTEXT
- [ ] OSSF Scorecard com execucao agendada
      **Verify:** `grep -rq "ossf/scorecard-action" .github/workflows/ && grep -rq "schedule:" .github/workflows/`
      **Source:** CONTEXT
- [ ] Scanner de secrets (gitleaks ou trufflehog) presente em algum workflow
      **Verify:** `grep -riqE "gitleaks|trufflehog" .github/workflows/`
      **Source:** CONTEXT
- [ ] Supply-chain: actions pinadas por SHA, permissions least-privilege, harden-runner, concurrency
      **Verify:** `! grep -rEq "uses:\s*[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+@v[0-9]" .github/workflows/ && grep -rq "permissions:" .github/workflows/ && grep -rq "step-security/harden-runner" .github/workflows/ && grep -rq "concurrency:" .github/workflows/`
      **Source:** CONTEXT
- [ ] Release: dispara em tag `v*`, publica o TFM Windows e cria GitHub Release
      **Verify:** `grep -rqF "v*" .github/workflows/ && grep -riEq "action-gh-release|softprops|gh release create" .github/workflows/`
      **Source:** CONTEXT
- [ ] Workflow do SonarQube Cloud existe e referencia o token de scan
      **Verify:** `grep -riq "sonarscanner" .github/workflows/ && grep -riq "SONAR_TOKEN" .github/workflows/`
      **Source:** CONTEXT

### Manual
- _(none)_

## Deferred to PR review
- Execucao real do SonarQube Cloud (org/projeto/token configurados, scan sem findings bloqueantes).
- Habilitar secret scanning + push protection nas Settings (toggle nativo, nao versionado).
- Habilitar branch protection em `main` (status checks obrigatorios antes de merge).
- Habilitar Dependabot security alerts nas Settings (separado do `dependabot.yml`).
- Confirmar visualmente o badge do OSSF Scorecard no README apos o primeiro run.

## Notes
Coverage COLLECTION nasce aqui; o GATE de 90% que falha o build e da phase `cobertura-e-ci`
(D-2026-07-28-ci-seguranca-1) — nao duplicar. Repo publico -> CodeQL/Scorecard/Dependabot
gratuitos. Sem `.cs` novo nesta phase (esperado, infra-only) -> Gate 3 do reviewer reporta
SKIPPED normalmente.
