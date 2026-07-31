shipped_at: 2026-07-31T12:14:01Z
verdict: APPROVED_WITH_WARNINGS
by: Alison Amorim

## Learnings
- A metrica `coverage` do SonarQube conta LINHAS + CONDICOES: planejar meta de cobertura so com linhas subdimensiona o esforco (aqui, 187 previstas contra 249 reais).
- Gate que roda o medidor com `;` em vez de `&&` le o artefato da execucao ANTERIOR quando a atual falha — encadeie com `&&` e escreva num diretorio limpo por rodada.
- `find ... | sort | tail -1` sobre diretorios GUID do VSTest seleciona por ordem lexicografica, nao por tempo: escolhe relatorio arbitrario.
- Gate que compara duas strings de config entre si sem ancorar o literal esperado passa quando as duas derivam juntas — pine o valor.
- Ao extrair um `Verify:` de documentacao para ataque adversarial, remova o sufixo `|| echo`: senao o shell sempre retorna 0 e a prova nao vale nada.
