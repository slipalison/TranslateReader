# Phase 23: Review  (slug: snippet-translation)

**Verdict:** APPROVED_WITH_WARNINGS

**Iter:** 6 (fix round pos-loop, autorizado pelo usuario — 3o feedback com screenshots do app real) · Reviewer: `jdi-reviewer-translatereader` (mode=verify + dod-critic fold-in, D-5 enhanced) · Data: 2026-08-10
**HEAD revisado:** `37b5a21` · Fixes revisados: `044870b` (D-A: guarda de proporcao + retry sem contexto + purga no restore) e `daf11a7` (D-B: re-medicao pos-reflow + contorno por coluna) · Baseline da phase: `02a4c6c` · Base do round: `d947dad` (round 3 convergido)

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `net10.0-windows10.0.19041.0` Release: exit 0, `0 Warning(s), 0 Error(s)`. Round nao tocou `Platforms/` -> build mobile secundario nao exigido |
| Tests | PASS | C#: **414 passed / 0 failed / 2 skipped (GPU-only pre-existentes) / 416 total** >= baseline 167 (D-2), +8 vs `d947dad` (3 `SnippetTranslationManagerTests` + 4 `PromptUtilityTests` + 1 `HybridWebViewContractTests`). JS: **175/175, 0 fail, 0 skipped** (+17: 5 do D-A, 12 do D-B). Zero teste perdido nome a nome: `comm -23` VAZIO nos dois lados contra a suite de `d947dad` — re-derivado por este review, nao herdado do self-report |
| Coverage | PASS | `bash scripts/coverage-gate.sh` **exit 0 capturado diretamente (2 execucoes: gate 3 + Verify do DoD 8)**: `COVERAGE_SCOPE covered=1340 valid=1411 pct=94.97 files=26` (piso 90, D-6); `COVERAGE_JS covered=1512 valid=1522 pct=99.34 files=5` (piso 85, D-...-4); `COVERAGE_GUARD new_app_cs=0 waived=0`; zero `COVERAGE_WAIVER_INVALID`. Por arquivo tocado: `TranslationManager.cs` **257/257 = 100%**, `PromptUtility.cs` **39/40 = 97.5%** — ambos >= 90 |
| Lint | WARN | `dotnet format whitespace --verify-no-changes` — as MESMAS 2 violacoes FINALNEWLINE legadas (`Platforms/Android/MainActivity.cs`, `MainApplication.cs`), fora do diff do round (W-7). Nenhum dos 3 `.cs` tocados aparece no log. Zero ID novo em `NoWarn`/`.editorconfig` (diff vazio em `Directory.Build.props`/`.editorconfig`) |
| Security/Layer | PASS | 5.1-5.17 re-executados em HEAD: todos limpos ou identicos ao baseline abencoado (tabela abaixo). Os dois fixes verificados NA FONTE e por execucao — secoes dedicadas abaixo. Zero catch novo no diff (o unico hit de grep e um comentario) |
| Consistency | PASS | 3 commits no round (`044870b` fix, `daf11a7` fix, `37b5a21` docs), conventional, escopo `snippet-translation`, tipos adequados (`fix`/`docs`, nao `feat` cego — D-4), 1 commit por causa-raiz. Arquivos por commit conferidos: batem exatamente com o SUMMARY. TODOS os numeros do SUMMARY iter 6 (175/175, 414+2=416, 94.97, 99.34, GUARD 0/0, lint legado) foram REPRODUZIDOS por este review |
| UI Validation | SKIPPED | has_frontend=false (native MAUI client) — por design, nunca bloqueia. Nota: o orquestrador validou em Chrome real contra o wwwroot real (linha envenenada 10x -> NAO aplicada + `snip-remove` com ancora correta; linha legitima aplicada com vidro; ResizeObserver re-medindo clip-path/d em <500ms apos bump de font-size; `refreshSnippetBlobs` exposto) — evidencia externa, nao gate. Duas sondas anteriores que sugeriam observer morto foram testes INVALIDOS (o estilo nao alcancava o conteudo paginado) e nao contam contra o fix |
| DoD | PASS | **10/10 auto PASS, 0 manual** (`dod=auto_only`). Todos os `Verify:` extraidos VERBATIM do CONTEXT.md (sed sobre o proprio arquivo, sem transcricao manual) e re-executados integralmente em HEAD por este review |

## Blockers

Nenhum. Os dois defeitos reportados pelo usuario (3o feedback) estao corrigidos — verificacao cetica abaixo.

### Verificacao do fix D-A (`044870b`) — guarda de proporcao, retry sem contexto, purga no restore

1. **Cache envenenado REALMENTE tratado como miss e sobrescrito (fonte):** `TranslationManager.cs:406-411` — `cached is not null && !IsSnippetTranslationTooLong(...)` e a UNICA condicao que aceita o cache; reprovou -> `GenerateValidSnippetTranslationAsync`. A gravacao `if (!string.Equals(translated, cached, Ordinal)) SaveTranslationAsync(...)` sobrescreve sob a MESMA chave salgada, e `TranslationCacheAccess.SaveTranslationAsync` e um upsert real (`ON CONFLICT(BookId, ChapterHRef, OriginalHash) DO UPDATE`, `TranslationCacheAccess.cs:56-62`) — a linha podre do cache morre na primeira re-traducao. `SaveSnippetAsync` tambem sobrescreve a linha podre de `SnippetTranslations` (DELETE de ranges sobrepostos + INSERT em transacao, `SnippetTranslationAccess.cs:66-83`). Teste `WhenCachedTranslationIsImplausiblyLong_TreatsItAsAMissAndOverwritesTheCache` pina `Received(1)` no engine E no save do cache. Impossivel `translated == cached` mascarar o save no caminho envenenado: `translated` sempre passou a guarda, `cached` reprovou — comprimentos divergem por construcao.
2. **Retry usa DE FATO as mensagens sem contexto:** `GenerateSnippetTranslationAsync` (`TranslationManager.cs:452-456`) despacha por `includeParagraphContext` para o overload de 5 parametros novo (`PromptUtility.cs:32-42`), que delega `BuildSnippetSystemMessage(paragraph: null)` -> o bloco `Paragraph for context` so entra sob `!string.IsNullOrWhiteSpace(paragraph)` (`PromptUtility.cs:101-107`). Teste do manager distingue os DOIS system messages por valor (`system-with-context` / `system-without-context`) e pina `Received(1)` em CADA assinatura com os argumentos exatos; testes do utility provam ausencia de `Paragraph for context`/`disambiguation context only` e presenca de `EXCLUSIVELY` + trecho delimitado no overload novo.
3. **Falha dupla nao persiste NADA e sai pela fronteira de erro existente:** o throw (`TranslationManager.cs:444`) ocorre antes de qualquer save; teste pina `DidNotReceive()` em `SaveTranslationAsync` E `SaveSnippetAsync`. Fluxo de UI conferido na fonte: `InvalidOperationException` sobe por `RunSnippetTranslationsAsync` -> catch unico do PageModel (`ReaderPageModel.cs:356-361`: DisplayAlert amigavel, retorna lista vazia) -> `HandleSnipRequestAsync` ve `results.Count == 0` -> `clearSnippetLoading` (`ReaderPage.xaml.cs:501-508`) — alerta + limpeza de placeholder, sem stack trace na UI, OCE continua fluindo (rethrow em `ReaderPage.xaml.cs:498` e `ReaderPageModel.cs:354`).
4. **Purga JS restrita a guarda de comprimento (fonte + teste):** em `restoreSnippets` (`snippets.js:1073-1084`) a ordem e: paragrafo ausente -> `continue` silencioso; hash divergente -> `continue` silencioso SEM remove (`snippets.js:1072`, seguranca de re-paginacao intocada); SO a guarda de comprimento emite `snip-remove|` com `{chapterHRef, paragraphIndex, sentenceStart, sentenceEnd}` do proprio item — shape IDENTICO ao emissor pre-existente (`_onSnipRemoveClick`, `snippets.js:915-918`) e ao record `SnippetRemoveRequest` (camelCase via `ReaderJsonContext`), roteando pelo handler existente `HandleSnipRemoveAsync` -> `RemoveSnippetAsync` por ancora exata. 3 testes novos cobrem os 3 ramos (purga com `deepStrictEqual` no JSON; legitimo aplica sem purge; hash divergente descarta sem purge).
5. **Coerencia entre as guardas C#/JS:** a guarda de hash roda ANTES da de comprimento no restore, entao o `original` reconstruido do DOM tem (modulo colisao FNV) o mesmo texto do `request.Text` que o C# validou — formulas identicas nunca divergem sobre a mesma linha, e linha salva pelo C# pos-fix jamais e purgada pelo JS. O risco residual e de EDICAO futura de uma das constantes (W-15).

### Verificacao do fix D-B (`daf11a7`) — blobs re-medidos pos-reflow + contorno por coluna

1. **Ciclo de vida do observer sem leak (fonte + teste):** `_resizeObserver` e singleton de sessao (`snippets.js:230`, criado 1x no load, guardado por `typeof ResizeObserver !== 'undefined'`); TODO `mountSnippetLayer` comeca com `disconnect()` e re-observa so os candidatos do mount atual (`snippets.js:803-812`) — observacoes de capitulo anterior nunca acumulam em nos destacados; `unmountSnippetLayer` desconecta tambem (`snippets.js:836`). O alvo observado e o paragrafo candidato, que `_wrapParagraph` muta IN PLACE (`snippets.js:744-772` — o `el` permanece no DOM), entao a observacao e sobre no vivo. Coalescencia: `_blobRefreshScheduled` reseta DENTRO do callback agendado antes do sweep (`snippets.js:242-245`) — sem flag preso; rAF com fallback `setTimeout` (harness). `document.fonts.ready.then(_renderAllBlobs)` guardado por `if (document.fonts)`; callbacks em promise settled disparam 1x e nao acumulam. O sweep so mexe em mask/svg `position:absolute` — nao altera o tamanho do paragrafo, sem loop de feedback com o proprio observer. 6 testes novos (observa cada paragrafo; unmount desconecta; callback re-mede via timer de fallback assertando o DELTA de timers, nao total absoluto; fonts.ready re-mede; mount nao lanca sem nenhum dos dois suportes) + 4 no harness (stubs OPT-IN ausentes por default — os testes pre-existentes seguem exercitando o caminho "host sem suporte" de graca).
2. **`refreshSnippetBlobs` e a ponte C#:** `window.refreshSnippetBlobs = _renderAllBlobs` (`snippets.js:468`, identidade pinada por teste JS + contract test C#). `ReaderPage` chama `RefreshSnippetBlobsAsync()` apos `GoToPageAsync`/`GoToLastPageAsync`/`NextPageAsync`/`PrevPageAsync`/`RestoreScrollPositionAsync` — string CONSTANTE em `EvaluateJavaScriptAsync("refreshSnippetBlobs()")`, zero interpolacao, zero superficie 5.8 nova. Sweep vazio e no-op barato (`_blobDescriptors` vazio -> so o retire-loop).
3. **Particao por coluna correta e goldens intactos:** `_columnGroupsOf` (`snippets.js:117-130`) abre grupo novo quando o `top` da linha regride (`top < previousTop`) — o unico sinal deterministico de wrap de coluna, viabilizado pela REMOCAO do sort global de points (a ordem natural de `getClientRects()` e ordem de leitura; o sort antigo escondia o salto para tras e intercalava colunas). Cada grupo gera bandas proprias e `_blobPath(bands, 10)` proprio (literal do DoD 4 preservado), concatenados em subpaths `M...Z M...Z`. Teste 2-colunas pina exatamente 2 `M`/2 `Z`; teste same-column pina 1 `M`/1 `Z` (o contorno unico do iter 3 nao regride). Goldens single-column BYTE-IDENTICOS provados por diff: `git diff d947dad..HEAD -- test/js/snippets.test.js` tem **0 linhas removidas** (adicoes puras) e os 4 testes de nome exato do DoD 4 passam em execucao real desta iter. `OFF=8`/`padX=5`/`padY=1.5` e regex unica de `_splitSentences` re-contados pos-diff (1x cada, comment-stripped). Nota de fronteira (nao-bloqueante): a particao opera sobre LINHAS ja agrupadas por proximidade de `cy` — se cauda e cabeca de colunas distintas caissem a menos de `0.6*height` em `cy`, ainda se fundiriam antes da particao; em layout real de pager as colunas compartilham a altura cheia (cauda no rodape, cabeca no topo), tornando o caso inatingivel na geometria que o app produz.
4. **Arquivos congelados intocados:** `translation.js`/`paginated.js`/`scroll.js` com diff VAZIO vs `02a4c6c` re-executado em HEAD (0 arquivos); `harness.js` nao e congelado (superficie declarada da phase).

## Warnings

Novas do iter 6 (2, ambas menores):

- **W-15 — Formula da guarda duplicada entre linguagens sem pino cruzado.** `translated.Length > (text.Length * 3) + 120` vive em `TranslationManager.cs:423-424` e `snippets.js:47-49` como espelhos documentados (comentarios apontam um para o outro; RPC assincrono no restore justificadamente evitado). A coerencia atual e garantida pela guarda de hash rodar antes (texto identico dos dois lados), mas editar UMA das constantes reabre churn de purga (C# salva -> JS purga a cada restore -> re-traduz -> salva -> purga). Fix barato: `HybridWebViewContractTests` ja le os arquivos JS — um contract test assertando o literal `* 3) + 120` nos DOIS arquivos pina a paridade. Mesmo tratamento de par ja aceito para `ComputeSnippetHash`/`_snipHash` (aquele tem golden `9d2a73a5` dos dois lados; este par nao tem equivalente).
- **W-16 — Predicado de plausibilidade mora no Manager.** `IsSnippetTranslationTooLong` e uma regra numerica de validacao dentro de `TranslationManager` — leitura estrita de The Method mandaria para Engine/Utility (5.5). Julgamento: o fluxo tentar-com-contexto -> retry-sem-contexto -> falhar e orquestracao legitima de use case, e o predicado e 1 expressao consistente com `CleanTranslationOutput`/`ComputeSnippetHash` ja abencoados no mesmo arquivo. Candidato a extracao (junto com W-15) se a validacao de resposta crescer.

Abertas de iters anteriores (re-verificadas em HEAD `37b5a21`; o diff do iter 6 nao piorou nenhuma):

- **W-2 — Hint pode ressuscitar com a camada desmontada (JS).** Zero `removeEventListener` em `snippets.js`; inalterado.
- **W-3 — `SnippetRequest.ChapterHRef` string nao-anulavel carregando null do WebView** no modo paginado. Inalterado.
- **W-4 — `_APP_ACCENT` hardcoded** duplicando o accent dos tokens XAML (DRY). Inalterado.
- **W-5 — `SnippetLabels.cs` 0/15 e `SnippetTheme.cs` 0/1** — re-confirmado no `COVERAGE_FILE` desta iter; gate agregado passa com folga (94.97).
- **W-6 — Legado: `dotnet test` a nivel de solution sai 1** (CA1711 em TFMs iOS/MacCatalyst). Pre-existente de `main`.
- **W-7 — Legado: FINALNEWLINE em `Platforms/Android/MainActivity.cs` e `MainApplication.cs`** (re-confirmado hoje; fora do diff).
- **W-8 — OCE absorvida so na fronteira terminal de event handler** (grep identico ao baseline — o iter 6 nao adicionou nenhum catch; o unico `+` com "catch" no diff e comentario).
- **W-9 — Sem guard de reentrancia no `EnsureModelDownloadedAsync`**; mitigado pelo overlay modal.
- **W-10 — Afinidade de thread das propriedades observaveis** no handler hybrid (correto no Windows, alvo locked).
- **W-11 — Falha parcial em selecao multi-trecho diverge UI/banco ate reload.** Inalterado em natureza; nota: o throw da guarda dupla (D-A) e um GATILHO NOVO para o mesmo cenario (trechos 1..k-1 persistidos, alerta geral, UI sem aplicar nada ate o proximo restore — que os aplica corretamente, pois passaram na guarda). Mesma phase de higiene.
- **W-12 — Custo do sweep `_renderAllBlobs`.** Inalterado em natureza; nota: o iter 6 ADICIONA disparadores do sweep (fonts.ready, ResizeObserver coalescido, 5 call sites C#) — todos 1x por evento discreto de navegacao/reflow, nunca por token/paragrafo em loop; o proprio teste do observer prova coalescencia (1 timer por rajada).
- **W-13 — `_originalParagraphText` trunca periodo com MARKUP ao primeiro filho** (`snippets.js`, campo `paragraph` do payload). Fora do diff do iter 6; fix de 1 linha sugerido no iter 5 segue valido. Mitigacao extra agora: se o contexto truncado induzir resposta longa, a guarda D-A derruba e o retry sem contexto cobre.
- **W-14 — Hint nao e re-medido em resize.** Inalterado.

## Gate 5 — detalhe das checagens (todas re-executadas neste review, iter 6)

| Check | Resultado |
|---|---|
| 5.1 Client -> Access/Engine | limpo (grep sem output) |
| 5.2 Storage em `Contracts/Access` | limpo — `ISnippetTranslationAccess`/`IPromptUtility` sem SQL/Sqlite (o overload novo e puro dominio) |
| 5.3 Manager -> Manager | limpo — hits sao cada Manager implementando o proprio contrato; o diff do iter 6 injeta zero dependencia nova (so metodos privados) |
| 5.4 PageModel >1 Manager por caso de uso | ok — nenhum PageModel no diff (`ReaderPageModel` intocado neste iter) |
| 5.5 Regra de negocio em Manager/PageModel | ok com ressalva registrada (W-16): retry e orquestracao; o predicado de plausibilidade e a unica regra e e 1 expressao |
| 5.6 Zip-slip | so baseline (`ParsingEngine`, intocado) |
| 5.7 XXE | zero hits |
| 5.8 WebView JS injection | 1 site novo de `EvaluateJavaScriptAsync` — string CONSTANTE `"refreshSnippetBlobs()"`, sem interpolacao. Auditoria dos 12 sites interpolados re-executada: todos `JsStr(...)` ou variaveis `*Json` pre-serializadas |
| 5.9 Secrets/PII em log | limpo (a mensagem da `InvalidOperationException` nova nao embute texto de livro — so descreve a condicao) |
| 5.10 Sync-over-async / OCE | zero hits de `.Result`/`.Wait()`; catches de OCE identicos ao baseline (W-8); `ct` atravessa os 2 metodos privados novos ate o engine; teste de cancelamento pre-existente segue verde |
| 5.11 Eventos +=/-= | subscribe=5 / unsubscribe=4 — IDENTICO ao baseline do bootstrap; nenhum evento C# novo. No JS, observer/fonts sao geridos por mount/unmount (disconnect provado por teste), zero listener novo de window/document |
| 5.12 Static mutavel | nenhum novo pelo iter 6 — hits: baseline `_nativeLibraryConfigured` (abencoado) + 3 properties computadas `static` expression-bodied em `SettingsOverlay` (sem estado, fora do diff). No JS, `_resizeObserver` e singleton imutavel apos criacao e `_blobRefreshScheduled` e flag de coalescencia com reset provado |
| 5.13 Cache in-memory sem bound | nenhum novo; `_blobs` Map segue bounded (retire-loop remove stale, re-verificado em `_renderAllBlobs`) |
| 5.14 Alocacao em hot path | limpo — guarda e comparacao de length O(1); `_columnGroupsOf` roda por sweep discreto de render, nao por token/paragrafo de traducao; zero `ReadAll*` novo |
| 5.15 Fail fast | `InvalidOperationException` nova E fail-fast correto (csharp.md par.1) convertida em estado de UI SO no catch unico do PageModel; zero catch novo; sem Result/Try pattern |
| 5.16 TODO sem ticket | zero |
| 5.17 Disciplina de teste D-2 | NSubstitute so contra interfaces (grep limpo, incluindo o mock das DUAS assinaturas do `IPromptUtility`); zero I/O real nos arquivos de teste novos/tocados (o `File.ReadAllText` de `HybridWebViewContractTests` le o proprio fonte JS do repo — padrao de contract test prescrito pelo CONTEXT, pre-existente); caminho novo coberto em sucesso, falha (throw sem persistencia) e o cancelamento pre-existente cobre o caminho de cancel |

## DoD Checklist (gate 8)

Comandos extraidos VERBATIM do CONTEXT.md (via sed no proprio arquivo, sem transcricao manual) e executados integralmente em HEAD `37b5a21` por este review.

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | Ground truth v0.2.0 existe e e medida (PIXEL-SPEC + >= 4 screenshots) | CONTEXT (D-...-4) | Auto | PASS | exit 0; 14 literais presentes; >= 4 JPGs; `design/` fora do diff do iter 6 |
| 2 | Tabela/Model/Access novos, storage invisivel no contrato, round-trip testado | CONTEXT (D-...-1) | Auto | PASS | exit 0; filtro `~Snippet`: **40 passed / 0 failed** (piso 12; +8 vs iter 5 = os 8 testes novos com "Snippet" no FQN) |
| 3 | Split de periodos: UMA funcao, regex literal, fronteiras testadas | CONTEXT (D-...-4) | Auto | PASS | exit 0; regex 1x re-contada comment-stripped POS-diff; `# pass 6`, fail 0 |
| 4 | Geometria dourada do blob (4 testes de nome exato) | CONTEXT (D-...-4) | Auto | PASS | exit 0; 4 nomes exatos ok no TAP (8 `blob geometry` no total, incl. os 2 de coluna novos); 0 linhas REMOVIDAS no diff de `snippets.test.js` (goldens byte-identicos); `_blobPath(bands, 10)` literal preservado dentro do map novo; OFF=8/padX=5/padY=1.5 intactos |
| 5 | Persistencia: restaura, respeita toggle, descarta hash divergente | CONTEXT (D-...-1) | Auto | PASS | exit 0; 4 testes de nome exato pass (suite `restore/toggle/remove` agora com 15 pass — os 3 de purga entram no mesmo padrao); `ShowingOriginal` no Model |
| 6 | Independente do modo: `_snippetRoots` unica fonte, 2 modos testados | CONTEXT (D-...-3) | Auto | PASS | exit 0; igualdade corpo==arquivo re-executada POS-diff (os hunks do iter 6 — guarda ~41-49, colunas ~107-186, observer ~224-250, mount/unmount ~800-840, restore ~1073-1084 — nao intersectam `_snippetRoots`) |
| 7 | 3 JS congelados intocados, seletor nao duplicado, ordem de carga | CONTEXT (D-...-2,-3) | Auto | PASS | exit 0; diff vazio vs BASELINE `02a4c6c` em HEAD (re-executado 2x: gate estrutural + Verify); zero querySelectorAll proibido (comment-stripped); snippets.js depois de translation.js |
| 8 | Gate de cobertura com 5 JS e sem `.cs` novo no app MAUI | CONTEXT (D-...-2) | Auto | PASS | exit 0; `COVERAGE_JS ... files=5`; `COVERAGE_GUARD new_app_cs=0 waived=0`; pisos 90/85 inalterados; zero WAIVER_INVALID |
| 9 | Build limpo + 2 suites verdes, piso derivado de `main`, zero teste perdido | CONTEXT (D-...-6) | Auto | PASS | exit 0; build 0 Error(s); C# 414/0/2 = 416 total, piso B+12 superado, comm -23 vazio vs `main`; JS 175/175 fail 0 skipped 0, comm vazio vs `main` E (checagem extra deste review) vs `d947dad` nos 6 arquivos: 0 perdido, 17 adicionados |
| 10 | Literais visuais na spec E no JS; zero pt-BR no JS; rotulos via `setSnippetLabels` | CONTEXT (D-...-2,-4) | Auto | PASS | exit 0; 15 literais nos 2 arquivos; grep pt-BR em `snippets.js` = 0 (o JS novo do iter 6 — guarda, observer, colunas — nao contem string de UI); 9 strings pt-BR no ReaderPage |

**Totals:** 10 items | Auto: 10 (10 PASS, 0 FAIL) | Manual: 0 pending

Nota: `.jdi/PROJECT.md` nao possui secao `## Definition of Done`; o conjunto acima (CONTEXT.md, `dod=auto_only`) e o DoD integral. Itens humanos vivem em `## Deferred to PR review` do CONTEXT — a validacao do orquestrador em Chrome real (purga + ResizeObserver + refresh expostos) e evidencia externa a favor, mas a conferencia em DEVICE segue deferida ao PR review por decisao da phase.

## DoD-critic (D-5 enhanced, segunda passada — mesmo reviewer, mode=dod-critic)

Re-inspecao adversarial dos 10 rows Auto/PASS contra os ARTEFATOS em HEAD `37b5a21` (nao contra os comandos):

```json
[
  {"row": 1, "hollow": false, "evidence": "design/ nao aparece em git diff --name-only d947dad..HEAD — a spec e os screenshots sao exatamente os ja validados; os 14 literais re-grepados no arquivo real"},
  {"row": 2, "hollow": false, "evidence": "40 testes ~Snippet re-executados reais (0 failed); o salto 32->40 explicado nominalmente (3 manager + 4 prompt + 1 contract, todos com Snippet no FQN), nao e drift do filtro"},
  {"row": 3, "hollow": false, "evidence": "regex re-contada comment-stripped APOS um iter que adicionou ~100 linhas ao arquivo; _splitSentences segue fonte unica; nenhum hunk do iter 6 toca split"},
  {"row": 4, "hollow": false, "evidence": "git diff d947dad..HEAD em snippets.test.js: 0 linhas removidas — os goldens sao os MESMOS bytes; o refactor de _blobFromEls (sort removido + grupos por coluna) nao os invalida porque os 4 goldens exercitam _blobPath direto e os 2 testes novos pinam a integracao (2M/2Z vs 1M/1Z); os 4 passam por nome exato em execucao real desta iter"},
  {"row": 5, "hollow": false, "evidence": "harness real, DOM mutado; os 3 testes de purga novos usam o restoreSnippets REAL e pinam o JSON do snip-remove por deepStrictEqual contra o shape do record C#; o ramo hash-divergente-sem-purge tem teste proprio"},
  {"row": 6, "hollow": false, "evidence": "hunks do iter 6 nao intersectam _snippetRoots; igualdade corpo==arquivo verdadeira em HEAD por re-execucao do proprio Verify"},
  {"row": 7, "hollow": false, "evidence": "diff dos 3 JS congelados vazio contra o BASELINE real (02a4c6c) em HEAD, re-executado 2x; harness.js foi tocado mas NAO e congelado (superficie declarada no CONTEXT); ordem de carga no index.html real"},
  {"row": 8, "hollow": false, "evidence": "gate re-executado 2x com exit 0 real; SCOPE 94.97 SUBIU de 94.81 coerente com TranslationManager 100% (257/257, +19 linhas) e PromptUtility 97.5% entrando maiores no AM scope — numero deriva do diff, nao stale"},
  {"row": 9, "hollow": false, "evidence": "416 C# reais / 175 JS reais rodados por este review; comm -23 vazio nos 2 lados. Ponto cego conhecido do comando (comm JS ve so os 4 arquivos de main) fechado de novo por comm independente vs d947dad nos 6 arquivos: zero perdido, 17 adicionados (os 17 anunciados: 5+12)"},
  {"row": 10, "hollow": false, "evidence": "diff do iter 6 lido na integra: zero literal pt-BR novo em snippets.js (guarda/observer/colunas sao codigo sem string de UI; comentarios em ingles); grep do Verify re-executado com 0 hits"}
]
```

**Resultado do critic: nenhum row hollow.** Observacao de escopo (nao rebaixa nenhum row): o DoD desta phase nao tem item que force a guarda de comprimento nem a re-medicao pos-reflow — a prova de ambos vive nos 25 testes novos + na validacao externa do orquestrador em Chrome real, fora do DoD formal. W-15 (paridade de formula sem pino) nao torna nenhum row hollow porque nenhum criterio do DoD cobre a formula. O critic so aperta, nunca afrouxa — verdito mantido.

## Recommendation

Os dois defeitos do 3o feedback estao corrigidos pela raiz e provados por teste + execucao independente deste review. D-A: a resposta do modelo agora passa por uma guarda deterministica de proporcao em TODOS os caminhos (cache, inferencia 1 com contexto, retry sem contexto), nada invalido e persistido ou aplicado, e o restore purga sozinho as linhas envenenadas de sessoes antigas pelo canal `snip-remove` existente — mantendo o descarte silencioso (sem delete) para hash divergente. D-B: os blobs se re-medem apos os tres reflows que os matavam no app real (fontes async via `fonts.ready`, resize de paragrafo via `ResizeObserver` coalescido com ciclo de vida provado, navegacao via `refreshSnippetBlobs()` chamado dos 5 call sites C#), e paragrafo fragmentado entre colunas ganha um contorno por coluna sem banda atravessando o vao — com os goldens single-column byte-identicos. Todos os numeros do SUMMARY foram reproduzidos (175/175 JS, 414+2 C#, 94.97/99.34, GUARD 0/0). Duas warnings novas menores (W-15 paridade da formula da guarda sem pino cruzado — contract test de 1 assert sugerido; W-16 predicado de plausibilidade no Manager) somam-se as W-2..W-14 ja aceitas como candidatas a phase de higiene — nenhuma bloqueia. Pronto para `/jdi-ship snippet-translation`.
