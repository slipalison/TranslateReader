# Todos — scope creep registrado

Append-only. Itens fora do escopo de uma phase discutida, candidatos a phase futura ou acao
manual do usuario. Nunca vira phase automaticamente — precisa ser promovido via
`/jdi-add-phase`.

## De `ci-seguranca` (2026-07-28)

- Build + testes de Android/iOS no pipeline de CI — exigiria workload MAUI mobile instalado no
  runner (e possivelmente emulador/simulador). Nao pedido explicitamente pelo card.
- Assinatura e publicacao em lojas (Google Play Console, Apple App Store Connect/TestFlight) no
  workflow de release — exige certificados/secrets inexistentes hoje.
- `zizmor` (linter estatico de workflows do GitHub Actions) — reforco opcional de rigor, nao
  obrigatorio no card; considerar se quiser elevar ainda mais a regua de supply-chain.
- SonarQube self-hosted (servidor proprio) — descartado a favor do SonarQube Cloud
  (D-2026-07-28-ci-seguranca-6); revisitar so se o projeto ganhar backend/infra propria.

## De `readme` (2026-07-29)

- **[SEGURANCA] Extracao de imagem de EPUB nao tem containment de path (zip-slip) nem bound de
  tamanho descomprimido (zip-bomb).** Achado colateral do review da phase `readme` (B-1): o README
  afirmava esse controle, e a verificacao mostrou que ele nao existe. Nao e defeito de doc — e
  divida de codigo real contra `.claude/rules/csharp.md` secao 4 ("EPUB files are untrusted input.
  Extract zip entries defensively: reject entry paths that escape the target directory ... Bound
  decompressed sizes").

  Evidencia:
  - `src/TranslateReader.Core/Business/Managers/ReadingManager.cs:59-60` monta o caminho de saida
    direto da entrada nao confiavel:
    `Path.Combine(imagesDir, relativePath.Replace('/', Path.DirectorySeparatorChar))` seguido de
    `fileUtility.WriteFileAsync(outputPath, content)`. `relativePath` vem de
    `epub.Content.Images.Local` (`ParsingEngine.cs:62-64`), ou seja, dos caminhos internos do EPUB.
  - `src/TranslateReader.Core/Utilities/FileUtility.cs:31-32` escreve sem validar:
    `Directory.CreateDirectory(Path.GetDirectoryName(filePath)!)` +
    `File.WriteAllBytesAsync(filePath, content)`. Um EPUB com entrada `../../evil.png` escreve
    fora de `imagesDir`.
  - Nao ha `Path.GetFullPath` + checagem de containment em lugar nenhum de `src/` (grep por
    `GetFullPath|ExtractToFile|ExtractToDirectory|entry.FullName` -> zero resultados), nem
    qualquer limite de bytes por entrada ou por livro (grep por `maxSize|maxBytes|uncompressed|
    sizeLimit` -> zero resultados).
  - A regra de Semgrep `translatereader-zip-slip` **nao pega este caso**: ela casa
    `Path.Combine($DEST, $ENTRY.FullName)` e `$ENTRY.ExtractToFile(...)`, e o codigo real nao
    passa por nenhum dos dois. Ou seja, o gate de CI da falsa sensacao de cobertura aqui.
  - **CORRECAO DO DIAGNOSTICO (2026-07-29, verify round 3):** a redacao anterior dizia que a
    regra falha por causa de uma "variavel intermediaria (`relativePath`)" — isso e FRACO e
    enganoso. O reviewer provou com probe de 4 casos que a regra exige o **acesso sintatico a
    `.FullName`**; como o projeto extrai via VersOne.Epub e nunca toca `ZipArchiveEntry`, a regra
    nao pode disparar no caminho real em nenhuma forma que ele venha a assumir. Quem for
    consertar deve reescrever a regra para o padrao real, nao normalizar a variavel.
    Fonte canonica desta analise: `D-2026-07-29-epub-zip-slip-1` em `.jdi/DECISIONS.md`.
  - **Este item virou phase:** `epub-zip-slip` (posicao 11 no ROADMAP), com escopo de DUAS
    entregas — o containment/bound no codigo E a correcao da regra. Entregar so a primeira deixa
    o defeito invisivel para o CI na proxima regressao.

  Correcao esperada quando virar phase: resolver com `Path.GetFullPath` e exigir
  `StartsWith(imagesDir, StringComparison.Ordinal)` antes de escrever, rejeitando (nao sanitizando)
  o que escapar; somar um limite de bytes por entrada e por livro. Codigo legado (anterior a
  `4285f25`), entao D-2 isenta as phases atuais — mas seguranca e prioridade 1 do projeto e isso
  nao tem boundary de legado.

  **NOTA (2026-07-30, `the-method-refactor`):** a fase `the-method-refactor` toca
  `ReadingManager.cs`/`FileUtility.cs` para corrigir OUTRO achado (fronteira de camada — ver
  D-2026-07-30-the-method-refactor-3), mas explicitamente NAO mexe nas linhas 59-60/31-32 acima.
  `epub-zip-slip` continua dona exclusiva deste item.

- [UI/CODIGO] Controles que a UI promete e o codigo nao honra (achado adjacente do verify round 3
  da phase `readme` — mesma classe de defeito que a phase passou 3 rounds tirando do README, mas
  na tela):
  - **Model picker morto:** `TranslationModelName` e escrito por 3 botoes vivos no painel de
    configuracoes, mas nao e consumido por ninguem — `DownloadModelIfNeededAsync` usa
    `DefaultModel` incondicionalmente e `GetModelPath()` devolve o primeiro `*.gguf` por
    enumeracao cega de nome. O usuario escolhe um modelo e o app ignora.
  - **`IsModelAvailable` e flag de sessao:** escrito so em `ReaderPageModel.cs:275` e `:300`,
    nunca consulta o disco, e o PageModel e `AddTransient` — reabrir o app esconde o botao
    "Excluir modelo" mesmo com 1,6 GB em disco.
  - **Download de modelo nao e retomavel:** `ModelAccess` usa `FileMode.Create` (trunca) e nao
    manda header `Range`; um `.tmp` interrompido nao conta como disponivel, entao rebaixa os
    1,6 GB do zero.

## De `regression-suite` (2026-07-30)

- **[THE-METHOD] `ReadingManager.ExtractImagesIfNeededAsync` toca o filesystem real direto**
  (`Directory.Exists`/`Directory.GetFileSystemEntries`, `ReadingManager.cs:50-54`) em vez de
  passar por `IFileUtility`. Cheiro de violacao de fronteira de camada (CLAUDE.md: "Business
  Layer (Managers) -> Engines, ResourceAccess, Utilities", nunca Resources direto). Efeito
  colateral pratico: o branch "imagens ja extraidas, pula" fica sem teste na phase
  `regression-suite` porque cobri-lo exigiria I/O de disco real num teste novo, proibido por
  `.claude/rules/csharp.md` §6. Corrigir exige mover a checagem de existencia para
  `IFileUtility` (novo metodo na interface) — mudanca de seam de producao, candidata natural
  para `the-method-refactor`. So depois disso a rede de testes consegue caracterizar os dois
  branches sem violar a regra de isolamento. Ver `D-2026-07-30-regression-suite-5(1)`.
  **RESOLVIDO em `the-method-refactor`** — ver `D-2026-07-30-the-method-refactor-3`
  (`IFileUtility.DirectoryHasContent`).

- **[TESTABILIDADE] `TranslationEngine` acopla direto a tipos concretos do LLamaSharp**
  (`LLamaWeights`, `StatelessExecutor`, `TranslationEngine.cs:20-32,98-107`), sem interface-seam
  para substituir em teste. Hoje so 5 testes unitarios reais cobrem o file (140 linhas) — o
  resto e caminho de carregamento de modelo, exercitado so pelos 2 testes de integracao
  `[Fact(Skip=...)]` que exigem um `.gguf` real via `LLAMASHARP_TEST_MODEL`. Se
  `the-method-refactor` decidir abrir uma interface de fabrica em torno de `LLamaWeights`/
  `StatelessExecutor` (facilitaria tanto teste quanto troca de backend mobile — overlap com a
  phase `llm-mobile`), a rede desta fase nao caracteriza esse caminho hoje; qualquer mudanca ali
  precisa de revisao manual adicional. Ver `D-2026-07-30-regression-suite-5(2)`.
  **DEFERIDO para `llm-mobile`** — ver `D-2026-07-30-the-method-refactor-6` (nao e violacao
  hoje; abstrair sem 2a implementacao real seria YAGNI). **DEFERIDO NOVAMENTE em `coverage-90`**
  — ver `D-2026-07-31-coverage-90-4`: as 52 linhas descobertas de `TranslationEngine.cs`
  permanecem fora do escopo dessa fase tambem, plano de 90% de cobertura nao depende delas.

- **[PROCESSO/DoD] Grep de guardrail anti-multi-target e estreito demais — endurecer nas phases
  futuras.** O `Verify:` do item 5 do DoD desta phase (CONTEXT.md linha 98) e
  `test $(grep -c "net10.0-windows" test/TranslateReader.Tests/TranslateReader.Tests.csproj) -eq 0
  && test $(find test -name "*.csproj" | wc -l) -eq 1`. Ele passa, e o guardrail de fato foi
  honrado — mas o grep procura literalmente `net10.0-windows`, entao um multi-target hipotetico
  `<TargetFrameworks>net10.0;net10.0-android</TargetFrameworks>` (ou `-ios`, ou `-maccatalyst`)
  passaria batido. O que realmente provou a decisao (c) foi a INSPECAO: `<TargetFramework>` no
  singular, ausencia de `UseMaui`, exatamente 1 `.csproj` sob `test/`.
  **O DoD desta phase esta locked e nao deve ser editado** (a phase passou nele) — o item aqui e
  para quem escrever um guardrail equivalente numa phase futura: probe tambem
  `<TargetFrameworks` (plural) e `UseMaui`, nao so um TFM nomeado. Ex.:
  `grep -qE "<TargetFrameworks|UseMaui" test/**/*.csproj && exit 1`. Sem esse reforco o gate da
  falsa sensacao de cobertura, mesma classe de defeito da regra Semgrep `translatereader-zip-slip`
  registrada em `## De \`readme\``.
  **Aplicado em `the-method-refactor`**: os `Verify:` desta fase usam pares
  presenca-positiva/ausencia-negativa (ex.: zero ocorrencias de `Regex\.(Replace|Match|IsMatch)\(`
  E `>= 7` `[GeneratedRegex]`) em vez de um unico grep literal, para nao repetir a classe de
  defeito.

## De `the-method-refactor` (2026-07-30)

- **[PERF/INFRA] Infraestrutura de medicao (BenchmarkDotNet, `dotnet-counters`, `dotnet-gcdump`)
  nao existe no repo.** `.claude/rules/csharp.md` §2 exige "Measure before optimizing" para
  qualquer ganho de memoria/CPU DECLARADO; esta fase decidiu (D-2026-07-30-the-method-refactor-2,
  opcao a) nao criar essa infra agora e se limitar a mudancas de conformidade de regra
  prováveis por inspecao. Se uma fase futura quiser reivindicar ganho de memoria/bateria
  mensurado (nao so conformidade de regra), precisa desta infra primeiro — candidato a phase
  propria ou a sub-escopo de `llm-mobile` (onde o consumo de bateria/memoria em dispositivo real
  passa a importar de verdade).

- **[AUDITORIA] Esta fase e finding-driven e NAO exaustiva** (D-2026-07-30-the-method-refactor-1).
  3 achados de codigo foram fechados (ReadingManager/IFileUtility, TranslationManager/HtmlUtility,
  ParsingEngine/GeneratedRegex) e 2 foram deferidos com motivo nomeado (TranslationEngine/
  LLamaSharp -> `llm-mobile`; zip-slip -> `epub-zip-slip`). Isso NAO significa que nao existam
  outras violacoes de CLAUDE.md/`.claude/rules/csharp.md` no Core ou no app MAUI — apenas que
  nao foram auditadas nesta sessao (orcamento de contexto e escopo finding-driven, nao
  rewrite amplo). Uma varredura completa do app MAUI (fora da rede de testes, D-2026-07-30-
  regression-suite-2) continua um gap conhecido e deliberadamente aceito.

- **[TESTE] `ParsingEngine`: 6 dos 7 `[GeneratedRegex]` nao sao discriminados por nenhum teste.**
  Medido por mutacao na T-2 de `the-method-refactor` (evidencia completa na SUMMARY da fase).
  So `ImgSrcRegex` morde sozinho (quebra-lo derruba 2 testes); `SvgImageXlinkHrefRegex` e
  `SvgImageHrefRegex` so mordem JUNTOS (1 teste); e os 4 restantes — `OpfTitleRegex`,
  `LinkTagRegex`, `StylesheetRelRegex`, `StylesheetHrefRegex` — podem ser quebrados sem que a
  suite (203 casos) acuse nada. Consequencias: (1) o caminho inteiro de `InlineCssLinks` (inline
  de `<link rel="stylesheet">` em `<style>`) nao tem assercao nenhuma; (2) `UpdateOpfTitleAsync`
  nunca e executado por teste algum — `CreateTranslatedEpubAsync` so aparece MOCKADO em
  `TranslationManagerTests`. Fechar isso exige I/O de disco real (proibido por
  `.claude/rules/csharp.md` §6) ou um seam de producao — a API de `ParsingEngine` recebe path,
  nao stream. Mesmo motivo tecnico ja registrado na `regression-suite` (SUMMARY > Lacuna 4).
  Candidato: phase de teste de integracao com fixtures proprias, ou refactor da API para stream.
  **RESOLVIDO na iter 2 de `the-method-refactor`** (blocker do DoD critic) — o diagnostico acima
  errou ao concluir "so com I/O de disco ou seam novo": `ParsingEngineRegexTests.cs` alcanca os 7
  `[GeneratedRegex]` por reflection (`BindingFlags.NonPublic | Static`) e asserta COMPORTAMENTO de
  cada padrao (match/no-match/grupos/case), sem disco e sem mudar 1 byte de producao. Mutacao
  medida: corromper qualquer 1 dos 7 patterns derruba teste; remover `IgnoreCase` de qualquer 1
  dos 7 derruba exatamente 1 teste cada; remover `Singleline` de `OpfTitleRegex` derruba 1. Ver
  `D-2026-07-30-the-method-refactor-7`. **Residuo NAO coberto** (continua valendo): o wiring
  end-to-end de `InlineCssLinks`/`UpdateOpfTitleAsync` (o padrao certo chamado no lugar certo,
  sobre um EPUB real) segue sem assercao — isso sim exige fixture com I/O. O que morreu foi a
  classe de defeito "pattern/options corrompidos passam em silencio".

- **[PROCESSO/DoD] Ratchet do piso de atributos de teste: subir o limiar ao FECHAR cada phase, nao
  no meio dela.** O item 5 do DoD de `the-method-refactor` mede `[Fact]`/`[Theory]` VIVOS com piso
  `-ge 193` (baseline 192 + 1) enquanto a medida real ao fim da phase e **214** — 21 atributos de
  folga, janela em que uma regressao passaria o DoD 5 (o Gate 2 do reviewer, que compara 227
  aprovados / 229 totais, cobre essa janela hoje). Subir o piso e tentador, mas quem esta DENTRO da
  phase ja sabe que passa: apertar o proprio criterio no fim da corrida e movimento de trave, nao
  endurecimento — recusado na iter 4, ver `D-2026-07-30-the-method-refactor-9` (secao "Nao fechado
  nesta rodada"). O lugar certo e o `/jdi-ship`/`/jdi-discuss` da PROXIMA phase: ao abrir a phase
  seguinte, o piso do guardrail nasce igual a medida fechada da anterior (214 -> `-ge 215` na
  primeira mudanca de teste). Mesma classe do item `[PROCESSO/DoD]` de `regression-suite` acima:
  criterio de DoD se endurece na virada, com o numero ja publicado, nunca retroativamente.
  Residuos de MEDIDA conhecidos e deliberadamente mantidos: `[ Fact ]` com espacos nao conta
  (fail-closed, subconta, consistente com o baseline 192) e `"[Fact]"` em string literal conta
  (sobreconta, so alcancavel de proposito) — fechar qualquer um dos dois exige parsear C#.
  **AINDA nao aplicado em `coverage-90`**: esta fase nao toca a suite C# de forma que mude a
  contagem de `[Fact]`/`[Theory]` de modo relevante (so `ModelAccessTests.cs` ganha 1 metodo de
  teste novo) — o ratchet do piso continua pendente para a proxima phase que reescrever esse
  DoD item de forma material.

## De `sonar-zero-issues` (2026-07-30)

- **[CI/COBERTURA-DE-SCAN] O job Sonar nao compila o projeto App (`src/TranslateReader`) —
  "0 issues" e cego pro head MAUI.** `.github/workflows/sonarqube.yml` roda `dotnet build
  src/TranslateReader.Core/TranslateReader.Core.csproj -c Release` e `dotnet test
  test/TranslateReader.Tests/TranslateReader.Tests.csproj` entre o `begin`/`end` do
  `dotnet-sonarscanner` — nunca compila `src/TranslateReader.csproj` (o head MAUI, TFM
  `net10.0-windows10.0.19041.0`). O analisador C#/Roslyn do Sonar so enxerga o que e compilado
  nessa janela: `PageModels/*.cs`, `Pages/*.xaml.cs`, `Platforms/**/*.cs`,
  `Utilities/*Converter.cs` e `MauiProgram.cs`/`AppShell.xaml.cs` (as mesmas 1516 linhas que
  `D-2026-07-30-regression-suite-2` ja registrou como fora da rede de testes) sao
  ESTRUTURALMENTE invisiveis ao Sonar hoje — nao aparecem nas 113 issues do inventario porque
  nunca foram escaneadas, nao porque estao limpas. O mecanismo `sonar.qualitygate.wait=true`
  desta fase (D-2026-07-30-sonar-zero-issues-2/6) protege exatamente o que o job ja escaneia
  (Core C# + JS/HTML/CSS/PowerShell) — nao estende a cobertura de scan. Fechar isso exige um job
  Sonar novo em `windows-latest` com workload MAUI instalado (o job `build` do CI ja roda
  Windows por outro motivo — D-2026-07-28-ci-seguranca-5 — mas nunca rodou `dotnet-sonarscanner`
  em cima dele), somando tempo de execucao e complexidade real de CI — candidato a phase propria
  ou extensao de `pipeline-unificada`/`cobertura-e-ci`, nao decidido nem iniciado aqui. Ver
  `D-2026-07-30-sonar-zero-issues-6`.
  **Confirmado na rodada de warnings (iter 3):** segue exatamente como escrito acima — W-3(c) da
  REVIEW nao foi fechada e nao deve ser: criar job Sonar em `windows-latest` com workload MAUI e
  infraestrutura nova, ja declarada fora de escopo por `D-2026-07-30-sonar-zero-issues-6`.
  **AINDA nao fechado em `coverage-90`**: `sonar.javascript.lcov.reportPaths` (D-2026-07-31-
  coverage-90-2) fecha a lacuna de cobertura de JS dentro do job existente, mas NAO abre o job
  Windows/MAUI — este item continua aberto exatamente como escrito.

- **[CI/QUALITY-GATE] O "Sonar way" so mede New Code — dois cegos estruturais que nenhum comando
  deste repo fecha** (W-3(a) e W-3(b) da REVIEW iter 2 da phase `sonar-zero-issues`). (a) Issue
  levantada em linha LEGADA nao alterada nao entra em New Code: um upgrade de regra/analisador pode
  flagrar codigo velho e o gate segue verde. (b) Code smell de New Code abaixo do debt ratio do
  rating A (~5%) nao reprova: smells pequenos acumulam sem travar PR. **Por que nao foi fechado
  aqui:** as duas condicoes vivem na definicao do Quality Gate, que e configuracao do projeto no
  SonarCloud — FORA do repositorio, nao versionavel, nao alcancavel por commit nem por
  `Verify:`. Fechar exigiria criar um Quality Gate customizado na UI do SonarCloud (ex.: condicoes
  sobre Overall Code, nao so New Code) e apontar o projeto para ele — decisao de politica com custo
  real (todo o legado passaria a reprovar de uma vez, o que e exatamente o que `D-2` isenta), a ser
  tomada pelo dono do projeto, nao por uma phase de codigo. Ate la o mecanismo entregue
  (`sonar.qualitygate.wait=true`, `D-2026-07-30-sonar-zero-issues-2`) protege o que promete e so
  isso: **regressao em New Code do que o job compila**.
  **Reafirmado em `coverage-90`**: ver D-2026-07-31-coverage-90-7 — o mesmo limite explica por que
  nenhum `Verify:` desta fase usa o Quality Gate como prova da meta de 90% Overall.

- **[LEGADO/D-2] Achados legados do gate de seguranca/camada, nenhum tocado por esta phase, todos
  pre-existentes em `main`** (W-5 da REVIEW iter 2 — enumerados aqui porque ate agora so existiam
  no corpo da review): `catch { }` vazio em `src/TranslateReader/Pages/ReaderPage.xaml.cs:326` e
  `:434`; `catch (OperationCanceledException) { }` sem rethrow em
  `src/TranslateReader/PageModels/LibraryPageModel.cs:183`,
  `src/TranslateReader/PageModels/ReaderPageModel.cs:222` e `ReaderPage.xaml.cs:308` (conversao de
  cancelamento no boundary de UI — `.claude/rules/csharp.md` §1 diz que
  `OperationCanceledException` sempre flui); `static` mutavel em
  `src/TranslateReader.Core/Business/Engines/TranslationEngine.cs:16` (§2.4: statics sao
  `static readonly` e imutaveis); desequilibrio de eventos `+=`/`-=` = 5/4 no app (§2.4: "todo `+=`
  precisa de um `-=`"). Todos fora do diff da phase e cobertos por `D-2` (codigo anterior a
  `4285f25`). **Nao refatorados aqui de proposito:** seriam o "rewrite amplo" que o escopo das
  phases deste repo proibe, e todos ficam no app MAUI, que hoje esta fora da rede de testes
  (`D-2026-07-30-regression-suite-2`) — mexer sem rede e trocar um cheiro conhecido por um bug
  desconhecido. Candidatos naturais a uma phase de higiene do head MAUI, junto com a varredura
  completa ja registrada no item `[AUDITORIA]` de `the-method-refactor`.

## De `sonar-zero-issues` — pos-CI do PR #12 (2026-07-30)

- **[PROCESSO/WAIVER] `#pragma warning disable` NAO e waiver valido para regra de analisador
  externo no SonarCloud.** Provado no PR #12: o pragma em `HtmlUtility.cs:147-150` suprime
  `SYSLIB1044` no compilador (build local e do CI: 0 SYSLIB no Core), mas o importador
  `external_roslyn` le o diagnostico do log do MSBuild e ignora o estado de supressao — a issue
  seguia aberta no SonarCloud. So `sonar.issue.ignore.multicriteria` fecha do lado do Sonar.
  Regra geral para as proximas phases: um waiver so vale se for provado NO SISTEMA QUE LEVANTA a
  issue; provar no compilador e provar a coisa errada. Ver `D-2026-07-30-sonar-zero-issues-12`.
- **[PROCESSO/GATE] Gate local nao substitui a analise remota.** Os analisadores do SonarCloud
  (`csharpsquid`, `external_roslyn`, `javascript`, `Web`, `css`) NAO rodam em `dotnet build`. Dois
  achados desta phase so apareceram depois do push: o smell `S125` que o proprio refactor introduziu
  e as 2 `CA1826` dos testes novos do T-6. Qualquer phase futura que prometa "0 issues do Sonar"
  precisa contar com um ciclo de push+CI dentro do proprio escopo, nao so `Verify:` locais.
  **Aplicado em `coverage-90`**: ver D-2026-07-31-coverage-90-6 — nenhum `Verify:` desta fase
  alega provar "zero issue nova" localmente; a confirmacao real fica em `## Deferred to PR review`.

## De `coverage-90` (2026-07-31)

- **[FERRAMENTA] Lint estatico dedicado para o JS do WebView (eslint) nao entra nesta fase.**
  D-2026-07-31-coverage-90-1 travou o harness de teste JS em `node:test`+`node:vm` nativo
  (zero dependencia nova); adicionar `eslint` para os 4 scripts de producao + os novos
  `test/js/*.test.js` traria valor real (a analise `javascript` do SonarCloud so roda apos
  push+CI, per D-2026-07-30-sonar-zero-issues-6), mas exige `package.json` + config propria —
  contradiria a escolha deliberada de "zero dependencia nova" desta fase. Candidato a phase
  futura de qualidade de JS (ou sub-escopo de `pipeline-unificada`), junto com a decisao ja
  registrada de nao abrir um job Sonar Windows/MAUI (item `[CI/COBERTURA-DE-SCAN]` acima).
- **[CONTINGENCIA] `ParsingEngine.cs` (45 linhas descobertas, 71% hoje) fica fora do plano
  principal desta fase (D-2026-07-31-coverage-90-5) — reserva nomeada SE a soma real de JS +
  `ModelAccess` + `FileUtility`/`HtmlUtility` ficar abaixo das 187 linhas necessarias apos a
  primeira medicao local. Se acionada, usa o MESMO padrao de fixture `.epub` real ja estabelecido
  em `ParsingEngineTests.cs` (autorizado nomeadamente no PLAN de `sonar-zero-issues`, T-6) — nao
  inventa um terceiro padrao de excecao a `.claude/rules/csharp.md` §6.

- **[CODIGO/RECURSO] `ReadEpubSafeAsync` deixa o handle do zip aberto quando o fallback e
  acionado.** Achado colateral de `coverage-90` (T-7), reportado no SUMMARY e cobrado como work
  item pelo reviewer (Warning 1 da REVIEW iter 1). `src/TranslateReader.Core/Business/Engines/
  ParsingEngine.cs:138-190`: a leitura estrita (`:162`) lanca `EpubPackageException`, o `catch`
  (`:164`) refaz a leitura com `fallbackOptions` (`:188`) — e o descarte do `EpubBook`/arquivo da
  primeira tentativa fica inteiramente por conta do VersOne.Epub, que nao o fecha.
  Evidencia empirica: o teste novo precisou de um guard `catch (IOException)` no proprio `Dispose`
  para conseguir apagar o temp dir — `test/TranslateReader.Tests/ParsingEngineEdgeCaseTests.cs:
  47-52` ("VersOne.Epub keeps the archive handle open when the strict read throws
  EpubPackageException"). Consequencia real: em Windows o `.epub` segue TRAVADO depois que
  `ExtractCoverImageAsync`/`ParseBookAsync` retornam, entao renomear/apagar/reimportar o livro
  falha ate o GC rodar. Nao re-provado em processo isolado (o lock e interno a lib de terceiro) —
  confirmar antes de corrigir. Corrigir exige mexer em `src/`, o que `coverage-90` proibiu por
  decisao de escopo; contra `.claude/rules/csharp.md` §2.4 ("Dispose what you own"). Candidato a
  phase de higiene do Core ou a sub-escopo de `epub-zip-slip` (mesmo arquivo, mesmo caminho de
  leitura de zip).

- **[CODIGO/CONTRATO] `ExtractCoverImageAsync` devolve `byte[0]` em vez de `null` quando a capa do
  manifesto aponta para arquivo ausente.** Mesmo achado/mesma origem do item acima (SUMMARY de
  `coverage-90`, Warning 1 da REVIEW iter 1). `src/TranslateReader.Core/Business/Engines/
  ParsingEngine.cs:316` (`return imageFile?.Content;`) devolve o `Content` sem a guarda
  `Length > 0` que as DUAS fontes anteriores do mesmo metodo aplicam (`:72`
  `epub.CoverImage is { Length: > 0 }` e `:75` `epub.Content.Cover?.Content is { Length: > 0 }`).
  Como `ReadEpubSafeAsync` liga `IgnoreMissingFileError = true` (`:156` no caminho estrito, `:184`
  no fallback), o
  VersOne.Epub materializa um placeholder VAZIO para o item ausente — e ele escapa como `byte[0]`
  por esse terceiro caminho. O contrato e `Task<byte[]?>`: quem consome recebe "tem capa" e depois
  grava/renderiza 0 byte, em vez de cair no caminho de "sem capa". Comportamento hoje FIXADO por
  teste de caracterizacao (`ParsingEngineEdgeCaseTests.cs:185`
  `ExtractCoverImageAsync_WithACoverImagePropertyPointingAtAMissingFile_ReturnsNoBytes`), com a
  assercao deliberadamente frouxa `Assert.Empty(cover ?? [])` (`:196`) justamente para NAO
  cristalizar o defeito como contrato. Correcao esperada: aplicar a mesma guarda `Length > 0` no
  `:316`; ao fazer isso, apertar `:196` para exigir `Assert.Null(cover)` (Warning 3 da REVIEW).
  Uma linha de producao — nao foi feita aqui porque `coverage-90` fechou `src/` por decisao de
  escopo, nao por dificuldade.
