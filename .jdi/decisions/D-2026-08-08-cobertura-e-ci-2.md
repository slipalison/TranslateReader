D-2026-08-08-cobertura-e-ci-2 (2026-08-08): "codigo novo" para o gate = arquivos **criados OU
modificados** apos o boundary `4285f25` (D-2), nao apenas criados.

Comando canonico do escopo:
`git log --diff-filter=AM --pretty=format: --name-only 4285f25..HEAD | sort -u | grep -E '\.cs$'`

Motivo: `.claude/rules/csharp.md` §6 diz literalmente "**New/changed** code after commit `4285f25`
ships unit tests in the same PR: >=90% line coverage". O Gate 3 do reviewer hoje usa
`--diff-filter=A` (so criados) — divergencia real entre a regra escrita e a regra executada, e o
efeito e um buraco permanente: um arquivo legado editado hoje nunca mais entra em gate nenhum.

**Custo aceito conscientemente:** com `AM` o arquivo legado editado entra INTEIRO no denominador —
mexer em 2 linhas de `ParsingEngine.cs` (45 linhas descobertas hoje) puxa o arquivo todo para os
90%. Isso e mais duro que D-2, e foi a escolha explicita do usuario nesta sessao. Duas valvulas,
ambas auditaveis, nenhuma silenciosa: (i) o waiver de D-...-4, que exige path + referencia a uma
decisao; (ii) uma decisao propria da fase que precisar, superseding esta.

Exclusoes do escopo, fixas:
- `test/**` sai da lista: e o instrumento de medicao, nao o medido (e `IncludeTestAssembly` do
  coverlet ja e false por default, entao esses arquivos nunca aparecem no relatorio).
- Arquivo em escopo **ausente do relatorio Cobertura** nao e falha automatica: interface em
  `Contracts/`, record e enum nao geram linha executavel. O script os lista nominalmente na saida
  como `sem linhas instrumentadas` e os tira do denominador. **Excecao:** ausencia de arquivo sob
  `src/TranslateReader/` (app MAUI) e falha dura — e o caso cego de verdade, tratado em D-...-4.
