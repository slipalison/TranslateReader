shipped_at: 2026-07-30T21:08:45Z
verdict: APPROVED_WITH_WARNINGS
by: Alison Amorim

## Learnings
- Um `Verify:` que mede contagem agregada (`-eq N`, `grep -c`) quase sempre e oco: o DoD critic derrubou esta phase 2x ate os gates medirem a propriedade POR ITEM (por nome, por arquivo), nao por total.
- Todo `Verify:` novo exige matriz de mutacao nos dois sentidos — pega o mutante realista E continua exit 0 no repo limpo; sem isso o gate so prova que o comando roda.
- Mudar DoD locked e legal pelo caminho append-only (decisao nova supersedendo em DECISIONS.md, depois a linha do CONTEXT.md) — nunca reescrevendo a decisao anterior.
- Regex compile-time-known migrado para `[GeneratedRegex]` deve carregar `RegexOptions` para dentro do atributo; o option perdido nao quebra build nem teste (risco silencioso, csharp.md §2.1).
- Metodo privado sem seam pode ser caracterizado por reflection com null-guard explicito — evita expor API de producao so para teste e respeita a proibicao de I/O em teste novo (csharp.md §6).
