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

D-2026-07-29-epub-zip-slip-1: Phase 'Zip-slip e bound de descompressao no EPUB'
(slug: epub-zip-slip) adicionada. Reason: achado da phase `readme` — ao verificar um claim de
seguranca do README o reviewer descobriu que o claim era falso E que a lacuna e real. Evidencia:
`ReadingManager.cs:59-60` monta o caminho de saida a partir de `epub.Content.Images.Local`
(derivado do arquivo EPUB, input nao confiavel) e entrega a `FileUtility.cs:31-32`, que faz
`Path.Combine` + `Directory.CreateDirectory` + `File.WriteAllBytesAsync` sem containment check e
sem bound de tamanho. Greps de confirmacao em `src/`: `GetFullPath|ExtractToFile|
ExtractToDirectory|entry.FullName` = zero; `maxSize|maxBytes|uncompressed|sizeLimit` = zero.
Viola `.claude/rules/csharp.md` §4 ("EPUB files are untrusted input... reject entry paths that
escape the target directory — zip-slip. Bound decompressed sizes").
AGRAVANTE (motivo de virar phase e nao ficar em todos.md): a regra custom
`translatereader-zip-slip` em `.semgrep/dotnet-security.yml` **nao cobre o caminho real** —
comprovado empiricamente pelo reviewer com probe de 4 casos: `Path.Combine(dest, entry.FullName)`
detecta, `entry.ExtractToFile(...)` detecta, espelho de `ReadingManager.cs:59` NAO detecta.
A regra exige o acesso sintatico a `.FullName`; como o projeto extrai via VersOne.Epub e nunca
toca `ZipArchiveEntry`, a regra nao pode disparar no unico vetor de zip-slip do produto em
nenhuma forma que ele venha a assumir. O gate de CI da falso conforto.
Escopo locked: a phase entrega **duas** coisas — (1) o containment de path (`Path.GetFullPath` +
verificacao de prefixo do diretorio destino) e o bound de tamanho descomprimido; (2) a correcao
da regra Semgrep para casar o padrao real, com fixture provando red antes e green depois.
Entregar so (1) deixa o defeito invisivel para o CI na proxima regressao. Codigo tocado e
pos-boundary, entao vale o gate de 90% de cobertura (D-6) e o teste comeca falhando (bugfix
starts with a failing test, `.claude/rules/csharp.md` §6).

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

D-7 (2026-07-30): Modelo do reviewer PINADO em **Fable 5 com reasoning `xhigh`**
(`.jdi/agents/jdi-reviewer-translatereader.md` > `runtime_intent.reasoning: xhigh` +
`runtime_overrides.claude: {model: fable, effort: xhigh}`). Pedido explicito do usuario na
invocacao de /jdi-issue que criou as phases `regression-suite` e `the-method-refactor`.
Supersede parcialmente a nota "No model pinned anywhere" que o bootstrap havia registrado com
base em PROJECT.md > LLM config ("Provider: nao definido — usar o default do ambiente"):
o pin vale SO para o reviewer; o doer continua herdando o default do ambiente. Racional
registrado: o reviewer e o gate que decide BLOCKED/APPROVED e roda o `mode=dod-critic`
(deteccao de Gate-8 Auto/PASS oco) — profundidade de raciocinio ali e mais barata que um
defeito que passa. Sincronizado em `reviewers.md` e `registry.md`.

D-2026-07-30-regression-suite-1: Phase 'Rede de testes de regressao' (slug: regression-suite)
adicionada via /jdi-issue (card colado: "com base no The Method, crie testes que garanta que as
funcionalidades sempre funcione da mesma forma independente das alteracoes sem quebrar o
funcionamento do aplicativo. E no termino, refatore todo o projeto com base no The Method e nas
boas praticas de desenvolvimento, lembrese que e um aplicativo e devemos economizar em consumo de
memoria e processamento, pois em um sistema android ou IOS iriamos consumir toda a memoria do
dispositivo e consumir toda a bateria do usuario e isso e uma experiencia inaceitavel").
Reason: o card contem DOIS entregaveis sequenciais e o proprio texto fixa a ordem ("E no
termino, refatore") — dividido em duas phases, `regression-suite` (esta) e `the-method-refactor`
(D-2026-07-30-the-method-refactor-1). Decisao de escopo confirmada pelo usuario nesta invocacao:
encadear ate o PR SOMENTE esta phase; o refactor roda depois, com a rede ja mergeada em `main`
protegendo-o e ja verde no CI.
ACHADO ESTRUTURAL que o card nao previu e que define o escopo desta phase:
`test/TranslateReader.Tests` tem `<TargetFramework>net10.0</TargetFramework>` e um unico
`ProjectReference` para `src/TranslateReader.Core`. O projeto MAUI (`src/TranslateReader`,
1516 linhas) portanto NAO tem caminho de teste algum hoje — `ReaderPageModel` (303 linhas),
`LibraryPageModel` (236), `ReaderPage.xaml.cs` (488) e `SettingsOverlay.xaml.cs` (221) estao
com zero cobertura e sao estruturalmente inalcancaveis: `LibraryPageModel` importa
`CommunityToolkit.Maui.Extensions` e `ReaderPageModel` usa `[QueryProperty]` do Shell, entao
testa-los exige um test project com TFM de MAUI (`net10.0-windows...`), que so roda em Windows
e sai do job `test` do CI (ubuntu-latest, D-2026-07-28-ci-seguranca-5). A phase DEVE decidir
explicitamente entre (a) segundo test project multi-TFM rodando so no job `build` do Windows,
(b) extrair a logica testavel dos PageModels para onde o test project atual alcance sem violar
The Method, ou (c) registrar a lacuna como aceita e limitar a rede ao Core — a escolha entra
no CONTEXT.md como decisao locked, nao pode ficar implicita, porque e exatamente a massa de
codigo que o refactor da phase seguinte vai tocar.
Baseline a preservar: 167 testes (D-2), hoje 169 atributos [Fact]/[Theory] em 17 arquivos.
Codigo de teste novo e pos-boundary `4285f25`, entao vale o gate de 90% (D-6).

D-2026-07-30-the-method-refactor-1: Phase 'Refactor The Method + memoria/CPU mobile'
(slug: the-method-refactor) adicionada via /jdi-issue, mesma card de
D-2026-07-30-regression-suite-1. Reason: segunda metade do card ("refatore todo o projeto com
base no The Method e nas boas praticas... economizar em consumo de memoria e processamento").
Escopo locked como **finding-driven, nao rewrite amplo** — confirmado pelo usuario nesta
invocacao. Justificativa registrada: PROJECT.md > Code Design ja atesta aderencia estrutural
total (4 Manager, 3 Engine, 6 Access, 3 Utility, naming 100%), entao "refatorar todo o projeto"
sem violacao apontada produziria um diff de 71 arquivos sem defeito nomeado, humanamente
irrevisavel, e tocaria as 1516 linhas do projeto MAUI justamente onde a rede de testes nao
alcanca (ver achado estrutural em D-2026-07-30-regression-suite-1). Em vez disso: auditoria
primeiro, e cada mudanca precisa de (1) uma violacao nomeada de CLAUDE.md ou de
`.claude/rules/csharp.md`, ou um hotspot de memoria/CPU medido, e (2) um teste da phase
`regression-suite` provando que o comportamento observavel nao mudou. Alvos de performance
declarados pelo card (mobile: memoria e bateria) mapeiam nos hot paths que
`.claude/rules/csharp.md` ja nomeia: ParsingEngine, HtmlUtility, TranslationEngine, loops de
capitulo/paragrafo/token, loops de linha do SQLite, e o limite de LOH (>= 85.000 bytes).
DEPENDENCIA: nao comecar antes de `regression-suite` estar mergeada em `main` — a rede e a
unica prova de que o refactor nao alterou comportamento. Nota: `.claude/rules/csharp.md` §2
exige "Measure before optimizing (BenchmarkDotNet, dotnet-counters, dotnet-gcdump)"; nao ha
infra de benchmark no repo hoje, entao a phase deve decidir se cria essa infra ou se limita as
mudancas as que sao conformidade de regra (provavel por inspecao) em vez de otimizacao
especulativa — ganho de bateria/memoria nao pode ser DECLARADO sem medida.

D-2026-07-30-regression-suite-2: Decisao central da fase (opcao exigida pelo achado estrutural
de D-2026-07-30-regression-suite-1) — **opcao (c) escolhida**: aceitar a lacuna do projeto MAUI
e limitar a rede desta fase a `src/TranslateReader.Core`, catalogando explicitamente o que fica
desprotegido. Decidido em modo `auto` (sem interacao), racional registrado:
- Opcao (a) (2o test project multi-TFM em `net10.0-windows10.0.19041.0`) exige infraestrutura
  nova inexistente hoje — csproj novo, referencia a `CommunityToolkit.Maui`/`.Extensions`, e um
  jeito de satisfazer `[QueryProperty]` (Shell) sem Shell vivo. Construir isso e trabalho
  formatado como producao (novo projeto, novos seams de DI) — risco real de virar o refactor que
  esta fase esta proibida de fazer (D-2026-07-30-regression-suite-1: "encadear ate o PR SOMENTE
  esta phase"). Alem disso, so rodaria no job `build` (windows-latest) do CI — o job `test`
  (ubuntu-latest, D-2026-07-28-ci-seguranca-5) nao tem workload MAUI, e esse job nunca foi
  escopado para rodar `dotnet test` (so `dotnet build -f net10.0-windows...`).
- Opcao (b) (extrair logica testavel dos PageModels) e refactor comportamental por definicao —
  ja esta explicitamente alocada em `the-method-refactor` (D-2026-07-30-the-method-refactor-1),
  que por sua vez DEPENDE desta fase estar mergeada primeiro. Faze-la aqui inverteria a
  dependencia que o usuario ja travou.
- Opcao (c): cruzando com os alvos que `the-method-refactor` ja declara (D-2026-07-30-
  the-method-refactor-1: ParsingEngine, HtmlUtility, TranslationEngine, loops de capitulo/
  paragrafo/token, loops de linha do SQLite, limite de LOH) — todos vivem em
  `TranslateReader.Core`, alcancaveis hoje pelo test project `net10.0` existente. Os arquivos
  App-layer citados no achado estrutural (`ReaderPage.xaml.cs` 488, `ReaderPageModel.cs` 303,
  `LibraryPageModel.cs` 236, `SettingsOverlay.xaml.cs` 221 — 1248 das 1516 linhas do app MAUI)
  ficam **fora** desta rede.
Inventario explicito do que fica desprotegido (registrado tambem em `.jdi/todos.md`): se uma
fase futura tocar `ReaderPage.xaml.cs`, `ReaderPageModel.cs`, `LibraryPageModel.cs`,
`SettingsOverlay.xaml.cs`, `Pages/Controls/TranslateBookPopup.xaml.cs`, `MauiProgram.cs`,
`AppShell.xaml.cs` ou `Utilities/*Converter.cs`, nenhum teste automatizado deste repo pega uma
regressao de comportamento ali — so revisao manual, ate uma fase fechar essa lacuna
deliberadamente.

D-2026-07-30-regression-suite-3: Dentro do Core, a lacuna objetiva de maior valor e
`BookTranslationJobAccess.cs` (107 linhas, 4 metodos publicos: `FetchActiveJobAsync`,
`SaveJobAsync`, `UpdateJobProgressAsync`, `DeleteJobAsync` — persiste o estado de pause/resume
da traducao de livro completo, a capacidade de destaque do produto per `PROJECT.md`). Hoje tem
ZERO teste dedicado — confirmado: nao esta entre os 16 arquivos de `test/TranslateReader.Tests`;
`grep BookTranslationJob` so bate em `TranslationManagerTests.cs` via interface substituida
(`ITranslationManager`), nunca contra a classe real. As 5 classes `*Access` irmas ja tem arquivo
de teste dedicado seguindo um padrao estabelecido e reusavel (`InMemoryDatabase.cs` + construtor
`(connectionString, initializeOnStartup: true)`). Esta fase cria `BookTranslationJobAccessTests.cs`
seguindo exatamente esse padrao — nao inventa infraestrutura de teste nova.

D-2026-07-30-regression-suite-4: Dois gaps adicionais, mais estreitos, fecham dentro do Core:
(1) o contrato de ordenacao de `BooksAccess.FetchAllBooksAsync`
(`ORDER BY LastOpenedAt DESC, DateAdded DESC` — livros abertos recentemente aparecem primeiro na
estante) nao tem teste nenhum provando a ordem, so uma asserção de contagem
(`BooksAccessTests.cs:57-60`, `Assert.Equal(2, books.Count)`); um refactor futuro dessa query
pode quebrar a ordem da estante em silencio e nada acusaria. (2) `ReadingManager.LoadProgressAsync`
so tem o caso nulo testado (`ReadingManagerTests.cs:78-86`); o caso de progresso encontrado nunca
foi caracterizado — gap de 1 linha, fechavel so com mock (sem I/O de disco).

D-2026-07-30-regression-suite-5: Dois gaps reais adicionais ficam DELIBERADAMENTE fora desta
fase, cada um por um motivo tecnico nomeado — nao sao descartados em silencio, viram achado
registrado para `the-method-refactor`:
(1) O branch "ja extraido, pula" de `ReadingManager.ExtractImagesIfNeededAsync`
(`ReadingManager.cs:50-54`) checa `Directory.Exists`/`Directory.GetFileSystemEntries` direto
contra o filesystem real, em vez de passar por `IFileUtility` — ja e por si so um cheiro de
violacao de fronteira de camada de The Method (Business Layer tocando Resource direto; CLAUDE.md:
"Business Layer (Managers) -> Engines, ResourceAccess, Utilities", nunca Resources). Testar esse
branch hoje exigiria I/O de disco real num teste NOVO, o que `.claude/rules/csharp.md` §6 proibe
("Isolated: no network/disk/real SQLite in unit tests") para qualquer teste escrito depois do
boundary `4285f25`. Fechar a lacuna exige mudanca de seam na producao (rotear a checagem de
existencia por `IFileUtility`) — isso e trabalho de refactor, fora do escopo desta fase pelo
proprio estatuto dela (D-2026-07-30-regression-suite-1). Registrado como achado nomeado para
`the-method-refactor` em `.jdi/todos.md`.
(2) O caminho de carregamento de modelo do `TranslationEngine`
(`InitializeAsync`/`CreateExecutor`, `TranslationEngine.cs:20-32,98-107`) envolve tipos concretos
do LLamaSharp (`LLamaWeights`, `StatelessExecutor`) sem nenhuma interface-seam para substituir; os
2 testes `[Trait("Category","Integration")][Fact(Skip=...)]` ja existentes em
`TranslationEngineTests.cs` sao o unico jeito de exercitar carregamento real de modelo (precisam
de fixture GGUF, opt-in via `LLAMASHARP_TEST_MODEL`), e ficam inalterados por esta fase. Estender
a cobertura unitaria aqui exigiria introduzir uma abstracao sobre o LLamaSharp — mudanca de seam
de producao, trabalho de refactor, fora de escopo.

D-2026-07-30-regression-suite-6 (guardrail): para manter a decisao de D-2026-07-30-
regression-suite-2 honesta na pratica (nao so no papel), esta fase NAO pode introduzir um
segundo test project ou multi-target o existente. `test/TranslateReader.Tests/
TranslateReader.Tests.csproj` permanece `<TargetFramework>net10.0</TargetFramework>` unico —
checado no DoD desta fase.

D-2026-07-30-the-method-refactor-2: Duas decisoes travadas exigidas pelo brief da fase (itens 3
e 5 do card colado via /jdi-issue). (A) Escopo restrito a `src/TranslateReader.Core` — espelha
a fronteira que `regression-suite` ja travou (D-2026-07-30-regression-suite-2): a rede de testes
de caracterizacao (192 atributos, PR #10 mergeado em `main`) so alcanca o Core; o app MAUI
(`src/TranslateReader/Pages`, `PageModels`, `Platforms`, `Utilities/*Converter.cs`,
`MauiProgram.cs`, `AppShell.xaml.cs`) permanece sem prova automatizada de comportamento. A
restricao 3 do brief exige que a rede seja a UNICA prova de nao-regressao — tocar codigo sem
rede nao pode ser feito nesta fase. (B) Resposta a exigencia de `.claude/rules/csharp.md` §2
("Measure before optimizing"): **opcao (a) escolhida** — a fase se limita a mudancas de
conformidade de regra, provaveis por inspecao estatica (sem BenchmarkDotNet/dotnet-counters/
dotnet-gcdump), nunca otimizacao especulativa declarada como ganho de memoria/bateria sem
medida. Introduzir infraestrutura de benchmark (opcao b) e trabalho novo, fora do estatuto
finding-driven desta fase (D-2026-07-30-the-method-refactor-1) — registrado em `.jdi/todos.md`
como candidato a fase futura, nao decidido aqui. O DoD desta fase verifica ambas as metades:
diff vazio em `src/TranslateReader/` e ausencia de `BenchmarkDotNet` em qualquer `.csproj`.

D-2026-07-30-the-method-refactor-3: Achado #1 fechado nesta fase — a violacao de fronteira de
camada pre-alocada em D-2026-07-30-regression-suite-5(1)
(`ReadingManager.ExtractImagesIfNeededAsync`, `ReadingManager.cs:53-54`, chama `Directory.Exists`/
`Directory.GetFileSystemEntries` direto contra o filesystem, pulando `IFileUtility` — viola
CLAUDE.md "Business Layer (Managers) -> Engines, ResourceAccess, Utilities", nunca Resources).
Correcao locked: `IFileUtility` ganha `bool DirectoryHasContent(string directoryPath)`;
`FileUtility` implementa (`Directory.Exists(directoryPath) && Directory.GetFileSystemEntries(
directoryPath).Length > 0`); `ExtractImagesIfNeededAsync` passa a chamar
`fileUtility.DirectoryHasContent(imagesDir)`. Efeito colateral que fecha a MESMA lacuna
registrada em `.jdi/todos.md` § `regression-suite`: o branch "ja extraido, pula" passa a ser
testavel com `IFileUtility` mockado (NSubstitute, ja usado em `ReadingManagerTests.cs`), sem I/O
real — fecha o gap sem violar `.claude/rules/csharp.md` §6. EXCLUI explicitamente a logica de
escrita em `ReadingManager.cs:59-60`/`FileUtility.cs:31-32` (`Path.Combine` + `WriteFileAsync`
sem containment de path) — esse e o vetor de zip-slip, propriedade da fase `epub-zip-slip`
(posicao 11, pendente, D-2026-07-29-epub-zip-slip-1); mesmo arquivo, linhas diferentes, sem
overlap de comportamento alterado. `FileUtilityTests.cs` (I/O real em temp dir, padrao
pre-existente da Utility layer) ganha caso para `DirectoryHasContent`, seguindo a mesma
convencao ja usada nos outros metodos do arquivo.

D-2026-07-30-the-method-refactor-4: Achado #2 (novo, levantado nesta sessao) —
`TranslationManager.cs:304-341` define 4 metodos privados de manipulacao de HTML via regex
(`ExtractParagraphs`, `ExtractTextBlocks`, `ReplaceTextBlocksInHtml`, `StripHtmlTags`, com 3
`[GeneratedRegex]`) que duplicam a responsabilidade que a propria tabela de componentes do
CLAUDE.md ja atribui a `HtmlUtility` ("Parsing e manipulacao de HTML para o reader (estatico)")
— o Manager ja usa `HtmlUtility.ExtractBodyContent(html)` no mesmo arquivo, entao a divisao de
responsabilidade esta inconsistente dentro da propria classe. Correcao locked: mover os 4
metodos (mantendo assinatura e nomes) + os 3 `[GeneratedRegex]` para `HtmlUtility` como
`public static`; `TranslationManager` passa a chamar `HtmlUtility.ExtractParagraphs(...)` etc.,
igual ao padrao ja usado para `ExtractBodyContent`. Mudanca e MOVE puro, sem alteracao de
comportamento — protegida pelas 48 ocorrencias de chamada indireta (`TranslateChapterAsync`/
`TranslateBookAsync`/`TranslateParagraphsAsync`) ja existentes em `TranslationManagerTests.cs`.
Nao introduz teste novo dedicado (comportamento ja caracterizado); o DoD verifica pela ausencia
das definicoes privadas no Manager e presenca publica em `HtmlUtility`.

D-2026-07-30-the-method-refactor-5: Achado #3 (novo, levantado nesta sessao, hotspot de CPU
nomeado por inspecao — nao medicao, per D-2026-07-30-the-method-refactor-2) — `ParsingEngine.cs`
chama `Regex.Replace`/`Regex.Match`/`Regex.IsMatch` com padrao inline literal repetidamente em
caminho por-capitulo: `UpdateOpfTitleAsync` (1, linha 126), `InlineCssLinks` (3, linhas
196/199/202 — regex externo + 2 checagens internas por `<link>` casado), `RewriteImagePaths` (3,
linhas 228/232/236 — uma por atributo de imagem, por capitulo) — total 7 padroes. Viola
`.claude/rules/csharp.md` §2.1: "Compile-time-known regex -> [GeneratedRegex] partial method,
never new Regex(...) per call" — o padrao e conhecido em tempo de compilacao e o
`TranslationManager` ja segue a convencao correta (`[GeneratedRegex]` para `ParagraphRegex`/
`TextBlockRegex`/`HtmlTagRegex`) a poucos arquivos de distancia, entao a inconsistencia e local
e nomeada. Correcao locked: `ParsingEngine` vira `partial class`; os 7 padroes viram
`[GeneratedRegex]` partial methods. Inspection-provable (sem BenchmarkDotNet, per
D-2026-07-30-the-method-refactor-2) — fonte gerada em compile-time elimina o lookup/compilacao
do padrao em runtime por chamada, ganho estrutural sem necessidade de medir para provar a
conformidade de regra. Protegida pelas 9 ocorrencias de `ExtractChapterContentAsync`/
`RewriteImagePaths`/`CreateTranslatedEpubAsync` ja em `ParsingEngineTests.cs` (fixtures reais de
EPUB).

D-2026-07-30-the-method-refactor-6: Achado pre-alocado D-2026-07-30-regression-suite-5(2)
(`TranslationEngine` acopla `LLamaWeights`/`StatelessExecutor` concretos, `TranslationEngine.cs:
20-32,98-107`, sem interface-seam) fica DEFERIDO explicitamente para a fase `llm-mobile`
(posicao 6, pendente), nao entra no escopo desta fase. Motivo nomeado: nao e violacao de
CLAUDE.md nem de `.claude/rules/csharp.md` hoje — a propria tabela de componentes do CLAUDE.md
define `TranslationEngine` como o Engine responsavel por "Inferencia local com LLamaSharp"; per
The Method, Engines sao exatamente o seam de volatilidade para tecnologia de terceiro, entao
acoplar a um unico backend concreto (Windows-only hoje, D-2026-07-29-readme-3) nao e, por si, um
defeito de camada. Introduzir uma abstracao de fabrica em torno de `LLamaWeights`/
`StatelessExecutor` sem uma segunda implementacao real seria abstracao especulativa (YAGNI) — so
se justifica quando `llm-mobile` precisar trocar de backend por plataforma (Android/iOS), que e
exatamente o escopo daquela fase. Os 2 testes de integracao `[Fact(Skip=...)]` seguem
inalterados. Overlap consciente registrado para o planner de `llm-mobile` ler esta decisao antes
de comecar.

D-2026-07-30-the-method-refactor-7: O `Verify:` do item 4 do Definition of Done desta fase
(CONTEXT.md, achado #3) fica SUPERSEDED pelo comando endurecido registrado abaixo. Motivo, com
contra-exemplo executado: o DoD critic (iter 1, read-only, segmento `## DoD Critic` de REVIEW.md)
provou que o `Verify:` original — `test $(grep -cE "Regex\.(Replace|Match|IsMatch)\(" F) -eq 0 &&
grep -q "public partial class ParsingEngine" F && test $(grep -c "\[GeneratedRegex" F) -ge 7` —
mede CONTAGEM, nunca IDENTIDADE: numa copia de `ParsingEngine.cs` com o pattern de
`StylesheetRelRegex` corrompido (`stylesheet` -> `stylsheet`) E `RegexOptions.IgnoreCase` removido
— exatamente a armadilha de migracao que o PLAN nomeou como risco #1 — o comando literal saiu
`exit 0`. A rede de testes tambem nao fechava o furo (medido na T-2: `StylesheetRelRegex`,
`OpfTitleRegex`, `LinkTagRegex` e `StylesheetHrefRegex` sozinhos produziam 0 falhas), entao a
unica prova de conformidade real era a inspecao manual byte-a-byte do reviewer — prova que vive
FORA do gate e nao sobrevive a proxima phase. Um `Verify:` que passa trivialmente e pior que
nenhum (`.jdi/todos.md` `[PROCESSO/DoD]`, mesma classe de defeito da regra Semgrep
`translatereader-zip-slip`). Esta decisao NAO reescreve nenhuma decisao anterior (append-only):
D-2026-07-30-the-method-refactor-5 continua valendo integralmente no QUE deve ser feito (7 padroes
inline -> `[GeneratedRegex]`, classe `partial`); o que muda e apenas COMO o DoD prova isso.

Novo criterio locked (4 propriedades, todas verificaveis por comando, nenhuma afrouxada em relacao
a versao anterior — o comando antigo esta contido no novo): (1) zero `Regex.(Replace|Match|IsMatch)(`
estatico no arquivo; (2) `public partial class ParsingEngine` presente; (3) EXATAMENTE 7
`[GeneratedRegex` (antes `>= 7`); (4) NOVO — para cada um dos 7, a linha de atributo conferida por
literal exato (`grep -F`, pattern E `RegexOptions` byte-a-byte) e ligada por adjacencia (`grep -A1`)
a assinatura `partial Regex <Nome>()` correspondente, mais `-eq 14` ocorrencias de linha dos 7 nomes
(7 declaracoes + 7 call sites, fechando o caso "regex declarado e nunca chamado"). Efeito: alterar
um unico caractere de qualquer pattern, remover um `RegexOptions.IgnoreCase`/`Singleline`, trocar
dois patterns de metodo ou orfanar um regex derruba o gate.

Prova por mutacao do proprio gate (5 mutacoes, copia em scratchpad, repo intocado): `stylsheet` +
sem `IgnoreCase` (o contra-exemplo do critico), so o pattern, so o `IgnoreCase`, `src`->`scr` em
`ImgSrcRegex`, e `IgnoreCase` removido de `ImgSrcRegex` — o comando ANTIGO da `exit 0` nas 5, o
NOVO da `exit 1` nas 5. Complemento (nao substituto): a mesma iter entrega
`test/TranslateReader.Tests/ParsingEngineRegexTests.cs`, que fecha a propriedade pelo lado do
COMPORTAMENTO (26 casos, reflection sobre as factories privadas, sem I/O de disco — §6 respeitada,
zero diff em producao). Os dois juntos cobrem o que nenhum cobre sozinho: o teste prova semantica
de casamento, o `Verify:` prova que o texto que gera essa semantica nao mudou.

D-2026-07-30-the-method-refactor-8: Os `Verify:` dos itens 4 e 5 do Definition of Done desta fase
(CONTEXT.md) ficam SUPERSEDED pelos comandos registrados no proprio CONTEXT.md sob esta decisao.
Ela NAO reescreve D-2026-07-30-the-method-refactor-5 nem -7 (append-only): o QUE deve ser feito
continua igual, e o endurecimento de identidade pattern/options entregue por D-...-7 continua
valendo integralmente — seu comando esta contido LITERALMENTE dentro do comando novo. O que muda e
so COMO o DoD prova as duas propriedades que o DoD critic (iter 2, segmento `## DoD Critic` de
REVIEW.md) derrubou com contra-exemplo EXECUTADO.

Furo 1 (item 4, achado #3): a promessa textual de D-...-7 — a clausula `-eq 14` fecha "regex
declarado e nunca chamado", sem qualificador — so valia para orfanamento SIMPLES. Contra-exemplo
M5 do critico: trocar UM token no call site `ParsingEngine.cs:196` (`StylesheetRelRegex` ->
`StylesheetHrefRegex`, nomes lookalike adjacentes, slip plausivel de refactor) deixa
`StylesheetRelRegex` declarado e nunca chamado COMPENSANDO a contagem agregada (segue 14) e o
comando sai `exit 0`. E furo DO CRITERIO, nao wiring fora dele. A rede de testes nao compensa por
construcao: os 26 casos de `ParsingEngineRegexTests.cs` invocam as factories por reflection (nunca
passam pelo wiring de producao) e `ParsingEngineTests` tem zero referencia a `stylesheet`/`css`/
`<link`. Causa raiz, mesma familia ja catalogada em `.jdi/todos.md` `[PROCESSO/DoD]`: o gate media
um proxy AGREGADO conveniente em vez da propriedade POR ITEM.
Correcao locked (clausula NOVA acrescentada; nenhuma clausula antiga removida ou afrouxada — o
`-eq 14` e os 7 pares `grep -A1 -F` permanecem): para CADA um dos 7 nomes exige-se
`declaracoes == 1` E `call sites >= 1`, e o numero de nomes que satisfazem AS DUAS condicoes tem
de ser EXATAMENTE 7. A varredura e feita em AWK que remove comentario de linha (`//`) e de bloco
(`/* */`, com estado entre linhas) antes de contar — comentar um call site nao o mantem vivo.

Furo 2 (item 5, guardrail agregado): duas clausulas mediam proxy errado.
(a) o criterio diz "nenhum pacote BenchmarkDotNet" sem escopo, mas o comando rodava `find src`:
`<PackageReference Include="BenchmarkDotNet"/>` em `test/TranslateReader.Tests/
TranslateReader.Tests.csproj` — o lugar NATURAL de infra de benchmark, exatamente o que
D-2026-07-30-the-method-refactor-2(B) barra — saia `exit 0`, e nenhum outro gate greppa
BenchmarkDotNet. Correcao locked: a busca cobre TODO arquivo capaz de declarar pacote em qualquer
lugar do repo (`*.csproj`, `*.props`, `*.targets`, `packages.config`, com `bin`/`obj`/`.git`
podados), sem depender de `Directory.Build.props`/`Directory.Packages.props` existirem hoje (nao
existem).
(b) o criterio diz "a contagem `[Fact]`/`[Theory]` nao regride", mas `grep -rhoE` conta TEXTO:
comentando 25 atributos reais (`// [Fact]`) a medida permanecia 214 e o comando saia `exit 0` com
189 atributos ATIVOS — regressao real passava. Correcao locked: conta-se so atributo VIVO, pela
mesma varredura AWK que descarta comentario de linha e de bloco. O piso continua `-ge 193`: o
criterio locka o baseline 192 e esta decisao corrige a MEDIDA, nao o limiar.

Containment formal (nenhuma clausula afrouxada): no item 4 o comando anterior esta contido
literalmente no novo; no item 5, `find .` podado e superconjunto de `find src`, e contagem VIVA <=
contagem TEXTUAL — logo, nas duas clausulas, NEW `exit 0` implica OLD `exit 0`.

Prova por mutacao (clone em scratchpad, repo real nunca mutado; OLD = comando vigente extraido por
sed de `CONTEXT.md` em `bc4f1c6`, NEW = comando desta decisao):
- item 4 — pristino OLD 0 / NEW 0; M5 do critico OLD 0 / NEW 1; os 7 orfaos COMPENSADOS (um por
  nome, call site trocado por outro nome, agregado sempre 14) OLD 0 / NEW 1 nos SETE; call site
  comentado com `// ` OLD 0 / NEW 1; call site dentro de bloco `/* */` OLD 0 / NEW 1; orfao
  SIMPLES (linha deletada) OLD 1 / NEW 1; e as mutacoes da iter 2 (pattern corrompido + sem
  `IgnoreCase`, options trocado, factory renomeada) OLD 1 / NEW 1 — o endurecimento de D-...-7
  sobrevive intacto.
- item 5 — BenchmarkDotNet no csproj de TESTE OLD 0 / NEW 1; em `Directory.Build.props` OLD 0 /
  NEW 1; em `Directory.Packages.props` (CPM) OLD 0 / NEW 1; no csproj de `src` OLD 1 / NEW 1;
  25 atributos comentados `// [Fact]` OLD 0 / NEW 1 (texto 214, vivo 189); 25 em `/* */` de uma
  linha OLD 0 / NEW 1; 25 em bloco `/* */` multi-linha OLD 0 / NEW 1; 25 atributos DELETADOS
  OLD 1 / NEW 1.
- falso positivo — no repo real sem mutacao os dois comandos saem `exit 0`, e a contagem VIVA e
  identica a TEXTUAL (214 = 214): em codigo limpo a medida nova nao muda o numero.

Zero linha de producao mudou por causa desta decisao — o codigo ja estava correto; o gate e que
nao provava.

D-2026-07-30-the-method-refactor-9: O `Verify:` do item 4 do Definition of Done desta fase
(CONTEXT.md) fica SUPERSEDED pelo comando registrado no proprio CONTEXT.md sob esta decisao. Ela
NAO reescreve D-2026-07-30-the-method-refactor-5, -7 nem -8 (append-only): o QUE deve ser feito
segue igual, o endurecimento de identidade pattern/options entregue por D-...-7 e a checagem POR
NOME com descarte de comentario entregue por D-...-8 seguem valendo integralmente. Muda UMA coisa:
como o passe AWK reconhece o nome da factory dentro de uma linha viva.

Furo (W-2/E5 da REVIEW iter 3, evasao EXECUTADA pelo reviewer): o passe AWK locked por D-...-8
testava o call site com `index(l, "<Nome>Regex()")` — casamento por SUBSTRING. Um call site trocado
por nome lookalike PREFIXADO (`ParsingEngine.cs:196`: `StylesheetRelRegex()` ->
`MyStylesheetRelRegex()`) CONTEM a string `StylesheetRelRegex()` como sufixo, entao a factory real
ficava DECLARADA E NUNCA CHAMADA e o gate saia `exit 0`. E a mesma familia do furo que D-...-8
fechou (orfao compensado), pela via do prefixo em vez da via da contagem agregada — e a mais
proxima de slip acidental das tres evasoes catalogadas em W-2, porque nao depende de nenhuma
construcao exotica de linguagem, so de um nome derivado.

Correcao locked (nenhuma clausula removida ou afrouxada; 12 das 13 clausulas ficam BYTE-IDENTICAS,
so o passe AWK muda): o reconhecimento do nome passa a exigir FRONTEIRA DE IDENTIFICADOR a
esquerda — a ocorrencia so conta se estiver no inicio da linha ou precedida por caractere fora de
`[A-Za-z0-9_]`. A fronteira a direita ja existia (o token inclui `()`). Implementacao: o
`if(index(l,t))` vira varredura de TODAS as ocorrencias de `t` na linha, aceitando a primeira que
tenha fronteira valida.

Containment formal (NEW `exit 0` implica OLD `exit 0` — provado, nao alegado):
- 12/13 clausulas identicas por comparacao literal de substring (`&&`-split); so a clausula do AWK
  difere.
- No AWK, o conjunto de linhas casadas por TOKEN e subconjunto das casadas por SUBSTRING, logo
  `c_novo[n] <= c_velho[n]` para todo nome.
- As DECLARACOES sao identicas nas duas versoes: a classificacao usa `index(l,"partial Regex " t)`,
  literal que ja embute um espaco antes do nome — toda declaracao reconhecida pela versao velha tem
  fronteira valida e e reconhecida igual pela nova. Medido: no arquivo pristino as duas versoes dao
  `d=1 / c=1` nos 7 nomes; no mutante de declaracao duplicada as duas dao `d=2`.
- Logo `k_novo <= k_velho`, e como o gate exige `k == 7`, nao existe estado de codigo em que a
  versao nova passe e a velha reprove. Nenhuma protecao antiga foi perdida.

Prova por mutacao nos DOIS sentidos (25 mutantes, harness em scratchpad, repo real nunca mutado;
OLD = comando vigente extraido por sed do CONTEXT.md em `7a4081a`):
- alvo novo — call site com lookalike PREFIXADO: `MyStylesheetRelRegex()` OLD 0 / NEW 1, e
  `CachedImgSrcRegex()` (2o nome, para nao provar em cima de um caso unico) OLD 0 / NEW 1. Nesse
  mutante o agregado `-eq 14` ainda le 14 e `[GeneratedRegex` ainda le 7: quem pega e
  exclusivamente o passe AWK novo.
- zero regressao — os 17 mutantes que a versao anterior ja pegava continuam pegos (OLD 1 / NEW 1):
  os 7 orfaos COMPENSADOS (um por nome), call site comentado com `//`, dentro de bloco `/* */`
  multi-linha, dentro de `///`, orfao SIMPLES (linha deletada), pattern corrompido + `IgnoreCase`
  removido (M1 da iter 2), `IgnoreCase` removido de `ImgSrcRegex`, rename consistente decl+call,
  `nameof(...)` sem parenteses, declaracao duplicada, e lookalike SUFIXADO.
- zero falso positivo novo — pristino OLD 0 / NEW 0, e as tres formas legitimas de call site que
  uma fronteira mal feita quebraria seguem 0/0: acesso por membro
  (`ParsingEngine.StylesheetRelRegex()`, fronteira `.`), chamada na coluna 1 (fronteira = inicio de
  linha) e chamada indentada com TAB.

Fora de escopo desta decisao (registrado, NAO fechado): as evasoes E1 (call site substituido por
string literal com o texto exato da invocacao) e E2 (call site vivo so sob `#if SIMBOLO_INDEFINIDO`)
continuam `exit 0`. Fecha-las exige, respectivamente, remover string literals do texto e resolver
diretiva de compilacao condicional — ou seja, parsear C# e o build graph, coisa que nenhum gate
textual em AWK/grep faz, e uma meia-solucao (heuristica de aspas, heuristica de `#if`) introduziria
falso positivo em codigo legitimo, que e a unica falha REALMENTE cara num gate. Diferente do
lookalike prefixado, essas duas nao tem caminho ACIDENTAL: o Core tem zero `#if` hoje, e escrever o
texto exato da invocacao dentro de uma string exige remover a chamada real de proposito. O backstop
declarado para codigo adversarial continua sendo o PR review humano (estatuto do /jdi-issue).

Nota de correcao a D-2026-07-30-the-method-refactor-8 (W-3 da REVIEW iter 3 — D-...-8 NAO e
reescrita, esta nota e o registro append-only da correcao): a frase de containment do item 5
daquela decisao, "`find .` podado e superconjunto de `find src`", nao e literalmente verdadeira. No
canto `bin`/`obj` ela e FALSA: um csproj com BenchmarkDotNet dentro de `src/**/obj/` ou
`src/**/bin/` da OLD 1 / NEW 0 (executado pelo reviewer, S4/S5). A divergencia e DELIBERADA e
CORRETA — artefato gerado pelo restore nao e declaracao de pacote, e o proprio criterio pede a poda
— e nenhuma protecao sobre declaracao REAL foi perdida (csproj de teste, `Directory.Packages.props`
com CPM e `Directory.Build.targets` todos OLD 0 / NEW 1). O que estava errado era a PALAVRA
"superconjunto": a relacao correta e "superconjunto sobre todo arquivo de declaracao REAL, com
`bin`/`obj`/`.git` deliberadamente excluidos". O claim de containment do item 5 vale nessa forma
corrigida; a MEDIDA e o COMANDO permanecem exatamente como D-...-8 os locked.

Nao fechado nesta rodada (W-5 da REVIEW iter 3 — registrado com motivo, nao esquecido): (a) `[ Fact ]`
com espacos nao e contado pelo passe AWK do item 5 — direcao FAIL-CLOSED (subconta: so pode derrubar
o gate, nunca deixar regressao passar) e semantica identica a do baseline 192, que tambem nunca os
contou; "corrigir" isso AFROUXA a medida e quebra a comparabilidade com o baseline. (b) String
literal `"[Fact]"` sobreconta — mesma classe de E1, exige parser. (c) Ratchet do piso `-ge 193` para
a medida atual (214) NAO e correcao de MEDIDA e sim mudanca do CRITERIO: o criterio locka o baseline
192, e um piso apertado pelo proprio doer ja sabendo que passa e movimento de trave, nao
endurecimento — exatamente o padrao que as iters 1-3 foram penalizadas por evitar. A janela de folga
de 21 atributos ja e coberta pelo Gate 2 (comparacao dos 227 aprovados / 229 totais). Ratchet e
politica ENTRE fases: roteado para `.jdi/todos.md`, para valer a partir da proxima phase.

Zero linha de producao mudou por causa desta decisao — pela terceira vez, o codigo ja estava
correto; o gate e que nao provava.

D-2026-07-30-sonar-zero-issues-0 (registro de phase): phase `sonar-zero-issues` registrada na
posicao 14 do ROADMAP. Origem: card despachado pelo usuario via `/jdi-issue` em 2026-07-30 —
"analise todas as issues que foram levantadas pelo sonarqube que estao na branch main, e resolva
todas elas, crie mecanismo que evite que esses tipos e issues voltem a acontecer no futuro"
(texto colado, sem URL de tracker). Baseline medido na API do SonarCloud no momento do registro
(`branch=main`, apos o merge do PR #11 / `6132078`): 113 issues abertas, 2 bugs, 7 vulnerabilities,
104 code smells, 0 security hotspots, coverage 72,7%, ncloc 4.074, sqale_index 401min.
D-2026-07-30-sonar-zero-issues-1: `dotnet-install.ps1` (41/113 issues, 36%, regras
`powershelldre:*`) e REMOVIDO do repo — opcao (b) das 3 do brief, nao (a) exclusao via
`sonar.exclusions` nem (c) corrigir codigo de terceiro. Evidencia: script vendored da Microsoft
(1573 linhas, commitado no legado `c86569e`), zero referencia em `src/`, `test/` ou qualquer
workflow (`grep -r "dotnet-install.ps1"` no repo inteiro so bate no proprio arquivo e numa
entrada de permissao Bash em `.claude/settings.local.json:38`), publicamente re-obtenivel em
`dotnet.microsoft.com/download/dotnet/scripts`. Excluir via `sonar.exclusions` manteria 1573
linhas mortas no repo so para o Sonar ignorar; corrigir 41 issues de estilo PowerShell em codigo
de terceiro nao gera valor e diverge do upstream. A linha de permissao stale em
`.claude/settings.local.json` e removida junto (mesmo commit) — arquivo rastreado pelo git
(D-2026-07-28-ci-seguranca-2), nao gitignorado.

D-2026-07-30-sonar-zero-issues-2 (mecanismo anti-recorrencia + fronteira com `baseline-de-estilo`):
o mecanismo desta fase e `sonar.qualitygate.wait=true` adicionado ao passo `dotnet-sonarscanner end`
em `.github/workflows/sonarqube.yml` — o scanner passa a falhar o job `sonarqube` (chamado por
`pipeline.yml:54`, ja check obrigatorio) se o Quality Gate do SonarCloud reprovar. O gate "Sonar way"
padrao mede New Code (rating de Reliability/Security/Maintainability = A), entao qualquer PR futuro
que reintroduza um bug/vulnerability/code smell bloqueante do tipo aqui resolvido derruba o pipeline
antes do merge — e a trava que o card pede ("evite que esses tipos... voltem a acontecer").
Fronteira com a phase `baseline-de-estilo` (posicao 1, pendente, goal "editorconfig, gitattributes e
analyzers configurados na raiz"): aquela phase e generica e local (Roslyn analyzers/editorconfig,
independe de rede/Sonar); esta fase e especifica do SonarQube e roda so em CI. Nao ha sobreposicao —
`baseline-de-estilo` continua dona integral do escopo dela, nada migrado.

D-2026-07-30-sonar-zero-issues-3 (taxonomia de resolucao, aplicada a todas as 113 issues): toda
issue termina em exatamente 1 de 3 estados, cada um com mecanismo auditavel em git (nunca so na UI
do SonarCloud): (a) FIX no codigo — maioria dos casos; (b) EXCLUSAO por `sonar.issue.ignore.
multicriteria` (rule+resourceKey) adicionado aos args do `begin` em `sonarqube.yml` — usado para
`Web:S7926` e `css:S4667` em `index.html` (ver D-...-4 abaixo), decisao deliberada sobre codigo
correto, nao defeito; (c) WAIVER via `#pragma warning disable <ID>`/`restore` no ponto exato, com
comentario citando a razao e este documento — usado para `SYSLIB1044` (`HtmlUtility.TextBlockRegex`,
backreference `\1` que o source generator nao otimiza, ja investigado no PR #11: mudar o pattern
mudaria comportamento) e para `xUnit1004` (os 2 `[Fact(Skip=...)]` de integracao do LLamaSharp em
`TranslationEngineTests.cs`, deliberados por D-2026-07-30-regression-suite-5(2) — desskipar quebra
CI sem fixture `.gguf`). Nenhuma issue fica "resolvida" so por decisao verbal nao rastreavel no repo.

D-2026-07-30-sonar-zero-issues-4 (`user-scalable=no` MANTIDO — waiver, nao fix por reflexo do
linter): `index.html:5` continua com `user-scalable=no`. Argumento: o modo Paginated do reader
(`paginated.js`) depende de viewport fixo para o calculo de coluna/pagina — pinch-zoom ativo quebra
o snap de pagina, uma feature central do produto. WCAG 2.1 SC 1.4.4 (Resize text) exige ALGUM
mecanismo de ampliacao ate 200%, nao especificamente pinch-zoom no WebView — o app ja oferece
tipografia configuravel (PROJECT.md > Stack: "temas Light/Dark/Sepia e tipografia configuravel"),
mecanismo equivalente e ja existente. Precedente de produto: leitores dedicados (Kindle, Apple
Books) tambem desabilitam pinch-zoom na tela de leitura e oferecem controle de fonte em vez disso.
Suprimido via `sonar.issue.ignore.multicriteria` (rule=`Web:S7926`, resourceKey=`**/index.html`),
mecanismo (b) da D-...-3. O mesmo mecanismo suprime `css:S4667` ("Empty source") na tag
`<style id="reader-theme"></style>` de `index.html:6` — vazia por design, populada em runtime via
JS ao trocar de tema (ThemeEngine gera o CSS, injetado via bridge), nao um estilo esquecido.

D-2026-07-30-sonar-zero-issues-5 (fronteira D-2/D-6 aplicada): esta fase E a "phase explicita"
exigida por D-2 para tocar `ParsingEngine.cs`, `BooksAccess.cs`, `SettingsAccess.cs`,
`ReadingStateAccess.cs`, `BookTranslationJobAccess.cs`, `TranslationManager.cs` e `HtmlUtility.cs`
(todos legados, pre-`4285f25`). Nenhuma mudanca planejada introduz caminho de codigo sem teste: toda
linha tocada ja e exercitada por suites existentes (`ParsingEngineTests.cs` com fixtures reais de
EPUB; `*AccessTests.cs` via `InMemoryDatabase`; `TranslationManagerTests.cs`, 48 ocorrencias
indiretas per D-2026-07-30-the-method-refactor-4) — D-6 (90% em codigo alterado) e satisfeito pela
cobertura ja existente sobre comportamento preservado, sem infra de teste nova. Excecao nomeada: os
3 arquivos JS do WebView (`translation.js`, `scroll.js`, `bridge.js`) NAO tem harness de teste
automatizado no repo (nenhum runner JS configurado, fora do escopo de qualquer `.csproj`) — D-6 nao
se aplica estruturalmente a eles; a confirmacao FUNCIONAL de que zoom/scroll/traducao continuam
corretos apos a migracao mecanica de API vai para `## Deferred to PR review` do CONTEXT.md, por ser
inerentemente humana (visual/interativa).

D-2026-07-30-sonar-zero-issues-6 (idempotencia do "0 issues" + limite do mecanismo): os `Verify:`
do DoD desta fase provam propriedades LOCAIS e reproduziveis (grep de identidade por arquivo/regra,
sem rede) — nao dependem de um scan real do SonarCloud, que so existe apos push+CI. A confirmacao
de que o Quality Gate real fica verde na branch/PR fica em `## Deferred to PR review`. LIMITE
estrutural do mecanismo (achado nesta sessao, registrado tambem em `.jdi/todos.md`):
`.github/workflows/sonarqube.yml` roda `dotnet build src/TranslateReader.Core/
TranslateReader.Core.csproj` e `dotnet test test/TranslateReader.Tests/...` entre o `begin`/`end`
do scanner — NUNCA compila `src/TranslateReader` (o head MAUI). O analisador C# do Sonar (Roslyn-
based) so ve o que e compilado nessa janela; logo `PageModels/`, `Pages/*.xaml.cs`, `Platforms/`,
`Utilities/*Converter.cs` e `MauiProgram.cs` sao estruturalmente invisiveis ao Sonar hoje — "0
issues" desta fase e valido para o que o Sonar de fato escaneia (Core C# + JS/HTML/PowerShell), nao
para o repo inteiro. Fechar isso exigiria um job Sonar rodando em `windows-latest` com workload MAUI
— infraestrutura de CI nova, fora do escopo desta fase (issues + mecanismo sobre o que ja e
escaneado), nao decidido aqui.
