# Phase 23: Review  (slug: snippet-translation)

**Verdict:** APPROVED_WITH_WARNINGS

**Iter:** 4 (round 2, iter 2 — re-review pos-fix de B-2) · Reviewer: `jdi-reviewer-translatereader` (mode=verify + dod-critic fold-in, D-5 enhanced) · Data: 2026-08-09
**HEAD revisado:** `942800a` · Fix revisado: `a4aa004` (B-2, 100% JS+harness+testes) · Baseline da phase: `02a4c6c`

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `net10.0-windows10.0.19041.0` Release: exit 0, `0 Aviso(s), 0 Erro(s)`. Phase nao tocou `Platforms/` -> build mobile secundario nao exigido |
| Tests | PASS | C#: **404 passed / 0 failed / 2 skipped (GPU-only pre-existentes) / 406 total** >= baseline 167 (D-2), identico ao iter 3 (iteracao 100% JS). JS: **148/148, 0 skipped** (142 + 6 novos). Zero teste perdido nome a nome: `comm -23` vazio contra a suite do iter 3 (`48cae53`) nos 6 arquivos de teste JS, e vazio contra `main` no C# (DoD 9) — ambos re-executados por este review |
| Coverage | PASS | `bash scripts/coverage-gate.sh` **exit 0 capturado diretamente**: `COVERAGE_SCOPE covered=1313 valid=1385 pct=94.80 files=26` (piso 90, D-6, identico aos iters 2-3 — nenhum `.cs` tocado); `COVERAGE_JS covered=1352 valid=1362 pct=99.27 files=5` (piso 85, D-...-4, subiu de 98.89 — os testes novos exercitam `_hasClass`/`_svgEl`); `COVERAGE_GUARD new_app_cs=0 waived=0`; zero `COVERAGE_WAIVER_INVALID` |
| Lint | WARN | `dotnet format whitespace --verify-no-changes` exit 2 — as MESMAS 2 violacoes FINALNEWLINE legadas (`Platforms/Android/MainActivity.cs`, `MainApplication.cs`), fora do diff do iter 4 (que so toca JS). Arquivos da phase: limpos. Legado = WARN, nunca BLOCK (W-7) |
| Security/Layer | PASS | 5.1-5.17 re-executados: todos limpos ou identicos ao baseline abencoado (detalhe abaixo). **B-2 verificado como RESOLVIDO na fonte e por execucao — nao por auto-relato do doer** (secao dedicada abaixo) |
| Consistency | PASS | 2 commits na iter (`a4aa004` fix + `942800a` docs), conventional, escopo `snippet-translation`, tipos adequados (`fix`/`docs`, nao `feat` cego — D-4); diff real da iter = exatamente `snippets.js` + `harness.js` + `harness.test.js` + `snippets.test.js` (+SUMMARY), como declarado. SUMMARY iter 4 apendado sem apagar iters anteriores; TODAS as afirmacoes de verificacao do SUMMARY foram re-checadas por este review e batem (contagens, coverage, lint, escopo, goldens byte-identicos) |
| UI Validation | SKIPPED | has_frontend=false (native MAUI client) — por design, nunca bloqueia. Nota: o orquestrador re-rodou o harness visual em Chrome real pos-fix e os 4 cenarios de UX (vidro de selecao, pulse de loading, blob+chip persistentes, toggle) seguem renderizando — registrado como evidencia externa, nao como gate |
| DoD | PASS | **10/10 auto PASS, 0 manual** (`dod=auto_only`). Todos os `Verify:` re-executados integralmente por este review no iter 4 — incluindo DoD 4 (4 goldens por nome exato) e DoD 7 (3 JS congelados com diff vazio vs `02a4c6c` + zero querySelectorAll proibido) |

## Blockers

Nenhum. **B-2 (iter 3) esta resolvido** — verificacao cetica abaixo.

### Verificacao do fix de B-2 (`a4aa004`) — 4 eixos exigidos, todos confirmados

1. **Ordem do unmount (fonte, nao changelog):** `snippets.js:707-726` — a varredura de blobs (717-721: `mask.remove()`, `svg.remove()`, `_blobs.clear()`) completa ANTES do loop de `_unwrapParagraph` (722-724). O Map e limpo no proprio sweep, entao nao retem nos destacados; `_unwrapParagraph` tem exatamente 1 call site em producao (o loop do unmount, confirmado por grep) e nunca mais encontra um no de blob nele. O guard `_hasClass(node, 'tr-blob')` em `snippets.js:669` permanece como defesa em profundidade para um blob orfao futuro.
2. **`_hasClass` e genuinamente SVG-safe (executado, nao lido):** `snippets.js:283-286` — quando `className` e objeto E `getAttribute` existe, le `getAttribute('class')` (string identica nos 2 ambientes); quando `getAttribute` nao existe, o valor vira nao-string e o guard `typeof value === 'string'` devolve false sem lancar; quando o atributo nao existe (null), idem. Os 3 casos degenerados foram EXECUTADOS por este review contra o harness (script de reproducao proprio): `hasClass_result=true`, `no_getAttribute_result=false`, `no_class_attr_result=false` — zero throw. Grep de `className` em `snippets.js`: **zero `className.indexOf` restante** (sobram apenas atribuicoes a elementos HTML e a leitura guardada dentro do proprio `_hasClass`); os 2 outros sites antigos (`_updateSentClasses:377`, `_loadingSpanAt:989`) migraram para `_hasClass`. `_svgEl:274-275` agora chama `createElementNS` incondicionalmente (fallback morto removido).
3. **O mimic do harness reproduz o perigo REAL (ponto cego fechado, nao pintado):** este review executou o SHAPE ANTIGO do codigo (`node.className && node.className.indexOf('tr-blob')`) contra um elemento criado por `env.document.createElementNS(SVG_NS,...)` do harness novo: **lanca `TypeError: svg.className.indexOf is not a function`** — ou seja, uma regressao ao padrao antigo fica vermelha na suite, nao invisivel. `FakeSvgElement.className` e `{baseVal, animVal}` (objeto truthy, nunca reescrito para string mesmo apos `setAttribute('class',...)`, via hook `_setClassName`); `getAttribute('class')` devolve a string normal. `matches()` do harness passou a ler classes via `classTokensOf` (fail-closed preservado: sem string em nenhum dos 2 caminhos -> lista vazia -> selector de classe nao casa); zero `className.split`/`className.indexOf` cru restante em `harness.js`. 4 testes de contrato novos em `harness.test.js` pinam esse comportamento. Fidelidade residual aceita (nota, nao warning): o mimic nao reproduz o `className` read-only do SVG real (uma ATRIBUICAO `svg.className = 'x'` no harness substituiria o objeto por string em silencio) — producao nunca atribui `className` a um no SVG (grep confirmado), entao a lacuna e inerte hoje.
4. **Espiral da morte executada por nome exato:** `unmount: completes without throwing and remains re-mountable with a snip blob and an active selection present (B-2 regression)` — **pass** (rodado isolado por este review): snip restaurado + selecao ativa (2 blobs no Map), `unmountSnippetLayer()` completa, zera `[data-pi]`/`[data-si]`/`[data-snip]`/`.tr-blob`/`.tr-blob-svg`/`_blobs.size`, texto restaurado, e `mountSnippetLayer()` seguinte re-envolve o paragrafo. `unmount: a stray glass blob is skipped without throwing even though its outline is an SVG element (B-2 belt and suspenders)` — **pass** (chama `_unwrapParagraph` DIRETO com o blob ainda anexado, provando a Parte 2 independente da reordenacao). Suite completa: 148/148.

## Warnings

Abertas de iters anteriores (re-verificadas em HEAD `942800a`; o diff do iter 4 nao piorou nenhuma):

- **W-2 — Hint pode ressuscitar com a camada desmontada (JS).** Ainda aplicavel: zero `removeEventListener` em `snippets.js`; listeners de documento seguem vivos apos `unmountSnippetLayer()` e `_clearSelection` -> `_renderHint` pode re-anexar `.tr-hint` com `_hintDismissed=false`.
- **W-3 — `SnippetRequest.ChapterHRef` tipado string nao-anulavel mas carrega null do WebView** no modo paginado. Inalterado (nenhum `.cs` no diff).
- **W-4 — `_APP_ACCENT` hardcoded** (`snippets.js:163`) duplica o accent dos tokens XAML (DRY). Ainda aplicavel.
- **W-5 — Linhas novas 0% cobertas em `SnippetLabels.cs` (0/15) e `SnippetTheme.cs` (0/1)** — re-confirmado no stdout do gate desta iter (`COVERAGE_FILE`); gate agregado passa com folga (94.80).
- **W-6 — Legado: `dotnet test` a nivel de solution sai 1** (CA1711 em TFMs iOS/MacCatalyst). Pre-existente de `main`, rota = phase futura.
- **W-7 — Legado: FINALNEWLINE em `Platforms/Android/MainActivity.cs` e `MainApplication.cs`** (exit 2 re-confirmado nesta iter; arquivos fora do diff).
- **W-8 — Nota de julgamento (aceita):** OCE absorvida so na fronteira terminal de event handler (`ReaderPage.xaml.cs:115` etc.), padrao baseline; catches novos re-lancam. Grep identico ao iter 3.
- **W-9 — Fluxo de snippet sem guard de reentrancia** no `EnsureModelDownloadedAsync` (sem SemaphoreSlim); mitigado pelo overlay modal; defense-in-depth barato para phase futura.
- **W-10 — Afinidade de thread das propriedades observaveis** no handler hybrid: correto no Windows (alvo locked); marshalar via MainThread quando traducao mobile existir.
- **W-11 — Falha parcial em selecao multi-trecho diverge UI/banco ate o proximo reload.** Minor, inalterado.
- **W-12 — Custo do sweep `_renderAllBlobs`** (re-mede todos os blobs vivos a cada mutacao). Inalterado no iter 4; registrado para livros com dezenas de trechos.

Nenhuma warning nova no iter 4.

## Gate 5 — detalhe das checagens (todas re-executadas neste review, iter 4)

| Check | Resultado |
|---|---|
| 5.1 Client -> Access/Engine | limpo (grep sem output) |
| 5.2 Storage em `Contracts/Access` | limpo |
| 5.3 Manager -> Manager | limpo — hits sao cada Manager implementando o proprio contrato (`TranslationManager : ITranslationManager, ISnippetTranslationManager` = 2 contratos do MESMO servico, D-...-5) |
| 5.4 PageModel >1 Manager por caso de uso | ok — inalterado (nenhum `.cs` no diff) |
| 5.5 Regra de negocio em Manager/PageModel | ok — inalterado |
| 5.6 Zip-slip | so baseline (`ParsingEngine`, intocado) |
| 5.7 XXE | zero hits |
| 5.8 WebView JS injection | inalterado (25 sites de `EvaluateJavaScriptAsync`, nenhum `.cs` no diff; auditoria completa do iter 3 permanece valida: tudo via `JsStr(...)`/`*Json`) |
| 5.9 Secrets/PII em log | limpo |
| 5.10 Sync-over-async / OCE | zero hits de `.Result`/`.Wait()`; catches de OCE identicos ao iter 3 (terminais = padrao baseline W-8) |
| 5.11 Eventos +=/-= | subscribe=5 / unsubscribe=4 — IDENTICO ao baseline do bootstrap; iter 4 nao tocou C#. No JS, `mountSnippetLayer` re-adiciona listeners so sob `if (!_mounted)` — sem duplicacao no ciclo unmount/remount (lido na fonte) |
| 5.12 Static mutavel | nenhum novo (baseline `_nativeLibraryConfigured` + expression-bodied de `SettingsOverlay`) |
| 5.13 Cache in-memory sem bound | nenhum novo em C# (unico hit = codegen em `obj/`). O Map `_blobs` e bounded por construcao e agora o `unmountSnippetLayer` completa a limpeza tambem em engine real — a excecao que B-2 abria foi fechada |
| 5.14 Alocacao em hot path | limpo (mudanca e UI glue no WebView; ver W-12) |
| 5.15 Fail fast | catches vazios restantes (`ReaderPage.xaml.cs:442`,`:699`) sao legado ja registrado; a falha silenciosa nova que B-2 introduzia foi removida |
| 5.16 TODO sem ticket | zero |
| 5.17 Disciplina de teste D-2 | NSubstitute so contra interfaces (grep limpo); I/O real em teste so nos padroes baseline abencoados (DesignSystem/FileUtility/ModelAccess/contract tests) |

## DoD Checklist (gate 8)

Todos os `Verify:` re-executados integralmente por este review no iter 4 (comandos verbatim do CONTEXT.md, nada herdado de iters anteriores nem do self-report do doer).

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | Ground truth v0.2.0 existe e e medida (PIXEL-SPEC + >= 4 screenshots) | CONTEXT (D-...-4) | Auto | PASS | exit 0; 14 literais presentes; >= 4 JPGs |
| 2 | Tabela/Model/Access novos, storage invisivel no contrato, round-trip testado | CONTEXT (D-...-1) | Auto | PASS | exit 0; filtro `~Snippet`: **30 passed / 0 failed** (piso 12) |
| 3 | Split de periodos: UMA funcao, regex literal, fronteiras testadas | CONTEXT (D-...-4) | Auto | PASS | exit 0; regex 1x (re-contada pos-diff); testes `splitSentences` fail 0, pass >= 5 |
| 4 | Geometria dourada do blob (4 testes de nome exato) | CONTEXT (D-...-4) | Auto | PASS | exit 0; 4 nomes exatos ok no TAP; corpo dos goldens **byte-identico** no diff do iter 4 (`git diff 48cae53..HEAD` sem linha `blob geometry`); `_blobPath(bands, 10)` literal; OFF=8/padX=5/padY=1.5 intactos |
| 5 | Persistencia: restaura, respeita toggle, descarta hash divergente | CONTEXT (D-...-1) | Auto | PASS | exit 0; 4 testes de nome exato pass |
| 6 | Independente do modo: `_snippetRoots` unica fonte, 2 modos testados | CONTEXT (D-...-3) | Auto | PASS | exit 0; contagem corpo==arquivo re-feita pos-diff (hunks do iter 4 nao tocam `_snippetRoots`); 2 testes de raiz pass |
| 7 | 3 JS congelados intocados, seletor nao duplicado, ordem de carga | CONTEXT (D-...-2,-3) | Auto | PASS | exit 0; diff vazio vs BASELINE `02a4c6c` re-verificado em HEAD; zero querySelectorAll proibido; snippets.js depois de translation.js no index.html |
| 8 | Gate de cobertura com 5 JS e sem `.cs` novo no app MAUI | CONTEXT (D-...-2) | Auto | PASS | exit 0; `COVERAGE_JS ... files=5`; `COVERAGE_GUARD new_app_cs=0 waived=0`; pisos 90/85 inalterados no script; zero WAIVER_INVALID |
| 9 | Build limpo + 2 suites verdes, piso derivado de `main`, zero teste perdido | CONTEXT (D-...-6) | Auto | PASS | exit 0; build 0 Error(s); C# 404/0/2 = 406 total, piso B+12 superado, comm -23 vazio vs `main`; JS 148/148 fail 0 skipped 0, comm -23 vazio vs `main` (4 arquivos) E vs `48cae53` (6 arquivos, checagem extra deste review) |
| 10 | Literais visuais na spec E no JS; zero pt-BR no JS; rotulos via `setSnippetLabels` | CONTEXT (D-...-2,-4) | Auto | PASS | exit 0; 15 literais nos 2 arquivos; grep pt-BR no JS = 0 (comentarios novos do iter 4 em ingles); 9 strings pt-BR no ReaderPage |

**Totals:** 10 items | Auto: 10 (10 PASS, 0 FAIL) | Manual: 0 pending

Nota: `.jdi/PROJECT.md` nao possui secao `## Definition of Done`; o conjunto acima (CONTEXT.md, `dod=auto_only`) e o DoD integral. Itens humanos vivem em `## Deferred to PR review` do CONTEXT (paridade visual real, blur em device, drag em toque, qualidade linguistica, posicao da pill, custo dos sliders, SonarCloud) e seguem fora do escopo deste gate por decisao da phase.

## DoD-critic (D-5 enhanced, segunda passada — mesmo reviewer, mode=dod-critic)

Re-inspecao adversarial dos 10 rows Auto/PASS contra os ARTEFATOS em HEAD `942800a` (nao contra os comandos):

```json
[
  {"row": 1, "hollow": false, "evidence": "design/v0.2.0 intocado pelo iter 4 (diff = snippets.js + harness + testes + SUMMARY); spec e screenshots identicos aos verificados nos iters 1-3"},
  {"row": 2, "hollow": false, "evidence": "nenhum arquivo C# no diff do iter 4; 30 testes ~Snippet re-executados reais (0 failed) nesta iter"},
  {"row": 3, "hollow": false, "evidence": "regex 1x re-contada apos o diff (o iter 4 adicionou comentarios, e a contagem e comment-stripped via sed); _splitSentences segue fonte unica com consumidores reais"},
  {"row": 4, "hollow": false, "evidence": "git diff 48cae53..HEAD em snippets.test.js nao contem NENHUMA linha com blob geometry — goldens byte-identicos; os 4 passam por nome exato em execucao real desta iter; constantes conferidas na fonte"},
  {"row": 5, "hollow": false, "evidence": "harness real, DOM mutado; os 4 testes de nome exato re-executados nesta iter; ShowingOriginal presente no Model"},
  {"row": 6, "hollow": false, "evidence": "hunks do iter 4 (linhas ~267-292, ~374-380, ~654-725, ~986-992 de snippets.js) nao intersectam _snippetRoots (149-159); igualdade corpo==arquivo re-executada e verdadeira em HEAD"},
  {"row": 7, "hollow": false, "evidence": "diff dos 3 JS congelados vazio contra o BASELINE real (02a4c6c) em HEAD; ordem de carga conferida no index.html real; zero querySelectorAll proibido no arquivo comment-stripped"},
  {"row": 8, "hollow": false, "evidence": "gate re-executado 2x por este review (gate 3 direto + Verify do DoD 8) com exit 0 real ambas as vezes; SCOPE 94.80 identico aos iters 2-3 (prova cruzada de zero .cs tocado); JS 99.27 files=5; GUARD 0/0 presente no log"},
  {"row": 9, "hollow": false, "evidence": "406 C# reais / 148 JS reais rodados por este review; comm -23 vazio nos 2 lados. Nota de escopo (nao rebaixa): o comm JS do DoD 9 compara contra main, onde snippets.test.js/harness.test.js nao existem — perda DENTRO desses 2 arquivos seria invisivel ao comando; este review fechou a lacuna com um comm independente contra 48cae53 (6 arquivos): zero perdido, 6 adicionados (os 6 anunciados). O criterio declarado (nenhum teste que existe hoje em main) e genuinamente provado pelo proprio comando"},
  {"row": 10, "hollow": false, "evidence": "diff do iter 4 lido na integra: comentarios novos em ingles, zero pt-BR novo no JS; literais visuais intocados pelo diff (nenhuma linha de estilo alterada)"}
]
```

**Resultado do critic: nenhum row hollow.** O ponto cego estrutural registrado no critic do iter 3 (harness sem semantica SVG real) foi FECHADO nesta iter e agora e um contrato testado do harness (4 testes) — o shape antigo do codigo lanca TypeError contra o mimic (provado por execucao neste review). O critic so aperta, nunca afrouxa — verdito mantido.

## Recommendation

B-2 esta resolvido pela raiz e com as duas cinturas exigidas: a reordenacao do unmount elimina o alcance do defeito no unico call site existente, e `_hasClass` elimina a classe inteira de bug (`className.indexOf` sobre no arbitrario) do arquivo — com o ponto cego do harness fechado de forma que uma regressao ao padrao antigo fica vermelha na suite em vez de invisivel. Todos os numeros do SUMMARY iter 4 foram reproduzidos por este review (148/148 JS, 406 C#, 94.80/99.27, exit 0 no gate, goldens intactos). Aprovado com as warnings de sempre (W-2..W-12), todas ja aceitas como candidatas a phase de higiene futura — nenhuma piorou. Pronto para `/jdi-ship snippet-translation`. Sugestao de higiene futura (sem acao agora): incluir os testes JS novos de cada phase no proprio floor de nao-regressao do DoD (o comm do DoD 9 so enxerga `main`), como este review fez manualmente contra `48cae53`.
