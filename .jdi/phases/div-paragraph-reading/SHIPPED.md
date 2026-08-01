shipped_at: 2026-08-01T21:57:23Z
verdict: APPROVED_WITH_WARNINGS
by: Alison Amorim

## Learnings
- Gate textual (grep) sobre codigo nao prova comportamento: um mutante que so muda `src/` passou nos 7 `Verify:` e nos 73 testes enquanto colapsava o capitulo — quem fecha e teste sobre a fixture que consegue DESSINCRONIZAR, nao a que qualquer seletor acerta.
- Teste de round-trip so discrimina se a fixture for capaz de expor o defeito: o teste de apply antigo usava um corpo onde todo elemento era paragrafo, entao nao distinguia seletor ingenuo de seletor correto.
- `Verify:` que roda runner de teste precisa PINAR o reporter (`--test-reporter=tap`): o Node 24 usa `spec` por padrao e o grep de TAP nunca casa — gate que reprova codigo correto tem o mesmo efeito pratico do gate oco.
- Piso de nao-regressao por CONTAGEM aceita stub sem assert e delecao compensada; comparar NOME A NOME contra `main` e o que prova.
- Harness de teste que falha ABERTO (seletor impossivel casando o documento inteiro) aprova codigo que o runtime real rejeita — em input nao confiavel isso e furo de seguranca, nao teto de fidelidade.
