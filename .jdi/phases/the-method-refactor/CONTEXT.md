# Phase 13: Refactor The Method + memoria/CPU mobile — Context (slug: the-method-refactor)

Gerado em modo `auto` via `/jdi-issue` (dispatch `mode=auto dod=auto_only`, brief = card colado
2026-07-30, sem interacao — decisoes justificadas pelo asker com evidencia de codigo lida nesta
sessao). Texto completo de cada decisao em `.jdi/DECISIONS.md`.

## Goal
Eliminar violacoes concretas de The Method (CLAUDE.md) e de `.claude/rules/csharp.md`,
finding-driven (nao rewrite amplo) — cada mudanca justificada por violacao nomeada (file:line +
regra) e protegida pela rede de 192 testes de `regression-suite` (mergeada em `main`, PR #10).

## Locked decisions
- D-...-1: origem (card), estatuto finding-driven, depende de `regression-suite` em `main`.
- D-...-2: (A) escopo = so `src/TranslateReader.Core` (espelha D-...-regression-suite-2; app
  MAUI sem rede de testes = sem prova). (B) medicao: opcao (a) — so mudanca conforme-por-
  inspecao, sem BenchmarkDotNet/dotnet-counters (infra fica em `todos.md`, fora desta fase).
- D-...-3 (achado #1, fecha): `ReadingManager.ExtractImagesIfNeededAsync` (linhas 53-54) chama
  `Directory.Exists`/`GetFileSystemEntries` direto, pulando `IFileUtility` (viola camada
  CLAUDE.md). Fix: `IFileUtility.DirectoryHasContent(string)` novo + `ReadingManager` roteia por
  ele. Fecha D-...-regression-suite-5(1) (branch vira testavel com mock). EXCLUI linhas 59-60
  (write path, vetor de zip-slip — propriedade de `epub-zip-slip`, posicao 11 pendente).
- D-...-4 (achado #2, fecha, novo nesta sessao): `TranslationManager.cs:304-341` tem 4 metodos
  privados de manipulacao HTML por regex que duplicam a responsabilidade ja atribuida a
  `HtmlUtility` pelo CLAUDE.md (o Manager ja chama `HtmlUtility.ExtractBodyContent` no mesmo
  arquivo). Fix: mover `ExtractParagraphs`/`ExtractTextBlocks`/`ReplaceTextBlocksInHtml`/
  `StripHtmlTags` (+ 3 `[GeneratedRegex]`) para `HtmlUtility` como `public static`. MOVE puro,
  protegido por 48 chamadas indiretas ja em `TranslationManagerTests.cs`.
- D-...-5 (achado #3, fecha, novo nesta sessao): `ParsingEngine.cs` usa `Regex.Replace/Match/
  IsMatch` inline em caminho por-capitulo — 7 ocorrencias (`UpdateOpfTitleAsync` L126;
  `InlineCssLinks` L196/199/202; `RewriteImagePaths` L228/232/236). Viola csharp.md §2.1
  (regex compile-time-known -> `[GeneratedRegex]`; `TranslationManager` ja segue essa
  convencao). Fix: classe vira `partial`, os 7 padroes viram `[GeneratedRegex]`. Inspection-
  provable. Protegido por `ParsingEngineTests.cs` (9 refs, fixtures EPUB reais).
- D-...-6 (deferido): `TranslationEngine` acopla `LLamaWeights`/`StatelessExecutor` concretos
  (D-...-regression-suite-5(2)) — NAO e violacao hoje (Engine e o seam correto de The Method
  para tech de terceiro); abrir interface-seam sem 2a implementacao real seria YAGNI. Vai para
  `llm-mobile` (posicao 6, pendente), que vai precisar trocar backend por plataforma.

## Canonical refs
- Card via `/jdi-issue`, mesma origem de D-2026-07-30-regression-suite-1 (sem URL de tracker).
- `CLAUDE.md` § The Method (camadas + tabela componentes); `.claude/rules/csharp.md` §1, §2.1, §6.
- `.jdi/phases/regression-suite/{CONTEXT,REVIEW}.md` — rede protetora (192 attrs, 196p/2s/198t).
- Lido nesta sessao: `ReadingManager.cs`, `FileUtility.cs`, `IFileUtility.cs`,
  `TranslationEngine.cs`, `TranslationManager.cs`, `LibraryManager.cs`, `SettingsManager.cs`,
  `ParsingEngine.cs`, `HtmlUtility.cs` + testes correspondentes. Total atual do repo: 192 attrs.

## Out of scope
- App MAUI (`Pages`, `PageModels`, `Platforms`, `Utilities/*Converter.cs`, `MauiProgram.cs`,
  `AppShell.xaml.cs`) — sem rede de testes.
- Zip-slip/zip-bomb (`ReadingManager.cs:59-60`, `FileUtility.cs:31-32`) — de `epub-zip-slip`.
- Interface-seam do `TranslationEngine`/LLamaSharp — deferido a `llm-mobile`.
- Infra de medicao (BenchmarkDotNet etc.) — nao criada nesta fase.
- Qualquer arquivo do Core sem violacao nomeada nesta sessao — auditoria nao e exaustiva.

## Definition of Done

### Auto-verifiable
- [ ] Achado #1: `IFileUtility.DirectoryHasContent` existe e `ReadingManager` roteia por ele
      em vez de `Directory.Exists`/`GetFileSystemEntries` diretos
      **Verify:** `grep -q "bool DirectoryHasContent" src/TranslateReader.Core/Contracts/Utilities/IFileUtility.cs && grep -q "DirectoryHasContent" src/TranslateReader.Core/Utilities/FileUtility.cs && test $(grep -cE "Directory\.(Exists|GetFileSystemEntries)" src/TranslateReader.Core/Business/Managers/ReadingManager.cs) -eq 0 && grep -q "fileUtility.DirectoryHasContent" src/TranslateReader.Core/Business/Managers/ReadingManager.cs`
      **Source:** CONTEXT
- [ ] Gap D-2026-07-30-regression-suite-5(1) fechado: teste mockado (sem I/O) do branch "ja
      extraido" em `ReadingManagerTests.cs` + caso real (temp dir) em `FileUtilityTests.cs`
      **Verify:** `grep -q "DirectoryHasContent" test/TranslateReader.Tests/ReadingManagerTests.cs && test $(grep -cE "\[Fact\]|\[Theory\]" test/TranslateReader.Tests/ReadingManagerTests.cs) -ge 8 && grep -q "DirectoryHasContent" test/TranslateReader.Tests/FileUtilityTests.cs`
      **Source:** CONTEXT
- [ ] Achado #2: os 4 metodos de manipulacao HTML saem privados de `TranslationManager` e
      passam a existir publicos/estaticos em `HtmlUtility`
      **Verify:** `test $(grep -cE "private static.*(ExtractParagraphs|ExtractTextBlocks|ReplaceTextBlocksInHtml|StripHtmlTags)\(" src/TranslateReader.Core/Business/Managers/TranslationManager.cs) -eq 0 && test $(grep -cE "public static.*(ExtractParagraphs|ExtractTextBlocks|ReplaceTextBlocksInHtml|StripHtmlTags)\(" src/TranslateReader.Core/Utilities/HtmlUtility.cs) -eq 4`
      **Source:** CONTEXT
- [ ] Achado #3: `ParsingEngine` vira `partial class`, os 7 padroes regex inline viram
      `[GeneratedRegex]` com pattern e `RegexOptions` byte-identicos aos inline originais e
      ligados ao mesmo nome de metodo, zero `Regex.Replace/Match/IsMatch` estaticos restantes e
      nenhuma das 7 factories orfa — cada nome com declaracao unica E >= 1 call site VIVO
      (comentario nao conta como chamada) casado por TOKEN EXATO (nome lookalike PREFIXADO,
      tipo `MyStylesheetRelRegex()`, nao conta como chamada de `StylesheetRelRegex`)
      **Verify:** `F=src/TranslateReader.Core/Business/Engines/ParsingEngine.cs; test $(grep -cE "Regex\.(Replace|Match|IsMatch)\(" $F) -eq 0 && grep -q "public partial class ParsingEngine" $F && test $(grep -c "\[GeneratedRegex" $F) -eq 7 && test $(grep -cE "(OpfTitle|LinkTag|StylesheetRel|StylesheetHref|ImgSrc|SvgImageXlinkHref|SvgImageHref)Regex\(\)" $F) -eq 14 && test $(awk 'BEGIN{split("OpfTitle LinkTag StylesheetRel StylesheetHref ImgSrc SvgImageXlinkHref SvgImageHref",N," ")} {l=$0; if(b){i=index(l,"*/"); if(i){l=substr(l,i+2); b=0} else next} sub(/\/\/.*/,"",l); while(i=index(l,"/*")){r=substr(l,i+2); j=index(r,"*/"); if(j){l=substr(l,1,i-1) substr(r,j+2)} else {l=substr(l,1,i-1); b=1; break}} for(n=1;n<=7;n++){t=N[n] "Regex()"; h=0; o=0; while((p=index(substr(l,o+1),t))>0){p+=o; if(p==1 || substr(l,p-1,1) !~ /[A-Za-z0-9_]/){h=1; break} o=p} if(h){if(index(l,"partial Regex " t)) d[n]++; else c[n]++}}} END{k=0; for(n=1;n<=7;n++) if(d[n]==1 && c[n]>=1) k++; print k}' $F) -eq 7 && test $(grep -A1 -F '[GeneratedRegex(@"(<dc:title[^>]*>)(.*?)(</dc:title>)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]' $F | grep -cF 'partial Regex OpfTitleRegex()') -eq 1 && test $(grep -A1 -F '[GeneratedRegex(@"<link\b([^>]*?)/?>", RegexOptions.IgnoreCase)]' $F | grep -cF 'partial Regex LinkTagRegex()') -eq 1 && test $(grep -A1 -F '[GeneratedRegex(@"\brel\s*=\s*""stylesheet""", RegexOptions.IgnoreCase)]' $F | grep -cF 'partial Regex StylesheetRelRegex()') -eq 1 && test $(grep -A1 -F '[GeneratedRegex(@"\bhref\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase)]' $F | grep -cF 'partial Regex StylesheetHrefRegex()') -eq 1 && test $(grep -A1 -F '[GeneratedRegex(@"(<img\b[^>]*?\bsrc\s*=\s*"")([^""]+)("")", RegexOptions.IgnoreCase)]' $F | grep -cF 'partial Regex ImgSrcRegex()') -eq 1 && test $(grep -A1 -F '[GeneratedRegex(@"(<image\b[^>]*?\bxlink:href\s*=\s*"")([^""]+)("")", RegexOptions.IgnoreCase)]' $F | grep -cF 'partial Regex SvgImageXlinkHrefRegex()') -eq 1 && test $(grep -A1 -F '[GeneratedRegex(@"(<image\b[^>]*?\bhref\s*=\s*"")([^""]+)("")", RegexOptions.IgnoreCase)]' $F | grep -cF 'partial Regex SvgImageHrefRegex()') -eq 1`
      **Source:** CONTEXT — `Verify:` SUPERSEDED tres vezes, sempre por contra-exemplo EXECUTADO
      (REVIEW.md): por D-2026-07-30-the-method-refactor-7 (a 1a versao contava
      ocorrencias e saia `exit 0` com pattern corrompido e `IgnoreCase` removido); por
      D-2026-07-30-the-method-refactor-8 (a 2a media a contagem AGREGADA `-eq 14` e saia `exit 0`
      com regex orfanado de forma COMPENSADA — call site `ParsingEngine.cs:196` trocado por nome
      lookalike; a checagem passou a ser POR NOME e a descartar comentario); e por
      D-2026-07-30-the-method-refactor-9 (a 3a casava o nome por SUBSTRING no `index()` do AWK,
      entao `MyStylesheetRelRegex()` no call site mantinha `StylesheetRelRegex` "chamado" e saia
      `exit 0` — W-2/E5 da REVIEW iter 3; agora o casamento exige fronteira de identificador).
      Prova de comportamento pareada: `test/TranslateReader.Tests/ParsingEngineRegexTests.cs`
- [ ] Guardrail agregado: zero diff em `src/TranslateReader/` (app MAUI), nenhum pacote
      BenchmarkDotNet declarado em QUALQUER csproj/props/targets/packages.config do repo
      (inclusive `test/`), contagem total de `[Fact]`/`[Theory]` VIVOS (nao comentados) nao
      regride do baseline 192
      **Verify:** `test $(git diff --name-only $(git merge-base main HEAD) -- src/TranslateReader/ | wc -l) -eq 0 && test $(find . -type d \( -name bin -o -name obj -o -name .git \) -prune -o -type f \( -name "*.csproj" -o -name "*.props" -o -name "*.targets" -o -name "packages.config" \) -exec grep -l "BenchmarkDotNet" {} \; | wc -l) -eq 0 && test $(awk 'FNR==1{b=0} {l=$0; if(b){i=index(l,"*/"); if(i){l=substr(l,i+2); b=0} else next} sub(/\/\/.*/,"",l); while(i=index(l,"/*")){r=substr(l,i+2); j=index(r,"*/"); if(j){l=substr(l,1,i-1) substr(r,j+2)} else {l=substr(l,1,i-1); b=1; break}} while(match(l,/\[Fact\]|\[Theory\]/)){k++; l=substr(l,RSTART+RLENGTH)}} END{print k+0}' $(find test/TranslateReader.Tests -name "*.cs")) -ge 193`
      **Source:** CONTEXT — `Verify:` SUPERSEDED por D-2026-07-30-the-method-refactor-8 (a versao
      original buscava pacote so em `find src`, e BenchmarkDotNet no csproj de TESTE saia
      `exit 0`; e contava atributo por TEXTO, entao 25 `[Fact]` comentados mantinham a medida em
      214 com 189 ativos — ambos contra-exemplos executados pelo DoD critic em REVIEW.md)

### Manual
- _(none — dod=auto_only; itens humanos foram para `## Deferred to PR review`)_

## Deferred to PR review
- Leitura humana: o MOVE do achado #2 manteve nomes/estilo coerentes com `HtmlUtility`
  existente (`ExtractBodyContent`, `InjectTags`) — julgamento subjetivo, grep nao mede.
- Validar se o raciocinio de deferimento do `TranslationEngine` (D-...-6, YAGNI ate `llm-mobile`
  precisar de 2a implementacao) segue valido quando aquela fase comecar de fato.
- Confirmar que a auditoria (3 achados fechados, 2 deferidos) nao deixou passar violacao obvia
  adicional no Core — fase e finding-driven por escopo, nao varredura exaustiva certificada.

## Notes
- Ordem sugerida: (1) achado #1 (menor raio, fecha gap de teste) -> (2) achado #3 (mecanico) ->
  (3) achado #2 (move maior) -> (4) guardrail agregado como ultimo commit.
- Todos os 3 achados sao MOVE/ROUTE, nao reescrita de logica — `regression-suite` prova
  comportamento inalterado apos cada commit atomico (1 achado = 1 commit).
- `IFileUtility` ja tem 6 metodos; o 7o (`DirectoryHasContent`) passa do "3-5 ideal" de
  CLAUDE.md mas continua 1 contrato so (dentro do "max 2 por servico") — aceitavel.
