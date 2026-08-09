D-2026-08-09-snippet-translation-6 (2026-08-09): Formato do plano para execucao por Sonnet (high),
LOCKED.

Requisito 5 do usuario: esta phase sera executada por uma LLM menor que a que planejou. Mesma
estrategia que fez a phase 22 (`pixel-perfect`) passar — ver `D-2026-08-02-pixel-perfect-8`. O
PLAN.md portanto:

(1) E SEQUENCIAL. Zero paralelismo, uma task por vez em ordem fixa T-1..T-N. Cada task toca o menor
conjunto de arquivos possivel e assume o repo no estado deixado pela anterior.

(2) T-1 E A SPEC, NAO CODIGO. Produz `design/v0.2.0/PIXEL-SPEC.md` + screenshots
(D-2026-08-09-snippet-translation-4) e grava `git rev-parse HEAD` em
`.jdi/phases/snippet-translation/BASELINE` (arquivo commitado) — ancora dos DoDs de fronteira.
Se o executor nao conseguir renderizar os bundles de 5 MB, a spec e as screenshots sao produzidas
na sessao de `/jdi-plan` e T-1 vira "verificar que a spec existe e esta completa".

(3) PASSOS NUMERADOS E IMPERATIVOS, com valores LITERAIS. Nada de "ajuste conforme o mockup": ou o
valor esta escrito na task, ou esta referenciado por secao NOMEADA da PIXEL-SPEC.

(4) CRITERIO DE SUCESSO POR TASK = COMANDO BASH LITERAL (Git Bash no Windows, executado da raiz do
repo). Exit 0 = task concluida; exit != 0 = NAO concluida, corrigir antes de seguir. Proibido pular
ou reordenar tasks.

(5) BLOCO "NAO FACA" POR TASK com os erros previstos desta phase, no minimo: nao editar
`translation.js`; nao duplicar o seletor de `_translatableCandidates`; nao sobrescrever
`window.applyTranslations`; nao copiar `bottom: 78px` do mockup sem re-derivar; nao criar `.cs`
novo em `src/TranslateReader/`; nao esquecer as 2 linhas de `scripts/coverage-gate.sh`; nao
hardcodar string pt-BR em `snippets.js`.

(6) COMMIT ATOMICO POR TASK, conventional commit com escopo `snippet-translation`
(`feat(snippet-translation): T-N <resumo>`; `docs(...)` para T-1, `test(...)` para as tasks de
teste), citando o D-XX quando aplicavel.

(7) PISO DE TESTES DERIVADO DE `main` DENTRO DO PROPRIO COMANDO, nunca cravado. Piso cravado ja
falhou neste repo: `D-2026-08-01-div-paragraph-reading-6` mostrou um piso 15 testes ABAIXO do
baseline real, que aceitava regressao silenciosa. Alem do piso, comparacao NOME A NOME (`comm -23`)
entre os testes de `main` e os verdes do HEAD.

(8) TODA task que mexe em JS entrega teste no MESMO commit, em `test/js/snippets.test.js`, rodando
com `node --test --test-reporter=tap` (reporter TAP pinado — sem isso o comando nao sai 0 no
Node 24, ver `D-2026-08-01-div-paragraph-reading-6`).
