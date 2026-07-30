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
  hoje; abstrair sem 2a implementacao real seria YAGNI).

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
