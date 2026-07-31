shipped_at: 2026-07-29T13:20:17Z
verdict: APPROVED_WITH_WARNINGS
by: Alison Amorim

## Learnings
- Documentacao mente do mesmo jeito que codigo quebra: por 2 rounds o doer converteu REGRAS de `.claude/rules/csharp.md` e do Semgrep em DESCRICOES do que o codigo ja faria. Ao escrever sobre seguranca/testes/CI, verificar cada frase contra o repo — e nomear a fonte quando enunciar politica.
- DoD por grep nao mede claim falso: os 10 `Verify:` deram 10/10 PASS na iteracao com os 3 blockers presentes. Gate de documentacao precisa de leitura critica, nao so de regex.
- Verificar um claim de seguranca do README expos lacuna real de produto (zip-slip sem containment) E cegueira do gate que deveria pega-la — virou a phase `epub-zip-slip` com escopo de duas entregas.
- Regra de SAST so protege o padrao que ela casa sintaticamente: `translatereader-zip-slip` exige `.FullName`, e o projeto extrai via VersOne.Epub — cobertura estruturalmente zero no unico vetor real. Ao escrever regra custom, provar com fixture que ela pega o caminho REAL do projeto, nao o exemplo canonico.
- Nao superdeclarar tem irmao: subdeclarar. Ao remover uma frase que mistura claim falso com verdadeiro, readmitir a metade verdadeira em vez de perder informacao correta.
