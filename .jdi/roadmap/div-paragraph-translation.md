---
order: 17
name: Traducao cega a paragrafo em `<div>` (EPUB de calibre)
---
- **Slug:** div-paragraph-translation
- **Goal:** traduzir o texto de EPUBs cujos paragrafos sao `<div>` e nao `<p>` — hoje `HtmlUtility.ExtractTextBlocks` casa so `p|h1-h6|li` e, num livro real do usuario, enxergou 360 de 2.274 blocos (11,2% do texto), gerou o EPUB "traduzido" com 48 de 53 documentos ainda em ingles e nao avisou nada; entrega tambem o sinal de cobertura de traducao, para que um livro de formato inesperado deixe de falhar em silencio
