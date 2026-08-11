# Phase 23: Review  (slug: snippet-translation)

**Verdict:** APPROVED_WITH_WARNINGS

**Iter:** 9 (fix pos-aprovacao — 6o feedback do usuario, screenshot do app real) · Reviewer: `jdi-reviewer-translatereader` (mode=verify + dod-critic fold-in, D-5 enhanced) · Data: 2026-08-11
**HEAD revisado:** `e67be75` · Fix revisado: `016ef1e` (redesign do walk: boundary CONTIDO em elemento divide via clones rasos, recursivo; so CROSSING adia) · Baseline da phase: `02a4c6c` · Base do round: `b16ba36` (iter 8 aprovado)

**Contexto:** o iter 8 fechou B-1/B-2/B-3, mas a regra "elemento atomico" adiava TODOS os boundaries internos — um paragrafo cujo corpo inteiro vive dentro de UM `<span>` (comum em EPUBs reais: Wardley Maps, exports Calibre/web) degenerava em periodo unico inselecionavel. O redesign separa as duas relacoes: boundary CRUZANDO borda de elemento adia (B-1 preservado); boundary CONTIDO divide o elemento recursivamente em clones rasos.

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `net10.0-windows10.0.19041.0` Release: exit 0, `0 Aviso(s), 0 Erro(s)`. Diff 100% JS/teste/doc |
| Tests | PASS | C#: **414 passed / 0 failed / 2 skipped (GPU-only pre-existentes) / 416 total** >= baseline 167 (D-2), IDENTICO (`git diff b16ba36..HEAD -- '*.cs'` VAZIO, re-derivado). JS: **210/210, 0 fail, 0 skipped** (era 204: +6 novos; 1 teste RENOMEADO com premissa nova por design, 1 com corpo atualizado — cobertura equivalente-ou-mais-forte, analise na secao dedicada; zero teste de `main` perdido) |
| Coverage | PASS | `bash scripts/coverage-gate.sh` **exit 0 capturado diretamente**: `COVERAGE_SCOPE covered=1340 valid=1411 pct=94.97 files=26` (piso 90, D-6) — IDENTICO, coerente com diff sem `.cs`; `COVERAGE_JS covered=1840 valid=1850 pct=99.46 files=5` (piso 85, D-...-4); `COVERAGE_GUARD new_app_cs=0 waived=0`; zero `COVERAGE_WAIVER_INVALID`. Numeros do doer (210/210, 416, 99.46, 94.97) TODOS reproduzidos |
| Lint | WARN | `dotnet format whitespace --verify-no-changes` — as MESMAS 2 violacoes FINALNEWLINE legadas (fora do diff, W-7). Zero ID novo em `NoWarn`/`.editorconfig` |
| Security/Layer | PASS | Lado C# identico ao baseline abencoado (zero `.cs`). Walk novo auditado na fonte: capability gate do B-3 preservado, clamp preservado, zero serializacao/reparse (cloneNode(false) e movimentacao de nodes), recursao bounded pelo parser — verificacao cetica abaixo |
| Consistency | PASS | 2 commits atomicos (`016ef1e` fix, `e67be75` docs), conventional, escopo `snippet-translation` (D-4). Arquivos batem com o SUMMARY; claim de mutacao ("exatamente os 4 testes de split recursivo falham no algoritmo antigo") coerente com a construcao dos asserts, conferido por leitura |
| UI Validation | SKIPPED | has_frontend=false. Nota: orquestrador re-rodou TODOS os probes em Chrome real — caso B 3 periodos com spans preservados, casos A/C byte-identicos ao pre-fix, B-1 e B-3 intactos, zero pageerror — convergente com os probes deste review |
| DoD | PASS | **10/10 auto PASS, 0 manual** (`dod=auto_only`). Comandos extraidos mecanicamente do CONTEXT.md (diff 0 linhas no round) e re-executados integralmente em HEAD `e67be75` |

## Blockers

Nenhum. Verificacao cetica do redesign abaixo — incluindo re-execucao dos 3 probes historicos deste reviewer (B-1, B-3 A/B, fallback B-2), todos verdes com as semanticas esperadas pos-redesign.

## Verificacao cetica do redesign (`016ef1e`)

**1. `_crossesElement` — semantica completa, sem terra de ninguem.** `overlaps && !fullyContained` e exatamente o complemento do split: para cada range de elemento, um match ou (a) nao overlapa (livre — irrelevante para esse range), ou (b) e integralmente contido (split legitimo), ou (c) cruza a borda (adiado — unica relacao que ainda adia, B-1). Analise de casos exaustiva por range; o match sobrevive sse NENHUM range e cruzado. **Aninhamento verificado na recursao:** boundary contido no `<em>` interno tambem e contido no `<span>` externo (range do em e subconjunto do range do span) — nao cruza nenhum, e mantido; `_distributeNodes` no nivel do em produz N pieces, o nivel do span ve `sub.pieces.length > 1` e clona A SI MESMO por piece, o nivel do paragrafo repete — **a divisao propaga por TODOS os ancestrais que contem o boundary** (re-derivado a mao E pinado pelo teste `doubly-nested`, que asserta 3 clones do span externo, 2 do em, e o encadeamento exato clone-dentro-de-span-dentro-de-periodo). Boundary contido no externo mas cruzando o interno: dropado pelo filtro (cruza o interno) — adia, correto. Consumo em ordem de documento com `state.matchIdx` compartilhado: cada recursao consome exatamente os matches do seu range antes de retornar (ranges de texto e elemento particionam o espaco achatado em cada nivel; comment tem largura zero — nenhum match e pulado nem consumido pelo no errado). Elemento SEM boundary interno: `sub.pieces.length === 1`, o resultado da recursao e descartado e o NO ORIGINAL move inteiro — e a recursao descartada nao mutou nada (sem match interno, `_consumeIntoPieces` nao executa nenhum `splitText`; push em array JS nao move DOM). Edge de boundary encostado na borda: `m.end == r.start`/`m.start == r.end` nao overlapa — livre; boundary comecando/terminando EXATAMENTE na borda interna do elemento (whitespace inicial/final do elemento) e contido — divide com um clone vazio numa ponta (cosmetico, inline, sem crash; texto dos periodos correto).

**2. Clones rasos — atributos sim, listeners nao; periodos clicaveis.** `cloneNode(false)` copia tag + atributos (incl. `data-*`) e NUNCA listeners — o `FakeElement.cloneNode` novo do harness e spec-faithful nisso (atributos via setAttribute + dataset; listeners jamais; `this.constructor` cobre a subclasse SVG) e o teste do caso B pina `class`/`data-x` em CADA clone. Clones de conteudo nunca precisaram de listener (conteudo inline de livro nunca teve); o listener de selecao vive no WRAPPER `_emptyPeriodSpan` (pointerdown adicionado a cada periodo novo, inalterado) e `_onSentPointerDown` resolve por `e.target.closest("[data-si]")` (snippets.js:779) — tap num clone descendente BORBULHA e resolve para o periodo certo no DOM real; o teste do caso B pina `tap(spans[1])` selecionando SO o periodo 2, e o Chrome real do orquestrador confirmou pill/selecao nos 3 periodos.

**3. Premissas alteradas dos 2 testes — cobertura equivalente-ou-mais-forte.** (a) O teste "boundary dentro de em e adiado" pinava o comportamento que este fix REMOVE por design (era exatamente a limitacao do 6o feedback); o renomeado pina a semantica nova com asserts mais fortes (3 periodos com textos exatos, 2 clones do em com textos exatos, encadeamento). A UNICA deferral restante (crossing) continua pinada pela regressao B-1 (`One. <em> Two words</em>` → 1 periodo, sem excecao — re-executada verde inclusive pelo probe deste reviewer). (b) O teste do cap B-2 precisava de boundary que AINDA adia — input trocado para um genuinamente cruzando (`One. <em> Two words are here</em> and more.`); o cap segue pinado (`['0','1']` estrito) e a nota sobre normalizacao do espaco duplo pelo `.trim()` do fallback e trait pre-existente documentado, nao regressao. Nenhuma garantia abencoada ficou sem pino.

**4. Explosao de nos — linear e bounded, sem necessidade de limite.** K boundaries contidos a profundidade D geram K x D clones rasos extras — LINEAR no numero de sentencas vezes a profundidade de aninhamento (nao ha efeito multiplicativo entre boundaries). Livro patologico: span com 200 frases → ~200 clones de span + 200 period spans — a MESMA ordem de grandeza que um paragrafo plain de 200 frases ja produz (200 spans); profundidade e bounded pelo parser HTML do Chromium (cap de aninhamento em 512, aplicado na propria injecao via innerHTML), entao a recursao de `_gatherElementRanges`/`_distributeNodes` nao alcanca estouro de pilha com input de livro. Custo e mount-time one-shot (evento discreto), nunca em loop de token/paragrafo — dentro da politica de hot paths do csharp.md.

**5. Probes historicos re-executados (semanticas pos-redesign corretas):** B-1 (`One. <em> Two words</em>`) → 1 periodo, sem excecao, stock E spec; B-3 A (`End. <!-- note --> Next sentence <em>y</em>.`) → monta OK, 2 periodos, "End." preservado; B-3 B → `["Intro word one.","Second half here."]`; fallback B-2 sobre o input historico → agora monta como 3 periodos (contido divide, por design), restore+remove sem `data-si` duplicado e `_rangeText(p,1,1)` correto — o cap segue funcionando sob a estrutura nova.

**6. Preservacoes do iter 8 auditadas na fonte:** capability gate `_isSplittableText` gateia o consumo de texto e o accounting de posicao nos DOIS pontos novos (`_gatherElementRanges` e `_distributeNodes` — Comment contribui zero e move inteiro, teste novo cobre comment DENTRO de elemento dividido); os dois `splitText` de `_consumeIntoPieces` seguem clampados (`Math.min(..., remaining.data.length)`); zero serializacao/reparse (cloneNode raso + movimentacao de nodes); `_topLevelElementRanges`/`_consumeTextNode` removidos sem referencia pendente (grep limpo, so mencao em comentario de teste).

**Observacao menor (nao-warning):** unmount de paragrafo com elemento DIVIDIDO restaura textContent byte-identico (pinado pelo teste novo) mas NAO a estrutura original (1 elemento vira N clones + separadores soltos fora deles — trade-off explicito, documentado no proprio nome do teste). Sem consequencia funcional: re-mount sobre a estrutura de clones produz os MESMOS periodos/indices (re-derivado — boundaries caem no texto livre entre clones), o DOM do capitulo e reconstruido a cada injecao, e a unica diferenca visual possivel e um gap de estilo em spans com background/border nos limites de periodo — ja presente no estado montado por design (separadores soltos). O teste antigo de unmount byte-identico segue pinando o caso nao-dividido.

## Verificacao adicional re-derivada

- **Regex single-source:** literal da fronteira 1x (re-contado); `_blobPath(bands, 10)` 1x; `OFF=8`/`padX=5`/`padY=1.5` 3/3; pt-BR em `snippets.js` = 0; zero `querySelectorAll` proibido (re-contado comment-stripped); aspas duplas no JS novo.
- **Congelados/goldens:** `translation.js`/`paginated.js`/`scroll.js` diff VAZIO vs `02a4c6c` em HEAD; zero linha de `blob geometry` removida no diff — goldens intactos.
- **Zero `.cs` no diff** — suite C# identica (416) por construcao e re-executada mesmo assim.
- **Contagem JS re-derivada:** 204 → 210 = +6 novos (caso B com atributos, doubly-nested, regressoes A/C, comment em elemento dividido, unmount com clones) + 1 renomeado (0 liquido) + 1 corpo atualizado; comm vazio vs `main`.

## Warnings

Nenhuma nova neste iter. Resolvida no iter 8: **W-13**. Carregadas (re-verificadas em HEAD `e67be75`; diff nao toca nenhum trecho citado):

- **W-2 — Hint pode ressuscitar com a camada desmontada (JS).** Zero `removeEventListener` (re-contado hoje). Inalterado.
- **W-3 — `SnippetRequest.ChapterHRef` string nao-anulavel carregando null do WebView** no modo paginado. Inalterado.
- **W-4 — `_APP_ACCENT` hardcoded** duplicando o accent dos tokens XAML. Inalterado.
- **W-5 — `SnippetLabels.cs` 0/15 e `SnippetTheme.cs` 0/1** — gate agregado passa com folga (94.97). Inalterado.
- **W-6 — Legado: `dotnet test` a nivel de solution sai 1** (CA1711 em TFMs iOS/MacCatalyst). Pre-existente de `main`.
- **W-7 — Legado: FINALNEWLINE em `Platforms/Android/MainActivity.cs` e `MainApplication.cs`** (re-confirmado hoje; fora do diff).
- **W-8 — OCE/excecoes absorvidas so em fronteiras terminais de event handler/teardown de WebView.** Inalterado.
- **W-9 — Sem guard de reentrancia no `EnsureModelDownloadedAsync`**; mitigado pelo overlay modal. Inalterado.
- **W-10 — Afinidade de thread das propriedades observaveis** no handler hybrid. Inalterado.
- **W-11 — Falha parcial em selecao multi-trecho diverge UI/banco ate reload.** Inalterado.
- **W-12 — Custo do sweep `_renderAllBlobs`.** Inalterado.
- **W-14 — Hint nao e re-medido em resize.** Inalterado.
- **W-15 — Formula da guarda C#/JS duplicada sem pino cruzado** (`* 3) + 120`). Inalterado.
- **W-16 — Predicado de plausibilidade mora no Manager.** Inalterado.
- **W-17 — "Texto acima do vidro" depende de CSS nao pinada** (`position: relative` de `.tr-sent`/`[data-snip]`). Inalterado.
- **W-18 — Resultado tardio pode aterrissar no paragrafo do capitulo ERRADO no modo paginado** (`_snippetCts` nao cancelado na troca de capitulo + `_findParagraph` tolerante). Pre-existente; candidata a phase de higiene.

## Gate 5 — detalhe

| Check | Resultado |
|---|---|
| 5.1-5.10, 5.14-5.16 (C#) | limpos/identicos ao baseline abencoado — zero `.cs` no diff |
| 5.11 Eventos +=/-= | subscribe=5 / unsubscribe=4 (baseline bootstrap); JS: zero listener novo de window/document; pointerdown por periodo inalterado (clones nunca carregam listener — spec de cloneNode) |
| 5.12/5.13 Static/cache | nenhum novo; `_snipOriginalNodes` segue bounded |
| 5.14 (JS) | split recursivo roda 1x por paragrafo por mount; K x D clones lineares, depth bounded pelo parser (512); zero `ReadAll*` novo |
| 5.15 Fail fast | sem catch novo, sem Result/Try; clamp e capability gate preservados na causa |
| 5.17 Disciplina de teste | NSubstitute so interfaces; zero I/O real novo. 6 regressoes novas pinam o redesign (incl. mutacao: os 4 testes de split recursivo falham no algoritmo antigo — coerencia conferida por leitura); harness `cloneNode` spec-faithful (atributos sim, listeners nao, deep opcional correto) |

## DoD Checklist (gate 8)

Comandos extraidos mecanicamente do CONTEXT.md (diff 0 linhas no round) e executados integralmente em HEAD `e67be75` por este review.

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | Ground truth v0.2.0 existe e e medida | CONTEXT (D-...-4) | Auto | PASS | exit 0; 14 literais; >= 4 JPGs; spec fora do diff |
| 2 | Tabela/Model/Access novos, storage invisivel, round-trip testado | CONTEXT (D-...-1) | Auto | PASS | exit 0; `~Snippet`: 40 passed / 0 failed — identico, 0 `.cs` no diff |
| 3 | Split de periodos: UMA funcao, regex literal, fronteiras testadas | CONTEXT (D-...-4) | Auto | PASS | exit 0; regex 1x re-contada POS-redesign (`_sentenceBoundaryMatches` segue derivando por `.source`); fail 0 |
| 4 | Geometria dourada do blob | CONTEXT (D-...-4) | Auto | PASS | exit 0; 4 nomes exatos ok; goldens intactos (zero linha `blob geometry` removida); literais preservados; `_blobPath` fora do diff |
| 5 | Persistencia: restaura, toggle, descarta hash divergente | CONTEXT (D-...-1) | Auto | PASS | exit 0; 4 nomes exatos; skip silencioso de hash divergente confirmado (fonte inalterada). Nota: snips persistidos ANTES deste fix sobre paragrafo que agora divide diferente terao hash divergente e serao pulados sem purge — mesma semantica ja abencoada no iter 8 (ancora invalida != registro podre) |
| 6 | Independente do modo: `_snippetRoots` unica fonte | CONTEXT (D-...-3) | Auto | PASS | exit 0; contagem corpo==arquivo re-executada pos-diff |
| 7 | 3 JS congelados intocados, seletor nao duplicado, ordem de carga | CONTEXT (D-...-2,-3) | Auto | PASS | exit 0; diff vazio vs BASELINE nos 3 congelados em HEAD |
| 8 | Gate de cobertura com 5 JS e sem `.cs` novo | CONTEXT (D-...-2) | Auto | PASS | exit 0; `files=5`; `GUARD 0/0`; pisos 90/85 inalterados; zero WAIVER_INVALID |
| 9 | Build limpo + 2 suites verdes, zero teste perdido | CONTEXT (D-...-6) | Auto | PASS | exit 0; build 0 Error(s); C# 416; JS 210/210 fail 0 skipped 0; comm vazio vs `main` (o renomeado nao pertencia a `main`) |
| 10 | Literais visuais na spec E no JS; zero pt-BR no JS | CONTEXT (D-...-2,-4) | Auto | PASS | exit 0; 15 literais; pt-BR 0 (re-contado); 9 strings pt-BR no ReaderPage |

**Totals:** 10 items | Auto: 10 (10 PASS, 0 FAIL) | Manual: 0 pending

Nota: `.jdi/PROJECT.md` nao possui secao `## Definition of Done`; o conjunto acima (CONTEXT.md, `dod=auto_only`) e o DoD integral. Itens humanos vivem em `## Deferred to PR review`.

## DoD-critic (D-5 enhanced, segunda passada — mesmo reviewer, mode=dod-critic)

Re-inspecao adversarial dos 10 rows Auto/PASS contra os ARTEFATOS em HEAD `e67be75`:

```json
[
  {"row": 1, "hollow": false, "evidence": "spec fora do diff; grep re-executado no arquivo real"},
  {"row": 2, "hollow": false, "evidence": "40 testes ~Snippet reais verdes; diff sem .cs"},
  {"row": 3, "hollow": false, "evidence": "regex 1x re-contada com o walk novo presente; _crossesElement/_distributeNodes nao escrevem o pattern; o criterio 'fronteiras testadas' ganhou 6 pinos novos cobrindo o caso que o 6o feedback expos"},
  {"row": 4, "hollow": false, "evidence": "goldens intactos por diff sem remocao de blob geometry; _blobPath fora do diff; 4 nomes exatos verdes em execucao real"},
  {"row": 5, "hollow": false, "evidence": "restoreSnippets intocado; 4 testes reais verdes; skip-sem-purge re-confirmado na fonte. Risco especifico checado: a mudanca de segmentacao pode invalidar hashes de snips pre-fix (paragrafos que agora dividem diferente) — comportamento coberto pelo proprio criterio ('descarta quando hash diverge'), nao pass oco"},
  {"row": 6, "hollow": false, "evidence": "contagem corpo==arquivo re-executada pos-diff"},
  {"row": 7, "hollow": false, "evidence": "diff dos 3 congelados vazio contra 02a4c6c em HEAD real"},
  {"row": 8, "hollow": false, "evidence": "gate re-executado com exit 0 real; JS 99.45->99.46 coerente com +6 testes cobrindo ~120 linhas novas; SCOPE identico (1340/1411) prova zero .cs no AM scope"},
  {"row": 9, "hollow": false, "evidence": "416 C# / 210 JS rodados por este review; o unico nome removido (teste renomeado) nasceu no iter 8, nunca existiu em main — comm -23 segue vazio por construcao E re-executado; a premissa antiga era exatamente o defeito corrigido, e a garantia remanescente (crossing adia) segue pinada pela regressao B-1"},
  {"row": 10, "hollow": false, "evidence": "JS novo lido na integra: zero string de UI; grep pt-BR 0 hits"}
]
```

**Resultado do critic: nenhum row hollow.** Verdito mantido.

## Recommendation

O redesign resolve a limitacao real do 6o feedback na causa (a relacao contido-vs-cruzando estava colapsada numa regra so) sem regredir nenhuma das conquistas dos iters anteriores: B-1 (crossing adia — probe e regressao verdes), B-2 (cap do fallback — probe verde sob a segmentacao nova), B-3 (capability gate + clamp — preservados nos dois pontos novos do walk e testados com comment dentro de elemento dividido). A recursao propaga a divisao por todos os ancestrais contendo o boundary (re-derivada a mao e pinada), clones rasos preservam atributos e nunca listeners (harness spec-faithful), periodos continuam clicaveis via bubbling + closest, e o custo e linear e bounded. Todos os gates e os 10 DoD passam por execucao real; numeros do doer reproduzidos integralmente. Trade-off documentado e aceito: unmount de elemento dividido restaura texto, nao estrutura (pinado por teste; re-mount estavel). W-2..W-12, W-14..W-18 seguem como candidatas a phase de higiene (nenhuma bloqueia; W-13 resolvida). Pronto para `/jdi-ship snippet-translation`.
