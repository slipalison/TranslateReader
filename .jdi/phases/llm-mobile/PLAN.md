# Phase 6: LLM em Android/iOS — Plan  (slug: llm-mobile)

## Goal

Traducao offline por LLM local passa a funcionar em Android e iOS, com modelo de licenca permissiva,
**sem nenhuma regressao no Windows e sem quebrar nada que ja funciona**.

## Locked decisions (from CONTEXT.md)

- **D-1** dois blocos sequenciais; "nao quebrar nada" medido contra baselines; entrega parcial
  verdadeira > entrega total nao verificada.
- **D-2** `Hy-MT2-1.8B` (Apache-2.0) entra no registry e vira default de instalacao nova; fallback de
  `ResolveModel` NAO muda. **D-3** config de backend vira dado puro `NativeBackendPlan.For(platform)`.
- **D-4** Android: `LLamaSharp.Backend.Cpu.Android` 0.27.0, minSdk 21->23, `.so` provado por script.
- **D-5** iOS nao usa LLamaSharp (falha no static ctor): engine propria, loop no Core atras de
  `ILlamaNativeAccess`, so declaracoes em `Platforms/iOS/`. **D-6** iOS 15.0 -> 16.4.
- **D-7** MacCatalyst sem backend, limitacao registrada. **D-8** `TranslationUnavailableException` e a
  unica forma de recusar; seam `IDeviceMemoryUtility`; limiar `RequiredMemoryBytes = SizeBytes * 1,5`.
- **D-9** XCFramework nunca no git, pin por tag + SHA-256 fail-closed com `--verify-only`.
- **D-10** Android vira alvo de primeira classe nos gates; job de CI iOS so entra se verde.

## Restricoes de execucao (nao negociaveis)

- **Bloco 1 = T-1..T-6 (waves 1-3)**, 100% verificavel nesta maquina. **Bloco 2 = T-7..T-8 (waves 4-5)**.
  Bloco 1 inteiro antes do Bloco 2; se o Bloco 2 travar, Bloco 1 continua entrega completa.
- Build local SEMPRE com csproj explicito + `-f` (`dotnet build src/TranslateReader/TranslateReader.csproj
  -c Release -f net10.0-android`); sem csproj da NETSDK1005 nos projetos `net10.0`-only.
- `ITranslationEngine` e o UNICO ponto de variacao por plataforma. Contrato nao alarga. Managers,
  PageModels, cache, prompts e validacao de snippet nao mudam de comportamento.
- Nenhum `using LLama` fora de `Business/Engines/`. O literal `NativeBackendPlan` NAO pode aparecer em
  `Business/Managers/` (grep do DoD 9) — o Manager recebe um `bool` calculado no `MauiProgram`.
- Zero static mutavel novo (baseline do gate 5.12 = exatamente 1: `_nativeLibraryConfigured`).
- Toda task fecha com `dotnet test` >= 455 passed / <= 2 skipped / 0 failed e ZERO nome de teste
  perdido (`comm -23` do DoD 1). Renomear/remover teste existente = falha, mesmo com contagem maior.
- Cobertura >= 90% em `.cs` novo do Core na MESMA task que o cria (`bash scripts/coverage-gate.sh`).

## Tasks

### Wave 1

#### T-1: gravar BASELINE e abrir a matriz de plataformas
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `.jdi/phases/llm-mobile/BASELINE`, `docs/NATIVE-BACKENDS.md`
- **Acceptance:**
  - `B=$(cat .jdi/phases/llm-mobile/BASELINE) && git cat-file -e "$B^{commit}" && git merge-base --is-ancestor "$B" HEAD` — valor = `git rev-parse HEAD` ANTES do commit desta task.
  - `for p in windows android ios maccatalyst; do grep -qE "^PLATFORM $p STATUS (SUPPORTED|UNVERIFIED|UNSUPPORTED) " docs/NATIVE-BACKENDS.md || exit 1; done && grep -qF 'D-2026-08-16-llm-mobile-7' docs/NATIVE-BACKENDS.md`
  - Status HONESTOS agora: `windows SUPPORTED`; `android`/`ios`/`maccatalyst` **UNSUPPORTED**. Android so vira SUPPORTED em T-6 (depois do `.so` medido) e iOS so vira UNVERIFIED em T-8. Escrever SUPPORTED antes da prova e hollow PASS.
- **Dependencies:** none
- **Test:** nenhum codigo novo; suite permanece 455/2/0.
- **Status:** completed

### Wave 2 (parallel-eligible)

#### T-2: `NativeBackendPlan` como dado puro + `TranslationEngine` sem literal de Windows
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Models/NativeBackendPlan.cs`, `src/TranslateReader.Core/Business/Engines/TranslationEngine.cs`, `test/TranslateReader.Tests/NativeBackendPlanTests.cs`
- **Acceptance:**
  - `E=src/TranslateReader.Core/Business/Engines/TranslationEngine.cs; M=src/TranslateReader.Core/Models/NativeBackendPlan.cs; test "$(grep -cE 'win-x64|cuda12|WithCuda\(true\)' $E)" -eq 0 && grep -qF 'NativeBackendPlan.For(' $E && grep -qF 'WithCuda(plan.UseCuda)' $E && grep -qF 'win-x64' $M && grep -qF 'cuda12' $M` — a forma `ForCurrentPlatform()` NAO satisfaz o grep `NativeBackendPlan.For(`; a plataforma e detectada uma unica vez e passada por VALOR.
  - `DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~NativeBackendPlan"` >= 4 passed / 0 failed, com os 4 nomes prescritos do DoD 2.
  - Guarda anti-regressao Windows: plano windows reproduz cuda ON, vulkan OFF, autofallback OFF, search dir `runtimes/win-x64/native/cuda12` — teste nomeado, nao inspecao. Nenhum static mutavel novo.
- **Dependencies:** none
- **Test:** `NativeBackendPlanTests.cs` (4 nomes prescritos), cobertura >= 90% no arquivo novo.
- **Status:** completed

#### T-3: Hy-MT2 no registry, default de instalacao nova e licencas documentadas
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Business/Managers/TranslationManager.cs`, `src/TranslateReader.Core/Models/ReadingSettings.cs`, `src/TranslateReader.Core/Access/SettingsAccess.cs`, `src/TranslateReader/Pages/Controls/SettingsOverlay.xaml`, `src/TranslateReader/Pages/Controls/SettingsOverlay.xaml.cs`, `docs/MODEL-LICENSES.md`, `test/TranslateReader.Tests/TranslationManagerTests.cs`, `test/TranslateReader.Tests/PixelSpecTests.cs`, `test/TranslateReader.Tests/SettingsAccessTests.cs`
- **Acceptance:**
  - **DoD 3 (CONTEXT.md) exita 0** — inclui o `curl` de `content-length` == `1133080448` (exige rede).
  - Wiring end-to-end, nao so `x:Name` (learning de `hy-mt-translation-model`: `SettingsOverlay` ja tinha 2 botoes mortos): o handler novo grava `"hy-mt2-1.8b"`, `UpdateModelButtonBorders` trata o nome, e `ResolveModel` acha no registry — provado por `DownloadModelIfNeededAsync_WhenSettingsAreDefault_DownloadsHyMt2`, nao por grep.
  - `grep -qF ': GemmaModel;' src/TranslateReader.Core/Business/Managers/TranslationManager.cs` (fallback INTACTO) e `grep -c 'Orientation="Horizontal"' src/TranslateReader/Pages/Controls/SettingsOverlay.xaml` == 0. `SettingsAccessTests` atualizado SEM renomear nem remover teste.
- **Dependencies:** none
- **Test:** `TranslationManagerTests.cs` (+ os 4 nomes do DoD 3), `PixelSpecTests.ModelRowNames` com `HyMt2ModelButton`, `SettingsAccessTests` com o default novo.
- **Status:** completed

#### T-4: backend Android oficial + minSdk alinhado ao binario
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader/TranslateReader.csproj`
- **Acceptance:**
  - **DoD 4 (CONTEXT.md) exita 0** — `LLamaSharp.Backend.Cpu.Android` 0.27.0 sob `ItemGroup Condition ... == 'android'`, Cuda12/Cpu continuam sob `'windows'`, `== 'android'">23.0<` presente e `21.0` ausente.
  - `DOTNET_CLI_UI_LANGUAGE=en dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-android` com **0 Error(s) E 0 Warning(s)** (baseline atual e 0W/0E — warning novo do pacote e regressao, nao ruido).
  - Windows Release intacto: `... -f net10.0-windows10.0.19041.0` com 0 Error(s).
- **Dependencies:** none
- **Test:** build Android/Windows (gate de build); suite inalterada.
- **Deferred:** inferencia real em Android e numeros de tokens/s -> `## Deferred to PR review`.
- **Status:** completed

### Wave 3 (parallel-eligible)

#### T-5: recusa graciosa por plataforma e por memoria
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Models/TranslationUnavailableException.cs`, `src/TranslateReader.Core/Models/ModelInfo.cs`, `src/TranslateReader.Core/Contracts/Utilities/IDeviceMemoryUtility.cs`, `src/TranslateReader.Core/Utilities/DeviceMemoryUtility.cs`, `src/TranslateReader.Core/Business/Engines/UnavailableTranslationEngine.cs`, `src/TranslateReader.Core/Business/Managers/TranslationManager.cs`, `src/TranslateReader/MauiProgram.cs`, `src/TranslateReader/PageModels/ReaderPageModel.cs`, `src/TranslateReader/PageModels/LibraryPageModel.cs`, `test/TranslateReader.Tests/TranslationEngineAvailabilityTests.cs`, `test/TranslateReader.Tests/TranslationManagerTests.cs`, `test/TranslateReader.Tests/SnippetTranslationManagerTests.cs`
- **Acceptance:**
  - **DoD 6 (CONTEXT.md) exita 0** e **DoD 2 fecha COMPLETO aqui** (`#if IOS` + >= 2 linhas `ITranslationEngine` no `MauiProgram`).
  - `test -z "$(grep -rlE 'LLama|LibraryImport|DllImport|NativeBackendPlan' src/TranslateReader.Core/Business/Managers/ --include=*.cs)"` — o Manager NAO pode nomear `NativeBackendPlan`: recebe um `bool` calculado no `MauiProgram` a partir de `NativeBackendPlan.For(...)`, compara e lanca (regra de negocio mora no dado, nao no Manager).
  - iOS/MacCatalyst registram `UnavailableTranslationEngine` (null object que lanca `TranslationUnavailableException`), entao o Bloco 1 SOZINHO ja remove o crash de static ctor do LLamaSharp nessas TFMs. `OperationCanceledException` continua fluindo intocada.
  - Nao regride Windows: `InitializeEngineIfNeededAsync_WhenDeviceMemoryIsSufficient_InitializesTheEngine` passa nesta maquina.
  - Nota aceita: o ctor de `TranslationManager` vai de 9 -> 11 params (o seam de memoria + o bool). Tensao com `.claude/rules/csharp.md` §7 / S107 e PRE-EXISTENTE (ja eram 9); NAO refatorar os 9 antigos — churn fora do escopo da phase.
- **Dependencies:** T-2, T-3
- **Test:** `TranslationEngineAvailabilityTests.cs` (3 nomes prescritos) + cobertura >= 90% nos 4 arquivos novos do Core.
- **Status:** completed

#### T-6: provar o `.so` no APK e promover Android a gate bloqueante
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `scripts/check-android-so.sh`, `docs/NATIVE-BACKENDS.md`, `.jdi/agents/jdi-reviewer-translatereader.md`
- **Acceptance:**
  - **DoD 5 (CONTEXT.md) exita 0** — tokens `SO_FOUND` / `SO_ALIGN <path> align=<n>` / `SO_COUNT` e modo `--check-doc`; extracao com fallback `unzip` -> PowerShell/.NET `ZipFile`; align lido do maior LOAD do ELF sem depender de `readelf`/NDK.
  - Falha fechado provado, nao afirmado: rodar o script contra um diretorio sem APK **exita != 0**; `SO_COUNT 0` nunca e sucesso; `--check-doc` com uma linha `SO_ALIGN` adulterada **exita != 0**.
  - `PLATFORM android STATUS SUPPORTED` entra SO AQUI (depois da medicao). Se algum `align` < 16384, o doc ganha linha `MITIGATION:` nomeando o `.so` e o caminho de correcao — limitacao registrada, nunca gate verde mentindo.
  - Metade Bloco 1 do DoD 8: `R=.jdi/agents/jdi-reviewer-translatereader.md; grep -qF 'dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-android' $R && test "$(grep -cE '^dotnet build -f net10\.0-android$' $R)" -eq 0 && test "$(grep -c 'a missing workload is reported as WARN, never as BLOCK' $R)" -eq 0 && grep -qF 'Android build failure = BLOCK' $R && grep -qF 'iOS build is CI-only' $R`
- **Dependencies:** T-4
- **Test:** o proprio script (caminho feliz + 2 caminhos de falha executados); suite inalterada.
- **Deferred:** veredito de 16 KB da ferramenta do Google Play e crescimento do pacote -> `## Deferred to PR review`.
- **Status:** completed

### Wave 4

#### T-7: loop de geracao iOS no Core, atras de contrato mockavel
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Contracts/Access/ILlamaNativeAccess.cs`, `src/TranslateReader.Core/Business/Engines/LlamaCppTranslationEngine.cs`, `test/TranslateReader.Tests/LlamaCppTranslationEngineTests.cs`
- **Acceptance:**
  - `test -z "$(grep -rlE '\bnint\b|IntPtr|LibraryImport|DllImport' src/TranslateReader.Core/Contracts/ --include=*.cs)"` e `test -z "$(grep -rlE 'LibraryImport|DllImport' src/TranslateReader.Core/ --include=*.cs)"`.
  - **Contrato pass-through obrigatorio:** cada operacao mapeia 1:1 num extern, porque a implementacao iOS de T-8 tem ZERO `if/for/while/switch/try` (DoD 7). Se o contrato exigir loop dentro do Access, o contrato esta errado e a task volta. Handles nativos ficam como estado da implementacao, nunca na assinatura. Maximo 2 contratos, 3-5 operacoes, nomes comportamentais.
  - `DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~LlamaCppTranslationEngine"` 0 failed, cobrindo sucesso, streaming, cancelamento (`OperationCanceledException` flui) e `Dispose`; `bash scripts/coverage-gate.sh` verde (>= 90% no arquivo novo).
- **Dependencies:** T-5
- **Test:** `LlamaCppTranslationEngineTests.cs` com NSubstitute sobre `ILlamaNativeAccess` — sem device, sem GGUF.
- **Deferred:** o teste prova o LOOP, nunca a inferencia; execucao real -> `## Deferred to PR review`.
- **Status:** completed

#### T-8: linkagem nativa do iOS, cadeia de suprimento fail-closed e job de CI macOS
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `scripts/fetch-llama-xcframework.sh`, `src/TranslateReader/TranslateReader.csproj`, `.gitignore`, `src/TranslateReader/Platforms/iOS/LlamaNativeAccess.cs`, `src/TranslateReader/MauiProgram.cs`, `.jdi/coverage-waivers.txt`, `.github/workflows/ci.yml`, `docs/NATIVE-BACKENDS.md`
- **Acceptance:**
  - **DoD 7 (CONTEXT.md) exita 0** — inclui a prova EXECUTADA do checksum (`--verify-only` com hash certo -> exit 0, com hash errado -> exit != 0), `NativeReference` com caminho LITERAL (zero `*`), `Kind="Static" ForceLoad="True" IsCxx="True"`, `<Error Condition="!Exists(...)">`, pins literais `LlamaCppRelease`/`LlamaCppXcframeworkSha256`, zero `latest|/master|/main/`, `.gitignore` e zero arquivo `xcframework` rastreado.
  - **DoD 8 (parte CI) e DoD 10 exitam 0** — job `build-ios` em runner macOS com as MESMAS actions pinadas por SHA dos 3 jobs existentes (que ficam intactos), sem tab no YAML; waiver de `src/TranslateReader/Platforms/iOS/LlamaNativeAccess.cs` citando `D-2026-08-16-llm-mobile-5`; `COVERAGE_JS ... files=5` inalterado.
  - `SupportedOSPlatformVersion` de `ios` 15.0 -> **16.4**; `maccatalyst` continua 15.0 e continua no `UnavailableTranslationEngine`. `docs/NATIVE-BACKENDS.md` recebe `PLATFORM ios STATUS UNVERIFIED` — **nunca SUPPORTED**: nada foi compilado nem executado aqui.
  - **ACEITACAO ESTRUTURAL APENAS.** Build iOS verde, inferencia real em iPhone/iPad, tokens/s e aceitacao de loja estao em `## Deferred to PR review`. Se o job nao puder ser dado como verde, D-10 manda NAO commitar o job: T-8 reporta entrega parcial e o Bloco 1 permanece completo e coerente sozinho.
- **Dependencies:** T-6, T-7
- **Test:** `--verify-only` (2 execucoes: aceita e rejeita) + suite inalterada; nenhum teste novo de unidade (arquivo de declaracoes).
- **Status:** partial — rebaixado na iteracao 2 do `/jdi-issue` (`.jdi/phases/llm-mobile/REVIEW.md` B-1, `.jdi/decisions/D-2026-08-16-llm-mobile-12.md`)

**Resultado real (correcao de B-1, iteracao 2):** a propria clausula desta acceptance ja previa esta
saida — "se o job nao puder ser dado como verde, D-10 manda NAO commitar o job" — e foi exercida.
`.jdi/phases/llm-mobile/REVIEW.md` mediu, no artefato REAL baixado por esta task
(`.cache/llama-xcframework/b10453/llama.framework`), que os 10 entry points
`[LibraryImport("__Internal", EntryPoint = "tr_llama_*")]` declarados em
`Platforms/iOS/LlamaNativeAccess.cs` nao correspondem a nenhum simbolo exportado: `tr_llama` tem ZERO
ocorrencias em headers e binario; `llama.h` real expoe 245 declaracoes `LLAMA_API`, todas `llama_*`; e
nenhum shim C que traduza `tr_llama_*` para `llama_*` existe em lugar nenhum do repo (nem `.c`/`.m`,
nem passo de build, nem segunda `NativeReference`) — necessario por design, ja que operacoes como
`tr_llama_sample_next_token` nao tem equivalente 1:1 na API real. Em `__Internal` + full AOT iOS isso
e falha de LINK deterministica (10 simbolos indefinidos), nao um risco: cognoscivel sem macOS, com o
proprio artefato ja baixado nesta maquina.

Por isso, ao contrario do texto original de aceitacao ("nunca SUPPORTED"), a linha correta em
`docs/NATIVE-BACKENDS.md` e `PLATFORM ios STATUS UNSUPPORTED`, nao `UNVERIFIED` — `UNVERIFIED` diria
"compila/linka, so nao foi executado aqui", o que e FALSO; `UNSUPPORTED` diz "nao linka", o que e
verdade PROVADA. O job `build-ios` sai do `ci.yml` (nao fica vermelho e commitado — violaria
D-2026-08-16-llm-mobile-10) ate a camada de simbolos nativos existir.

**O que permanece entregue, sem retrabalho** (nada de T-7/T-8 foi removido): `ILlamaNativeAccess` +
`LlamaCppTranslationEngine` com o loop de geracao provado por 15 testes NSubstitute (T-7, intacto);
`scripts/fetch-llama-xcframework.sh` com fetch pinado por tag + verificacao SHA-256 fail-closed
provada nos dois sentidos (`--verify-only` aceita hash certo, rejeita hash errado); `NativeReference`
com caminho literal + `Kind="Static" ForceLoad="True" IsCxx="True"` + `<Error Condition="!Exists(...)">`
no csproj; as 10 declaracoes de `LlamaNativeAccess.cs` continuam no repo, documentadas como
INCOMPLETAS (nao removidas, para nao perder o mapeamento de assinatura ja feito). O Bloco 1
(T-1..T-6, Android) permanece entrega completa e provada, sozinho.

**Caminho para fechar** (fora desta phase): (i) escrever e pinar um shim C compilado que exporte
`tr_llama_*` sobre a API real `llama_*`; ou (ii) redeclarar o P/Invoke direto contra `llama_*`
(marshalling de `llama_batch`/`llama_model_params` + sampler chain). Ambos exigem macOS para
compilar/linkar/validar e nao entram por decisao propria futura — ver
`.jdi/decisions/D-2026-08-16-llm-mobile-12.md`.

## Execution

- Total tasks: 8 (Bloco 1 = T-1..T-6, Bloco 2 = T-7..T-8)
- Waves: 5 — W1 `T-1` | W2 `T-2` `T-3` `T-4` | W3 `T-5` `T-6` | W4 `T-7` | W5 `T-8`
- Speedup paralelo estimado: 1,6x (8 tasks / 5 waves)
- Specialist unico: `jdi-doer-translatereader` (`.jdi/specialists.md` e single-stack, glob `**/*`)
- **Resultado real:** Bloco 1 (T-1..T-6) = entrega completa e provada, Android e alvo de primeira
  classe nos gates. Bloco 2 = T-7 completo (loop de geracao provado no Core atras de contrato
  mockavel); T-8 **parcial** (`D-2026-08-16-llm-mobile-12.md`) — fundacao da cadeia de suprimento e
  do binding pronta, camada de simbolos nativos (`tr_llama_*`) ainda sem shim C, job `build-ios`
  fora do `ci.yml` ate isso fechar.

## Files modified (all tasks)

`.jdi/phases/llm-mobile/BASELINE`, `.jdi/coverage-waivers.txt`, `.jdi/agents/jdi-reviewer-translatereader.md`,
`.github/workflows/ci.yml`, `.gitignore`, `docs/NATIVE-BACKENDS.md`, `docs/MODEL-LICENSES.md`,
`scripts/check-android-so.sh`, `scripts/fetch-llama-xcframework.sh`,
`src/TranslateReader/TranslateReader.csproj`, `src/TranslateReader/MauiProgram.cs`,
`src/TranslateReader/Platforms/iOS/LlamaNativeAccess.cs`,
`src/TranslateReader/Pages/Controls/SettingsOverlay.xaml{,.cs}`,
`src/TranslateReader/PageModels/{Reader,Library}PageModel.cs`,
`src/TranslateReader.Core/Models/{NativeBackendPlan,TranslationUnavailableException,ModelInfo,ReadingSettings}.cs`,
`src/TranslateReader.Core/Contracts/{Utilities/IDeviceMemoryUtility,Access/ILlamaNativeAccess}.cs`,
`src/TranslateReader.Core/Utilities/DeviceMemoryUtility.cs`,
`src/TranslateReader.Core/Business/Engines/{TranslationEngine,UnavailableTranslationEngine,LlamaCppTranslationEngine}.cs`,
`src/TranslateReader.Core/Business/Managers/TranslationManager.cs`,
`src/TranslateReader.Core/Access/SettingsAccess.cs`,
`test/TranslateReader.Tests/{NativeBackendPlan,TranslationEngineAvailability,LlamaCppTranslationEngine,TranslationManager,SnippetTranslationManager,PixelSpec,SettingsAccess}Tests.cs`

## Test requirements

- Unit (xUnit + NSubstitute): `DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release` >= 455 passed / <= 2 skipped / 0 failed
- Build: `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` e `-f net10.0-android` (0E; Android tambem 0W)
- Cobertura: `bash scripts/coverage-gate.sh` — piso 90% em codigo novo pos-`4285f25`, JS `files=5` inalterado
- Scripts: `bash scripts/check-android-so.sh --check-doc docs/NATIVE-BACKENDS.md` e `bash scripts/fetch-llama-xcframework.sh --verify-only` (aceita + rejeita)
- iOS: nenhum gate local. `net10.0-ios` exige macOS e o workload `maui-ios` nao existe nesta maquina.
