# Phase 20: Modelo de traducao hy-mt1.5-1.8b — Context (slug: hy-mt-translation-model)

Gerado em modo `auto` via `/jdi-issue` (`mode=auto dod=auto_only`), brief = card colado pelo usuario
2026-08-01 propondo `tencent/HY-MT1.5-1.8B-GGUF` como modelo adicional de download, ja validado pelo
orquestrador por WebFetch do repo/model card reais ANTES deste discuss (sem interacao humana). Duas
correcoes de numero do card (SizeBytes, chat-template) e dois achados criticos fora do card (licenca,
registry quebrado) foram confirmados por leitura direta do codigo nesta sessao — ver `## Locked
decisions`.

## Goal
hy-mt1.5-1.8b vira modelo de traducao selecionavel de verdade — corrige o bug pre-existente onde
`TranslationManager` ignora `TranslationModelName` e sempre baixa Gemma independente do botao clicado
na Settings, com `SizeBytes` real medido e licenca (territorio EU/UK/Coreia do Sul excluido)
documentada.

## Locked decisions
- **D-...-1** (ja registrada, criacao da phase): repo/arquivo do HY-MT1.5-1.8B confirmados reais;
  achados iniciais de licenca e registry quebrado ja apontados.
- **D-...-2** (validacao tecnica travada): `SizeBytes` correto = **1_133_080_512** (medido via
  `content-length` real, nao os 1_213_000_000 estimados pelo card a partir da UI do HF). Portugues
  suportado (36 linguas do model card real), mesma ressalva de fidelidade BR ja aplicada aos 3
  modelos existentes, nao e regressao nova. A preocupacao do card sobre chat template NAO se aplica
  — `TranslationEngine.CreateExecutor` ja usa `StatelessExecutor { ApplyTemplate = true, ... }`, que
  le o template dos metadados do proprio GGUF; nenhum campo `PromptTemplate` e adicionado (YAGNI).
  Sem system_prompt default e sampling recomendada (`top_k=20, top_p=0.6, repetition_penalty=1.05,
  temperature=0.7`) CONFIRMADOS reais pelo model card — o que fazer com eles esta em D-...-5/D-...-6.
- **D-...-3** (licenca, achado critico fora do card): Tencent HY Community License Agreement exclui
  EU/Reino Unido/Coreia do Sul do grant, exige atribuicao "Powered by Tencent HY", declaracao de
  nao-afiliacao e arquivo de aviso acompanhando distribuicoes. Forma minima LOCKED: (1)
  `THIRD-PARTY-NOTICES.md` novo na raiz do repo com as clausulas reais; (2) label curto e alcancavel
  em `SettingsOverlay` perto do botao HY-MT com a atribuicao + ponteiro pro arquivo. Geo-gating REAL
  (bloquear download por territorio) REJEITADO nesta phase — app cliente sem nenhuma infra de
  geolocalizacao hoje, construir isso so pra 1 modelo e desproporcional (YAGNI). Risco legal residual
  fica visivel em `## Deferred to PR review`, nao decidido em silencio.
- **D-...-4** (registry quebrado, achado critico fora do card, forma da correcao): escopo = opcao
  (b) — conserta o MECANISMO pros 2 modelos com `ModelInfo` real (gemma-2-2b + hy-mt1.5-1.8b); Qwen/
  Phi nao ganham URL real aqui (nao pedido pelo card). `TranslationManager` ganha `ModelRegistry`
  estatico (`IReadOnlyDictionary<string, ModelInfo>`, `StringComparer.Ordinal`) + `ResolveModel(string
  modelName)` com fallback pra gemma em nome desconhecido (cobre Qwen/Phi por decisao explicita, nao
  acidente). `ISettingsAccess` entra no construtor do Manager (Manager -> ResourceAccess permitido por
  The Method; Manager -> Manager sincrono e PROIBIDO, entao nao pode ser `ISettingsManager`) —
  `DownloadModelIfNeededAsync`/`InitializeEngineIfNeededAsync` leem `TranslationModelName` da settings
  persistida antes de resolver o modelo. ACHADO ADICIONAL necessario pro registry ser correto (nao so
  compilar): `IModelAccess.IsModelAvailable()`/`GetModelPath()` (sem parametro) checavam "qualquer
  `*.gguf` no diretorio" — com 2 modelos reais, trocar de gemma pra hy-mt sem apagar o anterior
  deixaria a selecao dependente da ordem de `Directory.EnumerateFiles`. As 2 assinaturas passam a
  exigir `string fileName`, checagem exata (`File.Exists`). `DownloadModelAsync`/`DeleteModelAsync`
  ficam com a mesma assinatura de hoje (delete continua apagando tudo — unico botao, sem selecao por
  modelo, fora de escopo mudar). Efeito aceito: os 2 GGUF podem coexistir em disco sem limpeza
  automatica — todo de produto.
- **D-...-5** (sampling, fora de escopo — opcao b): `TranslationTemperature = 0.1f` fica UNIFORME pra
  todos os modelos, incluindo hy-mt. Nenhum campo `Sampling`/`SamplingProfile` em `ModelInfo`, nenhuma
  mudanca em `ITranslationEngine`/`TranslationEngine.CreateInferenceParams`. Risco de qualidade
  conhecido (fornecedor recomenda `temperature=0.7`) registrado em `## Deferred to PR review`, nao
  descartado em silencio.
- **D-...-6** (forma do system prompt, fora de escopo): preocupacao do card e MODEL-AGNOSTIC —
  `PromptUtility.BuildTranslationMessages` sempre constroi seu proprio system message explicito pra
  todos os modelos, nunca depende do default do GGUF, entao a ausencia de `system_prompt` default do
  HY-MT nao quebra nada estruturalmente hoje (so possivel ajuste fino de qualidade). `PromptUtility`
  fica intocado nesta phase.

## Canonical refs
- `.jdi/decisions/D-2026-08-01-hy-mt-translation-model-1..6.md`
- `https://huggingface.co/tencent/HY-MT1.5-1.8B-GGUF` (repo do modelo, WebFetch do model card real)
- `https://huggingface.co/tencent/HY-MT1.5-1.8B-GGUF/blob/main/License.txt` (Tencent HY Community
  License Agreement, WebFetch nesta sessao)
- `src/TranslateReader.Core/Business/Managers/TranslationManager.cs` (`DefaultModel` -> `GemmaModel` +
  `HyMtModel` + `ModelRegistry` + `ResolveModel`)
- `src/TranslateReader.Core/Contracts/Access/IModelAccess.cs`,
  `src/TranslateReader.Core/Access/ModelAccess.cs` (`IsModelAvailable`/`GetModelPath` viram
  filename-aware)
- `src/TranslateReader.Core/Contracts/Access/ISettingsAccess.cs`,
  `src/TranslateReader.Core/Models/ReadingSettings.cs` (`TranslationModelName`, ja persistido)
- `src/TranslateReader/Pages/Controls/SettingsOverlay.xaml(.cs)` (3 botoes de modelo existentes,
  ganha o 4o + label de atribuicao)
- `src/TranslateReader.Core/Business/Engines/TranslationEngine.cs` (`CreateExecutor`,
  `ApplyTemplate = true` — motivo de D-...-2 rejeitar `PromptTemplate`)
- `.claude/rules/csharp.md` §1 (fail fast), §2.1 (Ordinal em chave de dicionario), §4 (seguranca,
  prioridade 1), §6 (bugfix comeca vermelho, 90% em codigo alterado, sem I/O em teste novo)
- `test/TranslateReader.Tests/{TranslationManagerTests,ModelAccessTests}.cs` (padrao de teste
  existente reusado)

## Out of scope
- URLs GGUF reais pra Qwen 2.5 3B / Phi 3.5 — D-...-4, nao pedido pelo card, vira todo.
- Sampling por modelo (`TopP`/`TopK`/`RepeatPenalty`) — D-...-5, todo.
- Forma do prompt (system vs user turn) — D-...-6, model-agnostic, todo.
- Templates de prompt de intervencao terminologica/contexto (so documentados em chines, untested
  EN->PT) — o proprio card ja marcou como especulativo, todo.
- Variantes quantizadas menores (2-bit/1.25-bit) — "vale medir, nao fazer agora" (card), todo.
- Geo-gating real por territorio (EU/UK/Coreia do Sul) — D-...-3, YAGNI, sem infra hoje.
- Limpeza automatica de modelo anterior ao trocar de selecao — D-...-4, decisao de produto, todo.

## Definition of Done

> `dod=auto_only`: todo item carrega `Verify:` executavel, no padrao ja endurecido desta base de
> codigo (`translated-epub-images`, `div-paragraph-reading`): `dotnet test --filter` real,
> `DOTNET_CLI_UI_LANGUAGE=en` (sumario local sai em pt-BR), `grep -q "Passed!"` + piso numerico via
> `awk` (nunca so o exit code do `dotnet test`, que sai 0 mesmo com filtro casando zero teste). Piso
> da suite inteira DERIVADO de `git merge-base origin/main HEAD` no proprio comando. Logs em
> `TestResults/` (`.gitignore:18`).

### Auto-verifiable
- [ ] Registry tem as DUAS entradas reais com os valores validados nesta sessao (nao os do card
      original): `hy-mt1.5-1.8b` com `FileName`/`DownloadUrl` exatos e `SizeBytes: 1_133_080_512`;
      `gemma-2-2b` preservado (`SizeBytes: 1_629_413_888`); o numero ERRADO do card
      (`1_213_000_000`) nao aparece em lugar nenhum; a estrutura e um dicionario tipado, nao um
      `if/else` de string solto
      **Verify:** `F=src/TranslateReader.Core/Business/Managers/TranslationManager.cs; grep -q 'Name: "hy-mt1.5-1.8b"' "$F" && grep -q 'FileName: "HY-MT1.5-1.8B-Q4_K_M.gguf"' "$F" && grep -q 'DownloadUrl: "https://huggingface.co/tencent/HY-MT1.5-1.8B-GGUF/resolve/main/HY-MT1.5-1.8B-Q4_K_M.gguf"' "$F" && grep -q 'SizeBytes: 1_133_080_512' "$F" && grep -q 'Name: "gemma-2-2b"' "$F" && grep -q 'SizeBytes: 1_629_413_888' "$F" && test "$(grep -c '1_213_000_000' "$F")" -eq 0 && grep -qE 'IReadOnlyDictionary<string, ?ModelInfo>' "$F"`
      **Source:** CONTEXT (D-...-2, D-...-4)

- [ ] Resolucao por settings funciona de verdade: hy-mt selecionado baixa/inicializa com a URL/
      arquivo do hy-mt; gemma continua default; nome desconhecido (Qwen/Phi/legado) cai pro gemma
      por fallback explicito, nao por acidente — 7 testes de `TranslationManagerTests.cs` passam
      (4 de `DownloadModelIfNeededAsync`, 3 de `InitializeEngineIfNeededAsync`, incluindo os 2
      existentes preservados e os 3 novos desta phase)
      **Verify:** `T=test/TranslateReader.Tests/TranslationManagerTests.cs; grep -q "DownloadModelIfNeededAsync_WhenSettingsSelectHyMt_DownloadsTheHyMtUrl" "$T" && grep -q "DownloadModelIfNeededAsync_WhenSettingsSelectAnUnregisteredModel_FallsBackToGemma" "$T" && grep -q "InitializeEngineIfNeededAsync_WhenSettingsSelectHyMt_UsesTheHyMtFileName" "$T" && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~DownloadModelIfNeededAsync|FullyQualifiedName~InitializeEngineIfNeededAsync" > TestResults/dod2.log 2>&1 && grep -q "Passed!" TestResults/dod2.log && awk -v n=7 '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (k&&f+0==0&&p+0>=n)?0:1}' TestResults/dod2.log`
      **Source:** CONTEXT (D-...-4)

- [ ] `IModelAccess.IsModelAvailable`/`GetModelPath` viram filename-aware (fecham o bug de "qualquer
      `*.gguf` no diretorio" que faria a troca de modelo silenciosamente pegar o arquivo errado
      quando 2 modelos coexistem em disco) — 8 testes de `ModelAccessTests.cs` passam, incluindo os
      2 novos que provam o fix por diferenca (existe um `.gguf` de OUTRO modelo no diretorio ->
      `IsModelAvailable`/`GetModelPath` devem falhar pro nome pedido, no mais devolver `true`/o path
      so por existir QUALQUER gguf)
      **Verify:** `I=src/TranslateReader.Core/Contracts/Access/IModelAccess.cs; T=test/TranslateReader.Tests/ModelAccessTests.cs; grep -qE 'bool IsModelAvailable\(string fileName\)' "$I" && grep -qE 'string GetModelPath\(string fileName\)' "$I" && grep -q "IsModelAvailable_ReturnsFalseWhenADifferentGgufFileExists" "$T" && grep -q "GetModelPath_ThrowsWhenOnlyADifferentGgufFileExists" "$T" && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~ModelAccessTests.IsModelAvailable|FullyQualifiedName~ModelAccessTests.GetModelPath" > TestResults/dod3.log 2>&1 && grep -q "Passed!" TestResults/dod3.log && awk -v n=8 '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (k&&f+0==0&&p+0>=n)?0:1}' TestResults/dod3.log`
      **Source:** CONTEXT (D-...-4)

- [ ] Licenca visivel e alcancavel: `THIRD-PARTY-NOTICES.md` existe na raiz com as clausulas reais
      (nome da licenca, exclusao EU/Reino Unido/Coreia do Sul, atribuicao "Powered by Tencent HY",
      declaracao de nao-afiliacao); `SettingsOverlay` mostra o botao do 4o modelo e a mesma
      atribuicao de forma alcancavel pelo usuario; o app inteiro continua compilando
      **Verify:** `test -f THIRD-PARTY-NOTICES.md && grep -qi "Tencent HY Community License" THIRD-PARTY-NOTICES.md && grep -qi "European Union" THIRD-PARTY-NOTICES.md && grep -qi "United Kingdom" THIRD-PARTY-NOTICES.md && grep -qi "South Korea" THIRD-PARTY-NOTICES.md && grep -qi "Powered by Tencent HY" THIRD-PARTY-NOTICES.md && grep -qi "not affiliated" THIRD-PARTY-NOTICES.md && X=src/TranslateReader/Pages/Controls/SettingsOverlay.xaml && grep -q 'x:Name="HyMtModelButton"' "$X" && grep -qi "Powered by Tencent HY" "$X" && C=src/TranslateReader/Pages/Controls/SettingsOverlay.xaml.cs && grep -q "OnHyMtClicked" "$C" && grep -q '"hy-mt1.5-1.8b"' "$C" && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0 > TestResults/dod4.log 2>&1 && grep -qE "^ *0 Error\(s\)" TestResults/dod4.log`
      **Source:** CONTEXT (D-...-3)

- [ ] Suite C# inteira sem regressao: `Failed: 0`, piso `Total >= B+5` (`B` = `[Fact]`+`[InlineData]`
      de `origin/main` no proprio comando, `+5` = os 5 testes novos desta phase — 3 em
      `TranslationManagerTests`, 2 em `ModelAccessTests`), `Skipped <= S` de `origin/main`, soma
      coerente (`Passed+Skipped+Failed == Total`), nenhum nome de teste publico de `origin/main`
      ausente no HEAD
      **Verify:** `mkdir -p TestResults && BASE=$(git merge-base origin/main HEAD) && B=$(( $(git grep -cE '^[[:space:]]*\[Fact' "$BASE" -- 'test/TranslateReader.Tests/*.cs' | awk -F: '{s+=$NF} END{print s+0}') + $(git grep -cE '^[[:space:]]*\[InlineData' "$BASE" -- 'test/TranslateReader.Tests/*.cs' | awk -F: '{s+=$NF} END{print s+0}') )) && S=$(git grep -cE 'Skip[[:space:]]*=' "$BASE" -- 'test/TranslateReader.Tests/*.cs' | awk -F: '{s+=$NF} END{print s+0}') && test "$B" -gt 0 && git grep -hoE 'public (async Task|void) [A-Za-z0-9_]+\(' "$BASE" -- 'test/TranslateReader.Tests/*.cs' | sed -E 's/^public (async Task|void) //; s/\($//' | sort -u > TestResults/dod5-base.txt && git grep -hoE 'public (async Task|void) [A-Za-z0-9_]+\(' -- 'test/TranslateReader.Tests/*.cs' | sed -E 's/^public (async Task|void) //; s/\($//' | sort -u > TestResults/dod5-head.txt && test -s TestResults/dod5-base.txt && test -z "$(comm -23 TestResults/dod5-base.txt TestResults/dod5-head.txt)" && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release > TestResults/dod5.log 2>&1 && grep -q "Passed!" TestResults/dod5.log && awk -v tn=$((B+5)) -v sn="$S" '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1);if($i=="Skipped:")s=$(i+1);if($i=="Total:")t=$(i+1)}} END{exit (k&&f+0==0&&t+0>=tn&&s+0<=sn+0&&p+0+s+0+f+0==t+0)?0:1}' TestResults/dod5.log`
      **Source:** CONTEXT

- [ ] Escopo de diff fechado: `TranslationEngine.cs`/`PromptUtility.cs`/`ITranslationManager.cs`/
      `ModelInfo.cs` intocados (prova D-...-5/D-...-6 honradas de verdade); no app MAUI so
      `SettingsOverlay.xaml`/`SettingsOverlay.xaml.cs` mudam (nenhum outro `Pages`/`PageModels`);
      no Core so `ModelAccess.cs`, `TranslationManager.cs`, `IModelAccess.cs` mudam; `THIRD-PARTY-
      NOTICES.md` existe
      **Verify:** `BASE=$(git merge-base origin/main HEAD) && test -z "$(git diff --name-only "$BASE" -- src/TranslateReader.Core/Business/Engines/TranslationEngine.cs src/TranslateReader.Core/Utilities/PromptUtility.cs src/TranslateReader.Core/Contracts/Managers/ITranslationManager.cs src/TranslateReader.Core/Models/ModelInfo.cs)" && test -z "$(git diff --name-only "$BASE" -- src/TranslateReader/ ':(exclude)src/TranslateReader/Pages/Controls/SettingsOverlay.xaml' ':(exclude)src/TranslateReader/Pages/Controls/SettingsOverlay.xaml.cs')" && test "$(git diff --name-only "$BASE" -- src/TranslateReader.Core/ | sort | tr '\n' ',')" = "src/TranslateReader.Core/Access/ModelAccess.cs,src/TranslateReader.Core/Business/Managers/TranslationManager.cs,src/TranslateReader.Core/Contracts/Access/IModelAccess.cs," && test -f THIRD-PARTY-NOTICES.md`
      **Source:** CONTEXT (D-...-4, D-...-5, D-...-6)

### Manual
- _(none — dod=auto_only; itens humanos foram para `## Deferred to PR review`)_

## Deferred to PR review
- Decisao legal/produto: risco residual pra usuarios em EU/Reino Unido/Coreia do Sul (licenca
  Tencent HY exclui esses territorios do grant) — este fluxo automatizado NAO implementa geo-gating
  (sem infra hoje, D-...-3); dono do repositorio decide se aceita o risco, adiciona aviso mais forte
  na UI, ou bloqueia manualmente.
  Ver `.jdi/todos/2026-08-01-hy-mt-translation-model.md`.
- Risco de qualidade de traducao: hy-mt roda com `Temperature=0.1` (deliberado, uniforme com os
  outros modelos) em vez da recomendacao do fornecedor (`temperature=0.7, top_k=20, top_p=0.6,
  repetition_penalty=1.05`) — D-...-5, nao medido nesta phase.
- Confirmacao visual/funcional real (device): baixar o hy-mt de verdade, trocar entre modelos na
  Settings, traduzir um paragrafo e confirmar que o resultado e coerente — sem harness neste
  ambiente (mesmo limite ja documentado em `translated-epub-images`/`conversion-performance`).
- Confirmacao do SonarCloud sem issue nova nos arquivos tocados — so existe apos push+CI.
- Leitura humana: revisar o texto de `THIRD-PARTY-NOTICES.md` e do label de atribuicao em
  `SettingsOverlay` contra o parecer juridico real da empresa, se houver — este fluxo automatizado
  nao substitui revisao legal.

## Notes
Nomes de teste prescritos nesta fase (ainda nao existem, serao criados pela wave de execucao):
`TranslationManagerTests.cs` — `DownloadModelIfNeededAsync_WhenSettingsSelectHyMt_
DownloadsTheHyMtUrl`, `DownloadModelIfNeededAsync_WhenSettingsSelectAnUnregisteredModel_
FallsBackToGemma`, `InitializeEngineIfNeededAsync_WhenSettingsSelectHyMt_UsesTheHyMtFileName` (as 3
novas) + os 3 testes existentes de `DownloadModelIfNeededAsync`/`InitializeEngineIfNeededAsync`
precisam de `ISettingsAccess` mockado no construtor (`FetchSettingsAsync()` retornando
`new ReadingSettings { TranslationModelName = "gemma-2-2b" }` por padrao) e das chamadas
`IsModelAvailable(Arg.Any<string>())`/`GetModelPath(Arg.Any<string>())` — churn mecanico esperado pra
compilar, nao e item de DoD por si.
`ModelAccessTests.cs` — `IsModelAvailable_ReturnsFalseWhenADifferentGgufFileExists`,
`GetModelPath_ThrowsWhenOnlyADifferentGgufFileExists` (as 2 novas, provam o fix por diferenca) + os 4
testes existentes de `IsModelAvailable`/`GetModelPath` precisam do parametro `ModelFileName` (ja
existe como `const` no arquivo) adicionado na chamada.
Auto-teste do asker: os 5 nomes de teste novos NAO existem ainda no repo neste momento — confirmado
por leitura direta de `TranslationManagerTests.cs`/`ModelAccessTests.cs` nesta sessao — prova de que
os itens do DoD nao passam vazio antes da implementacao. `SizeBytes` hoje nao e consumido por
nenhuma UI/logica (so campo informativo no `ModelInfo`), confirmado por grep — por isso o DoD 1 e
puramente estrutural, nao ha comportamento de runtime pra testar sobre esse campo especifico.
