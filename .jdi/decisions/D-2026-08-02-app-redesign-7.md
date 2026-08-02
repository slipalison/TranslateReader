D-2026-08-02-app-redesign-7 (2026-08-02): Gap 4 (busca por titulo/autor na Biblioteca) — ACEITO, e a
forma exata da superficie nova do Core fica travada aqui. LOCKED.
Achado: `ILibraryManager.SearchBooksAsync(query)` JA EXISTE e ja filtra titulo/autor com
`StringComparison.OrdinalIgnoreCase`, MAS devolve `IReadOnlyList<Book>` — e o grid da biblioteca
consome `BookSummary` (capa + progresso). Usar o metodo existente obrigaria a pagina a re-projetar
Book em summary, ou seja, regra de dominio dentro do PageModel: proibido por CLAUDE.md.
FORMA LOCKED (3 mudancas, todas no Core, todas alcancaveis pelo test project atual):
(1) `BookSummary` ganha `DateTime? LastReadAt` (de `progress?.UpdatedAt`) e `int TotalChapters` (de
    `book.TotalChapters`, ja populado por `ParsingEngine.ExtractMetadataAsync` = `ReadingOrder.Count`).
    `LastReadAt` alimenta o hero/Recentes (D-...-5); `TotalChapters` alimenta a linha
    "autor - N capitulos" do popup Traduzir (`desktop-library-translate-popup.jpg`), que hoje so
    recebe `bookTitle` no construtor.
(2) `ListBookSummariesAsync` ganha parametro OPCIONAL: `ListBookSummariesAsync(string query = "")`.
    Query vazia/whitespace = comportamento identico ao de hoje (todos os livros, mesma ordem do
    `FetchAllBooksAsync`), entao nenhum chamador existente e nenhum teste de baseline quebra; query
    nao vazia = mesmo predicado titulo/autor `OrdinalIgnoreCase` ja usado por `SearchBooksAsync`.
    REJEITADO adicionar um 6o metodo `SearchBookSummariesAsync`: `ILibraryManager` iria a 7
    operacoes (CLAUDE.md: 3-5 ideal por contrato) e duplicaria o predicado.
    REJEITADO trocar a assinatura/semantica de `SearchBooksAsync`: ela e baseline coberta por
    `LibraryManagerTests.SearchBooksAsync_FiltersCorrectly` e sumir com o nome viola a regra de nao
    regredir teste existente. Ela fica como esta (fica redundante — vira todo, nao lixo silencioso).
(3) `ListRecentBookSummariesAsync()` (D-...-5).
Filtro fica no Manager, inline, seguindo o precedente literal do `SearchBooksAsync` que ja esta la —
nao se cria Engine novo pra um `Contains` (YAGNI, e Engine e pra regra de negocio volatil).
Debounce/typeahead: NAO. O `Entry` de busca dispara a consulta no evento de texto alterado, sem
timer, sem cancelamento — a biblioteca e local, pequena e ja carregada; inventar debounce aqui e
complexidade sem caso reproduzivel.
