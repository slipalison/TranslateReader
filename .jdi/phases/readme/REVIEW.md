# Phase 10: Review  (slug: readme)

**Verdict:** APPROVED_WITH_WARNINGS

**Round:** 2 (`mode=verify`, iter=2). Commits de correcao revisados: `211a00b`
(`fix(readme): remove three false claims about code, tests and CI`) e `08a9857`
(`docs(readme): record fix round 1 and escalate the real zip-slip gap`). HEAD `08a9857`,
branch `jdi/readme`.

Escopo real da rodada de fix, confirmado por `git diff --name-only 02bd8eb..HEAD`: `README.md`,
`.jdi/phases/readme/SUMMARY.md`, `.jdi/todos.md` e o `REVIEW.md` da iteracao 1. Nenhum `.cs`,
`.csproj`, `.yml` ou `.slnx` tocado — a phase continua README-only.

**Os 3 blockers da iteracao 1 estao resolvidos.** Cada um foi re-verificado do zero contra o
repositorio, frase por frase, sem aceitar o SUMMARY do doer como evidencia. Nenhum blocker novo.
Um warning novo de peso (W-10) e um julgamento sobre uma remocao voluntaria (W-11) ficam abertos,
alem de um achado escalado que, na minha leitura, precisa de tratamento mais forte do que
`todos.md` (ver `## Achado escalado`).

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` -> `0 Erro(s)`, 40 avisos (todos `MVVMTK0045` em `ReaderPageModel.cs`, legado) |
| Tests | PASS | `Aprovado! - Com falha: 0, Aprovado: 169, Ignorado: 2, Total: 171`. Baseline 169+2 preservada; os 2 skips sao `TranslationEngineTests.InitializeAsync_LoadsModel_WithValidPath` e `.GenerateAsync_ProducesOutput_WithValidModel` |
| Coverage | SKIPPED | `git log --diff-filter=A ... 4285f25..HEAD \| grep '\.cs$'` -> 0 arquivos. Esperado (D-2/D-6) |
| Lint | PASS | `dotnet format --verify-no-changes` -> exit 0, zero diff |
| Security/Layer | PASS (com warnings) | B-1/B-2/B-3 resolvidos e re-verificados. Greps 5.1/5.2/5.10/5.17 limpos; 5.12 e 5.15 sem novidade sobre a baseline legada. Warnings novos: W-10, W-11 |
| Consistency | PASS | 2 commits novos, `fix(readme)` + `docs(readme)` (tipo correto: o de correcao e `fix`, o de artefato e `docs`), escopo `readme`, atomicos, trailer de sessao em 2/2 |
| UI Validation | SKIPPED | `has_frontend=false` (cliente MAUI nativo) — SKIP permanente por design |
| DoD | PASS (10/10 auto, 0 manual) | Os 10 `Verify:` do CONTEXT rodados verbatim, 10/10 exit 0 |

## Blockers

**Nenhum.** Os 3 da iteracao 1 foram fechados.

### Changelog dos blockers resolvidos

**B-1 — over-claim de seguranca (`README.md:321-334`) — RESOLVIDO.**
A frase que afirmava que "o proprio codigo ... valida path escape e limita tamanho descomprimido"
saiu por completo. O texto que ficou no lugar foi verificado clausula a clausula:

- "As regras obrigatorias para trata-los — rejeitar path escape ..., limitar tamanho descomprimido,
  parsear XML com DTD desabilitado, codificar todo valor derivado do livro antes de interpola-lo em
  JavaScript — estao escritas em `.claude/rules/csharp.md` secao 4." Li a secao 4: as quatro estao
  la, literalmente ("reject entry paths that escape the target directory ... Bound decompressed
  sizes. Parse XML with DTD processing disabled (`DtdProcessing.Prohibit`, no `XmlResolver`)" e
  "Never inject book-derived strings into JS without encoding"). **CONFERE.**
- "Sao **normativas para codigo novo**, nao uma descricao do que ja esta implementado." E exatamente
  o enquadramento que faltava. **CONFERE.**
- "O arquivo `.semgrep/dotnet-security.yml` traz 4 regras proprias ... `translatereader-zip-slip`,
  `translatereader-xxe`, `translatereader-webview-js-injection` e
  `translatereader-insecure-deserialization`". Li o arquivo: sao exatamente 4 regras, com esses 4
  ids. **CONFERE.**
- "reprovando o build nas de severidade ERROR". `semgrep.yml:44` roda
  `semgrep scan --config .semgrep/ --severity ERROR --error --metrics=off .`; das 4 regras, 3 sao
  `severity: ERROR` e a de WebView e `WARNING`. A qualificacao "nas de severidade ERROR" salva a
  frase. Confirmado na pratica: rodei o gate e o semgrep reporta `Scanning 112 files with 3 csharp
  rules` — 3, nao 4. **CONFERE.**
- "Sao regras de **deteccao em CI**, nao defesas em runtime: elas apontam o codigo que viola a
  politica, quem implementa a protecao e o codigo." Esta e a frase que fecha o blocker. **CONFERE.**

Varredura de controle: nao sobrou nenhuma outra afirmacao de hardening em runtime na secao. O unico
"aplicado" que restou e sobre supply chain de workflow, nao sobre codigo do app — e foi re-verificado
(ver abaixo). **Fechado.**

**B-2 — afirmacao falsa sobre a suite de testes (`README.md:256-271`) — RESOLVIDO.**
Cada numero e cada nome de arquivo do texto novo foi conferido:

| Afirmacao do README | Verificacao | Veredito |
|---|---|---|
| "171 testes, 169 passando e 2 ignorados" | `dotnet test` -> `Com falha: 0, Aprovado: 169, Ignorado: 2, Total: 171` | OK |
| "os dois de `TranslationEngineTests` marcados `Skip = "Requires GGUF model file for local development"`" | `TranslationEngineTests.cs:56` e `:69`, `[Fact(Skip = "Requires GGUF model file for local development")]` — string **identica**; e a saida do runner nomeia esses 2 testes | OK |
| "A suite nao acessa a rede" | Nenhum teste exercita caminho HTTP. `ModelAccessTests.cs:8` de fato instancia um `HttpClient` real, mas nenhum dos 8 testes chama `DownloadModelAsync` — so `IsModelAvailable`/`GetModelPath`/`DeleteModelAsync`. A afirmacao e sobre acesso a rede, e nao ha | OK (ver nota) |
| "SQLite in-memory pelo provider real `Microsoft.Data.Sqlite` (`InMemoryDatabase.cs`)" | `InMemoryDatabase.cs:1` `using Microsoft.Data.Sqlite;`, `:19` `new SqliteConnection(ConnectionString)` | OK |
| "diretorios temporarios sob `Path.GetTempPath()` (`FileUtilityTests`, `ModelAccessTests`, `ParsingEngineTests`)" | `grep -rln "Path.GetTempPath" test/` devolve **exatamente esses 3 arquivos**, nem mais nem menos | OK |
| "`HybridWebViewContractTests` le do disco os assets de JS/HTML do proprio repositorio" | `:15` monta `src/TranslateReader/Resources/Raw/wwwroot/js`, `:18/:212/:231` `File.ReadAllText` | OK |
| "O restante isola as dependencias com substitutes das interfaces de `Contracts/`" | Gate 5.17: `grep Substitute.For<` filtrado por nao-`I[A-Z]` -> vazio. Todo substitute mira interface | OK |
| "Para **codigo e testes novos**, a regra da secao 6 de `.claude/rules/csharp.md` pede isolamento completo ... a suite legada, anterior ao commit de boundary, nao a cumpre" | Atribuicao correta: a secao 6 e mesmo normativa e o texto agora a apresenta como convencao para codigo novo, nao como descricao | OK |

Nota (nao e warning): `ModelAccessTests.cs:8` cria um `HttpClient` vivo. Hoje nenhum teste o usa
para sair na rede, entao a frase e verdadeira; e so um detalhe que um teste futuro de download
tornaria falso sem ninguem perceber. **Fechado.**

**B-3 — topologia de CI falsa (`README.md:294-315`) — RESOLVIDO.**
Li `pipeline.yml` inteiro por conta propria. Ele declara exatamente 8 jobs, nesta ordem:
`ci` (:17), `codeql` (:23), `semgrep` (:31), `sca` (:38), `secret-scan` (:44), `sonarqube` (:50),
`dependency-review` (:62, `if: github.event_name == 'pull_request'`), `sbom` (:70,
`if: github.event_name == 'push'`). A tabela do README bate 8/8, na mesma ordem, com a coluna
Disparo marcando corretamente `somente PR` para dependency-review e `somente push` para sbom.

`scorecard.yml` saiu da tabela de despachados. A segunda tabela confere celula a celula:
`scorecard.yml` -> `schedule: cron "30 2 * * 6"` (dia 6 = sabado, 02:30 UTC), `push: branches:
[main]`, `workflow_dispatch`, e `publish_results: true` em `:42`; `release.yml` -> `push: tags:
["v*"]`, `runs-on: windows-latest`, `dotnet publish ... -f net10.0-windows10.0.19041.0`. A
justificativa "Nenhum dos dois declara `workflow_call`" tambem confere: `grep -c "workflow_call:"`
devolve 0 para os dois e 1 para os outros 8.

**Auto-catch do `ci.yml` verificado.** O doer diz ter corrigido a linha para "Suite de testes com
coleta de cobertura no Linux, mais build do app Windows". Li `ci.yml`: job `test` (`name: Test
(Linux)`, `runs-on: ubuntu-latest`, `:30` `dotnet test ... --collect:"XPlat Code Coverage"`) e job
`build` (`name: Build (Windows)`, `runs-on: windows-latest`, instala workload MAUI, builda o app).
O texto final esta **correto**. **Fechado** — com a ressalva de W-10 abaixo, que e da mesma secao
mas de outra classe.

**W-1 — comando de build Android — RESOLVIDO.**
Rodei os dois comandos verbatim do README:

- `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-ios` ->
  `0 Erro(s)`, 8 avisos, exit 0. **Compila.**
- `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-android` ->
  `error NETSDK1005`, exit 1 — como esperado nesta maquina, que nao tem SDK do Android.

A diferenca em relacao a iteracao 1 e que agora o README **avisa antes**: a propria linha do bloco
diz "exige o SDK do Android instalado (ver nota abaixo)" (`:229`) e existe um callout dedicado
`NETSDK1005` no build de Android (`:243-248`) citando `%LocalAppData%\Android\Sdk`, `$ANDROID_HOME`
e `$ANDROID_SDK_ROOT`. Conferi a ancora: `src/TranslateReader/TranslateReader.csproj` **linha 7** e
literalmente a condicao
`Condition="$([MSBuild]::IsOSPlatform('windows')) AND (Exists('$(LocalAppData)\Android\Sdk') OR Exists('$(ANDROID_HOME)') OR Exists('$(ANDROID_SDK_ROOT)'))"`.
A citacao esta certa ate o numero da linha.

Tambem confirmei a afirmacao "builde a partir de Linux/macOS, onde o TFM android e incondicional":
`csproj:4` acrescenta `net10.0-android` sob `Condition="!$([MSBuild]::IsOSPlatform('windows'))"`,
sem checagem de SDK. **Verdadeira.**

O callout pre-existente virou `NETSDK1005` a partir da raiz (`:250-252`), entao o leitor que topa
com o erro no comando Android **nao e mais mandado para a explicacao errada** — que era o ponto
da W-1. E o comando novo
`dotnet msbuild src/TranslateReader/TranslateReader.csproj -getProperty:TargetFrameworks`, rodado
verbatim, devolve `net10.0-windows10.0.19041.0;net10.0-ios;net10.0-maccatalyst`, exatamente o que o
README promete que ele faz. **Fechado.**

## Warnings

### Novos nesta rodada

- **W-10 — a coluna "Disparo" omite o cron proprio de 4 dos 8 jobs, e "somente push" vira
  enganoso (`README.md:298-307`).** A linha do CodeQL diz "push e PR, **mais cron semanal
  proprio**". Isso cria um contraste falso: CodeQL nao e o unico. Contei os `schedule:` de todos os
  workflows —

  | Workflow | Cron proprio | Como o README descreve o Disparo |
  |---|---|---|
  | `codeql.yml` | `26 7 * * 1` (seg) | "push e PR, mais cron semanal proprio" — completo |
  | `semgrep.yml` | `45 6 * * 1` (seg) | "push e PR" — **omite o cron** |
  | `sca.yml` | `50 5 * * 3` (qua) | "push e PR" — **omite o cron** |
  | `secret-scan.yml` | `15 4 * * 0` (dom) | "push e PR" — **omite o cron** |
  | `sbom.yml` | `20 3 * * 2` (ter) | "**somente push**" — omite o cron E usa exclusiva |
  | `ci.yml`, `sonarqube.yml`, `dependency-review.yml` | nenhum | corretos |

  Lida so como "quando o pipeline despacha este job", cada celula e defensavel. Mas a linha do
  CodeQL rompe esse enquadramento ao noticiar um gatilho que **nao** e do pipeline, e a partir dai o
  leitor conclui que os outros nao tem. Para `sbom` a palavra "somente" passa a ser ativamente
  errada: um SBOM aparece numa terca-feira sem push nenhum.

  **Nao e blocker.** B-3 foi bloqueado porque atribuia ao pipeline um workflow que ele nao dispara —
  falso por atribuicao. Aqui nada de falso e atribuido: os 8 jobs, a ordem e a condicionalidade
  PR/push estao todos certos; o defeito e detalhe distribuido de forma desigual. Correcao de uma
  linha: ou tirar a clausula de cron da linha do CodeQL, ou acrescenta-la nas outras 4.

- **W-11 — a remocao do claim de WebView foi overcorrection (julgamento pedido).**
  O doer removeu, junto com B-1, a frase "todo valor derivado do livro e codificado antes de chegar
  em JavaScript", mesmo tendo eu verificado essa frase como **verdadeira** na iteracao 1. A
  justificativa dele foi que a regra de Semgrep correspondente e `WARNING` e nao `ERROR`. Re-auditei
  os 10 call sites de `EvaluateJavaScriptAsync` em `ReaderPage.xaml.cs`:

  | Linha | Forma | Valor derivado do livro? | Codificado? |
  |---|---|---|---|
  | 121, 122 | `$"setMode({JsStr(mode)})"`, `$"applyCss({JsStr(...)})"` | nao | sim (`JsStr`) |
  | 306 | `$"applyTranslations({itemsJson})"` | **sim** (texto traduzido) | sim — `:305` `JsonSerializer.Serialize(items, ...)` |
  | 324, 461 | script constante | nao | n/a |
  | 444-445 | `$"scrollToChapter({JsStr(savedHRef)}, {savedPos})"` | sim (`HRef`) | sim (`JsStr`) |
  | 456 | `$"{functionName}({JsStr(html)})"` | **sim** (HTML do capitulo) | sim (`JsStr`) |
  | 467 | `$"appendChunk({JsStr(chunk)})"` | **sim** | sim (`JsStr`) |
  | 474 | `$"flushChunk('{functionName}')"` | **nao** — `functionName` so recebe os literais `"loadScrollContent"` (`:128`) e `"loadChapter"` (`:132`) | n/a |
  | 480 | `EvaluateJavaScriptAsync(expression)` | nao — expressoes internas | n/a |

  `JsStr` e `JsonSerializer.Serialize` (`:486-487`). **Todo valor derivado do livro e de fato
  codificado.** Os dois call sites que a regra de Semgrep nao consegue provar (`:306` serializa
  direto em vez de passar pelo helper; `:474` interpola um nome de funcao interno) sao limitacao do
  **pattern**, nao furo do codigo — e por isso mesmo a regra foi posta em `WARNING`.

  **Ruling:** remover a **frase composta** foi certo — ela emendava um claim falso (EPUB) num claim
  verdadeiro (WebView) com um "e", e salvar metade de uma frase sob pressao e como se erra de novo.
  Mas nao readmitir a metade verdadeira como frase propria custou informacao correta e verificada.
  E o erro inverso de B-1 (subdeclarar em vez de superdeclarar), muito menos danoso, e o texto que
  ficou continua verdadeiro — a codificacao de JS aparece agora como regra normativa da secao 4.
  **Nao bloqueia.** Sugestao para uma passada futura: uma frase propria, precisa, do tipo "hoje o
  codigo ja encoda todo valor derivado do livro antes de interpolar em JavaScript (`JsStr` =
  `JsonSerializer.Serialize`, `ReaderPage.xaml.cs:486`)" — que e uma afirmacao pontual e
  verificavel, nao um claim blanket de hardening.

### Carregados da iteracao 1 (nao tocados pela rodada de fix, todos confirmados ainda abertos)

- **W-3 — "7 tabelas do SQLite" lista nomes de modelo, nao de tabela (`README.md:125-152`).** Os
  nomes reais no DDL sao `Books`, `Chapters`, `Bookmarks`, `BookTranslationJobs` (plural) e
  `ReadingProgress`, `Settings`, `TranslationCache` (singular). As colunas conferem 1:1 com o DDL.
- **W-4 — `CLAUDE.md` esta defasado em relacao ao README.** A tabela de Componentes do `CLAUDE.md`
  lista 15 de 16 servicos (falta `BookTranslationJobAccess`) e a secao Modelos de Dados lista 6 de 7
  tabelas (falta `BookTranslationJob`). O README hoje e mais correto que a fonte que ele cita.
- **W-5 — inconsistencia interna de plataformas.** `README.md:10` diz "Windows, Android e iOS"
  (3), a tabela `:37-43` e a Stack `:161` dizem 4 (com macOS / Mac Catalyst). O csproj confirma 4.
- **W-6 — preambulo do Roadmap se contradiz (`README.md:338`).** "Tudo abaixo esta planejado ... nao
  existe no repositorio hoje", mas a linha de `bookmarks` (`:345`) diz "Hoje so existe a camada de
  dados".
- **W-7 — arvore de estrutura omite caminhos existentes.** Faltam `docs/` (contem
  `docs/translation-feature-plan.md`, nao linkado de lugar nenhum), `.semgrep/` (agora citado em
  prosa em `:328`) e `src/TranslateReader/Properties/`. Erra por omissao, nunca por invencao.
- **W-8 — o DoD desta phase e verificavel sem ser verdadeiro.** Confirmado outra vez nesta rodada:
  os 10 `Verify:` deram 10/10 PASS **tanto antes quanto depois** da correcao dos 3 blockers. Sao
  greps de presenca; nenhum consegue avaliar veracidade. Gate 8 verde nao e evidencia de README
  correto nesta classe de phase. Recomendacao para `/jdi-discuss` de futuras phases de doc:
  pelo menos um item que cruze afirmacao com artefato.
- **W-9 — imprecisao de contagem no SUMMARY da iteracao 1** (2 URLs do SonarCloud em 405-HEAD, nao
  3). O SUMMARY nao foi corrigido; segue sendo detalhe de artefato, nao de produto.

### Lacunas de "bem explicado" (nao bloqueantes, ver secao propria)

- **Nao ha "como usar" / primeiro uso**, e em particular nenhum aviso de que a traducao offline
  exige **baixar um modelo GGUF pelo app antes** de funcionar. Segue aberto.
- Sem screenshot/GIF. Opcional.

## Achado escalado: a regra `translatereader-zip-slip` nao cobre o unico caminho real de risco

Este e, na minha leitura, o output mais consequente da phase. **Verifiquei de forma independente e
CONFIRMO — com evidencia empirica, nao so por leitura de pattern.**

**A regra.** `.semgrep/dotnet-security.yml:24-27`, `pattern-either`:

```
- pattern: Path.Combine($DEST, $ENTRY.FullName)
- pattern: $ENTRY.ExtractToFile(Path.Combine(...), ...)
- pattern: $ENTRY.ExtractToFile(Path.Combine(...))
```

Os tres exigem sintaxe de `System.IO.Compression`: ou o acesso literal ao membro `.FullName`, ou a
chamada `.ExtractToFile`.

**O codigo real.** `ReadingManager.cs:57-61`:

```csharp
foreach (var (relativePath, content) in images)
{
    var outputPath = Path.Combine(imagesDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
    await fileUtility.WriteFileAsync(outputPath, content);
}
```

`relativePath` vem de `epub.Content.Images.Local` (`ParsingEngine.cs:63-64`, `images[img.FilePath] =
img.Content`) — caminho interno do EPUB, entrada nao confiavel. `FileUtility.cs:31-32` escreve com
`Directory.CreateDirectory(Path.GetDirectoryName(filePath)!)` + `File.WriteAllBytesAsync`, sem
containment. Nao existe `.FullName` nem `ExtractToFile` em lugar nenhum de `src/` — o projeto usa
VersOne.Epub, que nunca expoe `ZipArchiveEntry`.

**Prova empirica.** Rodei o proprio gate de CI contra `src/`:

```
semgrep scan --config .semgrep/ --severity ERROR --metrics=off src/
-> Ran 3 rules on 112 files: 0 findings.
```

Depois montei um probe com 4 variantes e rodei a mesma regra:

| Caso | Forma | Resultado |
|---|---|---|
| A | `Path.Combine(dest, entry.FullName)` | **DETECTADO** (linha 11) |
| B | `entry.ExtractToFile(Path.Combine(dest, entry.FullName), true)` | **DETECTADO** (linha 20) |
| C | espelho exato de `ReadingManager.cs:59` (`Path.Combine(imagesDir, relativePath.Replace(...))`) | **NAO detectado** |
| D | `Path.Combine(imagesDir, relativePath)` — variavel simples, sem `.Replace` | **NAO detectado** |

`Findings: 3` — todos em A e B, nenhum em C ou D. A regra funciona perfeitamente para o codigo que
ela foi escrita para pegar, e e cega para o codigo que este repositorio realmente tem.

**Conclusao — e ela e mais forte do que a que o doer registrou.** `todos.md` atribui a falha a "uma
variavel intermediaria (`relativePath`)". O caso D mostra que nao e sobre a variavel intermediaria
nem sobre o `.Replace`: a regra exige o **acesso sintatico a `.FullName`**. Como este codebase
extrai EPUB pela API do VersOne.Epub e nunca toca em `ZipArchiveEntry`, a regra
`translatereader-zip-slip` **nao pode disparar no caminho de extracao real, em nenhuma forma que ele
venha a assumir**. Nao e um falso negativo pontual; e cobertura estruturalmente zero sobre o unico
vetor de zip-slip do produto. O gate de CI verde sobre este risco significa, hoje, exatamente nada.

**O `todos.md` captura adequadamente? Nao. Recomendo escalar para phase propria.** Motivos:

1. **O lugar esta errado por definicao.** O cabecalho do proprio `.jdi/todos.md:4-5` diz "Nunca vira
   phase automaticamente — precisa ser promovido via `/jdi-add-phase`". Um item de seguranca
   prioridade 1 num backlog explicitamente nao-promotor e um item que expira em silencio.
2. **A prioridade do projeto contradiz o enquadramento.** `CLAUDE.md` § "Prioridade quando conflita"
   poe Seguranca em 1o, e o corpo deste reviewer diz que o gate 5.6 "nao tem boundary" — D-2 isenta
   legado de cobertura e estilo, nunca de seguranca. Registrar como scope creep trata como
   nice-to-have algo que a politica do repo classifica como topo.
3. **O escopo e maior do que o registro sugere.** O item de `todos.md` descreve so a divida de
   **codigo**. A phase precisa de **duas** entregas: (a) o containment (`Path.GetFullPath` +
   `StartsWith(imagesDir, StringComparison.Ordinal)`, rejeitando e nao sanitizando) mais um bound de
   bytes por entrada/livro; e (b) **consertar a regra de Semgrep**, senao o mesmo defeito volta sem
   ser detectado. Hoje nada em `todos.md` obriga (b).
4. **Ha um efeito README de segunda ordem.** A secao Seguranca agora diz "Quem cobra essas regras
   hoje e a CI". Isso e verdadeiro sobre a intencao e sobre 3 das 4 regras, e o README nao promete
   que as regras pegam tudo — por isso **nao e blocker**. Mas quem le fica com a impressao de que o
   risco de zip-slip esta sob vigilancia automatica, e ele nao esta.

Acao concreta sugerida: `/jdi-add-phase "hardening-epub" --goal "containment de path e bound de
tamanho na extracao de EPUB, mais regra de Semgrep que cubra o caminho real"`. Enquanto nao virar
phase, o item de `todos.md` merece ao menos ganhar a conclusao do caso D acima (cobertura zero, nao
falso negativo pontual) e a exigencia (b).

## Re-verificacao factual das secoes alteradas

Alem dos 3 blockers, re-conferi tudo que sobreviveu nas secoes tocadas.

### Hardening de supply chain (`README.md:317-319`) — CONFERE

O antecedente da frase mudou (agora "todos esses workflows" cobre as duas tabelas), entao refiz as
tres checagens:

- "toda action de terceiro e pinada por commit SHA completo": as **14** actions externas distintas
  em `.github/workflows/*.yml` estao todas em SHA de 40 hex (`actions/checkout`, `setup-dotnet`,
  `setup-java`, `upload-artifact`, `dependency-review-action`, `anchore/sbom-action`,
  `github/codeql-action/{init,analyze,upload-sarif}`, `gitleaks/gitleaks-action`,
  `ossf/scorecard-action`, `softprops/action-gh-release`, `step-security/harden-runner`,
  `trufflesecurity/trufflehog`). Nenhuma tag `@vN`. OK.
- "`permissions:` nega tudo no topo": os **11** workflows tem `permissions: contents: read` no topo.
  OK.
- "jobs em `ubuntu-latest` rodam sob `step-security/harden-runner`": 9 dos 11 usam harden-runner; os
  2 que nao usam sao `pipeline.yml` (so orquestra, sem runner proprio) e `release.yml`
  (`runs-on: windows-latest`). A qualificacao "ubuntu-latest" mantem a frase precisa. OK.

### Badges — 6/6 GET 200 (re-sondados nesta rodada)

`curl -sL -o /dev/null -w '%{http_code}' --max-time 30`:

| Badge | GET |
|---|---|
| Pipeline (`actions/workflows/pipeline.yml/badge.svg`) | 200 |
| CodeQL (`actions/workflows/codeql.yml/badge.svg`) | 200 |
| OpenSSF Scorecard (`api.scorecard.dev/...`) | 200 |
| Sonar Quality Gate (`metric=alert_status`) | 200 |
| Sonar Coverage (`metric=coverage`) | 200 |
| License (`img.shields.io/...Apache_2.0...`) | 200 |

Ordem locked (Pipeline -> CodeQL -> Scorecard -> alert_status -> coverage -> shields) confirmada por
posicao (`README.md:3-8`). Todo `actions/workflows/*.yml` citado em badge existe em
`.github/workflows/`.

### Restricao de acentos — PASS (metodo que nao consegue passar em falso)

Scan em Python que enumera **todo** code point > 127 e testa acento por decomposicao NFD
(`unicodedata.combining`), em vez de depender de locale do `grep -P`:

```
total non-ascii code points: 25
  U+2014 EM DASH  x25
ACCENTED LETTERS: 0  []
LETTERS (any non-ascii alphabetic): []
```

Subiu de 17 para 25 em-dashes por causa da prosa nova. A restricao locked e **sem acentos**, nao sem
em-dash; em-dash e pontuacao tipografica e ja era convencao do arquivo. Zero letras acentuadas, zero
caracteres alfabeticos nao-ASCII. **PASS.**

### Greps estruturais do Gate 5

| Check | Resultado |
|---|---|
| 5.1 Client -> Access/Engines | vazio (OK) |
| 5.2 storage tech em `Contracts/Access/` | vazio (OK) |
| 5.10 sync-over-async | vazio (OK) |
| 5.12 static mutavel | 1 hit, `TranslationEngine.cs:16` — baseline legada conhecida |
| 5.15 catch vazio | 5 hits, todos legados em `LibraryPageModel`/`ReaderPageModel`/`ReaderPage.xaml.cs`; nenhum `.cs` tocado nesta phase |
| 5.17 substitutes em concretos | vazio (OK) |

Nenhum `.cs` foi alterado na phase, entao nada disso e atribuivel a esta entrega — sao a mesma
baseline da iteracao 1.

## Leitura de ponta a ponta como recem-chegado ("bem explicado")

Reli o README inteiro do zero, nao so o diff.

**Melhorou de verdade em relacao a iteracao 1**, e nas partes que importam. A secao Build ficou
notavelmente melhor: em vez de tres comandos soltos, agora ha uma explicacao de *por que* os TFMs
variam por maquina, um comando para o leitor descobrir os seus, e dois callouts `NETSDK1005`
distintos que mandam o leitor para a causa certa. Isso e a diferenca entre um README que lista
comandos e um que ensina o projeto.

A secao Testes ficou mais honesta e, por isso, mais util: dizer "SQLite in-memory pelo provider real
e diretorios temporarios, porque a unidade sob teste **e** o acesso a recurso" explica uma decisao de
design; a versao anterior ("sem disco, sem SQLite real") era so falsa. Idem a secao Seguranca: a
distincao entre **deteccao em CI** e **defesa em runtime** e exatamente o tipo de precisao que faz um
leitor tecnico confiar no resto do documento. Um README que diz com clareza o que **nao** esta
implementado compra credibilidade para tudo que afirma que esta.

A ordem das secoes continua boa: identidade -> o que faz -> onde roda -> como e construido -> o que
tem dentro -> como buildar -> como testar -> seguranca -> o que falta -> como contribuir -> licenca.
Um recem-chegado clona, builda no Windows e roda os testes so com este arquivo.

**A lacuna que continua incomodando e a mesma:** nao ha "como usar". O leitor chega pela feature
principal (traducao offline EN -> PT-BR), le que ela existe, le que so roda em Windows — e nunca
descobre que precisa **baixar um modelo GGUF pelo app antes de qualquer traducao funcionar**, nem a
ordem de grandeza de disco e RAM que isso custa. O download aparece na lista de Funcionalidades como
se fosse mais um recurso, e nao como passo obrigatorio. E a duvida numero 1 de quem instala.
Continua sendo warning, nao blocker — o card pedia "preciso e bem explicado", e o documento hoje e
preciso; "bem explicado" ele cumpre para *entender e construir* o projeto, e ainda nao para *usar*
o app.

Segundo ponto: `docs/translation-feature-plan.md` existe, e o plano detalhado justamente dessa
feature, e nao e linkado de lugar nenhum (W-7).

## DoD Checklist (gate 8)

Os 10 `Verify:` do CONTEXT.md rodados verbatim da raiz do repo, no HEAD `08a9857`:

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | (a) Licenca: sem "Projeto privado", cita Apache 2.0 + `LICENSE` | CONTEXT | Auto | PASS | exit 0; `LICENSE` presente; secao `README.md:365-367` |
| 2 | (b)+readme-3 Traducao offline documentada + ressalva Windows-only -> `llm-mobile` | CONTEXT | Auto | PASS | exit 0; `README.md:22-33` |
| 3 | (c) 16 servicos reais na tabela | CONTEXT | Auto | PASS | exit 0; 16/16 existem no disco na camada declarada |
| 4 | (d) 3 projetos reais na estrutura | CONTEXT | Auto | PASS | exit 0; `README.md:176-210` |
| 5 | (e) `BookDetailPage`/`BookDetailPageModel` so em roadmap | CONTEXT | Auto | PASS | exit 0; `grep -c` = 0 para ambos os nomes de arquivo |
| 6 | (f) Build aponta o csproj (sem `-f` bare); `dotnet test` presente | CONTEXT | Auto | PASS | exit 0; comandos rodados de verdade — iOS compila, Android documenta o pre-requisito |
| 7 | (g) Modelos de Dados com `TranslationCache` + `BookTranslationJob` + `OriginalHash` | CONTEXT | Auto | PASS | exit 0 |
| 8 | (h)+(i) `.idea/` fora da arvore; temas Light/Dark/Sepia | CONTEXT | Auto | PASS | exit 0 |
| 9 | Badges D-2026-07-29-readme-2: 6 badges, URL real e resolvivel, ordem locked | CONTEXT | Auto | PASS | exit 0; 6/6 GET 200 re-sondados nesta rodada |
| 10 | D-2026-07-29-readme-4: 4 secoes novas (Seguranca, Testes+90%, Contributing/JDI, Licenca) | CONTEXT | Auto | PASS | exit 0; as 4 presentes, e agora factualmente corretas |

**Totals:** 10 items | Auto: 10 (10 PASS, 0 FAIL) | Manual: 0 pending

Registro para o dod-critic: estes mesmos 10 comandos davam 10/10 PASS **na iteracao 1, com os 3
blockers presentes**. Gate 8 nao mudou de estado porque nao e capaz de medir o que mudou. O que
fecha esta phase e a re-verificacao factual acima, nao a linha PASS do gate 8 (W-8).

## Recommendation

**Aprovado com warnings — pode seguir para `/jdi-ship`.** Os 3 blockers estao resolvidos com
evidencia verificada de forma independente, o W-1 foi corrigido junto e melhor do que o pedido, e
nada de novo classe-blocker apareceu. Os gates 1-4 continuam verdes e a baseline de 169+2 testes
esta preservada.

Antes do merge, se quiser gastar mais uma passada barata no `README.md` (tudo prosa, nenhuma linha
de codigo), a ordem de retorno e:

1. **W-10** — a coluna Disparo. Uma linha: ou tirar "mais cron semanal proprio" do CodeQL, ou
   acrescentar o cron nas linhas de Semgrep/SCA/Secret scan/SBOM. E o unico ponto do documento que
   hoje pode induzir conclusao errada sobre a CI.
2. **W-5** e **W-6** — duas contradicoes internas de uma linha cada (3 vs 4 plataformas; preambulo
   absoluto do Roadmap).
3. **W-11** — readmitir, como frase propria e precisa, o claim de encoding do WebView, que e
   verdadeiro e verificado.
4. A lacuna de "como usar": um paragrafo curto de primeiro uso (importar EPUB -> baixar o modelo
   GGUF -> traduzir), que e a maior melhoria de didatica restante.

Fora do escopo desta phase, e o item mais importante que ela produziu: **promover o achado de
zip-slip a phase propria** (`/jdi-add-phase`), com as duas entregas — o containment/bound no codigo
**e** a correcao da regra de Semgrep, que hoje tem cobertura estruturalmente zero sobre o caminho de
extracao real deste codebase. Deixar isso em `.jdi/todos.md`, que por definicao nunca vira phase
sozinho, subestima o achado.

W-3, W-4, W-7, W-8 e W-9 seguem como backlog de manutencao — nenhum segura o merge.
