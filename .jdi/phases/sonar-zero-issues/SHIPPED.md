shipped_at: 2026-07-31T02:02:56Z
verdict: APPROVED_WITH_WARNINGS
by: Alison Amorim

## Learnings
- Gate que ancora em "primeira ocorrencia do nome" mede o call site, nao a declaracao — ancore no padrao da DECLARACAO e conte a propriedade (parametros), nunca virgulas de uma janela textual.
- Antes de contar qualquer coisa em codigo com grep/awk, descarte comentario de linha E de bloco; e a guarda de paridade de aspas nao e opcional (um default `"https://x"` vira falso comentario).
- Mecanismo de CI condicionado a secret (`if: env.X != ''`) vira no-op SILENCIOSO por tras de check obrigatorio — sempre pareie com um step que FALHA onde o secret deveria existir.
- Issue de ferramenta em arquivo vendored de terceiro se resolve removendo o arquivo ou excluindo da analise com justificativa; corrigir codigo de terceiro e trabalho perdido (41 das 113 issues eram um instalador da Microsoft).
- Quality Gate "Sonar way" so mede New Code: nao pega issue nova em linha legada intocada, nem smell abaixo do debt ratio, e PR de fork passa verde sem escanear.
