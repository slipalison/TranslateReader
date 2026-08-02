# Phase 21: Redesign visual do app (Library, Reader, Settings) — Plan  (slug: app-redesign)

## Goal
Pages, controls e temas de Library/Reader/Settings/TranslatePopup seguem fielmente layout, paleta,
tipografia, componentes e animacoes dos mockups em `design/`, sem quebrar funcionalidade existente
em desktop e mobile.

## Locked decisions (from CONTEXT.md)
- D-...-2: lista FECHADA de arquivos; no Core so os 6 permitidos, `Engines/`/`Access/`/`Utilities/`/`Resources/Raw/` INTOCADOS.
- D-...-3: `DesignTokens.xaml` novo + chrome DARK-ONLY; tema de LEITURA (`ThemeEngine`/WebView) nao pode ser tocado.
- D-...-4: TOC = Client Layer puro, painel inline ~250px, gatilho so no idiom Desktop.
- D-...-5 / D-...-7: "Recentes" e busca sao filtro dentro da `LibraryPage`; recencia = `ReadingProgress.UpdatedAt`, nunca `Book.LastOpenedAt`.
- D-...-6: toggle grid/list REJEITADO (delta visual consciente).
- D-...-8: UM unico `SettingsOverlay.xaml` com branch `OnIdiom`, zero duplicacao; bloco de modelos + atribuicao Tencent preservados.
- D-...-9: card de modelo vem de `GetSelectedModelStatusAsync()`, nunca hardcode.
- D-...-10: logica testavel no Core; wiring do MAUI provado por `DesignSystemTests.cs`; `ci.yml` ganha job Android.

## Frozen contracts (acordados aqui — permitem paralelismo sem negociacao entre doers)
- `BookSummary`: `+ DateTime? LastReadAt` (null = sem progresso), `+ int TotalChapters`.
- `ILibraryManager`: `ListBookSummariesAsync(string query = "")` (param OPCIONAL, nao quebra chamador atual) e `Task<IReadOnlyList<BookSummary>> ListRecentBookSummariesAsync()`.
- `TranslationModelStatus`: `record TranslationModelStatus(string Name, string FileName, long SizeBytes, bool IsDownloaded)`.
- `ITranslationManager`: `Task<TranslationModelStatus> GetSelectedModelStatusAsync()`.
- `SettingsOverlay`: `public Task ShowAsync()` / `public Task HideAsync()` (animados), consumidos por `ReaderPage.xaml.cs::SyncSettingsOverlay`.
- `TranslateBookPopup`: ctor vira `public TranslateBookPopup(BookSummary book)` (substitui `(string bookTitle)`), consumido pelos 2 call sites de `LibraryPageModel`.
- Hex permitidos como CONTEUDO (nao chrome legada): `#FFFFFF`, `#F4ECD8`, `#5B4636`, `#E53E3E`.

## Tasks

### Wave 1 (parallel-eligible)

#### T-1: Tokens do mockup + chrome dark-only
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader/Resources/Styles/DesignTokens.xaml` (novo), `src/TranslateReader/App.xaml`, `src/TranslateReader/App.xaml.cs`, `src/TranslateReader/AppShell.xaml`, `src/TranslateReader/Resources/Styles/Styles.xaml`, `src/TranslateReader/Resources/Styles/Colors.xaml`
- **Acceptance:**
  - `DesignTokens.xaml` traz os 9 hexes exigidos (`161826 232532 E9E9ED 9184D9 A7A1DB F3F5FE 292B31 F5F4FF 2B2741`) + escalas neutra/accent, radius e shadow de `DESIGN-REFERENCE.md`; merged em `App.xaml`; `UserAppTheme = AppTheme.Dark` fixado.
  - `AppShell.xaml` restilizado por chave de token e continua com **exatamente 1** `<ShellContent>` (prova de D-...-5: nenhuma rota nova).
  - Nenhum hex de chrome legado introduzido; `ThemeEngine` e o CSS de leitura nao sao tocados.
  - Build Windows Release com 0 erros.
- **Dependencies:** none
- **Test:** sem unit test possivel (ResourceDictionary) — coberto por `DesignTokens_ExposeTheMockupPalette` (T-8) + DoD 1/2
- **Status:** completed

#### T-2: Core — busca, recentes e projecao em `BookSummary`
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Models/BookSummary.cs`, `src/TranslateReader.Core/Contracts/Managers/ILibraryManager.cs`, `src/TranslateReader.Core/Business/Managers/LibraryManager.cs`, `test/TranslateReader.Tests/LibraryManagerTests.cs`
- **Acceptance:**
  - Contrato conforme "Frozen contracts"; filtro inline no Manager por titulo OU autor com `StringComparison.OrdinalIgnoreCase` (precedente do `SearchBooksAsync`); query vazia devolve exatamente o comportamento de hoje.
  - `LastReadAt` vem de `ReadingProgress.UpdatedAt`; recentes ordenados desc e livro sem progresso EXCLUIDO; `Book.LastOpenedAt` nao e lido nem escrito.
  - `ListBookSummariesAsync_ReturnsSummaryWithProgress` (legado) continua verde sem alteracao de nome.
  - Sem N+1 gratuito: `FetchAllBooksAsync` chamado uma vez por operacao.
  - `line-rate >= 0.90` em `TranslateReader.Business.Managers.LibraryManager`.
- **Dependencies:** none
- **Test:** 5 `[Fact]` novos (sem `[Theory]`/`[InlineData]`): `ListBookSummariesAsync_WithoutQuery_ReturnsEveryBook`, `ListBookSummariesAsync_WithQuery_FiltersByTitleOrAuthorIgnoringCase`, `ListBookSummariesAsync_ProjectsLastReadAtAndTotalChapters`, `ListRecentBookSummariesAsync_OrdersByLastReadDescending`, `ListRecentBookSummariesAsync_ExcludesBooksWithoutReadingProgress`
- **Status:** completed

#### T-3: Core — status do modelo selecionado
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Models/TranslationModelStatus.cs` (novo), `src/TranslateReader.Core/Contracts/Managers/ITranslationManager.cs`, `src/TranslateReader.Core/Business/Managers/TranslationManager.cs`, `test/TranslateReader.Tests/TranslationManagerTests.cs`
- **Acceptance:**
  - `GetSelectedModelStatusAsync()` reusa `ResolveModel` + `IModelAccess.IsModelAvailable(fileName)` ja existentes; nenhum registro de modelo novo, nenhuma URL nova.
  - Nome desconhecido/legado cai no Gemma (mesmo fallback de `ResolveModel`) e nunca lanca.
  - Os 44 testes existentes de `TranslationManagerTests` nao mudam de nome nem regridem (aprendizado `div-paragraph-reading`: piso por NOME, nao por contagem).
  - `line-rate >= 0.90` em `TranslateReader.Business.Managers.TranslationManager`.
- **Dependencies:** none
- **Test:** 3 `[Fact]` novos: `GetSelectedModelStatusAsync_ReturnsTheModelSelectedInSettings`, `GetSelectedModelStatusAsync_ReportsDownloadedWhenTheFileExists`, `GetSelectedModelStatusAsync_FallsBackToGemmaForAnUnregisteredName`
- **Status:** completed

### Wave 2 (parallel-eligible)

#### T-4: SettingsOverlay — layout por idiom + restyle + animacao
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader/Pages/Controls/SettingsOverlay.xaml`, `src/TranslateReader/Pages/Controls/SettingsOverlay.xaml.cs`
- **Acceptance:**
  - `OnIdiom` SO nas propriedades de layout do `Border` externo (Desktop = painel direito altura cheia; default = bottom sheet de hoje); NENHUM controle irmao duplicado e nenhum `SettingsPanel.xaml`/`SettingsSheet.xaml` criado.
  - Os 18 `x:Name` de hoje e os 3 eventos publicos (`CloseRequested`, `SettingsChanged`, `DeleteModelRequested`) preservados; atribuicao "Powered by Tencent HY" preservada.
  - `ShowAsync()`/`HideAsync()` animam com `TranslateTo`/`FadeTo` — nao flip de `IsVisible`.
  - Os 25 hex legados do `.xaml` e os 9 `Color.FromArgb` do `.xaml.cs` viram chave de `DesignTokens.xaml`; amostras dos temas de leitura mantidas como conteudo.
- **Dependencies:** T-1
- **Test:** `SettingsOverlay_BranchesLayoutByIdiomWithoutDuplicatingTheControl` + `Overlays_UseAnimatedTransitions` (T-8); build Windows
- **Status:** pending

#### T-5: TranslateBookPopup — metadados do livro + banner offline
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader/Pages/Controls/TranslateBookPopup.xaml`, `src/TranslateReader/Pages/Controls/TranslateBookPopup.xaml.cs`
- **Acceptance:**
  - Ctor `public TranslateBookPopup(BookSummary book)`; `x:Name="BookMetaLabel"` mostra titulo + autor + `TotalChapters`; `x:Name="OfflineBanner"` traz o aviso de execucao 100% offline.
  - Resultado do popup continua `(string source, string target)` — contrato de retorno intacto.
  - Os 7 hex legados substituidos por tokens.
- **Dependencies:** T-1
- **Test:** `RedesignedXaml_HasNoLegacyChromeHex` + `EveryXamlEventHandler_ExistsInTheCodeBehind` (T-8); build Windows
- **Status:** completed

### Wave 3 (parallel-eligible)

#### T-6: LibraryPage — sidebar, busca, hero, filtro Recentes, chip e card de modelo
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader/Pages/LibraryPage.xaml`, `src/TranslateReader/Pages/LibraryPage.xaml.cs`, `src/TranslateReader/PageModels/LibraryPageModel.cs`
- **Acceptance:**
  - Os 7 `x:Name` prescritos existem (`SidebarPanel`, `BookCountLabel`, `SearchEntry`, `ContinueReadingHero`, `RecentFilterButton`, `TargetLanguageChip`, `ModelStatusCard`) + `BooksCollection` preservado; `OnIdiom` decide a sidebar; nenhuma rota nova.
  - `LibraryPageModel` recebe `ISettingsManager` (ja registrado no DI), expoe `SearchQuery` e consome `ListBookSummariesAsync`/`ListRecentBookSummariesAsync`/`GetSelectedModelStatusAsync` — zero "Gemma 2 2B"/"1.6 GB" no XAML.
  - Os 2 call sites passam a `new TranslateBookPopup(book)`; nenhum controle novo e decorativo (todos com binding ou handler real).
  - Mutacao de `[ObservableProperty]` vinda de background segue marshalada (`MainThread.BeginInvokeOnMainThread`); os 3 hex legados removidos.
- **Dependencies:** T-1, T-2, T-3, T-5
- **Test:** `LibraryPage_HasTheMockupStructure` + `EveryPageBinding_ResolvesToAKnownMember` (T-8); build Windows
- **Status:** pending

#### T-7: ReaderPage — painel de capitulos (TOC) animado
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader/Pages/ReaderPage.xaml`, `src/TranslateReader/Pages/ReaderPage.xaml.cs`, `src/TranslateReader/PageModels/ReaderPageModel.cs`
- **Acceptance:**
  - `ChaptersPanel` (~250px, inline a esquerda, empurra o conteudo), `ChaptersCollection` ligado a `Chapters`, `TocButton` visivel so no idiom Desktop via `OnIdiom`.
  - `ReaderPageModel` ganha `IsTocVisible` + `GoToChapterAsync(int index)` reusando o `LoadCurrentChapterAsync` privado — **zero** operacao nova em Manager/Engine/Access: `git diff` de `IReadingManager.cs` e `ReadingManager.cs` sai VAZIO.
  - `ReaderPage.xaml.cs` anima o TOC com `TranslateTo`/`FadeTo` e chama `SettingsOverlay.ShowAsync()/HideAsync()`; todo `+=` de `OnAppearing` mantem o `-=` par em `OnDisappearing` (csharp.md §2.4).
  - Os 8 hex legados removidos; `ContentWebView` e os `.js` de `Resources/Raw/` intocados.
- **Dependencies:** T-1, T-4
- **Test:** `ReaderPage_HasTheChapterNavigationPanel` + `Overlays_UseAnimatedTransitions` (T-8); build Windows
- **Status:** pending

### Wave 4

#### T-8: Rede estrutural do MAUI + job Android no CI
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `test/TranslateReader.Tests/DesignSystemTests.cs` (novo), `.github/workflows/ci.yml`
- **Acceptance:**
  - 8 `[Fact]` novos leem os XAML do disco no padrao de `HybridWebViewContractTests` (`AppContext.BaseDirectory` + `..`), com os nomes exatos exigidos pelo DoD 8.
  - `EveryPageBinding_ResolvesToAKnownMember` resolve a raiz antes do primeiro `.` de `{Binding X` e `Path=X`, aplica as convencoes do CommunityToolkit (`_books`->`Books`, `OpenBookAsync`->`OpenBookCommand`) e casa contra a UNIAO de `PageModel` + `BookSummary` + `Chapter`.
  - Nenhum teste falha ABERTO: cada parser assert que encontrou >= 1 ocorrencia antes de julgar (aprendizado `div-paragraph-reading`), senao um seletor quebrado passaria vazio.
  - `ci.yml` ganha job `Build (Android)` com `dotnet workload install maui-android` (mesmo uso de `sca.yml`) e `-f net10.0-android`; actions pinadas por SHA.
  - Suite inteira: `Failed: 0`, `Total >= B+16`, nenhum nome de teste de `origin/main` ausente no HEAD.
- **Dependencies:** T-1, T-2, T-3, T-4, T-5, T-6, T-7
- **Test:** os proprios 8 `[Fact]` + `dotnet test` completo com `--collect:"XPlat Code Coverage"`
- **Status:** pending

## Execution
- Total tasks: 8 | Waves: 4 | Estimated parallel speedup: 2x
- Wave 1 = T-1, T-2, T-3 (arquivos disjuntos, nenhuma dependencia entre si)
- Regra global: todo `Verify:`/diff ancora em `$(git merge-base origin/main HEAD)`, nunca em ref local (aprendizado `translated-epub-images`).

## Files modified (all tasks)
- `src/TranslateReader/Resources/Styles/{DesignTokens.xaml (novo),Styles.xaml,Colors.xaml}`
- `src/TranslateReader/{App.xaml,App.xaml.cs,AppShell.xaml}`
- `src/TranslateReader/Pages/{LibraryPage,ReaderPage}.xaml(.cs)`
- `src/TranslateReader/Pages/Controls/{SettingsOverlay,TranslateBookPopup}.xaml(.cs)`
- `src/TranslateReader/PageModels/{LibraryPageModel,ReaderPageModel}.cs`
- `src/TranslateReader.Core/Models/{BookSummary.cs,TranslationModelStatus.cs (novo)}`
- `src/TranslateReader.Core/Contracts/Managers/{ILibraryManager,ITranslationManager}.cs`
- `src/TranslateReader.Core/Business/Managers/{LibraryManager,TranslationManager}.cs`
- `test/TranslateReader.Tests/{LibraryManagerTests,TranslationManagerTests,DesignSystemTests (novo)}.cs`
- `.github/workflows/ci.yml`

## Test requirements
- Unit + estrutural: `DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --collect:"XPlat Code Coverage"`
- Build Windows: `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0`
- Build Android: job novo do `ci.yml` (local so quando o TFM resolve — ver DoD 2)
- Cobertura minima: 90% em `LibraryManager` e `TranslationManager` (PROJECT.md, D-2/D-6)
- Nao-regressao: `Total >= B+16` e nenhum nome de teste de `origin/main` ausente no HEAD
