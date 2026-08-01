D-2026-08-01-translated-epub-images-3 (comportamento por `ChapterContentPurpose`, fecha TAMBEM o
ponto de vista 1 do card sobre `InlineCssLinks`): `ExtractChapterContentAsync` passa a ramificar por
`purpose`:
- `Purpose.Export`: devolve `item.Content` (o HTML cru do capitulo, lido do `EpubBook`) SEM chamar
  `RewriteImagePaths` NEM `InlineCssLinks`. Nenhuma mutacao alem do que ja acontecia antes dessas
  duas chamadas.
- `Purpose.Display`: comportamento ATUAL inalterado (`RewriteImagePaths` + `InlineCssLinks`), MAIS
  uma guarda nova: `if (purpose == ChapterContentPurpose.Display && string.IsNullOrWhiteSpace(imagesDirectory)) throw new InvalidOperationException(...)`,
  posicionada ao lado das 2 guardas ja existentes no metodo (`ParsingEngine.cs:50-53`, ambas
  `InvalidOperationException` — escolhida por consistencia de estilo com o resto do MESMO metodo, em
  vez de `ArgumentException`, que nao tem nenhum uso hoje em `src/TranslateReader.Core`). Essa guarda
  torna a causa raiz historica (imagesDirectory vazio chegando no branch que monta a URL) um estado
  IMPOSSIVEL de alcancar em runtime para qualquer call site futuro, nao so os 4 de hoje —
  "prova de que nao vaza" vira propriedade estrutural do contrato, nao disciplina do chamador.

FECHA o ponto de vista 1 do card (`InlineCssLinks` tambem muta o HTML gravado no EPUB) SEM logica
adicional: `RebuildAllTranslatedChaptersAsync` (`TranslationManager.cs:193,201`) passa o `html`
INTEIRO (nao so o body) retornado por `ExtractChapterContentAsync` para
`HtmlUtility.ReplaceTextBlocksInHtml(html, translations)`, que so troca texto DENTRO de
`<p>`/`<div>` no corpo e devolve o resto do documento (incluindo `<head>`/`<link>`/`<img>`)
EXATAMENTE como recebeu. Logo, qualquer mutacao que `ExtractChapterContentAsync` aplicar sobrevive
verbatim no artefato final. Com `Purpose.Export` pulando as duas mutacoes, nem os `<img>`/`<image>`
nem os `<link rel="stylesheet">` do capitulo sao tocados — o `<link>` original permanece apontando
para o arquivo CSS real dentro do proprio zip (que `CreateTranslatedEpubAsync` nunca sobrescreve,
so as entradas listadas em `translatedChapterHtml`), entao o EPUB exportado fica um documento
padrao, portavel, sem dependencia de nenhum host do app.
Nenhuma mudanca em `RewriteImagePaths`/`InlineCssLinks`/`ReplaceImageRef`/`FindImage`/`FindCss`
internamente — o diff fica inteiro na nova ramificacao de `ExtractChapterContentAsync`.
