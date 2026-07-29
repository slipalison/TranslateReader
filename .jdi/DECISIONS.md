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

D-2026-07-29-readme-1: Phase 'README completo com badges' (slug: readme) adicionada via
/jdi-issue (card colado: "Melhore o Readme.md do projeto, adicione as badges deixe bem
explicado"). Reason: alem das badges pedidas, a varredura do README atual (160 linhas)
encontrou erros factuais que tornam o documento enganoso — o escopo da phase inclui corrigi-los:
(a) **"Licenca: Projeto privado"** e FALSO — repo e PUBLIC no GitHub com arquivo `LICENSE`
Apache 2.0; (b) a feature que da nome ao projeto (traducao offline EN->PT-BR via LLamaSharp,
traducao de livro completo com job persistido, cache por hash, download de modelo GGUF) NAO
aparece em Funcionalidades nem na Stack; (c) tabela de Componentes lista 6 de 16 servicos reais
(faltam TranslationManager, SettingsManager, TranslationEngine, ThemeEngine, SettingsAccess,
TranslationCacheAccess, ModelAccess, BookTranslationJobAccess, PromptUtility, HtmlUtility);
(d) Estrutura do Projeto poe Contracts/Business/Access/Models dentro de `src/TranslateReader/`
quando vivem em `src/TranslateReader.Core/` (a solution tem 3 projetos, o README mostra 1);
(e) documenta `BookDetailPage.xaml` e `BookDetailPageModel.cs` que NAO existem no repo — e a
mesma evidencia que gerou a phase `detalhe-livro`, entao ficam marcados como planejados, nao
como existentes; (f) comandos de build usam `-f <TFM>` a nivel de solution, que falha com
NETSDK1005 (learning de ci-seguranca W-5) — corrigir para o csproj do app; (g) Modelos de Dados
omite Settings, TranslationCache e BookTranslationJob; (h) estrutura cita `.idea/` que e
gitignorada; (i) temas dizem "claro/escuro" mas o ThemeEngine entrega Light/Dark/**Sepia**.
Decisoes locked: badges apontam para workflows que existem no momento do merge (`pipeline.yml`
depende da phase 9 — ver ordem abaixo); nada de badge apontando para arquivo inexistente;
README continua em pt-BR sem acentos (padrao do arquivo atual, nao reescrever a grafia);
nenhuma feature futura pode ser descrita como pronta — o que nao existe vai para uma secao
explicitamente de roadmap. A branch desta phase sai de `jdi/pipeline-unificada` (PR #7) porque
o README documenta a pipeline unificada e a badge aponta para `pipeline.yml`, que so existe la;
se a PR #7 for mergeada antes, um rebase em `main` colapsa a dependencia.

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

D-2026-07-28-pipeline-unificada-2: Trigger surface completo, arquivo por arquivo (fecha o
double-run do fato tecnico 1 do card). Hoje NENHUM dos 10 workflows declara `on: workflow_call:`
— a migracao adiciona esse trigger aos 8 que entram no orquestrador (`ci.yml`, `codeql.yml`,
`dependency-review.yml`, `sonarqube.yml`, `sbom.yml`, `sca.yml`, `secret-scan.yml`,
`semgrep.yml`) e remove `push:`/`pull_request:` de todos eles (mantidos so em `pipeline.yml`,
`scorecard.yml` e `release.yml`, que nao mudam). `workflow_dispatch:` fica assim: adicionado
(bare, sem inputs) em `codeql.yml`, `semgrep.yml` e `secret-scan.yml` (nao tinham); mantido em
`sca.yml` e `sbom.yml` (ja tinham); REMOVIDO de `ci.yml`, que vira `workflow_call` puro (fato
tecnico 6 do card — o orquestrador passa a ser o unico jeito de rodar build+test manualmente);
NUNCA adicionado em `dependency-review.yml`, que fica so `workflow_call` — a action
`dependency-review-action` precisa de `base`/`head` SHA reais de um pull_request, que nao
existem num `workflow_dispatch` manual, entao dar essa opcao quebraria a execucao. Nenhum
arquivo e renomeado; `pipeline.yml` e o unico arquivo novo desta fase.

D-2026-07-28-pipeline-unificada-3: Matriz de permissions por job caller (fato tecnico 2 do
card — permissions efetivas de um reusable sao limitadas pelas permissions do job caller,
nunca as internas do proprio reusable sozinhas). `pipeline.yml` fica com `contents: read` no
top-level. Cada job caller eleva SO o que o reusable correspondente ja declara internamente:
`codeql` e `semgrep` elevam `security-events: write` (upload de SARIF, igual ao que
`codeql.yml`/`semgrep.yml` ja tem hoje); `sbom` eleva `contents: write` (Dependency Submission
API, igual ao que `sbom.yml` ja tem hoje); `dependency-review` eleva `pull-requests: write`
(comment-summary-in-pr, igual ao que `dependency-review.yml` ja tem hoje); `sonarqube`, `sca`,
`secret-scan` e `ci` ficam em `contents: read` (nenhum dos 3 faz upload de SARIF nem escreve
no repo). Sub-declarar quebra em runtime (403 silencioso no upload); sobre-declarar e
regressao de hardening (D-2026-07-28-ci-seguranca-4) — nenhum dos dois e aceitavel.

D-2026-07-28-pipeline-unificada-4: Secrets nao fluem implicitamente pro reusable (fato tecnico
3 do card). `secrets: inherit` aparece uma unica vez em `pipeline.yml`, so no job caller do
`sonarqube.yml` (precisa de `SONAR_TOKEN`); nenhum outro job caller herda secrets — least
privilege. Pesquisa confirmou (GitHub Docs "Reuse workflows"): `secrets: inherit` funciona
mesmo sem o reusable declarar `on.workflow_call.secrets` — nao e preciso adicionar esse bloco
em `sonarqube.yml`. Risco adicional identificado (nao estava na lista do card): a deteccao de
modo PR do SonarCloud NAO pode depender de `github.event_name` implicito dentro do reusable,
porque esse valor resolve pra `"workflow_call"` la dentro (nao pro evento original que
disparou o `pipeline.yml`) — em vez disso, `sonarqube.yml` ganha `on: workflow_call: inputs:`
(chave de PR, branch, base) e `pipeline.yml` preenche esses inputs usando o PROPRIO contexto
(`github.event.pull_request.*`), que e inambiguo porque `pipeline.yml` e diretamente disparado
pelo evento `pull_request`. `fetch-depth: 0` no checkout de `sonarqube.yml` (ja presente)
permanece intocado — necessario pro blame/SCM data do scanner.

D-2026-07-28-pipeline-unificada-5: Jobs condicionais (fato tecnico 7 do card: `dependency-
review` so faz sentido em pull_request, `sbom` so em push pra main) tem o `if:` no JOB CALLER
dentro de `pipeline.yml` (`if: github.event_name == 'pull_request'` / `if: github.event_name
== 'push'`) — NUNCA dentro do proprio arquivo reusable. Risco identificado (nao estava na
lista do card): dentro de um job disparado por `workflow_call`, `github.event_name` sempre
resolve pra `"workflow_call"`, nunca pro evento que disparou o orquestrador — se o `if:` fosse
colocado dentro de `dependency-review.yml` ou `sbom.yml`, a condicao nunca seria verdadeira e
o job ficaria permanentemente desligado, mesmo rodando em PR/push real. So o `pipeline.yml`,
que e diretamente disparado pelo evento original, tem o `github.event_name` correto pra essa
checagem.

D-2026-07-28-pipeline-unificada-6: Dois guardrails adicionais identificados (nao estavam na
lista do card). (a) Nomes de artifact (`actions/upload-artifact` `name:`) devem continuar
unicos entre os reusables migrados: antes da migracao cada workflow tinha seu proprio `run_id`
(sem risco de colisao); depois todos compartilham o `run_id` do `pipeline.yml`, entao dois
jobs com o mesmo nome de artifact colidiriam. Hoje nao ha colisao (`coverage` em `ci.yml`,
`sbom-spdx` em `sbom.yml`, unicos nomes existentes no escopo migrado) — convencao pra qualquer
artifact futuro: prefixar com o nome do job. (b) Antes de qualquer edicao de arquivo desta
fase, capturar um snapshot dos required status checks atuais de `main` (`gh api
repos/:owner/:repo/branches/main/protection`) salvo em
`.jdi/phases/pipeline-unificada/branch-protection-before.json` — baseline auditavel pro remap
descrito em D-2026-07-28-pipeline-unificada-1(d), pra nao repetir o incidente de hoje (4
contexts com nome errado travando todos os PRs).

D-2026-07-28-pipeline-unificada-7: SUPERSEDE a clausula `secrets: inherit` da
D-2026-07-28-pipeline-unificada-4 (o resto da D-...-4 — inputs explicitos de PR-context e
`fetch-depth: 0` intocado — continua valendo integralmente). O job caller do sonar em
`pipeline.yml` passa a declarar `secrets: SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}`, e
`sonarqube.yml` passa a declarar o secret em `on: workflow_call: secrets: SONAR_TOKEN:
required: false`. Os dois arquivos mudam JUNTOS: adicionar o map so no caller falha com
"Invalid input, SONAR_TOKEN is not defined in the referenced workflow". `required: false` e
deliberado — preserva o no-op gracioso dos `if: env.SONAR_TOKEN != ''` quando o secret nao
existe (fork, repo clonado). Raciocinio em tres partes, levantado pelo reviewer e aceito pelo
orquestrador: (1) NAO ha regressao de seguranca hoje — `inherit` nao despeja secrets no
ambiente, apenas os torna referenciaveis, e `sonarqube.yml` referencia unicamente
`SONAR_TOKEN`; a exposicao em runtime e identica com e sem `inherit`, entao isso era WARN, nao
BLOCK. (2) A D-...-4 se auto-contradizia: intitula-se "secrets nao fluem implicitamente /
least privilege" e mandava usar justamente o mecanismo que derrota least privilege; o
argumento registrado a favor de `inherit` ("funciona sem o reusable declarar
`on.workflow_call.secrets`") era de conveniencia, nao de seguranca. (3) O risco e futuro e
previsivel, nao teorico: `todos.md` ja contempla assinatura e publicacao em loja; no dia em
que um `SIGNING_KEY` entrar nos secrets do repo, `inherit` o tornaria referenciavel pelo job
que roda `dotnet-sonarscanner` + JRE + analisadores baixados em runtime e que egressa pra
`sonarcloud.io` por design, com `harden-runner` em `egress-policy: audit` (nao `block`), que
registra mas nao conteria exfiltracao. Atenuante registrado: o reusable e LOCAL (`uses: ./`,
mesmo repo e mesmo commit), entao a clausula mais forte da regra Semgrep
`yaml.github-actions.security.secrets-inherit` ("or sourced from a third party") nao se
aplicava — o ganho aqui e defesa em profundidade, nao correcao de vulnerabilidade ativa. O
DoD 5 da fase (`grep -c "secrets: inherit" == 1`) era um PROXY do objetivo "least privilege",
nao o objetivo; quando proxy e meta divergem, a meta vence — CLAUDE.md fixa a ordem Seguranca
> Performance > Boas praticas. O DoD 5 foi reescrito no CONTEXT.md desta fase pra provar o
estado novo (zero `secrets: inherit` no repo + exatamente um pass-through explicito no caller
+ declaracao presente no `workflow_call` do callee). Efeito colateral esperado: limpa o check
vermelho `Semgrep OSS` (regra `secrets-inherit`, antes em `pipeline.yml:59`).

D-2026-07-29-readme-2: Conjunto e ordem de badges definidos para a phase `readme`, todos com
URL real e resolvivel: (1) Pipeline (`actions/workflows/pipeline.yml/badge.svg`) — build/test/
scan agregado, existe nesta branch (saida de `jdi/pipeline-unificada`, PR #7 — ver
D-2026-07-29-readme-1); (2) CodeQL (`actions/workflows/codeql.yml/badge.svg`) — badge PROPRIO,
NAO dobrado dentro do badge do Pipeline: `codeql.yml` e hibrido (`workflow_call` + `schedule`
semanal + `workflow_dispatch`, via D-2026-07-28-pipeline-unificada-2), entao tem execucoes fora
do grafo do orquestrador (o cron semanal) e o sinal de seguranca de code-scanning merece
visibilidade independente do status geral do pipeline; (3) OpenSSF Scorecard — mantido, ja
existia no README; (4) e (5) SonarCloud Quality Gate + Coverage (`sonarcloud.io/api/
project_badges/measure?project=slipalison_TranslateReader&metric={alert_status,coverage}`) —
project key e org confirmados em `.github/workflows/sonarqube.yml` (`/k:"slipalison_
TranslateReader" /o:"slipalison"`); (6) License (shield Apache 2.0, linkado pro arquivo
`LICENSE`). Ordem no README: saude de build -> scanners de seguranca -> score de supply chain
-> qualidade -> licenca. Nenhuma badge pode referenciar workflow ausente de
`.github/workflows/` — o DoD desta fase verifica isso programaticamente (extrai todo
`actions/workflows/*.yml` citado no README e testa `-f` contra o diretorio real).

D-2026-07-29-readme-3: Defeito adicional encontrado nesta fase (nao estava no card nem na
lista (a)-(i) de D-2026-07-29-readme-1): a tabela "Plataformas Suportadas" do README atual
(linhas 19-27) marca Android/iOS/macOS como "Suportado" sem ressalva — ao entrar a feature de
traducao offline em Funcionalidades/Stack (item b da D-2026-07-29-readme-1), a tabela fica
enganosa por omissao: a traducao (o diferencial do projeto) so roda hoje em Windows
(`LLamaSharp` backends `Cpu`/`Cuda12` condicionados a `'windows'` no csproj — `PROJECT.md` >
Stack). Mitigacao: a descricao da feature de traducao (Funcionalidades e/ou Stack) traz
explicitamente a ressalva "traducao offline: hoje somente Windows; Android/iOS planejado via
phase `llm-mobile`" — a tabela de plataformas em si nao muda (ela descreve o app como um todo,
que roda nas 4 plataformas). Verificado no mesmo DoD da feature de traducao, sem item proprio,
pra nao estourar o cap de 10 itens da phase.

D-2026-07-29-readme-4: Alem da correcao dos defeitos (a)-(i) de D-2026-07-29-readme-1, o README
ganha 4 conteudos que hoje nao existem e o repo ja justifica (pos ci-seguranca/sast-sca-sbom/
pipeline-unificada): (1) Seguranca — cita `SECURITY.md` (politica de report ja existe no
repo), o conjunto de scanners rodando (CodeQL, Semgrep, SCA dotnet, secret scan, OpenSSF
Scorecard) e o hardening de supply-chain (actions de terceiro pinadas por SHA, D-2026-07-28-
ci-seguranca-4); (2) como rodar os testes (`dotnet test`) e a regra de cobertura 90% em codigo
novo/alterado pos-boundary (D-6); (3) Contributing/JDI — ponteiro pra secao "JDI — Workflow de
Desenvolvimento" do `CLAUDE.md`, pra quem quiser contribuir; (4) licenca Apache 2.0 (fundida
com a correcao do defeito (a)).
