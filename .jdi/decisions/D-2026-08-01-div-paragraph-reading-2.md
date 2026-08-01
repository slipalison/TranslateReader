D-2026-08-01-div-paragraph-reading-2 (escopo real do defeito, corrige o brief): a leitura
interativa NAO passa extracao de HTML pelo C# — `ReaderPageModel.TranslateVisibleParagraphsAsync`
(`ReaderPageModel.cs:230`) chama `TranslationManager.TranslateParagraphsAsync`
(`TranslationManager.cs:284-325`), que recebe os paragrafos JA PRONTOS (a lista vinda do JS,
`VisibleParagraph[]`) e so faz hash/cache/prompt sobre `paragraphs[i].Text` — nunca chama
`HtmlUtility.ExtractTextBlocks`/`ExtractParagraphs`. Confirmado por leitura nesta sessao. Logo o
defeito "na leitura" e 100% JavaScript: `translation.js:7,26,45` faz `pg.querySelectorAll('p')` /
`querySelectorAll('p[data-original]')` nos 3 lugares (`getVisibleParagraphs`, `applyTranslations`,
`clearTranslations`); num EPUB calibre (so `<div class="calibreN">`) isso devolve `[]`,
`ReaderPage.xaml.cs:296` faz `return;` em silencio.
Correcao ao item 4 do brief: a paridade C#/JS que ele supunha importante NAO existe aqui (o C# nunca
roda neste caminho). A paridade que realmente importa e ENTRE as 3 funcoes JS entre si (mesmo
indice = mesma posicao no mesmo NodeList/array) — esse e o risco real do item 2 do brief, e o que
`D-...-3` fecha.
