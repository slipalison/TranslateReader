D-2026-08-02-app-redesign-5 (2026-08-02): Gap 2 (nav "Recentes") — ACEITO COM ESCOPO REDUZIDO: vira
filtro de estado dentro da propria `LibraryPage`, nunca uma rota/pagina nova; e a fonte de "recente"
e `ReadingProgress.UpdatedAt`, NAO `Book.LastOpenedAt`. LOCKED.
ACHADO QUE FORCA A DECISAO (grep completo nesta sessao, `LastOpenedAt` em todo o repo): a coluna
`Books.LastOpenedAt` NUNCA e escrita por nenhum caminho de producao. Ela aparece so no DDL, no INSERT
de `SaveBookAsync` (com o valor que veio do parsing do EPUB, ou seja, sempre null),
na leitura de `MapBook` e no `ORDER BY LastOpenedAt DESC, DateAdded DESC` de `FetchAllBooksAsync`.
`ReadingManager.OpenBookAsync` so faz `booksAccess.FetchBookAsync(bookId)` — nao marca nada. Logo,
uma tela "Recentes" ordenada por `LastOpenedAt` mostraria hoje exatamente a mesma coisa que a
Biblioteca, ordenada por `DateAdded` — uma feature que MENTE pro usuario.
REJEITADO: (a) construir o write path de `LastOpenedAt` (nova operacao em `IBooksAccess` + UPDATE +
chamada no `ReadingManager`) — e feature de produto nova, o card nao pede e o mockup (prototipo
estatico) nao prova; (b) `ShellContent`/rota nova `recent` com pagina propria — 3 artefatos novos
(rota, page, pagemodel) pra uma lista que e a mesma da Biblioteca com outro filtro.
ACEITO: a sidebar mantem os 2 itens do screenshot ("Biblioteca" e "Recentes") como um seletor de
FILTRO na mesma pagina (a propria `LibraryPage` troca a colecao exibida), alimentado por dado REAL
que ja e escrito hoje: `ReadingProgress.UpdatedAt`, gravado a cada `SaveProgressAsync` durante a
leitura. "Recentes" = livros que tem progresso de leitura, ordenados por `UpdatedAt` desc. A MESMA
consulta alimenta o hero "CONTINUE LENDO" do topo (o primeiro item dessa lista) — sem ela o hero nao
teria como escolher o livro certo, e hardcodar o primeiro da biblioteca seria mentira equivalente.
Forma no Core (ver D-2026-08-02-app-redesign-7): `ILibraryManager.ListRecentBookSummariesAsync()`,
que reusa a projecao de summary ja existente (o `foreach` de `ListBookSummariesAsync` ja tem o
`ReadingProgress` de cada livro em maos), filtra `LastReadAt != null` e ordena desc. ZERO mudanca em
`IReadingStateAccess`, zero SQL novo, zero migration.
