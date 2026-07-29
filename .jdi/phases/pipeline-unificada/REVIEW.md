# Phase 9: Review  (slug: pipeline-unificada)

**Round:** 2 (re-verify apos a fix round 1)
**Verdict:** APPROVED_WITH_WARNINGS

Revisao executada em `jdi/pipeline-unificada` @ `14809b3` — confirmado identico ao head da PR #7
(`gh pr view 7 --json headRefOid` = `14809b35e88192859fab97c59a24c2d9488e58b3`, `MERGEABLE`,
`OPEN`). Toda a bateria de gates foi **re-executada do zero**; nenhum numero do SUMMARY.md `## Fix
round 1` foi aceito sem reproducao independente.

Commits novos desde o round 1 (`7150d68`):

| Commit | Tipo/scope | Conteudo |
|---|---|---|
| `bf260b2` | `fix(pipeline-unificada)` | `pipeline.yml` + `sonarqube.yml` — map explicito de `SONAR_TOKEN` |
| `7a230ea` | `docs(pipeline-unificada)` | `DECISIONS.md` (append D-...-7) + `CONTEXT.md` (DoD 5 reescrito) |
| `14809b3` | `docs(pipeline-unificada)` | `SUMMARY.md` `## Fix round 1` |

---

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `net10.0-windows10.0.19041.0` Release — **0 Erro(s)**, 40 Aviso(s) (todos `MVVMTK0045`, legado) |
| Tests | PASS | **169 aprovados / 0 falhas / 2 ignorados / 171 total** — baseline 167 (D-2) nao regrediu |
| Coverage | SKIPPED | 0 `.cs` novos pos-`4285f25` e 0 `.cs` alterados na phase — D-6 nao se aplica (esperado) |
| Lint | WARN | `dotnet format --verify-no-changes` exit **2**; 13 diagnosticos `WHITESPACE`, 100% em arquivos legado nao tocados pela phase |
| Security/Layer | PASS | auditoria de workflow YAML-parsed + sweep C# de baseline: 0 blockers |
| Consistency | PASS | 11 commits, Conventional, scope `pipeline-unificada`, atomicos; 3 novos coerentes com a fix round |
| UI Validation | SKIPPED | `has_frontend=false` (cliente MAUI nativo) — permanente |
| DoD | PASS | **10/10 auto PASS**, 0 manual pendente (os 10 `Verify:` re-executados literalmente, com o 5 novo) |

Saidas reais reproduzidas:

```
$ dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0
    40 Aviso(s)
    0 Erro(s)

$ dotnet test
Aprovado!  - Com falha: 0, Aprovado: 169, Ignorado: 2, Total: 171, Duracao: 4 s

$ dotnet format --verify-no-changes ; echo $?
2   (13x "error WHITESPACE" — ThemeEngine.cs, ReadingManager.cs, ReaderPage.xaml.cs,
     HtmlInjectionTests.cs, ThemeEngineTests.cs, TranslationManagerTests.cs — todos legado)

$ git diff --name-only main..HEAD -- '*.cs' | wc -l
0
$ git log --diff-filter=A --pretty=format: --name-only 4285f25..HEAD | sort -u | grep -E '\.cs$' | wc -l
0
```

---

## 1. Correcao do secrets — o ponto central desta rodada

### 1.1 Forma da declaracao (AST, nao grep)

`yaml.safe_load` nos 11 workflows, iterando `jobs[*].secrets` e `on.workflow_call.secrets`:

```
=== jobs com chave `secrets:` no repo inteiro ===
pipeline.yml:sonarqube -> {'SONAR_TOKEN': '${{ secrets.SONAR_TOKEN }}'}

=== declaracoes workflow_call.secrets ===
sonarqube.yml -> {'SONAR_TOKEN': {'required': False}}
```

**Exatamente 1** job no repo passa secrets, e o faz de forma explicita e nominal. **Zero**
`secrets: inherit` em qualquer arquivo. O callee declara o secret, com `required: false`.

Caminho gracioso preservado: os `if: env.SONAR_TOKEN != ''` continuam em **7 steps**
(`sonarqube.yml` L44, 51, 57, 61, 78, 82, 86 — Setup Java, Setup .NET, Install scanner, Begin,
Build Core, Test, End). `harden-runner` e `Checkout` seguem sem guard, como antes. O
`env: SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}` no nivel do job (L30) esta intocado — e ele que
resolve o valor dentro do workflow chamado. `fetch-depth: 0` (L40) e os 3 inputs `pr-*` intocados.

### 1.2 Prova de runtime — Sonar ainda roda DE VERDADE

O risco real dessa mudanca era degradar silenciosamente pro no-op: se o pass-through quebrasse,
`env.SONAR_TOKEN` viraria `''`, os 7 guards pulariam e o job **ainda assim reportaria `success`**,
verde e inutil. Foi verificado passo a passo na API, nao pelo veredito do job:

```
$ gh api .../actions/runs/30447048593/jobs   # head 14809b3, o novo
SonarQube / SonarQube Cloud scan
  3. Harden runner              => success
  4. Checkout                   => success
  5. Setup Java                 => success     <- guardado por env.SONAR_TOKEN != ''
  6. Setup .NET                 => success     <- guardado
  7. Install dotnet-sonarscanner=> success     <- guardado
  8. Begin SonarQube analysis   => success     <- guardado
  9. Build Core                 => success     <- guardado
 10. Test with OpenCover coverage => success   <- guardado
 11. End SonarQube analysis     => success     <- guardado
```

Nenhum `skipped`. Os 7 steps guardados executaram, logo `env.SONAR_TOKEN` resolveu para
nao-vazio dentro do reusable — o pass-through explicito funciona.

**Comparacao com o head anterior** (`99d418b`, ainda com `secrets: inherit`), run `30445954364`:
sequencia de steps **identica**, todos `success`, zero `skipped`. Duracao 89s (antes) vs **81s**
(depois) — mesma ordem de grandeza; um no-op levaria ~15-20s (so harden-runner + checkout).
**Nao houve degradacao.**

**Confirmacao independente, fora do Actions:** o check `SonarCloud Code Analysis`
(`app_id=12526`, `sonarqubecloud`, apontando para
`sonarcloud.io/dashboard?id=slipalison_TranslateReader&pullRequest=7`) esta **`success`** no head
`14809b3`. Esse check so existe se o SonarCloud recebeu e processou a analise — prova
ponta-a-ponta de que o token chegou ao scanner.

### 1.3 `Semgrep OSS` — de vermelho para verde

Round 1: `Semgrep OSS` = `failure` (regra `yaml.github-actions.security.secrets-inherit` em
`pipeline.yml:59`). Agora:

```
$ gh pr checks 7
Semgrep OSS   pass   4s   .../runs/90560028852

$ semgrep scan --config "p/github-actions" .github/workflows/     # reproduzido local, pip
  Scanning 11 files with 11 yaml rules.
  Findings: 0 (0 blocking)
```

**Nao e blocker, nao e warning — RESOLVIDO.**

---

## 2. Honestidade da reescrita do DoD 5

O item de DoD foi reescrito pelo mesmo agente que mudou o codigo. Isso e exatamente o padrao que
transforma um gate em teatro, entao foi testado adversarialmente, nao apenas lido.

**Antes** (2 clausulas, ambas grep de texto):
```
test "$(grep -c "secrets: inherit" pipeline.yml)" = "1" && grep -B10 "secrets: inherit" pipeline.yml | grep -q "sonarqube.yml"
```

**Depois** (4 clausulas, uma delas AST):
```
! grep -rq "secrets: inherit" .github/workflows/                                  # (a) zero repo-wide
&& test "$(grep -Fc 'SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}' pipeline.yml)" = "1" # (b) exatamente 1 pass-through
&& grep -F -B10 '...' pipeline.yml | grep -q "sonarqube.yml"                       # (c) ancorado no job do sonar
&& python -c "...yaml.safe_load... 'SONAR_TOKEN' in workflow_call.secrets"         # (d) declaracao no callee
```

**Teste de discriminacao (o que prova que nao e vacuo):**

```
$ git archive 99d418b .github/workflows | tar -x -C /tmp/old   # arvore PRE-fix
$ cd /tmp/old && <comando NOVO do DoD 5>
NEW-DoD5-on-OLD-TREE EXIT=1     <- reprova o estado antigo

$ cd repo && <comando ANTIGO do DoD 5>
OLD-DoD5-on-NEW-TREE EXIT=1     <- o proxy antigo ficou insatisfazivel
```

O `Verify:` novo **reprova** a arvore que o antigo aprovava, e o antigo **reprova** a arvore nova.
Ou seja: a reescrita foi necessaria (nao havia como manter os dois) e o criterio novo e
estritamente mais forte — 4 clausulas contra 2, cobrindo caller **e** callee, uma delas por
parsing de YAML e nao por regex de linha. **Nao e mais fraco, nao e vacuo.** Residuo menor
registrado em W-9.

---

## 3. Legitimidade da D-2026-07-28-pipeline-unificada-7

- **Append-only respeitado.** `git diff main..HEAD -- .jdi/DECISIONS.md | grep "^-"` retorna
  **zero linhas** removidas/alteradas. O diff e 100% adicao no fim do arquivo (linha 194+).
- **D-...-4 verbatim intacta.** O texto integral da `-4` foi lido no arquivo atual e continua
  identico, inclusive a frase "`secrets: inherit` funciona mesmo sem o reusable declarar
  `on.workflow_call.secrets`" — nao houve tentativa de reescrever a historia.
- **Supersessao explicita.** A `-7` abre com "SUPERSEDE a clausula `secrets: inherit` da
  D-2026-07-28-pipeline-unificada-4" e delimita o escopo ("o resto da D-...-4 — inputs explicitos
  de PR-context e `fetch-depth: 0` intocado — continua valendo integralmente"). O bullet
  correspondente foi adicionado ao CONTEXT.md **sem** alterar o bullet da `-4`.
- **Raciocinio registrado.** As tres partes (sem regressao hoje / a `-4` se auto-contradizia /
  risco futuro previsivel com `SIGNING_KEY`), o atenuante (callee local `uses: ./` → defesa em
  profundidade, nao vulnerabilidade ativa) e a regra de decisao (proxy vs meta; Seguranca >
  Performance > Boas praticas) estao todos no texto. Correspondem ao que o round 1 levantou.

---

## 4. Regressao de distancia de grep (regra 6 do PLAN)

A fix round trocou 1 linha por 2 em `pipeline.yml`, deslocando tudo a partir da L59. Todas as
ancoras `-B` foram re-medidas empiricamente:

| Verify | ancora (match) | alvo `-B` | distancia | janela | margem |
|---|---|---|---|---|---|
| DoD 4 | `codeql.yml` @29 | `security-events: write` @28 | 1 | `-B10` | 9 |
| DoD 4 | `semgrep.yml` @36 | `security-events: write` @35 | 1 | `-B10` | 9 |
| DoD 4 | `sbom.yml` @75 | `contents: write` @74 | 1 | `-B10` | 9 |
| DoD 4 | `dependency-review.yml` @68 | `pull-requests: write` @67 | 1 | `-B10` | 9 |
| **DoD 5** | `SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}` @60 | `sonarqube.yml` @54 | **6** (era 5) | `-B10` | 4 |
| DoD 7 | `dependency-review.yml` @68 | `if: ...pull_request` @64 | 4 | `-B5` | **1** |
| DoD 7 | `sbom.yml` @75 | `if: ...push` @72 | 3 | `-B5` | 2 |

Claim do doer **confirmado**: so a DoD 5 mudou (5→6), porque a linha extra caiu dentro do proprio
bloco do job sonar; as demais ancoras sao pares intra-bloco que se deslocaram juntos. Todas
passam. A margem de 1 linha da DoD 7 (dependency-review) e pre-existente e fragil — ver W-10.

---

## 5. Sweep de hardening — re-executado por AST

`yaml.safe_load` nos 11 arquivos, iterando `jobs[*].uses` **e** `jobs[*].steps[*].uses`:

| Check | Resultado | Esperado (doer) |
|---|---|---|
| `uses:` de terceiro | **41**, 100% em 40-hex, **0 nao-pinados**, 0 tags mutaveis | 41 |
| `uses: ./` locais | **8** | 8 |
| Jobs `ubuntu-latest` com `harden-runner` | **10 / 10**, 0 faltando | 10 |
| `actions/checkout@` com `persist-credentials: false` | **12 / 12**, 0 faltando | 12 |
| `${{ }}` dentro de qualquer `run:` | **NENHUM** | 0 |
| `concurrency:` | so `pipeline.yml`, `scorecard.yml`, `release.yml` (top-level); **0 job-level** no repo | idem |
| `push`/`pull_request`/`pull_request_target` nos 8 reusables | **NENHUM** | idem |
| `uses: ./` resolvem | **8/8** existem e declaram `workflow_call` | 8/8 |
| `git diff main..HEAD -- scorecard.yml release.yml` | **VAZIO** | vazio |

Superficie de evento (parsed):

```
ci.yml                [workflow_call]                              push/PR=NONE
codeql.yml            [workflow_call, schedule, workflow_dispatch]  push/PR=NONE
dependency-review.yml [workflow_call]                               push/PR=NONE
sbom.yml              [workflow_call, workflow_dispatch, schedule]  push/PR=NONE
sca.yml               [workflow_call, workflow_dispatch, schedule]  push/PR=NONE
secret-scan.yml       [workflow_call, schedule, workflow_dispatch]  push/PR=NONE
semgrep.yml           [workflow_call, schedule, workflow_dispatch]  push/PR=NONE
sonarqube.yml         [workflow_call]                               push/PR=NONE
pipeline.yml          [push, pull_request, workflow_dispatch]       push(main)+PR
scorecard.yml         [schedule, push, workflow_dispatch]           push (original, intocado)
release.yml           [push]                                        push tags v* (original, intocado)
```

Matriz de permissions caller x reusable (top-level do `pipeline.yml`: `contents: read`):

| job caller | permissions do caller | permissions do job no reusable | veredito |
|---|---|---|---|
| `ci` | `contents: read` | `test`/`build`: herdam top-level `contents: read` | exato |
| `codeql` | `contents: read`, `actions: read`, `security-events: write` | `analyze`: os mesmos 3 | exato |
| `semgrep` | `contents: read`, `security-events: write` | `semgrep`: os mesmos 2 | exato |
| `sca` | `contents: read` | `sca`: `contents: read` | exato |
| `secret-scan` | `contents: read` | `gitleaks`/`trufflehog`: `contents: read` | exato |
| `sonarqube` | `contents: read` | `sonar`: herda top-level `contents: read` | exato |
| `dependency-review` | `contents: read`, `pull-requests: write` | `dependency-review`: os mesmos 2 | exato (`if: pull_request`) |
| `sbom` | `contents: write` | `sbom`: `contents: write` | exato (`if: push`) |

Zero sub-declaracao, zero sobre-declaracao. Bate com D-2026-07-28-pipeline-unificada-3.

Sweep C# (gate 5) — **0 `.cs` tocados**, entao o baseline foi so reconfirmado, sem achado novo:
5.1 sem hits, 5.2 sem hits, 5.10 sync-over-async sem hits, 5.12 unico static mutavel legado
(`TranslationEngine.cs:16`), 5.15 `catch { }` legado (`ReaderPage.xaml.cs:326,434`), 5.9 sem hits,
`+=`/`-=` = 5/4 (imbalance legado de bootstrap).

---

## 6. Estado ao vivo da PR #7 (head `14809b3`)

```
CodeQL                                | app=57789 github-advanced-security | success
CodeQL / Analyze C#                   | app=15368 github-actions           | success
CI / Test (Linux)                     | app=15368 github-actions           | success
CI / Build (Windows)                  | app=15368 github-actions           | success   <- concluiu durante a revisao
Semgrep / Semgrep SAST                | app=15368 github-actions           | success
Semgrep OSS                           | app=57789 github-advanced-security | success   <- era failure
SCA / Dependency vulnerability gate   | app=15368 github-actions           | success
Secret Scan / Gitleaks                | app=15368 github-actions           | success
Secret Scan / TruffleHog              | app=15368 github-actions           | success
SonarQube / SonarQube Cloud scan      | app=15368 github-actions           | success
SonarCloud Code Analysis              | app=12526 sonarqubecloud           | success
Dependency Review / Dependency review | app=15368 github-actions           | success
SBOM                                  | app=15368 github-actions           | skipped   <- correto em PR
```

**13 check-runs: 12 `success` + 1 `skipped` (SBOM, correto em PR). Zero `failure`, zero pendente.**
Todos os 9 contexts hoje required em `main` (sob os nomes antigos) tem correspondente verde sob o
nome novo — o remap pode ser aplicado com seguranca.

Run graph unico preservado: **todos** os jobs do Actions estao no run `30447048593`
(`gh run list --branch jdi/pipeline-unificada` traz somente runs `Pipeline`). Objetivo da phase
segue atingido.

**Branch protection nao foi tocada:** `gh api .../branches/main/protection` comparado por
`json.load` com `branch-protection-before.json` → `identical = True`. Os 9 contexts e seus
`app_id` seguem exatamente como no snapshot.

---

## Blockers

_Nenhum._

---

## Changelog de warnings do round 1

**RESOLVIDAS (2):**

- **W-1 — `secrets: inherit`.** Fechada por `bf260b2` (map explicito nos dois arquivos) +
  `7a230ea` (D-...-7 append-only + DoD 5 reescrito). Verificado: 0 `inherit` no repo, 1
  pass-through explicito, declaracao no callee com `required: false`, Sonar rodando em modo real
  no run `30447048593`, `Semgrep OSS` verde, `semgrep p/github-actions` com 0 findings.

- **W-2 — `CI / Build (Windows)` pendente.** Concluiu **`success`** no run `30447048593` durante
  esta revisao (polling ate `completed/success`). A pre-condicao do remap esta satisfeita: os 13
  check-runs do head `14809b3` sao 12 `success` + 1 `skipped`, nenhum pendente ou vermelho.

**AINDA ABERTAS (6 do round 1 + 2 novas):** abaixo.

## Warnings

- **W-3 — Lint legado (Gate 4, `dotnet format` exit 2).** 13 diagnosticos `WHITESPACE` em
  `ThemeEngine.cs:12,14`, `ReadingManager.cs:54`, `Pages/ReaderPage.xaml.cs:122,124`,
  `test/.../HtmlInjectionTests.cs:25,42`, `ThemeEngineTests.cs:12`,
  `TranslationManagerTests.cs:528,529` — todos em arquivos **nao tocados por esta phase**
  (0 `.cs` no diff `main..HEAD`). Isento por D-2. Endereçado pela phase `baseline-de-estilo`.

- **W-4 — `catch { }` vazio legado** em `src/TranslateReader/Pages/ReaderPage.xaml.cs:326` e
  `:434` (anteriores ao boundary `4285f25`). Viola `.claude/rules/csharp.md` §1, mas e legado
  intocado — WARN, nao BLOCK. Os `catch (OperationCanceledException) { }` em
  `LibraryPageModel.cs:183` / `ReaderPageModel.cs:222` / `ReaderPage.xaml.cs:308` seguem
  aceitaveis (cancelamento silencioso no boundary de `[RelayCommand]`).

- **W-5 — Racional de D-2026-07-28-pipeline-unificada-5 provavelmente impreciso.** A decisao
  afirma que `github.event_name` dentro de um job disparado por `workflow_call` resolve para
  `"workflow_call"`; a documentacao do GitHub descreve o contexto `github` como sempre associado
  ao workflow **caller**. Nao afeta a entrega — o `if:` esta no caller, que e correto sob qualquer
  das duas semanticas, e o comportamento foi provado no run real (`Dependency review` = success em
  PR, `SBOM` = skipped em PR). Nota de precisao documental; a `-7` nao mexeu nisso.

- **W-6 — O `Verify:` do DoD 9 e um proxy fraco para "100% SHA pin".** O regex
  `uses:\s*[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+@v[0-9]` so pega tags no formato `@vN`; passaria batido
  em `@main`, `@latest` ou tag sem `v`. O criterio real foi verificado por AST nesta rodada (41/41
  em 40-hex, 0 mutaveis) e passa de verdade — mas o comando deveria virar "todo `uses:` de
  terceiro casa `@[0-9a-f]{40}`" antes de ser reaproveitado noutra phase.

- **W-7 — `.jdi/phases/pipeline-unificada/LOOP.md` untracked** no working tree (reconfirmado:
  `git status --porcelain` traz so essa linha). Nao pertence ao conjunto de arquivos do PLAN;
  decidir se entra em commit ou no `.gitignore` antes do ship. Nota adicional: existe um
  `stash@{0}` ("slnx rewrite by IDE (pre-loop pipeline-unificada)") — decidir tambem o destino
  dele antes do ship.

- **W-8 — `harden-runner` em `egress-policy: audit`** nos 10 jobs ubuntu (pre-existente, herdado
  de `ci-seguranca`, fora do escopo desta phase). Em modo `audit` a action registra mas nao
  bloqueia egresso. Endurecer para `block` e candidato a `todos.md`. Relevancia reduzida agora que
  o `inherit` saiu, mas continua valendo.

- **W-9 (nova) — residuo de proxy no DoD 5 reescrito.** Duas frestas, ambas verificadas por AST
  nesta revisao e ambas OK hoje, mas nao provadas pelo proprio comando:
  (a) `! grep -rq "secrets: inherit"` e literal — `secrets:  inherit` (2 espacos) ou
  `secrets: 'inherit'` passariam batido; o robusto seria checar por AST que nenhum
  `jobs[*].secrets` tem valor escalar `inherit`.
  (b) o texto do criterio afirma `required: false`, mas a clausula python so verifica a
  **presenca** da chave `SONAR_TOKEN` em `workflow_call.secrets`, nao o valor de `required`.
  Mesma familia da W-6; nao invalida o PASS de hoje (AST confirmou `{'SONAR_TOKEN': {'required':
  False}}` e zero `inherit`), mas endurecer junto com a W-6.

- **W-10 (nova) — margem de 1 linha na ancora `-B5` do DoD 7.** `grep -B5 "dependency-review.yml"`
  precisa alcancar o `if:` a 4 linhas de distancia. Qualquer linha inserida dentro do bloco do job
  `dependency-review` de `pipeline.yml` (ex.: um `with:`, um comentario, uma permission a mais)
  quebra o DoD sem quebrar o workflow — falso negativo. Exatamente a classe de fragilidade que a
  regra 6 do PLAN existe para pegar, e que esta fix round quase acionou. Sugestao: `-B10` ou, de
  preferencia, verificacao por AST (`jobs['dependency-review'].if`).

---

## DoD Checklist (gate 8)

Os 10 comandos `Verify:` do CONTEXT.md foram extraidos programaticamente do arquivo e executados
**literalmente**, em bash, um a um, com captura de exit code.

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | Double-run fechado: 8 reusables perdem push/pull_request; so pipeline.yml os tem; scorecard mantem `push: branches:[main]` | CONTEXT | Auto | PASS | `EXIT=0` — corroborado por AST (§5) |
| 2 | `workflow_call:` nos 8; pipeline.yml existe; scorecard/release existem | CONTEXT | Auto | PASS | `EXIT=0` — 8/8 `uses: ./` resolvem para declarante de `workflow_call` |
| 3 | `workflow_dispatch:` correto por arquivo | CONTEXT | Auto | PASS | `EXIT=0` — confere com a tabela de triggers parseada |
| 4 | Matriz de permissions por job caller, sem elevacao generica | CONTEXT | Auto | PASS | `EXIT=0` — corroborado pela matriz caller x callee (§5) |
| 5 | **Least privilege real: zero `secrets: inherit`; 1 pass-through explicito de `SONAR_TOKEN`; declaracao no `workflow_call` do callee** (reescrito, D-...-7) | CONTEXT | Auto | PASS | `EXIT=0` — AST: 1 unico job com `secrets:`, valor explicito; callee `{'SONAR_TOKEN': {'required': False}}`. Discriminacao provada em §2 |
| 6 | Sonar com `workflow_call: inputs:` e `fetch-depth: 0` intocado | CONTEXT | Auto | PASS | `EXIT=0` — 3 inputs `pr-*` (L5-17), `fetch-depth: 0` (L40) |
| 7 | `if:` de evento no caller; nunca no reusable | CONTEXT | Auto | PASS | `EXIT=0` — `event_name` so em `pipeline.yml:64,72`. Ver W-10 |
| 8 | `concurrency:` so em pipeline/scorecard/release | CONTEXT | Auto | PASS | `EXIT=0` — AST top-level e job-level; 0 job-level no repo |
| 9 | Hardening intacto: SHA pin, harden-runner, persist-credentials, permissions | CONTEXT | Auto | PASS | `EXIT=0` — reforcado por AST: 41/41 SHA, 10/10 harden-runner, 12/12 persist-credentials. Ver W-6 |
| 10 | Snapshot do branch protection capturado ANTES de qualquer edicao | CONTEXT | Auto | PASS | `EXIT=0` — JSON valido e **identico** a protection ao vivo (`json.load` comparado: `identical = True`) |

**Totals:** 10 items | Auto: 10 (10 PASS, 0 FAIL) | Manual: 0 pendente

Nenhum item manual — `/jdi-confirm-dod` nao e necessario para esta phase.

---

## Gate 6 — consistencia com o PLAN

Os 8 commits originais permanecem como validados no round 1 (Conventional, scope = slug, tipos
apropriados, atomicos, `files_modified` do PLAN == `git diff --name-only main..HEAD`). Os 3
commits da fix round:

| Commit | Tipo/scope | Arquivos | Atomico? |
|---|---|---|---|
| `bf260b2` | `fix(pipeline-unificada)` | `pipeline.yml`, `sonarqube.yml` | sim — os 2 **precisam** mudar juntos (senao o call falha) |
| `7a230ea` | `docs(pipeline-unificada)` | `DECISIONS.md`, `CONTEXT.md` | sim — 1 assunto: registrar a decisao e o criterio |
| `14809b3` | `docs(pipeline-unificada)` | `SUMMARY.md` | sim |

Tipo `fix` correto para a mudanca de comportamento de seguranca; `docs` correto para artefatos
`.jdi/`. Scope = slug da phase em todos. D-4 OK. Nenhum arquivo fora do escopo autorizado da fix
round (`scorecard.yml`, `release.yml`, `TranslateReader.slnx` intocados; `git diff main..HEAD`
continua nos mesmos 9 workflows + artefatos `.jdi/`).

---

## Recommendation

**Aprovado.** A W-1 foi corrigida da forma certa: a mudanca minima nos dois arquivos que precisam
mudar juntos, com a decisao registrada por append (sem reescrever a `-4`) e o criterio de DoD
endurecido em vez de afrouxado — e, o mais importante, **provado em runtime que o Sonar continua
rodando de verdade**, o unico jeito dessa correcao dar errado silenciosamente.

Ordem de execucao (travada em D-2026-07-28-pipeline-unificada-1d) — a pre-condicao de checks
verdes (ex-W-2) **ja esta satisfeita**:

1. Resolver **W-7**: commitar ou ignorar `LOOP.md`; decidir o destino do `stash@{0}`.
2. Aplicar o PATCH de `branch-protection-remap.md` na variante `--input` com `checks[]` (preserva
   `app_id`; `contexts[]` gravaria `app_id: null` e afrouxaria o pin).
3. Verificar com o comando pos-PATCH do proprio arquivo (9 linhas esperadas).
4. So entao merge da PR #7.

Follow-up (fora desta phase, agrupavel numa unica phase de hardening de DoD/CI): **W-6** + **W-9**
(trocar os `Verify:` de proxy textual por asserção AST), **W-10** (`-B5` → AST ou `-B10`), **W-8**
(`egress-policy: block`), **W-5** (nota de precisao na D-...-5).

Qualidade da entrega: alta, e mais alta que no round 1. Os 10 DoD passam de verdade — o item 5, o
unico reescrito pelo proprio autor da correcao, foi submetido a teste de discriminacao contra a
arvore antiga e reprovou-a, o que descarta reescrita complacente.
