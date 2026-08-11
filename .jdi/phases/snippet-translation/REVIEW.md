# Phase 23: Review  (slug: snippet-translation)

**Verdict:** APPROVED_WITH_WARNINGS

**Iter:** 8, re-verify final (fix round pos-loop 3 — 5o feedback do usuario) · Reviewer: `jdi-reviewer-translatereader` (mode=verify + dod-critic fold-in, D-5 enhanced) · Data: 2026-08-10
**HEAD revisado:** `a52c653` · Fixes do round: `6edb678` (D-A split com markup) + `634e307`/`0b4be77` (D-B loading nunca-orfao) + `89c5fe1` (B-1) + `02be25b` (B-2) + `ce835e7` (B-3) · Baseline da phase: `02a4c6c` · Base do round: `81dd86b` (iter 7 aprovado)

**Os 3 blockers deste reviewer (B-1, B-2, B-3) estao RESOLVIDOS e verificados** — cada um com fix na fonte lido e re-derivado a mao, teste de regressao que falha com o fix revertido (discriminacao por mutacao conferida por leitura dos asserts) e os probes mecanicos deste reviewer re-executados verdes. Detalhe na secao dedicada.

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `net10.0-windows10.0.19041.0` Release: exit 0, `0 Aviso(s), 0 Erro(s)`. Diff 100% JS/teste/doc — build mobile secundario nao exigido |
| Tests | PASS | C#: **414 passed / 0 failed / 2 skipped (GPU-only pre-existentes) / 416 total** >= baseline 167 (D-2), IDENTICO (`git diff 81dd86b..HEAD -- '*.cs'` VAZIO em todos os sub-rounds). JS: **204/204, 0 fail, 0 skipped** (184 do iter 7 + 20: todos os diffs de teste do round 100% aditivos, zero teste removido vs `main` e vs cada base intermediaria) |
| Coverage | PASS | `bash scripts/coverage-gate.sh` **exit 0 capturado diretamente**: `COVERAGE_SCOPE covered=1340 valid=1411 pct=94.97 files=26` (piso 90, D-6) — IDENTICO ao iter 7 em todos os sub-rounds, coerente com diff sem `.cs`; `COVERAGE_JS covered=1792 valid=1802 pct=99.45 files=5` (piso 85, D-...-4); `COVERAGE_GUARD new_app_cs=0 waived=0`; zero `COVERAGE_WAIVER_INVALID`. Numeros do doer (204/204, 416, 99.45, 94.97) TODOS reproduzidos |
| Lint | WARN | `dotnet format whitespace --verify-no-changes` — as MESMAS 2 violacoes FINALNEWLINE legadas (`Platforms/Android/MainActivity.cs`, `MainApplication.cs`), fora do diff (W-7). Zero ID novo em `NoWarn`/`.editorconfig` |
| Security/Layer | PASS | Lado C# identico ao baseline abencoado (zero `.cs` no diff; bateria 5.1-5.17 re-executada no primeiro sub-round e re-checada aqui). B-1/B-2/B-3 fechados; classe de nodes da caminhada esgotada por capability gate — verificacao cetica abaixo |
| Consistency | PASS | Round completo: 9 commits atomicos, conventional, tipos `fix`/`docs` (D-4), escopo `snippet-translation`. Neste sub-round: `ce835e7` + `a52c653`; arquivos batem com o SUMMARY; claims de mutacao em DOIS eixos (revert do routing vs revert do clamp) coerentes com a construcao dos asserts, conferidos por leitura |
| UI Validation | SKIPPED | has_frontend=false (native MAUI client). Nota: orquestrador validou os 2 cenarios do B-3 em Chrome REAL com innerHTML parseando comments de verdade (mais fiel que o harness): cenario 1 monta sem excecao com "End." preservado, cenario 2 agrupa `["Intro word one.","Second half here."]` com o comment vivo no span, zero pageerror — evidencia externa convergente com os probes deste review |
| DoD | PASS | **10/10 auto PASS, 0 manual** (`dod=auto_only`). Comandos extraidos mecanicamente do CONTEXT.md (intocado no round inteiro) e re-executados integralmente em HEAD `a52c653` |

## Blockers

Nenhum aberto. B-1, B-2 e B-3 resolvidos — verificacao abaixo.

## Verificacao dos 3 fixes (cetica, na fonte + probes re-executados)

### B-1 (RESOLVIDO em `89c5fe1`) — `IndexSizeError` com whitespace de fronteira invadindo elemento inline

Filtro em `_wrapMarkupParagraph` agora por OVERLAP pleno (`m.end > r.start && m.start < r.end`) — re-derivado a mao: o caso original `<p>One. <em> Two words</em></p>` e descartado (1 periodo, elemento atomico); run terminando exatamente no inicio do elemento e mantida (correto — `splitText(length)` valido, pinado por teste de harness); fronteira interna segue descartada. `FakeText.splitText` spec-faithful (lanca `DOMException('IndexSizeError')` em offset > length, node nao mutado — ambos pinados) — **zero teste legado quebrou** (suite inteira verde; nenhum dependia da clamping silenciosa antiga). Regressao pina `doesNotThrow` + 1 periodo + `<em>` inteiro + "no character is lost"; com o filtro antigo o mount lanca e o teste falha. Probe deste reviewer re-executado: verde.

### B-2 (RESOLVIDO em `02be25b`) — `data-si` duplicado no fallback plain-text

`_plainPeriodSpans(text, startIndex, count)` nunca emite mais que `count = b - a + 1` spans; excedentes fundidos no ULTIMO com `join(' ')` — texto do range preservado integral (assert `spans[0].textContent === originalText` pina). TODOS os 3 call sites de `_spliceSpanBackToPeriods` na assinatura de 5 args com o `b` correto (`info.b`/`spanAnchor.b` — grep confirma zero caller antigo). Regressao com o fluxo exato (restore de sessao sem stash -> remove) assertando `['0','1']` estrito + `_rangeText(p,1,1)` correto; com o cap revertido seria `['0','1','1']`. Probe deste reviewer re-executado: `duplicate data-si: []`.

### B-3 (RESOLVIDO em `ce835e7`) — Comment node dessincronizava os offsets da caminhada

1. **`_isSplittableText(node)` (capability gate: `typeof node.splitText === 'function'`) gateia AMBOS os pontos:** o walk principal de `_wrapMarkupParagraph` (Comment move INTEIRO para o periodo aberto, custo ZERO de offset) e `_topLevelElementRanges` (Comment contribui zero para `pos`). Audit por grep: TODOS os usos de `.data`/`splitText` em `snippets.js` vivem atras do gate; `_unwrapParagraph` restaura o comment intocado (dentro do span -> flatten de volta ao top level em ordem); `_originalParagraphText` usa `span.textContent`, que exclui o comment nos dois lados (DOM spec) — coerente com o `el.textContent` original. Nenhum outro caminho assume `splitText` em nao-elemento.
2. **Clamp como defesa em profundidade, com necessidade REAL provada:** os dois `splitText` de `_consumeTextNode` clampados em `Math.min(needed, remaining.data.length)`. Re-derivado a mao: com SO o routing fix, o separador de uma fronteira fisicamente dividido entre dois Text nodes irmaos em volta do comment zero-width (`"End. "` + comment + `" Next..."`) ainda produzia `splitText(2)` num node de 1 char -> `IndexSizeError` — o clamp corta `min(2,1)=1` e a metade restante do separador segue como leading space do periodo seguinte (texto integral preservado; cosmetico e auto-consistente para hash/range). **O clamp NAO mascara erro legitimo de contabilidade:** sem comment, posicoes == texto achatado por construcao (so Element/Text) e o `Math.min` e no-op; os testes pre-existentes de markup pinam textContent EXATO por span (`'A bold claim here.'` etc.), deferral, unmount byte-identico e o `'One.  Two words'` do B-1 — um desvio real de offset moveria texto entre spans e quebraria esses strictEqual, com ou sem clamp.
3. **Exaustao da classe por capability, nao por tipo:** `splitText` presente <=> Text ou CDATASection (ambos CONTRIBUEM seu data ao textContent do pai e sao cortaveis — consumidos corretamente); ausente + sem `tagName` <=> Comment em DOM parseado de HTML (PI vira comment no parser; Doctype nao pode ser filho de `<p>`; CDATA em conteudo HTML vira comment, e num contexto foreign/XML real CDATASection HERDA splitText de Text — cai do lado certo do gate). Um no exotico hipotetico degrada para "move inteiro, offset zero" — exatamente o que `textContent` do pai faz com ele. Seguro por construcao.
4. **Regressoes discriminantes (mutacao em 2 eixos, conferida por leitura):** teste 1 (comment entre dois runs de texto) falha com SO o clamp revertido (`doesNotThrow` -> `IndexSizeError`); teste 2 (comment apos elemento) falha com SO o routing revertido (`'Intro word one.'` viraria `'Intro'` — o mis-grouping exato do probe B deste reviewer). Construidos por chamadas DOM diretas + `createComment`/`FakeComment` novo no harness (spec-faithful: `.data`, sem `tagName`, sem `splitText`, zero contribuicao ao textContent do pai via `collectText`) — necessario porque o parser HTML do harness nao entende sintaxe de comment.
5. **Probes deste reviewer re-executados (stand-in de Comment INDEPENDENTE do FakeComment novo — dupla confirmacao):** cenario A monta OK com 2 periodos e "End." preservado; cenario B agrupa `["Intro word one.","Second half here."]` — identico ao Chrome real do orquestrador.

Observacao menor (nao-warning): paragrafo SEM filho elemento cai em `_wrapPlainParagraph`, que reconstroi de `textContent` — um comment ali e dropado permanentemente (unmount nao o restaura). Invisivel e sem efeito funcional; trait pre-existente do caminho plain desde antes do iter 8.

## Verificacao adicional re-derivada neste sub-round

- **Regex single-source:** literal da fronteira 1x (re-contado); `_blobPath(bands, 10)` 1x; `OFF=8`/`padX=5`/`padY=1.5` 3/3; pt-BR em `snippets.js` = 0; JS novo com aspas duplas; zero `querySelectorAll` proibido (DoD 7).
- **Congelados/goldens:** `translation.js`/`paginated.js`/`scroll.js` diff VAZIO vs `02a4c6c` em HEAD; diff de `snippets.test.js` do sub-round com ZERO linha removida — goldens e testes pre-existentes byte-identicos por construcao.
- **Zero `.cs` no round inteiro** — suite C# identica (416) por construcao e re-executada mesmo assim.

## Warnings

Nenhuma nova neste re-verify final. Resolvida no iter 8: **W-13** (`_originalParagraphText` truncava periodo com markup — fonte + teste nomeado re-verificados em HEAD). Carregadas (re-verificadas em HEAD `a52c653`; nenhum trecho citado tocado pelo diff):

- **W-2 — Hint pode ressuscitar com a camada desmontada (JS).** Zero `removeEventListener` em `snippets.js` (re-contado hoje). Inalterado.
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
- **W-18 — Resultado tardio pode aterrissar no paragrafo do capitulo ERRADO no modo paginado** (`_snippetCts` nao cancelado na troca de capitulo + `_findParagraph` tolerante; display transitorio errado, linha persistida correta). Pre-existente; candidata a phase de higiene junto com as demais.

## Gate 5 — detalhe

| Check | Resultado |
|---|---|
| 5.1-5.10, 5.14-5.16 (C#) | limpos/identicos ao baseline abencoado — zero `.cs` no diff do round |
| 5.11 Eventos +=/-= | subscribe=5 / unsubscribe=4 (baseline bootstrap); JS: zero listener novo de window/document |
| 5.12/5.13 Static/cache | nenhum novo; `_snipOriginalNodes` bounded (consume + clear pinados por teste) |
| 5.14 (JS) | split roda 1x por paragrafo por mount; `Math.min` no hot path do walk e trivial; zero `ReadAll*` novo |
| 5.15 Fail fast | crash do B-1/B-3 eliminado NA CAUSA (offsets corretos + clamp), nao com catch — sem catch novo, sem Result/Try |
| 5.17 Disciplina de teste | NSubstitute so interfaces; zero I/O real novo. Harness agora spec-faithful em `splitText` E modela Comment — as 2 lacunas de fidelidade que mascaravam B-1/B-3 fechadas; 6 regressoes novas no round pinam exatamente os 3 blockers |

## DoD Checklist (gate 8)

Comandos extraidos mecanicamente do CONTEXT.md (intocado no round) e executados integralmente em HEAD `a52c653` por este review.

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | Ground truth v0.2.0 existe e e medida | CONTEXT (D-...-4) | Auto | PASS | exit 0; 14 literais; >= 4 JPGs; spec fora do diff |
| 2 | Tabela/Model/Access novos, storage invisivel, round-trip testado | CONTEXT (D-...-1) | Auto | PASS | exit 0; `~Snippet`: 40 passed / 0 failed — identico, 0 `.cs` no diff |
| 3 | Split de periodos: UMA funcao, regex literal, fronteiras testadas | CONTEXT (D-...-4) | Auto | PASS | exit 0; regex 1x re-contada POS-fix B-3; fail 0 |
| 4 | Geometria dourada do blob | CONTEXT (D-...-4) | Auto | PASS | exit 0; 4 nomes exatos ok; goldens intactos (diffs aditivos em todo o round); literais preservados |
| 5 | Persistencia: restaura, toggle, descarta hash divergente | CONTEXT (D-...-1) | Auto | PASS | exit 0; 4 nomes exatos; skip silencioso de hash divergente confirmado na fonte (purge exclusivo do guard de plausibilidade) |
| 6 | Independente do modo: `_snippetRoots` unica fonte | CONTEXT (D-...-3) | Auto | PASS | exit 0; contagem corpo==arquivo re-executada pos-diff |
| 7 | 3 JS congelados intocados, seletor nao duplicado, ordem de carga | CONTEXT (D-...-2,-3) | Auto | PASS | exit 0; diff vazio vs BASELINE nos 3 congelados em HEAD |
| 8 | Gate de cobertura com 5 JS e sem `.cs` novo | CONTEXT (D-...-2) | Auto | PASS | exit 0; `files=5`; `GUARD 0/0`; pisos 90/85 inalterados; zero WAIVER_INVALID |
| 9 | Build limpo + 2 suites verdes, zero teste perdido | CONTEXT (D-...-6) | Auto | PASS | exit 0; build 0 Error(s); C# 416; JS 204/204 fail 0 skipped 0; comm vazio vs `main`; diffs de teste 100% aditivos em todo o round |
| 10 | Literais visuais na spec E no JS; zero pt-BR no JS | CONTEXT (D-...-2,-4) | Auto | PASS | exit 0; 15 literais; pt-BR 0 (re-contado); 9 strings pt-BR no ReaderPage |

**Totals:** 10 items | Auto: 10 (10 PASS, 0 FAIL) | Manual: 0 pending

Nota: `.jdi/PROJECT.md` nao possui secao `## Definition of Done`; o conjunto acima (CONTEXT.md, `dod=auto_only`) e o DoD integral. Itens humanos vivem em `## Deferred to PR review` (paridade visual em device, blur, drag em toque, qualidade da traducao, posicao da pill, custo de sliders, SonarCloud pos-push).

## DoD-critic (D-5 enhanced, segunda passada — mesmo reviewer, mode=dod-critic)

Re-inspecao adversarial dos 10 rows Auto/PASS contra os ARTEFATOS em HEAD `a52c653`:

```json
[
  {"row": 1, "hollow": false, "evidence": "spec fora do diff do round; grep re-executado no arquivo real"},
  {"row": 2, "hollow": false, "evidence": "40 testes ~Snippet reais verdes; diff sem .cs em todo o round"},
  {"row": 3, "hollow": false, "evidence": "regex 1x re-contada com routing+clamp presentes; nenhuma segunda escrita do pattern. As lacunas que os criterios nao cobriam (whitespace cruzando node->elemento; Comment nodes) foram fechadas por B-1/B-3 e agora TEM pino proprio (6 regressoes) — o row deixou de ter blind spot conhecido"},
  {"row": 4, "hollow": false, "evidence": "goldens intactos por diffs aditivos com zero remocao em todo o round; _blobPath fora do diff; 4 nomes exatos verdes em execucao real"},
  {"row": 5, "hollow": false, "evidence": "restoreSnippets intocado desde o iter 7; 4 testes reais verdes; skip-sem-purge re-confirmado na fonte"},
  {"row": 6, "hollow": false, "evidence": "contagem corpo==arquivo re-executada pos-diff"},
  {"row": 7, "hollow": false, "evidence": "diff dos 3 congelados vazio contra 02a4c6c em HEAD real"},
  {"row": 8, "hollow": false, "evidence": "gate re-executado com exit 0 real em cada sub-round; JS 99.43->99.44->99.45 coerente com +4/+2 testes; SCOPE identico (1340/1411) prova zero .cs no AM scope"},
  {"row": 9, "hollow": false, "evidence": "416 C# / 204 JS rodados por este review; diffs de teste puramente aditivos re-verificados em cada sub-round; FakeText spec-faithful + FakeComment nao quebraram nenhum legado (suite inteira verde)"},
  {"row": 10, "hollow": false, "evidence": "JS novo lido na integra nos 3 sub-rounds: zero string de UI; grep pt-BR 0 hits"}
]
```

**Resultado do critic: nenhum row hollow.** Verdito mantido.

## Recommendation

O round fechou completo: derivacao D entregue (split de periodos preservando markup, com undo restaurando o `<em>` original via stash), D-B entregue (loading nunca-orfao com matching por ancora e semantica de href simetrico-loose correta), e os 3 blockers deste reviewer mortos NA CAUSA — B-1 (overlap filter), B-2 (cap do fallback) e B-3 (capability gate `_isSplittableText` + clamp de defesa em profundidade, ambos com necessidade provada por mutacao em eixos independentes). A classe de nodes da caminhada esta esgotada por construcao (gate por capability, nao por tipo: qualquer no exotico degrada para "move inteiro, offset zero", espelhando o proprio textContent). O harness saiu do round estruturalmente melhor: spec-faithful em `splitText` e modelando Comment — as duas lacunas de fidelidade que mascaravam os crashes nao existem mais. Todos os gates e os 10 DoD passam por execucao real; os numeros do doer foram reproduzidos integralmente. W-2..W-12, W-14..W-18 seguem como candidatas a phase de higiene (nenhuma bloqueia; W-13 resolvida). Pronto para `/jdi-ship snippet-translation`.
