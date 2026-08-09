# Phase 23: Review  (slug: snippet-translation)

**Verdict:** APPROVED_WITH_WARNINGS

**Iter:** 5 (round 3, iter 1 — re-review cobrindo os DOIS fixes do round: pill/fonte e poluicao de contexto) · Reviewer: `jdi-reviewer-translatereader` (mode=verify + dod-critic fold-in, D-5 enhanced) · Data: 2026-08-09
**HEAD revisado:** `1f5688f` · Fixes revisados: `b3005e1` (pill fit + fonte) e `25ef3f3` (contexto de paragrafo limpo + prompt endurecido + salt de cache) · Baseline da phase: `02a4c6c`

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `net10.0-windows10.0.19041.0` Release: exit 0, `0 Warning(s), 0 Error(s)`. Round nao tocou `Platforms/` -> build mobile secundario nao exigido |
| Tests | PASS | C#: **406 passed / 0 failed / 2 skipped (GPU-only pre-existentes) / 408 total** >= baseline 167 (D-2), +2 vs iter 4 (os 2 testes C# novos). JS: **158/158, 0 fail, 0 skipped** (148 + 8 do fix da pill + 2 do fix de contexto). Zero teste perdido nome a nome: `comm -23` VAZIO nos dois lados contra a suite do iter 4 (`dd5f98c`) — re-derivado por este review, nao herdado do self-report |
| Coverage | PASS | `bash scripts/coverage-gate.sh` **exit 0 capturado diretamente (2 execucoes: gate 3 + Verify do DoD 8)**: `COVERAGE_SCOPE covered=1316 valid=1388 pct=94.81 files=26` (piso 90, D-6 — agora COM os 2 `.cs` tocados no AM scope); `COVERAGE_JS covered=1422 valid=1432 pct=99.30 files=5` (piso 85, D-...-4); `COVERAGE_GUARD new_app_cs=0 waived=0`; zero `COVERAGE_WAIVER_INVALID`. Por arquivo tocado: `TranslationManager.cs` **238/238 = 100%**, `PromptUtility.cs` **34/36 = 94.4%** — ambos >= 90 |
| Lint | WARN | `dotnet format whitespace --verify-no-changes` — as MESMAS 2 violacoes FINALNEWLINE legadas (`Platforms/Android/MainActivity.cs`, `MainApplication.cs`), fora do diff do round (W-7). Os 4 `.cs` tocados nao aparecem no log. Zero ID novo em `NoWarn`/`.editorconfig` (diff vazio) |
| Security/Layer | PASS | 5.1-5.17 re-executados em HEAD: todos limpos ou identicos ao baseline abencoado (tabela abaixo). Os dois fixes verificados NA FONTE e por execucao — secoes dedicadas abaixo |
| Consistency | PASS | 4 commits no round (`b3005e1` fix, `8b081a7` docs, `25ef3f3` fix, `1f5688f` docs), conventional, escopo `snippet-translation`, tipos adequados (`fix`/`docs`, nao `feat` cego — D-4). Escopo de arquivos por commit conferido contra `git show --name-only`: bate exatamente com o SUMMARY. Todos os numeros do SUMMARY iter 5 (158/158, 408, 94.81, 99.30, exit 0 no gate, lint legado) foram REPRODUZIDOS por este review |
| UI Validation | SKIPPED | has_frontend=false (native MAUI client) — por design, nunca bloqueia. Nota: o orquestrador validou em Chrome real contra o wwwroot real (pill 1 linha em 1100/700/430px com degradacao confirmada; fonte computada `Inter, sans-serif`; payload `snip|` com snips irmaos presentes carregando `paragraph` byte-igual ao original) — evidencia externa, nao gate |
| DoD | PASS | **10/10 auto PASS, 0 manual** (`dod=auto_only`). Todos os `Verify:` extraidos VERBATIM do CONTEXT.md (sed sobre o proprio arquivo, sem transcricao manual) e re-executados integralmente em HEAD por este review |

## Blockers

Nenhum. Os dois defeitos reportados pelo usuario no round 3 estao corrigidos — verificacao cetica abaixo.

### Verificacao do fix 1 (`b3005e1`) — pill/hint: fit no viewport real + fonte Inter

1. **`_fitPill` termina e nao oscila (fonte, nao changelog):** `snippets.js:265-277` e straight-line — mede (`_fits`), remove tip/only, re-mede, remove o span de label do botao. SEM loop, SEM re-adicao de nada dentro da funcao -> terminacao trivial e oscilacao impossivel por construcao. Re-fit em resize NUNCA opera sobre pill ja degradada: `_onResize` (`snippets.js:665-671`) com `_sel` ativo chama `_renderSelection` -> `_showPill` -> `_buildPill` novo -> `_fitPill` no elemento fresco. Teste `resize: rebuilds the pill when a selection is active...` pina com `assert.notStrictEqual` que o ELEMENTO troca. Acessibilidade no degrau final: `title`/`aria-label` = `_labels.translateSnip` via `setAttribute` (API DOM, sem parse de HTML — sem superficie de injecao).
2. **nowrap + max-width + fonte:** os 4 seletores (`.tr-pill-tip`, `.tr-pill-only`, `.tr-pill-primary`, `.tr-hint`) ganharam `white-space: nowrap`; `.tr-pill`/`.tr-hint` ganharam `max-width: calc(100vw - 24px)`; TODO `var(--font-body)` eliminado do arquivo (pill, hint E chip -> `'Inter', sans-serif !important`, respondendo ao `!important` do `body` gerado pelo `ThemeEngine`, que ficou intocado — diff 0 linhas). 2 testes de CSS pinam ambos (busca por regra, nao snapshot cego).
3. **Hint dispensavel:** `_renderHint` (`snippets.js:559-570`) mede o hint recem-anexado e o REMOVE por inteiro se nao couber — nunca degrada por partes. Teste com viewport 0 pina.
4. **Nada proibido reintroduzido:** contagem comment-stripped de querySelectorAll com aspas simples e p/h1/li/div = **0**; os `querySelector` novos de `_fitPill` usam aspas duplas e classes proprias; `translation.js`/`paginated.js`/`scroll.js` com **diff VAZIO** vs `02a4c6c` re-executado em HEAD; goldens de geometria byte-identicos (diff do round em `snippets.test.js` contem 0 linhas com `blob geometry`); `_blobPath(bands, 10)`, `OFF=8`/`padX=5`/`padY=1.5` conferidos via DoD 4.

### Verificacao do fix 2 (`25ef3f3`) — contexto limpo + prompt endurecido + salt de cache

1. **`_originalParagraphText` reconstitui o original em ordem (fonte + execucao):** `snippets.js:1073-1087` percorre `p.childNodes` em ordem de documento: text node separador passa como esta (o wrap cria exatamente 1 espaco entre periodos, `snippets.js:686`, e `_spliceRange` preserva os separadores externos ao range); snip contribui `dataset.orig` (sempre gravado em `_buildSnipSpan:889`; e `_rangeText` = periodos unidos por ' ', que casa com o que os separadores originais eram); blob mask E svg pulados via `_hasClass(node,'tr-blob')` (substring: `'tr-blob-svg'.indexOf('tr-blob')===0`, semantica documentada em `snippets.js:309-313`); periodo/loading contribuem seu texto. Snip span NAO carrega `data-si` -> `_rangeText` (campo `text`) nunca inclui snip nem chip. **Unico call site** produzindo `paragraph` no payload = `snippets.js:1096` (grep integral do arquivo); zero uso remanescente de `p.textContent` para contexto de prompt. Os 2 testes novos executam o cenario EXATO reportado (snip mostrando traducao + snip mostrando original + periodo comum) e assertam byte-igualdade + ausencia de `She arrived`/`PT-BR`/`EN`. Gap residual em paragrafo com MARKUP -> W-13 (abaixo), nao afeta o cenario reportado (paragrafos texto-puro).
2. **Salt REALMENTE restrito ao caminho de snippet:** `SnippetCacheKeySalt = "snippet-prompt-v2|"` (`TranslationManager.cs:51`) e usado em EXATAMENTE 1 lugar — `TranslateSnippetAsync:404` — e a MESMA variavel `hash` salgada alimenta `FetchTranslationAsync` E `SaveTranslationAsync` (le e grava sob a chave nova, coerente). Os 4 call sites do caminho de paragrafo (`TranslationManager.cs:165,263,292,340`) seguem `ComputeHash(original, ...)` SEM salt — cache de paragrafo intacto, zero invalidacao. Teste novo `TranslateSnippetAsync_CacheKeyIsSaltedAwayFromTheLegacyParagraphHash` reproduz a formula legada INDEPENDENTE (SHA-256 proprio no teste, nao importado do SUT) e prova divergencia no fetch E no save via `Arg.Is`. Consequencia aceita: as entradas antigas de snippet viram linhas mortas no `TranslationCache` (inalcancaveis, bounded pelo uso passado) — sem necessidade de wipe.
3. **`ComputeSnippetHash` e a paridade `9d2a73a5` intocados:** FNV-1a (`TranslationManager.cs:385-397`) fora do diff do round; golden `9d2a73a5` presente e VERDE nos dois lados — JS (`snippets.test.js:7,125`, teste `golden` re-executado isolado: pass 1/fail 0) e C# (`SnipHashGolden`, `Assert.Equal` no resultado do manager, suite verde). O restore continua validando ancora contra o mesmo hash de sempre.
4. **Prompt endurecido conforme alegado:** system message (`PromptUtility.cs:72-91`) traz o trecho delimitado por aspas triplas + instrucao `Respond with EXCLUSIVELY the direct translation...` + paragrafo delimitado por aspas triplas com instrucao explicita de nao traduzir/repetir; user message segue sendo o trecho cru inalterado (`return (systemMessage, snippet)`, linha 30). Teste novo asserta `EXCLUSIVELY` + os DOIS blocos delimitados; o teste antigo manteve nome e escopo (paragrafo como contexto), perdendo apenas a assercao de substring que deixou de existir — zero teste perdido (comm vazio).

## Warnings

Novas do iter 5 (2, ambas menores):

- **W-13 — `_originalParagraphText` trunca periodo com MARKUP ao primeiro filho.** `snippets.js:1083` usa `node.childNodes[0].textContent`; para o wrap-span de paragrafo com filhos-elemento (`_wrapParagraph:694-703`, ex. `<em>X</em> resto...`), isso devolve so o texto do PRIMEIRO no movido — o campo `paragraph` fica truncado nesse sub-caso (regressao vs `p.textContent`, que era completo ali). Impacto contido: paragrafo com markup e sempre periodo UNICO, o campo `text` (via `_rangeText`, `textContent` completo) esta correto, e o system prompt agora repete o trecho inteiro — o contexto truncado quase coincide com o proprio trecho. Fix barato: `parts.push(node.textContent)` no branch `data-si` (seguro: periodo/loading nunca contem chip; chip so existe em snip, ja tratado antes via `dataset.orig`).
- **W-14 — Hint nao e re-medido em resize.** `_onResize` nunca reavalia `_hintEl`: hint que coube em janela larga pode estourar visualmente apos encolher (nowrap + max-width = clipping), e hint descartado em janela estreita nao volta ao alargar ate o proximo `_renderHint`. Cosmetico, parente de W-2 (ciclo de vida do hint) — candidato a mesma phase de higiene.

Abertas de iters anteriores (re-verificadas em HEAD `1f5688f`; o diff do round nao piorou nenhuma):

- **W-2 — Hint pode ressuscitar com a camada desmontada (JS).** Zero `removeEventListener` em `snippets.js`; inalterado.
- **W-3 — `SnippetRequest.ChapterHRef` string nao-anulavel carregando null do WebView** no modo paginado. Inalterado.
- **W-4 — `_APP_ACCENT` hardcoded** duplicando o accent dos tokens XAML (DRY). Inalterado.
- **W-5 — `SnippetLabels.cs` 0/15 e `SnippetTheme.cs` 0/1** — re-confirmado no `COVERAGE_FILE` desta iter; gate agregado passa com folga (94.81).
- **W-6 — Legado: `dotnet test` a nivel de solution sai 1** (CA1711 em TFMs iOS/MacCatalyst). Pre-existente de `main`.
- **W-7 — Legado: FINALNEWLINE em `Platforms/Android/MainActivity.cs` e `MainApplication.cs`** (re-confirmado; fora do diff).
- **W-8 — OCE absorvida so na fronteira terminal de event handler** (padrao baseline; grep identico — o round nao adicionou nenhum catch).
- **W-9 — Sem guard de reentrancia no `EnsureModelDownloadedAsync`**; mitigado pelo overlay modal.
- **W-10 — Afinidade de thread das propriedades observaveis** no handler hybrid (correto no Windows, alvo locked).
- **W-11 — Falha parcial em selecao multi-trecho diverge UI/banco ate reload.** Inalterado.
- **W-12 — Custo do sweep `_renderAllBlobs`.** Inalterado (o novo `_onResize` com `_sel` chama `_renderSelection`, que ja varria).

## Gate 5 — detalhe das checagens (todas re-executadas neste review, iter 5)

| Check | Resultado |
|---|---|
| 5.1 Client -> Access/Engine | limpo (grep sem output) |
| 5.2 Storage em `Contracts/Access` | limpo |
| 5.3 Manager -> Manager | limpo — hits sao cada Manager implementando o proprio contrato (`TranslationManager : ITranslationManager, ISnippetTranslationManager` = 2 contratos do MESMO servico, D-...-5) |
| 5.4 PageModel >1 Manager por caso de uso | ok — nenhum PageModel no diff do round |
| 5.5 Regra de negocio em Manager/PageModel | ok — o diff em `TranslationManager` e 1 const + 1 concat na chamada existente (orquestracao, nao regra); a regra de prompt vive em `PromptUtility` (Utility, lugar certo) |
| 5.6 Zip-slip | so baseline (`ParsingEngine`, intocado) |
| 5.7 XXE | zero hits |
| 5.8 WebView JS injection | nenhum site de `EvaluateJavaScriptAsync` no diff do round; auditoria completa do iter 3 permanece valida. No JS novo, `title`/`aria-label` via `setAttribute` (sem parse de HTML) e labels vindos do C# por `setSnippetLabels` |
| 5.9 Secrets/PII em log | limpo (o prompt endurecido embute texto de livro no prompt do LLM local — dado que JA ia ao mesmo engine como user message, sem mudanca de classe de risco; nada disso e logado) |
| 5.10 Sync-over-async / OCE | zero hits de `.Result`/`.Wait()`; catches de OCE identicos ao baseline (W-8); `TranslateSnippetAsync` mantem `ThrowIfCancellationRequested` + propagacao (teste de cancelamento pre-existente segue verde) |
| 5.11 Eventos +=/-= | subscribe=5 / unsubscribe=4 — IDENTICO ao baseline do bootstrap; nenhum evento C# novo. No JS, `_fitPill` nao adiciona listener; `_onResize` reusa o listener unico registrado sob `if (!_mounted)` |
| 5.12 Static mutavel | nenhum novo — `SnippetCacheKeySalt` e `private const` (imutavel por definicao); hits restantes = baseline abencoado |
| 5.13 Cache in-memory sem bound | nenhum novo; o salt cria linhas mortas no `TranslationCache` (SQLite, bounded pelo uso passado) — aceito, nao e cache in-memory |
| 5.14 Alocacao em hot path | limpo nos 2 arquivos tocados (concat de salt e 1x por chamada de snippet, nao por token/paragrafo em loop; `BuildSnippetSystemMessage` monta lista pequena 1x por trecho) |
| 5.15 Fail fast | zero catch novo; sem Result/Try pattern |
| 5.16 TODO sem ticket | zero |
| 5.17 Disciplina de teste D-2 | NSubstitute so contra interfaces (grep limpo, incluindo os testes novos); zero I/O real nos 2 arquivos de teste tocados; caminho novo coberto em sucesso (cache-miss com salt) e a divergencia de chave provada por formula independente |

## DoD Checklist (gate 8)

Comandos extraidos VERBATIM do CONTEXT.md (via sed no proprio arquivo, sem transcricao manual) e executados integralmente em HEAD `1f5688f` por este review.

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | Ground truth v0.2.0 existe e e medida (PIXEL-SPEC + >= 4 screenshots) | CONTEXT (D-...-4) | Auto | PASS | exit 0; 14 literais presentes (a sub-nota nova de degradacao nao removeu nenhum); >= 4 JPGs |
| 2 | Tabela/Model/Access novos, storage invisivel no contrato, round-trip testado | CONTEXT (D-...-1) | Auto | PASS | exit 0; filtro `~Snippet`: **32 passed / 0 failed** (piso 12; +2 vs iter 4 = os metodos novos com "Snippet" no FQN) |
| 3 | Split de periodos: UMA funcao, regex literal, fronteiras testadas | CONTEXT (D-...-4) | Auto | PASS | exit 0; regex 1x re-contada comment-stripped POS-diff do round; testes `splitSentences` fail 0 |
| 4 | Geometria dourada do blob (4 testes de nome exato) | CONTEXT (D-...-4) | Auto | PASS | exit 0; 4 nomes exatos ok no TAP; diff do round em `snippets.test.js` contem 0 linhas `blob geometry` (goldens byte-identicos); `_blobPath(bands, 10)` literal; OFF=8/padX=5/padY=1.5 intactos |
| 5 | Persistencia: restaura, respeita toggle, descarta hash divergente | CONTEXT (D-...-1) | Auto | PASS | exit 0; 4 testes de nome exato pass; `ShowingOriginal` no Model |
| 6 | Independente do modo: `_snippetRoots` unica fonte, 2 modos testados | CONTEXT (D-...-3) | Auto | PASS | exit 0; igualdade corpo==arquivo re-executada POS-diff (os hunks do round — CSS ~186-232, fit ~249-277, resize ~665, `_originalParagraphText` ~1073 — nao intersectam `_snippetRoots`) |
| 7 | 3 JS congelados intocados, seletor nao duplicado, ordem de carga | CONTEXT (D-...-2,-3) | Auto | PASS | exit 0; diff vazio vs BASELINE `02a4c6c` em HEAD; zero querySelectorAll proibido (comment-stripped); snippets.js depois de translation.js |
| 8 | Gate de cobertura com 5 JS e sem `.cs` novo no app MAUI | CONTEXT (D-...-2) | Auto | PASS | exit 0; `COVERAGE_JS ... files=5`; `COVERAGE_GUARD new_app_cs=0 waived=0`; pisos 90/85 inalterados; zero WAIVER_INVALID |
| 9 | Build limpo + 2 suites verdes, piso derivado de `main`, zero teste perdido | CONTEXT (D-...-6) | Auto | PASS | exit 0; build 0 Error(s); C# 406/0/2 = 408 total, piso B+12 superado, comm -23 vazio vs `main`; JS 158/158 fail 0 skipped 0, comm vazio vs `main` E (checagem extra deste review) vs `dd5f98c` nos 6 arquivos |
| 10 | Literais visuais na spec E no JS; zero pt-BR no JS; rotulos via `setSnippetLabels` | CONTEXT (D-...-2,-4) | Auto | PASS | exit 0; 15 literais nos 2 arquivos; grep pt-BR em `snippets.js` = 0 (o botao degradado usa `_labels.translateSnip`, nao literal); 9 strings pt-BR no ReaderPage |

**Totals:** 10 items | Auto: 10 (10 PASS, 0 FAIL) | Manual: 0 pending

Nota: `.jdi/PROJECT.md` nao possui secao `## Definition of Done`; o conjunto acima (CONTEXT.md, `dod=auto_only`) e o DoD integral. Itens humanos vivem em `## Deferred to PR review` do CONTEXT — a validacao visual do round 3 pelo orquestrador (Chrome real, 3 larguras) e evidencia externa a favor, mas a conferencia em DEVICE segue deferida ao PR review por decisao da phase.

## DoD-critic (D-5 enhanced, segunda passada — mesmo reviewer, mode=dod-critic)

Re-inspecao adversarial dos 10 rows Auto/PASS contra os ARTEFATOS em HEAD `1f5688f` (nao contra os comandos):

```json
[
  {"row": 1, "hollow": false, "evidence": "PIXEL-SPEC GANHOU conteudo no round (sub-nota de degradacao) sem remover nenhum dos 14 literais — diff lido na integra; screenshots intocados"},
  {"row": 2, "hollow": false, "evidence": "32 testes ~Snippet re-executados reais (0 failed); o salto 30->32 explicado nominalmente (2 metodos novos com Snippet no FQN), nao e drift silencioso do filtro"},
  {"row": 3, "hollow": false, "evidence": "regex 1x re-contada comment-stripped APOS um round que adicionou ~90 linhas ao arquivo; _splitSentences segue fonte unica com consumidores reais (incl. _spliceSpanBackToPeriods)"},
  {"row": 4, "hollow": false, "evidence": "git diff dd5f98c..HEAD em snippets.test.js: 0 linhas contendo blob geometry — goldens byte-identicos; os 4 passam por nome exato em execucao real desta iter"},
  {"row": 5, "hollow": false, "evidence": "harness real, DOM mutado; os testes novos de _originalParagraphText USAM restoreSnippets real (nao fixtures sinteticas) — o mesmo caminho de producao do restore"},
  {"row": 6, "hollow": false, "evidence": "hunks do round nao intersectam _snippetRoots; igualdade corpo==arquivo verdadeira em HEAD por re-execucao"},
  {"row": 7, "hollow": false, "evidence": "diff dos 3 JS congelados vazio contra o BASELINE real (02a4c6c) em HEAD, re-executado 2x (gate estrutural proprio + Verify do DoD 7); ordem de carga no index.html real"},
  {"row": 8, "hollow": false, "evidence": "gate re-executado 2x com exit 0 real; SCOPE 94.81 SUBIU de 94.80 exatamente pelos 2 .cs tocados entrando 100%/94.4% no AM scope — numero coerente com o diff, nao stale"},
  {"row": 9, "hollow": false, "evidence": "408 C# reais / 158 JS reais rodados por este review; comm -23 vazio nos 2 lados. Ponto cego conhecido do comando (comm JS ve so main) fechado de novo por comm independente vs dd5f98c: zero perdido, 10 adicionados (os 10 anunciados: 8+2)"},
  {"row": 10, "hollow": false, "evidence": "diff do round lido na integra: zero literal pt-BR novo em snippets.js — o degrau icon-only de _fitPill le _labels.translateSnip em runtime; o teste do aria-label injeta o literal PELO setSnippetLabels, nao pelo JS"}
]
```

**Resultado do critic: nenhum row hollow.** Observacao de escopo (nao rebaixa nenhum row): o DoD desta phase nao possui item que force a byte-igualdade do campo `paragraph` — a prova disso vive nos 2 testes novos de regressao + na validacao externa do orquestrador, ambos fora do DoD formal. W-13 (truncamento em paragrafo com markup) nao torna nenhum row hollow porque nenhum criterio do DoD cobre esse sub-caso. O critic so aperta, nunca afrouxa — verdito mantido.

## Recommendation

Os dois defeitos do round 3 estao corrigidos pela raiz e provados por teste + execucao independente deste review: a pill nunca mais quebra linha (nowrap + max-width + degradacao medida que termina por construcao e se re-ajusta em resize reconstruindo o elemento) e o campo `paragraph` do payload `snip|` volta a ser o original limpo (fonte unica `_originalParagraphText`, prompt endurecido com delimitadores e instrucao EXCLUSIVELY, salt `snippet-prompt-v2|` invalidando SO o cache de snippet — caminho de paragrafo e paridade `9d2a73a5` intocados, ambos verificados na fonte e por execucao). Todos os numeros do SUMMARY foram reproduzidos (158/158 JS, 406+2 C#, 94.81/99.30, exit 0 no gate). Duas warnings novas menores (W-13 truncamento de contexto em paragrafo com markup — fix de 1 linha sugerido; W-14 hint sem re-fit em resize) somam-se as W-2..W-12 ja aceitas como candidatas a phase de higiene — nenhuma bloqueia. Pronto para `/jdi-ship snippet-translation`.
