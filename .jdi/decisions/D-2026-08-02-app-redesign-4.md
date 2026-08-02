D-2026-08-02-app-redesign-4 (2026-08-02): Gap 1 (TOC de capitulos no Reader) — ACEITO, escopo
Client Layer puro, visivel so no idiom Desktop. LOCKED.
Verificado por leitura direta do codigo nesta sessao: `IReadingManager.LoadChaptersAsync(bookId)` JA
EXISTE e `ReaderPageModel` JA carrega `Chapters` + `CurrentChapterIndex` no `InitializeAsync` — o
dado do TOC ja esta em memoria desde a abertura do livro. Entao o painel e 100% Client: uma
`CollectionView` ligada a `Chapters` (numero de ordem + titulo, item atual destacado com o accent
`#9184D9` sobre `accent-900`, exatamente `design/screenshots/desktop-reader-toc.jpg`) + UM comando
novo no `ReaderPageModel` (`GoToChapterAsync(int index)`, que so faz `CurrentChapterIndex = index` e
reusa o `LoadCurrentChapterAsync` privado ja existente). NENHUMA operacao nova em Manager/Engine/
Access.
Geometria travada pelo screenshot: no desktop o painel e INLINE a esquerda (largura ~250px, a area
de conteudo comeca depois dele — ele empurra o conteudo, nao flutua por cima); o botao que o abre e
o hamburguer entre o "voltar" e o titulo.
Idiom: o hamburguer aparece SO no `OnIdiom Desktop`. Motivo verificado: `mobile-reader.jpg` mostra a
barra superior mobile com exatamente 4 elementos (voltar, titulo + "Cap. 1 de 6", "Aa", engrenagem)
e NENHUM hamburguer — colocar um no mobile seria inventar affordance que o design nao tem. O modo
Rolagem ja concatena o livro inteiro (`LoadScrollContentAsync`), entao a ausencia de TOC no mobile
nao deixa nenhum capitulo inalcancavel. TOC no mobile fica registrado como todo, nao como falha.
Comportamento ao escolher capitulo: fecha o painel, carrega o capitulo e salva progresso pelo
caminho que ja existe — nao inventar historico de navegacao, nem breadcrumb, nem busca no TOC.
