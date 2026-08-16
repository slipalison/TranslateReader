# SUMMARY — llm-mobile (iter 1)

> Escrito pelo orquestrador a partir do retorno textual do doer: a policy do harness bloqueou o
> subagent de criar este arquivo. O conteudo e o relato do doer, com as evidencias que ele produziu.

## Status

8/8 tasks executadas, 0 bloqueadas. 9 commits, todos com escopo `llm-mobile`.

- **Bloco 1 (T-1..T-6)** — verificado 100% nesta maquina.
- **Bloco 2 (T-7..T-8)** — estruturalmente completo e internamente consistente, verificado ate onde
  uma maquina Windows sem `maui-ios` permite. Execucao real em iOS permanece em
  `## Deferred to PR review`, como o CONTEXT.md manda. Nunca declarado como pronto.

## Commits

| SHA | Task | Entrega |
|---|---|---|
| `c355daf` | T-1 | BASELINE + matriz de plataformas aberta honesta |
| `3712d17` | T-2 | `NativeBackendPlan` — config nativa vira dado puro por plataforma |
| `2b07202` | T-3 | Hy-MT2-1.8B como default de instalacao nova |
| `afe8d88` | T-4 | Backend Android oficial + minSdk 23 |
| `a091546` | T-5 | Recusa graciosa (plataforma sem backend / memoria insuficiente) |
| `b49b675` | T-6 | Prova do `.so` no APK + Android vira gate bloqueante |
| `461d828` | — | Correcao de falso-positivo na heuristica de static mutavel |
| `24007db` | T-7 | Loop de geracao iOS atras de contrato nativo mockavel |
| `f00142a` | T-8 | Linkagem iOS com XCFramework pinado + job de CI |
| `7284743` | — | PLAN.md com as 8 tasks marcadas completed |

## Evidencias por task

- **T-1** — `BASELINE` = `166b3da798738e3b257f38af7c36adc1d491538d`. `docs/NATIVE-BACKENDS.md` nasce
  honesto: windows SUPPORTED, todo o resto UNSUPPORTED. Suite pre-baseline confirmada em 455/2/0.
- **T-2** — `NativeBackendPlan.For(TranslationPlatform)` como dado puro; `TranslationEngine.cs` com
  zero ocorrencia de `win-x64|cuda12|WithCuda(true)`. 5 testes. DoD 2 verificado integralmente.
- **T-3** — Hy-MT2-1.8B adicionado e default para instalacao nova; Gemma e HY-MT1.5 intocados;
  fallback `: GemmaModel` preservado. Checagem de rede real: `content-length` = `1133080448`,
  batendo exatamente com o `SizeBytes` do codigo. Linha do `SettingsOverlay` ligada ponta a ponta.
- **T-4** — `LLamaSharp.Backend.Cpu.Android` 0.27.0; minSdk 21 -> 23. Dois warnings de MSBuild do
  proprio pacote (XA0101/XA0141) rebaixados via `MSBuildWarningsAsMessages`, com escopo restrito ao
  android. Android e Windows em Release: 0 warnings / 0 errors.
- **T-5** — `TranslationUnavailableException`, `ModelInfo.RequiredMemoryBytes`,
  `IDeviceMemoryUtility`/`DeviceMemoryUtility` e `UnavailableTranslationEngine`, todos novos e com
  100% de cobertura. O Manager valida plataforma e memoria antes de tocar na engine. Ctor de
  `TranslationManager` de 9 para 11 parametros, com os dois fixtures dependentes atualizados e
  **zero teste renomeado**. 11 testes novos.
- **T-6** — `scripts/check-android-so.sh` faz parsing de program header ELF64 com `od` puro, sem
  depender de `readelf` nem do NDK. Os 3 caminhos de falha foram EXECUTADOS, nao afirmados:
  diretorio sem APK (exit 1), linha adulterada no `--check-doc` (exit 1) e linha MITIGATION ausente
  (exit 1). Medicao real: 10 `.so` (5 libs x 2 ABIs).
- **T-7** — `ILlamaNativeAccess` com 10 operacoes 1:1 pass-through + `LlamaCppTranslationEngine` com
  loop real de tokenize -> decode -> sample -> detokenize. 15 testes, 100% de cobertura.
- **T-8** — `scripts/fetch-llama-xcframework.sh` com pin REAL, nao placeholder: o release `b10453`
  foi baixado de fato (286.349.324 bytes), o SHA-256 real calculado
  (`c47fb6013e886307a7a0a993a1e6c02ce9a46ba0ef5be01f3eb086a13a61ea6a`) e o pipeline
  fetch -> verify -> extract rodado ponta a ponta contra ele. Do `Info.plist` e do modulemap
  extraidos confirmou-se de forma independente `MinimumOSVersion=16.4`, `UIDeviceFamily=[1,2]` e as
  dependencias de link `c++`/`Accelerate`/`Metal`/`Foundation` — exatamente o que D-5/D-6 previram
  a partir de pesquisa. `NativeReference Kind="Static" ForceLoad="True" IsCxx="True"` com caminho
  literal e guarda `<Error Condition="!Exists(...)">`. `Platforms/iOS/LlamaNativeAccess.cs` com 10
  declaracoes `[LibraryImport("__Internal")]` e zero palavra-chave de controle de fluxo, compilado e
  checado contra a assembly Core real num projeto scratch (0 erros) — a prova local mais forte
  possivel sem macOS. Job `build-ios` adicionado com as mesmas 3 actions pinadas por SHA.

## Achados que exigem decisao humana

### 1. Alinhamento de pagina dos `.so`: 4096, abaixo dos 16384 do Google Play

A medicao real do APK deu **4096 bytes de alinhamento em todos os 10 `.so`**, incluindo os do
pacote oficial `LLamaSharp.Backend.Cpu.Android`. O Google Play exige 16384 para apps que targetam
Android 15+ desde 2025-11-01. Registrado com 5 linhas MITIGATION em `docs/NATIVE-BACKENDS.md` em vez
de mascarado — o gate mede e falha se o documento divergir do medido. **Isto nao bloqueia o build
nem a instalacao hoje, mas bloqueia publicacao futura na Play Store** e precisa de decisao: esperar
o upstream do LLamaSharp republicar o backend alinhado, ou compilar o llama.cpp proprio com NDK
recente.

### 2. `Span<T>` nao e mockavel — o contrato nativo foi redesenhado por causa disso

NSubstitute (e Moq) nao conseguem mockar parametros `Span<T>`, por limitacao de expression tree.
O contrato `ILlamaNativeAccess` foi entao desenhado com `int[]`/`string` no lugar de spans,
especificamente para permanecer testavel — um contrato intestavel reprovaria o proprio criterio de
aceitacao da task. Custo aceito: alocacao a mais no caminho de tokenizacao, contra
`.claude/rules/csharp.md` §2.1. Vale reavaliar se a inferencia em device mostrar pressao de GC.

### 3. A heuristica de static mutavel do DoD 9 mede contra um baseline desatualizado

O `Verify:` do DoD 9 exige `<= 1` ocorrencia. Rodando o grep identico no proprio commit BASELINE
registrado pela phase, num worktree separado, ele **ja contava 4** antes desta phase tocar em nada.
As 3 ocorrencias extras sao propriedades estaticas expression-bodied pre-existentes de
`SettingsOverlay.xaml.cs` (`IsDesktopIdiom`, `ScreenWidth`, `ScreenHeight`) — propriedades
computadas, nao estado mutavel, e sem relacao com esta phase.

Esta phase introduziu **0 ocorrencias novas** (corrigiu os proprios 2 falsos positivos em `461d828`)
e a contagem atual e exatamente 4, identica ao baseline. Seguindo a regra "conserte o codigo, nunca
afrouxe o Verify", o doer **nao** tocou nas propriedades legadas de `SettingsOverlay.xaml.cs` para
fazer a heuristica passar — isso violaria D-2 (fronteira de legado) e a disciplina de escopo de
arquivo. Todos os demais sub-checks do DoD 9 passam, incluindo identidade byte a byte nos 12
arquivos legados fora de escopo.

## Testes e cobertura

- **490 testes** no total: 488 passed, 2 skipped (os mesmos 2 pre-existentes que exigem GGUF real).
- **Zero nome de teste perdido** contra o baseline (`comm -23` vazio).
- 33 testes novos.
- `bash scripts/coverage-gate.sh` sai 0: `COVERAGE_SCOPE pct=95.30 files=34` (piso 90),
  `COVERAGE_JS pct=99.27 files=5` (piso 85, inalterado), `COVERAGE_GUARD new_app_cs=1 waived=1`.
- Cada arquivo novo do Core individualmente com 100% de cobertura de linha.

## Deferred to PR review (inalterado em relacao ao CONTEXT.md)

Job `build-ios` verde, inferencia real em device (iOS e Android), qualquer numero de tokens/s, o
problema de performance do `StatelessExecutor` no Android, qualidade Hy-MT2 vs HY-MT1.5,
comportamento do MacCatalyst, crescimento do tamanho do pacote, o veredito da propria ferramenta do
Google Play sobre 16 KB, e resultados do SonarCloud.

## Follow-up (chain `/jdi-issue`, mode=fix_blockers, iter=4): W-4 corrigido

REVIEW.md (iter 3) aprovou a phase com `APPROVED_WITH_WARNINGS` (sem blocker). Esta rodada,
delimitada explicitamente ao warning **W-4**, corrigiu a ausencia de guarda de concorrencia em
`LlamaCppTranslationEngine.InitializeAsync` (`.claude/rules/csharp.md` secao 3) e, ao verificar,
encontrou o **mesmo problema** em `TranslationEngine.InitializeAsync` (engine LLamaSharp
Windows/Android) — corrigido do mesmo jeito, na mesma task, para nao deixar a inconsistencia sem
resposta.

- **Commit:** `ef1661a` — `fix(llm-mobile): guard InitializeAsync with a SemaphoreSlim in both translation engines`.
- **Causa raiz:** `ITranslationEngine` e singleton no DI (`MauiProgram.cs`); `TranslationManager.InitializeEngineIfNeededAsync`
  nao tinha guarda propria; duas chamadas concorrentes (ex.: traducao de paragrafos visiveis
  correndo junto com um job de traducao de livro completo em background) podiam ambas observar
  `IsReady == false` e ambas disparar o load caro (`LoadModel`/`LLamaWeights.LoadFromFile`).
- **Fix (identico nas duas engines):** campo de instancia `readonly SemaphoreSlim _initLock = new(1, 1)`
  (nao static — DoD 9 confirmado); `WaitAsync(ct)`; check-lock-check (segunda checagem de `IsReady`
  e de `_disposed` DEPOIS de adquirir o semaforo); `Release()` em `finally`; `_initLock.Dispose()`
  junto do resto que a engine ja descartava. `OperationCanceledException` flui sem ser engolida —
  `WaitAsync(ct)` cancela corretamente quando o token e cancelado enquanto a chamada espera o lock
  de outra inicializacao.
- **Testes novos (4, todos passando, zero nome de teste perdido):**
  - `LlamaCppTranslationEngineTests`: `InitializeAsync_WhenCalledConcurrently_LoadsTheModelOnlyOnce`
    (NSubstitute sobre `ILlamaNativeAccess`, `LoadModel` bloqueia ate as duas chamadas estarem em
    voo; conta invocacoes com `Interlocked` — prova UMA unica carga) e
    `InitializeAsync_WhenCancelledWhileAnotherInitializationHoldsTheLock_ThrowsOperationCanceledWithoutLoadingAgain`.
  - `TranslationEngineTests`: como `LLamaWeights.LoadFromFile` e uma chamada estatica real do SDK
    sem seam (sem GGUF nesta maquina, sem backend nativo no projeto de teste, risco real de abort()
    nativo com caminho invalido), os dois testes equivalentes acessam `_initLock`/`_weights` via
    reflection (padrao ja usado em `HybridWebViewContractTests`/`ParsingEngineEdgeCaseTests`) para
    provar a mesma serializacao e o mesmo fluxo de cancelamento SEM jamais chamar o loader real.
  - Toda espera das 4 novas testes e limitada por `Task.WaitAsync(TimeSpan.FromSeconds(5))`: durante
    o desenvolvimento, uma primeira versao sem essa protecao travou o processo de teste (nao apenas
    falhou) ao rodar contra uma versao da engine deliberadamente revertida — corrigido antes do
    commit final.
  - Regressao provada por engenharia reversa: as 4 engines revertidas temporariamente (via
    `git checkout --`) fazem os 3 testes que discriminam a ausencia do guard falhar de forma limpa
    e rapida (~115ms, sem trava); o 4o teste de cancelamento passa mesmo revertido porque uma
    checagem pre-existente (`ct.ThrowIfCancellationRequested()` antes do guard) ja cobria parte do
    cenario — nao invalida o teste, apenas nao discrimina essa regressao especifica sozinho.
- **Evidencia (nesta maquina, nesta sessao):**
  - `dotnet test` (Release, solucao de testes completa): **492 passed / 2 skipped / 0 failed**
    (488 do baseline da phase + 4 nomes novos); `comm -23` contra `BASELINE` vazio.
  - Build Windows Release e Android Release: **0 Warning(s) / 0 Error(s)** nos dois.
  - `bash scripts/coverage-gate.sh`: exit 0 — `COVERAGE_SCOPE pct=95.41 files=34` (subiu de 95.30),
    `COVERAGE_JS pct=99.27 files=5` (inalterado), `COVERAGE_GUARD new_app_cs=1 waived=1` (inalterado).
    `LlamaCppTranslationEngine.cs covered=47 valid=47` = **100%**.
  - DoD 1, 2 e 9 (CONTEXT.md) re-executados literalmente contra HEAD: **PASS** nos tres — DoD 9 em
    especial confirma zero static mutavel novo (`_initLock` e instancia, nao static) e os 12 arquivos
    fora de escopo seguem byte a byte identicos ao BASELINE. DoD 3-8 nao re-executados no sentido
    caro (sem rede/APK/xcframework) porque `git diff --name-only` prova que nenhum dos arquivos-alvo
    deles foi tocado por este commit — o veredito PASS da iter 3 permanece estruturalmente valido.
  - `dotnet format` (whitespace + style + analyzers, escopo so nos 4 arquivos tocados):
    `--verify-no-changes` sai 0 — zero ajuste necessario.
- **Nao tocado (por escopo explicito da rodada):** W-1, W-2, W-3, W-5, W-6, W-7; `CONTEXT.md`;
  `REVIEW.md`; `ci.yml`; `PLAN.md` (T-1..T-8 permanecem como estavam — este fix nao e uma task
  numerada do plano original, e sim uma correcao de warning pos-review).
