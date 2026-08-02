## De `pixel-perfect` (2026-08-02)

- **[BUG, confirmado, adiado]** Fonte "OpenDyslexic" no picker de configuracoes de leitura nao
  produz nenhum efeito visual — o texto do capitulo continua sendo renderizado com a fonte padrao
  do navegador. Causa raiz confirmada por leitura de codigo (fix-round iter 2, D-2026-08-02-pixel-
  perfect-10 item 5): "OpenDyslexic" e uma fonte de CONTEUDO, aplicada via CSS dentro da
  `HybridWebView` do Reader (nao e fonte nativa MAUI registrada em `MauiProgram.cs`). As outras
  opcoes do array `FontOptions` em
  `src/TranslateReader/Pages/Controls/SettingsOverlay.xaml.cs:10`
  ("Georgia", "serif", "sans-serif", "monospace") resolvem porque sao fontes de sistema do
  proprio motor de renderizacao do WebView (WebView2 no Windows). "OpenDyslexic" NAO e uma
  fonte de sistema em NENHUMA plataforma-alvo (Windows/Android/iOS/MacCatalyst) e o projeto nao
  empacota o arquivo de fonte (`.woff`/`.woff2`/`.ttf`) nem uma regra `@font-face` em
  `src/TranslateReader/Resources/Raw/wwwroot/` (onde vive o HTML/CSS servido pra WebView).

  Fora de escopo da phase `pixel-perfect`: corrigir exigiria tocar `Resources/Raw/**`
  (arquivo de fonte + `@font-face`), proibido pela fronteira Client-only desta phase
  (D-2026-08-02-pixel-perfect-1), cuja intocabilidade e verificada pelo DoD 11 (diff vazio de
  `src/TranslateReader.Core/` e `src/TranslateReader/Resources/Raw/` contra o `BASELINE` da
  phase). A opcao "OpenDyslexic" NAO foi removida do picker nesta phase — removê-la seria
  regressao funcional (o usuario ainda pode selecionar o nome, so nao ve o efeito).

  Para uma phase futura resolver:
  1. Obter o arquivo webfont OpenDyslexic (licenca SIL OFL, https://opendyslexic.org/) em
     formato `.woff2` (preferido para o WebView) e colocar em
     `src/TranslateReader/Resources/Raw/wwwroot/fonts/`.
  2. Adicionar uma regra `@font-face { font-family: 'OpenDyslexic'; src: url(...); }` no CSS
     servido pelo `HtmlUtility`/template HTML do Reader (procurar onde o CSS de tema/fonte e
     montado hoje, ex. `ThemeEngine` + o HTML base em `Resources/Raw/wwwroot/index.html`).
  3. Atualizar/registrar licenca em `THIRD-PARTY-NOTICES.md` (padrao ja seguido para Inter e
     Phosphor nesta mesma phase, T-1).
  4. Validar visualmente (screenshot real) que o glifo muda ao selecionar "OpenDyslexic" no
     picker — o gap so foi descoberto por teste manual ao vivo, nao por nenhum DoD automatico.
