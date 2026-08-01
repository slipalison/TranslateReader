# Phase 18: Imagem quebrada em livro traduzido — Context (slug: translated-epub-images)

Gerado em modo `auto` via `/jdi-issue` (dispatch `mode=auto dod=auto_only`, brief = card colado pelo
usuario 2026-08-01, sem interacao — decisoes tomadas e justificadas pelo asker). Este asker rodou
sem acesso a shell (ver `D-2026-08-01-translated-epub-images-8`) — os pisos numericos do DoD vem de
contagem estatica, nao de execucao real; ver aviso no item 5 e em `## Notes`.

## Goal
O EPUB traduzido nasce com as imagens quebradas — corrigir a geracao, provar que nenhuma URL do app
vaza para dentro do artefato exportado, e decidir explicitamente o que fazer com os livros ja
gerados quebrados.

## Locked decisions
- **D-...-1** (ja registrada): causa raiz medida em `main`/`9e07c83` — `ParsingEngine.ReplaceImageRef`
  grava `https://epub-images/{bookDir}/{path}` (URL do virtual host do WebView) porque
  `TranslationManager` chama `ExtractChapterContentAsync(..., imagesDirectory: string.Empty)` nos 3
  call sites de traducao; `bookDir` fica vazio e a URL malformada e escrita dentro do EPUB por
  `CreateTranslatedEpubAsync`.
- **D-...-2** (causa raiz reconfirmada no HEAD atual + forma da correcao): reconfirmado por leitura
  de codigo desta sessao, linhas inalteradas desde `D-...-1`. ACHADO NOVO: `ReplaceImageRef`
  (`ParsingEngine.cs:258`) devolve o `src` inalterado quando ja comeca com `http`/`https` — uma vez
  gravada, a URL malformada NUNCA se autocorrige, nem reabrindo o livro com `imagesDir` correto
  (explica por que o defeito e permanente, e por que a correcao tem de acontecer na GERACAO).
  Forma da correcao LOCKED: novo enum `ChapterContentPurpose { Display, Export }`
  (`Models/ChapterContentPurpose.cs`) vira parametro OBRIGATORIO (sem default) de
  `IParsingEngine.ExtractChapterContentAsync`. Rejeitados: (a) metodo novo dedicado — `IParsingEngine`
  ja tem 6 operacoes, legado ja registrado, uma 7a pioraria a violacao do "3-5 ideal" do CLAUDE.md;
  (b) reverter a reescrita depois de persistir — reintroduz a mesma classe de guarda de idempotencia
  silenciosa, mais risco, nenhum ganho; (c) `bool` cru — boolean-trap nos 4 call sites, enum
  documenta intencao sem crescer a contagem de operacoes do contrato (permanece 6).
- **D-...-3** (comportamento por purpose, fecha TAMBEM o ponto de vista 1 do card sobre
  `InlineCssLinks`): `Purpose.Export` devolve `item.Content` sem chamar `RewriteImagePaths` NEM
  `InlineCssLinks` — nenhuma mutacao. `Purpose.Display` mantem o comportamento atual + guarda nova:
  `imagesDirectory` vazio com `Purpose.Display` lanca `InvalidOperationException` (consistente com
  as 2 guardas ja existentes no mesmo metodo, `ParsingEngine.cs:50-53`). Como
  `RebuildAllTranslatedChaptersAsync` passa o `html` INTEIRO (nao so o body) para
  `HtmlUtility.ReplaceTextBlocksInHtml`, que so troca texto dentro de `<p>`/`<div>` e devolve o
  resto intocado, pular as duas mutacoes em `Purpose.Export` fecha `<img>` E `<link
  rel="stylesheet">` na MESMA correcao — o `<link>` original continua apontando pro CSS real dentro
  do proprio zip, nunca sobrescrito por `CreateTranslatedEpubAsync`. Nenhuma mudanca em
  `RewriteImagePaths`/`InlineCssLinks`/`ReplaceImageRef`/`FindImage`/`FindCss` internamente.
- **D-...-4** (os 4 call sites, decididos individualmente): `ReadingManager.LoadChapterContentAsync`
  (`ReadingManager.cs:30`) -> `Purpose.Display` (unico call site de exibicao do app, `imagesDir` ja
  correto). `TranslationManager.TranslateSingleChapterAsync` (linha 123),
  `RebuildAllTranslatedChaptersAsync` (linha 193 — o call site da causa raiz) e
  `TranslateChapterAsync` (linha 242) -> `Purpose.Export` nos 3 (confirmado por leitura: os 3 so
  extraem TEXTO do `html`, nunca o exibem nem o persistem como esta). Nenhum dos 3 muda o argumento
  `imagesDirectory` (continua `string.Empty`, irrelevante em `Purpose.Export`). Diff de producao
  fechado: `Models/ChapterContentPurpose.cs` (novo), `Contracts/Engines/IParsingEngine.cs`,
  `Business/Engines/ParsingEngine.cs`, `Business/Managers/TranslationManager.cs`,
  `Business/Managers/ReadingManager.cs` — nenhum outro arquivo de producao muda.
- **D-...-5** (ponto de vista 2 do card — capa: PARCIALMENTE REFUTADO): a miniatura da biblioteca
  (`BookSummary.CoverImagePath`) NAO sofre deste defeito — `ExtractCoverImageAsync`
  (`ParsingEngine.cs:68-79`) le bytes CRUS de imagem do manifesto (`epub.CoverImage` /
  `Content.Cover` / `FindCoverInManifest`), nunca passa por `ExtractChapterContentAsync`. PORÉM: se
  o EPUB tiver uma pagina XHTML de capa no spine (evidencia real: o fixture Practice tem uma entrada
  de capitulo com "cover" no href — `ParsingEngineTests.cs:75-85`), essa pagina sofre o MESMO root
  cause de -2/-3 ao ser LIDA para leitura, e fica corrigida pela MESMA correcao — nenhuma logica
  especial de "capa" adicionada.
- **D-...-6** (pontos de vista 4 e 6 do card — CONFIRMADOS, DELIBERADAMENTE fora de escopo): os 3
  regexes de imagem (`ParsingEngine.cs:352-359`) so casam atributo com aspas duplas (aspas
  simples/sem aspas/`srcset`/`<picture><source>`/`background-image` inline nunca sao reescritos);
  `FindImage`/`FindCss` nao decodificam `%XX`. Ambos confirmados por leitura de codigo, ambos
  ORTOGONAIS ao vazamento de URL do app (nenhum dos dois aciona `ReplaceImageRef`, entao nenhum pode
  produzir `https://epub-images/...` no artefato) — afetam LEITURA no app, nao exportacao, e
  independem de traducao. Fora de escopo: card e sobre livros traduzidos, nenhum fixture real
  exercita essas formas (YAGNI), correcao exige escopo de robustez maior. Registrado em
  `.jdi/todos/2026-08-01-translated-epub-images.md`.
- **D-...-7** (ponto de vista 5 do card — livros ja gerados e quebrados, decisao explicita): NENHUMA
  ferramenta de migracao/reparo automatico nesta fase (YAGNI). O EPUB traduzido quebrado e artefato
  DERIVADO e descartavel; o livro ORIGINAL nunca e mutado (`CreateTranslatedEpubAsync` faz
  `File.Copy` pra uma COPIA antes de escrever, `ParsingEngine.cs:91`); `TranslationCache` (chave
  `BookId`+`ChapterHRef`+hash) continua valida pro livro original, entao retraduzir DEPOIS da
  correcao regenera rapido (cache quente). `DeleteBookAsync` ja existe para remover a copia quebrada.
  Acao esperada do usuario (produto/UX) -> `## Deferred to PR review`.
- **D-...-8** (limite de ambiente desta sessao — origem do piso numerico do DoD): sessao sem shell.
  Piso fixado por contagem ESTATICA de `[Fact]`/`[Theory]` via grep: **304** ocorrencias em 24
  arquivos (medido nesta sessao). Piso seguro (contagem de atributo <= contagem de casos em
  runtime, nunca o contrario). Doer/reviewer devem capturar o `Total:` real de uma corrida limpa
  antes de implementar e usar esse numero se for MAIOR que 304 (ratchet so sobe).

## Canonical refs
- `.jdi/DECISIONS.md` D-2026-08-01-translated-epub-images-1 (diagnostico medido original)
- `src/TranslateReader/MauiProgram.cs:42-57` (mapeamento do virtual host `epub-images` ->
  `AppDataDirectory/books/images`)
- `src/TranslateReader.Core/Business/Engines/ParsingEngine.cs` (`ExtractChapterContentAsync`,
  `RewriteImagePaths`, `ReplaceImageRef`, `InlineCssLinks`, `ExtractCoverImageAsync`,
  `CreateTranslatedEpubAsync`)
- `src/TranslateReader.Core/Business/Managers/{TranslationManager,ReadingManager,LibraryManager}.cs`
- `src/TranslateReader/PageModels/LibraryPageModel.cs:170-181` (fluxo de traducao ->
  `ImportBookAsync` do EPUB traduzido)
- `.claude/rules/csharp.md` §1 (fail fast/excecoes), §4 (EPUB e input nao confiavel, WebView bridge
  minima), §6 (bugfix comeca vermelho, 90% em codigo alterado, sem I/O em teste NOVO — precedente
  de I/O autorizado para fixture de EPUB ja existe em `ParsingEngineTests.cs`)
- `.jdi/phases/conversion-performance/CONTEXT.md` — fixtures reais e `IParsingEngine` como
  restricao (contagem de operacoes, lazy-switch de `ExtractAllImagesAsync`)
- `.jdi/phases/div-paragraph-translation/CONTEXT.md` — dona de `TranslateBookAsync`/
  `ExtractTextBlocks`/`BookTranslationResult`, estilo de `Verify:` endurecido (`DOTNET_CLI_UI_LANGUAGE=en`,
  piso de `Passed:`/`Total:`, `TestResults/`)

## Out of scope
- Cobertura de forma dos regexes de imagem (aspas simples/sem aspas/`srcset`/`<picture>`/
  `background-image`) — D-...-6(a), `.jdi/todos/2026-08-01-translated-epub-images.md`.
- Normalizacao percent-encoded em `FindImage`/`FindCss` — D-...-6(b), mesmo todo.
- Ferramenta de migracao/reparo de livros ja traduzidos e quebrados — D-...-7, decisao explicita de
  nao construir.
- `CreateTranslatedEpubAsync`'s `FirstOrDefault` O(entries×capitulos) — ja julgado nao-dominante em
  `conversion-performance` (D-2026-07-31-conversion-performance-7), nao reaberto aqui.
- Lazy-switch de `ExtractChapterContentAsync`/`ReadEpubSafeAsync` (reparse do EPUB inteiro a cada
  chamada) — achado nomeado de `conversion-performance` (D-...-5b), fora do escopo deste bugfix.

## Definition of Done

> `dod=auto_only`: todo item carrega `Verify:` executavel. Segue o padrao endurecido desta base de
> codigo (`div-paragraph-translation` D-...-9, `conversion-performance` D-...-8): grep estrutural +
> `dotnet test --filter` real, `DOTNET_CLI_UI_LANGUAGE=en` obrigatorio (sumario local sai em pt-BR),
> `grep -q "Passed!"` + piso numerico parseado por `awk` (nunca so o exit code do `dotnet test`,
> que sai 0 mesmo quando o filtro casa ZERO teste). Logs em `TestResults/` (`.gitignore:18`).

### Auto-verifiable
- [ ] `Purpose.Export` devolve o capitulo BYTE-A-BYTE IDENTICO a entrada crua do EPUB (lida
      diretamente do zip, fora do pipeline), para TODOS os capitulos do fixture Practice — prova
      que nem `RewriteImagePaths` nem `InlineCssLinks` rodam em modo Export, sem depender de
      conhecer o conteudo exato do fixture
      **Verify:** `grep -q "Practice_ExtractChapterContentAsync_ForExport_MatchesRawZipEntryForEveryChapter" test/TranslateReader.Tests/ParsingEngineTests.cs && grep -q "ChapterContentPurpose.Export" test/TranslateReader.Tests/ParsingEngineTests.cs && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~Practice_ExtractChapterContentAsync_ForExport_MatchesRawZipEntryForEveryChapter" > TestResults/dod1.log 2>&1 && grep -q "Passed!" TestResults/dod1.log && awk -v n=1 '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (k&&f+0==0&&p+0>=n)?0:1}' TestResults/dod1.log`
      **Source:** CONTEXT (D-...-2, D-...-3)

- [ ] `Purpose.Display` com `imagesDirectory` vazio lanca `InvalidOperationException` (a causa raiz
      historica vira estado impossivel de alcancar) E `Purpose.Display` com diretorio real continua
      reescrevendo para `https://epub-images/...` como hoje (regressao do comportamento existente)
      **Verify:** `grep -q "Practice_ExtractChapterContentAsync_DisplayWithEmptyImagesDirectory_ThrowsInvalidOperationException" test/TranslateReader.Tests/ParsingEngineTests.cs && grep -q "ChapterContentPurpose.Display" src/TranslateReader.Core/Business/Engines/ParsingEngine.cs && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~Practice_ExtractChapterContentAsync_DisplayWithEmptyImagesDirectory_ThrowsInvalidOperationException|FullyQualifiedName~Practice_ExtractChapterContentAsync_RewritesImagePathsToVirtualHostUrl" > TestResults/dod2.log 2>&1 && grep -q "Passed!" TestResults/dod2.log && awk -v n=2 '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (k&&f+0==0&&p+0>=n)?0:1}' TestResults/dod2.log`
      **Source:** CONTEXT (D-...-3)

- [ ] Propriedade do ARTEFATO (nao so da funcao): um EPUB traduzido construido com o mesmo caminho
      de producao (capitulos extraidos em `Purpose.Export`, gravados via `CreateTranslatedEpubAsync`)
      nao tem NENHUMA entrada do zip (de nenhum tipo) contendo o literal `epub-images` (absoluto:
      medido 0 no EPUB-fonte) E nenhuma entrada GANHA `https://` — entrada a entrada por `FullName`,
      se o artefato a traz com `https://`, a MESMA entrada do original ja trazia (forma DIFERENCIAL
      por `D-2026-08-01-translated-epub-images-9(B)`: o fixture Practice tem 1 ocorrencia NATIVA de
      `https://` em `ops/styles/1266002537.css`, entao a forma absoluta reprovaria codigo correto) —
      a prova explicita que o card pede
      **Verify:** `grep -q "Practice_TranslatedEpubArtifact_ForExportedChapters_NoEntryContainsTheAppHost" test/TranslateReader.Tests/ParsingEngineTests.cs && grep -q "epub-images" test/TranslateReader.Tests/ParsingEngineTests.cs && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~Practice_TranslatedEpubArtifact_ForExportedChapters_NoEntryContainsTheAppHost" > TestResults/dod3.log 2>&1 && grep -q "Passed!" TestResults/dod3.log && awk -v n=1 '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (k&&f+0==0&&p+0>=n)?0:1}' TestResults/dod3.log`
      **Source:** CONTEXT (D-...-2, D-...-3, D-...-4)

- [ ] Wiring de producao: os 3 call sites de `TranslationManager` usam `Purpose.Export` (nenhum usa
      `Purpose.Display`), o unico call site de `ReadingManager` usa `Purpose.Display` (nenhum usa
      `Purpose.Export`) — estrutural E comportamental (verificacao de chamada via NSubstitute)
      **Verify:** `test "$(grep -c "ChapterContentPurpose.Export" src/TranslateReader.Core/Business/Managers/TranslationManager.cs)" -eq 3 && test "$(grep -c "ChapterContentPurpose.Display" src/TranslateReader.Core/Business/Managers/TranslationManager.cs)" -eq 0 && test "$(grep -c "ChapterContentPurpose.Display" src/TranslateReader.Core/Business/Managers/ReadingManager.cs)" -eq 1 && test "$(grep -c "ChapterContentPurpose.Export" src/TranslateReader.Core/Business/Managers/ReadingManager.cs)" -eq 0 && grep -q "ChapterContentPurpose.Display" test/TranslateReader.Tests/ReadingManagerTests.cs && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~TranslateBookAsync_UsesExportPurposeForCacheExtractionAndRebuild|FullyQualifiedName~TranslateChapterAsync_UsesExportPurposeToReadChapterHtml|FullyQualifiedName~LoadChapterContentAsync_ExtractsImagesThenParsesContent" > TestResults/dod4.log 2>&1 && grep -q "Passed!" TestResults/dod4.log && awk -v n=3 '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (k&&f+0==0&&p+0>=n)?0:1}' TestResults/dod4.log`
      **Source:** CONTEXT (D-...-4)

- [ ] Suite INTEIRA sem regressao: `Failed: 0`, `Total >= B+5` e `Skipped <= S` com `B` (`[Fact` +
      `[InlineData`) e `S` (`Skip =`) DERIVADOS de `main` no proprio comando, soma coerente
      (`Passed+Skipped+Failed == Total`), E nenhum nome de metodo publico de teste de `main` ausente
      no HEAD (`comm -23` vazio — contagem sozinha aceita stub sem assert e delecao compensada).
      Piso `304` de `D-...-8` REVOGADO por `D-2026-08-01-translated-epub-images-9(A)`: a corrida
      limpa real deu `Failed: 0, Passed: 336, Skipped: 2, Total: 338` (`B=338`, `S=2`, piso efetivo
      `Total >= 343`). Pre-requisito: ref local `main` == `origin/main`.
      **Verify:** `mkdir -p TestResults && B=$(git grep -hoE '\[(Fact|InlineData)' main -- 'test/TranslateReader.Tests/*.cs' | wc -l) && S=$(git grep -hoE 'Skip[[:space:]]*=' main -- 'test/TranslateReader.Tests/*.cs' | wc -l) && git grep -hoE 'public[[:space:]]+(async[[:space:]]+)?(Task|void)[[:space:]]+[A-Za-z0-9_]+' main -- 'test/TranslateReader.Tests/*.cs' | awk '{print $NF}' | sort -u > TestResults/names-main.txt && git grep -hoE 'public[[:space:]]+(async[[:space:]]+)?(Task|void)[[:space:]]+[A-Za-z0-9_]+' HEAD -- 'test/TranslateReader.Tests/*.cs' | awk '{print $NF}' | sort -u > TestResults/names-head.txt && test -z "$(comm -23 TestResults/names-main.txt TestResults/names-head.txt)" && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release > TestResults/dod5.log 2>&1 && grep -q "Passed!" TestResults/dod5.log && awk -v b="$B" -v s="$S" '/Passed!/{ok=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1);if($i=="Skipped:")k=$(i+1);if($i=="Total:")t=$(i+1)}} END{exit (ok && f+0==0 && t+0>=b+5 && k+0<=s+0 && p+0+k+0+f+0==t+0)?0:1}' TestResults/dod5.log`
      **Source:** CONTEXT (D-...-8, SUPERSEDIDA por D-...-9(A))

- [ ] Escopo de diff fechado: `src/TranslateReader/` (app MAUI) sem NENHUMA mudanca (nenhuma UI/
      migracao/reparo introduzida — reforca D-...-7); `src/TranslateReader.Core/` muda so nos 4
      arquivos previstos + o enum novo; o app inteiro continua compilando
      **Verify:** `test -z "$(git diff --name-only main -- src/TranslateReader/)" && test "$(git diff --name-only main -- src/TranslateReader.Core/ | sort | tr '\n' ',')" = "src/TranslateReader.Core/Business/Engines/ParsingEngine.cs,src/TranslateReader.Core/Business/Managers/ReadingManager.cs,src/TranslateReader.Core/Business/Managers/TranslationManager.cs,src/TranslateReader.Core/Contracts/Engines/IParsingEngine.cs,src/TranslateReader.Core/Models/ChapterContentPurpose.cs," && test -f src/TranslateReader.Core/Models/ChapterContentPurpose.cs && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0 > TestResults/dod6.log 2>&1 && grep -qE "^ *0 Error\(s\)" TestResults/dod6.log`
      **Source:** CONTEXT (D-...-4, D-...-7)

### Manual
- _(none — dod=auto_only; itens humanos foram para `## Deferred to PR review`)_

## Deferred to PR review
- Comunicacao ao usuario: livros ja traduzidos ANTES desta correcao continuam quebrados; o caminho
  e apagar da biblioteca e retraduzir (cache quente, rapido) — decisao de produto/UX de ONDE/COMO
  avisar, D-...-7.
- Confirmacao visual/funcional real (device ou WebView real) de que o livro traduzido abre e
  renderiza sem imagem quebrada — sem harness neste ambiente (espelha
  D-2026-07-30-regression-suite-2 / D-2026-07-31-conversion-performance-2).
- Confirmacao do SonarCloud sem issue nova nos arquivos tocados — so existe apos push+CI (mesmo
  limite ja documentado em `sonar-zero-issues`/`coverage-90`).
- Leitura humana: confirmar que os 6 pontos de vista pedidos pelo card foram endereçados com o
  rigor esperado (o card pediu explicitamente "mais pontos de vista").

## Notes
~~Piso numerico do DoD (itens 1-4 usam `n` pequeno fixo; item 5 usa 304)~~ — RESOLVIDO pelo doer em
`D-2026-08-01-translated-epub-images-9`, T-1: suite limpa medida (`Failed: 0, Passed: 336,
Skipped: 2, Total: 338`), piso do item 5 trocado por derivacao de `main` + `comm` nome a nome, e
item 3 trocado para a forma DIFERENCIAL de `https://` apos sondar o fixture (1 ocorrencia nativa em
`ops/styles/1266002537.css`, 0 de `epub-images`). Itens 1-4 e 6 seguem como estavam.

Nomes de teste prescritos nesta fase (ainda nao existem, serao criados pela wave de execucao):
`Practice_ExtractChapterContentAsync_ForExport_MatchesRawZipEntryForEveryChapter`,
`Practice_ExtractChapterContentAsync_DisplayWithEmptyImagesDirectory_ThrowsInvalidOperationException`,
`Practice_TranslatedEpubArtifact_ForExportedChapters_NoEntryContainsTheAppHost` (todos em
`ParsingEngineTests.cs`, reusando os helpers `ReadEntry`/`ReadOpf` ja existentes no arquivo);
`TranslateBookAsync_UsesExportPurposeForCacheExtractionAndRebuild`,
`TranslateChapterAsync_UsesExportPurposeToReadChapterHtml` (em `TranslationManagerTests.cs`, via
`_parsingEngine.Received(...).ExtractChapterContentAsync(Arg.Any<string>(), Arg.Any<string>(),
Arg.Any<string>(), ChapterContentPurpose.Export)`, seguindo o padrao `Received(1)` ja usado no
arquivo para `CreateTranslatedEpubAsync`). O teste ja existente
`Practice_ExtractChapterContentAsync_RewritesImagePathsToVirtualHostUrl` e as demais chamadas de
`ExtractChapterContentAsync` em `ParsingEngineTests.cs` (linhas 81, 94, 150, 224, 229, 240) e o teste
existente `LoadChapterContentAsync_ExtractsImagesThenParsesContent` (`ReadingManagerTests.cs`)
precisam do 4o argumento `ChapterContentPurpose.Display` adicionado ao call/setup — churn mecanico
esperado, nao e item de DoD por si, mas condicao para a suite compilar.

Auto-teste do asker: os 3 nomes de teste novos do item 1/2/3 NAO existem ainda no repo neste
momento — os `grep -q` desses nomes falham HOJE (confirmado por leitura direta dos arquivos citados
nesta sessao), prova de que os itens do DoD nao passam vazio antes da implementacao.
