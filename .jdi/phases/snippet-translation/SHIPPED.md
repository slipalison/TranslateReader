shipped_at: 2026-08-11T00:00:00Z
verdict: APPROVED_WITH_WARNINGS
by: Alison Amorim

## Learnings
- Toda logica duplicada entre C# e JS precisa de contract test que quebre o build no drift — `HybridWebViewContractTests` ja le os arquivos JS; usar isso desde a primeira duplicacao, nao depois (W-15 levou 5 iteracoes; W-21 segue aberta pelo regex de fronteira).
- Saida de LLM local nunca e confiavel por instrucao no prompt: validar deterministicamente (escopo, idioma, comprimento, contagem de sentencas) e so entao persistir/exibir — o modelo copiou o contexto do prompt em 5 rodadas seguidas apesar de delimitadores e instrucao explicita.
- Guarda heuristica sobre texto de LIVRO precisa ser testada contra prosa de ficcao real: frases de recusa ("I can't", "Desculpe,") sao aberturas de dialogo comuns, e purgar dado do usuario exige precisao muito maior que rejeitar uma inferencia.
- Harness de teste complacente esconde classe inteira de bug: `FakeText.splitText` sem validacao de offset mascarou um `IndexSizeError` real de WebView por 200+ testes verdes — harness deve ser spec-faithful onde o navegador lanca.
- Mockup de design nunca exercita fragmentacao multi-column: geometria ancorada em elemento fragmentado descasa anchor/origem no `_pager` — ancorar overlays em raiz estavel, nao no paragrafo.
