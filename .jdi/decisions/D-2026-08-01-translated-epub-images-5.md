D-2026-08-01-translated-epub-images-5 (ponto de vista 2 do card — capa: PARCIALMENTE REFUTADO,
parcialmente confirmado-sem-acao-extra): a miniatura da biblioteca (`BookSummary.CoverImagePath`,
exibida na grid ao "abrir da biblioteca") NAO sofre deste defeito. Evidencia:
`LibraryManager.SaveCoverImageAsync` (`LibraryManager.cs:81-90`) chama
`ParsingEngine.ExtractCoverImageAsync` (`ParsingEngine.cs:68-79`), que le BYTES CRUS da imagem de
capa direto do manifesto EPUB por 3 caminhos — `epub.CoverImage`, `epub.Content.Cover?.Content`, ou
`FindCoverInManifest` (`ParsingEngine.cs:312-332`, busca por `EpubManifestProperty.COVER_IMAGE` /
id `"cover"` / href contendo `"cover"`) — NENHUM desses 3 caminhos passa por
`ExtractChapterContentAsync`/`RewriteImagePaths`; sao arquivos de IMAGEM binaria referenciados pelo
manifesto, nao a pagina XHTML de capa. A miniatura e salva como arquivo LOCAL
(`covers/{nome}_cover.jpg`) e referenciada por path de disco (`LibraryManager.cs:86-89`), nunca por
URL do virtual host `epub-images`. Hipotese do card ("a miniatura pode ser a imagem quebrada") fica
REFUTADA para este caminho.

PORÉM, confirmado (nao e um defeito SEPARADO, e o MESMO root cause -2/-3): SE o EPUB tiver uma
pagina XHTML de capa no spine (`epub.ReadingOrder`, i.e., faz parte de `chapters`), essa pagina
sofre exatamente a mesma corrupcao de `Purpose.Export`/`Purpose.Display` que qualquer outro
capitulo. Evidencia de que isso ocorre em pelo menos 1 fixture real do proprio repo: o teste
JA EXISTENTE `Practice_ExtractChapterContentAsync_RewritesImagePathsToVirtualHostUrl`
(`test/TranslateReader.Tests/ParsingEngineTests.cs:75-85`) seleciona deliberadamente
`chapters.First(c => c.HRef.Contains("cover") || c.HRef.Contains("ad") || c.HRef.Contains("title"))`
— ou seja, o `ReadingOrder` do fixture Practice Makes Perfect TEM uma entrada de capitulo com
"cover" no href. Se o usuario abrir para LER (nao so ver a miniatura) um livro traduzido cuja
primeira pagina e a capa, essa pagina fica com `<img>`/`<image>` quebrado ate D-...-3/D-...-4 serem
aplicadas — e fica CORRIGIDA pela MESMA correcao, sem nenhum tratamento especial de "capa" no
codigo. Nenhuma decisao de codigo adicional necessaria alem do que -3/-4 ja cobrem.
