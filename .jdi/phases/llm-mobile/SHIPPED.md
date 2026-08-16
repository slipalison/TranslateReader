shipped_at: 2026-08-16T17:25:00-03:00
verdict: APPROVED_WITH_WARNINGS
by: Alison Amorim (chain autonoma /jdi-issue)

## Escopo real entregue

Android entregue e provado. iOS NAO entregue — fundacao pronta e gap nomeado (D-12).

- **Bloco 1 (T-1..T-6)** — completo e verificado nesta maquina: backend oficial
  `LLamaSharp.Backend.Cpu.Android` 0.27.0 com `libllama.so` medido dentro do APK, modelo
  `Hy-MT2-1.8B` (Apache-2.0) como default de instalacao nova, config nativa como dado puro por
  plataforma, recusa graciosa por plataforma e por memoria, gate do Android promovido a bloqueante.
- **Bloco 2** — T-7 completo (contrato `ILlamaNativeAccess` + `LlamaCppTranslationEngine`, 15 testes),
  T-8 **parcial**: fetch pinado e verificado do XCFramework e `NativeReference` corretos, mas as
  declaracoes `tr_llama_*` nao correspondem a simbolo exportado por artefato nenhum. Falta a camada C.

## Learnings

- **Um pacote NuGet oficial nao prova suporte de plataforma.** O LLamaSharp publica backend Android
  desde 0.24.0 e nunca publicou iOS — e o motivo nao e falta de binario: `SystemInfo.Get()` lanca
  `PlatformNotSupportedException` no static ctor de `NativeApi`, antes de qualquer hook de
  configuracao, e o slot de `SetDllImportResolver` por assembly ja esta tomado. Ler o codigo do
  fornecedor custou minutos e derrubou uma rota que a pesquisa por documentacao dava como viavel.
- **Sequenciar por verificabilidade salvou a entrega.** O plano proibiu comecar pelo iOS e exigiu que
  o bloco Android fechasse sozinho. Quando o iOS caiu, o que sobrou continuou sendo entrega completa
  em vez de meia funcionalidade — o inverso teria custado a phase inteira.
- **Emenda de `Verify:` so vale re-executando o comando COMPLETO.** Emendei o DoD 8 validando apenas
  os ramos novos; tres sub-checks continuaram assumindo o mundo de quatro jobs, inclusive um `grep`
  por uma frase que o commit anterior tinha apagado. Dois commits meus se invalidaram mutuamente e o
  reviewer reprovou com razao.
- **Teste de concorrencia precisa ser provado por mutacao, e a mutacao pode derrubar o host.**
  Reverter o fix para conferir que os testes pegavam a regressao travou o processo de teste inteiro,
  porque o metodo desprotegido nao tem ponto de `await` e rodava sincrono. Limitar toda espera com
  `Task.WaitAsync` transformou um hang de CI em falha rapida.
- **Quando um `Verify:` reprova, medir antes de consertar.** Dois criterios desta phase estavam
  objetivamente errados: um comparava contra um literal ja falso na propria linha de base, outro
  casava o proprio script que exigia. Em ambos o codigo estava certo. A regra "conserte o codigo,
  nunca o `Verify:`" protege contra fraude do executor — nao cobre comando comprovadamente falso, e a
  correcao precisa vir do orquestrador com registro (D-11, D-13).
- **O executor recusou fraudar um gate e isso foi o comportamento certo.** Podia ter mexido em tres
  propriedades legadas para a heuristica de static fechar; nao mexeu, citou a fronteira de legado, e
  deixou o item reprovando com a explicacao. Um numero verde obtido danificando codigo fora de escopo
  seria pior que o vermelho.
