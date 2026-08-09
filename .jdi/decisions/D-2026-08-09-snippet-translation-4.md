D-2026-08-09-snippet-translation-4 (2026-08-09): Ground truth do pixel-perfect v0.2.0 e geometria
dourada do blob, LOCKED.

Requisito 3 do usuario: pixel-perfect INEGOCIAVEL. A phase 22 (`pixel-perfect`) so ficou executavel
por LLM menor porque a spec MEDIDA nasceu ANTES do codigo (`D-2026-08-02-pixel-perfect-1`).
`design/v0.2.0/` hoje tem apenas os 2 bundles HTML — sem PIXEL-SPEC, sem screenshots. Logo:

T-1 DA PHASE PRODUZ A FONTE DE VERDADE, antes de qualquer linha de feature:
- `design/v0.2.0/PIXEL-SPEC.md` — medidas dos elementos NOVOS (span de periodo, blob de vidro +
  SVG stroke, pill desktop, pill mobile, hint, chip de idioma, keyframes), obtidas dos dois bundles
  renderizados, com secoes nomeadas que o PLAN cita por nome.
- `design/v0.2.0/screenshots/*.jpg` — no minimo 4 estados novos (selecao de 1 periodo; selecao
  multi-linha com pill; trecho em loading pulsando; trecho pronto com chip), desktop e mobile.
- Todo valor no codigo depois CITA a spec. Nenhum "ajuste conforme o mockup".

GEOMETRIA DOURADA. `_blobPath(bands, r)` e `_blobFromEls(els)` sao funcoes PURAS de retangulos ->
string de path SVG. Ganham teste com path esperado LITERAL, comparado caractere a caractere,
travando as constantes do mockup: `r = 10`, `OFF = 8`, `padX = 5`, `padY = 1.5`, agrupamento de
rects em linha por `Math.abs(L.cy - cy) < r.height * 0.6`, e juncao de bandas adjacentes no PONTO
MEDIO (`mid = (bands[i].y2 + bands[i+1].y1) / 2`). Sem isso, "quase pixel-perfect" e indetectavel:
a matematica deriva e ninguem percebe ate ver o app rodando.

DESKTOP vs MOBILE POR IDIOM, NAO POR MEDIA QUERY. Os mockups v0.2.0 sao "desktop/tablet" e
"mobile"; em MAUI, Tablet usa o layout desktop. Uma media query de largura classificaria tablet
como mobile. Entao `ReaderPage` escreve `document.documentElement.dataset.idiom = "phone"|"desktop"`
(derivado de `DeviceInfo.Idiom`) e o CSS de `snippets.js` seleciona a variante por
`[data-idiom="phone"]`.

OFFSET VERTICAL DA PILL — RE-DERIVADO, NAO COPIADO. No mockup o reader e a pagina inteira e o
`bottom` da pill e medido a partir da borda inferior da JANELA, que inclui o footer do proprio
mockup. No app o WebView ocupa so a area de conteudo — o footer (`ReaderFooter`, `PageProgressBar`)
e XAML nativo FORA do WebView. Copiar `bottom: 78px` empurraria a pill para o meio do texto. T-1
grava na PIXEL-SPEC as DUAS colunas: valor do mockup e valor derivado
(`mockup - altura do footer do mockup`), e o codigo usa o derivado. Mesma regra para o hint.

Valores do mockup ja extraidos (copiados para o CONTEXT.md para o executor nao depender de
re-extrair os bundles de 5 MB).
