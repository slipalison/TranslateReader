# Phase 1: Baseline de estilo — Plan  (slug: baseline-de-estilo)

## Leia antes de comecar (obrigatorio)

1. 7 tasks EM ORDEM (T-1 -> T-7). **Nao reordene, nao junte, nao paralelize** — a ordem e locked
   por D-...-1(3). 1 task = 1 commit atomico, mensagem literal dada em cada task.
2. "DoD N literal" = copie o comando `Verify:` do CONTEXT.md **sem editar**. Licao de
   `app-redesign`: reescrever um Verify pra ele passar e o modo classico de falhar a phase.
3. Regra de parada (D-...-1(4)): se pra um gate passar voce precisar mudar **uma linha de codigo
   de verdade** em `*.cs`/`*.xaml`/`*.js`, PARE, escreva `BLOCKED: <motivo>` no SUMMARY.md.
4. NAO FACA em nenhuma task: tocar `.github/` (D-...-5(3), DoD 8 exige diff vazio); corrigir
   warning legado (D-...-3(2) manda suprimir por ID); curinga em `NoWarn` ou `#pragma warning
   disable` novo; criar `.globalconfig`/`Directory.Packages.props`/`.githooks`/eslint/prettier;
   centralizar `TargetFramework`/`Nullable`/`ImplicitUsings` no `Directory.Build.props`
   (D-...-2(3) permite, YAGNI recusa — a condicao multi-TFM do app csproj e fragil).

## Goal
`.editorconfig` + `.gitattributes` + analyzers configurados na raiz, fechando a divida de estilo
recorrente sem refatorar codigo legado.

## Locked decisions (from CONTEXT.md)
D-...-1 legado so `dotnet format whitespace`, ordem de commit locked, semantico-zero;
D-...-2 analyzers built-in + Meziantou em `Directory.Build.props` na raiz;
D-...-3 `TreatWarningsAsErrors` + `NoWarn` fechado (so IDs, teto 12, comentario em linha propria);
D-...-4 `.gitattributes` `* text=auto eol=lf` + renormalize como primeiro commit de conteudo;
D-...-5 Gate 4 vira BLOCK escopado, ZERO job de lint no CI.

## Execucao
7 tasks, **1 wave, cadeia estritamente sequencial, 0 paralelismo**. Nao e falta de esforco:
T-3..T-6 escrevem/leem os MESMOS artefatos (`.editorconfig` -> comportamento do `dotnet format
whitespace` -> log de warnings -> `NoWarn`) e a ordem dos commits e locked por D-...-1(3).
Paralelizar quebraria a prova de DoD 5 (cada commit tem de ser legivel isolado) e o inventario de
T-4. Specialist de TODAS as tasks: `jdi-doer-translatereader` (single-stack, glob `**/*`).

---

## T-1: Ancora da phase (BASELINE) + arvore limpa
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `.jdi/phases/baseline-de-estilo/BASELINE` (novo)
- **Passos:**
  1. A arvore tem sujeira pre-existente (`.claude/scheduled_tasks.lock` deletado no `git status`).
     Limpe ANTES de tudo: `git restore --staged --worktree .claude/scheduled_tasks.lock` (se
     falhar, `git stash push -- <path>`). Esse arquivo NUNCA entra em commit desta phase — T-2 faz
     `git add --renormalize .`, que varre qualquer tracked modificado pro commit de line endings.
  2. `git rev-parse HEAD > .jdi/phases/baseline-de-estilo/BASELINE`, ANTES de qualquer outra
     mudanca. Esse SHA e o ancora de DoD 5 e DoD 8.
  3. Commit sozinho: `chore(baseline-de-estilo): anchor phase baseline commit`.
- **Acceptance (DoD 5 e DoD 8 dependem deste ancora):** BASELINE commitado, com SHA de commit
  valido, ancestral de HEAD e anterior a todo commit de conteudo da phase; arvore limpa ao fim:
  `B=$(tr -d ' \r\n' < .jdi/phases/baseline-de-estilo/BASELINE) && test -n "$B" && git cat-file -e "$B^{commit}" && git ls-files --error-unmatch .jdi/phases/baseline-de-estilo/BASELINE >/dev/null 2>&1 && git merge-base --is-ancestor "$B" HEAD && test -z "$(git status --porcelain)"`
- **Dependencies:** none
- **Test:** n/a (nenhum codigo tocado)
- **Status:** completed

## T-2: `.gitattributes` + `git add --renormalize .` (line endings, commit sozinho)
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `.gitattributes` (novo) + o que o renormalize alterar no index
- **Passos:**
  1. `.gitattributes` na raiz: primeira regra `* text=auto eol=lf`; depois os binarios
     obrigatorios de D-...-4(2) — `*.ttf *.epub *.png *.jpg *.jpeg *.ico *.zip *.gguf *.pfx` como
     `binary`. `*.svg` NAO entra (4 assets editaveis). Formato que DoD 3 exige: `*.<ext>` +
     espaco/tab + `binary` (ou `-text`), no inicio da linha.
  2. `git add --renormalize .`; conferir `git status`. D-...-4(1): o index ja e LF, entao isto e
     quase no-op de conteudo — se vier diff GRANDE de conteudo, pare e investigue.
  3. Commit: `chore(baseline-de-estilo): normalize line endings` (literal, D-...-4(3)).
  4. Working tree pra LF (D-...-4(1) manda LF tambem na working tree), **so se**
     `git status --porcelain` estiver vazio: `git rm --cached -r -q .` + `git reset --hard`.
  5. Rodar suite .NET Release + `node --test test/js/` ANTES de seguir. Risco real: literais
     verbatim/raw multi-linha (`@"..."`, `"""..."""` — ~118 ocorrencias em 12 arquivos) embutem o
     EOL do fonte, entao CRLF->LF muda o CONTEUDO da string. Mitigante forte: o job `test` do
     `ci.yml` ja roda em `ubuntu-latest` (checkout LF) e passa. Se ainda assim quebrar: BLOCKED.
- **NAO FACA:** declarar `*.svg` binario; juntar `.editorconfig` ou format neste commit
  (D-...-4(3): o renormalize vem sozinho).
- **Acceptance:** **DoD 3 literal** exit 0; **DoD 5 literal** exit 0 (ja tem de valer aqui — este
  commit e puro EOL); suite .NET e suite JS verdes (piso de DoD 7).
- **Dependencies:** T-1
- **Test:** `dotnet test ... -c Release` + `node --test test/js/`
- **Status:** completed

## T-3: `.editorconfig` na raiz
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `.editorconfig` (novo)
- **Passos:**
  1. **Linha 1 = `root = true`, sem comentario antes** — DoD 1 faz `head -1`. Errar isso e o modo
     mais bobo de reprovar a task.
  2. `[*]`: `charset = utf-8`, `end_of_line = lf` (casa com D-...-4; DoD 1 exige a chave no INICIO
     da linha), `insert_final_newline = true`, `trim_trailing_whitespace = true`,
     `indent_style = space`. `[*.md]` com `trim_trailing_whitespace = false`.
  3. `[*.cs]`: `indent_size = 4` (o que o codigo ja usa) + as preferencias de style que D-...-1(1)
     autoriza declarar (var / `this.` / ordem de using / chaves / expression-bodied) — valem pra
     codigo novo/tocado, nao viram churn no legado.
  4. Pelo menos uma regra `dotnet_diagnostic.<ID>.severity = ...` (DoD 1 exige o literal
     `dotnet_diagnostic.`). T-5 pode acrescentar mais, a partir do inventario medido em T-4.
  5. **NAO** declarar `indent_size` pra `[*.csproj]`/`[*.xml]`/`[*.xaml]` — unificar indentacao de
     csproj (app 4, Core/Tests 2) esta explicitamente fora de escopo.
  6. Commit: `chore(baseline-de-estilo): add root .editorconfig`.
- **Acceptance:** **DoD 1 literal** exit 0; **DoD 5 literal** exit 0 (nao toca `.cs`/`.xaml`/`.js`).
- **Dependencies:** T-2
- **Test:** n/a (config; o efeito e coberto por DoD 4/DoD 7 em T-6)
- **Status:** completed

## T-4: `Directory.Build.props` — analyzers ligados + inventario de warnings MEDIDO
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `Directory.Build.props` (novo)
- **Passos:**
  1. `Directory.Build.props` na RAIZ, **minimo**: `EnableNETAnalyzers=true`,
     `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild=true` +
     `<PackageReference Include="Meziantou.Analyzer" PrivateAssets="all"
     IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive" />`.
     Versao: ultima estavel — `3.0.141` na data do plano; confirme com
     `dotnet package search Meziantou.Analyzer --exact-match` e pine o que vier.
  2. **NAO** setar `TreatWarningsAsErrors` ainda: liga-lo antes do `NoWarn` existir quebra o build
     e voce perde o inventario. Ele entra em T-5.
  3. Medir o inventario COMPLETO (entrada de T-5):
     `mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0 > TestResults/bde-inv-app.log 2>&1; DOTNET_CLI_UI_LANGUAGE=en dotnet build test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release > TestResults/bde-inv-tests.log 2>&1; grep -ohE ": (warning|error) [A-Za-z]{2,4}[0-9]{3,5}" TestResults/bde-inv-*.log | awk '{print $3}' | sort -u > TestResults/bde-warning-ids.txt; cat TestResults/bde-warning-ids.txt`
  4. Copiar a lista de IDs medida pro SUMMARY.md — e a evidencia de que T-5 nao chutou.
  5. Commit: `chore(baseline-de-estilo): centralize analyzers in Directory.Build.props`
     (`TestResults/` e gitignored — nao commitar log).
- **Acceptance (= DoD 2 menos `TreatWarningsAsErrors`, provado por avaliacao MSBuild, nao grep):**
  `mkdir -p TestResults && for spec in "src/TranslateReader.Core/TranslateReader.Core.csproj|" "test/TranslateReader.Tests/TranslateReader.Tests.csproj|" "src/TranslateReader/TranslateReader.csproj|-p:TargetFramework=net10.0-windows10.0.19041.0"; do p="${spec%%|*}"; x="${spec#*|}"; o="TestResults/bde-t4-$(basename "$p" .csproj).json"; DOTNET_CLI_UI_LANGUAGE=en dotnet msbuild "$p" $x -nologo -getProperty:EnableNETAnalyzers -getProperty:EnforceCodeStyleInBuild -getProperty:AnalysisLevel -getItem:PackageReference > "$o" 2>&1 || exit 1; grep -qiE '"EnableNETAnalyzers": *"true"' "$o" && grep -qiE '"EnforceCodeStyleInBuild": *"true"' "$o" && grep -qi 'latest-recommended' "$o" && grep -q 'Meziantou.Analyzer' "$o" || exit 1; done && test -s TestResults/bde-warning-ids.txt`
  E **DoD 5 literal** exit 0.
- **Dependencies:** T-3
- **Test:** os 2 builds Release do passo 3 completam (com warnings, sem erro)
- **Status:** completed

## T-5: `TreatWarningsAsErrors` + `NoWarn` fechado (a partir do inventario medido)
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `Directory.Build.props`, `.editorconfig` (so severidades `IDE*`, se preciso)
- **Passos:**
  1. Particionar `TestResults/bde-warning-ids.txt`:
     - **`IDE*`** (acesos por `EnforceCodeStyleInBuild`): casa canonica e o `.editorconfig` —
       `dotnet_diagnostic.IDExxxx.severity = suggestion` pro que so seria churn de estilo no
       legado. **Nao** contam no teto de 12.
     - **`CS*`/`CA*`/`MA*`/`NU*`**: vao pro `<NoWarn>` do `Directory.Build.props`, por ID exato, e
       **contam** no teto. Conhecidos hoje (16 ocorrencias): `CS0618` (`DisplayAlert` obsoleto,
       `ReaderPage.xaml.cs`/`LibraryPage.xaml.cs`) e `CS0414` (`_needsInjection`).
  2. Formato EXATO (DoD 6 parseia isto): **UM unico** elemento `<NoWarn>` no arquivo inteiro,
     valor `$(NoWarn);ID;ID;...`; e **uma linha de comentario POR ID**, separada da linha do
     valor, com motivo + onde ocorre. Comentario na mesma linha do valor faz `grep -c >= 2` falhar.
  3. Setar `TreatWarningsAsErrors=true`. Rebuildar app (Windows Release) + Tests ate `0 Error(s)`.
  4. **Teto de 12 e regra de parada, nao meta a negociar** (D-...-3(3b) + licao de `pixel-perfect`:
     nao persiga zero-warning violando a fronteira). Se a lista `CS/CA/MA/NU` passar de 12:
     `BLOCKED` com a lista medida no SUMMARY, pro humano decidir se `latest-recommended` fica.
     **Proibido**: curinga; mover CA/MA pra `.editorconfig severity=none` so pra driblar o teto;
     corrigir codigo legado.
  5. Risco de CI que nenhum DoD cobre: `ci.yml` tem o job `build-android` (`net10.0-android`,
     Release) e `TreatWarningsAsErrors` vale la tambem — o Android pode acender IDs que o TFM
     Windows nao acende. Se a workload `maui-android` existir na maquina, rode
     `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-android` e
     inclua os IDs no `NoWarn` (contam no teto). Se nao existir, **registre no SUMMARY como risco
     a confirmar no PR** — nao e falha de task (fallback do specialist).
  6. Commit: `chore(baseline-de-estilo): treat warnings as errors with a closed NoWarn list`.
- **Acceptance:** **DoD 2 literal** exit 0 (agora completo, `TreatWarningsAsErrors=true` nos 3
  projetos); **DoD 6 literal** exit 0 (`0 Error(s)` nos 2 builds, zero `: warning CS|CA|MA|IDE`,
  `NoWarn` so com IDs concretos, <= 12, cada ID em >= 2 linhas); **DoD 5 literal** exit 0.
- **Dependencies:** T-4
- **Test:** builds Release do app (Windows) e de Tests, ambos `0 Error(s)`
- **Status:** pending

## T-6: `dotnet format whitespace` no repo inteiro (commit proprio)
- **Specialist:** jdi-doer-translatereader
- **Files modified:** os `.cs` que o subcomando `whitespace` alterar — no minimo
  `src/TranslateReader.Core/Business/Engines/ThemeEngine.cs`,
  `src/TranslateReader/Pages/ReaderPage.xaml.cs`,
  `test/TranslateReader.Tests/ThemeEngineTests.cs`,
  `test/TranslateReader.Tests/TranslationManagerTests.cs`
- **Passos:**
  1. `dotnet format whitespace` — subcomando `whitespace`, **nunca** o `dotnet format` completo
     (D-...-1(1): o completo aplicaria style/analyzer fixes ao legado que D-2 isenta). Fallback
     locked pelo CONTEXT.md: se o SDK nao descobrir o `TranslateReader.slnx`, rode por csproj nos
     3 projetos — o escopo (repo inteiro, whitespace) e o que esta locked, nao a forma de invocar.
  2. Conferir o diff: so espaco/indentacao/linha em branco/EOL. Qualquer token movido = pare.
  3. Rodar suite .NET + suite JS. Varios testes deste repo **leem o proprio fonte do disco e fazem
     grep/indice de linha** (`DesignSystemTests.cs`, `PixelSpecTests.cs`) — sao os candidatos a
     quebrar por whitespace. Se quebrar por isso: torne a ASSERCAO insensivel a espaco (o fonte
     esta certo, a assercao e que era fragil). Nunca reverta o format, nunca mude semantica, nunca
     afrouxe a assercao a ponto de ela deixar de checar o que checava.
  4. Commit: `style(baseline-de-estilo): apply dotnet format whitespace repo-wide`.
- **NAO FACA:** rodar `dotnet format` sem subcomando; misturar docs neste commit.
- **Acceptance:** **DoD 4 literal** exit 0; **DoD 7 literal** exit 0 (`Total >= 375`, `Failed: 0`,
  `Skipped <= 2`, `node --test test/js/`); **DoD 5 literal** exit 0 — este e o commit de maior
  risco pro semantico-zero.
- **Dependencies:** T-5
- **Test:** `dotnet test ... -c Release` (piso 375/0/2) + `node --test test/js/`
- **Status:** pending

## T-7: Fechar a divida de processo (reviewer, registry, doer, todos) — CI intocado
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `.jdi/agents/jdi-reviewer-translatereader.md`,
  `.jdi/agents/jdi-doer-translatereader.md`, `.jdi/registry/LEGACY.md`,
  `.jdi/registry/LEGACY-reviewers.md`, `.jdi/todos/LEGACY.md`
- **Passos:**
  1. **Gate 4 do reviewer** (~280-298): reescrever por D-...-5(1)(2) — lint vira **BLOCK apenas
     sobre os arquivos tocados pela phase em review**; drift fora do diff continua no maximo WARN
     (coerente com D-2); o comando do gate passa a ser o subcomando **`whitespace`** escopado, nao
     o `dotnet format` completo (o gate nao pode exigir mais do que a decisao mandou fazer).
     Remover "Tighten to BLOCK-on-new-files once the `baseline-de-estilo` phase ships...".
  2. **Armadilha do DoD 8:** `grep -ci 'WARN.only'` roda no arquivo INTEIRO do reviewer, nao so no
     Gate 4. Ha **5** ocorrencias: ~175 (build Android secundario), ~292 e ~298 (Gate 4), ~488
     (5.11 event handlers) e ~511 (5.12 static mutavel). As 3 fora do Gate 4 tem de ser
     **reescritas preservando o significado** (ex.: "WARN, nao BLOCK") — nunca apagadas nem
     promovidas a BLOCK: mudar a severidade de 5.11/5.12 e do build Android nao esta autorizado
     por nenhuma decisao desta phase.
  3. `.jdi/registry/LEGACY.md:30`: trocar "Lint: ... WARN-only enquanto nao existir
     `.editorconfig`/analyzers" pela realidade nova (`.editorconfig` + built-in analyzers +
     Meziantou + `TreatWarningsAsErrors`; gate = BLOCK escopado aos arquivos da phase).
  4. `.jdi/registry/LEGACY-reviewers.md:33-34`: remover "Gate 4 sobe para BLOCK-on-new-files
     **quando a phase** `baseline-de-estilo` entregar..." (DoD 8 conta `grep -c 'quando a phase'`
     == 0) e afirmar o estado atual.
  5. `.jdi/agents/jdi-doer-translatereader.md:71`: remover "no `.editorconfig` or custom analyzers
     exist yet" e descrever o toolchain real, com a versao do Meziantou pinada em T-4.
  6. `.jdi/todos/LEGACY.md:367-378`: marcar a entrada com o texto **literal**
     `RESOLVIDO em baseline-de-estilo`, citando o commit de T-6 como evidencia.
  7. Nao rodar `npx jdi-cli render`: as views `.jdi/registry.md`/`.jdi/reviewers.md`/`.jdi/todos.md`
     sao gitignored e geradas; so os arquivos por-entrada acima importam.
  8. Commit: `docs(baseline-de-estilo): close the recurring lint debt in reviewer, registry and todos`.
- **NAO FACA:** criar/editar qualquer coisa em `.github/` (D-...-5(3)); promover Gate 4 a BLOCK
  repo-wide (a decisao escopou aos arquivos tocados).
- **Acceptance:** **DoD 8 literal** exit 0 (inclui `git diff --name-only $BASELINE -- .github/`
  VAZIO); **DoD 5 literal** exit 0 (so toca `.md`).
- **Dependencies:** T-6
- **Test:** n/a (documentacao de processo)
- **Status:** pending

---

## Test requirements
- .NET: `DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release`
  — piso FIXO `Total >= 375`, `Failed: 0`, `Skipped <= 2`.
- JS: `node --test test/js/` verde (o renormalize toca todos os `.js`).
- Build: app Windows Release + Tests Release, `0 Error(s)` e zero `: warning CS|CA|MA|IDE`.
- Cobertura: **zero `.cs` novo nesta phase** -> Gate 3 reporta SKIPPED por D-2. Nao e falha e nao
  ha exigencia nova de cobertura.

## Mapa DoD -> task
DoD 1 -> T-3 | DoD 2 -> T-4 (parcial) + T-5 (completo) | DoD 3 -> T-2 | DoD 4 -> T-6 |
DoD 5 -> re-checado em T-2..T-7 | DoD 6 -> T-5 | DoD 7 -> T-2 (pre-check) + T-6 |
DoD 8 -> T-1 (ancora) + T-7

## Encerramento da phase (apos T-7)
1. Rodar os 8 DoD do CONTEXT.md em sequencia, literais, todos exit 0.
2. `SUMMARY.md`: inventario de warnings medido em T-4, `NoWarn` final com justificativa por ID, o
   que renormalize e format realmente mudaram, e qualquer BLOCKED.
3. PR de `feat/baseline-de-estilo` -> `main`, corpo com DoD 1-8 e resultado, a secao "Deferred to
   PR review" do CONTEXT.md copiada (diff fantasma LF no VS/Rider, smoke Windows, julgamento sobre
   o tamanho da lista `NoWarn`) e o risco do job `build-android` se ele nao tiver sido verificado
   localmente em T-5.
