D-2026-08-02-pixel-perfect-6 (2026-08-02): Grid adaptativo no desktop, 3 colunas fixas no mobile, LOCKED.
O grid do mockup desktop e CSS `auto-fill minmax(~150px,1fr)` com gap 20(coluna)/24(linha) —
na janela de referencia (1291px uteis) rendeu 7 colunas de 167px; no screenshot original de
1266px, 5 colunas. Nao existe "span fixo certo". Em MAUI: `GridItemsLayout` com `Span`
recalculado no `SizeChanged` da pagina pela formula fechada
`span = Math.Max(3, (int)((larguraDisponivelDoGrid + 20) / 187))` (187 = 167 de card + 20 de
gap). Mobile: 3 colunas fixas, gap 14/18 (medido no mockup mobile). A formula e codigo de
code-behind de View (Client Layer), nao regra de negocio — nenhum Engine envolvido.
