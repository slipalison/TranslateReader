D-2026-08-16-llm-mobile-8 (2026-08-16): Nenhuma plataforma pode crashar por falta de backend ou por
falta de memoria. `TranslationUnavailableException` e a unica forma de recusar, a fronteira de
conversao para UI continua sendo o `[RelayCommand]` do PageModel, e o gate de memoria usa UM seam
injetavel no Core — sem codigo platform-specific, LOCKED.

REGRA: antes de carregar o modelo, `TranslationManager.InitializeEngineIfNeededAsync` verifica
(1) se a plataforma tem backend (`NativeBackendPlan`, D-...-3) e (2) se ha memoria suficiente. Falhou
qualquer uma -> `TranslationUnavailableException` (novo tipo em `src/TranslateReader.Core/Models/`)
com mensagem generica e acionavel. Os PageModels ja tem o `catch (Exception ex)` dentro do
`[RelayCommand]` (`ReaderPageModel.cs`, `LibraryPageModel.cs`) — a phase so precisa tratar o tipo
novo ali; nenhuma outra camada converte excecao em estado de UI (`.claude/rules/csharp.md` §1).
`OperationCanceledException` continua fluindo intocada.

SEAM DE MEMORIA: `Contracts/Utilities/IDeviceMemoryUtility` + `Utilities/DeviceMemoryUtility`, cuja
implementacao le `GC.GetGCMemoryInfo().TotalAvailableMemoryBytes`. Nada de `ActivityManager` no
Android nem `os_proc_available_memory` no iOS.
MOTIVO: a alternativa platform-specific exigiria mais um par de arquivos em `Platforms/*` por
plataforma, mais waivers de cobertura, e nao poderia ser testada aqui — trocaria precisao que nao
conseguimos verificar por complexidade que conseguimos quebrar. `TotalAvailableMemoryBytes` e o teto
do PROCESSO (que e exatamente o que causa OOM em mobile), e roda em todas as TFMs. Passa no teste da
maquina de cappuccino: "quanta memoria este processo pode usar" nao sabe nada de traducao.

LIMIAR: `ModelInfo.RequiredMemoryBytes => SizeBytes + SizeBytes / 2` (1,5x). Para o Hy-MT2 de
1_133_080_448 bytes da ~1,70 GB, coerente com o footprint estimado de 1,5-1,8 GB (modelo + KV cache)
da pesquisa. O limiar e DADO no `ModelInfo`; o Manager so compara e lanca — regra de negocio nao mora
em Manager (CLAUDE.md).

CUSTO ACEITO: (a) 1,5x e conservador — algum device de 4 GB que talvez aguentasse vai ser recusado.
Recusa clara e infinitamente melhor que OOM kill no meio de uma traducao, e o numero e um literal
unico, facil de calibrar depois com medicao real; (b) `TotalAvailableMemoryBytes` e aproximacao do
teto do processo, nao RAM livre do device — por isso e usado SO para recusar com elegancia, nunca
para prometer desempenho.

NAO REGRIDE: quem hoje consegue traduzir no Windows tem que continuar conseguindo. O gate so pode
barrar quando o valor medido for realmente menor que o exigido — nenhum caminho novo pode lancar em
maquina que hoje funciona, e isso e teste nomeado, nao inspecao.
