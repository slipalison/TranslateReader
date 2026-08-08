D-2026-08-02-pixel-perfect-8 (2026-08-02): Formato do plano para executor LLM menor, LOCKED.
Esta phase sera executada por uma LLM menor/inferior a que planejou. O PLAN.md portanto:
(1) e SEQUENCIAL — zero paralelismo, uma task por vez, na ordem T-1..T-9; cada task toca o menor
conjunto de arquivos possivel e assume o repo no estado deixado pela anterior.
(2) cada task tem passos numerados IMPERATIVOS com valores literais (nada de "ajuste conforme o
mockup" — o valor esta escrito na task ou referenciado por secao nomeada do PIXEL-SPEC).
(3) cada task termina com um "Criterio de sucesso" que e um comando bash LITERAL (Git Bash no
Windows) — exit 0 = task concluida; exit != 0 = a task NAO esta concluida, corrigir antes de
seguir. Proibido pular ou reordenar tasks.
(4) cada task tem um bloco "NAO FACA" com os erros previstos (ex.: inverter ordem CSS->MAUI de
padding, mexer no Core, remover x:Name existente).
(5) commit atomico por task, conventional commit `style(pixel-perfect): T-N <resumo>`
(`feat(...)` para T-3/T-4 que adicionam list view/toggle, `test(...)` para T-9).
(6) baseline de testes fixada numericamente no DoD (piso absoluto), sem depender de
`origin/main` — a branch parte de `feat/app-redesign` (PR #20 ainda aberto) e um merge-base
calculado daria falso positivo.
