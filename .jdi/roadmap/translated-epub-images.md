---
order: 19
name: Imagem quebrada em livro traduzido
---
- **Slug:** translated-epub-images
- **Goal:** o EPUB traduzido nasce com as imagens quebradas — `ParsingEngine.ReplaceImageRef` reescreve todo `<img src>` para `https://epub-images/{bookDir}/{path}` (URL do virtual host do WebView) e o `TranslationManager` chama `ExtractChapterContentAsync` com `imagesDirectory` vazio, entao o HTML gravado dentro do EPUB pelo `CreateTranslatedEpubAsync` aponta para um host que so existe no app e com o diretorio do livro VAZIO; ao abrir o livro traduzido da biblioteca aparece imagem quebrada, e o arquivo tambem fica invalido em qualquer outro leitor — entrega a correcao da geracao, a prova de que nenhuma URL do app vaza para dentro do artefato, e a decisao explicita sobre os livros ja gerados quebrados
