# Phase 15: Cobertura de 90% no SonarQube sem issues novas — Context (slug: coverage-90)

## Goal
Escrever os testes que faltam ate a cobertura medida pelo SonarQube atingir 90%, sem introduzir
nenhuma issue nova — partindo de 75,9% em `main` (1428 lines to cover, 329 descobertas, das quais
195 sao JS do WebView sem harness nenhum no repo).

## Locked decisions
- D-2026-07-31-coverage-90-0: registro da phase, baseline SonarCloud (coverage 75,9%,
  lines_to_cover 1428, uncovered 329 = 195 JS + 134 C#).
- D-2026-07-31-coverage-90-1: rota (A) — harness JS real via `node:test`+`node:vm` nativo (zero
  dependencia nova), NAO (B) `sonar.coverage.exclusions`. Alvo: cobertura agregada dos 4 JS
  >= 85% local (lcov).
- D-2026-07-31-coverage-90-2: layout `test/js/<nome>.test.js`; comando de cobertura `node --test
  --experimental-test-coverage --test-reporter=lcov
  --test-reporter-destination=TestResults/js-lcov.info test/js/`; `sonarqube.yml` ganha
  `actions/setup-node` (SHA-pinned) + o step de teste JS (gated como os demais) +
  `sonar.javascript.lcov.reportPaths` no bloco `args=(...)` do `begin`.
- D-2026-07-31-coverage-90-3: `ModelAccess.cs` — `DownloadModelAsync` ganha teste com
  `HttpMessageHandler` fake (sem rede real) + diretorio temp real (excecao a §6, mesmo padrao de
  `ModelAccessTests.cs`/`FileUtilityTests.cs`). Alvo: cobertura >= 90% (de 39%).
- D-2026-07-31-coverage-90-4: `TranslationEngine.cs` (52 linhas) permanece DEFERIDO — nao reverte
  `D-2026-07-30-regression-suite-5(2)`/`D-2026-07-30-the-method-refactor-6`.
- D-2026-07-31-coverage-90-5: aritmetica-alvo — precisa >=187 linhas novas cobertas; plano
  (JS 166 + ModelAccess 20 + FileUtility/HtmlUtility 5 = 191) cobre com margem de 4, sem tocar
  `TranslationEngine`/`ParsingEngine`. `ParsingEngine` (45 linhas) e reserva de contingencia
  nomeada, mesmo padrao de fixture real de `ParsingEngineTests.cs` se acionada.
- D-2026-07-31-coverage-90-6: "sem issues nova" nao tem gate local possivel (analisadores Sonar
  nao rodam em `dotnet build`/`node --test`) — confirmacao vai para Deferred to PR review.
- D-2026-07-31-coverage-90-7: `sonar.qualitygate.wait` mede so New Code — nenhum `Verify:` desta
  fase usa Quality Gate/CI como prova da meta de 90% Overall.

## Canonical refs
- Card colado via `/jdi-issue` em 2026-07-31 (sem tracker/URL).
- SonarCloud API `component=slipalison_TranslateReader&branch=main` (pos-PR#12/`1af3a51`).
- `.github/workflows/sonarqube.yml`.
- `.jdi/phases/sonar-zero-issues/{CONTEXT,REVIEW,SHIPPED}.md` (Learnings do SHIPPED).
- Node docs (test runner lcov reporter, `--experimental-test-coverage`), pesquisada nesta sessao.

## Out of scope
- `TranslationEngine.cs` (52 linhas) — deferido para `llm-mobile` (D-...-4).
- `ParsingEngine.cs` (45 linhas) — reserva de contingencia, nao commitment do plano principal
  (D-...-5).
- `sonar.coverage.exclusions` para JS — rota B avaliada e rejeitada (D-...-1).
- Job Sonar novo em `windows-latest` com workload MAUI (cobertura de scan do App C#) — limite
  ja registrado em `.jdi/todos.md` § `sonar-zero-issues`, nao reaberto aqui.
- Quality Gate customizado (Overall vs New Code) no SonarCloud — fora do repo, ja registrado.
- `eslint`/lint estatico dedicado para o JS — contradiria "zero dependencia nova"; candidato
  futuro registrado em `.jdi/todos.md` § `coverage-90`.

## Definition of Done

### Auto-verifiable
- [ ] Harness de teste JS existe para os 4 scripts do WebView e todos os testes passam.
      **Verify:** `test -f test/js/paginated.test.js && test -f test/js/bridge.test.js && test -f test/js/translation.test.js && test -f test/js/scroll.test.js && S=$(node --test --test-reporter=tap test/js/) && printf '%s\n' "$S" | awk '/^# pass /{p=$3} /^# fail /{f=$3} END{exit (p>1 && f==0)?0:1}'`
      **Source:** CONTEXT (D-...-1, D-...-2); comando superseded por D-2026-07-31-coverage-90-8
      (o piso `p>1` nega a suite VAZIA — com os 4 `.test.js` esvaziados o Node ainda reporta
      `# pass 1`, contando o proprio arquivo; ratchet numerico fica para a proxima phase)

- [ ] Cobertura local agregada dos 4 scripts JS (`paginated.js`, `bridge.js`, `translation.js`,
      `scroll.js`) >= 85% via lcov.
      **Verify:** `rm -f TestResults/js-lcov.info && mkdir -p TestResults && node --test --experimental-test-coverage --test-reporter=lcov --test-reporter-destination=TestResults/js-lcov.info test/js/ >/dev/null 2>&1 && test -s TestResults/js-lcov.info && awk -F: '/^SF:/{f=$2} /^LH:/{h=$2} /^LF:/{l=$2} /^end_of_record/{if(f ~ /(paginated|bridge|translation|scroll)\.js$/){H+=h;L+=l;seen[f]=1}} END{n=0;for(k in seen)n++; exit (n==4 && L>0 && (H*100/L)>=85)?0:1}' TestResults/js-lcov.info`
      **Source:** CONTEXT (D-...-1, D-...-5); comando superseded por D-2026-07-31-coverage-90-8
      (artefato apagado antes da medicao + `&&` do runner ate a assercao + os 4 arquivos de
      producao distintos exigidos no lcov; piso de 85% inalterado)

- [ ] `ModelAccess.cs` cobertura local >= 90% (`DownloadModelAsync` testado com
      `HttpMessageHandler` fake, sem rede real).
      **Verify:** `rm -rf TestResults/dod3 && dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --collect:"XPlat Code Coverage;Format=cobertura" --results-directory TestResults/dod3 >/dev/null 2>&1 && test "$(find TestResults/dod3 -name coverage.cobertura.xml | wc -l)" -eq 1 && F=$(find TestResults/dod3 -name coverage.cobertura.xml) && grep -oE 'filename="[^"]*ModelAccess\.cs" line-rate="[0-9.]+"' "$F" | grep -oE '[0-9.]+"$' | tr -d '"' | awk '{n++; if($1+0<0.90) bad++} END{exit (n>0 && !bad)?0:1}'`
      **Source:** CONTEXT (D-...-3, D-...-5); comando superseded por D-2026-07-31-coverage-90-8
      (diretorio de resultados LIMPO e dedicado + `&&` do runner ate a assercao + exatamente 1
      relatorio exigido, no lugar de `find|sort|tail -1` lexicografico sobre GUIDs; comparacao
      numerica por classe, no lugar de `awk -v r="<multilinha>"` que virava comparacao de string;
      piso de 0.90 inalterado)

- [ ] `FileUtility.cs` e `HtmlUtility.cs` cobertura local = 100% (fechando os 3+2 linhas
      triviais descobertas hoje).
      **Verify:** `rm -rf TestResults/dod4 && dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --collect:"XPlat Code Coverage;Format=cobertura" --results-directory TestResults/dod4 >/dev/null 2>&1 && test "$(find TestResults/dod4 -name coverage.cobertura.xml | wc -l)" -eq 1 && F=$(find TestResults/dod4 -name coverage.cobertura.xml) && grep -oE 'filename="[^"]*FileUtility\.cs" line-rate="[0-9.]+"' "$F" | grep -oE '[0-9.]+"$' | tr -d '"' | awk '{n++; if($1+0<0.99) bad++} END{exit (n>0 && !bad)?0:1}' && grep -oE 'filename="[^"]*HtmlUtility\.cs" line-rate="[0-9.]+"' "$F" | grep -oE '[0-9.]+"$' | tr -d '"' | awk '{n++; if($1+0<0.99) bad++} END{exit (n>0 && !bad)?0:1}'`
      **Source:** CONTEXT (D-...-5); comando superseded por D-2026-07-31-coverage-90-8
      (mesmo mecanismo do item 3, em `TestResults/dod4`; cada arquivo aferido por classe, todas
      precisam ficar >= 0.99; pisos inalterados)

- [ ] CI wiring para cobertura JS: `sonarqube.yml` ganha `actions/setup-node`, o comando de
      teste JS com lcov, e `sonar.javascript.lcov.reportPaths` apontando pro arquivo certo.
      **Verify:** `W=.github/workflows/sonarqube.yml && P=$(grep -oE 'sonar\.javascript\.lcov\.reportPaths=[^ ")]+' "$W" | head -1 | cut -d= -f2) && D=$(grep -oE -- '--test-reporter-destination=[^ ]+' "$W" | head -1 | cut -d= -f2) && test -n "$P" && test "$P" = "$D" && test "$P" = "TestResults/js-lcov.info" && grep -qE 'uses: actions/setup-node@[0-9a-f]{40}' "$W" && grep -q -- '--experimental-test-coverage' "$W" && grep -q -- '--test-reporter=lcov' "$W"`
      **Source:** CONTEXT (D-...-2); comando superseded por D-2026-07-31-coverage-90-8 e depois
      por D-2026-07-31-coverage-90-9 (PROVA a correspondencia entre o caminho que o Sonar le e o
      que o reporter escreve, em vez de so constatar presenca das duas strings; `setup-node`
      exigido SHA-pinned por regex de 40 hex; mesmo conjunto de flags do comando antigo; -9 pina
      TAMBEM o literal `TestResults/js-lcov.info`, de modo que uma renomeacao COORDENADA dos dois
      lados do YAML — que mantinha `P = D` verde enquanto o item 2 media o caminho hardcoded —
      passa a reprovar; aditivo, nenhum piso ou criterio alterado)

### Manual
- _(none)_

## Deferred to PR review
- Confirmacao remota do SonarCloud de que a cobertura Overall da branch/PR atinge >=90% (so
  existe apos push+CI; os `Verify:` acima provam pisos locais por arquivo/agregado, nao o
  numero remoto — D-...-5).
- Confirmacao remota de "zero issues novas" (analisadores `external_roslyn`/`javascript`/
  `csharpsquid` do SonarCloud nao rodam local — D-...-6).
- Quality Gate (New Code) verde nao e usado como prova da meta de 90% Overall — sinal fraco,
  metricas diferentes (D-...-7).
- Confirmacao funcional/visual de que os 4 scripts de producao JS nao mudaram de comportamento
  (o harness so ADICIONA testes; qualquer edicao acidental de producao durante a escrita dos
  testes precisa de revisao humana, ja que nao ha suite E2E no WebView).

## Notes
Aritmetica completa (baseline, meta, alocacao por arquivo, margem) em
`D-2026-07-31-coverage-90-5`. Risco tecnico de implementacao (V8 `--experimental-test-coverage`
so atribui linha corretamente se `vm.Script` carregar o codigo com `filename` apontando pro
arquivo real de producao — copiar/colar como string literal quebra a atribuicao em silencio)
registrado em `D-2026-07-31-coverage-90-2`, responsabilidade do doer. Se a soma real ficar abaixo
de 187 linhas apos a primeira medicao, a reserva de contingencia e `ParsingEngine.cs` (ver
`## Out of scope` e `.jdi/todos.md` § `coverage-90`).
