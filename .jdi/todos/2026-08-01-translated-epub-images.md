## De `translated-epub-images` (2026-08-01)

- **[BUG, confirmado, adiado]** Cobertura de forma dos regexes de reescrita de imagem
  (`ParsingEngine.cs:352-359`, `ImgSrcRegex`/`SvgImageXlinkHrefRegex`/`SvgImageHrefRegex`) so casa
  atributo com ASPAS DUPLAS. `<img src='...'>` (aspas simples), `<img src=foo.png>` (sem aspas),
  `srcset`, `<picture><source>` e `background-image` em `style="..."` inline nunca sao reescritos
  para o virtual host `epub-images` — ficam com o path relativo original, que quebra NA LEITURA
  dentro do app (WebView), em QUALQUER livro (traduzido ou nao). Ortogonal ao defeito corrigido em
  `translated-epub-images` (nao aciona `ReplaceImageRef`, entao nao pode vazar URL do app pro
  artefato exportado). Nenhum dos 3 fixtures reais do repo exercita essas formas — candidato a
  fase de robustez de `HtmlUtility`/`ParsingEngine` quando houver caso reproduzivel. Ver
  D-2026-08-01-translated-epub-images-6(a).

- **[BUG, confirmado, adiado]** `FindImage`/`FindCss` (`ParsingEngine.cs:272-279`, `230-237`) nao
  decodificam `%XX` nem normalizam alem de `..`/`.` (`NormalizePath`, `ParsingEngine.cs:287-303`).
  Um `src` percent-encoded (`Images/My%20Book.png`) contra um `FilePath` de manifesto nao
  codificado (`Images/My Book.png`) falha o match e cai no mesmo caminho de path original
  preservado — mesma familia do item acima, mesmo motivo de adiamento. Ver
  D-2026-08-01-translated-epub-images-6(b).

- **[PRODUTO/UX, decisao humana]** Comunicar ao usuario, no lugar apropriado da UI (nao decidido
  nesta fase), que livros ja traduzidos ANTES da correcao continuam com imagem quebrada e precisam
  ser apagados da biblioteca + retraduzidos (o livro original nao foi afetado e o cache de traducao
  ja existente torna a retraducao rapida). Ver D-2026-08-01-translated-epub-images-7 e
  `## Deferred to PR review` de `.jdi/phases/translated-epub-images/CONTEXT.md`.
