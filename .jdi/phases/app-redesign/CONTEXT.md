# Phase 21: Redesign visual do app (Library, Reader, Settings) — Context (slug: app-redesign)

Gerado em modo `auto` (`mode=auto dod=auto_only`), brief = card do usuario ("recreie todo o design a
partir de `design/`, replique design/animacao/funcionalidades, nao quebre o app, valide desktop E
mobile") + `design/DESIGN-REFERENCE.md` e os 9 screenshots de `design/screenshots/`. Sem interacao
humana: os 5 gaps de funcionalidade levantados no brief foram decididos aqui, cada um com a razao
registrada. Tres achados de codigo NAO previstos pelo card mudaram as decisoes e estao provados por
leitura direta nesta sessao: `Book.LastOpenedAt` e coluna morta, o test project nao alcanca o
projeto MAUI, e o TFM `net10.0-android` nao e compilado por nenhum lugar do repo hoje.

## Goal
Pages, controls e temas de Library/Reader/Settings/TranslatePopup seguem fielmente layout, paleta,
tipografia, componentes e animacoes dos mockups em `design/`, sem quebrar funcionalidade existente
em desktop e mobile.

## Locked decisions
- **D-...-1** (criacao da phase): origem = mockups em `design/`, card colado via `/jdi-issue`.
- **D-...-2** (frame de escopo): mockup e fonte de verdade de estrutura E visual; lista FECHADA de
  arquivos que podem mudar (4 XAML de Pages/Controls + 2 PageModels + AppShell/App/Styles +
  `MauiProgram` se preciso; no Core so `BookSummary`, `ILibraryManager`, `ITranslationManager`,
  `LibraryManager`, `TranslationManager`, `TranslationModelStatus` novo; mais testes e `ci.yml`).
  Fora: `Engines/`, `Access/`, os `.js` do WebView, BookDetailPage, bookmarks, busca no livro, i18n.
- **D-...-3** (cores): novo `Resources/Styles/DesignTokens.xaml` com os hexes exatos do mockup;
  chrome do app vira DARK-ONLY (`UserAppTheme = AppTheme.Dark`) porque os mockups so existem em dark
  e inventar paleta clara e proibido pelo card. **O tema de LEITURA (Claro/Escuro/Sepia, gerado por
  `ThemeEngine` e aplicado dentro do WebView) NAO e afetado e nao pode ser tocado.**
- **D-...-4** (gap 1, TOC): ACEITO, Client Layer puro — `LoadChaptersAsync` ja existe e
  `ReaderPageModel.Chapters` ja esta carregado; so entra `GoToChapterAsync(int index)` reusando o
  `LoadCurrentChapterAsync` privado. Painel INLINE a esquerda (~250px, empurra o conteudo). Gatilho
  (hamburguer) so no idiom Desktop: `mobile-reader.jpg` nao tem hamburguer.
- **D-...-5** (gap 2, "Recentes"): ACEITO COM ESCOPO REDUZIDO — filtro dentro da propria
  `LibraryPage`, nunca rota/pagina nova. Fonte = `ReadingProgress.UpdatedAt` (escrito de verdade a
  cada `SaveProgressAsync`), NAO `Book.LastOpenedAt` (grep completo: coluna nunca escrita por
  producao, uma tela ordenada por ela mentiria). A mesma consulta alimenta o hero "CONTINUE LENDO".
- **D-...-6** (gap 3, toggle grid/list): REJEITADO — decorativo no proprio prototipo, sem screenshot
  de list view pra replicar, e botao morto na UI real e defeito. Delta visual consciente.
- **D-...-7** (gap 4, busca): ACEITO — `ListBookSummariesAsync(string query = "")` (param opcional,
  baseline intacta), `ListRecentBookSummariesAsync()`, e `BookSummary` ganha `LastReadAt` +
  `TotalChapters`. Filtro inline no Manager, seguindo o precedente do `SearchBooksAsync` existente.
  Sem debounce.
- **D-...-8** (gap 5, Settings): ACEITO com UM UNICO `SettingsOverlay.xaml` — so as propriedades de
  layout do `Border` externo viram `OnIdiom` (Desktop = painel direito altura cheia; default =
  bottom sheet de hoje). Zero duplicacao de controle/handlers. Bloco de modelos + atribuicao Tencent
  (D-2026-08-01-hy-mt-translation-model-3) PRESERVADO, mesmo nao aparecendo no mockup.
- **D-...-9** (card de modelo + chip de idioma): `GetSelectedModelStatusAsync()` novo em
  `ITranslationManager` (reusa `ResolveModel` + `IModelAccess.IsModelAvailable(fileName)` da phase
  anterior) — hardcodar "Gemma 2 2B - 1.6 GB" viraria mentira ao trocar de modelo. Chip de idioma =
  `ReadingSettings.TargetLanguage` via `ISettingsManager` no `LibraryPageModel`, SO exibicao.
- **D-...-10** (como se prova que nao quebrou): o test project e `net10.0` e referencia so o Core —
  nada de `src/TranslateReader` tem teste/cobertura possivel hoje. Entao (a) toda logica testavel
  nova vai pro Core; (b) o wiring do app e verificado por `DesignSystemTests.cs`, que le os XAML do
  disco no mesmo padrao de `HybridWebViewContractTests`; (c) `ci.yml` ganha job `Build (Android)`,
  porque hoje nenhum lugar do repo compila `net10.0-android`.

## Canonical refs
- `design/DESIGN-REFERENCE.md` + `design/screenshots/*.jpg` (9 estados) — fonte de verdade
- `.jdi/decisions/D-2026-08-02-app-redesign-1..10.md`
- `src/TranslateReader/Pages/{LibraryPage,ReaderPage}.xaml(.cs)`,
  `Pages/Controls/{SettingsOverlay,TranslateBookPopup}.xaml(.cs)`,
  `PageModels/{LibraryPageModel,ReaderPageModel}.cs`, `AppShell.xaml`, `App.xaml(.cs)`
- `src/TranslateReader.Core/Contracts/Managers/{ILibraryManager,IReadingManager,ITranslationManager}.cs`,
  `Business/Managers/{LibraryManager,TranslationManager}.cs`, `Models/BookSummary.cs`
- `test/TranslateReader.Tests/HybridWebViewContractTests.cs` (padrao de teste que le arquivo do disco)
- `src/TranslateReader/TranslateReader.csproj` (condicoes de `TargetFrameworks`),
  `.github/workflows/{ci.yml,sca.yml}` (`maui-android` ja usado em sca.yml)
- `CLAUDE.md` (The Method, D-3), `.claude/rules/csharp.md` §1/§2.4/§3/§6/§7

## Out of scope
- List view + toggle grid/list — D-...-6, precisa de mockup novo.
- "Recentes" como rota/pagina + write path de `Book.LastOpenedAt` — D-...-5.
- TOC no mobile — D-...-4.
- Retirar `ILibraryManager.SearchBooksAsync` (redundante depois desta phase) — D-...-7.
- Split de `ITranslationManager` (9 operacoes) — D-...-9.
- Harness de teste que alcance o projeto MAUI, i18n, tema claro da chrome — D-...-10/D-...-3.
- Tudo em `Engines/`, `Access/` e nos `.js` do WebView — D-...-2.
Registrados em `.jdi/todos/2026-08-02-app-redesign.md`.

## Definition of Done

> `dod=auto_only`: todo item carrega `Verify:` executavel, no padrao ja endurecido desta base
> (`hy-mt-translation-model`, `translated-epub-images`): `DOTNET_CLI_UI_LANGUAGE=en` (sumario local
> sai em pt-BR), `grep -q "Passed!"` + piso numerico via `awk` (nunca so o exit code do
> `dotnet test`, que sai 0 com filtro casando zero teste). Logs em `TestResults/` (`.gitignore:18`).
> Comandos em bash (Git Bash no Windows). Nomes de elemento/teste sao PRESCRITOS aqui — e o que
> torna o item verificavel.

### Auto-verifiable
- [ ] Tokens do mockup centralizados e reskin real (nao cosmetico): `DesignTokens.xaml` existe com a
      paleta exata, esta merged em `App.xaml`, a chrome e dark-only, e NENHUM hex de chrome legado
      sobra em `Pages/` ou `AppShell.xaml` (inclusive os `Color.FromArgb` do `SettingsOverlay.xaml.cs`)
      **Verify:** `T=src/TranslateReader/Resources/Styles/DesignTokens.xaml; test -f "$T" && for c in 161826 232532 E9E9ED 9184D9 A7A1DB F3F5FE 292B31 F5F4FF 2B2741; do grep -qi "#$c" "$T" || exit 1; done && grep -q "DesignTokens.xaml" src/TranslateReader/App.xaml && grep -qE "UserAppTheme" src/TranslateReader/App.xaml src/TranslateReader/App.xaml.cs && test "$(grep -rEio -- '#2563EB|#60A5FA|#8B6914|#1A1A2E|#2A2A3E|#1A1A1A|#E4E4E7|#F0F0F0|#E0E0E0|#333333|#666666|#999999' src/TranslateReader/Pages src/TranslateReader/AppShell.xaml | wc -l)" -eq 0`
      **Source:** CONTEXT (D-...-3)

- [ ] Compila nos dois alvos do card: Windows sempre (0 erros) e Android — localmente quando o TFM
      resolve, e obrigatoriamente no CI (job novo com `maui-android`), fechando o buraco de hoje em
      que nada no repo compila `net10.0-android`
      **Verify:** `mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0 > TestResults/dod2-win.log 2>&1 && grep -qE "^ *0 Error\(s\)" TestResults/dod2-win.log && grep -q "net10.0-android" .github/workflows/ci.yml && grep -q "maui-android" .github/workflows/ci.yml && TFMS=$(DOTNET_CLI_UI_LANGUAGE=en dotnet msbuild src/TranslateReader/TranslateReader.csproj -getProperty:TargetFrameworks 2>/dev/null | tr -d '\r') && case "$TFMS" in *android*) DOTNET_CLI_UI_LANGUAGE=en dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-android > TestResults/dod2-droid.log 2>&1 && grep -qE "^ *0 Error\(s\)" TestResults/dod2-droid.log ;; *) echo "SKIP android local: TFM nao resolve nesta maquina (sem Android SDK) - coberto pelo job do ci.yml" ;; esac`
      **Source:** CONTEXT (D-...-10)

- [ ] Biblioteca tem a estrutura do mockup e nada dela e decorativo: sidebar por idiom, contagem de
      livros, busca ligada ao Core, hero "CONTINUE LENDO", filtro Biblioteca/Recentes, chip de
      idioma e card de status do modelo — e nenhuma rota nova foi criada (prova de D-...-5)
      **Verify:** `X=src/TranslateReader/Pages/LibraryPage.xaml; M=src/TranslateReader/PageModels/LibraryPageModel.cs; for n in SidebarPanel BookCountLabel SearchEntry ContinueReadingHero RecentFilterButton TargetLanguageChip ModelStatusCard; do grep -q "x:Name=\"$n\"" "$X" || exit 1; done && grep -q "OnIdiom" "$X" && test "$(grep -c '<ShellContent' src/TranslateReader/AppShell.xaml)" -eq 1 && grep -q "ISettingsManager" "$M" && grep -q "ListBookSummariesAsync" "$M" && grep -q "SearchQuery" "$M" && grep -q "ListRecentBookSummariesAsync" "$M" && grep -q "GetSelectedModelStatusAsync" "$M" && test "$(grep -rEio -- 'Gemma 2 2B|1\.6 GB' "$X" | wc -l)" -eq 0`
      **Source:** CONTEXT (D-...-5, D-...-6, D-...-7, D-...-9)

- [ ] Reader ganha navegacao por capitulo de verdade: painel de capitulos ligado a `Chapters`,
      comando de salto no PageModel, gatilho por idiom, e nenhuma operacao nova em Manager/Engine/
      Access (prova de que D-...-4 foi Client Layer puro)
      **Verify:** `X=src/TranslateReader/Pages/ReaderPage.xaml; M=src/TranslateReader/PageModels/ReaderPageModel.cs; grep -q 'x:Name="ChaptersPanel"' "$X" && grep -q 'x:Name="ChaptersCollection"' "$X" && grep -q 'x:Name="TocButton"' "$X" && grep -q "OnIdiom" "$X" && grep -qE "GoToChapter(Async)?" "$M" && grep -q "IsTocVisible" "$M" && BASE=$(git merge-base origin/main HEAD) && test -z "$(git diff --name-only "$BASE" -- src/TranslateReader.Core/Contracts/Managers/IReadingManager.cs src/TranslateReader.Core/Business/Managers/ReadingManager.cs)"`
      **Source:** CONTEXT (D-...-4)

- [ ] Settings e popup: UM unico `SettingsOverlay` com branch de layout por idiom (nenhum controle
      irmao duplicado), todos os `x:Name` e handlers de hoje preservados, atribuicao Tencent
      preservada, e o popup Traduzir com metadados do livro + banner de offline do mockup
      **Verify:** `S=src/TranslateReader/Pages/Controls/SettingsOverlay.xaml; test ! -f src/TranslateReader/Pages/Controls/SettingsPanel.xaml && test ! -f src/TranslateReader/Pages/Controls/SettingsSheet.xaml && grep -q "OnIdiom" "$S" && for n in LightThemeButton DarkThemeButton SepiaThemeButton ScrollModeButton PaginatedModeButton FontPicker FontSizeSlider LineSpacingSlider LetterSpacingSlider WordSpacingSlider SourceLanguagePicker TargetLanguagePicker GemmaModelButton QwenModelButton PhiModelButton HyMtModelButton ModelStatusLabel DeleteModelButton; do grep -q "x:Name=\"$n\"" "$S" || exit 1; done && grep -qi "Powered by Tencent HY" "$S" && P=src/TranslateReader/Pages/Controls/TranslateBookPopup.xaml && grep -q 'x:Name="BookMetaLabel"' "$P" && grep -q 'x:Name="OfflineBanner"' "$P" && grep -qi "offline" "$P" && grep -qE "public TranslateBookPopup\(BookSummary" src/TranslateReader/Pages/Controls/TranslateBookPopup.xaml.cs`
      **Source:** CONTEXT (D-...-8, D-...-9)

- [ ] Animacao real (o card pede "animacao"), nao flip de `IsVisible`: os overlays de TOC e Settings
      entram/saem com API de animacao do MAUI
      **Verify:** `R=src/TranslateReader/Pages/ReaderPage.xaml.cs; S=src/TranslateReader/Pages/Controls/SettingsOverlay.xaml.cs; grep -qE "TranslateTo|FadeTo" "$R" && grep -qE "TranslateTo|FadeTo" "$S"`
      **Source:** CONTEXT (card; D-...-4, D-...-8)

- [ ] O Core novo funciona de verdade, provado por 8 testes novos nomeados (5 em
      `LibraryManagerTests`: query vazia = comportamento de hoje, query filtra titulo/autor
      ignorando caixa, projecao de `LastReadAt`/`TotalChapters`, recentes ordenados desc, recentes
      excluem livro sem progresso; 3 em `TranslationManagerTests`: status reflete a settings, marca
      baixado quando o arquivo existe, cai pro gemma em nome desconhecido)
      **Verify:** `L=test/TranslateReader.Tests/LibraryManagerTests.cs; T=test/TranslateReader.Tests/TranslationManagerTests.cs; for n in ListBookSummariesAsync_WithoutQuery_ReturnsEveryBook ListBookSummariesAsync_WithQuery_FiltersByTitleOrAuthorIgnoringCase ListBookSummariesAsync_ProjectsLastReadAtAndTotalChapters ListRecentBookSummariesAsync_OrdersByLastReadDescending ListRecentBookSummariesAsync_ExcludesBooksWithoutReadingProgress; do grep -q "$n" "$L" || exit 1; done && for n in GetSelectedModelStatusAsync_ReturnsTheModelSelectedInSettings GetSelectedModelStatusAsync_ReportsDownloadedWhenTheFileExists GetSelectedModelStatusAsync_FallsBackToGemmaForAnUnregisteredName; do grep -q "$n" "$T" || exit 1; done && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~ListBookSummariesAsync|FullyQualifiedName~ListRecentBookSummariesAsync|FullyQualifiedName~GetSelectedModelStatusAsync" > TestResults/dod7.log 2>&1 && grep -q "Passed!" TestResults/dod7.log && awk -v n=9 '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (k&&f+0==0&&p+0>=n)?0:1}' TestResults/dod7.log`
      **Source:** CONTEXT (D-...-5, D-...-7, D-...-9)

- [ ] O wiring do app MAUI (que nenhum teste de unidade alcanca) esta integro: 8 testes estruturais
      novos em `DesignSystemTests.cs` leem os XAML do disco e provam tokens, ausencia de hex legado,
      todo handler de XAML existente no code-behind, toda raiz de binding resolvendo a um membro
      real, estrutura de Library/Reader, branch de idiom do Settings e uso de animacao
      **Verify:** `D=test/TranslateReader.Tests/DesignSystemTests.cs; test -f "$D" && for n in DesignTokens_ExposeTheMockupPalette RedesignedXaml_HasNoLegacyChromeHex EveryXamlEventHandler_ExistsInTheCodeBehind EveryPageBinding_ResolvesToAKnownMember LibraryPage_HasTheMockupStructure ReaderPage_HasTheChapterNavigationPanel SettingsOverlay_BranchesLayoutByIdiomWithoutDuplicatingTheControl Overlays_UseAnimatedTransitions; do grep -q "$n" "$D" || exit 1; done && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~DesignSystemTests" > TestResults/dod8.log 2>&1 && grep -q "Passed!" TestResults/dod8.log && awk -v n=8 '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (k&&f+0==0&&p+0>=n)?0:1}' TestResults/dod8.log`
      **Source:** CONTEXT (D-...-10)

- [ ] Suite inteira sem regressao E cobertura >= 90% nas duas classes do Core alteradas (D-2/D-6):
      `Failed: 0`, piso `Total >= B+16` (`B` = `[Fact]`+`[InlineData]` de `origin/main` calculado no
      proprio comando; `+16` = 8 do Core + 8 estruturais), `Skipped <= S` de `origin/main`, soma
      coerente, nenhum nome de teste publico de `origin/main` ausente no HEAD, e `line-rate >= 0.90`
      em `LibraryManager` e `TranslationManager` no cobertura gerado
      **Verify:** `mkdir -p TestResults && rm -rf TestResults/cov && BASE=$(git merge-base origin/main HEAD) && B=$(( $(git grep -cE '^[[:space:]]*\[Fact' "$BASE" -- 'test/TranslateReader.Tests/*.cs' | awk -F: '{s+=$NF} END{print s+0}') + $(git grep -cE '^[[:space:]]*\[InlineData' "$BASE" -- 'test/TranslateReader.Tests/*.cs' | awk -F: '{s+=$NF} END{print s+0}') )) && S=$(git grep -cE 'Skip[[:space:]]*=' "$BASE" -- 'test/TranslateReader.Tests/*.cs' | awk -F: '{s+=$NF} END{print s+0}') && test "$B" -gt 0 && git grep -hoE 'public (async Task|void) [A-Za-z0-9_]+\(' "$BASE" -- 'test/TranslateReader.Tests/*.cs' | sed -E 's/^public (async Task|void) //; s/\($//' | sort -u > TestResults/dod9-base.txt && git grep -hoE 'public (async Task|void) [A-Za-z0-9_]+\(' -- 'test/TranslateReader.Tests/*.cs' | sed -E 's/^public (async Task|void) //; s/\($//' | sort -u > TestResults/dod9-head.txt && test -s TestResults/dod9-base.txt && test -z "$(comm -23 TestResults/dod9-base.txt TestResults/dod9-head.txt)" && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --collect:"XPlat Code Coverage" --results-directory TestResults/cov > TestResults/dod9.log 2>&1 && grep -q "Passed!" TestResults/dod9.log && awk -v tn=$((B+16)) -v sn="$S" '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1);if($i=="Skipped:")s=$(i+1);if($i=="Total:")t=$(i+1)}} END{exit (k&&f+0==0&&t+0>=tn&&s+0<=sn+0&&p+0+s+0+f+0==t+0)?0:1}' TestResults/dod9.log && F=$(find TestResults/cov -name coverage.cobertura.xml | head -1) && test -n "$F" && for c in TranslateReader.Business.Managers.LibraryManager TranslateReader.Business.Managers.TranslationManager; do R=$(grep -o "<class name=\"$c\"[^>]*>" "$F" | head -1 | sed -E 's/.*line-rate="([0-9.]+)".*/\1/'); test -n "$R" && awk -v r="$R" 'BEGIN{exit (r+0>=0.90)?0:1}' || exit 1; done`
      **Source:** CONTEXT (D-...-10; PROJECT.md/D-2/D-6)

- [ ] Escopo de diff fechado (prova de que um reskin da superficie inteira nao vazou pro motor):
      `Business/Engines/`, `Access/`, `Utilities/` do Core e `Resources/Raw/` (JS do WebView)
      INTOCADOS; no Core so os 6 arquivos permitidos por D-...-2 mudam
      **Verify:** `BASE=$(git merge-base origin/main HEAD) && test -z "$(git diff --name-only "$BASE" -- src/TranslateReader.Core/Business/Engines/ src/TranslateReader.Core/Access/ src/TranslateReader.Core/Utilities/ src/TranslateReader/Resources/Raw/)" && test -z "$(git diff --name-only "$BASE" -- src/TranslateReader.Core/ ':(exclude)src/TranslateReader.Core/Models/BookSummary.cs' ':(exclude)src/TranslateReader.Core/Models/TranslationModelStatus.cs' ':(exclude)src/TranslateReader.Core/Contracts/Managers/ILibraryManager.cs' ':(exclude)src/TranslateReader.Core/Contracts/Managers/ITranslationManager.cs' ':(exclude)src/TranslateReader.Core/Business/Managers/LibraryManager.cs' ':(exclude)src/TranslateReader.Core/Business/Managers/TranslationManager.cs')"`
      **Source:** CONTEXT (D-...-2)

### Manual
- _(none — `dod=auto_only`; os criterios humanos foram para `## Deferred to PR review`)_

## Deferred to PR review
- **Paridade visual real com os mockups** (pixel, ritmo de espacamento, peso tipografico, gradiente
  das capas sem imagem, "esta bonito") — o humano compara contra `design/screenshots/*.jpg` e os
  dois HTML originais. Nenhum gate automatico julga isso.
- **Fluidez das animacoes** (duracao, easing, ausencia de flicker no abrir/fechar do TOC, do painel
  de Settings e do popup) — o DoD so prova que ha animacao, nao que ela esta boa.
- **Smoke path em device/emulador real**: Library -> abrir livro -> Reader -> TOC -> Settings ->
  trocar tema/idioma -> voltar, em Windows E Android. Nao ha harness de UI no repo (D-...-10) e o
  gate de Android e compilacao, nao execucao.
- **Delta consciente vs o screenshot**: o toggle grid/list nao foi portado (D-...-6) e o mobile nao
  tem TOC (D-...-4). Se o dono quiser qualquer um dos dois, precisa de mockup/decisao nova.
- **Chrome dark-only** (D-...-3): usuarios em tema claro do SO passam a ver o app sempre escuro. E a
  leitura fiel do mockup, mas e mudanca de comportamento visivel — confirmar que e o desejado.
- **SonarCloud sem issue nova** nos arquivos tocados — so existe apos push + CI.

## Notes
- **Nomes prescritos** (nao existem hoje; sao o que torna o DoD verificavel). `LibraryPage.xaml`:
  `SidebarPanel`, `BookCountLabel`, `SearchEntry`, `ContinueReadingHero`, `RecentFilterButton`,
  `TargetLanguageChip`, `ModelStatusCard` (+ `BooksCollection`, que ja existe).
  `LibraryPageModel`: propriedade `SearchQuery`. `ReaderPage.xaml`: `ChaptersPanel`,
  `ChaptersCollection`, `TocButton`; `ReaderPageModel`: `IsTocVisible` + `GoToChapterAsync`.
  `TranslateBookPopup.xaml`: `BookMetaLabel`, `OfflineBanner`; construtor passa a receber
  `BookSummary` (precisa de `TotalChapters`, ver D-...-7).
- **Os 16 testes novos sao `[Fact]` simples** (sem `[Theory]`/`[InlineData]`), pra que o piso
  `B+16` case exatamente com o contador do comando do DoD 9.
- **`DesignSystemTests` — regras que evitam falso-positivo/falso-negativo:** binding resolve a raiz
  antes do primeiro `.` tanto de `{Binding X` quanto de `Path=X`; considera as convencoes do
  CommunityToolkit (`_books` -> `Books`, `OpenBookAsync` -> `OpenBookCommand`); e resolve contra a
  UNIAO dos membros de `PageModel` + `BookSummary` + `Chapter` (em vez de tentar parsear escopo de
  `DataTemplate`, que seria fragil) — o objetivo e pegar typo/binding orfao, nao tipar XAML.
  Hex permitidos por serem CONTEUDO do design, nao chrome legada: `#FFFFFF`/`#F4ECD8`/`#5B4636`
  (amostras dos temas de leitura) e `#E53E3E` (acao destrutiva, a paleta do mockup nao define
  vermelho — nao inventar um).
- **Auto-teste do asker:** nenhum dos 16 nomes de teste, nenhum dos `x:Name` prescritos e nenhum
  `GoToChapter`/`ListRecentBookSummariesAsync`/`GetSelectedModelStatusAsync` existe no repo neste
  momento (confirmado por leitura direta dos 4 XAML, dos 2 PageModels, dos 3 contratos e dos 2
  arquivos de teste nesta sessao) — prova de que nenhum item do DoD passa vazio antes da execucao.
- **Ordem sugerida pro planner:** tokens/tema primeiro (todo o resto depende deles), depois Core
  (`BookSummary` + 2 Managers + testes, unica parte com dependencia de compilacao pro app), depois
  as 4 superficies XAML em paralelo, e `DesignSystemTests` + job de Android por ultimo.
