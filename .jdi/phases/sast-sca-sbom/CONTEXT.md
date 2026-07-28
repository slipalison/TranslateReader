# Phase 8: Suplemento SAST/SCA/SBOM (paridade simulator-ccb) — Context  (slug: sast-sca-sbom)

## Goal
Semgrep SAST com regras custom (zip-slip/XXE/WebView), gate SCA nativo dotnet (bloqueia CVE HIGH/CRITICAL), bump SQLitePCLRaw (GHSA-2m69-gcr7-jv3q), TruffleHog verified, SBOM Syft e SECURITY.md.

## Locked decisions
(texto completo de cada uma em `.jdi/DECISIONS.md`)
- D-2026-07-28-sast-sca-sbom-1: escopo completo (aplicaveis + nao-aplicaveis, com porques) ja definido via analise comparativa do `ci.yml` de `simulator-ccb`. Nao redecidido aqui — so transformado em DoD executavel.
- D-2026-07-28-ci-seguranca-4: hardening obrigatorio (SHA pin, permissions least-privilege, harden-runner, concurrency) vale tambem pros workflows novos desta fase.
- D-2 (brownfield): bump do `SQLitePCLRaw.bundle_green` e mudanca pontual de seguranca sobre csproj legado, permitida (prioridade 1 = seguranca); nenhum outro arquivo legado e tocado.

## Canonical refs
- `github.com/slipalison/simulator-ccb` — `ci.yml` (analise comparativa ja consumida, nao re-fetch).
- `GHSA-2m69-gcr7-jv3q` — advisory HIGH em `lib.e_sqlite3`, transitiva de `SQLitePCLRaw.bundle_green` 2.1.11.
- `.jdi/phases/ci-seguranca/SUMMARY.md` — SHA pins e convencoes de hardening ja resolvidos, reutilizaveis.

## Out of scope
- DAST/OWASP ZAP — app cliente MAUI sem superficie HTTP, sem alvo de scan dinamico.
- Trivy Image + Dockle — sem Docker/imagem/registry no projeto.
- Checkov — sem IaC (compose/terraform/k8s).
- Trivy FS — cego pra NuGet sem `packages.lock.json`; substituido pelo gate SCA nativo dotnet.
- Gate de cobertura no CI — pertence a phase `cobertura-e-ci` (D-2026-07-28-ci-seguranca-1).

## Definition of Done

### Auto-verifiable
- [ ] `.semgrep/` cobre os 4 riscos reais do repo: zip-slip (extracao EPUB), XXE, WebView JS injection, deserializacao insegura
      **Verify:** `test -d .semgrep && grep -rqiE "zip-?slip" .semgrep/ && grep -rqiE "xxe|dtdprocessing|xmlresolver" .semgrep/ && grep -rqiE "webview|evaluatejavascript" .semgrep/ && grep -rqiE "binaryformatter|typenamehandling" .semgrep/`
      **Source:** CONTEXT
- [ ] Workflow `semgrep.yml`: instala via pip, roda `--config auto` + `--config .semgrep/`, gera SARIF, upload categoria `semgrep`
      **Verify:** `test -f .github/workflows/semgrep.yml && grep -q "pip install semgrep" .github/workflows/semgrep.yml && grep -qF -- "--config auto" .github/workflows/semgrep.yml && grep -qF -- "--config .semgrep" .github/workflows/semgrep.yml && grep -qF -- "--sarif" .github/workflows/semgrep.yml && grep -q "upload-sarif" .github/workflows/semgrep.yml && grep -qi "category: semgrep" .github/workflows/semgrep.yml`
      **Source:** CONTEXT
- [ ] Gate SCA: `dotnet list package --vulnerable --include-transitive` (Core+app+tests), falha em High/Critical
      **Verify:** `grep -rqF -- "--vulnerable" .github/workflows/ && grep -rqF -- "--include-transitive" .github/workflows/ && grep -rq "TranslateReader.Core.csproj" .github/workflows/ && grep -rqiE "high|critical" .github/workflows/`
      **Source:** CONTEXT
- [ ] `SQLitePCLRaw.bundle_green` sai da versao vulneravel 2.1.11; gate SCA nao reporta mais o pacote
      **Verify:** `! grep -q 'Include="SQLitePCLRaw.bundle_green" Version="2.1.11"' src/TranslateReader.Core/TranslateReader.Core.csproj && grep -q "SQLitePCLRaw.bundle_green" src/TranslateReader.Core/TranslateReader.Core.csproj && ! dotnet list src/TranslateReader.Core/TranslateReader.Core.csproj package --vulnerable --include-transitive | grep -qi "SQLitePCLRaw"`
      **Source:** CONTEXT
- [ ] Suite de testes existente permanece verde apos o bump
      **Verify:** `dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj --nologo`
      **Source:** CONTEXT
- [ ] TruffleHog verified-secrets (`--only-verified`, `fetch-depth: 0`) presente e SHA-pinned
      **Verify:** `grep -rqi "trufflesecurity/trufflehog" .github/workflows/ && grep -rqF -- "--only-verified" .github/workflows/ && grep -rqF -- "fetch-depth: 0" .github/workflows/ && ! grep -rEq "trufflesecurity/trufflehog@v[0-9]" .github/workflows/`
      **Source:** CONTEXT
- [ ] Hardening (SHA pin, permissions, harden-runner, concurrency) cobre tambem os workflows novos
      **Verify:** `! grep -rEq "uses:\s*[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+@v[0-9]" .github/workflows/ && grep -rq "permissions:" .github/workflows/ && grep -rq "step-security/harden-runner" .github/workflows/ && grep -rq "concurrency:" .github/workflows/`
      **Source:** CONTEXT
- [ ] Workflow `sbom.yml`: SBOM SPDX-JSON via `anchore/sbom-action`, `dependency-snapshot: true`, artifact 30d, informacional
      **Verify:** `test -f .github/workflows/sbom.yml && grep -qi "anchore/sbom-action" .github/workflows/sbom.yml && grep -qi "spdx-json" .github/workflows/sbom.yml && grep -qF -- "dependency-snapshot: true" .github/workflows/sbom.yml && grep -q "retention-days: 30" .github/workflows/sbom.yml`
      **Source:** CONTEXT
- [ ] `SECURITY.md` (raiz ou `.github/`): report privado via Security Advisories, prazo e escopo, em ingles
      **Verify:** `f=$(find . -maxdepth 2 -iname "SECURITY.md" 2>/dev/null | head -1); test -n "$f" && grep -qi "security advisor" "$f" && grep -qiE "report|respons" "$f"`
      **Source:** CONTEXT

### Manual
- _(none)_

## Deferred to PR review
- Triagem dos findings reais do Semgrep no primeiro run (false-positive vs achado genuino).
- Confirmacao visual do `dependency-snapshot` no Dependency Graph do GitHub apos merge.
- Teste do fluxo de report do `SECURITY.md` (abrir 1 GitHub Security Advisory real).

## Notes
- Fase estende o branch/PR de `ci-seguranca` (`jdi/ci-seguranca`, PR #1); commits com escopo `sast-sca-sbom`.
- Hardening reusa a logica do DoD-8 de `ci-seguranca` (diretorio-wide); SHAs novos (TruffleHog, `anchore/sbom-action`) resolvidos pelo doer em tempo de execucao.
- Ordem sugerida ao planner: bump do SQLitePCLRaw antes do gate SCA (pipeline nasce verde); demais tasks sao independentes entre si.
- Sem `.cs` novo esperado (infra-only, como `ci-seguranca`) -> Gate 3 (cobertura 90%) do reviewer reporta SKIPPED.
