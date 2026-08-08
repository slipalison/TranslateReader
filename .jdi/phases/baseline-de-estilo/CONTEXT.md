# Phase 1: Baseline de estilo — Context (slug: baseline-de-estilo)

Capturado em 2026-08-08. Esta phase e a dona nomeada de um warning que reapareceu em TODA
REVIEW.md ja shipada (`ci-seguranca`, `coverage-90`, `conversion-performance`, `regression-suite`,
`sast-sca-sbom`, `pipeline-unificada`, `app-redesign`, `pixel-perfect`). Estado verificado hoje:
`.editorconfig`, `.gitattributes` e `Directory.Build.props` NAO existem em lugar nenhum do repo;
`dotnet format --verify-no-changes` acusa 3 erros WHITESPACE, todos em linha legada.

## Goal
`.editorconfig` + `.gitattributes` + analyzers configurados na raiz, fechando a divida de estilo
recorrente sem refatorar codigo legado.

## Locked decisions
- **D-...-1**: legado normalizado SO por `dotnet format whitespace` repo-wide (nao o format
  completo). Ordem obrigatoria dos commits: gitattributes+renormalize -> editorconfig ->
  Directory.Build.props -> format whitespace -> docs. Diff em `*.cs`/`*.xaml`/`*.js`
  semantico-zero; se precisar mudar codigo de verdade, para com BLOCKED.
- **D-...-2**: analyzers = built-in (`EnableNETAnalyzers`, `AnalysisLevel=latest-recommended`,
  `EnforceCodeStyleInBuild`) + `Meziantou.Analyzer`, centralizados em `Directory.Build.props` na
  raiz. StyleCop e SonarAnalyzer REJEITADOS.
- **D-...-3**: `TreatWarningsAsErrors=true` + `NoWarn` fechado: so IDs concretos, teto 12, cada ID
  documentado em linha de comentario propria. Legado congelado por supressao, nunca corrigido.
- **D-...-4**: `.gitattributes` com `* text=auto eol=lf` + um unico `git add --renormalize .` como
  PRIMEIRO commit; binarios declarados (`ttf epub png jpg jpeg ico zip gguf pfx`), `svg` e texto.
- **D-...-5**: Gate 4 do reviewer vira BLOCK **so nos arquivos tocados pela phase**; NENHUM job de
  lint no `ci.yml` (`.github/` com diff vazio). Reviewer/doer/registry/todos realinhados.

## Canonical refs
- `.jdi/decisions/D-2026-08-08-baseline-de-estilo-1..5.md`
- `.claude/rules/csharp.md` (fonte das regras que o Meziantou mecaniza) e `CLAUDE.md`
- `.jdi/todos/LEGACY.md:367-378` (as violacoes WHITESPACE com file:line)
- Superficies: `.editorconfig`, `.gitattributes`, `Directory.Build.props` (novos, raiz);
  `.jdi/agents/jdi-reviewer-translatereader.md` (Gate 4, ~280-298),
  `.jdi/agents/jdi-doer-translatereader.md:71`, `.jdi/registry/LEGACY.md:30`,
  `.jdi/registry/LEGACY-reviewers.md:33-34`, `.jdi/todos/LEGACY.md`.

## Out of scope
- Job de lint no CI, em qualquer escopo (D-...-5 rejeitou explicitamente).
- Linter/formatter JS (eslint/prettier) para `src/TranslateReader/Resources/Raw/wwwroot/js` e
  `test/js` -> todos.
- Formatador de XAML e unificacao da indentacao dos csproj (app usa 4 espacos, Core/Tests usam 2)
  -> todos. Nenhum formatador de XML existe no toolchain.
- `Directory.Packages.props` (central package management), `.globalconfig`, ativar `.githooks`.
- Corrigir qualquer warning legado (D-...-3 manda suprimir por ID) e qualquer refactor de
  The Method / carry-over roteado para `the-method-refactor`.

## Definition of Done

> Comandos em bash (Git Bash no Windows), da RAIZ do repo. `DOTNET_CLI_UI_LANGUAGE=en` onde a
> saida do dotnet e parseada (o sumario local sai em pt-BR). Logs em `TestResults/` (gitignored).
> `BASELINE` = `git rev-parse HEAD` gravado em `.jdi/phases/baseline-de-estilo/BASELINE` na
> primeira task, antes do primeiro commit da phase (padrao herdado de `pixel-perfect`).

### Auto-verifiable
- [ ] **DoD 1 — `.editorconfig` na raiz, commitado, com severidade explicita.** `root = true` na
      primeira linha, secao `[*.cs]`, `end_of_line = lf` (casa com D-...-4) e ao menos uma regra
      `dotnet_diagnostic.*`
      **Verify:** `test -f .editorconfig && git ls-files --error-unmatch .editorconfig >/dev/null 2>&1 && head -1 .editorconfig | tr -d '\r' | grep -qE '^root *= *true' && grep -qE '^\[\*\.cs\]' .editorconfig && grep -qE '^end_of_line *= *lf' .editorconfig && grep -q 'dotnet_diagnostic\.' .editorconfig`
      **Source:** D-...-1, D-...-4
- [ ] **DoD 2 — `Directory.Build.props` REALMENTE flui pros 3 projetos.** Prova por avaliacao
      MSBuild (nao por grep): `EnableNETAnalyzers`, `EnforceCodeStyleInBuild` e
      `TreatWarningsAsErrors` = true, `AnalysisLevel=latest-recommended` e o `PackageReference` do
      `Meziantou.Analyzer` presentes em Core, Tests E app
      **Verify:** `mkdir -p TestResults && for spec in "src/TranslateReader.Core/TranslateReader.Core.csproj|" "test/TranslateReader.Tests/TranslateReader.Tests.csproj|" "src/TranslateReader/TranslateReader.csproj|-p:TargetFramework=net10.0-windows10.0.19041.0"; do p="${spec%%|*}"; x="${spec#*|}"; o="TestResults/bde-props-$(basename "$p" .csproj).json"; DOTNET_CLI_UI_LANGUAGE=en dotnet msbuild "$p" $x -nologo -getProperty:EnableNETAnalyzers -getProperty:EnforceCodeStyleInBuild -getProperty:TreatWarningsAsErrors -getProperty:AnalysisLevel -getItem:PackageReference > "$o" 2>&1 || exit 1; grep -qiE '"EnableNETAnalyzers": *"true"' "$o" && grep -qiE '"EnforceCodeStyleInBuild": *"true"' "$o" && grep -qiE '"TreatWarningsAsErrors": *"true"' "$o" && grep -qi 'latest-recommended' "$o" && grep -q 'Meziantou.Analyzer' "$o" || exit 1; done && test -f Directory.Build.props && git ls-files --error-unmatch Directory.Build.props >/dev/null 2>&1`
      **Source:** D-...-2, D-...-3
- [ ] **DoD 3 — `.gitattributes` + repo 100% LF.** `* text=auto eol=lf`, binarios declarados, e
      NENHUM arquivo com `i/crlf` ou `i/mixed` no index (prova do renormalize)
      **Verify:** `test -f .gitattributes && git ls-files --error-unmatch .gitattributes >/dev/null 2>&1 && grep -qE '^\*[[:space:]]+text=auto[[:space:]]+eol=lf' .gitattributes && for e in ttf epub png jpg jpeg ico zip gguf pfx; do grep -qE "^\*\.$e[[:space:]]+(binary|-text)" .gitattributes || exit 1; done && test -z "$(git ls-files --eol | grep -E 'i/(crlf|mixed)')"`
      **Source:** D-...-4
- [ ] **DoD 4 — `dotnet format whitespace` limpo no repo inteiro.** Escopo exatamente o de
      D-...-1: subcomando `whitespace`, nao o format completo. Mata as violacoes de
      `ThemeEngine.cs`, `ReaderPage.xaml.cs`, `ThemeEngineTests.cs`, `TranslationManagerTests.cs`
      **Verify:** `mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet format whitespace --verify-no-changes > TestResults/bde-format.log 2>&1; test $? -eq 0`
      **Source:** D-...-1
- [ ] **DoD 5 — a phase inteira e semantico-zero em codigo.** Renormalize (D-...-4) + whitespace
      (D-...-1) tocam muitos arquivos de uma vez; o diff contra o ancora, ignorando espaco/linha
      em branco/CR, tem de ser VAZIO em `*.cs`, `*.xaml` e `*.js`
      **Verify:** `B=$(tr -d ' \r\n' < .jdi/phases/baseline-de-estilo/BASELINE) && test -n "$B" && test -z "$(git diff --ignore-all-space --ignore-blank-lines --ignore-cr-at-eol "$B" -- '*.cs' '*.xaml' '*.js')"`
      **Source:** D-...-1, D-...-4
- [ ] **DoD 6 — build passa COM warnings-as-errors e o `NoWarn` e fechado.** App (Windows
      Release) e Tests com `0 Error(s)`, zero `warning CS|CA|MA|IDE` nos dois logs, e a lista
      `NoWarn` do `Directory.Build.props`: so IDs concretos (nenhum curinga), cada ID
      aparecendo em >= 2 linhas do arquivo (valor + comentario com o motivo).
      **Teto numerico de 12 REVOGADO por D-...-6** — a lista e exatamente o que sobrou da medicao
      apos a calibracao das rules mal aplicadas ao tipo de projeto; a validacao estrutural
      (elemento unico, sem curinga, comentario por ID) continua valendo integralmente.
      **Verify:** `mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0 > TestResults/bde-build-app.log 2>&1 && grep -qE "^ *0 Error\(s\)" TestResults/bde-build-app.log && DOTNET_CLI_UI_LANGUAGE=en dotnet build test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release > TestResults/bde-build-tests.log 2>&1 && grep -qE "^ *0 Error\(s\)" TestResults/bde-build-tests.log && ! grep -qE ": warning (CS|CA|MA|IDE)[0-9]+" TestResults/bde-build-app.log TestResults/bde-build-tests.log && P=Directory.Build.props && V=$(tr -d '\r' < "$P" | tr '\n' ' ' | sed -n 's/.*<NoWarn>\(.*\)<\/NoWarn>.*/\1/p') && test -n "$V" && T=$(echo "$V" | tr ';' '\n' | sed 's/\$(NoWarn)//' | tr -d ' ') && test "$(echo "$T" | grep -vcE '^([A-Za-z]{2,4}[0-9]{3,5})?$')" -eq 0 && IDS=$(echo "$T" | grep -E '^[A-Za-z]{2,4}[0-9]{3,5}$' | sort -u) && test -n "$IDS" && for id in $IDS; do test "$(grep -c "$id" "$P")" -ge 2 || exit 1; done`
      **Source:** D-...-2, D-...-3, D-...-6
- [ ] **DoD 7 — nada quebrou: suite .NET verde + suite JS verde.** Piso FIXO `Total >= 375`,
      `Failed: 0`, `Skipped <= 2` (baseline de `pixel-perfect`), e `node --test test/js/` verde
      (o renormalize toca todos os `.js` de `wwwroot` e `test/js`)
      **Verify:** `mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release > TestResults/bde-suite.log 2>&1 && grep -q "Passed!" TestResults/bde-suite.log && awk '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Skipped:")s=$(i+1);if($i=="Total:")t=$(i+1)}} END{exit (k&&f+0==0&&t+0>=375&&s+0<=2)?0:1}' TestResults/bde-suite.log && node --test test/js/ > TestResults/bde-js.log 2>&1`
      **Source:** D-...-1, D-...-4
- [ ] **DoD 8 — divida de processo fechada e CI intocado.** Gate 4 do reviewer sem "WARN only" e
      sem "Tighten to BLOCK", com BLOCK presente; `registry/LEGACY.md`,
      `registry/LEGACY-reviewers.md` e `jdi-doer-translatereader.md` sem as frases que dizem que
      nao existe `.editorconfig`; `todos/LEGACY.md` com o marcador literal
      `RESOLVIDO em baseline-de-estilo`; e `.github/` com diff VAZIO (D-...-5 proibiu job de lint)
      **Verify:** `R=.jdi/agents/jdi-reviewer-translatereader.md && test "$(grep -ci 'WARN.only' "$R")" -eq 0 && test "$(grep -ci 'Tighten to BLOCK' "$R")" -eq 0 && grep -q 'BLOCK' "$R" && test "$(grep -ci 'WARN.only' .jdi/registry/LEGACY.md)" -eq 0 && test "$(grep -ci 'WARN.only' .jdi/registry/LEGACY-reviewers.md)" -eq 0 && test "$(grep -c 'quando a phase' .jdi/registry/LEGACY-reviewers.md)" -eq 0 && test "$(grep -c 'custom analyzers exist yet' .jdi/agents/jdi-doer-translatereader.md)" -eq 0 && grep -q 'RESOLVIDO em baseline-de-estilo' .jdi/todos/LEGACY.md && B=$(tr -d ' \r\n' < .jdi/phases/baseline-de-estilo/BASELINE) && test -z "$(git diff --name-only "$B" -- .github/)"`
      **Source:** D-...-5

### Manual
- _(none — todos os itens sao auto-verificaveis)_

## Deferred to PR review
- Confirmar que a working tree em LF nao gera diff fantasma no Visual Studio/Rider no Windows.
- Smoke em Windows: abrir a app uma vez pos-format e pos-analyzers (nenhuma mudanca de
  comportamento e esperada — DoD 5 prova semantico-zero, mas o olho humano fecha).
- Julgamento sobre o tamanho real da lista `NoWarn`: se chegar perto do teto de 12, isso e sinal
  de que `latest-recommended` + Meziantou pesaram demais sobre o legado.

## Notes
- **Ancora**: a primeira task grava `git rev-parse HEAD` em
  `.jdi/phases/baseline-de-estilo/BASELINE` (arquivo commitado). DoD 5 e DoD 8 dependem dele.
- **Formato do `NoWarn`** (DoD 6 depende): o valor num elemento `<NoWarn>` unico no
  `Directory.Build.props`, e o comentario com o motivo de cada ID em LINHA SEPARADA — comentario
  na mesma linha do valor faz o `grep -c >= 2` falhar.
- **Fallback do DoD 4**: se `dotnet format whitespace` sem argumento nao descobrir o
  `TranslateReader.slnx` neste SDK, rodar por csproj (`for p in <3 csproj>`) — o escopo (repo
  inteiro, whitespace) e o que esta locked, nao a forma de invocar.
- **Zero arquivo `.cs` novo nesta phase** -> Gate 3 (cobertura) reporta SKIPPED por D-2, o que
  nao e falha. Nenhuma exigencia nova de cobertura.
- Warnings pre-existentes conhecidos que devem entrar no `NoWarn`: `CS0618` (`DisplayAlert`
  obsoleto, `ReaderPage.xaml.cs`/`LibraryPage.xaml.cs`) e `CS0414` (`_needsInjection`) — 16
  ocorrencias no build Windows Release hoje. O que `CA*`/`MA*` acender e descoberto na execucao.
