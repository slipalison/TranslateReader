D-2026-08-01-div-paragraph-reading-5 (sinal de falha, so JS; escopo do modo Scroll confirmado):
`getVisibleParagraphs` emite `console.warn` quando `pg` existe, `pg.textContent.trim()` nao e vazio
e `_translatableCandidates(pg)` devolve `[]` para o capitulo inteiro — mesmo canal ja usado em
`paginated.js` ("chapter-container NOT FOUND"). Fica so em JS: nenhuma mudanca em
`ReaderPage.xaml.cs`/`ReaderPageModel.cs` (fora da rede de testes, D-2026-07-30-regression-suite-2)
e necessaria, porque com o fix de `D-...-3` uma pagina calibre real deixa de cair nesse ramo — o
warn cobre so o residuo (markup realmente sem paragrafo reconhecivel). Aviso VISIVEL ao usuario
(toast/alert) fica em `## Deferred to PR review`, mesma decisao de produto ja tomada em
`D-2026-08-01-div-paragraph-translation-4`.
Confirmado por leitura (item 5 do brief): modo Scroll nao tem traducao por paragrafo —
`scroll.js` so sincroniza `.chapter-content` (progresso de leitura), sem funcao de traducao alguma;
`OnTranslateButtonClicked` (`ReaderPage.xaml.cs:261-264`) bloqueia com `DisplayAlert` antes de
chegar em qualquer codigo paginado. Comportamento INTACTO nesta phase — registrado para nao virar
buraco depois.
