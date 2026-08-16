# Phase 6: LLM em Android/iOS — Context  (slug: llm-mobile)

Gerado em 2026-08-16 em modo autonomo (`mode=auto`, `dod=auto_only`) — sem humano no loop.
Brief primario = pesquisa tecnica conduzida em 2026-08-16 (5 investigacoes, ~80 fontes, mais leitura
direta do `build-xcframework.sh` do llama.cpp e do codigo-fonte do LLamaSharp). **Nao e issue de
tracker.** A phase JA EXISTIA no roadmap (`.jdi/roadmap/llm-mobile.md`, position 6, sem artefatos);
o passo `/jdi-add-phase` foi deliberadamente PULADO pelo orquestrador para nao criar um
`llm-mobile-2` duplicado.

Todo valor literal abaixo foi MEDIDO ou verificado por URL nesta sessao. **Nao re-pesquisar** o que
ja esta resolvido aqui — em especial o bloqueio de iOS (secao `Bloqueio iOS`), que foi provado por
leitura de codigo-fonte e nao deve ser reaberto.

## Goal

A traducao offline por LLM local passa a funcionar em Android e iOS (iPhone e iPad), com modelo de
licenca permissiva, **sem nenhuma regressao no Windows e sem quebrar nada que ja funciona**.

## Requisito inegociavel do usuario

> "Nao quebre nenhuma funcionalidade existente. Precisamos sempre evoluir o sistema e nao piorar."

E requisito de PRIMEIRA CLASSE do DoD (DoD 1 e DoD 9), medido contra baselines gravados — nao uma
intencao. Baselines de 2026-08-16 na branch `feat/llm-mobile`:
`dotnet test` = **455 passed / 2 skipped / 0 failed** (os 2 skips sao `TranslationEngineTests` que
exigem GGUF real — pre-existentes, nao mexer); build Android Debug `net10.0-android` = **0 warnings /
0 errors**; build Windows Release = **0 errors**; APK atual = 26 `.so` e **zero** de llama/ggml
(baseline NEGATIVO de DoD 5).

## Ordem de execucao (restricao dura para o planner)

1. **Bloco 1 — base + Android.** TUDO verificavel nesta maquina (Windows, sem workload `maui-ios`).
   Config nativa por plataforma, modelo Apache-2.0, gating de memoria + degradacao graciosa, backend
   Android, `.so` no APK, gates do reviewer corrigidos. Ao fim do Bloco 1, Android demonstravelmente
   pronto e Windows intacto.
2. **Bloco 2 — iOS.** NADA verificavel localmente (so CI macOS + testes de unidade). Engine iOS com
   P/Invoke atras da abstracao mockavel, `NativeReference` com XCFramework pinado, job de CI macOS,
   MacCatalyst tratado.

**NAO comecar pelo iOS.** Se o Bloco 2 travar, o Bloco 1 continua sendo entrega completa; o inverso
nao existe. Se o Bloco 2 se mostrar inviavel, a saida CORRETA e registrar o estado real e deixar iOS
para uma phase seguinte — nunca inventar `Verify:` que passa, nunca declarar iOS funcionando sem
prova, nunca repetir estimativa de tokens/s como se fosse medicao.

`T-1` deve gravar `.jdi/phases/llm-mobile/BASELINE` com o commit base da branch (`git rev-parse HEAD`
antes do primeiro commit da phase) — varios `Verify:` dependem desse arquivo.

## Locked decisions

- **D-2026-08-16-llm-mobile-1** — Dois blocos sequenciais; "nao quebrar nada" e requisito de primeira
  classe medido contra baselines; entrega parcial verdadeira > entrega total nao verificada.
- **D-2026-08-16-llm-mobile-2** — `Hy-MT2-1.8B` (Apache-2.0) entra no registry e vira o default de
  **instalacao nova**; gemma e HY-MT1.5 continuam selecionaveis; fallback de `ResolveModel` NAO muda.
  *Corrige o brief:* o default de hoje e `gemma-2-2b` (`ReadingSettings.cs:12` + `SettingsAccess.cs:54`),
  nao o HY-MT1.5.
- **D-2026-08-16-llm-mobile-3** — Config de backend vira dado puro `NativeBackendPlan.For(platform)`;
  `TranslationEngine.cs` perde todo literal Windows; quatro plataformas testadas na mesma maquina.
- **D-2026-08-16-llm-mobile-4** — Android: `LLamaSharp.Backend.Cpu.Android` 0.27.0 sob Condition
  android; minSdk 21.0 -> 23.0; `.so` e alinhamento 16 KB provados por `scripts/check-android-so.sh`,
  que falha fechado e cujo valor medido e registrado (nao um threshold que nao controlamos).
- **D-2026-08-16-llm-mobile-5** — iOS NAO usa LLamaSharp (falha no static ctor). Engine propria com
  P/Invoke; loop de geracao no Core atras de `ILlamaNativeAccess` mockavel e testado; SO as
  declaracoes em `src/TranslateReader/Platforms/iOS/`; Core NAO vira multi-TFM; XCFramework oficial
  ESTATICO com slice extraida + `NativeReference Kind="Static"` + `[LibraryImport("__Internal")]`.
- **D-2026-08-16-llm-mobile-6** — iOS `SupportedOSPlatformVersion` 15.0 -> **16.4** (minimo do binario
  oficial). App nao publicado -> zero usuarios cortados.
- **D-2026-08-16-llm-mobile-7** — MacCatalyst NAO ganha backend nesta phase; limitacao registrada +
  degradacao graciosa + todo para phase futura.
- **D-2026-08-16-llm-mobile-8** — `TranslationUnavailableException` e a unica forma de recusar; seam
  de memoria unico no Core (`IDeviceMemoryUtility` -> `GC.GetGCMemoryInfo().TotalAvailableMemoryBytes`);
  limiar `ModelInfo.RequiredMemoryBytes = SizeBytes * 1,5`; fronteira de UI segue no `[RelayCommand]`.
- **D-2026-08-16-llm-mobile-9** — XCFramework nunca entra no git; fetch por script pinado por tag +
  SHA-256 fail-closed com modo `--verify-only` testavel sem rede; `NativeReference` com caminho
  literal + `<Error Condition="!Exists(...)">`.
- **D-2026-08-16-llm-mobile-10** — Android vira alvo de primeira classe nos gates; o agent
  `jdi-reviewer-translatereader` e corrigido junto; job de CI iOS so entra na branch se estiver verde.

## Bloqueio iOS (provado por codigo — NAO re-litigar)

`NativeApi.Load.cs`: o static ctor chama `SetDllImportResolver()` e logo `llama_empty_call()`,
forcando o carregamento no primeiro uso do tipo. O early-return de plataforma existe **so para
Android**. Em iOS o resolver E registrado — e `SetDllImportResolver` aceita **um** registro por
assembly, entao o app nao pode registrar o seu. O resolver chama `NativeLibraryUtils.TryLoadLibrary`,
cuja primeira linha e `SystemInfo.Get()`; `Load/SystemInfo.cs:22-40` termina em
`throw new PlatformNotSupportedException()` para qualquer plataforma fora de Windows/Linux/OSX.
A falha e no **static constructor**, antes de qualquer hook. Fornecer binario — estatico OU dinamico
— nao muda isso. **Nao existe rota "reusar o LLamaSharp em iOS".**

## Canonical refs

- Codigo alvo: `src/TranslateReader/TranslateReader.csproj:4-7,45-47,84-87`,
  `src/TranslateReader.Core/Business/Engines/TranslationEngine.cs:16,34-52`,
  `src/TranslateReader.Core/Business/Managers/TranslationManager.cs:25-56`,
  `src/TranslateReader.Core/Models/ReadingSettings.cs:12`,
  `src/TranslateReader.Core/Access/SettingsAccess.cs:54`,
  `src/TranslateReader/MauiProgram.cs:81`, `src/TranslateReader/Pages/Controls/SettingsOverlay.xaml*`,
  `.github/workflows/ci.yml`, `.jdi/agents/jdi-reviewer-translatereader.md` (Gate 1),
  `scripts/coverage-gate.sh`, `.jdi/coverage-waivers.txt`.
- Modelo: https://huggingface.co/tencent/Hy-MT2-1.8B-GGUF/resolve/main/Hy-MT2-1.8B-Q4_K_M.gguf
  (`content-length` medido **1133080448**); licenca HY-MT1.5:
  https://huggingface.co/tencent/HY-MT1.5-1.8B/raw/main/License.txt
- Android backend: https://www.nuget.org/packages/LLamaSharp.Backend.Cpu.Android (0.27.0, 2026-04-26);
  16 KB page size: https://android-developers.googleblog.com/2025/05/prepare-play-apps-for-devices-with-16kb-page-size.html;
  perf nao triada: https://github.com/SciSharp/LLamaSharp/issues/1224
- iOS: https://raw.githubusercontent.com/ggml-org/llama.cpp/master/build-xcframework.sh
  (`BUILD_SHARED_LIBS=OFF`, `IOS_MIN_OS_VERSION=16.4`, `GGML_METAL=ON` + `GGML_METAL_EMBED_LIBRARY=ON`,
  `UIDeviceFamily=[1,2]`, modulemap exige `c++`/`Accelerate`/`Metal`/`Foundation`);
  releases com asset `llama-bXXXX-xcframework.zip`: https://github.com/ggml-org/llama.cpp/releases;
  xcframework estatico via `NativeReference` e caminho quebrado se passado inteiro:
  https://github.com/xamarin/xamarin-macios/issues/19883 (aberta desde 2024-01, sem fix).
- Regras: `CLAUDE.md` (camadas The Method — bloqueantes), `.claude/rules/csharp.md`
  (§1 excecao so pra erro + fronteira no `[RelayCommand]`, §2 alocacao/LOH, §3 concorrencia e UI
  thread, §4 seguranca/supply chain, §6 90% em codigo novo pos-`4285f25`), `.jdi/PROJECT.md`.

## Out of scope

- Trocar o runtime de inferencia (ONNX/GenAI, MLC, ExecuTorch) — fallback documentado, nao implementado.
- Compilar llama.cpp proprio para Android enquanto o pacote oficial atender.
- Mexer em UI/UX de leitura, snippet translation, paginacao ou temas (a UNICA mudanca de UI permitida
  e a linha nova `HyMt2ModelButton` no `SettingsOverlay`).
- Alterar o schema do SQLite ou o formato do cache de traducao.
- Remover Gemma ou HY-MT1.5 do registry.
- Exigir teste em device fisico como gate automatico.
- Corrigir MacCatalyst; mostrar licenca na UI; entitlement de memoria do iOS; calibrar o limiar de
  memoria com medicao real.
Todos registrados em `.jdi/todos/2026-08-16-llm-mobile.md`.

## Definition of Done

> `dod=auto_only`. Comandos em **bash (Git Bash no Windows), executados da RAIZ do repo**.
> `DOTNET_CLI_UI_LANGUAGE=en` porque o sumario local sai em pt-BR. Logs em `TestResults/` (gitignored).
> `BASELINE` = commit gravado por T-1 em `.jdi/phases/llm-mobile/BASELINE`.
> Todo build local passa `-f` explicito: sem isso o MSBuild tenta TFMs sem workload e falha por
> motivo alheio a phase. Os 14 ACs do card estao mapeados: DoD 1 = AC1+AC2, DoD 2 = AC6,
> DoD 3 = AC7+AC8, DoD 4 = AC3, DoD 5 = AC4+AC12, DoD 6 = AC9+AC14, DoD 7 = AC13, DoD 8 = AC5,
> DoD 9 = AC11, DoD 10 = AC10.

### Auto-verifiable

- [ ] **DoD 1 — Nada quebrou: suite verde, nenhum teste perdido NOME A NOME, Windows Release intacto.**
      Contagem sozinha nao serve (testes novos mascaram testes removidos); os 2 skips pre-existentes
      nao podem virar 3
      **Verify:** `mkdir -p TestResults && B=$(cat .jdi/phases/llm-mobile/BASELINE) && test -n "$B" && git grep -hoE 'public (async Task|void) [A-Za-z0-9_]+\(' "$B" -- 'test/TranslateReader.Tests/*.cs' | sed -E 's/^public (async Task|void) //; s/\($//' | sort -u > TestResults/llm-base-tests.txt && test -s TestResults/llm-base-tests.txt && git grep -hoE 'public (async Task|void) [A-Za-z0-9_]+\(' -- 'test/TranslateReader.Tests/*.cs' | sed -E 's/^public (async Task|void) //; s/\($//' | sort -u > TestResults/llm-head-tests.txt && test -z "$(comm -23 TestResults/llm-base-tests.txt TestResults/llm-head-tests.txt)" && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release > TestResults/llm-tests.log 2>&1 && grep -q "Passed!" TestResults/llm-tests.log && awk '/Passed!/{for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1);if($i=="Skipped:")s=$(i+1)}} END{exit (f+0==0 && p+0>=455 && s+0<=2)?0:1}' TestResults/llm-tests.log && DOTNET_CLI_UI_LANGUAGE=en dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0 > TestResults/llm-win.log 2>&1 && grep -qE "^ *0 Error\(s\)" TestResults/llm-win.log`
      **Source:** D-2026-08-16-llm-mobile-1 (AC1, AC2)
- [ ] **DoD 2 — Config nativa e dado puro por plataforma, com Windows byte-identico e as 4 plataformas
      testadas.** `TranslationEngine.cs` sem UM literal de Windows; a selecao de engine no
      `MauiProgram` e condicional de plataforma
      **Verify:** `E=src/TranslateReader.Core/Business/Engines/TranslationEngine.cs; M=src/TranslateReader.Core/Models/NativeBackendPlan.cs; T=test/TranslateReader.Tests/NativeBackendPlanTests.cs; P=src/TranslateReader/MauiProgram.cs; test -f "$M" && test -f "$T" && test "$(grep -cE 'win-x64|cuda12|WithCuda\(true\)' "$E")" -eq 0 && grep -qF 'NativeBackendPlan.For(' "$E" && grep -qF 'WithCuda(plan.UseCuda)' "$E" && grep -qF 'win-x64' "$M" && grep -qF 'cuda12' "$M" && grep -qF '#if IOS' "$P" && test "$(grep -c 'ITranslationEngine' "$P")" -ge 2 && for t in NativeBackendPlan_Windows_KeepsCudaAndTheWin64SearchDirectory NativeBackendPlan_Android_DisablesCudaAndDeclaresNoSearchDirectory NativeBackendPlan_IOS_ReportsTheManagedBackendAsUnsupported NativeBackendPlan_MacCatalyst_ReportsTheManagedBackendAsUnsupported; do grep -qF "$t" "$T" || { echo "MISSING TEST $t"; exit 1; }; done && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~NativeBackendPlan" > TestResults/llm-dod2.log 2>&1 && grep -q "Passed!" TestResults/llm-dod2.log && awk '/Passed!/{for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (f+0==0 && p+0>=4)?0:1}' TestResults/llm-dod2.log`
      **Source:** D-2026-08-16-llm-mobile-3 (AC6)
- [ ] **DoD 3 — Modelo Apache-2.0 default para instalacao nova, licencas documentadas e settings
      legado INTACTO.** A URL responde com exatamente o `SizeBytes` do codigo; quem ja escolheu um
      modelo continua resolvendo para o mesmo arquivo
      **Verify:** `TM=src/TranslateReader.Core/Business/Managers/TranslationManager.cs; L=docs/MODEL-LICENSES.md; T=test/TranslateReader.Tests/TranslationManagerTests.cs; X=src/TranslateReader/Pages/Controls/SettingsOverlay.xaml; grep -qF 'Hy-MT2-1.8B-Q4_K_M.gguf' "$TM" && grep -qF '1_133_080_448' "$TM" && grep -qF 'hy-mt1.5-1.8b' "$TM" && grep -qF 'gemma-2-2b' "$TM" && grep -qF ': GemmaModel;' "$TM" && grep -qF '"hy-mt2-1.8b"' src/TranslateReader.Core/Models/ReadingSettings.cs && grep -qF '"hy-mt2-1.8b"' src/TranslateReader.Core/Access/SettingsAccess.cs && grep -qF 'x:Name="HyMt2ModelButton"' "$X" && grep -qF 'HyMt2ModelButton' test/TranslateReader.Tests/PixelSpecTests.cs && U=$(grep -oE 'https://huggingface\.co/tencent/Hy-MT2-1\.8B-GGUF/resolve/main/[^"]+\.gguf' "$TM" | head -1) && test -n "$U" && CL=$(curl -sILf --max-time 180 "$U" | tr -d '\r' | awk 'tolower($1)=="content-length:"{v=$2} END{print v+0}') && test "$CL" -eq 1133080448 && test -f "$L" && grep -qF 'Apache-2.0' "$L" && grep -qF 'Hy-MT2-1.8B' "$L" && grep -qF 'HY-MT1.5' "$L" && grep -qF 'EUROPEAN UNION' "$L" && grep -qF 'gemma-2-2b' "$L" && for t in DownloadModelIfNeededAsync_WhenSettingsSelectHyMt_DownloadsTheHyMtUrl DownloadModelIfNeededAsync_WhenSettingsSelectAnUnregisteredModel_FallsBackToGemma DownloadModelIfNeededAsync_WhenSettingsAreDefault_DownloadsHyMt2 DownloadModelIfNeededAsync_WhenSettingsSelectALegacyModel_KeepsThatModel; do grep -qF "$t" "$T" || { echo "MISSING TEST $t"; exit 1; }; done && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~WhenSettingsAreDefault_DownloadsHyMt2|FullyQualifiedName~WhenSettingsSelectALegacyModel_KeepsThatModel|FullyQualifiedName~WhenSettingsSelectAnUnregisteredModel_FallsBackToGemma" > TestResults/llm-dod3.log 2>&1 && grep -q "Passed!" TestResults/llm-dod3.log && awk '/Passed!/{for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (f+0==0 && p+0>=3)?0:1}' TestResults/llm-dod3.log`
      **Source:** D-2026-08-16-llm-mobile-2 (AC7, AC8)
- [ ] **DoD 4 — Android compila em Release com o backend oficial na Condition certa e minSdk alinhado
      ao binario.** Windows continua com os seus backends na Condition dele; 0 errors E 0 warnings
      **Verify:** `C=src/TranslateReader/TranslateReader.csproj; grep -qE 'LLamaSharp\.Backend\.Cpu\.Android"[^>]*Version="0\.27\.0"' "$C" && L=$(grep -n 'LLamaSharp.Backend.Cpu.Android' "$C" | head -1 | cut -d: -f1) && test -n "$L" && G=$(head -n "$L" "$C" | grep -n '<ItemGroup' | tail -1 | cut -d: -f1) && sed -n "${G}p" "$C" | grep -qF "== 'android'" && LW=$(grep -n 'LLamaSharp.Backend.Cuda12' "$C" | head -1 | cut -d: -f1) && GW=$(head -n "$LW" "$C" | grep -n '<ItemGroup' | tail -1 | cut -d: -f1) && sed -n "${GW}p" "$C" | grep -qF "== 'windows'" && grep -qE "== 'android'\">23\.0<" "$C" && test "$(grep -cE "== 'android'\">21\.0<" "$C")" -eq 0 && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-android > TestResults/llm-android.log 2>&1 && grep -qE "^ *0 Error\(s\)" TestResults/llm-android.log && grep -qE "^ *0 Warning\(s\)" TestResults/llm-android.log`
      **Source:** D-2026-08-16-llm-mobile-4 (AC3)
- [ ] **DoD 5 — `libllama.so` arm64-v8a chega no APK e o alinhamento de CADA `.so` nativo esta MEDIDO
      e REGISTRADO sem divergencia.** Baseline negativo: hoje o APK tem 26 `.so` e ZERO de llama/ggml.
      Script falha fechado — zero `.so` encontrado e falha, nunca sucesso vazio
      **Verify:** `S=scripts/check-android-so.sh; D=docs/NATIVE-BACKENDS.md; test -f "$S" && test -f "$D" && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet build src/TranslateReader/TranslateReader.csproj -c Debug -f net10.0-android > TestResults/llm-android-dbg.log 2>&1 && grep -qE "^ *0 Error\(s\)" TestResults/llm-android-dbg.log && bash "$S" --check-doc "$D" > TestResults/llm-so.log 2>&1 && grep -qE '^SO_FOUND lib/arm64-v8a/libllama\.so$' TestResults/llm-so.log && grep -qE '^SO_COUNT [1-9][0-9]*$' TestResults/llm-so.log && test "$(grep -c '^SO_ALIGN ' TestResults/llm-so.log)" -ge 1 && test "$(grep -c '^SO_ALIGN ' TestResults/llm-so.log)" -eq "$(grep -c '^SO_FOUND ' TestResults/llm-so.log)" && while read -r line; do grep -qF "$line" "$D" || { echo "DOC MISSING: $line"; exit 1; }; done < <(grep '^SO_ALIGN ' TestResults/llm-so.log)`
      **Source:** D-2026-08-16-llm-mobile-4 (AC4, AC12)
- [ ] **DoD 6 — Plataforma sem backend e device sem memoria RECUSAM com erro tratado, nunca crash; a
      matriz de plataformas esta escrita.** Cobre tambem o MacCatalyst (AC14): estado declarado com
      token ASCII checavel, nao prosa
      **Verify:** `X=src/TranslateReader.Core/Models/TranslationUnavailableException.cs; I=src/TranslateReader.Core/Contracts/Utilities/IDeviceMemoryUtility.cs; U=src/TranslateReader.Core/Utilities/DeviceMemoryUtility.cs; T=test/TranslateReader.Tests/TranslationEngineAvailabilityTests.cs; D=docs/NATIVE-BACKENDS.md; test -f "$X" && test -f "$I" && test -f "$U" && test -f "$T" && grep -qF 'RequiredMemoryBytes' src/TranslateReader.Core/Models/ModelInfo.cs && grep -qF 'TotalAvailableMemoryBytes' "$U" && grep -qF 'IDeviceMemoryUtility' src/TranslateReader/MauiProgram.cs && grep -qF 'TranslationUnavailableException' src/TranslateReader/PageModels/ReaderPageModel.cs && grep -qF 'TranslationUnavailableException' src/TranslateReader/PageModels/LibraryPageModel.cs && for t in InitializeEngineIfNeededAsync_WhenDeviceMemoryIsBelowTheModelRequirement_ThrowsTranslationUnavailable InitializeEngineIfNeededAsync_WhenTheBackendIsUnsupportedOnThisPlatform_ThrowsTranslationUnavailable InitializeEngineIfNeededAsync_WhenDeviceMemoryIsSufficient_InitializesTheEngine; do grep -qF "$t" "$T" || { echo "MISSING TEST $t"; exit 1; }; done && test -f "$D" && grep -qE '^PLATFORM windows STATUS SUPPORTED ' "$D" && grep -qE '^PLATFORM android STATUS SUPPORTED ' "$D" && grep -qE '^PLATFORM ios STATUS (SUPPORTED|UNVERIFIED|UNSUPPORTED) ' "$D" && grep -qE '^PLATFORM maccatalyst STATUS UNSUPPORTED ' "$D" && grep -qF 'D-2026-08-16-llm-mobile-7' "$D" && test -f .jdi/decisions/D-2026-08-16-llm-mobile-7.md && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~TranslationEngineAvailability" > TestResults/llm-dod6.log 2>&1 && grep -q "Passed!" TestResults/llm-dod6.log && awk '/Passed!/{for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (f+0==0 && p+0>=3)?0:1}' TestResults/llm-dod6.log`
      **Source:** D-2026-08-16-llm-mobile-8, D-2026-08-16-llm-mobile-7 (AC9, AC14)
- [ ] **DoD 7 — A referencia nativa do iOS NAO e no-op e a cadeia de suprimento e fail-closed
      PROVADA.** Caminho literal (nunca glob) + `<Error Condition="!Exists(...)">` + tag e SHA-256
      literais + o comparador de checksum REJEITA hash errado (executado, nao grepado). O binding so
      declara: zero controle de fluxo, zero P/Invoke no Core, zero ponteiro nos contratos
      **Verify:** `C=src/TranslateReader/TranslateReader.csproj; F=scripts/fetch-llama-xcframework.sh; P=src/TranslateReader/Platforms/iOS/LlamaNativeAccess.cs; test -f "$F" && test -f "$P" && grep -qF 'NativeReference' "$C" && test "$(grep 'NativeReference' "$C" | grep -c '\*')" -eq 0 && grep -qF 'Kind="Static"' "$C" && grep -qF 'ForceLoad="True"' "$C" && grep -qF 'IsCxx="True"' "$C" && grep -qE '<Error [^>]*Condition="!Exists\(' "$C" && grep -qF 'FetchLlamaXcframework' "$C" && grep -qF 'fetch-llama-xcframework.sh' "$C" && TAG=$(grep -oE '<LlamaCppRelease>[^<]+</LlamaCppRelease>' "$C" | sed -E 's:</?LlamaCppRelease>::g') && echo "$TAG" | grep -qE '^b[0-9]+$' && grep -oE '<LlamaCppXcframeworkSha256>[0-9a-f]{64}</LlamaCppXcframeworkSha256>' "$C" | grep -q . && test "$(grep -i 'llama' "$C" | grep -ciE 'latest|/master|/main/')" -eq 0 && grep -qi 'xcframework' .gitignore && test -z "$(git ls-files | grep -i xcframework | grep -vE '^(scripts/|\.jdi/|docs/)')" && tmp=$(mktemp) && printf 'llm-mobile' > "$tmp" && SHA=$(sha256sum "$tmp" | cut -d' ' -f1) && bash "$F" --verify-only "$tmp" "$SHA" && ! bash "$F" --verify-only "$tmp" 0000000000000000000000000000000000000000000000000000000000000000 && rm -f "$tmp" && grep -qF '__Internal' "$P" && test "$(grep -cE 'LibraryImport|DllImport' "$P")" -ge 10 && test "$(grep -cE '\b(if|for|foreach|while|switch|try)\b' "$P")" -eq 0 && test -z "$(grep -rlE 'LibraryImport|DllImport' src/TranslateReader.Core/ --include=*.cs)" && test -z "$(grep -rlE '\bnint\b|IntPtr|LibraryImport|DllImport' src/TranslateReader.Core/Contracts/ --include=*.cs)"`
      **Source:** D-2026-08-16-llm-mobile-5, D-2026-08-16-llm-mobile-9 (AC13)
- [ ] **DoD 8 — Job de CI iOS existe e esta bem formado, os outros tres jobs continuam intactos, e o
      Gate 1 do reviewer foi corrigido.** *Nao prova que o build iOS passa* — isso e
      `## Deferred to PR review`. Checagem de tabs cobre o erro de sintaxe YAML mais comum
      **Verify:** `W=.github/workflows/ci.yml; R=.jdi/agents/jdi-reviewer-translatereader.md; test -f "$W" && ! grep -q "$(printf '\t')" "$W" && { { grep -qE '^  build-ios:' "$W" && grep -qE '^ +runs-on: macos' "$W" && grep -qF 'dotnet workload install maui-ios' "$W" && grep -qF 'dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-ios' "$W"; } || { test "$(grep -cE '^  build-ios:' "$W")" -eq 0 && test -f .jdi/decisions/D-2026-08-16-llm-mobile-12.md && grep -qF 'build-ios' "$W" && grep -qE '^PLATFORM ios STATUS UNSUPPORTED ' docs/NATIVE-BACKENDS.md; }; } && grep -qF 'dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-android' "$W" && grep -qF 'dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0' "$W" && grep -qF 'dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release' "$W" && JOBS=$(grep -cE '^  (test|build|build-android|build-ios):' "$W") && test "$JOBS" -ge 3 && test "$(grep -c 'actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1' "$W")" -eq "$JOBS" && test "$(grep -c 'actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68' "$W")" -eq "$JOBS" && test "$(grep -cE '@(v[0-9]+|main|master)[[:space:]]*$' "$W")" -eq 0 && grep -qF 'dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-android' "$R" && test "$(grep -cE '^dotnet build -f net10\.0-android$' "$R")" -eq 0 && test "$(grep -c 'a missing workload is reported as WARN, never as BLOCK' "$R")" -eq 0 && grep -qF 'Android build failure = BLOCK' "$R" && grep -qF 'iOS is never a local gate' "$R"`
      **Source:** D-2026-08-16-llm-mobile-10 (AC5)
- [ ] **DoD 9 — The Method preservado e o que estava fora do escopo continua BYTE A BYTE igual.**
      Nenhum tipo de backend nativo vaza para fora de `Business/Engines/`; Client nao pula camada;
      nenhum static mutavel novo (baseline do reviewer 5.12 = exatamente 1)
      **Verify:** `B=$(cat .jdi/phases/llm-mobile/BASELINE) && test -n "$B" && test -z "$(grep -rlE '^using LLama' src/ --include=*.cs | grep -v '^src/TranslateReader.Core/Business/Engines/')" && test -z "$(grep -rlE 'LLama|LibraryImport|DllImport|NativeBackendPlan' src/TranslateReader.Core/Business/Managers/ --include=*.cs)" && test -z "$(grep -rlE 'using TranslateReader\.Core\.(Access|Business\.Engines)' src/TranslateReader/PageModels/ src/TranslateReader/Pages/ --include=*.cs)" && test -z "$(grep -rlE 'Sqlite(Connection|Command|DataReader)|System\.Data\.' src/TranslateReader.Core/Contracts/Access/ --include=*.cs)" && test "$(grep -rnE '\bstatic\b' src/TranslateReader.Core/ src/TranslateReader/ --include=*.cs | grep -vE 'static\s+(readonly|class|partial)' | grep -vE '\(' | wc -l)" -le "$(git grep -rnE '\bstatic\b' "$B" -- 'src/TranslateReader.Core/*.cs' 'src/TranslateReader/*.cs' | grep -vE 'static\s+(readonly|class|partial)' | grep -vE '\(' | wc -l)" && test -z "$(grep -rnE '\)\.Result\b|\.Wait\(\)|GetAwaiter\(\)\.GetResult\(\)' src/ --include=*.cs)" && test -z "$(git diff --name-only "$B" -- src/TranslateReader/Resources/Raw/wwwroot/ src/TranslateReader.Core/Business/Engines/ParsingEngine.cs src/TranslateReader.Core/Business/Engines/ThemeEngine.cs src/TranslateReader.Core/Business/Managers/ReadingManager.cs src/TranslateReader.Core/Business/Managers/LibraryManager.cs src/TranslateReader.Core/Business/Managers/SettingsManager.cs src/TranslateReader.Core/Access/BooksAccess.cs src/TranslateReader.Core/Access/ReadingStateAccess.cs src/TranslateReader.Core/Access/TranslationCacheAccess.cs src/TranslateReader.Core/Access/SnippetTranslationAccess.cs src/TranslateReader.Core/Access/BookTranslationJobAccess.cs src/TranslateReader.Core/Utilities/HtmlUtility.cs src/TranslateReader.Core/Utilities/PromptUtility.cs)"`
      **Source:** D-2026-08-16-llm-mobile-1, D-2026-08-16-llm-mobile-5 (AC11)
- [ ] **DoD 10 — Gate de cobertura verde, com waiver DISCIPLINADO.** Todo waiver aponta para arquivo
      existente e cita uma decisao DESTA phase; contagem de JS inalterada (5); nenhum waiver invalido
      **Verify:** `mkdir -p TestResults && bash scripts/coverage-gate.sh > TestResults/llm-gate.log 2>&1 && grep -qE '^COVERAGE_SCOPE ' TestResults/llm-gate.log && grep -qE '^COVERAGE_JS .*files=5$' TestResults/llm-gate.log && test "$(grep -c 'COVERAGE_WAIVER_INVALID' TestResults/llm-gate.log)" -eq 0 && G=$(grep -E '^COVERAGE_GUARD ' TestResults/llm-gate.log) && test -n "$G" && N=$(echo "$G" | sed -E 's/.*new_app_cs=([0-9]+).*/\1/') && WV=$(echo "$G" | sed -E 's/.*waived=([0-9]+).*/\1/') && test "$WV" -ge "$N" && W=.jdi/coverage-waivers.txt && A=$(grep -cE '^src/' "$W" || true) && Bq=$(grep -E '^src/' "$W" | grep -cF 'D-2026-08-16-llm-mobile-' || true) && test "$A" -eq "$Bq" && while read -r p; do test -f "$p" || { echo "WAIVED PATH MISSING: $p"; exit 1; }; done < <(grep -E '^src/' "$W" | awk '{print $1}')`
      **Source:** D-2026-08-16-llm-mobile-5 (AC10)

### Manual

- _(none — `dod=auto_only`; itens que exigem humano/hardware foram para `## Deferred to PR review`)_

## Deferred to PR review

Nao sao itens descartados: sao itens que NENHUM comando desta maquina pode provar. Um `Verify:` que
exita 0 sem provar o item e o "hollow PASS" que o DoD critic existe para pegar — preferimos declarar
a limitacao a inventar um comando que passa. O chain autonomo os expoe no corpo do PR.

- **Build iOS VERDE.** Exige runner macOS; esta maquina e Windows e nem tem o workload `maui-ios`.
  DoD 8 prova que o job existe e esta bem formado — nada mais. O verde so aparece no PR
  (D-2026-08-16-llm-mobile-10; regra: job vermelho nao e commitado).
- **Inferencia real em iPhone/iPad.** Metal exige GPU Apple7+ (A14/M1) e **nao roda no simulador**.
  Precisa de device fisico com o GGUF de 1,06 GB baixado.
- **Inferencia real em Android.** Device fisico ou emulador com o modelo baixado.
- **Numeros de tokens/s.** As faixas da pesquisa (iOS Metal ~25-40 t/s; Android CPU ~10-20 t/s) sao
  ESTIMATIVAS ancoradas em benchmarks de 1B/3B. Se nao houver medicao em hardware real, dizer que nao
  foi medido — nunca repetir a estimativa como resultado observado.
- **Perf do `StatelessExecutor` no Android (issue #1224).** ~16 t/s no `llama-bench` contra ~0.18 t/s
  no executor, sem resposta de maintainer. Se reproduzir, documentar com NUMERO MEDIDO.
- **Qualidade linguistica do Hy-MT2 vs HY-MT1.5.** Exige rodar os dois GGUF; nao ha assert possivel.
- **Comportamento real em MacCatalyst.** Exige um Mac (D-2026-08-16-llm-mobile-7).
- **Crescimento do pacote** (`.so` do Android, framework estatico do iOS) — so mensuravel num package
  build por loja.
- **Alinhamento 16 KB conforme a ferramenta do proprio Google Play.** DoD 5 mede o LOAD align do ELF,
  que e um proxy fiel mas nao e o veredito da loja.
- **Aceitacao nas lojas** do payload nativo (App Store rejeita `.dylib` solta; usamos framework
  estatico, mas so a submissao decide).
- **SonarCloud sem issue nova** nos arquivos tocados — so existe apos push + CI.

## Notes

### Achados no codigo real (verificados nesta sessao — nao inferidos do brief)

1. **O default de traducao HOJE e `gemma-2-2b`, nao HY-MT1.5.** Declarado em DOIS lugares
   (`Models/ReadingSettings.cs:12` e `Access/SettingsAccess.cs:54`). O brief dizia que HY-MT1.5 era o
   default; ele e apenas selecionavel. A troca de default acontece nesses dois pontos.
2. **`ResolveModel` faz fallback para Gemma para nome desconhecido** — e o `SettingsOverlay` GRAVA
   dois nomes que nao existem no registry (`qwen-2.5-3b`, `phi-3.5`). Mudar o alvo do fallback
   dispararia download de 1,06 GB para esses usuarios. Nao mexer.
3. **`TranslateReader.Core.csproj` e `<TargetFramework>net10.0</TargetFramework>` — TFM UNICO.**
   E o fato que decide a arquitetura do Bloco 2 (D-2026-08-16-llm-mobile-5): nada de `#if IOS` no
   Core, e multi-targetar o Core arriscaria o restore do projeto de teste.
4. **`ITranslationEngine` tem 4 membros e ZERO tipo LLamaSharp na assinatura** — e o ponto de corte
   limpo para engines por plataforma. Nao alargar o contrato.
5. **`scripts/coverage-gate.sh` trata `.cs` novo do app e `.cs` novo de OUTRO projeto de formas
   opostas**: o primeiro dispara `COVERAGE_GUARD` (exit 2) e exige waiver citando um `D-`; o segundo
   cairia em `COVERAGE_SKIP reason=no-instrumented-lines` e sumiria em silencio. Por isso o binding
   iOS mora no app, com waiver — prestacao de contas, nao conveniencia.
6. **`PixelSpecTests.ModelRowNames`** = `["GemmaModelButton","QwenModelButton","PhiModelButton","HyMtModelButton"]`
   e `SettingsOverlay_ModelsAreAVerticalRadioList` tambem proibe `Orientation="Horizontal"` no XAML
   inteiro. A linha nova entra sem quebrar isso, e o nome novo entra no array.
7. **Os PageModels ja tem `catch (Exception ex)` dentro dos `[RelayCommand]`**
   (`ReaderPageModel.cs:98,130,258,356`; `LibraryPageModel.cs:271`) — a fronteira de conversao para UI
   ja existe; a phase so trata o tipo novo ali. Nenhuma outra camada converte excecao em estado de UI.
8. **`_nativeLibraryConfigured` (`TranslationEngine.cs:16`) e o UNICO static mutavel do repo** e ja e
   o baseline WARN do gate 5.12 do reviewer. A phase nao pode introduzir um segundo.
9. **`ci.yml` e reusable workflow** chamado por `pipeline.yml`; jobs atuais: `test` (ubuntu),
   `build` (windows), `build-android` (ubuntu). Todas as actions pinadas por SHA.

### Nomes prescritos (o DoD depende deles — nao renomear)

`src/TranslateReader.Core/Models/NativeBackendPlan.cs` (+ enum `TranslationPlatform`, factory
`NativeBackendPlan.For(...)`, propriedade `UseCuda`), `Models/TranslationUnavailableException.cs`,
`Models/ModelInfo.RequiredMemoryBytes`, `Contracts/Utilities/IDeviceMemoryUtility.cs`,
`Utilities/DeviceMemoryUtility.cs`, `Contracts/Access/ILlamaNativeAccess.cs`,
`Business/Engines/LlamaCppTranslationEngine.cs`,
`src/TranslateReader/Platforms/iOS/LlamaNativeAccess.cs`,
`src/TranslateReader/Pages/Controls/SettingsOverlay.xaml` -> `x:Name="HyMt2ModelButton"`.
Testes: `NativeBackendPlanTests.cs`, `TranslationEngineAvailabilityTests.cs`,
`LlamaCppTranslationEngineTests.cs`, mais os 2 nomes novos em `TranslationManagerTests.cs`.
Scripts/docs: `scripts/check-android-so.sh` (tokens `SO_FOUND` / `SO_ALIGN ... align=<n>` /
`SO_COUNT` e modo `--check-doc`), `scripts/fetch-llama-xcframework.sh` (modo
`--verify-only <arquivo> <sha256>`), `docs/MODEL-LICENSES.md`, `docs/NATIVE-BACKENDS.md` (linhas
`PLATFORM <tfm> STATUS <SUPPORTED|UNVERIFIED|UNSUPPORTED> ...` + as linhas `SO_ALIGN`).
MSBuild: propriedades `LlamaCppRelease` (tag `bNNNN`) e `LlamaCppXcframeworkSha256` (64 hex), target
`FetchLlamaXcframework`.

### Sequencia sugerida ao planner

**Bloco 1:** T-1 BASELINE + `docs/NATIVE-BACKENDS.md` inicial -> T-2 `NativeBackendPlan` + testes das
4 plataformas + `TranslationEngine` limpo -> T-3 Hy-MT2 no registry + default + licencas + linha do
`SettingsOverlay` + testes de settings legado -> T-4 `IDeviceMemoryUtility` +
`TranslationUnavailableException` + gating + tratamento nos PageModels -> T-5 backend Android +
minSdk 23 + `scripts/check-android-so.sh` + registro do alinhamento -> T-6 correcao do agent
`jdi-reviewer-translatereader` (Gate 1).
**Bloco 2:** T-7 job de CI iOS (probe: so fica se verde) -> T-8 `scripts/fetch-llama-xcframework.sh`
+ pins + target MSBuild + `.gitignore` -> T-9 `ILlamaNativeAccess` + `LlamaCppTranslationEngine` +
testes com NSubstitute -> T-10 `Platforms/iOS/LlamaNativeAccess.cs` + `NativeReference` + `#if IOS`
no `MauiProgram` + waiver de cobertura.
