D-2026-08-02-pixel-perfect-4 (2026-08-02): Toggle grid/list ACEITO — reverte D-2026-08-02-app-redesign-6
com evidencia nova, LOCKED.
A D-...-6 da phase app-redesign rejeitou o toggle baseada no DESIGN-REFERENCE.md, que afirmava que
o toggle era "decorativo no prototipo". Testado AO VIVO nesta sessao: clicar no icone `rows` do
mockup desktop TROCA o layout de verdade para uma list view completa (row 84h com capa 40x60,
titulo/autor, mini progress + %, botao ⋮). Ground truth novo: screenshot
`design/screenshots/desktop-library-list.jpg` + secao "LIST VIEW" do PIXEL-SPEC.
Implementacao: 2 CollectionViews irmas (`BooksCollection` grid — preservada — e
`BooksListCollection` list) alternadas por `IsListView` no `LibraryPageModel`
(`ShowGridViewCommand`/`ShowListViewCommand`); trocar `ItemsLayout` em runtime foi rejeitado
(bugs conhecidos de CollectionView no MAUI, e 2 views e mais simples pra LLM menor).
Escopo: toggle e list view SO no desktop — o mockup mobile nao tem o toggle.
O DESIGN-REFERENCE.md ganha correcao apontando pra ca (gap 3 estava errado).
