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
      **Verify:** `test -f test/js/paginated.test.js && test -f test/js/bridge.test.js && test -f test/js/translation.test.js && test -f test/js/scroll.test.js && node --test test/js/`
      **Source:** CONTEXT (D-...-1, D-...-2)

- [ ] Cobertura local agregada dos 4 scripts JS (`paginated.js`, `bridge.js`, `translation.js`,
      `scroll.js`) >= 85% via lcov.
      **Verify:** `node --test --experimental-test-coverage --test-reporter=lcov --test-reporter-destination=TestResults/js-lcov.info test/js/ >/dev/null 2>&1; awk -F: '/^SF:/{f=$2} /^LH:/{h=$2} /^LF:/{l=$2} /^end_of_record/{if(f ~ /(paginated|bridge|translation|scroll)\.js$/){H+=h;L+=l}} END{exit (L>0 && (H*100/L)>=85)?0:1}' TestResults/js-lcov.info`
      **Source:** CONTEXT (D-...-1, D-...-5)

- [ ] `ModelAccess.cs` cobertura local >= 90% (`DownloadModelAsync` testado com
      `HttpMessageHandler` fake, sem rede real).
      **Verify:** `dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --collect:"XPlat Code Coverage;Format=cobertura" --results-directory TestResults >/dev/null 2>&1; F=$(find TestResults -name "coverage.cobertura.xml" | sort | tail -1); R=$(grep -oE '<class name="[^"]*" filename="[^"]*ModelAccess\.cs" line-rate="[0-9.]+"' "$F" | grep -oE '[0-9.]+"$' | tr -d '"'); awk -v r="$R" 'BEGIN{exit (r!="" && r>=0.90)?0:1}'`
      **Source:** CONTEXT (D-...-3, D-...-5)

- [ ] `FileUtility.cs` e `HtmlUtility.cs` cobertura local = 100% (fechando os 3+2 linhas
      triviais descobertas hoje).
      **Verify:** `dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --collect:"XPlat Code Coverage;Format=cobertura" --results-directory TestResults >/dev/null 2>&1; F=$(find TestResults -name "coverage.cobertura.xml" | sort | tail -1); FU=$(grep -oE '<class name="[^"]*" filename="[^"]*FileUtility\.cs" line-rate="[0-9.]+"' "$F" | grep -oE '[0-9.]+"$' | tr -d '"'); HU=$(grep -oE '<class name="[^"]*" filename="[^"]*HtmlUtility\.cs" line-rate="[0-9.]+"' "$F" | grep -oE '[0-9.]+"$' | tr -d '"'); awk -v a="$FU" -v b="$HU" 'BEGIN{exit (a!="" && b!="" && a>=0.99 && b>=0.99)?0:1}'`
      **Source:** CONTEXT (D-...-5)

- [ ] CI wiring para cobertura JS: `sonarqube.yml` ganha `actions/setup-node`, o comando de
      teste JS com lcov, e `sonar.javascript.lcov.reportPaths` apontando pro arquivo certo.
      **Verify:** `W=.github/workflows/sonarqube.yml; grep -q "sonar.javascript.lcov.reportPaths" "$W" && grep -q "actions/setup-node@" "$W" && grep -q -- "--experimental-test-coverage" "$W" && grep -q -- "--test-reporter=lcov" "$W" && grep -q "TestResults/js-lcov.info" "$W"`
      **Source:** CONTEXT (D-...-2)

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
