shipped_at: 2026-08-01T01:13:29Z
verdict: APPROVED_WITH_WARNINGS
by: Alison Amorim

## Learnings
- Antes de "otimizar" uma estrutura de dados, cheque a API por baixo: o `Dictionary` nao era a causa raiz — `ReadBookAsync` (eager) ja tinha carregado 44 MB antes de ele existir.
- Fix de memoria precisa de gate MEDIDO alem do estrutural: um grep passa com `OpenBookAsync` que acumula tudo numa lista; so a medicao de pico retido pega.
- `yield return` nao pode viver dentro de `try/catch` — fallback em iterador vira helper separado, e as opcoes compartilhadas provam "mesma tolerancia" por estrutura.
- Teste de memoria confiavel mede alocacao/retencao (`GC.GetTotalMemory`), nunca tempo de parede; com warm-up, full-GC no baseline e `DisableParallelization`.
- Assinatura nova com `CancellationToken` que o unico chamador passa como `default` e proxy que nao prova nada — ou o token flui na cadeia inteira, ou fica registrado como achado.
