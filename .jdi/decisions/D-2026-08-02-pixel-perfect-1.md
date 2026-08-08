D-2026-08-02-pixel-perfect-1 (2026-08-02): Fonte de verdade e fronteira de escopo, LOCKED.
(1) A fonte de verdade NUMERICA da phase e `design/PIXEL-SPEC.md`, gerado nesta sessao renderizando
os dois mockups num Chrome real e lendo `getComputedStyle`/`getBoundingClientRect` de cada
componente (nao e estimativa de screenshot). Screenshots em `design/screenshots/` sao referencia
visual secundaria; em divergencia, vale o PIXEL-SPEC. O executor NAO inventa valor: se a spec nao
cobre algo, a task para com BLOCKED e descreve o que falta.
(2) A phase e 100% Client Layer: `src/TranslateReader.Core/**` INTOCADO (git diff vazio), assim
como `Resources/Raw/` (JS/CSS do WebView) e o tema de leitura do `ThemeEngine`. Superficies
permitidas: os 4 XAML de Pages/Controls + code-behinds, 2 PageModels, `Resources/Styles/*`,
`Resources/Fonts/*`, `MauiProgram.cs`, `App.xaml(.cs)`, `AppShell.xaml`, `TranslateReader.csproj`
(so ItemGroup de fonts, se necessario), `THIRD-PARTY-NOTICES.md` e `test/TranslateReader.Tests/`.
Consequencia: os gates de cobertura de Core (D-2/D-6) nao geram exigencia nova — o wiring MAUI
continua provado por testes estruturais que leem XAML/code-behind do disco (padrao D-...-10 da
phase app-redesign).
