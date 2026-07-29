# Decisoes locked do projeto

Append-only. Nunca editar decisao existente — adicionar uma nova que a supersede.

D-1 (2026-07-28): Code design = **The Method** (Juval Lowy, decomposicao baseada em
volatilidade). Camadas fechadas Client -> Managers -> Engines -> ResourceAccess -> Resources,
com Utilities vertical. Nao foi escolhido nesta adocao: ja estava locked e documentado pelo
usuario em `CLAUDE.md` ("Regras de Camada (OBRIGATORIAS)") e em `README.md`, e confirmado
pela estrutura fisica e pelo naming do repositorio (4 Manager, 3 Engine, 6 Access, 3 Utility).
As regras PROIBIDO de CLAUDE.md (sem Manager -> Manager sincrono, sem pular camadas, sem
regra de negocio em Manager ou PageModel) valem como criterio de review.

D-2 (2026-07-28): Adocao brownfield. Boundary do legado = commit `4285f25`
(`4285f25c308f6aeb0877202bb4aabf66523f7c1e`). A partir daqui:
- gate de cobertura de 80% vale **somente** para arquivos criados/alterados depois desse commit;
- codigo pre-existente fica isento e nao deve ser refatorado sem uma phase explicita;
- os 167 testes existentes sao baseline: podem ser mantidos, nao devem regredir.

D-3 (2026-07-28): As skills de arquitetura genericas do JDI (`clean-architecture`, `ddd`,
`hexagonal`, `onion`, `vertical-slice`) **nao** se aplicam a este projeto e nao podem
redecidir a arquitetura. O JDI e camada de processo; a arquitetura vigente e The Method (D-1).
Origem: secao "JDI — Workflow de Desenvolvimento" do `CLAUDE.md`.

D-4 (2026-07-28): Conventional commits passam a valer para commits novos (escopo = slug da
phase). O historico legado tem 0/10 commits no padrao e nao sera reescrito.

D-5 (2026-07-28): D-1 confirmada explicitamente pelo usuario (a adocao havia registrado o
design por inferencia de CLAUDE.md/README, sem canal interativo). The Method esta LOCKED.
Na mesma confirmacao o usuario aprovou os 6 candidatos detectados como phases do roadmap
e orquestracao `enhanced` em `config.json`.

D-2026-07-28-ci-seguranca-1: Phase 'Pipeline CI/CD com seguranca + correcao do .slnx'
(slug: ci-seguranca) adicionada via /jdi-issue (card colado). Reason: usuario pediu ajuste do
.slnx ("esta errado" — diagnostico: folder /.idea/ referencia arquivos gitignored) + pipeline
GitHub Actions com todas as validacoes de seguranca open source disponiveis, testes, releases
e SonarQube, com rigor. Overlap consciente com a phase `cobertura-e-ci`: o workflow de CI
nasce aqui; `cobertura-e-ci` mantem apenas o threshold do coverlet (90%, D-6) e o gate de
cobertura no pipeline.

D-2026-07-28-sast-sca-sbom-1: Phase 'Suplemento SAST/SCA/SBOM (paridade simulator-ccb)'
(slug: sast-sca-sbom) adicionada. Reason: usuario pediu comparacao com os mecanismos de
seguranca de github.com/slipalison/simulator-ccb e inclusao do que for relevante. Analise
comparativa (ci.yml de 1296 linhas, 12 jobs de seguranca) definiu o escopo:
APLICAVEIS (entram na phase): Semgrep SAST (2a camada rapida + regras custom para os riscos
reais deste repo: zip-slip na extracao de EPUB, XXE, injection no bridge JS do WebView,
BinaryFormatter); gate SCA continuo via `dotnet list package --vulnerable
--include-transitive` com falha em HIGH/CRITICAL; bump de SQLitePCLRaw.bundle_green 2.1.11
(CVE GHSA-2m69-gcr7-jv3q HIGH, transitiva via lib.e_sqlite3 — mudanca pontual de seguranca
permitida sobre legado, prioridade 1, nao e refactor em massa; supersede o roteamento
"so Dependabot" de W-1); TruffleHog --only-verified (verificacao ativa de credencial,
complementa gitleaks); SBOM Syft SPDX + dependency-snapshot (supply-chain/compliance,
informacional); SECURITY.md (politica de report — tambem vira PASS no check Security-Policy
do Scorecard).
NAO-APLICAVEIS (decisao auditada, nao re-levantar): DAST/OWASP ZAP — TranslateReader e app
cliente MAUI sem superficie HTTP; nao ha alvo pra scan dinamico, ZAP sem endpoint e teatro
de seguranca. Trivy Image + Dockle — nao ha Docker/imagem/registry. Checkov — nao ha IaC
(compose/terraform/k8s). Trivy FS — cego para NuGet sem packages.lock.json (projeto nao usa
lock files); substituido pelo gate SCA nativo dotnet, que enxerga transitivas (provado: NU1903
detectado no probe). Coverage gate no CI segue fora (pertence a phase cobertura-e-ci, D-2026-
07-28-ci-seguranca-5).

D-2026-07-28-pipeline-unificada-1: Phase 'Pipeline unificada (orquestrador reusable)'
(slug: pipeline-unificada) adicionada. Reason: usuario esperava visualizar todos os fluxos
numa unica pipeline; hoje cada push/PR gera ~8 runs separados na aba Actions. Validacao
tecnica: GitHub nao tem view nativa cross-workflow, mas reusable workflows (`workflow_call`)
entregam run graph unico — um orquestrador `pipeline.yml` (on: push main / pull_request /
workflow_dispatch) chama os demais como sub-workflows aninhados. Decisoes locked:
(a) ficam FORA do orquestrador, por limite tecnico real: `scorecard.yml` (OSSF exige workflow
isolado com `id-token: write` para `publish_results: true` — embutir quebra publish/badge) e
`release.yml` (trigger `tags: v*`, fluxo de release nao pertence ao pipeline de commit);
(b) scanners com cron semanal viram hibridos: mesmo arquivo com `on: workflow_call` +
`on: schedule` — rodam no grafo do orquestrador em push/PR e standalone no agendamento;
(c) chamadas locais usam path relativo (`uses: ./.github/workflows/_x.yml`) — sem SHA pin
(mesmo repo, sem supply-chain externo); hardening D-2026-07-28-ci-seguranca-4 continua valendo
dentro de cada reusable; (d) nomes de check mudam para `Pipeline / <job>` — branch protection
DEVE ser re-mapeada na mesma phase, com verificacao de que os required contexts batem com os
check names reais (incidente de hoje: 4 contexts com nome errado travaram todos os PRs);
(e) concurrency passa a ser do orquestrador (cancel-in-progress unico por ref).

D-6 (2026-07-28): Gate de cobertura sobe de 80% para 90% em codigo novo/alterado pos-boundary
`4285f25`. Origem: o usuario elevou o threshold em `.claude/rules/csharp.md` §6 no mesmo dia
do bootstrap. Supersede o numero de D-2; o boundary, a isencao do legado e o baseline de 167
testes permanecem inalterados. Sincronizado em `config.json` (`coverage_min`), `PROJECT.md`,
`ROADMAP.md` (goal da phase `cobertura-e-ci`), `reviewers.md`, `registry.md` e nos specialists
doer/reviewer.

D-2026-07-28-ci-seguranca-2: Escopo do fix do `TranslateReader.slnx` fica limitado a remover
o bloco de pasta `/.idea/` (3 refs a `.idea/.idea.TranslateReader/.idea/{encodings,indexLayout,
vcs}.xml`, gitignoradas por `.gitignore:29` — quebram a abertura da solution num clone limpo).
A referencia a `.claude/settings.local.json` no bloco `/.claude/` do `.slnx` permanece como
esta: o arquivo e rastreado pelo git (nao gitignorado), entao nao quebra nada; trocar pela
variante `.claude/settings.example.json` seria mudanca de escopo nao pedida pelo card ("ajuste
o .slnx pois esta errado" refere-se so ao `/.idea/` quebrado) e fica fora desta fase.

D-2026-07-28-ci-seguranca-3: "Todas as validacoes de seguranca open source disponiveis hoje
no GitHub" (card) fica definido, para esta fase, como o conjunto finito: CodeQL (queries
`security-extended`, linguagem `csharp`), Dependabot (`.github/dependabot.yml`, ecosystems
`nuget` + `github-actions`), `dependency-review-action` em pull requests, OSSF Scorecard
(workflow agendado + badge), scanner de secrets via action (gitleaks ou trufflehog) como
complemento ao secret scanning nativo do GitHub. Toggles nativos do GitHub que nao sao
arquivo versionado (push protection de secret scanning, branch protection em `main`,
Dependabot security alerts) NAO entram no DoD automatizavel desta fase — vao para
`## Deferred to PR review` do CONTEXT.md, pois exigem acao do dono do repositorio nas
Settings, nao um workflow.

D-2026-07-28-ci-seguranca-4: Resposta a "seja rigido" do card — hardening de supply-chain e
obrigatorio em todo workflow criado nesta fase: 100% das actions de terceiro pinadas por full
commit SHA (nunca tag `@vN` mutavel), bloco `permissions:` least-privilege (nega tudo no
top-level, cada job eleva so o que precisa), `step-security/harden-runner` em todo job
`ubuntu-latest`, `concurrency` com `cancel-in-progress` pra evitar runs supersedidos.
`zizmor` (linter estatico de workflows) e opcional, nao obrigatorio — registrado em `todos.md`.

D-2026-07-28-ci-seguranca-5: CI valida em 2 jobs: (a) `test` em `ubuntu-latest`, mirando so
`test/TranslateReader.Tests` + `src/TranslateReader.Core` (sem workload MAUI, mais rapido) com
`--collect:"XPlat Code Coverage"`; (b) `build` em `windows-latest`, `dotnet build -f
net10.0-windows10.0.19041.0` (mesmo alvo do Gate 1 do reviewer — unico TFM com backend
LLamaSharp hoje). O GATE de 90% (D-6) que falha o build fica para a phase `cobertura-e-ci`
(ver D-2026-07-28-ci-seguranca-1) — aqui so nasce a coleta de cobertura. Build de Android/iOS
em CI fica fora desta fase (exigiria workload + assinatura, nao pedido explicitamente no
card) — registrado em `todos.md`.

D-2026-07-28-ci-seguranca-6: "Releases" do card = workflow disparado por tag `v*`, publica o
TFM Windows (`dotnet publish -f net10.0-windows10.0.19041.0`) e cria GitHub Release com o
artefato anexado (ex.: `softprops/action-gh-release`, pinada por SHA — D-2026-07-28-
ci-seguranca-4). Assinatura/publicacao em loja (Google Play, App Store/TestFlight) fica fora
de escopo — sem certificados/secrets hoje — registrado em `todos.md`. "SonarQube" do card =
SonarQube Cloud (antigo SonarCloud, gratuito para open source; a action `sonarcloud-github-
action` esta deprecada em favor de `dotnet-sonarscanner` begin/end ao redor do build,
confirmado por pesquisa web em 2026-07-28). O workflow passa a existir e referenciar
`SONAR_TOKEN`, mas a execucao real (org/projeto criados no Sonar, token configurado nos
secrets, scan concluido sem findings novos bloqueantes) depende de acao humana fora do
repositorio — fica em `## Deferred to PR review`.
