# Design Reference — extraido de design/TranslateReader Desktop.html e Mobile.html

Mockups sao paginas "artifact bundle" (React compilado + assets base64), nao abrem
como HTML puro em editor de texto (arquivo de 4-5MB, 1 linha gigante com o bundle).
Foram renderizados num servidor local + Chrome pra extrair tokens computados via
`getComputedStyle` e capturar screenshots. Screenshots de referencia em
`design/screenshots/*.jpg` (9 estados, desktop + mobile).

## Design tokens (extraidos via CSS custom properties do :root)

### Cores base
```
--color-bg:        #161826   (fundo geral, dark)
--color-surface:   #232532   (superficie elevada — sidebar, cards, paineis)
--color-text:      #e9e9ed
--color-accent:    #9184d9   (roxo primario — links, active nav, progress, focus)
--color-accent-2:  #a7a1db   (roxo secundario)
--color-divider:   color-mix(in srgb, #e9e9ed 16%, transparent)
```

### Escala neutra (texto secundario, bordas, placeholders)
```
100 #f3f5fe  200 #e4e7f5  300 #cfd3e5  400 #b2b6ca  500 #9397ab
600 #75798c  700 #595d6c  800 #3f424d  900 #292b31
```

### Escala accent (roxo — usar p/ estados hover/active/selected)
```
100 #f5f4ff  200 #e7e5fe  300 #d2cefd  400 #b5abfc  500 #968ae0
600 #796cbf  700 #5d5294  800 #423a6a  900 #2b2741
```

### Escala accent-2 (roxo acinzentado — variante mais neutra do accent)
```
100 #f5f4ff  200 #e7e5fe  300 #d2cefd  400 #b5afe8  500 #9690c9
600 #7972a9  700 #5c5783  800 #423e5d  900 #2b293a
```

### Secao/glow (usado em backgrounds decorativos, gradientes de capa)
```
--color-section:       #262a60
--color-section-glow:  #353b80
--color-section-ghost: #4c5397
```

### Tipografia
```
--font-heading / --font-body: "Inter", system-ui, sans-serif
--font-heading-weight: 500
```
Conteudo do livro (area de leitura) usa serifada — no mockup aparenta Georgia
(confirmado no picker "Fonte" = Georgia por padrao).

### Espacamento / raio / sombra
```
--space-1..8: 2.8 / 5.6 / 8.4 / 11.2 / 16.8 / 22.4 px  (escala ~1.4x, base 2.8px — nao é 4/8px padrao)
--radius-sm: 4px   --radius-md: 8px   --radius-lg: 14px
--shadow-sm: 0 0 0 1px #3f424d
--shadow-md: 0 0 0 1px #595d6c, 0 6px 18px rgba(0,0,0,0.55)
--shadow-lg: 0 0 0 1px #9397ab, 0 16px 40px rgba(0,0,0,0.65)
```
Sombras sao "hairline stroke + drop shadow" combinados (nao apenas blur) — no MAUI
mapear pra `Border.Stroke` (cor da escala neutra) + `Border.Shadow` (offset/radius/opacity).

### Tema de leitura (dentro do WebView / area de conteudo, independente do tema do app)
- **Claro:** fundo branco, texto quase preto
- **Escuro:** fundo escuro (mesma familia de --color-bg), texto claro
- **Sepia:** fundo `#f3ead6`-ish (creme), texto marrom escurecido — ja e o 3º botao
  em `SettingsOverlay.xaml` hoje (`SepiaThemeButton`), so falta o estilo visual bater.

## Inventario de telas (screenshot -> mapeamento pro componente atual)

| Screenshot | Tela do mockup | Componente atual no app | Gap |
|---|---|---|---|
| `desktop-library.jpg` | Biblioteca desktop: sidebar fixa (logo + nav Biblioteca/Recentes + card "modelo de traducao" no rodape) + topo (titulo+contagem, busca, toggle grid/list, seletor idioma PT-BR, botao Importar) + hero "Continue lendo" + grid de capas com gradiente | `LibraryPage.xaml` (grid 3-col simples, sem sidebar, sem hero, sem busca, sem toggle view) | Redesenho completo do layout; sidebar so existe em largura desktop (`OnIdiom`/`DeviceIdiom`) |
| `mobile-library.jpg` | Biblioteca mobile: sem sidebar, header compacto (logo-botao + titulo+contagem + badge idioma + botao "+"), busca abaixo, hero, grid 3-col | mesmo `LibraryPage.xaml` | Idem, variante estreita |
| `desktop-library-context-menu.jpg` | Menu de 3 pontos no card: "Traduzir livro" / "Excluir" | `FlyoutBase.ContextFlyout` com `MenuFlyoutItem`s (ja existe, `LibraryPage.xaml:38-47`) | So restyle visual |
| `desktop-library-translate-popup.jpg` | Modal "Traduzir livro": book icon, titulo+autor+capitulos, 2 pickers idioma, banner info "roda 100% offline", Cancelar/Traduzir | `TranslateBookPopup.xaml` (ja tem quase tudo: titulo, pickers, botoes) | Falta o icone do livro, o banner de aviso offline; resto e so cor/raio |
| `desktop-reader.jpg` / `mobile-reader.jpg` | Reader: topo escuro (voltar, hamburguer=TOC, titulo+capitulo/autor, Aa, engrenagem) + area de conteudo sepia + rodape escuro (pagina X/Y, botao Proximo) | `ReaderPage.xaml` (`Shell.TitleView` ja tem os 2 botoes Aa/engrenagem; falta hamburguer=TOC) | TOC (capitulos) hoje **nao existe** como painel — precisa avaliar se ja ha dado equivalente (lista de Chapters do book) |
| `desktop-reader-toc.jpg` | Painel de capitulos (sidebar esquerda dentro do reader, lista numerada, capitulo atual destacado) | inexistente na UI atual (dado existe: `Chapter` model, `BooksAccess`) | Feature nova de UI sobre dado ja existente — nao e so estilo |
| `desktop-reader-settings-panel.jpg` / `mobile-reader-settings-sheet.jpg` | Configuracoes de leitura: tema (Claro/Escuro/Sepia), modo (Paginado/Rolagem), fonte, 4 sliders, traducao (idioma origem/destino), lista de modelos locais com radio+tamanho+status | `SettingsOverlay.xaml` (**ja tem 1:1** todos os campos — tema, modo, picker fonte, 4 sliders, pickers idioma, botoes de modelo incl. HY-MT) | So restyle: no mockup e side-panel (desktop, ancorado na direita) vs bottom-sheet (mobile, ja e o padrao atual do `SettingsOverlay`) — hoje o app so tem o layout mobile (bottom sheet) pros dois idiomas |

## Gaps de funcionalidade (nao so visual) encontrados

1. **TOC de capitulos no Reader** — o mockup mostra um painel de navegacao por
   capitulos (hamburguer no topo abre lista lateral). Hoje `ReaderPage` no tem
   isso; so tem Anterior/Proximo por pagina. O dado (`Chapter.Title`,
   `Chapter.OrderIndex`) ja existe via `BooksAccess`. Isso e follow-up de
   funcionalidade real, nao so CSS — decisao de escopo pro `/jdi-discuss`.
2. **Nav "Recentes"** — sidebar do mockup lista "Biblioteca" e "Recentes" como
   2 destinos. Hoje `AppShell.xaml` so registra 1 `ShellContent` (library).
   `Book.LastOpenedAt` ja existe no modelo — decisao de escopo: implementar
   "Recentes" como view filtrada (livros ordenados por `LastOpenedAt`) ou
   deixar fora do escopo desta fase.
3. **Toggle grid/list na Biblioteca** — ~~no proprio mockup (prototipo estatico) o
   toggle nao mudava o layout de fato — pode ser decorativo~~ **CORRECAO
   (2026-08-02): FALSO.** Testado ao vivo: o toggle FUNCIONA e troca para uma
   list view completa (row 84px com capa, titulo/autor, progresso e menu ⋮).
   Screenshot: `screenshots/desktop-library-list.jpg`; medidas na secao
   "LIST VIEW" de `PIXEL-SPEC.md`. Ver D-2026-08-02-pixel-perfect-4 (aceito).
4. **Busca na Biblioteca** — mockup tem campo de busca por titulo/autor
   funcional (ja existe endpoint equivalente? checar `LibraryManager`/
   `BooksAccess`). Se nao existir filtro, e feature nova sobre dado existente.
5. **Desktop side-panel vs mobile bottom-sheet para Settings** — hoje
   `SettingsOverlay` so implementa o layout mobile (bottom sheet). Mockup pede
   variante desktop (painel lateral direito, altura cheia). Precisa de
   `OnIdiom`/`DeviceIdiom` para escolher o layout, mantendo o mesmo
   code-behind/bindings.

## Responsividade observada

- **Desktop (largura ampla):** sidebar fixa a esquerda (Biblioteca/Recentes +
  card de status do modelo no rodape), conteudo principal com header + grid.
  Settings abre como painel lateral direito (altura cheia, desliza da direita).
- **Mobile (largura estreita):** sem sidebar; header compacto no topo,
  navegacao entre Biblioteca/Recentes nao ficou visivel no prototipo (so 1
  tela mobile foi desenhada) — ver gap #2. Settings abre como bottom sheet
  (arrasta de baixo, handle no topo, ja e o comportamento atual do
  `SettingsOverlay`).
- Breakpoint exato nao ficou definido no mockup (e HTML/CSS livre, nao MAUI);
  usar o breakpoint que o app ja usa hoje para diferenciar desktop/mobile
  (`DeviceIdiom`/`OnIdiom` — confirmar convencao existente no projeto antes de
  introduzir uma nova).

## Screenshots

Todos em `design/screenshots/`:
- `desktop-library.jpg`, `desktop-reader.jpg`, `desktop-reader-settings-panel.jpg`,
  `desktop-reader-toc.jpg`, `desktop-library-context-menu.jpg`,
  `desktop-library-translate-popup.jpg`
- `mobile-library.jpg`, `mobile-reader.jpg`, `mobile-reader-settings-sheet.jpg`
