D-2026-08-02-app-redesign-3 (2026-08-02): Sistema de cores centralizado + chrome dark-only, LOCKED.
(1) Nasce `src/TranslateReader/Resources/Styles/DesignTokens.xaml`, merged em `App.xaml`, com os
tokens EXATOS extraidos do mockup (`design/DESIGN-REFERENCE.md`): `#161826` (bg), `#232532`
(surface), `#E9E9ED` (text), `#9184D9` (accent), `#A7A1DB` (accent-2), escala neutra 100-900
(`#F3F5FE`..`#292B31`), escala accent 100-900 (`#F5F4FF`..`#2B2741`), raios 4/8/14 e as sombras
"hairline stroke + drop shadow" (`Border.Stroke` da escala neutra + `Border.Shadow`). Nenhum hex de
chrome fica solto em XAML ou em code-behind depois desta phase — o `SettingsOverlay.xaml.cs` de hoje
hardcoda `Color.FromArgb("#2563EB")` em 4 metodos (`UpdateThemeButtonBorders`,
`UpdateReadingModeButtonBorders`, `UpdateModelButtonBorders`), tudo isso passa a ler recurso.
(2) A chrome do app (tudo FORA do WebView) passa a ser DARK-ONLY: `App.xaml.cs` fixa
`UserAppTheme = AppTheme.Dark` (hoje nao existe nenhum `UserAppTheme` no repo, o app segue o tema do
SO) e os `AppThemeBinding Light=.../Dark=...` da chrome viram token unico. Motivo: os dois mockups
existem SO em dark; manter o par light/dark obrigaria a inventar uma paleta clara que o design nao
define — exatamente o que o card proibe. Alternativa rejeitada: deixar o `AppThemeBinding` e repetir
a cor dark nos dois lados (mentira estrutural, some no primeiro toque futuro).
(3) LIMITE CRITICO, nao confundir: o tema de LEITURA (Claro/Escuro/Sepia) e o do CONTEUDO do livro,
gerado por `ThemeEngine`/`ISettingsManager.GenerateReaderCss` e aplicado dentro do WebView. Ele NAO
e afetado por (2), continua com as 3 opcoes e nao pode ser tocado nesta phase. Os 3 botoes de tema
no `SettingsOverlay` continuam existindo e continuam trocando o tema do conteudo; so o estilo dos
botoes muda (viram os 3 cards "Aa Claro / Aa Escuro / Aa Sepia" do screenshot
`desktop-reader-settings-panel.jpg`, cujas cores de amostra — branco, escuro, creme — sao conteudo
do design e nao contam como hex legado).
