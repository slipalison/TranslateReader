D-2026-08-09-snippet-translation-5 (2026-08-09): Semantica da traducao do trecho — prompt com
contexto, cache reusado e sobreposicao destrutiva, LOCKED.

O mockup FINGE a traducao: `_snipText(pIdx, a, b)` recorta um PT-BR pre-escrito com regra
proporcional. No app real o LLM precisa receber texto. Decisao:

PROMPT = TRECHO + PARAGRAFO COMO CONTEXTO. O modelo recebe o texto do run contiguo `a..b` como o
que deve traduzir, e o paragrafo INTEIRO como contexto, com instrucao explicita de devolver
SOMENTE a traducao do trecho. Sem isso, um periodo isolado perde sujeito e referencia de pronome
("Ela disse que...") — que e justamente o caso de uso da feature (o leitor seleciona o periodo que
NAO entendeu).

`IPromptUtility` ganha a segunda operacao `BuildSnippetTranslationMessages(snippet, paragraph,
sourceLanguage, targetLanguage, bookTitle, chapterTitle)`. Contrato fica com 2 operacoes — dentro
do limite de CLAUDE.md. NAO sobrecarregar `BuildTranslationMessages` com parametro opcional: os
dois prompts tem instrucoes diferentes e um default silencioso viraria bug de qualidade invisivel.

CACHE: `TranslationCache`, a tabela que ja existe, com
`OriginalHash = ComputeHash(textoDoTrecho, sourceLanguage, targetLanguage)` — mesmo `ComputeHash`
do `TranslationManager`, mesma chave `(BookId, ChapterHRef, OriginalHash)`. Um trecho identico em
outro lugar do mesmo capitulo acerta o cache e nao paga inferencia de novo. `TranslationCache`
continua sendo SO cache; a lista de trechos ativos vem de `SnippetTranslations`
(D-2026-08-09-snippet-translation-1).

SOBREPOSICAO E DESTRUTIVA (regra do mockup, mantida). Ao traduzir a selecao, cada run contiguo
`a..b` apaga todo trecho ja existente no MESMO paragrafo que intersecte `a..b` — condicao literal
`!(o.b < a || o.a > b)`. Apaga do DOM E do banco. Nao existe trecho aninhado nem sobreposto.

A selecao vira runs contiguos antes de traduzir: `_runsOf(set)` agrupa indices consecutivos, e cada
run e UM trecho independente (uma linha em `SnippetTranslations`, uma chamada de inferencia). Chave
do trecho no DOM: `chapterHRef:paragraphIndex:a:b`.

CONTRATO DE MANAGER — SEGUNDO CONTRATO, NAO 10a OPERACAO. `ITranslationManager` ja tem 9 operacoes,
acima do ideal de 3-5 de CLAUDE.md. A feature entra por um contrato novo
`Contracts/Managers/ISnippetTranslationManager` (`TranslateSnippetAsync`, `FetchSnippetsAsync`,
`SetShowingOriginalAsync`, `RemoveSnippetAsync`), IMPLEMENTADO PELA MESMA CLASSE
`TranslationManager` (CLAUDE.md permite ate 2 contratos por servico). Assim nao ha chamada sincrona
Manager -> Manager, o `ReaderPageModel` continua com no maximo 1 Manager por caso de uso, e
`ITranslationManager` nao incha.

Fluxo assincrono: `ReaderPageModel` -> `ISnippetTranslationManager` -> `ITranslationEngine` /
`ITranslationCacheAccess` / `ISnippetTranslationAccess`. `CancellationToken` flui ponta a ponta;
`OperationCanceledException` nunca e engolida; a marcacao de estado na UI so acontece na thread
principal (`.claude/rules/csharp.md` §1 e §3). O download/carga do modelo reusa o fluxo
`DownloadModelIfNeededAsync` / `InitializeEngineIfNeededAsync` que ja existe — equivalente ao
`ensureModel()` do mockup.
