shipped_at: 2026-07-30T11:55:47Z
verdict: APPROVED_WITH_WARNINGS
by: Alison Amorim

## Learnings
- Um `Verify:` de DoD que conta atributos (`grep -c ... -ge N`) ou faz grep de substring e satisfeito por teste que nao afirma nada. A unica prova de que um teste fixa comportamento e mutar esse comportamento em `src/`, confirmar que o teste novo falha, e reverter — 2a phase seguida em que "DoD por grep nao mede claim falso" apareceu (a 1a foi `readme`).
- Asserção secundaria (`DidNotReceive()`) so e load-bearing se o caminho ate ela for alcancavel: provar com mutacao que MOVE o efeito (throw depois da chamada), nao so que o remove — remover falha na asserção primaria e nao exercita a secundaria.
- NSubstitute: auto-value de `Task<string?>` e `string.Empty`, nao null. Em `TranslationManager` isso e lido como cache HIT e o teste passa vacuamente. Configurar o mock de cache explicitamente (`.Returns((string?)null)`) sempre que a asserção dependa do miss.
- `dotnet format --verify-no-changes` JA falha no baseline deste repo (12 violacoes WHITESPACE legadas, isentas por D-2) e no escopo da solucao exige `core.longpaths=true` (fixtures EPUB estouram MAX_PATH). Comparar contra o baseline; nunca esperar lista limpa nem reformatar legado.
- Ao reportar saida de ferramenta, declarar o ESCOPO medido: dois warnings de precisao nesta phase nasceram de apresentar recorte parcial como total (7 violacoes do test project ditas como "a lista", quando a solucao tem 12; e uma linha de tabela de mutacao sem o resultado do teste).
