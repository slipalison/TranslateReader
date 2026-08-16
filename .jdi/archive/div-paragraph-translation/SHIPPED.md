shipped_at: 2026-08-01T04:13:40Z
verdict: APPROVED_WITH_WARNINGS
by: Alison Amorim

## Learnings
- Gate que so descreve a FORMA do arquivo (nome de teste, contagem de regex, escopo de diff) aprova codigo que nao compila — pelo menos um item do DoD tem que EXECUTAR a suite.
- `dotnet test` com `--filter` que casa ZERO teste sai com exit 0 e sem linha de sumario: o `grep -q "Passed!"` e load-bearing, exit code sozinho nao basta.
- Corrigir a extracao sem corrigir a substituicao entrega traducao cacheada que nunca chega ao arquivo — quem seleciona para ler e quem seleciona para escrever tem que ser a MESMA regra.
- Unir duas fontes de blocos de texto sem garantir disjuncao traduz o mesmo trecho 2x e desalinha o `index++` da substituicao; resolver por alternacao unica torna a disjuncao invariante estrutural.
- EPUB de calibre nao usa `<p>`: qualquer heuristica de extracao ancorada so em `p|h|li` cobre uma fracao do texto e falha em silencio.
