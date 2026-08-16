# Todos — sessao de discuss `llm-mobile` (2026-08-16)

Itens levantados na captura de decisoes e conscientemente empurrados para fora do escopo
(ver `## Out of scope` em `.jdi/phases/llm-mobile/CONTEXT.md`).

- **[PLATFORM] MacCatalyst sem backend de inferencia.** `D-2026-08-16-llm-mobile-7` registra que
  `net10.0-maccatalyst` sofre o mesmo `PlatformNotSupportedException` do iOS e NAO e corrigido aqui:
  verificar exige um Mac para compilar e outro para executar, e empilhar isso no Bloco 2 (que ja e o
  bloco com maior chance de nao fechar) troca risco por nada. Depois desta phase o caminho ja existe:
  a engine `LlamaCppTranslationEngine` e o contrato `ILlamaNativeAccess` sao TFM-agnosticos, entao a
  phase futura e uma slice adicional do XCFramework + um segundo `LlamaNativeAccess` em
  `Platforms/MacCatalyst/` + um job de CI. Enquanto isso: traducao indisponivel com mensagem tratada,
  resto do app intacto.

- **[MODELS] Runtime alternativo de inferencia como fallback documentado, nao implementado.**
  `Microsoft.ML.OnnxRuntimeGenAI` 0.15.2 tem `.aar` E `.xcframework` DENTRO do nupkg e o model
  builder suporta `HunYuan Dense V1` — ou seja, resolveria iOS e Android com UM pacote NuGet, sem
  P/Invoke a mao. Ficou fora porque trocar o runtime de inferencia reescreve `TranslationEngine`,
  invalida o cache de traducao existente e joga fora o caminho Windows/CUDA que ja funciona. Se o
  custo de manter os P/Invoke de iOS se provar alto ao longo do tempo, ESTE e o plano B a avaliar.

- **[MODELS] Alternativas ja avaliadas e descartadas — nao repetir a analise.** Bergamot/Marian (sem
  port iOS nativo; Firefox iOS roda via WASM; par en-pt so no tier "tiny"; repo de modelos arquivado
  2025-12-15); NLLB-600M e Tower-Plus-2B (CC-BY-NC, nao-comercial); MADLAD-3B (1,65 GB, qualidade por
  par inferior); OPUS-MT ONNX (GenAI nao suporta encoder-decoder; loop seq2seq teria que ser escrito a
  mao); MLC-LLM e ExecuTorch (zero binding .NET, build pesado); MediaPipe LLM (maintenance-only desde
  2026); ML Kit e Apple Translation (closed-source, violam o requisito de traducao offline propria).

- **[UI] Licenca do modelo nao aparece na tela de selecao.** `D-2026-08-16-llm-mobile-2` documenta a
  exclusao territorial do HY-MT1.5 em `docs/MODEL-LICENSES.md`, mas o `SettingsOverlay` continua
  listando o modelo sem nenhuma indicacao de licenca. Mostrar isso na UI e feature de produto (texto,
  layout, possivelmente link externo) e a phase esta explicitamente proibida de mexer em UI/UX alem da
  linha nova do Hy-MT2. Vale abrir quando o app se aproximar de publicacao em loja.

- **[PERF] Issue #1224 do LLamaSharp nao triada.** Relato de `llama-bench` a ~16 t/s contra
  `StatelessExecutor` a ~0.18 t/s num Pixel (gemma3-1b), sem resposta de maintainer
  (https://github.com/SciSharp/LLamaSharp/issues/1224). Se reproduzir, a traducao pode compilar e
  ainda assim ser inutilizavel no Android. Fora do escopo porque exige device fisico com o GGUF de
  1,06 GB baixado — verificacao humana, registrada em `## Deferred to PR review`. Se reproduzir,
  documentar com NUMERO MEDIDO, nunca afirmar que "funciona".

- **[PERF] Calibracao do limiar de memoria.** `D-2026-08-16-llm-mobile-8` fixa
  `RequiredMemoryBytes = SizeBytes * 1,5` sem medicao em device. O numero e um literal unico e
  proposital; recalibrar quando houver footprint real medido em iPhone/Android (teto observado de
  ~2,2 GB em device de 4 GB mesmo COM `com.apple.developer.kernel.increased-memory-limit`).

- **[IOS] Entitlement `com.apple.developer.kernel.increased-memory-limit`.** Recomendado para o
  footprint de ~1,5-1,8 GB, mas exige provisioning profile e conta de desenvolvedor — nada disso
  existe/e verificavel nesta phase. Avaliar junto com a preparacao de publicacao na App Store.

- **[SIZE] Medir o crescimento do binario.** O GGUF continua sendo baixado pos-install (pratica aceita
  nas duas lojas), mas o `.so` do Android e o framework estatico do iOS aumentam o pacote. So e
  mensuravel de verdade num package build por loja; registrado em `## Deferred to PR review`.
