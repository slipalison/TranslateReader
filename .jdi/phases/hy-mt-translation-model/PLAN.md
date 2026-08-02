# Phase 20: Modelo de traducao hy-mt1.5-1.8b — Plan  (slug: hy-mt-translation-model)

## Goal
hy-mt1.5-1.8b vira modelo selecionavel de verdade: corrige o bug onde `TranslationManager` ignora
`TranslationModelName` e sempre baixa Gemma, fecha o bug de `IModelAccess` que pega "qualquer
`*.gguf`" do diretorio, e documenta a licenca Tencent HY (EU/UK/Coreia do Sul fora do grant).

## Pre-flight OBRIGATORIO — a armadilha do DoD 6
O DoD 6 exige que `git diff --name-only $(git merge-base origin/main HEAD) -- src/TranslateReader.Core/`
seja EXATAMENTE `Access/ModelAccess.cs,Business/Managers/TranslationManager.cs,Contracts/Access/IModelAccess.cs,`.
Se a phase rodar sobre uma branch que ja carrega trabalho anterior nao mergeado (ex.
`jdi/div-paragraph-reading` traz `Utilities/HtmlUtility.cs`), o gate reprova por motivo alheio ao codigo
desta phase. **Antes de T-1**, rodar:

```bash
BASE=$(git merge-base origin/main HEAD); git diff --name-only "$BASE" -- src/TranslateReader.Core/ src/TranslateReader/
```

Saida vazia -> seguir. Saida nao-vazia -> **task `blocked`** com a lista, e a phase precisa de branch
partindo de `origin/main`. **PROIBIDO** "consertar" editando o `Verify:` do CONTEXT.

## Locked decisions
- `D-...-2` `SizeBytes` = `1_133_080_512` (medido); `1_213_000_000` do card e ERRADO; nenhum campo
  `PromptTemplate` (o `StatelessExecutor { ApplyTemplate = true }` ja le o template do GGUF).
- `D-...-3` licenca via `THIRD-PARTY-NOTICES.md` novo + label alcancavel no `SettingsOverlay`.
  Geo-gating REJEITADO (sem infra, YAGNI).
- `D-...-4` `ModelRegistry` estatico (`IReadOnlyDictionary<string, ModelInfo>`, `StringComparer.Ordinal`)
  + `ResolveModel` com fallback pra gemma; `ISettingsAccess` no construtor do Manager (Manager ->
  ResourceAccess e permitido; Manager -> Manager sincrono e PROIBIDO); `IsModelAvailable`/`GetModelPath`
  passam a exigir `string fileName` com `File.Exists` exato.
- `D-...-5`/`D-...-6` `TranslationEngine.cs`, `PromptUtility.cs`, `ITranslationManager.cs` e
  `ModelInfo.cs` ficam INTOCADOS. `Temperature=0.1f` uniforme. Nenhum campo de sampling.
- `D-1`/`D-2`/`D-6` The Method, boundary `4285f25`, 90% em codigo alterado. `csharp.md` §6 (bugfix
  comeca VERMELHO) manda na ordem das tasks.

## Aritmetica dos pisos — MARGEM ZERO nos dois filtros
| Gate | Existentes | Novos | Total | Piso |
|---|---|---|---|---|
| DoD 2 (`~DownloadModelIfNeededAsync\|~InitializeEngineIfNeededAsync`) | 4 | 3 (T-5) | 7 | **>= 7** |
| DoD 3 (`~ModelAccessTests.IsModelAvailable\|~ModelAccessTests.GetModelPath`) | 6 | 2 (T-1) | 8 | **>= 8** |
| DoD 5 (suite inteira) | B (merge-base) | 5 | B+5 | **>= B+5** |

**Consequencia:** deletar, renomear ou pular QUALQUER teste existente reprova o gate. So e permitido
SOMAR. DoD 5 ainda compara nome a nome contra a base — renomear = ausencia = reprova.
**Decisao nova nesta phase: NENHUMA.** Nao criar `.jdi/decisions/*`, nao editar `.jdi/DECISIONS.md`
nem `.jdi/todos.md` (views geradas). Scope creep -> arquivo NOVO em `.jdi/todos/` + `npx -y jdi-cli render`.

## Tasks

### Wave 1 (parallel-eligible — arquivos disjuntos)

#### T-1: Teste VERMELHO do bug "qualquer `*.gguf`" em `ModelAccess`
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `test/TranslateReader.Tests/ModelAccessTests.cs`
- **Acceptance:**
  - Pre-flight do topo deste PLAN executado e VAZIO (senao `blocked`).
  - 2 testes novos, nomes EXATOS (o `Verify:` do DoD 3 faz `grep -q`):
    `IsModelAvailable_ReturnsFalseWhenADifferentGgufFileExists` e
    `GetModelPath_ThrowsWhenOnlyADifferentGgufFileExists`. Fixture: criar `_modelsDir` contendo
    APENAS `other-model.gguf` e pedir o `ModelFileName` (`test-model.gguf`, `const` ja no arquivo).
  - Escritos contra a API PARAMETRICA de HOJE (`IsModelAvailable()` / `GetModelPath()`, sem argumento)
    — e exatamente isso que os faz falhar agora: hoje devolvem `true`/o path do arquivo ERRADO.
  - Transcript VERMELHO no SUMMARY antes de T-2:
    `DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~ModelAccessTests"` com `Failed: 2` e os 2 nomes visiveis.
  - Os 6 testes existentes de `IsModelAvailable`/`GetModelPath` intocados. Zero rede, zero SQLite.
- **Dependencies:** none
- **Test:** os proprios (xUnit, `Path.GetTempPath()` isolado por `Guid`, ja e o padrao do arquivo)
- **Commit:** `test(hy-mt-translation-model): cover model lookup with a foreign gguf on disk`
- **Status:** completed
- **DoD:** item 3 (metade vermelha)

#### T-2: `THIRD-PARTY-NOTICES.md` na raiz
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `THIRD-PARTY-NOTICES.md` (novo)
- **Acceptance:**
  - Contem, em ingles, todas as strings que o `Verify:` do DoD 4 procura (case-insensitive):
    `Tencent HY Community License`, `European Union`, `United Kingdom`, `South Korea`,
    `Powered by Tencent HY`, `not affiliated`.
  - Clausulas REAIS, nao parafrase inventada: exclusao territorial, atribuicao obrigatoria,
    declaracao de nao-afiliacao e obrigacao de acompanhar distribuicoes. Se precisar da redacao
    literal, 1 WebFetch de `https://huggingface.co/tencent/HY-MT1.5-1.8B-GGUF/blob/main/License.txt`
    (dentro do orcamento de 2 lookups). Nunca inventar texto legal.
  - Identifica o artefato: repo `tencent/HY-MT1.5-1.8B-GGUF`, arquivo `HY-MT1.5-1.8B-Q4_K_M.gguf`,
    link do License.txt. O modelo NAO e redistribuido pelo repo — e baixado em runtime pelo usuario.
  - **Escopo:** SO a entrada do Tencent HY. Nao inventariar Gemma/Qwen/Phi nem dependencias NuGet
    (scope creep; fica como item de PR review).
- **Dependencies:** none
- **Test:** N/A (artefato de documentacao; coberto pelo `Verify:` do DoD 4)
- **Commit:** `docs(hy-mt-translation-model): add third-party notices for the Tencent HY model`
- **Status:** completed
- **DoD:** item 4 (parte do arquivo)

#### T-3: 4o botao de modelo + atribuicao no `SettingsOverlay`
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader/Pages/Controls/SettingsOverlay.xaml`,
  `src/TranslateReader/Pages/Controls/SettingsOverlay.xaml.cs`
- **Acceptance:**
  - XAML: `<Button x:Name="HyMtModelButton" Text="HY-MT 1.8B" Clicked="OnHyMtClicked" ...>` clonando
    atributos dos 3 botoes existentes (`BorderColor="Transparent"`, `BorderWidth="2"`,
    `CornerRadius="20"`, `Padding="12,6"`, `FontSize="12"`, mesmos `AppThemeBinding`).
  - **Alcancavel em tela estreita:** envolver o `HorizontalStackLayout` dos botoes (linha 192) em
    `<ScrollView Orientation="Horizontal" HorizontalScrollBarVisibility="Never">`. Os 3 botoes
    existentes ficam byte-identicos dentro dele. Sem isso o 4o botao sai da viewport no telefone e o
    DoD 4 ("alcancavel pelo usuario") e falso mesmo com o `grep` verde.
  - `<Label>` de atribuicao logo abaixo do grupo de botoes, contendo literalmente
    `Powered by Tencent HY` + nao-afiliacao + ponteiro `THIRD-PARTY-NOTICES.md`
    (ex.: `Text="Powered by Tencent HY. Nao afiliado a Tencent. Detalhes em THIRD-PARTY-NOTICES.md."`),
    `FontSize="12"` e `LineBreakMode="WordWrap"` no estilo do `ModelStatusLabel`.
  - Code-behind: `OnHyMtClicked` copiando `OnQwenClicked` com `"hy-mt1.5-1.8b"`, e
    `UpdateModelButtonBorders` ganha a linha do `HyMtModelButton` com a MESMA regra dos outros 3.
  - Nenhum outro arquivo de `src/TranslateReader/` muda — em especial `MauiProgram.cs` NAO muda
    (o `AddTransient<ITranslationManager, TranslationManager>()` resolve o novo parametro de
    construtor por DI, e `ISettingsAccess` ja esta registrado; `ModelAccess` nao muda de construtor).
  - `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0`
    com `0 Error(s)`. Os 2 testes vermelhos de T-1 sao esperados nesta janela — nao marcam esta task
    como falha (o gate desta task e o BUILD, nao a suite).
- **Dependencies:** none
- **Test:** build do TFM Windows (`src/TranslateReader/` nao tem projeto de teste — D-2026-07-30-regression-suite-2)
- **Commit:** `feat(hy-mt-translation-model): add the HY-MT model button to the settings overlay`
- **Status:** completed
- **DoD:** item 4 (parte da UI)

### Wave 2

#### T-4: `IModelAccess`/`ModelAccess` filename-aware (fecha o bug de T-1)
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Contracts/Access/IModelAccess.cs`,
  `src/TranslateReader.Core/Access/ModelAccess.cs`,
  `src/TranslateReader.Core/Business/Managers/TranslationManager.cs`,
  `test/TranslateReader.Tests/ModelAccessTests.cs`,
  `test/TranslateReader.Tests/TranslationManagerTests.cs`
- **Acceptance:**
  - Contrato com o texto EXATO que o `Verify:` do DoD 3 procura: `bool IsModelAvailable(string fileName);`
    e `string GetModelPath(string fileName);` (nome do parametro literalmente `fileName`). Cada um dos
    2 membros alterados ganha `<summary>` de 1 linha (`csharp.md` §7); os outros 2 membros ficam
    intocados (D-2, nao refatorar legado por estilo). `DownloadModelAsync`/`DeleteModelAsync` mantem
    assinatura.
  - `ModelAccess`: `File.Exists(Path.Combine(_modelsDirectory, fileName))` — checagem EXATA;
    `Directory.EnumerateFiles(..., "*.gguf")` sai das duas operacoes. `GetModelPath` continua
    lancando `FileNotFoundException` (fail fast, `csharp.md` §1) sem expor caminho de perfil do
    usuario na mensagem (§4). Nenhuma validacao nova de path (o argumento vem de constante do
    registry, nao de input externo — YAGNI).
  - Call sites do Manager adaptados MINIMAMENTE nesta task: `IsModelAvailable(DefaultModel.FileName)`
    e `GetModelPath(DefaultModel.FileName)`. O registry e a leitura de settings sao T-5 — nao
    antecipar.
  - `ModelAccessTests`: os 6 existentes passam a chamar com `ModelFileName` e, onde criam o arquivo,
    usam esse mesmo nome (`IsModelAvailable_ReturnsTrueWhenGgufExists` grava `ModelFileName`, nao
    `model.gguf`). Os 2 de T-1 ficam VERDES apenas trocando `()` por `(ModelFileName)`. **Nenhum nome
    de teste muda.**
  - `TranslationManagerTests`: churn mecanico `IsModelAvailable(Arg.Any<string>())` /
    `GetModelPath(Arg.Any<string>())` nos 3 testes que os stubam. Nenhum nome muda, nenhum teste novo.
  - `--filter "FullyQualifiedName~ModelAccessTests.IsModelAvailable|FullyQualifiedName~ModelAccessTests.GetModelPath"`
    -> `Passed!` com `Passed >= 8`, `Failed: 0`. Suite inteira volta ao VERDE.
  - Cobertura dos 2 metodos alterados >= 90% linha+branch (`--collect:"XPlat Code Coverage"`),
    reportada no SUMMARY (D-6).
- **Dependencies:** T-1
- **Test:** os 8 do filtro do DoD 3 + suite completa como regressao
- **Commit:** `fix(hy-mt-translation-model): make model availability checks filename-aware`
- **Status:** completed
- **DoD:** itens 3 e 6

### Wave 3

#### T-5: `ModelRegistry` + `ResolveModel` + selecao vinda da settings persistida
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Business/Managers/TranslationManager.cs`,
  `test/TranslateReader.Tests/TranslationManagerTests.cs`
- **Acceptance:**
  - `DefaultModel` vira `GemmaModel` (valores preservados) e nasce `HyMtModel`. Cada argumento nomeado
    em UMA linha, texto EXATO (o `Verify:` do DoD 1 e `grep` de linha):
    `Name: "hy-mt1.5-1.8b"` · `FileName: "HY-MT1.5-1.8B-Q4_K_M.gguf"` ·
    `DownloadUrl: "https://huggingface.co/tencent/HY-MT1.5-1.8B-GGUF/resolve/main/HY-MT1.5-1.8B-Q4_K_M.gguf"` ·
    `SizeBytes: 1_133_080_512`; e `Name: "gemma-2-2b"` · `SizeBytes: 1_629_413_888`.
    `1_213_000_000` NAO pode aparecer no arquivo (numero errado do card).
  - `private static readonly IReadOnlyDictionary<string, ModelInfo> ModelRegistry =
    new Dictionary<string, ModelInfo>(StringComparer.Ordinal) { ... }` com as 2 entradas chaveadas por
    `Name` (o texto do tipo declarado casa `IReadOnlyDictionary<string, ?ModelInfo>`). `Ordinal` e
    obrigatorio (`csharp.md` §2.1). `FrozenDictionary` e aceitavel mas NAO exigido (2 entradas,
    caminho frio) — nao gastar attempt nisso.
  - `private static ModelInfo ResolveModel(string modelName)` com `TryGetValue` e fallback EXPLICITO
    pra `GemmaModel`, com 1 linha de comentario WHY (Qwen/Phi ainda sem URL real — D-...-4). Sem
    `if/else` de string solto, sem `null`.
  - `ISettingsAccess settingsAccess` entra como ULTIMO parametro do construtor primario;
    `DownloadModelIfNeededAsync` e `InitializeEngineIfNeededAsync` leem
    `(await settingsAccess.FetchSettingsAsync()).TranslationModelName` e resolvem o modelo antes de
    decidir. `InitializeEngineIfNeededAsync` mantem o short-circuit por `translationEngine.IsReady`
    (guard clause primeiro, sem I/O desnecessario). `ITranslationManager` NAO muda.
  - **8 params de construtor:** ultrapassa o teto de 7 do `csharp.md` §7 / Sonar S107. E consequencia
    FORCADA de D-...-4 + DoD 6 (parameter object exigiria arquivo novo no Core ou mudanca em
    `MauiProgram.cs`, ambos proibidos pelo `Verify:` do item 6). Aceitar, deixar 1 linha WHY no codigo
    e registrar no SUMMARY como exposicao conhecida pro PR review. **Nao refatorar pra contornar.**
  - 3 testes novos, nomes EXATOS: `DownloadModelIfNeededAsync_WhenSettingsSelectHyMt_DownloadsTheHyMtUrl`,
    `DownloadModelIfNeededAsync_WhenSettingsSelectAnUnregisteredModel_FallsBackToGemma`,
    `InitializeEngineIfNeededAsync_WhenSettingsSelectHyMt_UsesTheHyMtFileName`.
  - Os 3 asseveram a URL/filename LITERAL (nunca `Arg.Any` na assercao) e o primeiro tambem
    `DidNotReceive()` na URL do gemma — sem isso um mutante que sempre baixa gemma passa verde.
  - Fixture: campo `ISettingsAccess` mockado no construtor da classe de teste com default
    `FetchSettingsAsync()` -> `new ReadingSettings { TranslationModelName = "gemma-2-2b" }`, para os 4
    testes existentes continuarem verdes SEM renomear. Zero I/O, NSubstitute so sobre `Contracts/`.
  - Antes de implementar `ResolveModel`, rodar os 3 testes novos com o Manager ainda em `GemmaModel`
    e colar o transcript VERMELHO (`Failed: 3`) no SUMMARY (`csharp.md` §6) — mesmo commit.
  - `--filter "FullyQualifiedName~DownloadModelIfNeededAsync|FullyQualifiedName~InitializeEngineIfNeededAsync"`
    -> `Failed: 0`, `Passed >= 7`. Cobertura >= 90% no codigo alterado, reportada no SUMMARY.
- **Dependencies:** T-4
- **Test:** os 7 do filtro do DoD 2 + suite completa
- **Commit:** `feat(hy-mt-translation-model): resolve the translation model from persisted settings`
- **Status:** completed
- **DoD:** itens 1, 2, 5 e 6

### Wave 4

#### T-6: Corrida final dos 6 `Verify:` + escopo de diff
- **Specialist:** jdi-doer-translatereader
- **Files modified:** nenhum arquivo versionado (so `TestResults/*.log`, ja no `.gitignore:18`)
- **Acceptance:**
  - Os 6 `Verify:` do CONTEXT rodados um a um, VERBATIM, via Bash (Git Bash no Windows), saida colada
    no SUMMARY. Nada de adaptar comando: `DOTNET_CLI_UI_LANGUAGE=en` e os pisos por `awk` fazem parte
    do gate.
  - DoD 5: `Failed: 0`, `Total >= B+5`, `Skipped <= S`, `Passed+Skipped+Failed == Total` e
    `comm -23 base head` VAZIO (nenhum nome de teste de `origin/main` sumiu).
  - DoD 6: diff do Core exatamente `ModelAccess.cs,TranslationManager.cs,IModelAccess.cs`; no app so
    `SettingsOverlay.xaml`/`.xaml.cs`; `TranslationEngine.cs`/`PromptUtility.cs`/`ITranslationManager.cs`/
    `ModelInfo.cs` com diff vazio.
  - Qualquer piso abaixo do exigido -> task `blocked` com o numero real. **NUNCA** deletar/pular teste
    nem afrouxar `Verify:` para fechar conta.
  - `.gitignore` e `.jdi/phases/div-paragraph-reading/REVIEW.md` tem alteracao local do usuario nao
    commitada — ficam FORA de todo commit desta phase.
- **Dependencies:** T-3, T-5
- **Test:** suite C# completa + build do TFM Windows
- **Commit:** nenhum (sem diff versionado)
- **Status:** completed
- **DoD:** itens 1 a 6 (corrida de fechamento)

## Riscos nomeados
- **R1 — branch errada mata o DoD 6.** Diff medido contra `merge-base origin/main HEAD`: trabalho
  anterior nao mergeado no Core aparece no diff e reprova. Por isso o pre-flight vem ANTES de T-1.
- **R2 — margem zero nos filtros.** 7/7 e 8/8 exatos. Um teste renomeado ou removido reprova 2 gates.
- **R3 — 8 params de construtor (S107 / `csharp.md` §7).** Forcado pelas decisoes locked; nao ha forma
  legal de contornar sem violar o DoD 6. Vai pro SUMMARY como exposicao, nao vira refactor.
- **R4 — grep de linha e literal.** `Name: "hy-mt1.5-1.8b"` quebrado em 2 linhas pelo `dotnet format`
  reprova o DoD 1 com codigo correto. Conferir com `grep` DEPOIS do `dotnet format`, nao antes.
- **R5 — 4o botao fora da viewport.** `grep` de `x:Name` verde nao prova alcancabilidade; o
  `ScrollView` horizontal de T-3 e o que honra "alcancavel pelo usuario" do DoD 4.
- **R6 — coexistencia de 2 GGUF em disco.** Aceito por D-...-4 (delete continua apagando tudo, sem
  limpeza automatica ao trocar). Nao inventar limpeza nesta phase — e todo de produto.

## Learnings aplicados
- Gate textual sobre codigo nao prova comportamento -> DoD 1 e o unico so-grep (campo informativo);
  2, 3 e 5 rodam suite e leem `Passed!` + piso numerico, nunca exit code.
- Teste so discrimina se a fixture consegue DESSINCRONIZAR -> as 2 fixtures novas de T-1 poem um gguf
  de OUTRO modelo no diretorio; os 6 testes antigos passavam com o seletor ingenuo.
- Piso por CONTAGEM aceita stub e delecao compensada -> DoD 5 compara NOME A NOME contra a base.
- `Verify:` nunca ancora em ref local -> tudo por `git merge-base origin/main HEAD`.
- Prova por mutacao so vale se aplicada -> assercoes de T-5 sao literais (URL/filename exatos) +
  `DidNotReceive` no gemma, entao "sempre gemma" morre vermelho.

## Execution
- Total tasks: 6 · Waves: 4 · Speedup paralelo: ~1,7x (T-1 ∥ T-2 ∥ T-3)
- DoD 6/6: 1:T-5 · 2:T-5 · 3:T-1+T-4 · 4:T-2+T-3 · 5:T-5+T-6 · 6:T-4+T-5+T-6
- Testes novos: 5 exatos (2 em `ModelAccessTests` por T-1, 3 em `TranslationManagerTests` por T-5)

## Files modified (all tasks)
- `THIRD-PARTY-NOTICES.md` (novo)
- `src/TranslateReader.Core/Contracts/Access/IModelAccess.cs`
- `src/TranslateReader.Core/Access/ModelAccess.cs`
- `src/TranslateReader.Core/Business/Managers/TranslationManager.cs`
- `src/TranslateReader/Pages/Controls/SettingsOverlay.xaml`, `SettingsOverlay.xaml.cs`
- `test/TranslateReader.Tests/ModelAccessTests.cs`, `test/TranslateReader.Tests/TranslationManagerTests.cs`

## Test requirements
- C#: `DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release`
  — `Failed: 0`, `Total >= B+5`; filtros do DoD 2 (>= 7) e DoD 3 (>= 8)
- Cobertura: `--collect:"XPlat Code Coverage"` >= 90% linha+branch no codigo alterado (D-6)
- Build: `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0`
  com `0 Error(s)`
- `dotnet format` antes de cada commit; zero I/O de rede/SQLite em teste novo; NSubstitute so sobre `Contracts/`
