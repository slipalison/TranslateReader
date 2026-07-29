# Phase 9: Review  (slug: pipeline-unificada)

**Verdict:** APPROVED_WITH_WARNINGS

Revisao executada em `jdi/pipeline-unificada` @ `99d418b` — confirmado identico ao head da PR #7
(`gh pr view 7 --json headRefOid` = `99d418b2b274d3c11ce78b09a9516370125abfdb`). Nenhum numero do
SUMMARY.md foi aceito sem re-execucao independente.

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `net10.0-windows10.0.19041.0` Release — **0 Erro(s)**, 40 Aviso(s) (todos `MVVMTK0045`, legado) |
| Tests | PASS | **169 aprovados / 0 falhas / 2 ignorados / 171 total** — baseline 167 (D-2) nao regrediu |
| Coverage | SKIPPED | 0 `.cs` novos pos-`4285f25` e 0 `.cs` alterados na phase — D-6 nao se aplica (esperado) |
| Lint | WARN | `dotnet format --verify-no-changes` exit **2**; 100% dos hits em arquivos legado nao tocados pela phase |
| Security/Layer | PASS | auditoria de workflow YAML-parsed: 0 blockers. Detalhe completo abaixo |
| Consistency | PASS | 8 commits, Conventional, scope `pipeline-unificada`, 1:1 com as tasks do PLAN |
| UI Validation | SKIPPED | `has_frontend=false` (cliente MAUI nativo) — permanente |
| DoD | PASS | **10/10 auto PASS**, 0 manual pendente |

---

## Auditoria de workflow (nucleo desta revisao)

Metodo: `yaml.safe_load` nos 11 arquivos + inspecao da AST (nao grep ingenuo). `actionlint` ausente
no ambiente, conforme registrado no CONTEXT.

### A. `scorecard.yml` / `release.yml` intocados — CONFIRMADO

```
$ git diff main..HEAD -- .github/workflows/scorecard.yml .github/workflows/release.yml
(vazio)
```

`git diff --name-only main..HEAD` traz exatamente os 9 workflows + 5 artefatos `.jdi/` do PLAN.
Nada fora do conjunto declarado.

### B. Double-run fechado — CONFIRMADO (YAML-parsed, nao grep)

| arquivo | `on:` (parsed) | push/PR |
|---|---|---|
| ci.yml | `[workflow_call]` | NENHUM |
| codeql.yml | `[workflow_call, schedule, workflow_dispatch]` | NENHUM |
| semgrep.yml | `[workflow_call, schedule, workflow_dispatch]` | NENHUM |
| sca.yml | `[workflow_call, workflow_dispatch, schedule]` | NENHUM |
| secret-scan.yml | `[workflow_call, schedule, workflow_dispatch]` | NENHUM |
| sonarqube.yml | `[workflow_call]` | NENHUM |
| dependency-review.yml | `[workflow_call]` | NENHUM |
| sbom.yml | `[workflow_call, workflow_dispatch, schedule]` | NENHUM |
| **pipeline.yml** | `[push, pull_request, workflow_dispatch]` | push(main)+PR |
| scorecard.yml | `[schedule, push, workflow_dispatch]` | push (original) |
| release.yml | `[push]` (tags `v*`) | push (original) |

Nenhum reusable retem `push:`/`pull_request:`/`pull_request_target:`. Superficie de evento
equivalente a de antes, sem alargamento: os 8 tinham `push: branches:[main]` + `pull_request`
(dependency-review so `pull_request`, sbom so `push:main`) e o `pipeline.yml` reproduz exatamente
isso via `if:` de caller. `schedule` dos scanners ja existia e foi preservado
(`git diff` nao contem nenhuma linha `cron`).

### C. `concurrency:` — CONFIRMADO

Presente **apenas** em `pipeline.yml`, `scorecard.yml`, `release.yml` (top-level; zero job-level em
todo o repo). Os 3 batem com o baseline: scorecard e release ja tinham o bloco antes da phase e o
diff deles e vazio. Os 8 reusables perderam o `concurrency` identico
(`${{ github.workflow }}-${{ github.ref }}` / `cancel-in-progress: true`) — sem colisao de group,
sem deadlock.

### D. Matriz de permissions caller x reusable — PASS, job a job

`pipeline.yml` top-level: `contents: read`. Comparacao por job entre o que o CALLER declara e o que
o job REAL dentro do reusable declara:

| job caller | permissions do caller | permissions do job no reusable | veredito |
|---|---|---|---|
| `ci` | `contents: read` | `test`/`build`: sem bloco -> herda top-level `contents: read` | **exato** |
| `codeql` | `contents: read`, `actions: read`, `security-events: write` | `analyze`: `security-events: write`, `actions: read`, `contents: read` | **exato** |
| `semgrep` | `contents: read`, `security-events: write` | `semgrep`: `security-events: write`, `contents: read` | **exato** |
| `sca` | `contents: read` | `sca`: `contents: read` | **exato** |
| `secret-scan` | `contents: read` | `gitleaks`/`trufflehog`: `contents: read` | **exato** |
| `sonarqube` | `contents: read` | `sonar`: sem bloco -> herda top-level `contents: read` | **exato** |
| `dependency-review` | `contents: read`, `pull-requests: write` | `dependency-review`: `contents: read`, `pull-requests: write` | **exato** |
| `sbom` | `contents: write` | `sbom`: `contents: write` | **exato** (`write` cobre `read` do checkout) |

Zero sub-declaracao (nenhum upload de SARIF ficaria 403) e zero sobre-declaracao (nenhuma
regressao de hardening vs D-2026-07-28-ci-seguranca-4). Bate 1:1 com D-2026-07-28-pipeline-unificada-3.

**Prova empirica** (nao so YAML): no run real `30445954364` os uploads de SARIF de `CodeQL / Analyze
C#` e `Semgrep / Semgrep SAST` concluiram `success`, e `SBOM` (unico com `contents: write`) foi
corretamente `skipped` em PR.

### E. `secrets: inherit` — 1 ocorrencia, no sonar

Varredura AST em todos os 11 arquivos: **exatamente 1** job com chave `secrets:` no repo inteiro —
`pipeline.yml:sonarqube -> inherit`. Nenhum outro caller herda secrets. Ver W-1 para o julgamento.

### F. Event-gating no caller, ausente nos reusables — CONFIRMADO

`github.event_name` aparece em **2 linhas em todo o repo**, ambas em `pipeline.yml`:
`:63 if: github.event_name == 'pull_request'` (dependency-review) e
`:71 if: github.event_name == 'push'` (sbom). Zero ocorrencias dentro de qualquer reusable.
Comportamento provado no run real: `Dependency Review / Dependency review` = `success` em PR,
`SBOM` = `skipped` em PR.

### G. Sonar — inputs e injection

- `grep -c "inputs\." sonarqube.yml` = **3**, todas em linhas `SONAR_PR_KEY|BRANCH|BASE:` (L60-62),
  dentro de `env:`, nunca dentro de `run:`.
- **Zero `${{ }}` em qualquer bloco `run:` dos 11 workflows** — verificado por AST (itera todo
  `jobs[*].steps[*].run`), nao por regex de linha.
- `pr-branch` vem de `github.event.pull_request.head.ref` (input atacavel) mas entra por `env:` e e
  consumido como `"$SONAR_PR_BRANCH"` dentro de array bash com aspas -> sem script injection.
- `fetch-depth: 0` intocado (L37). Os `if: env.SONAR_TOKEN != ''` de todos os steps preservados.

### H. SHA pins

Contagem via AST (`uses:` de step **e** de job): **41 usos de terceiro, 100% pinados em 40-hex.
0 tags mutaveis `@vN`. 8 usos locais `./`** (isentos por D-2026-07-28-pipeline-unificada-1c).

### I. harden-runner / persist-credentials

- Jobs `ubuntu-latest`: **10**; com `step-security/harden-runner`: **10** — 0 faltando.
- `actions/checkout@`: **12**; com `persist-credentials: false`: **12** — 0 faltando.
- Os 2 jobs nao-ubuntu (`ci.yml:build`, `release.yml:release`, ambos `windows-latest`) nao exigem
  harden-runner (a action e Linux-only) — comportamento pre-existente, nao regressao.

### J. Resolucao dos `uses: ./`

Todos os 8 resolvem para arquivo existente que declara `workflow_call:`:

```
ci -> ci.yml exists=True workflow_call=True          sonarqube -> sonarqube.yml exists=True workflow_call=True
codeql -> codeql.yml exists=True workflow_call=True  dependency-review -> ... exists=True workflow_call=True
semgrep -> semgrep.yml exists=True workflow_call=True  sbom -> sbom.yml exists=True workflow_call=True
sca -> sca.yml exists=True workflow_call=True        secret-scan -> ... exists=True workflow_call=True
```

### K. Run graph unico — CONFIRMADO na API

```
$ gh run list --branch jdi/pipeline-unificada
in_progress  ...  Pipeline  pull_request  30445954364
completed success  Pipeline  pull_request  30445084653
completed cancelled Pipeline  pull_request  30444846178
```

Somente runs `Pipeline`. Os 12 checks do Actions apontam todos para o mesmo `runs/30445954364`.
O objetivo da phase (1 run no lugar de ~8) esta objetivamente atingido.

### L. Artifacts unicos

`coverage` (ci.yml:test) e `sbom-spdx` (sbom.yml:sbom) — unicos no escopo migrado, sem colisao sob
o `run_id` agora compartilhado. (`SARIF file` em scorecard.yml segue fora do orquestrador.)

---

## Julgamento dos 2 itens sinalizados pelo doer

### Item 1 — `Semgrep OSS` vermelho: `secrets: inherit` em `pipeline.yml:59`

**Reproduzido localmente** (semgrep instalado via pip):

```
$ semgrep scan --config "p/github-actions" .github/workflows/pipeline.yml
❯❯❱ yaml.github-actions.security.secrets-inherit.secrets-inherit  ❰❰ Blocking ❱❱
   59┆ secrets: inherit
```

**Veredito: o doer agiu certo em NAO alterar; e a decisao D-2026-07-28-pipeline-unificada-4 deve
ser emendada numa fase de follow-up para o map explicito. Nao e blocker desta phase.**

Raciocinio, em tres partes:

1. **Nao ha regressao de seguranca nesta phase.** Antes da migracao, `sonarqube.yml` era disparado
   direto por `push`/`pull_request` e, como qualquer workflow do repo, podia referenciar qualquer
   `secrets.*`. `secrets: inherit` restabelece exatamente essa mesma superficie — nao a alarga.
   Alem disso `inherit` nao despeja secrets no ambiente: apenas os torna *referenciaveis*, e
   `sonarqube.yml` referencia unicamente `SONAR_TOKEN` (L27). A exposicao em runtime hoje e
   identica com e sem `inherit`. Por isso **WARN, nao BLOCK**.

2. **Mesmo assim, o map explicito e estritamente melhor — e a propria decisao locked pede isso.**
   D-2026-07-28-pipeline-unificada-4 se intitula "Secrets nao fluem implicitamente pro reusable" e
   justifica-se por "least privilege". `secrets: inherit` e precisamente o mecanismo que *derrota*
   least privilege. A decisao contradiz o proprio objetivo declarado: o argumento registrado a
   favor de `inherit` ("funciona sem o reusable declarar `on.workflow_call.secrets`") e de
   **conveniencia**, nao de seguranca. Apontar isso nao e reabrir decisao por capricho — e
   registrar que a implementacao nao atinge a meta que a decisao enunciou.

3. **O risco e futuro e previsivel, nao teorico.** `PROJECT`/`todos.md` ja contemplam assinatura e
   publicacao em loja (Play/App Store). No dia em que um `SIGNING_KEY` entrar nos secrets do repo,
   `inherit` o torna referenciavel pelo job que roda `dotnet-sonarscanner` + JRE + analisadores
   baixados em runtime e que exfiltra dados para `sonarcloud.io` por design. O `harden-runner` esta
   em `egress-policy: audit`, nao `block`, entao nao conteria exfiltracao. Atenuante real e
   legitimo: o reusable e **local** (`./`), mesmo repo e mesmo commit — a clausula mais forte da
   regra Semgrep ("or sourced from a third party") nao se aplica, e a fronteira de confianca aqui e
   o repositorio, nao o arquivo. Por isso o ganho e **defesa em profundidade**, nao correcao de
   vulnerabilidade ativa.

**Encaminhamento recomendado (follow-up, fora desta phase):**

```yaml
# pipeline.yml
    secrets:
      SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}

# sonarqube.yml — OBRIGATORIO junto, senao o call falha com
# "Invalid input, SONAR_TOKEN is not defined in the referenced workflow"
on:
  workflow_call:
    inputs: { ... }
    secrets:
      SONAR_TOKEN:
        required: false
```

Requer: emendar D-2026-07-28-pipeline-unificada-4 + reescrever o DoD 5 (o `grep -c "secrets:
inherit" == 1` e um **proxy** do objetivo "least privilege", nao o objetivo; quando proxy e meta
divergem, a meta vence — CLAUDE.md fixa a ordem Seguranca > Performance > Boas praticas). Fazer
isso dentro da phase atual reprovaria o DoD como escrito e reabriria decisao travada sem mandato:
o doer escalou corretamente em vez de decidir sozinho.

Nao bloqueia porque: `Semgrep OSS` nao e required context, o gate real (`Semgrep / Semgrep SAST`,
regras custom `.semgrep/`) esta verde, e a exposicao efetiva nao mudou (ponto 1).

### Item 2 — `CodeQL` nao precisa de remap (8 contexts, nao 9)

**Veredito: claim do doer CONFIRMADO pela API. O `branch-protection-remap.md` produz config
correta e nao-alargante.**

Verificacao independente via `gh api repos/slipalison/TranslateReader/commits/99d418b.../check-runs`:

```
CodeQL                                | app_id=57789 | github-advanced-security | success
CodeQL / Analyze C#                   | app_id=15368 | github-actions           | success
CI / Test (Linux)                     | app_id=15368 | github-actions           | success
CI / Build (Windows)                  | app_id=15368 | github-actions           | (pending, re-run)
Semgrep / Semgrep SAST                | app_id=15368 | github-actions           | success
SCA / Dependency vulnerability gate   | app_id=15368 | github-actions           | success
Secret Scan / Gitleaks                | app_id=15368 | github-actions           | success
Secret Scan / TruffleHog              | app_id=15368 | github-actions           | success
SonarQube / SonarQube Cloud scan      | app_id=15368 | github-actions           | success
Dependency Review / Dependency review | app_id=15368 | github-actions           | success
SBOM                                  | app_id=15368 | github-actions           | skipped
Semgrep OSS                           | app_id=57789 | github-advanced-security | failure
SonarCloud Code Analysis              | app_id=12526 | sonarqubecloud           | success
```

`CodeQL` e de fato `app_id 57789` (github-advanced-security), **nao** um job do Actions — o job do
Actions e `CodeQL / Analyze C#` (15368), que nunca foi required. O snapshot
`branch-protection-before.json` ja registra `{"context":"CodeQL","app_id":57789}`, coerente.
Logo **8 dos 9 contexts mudam de nome**, nao 9. `check-names-after.txt` (13 nomes) confere linha a
linha com a saida ao vivo — nao foi escrito a mao.

**Snapshot fiel:** `gh api .../branches/main/protection` ao vivo bate byte a byte com
`branch-protection-before.json` — a protection nao foi tocada por ninguem, como prometido.

**Nao-alargamento do PATCH recomendado (variante `--input` com `checks[]`):** 9 contexts antes -> 9
depois, mapeamento 1:1, `app_id` preservado em todos (57789 no CodeQL, 15368 nos 8 do Actions).
Os 9 nomes-alvo **existem todos** na captura ao vivo. Nenhum context novo foi adicionado e nenhum
removido -> superficie de gate identica, so renomeada. O arquivo tambem acerta ao **excluir**
`SBOM` (so existe em `push`; exigi-lo travaria toda PR), `CodeQL / Analyze C#` (endurecimento novo,
fora de escopo) e os checks de app nunca-required.

**Correcao tecnica do doer validada:** `-F strict=true` (tipado) e realmente necessario — `-f`
envia a string `"true"` e o endpoint responde 422 em campo boolean. A preferencia por `checks[]`
sobre `contexts[]` tambem esta certa: `contexts[]` grava `app_id: null` e afrouxa o pin de app
(qualquer app poderia satisfazer o gate). O rollback restaura exatamente o baseline.

---

## Blockers

_Nenhum._

## Warnings

- **W-1 — `secrets: inherit` deve virar map explicito (follow-up).** `pipeline.yml:59`. Ver
  "Item 1" acima para o raciocinio completo. Acao: nova phase que emende
  D-2026-07-28-pipeline-unificada-4, reescreva o DoD 5 e altere `pipeline.yml` + `sonarqube.yml`
  em conjunto. Nao fazer dentro desta phase.

- **W-2 — `CI / Build (Windows)` estava `pending` no momento da revisao** (run `30445954364`, em
  `in_progress`). O run anterior completo (`30445084653`) foi `success` com esse job em 7m35s. O
  PATCH do branch protection **so pode ser executado depois** que esse check reportar verde, senao
  o remap trava a PR num context que nunca reportou. Confirmar com `gh pr checks 7` antes do PATCH.

- **W-3 — Lint legado (Gate 4, `dotnet format` exit 2).** Hits em `Pages/ReaderPage.xaml.cs:124`,
  `test/.../HtmlInjectionTests.cs:25,42`, `ThemeEngineTests.cs:12`,
  `TranslationManagerTests.cs:528,529` — todos WHITESPACE, todos em arquivos **nao tocados por esta
  phase** (0 `.cs` no diff). Exento por D-2. Sera endereçado pela phase `baseline-de-estilo`.

- **W-4 — `catch { }` vazio legado** em `src/TranslateReader/Pages/ReaderPage.xaml.cs:326` e `:434`
  (blame: `1a078be` 2026-03-26 e `5586bae` 2026-03-24, ambos anteriores ao boundary `4285f25` de
  2026-03-31). Viola `.claude/rules/csharp.md` §1, mas e legado intocado — WARN, nao BLOCK. Os
  `catch (OperationCanceledException) { }` em `LibraryPageModel.cs:183` / `ReaderPageModel.cs:222` /
  `ReaderPage.xaml.cs:308` sao aceitaveis (cancelamento silencioso no boundary de `[RelayCommand]`).

- **W-5 — Racional de D-2026-07-28-pipeline-unificada-5 provavelmente impreciso.** A decisao afirma
  que `github.event_name` dentro de um job disparado por `workflow_call` resolve para
  `"workflow_call"`. A documentacao do GitHub descreve o `github` context como sempre associado ao
  workflow **caller**, inclusive `event_name`. **Isso nao afeta a entrega**: o `if:` foi colocado no
  caller, que e correto e estritamente mais seguro sob qualquer das duas semanticas (o PLAN ja
  registra isso na regra 8), e o comportamento foi provado no run real. Fica so como nota de
  precisao documental caso a decisao seja citada em fases futuras.

- **W-6 — O `Verify:` do DoD 9 e um proxy fraco para "100% SHA pin".** O regex
  `uses:\s*[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+@v[0-9]` so pega tags no formato `@vN`; passaria batido
  em `@main`, `@latest` ou tag sem `v`. O criterio real foi verificado por AST nesta revisao
  (41/41 em 40-hex, 0 mutaveis) e **passa de verdade** — mas o comando do DoD deveria ser endurecido
  para "todo `uses:` de terceiro casa `@[0-9a-f]{40}`" antes de ser reaproveitado noutra phase.

- **W-7 — `.jdi/phases/pipeline-unificada/LOOP.md` untracked** no working tree. Nao pertence ao
  conjunto de arquivos do PLAN; decidir se entra em commit ou no `.gitignore` antes do ship.

- **W-8 — `harden-runner` em `egress-policy: audit`** nos 10 jobs ubuntu (pre-existente, herdado de
  `ci-seguranca`, fora do escopo desta phase). Relevante como contexto de W-1: em modo `audit` a
  action registra mas nao bloqueia egresso. Endurecer para `block` e candidato a `todos.md`.

---

## DoD Checklist (gate 8)

Os 10 comandos `Verify:` do CONTEXT.md foram executados **literalmente**, em bash, um a um.

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | Double-run fechado: 8 reusables perdem push/pull_request; so pipeline.yml os tem; scorecard mantem `push: branches:[main]` | CONTEXT | Auto | PASS | exit 0 — corroborado por AST (secao B) |
| 2 | `workflow_call:` nos 8; pipeline.yml existe; scorecard/release existem | CONTEXT | Auto | PASS | exit 0 — corroborado por AST (secao B/J) |
| 3 | `workflow_dispatch:` correto por arquivo (add em codeql/semgrep/secret-scan; mantido em sca/sbom; removido de ci; nunca em dependency-review) | CONTEXT | Auto | PASS | exit 0 — confere com o diff por arquivo |
| 4 | Matriz de permissions por job caller, sem elevacao generica | CONTEXT | Auto | PASS | exit 0 — corroborado pela matriz completa (secao D) |
| 5 | `secrets: inherit` 1x, so no caller do sonar | CONTEXT | Auto | PASS | exit 0 — AST: 1 unico job com chave `secrets:` no repo. Ver W-1 |
| 6 | Sonar com `workflow_call: inputs:` e `fetch-depth: 0` intocado | CONTEXT | Auto | PASS | exit 0 — 3 inputs (L5-17), `fetch-depth: 0` em L37 |
| 7 | `if:` de evento no caller; nunca no reusable | CONTEXT | Auto | PASS | exit 0 — `event_name` so em pipeline.yml:63,71 (secao F) |
| 8 | `concurrency:` so em pipeline/scorecard/release | CONTEXT | Auto | PASS | exit 0 — AST top-level e job-level (secao C) |
| 9 | Hardening intacto: SHA pin, harden-runner, persist-credentials, permissions | CONTEXT | Auto | PASS | exit 0 — reforcado por AST: 41/41 SHA, 10/10 harden-runner, 12/12 persist-credentials. Ver W-6 |
| 10 | Snapshot do branch protection capturado ANTES de qualquer edicao | CONTEXT | Auto | PASS | exit 0 — JSON valido, 9 contexts com `app_id`, identico a protection ao vivo |

**Totals:** 10 items | Auto: 10 (10 PASS, 0 FAIL) | Manual: 0 pendente

Nenhum item manual — `/jdi-confirm-dod` nao e necessario para esta phase.

---

## Gate 6 — consistencia com o PLAN

| Commit | Tipo/scope | Task |
|---|---|---|
| `628ea97` | `chore(pipeline-unificada)` | T-1 snapshot |
| `87919bf` | `refactor(pipeline-unificada)` | T-2 ci.yml |
| `834c6f4` | `refactor(pipeline-unificada)` | T-3 4 scanners |
| `7b4674b` | `refactor(pipeline-unificada)` | T-4 sonar/dep-review/sbom |
| `74b9347` | `feat(pipeline-unificada)` | T-5 pipeline.yml |
| `00ae5cd` | `docs(pipeline-unificada)` | T-7 check names + remap |
| `2c144a6` | `docs(pipeline-unificada)` | PLAN status + SUMMARY |
| `99d418b` | `docs(pipeline-unificada)` | T-7 correcao do remap |

Todos Conventional Commits com scope = slug da phase, tipos apropriados (`refactor` para superficie
de trigger, `feat` so para o arquivo novo, `chore` para snapshot, `docs` para artefatos) — D-4 OK.
Atomicos, 1 assunto por commit. T-6 sem commit e coerente com o PLAN (`files modified: nenhum`, zero
correcoes necessarias). `files_modified` do PLAN == `git diff --name-only main..HEAD`, exatamente.

---

## Recommendation

**Aprovado para seguir com o remap e o merge**, na ordem travada
(D-2026-07-28-pipeline-unificada-1d), com uma pre-condicao:

1. Aguardar `CI / Build (Windows)` reportar verde no run `30445954364` (`gh pr checks 7`) — **W-2**.
2. So entao aplicar o PATCH da variante recomendada (`--input` com `checks[]`, que preserva
   `app_id`) de `branch-protection-remap.md`.
3. Verificar com o comando pos-PATCH do proprio arquivo (9 linhas esperadas).
4. So entao fazer o merge da PR #7.

Abrir uma phase de follow-up para **W-1** (map explicito de `SONAR_TOKEN` + emenda a
D-2026-07-28-pipeline-unificada-4 + reescrita do DoD 5) e, oportunisticamente, **W-6** (endurecer o
`Verify:` de SHA pin) e **W-8** (`egress-policy: block`). Resolver **W-7** (LOOP.md untracked) antes
do `/jdi-ship`.

Qualidade da entrega: alta. Os 10 DoD passam de verdade — reexecutados um a um e, onde o comando do
DoD era fraco (9), o criterio real foi verificado por AST e passou mesmo assim. Os dois itens que o
doer escalou eram exatamente os certos para escalar, e o achado sobre o `app_id` do `CodeQL` evitou
um remap errado que teria travado `main` de novo.
