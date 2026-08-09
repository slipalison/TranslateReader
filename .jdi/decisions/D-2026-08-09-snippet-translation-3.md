D-2026-08-09-snippet-translation-3 (2026-08-09): Camada de trechos independente do modo de leitura,
com `translation.js` de diff VAZIO, LOCKED.

Requisito 4 do usuario: trechos funcionam em paginado E rolagem. Hoje a traducao por paragrafo e
paginado-only — bloqueada em `ReaderPage.xaml.cs:326` com `DisplayAlert`, e `getVisibleParagraphs`
depende de `document.getElementById('_pager')` + `_currentPage`, que so existem em paginado
(`paginated.js`). Em rolagem o conteudo vai para `#chapter-container` e pode conter VARIOS
capitulos, cada um num `.chapter-content[data-chapter-href]` (ver `scroll.js`).

RAIZ PARAMETRIZADA. `snippets.js` expoe UMA funcao resolvedora:

    function _snippetRoots()  ->  [ { root: Element, chapterHRef: string|null }, ... ]

    paginado : [ { root: document.getElementById('_pager'), chapterHRef: null } ]
               (null = "o capitulo corrente", que o C# ja conhece e envia no payload)
    rolagem  : um item por `.chapter-content`, com chapterHRef = dataset.chapterHref

`_pager` e `chapter-content` aparecem UMA vez cada no arquivo, dentro do corpo de `_snippetRoots`.
Todo o resto de `snippets.js` fala com `root`, nunca com o modo. Mesmo design de "fonte unica" que
`D-2026-08-01-div-paragraph-reading-3` impos a `translation.js`: dessincronia entre modos deixa de
ser possivel por construcao.

Os periodos sao construidos sobre `_translatableCandidates(root)` — o helper de `translation.js`,
consumido como global, SEM COPIAR o seletor. Duplicar a regra de "o que e um paragrafo" regride as
phases `div-paragraph-translation` e `div-paragraph-reading`.

`src/TranslateReader/Resources/Raw/wwwroot/js/translation.js` FICA COM DIFF VAZIO nesta phase. Ele
esta protegido por um gate estrutural de DoD da phase 18 e nao tem nada a ganhar aqui.

COEXISTENCIA COM A TRADUCAO POR PARAGRAFO — orquestrada no C#, nao por monkey-patch. `applyTranslations`
faz `ps[idx].textContent = tr`, o que destruiria os spans de periodo, e `clearTranslations` devolve
`dataset.original` como TEXTO PLANO, o que mataria a camada em definitivo. Entao `ReaderPage`:

    antes de `applyTranslations(...)`   -> `unmountSnippetLayer()`
    depois de `clearTranslations()`     -> `mountSnippetLayer()`

Enquanto o modo de traducao por paragrafo esta ativo, a camada de trechos fica desmontada e a pill
nao aparece — trechos so existem sobre o texto ORIGINAL, exatamente como no mockup. Proibido
sobrescrever `window.applyTranslations` a partir de `snippets.js`.

A traducao por paragrafo CONTINUA paginado-only. O `scrollWarn`/`DisplayAlert` de
`ReaderPage.xaml.cs:326-330` fica intacto — liberar aquilo no scroll e escopo de outra phase
(registrado em `.jdi/todos/2026-08-09-snippet-translation.md`).

REJEITADO criar os spans so no capitulo visivel em rolagem (lazy por scroll): mais estado, mais
teste e um modo de falha novo (trecho persistido invisivel porque o capitulo ainda nao foi montado),
sem medicao que prove que o DOM extra doi. Se doer, a saida ja esta nomeada no todo.
