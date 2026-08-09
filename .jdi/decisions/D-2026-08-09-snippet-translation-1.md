D-2026-08-09-snippet-translation-1 (2026-08-09): Persistencia e ancoragem do trecho traduzido, LOCKED.

Requisito 1 do usuario (fechar e reabrir o livro mantem os trechos traduzidos visiveis) NAO existe
no mockup — la os snips vivem em `this.state.snips` e morrem ao abrir o livro. Logo a persistencia e
invencao desta phase e precisa de um modelo proprio.

TABELA NOVA `SnippetTranslations` (DDL inline `CREATE TABLE IF NOT EXISTS` no proprio Access, como
as 7 tabelas ja existentes — o repo nao tem framework de migration):

    Id INTEGER PK AUTOINCREMENT
    BookId INTEGER NOT NULL
    ChapterHRef TEXT NOT NULL
    ParagraphIndex INTEGER NOT NULL      -- indice na lista de _translatableCandidates
    SentenceStart INTEGER NOT NULL       -- `a` do run contiguo
    SentenceEnd INTEGER NOT NULL         -- `b` do run contiguo
    OriginalHash TEXT NOT NULL           -- hash do texto original a..b (guarda de integridade)
    TranslatedText TEXT NOT NULL
    ShowingOriginal INTEGER NOT NULL     -- estado do toggle, persistido
    CreatedAt TEXT NOT NULL
    UNIQUE(BookId, ChapterHRef, ParagraphIndex, SentenceStart, SentenceEnd)

ANCORAGEM = indice + hash como GUARDA. Ao abrir o capitulo, o trecho so e restaurado se o hash do
texto original `a..b` reconstruido do DOM bater com `OriginalHash`. Divergiu -> o trecho e
DESCARTADO EM SILENCIO (a linha permanece no banco, mas nao renderiza). Regra inegociavel: e
preferivel perder um trecho a colar uma traducao no periodo errado.

REJEITADO "reancorar por hash varrendo o capitulo": custo O(paragrafos x periodos) a cada abertura
de capitulo, contra `.claude/rules/csharp.md` §2 (hot path de leitura), para um cenario — paragrafo
mudar de posicao — que so ocorre se `_translatableCandidates` mudar, e nesse dia a guarda de hash ja
protege o usuario do erro visivel.
REJEITADO "so indice, sem hash": sem guarda, uma mudanca futura no seletor de blocos faz a traducao
reaparecer colada no periodo errado, sem aviso — exatamente a classe de defeito silencioso que as
phases `div-paragraph-translation` e `div-paragraph-reading` existiram para matar.

O ESTADO DO TOGGLE PERSISTE (coluna `ShowingOriginal`). Reabrir o livro devolve cada trecho no
mesmo estado em que o usuario o deixou, nao num default.

`TranslationCache` NAO e reusado para saber quais trechos existem — ele nao tem nocao de "ativo",
e cache puro de custo de inferencia (ver D-2026-08-09-snippet-translation-5, que o mantem nesse
papel).

Contrato novo `ISnippetTranslationAccess` em `Contracts/Access/` (3-5 operacoes, nomes
comportamentais, ZERO vazamento de SQL/SQLite na interface — CLAUDE.md regra 4):
`FetchSnippetsAsync(bookId, chapterHRef)`, `SaveSnippetAsync(snippet)`,
`RemoveSnippetAsync(bookId, chapterHRef, paragraphIndex, sentenceStart, sentenceEnd)`,
`SetShowingOriginalAsync(...)`, `RemoveSnippetsForBookAsync(bookId)`. Model
`SnippetTranslation` em `Models/`. Registro no DI de `MauiProgram.cs` junto dos demais Access.
