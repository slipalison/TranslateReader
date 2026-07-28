# Phase 8: Suplemento SAST/SCA/SBOM (paridade simulator-ccb) — Plan  (slug: sast-sca-sbom)

## Goal
Semgrep SAST com regras custom (zip-slip/XXE/WebView/deserializacao), gate SCA nativo dotnet (bloqueia
CVE HIGH/CRITICAL), bump do SQLitePCLRaw (GHSA-2m69-gcr7-jv3q), TruffleHog verified, SBOM Syft e
SECURITY.md. **Specialist (todas as tasks):** `jdi-doer-translatereader` (glob `**/*`).

## Locked decisions (de CONTEXT.md)
- **D-...-sast-sca-sbom-1:** escopo fechado — nao re-levantar DAST/ZAP, Trivy Image/FS, Dockle, Checkov.
- **D-...-ci-seguranca-4:** hardening obrigatorio vale para os workflows novos desta phase.
- **D-2:** so o `TranslateReader.Core.csproj` pode ser tocado no legado (seguranca = prioridade 1).
  **Nenhum `.cs` legado e alterado** — nem para calar finding de Semgrep.
- **D-4:** conventional commits, escopo `sast-sca-sbom`, 1 task = 1 commit, mensagem em ingles.

## Convencoes obrigatorias (valem para T-2..T-7, nao repetidas por task)
1. `permissions:` no topo com `contents: read`; cada job eleva so o que precisa.
2. `concurrency: { group: "${{ github.workflow }}-${{ github.ref }}", cancel-in-progress: true }`.
3. `step-security/harden-runner` (`egress-policy: audit`) como 1o step de todo job `ubuntu-latest`.
4. Action de terceiro pinada por SHA de 40 hex + comentario da versao, nunca `@vN`. Resolver com
   `gh api repos/<owner>/<repo>/git/ref/tags/<tag> --jq .object.sha`; reusar os SHAs ja pinados em
   `.github/workflows/` (harden-runner, checkout, setup-dotnet, upload-artifact, codeql-action).
5. `actions/checkout` com `persist-credentials: false` (requisito Scorecard).
6. **Nunca interpolar `${{ }}` dentro de `run:`** — sempre via `env:` (script injection; W-3 da phase 7).
7. Validar YAML antes de commitar (actionlint ausente):
   `python -c "import yaml,sys;[yaml.safe_load(open(p)) for p in sys.argv[1:]]" <files>`.

**Ambiente:** Windows; bash + pwsh; `python`+`pyyaml`; `pip` (semgrep instalavel); actionlint ausente;
`npx jdi-cli` quebrado; `gh` autenticado. Greps: `sls '<re>' <path>` (pwsh) / `grep -E` (bash).

## Tasks

### Wave 1 (paralelizavel — `files_modified` disjuntos)

#### T-1: Tirar SQLitePCLRaw da versao vulneravel (GHSA-2m69-gcr7-jv3q)
- **Files:** `src/TranslateReader.Core/TranslateReader.Core.csproj`
- **Como:** resolver versoes ANTES de editar —
  `curl -s https://api.nuget.org/v3-flatcontainer/sqlitepclraw.{bundle_green,lib.e_sqlite3}/index.json`.
  Primario: subir `bundle_green` para a menor versao que ja traga `lib.e_sqlite3` patched.
  **Risco confirmado na pesquisa:** ate 2025-03 o ultimo `bundle_green` estavel era o proprio **2.1.11**,
  enquanto `lib.e_sqlite3` seguiu para a serie 3.x (3.50.3+, versao = versao do SQLite). Sem release novo
  do bundle, usar o fallback vendor-blessed: manter `bundle_green` e adicionar top-level
  `<PackageReference Include="SQLitePCLRaw.lib.e_sqlite3" Version="<3.x patched>" />`, que sobrepoe a
  transitiva por nearest-wins. Registrar caminho + versoes no SUMMARY.md. **Nao reformatar a linha so
  para satisfazer o grep** — se o DoD 4 ficar vermelho por falta de release, documentar, nao mascarar.
- **Acceptance:**
  - `dotnet restore src/TranslateReader.Core/TranslateReader.Core.csproj` sem `NU1903`/`NU1902`
  - `dotnet list src/TranslateReader.Core/TranslateReader.Core.csproj package --vulnerable
    --include-transitive` nao lista nenhuma linha `SQLitePCLRaw`
  - `dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj --nologo` -> **167 passed, 0 failed**
  - `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` OK
- **Deps:** none | **Test:** restore + `--vulnerable` + suite | **DoD:** 4, 5 | **Status:** pending

#### T-3: Regras Semgrep custom + workflow `semgrep.yml`
- **Files:** `.semgrep/dotnet-security.yml`, `.github/workflows/semgrep.yml`
- **Como:** 4 regras `languages: [csharp]`: **zip-slip** (`ExtractToFile`/`Path.Combine` com
  `entry.FullName` sem validar escape), **xxe** (`XmlReaderSettings` sem `DtdProcessing.Prohibit` ou com
  `XmlResolver`, `XmlDocument.LoadXml`), **webview-js-injection** (`EvaluateJavaScriptAsync` com
  interpolacao/concatenacao), **insecure-deserialization** (`BinaryFormatter`, `TypeNameHandling`).
  **Achado que decide a regra WebView:** `src/TranslateReader/Pages/ReaderPage.xaml.cs` JA usa
  `EvaluateJavaScriptAsync($"...")` em 7 pontos (121, 122, 306, 445, 456, 467, 474); quase todos passam
  pelo encoder `JsStr(...)` (`JsonSerializer.Serialize`), mas `:306` (`applyTranslations({itemsJson})`) e
  `:474` (`flushChunk('{functionName}')`) interpolam cru. Logo a regra PRECISA de `pattern-not` para
  argumento envolto em `JsStr(...)`; e como D-2 proibe tocar o `.cs` legado, se sobrar finding a regra
  WebView entra como `severity: WARNING` com justificativa inline — `ERROR` so em regra limpa no tree.
  Workflow: ubuntu-latest; `push[main] + pull_request + schedule` semanal; `security-events: write` +
  `contents: read`; `pip install semgrep`; step informacional `semgrep --config auto --config .semgrep/
  --sarif --output semgrep.sarif` (`continue-on-error: true`) -> `github/codeql-action/upload-sarif`
  (`category: semgrep`, `if: always()`) -> gate `semgrep --config .semgrep/ --severity ERROR --error .`.
  Se o run local provar `auto` limpo em ERROR, incluir `auto` no gate; senao `auto` fica informacional
  (triagem dos findings do registry ja deferida ao PR review).
- **Acceptance:**
  - os 4 greps do DoD 1 casam em `.semgrep/`; os 7 greps do DoD 2 casam em `semgrep.yml`
  - **regras vivas:** `semgrep --config .semgrep/ <dir sintetico no scratchpad>` da >= 1 finding para CADA
    uma das 4 regras (snippets vulneraveis fora do repo, nunca dentro)
  - **nasce verde:** `semgrep --config .semgrep/ --severity ERROR --error .` sai 0 no tree atual, saida no
    SUMMARY.md. Se `pip install semgrep` falhar: `yaml.safe_load` nas regras + registrar run pendente
    (nao inventar PASS)
- **Deps:** none | **Test:** semgrep local (positivo em fixture + limpo no repo) + greps | **DoD:** 1, 2
- **Status:** pending

#### T-4: TruffleHog `--only-verified` como job em `secret-scan.yml`
- **Files:** `.github/workflows/secret-scan.yml`
- **Justificativa (escolha pedida):** 2o job no workflow existente, nao arquivo novo — mesma preocupacao,
  mesmos triggers, mesmo `contents: read`, um unico `concurrency`; arquivo separado duplicaria os 3 sem
  ganho. Complementa o gitleaks (regex) com verificacao ativa de credencial.
- **Como:** job `trufflehog` paralelo ao `gitleaks`: harden-runner -> checkout `fetch-depth: 0` +
  `persist-credentials: false` -> `trufflesecurity/trufflehog@<sha40> # vX.Y.Z` com `extra_args:
  --only-verified` e `base: ""` (historico completo; com `base == head` num push de 1 commit a action
  falha por range vazio).
- **Acceptance:**
  - os 4 greps do DoD 6 casam (incl. o negativo: nenhum `trufflesecurity/trufflehog@v[0-9]`)
  - `git diff` **so adiciona** o bloco do job novo (job `gitleaks` intacto); YAML valida
- **Deps:** none | **Test:** parser YAML + greps do DoD | **DoD:** 6 | **Status:** pending

#### T-5: Workflow `sbom.yml` (Syft SPDX + dependency snapshot)
- **Files:** `.github/workflows/sbom.yml`
- **Como:** ubuntu-latest; `push[main] + workflow_dispatch + schedule` semanal; top-level `contents: read`
  e **`contents: write` apenas no job** (o `dependency-snapshot` usa a Dependency Submission API);
  harden-runner -> checkout -> `anchore/sbom-action@<sha40>` (`format: spdx-json`, `output-file:
  sbom.spdx.json`, `dependency-snapshot: true`, **`upload-artifact: false`**) -> `actions/upload-artifact`
  com `retention-days: 30`. **Por que o upload manual:** a input nativa da action chama-se
  `upload-artifact-retention` e NAO casa o `grep "retention-days: 30"` do DoD 8. Informacional: nada falha.
- **Acceptance:**
  - os 4 greps do DoD 8 casam em `.github/workflows/sbom.yml`
  - `contents: write` aparece **apenas** no bloco do job; nenhum `exit 1`/`fail-on`; YAML valida
- **Deps:** none | **Test:** parser YAML + greps do DoD | **DoD:** 8 | **Status:** pending

#### T-6: `SECURITY.md` na raiz (politica de report)
- **Files:** `SECURITY.md`
- **Como:** **em ingles**. Secoes: *Supported Versions*; *Reporting a Vulnerability* via GitHub Security
  Advisories (aba Security -> "Report a vulnerability",
  `https://github.com/slipalison/TranslateReader/security/advisories/new`), explicitando **nao abrir issue
  publica**; *Response targets* (ack <= 72h, triagem <= 7 dias, disclosure coordenado); *Scope* (EPUB nao
  confiavel, bridge JS do WebView, download do modelo GGUF, dados locais em SQLite) e *Out of scope* (sem
  bug bounty; falha em dependencia de terceiro vai ao upstream). Vira PASS no check Security-Policy do
  Scorecard entregue na phase 7.
- **Acceptance:**
  - DoD 9 passa (`find . -maxdepth 2 -iname "SECURITY.md"` + `security advisor` + `report|respons`)
  - `sls 'security/advisories/new' SECURITY.md` casa; texto 100% em ingles; sem e-mail pessoal
- **Deps:** none | **Test:** greps do DoD + leitura | **DoD:** 9 | **Status:** pending

### Wave 2

#### T-2: Gate SCA nativo dotnet (`--vulnerable --include-transitive`)
- **Files:** `.github/workflows/sca.yml`
- **Como:** ubuntu-latest; `push[main] + pull_request + schedule` semanal + `workflow_dispatch`;
  `contents: read`; harden-runner -> checkout -> setup-dotnet `10.0.x` -> `dotnet workload install
  maui-android` (no Linux o app csproj resolve so para `net10.0-android`, TFM que exige workload; Core e
  Tests sao `net10.0` puro) -> gate varrendo os **3 csprojs explicitamente** (nunca a solution —
  NETSDK1005, learning da phase 7): `src/TranslateReader.Core/TranslateReader.Core.csproj`,
  `src/TranslateReader/TranslateReader.csproj`, `test/TranslateReader.Tests/TranslateReader.Tests.csproj`,
  via `dotnet list <csproj> package --vulnerable --include-transitive --format json` + `jq` filtrando
  `severity` `High`/`Critical` -> `exit 1` (o comando sai 0 mesmo com achados; o gate e o parser).
  **Fallback:** se o restore do app csproj nao fechar no Linux, mover para `windows-latest` +
  `dotnet workload install maui` (sem harden-runner, mesma isencao do job `build` do `ci.yml`); registrar
  escolha e motivo no SUMMARY.md.
- **Acceptance:**
  - os 4 greps do DoD 3 casam em `.github/workflows/`
  - **verde:** mesmo comando/parser rodado local nos 3 csprojs (com T-1 aplicada) -> exit 0, saida no SUMMARY.md
  - **vermelho provado:** parser contra JSON fixture com `"severity": "High"` (scratchpad) -> exit 1.
    Gate que nunca falha nao e gate
  - YAML valida; nenhum `${{ }}` dentro de `run:`
- **Deps:** T-1 (o gate tem que nascer verde — ordem exigida pelo CONTEXT)
- **Test:** dry-run local (verde + vermelho) + parser YAML + greps | **DoD:** 3 | **Status:** pending

### Wave 3

#### T-7: Auditoria de hardening + bateria completa do DoD
- **Files:** `.github/workflows/*.yml`, `.semgrep/*.yml` (somente correcoes)
- **Acceptance:**
  - **100% pinado em TODOS os workflows (novos + phase 7):** `(sls 'uses:' .github/workflows/*.yml).Count`
    == `(sls 'uses:\s*\S+@[0-9a-f]{40}' .github/workflows/*.yml).Count` **e**
    `sls 'uses:\s*[\w.-]+/[\w.-]+@v[0-9]' .github/workflows/*.yml` sem match; contagem antes/depois no SUMMARY.md
  - todo workflow tem `permissions:` + `concurrency:`; todo job `ubuntu-latest` tem harden-runner; todo
    `actions/checkout` tem `persist-credentials: false`
  - **zero `${{` dentro de bloco `run:`** em todos os workflows (learning W-3 da phase 7)
  - YAML valida em `.github/workflows/*.yml`, `.github/dependabot.yml` e `.semgrep/*.yml` via `yaml.safe_load`
    (actionlint ausente; comando usado registrado no SUMMARY.md)
  - **as 9 linhas `Verify:` do CONTEXT.md executadas em bash**, uma a uma, saida real colada no SUMMARY.md;
    qualquer FAIL vira correcao aqui ou justificativa explicita (caso T-1)
- **Deps:** T-1..T-6 | **Test:** bateria dos 9 `Verify:` + validacao YAML | **DoD:** 7 (+ re-check 1..6, 8, 9)
- **Status:** pending — se nada precisar de ajuste, nao ha commit; evidencia no SUMMARY.md.

## Execution
- **Total tasks:** 7
- **Waves:** 3 — W1: T-1, T-3, T-4, T-5, T-6 (files disjuntos) · W2: T-2 (dep T-1) · W3: T-7 (dep T-1..T-6)
- Estimated parallel speedup: ~2.3x (5 de 7 tasks na wave 1)
- **DoD -> task:** 1,2 -> T-3 · 3 -> T-2 · 4,5 -> T-1 · 6 -> T-4 · 7 -> T-7 · 8 -> T-5 · 9 -> T-6

## Files modified (todas as tasks)
- `src/TranslateReader.Core/TranslateReader.Core.csproj` (unico legado tocado — D-2)
- `.semgrep/dotnet-security.yml`, `SECURITY.md`
- `.github/workflows/`: `semgrep.yml`, `sca.yml`, `sbom.yml` (novos), `secret-scan.yml` (job add)

## Test requirements
- Phase **infra-only**: nenhum `.cs` novo -> Gate 3 do reviewer (cobertura 90%, D-6) reporta **SKIPPED**.
- Baseline D-2: `dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj --nologo` segue com
  **167 testes verdes**; T-1 e o unico risco real (bump de dependencia nativa do SQLite).
- Por task: parser YAML (convencao 7) + greps de aceite + dry-runs locais obrigatorios (semgrep em T-3,
  parser do gate SCA em T-2). Nenhum gate de cobertura entra no pipeline nesta phase.
