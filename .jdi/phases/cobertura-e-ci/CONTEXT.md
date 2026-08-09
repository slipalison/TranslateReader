# Phase 2: Cobertura e CI — Context (slug: cobertura-e-ci)

Capturado em 2026-08-08. **Enquadramento corrigido (D-...-6):** a metade "workflow de CI com build e
testes" do goal do ROADMAP **ja existe** — `ci-seguranca`, `sast-sca-sbom` e `pipeline-unificada`
rodaram fora da ordem do roadmap e deixaram 12 workflows com orquestrador `pipeline.yml`. E a outra
metade nao existe da forma escrita: `coverlet.collector` **nao** suporta threshold. Esta fase e o
**gate**, exatamente como D-2026-07-28-ci-seguranca-1/-5 ja tinham alocado.

## Goal
O piso de 90% em codigo novo/alterado (D-6) deixa de ser prosa dentro do reviewer e vira um medidor
versionado que falha local e no CI, com job de cobertura proprio no pipeline.

## Locked decisions
- **D-...-1**: gate = `scripts/coverage-gate.sh` (bash, executa a propria medicao, diretorio limpo
  por execucao, cobertura **ponderada por linhas**) + SonarCloud New Code 80% -> 90% como 2a camada.
  `coverlet.msbuild` rejeitado (so mede agregado, que D-2 manda ignorar). Gate 3 do reviewer passa a
  CHAMAR o script.
- **D-...-2**: escopo = arquivos **criados OU modificados** pos-`4285f25` (`--diff-filter=AM`),
  fiel a `.claude/rules/csharp.md` §6. `test/**` fora. Arquivo sem linha instrumentada e listado e
  excluido do denominador, nunca ignorado em silencio.
- **D-...-3**: **bloqueia local E no CI**, em `push` e em `pull_request`. Sem modo informativo. O
  local do bloqueio no CI e o job dedicado (D-...-5 supersede o "step no `ci.yml`"); `ci.yml` perde
  `--collect` e o artifact — medicao unica, sem suite rodada duas vezes por PR.
- **D-...-4**: denominador = C# do Core + JS dos 4 scripts do WebView (piso proprio 85%, rodando
  fora do `sonarqube.yml`) + **guarda dura**: `.cs` NOVO em `src/TranslateReader/` falha (exit 2),
  com valvula auditavel `.jdi/coverage-waivers.txt` (path + referencia `# D-...` obrigatoria).
- **D-...-5**: `.github/workflows/coverage.yml` (`workflow_call` puro, hardening de
  D-2026-07-28-ci-seguranca-4) + caller `Coverage` no `pipeline.yml` + reportgenerator
  (STEP_SUMMARY + artifact) + fechamento do **W-2 Android** (NoWarn por ID preferido; desligar
  `TreatWarningsAsErrors` so no TFM android e ultimo recurso) + protocolo de remap de branch
  protection (snapshot antes, nome derivado do YAML, mutacao so depois do 1o run).
- **D-...-6**: ordem obrigatoria (medidor -> CI -> docs -> branch protection -> Android por ultimo)
  e linha de corte pre-escolhida caso o escopo estoure: itens 1-3 ficam, 4-5 viram phase propria.

## Canonical refs
- `.jdi/decisions/D-2026-08-08-cobertura-e-ci-1..6.md`
- `.claude/rules/csharp.md` §6 (fonte do "new/changed"), `.jdi/decisions/LEGACY.md` (D-2, D-6,
  D-2026-07-28-ci-seguranca-1/-4/-5, D-2026-07-30-regression-suite-2/-6,
  D-2026-07-31-coverage-90-1/-7/-8/-9, D-2026-07-28-pipeline-unificada-1/-2/-6)
- `.jdi/phases/baseline-de-estilo/REVIEW.md:176-190` (W-2, texto integral)
- Superficies: `scripts/coverage-gate.sh`, `.jdi/coverage-waivers.txt`,
  `.github/workflows/coverage.yml` (novos); `.github/workflows/{pipeline,ci}.yml`,
  `Directory.Build.props`, `.jdi/agents/jdi-reviewer-translatereader.md` (Gate 3, 200-278),
  `.jdi/agents/jdi-doer-translatereader.md:68,70`, `.jdi/PROJECT.md:27`,
  `.jdi/registry/LEGACY.md:26` (todos ainda dizem `coverlet.collector 8.0.1`; o repo usa 10.0.1)

## Out of scope
- Medir/tocar `src/TranslateReader/` (app MAUI) alem da guarda: a lacuna esta aceita por
  D-2026-07-30-regression-suite-2 e fecha-la e phase propria.
- Segundo test project ou multi-target do existente (proibido por D-2026-07-30-regression-suite-6).
- Job de lint/format no CI (rejeitado em D-2026-08-08-baseline-de-estilo-5) -> todos.
- Sonar em `windows-latest` com workload MAUI para medir o app (herdado de
  D-2026-07-30-sonar-zero-issues-6) -> todos.
- Corrigir qualquer warning congelado no `NoWarn` da fase anterior; `NoWarn` so encolhe por decisao.
- Build/teste de iOS/MacCatalyst no CI e assinatura/publicacao em loja -> todos (LEGACY.md:9-12).

## Definition of Done

> Comandos em bash (Git Bash no Windows), da RAIZ do repo. Logs em `TestResults/` (gitignored).
> Contrato de saida do script (varios `Verify:` dependem dele), uma linha por bloco em stdout:
> `COVERAGE_SCOPE covered=<int> valid=<int> pct=<float> files=<int>`;
> `COVERAGE_JS covered=<int> valid=<int> pct=<float> files=<int>`;
> `COVERAGE_GUARD new_app_cs=<int> waived=<int>`; e uma linha `COVERAGE_FILE <path> covered=<int>
> valid=<int>` por arquivo medido / `COVERAGE_SKIP <path> reason=<...>` por arquivo sem linha
> instrumentada. Exit: `0` pass, `1` abaixo do piso, `2` guarda do app MAUI, `3` falha de medicao.

### Auto-verifiable
- [ ] **DoD 1 — o medidor existe, e commitado como executavel, e prova que sabe FALHAR e que mede a
      execucao ATUAL.** Defaults pinados no script (90 / 85), verde no repo real, vermelho quando o
      piso sobe para 101, e o diretorio de trabalho apagado a cada rodada (o sentinel plantado antes
      da execucao nao sobrevive) — fecha os dois defeitos catalogados nos learnings de `coverage-90`
      (ler artefato velho / `;` que descarta exit code)
      **Verify:** `mkdir -p TestResults/coverage-gate && git ls-files -s scripts/coverage-gate.sh | grep -q '^100755' && grep -qE '^COVERAGE_MIN=\$\{COVERAGE_MIN:-90\}' scripts/coverage-gate.sh && grep -qE '^COVERAGE_JS_MIN=\$\{COVERAGE_JS_MIN:-85\}' scripts/coverage-gate.sh && touch TestResults/coverage-gate/STALE.sentinel && bash scripts/coverage-gate.sh > TestResults/cec-gate.log 2>&1 && ! test -e TestResults/coverage-gate/STALE.sentinel && { COVERAGE_MIN=101 bash scripts/coverage-gate.sh > TestResults/cec-gate-101.log 2>&1; test $? -ne 0; }`
      **Source:** D-...-1, D-...-3
- [ ] **DoD 2 — escopo `AM` pos-boundary e numero PONDERADO POR LINHAS, nao media de taxas.** A
      aritmetica da propria saida prova a forma da metrica (`pct == 100*covered/valid`), o que uma
      media nao-ponderada de `line-rate` nao satisfaz; nenhum arquivo de `test/` entra no
      denominador; e o piso de 90% e atingido de verdade
      **Verify:** `mkdir -p TestResults && grep -q 'diff-filter=AM' scripts/coverage-gate.sh && grep -q '4285f25' scripts/coverage-gate.sh && bash scripts/coverage-gate.sh > TestResults/cec-scope.log 2>&1 && ! grep -qE '^COVERAGE_FILE test/' TestResults/cec-scope.log && L=$(grep -m1 '^COVERAGE_SCOPE ' TestResults/cec-scope.log) && test -n "$L" && echo "$L" | awk '{for(i=2;i<=NF;i++){split($i,kv,"=");v[kv[1]]=kv[2]} if(v["valid"]+0<=0) exit 1; r=100*v["covered"]/v["valid"]; d=v["pct"]-r; if(d<0)d=-d; exit (v["pct"]+0>=90 && d<=0.06)?0:1}'`
      **Source:** D-...-1, D-...-2
- [ ] **DoD 3 — a guarda do app MAUI dispara de verdade, e o waiver e a UNICA saida.** Prova
      red-then-green executada: um `.cs` novo em `src/TranslateReader/` faz o gate sair com exit 2;
      o mesmo path com referencia de decisao em `.jdi/coverage-waivers.txt` volta a verde; e o repo
      fica limpo no fim (waiver restaurado, probe removido)
      **Verify:** `mkdir -p TestResults && P=src/TranslateReader/Probe_CoverageGate.cs && W=.jdi/coverage-waivers.txt && test -f "$W" && git ls-files --error-unmatch "$W" >/dev/null 2>&1 && cp "$W" TestResults/cec-waivers.bak && printf 'namespace TranslateReader;\ninternal sealed class ProbeCoverageGate { }\n' > "$P" && { bash scripts/coverage-gate.sh > TestResults/cec-guard-red.log 2>&1; R1=$?; printf '%s # D-2026-08-08-cobertura-e-ci-4 probe do DoD 3\n' "$P" >> "$W"; bash scripts/coverage-gate.sh > TestResults/cec-guard-green.log 2>&1; R2=$?; cp TestResults/cec-waivers.bak "$W"; rm -f "$P"; test "$R1" -eq 2 && test "$R2" -eq 0 && git diff --quiet -- "$W" && test -z "$(git status --porcelain -- "$P")"; }`
      **Source:** D-...-4
- [ ] **DoD 4 — JS medido pelo gate (nao so pelo Sonar), 4 arquivos, piso 85%, sem sequestrar o
      caminho pinado do Sonar.** O lcov e o desta execucao, dentro do diretorio do gate, e as duas
      ocorrencias de `TestResults/js-lcov.info` em `sonarqube.yml` continuam intactas
      (D-2026-07-31-coverage-90-9)
      **Verify:** `mkdir -p TestResults && bash scripts/coverage-gate.sh > TestResults/cec-js.log 2>&1 && L=$(grep -m1 '^COVERAGE_JS ' TestResults/cec-js.log) && test -n "$L" && echo "$L" | awk '{for(i=2;i<=NF;i++){split($i,kv,"=");v[kv[1]]=kv[2]} if(v["valid"]+0<=0) exit 1; r=100*v["covered"]/v["valid"]; d=v["pct"]-r; if(d<0)d=-d; exit (v["files"]+0==4 && v["pct"]+0>=85 && d<=0.06)?0:1}' && test -f TestResults/coverage-gate/js-lcov.info && test "$(grep -c 'TestResults/js-lcov.info' .github/workflows/sonarqube.yml)" -eq 2`
      **Source:** D-...-4
- [ ] **DoD 5 — job dedicado no pipeline, hardening intacto, medicao unica, piso nao-afrouxavel pelo
      YAML.** `coverage.yml` e `workflow_call` puro (sem `push`/`pull_request`/`dispatch`/`schedule`,
      sem `concurrency` proprio), `permissions: contents: read`, harden-runner presente, 100% das
      actions de terceiro pinadas por SHA de 40 hex, chama o script, **nao** define
      `COVERAGE_MIN`/`COVERAGE_JS_MIN`; `pipeline.yml` tem o caller `Coverage`; e `ci.yml` nao
      coleta mais cobertura
      **Verify:** `F=.github/workflows/coverage.yml && test -f "$F" && git ls-files --error-unmatch "$F" >/dev/null 2>&1 && grep -qE '^[[:space:]]+workflow_call:' "$F" && ! grep -qE '^[[:space:]]*(push|pull_request|workflow_dispatch|schedule):' "$F" && ! grep -qE '^[[:space:]]*concurrency:' "$F" && grep -qE '^permissions:' "$F" && grep -q 'step-security/harden-runner' "$F" && grep -q 'scripts/coverage-gate.sh' "$F" && ! grep -qE 'COVERAGE_(MIN|JS_MIN)' "$F" && U=$(grep -hE '^[[:space:]]+uses:' "$F" | grep -v 'uses: \./') && test -n "$U" && test "$(printf '%s\n' "$U" | grep -cvE '@[0-9a-f]{40}')" -eq 0 && grep -qF 'uses: ./.github/workflows/coverage.yml' .github/workflows/pipeline.yml && awk '/^  coverage:/{f=1;next} f&&/^  [a-z]/{f=0} f&&/name: Coverage/{ok=1} END{exit ok?0:1}' .github/workflows/pipeline.yml && ! grep -q 'XPlat Code Coverage' .github/workflows/ci.yml`
      **Source:** D-...-3, D-...-5
- [ ] **DoD 6 — o numero fica visivel sem baixar nada, e o artifact nao colide.** reportgenerator com
      `MarkdownSummaryGithub` no `$GITHUB_STEP_SUMMARY` + `Html` em artifact com
      `if-no-files-found: error`, e ZERO nome de artifact duplicado entre todos os workflows (todos
      compartilham o mesmo `run_id` desde `pipeline-unificada`, D-2026-07-28-pipeline-unificada-6(a))
      **Verify:** `F=.github/workflows/coverage.yml && grep -q 'reportgenerator' "$F" && grep -q 'reporttypes' "$F" && grep -q 'MarkdownSummaryGithub' "$F" && grep -q 'Html' "$F" && grep -q 'GITHUB_STEP_SUMMARY' "$F" && grep -q 'if-no-files-found: error' "$F" && test -z "$(grep -rhA6 'upload-artifact@' .github/workflows | grep -E '^[[:space:]]+name: ' | sed -E 's/^[[:space:]]+name:[[:space:]]*//' | sort | uniq -d)"`
      **Source:** D-...-5
- [ ] **DoD 7 — W-2 do Android respondido por MEDICAO e registro, nunca por relaxamento cego.** O
      artefato da fase registra o resultado real do `build-android` (zero IDs novos, ou a lista);
      `TreatWarningsAsErrors` continua `true`; o `NoWarn` mantem as invariantes da fase anterior (sem
      curinga, cada ID com comentario proprio); e se em algum lugar `TreatWarningsAsErrors` virou
      `false`, o arquivo prova que e escopado no TFM android e cita esta decisao
      **Verify:** `A=.jdi/phases/cobertura-e-ci/android-warnings.md && test -f "$A" && git ls-files --error-unmatch "$A" >/dev/null 2>&1 && { grep -q 'RESULTADO: zero IDs novos' "$A" || grep -qE '^- (CA|CS|MA|XA|NETSDK)[0-9]{3,5}' "$A"; } && P=Directory.Build.props && grep -qE '<TreatWarningsAsErrors>[[:space:]]*true' "$P" && V=$(tr -d '\r' < "$P" | tr '\n' ' ' | sed -n 's/.*<NoWarn>\(.*\)<\/NoWarn>.*/\1/p') && test -n "$V" && T=$(echo "$V" | tr ';' '\n' | sed 's/\$(NoWarn)//' | tr -d ' ') && test "$(echo "$T" | grep -vcE '^([A-Za-z]{2,4}[0-9]{3,5})?$')" -eq 0 && { for id in $(echo "$T" | grep -E '^[A-Za-z]{2,4}[0-9]{3,5}$' | sort -u); do test "$(grep -c "$id" "$P")" -ge 2 || exit 1; done; for f in Directory.Build.props src/TranslateReader/TranslateReader.csproj; do if grep -qE '<TreatWarningsAsErrors>[[:space:]]*false' "$f"; then grep -q 'net10.0-android' "$f" || exit 1; grep -q 'D-2026-08-08-cobertura-e-ci-5' "$f" || exit 1; fi; done; }`
      **Source:** D-...-5
- [ ] **DoD 8 — uma regra, um lugar: o reviewer chama o script e a documentacao para de mentir a
      versao.** Gate 3 referencia `scripts/coverage-gate.sh` e nao contem mais a implementacao de
      media nao-ponderada (`Measure-Object -Average`); nenhum dos 4 arquivos de processo ainda diz
      `coverlet.collector 8.0.1`
      **Verify:** `R=.jdi/agents/jdi-reviewer-translatereader.md && grep -q 'scripts/coverage-gate.sh' "$R" && ! grep -q 'Measure-Object -Average' "$R" && test "$(grep -l 'coverlet.collector 8.0.1' .jdi/PROJECT.md .jdi/registry/LEGACY.md .jdi/agents/jdi-doer-translatereader.md .jdi/agents/jdi-reviewer-translatereader.md 2>/dev/null | wc -l)" -eq 0 && grep -q 'coverlet.collector 10.0.1' .jdi/PROJECT.md`
      **Source:** D-...-1, D-...-6
- [ ] **DoD 9 — remap de branch protection com baseline e com o nome de check DERIVADO do YAML.**
      Snapshot commitado antes das edicoes, documento de remap citando o literal, e o literal
      conferido contra o que o `pipeline.yml` realmente produz (`name:` do orquestrador + ` / ` +
      `name:` do job) — e exatamente essa divergencia que travou todos os PRs em
      D-2026-07-28-pipeline-unificada-1(d)
      **Verify:** `B=.jdi/phases/cobertura-e-ci/branch-protection-before.json && M=.jdi/phases/cobertura-e-ci/branch-protection-remap.md && test -f "$B" && git ls-files --error-unmatch "$B" >/dev/null 2>&1 && grep -q 'required_status_checks' "$B" && test -f "$M" && git ls-files --error-unmatch "$M" >/dev/null 2>&1 && grep -qF 'Pipeline / Coverage' "$M" && W=.github/workflows/pipeline.yml && N=$(grep -m1 -E '^name:' "$W" | sed -E 's/^name:[[:space:]]*//' | tr -d '\r') && J=$(awk '/^  coverage:/{f=1;next} f&&/^[[:space:]]+name:/{sub(/^[[:space:]]+name:[[:space:]]*/,"");print;exit}' "$W" | tr -d '\r') && test "$N / $J" = "Pipeline / Coverage"`
      **Source:** D-...-5

### Manual
- _(none — todos os itens sao auto-verificaveis)_

## Deferred to PR review
- **Subir o Quality Gate do SonarCloud** de `new_coverage >= 80` para `>= 90` (D-...-1(b)): mora na
  UI/API do SonarCloud, nao ha arquivo versionado nem `SONAR_TOKEN` local. Confirmar apos o merge e
  registrar o antes/depois.
- **Mutacao real da branch protection** (adicionar o required context `Pipeline / Coverage`): exige
  token de admin e so pode ser feita DEPOIS de o job ter rodado uma vez com esse nome. Adicionar
  antes trava o repositorio.
- **Resultado real do `build-android`** com `TreatWarningsAsErrors=true` (W-2): nao ha Android SDK
  em maquina local; a primeira execucao da PR e a medicao. Se acender ID novo, a resposta e decisao
  nova (D-...-5(3) fixa a ordem de preferencia), nao patch.
- Confirmar no primeiro run que o check aparece com o nome exato `Pipeline / Coverage` e que a
  duracao do job novo nao dobrou o tempo de PR (a coleta saiu do `ci.yml`, entao o esperado e
  empate).

## Notes
- **Nao ha Bash nesta sessao de discuss** — nenhum numero de cobertura foi medido aqui. Os pisos
  (90% escopo, 85% JS) vem de D-6 e de D-2026-07-31-coverage-90-1, nao de medicao desta sessao. A
  primeira task do doer deve medir e reportar o numero de partida antes de ligar o bloqueio.
- **Dedup obrigatorio no parser** (D-...-1): o mesmo `.cs` pode render varios `<class>` no Cobertura;
  contar `<line>` sem deduplicar por `filename + number` infla o denominador de classes parciais.
- **Ordem de commits** (D-...-6): script -> workflow -> docs de processo -> branch protection ->
  Android. O Android e o unico item que so resolve no CI.
- **Escopo no limite**: 5 superficies novas + 1 item que so fecha na PR. Se o planner nao couber em
  8 tasks, D-...-6 ja fixou a linha de corte (itens 1-3 ficam; 4-5 viram phase propria) — dividir
  exige registrar decisao, nao e default.
- `npx -y jdi-cli render` nao pode ser executado aqui (sem Bash; e o CLI 0.12.1 quebra em Windows).
  As 6 decisoes estao gravadas em `.jdi/decisions/`; `DECISIONS.md` e view gerada e vai ficar
  desatualizada ate o render rodar.
