D-2026-08-02-app-redesign-2 (2026-08-02): Frame de escopo e fidelidade da phase `app-redesign`, LOCKED.
Os mockups (`design/TranslateReader Desktop.html`, `design/TranslateReader Mobile.html`, condensados em
`design/DESIGN-REFERENCE.md` + 9 screenshots em `design/screenshots/`) sao fonte de verdade de
ESTRUTURA e de VISUAL ao mesmo tempo. Consequencia literal do card ("nao deve ... inventar um layout
novo"): nada que os 9 screenshots nao mostram entra nesta phase, e nada que eles mostram e
substituido por uma invencao. Lista FECHADA de arquivos que podem mudar:
`src/TranslateReader/Pages/LibraryPage.xaml(.cs)`, `Pages/ReaderPage.xaml(.cs)`,
`Pages/Controls/SettingsOverlay.xaml(.cs)`, `Pages/Controls/TranslateBookPopup.xaml(.cs)`,
`PageModels/LibraryPageModel.cs`, `PageModels/ReaderPageModel.cs`, `AppShell.xaml`, `App.xaml(.cs)`,
`Resources/Styles/*.xaml` (incluindo o novo `DesignTokens.xaml`), `MauiProgram.cs` (so se DI novo for
necessario), no Core apenas `Models/BookSummary.cs`, `Contracts/Managers/ILibraryManager.cs`,
`Contracts/Managers/ITranslationManager.cs`, `Business/Managers/LibraryManager.cs`,
`Business/Managers/TranslationManager.cs` e um `Models/TranslationModelStatus.cs` novo, mais
`test/TranslateReader.Tests/*` e `.github/workflows/ci.yml`.
NAO entra (cada um ja tem phase propria no ROADMAP ou nao aparece em nenhum screenshot):
BookDetailPage (`detalhe-livro`), bookmarks (`bookmarks`), busca full-text dentro do livro
(`busca-no-livro`), qualquer mexida em `Business/Engines/*`, `Access/*`,
`Resources/Raw/wwwroot/js/*` (o WebView, sua paginacao e sua traducao interativa ficam intocados) e
i18n (o app inteiro e pt-BR hardcoded hoje, os mockups sao pt-BR, trocar isso e outra phase).
Motivo de fechar a lista: esta phase toca a superficie visual inteira do app ao mesmo tempo, entao o
unico jeito de provar "nao quebrei nada" sem harness de UI e limitar o raio de explosao e verificar
o diff.
