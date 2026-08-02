D-2026-08-01-hy-mt-translation-model-5 (2026-08-01): Lacuna de sampling (achado fora do card) fica
DELIBERADAMENTE fora de escopo nesta phase — **opcao (b) escolhida** entre as 2 do brief. Hoje
`TranslationEngine.CreateInferenceParams` (`TranslationEngine.cs:122-133`) so seta `Temperature` no
`DefaultSamplingPipeline`; `TopP`/`TopK`/`RepeatPenalty` nunca sao setados, pra NENHUM dos modelos
existentes. O valor `TranslationTemperature = 0.1f` (`TranslationManager.cs:28`) e uma escolha
deliberada de determinismo/traducao literal, documentada como tal — conflita com a recomendacao
geral do fornecedor do HY-MT (`temperature=0.7`, mais criativo/geral-purpose). Esta phase mantem
`TranslationTemperature = 0.1f` UNIFORME pra TODOS os modelos, incluindo hy-mt1.5-1.8b — nao
introduz `SamplingProfile`/campo `Sampling` em `ModelInfo`, nao muda `ITranslationEngine` nem
`TranslationEngine.CreateInferenceParams`. Racional: extender o contrato de sampling e mudanca real
de superficie (`ITranslationEngine.GenerateAsync`/`GenerateStreamingAsync` ganhariam parametro novo,
ou os 3 call sites em `TranslationManager` passariam a montar um objeto de sampling por modelo) —
maior que "adicionar 1 modelo pro download + consertar o registry quebrado", e todos os valores
recomendados pelo fornecedor (`top_k=20, top_p=0.6, repetition_penalty=1.05`) ficariam sem uso real
enquanto so gemma/qwen/phi existirem no registry (D-...-4 escopo (b), so gemma+hy-mt tem
`ModelInfo` real). RISCO CONHECIDO, nao descartado em silencio: a qualidade de traducao do hy-mt
pode degradar rodando so com `Temperature=0.1` fora da recomendacao do fornecedor — registrado em
`## Deferred to PR review` do CONTEXT.md e como todo para phase futura ("sampling profile por
modelo").
