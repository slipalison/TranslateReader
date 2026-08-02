D-2026-08-01-hy-mt-translation-model-4 (2026-08-01): Desenho LOCKED da correcao do registry quebrado
(achado critico fora do card: `SettingsOverlay.xaml.cs` ja grava `TranslationModelName` para
gemma-2-2b/qwen-2.5-3b/phi-3.5, mas `TranslationManager.DownloadModelIfNeededAsync`/
`InitializeEngineIfNeededAsync` sempre usavam o `DefaultModel` hardcoded, entao clicar em
Qwen/Phi ja era um botao morto). Escopo confirmado = opcao (b): consertar o MECANISMO do registry
para os modelos que TEM `ModelInfo` real (gemma-2-2b + hy-mt1.5-1.8b); Qwen/Phi NAO ganham URL real
nesta phase (o card nao pediu, inventar URL seria scope creep) — continuam resolvendo para o
default via fallback (ver abaixo), registrado como todo para phase futura.
Desenho: (1) `TranslationManager` ganha um `ModelRegistry` estatico
(`IReadOnlyDictionary<string, ModelInfo>`, chave = `ModelInfo.Name`, `StringComparer.Ordinal` —
`.claude/rules/csharp.md` §2.1, Ordinal pra chave/nao culture-aware) com as DUAS entradas reais:
gemma-2-2b (constante ja existente, so renomeada de `DefaultModel` pra `GemmaModel`) e hy-mt1.5-1.8b
(`FileName: "HY-MT1.5-1.8B-Q4_K_M.gguf"`, `DownloadUrl:
"https://huggingface.co/tencent/HY-MT1.5-1.8B-GGUF/resolve/main/HY-MT1.5-1.8B-Q4_K_M.gguf"`,
`SizeBytes: 1_133_080_512` — D-...-2). (2) Metodo privado `ResolveModel(string modelName)`: se o
nome esta no registry devolve o `ModelInfo`; senao devolve `GemmaModel` (fallback, cobre
qwen-2.5-3b/phi-3.5/qualquer nome legado gravado antes desta phase — mesmo comportamento OBSERVAVEL
de hoje pra esses 2, so que agora por decisao explicita em vez de acidente de wiring). (3)
`TranslationManager` ganha `ISettingsAccess settingsAccess` no construtor (Manager -> ResourceAccess
e permitido por The Method; Manager -> Manager sincrono e PROIBIDO, entao NAO pode depender de
`ISettingsManager` — `SettingsManager` ja usa exatamente esse padrao, injetar `ISettingsAccess`
direto). `DownloadModelIfNeededAsync`/`InitializeEngineIfNeededAsync` chamam
`await settingsAccess.FetchSettingsAsync()` e resolvem o modelo antes de decidir baixar/inicializar.
Nenhuma mudanca de assinatura em `ITranslationManager` (os 2 metodos continuam sem parametro de
modelo — a fonte da verdade e a settings persistida, igual ao app ja fazia pros 3 botoes).
(4) ACHADO ADICIONAL nesta sessao, necessario pro registry ser CORRETO e nao so COMPILAR:
`IModelAccess.IsModelAvailable()`/`GetModelPath()` (sem parametro) checavam "qualquer *.gguf no
diretorio" — com 2 modelos reais agora, se o usuario baixar gemma e depois trocar pra hy-mt,
`IsModelAvailable()` continuaria `true` (achando o gguf ERRADO) e `GetModelPath()` devolveria
qualquer um dos dois arquivos, dependendo da ordem do `Directory.EnumerateFiles` — o troca de modelo
ficaria silenciosamente quebrada de novo, so que de um jeito novo. Correcao: as 2 assinaturas
passam a exigir `string fileName` (`bool IsModelAvailable(string fileName)`,
`string GetModelPath(string fileName)`), checando o arquivo EXATO
(`File.Exists(Path.Combine(dir, fileName))`) em vez do glob `*.gguf`. `DownloadModelAsync`/
`DeleteModelAsync` ficam com a MESMA assinatura de hoje (`DownloadModelAsync` ja deriva o nome do
arquivo da propria URL; `DeleteModelAsync` continua apagando TUDO no diretorio — unico botao de
delete na UI, sem selecao por modelo, comportamento de "reset" preservado, fora de escopo mudar
aqui). Efeito colateral aceito e registrado (nao e bug, e decisao): se o usuario baixar os 2
modelos sem apagar entre trocas, os 2 arquivos GGUF coexistem em disco (~1,1GB + ~1,6GB) — nao ha
limpeza automatica do modelo anterior nesta phase; vira todo de produto/UX.
