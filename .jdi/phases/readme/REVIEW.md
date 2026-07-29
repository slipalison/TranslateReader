# Phase 10: Review  (slug: readme)

**Verdict:** BLOCKED

Iteracao 1 (`mode=verify`). Range revisado: `657cfe6..02bd8eb` (8 commits). Unico arquivo de
producao tocado: `README.md`. Nenhum `.cs`, `.csproj`, `.yml` ou `.slnx` alterado — confirmado por
`git diff --name-only 657cfe6~1 02bd8eb` (3 arquivos: `README.md`, `PLAN.md`, `SUMMARY.md`).

Fase de documentacao: os gates 1-4 sao baratos e servem so para provar ausencia de regressao. O
peso do review esta no Gate 5 + Gate 8 — **um README que afirma algo falso E o defeito**. Foram
verificadas uma a uma todas as afirmacoes factuais do arquivo contra o repositorio.
**Tres afirmacoes falsas sobreviveram**, uma delas um over-claim de seguranca.

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` -> `0 Erro(s)`, 40 avisos (todos `MVVMTK0045` em `ReaderPageModel.cs`, legado pre-existente) |
| Tests | PASS | `Aprovado! - Com falha: 0, Aprovado: 169, Ignorado: 2, Total: 171`. Baseline 169+2 preservada; os 2 skips sao os `TranslationEngineTests` que exigem GGUF real |
| Coverage | SKIPPED | `git log --diff-filter=A ... 4285f25..HEAD \| grep '\.cs$'` vazio — 0 arquivo `.cs` novo/alterado na phase. Esperado (D-2/D-6), mesmo padrao de `ci-seguranca`/`pipeline-unificada` |
| Lint | PASS | `dotnet format --verify-no-changes` -> exit 0, zero diff de formatacao |
| Security/Layer | **BLOCK** | B-1: README declara um controle de seguranca (validacao de path escape + limite de tamanho descomprimido) que **nao existe no codigo**. Greps estruturais 5.1/5.2/5.10 limpos; 5.12/5.15 sem novidade sobre a baseline legada |
| Consistency | PASS | 8 commits, todos `docs(readme): ...` (type correto — nada de `feat` cego), escopo `readme`, atomicos 1:1 com T-1..T-7 + 1 commit de artefato, trailer de sessao em 8/8 |
| UI Validation | SKIPPED | `has_frontend=false` (cliente MAUI nativo) — SKIP permanente por design |
| DoD | PASS (10/10 auto, 0 manual) | Os 10 `Verify:` do CONTEXT rodados verbatim, todos exit 0. **Porem hollow** — ver W-8: nenhum dos 10 comandos e capaz de pegar B-1, B-2 ou B-3 |

## Blockers

### B-1 — Over-claim de seguranca: controle declarado que nao existe (`README.md:286-288`)

> "O proprio codigo trata como **entrada nao confiavel** os arquivos EPUB (extracao de zip valida
> path escape e limita tamanho descomprimido)"

**Nao ha, em lugar nenhum do repositorio, validacao de path escape na extracao nem limite de
tamanho descomprimido.** Evidencia:

- `grep -rnE "ExtractToFile|ExtractToDirectory|entry\.FullName"` em `src/` -> nenhum resultado.
  Nao existe extracao de entry de zip para disco com validacao.
- O unico `ZipFile.Open` (`src/TranslateReader.Core/Business/Engines/ParsingEngine.cs:93`) abre a
  **copia de saida** em `ZipArchiveMode.Update` para reescrever capitulos traduzidos — nao extrai
  entradas nao confiaveis.
- O caminho que realmente escreve conteudo derivado do EPUB em disco e
  `src/TranslateReader.Core/Business/Managers/ReadingManager.cs:56-62`:
  ```
  var outputPath = Path.Combine(imagesDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
  await fileUtility.WriteFileAsync(outputPath, content);
  ```
  `relativePath` vem de `epub.Content.Images.Local` (`ParsingEngine.cs:62-64`), ou seja, dos
  caminhos internos do EPUB — **entrada nao confiavel**. Nao ha `Path.GetFullPath` + checagem de
  containment, nao ha rejeicao de `..`/path absoluto/letra de drive, e
  `src/TranslateReader.Core/Utilities/FileUtility.cs:31-32` faz
  `Directory.CreateDirectory(...)` + `File.WriteAllBytesAsync(filePath, content)` direto.
- `grep -niE "maxSize|maxBytes|uncompressed|sizeLimit"` em `ParsingEngine.cs` -> nada. Nenhum bound
  de tamanho descomprimido em lugar algum.
- O que **existe** e uma regra *preventiva* de Semgrep (`.semgrep/dotnet-security.yml`, id
  `translatereader-zip-slip`) e o mandato normativo em `.claude/rules/csharp.md` secao 4. Ambos
  dizem o que o codigo **deve** fazer; o README converteu isso numa afirmacao descritiva de que o
  codigo **ja faz**.

Isso e o pior tipo de defeito de documentacao: um README publico anunciando defesas contra
zip-slip e zip-bomb que o app nao tem. Pela regra de prioridade do projeto (1) Seguranca, e
BLOCK-class no Gate 5.

**Correcao (uma linha de prosa):** trocar por algo que descreva a postura real — por exemplo, que
as regras obrigatorias de tratamento de EPUB como entrada nao confiavel estao em
`.claude/rules/csharp.md` e sao cobradas por regras proprias de Semgrep em `.semgrep/`, sem
afirmar que a validacao ja esta implementada. (Implementar o controle de fato e trabalho de codigo
— ver W-2, fora do escopo desta phase README-only.)

### B-2 — Afirmacao falsa sobre a suite de testes (`README.md:242-244`)

> "Sao xUnit + NSubstitute, isolados: sem rede, sem disco e sem SQLite real."

Falso em duas das tres clausulas. Evidencia direta no proprio repositorio:

- **Disco real:** `test/TranslateReader.Tests/FileUtilityTests.cs:18,24,31,43,63,86` usa
  `File.WriteAllTextAsync` / `File.ReadAllTextAsync` / `File.WriteAllText` de verdade;
  `test/TranslateReader.Tests/HybridWebViewContractTests.cs:18,212,231` faz `File.ReadAllText`
  sobre os assets de JS/HTML.
- **SQLite real:** `test/TranslateReader.Tests/InMemoryDatabase.cs:19` ->
  `_anchor = new SqliteConnection(ConnectionString)`. E o provider `Microsoft.Data.Sqlite` real
  (in-memory, mas motor SQLite real), nao um substitute.

Mesma origem de B-1: a frase foi derivada da **regra** de `.claude/rules/csharp.md` secao 6
("no network/disk/real SQLite in unit tests") e apresentada como **descricao do estado atual**.
A regra vale para codigo novo pos-`4285f25`; a suite legada nao a cumpre.

**Correcao:** ou remover a clausula, ou reescrever como regra para contribuicao nova
("testes novos devem ser isolados: sem rede, sem disco e sem SQLite real"), que e o que a
`csharp.md` de fato manda.

### B-3 — Topologia de CI falsa: Scorecard nao e disparado pelo pipeline (`README.md:267-279`)

> "Todo push e todo pull request passam pelo orquestrador `pipeline.yml`, que dispara os workflows
> **reusaveis abaixo**:"

e a tabela logo em seguida inclui a linha **OpenSSF Scorecard / `scorecard.yml`**.

`pipeline.yml` despacha exatamente 8 jobs: `ci`, `codeql`, `semgrep`, `sca`, `secret-scan`,
`sonarqube`, `dependency-review` (so em PR), `sbom` (so em push). **`scorecard.yml` nao esta
entre eles.** Alem disso `scorecard.yml` nem e reusavel: seus triggers sao
`schedule: cron "30 2 * * 6"` + `push: branches: [main]` + `workflow_dispatch` — nao tem
`workflow_call`. Logo a linha da tabela esta sob uma frase que a descreve errado em dois pontos
(nao e disparado pelo orquestrador, e nao e reusavel).

Secundariamente, a mesma frase generaliza demais para mais duas linhas: `dependency-review` so
roda em pull request e `sbom` so roda em push — nao em "todo push e todo pull request".

**Correcao:** separar a linha do Scorecard (workflow independente, cron semanal + push em `main`)
ou reescrever a frase introdutoria para nao afirmar despacho universal.

## Warnings

- **W-1 — comando de build Android documentado falha neste ambiente (`README.md:226-227`).**
  Rodei o comando verbatim: `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f
  net10.0-android` -> `error NETSDK1005: O arquivo de ativos ... nao tem um destino para
  'net10.0-android'`, exit 1. Causa: `src/TranslateReader/TranslateReader.csproj:7` so acrescenta o
  TFM android se existir `$(LocalAppData)\Android\Sdk` / `$ANDROID_HOME` / `$ANDROID_SDK_ROOT`.
  `dotnet msbuild -getProperty:TargetFrameworks` nesta maquina devolve
  `net10.0-windows10.0.19041.0;net10.0-ios;net10.0-maccatalyst`. Ironia relevante: o callout logo
  abaixo (`README.md:236-238`) explica NETSDK1005 como se fosse exclusivo do build a nivel de
  solution, entao o leitor que topar com o erro no comando Android e mandado para a explicacao
  errada. Nao e blocker (o corpo do reviewer classifica falha de TFM mobile como WARN, e o DoD (f)
  so exigia matar o `-f` bare e apontar o csproj — cumprido). Sugestao: pre-requisito explicito de
  Android SDK nessa linha. O comando iOS, testado verbatim, compila (`0 Erro(s)`).
- **W-2 — divida de seguranca real por tras de B-1 (legado, D-2 isenta esta phase).** Independente
  da redacao do README, `ReadingManager.cs:56-62` + `FileUtility.cs:31-32` escrevem caminhos
  derivados de EPUB sem containment check e sem bound de tamanho. E codigo anterior a `4285f25`,
  entao nao bloqueia **esta** phase, mas Gate 5.6 nao tem boundary: vale abrir phase de hardening
  (`Path.GetFullPath` + `StartsWith(imagesDir)` e limite de bytes por entrada).
- **W-3 — "7 tabelas do SQLite" lista nomes de modelo, nao de tabela (`README.md:125-152`).** Os
  nomes reais no DDL sao `Books`, `Chapters`, `Bookmarks`, `BookTranslationJobs` (plural) e
  `ReadingProgress`, `Settings`, `TranslationCache` (singular). O README lista os 7 no singular.
  As **colunas** conferem 1:1 com o DDL de `BooksAccess.cs:23-42`, `ReadingStateAccess.cs:23-38`,
  `SettingsAccess.cs:23-26`, `TranslationCacheAccess.cs:22-30` e `BookTranslationJobAccess.cs:23-32`
  — inclusive o `UNIQUE(BookId, ChapterHRef, OriginalHash)` e o `LastCompletedChapterIndex`.
  Unica coluna com nuance perdida: `ReadingProgress.BookId` e `NOT NULL UNIQUE` no DDL, e o README
  so marca `UNIQUE` em `TranslationCache`.
- **W-4 — `CLAUDE.md` ficou defasado em relacao ao README (backlog).** Confirmado o achado do doer:
  a tabela de Componentes do `CLAUDE.md` lista 15 de 16 servicos (falta `BookTranslationJobAccess`),
  e a secao Modelos de Dados do `CLAUDE.md` lista 6 de 7 tabelas (falta `BookTranslationJob`).
  O README agora e **mais correto que a fonte que ele cita**. Registrar em `todos.md` / phase de
  manutencao.
- **W-5 — inconsistencia interna de plataformas.** `README.md:10` diz "projetado para Windows,
  Android e iOS (iPhone/iPad)" (3 plataformas), enquanto a tabela `README.md:37-43` e a Stack
  `README.md:161` dizem 4 (com macOS / Mac Catalyst). A linha 10 e pre-existente, mas o texto novo
  da linha 161 tornou o conflito visivel. O csproj confirma 4 TFMs.
- **W-6 — preambulo do Roadmap se contradiz (`README.md:293`).** "Tudo abaixo esta planejado, nao
  construido — **nao existe no repositorio hoje**", mas a propria linha de `bookmarks` (`:300`)
  diz "Hoje so existe a camada de dados". A ressalva da linha vence, mas o preambulo absoluto
  deveria ser suavizado.
- **W-7 — arvore de estrutura omite caminhos existentes.** Faltam `docs/` (contem
  `docs/translation-feature-plan.md`, referenciado pelo ROADMAP como evidencia de `llm-mobile`),
  `.semgrep/` (citado em prosa na secao Seguranca, `README.md:274`) e
  `src/TranslateReader/Properties/`. Todo caminho **mostrado** existe (verifiquei os 20 diretorios
  e 13 arquivos da arvore, 100% presentes) e `.idea/` sumiu corretamente — a arvore erra por
  omissao, nao por invencao. Baixa severidade.
- **W-8 — o DoD desta phase e verificavel sem ser verdadeiro (risco de hollow pass).** Os 10
  `Verify:` sao greps de presenca (`grep -qi "Scorecard"`, `grep -qi "Semgrep"`, `grep -q "90%"`).
  Nenhum deles poderia ter reprovado B-1, B-2 ou B-3, porque todos os tres sao afirmacoes **falsas
  sobre conteudo presente**. Recomendacao para o `/jdi-discuss` de futuras phases de documentacao:
  pelo menos um item de DoD que cruze a afirmacao com o artefato (ex: "todo workflow citado como
  disparado pelo pipeline aparece em `pipeline.yml`").
- **W-9 — imprecisao no SUMMARY (nao no README).** O desvio 1 do `SUMMARY.md` diz que o probe dos
  "3 badges externos" mudou de HEAD para GET por causa do 405. Verifiquei independentemente: so as
  **2** URLs do SonarCloud retornam `405` em HEAD; `api.scorecard.dev` responde `200` tanto em HEAD
  quanto em GET. A conclusao do doer (405 e falha do metodo de sondagem, nao da URL) esta correta,
  a contagem e que ficou imprecisa.

## Verificacao factual detalhada (nucleo do review)

### Tabela de 16 componentes — CONFERE

Contagem de linhas da tabela: 16 (`grep -cE '^\| \`[A-Za-z]+(Manager|Engine|Access|Utility)\` \|'`).
Cada servico existe no disco, na camada declarada:

| Camada | Declarado | Real (`ls`) | Veredito |
|---|---|---|---|
| Manager (4) | Reading, Library, Translation, Settings | `Business/Managers/` tem exatamente esses 4 | OK |
| Engine (3) | Parsing, Translation, Theme | `Business/Engines/` tem exatamente esses 3 | OK |
| ResourceAccess (6) | Books, ReadingState, Settings, TranslationCache, Model, BookTranslationJob | `Access/` tem exatamente esses 6 | OK |
| Utility (3) | File, Prompt, Html | `Utilities/` tem exatamente esses 3 | OK |

As 15 descricoes que existem em `CLAUDE.md` foram copiadas **literalmente**, como o CONTEXT mandou
— comparei celula a celula. A 16a (`BookTranslationJobAccess`) foi derivada: ver ruling do desvio 3.

Detalhe correto e facil de errar: `HtmlUtility` esta marcado "(estatico)" e a arvore mostra
`Contracts/Utilities/` com apenas `IFileUtility, IPromptUtility` — confere com o `ls` real (nao
existe `IHtmlUtility`).

### Diagrama de camadas e casos de uso — CONFEREM

As regras de chamada (`README.md:79-84`) sao transcricao fiel do bloco de `CLAUDE.md`. Spot-check
do caso de uso 7 ("`LibraryPage -> TranslationManager -> ParsingEngine + TranslationEngine +
BookTranslationJobAccess`"): o ctor de `TranslationManager.cs:14-21` injeta `ITranslationEngine`,
`IModelAccess`, `ITranslationCacheAccess`, `IBookTranslationJobAccess`, `IPromptUtility`,
`IBooksAccess`, `IParsingEngine`. Caso 6 idem. Confere.

### Versoes declaradas — TODAS CONFEREM

| Afirmacao README | Fonte | Veredito |
|---|---|---|
| LLamaSharp 0.27.0 | `TranslateReader.Core.csproj:19` | OK |
| Backends Cpu/Cuda12 0.27.0 sob condicao `windows` | app csproj:84-86 (`ItemGroup Condition ... == 'windows'`) | OK — a ressalva D-...-3 esta factualmente ancorada |
| VersOne.Epub 3.3.6 | Core.csproj | OK |
| Microsoft.Data.Sqlite.Core 10.0.10 | Core.csproj | OK |
| SQLitePCLRaw.bundle_green 2.1.11 | Core.csproj | OK |
| CommunityToolkit.Mvvm 8.4.2 | app csproj:78 | OK |
| CommunityToolkit.Maui 14.2.2 | app csproj:79 | OK |
| .NET 10 / `net10.0` | os 3 csproj | OK |
| MAUI (sem numero de versao) | `Microsoft.Maui.Controls` 10.0.60 | OK (nao ha numero afirmado, logo nada a contradizer) |

### Nada nao-construido descrito como pronto — CONFERE (e o desvio 2 estava certo)

- `BookDetailPage` / `BookDetailPageModel`: `grep -c` = 0 para os nomes de arquivo; aparecem so na
  tabela de Roadmap, marcados "Planejado", apontando `detalhe-livro`. Os arquivos de fato nao
  existem (`ls src/TranslateReader/Pages` -> `LibraryPage`, `ReaderPage`, `Controls/`).
- `bookmarks`, `busca-no-livro`, `llm-mobile`: todos so na tabela de Roadmap, coluna "Planejado".
- A ordem das 6 linhas do Roadmap (`baseline-de-estilo`, `cobertura-e-ci`, `bookmarks`,
  `detalhe-livro`, `busca-no-livro`, `llm-mobile`) bate exatamente com Phases 1-6 de
  `.jdi/ROADMAP.md` — a afirmacao "na ordem em que sera atacada" e verdadeira.
- Varredura de outras alegacoes "shipped-sounding" nas Funcionalidades: cada bullet foi checado
  contra codigo. Todos ancorados (`CreateTranslatedEpubAsync` para o export de EPUB,
  `IModelAccess` para o download do GGUF, `TranslationCacheAccess` para o cache por hash,
  `BookTranslationJob` + `LastCompletedChapterIndex` para pause/retomada). Nenhum sobrevivente.

### Badges — 6/6 resolvem 200 em GET

Sondados por mim com `curl -sL -o /dev/null -w '%{http_code}'`:

| Badge | GET | HEAD |
|---|---|---|
| Pipeline (`actions/workflows/pipeline.yml/badge.svg`) | **200** | — |
| CodeQL (`actions/workflows/codeql.yml/badge.svg`) | **200** | — |
| OpenSSF Scorecard (`api.scorecard.dev/...`) | **200** | 200 |
| Sonar Quality Gate (`metric=alert_status`) | **200** | 405 |
| Sonar Coverage (`metric=coverage`) | **200** | 405 |
| License (`img.shields.io/...Apache_2.0...`) | **200** | — |

Ordem locked (Pipeline -> CodeQL -> Scorecard -> alert_status -> coverage -> shields) confirmada
por posicao no arquivo. Todo `actions/workflows/*.yml` citado em badge existe em
`.github/workflows/`. **O 405-em-HEAD do SonarCloud e independentemente confirmado** e e mesmo
falha do metodo de sondagem, nao da URL — o desvio 1 do doer procede (com a imprecisao de contagem
de W-9).

### Restante da secao Seguranca — confere, exceto B-3

Verifiquei linha a linha da tabela de 8 scanners:

- CodeQL "`security-extended` + run agendado semanal": `codeql.yml:36` `queries: security-extended`,
  `codeql.yml:5-6` `schedule: cron "26 7 * * 1"`. OK.
- Semgrep "registry + regras proprias em `.semgrep/`": `semgrep.yml:37`
  `--config auto --config .semgrep/`; `.semgrep/dotnet-security.yml` contem os ids
  `translatereader-zip-slip`, `translatereader-xxe`, `translatereader-webview-js-injection` — os
  tres exatamente como o README enumera. OK.
- SCA "reprova em CVE High/Critical": `sca.yml:43` `SCA gate (fail on High/Critical)`,
  `sca.yml:55` `BLOCKED_SEVERITIES = {"High", "Critical"}`. OK.
- Secret scan "Gitleaks sobre o historico": `secret-scan.yml:31` `gitleaks/gitleaks-action` com
  `fetch-depth: 0`. OK.
- Dependency review "resumo comentado no PR": `dependency-review.yml:31`
  `comment-summary-in-pr: on-failure`. OK.
- SBOM "SPDX com Syft + Dependency Submission API": `sbom.yml:32-38` `anchore/sbom-action`,
  `format: spdx-json`, `dependency-snapshot: true`. OK.
- SonarQube Cloud: `sonarqube.yml` presente, project key/org conferem com D-...-2. OK.
- **Scorecard: ver B-3.**
- "toda action de terceiro pinada por commit SHA completo": as 13 actions externas em
  `.github/workflows/*.yml` estao todas em SHA de 40 hex, com a tag so em comentario. OK.
- "`permissions:` nega tudo no topo": os 11 workflows tem `permissions: contents: read` no topo —
  e declarar qualquer escopo zera os demais, entao a afirmacao procede. OK.
- "jobs em `ubuntu-latest` rodam sob `step-security/harden-runner`": 9 workflows usam harden-runner;
  os 2 que nao usam sao `pipeline.yml` (so orquestra, nao tem runner proprio) e `release.yml` (roda
  em `windows-latest`). A qualificacao "ubuntu-latest" salva a frase. OK — precisa e verdadeira.
- Claim do WebView ("todo valor derivado do livro e codificado antes de chegar em JavaScript"):
  todas as interpolacoes em `ReaderPage.xaml.cs` passam por `JsStr(...)`
  (= `JsonSerializer.Serialize`, `:486-487`) ou por um `*Json` pre-serializado (`:305-306`). OK.
- Claim de cobertura ("a CI **coleta** mas ainda **nao reprova**"): `ci.yml:29-36` roda
  `--collect:"XPlat Code Coverage"` e sobe artefato, sem threshold. OK.

### Restricao de acentos — PASS (metodo declarado)

Como instruido, **nao** usei a forma que da falso-positivo. Rodei um scan em Python que enumera
todo code point > 127 (impossivel abortar silenciosamente):

```
non-ascii count: 17  — todos U+2014 EM DASH, nas linhas 26,30,33,157,161,166,179,198,206,260,264,275,293,300,301,312,313
```

Zero letras acentuadas. Como contra-prova rodei tambem
`LC_ALL=C.UTF-8 grep -nP "[\x{00C0}-\x{00FF}]" README.md` -> saida vazia, exit 1, **sem** a
mensagem "supports only unibyte and UTF-8 locales" que o doer reportou. Os 17 em-dash sao
pontuacao tipografica, nao acento, e ja eram convencao do arquivo. **PASS.**

## Ruling sobre os 3 desvios declarados pelo doer

**Desvio 2 (remover o bullet "Bookmarks" de Funcionalidades) — CORRETO, era obrigatorio.**
Nao aceitei a justificativa: verifiquei. `IReadingManager` expoe exatamente 5 operacoes
(`OpenBookAsync`, `LoadChaptersAsync`, `LoadChapterContentAsync`, `SaveProgressAsync`,
`LoadProgressAsync`) — **nenhuma** de bookmark. `grep -rniE "bookmark" src/TranslateReader/` (Pages
+ PageModels + XAML) retorna **zero** ocorrencias: nao existe UI. So
`IReadingStateAccess:9-11` tem as 3 operacoes de dados. Manter o bullet seria exatamente a
violacao que D-2026-07-29-readme-1 proibe ("nenhuma feature futura descrita como pronta"). Migrar
para o Roadmap com a nota "so existe a camada de dados" e a acao certa. **Aprovado.**

**Desvio 3 (derivar a descricao de `BookTranslationJobAccess`) — CORRETO.**
Comparei a celula do README ("Estado do job de traducao de livro completo: buscar job ativo,
salvar, atualizar progresso, remover") com `Contracts/Access/IBookTranslationJobAccess.cs`:
`FetchActiveJobAsync` / `SaveJobAsync` / `UpdateJobProgressAsync` / `DeleteJobAsync`. Mapeamento
1:1, nada inventado, e no estilo das outras 15 celulas. A alternativa (omitir o 16o servico) teria
reprovado o DoD (c). **Aprovado** — e o gap do `CLAUDE.md` vira backlog (W-4).

**Desvio 1 (coluna "Volatilidade Encapsulada" -> "Responsabilidade") — ACEITO, com ressalva.**
Julgado, nao aceito de cara. Argumento contra: The Method e o design travado (D-1/D-5) e
decomposicao por volatilidade e a ideia que o define; o README antigo usava o rotulo de proposito.
Argumento a favor, que vence: o CONTEXT mandou **reusar literalmente** as descricoes de
`CLAUDE.md` e proibiu explicitamente "inventar volatilidade encapsulada nova" (Notes do CONTEXT).
Aquelas descricoes sao responsabilidades ("Orquestra leitura: abrir livro, salvar/carregar
progresso, navegar"), nao eixos de volatilidade. Manter o cabecalho "Volatilidade Encapsulada"
sobre texto de responsabilidade seria um **rotulo falso** — precisamente o defeito que esta phase
existe para eliminar. Renomear e a escolha honesta. **Ressalva:** o enquadramento de volatilidade
sobrevive apenas na prosa acima da tabela (`README.md:47`, "The Method (Decomposicao Baseada em
Volatilidade)"), entao nao se perdeu do documento. Se o dono quiser a moldura de volta, o caminho
certo e uma **coluna adicional** com o eixo de volatilidade real de cada servico — nunca renomear
o cabecalho de volta sem trocar o texto. Fica como sugestao, nao como pendencia.

## Leitura de ponta a ponta como recem-chegado ("bem explicado")

Ordem das secoes esta boa: identidade -> o que faz -> onde roda -> como e construido -> o que tem
dentro -> como buildar -> como testar -> seguranca -> o que falta -> como contribuir -> licenca.
Um recem-chegado consegue clonar, buildar no Windows e rodar os testes so com o README. A secao de
Roadmap separando "pronto" de "planejado" e a maior melhoria sobre a versao anterior, e o callout
do NETSDK1005 mostra o porque do comando, nao so o comando. Lacunas substantivas, todas como
warning:

- **Nao ha "como usar".** Nenhuma linha sobre o primeiro uso: importar um EPUB, e sobretudo que a
  traducao offline exige **baixar um modelo GGUF pelo app antes** (o `DefaultModel` e
  `gemma-2-2b-it-Q4_K_M.gguf`, `TranslationManager.cs:23-25`). O leitor descobre que existe
  download de modelo na lista de features, mas nunca que e passo obrigatorio, nem a ordem de
  grandeza de disco/RAM. E a duvida numero 1 de quem chega pela feature principal.
- **Falta o pre-requisito de SDK Android** no bloco de build (W-1) — o unico ponto do README que
  entrega um comando que falha.
- Nao ha screenshot nem GIF. Para um leitor de EPUB com temas, e a forma mais barata de comunicar
  o produto. Opcional.
- `docs/translation-feature-plan.md` existe e nao e linkado de lugar nenhum (W-7).

Nada disso e bloqueante. B-1/B-2/B-3 sao.

## DoD Checklist (gate 8)

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | (a) Licenca: sem "Projeto privado", cita Apache 2.0 + `LICENSE` | CONTEXT | Auto | PASS | exit 0; `LICENSE` presente; secao `README.md:320-322` |
| 2 | (b)+readme-3 Traducao offline documentada + ressalva Windows-only -> `llm-mobile` | CONTEXT | Auto | PASS | exit 0; `README.md:22-33`; ressalva ancorada em app csproj:84-86 |
| 3 | (c) 16 servicos reais na tabela | CONTEXT | Auto | PASS | exit 0; 16 linhas de tabela; 16/16 existem no disco na camada declarada |
| 4 | (d) 3 projetos reais na estrutura | CONTEXT | Auto | PASS | exit 0; `README.md:176-210`; todos os 33 caminhos mostrados existem |
| 5 | (e) `BookDetailPage`/`BookDetailPageModel` so em roadmap | CONTEXT | Auto | PASS | exit 0; `grep -c` = 0 para ambos os nomes de arquivo; `detalhe-livro` presente |
| 6 | (f) Build aponta o csproj (sem `-f` bare); `dotnet test` presente | CONTEXT | Auto | PASS | exit 0; `grep -c "dotnet build -f "` = 0; comandos rodados de verdade (ver W-1 p/ android) |
| 7 | (g) Modelos de Dados com `TranslationCache` + `BookTranslationJob` + `OriginalHash` | CONTEXT | Auto | PASS | exit 0; colunas conferidas contra o DDL real (ver W-3) |
| 8 | (h)+(i) `.idea/` fora da arvore; temas Light/Dark/Sepia | CONTEXT | Auto | PASS | exit 0; `grep -c "\.idea"` = 0; "claro/escuro" eliminado |
| 9 | Badges D-2026-07-29-readme-2: 6 badges, URL real e resolvivel, ordem locked | CONTEXT | Auto | PASS | exit 0; 6/6 GET 200 sondados por mim; ordem confirmada; workflows citados existem |
| 10 | D-2026-07-29-readme-4: 4 secoes novas (Seguranca, Testes+90%, Contributing/JDI, Licenca) | CONTEXT | Auto | PASS | exit 0; todas as 4 presentes — mas ver B-2 e B-3, dentro de 2 delas |

**Totals:** 10 items | Auto: 10 (10 PASS, 0 FAIL) | Manual: 0 pending

Gate 8 fecha PASS, e mesmo assim a phase esta BLOCKED: os blockers estao em afirmacoes que nenhum
dos 10 comandos consegue avaliar (W-8). Gate 8 verde nao e evidencia de README correto aqui.

## Recommendation

Nao e retrabalho de phase — sao **tres correcoes de prosa** em `README.md`, nenhuma linha de
codigo, nenhuma decisao a redecidir:

1. `README.md:286-288` — parar de afirmar que o codigo valida path escape e limita tamanho
   descomprimido; descrever a postura real (regra obrigatoria em `.claude/rules/csharp.md` +
   regras proprias de Semgrep). **Este e o unico blocker de classe seguranca.**
2. `README.md:242-244` — transformar "sao isolados: sem rede, sem disco e sem SQLite real" em
   regra para testes novos, ou remover a clausula.
3. `README.md:267-279` — tirar `scorecard.yml` de baixo de "o pipeline dispara os workflows
   reusaveis abaixo", ou reescrever a frase; de quebra, marcar `dependency-review` (so PR) e
   `sbom` (so push) como condicionais.

Aproveitar a mesma passada para W-1 (pre-requisito de Android SDK), W-5 (linha 10 com 3 de 4
plataformas) e W-6 (preambulo do Roadmap). Um unico commit `fix(readme): ...` resolve todos.

W-2 (hardening real de zip-slip/zip-bomb) e W-4 (`CLAUDE.md` com 15/16 servicos e 6/7 tabelas) sao
trabalho de outra phase — registrar no roadmap, nao segurar esta.

Depois do commit de correcao, re-rodar `/jdi-verify readme`. Gates 1-4, 6 e 7 nao precisam de nova
evidencia (nenhum `.cs` sera tocado); a iteracao 2 deve reconferir os tres trechos corrigidos e a
bateria de DoD.
