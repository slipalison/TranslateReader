# Phase 8: Review  (slug: sast-sca-sbom)

**Verdict:** APPROVED_WITH_WARNINGS

Review iter 1 do range `1bd363f..c33e55c` (7 commits, branch `jdi/ci-seguranca`). Phase infra-only:
0 arquivos `.cs` tocados — unico legado alterado e o `TranslateReader.Core.csproj` (override de
seguranca, isencao D-2). Todos os gates executados de verdade nesta maquina (Windows, bash,
semgrep 1.159.0 via pip, `gh` autenticado).

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `dotnet restore` exit 0 + `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` — **0 erros**, 40 warnings legados (MVVMTK0045). O override da lib nativa nao quebrou o TFM Windows |
| Tests | PASS | `dotnet test` (full) exit 0; suite do projeto de teste: **169 passed, 0 failed, 2 skipped, 171 total** (`Aprovado! – Com falha: 0, Aprovado: 169, Ignorado: 2`). Baseline 167 (D-2) preservada; 2 skips = LLamaSharp model-dependent pre-existentes. Bump nativo do SQLite nao regrediu nada |
| Coverage | SKIPPED | 0 arquivos `.cs` novos pos-`4285f25` (`git log --diff-filter=A` retorna apenas workflows/`.semgrep/`/`SECURITY.md`/docs). Esperado pelo CONTEXT — nao e falha |
| Lint | WARN | `dotnet format --verify-no-changes` exit 2 — 12 errors WHITESPACE nos MESMOS 6 arquivos legados do round anterior (ThemeEngine.cs, ReadingManager.cs, ReaderPage.xaml.cs, HtmlInjectionTests, ThemeEngineTests, TranslationManagerTests). Nenhum arquivo tocado pela phase. WARN-only ate `baseline-de-estilo` (D-2) |
| Security/Layer | PASS | NU1903 **ELIMINADO** (W-1 da phase 7 fechado); listing `--vulnerable` limpo nos 3 csprojs; hardening 10/10 workflows verde (41/41 pins, 3 spot-checks contra tags live OK); semgrep gate ERROR exit 0 + exatamente os 2 WARNINGs legados previstos; parser SCA provado vermelho/verde com o script literal do workflow. Detalhe abaixo |
| Consistency | PASS | 7 commits conventional, escopo `sast-sca-sbom`, tipos variados corretamente (`fix` dep override, `feat` x4, `docs` x2 — D-4); atomicos 1:1 com T-1..T-6; T-7 auditoria sem commit (documentado no SUMMARY). `files_modified` do PLAN todos presentes no log. Diffs aditivos verificados: `secret-scan.yml` 25+/0- (job `gitleaks` intacto), csproj 4+/0- |
| UI Validation | SKIPPED | has_frontend=false (cliente MAUI nativo) — permanente |
| DoD | PASS (1 PARTIAL justificado) | 9 itens, todos Auto, 0 Manual: **8 PASS + DoD 4 PARTIAL** (clausula impossivel upstream; substancia de seguranca provada — ver Warnings). Nenhum Auto FAIL de criterio real |

## Detalhe do Gate 5 (Security/Layer + auditoria de workflows)

**SCA / GHSA-2m69-gcr7-jv3q (o ponto de risco da phase):**
- `dotnet restore --force` nos 3 csprojs (Core, app, tests): **zero warnings NU1903/NU1902** — o
  advisory que a phase 7 deixou aberto (W-1) esta fechado.
- `dotnet list <csproj> package --vulnerable --include-transitive` nos 3: "nao tem nenhum pacote
  vulneravel" — nenhuma linha `SQLitePCLRaw` (nem de nenhum outro pacote).
- Override no `TranslateReader.Core.csproj`: `SQLitePCLRaw.lib.e_sqlite3 3.53.3` top-level
  (nearest-wins), com comentario WHY citando o advisory. Unico legado tocado (D-2, prioridade 1).

**Auditoria de hardening (10 workflows, D-2026-07-28-ci-seguranca-4):**
- SHA pin: **41/41** `uses:` pinados em SHA-40 (contagens iguais), **zero** `@vN`, todos com
  comentario de versao. Confere com o antes/depois do SUMMARY (28 -> 41).
- Spot-check de 3 pins novos contra tags live via `gh api .../git/ref/tags/...`:
  `trufflesecurity/trufflehog@6f3c981e7b77f235fd2702dd74af25fc4b72bf11` = v3.96.0 ✔,
  `anchore/sbom-action@e22c389904149dbc22b58101806040fa8d37a610` = v0.24.0 ✔,
  `actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a` = v7.0.1 ✔.
- Estrutural (script python sobre YAML parseado): 10/10 com `permissions:` top-level
  (`contents: read`) + `concurrency:` com cancel-in-progress; harden-runner **1o step de todo job
  ubuntu**; `persist-credentials: false` em **todo** checkout; **zero `${{ }}` dentro de `run:`**
  (learning W-3 da phase 7 mantido).
- Elevacoes job-level, todas justificadas: `sbom.yml` `contents: write` (Dependency Submission API,
  comentado inline); `semgrep.yml` `security-events: write` (upload SARIF); demais (codeql,
  scorecard, release, dependency-review) sao legado da phase 7 ja auditado.
- YAML valido em 12 arquivos (10 workflows + dependabot + `.semgrep/dotnet-security.yml`).

**Semgrep (rodado por este reviewer, 1.159.0):**
- Gate: `semgrep scan --config .semgrep/ --severity ERROR --error --metrics=off .` ->
  `Ran 3 rules on 71 files: 0 findings.` **exit 0** — pipeline nasce verde, igual ao SUMMARY.
- Scan completo `.semgrep/`: `Ran 4 rules on 71 files: 2 findings.` — exatamente
  `translatereader-webview-js-injection` WARNING em `ReaderPage.xaml.cs:306` e `:474`, os 2
  call-sites legados sem `JsStr()` previstos no PLAN (D-2 proibe toca-los). **Nenhum desvio novo.**
- Regras cobrem os 4 riscos reais (zip-slip ERROR, XXE ERROR, WebView WARNING justificado inline,
  deserializacao ERROR), com CWE + referencias; a demote a WARNING traz o porque no proprio yml.

**Parser do sca.yml (gate que falha de verdade):**
- Script extraido **verbatim** do bloco `run:` do workflow e executado local:
  fixture com `"severity": "High"` -> `[BLOCKED] Evil.Package ... SCA gate: 1 High/Critical ...`
  **exit 1**; fixture so-Moderate -> `[info] ... no High/Critical ...` **exit 0**.
- Sem superficie de injection: `shell: python` sem nenhum `${{ }}` no bloco; severidades lidas do
  JSON do `dotnet list` (locale-independent); varre os 3 csprojs explicitamente (NETSDK1005 evitado).

**Bateria C# canonica (5.1–5.17):** 5.1/5.2/5.9/5.10 limpos; 5.11 eventos 5+=/4-=, 5.12 um static
mutavel (TranslationEngine.cs:16), 4 catch OCE + 2 catch vazios — **identico ao baseline legado**
(W-6 da phase 7), zero regressao. Esperado: a phase nao tocou `.cs`.

## Blockers

Nenhum.

## Warnings

- **W-1 (DoD 4 PARTIAL, pre-autorizado):** a clausula-grep exige `bundle_green` fora da 2.1.11, mas
  **nao existe release upstream acima de 2.1.11** (`first_patched: null` no advisory; confirmado
  pelo doer no NuGet em execucao). Julgamento deste reviewer: **justificativa VALIDA** — o advisory
  cita `lib.e_sqlite3`, e o override 3.53.3 comprova a limpeza pelos 2 criterios que importam
  (zero NU1903 no restore forcado; listing `--vulnerable --include-transitive` sem nenhuma linha nos
  3 csprojs), com suite e build Release verdes. Nao mascarado (linha do csproj intacta), documentado
  em PLAN/SUMMARY/comentario do csproj. Acao futura: se um dia sair `bundle_green` > 2.1.11
  (Dependabot avisara), remover o override e absorver o bundle.
- **W-2 (semgrep, legado):** regra WebView demovida a `severity: WARNING` por causa dos 2 call-sites
  legados `ReaderPage.xaml.cs:306` (`applyTranslations({itemsJson})`) e `:474`
  (`flushChunk('{functionName}')`) — D-2 proibe corrigi-los nesta phase. Codigo novo continua
  guardado (regra ativa + gate ERROR nas demais). Rota: futura phase de hardening de codigo, junto
  com o W-4 abaixo.
- **W-3 (lint, legado — era W-4 da phase 7):** whitespace drift nos mesmos 6 arquivos legados,
  nenhum tocado pela phase. Rota inalterada: `baseline-de-estilo`.
- **W-4 (baselines C# legados — era W-6 da phase 7):** 4 `catch (OperationCanceledException)` que
  engolem, 2 `catch { }`, 1 static mutavel, eventos 5+=/4-=. Re-conferidos, inalterados,
  pre-boundary `4285f25`.

**Fechado nesta phase:** o W-1 da review de `ci-seguranca` (NU1903 GHSA-2m69-gcr7-jv3q) — resolvido
por `1bd363f`, sem esperar o Dependabot.

## DoD Checklist (gate 8)

Todas as 9 linhas `Verify:` do CONTEXT.md executadas verbatim em bash por este reviewer.

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | `.semgrep/` cobre zip-slip, XXE, WebView, deserializacao | CONTEXT | Auto | PASS | exit 0 (4 greps casam em `.semgrep/dotnet-security.yml`) |
| 2 | `semgrep.yml`: pip, `--config auto` + `.semgrep/`, SARIF, upload categoria `semgrep` | CONTEXT | Auto | PASS | exit 0 (7 greps) |
| 3 | Gate SCA `--vulnerable --include-transitive`, falha High/Critical | CONTEXT | Auto | PASS | exit 0; parser provado exit 1 (High) / exit 0 (Moderate) |
| 4 | `bundle_green` fora da 2.1.11 + listing sem `SQLitePCLRaw` | CONTEXT | Auto | **PARTIAL** | clausula 1 FAIL (nenhum release > 2.1.11 existe upstream); clausulas 2 e 3 PASS — listing dos 3 csprojs sem linha vulneravel, zero NU1903. Justificativa aceita (ver W-1) |
| 5 | Suite verde apos o bump | CONTEXT | Auto | PASS | `Aprovado! – Com falha: 0, Aprovado: 169, Ignorado: 2, Total: 171` exit 0 |
| 6 | TruffleHog `--only-verified`, `fetch-depth: 0`, SHA-pinned | CONTEXT | Auto | PASS | exit 0; pin = tag live v3.96.0 |
| 7 | Hardening cobre workflows novos (pin/permissions/harden-runner/concurrency) | CONTEXT | Auto | PASS | exit 0; auditoria estrutural 10/10 verde |
| 8 | `sbom.yml`: sbom-action, spdx-json, `dependency-snapshot: true`, retention 30d | CONTEXT | Auto | PASS | exit 0; pin = tag live v0.24.0 |
| 9 | `SECURITY.md` com report privado via Security Advisories | CONTEXT | Auto | PASS | exit 0; em ingles, link `security/advisories/new`, response targets, scope/out-of-scope |

**Totals:** 9 items | Auto: 9 (8 PASS, 1 PARTIAL justificado, 0 FAIL de criterio real) | Manual: 0 pending

(PROJECT.md nao declara secao `## Definition of Done`; o DoD da phase vem integralmente do
CONTEXT.md — dentro do mapeamento do Gate 8, nao e INCONCLUSIVE.)

## Recommendation

Aprovado com warnings — nenhum blocker; os 4 warnings sao (1) clausula de DoD impossivel upstream
com substancia de seguranca provada e (2-4) legado ja roteado (hardening de codigo /
`baseline-de-estilo`). Pode seguir para `/jdi-ship sast-sca-sbom`. No PR: incluir os 3 itens de
`## Deferred to PR review` do CONTEXT (triagem dos findings do `--config auto` no 1o run do
Semgrep; confirmar o dependency-snapshot no Dependency Graph pos-merge; testar o fluxo do
SECURITY.md com 1 advisory real). Lembrete de manutencao: derrubar o override
`SQLitePCLRaw.lib.e_sqlite3` quando existir `bundle_green` corrigido.
