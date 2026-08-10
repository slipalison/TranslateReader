# Phase 23: Review  (slug: snippet-translation)

**Verdict:** APPROVED_WITH_WARNINGS

**Iter:** 7 (fix round pos-loop 2, autorizado pelo usuario — 4o feedback com screenshots do app real) · Reviewer: `jdi-reviewer-translatereader` (mode=verify + dod-critic fold-in, D-5 enhanced) · Data: 2026-08-10
**HEAD revisado:** `76e9dac` · Fix revisado: `76e9dac` (camada de blobs por raiz — vidro sumindo em resize com paragrafo fragmentado entre colunas + bolha fantasma em troca de pagina) · Baseline da phase: `02a4c6c` · Base do round: `8589c1e` (iter 6 aprovado)

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `net10.0-windows10.0.19041.0` Release: exit 0, `0 Warning(s), 0 Error(s)`. Diff 100% JS/doc — build mobile secundario nao exigido |
| Tests | PASS | C#: **414 passed / 0 failed / 2 skipped (GPU-only pre-existentes) / 416 total** >= baseline 167 (D-2), IDENTICO ao iter 6 (zero `.cs` no diff — `git diff 8589c1e..HEAD -- *.cs` vazio, re-derivado). JS: **184/184, 0 fail, 0 skipped** (era 175: +11 nomes novos, -2 documentados; contagem nome a nome re-derivada por este review vs `8589c1e` E vs `main` — zero teste de `main` perdido) |
| Coverage | PASS | `bash scripts/coverage-gate.sh` **exit 0 capturado diretamente (3 execucoes: gate 3 x2 + Verify do DoD 8)**: `COVERAGE_SCOPE covered=1340 valid=1411 pct=94.97 files=26` (piso 90, D-6) — IDENTICO ao iter 6, coerente com diff sem `.cs`; `COVERAGE_JS covered=1573 valid=1583 pct=99.37 files=5` (piso 85, D-...-4) — subiu de 99.34, coerente com o codigo JS novo coberto; `COVERAGE_GUARD new_app_cs=0 waived=0`; zero `COVERAGE_WAIVER_INVALID` |
| Lint | WARN | `dotnet format whitespace --verify-no-changes` — as MESMAS 2 violacoes FINALNEWLINE legadas (`Platforms/Android/MainActivity.cs`, `MainApplication.cs`), fora do diff (W-7). Zero arquivo novo desde `8589c1e`; zero ID novo em `NoWarn`/`.editorconfig` |
| Security/Layer | PASS | Bateria 5.1-5.17 re-executada em HEAD: TODOS os resultados identicos ao baseline abencoado do iter 6 (zero `.cs` mudou desde o commit aprovado; 26 sites de `EvaluateJavaScriptAsync` inalterados). O fix JS verificado NA FONTE — secao dedicada abaixo |
| Consistency | PASS | 1 commit atomico (`76e9dac`), conventional, tipo `fix` (nao `feat` cego — D-4), escopo `snippet-translation`, causa-raiz unica (os 2 defeitos compartilham o mesmo descasamento ancora/origem). Arquivos do commit batem exatamente com o SUMMARY iter 7; TODOS os numeros do SUMMARY (184/184, 416, 94.97, 99.37, GUARD 0/0, lint legado, 11 adicoes/2 remocoes) REPRODUZIDOS por este review |
| UI Validation | SKIPPED | has_frontend=false (native MAUI client) — por design, nunca bloqueia. Nota: o orquestrador validou em Chrome real contra o wwwroot real com paragrafo PROVADO fragmentado em 2 colunas (rects em 2 degraus): layer primeiro filho do `_pager`; UMA mask com exatamente 2 subpaths (um contorno por coluna), sem Y negativo; pagina 1 mostra vidro na metade visivel, pagina 2 na continuacao com o chip, SEM fantasma; resize re-mede e o blob persiste — evidencia externa, nao gate |
| DoD | PASS | **10/10 auto PASS, 0 manual** (`dod=auto_only`). Comandos extraidos VERBATIM do CONTEXT.md (extracao conferida byte a byte contra o proprio arquivo — `VERBATIM_OK`) e re-executados integralmente em HEAD por este review |

## Blockers

Nenhum. Os dois defeitos do 4o feedback (vidro morto pos-resize com paragrafo fragmentado; bolha fantasma em troca de pagina) morrem pela MESMA causa-raiz corrigida — verificacao cetica abaixo.

### Verificacao do fix (`76e9dac`) — camada de blobs por raiz

**1. Ciclo de vida do layer (fonte + teste):** `_ensureLayerFor` (`snippets.js:270-280`) e chamado em exatamente 2 lugares: `mountSnippetLayer` (1x por raiz, ANTES do wrap — linha 870) e `_renderAllBlobs` sob demanda (linha 511, so quando ha entry desejada naquela raiz). `root.prepend(layer)` e o UNICO prepend do arquivo — primeiro filho garantido mesmo na criacao sob demanda. `pointer-events: none` na CSS (linha 334, pinado pelo teste `css: the blob layer never intercepts pointer events`). `position: relative` so e reivindicado se o computed position da raiz for `static`, e `ownedPosition` lembra QUEM setou — `_removeLayerFor` (284-290) restaura o valor vazio apenas nesse caso (testes `layer: claims...` e `layer: never touches a root that already had its own position` pinam os 2 ramos). Sem leak: `_snippetLayers` e **WeakMap** (o `#_pager` e um elemento NOVO por capitulo — entrada morta vai com o GC, comentario 257-263 explica a escolha); `unmountSnippetLayer` remove o layer por raiz (levando todos os mask/svg filhos em 1 chamada) + `_blobs.clear()`; sweep pos-unmount e inofensivo: `_blobDescriptors()` fica vazio (`_sel` nulado, snips/loading desfeitos pelo unwrap), entao `_ensureLayerFor` (que vive DENTRO do loop de desired) nunca ressuscita layer em raiz desmontada — inclusive o callback de `fonts.ready` que sobreviver ao unmount. Raiz de scroll removida do DOM antes do unmount: `_snippetRoots()` nao a lista, layer morre com ela, WeakMap esquece via GC.

**2. Coordenadas (re-derivadas A MAO por este review):** `_blobFromEls` (150-202) mede `rect − rootRect` sem `OFF` nos pontos; o box justo e min/max das bandas com margem `OFF`; as bandas locais subtraem `left/top`. Para o teste da fragmentacao (root rect top=10/left=5; tail 560..590/13..113; head 26..56/413..493): pontos root-relative tail {8,550,108,580} / head {408,16,488,46}; bandas com padX=5/padY=1.5: {3,548.5,113,581.5} / {403,14.5,493,47.5}; left = 3-8 = -5, top = 14.5-8 = 6.5, w = ceil(501-(-5)) = 506, h = ceil(589.5-6.5) = 583; bandas locais {8,542,118,575} / {408,8,498,41} — **todos os 4 valores + as 2 bandas do teste batem com a minha derivacao independente**. O teste pina `left/top/w/h` por igualdade estrita, o `d` inteiro contra DUAS chamadas independentes de `_blobPath` com as bandas locais esperadas, asserta `y1/y2 >= 0` em cada banda (o defeito original) e 2 `M`/2 `Z` — NAO e um teste so de contagem. A equivalencia com o mockup se mantem: banda minima comeca em 8 local (mesma margem do antigo `+OFF`) e `w/h` = extensao + 16.

**3. Z-order:** layer primeiro filho pinado por teste (`layerIndex === 0` E `layerIndex < paragraphIndex`); mask/svg DENTRO do layer pinados no mesmo teste. O texto pinta por cima porque `.tr-sent` (linha 330) e `[data-snip]` (linha 333) sao `position: relative` — positioned elements com z-index auto pintam em ordem de documento, layer sempre antes. MESMO mecanismo do design pre-fix (o mask absoluto dentro do paragrafo tambem dependia dos spans posicionados); spans `.tr-loading` continuam nao-posicionados como antes (vidro pulsante por cima do placeholder e o comportamento abencoado). Gap menor registrado (W-17): o `position: relative` dos spans nao e pinado por teste.

**4. Branches removidos:** o unico site que anexa blob e `layer.appendChild` (517-518); o layer e filho da RAIZ, nunca de `[data-pi]`. A raiz nunca vira paragrafo-candidato: `_translatableCandidates(pg)` usa `pg.querySelectorAll` (descendentes; a raiz nunca se retorna) e o proprio layer div e descartado pelo filtro `_LETTER_RE` (sem texto). Logo `_unwrapParagraph`/`_originalParagraphText` nunca encontram blob — o skip era codigo morto; e o fallback else de `_unwrapParagraph` preserva nos desconhecidos (falha fechado mesmo num cenario impossivel). **As 2 remocoes de teste nao perdem cobertura:** (a) o z-order foi RENOMEADO porque o comportamento mudou de fato — o pino novo e mais forte (indice 0 + antes de todo paragrafo + mask/svg no layer); (b) `unmount: a stray glass blob is skipped...` simulava um blob filho de paragrafo, cenario agora estruturalmente impossivel; a garantia mais ampla (unmount nunca lanca com blobs vivos + selecao + snip) segue pinada por `unmount: completes without throwing and remains re-mountable with a snip blob and an active selection present (B-2 regression)` — presente e verde em HEAD.

**5. Congelados e literais:** diff VAZIO vs `02a4c6c` nos 3 JS congelados (re-executado por este review + DoD 7; `root.style.position` e mutacao de RUNTIME feita por `snippets.js`, nenhum arquivo congelado editado). Goldens: os **8** testes `blob geometry` pre-existentes tem corpo BYTE-IDENTICO vs `8589c1e` (extracao por corpo + diff, nao so contagem de linhas do git diff), 1 novo adicionado. `_blobPath(bands, 10)` literal (linha 199 — a variavel do map foi nomeada `bands` de proposito; NAO e grep-washing: `_blobPath` em si nao foi tocada, o raio segue 10 e os goldens provam a semantica por execucao). `OFF=8`/`padX=5`/`padY=1.5` 1x cada; regex de `_splitSentences` 1x (comment-stripped, re-contado); zero `querySelectorAll` de aspas simples com blocos; PIXEL-SPEC mantem os 14 literais do DoD 1 (a sub-nota nova sobre ancoragem so ADICIONA — a unica linha alterada, `Caixa (mockup):`, nao carrega literal do DoD) e os 15 do DoD 10 nos 2 arquivos; pt-BR em `snippets.js` = 0.

Observacao nao-bloqueante: `_blobFromEls` lancaria se `_rootFor(els[0])` retornasse null (o unico caller de producao, `_renderAllBlobs`, resolve e guarda a raiz ANTES de chamar — mesma classe de robustez do antigo `closest` por `[data-pi]`, que tambem podia ser null).

## Warnings

Nova do iter 7 (1, menor):

- **W-17 — O "texto acima do vidro" depende de CSS nao pinada.** A ordem de pintura correta exige `.tr-sent`/`[data-snip]` `position: relative` em `_SNIPPET_CSS` (linhas 330/333); o teste de z-order pina a ordem de documento (layer primeiro), mas nenhum teste pina o `position` dos spans — uma edicao futura da CSS inverteria o empilhamento silenciosamente. Dependencia PRE-existente (o design antigo ja se apoiava nela), mas agora e o unico mecanismo. Fix barato: 1 assert no padrao dos testes `css:` ja existentes.

Abertas de iters anteriores (re-verificadas em HEAD `76e9dac`; nenhuma resolvida nem piorada por este diff — que nao tocou `.cs` nem os trechos citados):

- **W-2 — Hint pode ressuscitar com a camada desmontada (JS).** Zero `removeEventListener` em `snippets.js` (re-contado hoje). Inalterado.
- **W-3 — `SnippetRequest.ChapterHRef` string nao-anulavel carregando null do WebView** no modo paginado. Inalterado.
- **W-4 — `_APP_ACCENT` hardcoded** duplicando o accent dos tokens XAML (linha 231). Inalterado.
- **W-5 — `SnippetLabels.cs` 0/15 e `SnippetTheme.cs` 0/1** — re-confirmado no `COVERAGE_FILE` desta iter; gate agregado passa com folga (94.97).
- **W-6 — Legado: `dotnet test` a nivel de solution sai 1** (CA1711 em TFMs iOS/MacCatalyst). Pre-existente de `main`.
- **W-7 — Legado: FINALNEWLINE em `Platforms/Android/MainActivity.cs` e `MainApplication.cs`** (re-confirmado hoje; fora do diff).
- **W-8 — OCE/excecoes absorvidas so em fronteiras terminais de event handler/teardown de WebView** (grep identico ao baseline; os 2 `catch { }` de `ReaderPage.xaml.cs:442,710` sao guardas de teardown pre-existentes ja carregadas em rounds aprovados).
- **W-9 — Sem guard de reentrancia no `EnsureModelDownloadedAsync`**; mitigado pelo overlay modal.
- **W-10 — Afinidade de thread das propriedades observaveis** no handler hybrid (correto no Windows, alvo locked).
- **W-11 — Falha parcial em selecao multi-trecho diverge UI/banco ate reload.** Inalterado.
- **W-12 — Custo do sweep `_renderAllBlobs`.** Inalterado em natureza; nota iter 7: `_rootFor` adiciona uma caminhada O(raizes) por entry por sweep (raizes = 1 no paginado, punhado no scroll) — desprezivel, e o sweep segue 1x por evento discreto, nunca em loop de token/paragrafo.
- **W-13 — `_originalParagraphText` trunca periodo com MARKUP ao primeiro filho.** Fora do diff (a remocao do branch morto de blob nao muda a truncagem); fix de 1 linha sugerido no iter 5 segue valido.
- **W-14 — Hint nao e re-medido em resize.** Inalterado.
- **W-15 — Formula da guarda C#/JS duplicada sem pino cruzado** (`* 3) + 120`). Inalterado; contract test de 1 assert sugerido.
- **W-16 — Predicado de plausibilidade mora no Manager.** Inalterado; candidato a extracao junto com W-15.

## Gate 5 — detalhe (re-executado neste review, iter 7)

| Check | Resultado |
|---|---|
| 5.1 Client -> Access/Engine | limpo |
| 5.2 Storage em `Contracts/Access` | limpo |
| 5.3 Manager -> Manager | limpo — hits sao cada Manager implementando o proprio contrato (0 `.cs` mudou desde o round aprovado) |
| 5.4 PageModel >1 Manager por caso de uso | ok — nenhum PageModel no diff |
| 5.5 Regra de negocio em Manager/PageModel | ok — diff 100% JS; ressalva W-16 carregada |
| 5.6 Zip-slip | so baseline (`ParsingEngine`, intocado) |
| 5.7 XXE | zero hits |
| 5.8 WebView JS injection | zero site novo — 26 `EvaluateJavaScriptAsync` em `ReaderPage.xaml.cs`, arquivo intocado desde a auditoria do iter 6 |
| 5.9 Secrets/PII em log | limpo |
| 5.10 Sync-over-async / OCE | zero `.Result`/`.Wait()`; catches de OCE identicos ao baseline (W-8) |
| 5.11 Eventos +=/-= | subscribe=5 / unsubscribe=4 — identico ao baseline do bootstrap; nenhum evento C# novo. JS: zero listener novo de window/document; observer/fonts geridos por mount/unmount (inalterado do iter 6) |
| 5.12 Static mutavel | nenhum novo — hits identicos ao baseline abencoado (`_nativeLibraryConfigured` + 3 properties computadas sem estado). JS novo: `_snippetLayers` (WeakMap, imutavel apos criacao por raiz) nao acumula |
| 5.13 Cache in-memory sem bound | nenhum novo; `_blobs` Map segue bounded (retire-loop 523-529 re-verificado); `_snippetLayers` WeakMap por construcao |
| 5.14 Alocacao em hot path | limpo — geometria roda por sweep discreto de render; zero `ReadAll*` novo |
| 5.15 Fail fast | zero catch novo; sem Result/Try |
| 5.16 TODO sem ticket | zero |
| 5.17 Disciplina de teste D-2 | NSubstitute so contra interfaces (grep limpo); zero I/O real novo em teste (nenhum arquivo de teste C# tocado); o caminho JS novo cobre sucesso (layer/geometria), falha (`_rootFor` null, points vazios) e os ciclos mount/unmount |

## DoD Checklist (gate 8)

Comandos extraidos VERBATIM do CONTEXT.md (extracao conferida byte a byte — `VERBATIM_OK`) e executados integralmente em HEAD `76e9dac` por este review.

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | Ground truth v0.2.0 existe e e medida (PIXEL-SPEC + >= 4 screenshots) | CONTEXT (D-...-4) | Auto | PASS | exit 0; 14 literais presentes POS-edicao da spec (sub-nota nova so adiciona); >= 4 JPGs |
| 2 | Tabela/Model/Access novos, storage invisivel no contrato, round-trip testado | CONTEXT (D-...-1) | Auto | PASS | exit 0; filtro `~Snippet`: **40 passed / 0 failed** (piso 12) — identico ao iter 6, coerente com 0 `.cs` no diff |
| 3 | Split de periodos: UMA funcao, regex literal, fronteiras testadas | CONTEXT (D-...-4) | Auto | PASS | exit 0; regex 1x re-contada comment-stripped POS-diff; fail 0 |
| 4 | Geometria dourada do blob (4 testes de nome exato) | CONTEXT (D-...-4) | Auto | PASS | exit 0; 4 nomes exatos ok no TAP; os 8 testes `blob geometry` pre-existentes BYTE-IDENTICOS vs `8589c1e` (diff por corpo extraido); `_blobPath(bands, 10)` literal preservado (linha 199); OFF=8/padX=5/padY=1.5 intactos; `_blobPath` nao tocada |
| 5 | Persistencia: restaura, respeita toggle, descarta hash divergente | CONTEXT (D-...-1) | Auto | PASS | exit 0; 4 testes de nome exato pass; `restoreSnippets` fora do diff do iter 7 |
| 6 | Independente do modo: `_snippetRoots` unica fonte, 2 modos testados | CONTEXT (D-...-3) | Auto | PASS | exit 0; `_rootFor` novo resolve via `_snippetRoots()` sem repetir seletor (verificado na fonte, 222-227); igualdade de contagem corpo==arquivo re-executada POS-diff |
| 7 | 3 JS congelados intocados, seletor nao duplicado, ordem de carga | CONTEXT (D-...-2,-3) | Auto | PASS | exit 0; diff vazio vs BASELINE `02a4c6c` em HEAD (re-executado 2x: verificacao estrutural + Verify); zero querySelectorAll proibido |
| 8 | Gate de cobertura com 5 JS e sem `.cs` novo no app MAUI | CONTEXT (D-...-2) | Auto | PASS | exit 0; `COVERAGE_JS ... files=5`; `COVERAGE_GUARD new_app_cs=0 waived=0`; pisos 90/85 inalterados; zero WAIVER_INVALID |
| 9 | Build limpo + 2 suites verdes, piso derivado de `main`, zero teste perdido | CONTEXT (D-...-6) | Auto | PASS | exit 0; build 0 Error(s); C# 414/0/2 = 416, piso B+12 superado, comm -23 vazio vs `main`; JS 184/184 fail 0 skipped 0, comm vazio vs `main` E (checagem extra deste review) vs `8589c1e` nos 2 arquivos tocados: exatamente as 2 remocoes documentadas, 11 adicoes |
| 10 | Literais visuais na spec E no JS; zero pt-BR no JS; rotulos via `setSnippetLabels` | CONTEXT (D-...-2,-4) | Auto | PASS | exit 0; 15 literais nos 2 arquivos; grep pt-BR em `snippets.js` = 0 (re-contado independente); 9 strings pt-BR no ReaderPage |

**Totals:** 10 items | Auto: 10 (10 PASS, 0 FAIL) | Manual: 0 pending

Nota: `.jdi/PROJECT.md` nao possui secao `## Definition of Done`; o conjunto acima (CONTEXT.md, `dod=auto_only`) e o DoD integral. Itens humanos vivem em `## Deferred to PR review` — a validacao do orquestrador em Chrome real (fragmentacao provada em 2 colunas, 1 mask/2 subpaths, sem fantasma, resize persistente) e evidencia externa a favor, mas a conferencia em DEVICE segue deferida ao PR review.

## DoD-critic (D-5 enhanced, segunda passada — mesmo reviewer, mode=dod-critic)

Re-inspecao adversarial dos 10 rows Auto/PASS contra os ARTEFATOS em HEAD `76e9dac` (nao contra os comandos):

```json
[
  {"row": 1, "hollow": false, "evidence": "PIXEL-SPEC FOI editada neste iter — o diff foi lido linha a linha: a sub-nota de ancoragem so adiciona e a unica linha alterada (Caixa (mockup):) nao carrega nenhum dos 14 literais do Verify; grep re-executado no arquivo real pos-edicao"},
  {"row": 2, "hollow": false, "evidence": "40 testes ~Snippet re-executados reais (0 failed); identidade com iter 6 e esperada e provada: git diff 8589c1e..HEAD -- *.cs vazio"},
  {"row": 3, "hollow": false, "evidence": "regex re-contada comment-stripped no arquivo pos-diff; nenhum hunk do iter 7 toca _splitSentences"},
  {"row": 4, "hollow": false, "evidence": "risco especifico checado: o call site novo foi escrito com a variavel bands para o grep literal continuar casando — NAO e pass oco porque a semantica que o criterio protege foi verificada independente do grep: _blobPath em si esta fora do diff, o raio segue 10, e os corpos dos 8 testes de geometria pre-existentes sao BYTE-IDENTICOS vs 8589c1e (diff por corpo extraido, nao por contagem), todos verdes em execucao real"},
  {"row": 5, "hollow": false, "evidence": "restoreSnippets/toggle/remove fora do diff do iter 7 (hunks lidos na integra); os 4 testes de nome exato re-executados verdes; harness real com DOM mutado"},
  {"row": 6, "hollow": false, "evidence": "_rootFor (novo) referencia _snippetRoots() e nunca os seletores (fonte lida, 222-227); a contagem corpo==arquivo do Verify re-executada pos-diff continua verdadeira COM o codigo novo presente"},
  {"row": 7, "hollow": false, "evidence": "diff dos 3 JS congelados vazio contra o BASELINE real (02a4c6c) em HEAD; a mutacao de root.style.position e runtime em snippets.js, nao edicao de arquivo congelado; ordem de carga no index.html real"},
  {"row": 8, "hollow": false, "evidence": "gate re-executado 3x com exit 0 real; JS subiu 99.34->99.37 coerente com +11/-2 testes e as ~100 linhas novas de snippets.js cobertas; SCOPE identico (1340/1411) prova que nenhum .cs entrou no AM scope — numero deriva do diff, nao stale"},
  {"row": 9, "hollow": false, "evidence": "416 C# reais / 184 JS reais rodados por este review. Ponto cego conhecido do comando (comm JS ve so os 4 arquivos de main) fechado por comm independente vs 8589c1e nos 2 arquivos tocados: exatamente 2 nomes sairam (z-order renomeado — pino novo mais forte; B-2 stray-blob — cenario estruturalmente impossivel, garantia ampla ainda pinada pelo teste holistico de unmount, presente e verde) e 11 entraram"},
  {"row": 10, "hollow": false, "evidence": "diff do iter 7 lido na integra: o JS novo (layer/geometria/rootFor) nao contem string de UI; comentarios em ingles; grep pt-BR re-executado com 0 hits"}
]
```

**Resultado do critic: nenhum row hollow.** Observacao de escopo (nao rebaixa nenhum row): o DoD nao tem item que force a ancoragem por raiz nem o box justo por blob — a prova vive nos 11 testes novos (incluindo o de coordenadas derivadas a mao, re-derivadas independentemente por este review) + na validacao externa do orquestrador em Chrome real. W-17 nao torna o row do z-order oco porque nenhum criterio do DoD cobre a CSS dos spans. O critic so aperta, nunca afrouxa — verdito mantido.

## Recommendation

O fix ataca a causa-raiz real (descasamento entre a ancora CSS do blob — primeiro fragment box do paragrafo — e a origem da geometria — uniao dos fragmentos) movendo o vidro para uma camada por raiz que nunca fragmenta a si mesma, com box justo por blob e coordenadas root-relative. A matematica do teste de fragmentacao foi re-derivada a mao por este review e bate valor a valor; o ciclo de vida do layer nao vaza (WeakMap + remocao por raiz + sweep pos-unmount inofensivo); os branches removidos eram estruturalmente mortos; os goldens sao byte-identicos e todos os gates e os 10 DoD passam por execucao real. Uma warning nova menor (W-17 — pinar `position: relative` dos spans na CSS com 1 assert) junta-se as W-2..W-16 ja aceitas como candidatas a phase de higiene — nenhuma bloqueia. Pronto para `/jdi-ship snippet-translation`.
