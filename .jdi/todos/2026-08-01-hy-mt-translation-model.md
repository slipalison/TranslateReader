## De `hy-mt-translation-model` (2026-08-01)

- **[BUG, confirmado, adiado]** Botoes "Qwen 3B" e "Phi 3.5" em `SettingsOverlay.xaml` continuam
  mortos apos esta phase — `ResolveModel` cai no fallback gemma-2-2b pra qualquer nome fora do
  registry (D-2026-08-01-hy-mt-translation-model-4), entao clicar neles grava
  `TranslationModelName` mas o download/inicializacao usa gemma do mesmo jeito. O card desta phase
  pediu especificamente adicionar hy-mt como modelo — nao pediu URLs reais pra Qwen/Phi, inventa-las
  seria scope creep. Fase futura decide: (a) achar/validar URLs GGUF reais pra Qwen 2.5 3B e Phi 3.5
  e registra-los como `ModelInfo`, ou (b) remover os 2 botoes da UI ate terem suporte real.

- **[MELHORIA, fora de escopo]** Sampling por modelo (`SamplingProfile`/campo `Sampling` em
  `ModelInfo`, `TopP`/`TopK`/`RepeatPenalty` wireados em `TranslationEngine.CreateInferenceParams`).
  O fornecedor do HY-MT1.5-1.8B recomenda `top_k=20, top_p=0.6, repetition_penalty=1.05,
  temperature=0.7`; esta phase mantem `TranslationTemperature=0.1f` uniforme pra todos os modelos
  (decisao explicita, ver D-2026-08-01-hy-mt-translation-model-5) — risco de qualidade de traducao
  degradada pro hy-mt rodando fora da recomendacao do fornecedor, nao medido.

- **[MELHORIA, fora de escopo]** Forma do prompt (`PromptUtility.BuildTranslationMessages`) sempre
  manda a instrucao de traducao no turno de SISTEMA, pra todos os modelos — o HY-MT nao documenta
  system_prompt default, entao pode responder melhor com a instrucao no turno de USUARIO. Preocupacao
  MODEL-AGNOSTIC (afetaria qualquer modelo no mesmo caso), nao estruturalmente quebrada hoje (o app
  nunca depende do system prompt default do GGUF, sempre constroi o proprio). Ver
  D-2026-08-01-hy-mt-translation-model-6.

- **[FEATURE, especulativo, nao construir sem caso reproduzivel]** O model card do HY-MT1.5-1.8B
  documenta templates de prompt pra intervencao terminologica (glossario) e traducao contextual
  (paragrafo anterior) — SO na versao em chines do card, nao testado pro par EN->PT. O proprio card
  colado pelo usuario ja marcou isso como "untested, dont build on it blindly".

- **[FEATURE, especulativo]** Variantes quantizadas menores (2-bit / 1.25-bit) do HY-MT existem com
  APK demo, se 1,1GB ainda for pesado demais pro bundle do app — "vale medir, nao fazer agora" (nota
  do proprio card colado).

- **[PRODUTO/UX, decisao humana]** Se o usuario baixar mais de um modelo (ex.: gemma e depois
  hy-mt) sem apagar entre trocas, os 2 arquivos GGUF coexistem em disco (~1,1GB + ~1,6GB) — nao ha
  limpeza automatica do modelo anterior nem selecao de QUAL modelo apagar (o unico botao "Apagar
  modelo" existente sempre limpa TUDO no diretorio de modelos). Ver
  D-2026-08-01-hy-mt-translation-model-4.

- **[LEGAL/PRODUTO, decisao humana]** Risco legal residual para usuarios em EU/Reino Unido/Coreia do
  Sul: a Tencent HY Community License Agreement exclui esses 3 territorios do grant de licenca, e o
  app nao tem (nem esta phase constroi) nenhuma infraestrutura de geolocalizacao/deteccao de
  territorio pra bloquear o download nesses casos. Ver
  D-2026-08-01-hy-mt-translation-model-3 e `## Deferred to PR review` de
  `.jdi/phases/hy-mt-translation-model/CONTEXT.md`.
