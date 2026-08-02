D-2026-08-01-hy-mt-translation-model-2 (2026-08-01): Validacao tecnica do card RECONFIRMADA e travada
como fonte de verdade para esta phase (supersede os numeros errados do card colado pelo usuario).
(a) `SizeBytes` correto e **1_133_080_512** (medido via `content-length` real do download URL), nao
1_213_000_000 como o card estimava a partir da UI do HF. (b) Portugues ("pt") esta entre as 36
linguas suportadas do model card real do HY-MT1.5-1.8B — nao e uma variante BR-especifica, mesma
ressalva de fidelidade ja aplicada aos 3 modelos existentes (gemma-2-2b/qwen-2.5-3b/phi-3.5), NAO
e regressao nova, nao precisa de tratamento especial. (c) A preocupacao do card sobre "chat template
muda silenciosamente" NAO SE APLICA a este codebase e fica explicitamente REJEITADA como risco:
`TranslationEngine.CreateExecutor` (`TranslationEngine.cs:99-108`) ja constroi
`new StatelessExecutor(weights, _modelParams!) { ApplyTemplate = true, SystemMessage = systemMessage }`
— `ApplyTemplate = true` faz o LLamaSharp ler o template de chat embutido nos METADADOS do proprio
arquivo GGUF no load, nao existe nenhuma string de template hardcoded pra Gemma em lugar nenhum do
codigo. Trocar o arquivo GGUF ja resolve o template de graca. Por isso esta phase NAO adiciona
nenhum campo `PromptTemplate`/const de template em `ModelInfo` — seria resolver um problema que ja
esta resolvido (YAGNI, CLAUDE.md "no design for hypothetical future requirements"). (d) Confirmado
por leitura do model card real: HY-MT1.5-1.8B nao declara `system_prompt` default, e a sampling
recomendada pelo fornecedor e `top_k=20, top_p=0.6, repetition_penalty=1.05, temperature=0.7` — ambos
os dois claims do card SAO precisos; o que fazer com eles esta decidido em D-...-5 e D-...-6.
