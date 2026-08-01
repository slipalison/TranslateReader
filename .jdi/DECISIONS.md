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

D-2026-07-30-sonar-zero-issues-7: O `Verify:` do item 1 do Definition of Done desta fase
(CONTEXT.md) fica SUPERSEDED pelo comando registrado no proprio CONTEXT.md sob esta decisao. Ela
NAO reescreve D-2026-07-30-sonar-zero-issues-1 (append-only): o QUE deve ser feito segue
identico — `dotnet-install.ps1` REMOVIDO do repo e a permissao stale de
`.claude/settings.local.json:38` removida no MESMO commit. Muda so COMO o DoD prova isso.

Furo (auto-derrota, medido antes de qualquer edicao): o comando vigente era
`test ! -e dotnet-install.ps1 && test -z "$(grep -rl 'dotnet-install\.ps1' --exclude-dir=.git . 2>/dev/null)"`.
`grep -r` varre TODO byte do working tree, entao ele bate em tres classes de arquivo que a phase
nao pode e nao deve limpar:
(a) o proprio registro de auditoria — `.jdi/DECISIONS.md:735,739` (a D-...-1 CITA o nome do
    arquivo por design) e `.jdi/phases/sonar-zero-issues/CONTEXT.md:9,43,44` (o proprio item do
    DoD contem a string que ele proibe — o gate se auto-derrota por construcao);
(b) `.jdi/phases/sonar-zero-issues/PLAN.md` (7 ocorrencias, mesma natureza de registro);
(c) `.idea/.idea.TranslateReader/.idea/workspace.xml` — untracked e gitignorado
    (`.gitignore:29`), estado local de IDE de UMA maquina; nenhum commit desta phase o alcanca,
    e o DoD passaria ou reprovaria conforme a maquina que rodasse o comando.
Consequencia: o comando antigo saia `exit 1` mesmo com a phase 100% entregue — gate impossivel de
satisfazer, que e a forma degenerada do defeito ja catalogado em `.jdi/todos.md` `[PROCESSO/DoD]`
(o gate mede um PROXY, aqui o proxy "nenhum byte do repo menciona a string", em vez da propriedade
real).

Propriedade REAL locked: **(1) o arquivo nao existe mais no working tree E (2) nenhum arquivo
RASTREADO pelo git fora de `.jdi/` o referencia.** `.jdi/` e excluido por ser registro de
auditoria append-only cuja funcao E citar o nome; arquivo untracked/ignorado e excluido porque
nao e conteudo do repo. Comando novo (byte-a-byte igual ao que vai para o CONTEXT.md):
`test ! -e dotnet-install.ps1 && test -z "$(git grep -l 'dotnet-install\.ps1' -- . ':(exclude).jdi' 2>/dev/null)"`

Nenhuma protecao afrouxada em relacao ao que o criterio queria dizer: a clausula `test ! -e` fica
BYTE-IDENTICA, e a segunda clausula continua exigindo ZERO referencia — o que muda e o universo
varrido, que passa a ser exatamente o universo que a phase controla (arquivos rastreados, fora do
registro de auditoria). O caso concreto que motivou a clausula em D-...-1 — a permissao stale em
`.claude/settings.local.json:38`, arquivo RASTREADO (D-2026-07-28-ci-seguranca-2) — continua
integralmente coberto: `git grep` o enxerga, e o gate reprova se ela voltar.

Prova por mutacao nos DOIS sentidos (executada, repo real):
- estado ANTES da entrega (arquivo presente + permissao presente): NEW `exit 1`. OLD tambem
  `exit 1`, mas por motivo errado (bateria em `.jdi/` e `.idea/`, nao no defeito).
- estado DEPOIS da entrega (arquivo deletado + permissao removida): NEW `exit 0`; OLD `exit 1`
  (auto-derrota — bate em `.jdi/DECISIONS.md`, `CONTEXT.md`, `PLAN.md`, `.idea/workspace.xml`).
- mutante realista M1 — arquivo deletado mas a linha de permissao de
  `.claude/settings.local.json` READICIONADA (regressao exata que D-...-1 quer barrar):
  NEW `exit 1`.
- mutante M2 — permissao removida mas `dotnet-install.ps1` restaurado: NEW `exit 1` (pega pelas
  duas clausulas).
- falso positivo — no repo entregue, sem mutacao, NEW `exit 0`.

D-2026-07-30-sonar-zero-issues-8: O `Verify:` do item 9 do Definition of Done desta fase
(CONTEXT.md — `TranslationManager.cs`, S107 + S3267) fica SUPERSEDED pelo comando registrado nesta
decisao e copiado byte-a-byte para o CONTEXT.md. Ela NAO reescreve `D-2026-07-30-sonar-zero-issues-5`
nem qualquer decisao anterior (append-only): o QUE deve ser feito segue identico — os 2 helpers
privados `TranslateChaptersWithCacheAsync` e `TranslateSingleChapterAsync` declaram no maximo 7
parametros (S107, via objeto de contexto privado) e o loop de `chapters` usa
`.Select(chapter => chapter.HRef)` (S3267). Muda so COMO o DoD prova isso. Nenhuma linha de
producao muda por causa desta decisao — o refactor da iter 1 ja entrega 5 parametros reais em cada
metodo; o que estava quebrado era a PROVA.

Furo (contra-exemplo EXECUTADO pelo DoD critic da iter 1 — ver `## DoD Critic` em
`.jdi/phases/sonar-zero-issues/REVIEW.md`, veredito BLOCKED): o comando vigente era
`F=src/TranslateReader.Core/Business/Managers/TranslationManager.cs; for M in TranslateChaptersWithCacheAsync TranslateSingleChapterAsync; do N=$(awk -v m="$M(" 'index($0,m){f=1} f{printf "%s",$0; if(/\)$/ && f>1){exit} f++}' "$F" | grep -o "," | wc -l); test "$N" -le 6 || exit 1; done && grep -q "chapters.Select(chapter => chapter.HRef)" "$F"`
Esse `awk` NAO mede parametros. Ele (1) acha a PRIMEIRA linha do arquivo que contem `<Nome>(`,
(2) concatena linhas ate a primeira terminada em `)`, (3) conta as virgulas dessa janela textual.
Para `TranslateChaptersWithCacheAsync` a primeira ocorrencia no arquivo e o CALL SITE
(`TranslationManager.cs:59`, 5 argumentos), nao a declaracao (`:147`) — a janela mede o CHAMADOR.
Consequencia medida: uma copia do arquivo com 3 parametros extras inseridos na DECLARACAO (8 no
total — exatamente a violacao S107 que o item existe para impedir) continua saindo `exit 0`. O gate
e POSICIONAL, nao semantico: o proprio SUMMARY da iter 1 admite ter reordenado a declaracao de
`TranslateSingleChapterAsync` para antes do chamador "para a janela cair sobre a declaracao".
Mesma familia de defeito ja catalogada em `.jdi/todos.md` `[PROCESSO/DoD]` e causa das 2
reprovacoes da phase `the-method-refactor`: o gate mede um PROXY conveniente — aqui "virgulas de
uma janela textual" — em vez da propriedade.

Propriedade REAL locked: **cada um dos dois metodos DECLARA no maximo 7 parametros, contados na
assinatura da propria DECLARACAO, independentemente da posicao dela em relacao a qualquer
chamador e do numero de linhas em que a assinatura esteja quebrada.** Comando novo (byte-a-byte
igual ao que vai para o CONTEXT.md):
`F=src/TranslateReader.Core/Business/Managers/TranslationManager.cs; for M in TranslateChaptersWithCacheAsync TranslateSingleChapterAsync; do test $(grep -cE "^[[:space:]]*private async Task $M\(" "$F") -eq 1 || exit 1; N=$(awk -v m="private async Task $M(" 'index($0,m){f=1} f{for(i=1;i<=length($0);i++){h=substr($0,i,1); if(h=="("){p++} else if(h==")"){p--; if(p==0){print (s?k+1:0); exit}} else if(h=="<"){if(pc!=" ")a++} else if(h==">"){if(a>0)a--} else if(h=="["){b++} else if(h=="]"){if(b>0)b--} else if(h=="{"){c++} else if(h=="}"){if(c>0)c--} else if(h==","){if(p==1&&a==0&&b==0&&c==0)k++} else if(p>=1&&h!=" "&&h!="\t"){s=1} pc=h}}' "$F"); test -n "$N" && test "$N" -le 7 || exit 1; done && grep -q "chapters.Select(chapter => chapter.HRef)" "$F"`

Como ele mede (3 mudancas de natureza, nao de limiar):
1. ANCORA NA DECLARACAO: `^[[:space:]]*private async Task <Nome>(`, exigida EXATAMENTE 1 vez
   (`grep -cE ... -eq 1`). Nenhum call site casa esse prefixo — `await <Nome>(...)` nao contem
   `private async Task `. Some a declaracao (rename/mudanca de assinatura) e o gate reprova.
2. CONTA PARAMETROS, NAO VIRGULAS DE JANELA: varre caractere a caractere a partir da ancora ate o
   `)` que fecha a assinatura (profundidade de parenteses de volta a 0) e conta SEPARADORES de
   parametro — virgulas em profundidade de parenteses 1 com profundidade 0 de `<>`, `[]` e `{}`.
   Parametros = separadores + 1 (0 se a assinatura for vazia). Virgula de generico
   (`Dictionary<string, int>`), de atributo (`[Foo(1, 2)]`) e de inicializador nao contam.
3. LIMIAR NA UNIDADE CERTA: `-le 7` PARAMETROS (o antigo comparava `-le 6` VIRGULAS de uma janela
   arbitraria). Mesma fronteira do S107, agora contada na unidade que a regra usa.

Nenhuma protecao afrouxada: a clausula `grep -q "chapters.Select(chapter => chapter.HRef)"` fica
BYTE-IDENTICA (S3267 continua preso do mesmo jeito), e o novo comando e ESTRITAMENTE mais forte —
reprova tudo que o antigo reprovava e mais (matriz abaixo, colunas NEW/OLD).

Prova por mutacao (executada; repo real NUNCA mutado — copias em `/tmp/s107/<var>/`,
`git status --porcelain` vazio ao final; `Nc/Ns` = parametros medidos em
`TranslateChaptersWithCacheAsync`/`TranslateSingleChapterAsync`):

| Var | Mutante | NEW | OLD | Nc/Ns |
|---|---|---|---|---|
| m0 | copia intacta do arquivo entregue | `exit 0` | `exit 0` | 5/5 |
| m1 | 3 params extras na DECL de `TranslateChaptersWithCacheAsync` (8) | **`exit 1`** | `exit 0` | 8/5 |
| m2 | 3 params extras na DECL de `TranslateSingleChapterAsync` (8) | **`exit 1`** | `exit 1` | 5/8 |
| m3 | REORDER puro: as 2 decls movidas para DEPOIS dos chamadores | `exit 0` | **`exit 1`** | 5/5 |
| m4 | m3 + 8 params na DECL de `TranslateSingleChapterAsync` | **`exit 1`** | `exit 1` | 5/8 |
| m11 | a mesma violacao de 8 params colapsada em UMA linha | **`exit 1`** | `exit 0` | 8/5 |
| m12 | fronteira: exatamente 7 params | `exit 0` | `exit 0` | 7/5 |
| m5 | 6o param `Dictionary<string, int>` (virgula de generico) | `exit 0` | `exit 0` | 6/5 |
| m7 | 6o param com default `1 > 0 ? 1 : 0` (`>` sem par) | `exit 0` | `exit 0` | 6/5 |
| m10 | 6o param com default `1 < 2 ? 1 : 0` (`<` sem par) | `exit 0` | `exit 0` | 6/5 |
| m8 | clausula S3267 revertida para `foreach (var chapter in chapters)` | `exit 1` | `exit 1` | 5/5 |
| m9 | declaracao renomeada (ancora ausente) | `exit 1` | `exit 1` | — |

m1 e o contra-exemplo do critico reproduzido: OLD `exit 0` (falso PASS), NEW `exit 1`. m3 e a prova
de que o gate deixou de ser posicional: reordenacao pura (mesmo multiset de linhas, verificado por
`sort`+`cmp`) NAO muda o resultado do NEW, enquanto DERRUBA o OLD. m8/m9 provam que nao houve
regressao de gate.

Residuos DECLARADOS (nenhum silenciado; nenhum ocorre nos 2 metodos de hoje, que nao tem valor
default nem literal na assinatura):
- Virgula dentro de LITERAL DE STRING num valor default (`string x = "a,b"`) e contada como
  separador — medido em m6: 6 parametros reais reportados como 7. Direcao SEGURA (superestima):
  so pode causar reprovacao falsa, nunca aprovacao falsa. Idem para virgula dentro de comentario
  `//` escrito no meio da assinatura.
- `<` colado a um identificador dentro de um valor default (ex.: `int x = 1<2 ? 1 : 0`, sem
  espaco) abriria a profundidade de generico e subestimaria. As duas variantes com formatacao
  normal foram fechadas e medidas: `>` sem par por clamp `if(a>0)a--` (m7 = 6, correto), `<` sem
  par pela guarda `if(pc!=" ")` (m10 = 6, correto). C# exige constante em valor default, entao
  expressao relacional em assinatura e teoricamente possivel e praticamente inexistente.
- A ancora fixa a forma `private async Task <Nome>(`. Trocar tipo de retorno (`Task<T>`) ou
  visibilidade faz `grep -c` dar != 1 e o gate REPROVA — falha ruidosa e deliberada: mudanca de
  assinatura desses 2 metodos obriga revisitar este item do DoD em vez de passar silenciosamente.

D-2026-07-30-sonar-zero-issues-9: O `Verify:` do item 9 do Definition of Done desta fase
(CONTEXT.md - `TranslationManager.cs`, S107 + S3267) fica SUPERSEDED pelo comando registrado nesta
decisao e copiado byte-a-byte para o CONTEXT.md. Ela NAO reescreve `D-2026-07-30-sonar-zero-issues-5`
nem `-8` (append-only): o QUE deve ser feito segue identico - os 2 helpers privados
`TranslateChaptersWithCacheAsync` e `TranslateSingleChapterAsync` declaram no maximo 7 parametros
(S107) e o loop de `chapters` usa `.Select(chapter => chapter.HRef)` (S3267) - e as 3 propriedades
que D-...-8 instalou (ancora na DECLARACAO exigida 1x, contagem de PARAMETROS e nao de virgulas de
janela, limiar `-le 7` na unidade certa) continuam valendo integralmente. Muda UMA coisa: como o
passe AWK trata COMENTARIO dentro da assinatura. Nenhuma linha de producao muda por causa desta
decisao - as 2 assinaturas de hoje nao tem comentario algum; o que estava quebrado era a MEDIDA.

Furo (W-1 da REVIEW iter 2, evasao EXECUTADA pelo reviewer e reproduzida de forma independente pelo
DoD critic): o scanner char-a-char de D-...-8 le o texto cru da linha, entao um `)` dentro de um
comentario na lista de parametros derruba a profundidade de parenteses a 0 e ENCERRA a varredura
cedo. Medido nesta rodada: `IReadOnlyList<Chapter> chapters, // ver nota 2)` com 3 parametros extras
(8 reais na DECLARACAO - exatamente a violacao S107 que o item existe para impedir) reporta N=3 e sai
**exit 0**, falso PASS. Vale para os DOIS metodos (w1a, w1f) e para comentario de bloco de uma
linha (`/* ver nota 2) fim */`) e multi-linha. Mesma familia ja fechada na phase `the-method-refactor`
(D-2026-07-30-the-method-refactor-8): gate textual que conta estrutura tem de descartar comentario
ANTES de contar.

Correcao locked (nenhuma clausula removida ou afrouxada; das 3 clausulas do comando, 2 ficam
BYTE-IDENTICAS - a ancora `grep -cE ... -eq 1` e `grep -q "chapters.Select(chapter => chapter.HRef)"`
- e so o programa AWK muda): antes de contar, cada linha passa por um descarte de comentario de linha
(`//`) e de bloco (`/* */`, com estado entre linhas), a MESMA tecnica do item 4 daquela phase. Duas
diferencas deliberadas em relacao ao passe de la, ambas medidas: (1) a deteccao da ancora e a
varredura rodam sobre a linha JA limpa, entao uma declaracao comentada nao ancora nada e o gate
reprova por ausencia de medida (`test -n "$N"`); (2) o corte no `//` so acontece com PARIDADE PAR de
aspas a esquerda - sem essa guarda, um default `string url = "https://x"` teria a virgula do
parametro descartada junto com a falsa "abertura de comentario", subcontando 1 separador (fail-OPEN
novo). Com a guarda, o mutante `res_url_in_sig` mede 6 no comando novo e no velho, identicos.

Containment formal (provado por medida, nao alegado): em TODA entrada cujas duas assinaturas nao
contenham comentario, o descarte e no-op sobre a regiao varrida e os dois comandos computam o MESMO
N - verificado nos 14 mutantes sem comentario na assinatura (m0, m1, m2, m8, m9, ok_generic,
ok_attribute, ok_default, ok_oneline, ok2_xmldoc_above, ok2_block_above, ok2_block_ml_above,
res_url_in_sig, e o repo real), N identico em 14/14. Ou seja: nesse dominio inteiro - que inclui o
codigo de hoje e todo mutante que o comando anterior ja pegava - as duas versoes sao a MESMA funcao,
logo nenhuma protecao foi perdida. Fora dele (comentario DENTRO da assinatura) o comando anterior
erra nas duas direcoes e o novo acerta: subconta com `)` no comentario (w1a/w1b/w1c/w1d/w1f: OLD le
3, real 8/8/8/5/8) e superconta com virgula no comentario (div: OLD le 8, real 7). O UNICO caso em
que o novo passa e o anterior reprova (`div_comma_in_comment_7real`) e uma REPROVACAO FALSA do
anterior, residuo que D-...-8 ja declarava por escrito ("idem para virgula dentro de comentario `//`
escrito no meio da assinatura ... direcao SEGURA (superestima): so pode causar reprovacao falsa"):
sao 7 parametros DECLARADOS, e `exit 0` e o veredito CORRETO para o criterio. Remover um erro de
medida nao afrouxa o criterio.

Comando novo (byte-a-byte igual ao que vai para o CONTEXT.md):
`F=src/TranslateReader.Core/Business/Managers/TranslationManager.cs; for M in TranslateChaptersWithCacheAsync TranslateSingleChapterAsync; do test $(grep -cE "^[[:space:]]*private async Task $M\(" "$F") -eq 1 || exit 1; N=$(awk -v m="private async Task $M(" '{l=$0; if(g){i=index(l,"*/"); if(i){l=substr(l,i+2); g=0} else next} o=0; while((z=index(substr(l,o+1),"//"))>0){z+=o; pre=substr(l,1,z-1); if(gsub(/"/,"&",pre)%2==0){l=substr(l,1,z-1); break} o=z+1} while(i=index(l,"/*")){r=substr(l,i+2); j=index(r,"*/"); if(j){l=substr(l,1,i-1) substr(r,j+2)} else {l=substr(l,1,i-1); g=1; break}} if(index(l,m))f=1; if(f){for(i=1;i<=length(l);i++){h=substr(l,i,1); if(h=="("){p++} else if(h==")"){p--; if(p==0){print (s?k+1:0); exit}} else if(h=="<"){if(pc!=" ")a++} else if(h==">"){if(a>0)a--} else if(h=="["){b++} else if(h=="]"){if(b>0)b--} else if(h=="{"){c++} else if(h=="}"){if(c>0)c--} else if(h==","){if(p==1&&a==0&&b==0&&c==0)k++} else if(p>=1&&h!=" "&&h!="\t"){s=1} pc=h}}}' "$F"); test -n "$N" && test "$N" -le 7 || exit 1; done && grep -q "chapters.Select(chapter => chapter.HRef)" "$F"`

Prova por mutacao (20 mutantes; copias em scratchpad, repo real NUNCA mutado - `git status
--porcelain` vazio ao final; `Nc/Ns` = parametros medidos em
`TranslateChaptersWithCacheAsync`/`TranslateSingleChapterAsync`):

| Var | Mutante | NEW | OLD | Nc/Ns NEW | Nc/Ns OLD |
|---|---|---|---|---|---|
| w1a | **evasao do reviewer**: 8 params na DECL + `// ver nota 2)` na assinatura | **`exit 1`** | `exit 0` | 8/5 | 3/5 |
| w1f | a mesma evasao no OUTRO metodo (`...SingleChapter`) | **`exit 1`** | `exit 0` | 5/8 | 5/3 |
| w1b | 8 params + comentario de BLOCO `/* ver nota 2) fim */` na assinatura | **`exit 1`** | `exit 0` | 8/5 | 3/5 |
| w1c | 8 params + bloco `/* ... ) ... */` quebrado em 2 linhas | **`exit 1`** | `exit 0` | 8/5 | 3/5 |
| w1d | so o comentario `// ver nota 2)`, 5 params reais | `exit 0` | `exit 0` | 5/5 | 3/5 |
| w1g | `(` desbalanceado no comentario + 8 params | `exit 1` | `exit 1` | 5/8 | none |
| m0 | copia intacta do arquivo entregue | `exit 0` | `exit 0` | 5/5 | 5/5 |
| m1 | 8 params na DECL de `...ChaptersWithCache` | `exit 1` | `exit 1` | 8/5 | 8/5 |
| m2 | 8 params na DECL de `...SingleChapter` | `exit 1` | `exit 1` | 5/8 | 5/8 |
| m8 | clausula S3267 revertida (`chapters.Select(c => c.HRef)`) | `exit 1` | `exit 1` | 5/5 | 5/5 |
| m9 | declaracao renomeada (ancora ausente) | `exit 1` | `exit 1` | none/5 | none/5 |
| ok | 6o param generico `IReadOnlyDictionary<string, int>` | `exit 0` | `exit 0` | 6/5 | 6/5 |
| ok | 6o param com atributo `[CallerMemberName] string caller = ""` | `exit 0` | `exit 0` | 6/5 | 6/5 |
| ok | 6o param com default `string extra = ""` | `exit 0` | `exit 0` | 6/5 | 6/5 |
| ok | assinatura inteira colapsada em UMA linha | `exit 0` | `exit 0` | 5/5 | 5/5 |
| ok2 | `/// <summary>` imediatamente acima da declaracao | `exit 0` | `exit 0` | 5/5 | 5/5 |
| ok2 | `/* helper */` de uma linha acima da declaracao | `exit 0` | `exit 0` | 5/5 | 5/5 |
| ok2 | bloco `/*` ... `)` ... `*/` multi-linha acima da declaracao | `exit 0` | `exit 0` | 5/5 | 5/5 |
| res | 6o param com default `string url = "https://x"` (guarda de aspas) | `exit 0` | `exit 0` | 6/5 | 6/5 |
| div | 7 params reais + `// nota, com virgula` na assinatura | `exit 0` | **`exit 1`** | 7/5 | 8/5 |

w1a/w1f/w1b/w1c fecham o furo (OLD dava falso PASS nos quatro). m1/m2/m8/m9 provam zero regressao de
gate. As 8 formas legitimas (ok/ok2/res + repo real) seguem `exit 0` - zero falso positivo novo,
inclusive nas tres que um descarte de comentario mal feito quebraria (doc `///`, bloco acima da
declaracao, `//` dentro de literal de string).

Residuos DECLARADOS (nenhum silenciado; nenhum ocorre nas 2 assinaturas de hoje):
- Verbatim/raw string com aspas escapadas na assinatura pode inverter a paridade da guarda e impedir
  o corte no `//` - a linha volta a ser tratada como o comando ANTERIOR a tratava, nunca pior que o
  estado locked por D-...-8. C# exige constante em valor default, entao literal exotico em assinatura
  e teoricamente possivel e praticamente inexistente.
- `/*` dentro de literal de string na assinatura nao tem guarda de aspas (so o `//` tem): abriria
  estado de bloco e engoliria linhas ate um `*/`, fazendo a ancora sumir e `test -n "$N"` reprovar.
  Direcao FAIL-CLOSED (reprovacao ruidosa), nunca aprovacao falsa.
- Os residuos de D-...-8 que nao sao de comentario continuam valendo como la escrito (virgula dentro
  de literal de string num default; `<` colado a identificador num default; ancora fixa na forma
  `private async Task <Nome>(`, cuja ausencia REPROVA de proposito).
- Evasao que exige parser C# de verdade (`#if`, texto da assinatura dentro de string) continua fora
  de alcance de qualquer gate textual - jurisprudencia locked em D-2026-07-30-the-method-refactor-9.
  Backstop semantico: o S107 do proprio SonarCloud (analisador Roslyn) rodando com
  `sonar.qualitygate.wait=true` em New Code, mais o PR review humano.

D-2026-07-30-sonar-zero-issues-10: o mecanismo anti-recorrencia locked por
`D-2026-07-30-sonar-zero-issues-2` (`sonar.qualitygate.wait=true` no `dotnet-sonarscanner end`)
ganha um guard contra desaparecimento SILENCIOSO, e o `Verify:` do item 10 do Definition of Done
passa a prova-lo. Esta decisao NAO reescreve D-...-2 nem qualquer decisao anterior (append-only): o
QUE deve existir segue identico - o `end` roda com `sonar.qualitygate.wait=true` e o job `sonarqube`
e chamado por `pipeline.yml`. Acrescenta-se UMA garantia: o job nao pode ficar verde sem ter
escaneado nada.

Furo (W-3(d) da REVIEW iter 2): os 7 steps uteis de `.github/workflows/sonarqube.yml` sao
condicionados a `if: env.SONAR_TOKEN != ''`. Sem o secret, TODOS sao pulados, o job termina com
sucesso e o Quality Gate nunca roda - o mecanismo inteiro vira no-op sem nenhum sinal. Como
`sonarqube` e required check da branch protection, um check verde sem scan e pior que a ausencia do
check: da garantia falsa. Um mecanismo anti-recorrencia que desaparece em silencio nao e mecanismo.

Correcao locked: um step novo `Assert the scan is not silently skipped`, gated em
`if: env.SONAR_TOKEN == ''` (portanto so roda no cenario que interessa) e posicionado logo apos o
`harden-runner`, antes do checkout. Ele resolve `TOKEN_EXPECTED` a partir do contexto e:
- `TOKEN_EXPECTED == true` -> `::error` + `exit 1` (o job FALHA);
- caso contrario -> `::warning` explicito de que scan e Quality Gate foram pulados (deixa de ser
  silencioso mesmo onde falhar seria errado).

`TOKEN_EXPECTED` = `github.repository == 'slipalison/TranslateReader'` E
`github.actor != 'dependabot[bot]'` E NAO (`github.event_name == 'pull_request'` E
`github.event.pull_request.head.repo.fork`). Tabela de contexto (comportamento COM o token ausente;
com o token presente o step nem roda):

| Contexto | TOKEN_EXPECTED | Efeito |
|---|---|---|
| `push` em `main` do repo de origem | true | **falha** |
| PR de branch do proprio repo de origem | true | **falha** |
| `workflow_dispatch` no repo de origem | true | **falha** |
| PR vindo de FORK | false | warning (GitHub nao expoe secrets a PR de fork - ausencia legitima) |
| PR do Dependabot | false | warning (Dependabot usa o cofre proprio de secrets, `.github/dependabot.yml` tem 2 ecossistemas semanais) |
| fork/clone do repo rodando o proprio CI | false | warning (`github.repository` diferente) |

Por que NAO falhar em fork/Dependabot: nesses contextos a ausencia do secret e uma decisao de
seguranca do proprio GitHub, nao um defeito do repo - nao ha o que consertar, e falhar transformaria
todo PR externo e todo bump semanal do Dependabot em check vermelho permanente. O sinal ali e o
`::warning`, que ja mata o "silencio". Detalhe de semantica de expressao verificado: em `push`,
`github.event.pull_request` e nulo, mas `&&` do GitHub curto-circuita no primeiro operando falso
(`github.event_name == 'pull_request'`), entao nao ha desreferencia nula.

Containment formal (mais forte que "provado clausula a clausula"): o comando anterior do item 10 e
PREFIXO LITERAL do novo, seguido de ` && `. Logo `NEW exit 0` implica `OLD exit 0` por construcao,
e nenhuma protecao antiga pode ter sido perdida. As clausulas acrescentadas medem, em pares
presenca-positiva/ausencia-negativa (licao do `[PROCESSO/DoD]` de `regression-suite` em
`todos.md`): EXATAMENTE 1 step gated em `env.SONAR_TOKEN == ''`; que o corpo desse step contenha
`exit 1`; e que a expressao carregue as tres partes do escopo (`github.repository ==
'slipalison/TranslateReader'`, `head.repo.fork`, `dependabot[bot]`) - ou seja, um "fix" que sempre
falhe, quebrando fork e Dependabot, tambem reprova.

Comando novo (byte-a-byte igual ao que vai para o CONTEXT.md):
`grep -A3 "dotnet-sonarscanner end" .github/workflows/sonarqube.yml | grep -q "sonar.qualitygate.wait=true" && W=.github/workflows/sonarqube.yml && test $(grep -c "if: env.SONAR_TOKEN == ''" "$W") -eq 1 && G=$(awk "/if: env\.SONAR_TOKEN == ''/{f=1;next} f&&/^      - name:/{exit} f" "$W") && printf '%s' "$G" | grep -qE "^ +exit 1$" && printf '%s' "$G" | grep -q "github.repository == 'slipalison/TranslateReader'" && printf '%s' "$G" | grep -q "head.repo.fork" && printf '%s' "$G" | grep -q "dependabot\[bot\]"`

Prova por mutacao (9 mutantes do `sonarqube.yml`, copias em scratchpad, repo real nunca mutado):

| Mutante | NEW | OLD |
|---|---|---|
| intacto (arquivo entregue) | `exit 0` | `exit 0` |
| step de guard DELETADO | **`exit 1`** | `exit 0` |
| `exit 1` do guard trocado por `echo` (guard vira aviso) | **`exit 1`** | `exit 0` |
| `if:` do guard invertido para `!= ''` (nunca roda quando importa) | **`exit 1`** | `exit 0` |
| `TOKEN_EXPECTED` hardcoded `'false'` (guard nunca falha) | **`exit 1`** | `exit 0` |
| carve-out de FORK removido (quebraria PR externo) | **`exit 1`** | `exit 0` |
| carve-out de DEPENDABOT removido (quebraria bump semanal) | **`exit 1`** | `exit 0` |
| segundo step `== ''` duplicado (guard ambiguo) | **`exit 1`** | `exit 0` |
| `sonar.qualitygate.wait=true` removido do `end` | `exit 1` | `exit 1` |

O ultimo mutante e a prova de nao-regressao: a protecao original de D-...-2 continua presa
identica. Os outros 7 sao furos que o comando anterior nao via.

Escopo: muda `.github/workflows/sonarqube.yml` (CI), zero linha de `src/` e zero teste. NAO fecha os
itens (a), (b) e (c) da W-3 - "Sonar way" so mede New Code (issue nova em linha legada nao alterada e
smell abaixo do debt ratio seguem invisiveis) e o C# do app MAUI segue fora do scan por
D-2026-07-30-sonar-zero-issues-6. Os tres sao limites do produto/pipeline, nao deste yml: (a) e (b)
exigiriam trocar o Quality Gate na config do SonarCloud, que vive FORA do repo e nao e versionavel
aqui; (c) exigiria job novo em `windows-latest` com workload MAUI. Registrados em `.jdi/todos.md`.

D-2026-07-30-sonar-zero-issues-11 (defeito de execucao achado pelo CI do PR #12 — supersede o
`Verify:` do item 10 do DoD, D-...-2/-10 permanecem intocadas no QUE decidem): a flag
`sonar.qualitygate.wait=true` foi entregue na fase `end` do `dotnet-sonarscanner` e o CI provou que
ali ela nao funciona. Log do job (run 30597997934, job 91054481106):
`This setting is not valid in the "end" phase in this version of the C# plugin:
sonar.qualitygate.wait` seguido de `Post-processing failed. Exit code: 1`. Ou seja: o mecanismo
anti-recorrencia nao esperava Quality Gate nenhum — o job falhava por parametro invalido, ANTES de
consultar o gate, e o PR ficava vermelho pelo motivo errado. Correcao: a flag passa para o array de
argumentos do `dotnet-sonarscanner begin` (posicao onde o SonarScanner for .NET 11.2.1 a aceita) e
sai do `end`.
Consequencia para o gate: o `Verify:` anterior fazia `grep -A3 "dotnet-sonarscanner end" | grep -q
"sonar.qualitygate.wait=true"` — provava PRESENCA da string, nunca VALIDADE da posicao, e por isso
passou verde localmente enquanto o mecanismo estava quebrado no runner. Mesma familia de defeito ja
catalogada em `.jdi/todos.md` (`[PROCESSO/DoD]`) e nas duas reprovacoes do DoD critic desta phase: o
comando media um proxy conveniente em vez da propriedade. O `Verify:` novo exige a flag DENTRO do
bloco `args=(...)` do `begin` E a ausencia dela na linha do `end` (a posicao invalida), mantendo
integralmente as clausulas de guarda de token de D-...-10.
Achado colateral do mesmo log, corrigido no mesmo esforco: `HtmlUtility.cs(104,5) warning S125
"Remove this commented out code"` — smell NOVO, introduzido pelo proprio refactor de `InjectTags`
desta phase (T-4). O comentario explicava o PORQUE do ponto de insercao do CSS (permitido por
`.claude/rules/csharp.md` §7), mas continha `</head>` e identificadores, e a heuristica do S125 leu
aquilo como codigo comentado. Reescrito como prosa, preservando a justificativa. Registro do limite
que isso expoe: os analisadores do SonarCloud NAO rodam no `dotnet build` local, entao nenhum gate
local desta phase poderia ter pego esse smell — so o CI pega, e so depois do push.

D-2026-07-30-sonar-zero-issues-12 (fechamento do laco: 3 issues que a analise do PR #12 mostrou e
que os gates locais nao podiam ver): com o `sonar.qualitygate.wait` finalmente valido (D-...-11), a
primeira analise que de fato completou expos 3 issues abertas — nenhuma delas visivel em
`dotnet build`/`dotnet test`, porque os analisadores do SonarCloud so rodam no scanner.
(1) `test/TranslateReader.Tests/ParsingEngineTests.cs:246,278` — `external_roslyn:CA1826` INFO,
"Do not use Enumerable methods on indexable collections". Sao issues NOVAS, introduzidas pelos
testes que esta propria phase escreveu no T-6 (`.First()` sobre o `IReadOnlyList<Chapter>` de
`ExtractChaptersAsync`). Registro honesto: a phase zerou 113 issues e introduziu 2 no caminho.
Corrigidas com indexador (`[0]`) — mudanca de 2 caracteres por linha, sem alterar assercao.
(2) `src/TranslateReader.Core/Utilities/HtmlUtility.cs:148` — `external_roslyn:SYSLIB1044`, o waiver
declarado em D-...-3 mecanismo (c). O `#pragma warning disable/restore` esta na posicao correta
(147/150, envolvendo o atributo em 148) e FUNCIONA no compilador: o build local e o do CI reportam 0
SYSLIB no Core. Mas o importador `external_roslyn` do SonarCloud le o diagnostico do log do MSBuild
e IGNORA o estado de supressao, entao a issue continuava aberta la. Conclusao operacional: para
regra importada de analisador externo, `#pragma` nao e mecanismo de waiver valido no Sonar — so
`sonar.issue.ignore.multicriteria` e. Esta issue migra do mecanismo (c) para o (b) da taxonomia
D-...-3, com entrada `e3` (ruleKey `external_roslyn:SYSLIB1044`, resourceKey `**/HtmlUtility.cs`).
O pragma permanece no codigo por higiene de build (suprime o aviso do compilador) e porque documenta
o porque no ponto exato; a exclusao cuida do lado do Sonar.
Licao registrada tambem em `.jdi/todos.md`: um "waiver" so vale se for provado no sistema que
levanta a issue — provar no compilador e provar a coisa errada.

D-2026-07-30-sonar-zero-issues-13 (CORRECAO de D-...-12, medida no CI do PR #12 — a correcao
anterior estava errada no remedio, certa no diagnostico): D-...-12 afirmou que mover o waiver de
`SYSLIB1044` do `#pragma` (mecanismo c) para `sonar.issue.ignore.multicriteria` (mecanismo b)
fecharia a issue no SonarCloud. **Nao fecha.** Medicao do run 30598994128:
- o argumento `e3` CHEGOU ao scanner (o log ecoa
  `/d:sonar.issue.ignore.multicriteria.e3.ruleKey="external_roslyn:SYSLIB1044"` e
  `multicriteria=e1,e2,e3`);
- as duas exclusoes de regra NATIVA do mesmo bloco funcionaram: consulta a API por
  `rules=Web:S7926,css:S4667` no PR retorna **0** issues abertas;
- a issue `external_roslyn:SYSLIB1044` em `HtmlUtility.cs:148` seguiu **aberta**.
Conclusao medida: `sonar.issue.ignore.multicriteria` filtra issue levantada pelos analisadores do
proprio Sonar, e NAO filtra issue importada de analisador externo (`external_roslyn:*`). Somado ao
que D-...-12 ja provou sobre o `#pragma` (funciona no compilador — o build local e o do CI emitem
zero SYSLIB1044 —, mas o importador ignora o estado de supressao), o resultado e que **nenhum dos
dois mecanismos que esta phase tem no repo remove uma issue de analisador externo do SonarCloud**.
A entrada `e3` foi REMOVIDA do workflow: config que nao faz o que promete e a mesma classe de
defeito da regra Semgrep `translatereader-zip-slip` ja catalogada em `.jdi/todos.md` — da falsa
sensacao de cobertura para quem ler o arquivo depois.
Sobram exatamente dois caminhos, ambos decisao do humano e nenhum deles executavel por esta phase:
(1) marcar a issue como *Accepted* na UI do SonarCloud — caminho canonico do produto, mantem o
registro visivel e auditado, e acao fora do repositorio;
(2) `<NoWarn>SYSLIB1044</NoWarn>` no csproj do Core — funcionaria (o diagnostico deixa de existir
para o importador), mas e supressao no PROJETO INTEIRO, exatamente a "vassoura" que o gate (b) desta
phase reprova: esconderia um SYSLIB1044 legitimo em qualquer outro regex futuro do Core.
Estado final honesto da phase: 112 das 113 issues fora da analise; a ultima e um waiver INFO
documentado, com Quality Gate OK (INFO nao move rating nenhum), aguardando a decisao acima.

D-2026-07-31-coverage-90-0 (registro de phase): phase `coverage-90` registrada na posicao 15 do
ROADMAP. Origem: card despachado pelo usuario via `/jdi-issue` em 2026-07-31 — "adicione os tests
faltante ate chegar em 90% de cobertura no Sonarqube e sem issues nova" (texto colado, sem URL de
tracker). Baseline medido na API do SonarCloud no momento do registro (`branch=main`, apos o merge
do PR #12 / `1af3a51`): coverage 75,9%, line_coverage 77,0%, lines_to_cover 1428, uncovered_lines
329, ncloc 3103, 1 issue aberta (o waiver INFO `SYSLIB1044` de D-2026-07-30-sonar-zero-issues-13).
Distribuicao das 329 linhas descobertas: 195 em JavaScript do WebView (`paginated.js` 70,
`bridge.js` 60, `translation.js` 38, `scroll.js` 27 — todos 0%, sem harness JS no repo) e 134 em C#
(`TranslationEngine` 52, `ParsingEngine` 45, `ModelAccess` 25, `FileUtility` 3, `HtmlUtility` 2,
`ThemeEngine` 1, models 6).
D-2026-07-31-coverage-90-1 (rota: harness JS real, nao exclusao de denominador): entre as 3 rotas
do brief, fica LOCKED a rota (A) — harness JS real via `node:test`+`node:vm` nativo do Node 24
(zero dependencia nova) — e NAO a rota (B) `sonar.coverage.exclusions`. Motivo: (B) so entrega o
NUMERO — o denominador cai de 1428 para 1233 e o numero de HOJE (1099 linhas cobertas) ja vira
89,1%, faltando so +11 linhas de C#; mas o JS continua 0% testado, contradizendo o pedido literal
do card ("adicione os tests faltante") e o proprio brief exige justificativa forte + Deferred to
PR review pra essa rota. (A) e viavel sem infra nova porque inspecao direta dos 4 arquivos
(`paginated.js`, `bridge.js`, `translation.js`, `scroll.js`) confirmou que sao 100% atribuicoes
flat `window.X = function(){}` sobre leitura/escrita de propriedade DOM simples
(`getElementById`, `querySelectorAll`, `.dataset`, `.offsetWidth/offsetLeft/scrollWidth`,
`getBoundingClientRect`, `window.addEventListener('resize', ...)`) — nenhum uso de framework ou
modulo, entao um sandbox `vm.createContext` com `window`/`document` stub minimos basta pra
exercitar toda funcao exportada, sem jsdom. Alvo local: cobertura agregada dos 4 arquivos >= 85%
via lcov (>= ~166 das 195 linhas descobertas) — abaixo de 100% de proposito, pra nao forcar cobrir
ramos puramente defensivos (ex.: a cadeia de retry `setTimeout` de `_sendReady` em `bridge.js`,
4 branches de deteccao de host).

D-2026-07-31-coverage-90-2 (layout do harness + wiring de CI): testes JS vivem em
`test/js/<nome>.test.js` (1 arquivo por script de producao: `paginated`, `bridge`, `translation`,
`scroll`), fora de `TranslateReader.Tests.csproj` (nao e .NET) — usam `node:test` +
`node:assert/strict` + `node:vm`. Comando de cobertura local: `node --test
--experimental-test-coverage --test-reporter=lcov
--test-reporter-destination=TestResults/js-lcov.info test/js/` (flags confirmadas via doc oficial
do Node, disponiveis desde 20.11 — pesquisa web desta sessao). Risco tecnico registrado: a
cobertura V8 (`--experimental-test-coverage`) so atribui linhas corretamente se o `vm.Script`
carregar o codigo com `filename` apontando pro caminho REAL do arquivo de producao
(`fs.readFileSync` + `new vm.Script(code, {filename: path})`) — copiar/colar o codigo como string
literal quebra a atribuicao de cobertura silenciosamente; isso e responsabilidade do doer, nao do
DoD (nao ha `Verify:` local pra essa propriedade de implementacao). `.github/workflows/
sonarqube.yml` ganha: `actions/setup-node` (pinada por SHA, D-2026-07-28-ci-seguranca-4) ANTES do
`dotnet-sonarscanner begin`; o comando de teste JS acima, gated `if: env.SONAR_TOKEN != ''` —
MESMO padrao dos steps ja existentes no arquivo, reusando o guard de
D-2026-07-30-sonar-zero-issues-10 (falha alto no repo de origem se o secret sumir, o que agora
tambem cobre "os testes JS nao rodaram", sem gap novo: hoje ZERO teste JS existe em qualquer
contexto, entao esta fase e estritamente uma melhoria em todo cenario, inclusive fork/Dependabot);
`sonar.javascript.lcov.reportPaths` entra no MESMO bloco `args=(...)` do `begin` que ja tem
`sonar.cs.opencover.reportsPaths`, apontando pro `TestResults/js-lcov.info`.

D-2026-07-31-coverage-90-3 (ModelAccess + excecao de I/O real): `ModelAccess.cs` (25 linhas
descobertas, 39% hoje) e alvo direto — `DownloadModelAsync` (o metodo maior e o unico sem teste
hoje, confirmado em `ModelAccessTests.cs`) ganha teste com `HttpMessageHandler` fake injetado via
`HttpClient` (ja e parametro de construtor — zero mudanca de seam), SEM rede real. O teste ESCREVE
em diretorio temp real (`Path.GetTempPath()+Guid`, mesma convencao ja usada no proprio
`ModelAccessTests.cs` e em `FileUtilityTests.cs`), porque o comportamento sob teste (buffer,
progress, swap atomico `tmp`->final via `File.Move`) SO existe como efeito em disco. Alvo local:
cobertura de `ModelAccess.cs` >= 90% (de 39% hoje). Esta e a UNICA excecao NOVA a
`.claude/rules/csharp.md` §6 ("no disk... in unit tests") nesta fase, ao lado da que
`ParsingEngineTests.cs` ja usa (fixture `.epub` real, autorizada nomeadamente no PLAN de
`sonar-zero-issues`, T-6) — SE a contingencia de `ParsingEngine` (D-...-5) for acionada, segue o
MESMO padrao de fixture real, nao um terceiro padrao. Nenhuma outra classe ganha excecao; rede
real e SQLite real continuam banidos sem excecao em qualquer teste novo desta fase.

D-2026-07-31-coverage-90-4 (TranslationEngine mantido deferido): as 52 linhas de
`TranslationEngine.cs` (o maior gap de C#, caminho de `LLamaWeights`/`StatelessExecutor` sem
interface-seam) NAO sao tocadas — esta fase NAO reverte `D-2026-07-30-regression-suite-5(2)` nem
`D-2026-07-30-the-method-refactor-6` (abrir o seam pertence a `llm-mobile`). Consequencia numerica
explicita: isso so e seguro porque D-...-1 + D-...-3 ja fecham a meta sem precisar dessas 52
linhas (ver aritmetica em D-...-5 abaixo). Se a execucao ficar abaixo do plano, a reserva de
contingencia e `ParsingEngine` (45 linhas, ja com padrao de fixture estabelecido), nunca
`TranslationEngine`.

D-2026-07-31-coverage-90-5 (aritmetica-alvo, amarra a fase): baseline (D-...-0) lines_to_cover=
1428, covered=1099 (77,0%). Meta >=90% de 1428 => covered >= 1286 (ceil de 0,9*1428) => precisa de
>= 187 linhas NOVAS cobertas. Plano: JS >=85% de 195 => >=166 (D-...-1); ModelAccess 39%->90% de
~25 linhas descobertas => +-20 (D-...-3); `FileUtility.cs`(3) + `HtmlUtility.cs`(2) fechados a
100%, sem infra nova => +5. Soma = 166+20+5 = 191 >= 187, margem de 4 linhas. `TranslationEngine`
(52, D-...-4) e `ParsingEngine`(45) ficam fora do plano principal por design — `ParsingEngine` e a
reserva nomeada se o numero real (so mensuravel apos implementar; ferramentas de cobertura de JS
e C# sao distintas e nao produzem um numero unico local) ficar abaixo de 187. O numero AGREGADO
real do SonarCloud (que pode divergir do proxy local — o analisador JS do Sonar conta linha
executavel de um jeito, V8/node de outro, ambos sao proxies, nao a mesma medida) so existe apos
push+CI — `## Deferred to PR review`; os itens Auto do DoD provam os PISOS locais por
arquivo/agregado, nao o numero remoto.

D-2026-07-31-coverage-90-6 ("sem issues nova" — sem gate local possivel, mesmo limite ja medido em
`sonar-zero-issues`): a segunda condicao do card vale para as issues que os testes NOVOS desta
fase introduzem — precedente direto e medido: a fase anterior zerou 113 e introduziu 2 (`CA1826`)
nos proprios testes novos, so visiveis apos push (D-2026-07-30-sonar-zero-issues-12). Nenhum
analisador do SonarCloud (`external_roslyn`, `javascript`, `csharpsquid`) roda em `dotnet build`/
`node --test` local — um `Verify:` que fingisse provar "zero issue nova" localmente repetiria o
exato erro de proxy ja catalogado varias vezes em `.jdi/todos.md` `[PROCESSO/DoD]`. O DoD desta
fase portanto NAO contem item alegando provar isso; a confirmacao real vai para
`## Deferred to PR review`, mesmo mecanismo de `D-2026-07-30-sonar-zero-issues-6`. Mitigacao de
escrita (nao gated): novo teste C# usa indexador em vez de `.First()`/`.Last()` sobre
`IReadOnlyList<T>` (padrao CA1826 ja corrigido em `sonar-zero-issues`); novo teste JS usa
`const`/`let`/`===`, nunca `var`/`==`.

D-2026-07-31-coverage-90-7 (Quality Gate mede so New Code — cautela sobre `Verify:`):
`sonar.qualitygate.wait=true` (D-2026-07-30-sonar-zero-issues-2/10/11) mede so New Code
(`new_coverage>=80` hoje). O diff desta fase e majoritariamente arquivo de teste NOVO + poucas
linhas alteradas em producao (`ModelAccess.cs`, `FileUtility.cs`, `HtmlUtility.cs`,
`sonarqube.yml`) — Quality Gate verde e sinal FRACO pra "chegamos a 90% Overall": New Code
coverage e Overall coverage sao metricas diferentes, e uma fase que so ADICIONA teste pode
satisfazer a primeira sem mover a segunda o bastante. Nenhum `Verify:` do DoD desta fase
referencia `sonar.qualitygate.wait` ou status de CI como prova da meta de 90% — so os pisos
locais por arquivo/agregado (D-...-5) e a confirmacao remota fica em
`## Deferred to PR review`.

D-2026-07-31-coverage-90-8 (os `Verify:` do DoD passam a medir a execucao ATUAL — supersede os
comandos dos itens 1, 2, 3, 4 e 5 de `.jdi/phases/coverage-90/CONTEXT.md`; os CRITERIOS e os PISOS
ficam identicos): o DoD critic da iter 1 derrubou tres linhas como OCAS e declarou residuo em
outras duas, todas da mesma familia — **o gate lia um artefato de medicao que ja estava em disco em
vez de exigir que a medicao DESTA execucao tivesse sucesso**. Dois defeitos mecanicos, ambos
reproduzidos por medicao propria nesta iter 2:
(i) **`;` descarta o exit code do runner.** Os itens 2, 3 e 4 usavam
`<runner> >/dev/null 2>&1; <leitor do relatorio>` — se `node --test` ou `dotnet test` falhasse, o
`awk`/`grep` seguinte lia o artefato antigo e o gate saia 0. Contra-exemplo EXECUTADO (item 2): com
o `node` removido do `PATH` (regressao plausivel: o step `actions/setup-node` do `sonarqube.yml`
nasceu nesta propria fase, T-8) e um `TestResults/js-lcov.info` valido de 5399 bytes em disco, o
comando ANTIGO saiu **exit 0** sem executar 1 teste; o NOVO saiu 127. Contra-exemplo EXECUTADO
(itens 3 e 4): invertendo uma assercao viva (`FileUtilityTests.cs:81`,
`Assert.Equal(".epub", ...)` -> `".MUTANT"`), com a suite REPROVANDO, os comandos ANTIGOS sairam
**exit 0** e os NOVOS sairam 1. Nota de honestidade: o contra-exemplo LITERAL do critico para o
item 2 (`throw` num `.test.js`) NAO reproduz identico neste runtime — o reporter lcov do Node
trunca o destino para um stub de 4 bytes (`TN:`), entao o comando antigo falhava por ACIDENTE, nao
por design; o defeito estrutural continua real e esta provado pelos dois casos acima.
(ii) **selecao de relatorio arbitraria.** Os itens 3 e 4 faziam
`find TestResults -name "coverage.cobertura.xml" | sort | tail -1`. Os diretorios sao GUIDs do
VSTest, entao `sort` e LEXICOGRAFICO e nao tem relacao nenhuma com tempo. Medido neste repo com 4
relatorios em disco: o comando escolheu `9a248056-...` (mtime 07:49:59) enquanto o mais recente era
`3e886ce2-...` (mtime 07:53:32).

**Mecanismo adotado (deliberadamente determinista, nao heuristico):** artefato de destino LIMPO por
execucao + encadeamento com `&&` do runner ate a assercao. Itens 3 e 4 escrevem em
`--results-directory TestResults/dod3` e `TestResults/dod4`, apagados com `rm -rf` imediatamente
antes, e o gate exige `find ... | wc -l` **igual a 1** — com um diretorio limpo e um unico projeto
de teste existe exatamente 1 relatorio, entao a selecao deixa de ser heuristica e passa a ser
provada. Item 2 apaga `TestResults/js-lcov.info` com `rm -f` antes de rodar e exige `test -s` depois
— **mantendo o mesmo caminho que o CI usa** (`sonarqube.yml:107,137`), para que o item 5 continue
aferindo a mesma string que o item 2 produz. Selecao por mtime foi REJEITADA: `find -printf "%T@"`
nao existe em todo ambiente (BSD/macOS) e o diretorio limpo e determinista sem depender de
extensao GNU.

**Endurecimentos adicionais, todos com contra-exemplo executado e zero falso positivo no repo real:**
(a) item 2 conta arquivos de PRODUCAO DISTINTOS no lcov e exige os **4** (`seen[f]` + `n==4`), nao
so a razao agregada — assim um script que nunca foi carregado nao pode ser mascarado pelos outros
tres; medido: esvaziando so `test/js/scroll.test.js`, ANTIGO exit 0 / NOVO exit 1. (b) itens 3 e 4
passaram a comparar `line-rate` como NUMERO por classe (`$1+0<0.90` / `<0.99` em `awk`, reprovando
se QUALQUER classe do arquivo ficar abaixo, e exigindo `n>0` matches) — o comando antigo montava um
`R` MULTILINHA (`ModelAccess` tem 2 classes, `FileUtility` 3) e o `awk` acabava fazendo comparacao
de STRING, que so coincide com a numerica por sorte; verificado que o novo reprova de fato
apontando-o para `TranslationEngine.cs` (line-rate 0.21/0.4/0.2, deferido por D-...-4) -> exit 1, e
subindo o piso de `FileUtility`/`HtmlUtility` para 1.01 -> exit 1, e o de JS para 101% -> exit 1.
(c) item 1 (residuo "aceita suite VAZIA") exige `# pass > 1` e `# fail == 0` do reporter `tap`;
medido que com os 4 `.test.js` esvaziados o Node ainda reporta `# pass 1` (conta o proprio arquivo
como teste), entao um piso `> 0` seria vacuo e `> 1` e o menor piso que discrimina: ANTIGO exit 0 /
NOVO exit 1. (d) item 5 (residuo "grep prova presenca de string, nao correspondencia") passou a
EXTRAIR o caminho de `sonar.javascript.lcov.reportPaths=` e o de `--test-reporter-destination=` do
mesmo YAML e exigir `"$P" = "$D"`, alem de exigir o `actions/setup-node@` SHA-pinned com regex de 40
hex; medido: trocando o `reportPaths` para `TestResults/coverage/js.info`, ANTIGO exit 0 / NOVO exit
1. Fecha localmente a mesma classe de defeito que quebrou `sonar-zero-issues`
(`sonar.qualitygate.wait` presente e invalido, verde local, exit 1 no runner).

**O que NAO muda:** os criterios e os PISOS sao literalmente os mesmos (JS agregado >= 85%,
`ModelAccess.cs` >= 90%, `FileUtility.cs`/`HtmlUtility.cs` >= 99%, mesmo conjunto de strings do CI).
Nenhum piso foi afrouxado e nenhum teste foi deletado ou relaxado — esta decisao troca COMO se mede,
nunca O QUE se exige. Nenhuma linha de `src/` e tocada.

**O que fica deferido de proposito:** o ratchet NUMERICO de contagem de teste JS (ex.: `# pass >=
60`, a medida fechada desta fase) NAO entra aqui. O item `[PROCESSO/DoD]` de `.jdi/todos.md`
(`## De the-method-refactor`) ja decidiu que piso de contagem se ergue na VIRADA da phase, com o
numero ja publicado — apertar o proprio criterio no fim da corrida, sabendo que passa, e movimento
de trave, nao endurecimento. O piso `> 1` adotado em (c) nao e ratchet: ele so nega a suite vazia,
nao codifica a medida desta fase.

D-2026-07-31-coverage-90-9 (o item 5 do DoD pina TAMBEM o literal do caminho do lcov — supersede
o comando do item 5 de `.jdi/phases/coverage-90/CONTEXT.md` fixado por D-2026-07-31-coverage-90-8;
o CRITERIO e os demais requisitos ficam identicos): a REVIEW da iter 2 registrou como W-3 um
residuo do endurecimento anterior. O item 5 passou a exigir `"$P" = "$D"` (o caminho que o Sonar le
== o caminho que o reporter do node escreve), o que fecha a divergencia UNILATERAL, mas nao pina o
literal: uma mudanca COORDENADA dos dois lados do `sonarqube.yml` mantem a igualdade e o gate
continua verde, enquanto o item 2 segue medindo o caminho hardcoded `TestResults/js-lcov.info`.
Os itens 2 e 5 deixariam de aferir a mesma string sem que nenhum gate reclamasse.
Contra-exemplo EXECUTADO nesta iter 3: renomeando `TestResults/js-lcov.info` ->
`TestResults/coverage/js.info` nas DUAS ocorrencias do YAML (`sonarqube.yml:107` e `:137`), o
comando do item 5 fixado por D-...-8 saiu **exit 0**; com o literal pinado sai **exit 1**. Os dois
cenarios ja cobertos continuam cobertos: divergencia unilateral (so a linha 107 renomeada) = exit 1
nos dois comandos, e `setup-node` pinado por tag em vez de SHA = exit 1. No repo real, exit 0 sem
falso positivo.
**Mecanismo:** insere `&& test "$P" = "TestResults/js-lcov.info"` imediatamente apos
`&& test "$P" = "$D"`. E ADITIVO: todo teste do comando anterior (`test -n "$P"`, a igualdade
`P = D`, o regex de 40 hex do `setup-node@`, `--experimental-test-coverage`, `--test-reporter=lcov`)
permanece literalmente no lugar. **Nenhum piso foi afrouxado e nenhum criterio mudou** — o item 5
continua sendo "CI wiring para cobertura JS", so que agora prova tambem que o caminho aferido e o
MESMO que o item 2 mede localmente, amarrando os dois gates a uma unica string.
**Por que agora e nao na proxima phase:** a W-3 nomeava a proxima phase que tocasse o YAML como
lugar natural do conserto, mas o custo medido e de um `test` adicional, com contra-exemplo e zero
falso positivo em duas rodadas; adiar deixaria os itens 2 e 5 desacoplados no unico momento em que
o par foi verificado de ponta a ponta. Nenhuma linha de `src/` e tocada por esta decisao.

D-2026-08-01-div-paragraph-translation-0 (registro de phase): phase `div-paragraph-translation`
registrada no ROADMAP. Origem: BUG REPORT do usuario em 2026-08-01 — ele converteu
"Staff Engineer: Leadership beyond the management track" (Will Larson, 4,7 MB, EPUB 2.0 gerado por
calibre) e "nao traduziu". Base: `main` @ `ad607ac`.
Diagnostico medido nesta sessao (nao inferido — probe do pipeline real do Core contra o arquivo do
usuario + inspecao do banco do app em `%LOCALAPPDATA%\...\translatereader.db`):
- o pipeline de parsing esta INTEGRO para esse livro: 53 capitulos no ReadingOrder, 902.266 chars de
  HTML, zero capitulo vazio; `CreateTranslatedEpubAsync` substituiu 53/53 entradas num probe com
  sentinela; o modelo `gemma-2-2b-it-Q4_K_M.gguf` esta integro (1.708.582.752 bytes).
- CAUSA RAIZ: o livro tem **zero tags `<p>`** — e uma conversao calibre, e os paragrafos sao
  `<div class="calibreN">`. `HtmlUtility.ExtractTextBlocks` casa `<(p|h[1-6]|li)\b...>` e por isso
  enxerga apenas **360 blocos / 11.114 palavras**, contra **1.914 blocos / 88.042 palavras** que
  vivem em `<div>` folha e sao ignorados. Cobertura de traducao do livro: **11,2% do texto**.
- confirmacao aritmetica: `TranslationCache` para `BookId=12` tem **exatamente 360 entradas** — o
  motor traduziu tudo o que conseguia ver e nada alem. A corrida inteira levou 3min56s
  (14:19:31 -> 14:23:27), coerente com 360 blocos curtos.
- consequencia observavel: o EPUB gerado (`Books.Id=15`,
  `..._translated_544b0db6.epub`) tem **5 de 53 documentos em portugues, 48 ainda em ingles**.
- SEGUNDO DEFEITO (silencio): o fluxo tratou isso como sucesso — `TranslateBookAsync` chamou
  `RebuildAllTranslatedChaptersAsync`, apagou o job (`DeleteJobAsync`) e devolveu o caminho do EPUB
  sem nenhum sinal de que 89% do texto nao foi traduzido. Nao ha excecao, nao ha aviso, nao ha
  metrica exposta ao usuario.
Escopo pedido pelo usuario nesta invocacao: os DOIS defeitos (extracao + sinal de cobertura).

D-2026-08-01-div-paragraph-translation-1 (extracao): `ExtractTextBlocks` tenta `p|h1-6|li`
(regex atual, intocada). SO quando isso devolve zero blocos PARA AQUELE CORPO, cai num fallback
de div-folha (div sem `<div>` aninhado antes do fechamento — lookahead negativo por caractere,
com `RegexTimeoutMilliseconds` que toda regex de `HtmlUtility` ja carrega, csharp.md §4/ReDoS).
Bloco de div so conta se tiver >= 1 letra Unicode (`char.IsLetter`) apos `StripHtmlTags` — filtra
imagem/bullet/numero isolado sem dependencia nova. REJEITADO: parser de HTML real (AngleSharp/
HtmlAgilityPack) — mudaria a arquitetura 100%-regex de `HtmlUtility` de bugfix pra rewrite de
Utility inteira (mesmo racional de `coverage-90`: zero dependencia nova quando da pra resolver
sem); nesting real medido no livro do usuario e raso (`calibreN` direto), lookahead basta.
Fallback e por CHAMADA (=por capitulo), sem heuristica de "livro inteiro".

D-2026-08-01-div-paragraph-translation-2 (baseline): os 3 fixtures reais (`Wardley Maps`,
`Righting software`, `Practice Makes Perfect`) nao tem `<div>` fora de `p|h1-6|li` hoje, entao o
fallback nunca ativa neles — provado por teste de caracterizacao (fixa a contagem ATUAL antes da
mudanca), nao so "codigo intocado".

D-2026-08-01-div-paragraph-translation-3 (sinal de cobertura): `TranslateBookAsync` passa a
devolver `BookTranslationResult(string EpubPath, double CoveredTextRatio)` em vez de `string`
cru. `CoveredTextRatio` = caracteres NAO-espaco extraidos em blocos / caracteres NAO-espaco do
corpo inteiro (`StripHtmlTags` + `char.IsWhiteSpace`), agregado por capitulo dentro de
`RebuildAllTranslatedChaptersAsync` (ja itera todo capitulo — zero I/O novo); 1.0 se o corpo for
vazio. NUNCA lanca excecao por cobertura baixa (csharp.md §1: formato inesperado e fluxo
esperado, nao erro). `ILogger` REJEITADO como veiculo: nenhum Manager/Engine do Core injeta
logger hoje — infra nova fora de escopo de bugfix. `IProgress<BookTranslationProgress>`
REJEITADO como veiculo unico: o parametro pode ser `null`, e o ponto do defeito e nunca ficar em
silencio.

D-2026-08-01-div-paragraph-translation-4 (impacto em src/TranslateReader/): mudar o retorno
obriga 1 ajuste MECANICO em `LibraryPageModel.TranslateBookAsync` (ler `result.EpubPath`) — nao
e UI nova. Decidir SE/COMO avisar visualmente o usuario sobre `CoveredTextRatio` baixo fica em
`## Deferred to PR review` do CONTEXT.md (decisao de produto/UX humana).

D-2026-08-01-div-paragraph-translation-5 (fixture de teste): nem o EPUB do usuario (protegido,
caminho pessoal, obra com direitos) nem um `.epub` sintetico novo tipo `CreateOrphanCoverEpub` —
o defeito vive inteiro em `HtmlUtility.ExtractTextBlocks(string bodyContent)`, que nao toca
arquivo. Teste usa STRING HTML literal reproduzindo a forma calibre — sem I/O, sem EPUB, sem
questao de copyright, mais estreito que o precedente do brief. Corpos sinteticos (Fixture A/B)
fixados em `## Notes` do CONTEXT.md.

D-2026-08-01-div-paragraph-translation-6 (bugfix comeca vermelho): os testes de Fixture A/B e a
caracterizacao dos 3 fixtures reais sao escritos ANTES do fallback existir — o de Fixture A fica
vermelho (0 blocos) ate o fallback ser implementado.

D-2026-08-01-div-paragraph-translation-7 (selecao de blocos: uniao disjunta numa UNICA regex) —
**supersede SO o gatilho** de `D-2026-08-01-div-paragraph-translation-1`; todo o resto daquela
decisao (div-folha por lookahead negativo por caractere, guarda de letra Unicode `char.IsLetter`
apos `StripHtmlTags`, `RegexTimeoutMilliseconds` obrigatorio, rejeicao de AngleSharp/
HtmlAgilityPack, decisao por CHAMADA) continua valendo integralmente.
Motivo medido (livro-origem do bug report, 53 documentos): B = palavras em `p|h1-6|li` = 11.114;
D = palavras em div-folha = 88.042; C = corpo total = 88.107. B + D = 99.156 > C = 88.107, ou seja
>= 11.049 palavras vivem DENTRO de div-folha e seriam contadas duas vezes por uma uniao ingenua.
Consequencias:
- O gatilho "fallback so quando `p|h|li` devolve ZERO blocos" cobre apenas 39.051 palavras =
  44,3% do corpo, porque 33 dos 53 documentos tem ALGUNS blocos `p|h|li` e a prosa toda em
  `<div>`. Entregaria o mesmo bug de volta ao usuario.
- Uniao ingenua (`p|h|li` + todo div-folha) traduz 11.049 palavras 2x e, pior, faz a lista de
  extracao deixar de casar 1:1 com a varredura de `ReplaceTextBlocksInHtml` (`translations[index++]`):
  traducao escrita no bloco errado — falha silenciosa pior que a original.
Regra nova: UMA `[GeneratedRegex]` com alternacao, branch `p|h[1-6]|li` PRIMEIRO e branch de
div-folha em segundo, onde o branch de div so casa `<div ...>` cujo conteudo NAO contem `<div`,
`<p`, `<h[1-6]` nem `<li` (token temperado `(?:(?!...).)*`). As duas fontes ficam disjuntas POR
CONSTRUCAO, em ordem de documento, num unico `Matches` — dedup vira invariante estrutural, nao
codigo. Teto da regra = 88.107 palavras = 100% do corpo.
REJEITADA a alternativa (a) "escolher por corpo quem rende mais texto" (teto 88.042 = 99,93%,
delta de so 65 palavras) por dois motivos que nao sao os 0,07%: (i) exigiria recomputar a mesma
decisao dentro de `ReplaceTextBlocksInHtml`, que recebe o html INTEIRO e nao o body — divergencia
entre as duas passagens = traducao no paragrafo errado; (ii) um `<div class="section">` com varios
`<p>` dentro (forma dos 3 fixtures reais) venceria a comparacao e viraria UM bloco gigante,
regredindo granularidade, cache e tamanho de prompt.

D-2026-08-01-div-paragraph-translation-8 (simetria extracao/substituicao): `ExtractTextBlocks` e
`ReplaceTextBlocksInHtml` compartilham OBRIGATORIAMENTE a mesma selecao (a regex de
`D-...-7`) E o mesmo predicado de filtro. Defeito estrutural que motiva a decisao:
`ReplaceTextBlocksInHtml` usa `TextBlockRegex` sozinha (`HtmlUtility.cs:43`), entao corrigir so a
extracao faria o motor traduzir e cachear os divs e NUNCA escrever nada no EPUB — a correcao
entregaria o livro igualmente em ingles, so que mais lenta.
O predicado e assimetrico POR BRANCH e simetrico entre as duas passagens: branch de div exige
>= 1 `char.IsLetter` apos `StripHtmlTags` (filtra imagem/bullet/numero isolado); branch
`p|h[1-6]|li` mantem o filtro de whitespace ATUAL (`string.IsNullOrWhiteSpace`) — endurece-lo
mudaria a baseline de caracterizacao dos 3 fixtures reais (`D-...-2`). Filtro diferente entre as
duas passagens desalinha `translations[index++]`; e a falha que o teste de round-trip mata.
