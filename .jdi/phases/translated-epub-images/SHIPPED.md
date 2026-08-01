shipped_at: 2026-08-01T23:26:11Z
verdict: APPROVED_WITH_WARNINGS
by: Alison Amorim

## Learnings
- HTML produzido para a UI e HTML gravado num artefato sao contratos DIFERENTES: reusar o mesmo metodo para os dois vazou a URL do virtual host (`https://epub-images/...`) para dentro do EPUB entregue ao usuario e para qualquer leitor de terceiro.
- Pos-condicao de bugfix de artefato tem que ser sobre o ARTEFATO: abrir o zip gerado e varrer TODAS as entradas (nao so as reescritas) foi o que pegou o vazamento em `.opf`; teste sobre a funcao sozinho nao pegava.
- Gate absoluto ("nenhuma ocorrencia de X") reprova codigo correto quando o insumo ja tem X nativo — a forma DIFERENCIAL (nenhuma entrada GANHA o que a mesma entrada do original nao tinha) e o que mede a propriedade.
- `Verify:` nunca deve ancorar em ref LOCAL (`main`): fica velho, some em clone fresco de CI e reprova codigo correto. Ancore em `$(git merge-base origin/main HEAD)`.
- Prova por mutacao so vale se a mutacao foi APLICADA: confirme com grep antes/depois — uma mutacao que nao aplicou deixa o teste verde e le como "gate fraco".
