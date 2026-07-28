# Phase 7: Review  (slug: ci-seguranca)

**Verdict:** APPROVED_WITH_WARNINGS

Round: 2 (re-verify pos fix round 1) | Modo: verify | Reviewer: `jdi-reviewer-translatereader` | Data: 2026-07-28

Re-execucao completa de todos os gates apos o fix round 1 (commits `4e04407`, `d5035a5`, `e679a21`, `9326e0d`, `6fe2f3b` — ver SUMMARY § Fix round 1). Round 1 fechou com 6 warnings; este round confirma **3 resolvidos** (W-2, W-3, W-5) + finding do critic resolvido, e **3 permanecem abertos por design** (W-1 roteado pro Dependabot; W-4/W-6 legado isento por D-2). Phase segue infra-only: fix round tocou apenas 3 workflows + docs `.jdi/` — 0 arquivos `.cs` (skills estruturais sem alvo de codigo, como no round 1; greps canonicos de camada executados mesmo assim).

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | Comando corrigido (W-5): `dotnet restore` + `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` — exit 0, 0 erros, 42 warnings legados (MVVMTK0045 etc.). NU1903 (SQLitePCLRaw 2.1.11) ainda presente no restore — W-1 segue aberto |
| Tests | PASS | 169 aprovados / 2 ignorados (skips pre-existentes de integracao LLM) / 0 falhas — total 171. Baseline 167 (D-2) preservado. Exit 0 |
| Coverage | SKIPPED | `git log --diff-filter=A ... 4285f25..HEAD` segue sem `.cs` novo (fix round tocou so YAML + docs) — adopted mode, gate 90% (D-6) sem alvo. Esperado (CONTEXT § Notes) |
| Lint | WARN | `dotnet format --verify-no-changes` — mesmos errors WHITESPACE legados do round 1 (ThemeEngine.cs:12/14, ReadingManager.cs:54, ReaderPage.xaml.cs:122/124, HtmlInjectionTests, ThemeEngineTests, TranslationManagerTests). Nenhum arquivo tocado pela phase. WARN-only ate `baseline-de-estilo` (W-4) |
| Security/Layer | PASS (com warnings) | Greps C# 5.1/5.2/5.9/5.10 limpos; 5.11 (5+=/4-=), 5.12 (1 static mutavel), 4 catches OCE + 2 catch vazios = exatamente os baselines legados, inalterados (W-6). Re-auditoria completa dos 7 workflows pos-fix: ver tabela abaixo — tudo verde, incluindo **0 `${{ }}` dentro de `run:`** (era 1 no round 1) |
| Consistency | PASS | 5 commits do fix round: `4e04407` (so sonarqube.yml), `d5035a5` (so release.yml), `9326e0d` (so secret-scan.yml), `e679a21` (docs — correcao do Gate 1 nos 3 files de routing/docs, 1 mudanca logica), `6fe2f3b` (so SUMMARY.md). Conventional Commits, scope `ci-seguranca`, types adequados (`ci`/`docs`) — D-4 OK. Atomicos 1:1 com os warnings enderecados |
| UI Validation | SKIPPED | has_frontend=false (cliente MAUI nativo) — permanente |
| DoD | PASS | 10/10 auto PASS (re-executados verbatim em bash neste round), 0 manual pending |

## Re-auditoria de seguranca dos workflows (Gate 5 — pos fix round)

Executada de verdade neste round (nao herdada do round 1 nem do SUMMARY):

| Check | Resultado round 2 |
|---|---|
| `uses:` total vs SHA-40 pinned | 28 = 28 (100%); ci 6, codeql 4, dep-review 3, release 3, scorecard 5, secret-scan 3, sonarqube 4 — nenhuma linha `uses:` adicionada/removida pelo fix round |
| Refs mutaveis `@vN` | 0 (grep sem match) |
| `permissions:` top-level least-privilege | 7/7 workflows com `contents: read` no topo |
| Elevacao por job | Inalteradas e minimas: codeql `security-events: write`; dep-review `pull-requests: write`; release `contents: write`; scorecard `security-events` + `id-token: write` |
| `concurrency` + `cancel-in-progress: true` | 7/7 |
| `step-security/harden-runner` (1o step) | 6/6 jobs ubuntu-latest (`runs-on` inventariado: 6 ubuntu + 2 windows — ci build e release isentos, harden-runner nao suporta Windows). Egress `audit` mantido (tuning `block` = futuro, fora de escopo) |
| checkout `persist-credentials: false` | 8/8 checkouts |
| **`${{ }}` dentro de `run:`** | **0/7 workflows** — verificado por parse YAML real (python, todos os `steps[].run` de todos os jobs), nao grep de linha. Round 1 tinha 1 (release.yml:39). Restantes `${{ }}` sao `concurrency.group`, `env:` e `with:` (contextos seguros) |
| Secrets | `SONAR_TOKEN` so via `secrets.` no `env` do job (sonarqube.yml:21); guard `if: env.SONAR_TOKEN != ''` intacto nos 7 steps; token expandido pelo shell (`$SONAR_TOKEN`, linhas 58/71), nunca inline em `run:`. Nenhum secret ecoado |
| `pull_request_target` / `workflow_run` | 0 ocorrencias |
| Cron | 3 validos: codeql `26 7 * * 1`, scorecard `30 2 * * 6`, secret-scan `15 4 * * 0` |
| YAML syntax (python yaml.safe_load — actionlint indisponivel) | 8/8 OK (dependabot + 7 workflows, incluindo os 3 alterados) |

### Verificacao dos fixes (linha a linha)

| Item round 1 | Status | Evidencia round 2 |
|---|---|---|
| **W-2** (sonarqube.yml — cobertura nunca gerada) | **RESOLVIDO** (`4e04407`) | `sonarqube.yml:67`: `--collect:"XPlat Code Coverage;Format=opencover" --results-directory TestResults` — mecanismo correto pro **coverlet.collector** (unico referenciado no test csproj, confirmado: `coverlet.collector 8.0.1`). `sonarqube.yml:59`: `sonar.cs.opencover.reportsPaths="**/TestResults/**/coverage.opencover.xml"` casa com o output do collector (`TestResults/<guid>/coverage.opencover.xml`, validado local pelo doer com root `<CoverageSession>`) |
| **W-3** (release.yml:39 — `${{ github.ref_name }}` cru no `run:` pwsh) | **RESOLVIDO** (`d5035a5`) | `release.yml:39-41`: `env: RELEASE_TAG: ${{ github.ref_name }}` + `${env:RELEASE_TAG}` no script. `release.yml:46` (`with: files:`) segue interpolado — nao e shell, OK. Zero `${{ }}` em `run:` no repo inteiro |
| **W-5** (docs Gate 1 — build a nivel de solution falha NETSDK1005) | **RESOLVIDO** (`e679a21`) | Comando com csproj explicito em `jdi-reviewer-translatereader.md:160/166`, `reviewers.md:17`, nota datada em `registry.md:41-44`. Grep pelo comando antigo a nivel de solution: 0 hits. Comando corrigido foi o executado no Gate 1 deste round — exit 0 |
| **Critic** (secret-scan.yml — `on: push` sem filtro) | **RESOLVIDO** (`9326e0d`) | `secret-scan.yml:4-5`: `push: branches: [main]`; `pull_request` + cron mantidos; `fetch-depth: 0` preservado (linha 32 — gitleaks segue vendo historico completo) |

## Blockers

_(nenhum)_

## Warnings

Abertos (3 — todos intencionalmente nao enderecados no fix round, com rota definida):

- **W-1 (seguranca, dependencia legada) — ABERTO:** NU1903 no restore — `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (transitiva de `SQLitePCLRaw.bundle_green`) com advisory HIGH (GHSA-2m69-gcr7-jv3q). Confirmado ainda presente neste round (3 projetos). Rota: Dependabot `nuget` (recem-configurado) abre o PR de bump; `dependency-review` bloqueia regressoes futuras. Aceitar o bump assim que chegar.
- **W-4 (lint, legado) — ABERTO:** whitespace drift nos mesmos arquivos legados do round 1 (isento por D-2; nenhum arquivo da phase). Rota: phase `baseline-de-estilo` (`.editorconfig` + analyzers).
- **W-6 (baselines legados C#) — ABERTO:** inalterados e re-conferidos neste round: 4 `catch (OperationCanceledException)` que engolem (LibraryPageModel:183, ReaderPageModel:222, ReaderPage.xaml.cs:308, TranslationManager:62), 2 `catch { }` vazios (ReaderPage.xaml.cs:326, 434), 1 static mutavel (TranslationEngine.cs:16, guard one-shot), eventos 5 `+=` / 4 `-=`. Tudo pre-boundary `4285f25` (D-2). Rota: futura phase de hardening de codigo.

Resolvidos neste fix round (changelog):

- ~~W-2~~ cobertura OpenCover via coverlet.collector no sonarqube.yml — `4e04407`.
- ~~W-3~~ tag de release via env indirection no pwsh — `d5035a5`.
- ~~W-5~~ comando do Gate 1 corrigido pra apontar o csproj do app (agent + reviewers + registry) — `e679a21`.
- ~~Critic~~ trigger `push` do secret-scan escopado pra `main` — `9326e0d`.

## DoD Checklist (gate 8)

Fonte: `CONTEXT.md § Definition of Done` (PROJECT.md nao declara secao DoD — coberto pelos gates 1-7; nao e INCONCLUSIVE pois CONTEXT declara). Todos os 10 re-executados verbatim em bash neste round 2.

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | slnx sem `.idea` + projetos preservados | CONTEXT | Auto | PASS | exit 0 |
| 2 | CI: build Windows TFM + test Linux + XPlat coverage | CONTEXT | Auto | PASS | exit 0 |
| 3 | CodeQL csharp + security-extended | CONTEXT | Auto | PASS | exit 0 |
| 4 | dependabot.yml nuget + github-actions | CONTEXT | Auto | PASS | exit 0 |
| 5 | dependency-review-action em pull_request | CONTEXT | Auto | PASS | exit 0 |
| 6 | ossf/scorecard-action + schedule | CONTEXT | Auto | PASS | exit 0 |
| 7 | gitleaks\|trufflehog presente | CONTEXT | Auto | PASS | exit 0 |
| 8 | SHA pin + permissions + harden-runner + concurrency | CONTEXT | Auto | PASS | exit 0; auditoria independente mais forte: 28/28 pins, 7/7 permissions, 7/7 concurrency, 6/6 harden-runner ubuntu |
| 9 | release em tag `v*` + action-gh-release | CONTEXT | Auto | PASS | exit 0 |
| 10 | sonarscanner + SONAR_TOKEN | CONTEXT | Auto | PASS | exit 0 (W-2 resolvido — cobertura agora e gerada de verdade) |

**Totals:** 10 items | Auto: 10 (10 PASS, 0 FAIL) | Manual: 0 pending

Nota (mantida do round 1): o regex negativo do DoD-8 nao cobriria subpath actions (`owner/repo/sub@vN`); a auditoria por contagem (28 `uses:` = 28 `@sha40`) fecha essa brecha — sem pass oco.

## Recommendation

Aprovado com warnings — os 3 restantes sao legado/dependencia com rota definida, nada fixavel dentro desta phase. Pode seguir para `/jdi-ship ci-seguranca`. No PR body: listar os `## Deferred to PR review` do CONTEXT + W-1 (aceitar o bump do SQLitePCLRaw quando o Dependabot abrir). Tuning futuro opcional: harden-runner `egress-policy: audit -> block` apos observar os primeiros runs.
