---
order: 18
name: Traducao interativa cega a paragrafo em `<div>` (leitura)
---
- **Slug:** div-paragraph-reading
- **Goal:** aplicar a traducao interativa por paragrafo visivel (usada pelo ReaderPage) a mesma correcao que `div-paragraph-translation` entregou para o livro completo — hoje `HtmlUtility.ExtractParagraphs` casa so `<p\b[^>]*>(.*?)</p>`, entao num EPUB de calibre cujos paragrafos sao `<div class="calibreN">` a traducao durante a leitura devolve zero paragrafos e nao avisa nada; o defeito de classe ja esta nomeado em `.jdi/todos.md` (secao `De div-paragraph-translation`), onde foi deixado fora de escopo por decisao explicita do usuario
