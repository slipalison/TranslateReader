D-2026-08-01-translated-epub-images-4 (os 4 call sites de `ExtractChapterContentAsync` em producao,
decididos individualmente): nenhum dos 3 call sites de `TranslationManager` muda o argumento
`imagesDirectory` (continuam passando `string.Empty` — irrelevante em `Purpose.Export`, que nunca
le esse parametro); todos os 4 ganham o novo argumento obrigatorio.
- `ReadingManager.LoadChapterContentAsync` (`ReadingManager.cs:30`) -> `Purpose.Display`. E o UNICO
  call site de exibicao do app inteiro: usa `imagesDir` real e ja correto
  (`Path.Combine(booksDirectory, "images", bookId.ToString())`, `ReadingManager.cs:28`), devolvido
  ao WebView via `ChapterHtmlResult`.
- `TranslationManager.TranslateSingleChapterAsync` (`TranslationManager.cs:123`) -> `Purpose.Export`.
  Confirmado por leitura: o `html` retornado alimenta SO
  `HtmlUtility.ExtractTextBlocks(HtmlUtility.ExtractBodyContent(html))` (linha 124) para popular o
  cache de traducao — a variavel e descartada em seguida, nunca chega a WebView nem a disco.
- `TranslationManager.RebuildAllTranslatedChaptersAsync` (`TranslationManager.cs:193`) ->
  `Purpose.Export`. Este E o call site da causa raiz: sua saida (`translatedChapters[href]`) e o
  dicionario que `CreateTranslatedEpubAsync` grava dentro do artefato exportado
  (`TranslationManager.cs:73-75`).
- `TranslationManager.TranslateChapterAsync` (`TranslationManager.cs:242`) -> `Purpose.Export`.
  Mesmo raciocinio do call site de 123: so consome `HtmlUtility.ExtractBodyContent` +
  `HtmlUtility.ExtractParagraphs` (linhas 243-244) para traducao interativa por paragrafo
  (`IAsyncEnumerable<TranslatedParagraph>`); o `ReaderPage` ja tem o capitulo ORIGINAL carregado
  via `LoadChapterContentAsync` e so recebe TEXTO traduzido pela bridge, nunca HTML inteiro
  re-renderizado por este caminho.
Nenhum dos 3 call sites de `TranslationManager` precisa de imagem/CSS reescritos — os 3 so extraem
TEXTO. Isso confirma que a mistura de responsabilidades em `ExtractChapterContentAsync` nunca teve
justificativa nesses 3 pontos: eles sempre pediram HTML "para ler texto", nunca "para exibir".
Diff de producao esperado, escopo fechado: `Models/ChapterContentPurpose.cs` (novo),
`Contracts/Engines/IParsingEngine.cs` (assinatura), `Business/Engines/ParsingEngine.cs` (ramificacao
+ guarda), `Business/Managers/TranslationManager.cs` (3 call sites), `Business/Managers/ReadingManager.cs`
(1 call site). Nenhum outro arquivo de producao muda.
