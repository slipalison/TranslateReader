D-2026-08-01-div-paragraph-reading-3 (fix no JS, uma unica fonte de selecao): `translation.js`
ganha `function _translatableCandidates(pg)` — nao exportada em `window`, mesmo padrao de
`_stepW`/`_applyLayout` ja usado em `paginated.js` — que roda
`pg.querySelectorAll('p, h1, h2, h3, h4, h5, h6, li, div')` (ordem de documento preservada) e
filtra: um elemento `DIV` so entra se (a) `el.querySelector('div, p, h1, h2, h3, h4, h5, h6, li')`
for `null` (folha — mesma nocao de "leaf" de `D-2026-08-01-div-paragraph-translation-7`, aqui via
arvore DOM real em vez de lookahead de regex) e (b) o texto (`dataset.original ?? textContent`)
casar `/\p{L}/u` (guarda de letra Unicode, espelha `ContainsLetter` do C#); os demais tags entram
com o filtro de texto nao-vazio que ja existia. `getVisibleParagraphs`, `applyTranslations` e
`clearTranslations` (que passa a filtrar por `dataset.original !== undefined` em vez de
`querySelectorAll('p[data-original]')`) chamam SO essa funcao — dessincronia deixa de ser possivel
por construcao (fecha o risco do item 2 do brief).
`test/js/harness.js` (`D-2026-07-31-coverage-90-1`, zero dependencia nova) ganha suporte a grupos de
selector separados por virgula em `parseSelector`/`matchDescendants` (`querySelectorAll('a, b')` =
uniao) — extensao necessaria porque a producao passa a usar selector composto; nenhum outro recurso
CSS (`:not`, `:has`, combinadores) e adicionado ao harness, escopo raso de proposito.
REJEITADO: `:has()`/CSS puro para achar div-folha — suporte inconsistente entre motores de WebView
(WebView2/WKWebView/Android WebView) nas versoes minimas do app; `querySelector` filho aninhado e
suportado universalmente e nao introduz risco de compatibilidade.
