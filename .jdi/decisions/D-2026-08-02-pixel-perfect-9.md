D-2026-08-02-pixel-perfect-9 (2026-08-02): Correcao do piso numerico do DoD 10 (367 -> 365
baseline), LOCKED.
O `CONTEXT.md` original (D-...-8) fixou a baseline da suite em "367 testes (365 passed + 2
skipped)" e o piso do DoD 10 em `Total >= 377`. Essa contagem estava ERRADA: verificacao
independente (`git grep -cE '^\s*\[Fact'` + `[InlineData]` no commit BASELINE
`82df8420ab306c3f5a06e07edc72a0469e5af65c`, cruzada com `dotnet test` rodando 375/375 apos os 10
testes novos da phase, 0 falhas, 2 skips) confirma baseline REAL = 365 (316 `[Fact]` + 49
`[InlineData]`), nao 367. A doer da iteracao 1 chegou no mesmo numero de forma independente
(`git stash` + suite no commit pre-phase) e reportou o DoD 10 como falhando por essa causa —
achado correto, root-caused aqui em vez de contornado com um teste de preenchimento.
Correcao: DoD 10 do CONTEXT.md passa a exigir `Total >= 375` (365 + 10, nao 367 + 10). Nao ha
mudanca de escopo, decisao de arquitetura ou comportamento do app — e correcao de um erro de
contagem cometido no planejamento, mesma categoria de fix que
D-2026-08-02-app-redesign (DoD 1, hex `#1A1A1A`): raiz corrigida no artefato que estava errado
(aqui, o proprio CONTEXT.md), nao um workaround no lado do teste.
