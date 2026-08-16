D-2026-08-16-llm-mobile-3 (2026-08-16): A configuracao de backend nativo vira DADO PURO calculado por
plataforma (`NativeBackendPlan.For(platform)`), e nao ramificacao espalhada; `TranslationEngine.cs`
perde todo literal Windows, LOCKED.

PROBLEMA MEDIDO: `Business/Engines/TranslationEngine.cs:34-52` roda `ConfigureNativeLibrary()`
INCONDICIONALMENTE em toda plataforma, com `.WithCuda(true)`, `.WithAutoFallback(false)` e
`WithSearchDirectory(<base>/runtimes/win-x64/native/cuda12)`. Em Android isso e ignorado (o
LLamaSharp faz early-return de plataforma no Android e resolve o `.so` pelo search path do APK), mas
em qualquer outra plataforma e configuracao errada executada as cegas. E o ponto mais provavel de
quebra em mobile e o primeiro a corrigir.

DESENHO LOCKED:
- `src/TranslateReader.Core/Models/NativeBackendPlan.cs` — record + enum `TranslationPlatform`
  (`Windows`, `Android`, `IOS`, `MacCatalyst`, `Other`) + factory estatica PURA
  `NativeBackendPlan.For(TranslationPlatform)`. Todo literal (`runtimes`, `win-x64`, `cuda12`,
  `UseCuda`) vive AQUI e em nenhum outro lugar.
- `TranslationEngine.ConfigureNativeLibrary()` apenas APLICA o plano
  (`.WithCuda(plan.UseCuda)`, search directory so quando o plano declara um). Zero ocorrencia de
  `win-x64`, `cuda12` ou `WithCuda(true)` no arquivo da engine.
- Sem `#if` dentro do Core. O Core e `net10.0` unico; a plataforma chega como VALOR
  (`OperatingSystem.Is*()` mapeado uma unica vez), o que e o que torna o comportamento testavel.

MOTIVO DA FORMA: um `if (OperatingSystem.IsWindows())` dentro do metodo que chama o LLamaSharp e
inverificavel numa suite que roda so em Windows — o caminho Android/iOS nunca executaria em teste.
Uma funcao pura parametrizada pela plataforma torna os QUATRO caminhos testaveis na mesma maquina,
que e a unica forma honesta de provar AC6 sem device.

GUARDA DE NAO-REGRESSAO: o plano de Windows tem que reproduzir EXATAMENTE o comportamento de hoje
(cuda ligado, vulkan desligado, autofallback desligado, search dir `runtimes/win-x64/native/cuda12`).
Isso e teste nomeado, nao inspecao.

CUSTO ACEITO: um record + um enum novos no Core para uma decisao que "cabia num if". Trocamos duas
declaracoes de codigo por cobertura real de quatro plataformas; sem isso, AC6 so poderia ser provado
por leitura humana — exatamente o hollow PASS que esta phase existe para evitar. O guard estatico
existente `_nativeLibraryConfigured` (unico static mutavel do repo, ja WARN-baseline do reviewer
5.12) permanece como esta; a phase NAO pode introduzir um segundo static mutavel.
