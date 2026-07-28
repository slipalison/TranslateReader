# Phase 7: Review  (slug: ci-seguranca)

**Verdict:** APPROVED_WITH_WARNINGS

Iteracao: 1 | Modo: verify | Reviewer: `jdi-reviewer-translatereader` | Data: 2026-07-28

Phase infra-only (0 arquivos `.cs` tocados — confirmado via `git diff --name-only 4285f25..HEAD -- src/ test/` = vazio). Skills estruturais (the-method/dry/kiss/yagni/clean-code) sem alvo de codigo nesta phase; gates de camada verificados pelos greps canonicos mesmo assim.

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `dotnet restore TranslateReader.slnx` OK; `dotnet build src/TranslateReader/TranslateReader.csproj -f net10.0-windows10.0.19041.0` exit 0. O comando documentado a nivel de SOLUTION (`dotnet build -f <win-tfm>`) falha com NETSDK1005 em Core/Tests (targets `net10.0`-only) — quirk pre-existente do `-f` forcado na solution inteira, nao regressao do T-1 (ver Warnings W-5) |
| Tests | PASS | 169 aprovados / 2 ignorados (skips pre-existentes de integracao LLM) / 0 falhas — total 171. Baseline 167 (D-2) preservado, sem regressao. Exit 0 |
| Coverage | SKIPPED | `git log --diff-filter=A ... 4285f25..HEAD` sem `.cs` novo — adopted mode, gate 90% (D-6) sem alvo. Esperado para phase infra-only (CONTEXT § Notes) |
| Lint | WARN | `dotnet format --verify-no-changes` exit 2 — apenas WHITESPACE em arquivos legados (ThemeEngine.cs, ReadingManager.cs, ReaderPage.xaml.cs, testes). Nenhum arquivo tocado pela phase (phase nao tocou `.cs`). WARN-only ate `baseline-de-estilo` shipar `.editorconfig` |
| Security/Layer | PASS (com warnings) | Greps C# 5.1/5.2/5.9/5.10 limpos; 5.11 (5+=/4-=), 5.12 (1 static mutavel) e catches de OCE = exatamente os baselines legados pre-boundary, inalterados. Auditoria completa dos 7 workflows: ver tabela abaixo — 28/28 SHA-pinned (10/10 pins conferidos contra tags reais via `git ls-remote`), least-privilege OK, sem `pull_request_target`, sem injecao critica. Warnings W-1..W-3 |
| Consistency | PASS | 8 commits `de2f329..HEAD`: 7 tasks (1 task = 1 commit, arquivos batem 1:1 com `files_modified` do PLAN) + 1 `docs` (SUMMARY). Conventional Commits com scope `ci-seguranca`, types adequados (`fix` no slnx, `ci` nos workflows, `docs` no summary) — D-4 OK. T-8 sem commit conforme previsto no PLAN (auditoria sem correcoes) |
| UI Validation | SKIPPED | has_frontend=false (cliente MAUI nativo) — permanente |
| DoD | PASS | 10/10 auto PASS (executados verbatim em bash), 0 manual pending |

## Auditoria de seguranca dos workflows (Gate 5 — deliverable da phase)

Executado de verdade nesta review (nao herdado do SUMMARY):

| Check | Resultado |
|---|---|
| `uses:` total vs SHA-40 pinned | 28 = 28 (100%); ci 6, codeql 4, dep-review 3, release 3, scorecard 5, secret-scan 3, sonarqube 4 |
| Refs mutaveis `@vN` | 0 (grep sem match) |
| Pins conferidos contra tags reais (`git ls-remote <repo> refs/tags/<tag>^{}`) | 10/10 OK: checkout v7.0.1, setup-dotnet v6.0.0, upload-artifact v7.0.1, setup-java v5.6.0, harden-runner v2.20.0, codeql-action v4.37.3, dependency-review-action v5.0.0, scorecard-action v2.4.4, gitleaks-action v3.0.0, action-gh-release v3.0.2 — todos os SHAs batem com o commit peeled da tag |
| `permissions:` top-level least-privilege | 7/7 workflows com `contents: read` no topo |
| Elevacao por job | Justificadas e minimas: codeql `security-events: write`+`actions: read`; scorecard `security-events`+`id-token: write`+`actions: read`; dependency-review `pull-requests: write` (exigido por `comment-summary-in-pr`); release `contents: write` (cria a Release). ci/sonarqube/secret-scan sem elevacao |
| `concurrency` + `cancel-in-progress: true` | 7/7 |
| `step-security/harden-runner` (1o step) | 6/6 jobs ubuntu-latest; 2 jobs Windows (ci build, release) isentos — harden-runner nao suporta Windows |
| checkout `persist-credentials: false` | 8/8 checkouts |
| Secrets | `SONAR_TOKEN` so via `secrets.` em `env` do job; guard `if: env.SONAR_TOKEN != ''` nos 7 steps do scanner (no-op sem token, conforme D-...-6); `$SONAR_TOKEN` expandido pelo shell, nunca `${{ }}` inline em `run:`; `GITHUB_TOKEN` -> gitleaks padrao. Nenhum secret ecoado |
| `pull_request_target` / `workflow_run` | 0 ocorrencias |
| Interpolacao `${{ }}` em `run:` | 1 ocorrencia: `release.yml:39` `${{ github.ref_name }}` em pwsh — ver W-3 (baixa severidade, trigger exige write access). Demais `${{ }}` sao `concurrency.group` e `env` (contextos seguros) |
| Cron | 3 validos: codeql `26 7 * * 1`, scorecard `30 2 * * 6`, secret-scan `15 4 * * 0` |
| dependabot.yml | version 2; ecosystems `nuget` + `github-actions`, dir `/`, weekly — mantem os proprios SHA pins atualizados |
| YAML syntax (python yaml.safe_load — actionlint indisponivel) | 8/8 OK (dependabot + 7 workflows) |
| CodeQL | `languages: csharp`, `build-mode: none` (evita workload MAUI no runner), `queries: security-extended` |
| slnx (T-1) | Diff = exatamente o bloco `/.idea/` (3 File refs gitignoradas); 3 `<Project Path=` preservados antes/depois; bloco `/.claude/` intocado (D-...-2) |

## Blockers

_(nenhum)_

## Warnings

- **W-1 (seguranca, dependencia legada):** NU1903 no restore — `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (transitiva de `SQLitePCLRaw.bundle_green`) tem advisory HIGH conhecida (GHSA-2m69-gcr7-jv3q). Nao foi introduzida nesta phase; o Dependabot `nuget` recem-criado deve abrir o PR de bump no primeiro run e o `dependency-review` passa a bloquear regressoes futuras. Recomendado aceitar o bump assim que chegar.
- **W-2 (funcional, sonarqube.yml):** o step "Test with OpenCover coverage" usa `-p:CollectCoverage=true -p:CoverletOutputFormat=opencover` — propriedades do **coverlet.msbuild**, mas o test csproj referencia apenas **coverlet.collector** (confirmado em `test/TranslateReader.Tests/TranslateReader.Tests.csproj`). Nenhum `coverage.opencover.xml` sera gerado e o Sonar rodara SEM cobertura quando o token for configurado. Fix antes de ativar o SONAR_TOKEN: adicionar `coverlet.msbuild` ao test csproj OU trocar para `--collect:"XPlat Code Coverage;Format=opencover"` e ajustar `sonar.cs.opencover.reportsPaths`. Execucao real ja esta em Deferred (D-...-6), por isso nao bloqueia.
- **W-3 (defesa em profundidade, release.yml:39):** `${{ github.ref_name }}` interpolado direto no `run:` pwsh (`Compress-Archive ... "TranslateReader-win-x64-${{ github.ref_name }}.zip"`). Nome de tag admite `$()` — subexpressao pwsh executavel. Trigger `push: tags` exige write access (ator ja poderia editar o workflow), logo severidade baixa; ainda assim, padrao recomendado: `env: TAG: ${{ github.ref_name }}` + usar `$env:TAG` no script. `release.yml:44` (`with: files:`) nao e shell — OK.
- **W-4 (lint, legado):** whitespace drift em arquivos legados reportado pelo `dotnet format` (isento por D-2; nenhum arquivo da phase). Endereacar na phase `baseline-de-estilo`.
- **W-5 (harness do reviewer/CLAUDE.md):** o comando documentado do Gate 1 (`dotnet build -f net10.0-windows10.0.19041.0` na solution) falha com NETSDK1005 porque força o TFM Windows em Core/Tests (`net10.0`-only). Determinista e independente do conteudo do `.slnx` (File refs de solution folder nao afetam TFM) — pre-existente, nao e regressao do T-1. O ci.yml do doer ja faz o certo (builda o csproj do app com `-f`). Atualizar o comando do gate/docs para apontar `src/TranslateReader/TranslateReader.csproj`.
- **W-6 (baselines legados C#, inalterados):** 4 `catch (OperationCanceledException)` que engolem (LibraryPageModel:183, ReaderPageModel:222, ReaderPage.xaml.cs:308, TranslationManager:62 — conferir rethrow neste ultimo) + 2 `catch { }` vazios (ReaderPage.xaml.cs:326, 434) contra csharp.md §1; 1 static mutavel (TranslationEngine.cs:16, guard one-shot documentado); eventos 5 `+=` / 4 `-=`. Tudo pre-boundary `4285f25`, zero mudanca nesta phase — registrado para futura phase de hardening de codigo, nao bloqueia (D-2).

## DoD Checklist (gate 8)

Fonte: `CONTEXT.md § Definition of Done` (PROJECT.md nao declara secao `## Definition of Done` — baseline projeto coberto pelos gates 1-7; nao e INCONCLUSIVE pois CONTEXT declara). Todos executados verbatim em bash nesta review.

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | slnx sem `.idea` + projetos preservados | CONTEXT | Auto | PASS | exit 0; 3 `<Project Path=` presentes |
| 2 | CI: build Windows TFM + test Linux + XPlat coverage | CONTEXT | Auto | PASS | exit 0 (ci.yml jobs `test`/`build`) |
| 3 | CodeQL csharp + security-extended | CONTEXT | Auto | PASS | exit 0 (codeql.yml) |
| 4 | dependabot.yml nuget + github-actions | CONTEXT | Auto | PASS | exit 0 |
| 5 | dependency-review-action em pull_request | CONTEXT | Auto | PASS | exit 0 |
| 6 | ossf/scorecard-action + schedule | CONTEXT | Auto | PASS | exit 0 (cron `30 2 * * 6`) |
| 7 | gitleaks\|trufflehog presente | CONTEXT | Auto | PASS | exit 0 (gitleaks-action v3) |
| 8 | SHA pin + permissions + harden-runner + concurrency | CONTEXT | Auto | PASS | exit 0; auditoria independente mais forte: 28/28 pins, 7/7 permissions, 7/7 concurrency, 6/6 harden-runner ubuntu |
| 9 | release em tag `v*` + action-gh-release | CONTEXT | Auto | PASS | exit 0 (release.yml) |
| 10 | sonarscanner + SONAR_TOKEN | CONTEXT | Auto | PASS | exit 0 (sonarqube.yml; ver W-2 sobre cobertura) |

**Totals:** 10 items | Auto: 10 (10 PASS, 0 FAIL) | Manual: 0 pending

Nota: o regex negativo do DoD-8 nao cobriria subpath actions (`owner/repo/sub@vN`); a auditoria por contagem (28 `uses:` = 28 `@sha40`) fecha essa brecha — nao ha pass oco.

## Recommendation

Aprovado com warnings — pode seguir para `/jdi-ship ci-seguranca`. Acoes recomendadas:

1. **Antes de ativar o SonarQube (token):** corrigir W-2 (opencover nunca gerado) — 1 linha no csproj ou no workflow.
2. **No PR body**, listar os `## Deferred to PR review` do CONTEXT (execucao real do Sonar, secret scanning + push protection, branch protection em `main`, Dependabot security alerts, badge do Scorecard apos 1o run) + W-1 (bump do SQLitePCLRaw quando o Dependabot abrir o PR).
3. **Oportunistico:** W-3 (env indirection no release.yml) e W-5 (corrigir o comando do Gate 1 nos docs) sao mudancas de 1 linha, candidatas a fix rapido em phase futura ou no proprio PR.
