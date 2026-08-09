# PIXEL-SPEC v0.2.0 — selecao e traducao de periodos (medidas exatas dos mockups)

Fonte de verdade NUMERICA para a phase `snippet-translation`. Extraida em 2026-08-09 renderizando
`design/v0.2.0/TranslateReader Desktop.html` e `design/v0.2.0/TranslateReader Mobile.html` num
Chrome real (playwright-core, viewport 1280x800 desktop / frame iOS 402x874 mobile) e lendo
`getBoundingClientRect()` + `getComputedStyle()` de cada elemento novo, em cada estado novo.
Onde este arquivo e o screenshot divergirem, VALE ESTE ARQUIVO. Nao invente valor que nao esteja
aqui — se faltar, marque a task como BLOCKED e descreva o que falta.

Esta spec cobre SO os elementos novos da feature (span de periodo, blob de vidro, pill de selecao,
hint de primeira vez, chip de idioma, snip). Chrome do app (header, footer, TOC, settings) segue
`design/v0.1.0/PIXEL-SPEC.md` — nada daquilo muda nesta phase.

## Como ler esta spec (CSS -> CSS do WebView)

- A feature vive INTEIRA no DOM do WebView (`js/snippets.js`) — os valores aqui sao CSS literal,
  sem conversao para MAUI.
- **Idiom**: desktop/tablet vs phone NAO e media query. O C# estampa `data-idiom="desktop"` ou
  `data-idiom="phone"` no `<body>` (via `DeviceInfo.Idiom`) e o CSS/JS seleciona os valores da
  coluna correspondente. Onde a tabela tem uma coluna so, o valor vale para os dois idioms.
- **`AC`** = accent do TEMA de leitura ativo em formato `r,g,b` (ver tabela de temas). Todas as
  cores do blob/chip derivam de `AC` — nunca hardcodar um accent especifico.
- Contexto tipografico: os elementos inline (span, chip) herdam a fonte do reader
  (`Georgia 18px` desktop / `17px` mobile no default). Valores `em` escalam com a fonte escolhida
  pelo usuario — manter `em`, nao congelar px.
- Pills e hint usam `Inter` (`var(--font-body)` do app) e NAO escalam com a fonte do reader.

## Temas do reader (bg / text / accent)

| Tema | bg | text | accent | accent `AC` |
|---|---|---|---|---|
| Claro | `#FFFFFF` | `#1A1A1A` | `#2563EB` | `37,99,235` |
| Escuro | `#1A1A2E` | `#E4E4E7` | `#60A5FA` | `96,165,250` |
| Sepia | `#F4ECD8` | `#5B4636` | `#8B6914` | `139,105,20` |

`darkPage` = (tema == Escuro). Claro e Sepia usam o branch "claro" das formulas.
O botao primario da pill usa o accent do APP (`#9184d9`, classe `.btn-primary` existente),
NAO o accent do tema — medido: `color: rgb(145,132,217)` em tema sepia.

## Span de periodo (unidade clicavel)

Idem nos dois idioms; valores computados confirmados (18px: padding `1.8px 4.32px`):

```
position: relative; cursor: pointer; user-select: none; -webkit-user-select: none;
border-radius: 8px; padding: 0.1em 0.24em; margin: 0 -0.24em;
box-decoration-break: clone; -webkit-box-decoration-break: clone;
```

- Desktop APENAS: `transition: background 0.22s ease` + hover `background: rgba(127,127,168,0.14)`
  (hover so quando o periodo NAO esta selecionado; selecionado -> `transparent`).
- Mobile: sem hover, sem transition.
- Atributos: `data-pi` (indice do paragrafo) e `data-si` (indice do periodo).

## Split de periodos (regex literal, uma unica fonte)

```
/(?<=[.!?…]["”’»)\]]?)\s+(?=[A-ZÀ-Þ"“«'(])/  ->  .map(s => s.trim()).filter(Boolean)
```

## Blob de vidro (selecao e snip)

Dois elementos irmaos posicionados sobre o `<p>` (que vira `position: relative`):

1. `<span>` mascara: `position: absolute; left: -8px; top: -8px; width: <ceil(parRect.width)+16>px;
   height: <ceil(parRect.height)+16>px; display: block; pointer-events: none;
   clip-path: path('<d>'); background: <fill>; backdrop-filter: blur(9px) saturate(180%);
   -webkit-backdrop-filter: blur(9px) saturate(180%)`
2. `<svg>` contorno: mesmo `left/top/width/height`, `overflow: visible; pointer-events: none`,
   filho `<path d="<d>" fill="none" stroke="<stroke>" stroke-width="1.25"
   style="filter: drop-shadow(0 6px 16px <glow>)">`.

Cores (medidas em sepia e escuro, formula geral):

| Papel | darkPage (Escuro) | Claro/Sepia | Medido (sepia `AC=139,105,20`) |
|---|---|---|---|
| fill | `linear-gradient(180deg, rgba(255,255,255,0.18), rgba(255,255,255,0.07))` | `linear-gradient(180deg, rgba(AC,0.17), rgba(AC,0.07))` | `linear-gradient(rgba(139,105,20,0.17), rgba(139,105,20,0.07))` |
| stroke | `rgba(AC,0.45)` | `rgba(AC,0.34)` | `rgba(139,105,20,0.34)`; escuro medido `rgba(96,165,250,0.45)` |
| glow | `rgba(AC,0.3)` | `rgba(AC,0.3)` | `drop-shadow(rgba(139,105,20,0.3) 0px 6px 16px)` |

Geometria (constantes travadas — teste dourado de `_blobPath`):

- Rects: `getClientRects()` de cada span coberto, filtrando `w > 1 && h > 1`; ordenar por `top`,
  depois `left`; agrupar em linha quando `Math.abs(L.cy - cy) < r.height * 0.6`.
- Constantes: `OFF = 8`, `padX = 5`, `padY = 1.5`, raio `r = 10` limitado por
  `Math.min(r, (x2-x1)/2, (y2-y1)/2)`.
- Bandas adjacentes se encontram no ponto medio: `mid = (bands[i].y2 + bands[i+1].y1) / 2`.
- Caixa: `w = Math.ceil(parRect.width) + 16`, `h = Math.ceil(parRect.height) + 16`;
  posicao `left: -8px; top: -8px`.
- Re-medir em: mudanca de selecao/snips, `resize`, e qualquer reflow do reader (fonte, espacamento).

Animacoes (keyframes literais):

```
@keyframes trGlassIn { from { opacity: 0; transform: scale(0.985); } to { opacity: 1; transform: scale(1); } }
@keyframes trPulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.45; } }
@keyframes trFadeUp { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: translateY(0); } }
```

- Blob de selecao e de snip pronto: `animation: trGlassIn 0.25s ease`.
- Blob de snip em loading: `animation: trPulse 1.1s ease-in-out infinite` (opacity medida a meio
  ciclo: `0.466`).

## Pill de selecao

| Propriedade | `data-idiom="desktop"` | `data-idiom="phone"` |
|---|---|---|
| position | `fixed; left: 50%; transform: translateX(-50%)` | `absolute; left: 10px; right: 10px` |
| bottom (VER derivadas) | `78px` paginado / `32px` rolagem | `bottom: 102px` |
| z-index | `35` | `30` |
| layout | `flex; align-items: center; gap: 10px` | `flex; align-items: center; gap: 6px` |
| padding | `7px 8px 7px 16px` | `6px 6px 6px 12px` |
| border-radius | `999px` | `999px` |
| background | `rgba(28,30,48,0.58)` | `rgba(28,30,48,0.6)` |
| backdrop-filter | `blur(26px) saturate(190%)` | `blur(26px) saturate(190%)` |
| box-shadow | `inset 0 1px 0 rgba(255,255,255,0.18), inset 0 -1px 0 rgba(0,0,0,0.35), 0 16px 40px -12px rgba(0,0,0,0.75)` | idem com ultimo termo `0 16px 40px -14px rgba(0,0,0,0.8)` |
| color | `#e9e9ed` | `#e9e9ed` |
| animation | `trFadeUp 0.22s ease` | `trFadeUp 0.22s ease` |
| altura total medida | `46px` | `44px` |

Conteudo desktop, na ordem: icone `ph-text-align-left` 15px cor accent do APP; contador
`font-size: 12px; white-space: nowrap` ("N periodo(s) selecionado(s)"); dica
`· toque em outro período para estender` `11px rgba(233,233,237,0.55)` (so quando 1 periodo
selecionado E paragrafo com >1); grupo −/+ `gap: 2px; padding: 2px; border-radius: 999px;
background: rgba(255,255,255,0.07)` com botoes `26x26` (`ph-minus`/`ph-plus` 13px, opacity `1`
habilitado / `0.35` desabilitado); `único período deste parágrafo` `11px rgba(233,233,237,0.5)`
quando o paragrafo tem 1 periodo; divisor `1px x 20px rgba(255,255,255,0.16)`; botao primario
`.btn-primary` `min-height: 32px; border-radius: 999px` (medido: `32px` alto, fonte `14px/500`,
cor `#9184d9`) com `ph-translate` 15px + "Traduzir trecho"; botao X `28x28` com `ph-x` 14px.

Conteudo mobile, na ordem: contador `11px`; espacador `flex: 1`; grupo −/+ com botoes `28x28`;
botao primario `min-height: 32px; height: 32px; font-size: 12px` com `ph-translate` 14px +
"Traduzir"; botao X `30x30` com `ph-x` 14px. Sem icone inicial, sem dica, sem `onlySentence`,
sem divisor.

## Hint de primeira vez (some apos a primeira selecao; nunca mais volta na sessao)

| Propriedade | desktop | phone |
|---|---|---|
| position/bottom | `fixed; left: 50%; bottom: <mesmo da pill paginado/rolagem>` | `absolute; left: 50%; bottom: 104px` |
| z-index | `34` | `29` |
| gap / padding | `9px` / `8px 16px` | `8px` / `7px 14px` |
| background | `rgba(28,30,48,0.5)` | `rgba(28,30,48,0.55)` |
| backdrop-filter | `blur(20px) saturate(180%)` | `blur(20px) saturate(180%)` |
| box-shadow | `inset 0 1px 0 rgba(255,255,255,0.14), 0 12px 30px -14px rgba(0,0,0,0.8)` | idem com `-16px` e alpha `0.85` |
| color / font | `rgba(233,233,237,0.82)` / `12px` | idem / `11px` |
| icone | `ph-cursor-text` 15px cor accent do APP | idem 14px |
| animation | `trFadeUp 0.4s ease` | `trFadeUp 0.4s ease` |
| texto | "Toque em um período; toque em outro para estender a seleção" | "Toque em um período; outro toque adiciona" |
| altura medida | `34.6px` | `31px` |

## Snip (trecho traduzido) + chip de idioma

Span do snip (`data-snip`): mesmo padding/margin do span de periodo (`0.1em 0.24em` /
`0 -0.24em`, `box-decoration-break: clone`), `cursor: pointer; user-select: none`, SEM
`border-radius` (o visual vem do blob). Clique alterna original/traducao; durante loading mostra
o texto ORIGINAL com blob pulsando e SEM chip.

Chip (aparece so com o snip pronto, inline no fim do trecho):

```
display: inline-flex; align-items: center; gap: 5px;            /* phone: gap: 4px */
vertical-align: 0.08em; margin-left: 7px;                        /* phone: margin-left: 6px */
padding: 2px 8px;                                                /* phone: padding: 2px 7px */
border-radius: 999px; font-family: var(--font-body);
font-size: 0.6em; font-weight: 500; letter-spacing: 0.07em;
color: <accent do TEMA>; background: rgba(AC,0.13);
box-shadow: 0 0 0 1px rgba(AC,0.38); white-space: nowrap;
```

Medido em sepia/18px: fonte do chip `10.8px`, icone troca `ph-arrows-left-right` `1.25em`
(`13.5px`), icone fechar `ph-x` `1.15em` (`12.42px`) com `opacity: 0.65; cursor: pointer`,
cor `rgb(139,105,20)`. Label: idioma DESTINO curto quando mostrando traducao, ORIGEM quando
mostrando original. Mapa: English->EN, Brazilian Portuguese (PT-BR)->PT-BR, Spanish->ES,
French->FR, German->DE, Italian->IT, Japanese->JA, Korean->KO, Chinese (Simplified)->ZH,
Russian->RU; fallback `slice(0,2).toUpperCase()`.

## Derivadas de posicionamento (NAO copiar o bottom cru — D-2026-08-09-snippet-translation-4)

No mockup o footer faz parte da mesma janela; no app o footer e XAML fora do WebView. O codigo
usa a coluna "derivada"; a coluna "medida" existe para auditoria contra o mockup.

| Contexto | Medido no mockup | Derivada a usar no app |
|---|---|---|
| Pill desktop paginado | `bottom: 78px` da janela 800px; footer top `746`, altura `54px` | borda inferior da pill **24px acima do topo do footer** (78 − 54 = 24) |
| Pill desktop rolagem | `bottom: 32px`; footer ausente | **32px acima da borda inferior do viewport do WebView** |
| Hint desktop | `bottom: 78px` (paginado; acompanha a pill) | mesma linha de base da pill no modo ativo |
| Pill phone | `bottom: 102px` do frame 874px; footer top `845` (altura `29px` visivel + home bar) | borda inferior da pill **10px acima do topo do footer** (mockup: gap medido `10.0px`) |
| Hint phone | `bottom: 104px` | **2px acima da linha de base da pill** (104 − 102) |

Larguras medidas: pill desktop `462.4x46px` (conteudo "2 períodos selecionados" + −/+ +
"Traduzir trecho" + X); pill phone `382x44px` (left/right 10px do frame 402px); hint desktop
`413.7x34.6px`; hint phone `277.3x31px`.

## Interacao (estados que os screenshots congelam)

- `sel = { p, anchor, set[] }` restrita a UM paragrafo; tap alterna periodo no set (esvaziou ->
  some a pill); tap em outro paragrafo reinicia; drag com `pointerdown` -> `pointermove`
  (`document.elementFromPoint` + `closest('[data-si]')`) estende contiguo do anchor; `pointerup`
  no document encerra o drag; `Escape` limpa (desktop); clique fora limpa; troca de
  pagina/capitulo limpa.
- `+` estende ao proximo periodo; `−` remove o ultimo; opacidade `0.35` quando nao aplicavel.
- Traduzir: runs contiguos do set viram snips independentes; snips sobrepostos no mesmo paragrafo
  sao substituidos; blob em `trPulse` durante o loading; chip so quando pronto.

## Screenshots de baseline (8, JPEG q90)

| Arquivo | Estado |
|---|---|
| `screenshots/desktop-reader-select-hint.jpg` | reader recem-aberto, hint visivel, sepia paginado |
| `screenshots/desktop-reader-selection.jpg` | 2 periodos selecionados, blob + pill completa |
| `screenshots/desktop-reader-snippet-loading.jpg` | snip em loading (texto original, blob pulsando) |
| `screenshots/desktop-reader-snippet.jpg` | snip pronto, texto traduzido + chip `⇆ PT-BR ×` |
| `screenshots/desktop-reader-snippet-original.jpg` | snip alternado para original (chip `EN`) |
| `screenshots/desktop-reader-selection-scroll.jpg` | modo rolagem, tema escuro, pill a 32px |
| `screenshots/mobile-reader-selection.jpg` | frame iOS, selecao multi-linha + pill compacta |
| `screenshots/mobile-reader-snippet.jpg` | frame iOS, snip pronto + chip |
