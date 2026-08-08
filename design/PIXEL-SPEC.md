# PIXEL-SPEC — medidas exatas extraidas dos mockups (getComputedStyle)

Fonte de verdade NUMERICA para a phase `pixel-perfect`. Extraida em 2026-08-02 renderizando
`design/TranslateReader Desktop.html` e `design/TranslateReader Mobile.html` num Chrome real e
lendo `getBoundingClientRect()` + `getComputedStyle()` de cada elemento. Onde este arquivo e o
screenshot divergirem, VALE ESTE ARQUIVO (screenshot tem compressao/escala; isto aqui e o CSS
computado). Nao invente valor que nao esteja aqui — se faltar, marque a task como BLOCKED e
descreva o que falta.

## Como ler esta spec (mapeamento CSS -> MAUI)

- `f:14px/500` = FontSize 14, peso 500 (Medium). Pesos usados no mockup: **400 (Regular) e
  500 (Medium). Nao existe bold(700) no mockup** — onde o XAML atual usa
  `FontAttributes="Bold"`, a spec manda peso 500 = `FontFamily="InterMedium"` SEM
  `FontAttributes`.
- `p:16px 28px` (CSS: vertical horizontal) -> MAUI `Padding="28,16"` (horizontal,vertical).
  `p:10px 12px 6px 34px` (CSS: top right bottom left) -> MAUI `Padding="34,10,12,6"`
  (left,top,right,bottom). **CUIDADO: a ordem CSS e diferente da ordem MAUI.**
- `g:12px` = gap entre filhos -> `Spacing="12"` (StackLayout) ou
  `ColumnSpacing`/`RowSpacing` (Grid).
- `r:8px` = corner radius -> `StrokeShape="RoundRectangle 8"`.
- `bd:0.67 #e9e9ed@0.16` = borda hairline. Em MAUI usar `StrokeThickness="1"` +
  `Stroke={StaticResource ColorDivider}` (1px logico e o hairline pratico da plataforma).
- Cores com `@alpha` -> token com canal alfa (ex.: `#9184d9@0.1` = `#1A9184D9`).
- px do CSS -> DIP do MAUI 1:1 (nao converter).
- `ls1px` = letter spacing -> `CharacterSpacing` (MAUI usa a mesma unidade visual; usar o
  valor indicado).
- Fontes: chrome inteira = **Inter** (`InterRegular` 400 / `InterMedium` 500). Serifada
  aparece SO no "Aa" dos cards de tema (usar `FontFamily="Georgia"` nao e possivel
  cross-platform: usar o glyph "Aa" com InterRegular e aceitavel como delta consciente,
  OU `FontFamily="Times New Roman"` no Windows — ver D-px-2 no CONTEXT).

## Cores -> tokens (todos ja existem em DesignTokens.xaml, exceto os marcados NOVO)

| Hex medido | Token |
|---|---|
| `#161826` | ColorBg |
| `#232532` | ColorSurface |
| `#e9e9ed` | ColorText |
| `#9184d9` | ColorAccent |
| `#b2b6ca` | Neutral400 |
| `#9397ab` | Neutral500 |
| `#75798c` | Neutral600 |
| `#595d6c` | Neutral700 |
| `#3f424d` | Neutral800 |
| `#292b31` | Neutral900 |
| `#e9e9ed@0.16` | ColorDivider (`#29E9E9ED`) |
| `#e08a8a` | **NOVO** `ColorDanger` (`#E08A8A`) — substitui o `#E53E3E` atual |
| `#9184d9@0.10` | **NOVO** `AccentTint10` (`#1A9184D9`) — item de nav/TOC ativo |
| `#9184d9@0.08` | **NOVO** `AccentTint08` (`#149184D9`) — row de modelo selecionada |
| `#292b31@0.5` | **NOVO** `OverlayScrim` (`#80292B31`) — backdrop de modal |
| `#161826@0.65` | **NOVO** `CoverScrim` (`#A6161826`) — fundo do botao ⋮ sobre a capa |
| `#e9e9ed@0.7` | **NOVO** `TextMuted70` (`#B3E9E9ED`) — labels de campo do Settings |
| `#e9e9ed@0.55` | **NOVO** `TextMuted55` (`#8CE9E9ED`) — autor dentro da capa |
| `#e9e9ed@0.4` | **NOVO** `TextMuted40` (`#66E9E9ED`) — badge EPUB |
| `#000000@0.35` | **NOVO** `ProgressTrackOnCover` (`#59000000`) |

## Icones — Phosphor (font MIT, 2 pesos usados: Regular e Fill)

O mockup usa a icon-font **Phosphor** (`@phosphor-icons/web`). Baixar os 2 TTF e registrar
como `Phosphor` e `PhosphorFill`. Uso: `<Label FontFamily="Phosphor" Text="&#xE30C;" />`.

| Icone (classe no mockup) | Codepoint | Peso | Onde |
|---|---|---|---|
| translate | `E4A2` | Regular | logo (sidebar/mobile), menu "Traduzir livro", botao Traduzir do popup |
| books | `E758` | Fill | nav "Biblioteca" |
| clock-counter-clockwise | `E1A0` | Regular | nav "Recentes" |
| cpu | `E610` | Regular | card "Modelo de traducao" + status do modelo no Settings |
| magnifying-glass | `E30C` | Regular | dentro do campo de busca |
| squares-four | `E464` | Regular | toggle grid |
| rows | `E5A2` | Regular | toggle list |
| globe-hemisphere-west | `E28C` | Regular | chip de idioma PT-BR |
| plus | `E3D4` | Regular | botao Importar / botao "+" mobile |
| arrow-right | `E06C` | Regular | botao Continuar (hero) / seta do hero mobile |
| arrow-left | `E058` | Regular | voltar (reader) |
| list | `E2F0` | Regular | hamburguer TOC (reader desktop) |
| gear-six | `E272` | Regular | engrenagem (reader) |
| caret-left | `E138` | Regular | botao Anterior (footer) |
| caret-right | `E13A` | Regular | botao Proximo (footer) |
| x | `E4F6` | Regular | fechar (Settings, popup) |
| book-open | `E0E6` | Regular | modo Paginado |
| arrows-vertical | `EB04` | Regular | modo Rolagem |
| check-circle | `E184` | Fill | radio ON da lista de modelos |
| circle | `E18A` | Regular | radio OFF da lista de modelos |
| circle-dashed | `E602` | Regular | status "Modelo nao baixado" |
| dots-three-vertical | `E208` | Regular | menu ⋮ dos cards/rows |
| trash | `E4A6` | Regular | menu "Excluir" |
| shield-check | `E40C` | Regular | banner offline do popup |

---

# DESKTOP (`TranslateReader Desktop.html`)

## Library — sidebar (232px, altura cheia)

- Container: `aside` 232w, bg herdado `#161826`, `p:20px 16px` -> `Padding="16,20"`,
  gap 24 entre blocos (logo / nav / spacer / model-card). Borda direita: hairline ColorDivider.
- Logo row: `p:4px 6px` gap 10. Chip do icone: 32x32, borda 1 ColorAccent, r8, icone
  `translate` 18 ColorAccent. Texto "TranslateReader" 16/500 ls-0.16 ColorText.
- Nav (gap 2 entre itens): item 40h, r8, `p:9px 10px` -> `Padding="10,9"`, gap 10,
  icone 17, texto 14/400.
  - Ativo ("Biblioteca"): bg `AccentTint10`, icone books-Fill ColorAccent, texto ColorAccent.
  - Inativo ("Recentes"): bg transparente, icone+texto Neutral500.
- Model card (rodape): borda hairline ColorDivider, r8, p12, gap 8 vertical.
  - Row titulo: icone `cpu` 15 + "Modelo de traducao" 12/400 Neutral400, gap 8.
  - Nome: "Gemma 2 2B · 1.6 GB" 13/400 ColorText (via binding, nunca hardcode).
  - Status: "Modelo nao baixado" 11/400 Neutral600.

## Library — top bar (67h)

- Container: `p:16px 28px` -> `Padding="28,16"`, gap 12, alinhamento vertical center.
- Titulo "Biblioteca" 20/500 ls-0.3 ColorText; count "8 livros" 12/400 Neutral600 na MESMA
  linha (baseline), 12px depois do titulo — NAO empilhado.
- Busca: 260x35, bg ColorSurface, borda hairline ColorDivider, r8,
  `p:6px 10px 6px 32px` -> `Padding="32,6,10,6"`; icone magnifying-glass 15 Neutral500
  posicionado 10px da esquerda, centro vertical. Placeholder "Buscar titulo ou autor..."
  cor Neutral500, texto 13.
- Toggle grid/list (segmented): container 80x30, borda hairline, r8, 2 celulas iguais
  `p:7px 12px`, icone 15. Celula ativa: anel interno 1px ColorAccent (em MAUI: Border
  interno com Stroke ColorAccent) + icone ColorAccent; inativa: icone Neutral500.
- Chip idioma: 34h, borda hairline, r8, `p:10,6` gap 6; icone globe 15 + "PT-BR" 14/500
  ColorText.
- Importar: 34h, borda 1 ColorAccent, r8, `p:10,6` gap 6; icone plus 15 + "Importar"
  14/500, tudo ColorAccent. Fica NA TOP BAR (o `ToolbarItem` nativo atual sai).

## Library — hero "CONTINUE LENDO" (116h)

- Border: bg ColorSurface, r14, p16, gap horizontal 20, stroke hairline Neutral800.
- Capa: 56x84, gradiente ColorSection->ColorSectionGlow, r4, "lombada" = faixa interna
  esquerda 3px ColorAccent@50% (`#809184D9`); mini-titulo do livro 7/400 `#D9E9E9ED`
  centrado no rodape da capa.
- Coluna central (gap 4): "CONTINUE LENDO" 10/400 CharacterSpacing 1 ColorAccent
  (`TextTransform="Uppercase"`); titulo 17/400 ColorText; meta "Marcus Aurelius · 96%"
  12/400 Neutral500; progress: 380x3, r2, track Neutral900, fill ColorAccent, 8px acima
  da base.
- Botao "Continuar": 29h, borda 1 ColorAccent, r8, `p:10,6` gap 6, texto 14/500 + icone
  arrow-right 15, ambos ColorAccent.

## Library — grid de capas

- Colunas ADAPTATIVAS: CSS `auto-fill minmax(~150px,1fr)`, gap coluna 20, gap linha 24.
  Em MAUI: `GridItemsLayout` com `Span` recalculado em `SizeChanged`:
  `span = max(3, (int)((larguraDisponivel + 20) / 187))` (187 = 167 card + 20 gap;
  na janela de referencia 1291px de conteudo deu 7 colunas de 167px).
- Card (largura fluida ~167, gap interno 8 entre capa e labels):
  - Capa: aspecto 2:3 (167x251 na referencia — em MAUI manter `HeightRequest =
    largura * 1.5` ou aspecto via layout), gradiente, r6, `p:14px 12px` ->
    `Padding="12,14"`, stroke hairline Neutral800, lombada interna esquerda 4px
    ColorAccent@35% (`#599184D9`).
  - Dentro da capa (topo): titulo 14/500 ColorText (max 2 linhas), autor 10/400
    TextMuted55.
  - Dentro da capa (base): badge "EPUB" 9/400 CharacterSpacing 0.72 TextMuted40.
  - Progress na base da capa: altura 4, track ProgressTrackOnCover, fill ColorAccent.
  - Botao ⋮: 28x28, r6, bg CoverScrim, icone dots-three-vertical 16 ColorText,
    canto sup. direito com offset 6. Tap abre o mesmo MenuFlyout do card
    (`FlyoutBase.ShowAttachedFlyout`).
  - Abaixo da capa: titulo 12/400 ColorText, autor 11/400 Neutral600.

## Library — LIST VIEW (toggle `rows`) — NOVO, o mockup TEM esse layout funcional

Screenshot: `design/screenshots/desktop-library-list.jpg`.
- Row: altura 84, r10, `p:12px 10px` -> `Padding="10,12"`, gap 16, bg transparente
  (hover ColorSurface — hover e opcional, nao ha teste).
- Capa: 40x60, gradiente, r3.
- Coluna texto: titulo 14/400 ColorText; autor 12/400 Neutral600.
- Direita (gap 10): mini progress 96x3 (track Neutral900, fill ColorAccent) + "12%"
  11/400 Neutral500 + botao ⋮ 30x30 r8 icone 16.
- So no DESKTOP (mobile nao tem toggle no mockup).

## Library — menu de contexto (⋮)

Mockup: 160w, bg ColorSurface, r8, p4, sombra ShadowMd; item 28h r8 icone 15 + texto
13/500; "Traduzir livro" ColorText, "Excluir" **ColorDanger**. Em MAUI o `MenuFlyout` e
nativo e nao estiliza — manter nativo (delta consciente registrado). O que E obrigatorio:
o botao ⋮ visivel abrindo o flyout.

## Library — modal "Traduzir livro" (440w)

- Backdrop: OverlayScrim cobrindo a tela.
- Card: 440w, bg ColorSurface, r14, `p:11.2px` -> `Padding="12"` interno com gap ~8,
  sombra ShadowLg + stroke Neutral500.
- Titulo "Traduzir livro" 20/500 ColorText.
- Book row (gap 12): capa 34x51 gradiente r3 lombada 2px; titulo 13/400 ColorText;
  meta "Lewis Carroll · 6 capitulos" 11/400 Neutral500.
- Selects lado a lado (label 12/400 TextMuted70; campo 36h bg ColorSurface borda hairline
  r8 `p:10,6`), icone arrow-right 15 ColorAccent entre eles.
- **ORDEM: banner offline vem DEPOIS dos selects** (hoje esta antes — corrigir).
- Banner: bg **ColorBg** (nao accent!), r8, `p:12,10` gap 8; icone shield-check 15 +
  texto 12/400, ambos Neutral500.
- Botoes (direita, 29h, r8, `p:10,6`, texto 14/500, gap 8 entre eles):
  - "Cancelar": borda hairline ColorDivider, texto ColorText, bg transparente.
  - "Traduzir": borda 1 ColorAccent, texto+icone translate 15 ColorAccent, bg
    transparente. **Outline, nao solido.**

## Reader — top bar (59h)

- `p:10px 16px` -> `Padding="16,10"`, gap 8, hairline inferior ColorDivider.
- Botoes 36x36 r8 bg transparente, icones 18: arrow-left (voltar), list (TOC, so Desktop).
- Bloco titulo: titulo 14/400 ColorText 1 linha; subtitulo
  "Capitulo {N} de {Total} — {Autor}" 11/400 Neutral600. **Subtitulo NAO existe hoje —
  criar.**
- Direita: "Aa" 15/500 (botao 36x36) + gear-six 18 (botao 36x36).

## Reader — painel TOC (250w)

- `p:18px 12px` -> `Padding="12,18"`; bg ColorBg; hairline direita.
- Header "Capitulos" 13/500 CharacterSpacing 1 Neutral600 uppercase, `p:0 10px`.
- Lista gap 2; item: r8, `p:10,8`, gap 10, numero 11/400 Neutral600 (coluna 16w),
  titulo 13/400 ColorText.
- Item ATIVO: bg AccentTint10 + titulo ColorAccent (numero continua Neutral600).

## Reader — footer (54h, so no modo Paginado)

- `p:10px 20px` -> `Padding="20,10"`, hairline superior ColorDivider.
- Centro (gap vertical 6): "Pagina {p} / {t} · Capitulo {c} de {n}" 12/400 Neutral500 +
  mini progress 200x2 r1 (track Neutral900, fill ColorAccent).
- "Proximo": 34h, borda hairline ColorDivider, r8, `p:10,6` gap 6, texto 14/500 ColorText
  + caret-right 14. ("Anterior" identico com caret-left, aparece conforme logica atual.)

## Settings — painel desktop (380w, altura cheia, direita)

- bg ColorSurface, sombra ShadowLg + stroke Neutral500. **Largura 380 (hoje 400).**
- Header 72h `p:18px 20px` -> `Padding="20,18"`: titulo 17/400 ColorText + botao X 36x36
  r8 icone x 17.
- Corpo `p:4px 20px 28px` -> `Padding="20,4,20,28"`, gap 22 entre blocos.
- Section header (h6): 13/500 CharacterSpacing 1 Neutral600 uppercase ("Tema",
  "Modo de leitura", "Traducao", "Modelo local").
- Tema: 3 cards iguais (1/3 cada, gap 10), 67h, r10, `p:8,12` gap 8 vertical, borda
  hairline; conteudo: "Aa" 17 serif + label 12/400; cores por card:
  Claro `ReadingLightBg`/`ReadingLightText`, Escuro `ReadingDarkBg`/`ReadingDarkText`,
  Sepia `ReadingSepiaBg`/`ReadingSepiaText`. Selecionado: borda 1 ColorAccent (+anel 1px).
- Modo de leitura (segmented): container borda hairline r8, 35h, 2 celulas 50%
  `p:12,7` gap 6, icone 15 + texto 13/400. **Ordem: Paginado (book-open), Rolagem
  (arrows-vertical).** Ativa: texto+icone ColorAccent; inativa ColorText.
- Campo Fonte: label 12/400 TextMuted70; Picker 36h bg ColorSurface borda hairline r8.
- Sliders (4 rows de 49h): label 12/400 TextMuted70 esquerda; VALOR 12/400 **ColorAccent**
  direita ("18px", "1.7", "0.0px", "0.0px"); slider track ColorAccent/Neutral700 thumb
  ColorAccent (ja e assim).
- Traducao: 2 pickers lado a lado 50% gap 12 (label 12 TextMuted70, campo 36h).
- **Modelo local — vira LISTA VERTICAL (hoje sao pills horizontais):** rows gap 8;
  row 53h, r8, `p:12,10`, gap 12, borda hairline; icone radio 16 (circle OFF Neutral500 /
  check-circle Fill ON ColorAccent); nome 13/400 ColorText; filename (ex.
  "gemma-2-2b-it-Q4_K_M.gguf") 11/400 Neutral500 abaixo do nome; tamanho "1.6 GB"
  11/400 Neutral600 na direita. Row SELECIONADA: bg AccentTint08 + borda 1 ColorAccent.
  Os 4 modelos atuais (Gemma/Qwen/Phi/HY-MT) permanecem; dados via binding existente.
- Status: icone circle-dashed 14 + "Modelo nao baixado" 12/400 Neutral600, gap 8.
- "Excluir modelo" (nao aparece no mockup, PRESERVAR funcionalidade): bg ColorDanger,
  texto ColorBg, r8 — unico botao solido.
- Atribuicao Tencent: PRESERVAR (12/400 Neutral500).

---

# MOBILE (`TranslateReader Mobile.html` — conteudo do frame: 366w util)

## Library mobile — header (34h, sem sidebar)

- Row gap 8: chip logo 30x30 borda 1 ColorAccent r8 icone translate 16; titulo
  "Biblioteca" 17/400 ls-0.17; "· 6" 11/400 Neutral600 inline; [espaco]; chip "PT"
  36x34 borda hairline r8 texto 11/500; botao "+" 34x34 borda 1 ColorAccent r8 icone
  plus 17 ColorAccent.
- Busca ABAIXO do header, largura cheia: 38h, bg ColorSurface, borda hairline, **r10**,
  `p:6px 10px 6px 34px` -> `Padding="34,6,10,6"`, icone 15.

## Library mobile — hero compacto (90h)

- r14, p12, gap 12; capa 44x66 r4; "CONTINUE LENDO" 9/400 CharacterSpacing 0.9
  ColorAccent; titulo 14/400; progress 3h; icone arrow-right 17 ColorAccent na direita,
  centro vertical. **Sem autor/%, sem botao "Continuar"** — o card inteiro e clicavel.

## Library mobile — grid

- 3 colunas fixas, gap linha 18, gap coluna 14, card ~113w (fluido). Sem toggle
  grid/list, sem list view no mobile.

## Reader mobile

- Header: botoes 38x38 r8 icones 18; titulo 13/400 CENTRADO; subtitulo "Cap. {N} de {T}"
  10/400 Neutral600 (SEM autor no mobile); Aa 14/500; sem hamburguer/TOC.
- Settings = bottom sheet: r 18 topo (`RoundRectangle 18,18,0,0`), handle 36x4 r2
  Neutral700 centrado 10px do topo; header 46h `p:20,8` titulo 16/400, X 30x30 icone 15;
  corpo `p:20,4,20,56` gap 20; cards de tema 59h `p:6,10` gap 6; segmented modo 35h
  celulas 50%; resto igual ao desktop proporcionalmente.

---

## Diferencas intencionais mantidas (nao sao bug da spec)

1. `MenuFlyout` continua nativo (nao estilizavel em WinUI) — botao ⋮ e obrigatorio,
   visual do menu aberto e delta consciente.
2. "Excluir modelo" + atribuicao Tencent nao existem no mockup mas PERMANECEM
   (funcionalidade real, D-...-8 da phase app-redesign).
3. Barras de progresso de traducao/download (overlays da LibraryPage/ReaderPage) nao
   existem no mockup — apenas re-tokenizar (ColorDanger no "Pausar"/"Cancelar"), layout
   fica.
4. Serifa do "Aa" dos cards de tema: ver D-px-2 (Georgia nao e portavel).
