D-2026-08-16-llm-mobile-5 (2026-08-16): iOS NAO usa o LLamaSharp. Recebe implementacao propria de
`ITranslationEngine` com P/Invoke contra a C API do llama.cpp; o LOOP de geracao mora no Core atras
de contrato mockavel e testado, e SO as declaracoes nativas moram em `src/TranslateReader/Platforms/iOS/`.
O Core NAO vira multi-TFM, LOCKED.

BLOQUEIO (provado por leitura do codigo-fonte do LLamaSharp em 2026-08-16 — NAO re-litigar):
1. `LLama/Native/NativeApi.Load.cs` — o static ctor de `NativeApi` chama `SetDllImportResolver()` e
   em seguida `llama_empty_call()`, forcando o carregamento no PRIMEIRO uso do tipo.
2. O early-return de plataforma existe SOMENTE para Android
   (`if (OperatingSystem.IsAndroid()) { return; }`). Em iOS o resolver E registrado — e
   `NativeLibrary.SetDllImportResolver` aceita UM registro por assembly, entao o app nao pode
   registrar o seu depois (segunda chamada lanca).
3. O resolver chama `NativeLibraryUtils.TryLoadLibrary(...)`, cuja primeira linha e `SystemInfo.Get()`.
4. `LLama/Native/Load/SystemInfo.cs:22-40` — `Get()` termina em `throw new PlatformNotSupportedException()`
   para qualquer coisa que nao seja Windows/Linux/OSX; `GetPlatformPathParts` fecha com
   `throw new RuntimeError("Your operating system is not supported...")`.
A falha acontece no STATIC CONSTRUCTOR, antes de qualquer hook de configuracao ser alcancavel.
Fornecer binario nativo — estatico OU dinamico — nao muda isso. Nao existe rota "reusar o LLamaSharp
em iOS"; qualquer plano que dependa disso esta errado. Upstream nao vai resolver: TFM iOS comentado
no proprio repo ("Temporarily Disable iOS and MacCatalyst until native lib support is added") e a
unica issue pedindo xcframework (#1181) morreu stale em 2025-07-13.

LINKAGEM LOCKED — XCFramework OFICIAL, slice extraida, `NativeReference Kind="Static"`:
`build-xcframework.sh` do llama.cpp usa `BUILD_SHARED_LIBS=OFF`: o artefato oficial e um framework
ESTATICO, com `GGML_METAL=ON` + `GGML_METAL_EMBED_LIBRARY=ON` (sem `.metallib` solto),
`UIDeviceFamily = [1,2]` (iPhone e iPad, exatamente o alvo) e um unico framework `llama` agregando
ggml/mtmd/gguf. O modulemap declara dependencia de `c++`, `Accelerate`, `Metal` e `Foundation`.
Entregar o `.xcframework` INTEIRO ao `NativeReference` e caminho conhecido-quebrado: dotnet/macios
#19883 ("XCFramework of static library can not be linked") — aberta desde 2024-01, sem resposta de
maintainer, com o par de sintomas "The framework is a framework of static libraries, and will not be
copied to the app" seguido de `ld: framework not found`. Portanto:
- extrair a slice `ios-arm64` no build e apontar `NativeReference` para o binario estatico dela,
  com `Kind="Static" ForceLoad="True" IsCxx="True" SmartLink="False"` e os frameworks do modulemap;
- P/Invoke com `[LibraryImport("__Internal")]` sob a TFM iOS (o codigo esta linkado no binario do
  app, nao numa dylib) — mesmo padrao do whisper.net, que usa a mesma stack ggml;
- `.dylib` solto esta descartado por outra razao independente: a App Store REJEITA dylib solta
  (`dotnet/macios` BundleContents), `.framework`/`.xcframework` e a forma legal.

ONDE O CODIGO MORA (esta e a parte que decide se o resultado e testavel):
- `src/TranslateReader.Core/Contracts/Access/ILlamaNativeAccess.cs` — contrato FINO e mockavel
  (no maximo 2 contratos, 3-5 operacoes cada, nomes comportamentais). PROIBIDO vazar `nint`/`IntPtr`/
  `LibraryImport`/`DllImport` em `Contracts/` — mesma regra que ja proibe SQL em `Contracts/Access/`.
- `src/TranslateReader.Core/Business/Engines/LlamaCppTranslationEngine.cs` — a implementacao de
  `ITranslationEngine` com o loop de geracao (tokenize -> decode -> sample -> detokenize, streaming,
  cancelamento, dispose). E aqui que mora a logica de verdade, compila em `net10.0` puro, e tem
  teste unitario com NSubstitute em `test/TranslateReader.Tests/LlamaCppTranslationEngineTests.cs`,
  sem device e sem GGUF.
- `src/TranslateReader/Platforms/iOS/LlamaNativeAccess.cs` — SO declaracoes `[LibraryImport("__Internal")]`,
  structs blittable e constantes. ZERO controle de fluxo (`if`/`for`/`while`/`switch`/`try`). Se esse
  arquivo precisar de um loop, a logica esta no lugar errado.
- `MauiProgram.cs` escolhe a implementacao com `#if IOS` (unico `#if` de plataforma da phase; o
  arquivo ja usa `#if` hoje). `ITranslationEngine` continua sendo o UNICO ponto de variacao: Managers,
  validacao de snippet, cache, prompts e PageModels NAO mudam. Se a implementacao exigir mudar um
  Manager, o desenho esta errado e a task volta.

POR QUE O BINDING FICA NO PROJETO DO APP E NAO NO CORE: `TranslateReader.Core.csproj` e
`<TargetFramework>net10.0</TargetFramework>` — TFM UNICO. Multi-targetar o Core para
`net10.0;net10.0-ios;net10.0-maccatalyst` colocaria TFMs sem workload no caminho de restore do
projeto de TESTE, arriscando o comando que produz o baseline de 455 testes numa maquina Windows sem
`maui-ios`. O binding e platform-compiled por natureza; o projeto do app ja e multi-TFM e ja tem
`Platforms/iOS/`. Um terceiro projeto ios-only foi descartado por ser PIOR em prestacao de contas:
seus `.cs` cairiam no `COVERAGE_SKIP reason=no-instrumented-lines` do gate, sumindo em silencio, ao
passo que um `.cs` novo sob `src/TranslateReader/` dispara `COVERAGE_GUARD` (exit 2) e so passa com
linha explicita em `.jdi/coverage-waivers.txt` citando ESTA decisao.

CUSTO ACEITO: (a) ~25-35 entrypoints P/Invoke escritos e mantidos a mao, que precisam ser revisados
a cada bump de release do llama.cpp — o preco de o LLamaSharp nao suportar iOS; (b) um arquivo do app
sem cobertura, coberto por waiver rastreavel e restrito a declaracoes; (c) `LlamaCppTranslationEngine`
tem teste unitario mas NUNCA execucao real nesta phase — nenhuma maquina daqui compila iOS. O que os
testes provam e o loop, nao a inferencia; a inferencia real e "Deferred to PR review", declarada.
