# Phase 23: Traducao de trechos por selecao de periodos — Plan (slug: snippet-translation)

## Leia antes de comecar (obrigatorio)

1. **8 tasks, ordem fixa T-1 -> T-8, de cima para baixo.** Nao pule, nao reordene, nao junte. O
   campo `Wave` de cada task documenta quais tasks tem `files_modified` disjuntos (seguras em
   paralelo); `D-2026-08-09-snippet-translation-6` trava a EXECUCAO em sequencial.
2. **Pre-condicao de branch (bloqueante).** O DoD 9 deriva o piso de testes de `main`. Se voce
   trabalhar EM `main`, o piso cresce junto com os seus commits e o DoD 9 fica impossivel de
   passar. Rode ANTES do T-1 e so siga com `PRECONDITION_OK`:
   `git fetch origin main:main && test "$(git rev-parse --abbrev-ref HEAD)" != "main" && test "$(git rev-parse main)" = "$(git rev-parse origin/main)" && git merge-base --is-ancestor main HEAD && echo PRECONDITION_OK`
   Se falhar: `git checkout -b feat/snippet-translation main` (ou rebase) e repita. Nao mova `main`
   durante a phase.
3. **Toda medida vem de `design/v0.2.0/PIXEL-SPEC.md`** (secao citada em cada task) ou esta escrita
   literal aqui. Valor que nao existe em nenhum dos dois: NAO invente — escreva
   `BLOCKED: <o que falta>` no `SUMMARY.md` e pare a task. Os bundles HTML de 5 MB em
   `design/v0.2.0/` NAO devem ser abertos: a spec e este plano ja trazem tudo.
4. **Criterio de sucesso por task = comando bash** (Git Bash, da raiz do repo). Exit 0 = concluida.
   Exit != 0 = corrija antes de seguir. Onde o criterio disser "o comando do DoD N", copie-o
   LITERAL do `CONTEXT.md` desta phase.
5. **Commit atomico por task**, conventional, escopo `snippet-translation`, mensagem em ingles,
   citando o `D-` no corpo. Tipos: T-1 `docs`, T-2 `test`, T-3..T-8 `feat`.
6. **`node --test` sempre com `--test-reporter=tap`** (sem isso o comando nao sai 0 no Node 24 —
   `D-2026-08-01-div-paragraph-reading-6`). Nenhum teste `skip`: o DoD 9 exige `# skipped 0`.
7. **NAO FACA — global (vale nas 8 tasks):**
   - NAO editar `js/translation.js`, `js/paginated.js`, `js/scroll.js` (diff tem que ser VAZIO).
   - NAO reimplementar o seletor de blocos: use o global `_translatableCandidates(root)`.
     Proibido copiar `_CANDIDATE_SELECTOR` ou a string `'p, h1`.
   - NAO sobrescrever `window.applyTranslations` / `clearTranslations` / `getVisibleParagraphs`.
   - NAO criar nenhum `.cs` NOVO em `src/TranslateReader/` (quebra o `COVERAGE_GUARD`, exit 2).
     Modelo novo vai para `src/TranslateReader.Core/Models/`.
   - NAO copiar `bottom: 78px`/`102px` cru do mockup: use a tabela "Derivacoes", item B.
   - NAO hardcodar string pt-BR em `snippets.js` (nem em comentario) — rotulos chegam por
     `setSnippetLabels`. Comentarios de JS/C# em ingles.
   - NAO renomear nem apagar teste existente (o `comm -23` do DoD 9 e nome a nome).
   - **Armadilha do DoD 7:** o gate reprova qualquer `querySelectorAll('...')` com aspas SIMPLES
     cujo seletor contenha `p`, `h1`, `li` ou `div` — isso inclui `'[data-pi]'`, `'[data-snip]'`
     e `'.tr-pill'`. Em `snippets.js` use SEMPRE aspas duplas dentro de `querySelectorAll(...)`
     (`querySelectorAll("[data-si]")`) ou guarde a referencia do elemento em variavel.

## Goal
Selecionar 1..N periodos de um paragrafo e traduzir so eles: UX liquid-glass pixel-perfect aos
mockups v0.2.0, traducao PERSISTENTE por livro, toggle original/traducao por trecho, em paginado
E rolagem, desktop E phone.

## Locked decisions (CONTEXT.md)
D-...-1 tabela `SnippetTranslations` + ancora indice/hash com descarte silencioso; D-...-2 UI toda
no WebView, `js/snippets.js` novo, ponte por `snip|`/`snip-toggle|`/`snip-remove|`, gate de 4 -> 5
arquivos JS; D-...-3 `_snippetRoots()` como unica fonte de raiz, `translation.js` com diff vazio,
coexistencia orquestrada no C#; D-...-4 spec-first + geometria dourada + `data-idiom` + `bottom`
re-derivado; D-...-5 prompt com paragrafo de contexto, cache em `TranslationCache`, sobreposicao
destrutiva, contrato `ISnippetTranslationManager` na MESMA classe `TranslationManager`;
D-...-6 formato do plano.

## Derivacoes deste plano (o que nao estava no CONTEXT nem na spec, e por que)

| # | Derivacao | Motivo |
|---|---|---|
| A | **T-1 nao produz a spec.** `design/v0.2.0/PIXEL-SPEC.md` + 8 screenshots ja foram commitados (`f572d11`), exatamente o fallback previsto em D-...-6(2). T-1 vira gravar `BASELINE` + rodar o Verify do DoD 1 | a spec ja existe, medida em Chrome real |
| B | **`bottom` da pill/hint = px acima da borda inferior do WebView:** paginado desktop `24`; rolagem desktop `32`; paginado phone `10`; rolagem phone `32` (spec silente — derivado da regra de modo). Hint: mesma linha da pill no desktop, `+2px` no phone | no app o footer e XAML FORA do WebView, e a borda inferior do WebView JA e o topo do footer em paginado (`ReaderFooter.IsVisible = IsPaginatedMode()`) |
| C | **`position: fixed` nos DOIS idioms** para pill e hint (mantendo `left:10px; right:10px` no phone) | o mockup usa `absolute` porque o frame e um container posicionado; no app o body ROLA em modo rolagem e `absolute` sairia da tela |
| D | **Paragrafo com filhos elemento (`el.children.length > 0`) vira UM unico periodo** (`data-si="0"`), MOVENDO os childNodes para dentro do span — nunca serializando/recriando HTML | preserva `<em>`/`<a>`/`<img>` do livro (input NAO confiavel, `csharp.md` §4) sem perder a feature; o estado "1 periodo" ja tem UI no mockup (`onlySentence`) |
| E | **Hash da ancora = FNV-1a 32 bits, 8 hex minusculos**, implementado nos DOIS lados (`_snipHash` em JS, `ComputeSnippetHash` em `TranslationManager`) com vetor dourado compartilhado | o DoD 5 exige que o JS DESCARTE por hash divergente, e SHA-256 no WebView e assincrono (`SubtleCrypto`); `TranslationCache` continua com o `ComputeHash` SHA-256 existente (D-...-5) |
| F | **Formato do path do blob:** toda coordenada por `_n(v) = v.toFixed(1)`, comandos separados por UM espaco | a spec trava a GEOMETRIA (OFF/padX/padY/r/ponto medio), nao o formato de emissao; fixar `toFixed(1)` torna o teste dourado do DoD 4 escrivel a mao |
| G | **Fontes no WebView:** copiar `Phosphor.ttf`, `Inter-Regular.ttf`, `Inter-Medium.ttf` de `Resources/Fonts/` para `Resources/Raw/wwwroot/fonts/` + `@font-face` dentro do `<style>` de `snippets.js` | `ph-*` e `var(--font-body)` nao existem dentro do WebView; sem isso o icone vira caixa vazia e a pill sai em serifada |
| H | **Accent do tema chega por `setSnippetLabels`** (`theme: { bg, accent }`), via operacao nova `ISettingsManager.ResolveThemeColors(settings)` delegando ao `IThemeEngine` existente. O JS deriva `AC` (`_hexRgb`) e `darkPage` (`_luma(bg) < 0.5`) | Client nao pode chamar Engine (The Method); duplicar a tabela de temas no ReaderPage seria regra de negocio no Client. `ThemeEngine.cs` fica INTOCADO |
| I | **`data-idiom` no `document.documentElement`** (CSS `html[data-idiom="phone"]`), nao no `<body>` | "Nomes prescritos" do CONTEXT (`document.documentElement.dataset.idiom`) manda sobre a prosa da spec; o DoD 10 faz grep de `dataset.idiom` |
| J | **`window.clearSnippetSelection()`** entra na API JS (chamada pelo C# em troca de pagina/capitulo) | troca de pagina acontece dentro de `paginated.js`; hookar de fora exigiria monkey-patch, proibido por D-...-3 |
| K | **`bridge.js` ganha `window.sendRawMessage(message)`** (extraido de `_sendReady`, que vira `if (!window.sendRawMessage('ready')) setTimeout(_sendReady, 100)`) | `snippets.js` precisa do MESMO canal; duplicar a cadeia de 4 hosts violaria DRY. `bridge.js` nao esta congelado por nenhum DoD |

**Registrar no SUMMARY.md** (para o `/jdi-ship` levar aos todos): a derivacao D deixa o split por
periodo indisponivel DENTRO de paragrafos com markup inline — a evolucao (split preservando markup
no nivel de text node) e phase futura, nao debito escondido.

## Pisos do CONTEXT — conferidos contra a contagem real deste plano

- **DoD 2 (`>= 12` testes em `FullyQualifiedName~Snippet`):** VALIDO. Planejado: 10 (T-6) + 8 (T-7)
  + 3 (T-7, `PromptUtility` com "Snippet" no nome) + 4 (T-8) = **25**. Margem ~2x.
- **DoD 9 (`total >= B+12`, `B` derivado de `main`):** VALIDO. `B` medido hoje = 326 `[Fact` + 49
  `[InlineData` = **375**; o plano adiciona **>= 25** testes C#. Nenhuma correcao ao CONTEXT e
  necessaria — o piso e frouxo de proposito e continua correto. **Sinal para o reviewer:** entrega
  que fecha em `B+12..B+20` provavelmente cortou testes planejados; conferir contra esta tabela.
- **JS:** o plano adiciona ~36 testes em `test/js/snippets.test.js`, >= 4 em `harness.test.js` e
  1 em `bridge.test.js`.

## Nomes congelados (o DoD depende — nao renomear)
JS: `_splitSentences`, `_snippetRoots`, `_runsOf`, `_blobFromEls`, `_blobPath`, `_snipHash`,
`window.mountSnippetLayer`, `window.unmountSnippetLayer`, `window.setSnippetLabels`,
`window.restoreSnippets`, `window.applySnippetTranslation`, `window.setSnippetLoading`,
`window.clearSnippetSelection`. DOM: `data-pi`, `data-si`, `data-snip`
(`chapterHRef:paragraphIndex:a:b`), `document.documentElement.dataset.idiom`.
C#: `Models/SnippetTranslation.cs`, `Contracts/Access/ISnippetTranslationAccess.cs`,
`Access/SnippetTranslationAccess.cs`, `Contracts/Managers/ISnippetTranslationManager.cs`,
`IPromptUtility.BuildSnippetTranslationMessages`, `TranslationManager.ComputeSnippetHash`.
Testes: `test/js/snippets.test.js`, `test/TranslateReader.Tests/SnippetTranslationAccessTests.cs`,
`test/TranslateReader.Tests/SnippetTranslationManagerTests.cs`.

---

## Tasks

### T-1: Ancorar a baseline da phase e conferir a ground truth v0.2.0
- **Wave:** 1 · **Specialist:** jdi-doer-translatereader
- **Files modified:** `.jdi/phases/snippet-translation/BASELINE` (novo)
- **Passos:**
  1. Confirmar a pre-condicao de branch (preambulo, item 2). Sem `PRECONDITION_OK`, parar.
  2. `git rev-parse HEAD > .jdi/phases/snippet-translation/BASELINE` — ANTES de qualquer outra
     mudanca. Este arquivo e lido pelos DoD 2, 7 e 8.
  3. Rodar o Verify do **DoD 1** e conferir que `design/v0.2.0/PIXEL-SPEC.md` + 8 screenshots ja
     estao no repo. Se algum grep falhar: `BLOCKED` (a spec e pre-requisito, nao se inventa).
- **NAO FACA:** nao regenerar a spec nem as screenshots; nao abrir os bundles de 5 MB; nao gravar
  `BASELINE` depois de commitar codigo.
- **Acceptance:** `BASELINE` existe e contem um SHA de commit ancestral de `HEAD`; o comando do
  **DoD 1** sai 0.
- **Criterio de sucesso:**
  `B=.jdi/phases/snippet-translation/BASELINE; test -f "$B" && test "$(git cat-file -t "$(cat "$B")" 2>/dev/null)" = commit && git merge-base --is-ancestor "$(cat "$B")" HEAD && S=design/v0.2.0/PIXEL-SPEC.md && test -f "$S" && for k in "blur(9px) saturate(180%)" "blur(26px) saturate(190%)" "blur(20px) saturate(180%)" "border-radius: 8px" "0.1em 0.24em" "box-decoration-break" "stroke-width" "1.25" "trGlassIn" "trPulse" "rgba(28,30,48,0.58)" "rgba(28,30,48,0.6)" "bottom: 102px" "data-idiom"; do grep -qF "$k" "$S" || { echo "MISSING $k"; exit 1; }; done && test "$(ls design/v0.2.0/screenshots/*.jpg 2>/dev/null | wc -l)" -ge 4`
- **Dependencies:** none · **Test:** DoD 1 (task de ancoragem, sem teste novo)
- **Commit:** `docs(snippet-translation): T-1 record phase baseline commit`
- **Status:** completed

### T-2: Harness JS ganha `getClientRects`, `closest` e `elementFromPoint`
- **Wave:** 2 · **Specialist:** jdi-doer-translatereader
- **Files modified:** `test/js/harness.js`, `test/js/harness.test.js`
- **Passos:**
  1. `FakeElement.getClientRects()` -> `Array.isArray(this.rects) ? this.rects : [this.rect]`
     (o teste seta `el.rects = [{top,left,right,bottom,width,height}, ...]` para simular
     multi-linha; `el.rect` ja existe e serve o caso de uma linha).
  2. `FakeElement.closest(selector)` -> sobe por `this` e `parentNode` enquanto
     `nodeType === ELEMENT_NODE`, devolvendo o primeiro que casa via `parseSelector` +
     `matchesAnyPart` (reusar as funcoes que ja existem); `null` se ninguem casar. Seletor que o
     harness nao entende continua lancando `SyntaxError` — o harness falha FECHADO.
  3. `FakeDocument.elementFromPoint(x, y)` -> percorre `descendantElements(this.documentElement)`
     e devolve o ULTIMO elemento cujo `rect` (com `width > 0 && height > 0`) contem `(x, y)`
     (ultimo em ordem de documento = topo da pilha de pintura); `null` se nenhum.
  4. Um comentario de UMA linha em cada, no padrao do arquivo, dizendo POR QUE (nao o que).
  5. >= 4 testes novos em `harness.test.js`: rects multiplos vs rect unico; `closest` sobe ate o
     ancestral; `closest` devolve `null` fora da arvore; `elementFromPoint` escolhe o topo e
     devolve `null` fora de qualquer rect.
- **NAO FACA:** nao mexer em `matchDescendants`/`parseSimpleSelector`; nao fazer o harness casar
  seletor desconhecido; nao renomear teste existente.
- **Acceptance:** as 3 capacidades existem e tem teste; `# fail 0`; contagem de testes de
  `harness.test.js` >= (contagem em `main`) + 4.
- **Criterio de sucesso:**
  `H=test/js/harness.js; grep -qF 'getClientRects()' "$H" && grep -qF 'closest(selector)' "$H" && grep -qF 'elementFromPoint(' "$H" && mkdir -p TestResults && node --test --test-reporter=tap test/js/harness.test.js > TestResults/snip-t2.log 2>&1 && grep -qE '^# fail 0$' TestResults/snip-t2.log && BASE=$(git show main:test/js/harness.test.js | grep -cE '^test\(') && NOW=$(grep -cE '^test\(' test/js/harness.test.js) && test "$NOW" -ge "$((BASE+4))"`
- **Dependencies:** T-1 · **Test:** `test/js/harness.test.js`
- **Commit:** `test(snippet-translation): T-2 teach the JS harness client rects, closest and elementFromPoint`
- **Status:** completed

### T-3: `snippets.js` nasce — nucleo puro, raizes e gate de cobertura
- **Wave:** 3 · **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader/Resources/Raw/wwwroot/js/snippets.js` (novo),
  `src/TranslateReader/Resources/Raw/wwwroot/index.html`, `test/js/snippets.test.js` (novo),
  `scripts/coverage-gate.sh`
- **Spec:** PIXEL-SPEC secoes "Split de periodos" e "Blob de vidro (selecao e snip)".
- **Passos:**
  1. `index.html`: adicionar `<script src="js/snippets.js"></script>` DEPOIS da linha de
     `js/translation.js` (a ordem e checada pelo DoD 7).
  2. `scripts/coverage-gate.sh` (2 linhas, ~246 e ~262):
     `for name in bridge paginated scroll snippets translation; do` e
     `if [[ "$JS_FILES" -ne 5 ]]; then` + a mensagem `expected exactly 5 production JS files`.
     Pisos `COVERAGE_MIN:-90` e `COVERAGE_JS_MIN:-85` INALTERADOS.
  3. `function _splitSentences(text)` na coluna 0, UNICA fonte do split:
     `return String(text).split(/(?<=[.!?…]["”’»)\]]?)\s+(?=[A-ZÀ-Þ"“«'(])/).map(s => s.trim()).filter(Boolean);`
     A regex aparece UMA unica vez no arquivo inteiro.
  4. `function _runsOf(set)` — recebe indices, ordena numerico, agrupa consecutivos, devolve
     `[{ a, b }, ...]`.
  5. `function _snipHash(text)` — FNV-1a 32 bits sobre `charCodeAt`, 8 hex minusculos:
     `var h = 0x811c9dc5; for (var i = 0; i < text.length; i++) { h ^= text.charCodeAt(i); h = Math.imul(h, 0x01000193) >>> 0; } return h.toString(16).padStart(8, '0');`
  6. `function _blobPath(bands, r)` — PURO, `bands = [{x1,y1,x2,y2}, ...]` em coordenadas locais.
     (a) juncao no ponto medio: para cada par adjacente,
     `mid = (bands[i].y2 + bands[i+1].y1) / 2`, `bands[i].y2 = mid`, `bands[i+1].y1 = mid`;
     (b) por banda, `rr = Math.min(r, (x2-x1)/2, (y2-y1)/2)`; (c) emitir com `_n(v) = v.toFixed(1)`
     e comandos separados por UM espaco:
     `M x1+rr y1 L x2-rr y1 Q x2 y1 x2 y1+rr L x2 y2-rr Q x2 y2 x2-rr y2 L x1+rr y2 Q x1 y2 x1 y2-rr L x1 y1+rr Q x1 y1 x1+rr y1 Z`;
     (d) varias bandas = sub-paths concatenados com um espaco.
  7. `function _blobFromEls(els)` — `par = els[0].closest("[data-pi]")`,
     `parRect = par.getBoundingClientRect()`; junta `getClientRects()` de cada el filtrando
     `w > 1 && h > 1`; converte para local (`x = r.left - parRect.left + OFF`,
     `y = r.top - parRect.top + OFF`); ordena por `top` e depois `left`; agrupa em linha quando
     `Math.abs(L.cy - cy) < r.height * 0.6`; banda = `x1 = minX - padX`, `x2 = maxX + padX`,
     `y1 = minY - padY`, `y2 = maxY + padY`. Constantes no topo, LITERAIS:
     `var OFF = 8; var padX = 5; var padY = 1.5;`. Retorna
     `{ d: _blobPath(bands, 10), w: Math.ceil(parRect.width) + 16, h: Math.ceil(parRect.height) + 16 }`
     — a chamada tem que aparecer literalmente como `_blobPath(bands, 10)`.
  8. `function _snippetRoots()` na coluna 0 — UNICO lugar do arquivo onde as strings `_pager` e
     `chapter-content` podem aparecer: paginado ->
     `[{ root: document.getElementById('_pager'), chapterHRef: null }]`; rolagem -> um item por
     `.chapter-content` com `chapterHRef = el.dataset.chapterHref`. Branch por
     `_currentMode === 'scroll'` (global de `bridge.js`). Filtrar raiz nula. Fechar a funcao com
     `}` na coluna 0 (o DoD 6 delimita o corpo por isso).
  9. `test/js/snippets.test.js`: >= 5 testes `splitSentences: ...` (abreviacao com aspas/parenteses,
     reticencias, sem falso corte em "Dr. Silva" seguido de minuscula, string vazia, um periodo so);
     >= 3 `runsOf: ...`; >= 2 `snipHash: ...` incluindo o vetor dourado
     `const SNIP_HASH_GOLDEN = '<hex>';` para a entrada `Ela disse que sim.` (calcular rodando o
     codigo; o MESMO hex sera pinado no C# em T-7); os 4 testes de **nome exato** do DoD 4:
     `blob geometry: a single line yields one rounded band`,
     `blob geometry: two lines join at the midpoint between them`,
     `blob geometry: the radius never exceeds half the band`,
     `blob geometry: rects thinner than one pixel are ignored`;
     e os 2 do DoD 6: `root: paginated mode resolves the pager as the single root`,
     `root: scroll mode resolves one root per chapter with its own href`.
     Path dourado do 1o (igualdade caractere a caractere) para
     `_blobPath([{ x1: 0, y1: 0, x2: 100, y2: 30 }], 10)`:
     `'M 10.0 0.0 L 90.0 0.0 Q 100.0 0.0 100.0 10.0 L 100.0 20.0 Q 100.0 30.0 90.0 30.0 L 10.0 30.0 Q 0.0 30.0 0.0 20.0 L 0.0 10.0 Q 0.0 0.0 10.0 0.0 Z'`
     Do 3o (clamp do raio) para `_blobPath([{ x1: 0, y1: 0, x2: 12, y2: 8 }], 10)`:
     `'M 4.0 0.0 L 8.0 0.0 Q 12.0 0.0 12.0 4.0 L 12.0 4.0 Q 12.0 8.0 8.0 8.0 L 4.0 8.0 Q 0.0 8.0 0.0 4.0 L 0.0 4.0 Q 0.0 0.0 4.0 0.0 Z'`
     Do 2o: bandas `[{0,0,100,30},{0,34,80,64}]` -> o path contem `32.0` e NAO contem `30.0` nem
     `34.0` como borda das bandas unidas.
- **NAO FACA:** nao criar CSS nem DOM ainda (e T-4); nao citar `_pager`/`chapter-content` fora de
  `_snippetRoots`; nao mexer nos pisos 90/85 do gate; nao usar `querySelectorAll('...')` com aspas
  simples.
- **Acceptance:** os comandos dos **DoD 3**, **DoD 4** e **DoD 6** saem 0; o gate de cobertura sai
  0 reportando `files=5`.
- **Criterio de sucesso:** DoD 3 + DoD 4 + DoD 6 (copiar literais), E
  `mkdir -p TestResults && bash scripts/coverage-gate.sh > TestResults/snip-t3-gate.log 2>&1 && grep -qE '^COVERAGE_JS .*files=5$' TestResults/snip-t3-gate.log && grep -qF 'Ela disse que sim.' test/js/snippets.test.js`
- **Dependencies:** T-2 · **Test:** `test/js/snippets.test.js`
- **Commit:** `feat(snippet-translation): T-3 sentence split, blob geometry and mode-independent roots (D-2026-08-09-snippet-translation-3, -4)`
- **Status:** completed

### T-4: Camada visual — CSS literal, spans de periodo, blob, pill e hint
- **Wave:** 4 · **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader/Resources/Raw/wwwroot/js/snippets.js`,
  `src/TranslateReader/Resources/Raw/wwwroot/fonts/Phosphor.ttf` (novo, copia),
  `src/TranslateReader/Resources/Raw/wwwroot/fonts/Inter-Regular.ttf` (novo, copia),
  `src/TranslateReader/Resources/Raw/wwwroot/fonts/Inter-Medium.ttf` (novo, copia),
  `test/js/snippets.test.js`
- **Spec:** PIXEL-SPEC secoes "Span de periodo", "Blob de vidro", "Pill de selecao",
  "Hint de primeira vez", "Derivadas de posicionamento".
- **Passos:**
  1. Copiar os 3 TTF de `src/TranslateReader/Resources/Fonts/` para
     `src/TranslateReader/Resources/Raw/wwwroot/fonts/` (derivacao G). O `MauiAsset` do csproj ja
     cobre `Resources\Raw\**` recursivo — NAO editar o csproj.
  2. `mountSnippetLayer()` injeta UMA VEZ `<style id="_snipStyle">` em `document.head` com, LITERAL:
     `@font-face` de `'Phosphor'` -> `url('fonts/Phosphor.ttf') format('truetype')` e de `'Inter'`
     400/500; `.ph { font-family: 'Phosphor'; line-height: 1; font-style: normal; }` e os 7 glifos:
     `.ph-text-align-left:before{content:"\e484"}`, `.ph-minus:before{content:"\e32a"}`,
     `.ph-plus:before{content:"\e3d4"}`, `.ph-x:before{content:"\e4f6"}`,
     `.ph-cursor-text:before{content:"\e7d8"}`, `.ph-arrows-left-right:before{content:"\e0a0"}`,
     `.ph-translate:before{content:"\e4a2"}`.
  3. CSS do periodo (literal):
     `.tr-sent { position: relative; cursor: pointer; user-select: none; -webkit-user-select: none; border-radius: 8px; padding: 0.1em 0.24em; margin: 0 -0.24em; box-decoration-break: clone; -webkit-box-decoration-break: clone; }`
     `html[data-idiom="desktop"] .tr-sent { transition: background 0.22s ease; }`
     `html[data-idiom="desktop"] .tr-sent:not(.tr-on):hover { background: rgba(127,127,168,0.14); }`
  4. CSS do blob + keyframes (literal):
     `.tr-blob { position: absolute; left: -8px; top: -8px; display: block; pointer-events: none; backdrop-filter: blur(9px) saturate(180%); -webkit-backdrop-filter: blur(9px) saturate(180%); animation: trGlassIn 0.25s ease; }`
     `.tr-blob-svg { position: absolute; left: -8px; top: -8px; overflow: visible; pointer-events: none; }`
     `.tr-loading .tr-blob { animation: trPulse 1.1s ease-in-out infinite; }`
     `@keyframes trGlassIn { from { opacity: 0; transform: scale(0.985); } to { opacity: 1; transform: scale(1); } }`
     `@keyframes trPulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.45; } }`
     `@keyframes trFadeUp { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: translateY(0); } }`
     Cores por tema (derivacao H): `darkPage` ->
     `linear-gradient(180deg, rgba(255,255,255,0.18), rgba(255,255,255,0.07))`; claro/sepia ->
     `linear-gradient(180deg, rgba(AC,0.17), rgba(AC,0.07))`; `stroke = rgba(AC,0.45)` dark /
     `rgba(AC,0.34)` claro; `glow = rgba(AC,0.3)`; `<path fill="none" stroke-width="1.25"
     style="filter: drop-shadow(0 6px 16px <glow>)">`. Ate T-5 trazer `setSnippetLabels`, usar o
     default `_accentRgb` vazio -> blob sem cor de accent (NAO hardcodar um accent).
  5. CSS da pill e do hint: copiar as duas tabelas da spec (background `rgba(28,30,48,0.58)`
     desktop / `rgba(28,30,48,0.6)` phone, `backdrop-filter: blur(26px) saturate(190%)`,
     `border-radius: 999px`, box-shadows, `animation: trFadeUp 0.22s ease`, z-index 35/30 e 34/29;
     hint `blur(20px) saturate(180%)` e `trFadeUp 0.4s ease`). Variante phone via
     `html[data-idiom="phone"]`; `position: fixed` nos dois idioms (derivacao C).
     `bottom` por `_pillBottom()` (derivacao B):
     `_currentMode === 'scroll' ? 32 : (_idiom() === 'phone' ? 10 : 24)`; hint = pill (+2 no phone).
  6. Montagem: `mountSnippetLayer()` percorre `_snippetRoots()` e, por raiz,
     `_translatableCandidates(root)` com indice `pi`. Por paragrafo: pular se
     `el.dataset.original !== undefined`; setar `el.dataset.pi = pi` e
     `el.style.position = 'relative'`; se `el.children.length === 0`, quebrar
     `_splitSentences(el.textContent)` em N `<span class="tr-sent" data-si="j">` separados por um
     text node de espaco; senao (derivacao D) criar UM span `data-si="0"` e MOVER os childNodes
     para dentro dele. `unmountSnippetLayer()` desfaz: desembrulha todo `[data-si]` e `[data-snip]`
     (snip volta como text node de `data-orig`), apaga `data-pi` e o `style.position`, remove
     blobs, pill e hint. As duas sao IDEMPOTENTES.
  7. Selecao (`sel = { p, anchor, set }`, restrita a UM paragrafo): tap alterna o periodo no `set`
     (vazio -> `sel = null`); tap em periodo de outro paragrafo reinicia `{p, anchor: j, set:[j]}`;
     `pointerdown` marca `_dragging`; `pointermove` usa
     `document.elementFromPoint(e.clientX, e.clientY)` + `closest("[data-si]")` e preenche
     `anchor..j` contiguo; `pointerup` no `document` encerra; `Escape` limpa (so desktop); clique
     fora do texto limpa; `window.clearSnippetSelection()` limpa (derivacao J). Re-medir os blobs
     em `resize`.
  8. Pill e hint sao renderizados a partir de `_labels` (preenchido em T-5 por
     `setSnippetLabels`); ate la as chaves ficam vazias — NENHUMA string pt-BR no arquivo. Ordem
     dos filhos e tamanhos: secao "Pill de selecao" da spec. O hint some apos a primeira selecao e
     nao volta na sessao.
- **NAO FACA:** nao usar media query para desktop/phone (usar `data-idiom`); nao serializar
  `innerHTML` do paragrafo para reconstruir texto; nao citar `_pager`/`chapter-content` fora de
  `_snippetRoots`; nao deixar funcao sem teste (o piso de 85% de JS roda no T-8 e reprova a phase
  inteira); nao usar `querySelectorAll('...')` com aspas simples.
- **Acceptance:** `node --test test/js/` com `# fail 0` e `# skipped 0`; >= 10 testes novos em
  `snippets.test.js` (mount/unmount idempotentes, paragrafo com markup vira 1 periodo, tap, drag,
  Escape, `_pillBottom` nos 4 casos, hint some apos a 1a selecao); `snippets.js` com todos os
  literais visuais do DoD 10 e ZERO string pt-BR.
- **Criterio de sucesso:**
  `J=src/TranslateReader/Resources/Raw/wwwroot/js/snippets.js; for v in "blur(9px) saturate(180%)" "blur(26px) saturate(190%)" "blur(20px) saturate(180%)" "border-radius: 8px" "0.1em 0.24em" "0 -0.24em" "box-decoration-break: clone" "999px" "rgba(28,30,48,0.58)" "rgba(28,30,48,0.6)" "trGlassIn 0.25s" "trPulse 1.1s" "ph-translate" "ph-cursor-text" "ph-arrows-left-right"; do grep -qF "$v" "$J" || { echo "JS MISSING $v"; exit 1; }; done && test "$(grep -cE 'período|Traduzir|Toque em|Alternar|Descartar|Estender|Reduzir|seleção|tradução' "$J")" -eq 0 && grep -qF 'dataset.idiom' "$J" && for f in Phosphor Inter-Regular Inter-Medium; do test -s "src/TranslateReader/Resources/Raw/wwwroot/fonts/$f.ttf" || exit 1; done && mkdir -p TestResults && node --test --test-reporter=tap test/js/ > TestResults/snip-t4.log 2>&1 && grep -qE '^# fail 0$' TestResults/snip-t4.log && grep -qE '^# skipped 0$' TestResults/snip-t4.log`
- **Dependencies:** T-3 · **Test:** `test/js/snippets.test.js`
- **Commit:** `feat(snippet-translation): T-4 glass blob, selection pill and first-run hint (D-2026-08-09-snippet-translation-2, -4)`
- **Status:** completed

### T-5: Persistencia no JS — restore com guarda de hash, toggle, remove e envio ao C#
- **Wave:** 5 · **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader/Resources/Raw/wwwroot/js/snippets.js`,
  `src/TranslateReader/Resources/Raw/wwwroot/js/bridge.js`, `test/js/snippets.test.js`,
  `test/js/bridge.test.js`
- **Spec:** PIXEL-SPEC secoes "Snip (trecho traduzido) + chip de idioma" e "Interacao".
- **Passos:**
  1. `bridge.js` (derivacao K): extrair `window.sendRawMessage = function (message) { ... }`
     devolvendo `true`/`false` (mesma cadeia HybridWebView -> chrome.webview ->
     webkit.messageHandlers -> hybridWebViewHost, mesmo `try/catch`) e
     `function _sendReady() { if (!window.sendRawMessage('ready')) setTimeout(_sendReady, 100); }`.
     Comportamento identico ao de hoje. 1 teste novo em `bridge.test.js`
     (`sendRawMessage reports false when no host is available`); NENHUM teste existente renomeado.
  2. `window.setSnippetLabels(labels)` na coluna 0 (`^window\.setSnippetLabels = function`) guarda
     os rotulos e `labels.theme = { bg, accent }`; deriva `_accentRgb = _hexRgb(accent)` e
     `_darkPage = _luma(bg) < 0.5` (derivacao H). Chaves: `selectHint`, `extendTip`, `sentenceOne`,
     `sentenceMany`, `translateSnip`, `extendSel`, `shrinkSel`, `onlySentence`, `toggleSnip`,
     `removeSnip`, `langMap`.
  3. Chip de idioma (spec): `display: inline-flex; align-items: center; gap: 5px` (phone 4);
     `vertical-align: 0.08em; margin-left: 7px` (phone 6); `padding: 2px 8px` (phone `2px 7px`);
     `border-radius: 999px; font-family: var(--font-body); font-size: 0.6em; font-weight: 500;
     letter-spacing: 0.07em; color: <accent>; background: rgba(AC,0.13);
     box-shadow: 0 0 0 1px rgba(AC,0.38); white-space: nowrap`. Conteudo:
     `ph-arrows-left-right` 1.25em, label curto (`labels.langMap`, fallback
     `s.slice(0,2).toUpperCase()`), `ph-x` 1.15em `opacity: 0.65`. Label = idioma DESTINO quando
     mostra a traducao, ORIGEM quando mostra o original.
  4. `window.restoreSnippets(list)` — itens em camelCase vindos do C#
     (`chapterHRef, paragraphIndex, sentenceStart, sentenceEnd, originalHash, translatedText,
     showingOriginal`). Para cada: achar raiz/paragrafo por `paragraphIndex`, reconstruir o texto
     original dos periodos `a..b` e SO renderizar se `_snipHash(original) === item.originalHash`.
     Divergiu -> descarta EM SILENCIO, paragrafo intacto (D-...-1). Renderizar = trocar os spans
     `a..b` por UM `<span data-snip="chapterHRef:paragraphIndex:a:b" data-orig="<original>"
     data-trans="<traduzido>">` com o texto conforme `showingOriginal` + chip.
  5. `window.applySnippetTranslation(items)` — mesmo shape; antes de inserir, remove do DOM todo
     snip do MESMO paragrafo que intersecte `a..b` (`!(o.b < a || o.a > b)`).
     `window.setSnippetLoading(keys)` — array de chaves `chapterHRef:paragraphIndex:a:b`; marca
     `.tr-loading` (blob em `trPulse`, texto ORIGINAL, SEM chip).
  6. Envio ao C# por `window.sendRawMessage`: botao "Traduzir trecho" ->
     `'snip|' + JSON.stringify(runs)`, cada run
     `{ chapterHRef, paragraphIndex, sentenceStart, sentenceEnd, text, paragraph }` (`_runsOf`
     converte o `set`); clique no snip -> `'snip-toggle|' + JSON.stringify({..., showingOriginal})`;
     clique no `ph-x` do chip -> `'snip-remove|' + JSON.stringify({...})`. `chapterHRef` nulo
     (paginado) vai como `null` — o C# preenche com o capitulo corrente.
- **NAO FACA:** nao computar SHA-256 no JS; nao persistir texto original no banco; nao permitir
  snip aninhado/sobreposto; nao mudar o retry de `_sendReady`; nao renomear teste de `bridge.test.js`.
- **Acceptance:** o comando do **DoD 5** sai 0, com os 4 testes de nome exato:
  `restore: a snippet whose hash matches renders the translated text`,
  `restore: a snippet whose hash diverges is dropped and the paragraph is untouched`,
  `restore: a snippet saved showing the original comes back showing the original`,
  `toggle: switching a snippet swaps the text and flips the chip label`.
- **Criterio de sucesso:** o comando do **DoD 5** (copiar literal), E
  `mkdir -p TestResults && node --test --test-reporter=tap test/js/ > TestResults/snip-t5.log 2>&1 && grep -qE '^# fail 0$' TestResults/snip-t5.log && grep -qE '^# skipped 0$' TestResults/snip-t5.log && grep -qE '^window\.sendRawMessage = function' src/TranslateReader/Resources/Raw/wwwroot/js/bridge.js && for k in "snip|" "snip-toggle|" "snip-remove|"; do grep -qF "$k" src/TranslateReader/Resources/Raw/wwwroot/js/snippets.js || exit 1; done`
- **Dependencies:** T-4 · **Test:** `test/js/snippets.test.js`, `test/js/bridge.test.js`
- **Commit:** `feat(snippet-translation): T-5 snippet persistence, language chip and raw message channel (D-2026-08-09-snippet-translation-1, -2)`
- **Status:** pending

### T-6: Tabela, Model, Access e DI de `SnippetTranslations`
- **Wave:** 2 · **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Models/SnippetTranslation.cs` (novo),
  `src/TranslateReader.Core/Contracts/Access/ISnippetTranslationAccess.cs` (novo),
  `src/TranslateReader.Core/Access/SnippetTranslationAccess.cs` (novo),
  `src/TranslateReader/MauiProgram.cs`,
  `test/TranslateReader.Tests/SnippetTranslationAccessTests.cs` (novo)
- **Passos:**
  1. Model: `public sealed record SnippetTranslation(int Id, int BookId, string ChapterHRef,
     int ParagraphIndex, int SentenceStart, int SentenceEnd, string OriginalHash,
     string TranslatedText, bool ShowingOriginal, DateTime CreatedAt);`
  2. Contrato (com `<summary>`, ZERO palavra de SQL/SQLite/connection string):
     `Task<IReadOnlyList<SnippetTranslation>> FetchSnippetsAsync(int bookId, string chapterHRef);`
     `Task SaveSnippetAsync(SnippetTranslation snippet);`
     `Task RemoveSnippetAsync(int bookId, string chapterHRef, int paragraphIndex, int sentenceStart, int sentenceEnd);`
     `Task SetShowingOriginalAsync(int bookId, string chapterHRef, int paragraphIndex, int sentenceStart, int sentenceEnd, bool showingOriginal);`
     `Task RemoveSnippetsForBookAsync(int bookId);`
  3. Access no molde EXATO de `Access/TranslationCacheAccess.cs` (ctor `(string connectionString)`
     + ctor `(string, bool initializeOnStartup)`, DDL inline). Colunas, nesta ordem:
     `Id INTEGER PRIMARY KEY AUTOINCREMENT, BookId INTEGER NOT NULL, ChapterHRef TEXT NOT NULL,
     ParagraphIndex INTEGER NOT NULL, SentenceStart INTEGER NOT NULL, SentenceEnd INTEGER NOT NULL,
     OriginalHash TEXT NOT NULL, TranslatedText TEXT NOT NULL, ShowingOriginal INTEGER NOT NULL,
     CreatedAt TEXT NOT NULL`, mais a linha `UNIQUE(BookId, ChapterHRef, ParagraphIndex,
     SentenceStart, SentenceEnd)` com esse texto exato.
  4. `SaveSnippetAsync` e ATOMICO e implementa a sobreposicao destrutiva de D-...-5 numa transacao:
     `DELETE FROM SnippetTranslations WHERE BookId=$b AND ChapterHRef=$h AND ParagraphIndex=$p AND
     NOT (SentenceEnd < $a OR SentenceStart > $e)` seguido do `INSERT`. Uma linha de comentario WHY
     (a regra `!(o.b < a || o.a > b)`).
  5. `FetchSnippetsAsync` ordena por `ParagraphIndex, SentenceStart`.
  6. `MauiProgram.cs`: `services.AddSingleton<ISnippetTranslationAccess>(_ => new
     SnippetTranslationAccess(connectionString, initializeOnStartup: true));` junto dos demais Access.
  7. 10 testes em `SnippetTranslationAccessTests.cs` com `InMemoryDatabase` (copiar a forma de
     `TranslationCacheAccessTests.cs`): round-trip save+fetch; fetch filtra por capitulo; fetch
     vazio devolve lista vazia; fetch ordenado; save na MESMA ancora atualiza (UNIQUE); save apaga
     trecho sobreposto do mesmo paragrafo; save NAO apaga trecho nao-sobreposto; save NAO apaga
     sobreposto de OUTRO paragrafo; `SetShowingOriginalAsync` inverte; `RemoveSnippetAsync` remove
     so a ancora; `RemoveSnippetsForBookAsync` nao toca em outro livro.
- **NAO FACA:** nao usar EF/migration; nao expor `SqliteConnection` no contrato; nao tocar em
  `TranslationCacheAccess`; nao criar `.cs` em `src/TranslateReader/`.
- **Acceptance:** o comando do **DoD 2** sai 0.
- **Criterio de sucesso:** o comando do **DoD 2** (copiar literal), E
  `test "$(grep -cE '^\s*\[Fact' test/TranslateReader.Tests/SnippetTranslationAccessTests.cs)" -ge 10`
- **Dependencies:** T-1 · **Test:** `test/TranslateReader.Tests/SnippetTranslationAccessTests.cs`
- **Commit:** `feat(snippet-translation): T-6 snippet translations table, model and access (D-2026-08-09-snippet-translation-1)`
- **Status:** pending

### T-7: Contrato do Manager, prompt de trecho, cache e cores do tema
- **Wave:** 4 · **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Models/SnippetRequest.cs` (novo),
  `src/TranslateReader.Core/Models/SnippetToggleRequest.cs` (novo),
  `src/TranslateReader.Core/Models/SnippetRemoveRequest.cs` (novo),
  `src/TranslateReader.Core/Contracts/Managers/ISnippetTranslationManager.cs` (novo),
  `src/TranslateReader.Core/Business/Managers/TranslationManager.cs`,
  `src/TranslateReader.Core/Contracts/Utilities/IPromptUtility.cs`,
  `src/TranslateReader.Core/Utilities/PromptUtility.cs`,
  `src/TranslateReader.Core/Contracts/Managers/ISettingsManager.cs`,
  `src/TranslateReader.Core/Business/Managers/SettingsManager.cs`,
  `src/TranslateReader/MauiProgram.cs`,
  `test/TranslateReader.Tests/SnippetTranslationManagerTests.cs` (novo),
  `test/TranslateReader.Tests/PromptUtilityTests.cs`,
  `test/TranslateReader.Tests/SettingsManagerTests.cs`
- **Passos:**
  1. Modelos de payload (records `sealed` no Core, para o test project alcancar):
     `SnippetRequest(string ChapterHRef, int ParagraphIndex, int SentenceStart, int SentenceEnd, string Text, string Paragraph)`;
     `SnippetToggleRequest(string ChapterHRef, int ParagraphIndex, int SentenceStart, int SentenceEnd, bool ShowingOriginal)`;
     `SnippetRemoveRequest(string ChapterHRef, int ParagraphIndex, int SentenceStart, int SentenceEnd)`.
  2. `IPromptUtility.BuildSnippetTranslationMessages(string snippet, string paragraph,
     string sourceLanguage, string targetLanguage, string? bookTitle, string? chapterTitle)`
     -> `(string SystemMessage, string UserMessage)`. Implementar em `PromptUtility` no molde da
     operacao existente, com instrucao EXPLICITA de devolver SOMENTE a traducao do trecho, usando o
     paragrafo apenas como contexto. NAO sobrecarregar `BuildTranslationMessages` (D-...-5).
  3. `ISnippetTranslationManager` (4 operacoes, com `<summary>`):
     `Task<SnippetTranslation> TranslateSnippetAsync(int bookId, SnippetRequest request, string sourceLanguage, string targetLanguage, CancellationToken ct);`
     `Task<IReadOnlyList<SnippetTranslation>> FetchSnippetsAsync(int bookId, string chapterHRef);`
     `Task SetShowingOriginalAsync(int bookId, SnippetToggleRequest request);`
     `Task RemoveSnippetAsync(int bookId, SnippetRemoveRequest request);`
  4. `TranslationManager` passa a declarar `, ISnippetTranslationManager` e recebe
     `ISnippetTranslationAccess snippetTranslationAccess` no ctor primario.
     `TranslateSnippetAsync`: (a) `ct.ThrowIfCancellationRequested()`; (b) cache
     `hash = ComputeHash(request.Text, src, dst)` -> `translationCacheAccess.FetchTranslationAsync`;
     (c) no miss: `BuildSnippetTranslationMessages` + `translationEngine.GenerateAsync(system, user,
     TranslationTemperature, request.Text.Length * MaxTokenMultiplier, ct)` +
     `CleanTranslationOutput` + `SaveTranslationAsync`; (d) monta
     `new SnippetTranslation(0, bookId, request.ChapterHRef, request.ParagraphIndex,
     request.SentenceStart, request.SentenceEnd, ComputeSnippetHash(request.Text), translated,
     false, DateTime.UtcNow)`, `await snippetTranslationAccess.SaveSnippetAsync(...)` e retorna.
     As outras 3 operacoes so delegam ao Access.
  5. `private static string ComputeSnippetHash(string text)` no molde do `ComputeHash` existente,
     FNV-1a 32 bits (derivacao E), dentro de `unchecked`: `uint h = 2166136261u;
     foreach (var c in text) { h ^= c; h *= 16777619u; }
     return h.ToString("x8", CultureInfo.InvariantCulture);`. Uma linha WHY: o JS precisa
     reproduzir o hash para descartar ancora divergente.
  6. `ISettingsManager` ganha `ThemeColors ResolveThemeColors(ReadingSettings settings);` delegando
     ao `IThemeEngine` que `SettingsManager` ja recebe. `ThemeEngine.cs` fica INTOCADO.
  7. `MauiProgram.cs`: `services.AddTransient<ISnippetTranslationManager, TranslationManager>();`
     logo apos o registro de `ITranslationManager`.
  8. Testes (NSubstitute contra os contratos, nunca contra concretos):
     `SnippetTranslationManagerTests` (8): cache hit nao chama o engine; cache miss chama o engine
     e grava no cache; usa `BuildSnippetTranslationMessages` e NAO `BuildTranslationMessages`;
     salva com `ShowingOriginal == false`; `OriginalHash` do texto `Ela disse que sim.` ==
     `SnipHashGolden` (MESMO hex do T-3); `OperationCanceledException` propaga com `ct` cancelado;
     `SetShowingOriginalAsync` e `RemoveSnippetAsync` delegam com os argumentos certos.
     `PromptUtilityTests` (+3, com "Snippet" no nome): user message contem o trecho E o paragrafo;
     system message manda devolver so o trecho; saida difere de `BuildTranslationMessages`.
     `SettingsManagerTests` (+1): `ResolveThemeColors` devolve o accent do tema pedido.
- **NAO FACA:** nao adicionar 10a operacao em `ITranslationManager`; nao criar um segundo Manager;
  nao engolir `OperationCanceledException`; nao usar `.Result`/`.Wait()`; nao criar `.cs` em
  `src/TranslateReader/`.
- **Acceptance:** build Windows Release com `0 Error(s)`;
  `dotnet test --filter "FullyQualifiedName~Snippet"` verde com >= 20 testes; o hex dourado e o
  MESMO em `test/js/snippets.test.js` e em `SnippetTranslationManagerTests.cs`.
- **Criterio de sucesso:**
  `mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0 > TestResults/snip-t7-build.log 2>&1 && grep -qE "^ *0 Error\(s\)" TestResults/snip-t7-build.log && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~Snippet" > TestResults/snip-t7.log 2>&1 && grep -q "Passed!" TestResults/snip-t7.log && awk '/Passed!/{for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (f+0==0 && p+0>=20)?0:1}' TestResults/snip-t7.log && J=$(grep -oE "SNIP_HASH_GOLDEN[^0-9a-f]*[0-9a-f]{8}" test/js/snippets.test.js | grep -oE "[0-9a-f]{8}" | tail -1) && C=$(grep -oE "SnipHashGolden[^0-9a-f]*[0-9a-f]{8}" test/TranslateReader.Tests/SnippetTranslationManagerTests.cs | grep -oE "[0-9a-f]{8}" | tail -1) && test -n "$J" && test "$J" = "$C"`
- **Dependencies:** T-6 (Model/Access), T-3 (vetor dourado do hash)
- **Test:** `SnippetTranslationManagerTests.cs`, `PromptUtilityTests.cs`, `SettingsManagerTests.cs`
- **Commit:** `feat(snippet-translation): T-7 snippet translation manager, contextual prompt and theme colors (D-2026-08-09-snippet-translation-5)`
- **Status:** pending

### T-8: Ponte no `ReaderPage`, orquestracao com a traducao por paragrafo e fechamento dos gates
- **Wave:** 6 · **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader/Pages/ReaderPage.xaml.cs`,
  `src/TranslateReader/PageModels/ReaderPageModel.cs`,
  `src/TranslateReader/Serialization/ReaderJsonContext.cs`,
  `test/TranslateReader.Tests/HybridWebViewContractTests.cs`
- **Passos:**
  1. `ReaderJsonContext`: `[JsonSerializable]` para `SnippetRequest`, `List<SnippetRequest>`,
     `SnippetToggleRequest`, `SnippetRemoveRequest`, `List<SnippetTranslation>` e `List<string>`.
     Nunca reflexao — sempre `JsonTypeInfo`.
  2. `ReaderPageModel`: injetar `ISnippetTranslationManager` (4o parametro) e expor
     `Task<IReadOnlyList<SnippetTranslation>> TranslateSnippetsAsync(IReadOnlyList<SnippetRequest>, CancellationToken)`,
     `Task<IReadOnlyList<SnippetTranslation>> LoadSnippetsAsync(string chapterHRef)`,
     `Task SetSnippetShowingOriginalAsync(SnippetToggleRequest)`,
     `Task RemoveSnippetAsync(SnippetRemoveRequest)` e `ThemeColors CurrentThemeColors` (de
     `settingsManager.ResolveThemeColors(CurrentSettings)`). Inferencia em `Task.Run`, progresso
     por `MainThread.BeginInvokeOnMainThread`, `CancellationToken` ponta a ponta.
  3. `ReaderPage.OnHybridMessageReceived` despacha por prefixo, mantendo `"ready"` INTACTO:
     `snip|` -> desserializa `List<SnippetRequest>`, preenche `ChapterHRef` nulo com o capitulo
     corrente, chama `setSnippetLoading([...])`, `_pageModel.TranslateSnippetsAsync(...)` e devolve
     `applySnippetTranslation(<json>)`; `snip-toggle|` e `snip-remove|` -> desserializam e delegam
     ao PageModel. Excecao vira estado amigavel no PageModel;
     `OperationCanceledException` sempre flui.
  4. Injecao de C# em JS SEMPRE por `JsStr(...)` / `JsonSerializer.Serialize(..., JsonTypeInfo)` —
     conteudo de livro e input NAO confiavel (`csharp.md` §4).
  5. Ciclo de vida, apos `InjectChapterAsync`: `document.documentElement.dataset.idiom` =
     `DeviceInfo.Idiom == DeviceIdiom.Phone ? "phone" : "desktop"`; `setSnippetLabels({...})` com
     os rotulos pt-BR LITERAIS do CONTEXT ("Toque em um período; toque em outro para estender a
     seleção" / phone "Toque em um período; outro toque adiciona"; "toque em outro período para
     estender"; "período selecionado"/"períodos selecionados" (phone "período"/"períodos");
     "Traduzir trecho"/"Traduzir"; "Estender ao próximo período"; "Reduzir seleção"; "único período
     deste parágrafo"; "Alternar original / tradução"; "Descartar tradução") + `theme` (bg/accent);
     `mountSnippetLayer()`; `restoreSnippets(<json de LoadSnippetsAsync>)`.
  6. Coexistencia (D-...-3): `unmountSnippetLayer()` ANTES de `applyTranslations(...)` e
     `mountSnippetLayer()` DEPOIS de `clearTranslations()`. `clearSnippetSelection()` em toda troca
     de pagina (`GoToPageAsync`/`NextPageAsync`/`PrevPageAsync`/`GoToLastPageAsync`) e de capitulo.
     O `DisplayAlert` de `OnTranslateButtonClicked` (rolagem) fica INTACTO.
  7. `HybridWebViewContractTests` +4: `SnippetRequest`, `SnippetToggleRequest` e
     `SnippetRemoveRequest` desserializam de JSON camelCase; `SnippetTranslation` serializa em
     camelCase (`translatedText`/`showingOriginal`/`originalHash`); e um teste estrutural que le
     `snippets.js` e confere as 6 `window.*` prescritas.
  8. Rodar `dotnet format` antes do commit.
- **NAO FACA:** nao criar `.cs` novo em `src/TranslateReader/`; nao liberar a traducao por
  paragrafo no modo rolagem; nao concatenar string de livro dentro de `EvaluateJavaScriptAsync`;
  nao usar `.Result`/`.Wait()`; nao remover a linha `if (e.Message == "ready")`.
- **Acceptance:** os comandos dos **DoD 7**, **DoD 8**, **DoD 9** e **DoD 10** saem 0. Ler o LOG do
  build antes de declarar limpo (aprendizado de `cobertura-e-ci`: job verde nao prova zero warning).
- **Criterio de sucesso:** DoD 7 + DoD 8 + DoD 9 + DoD 10 (copiar literais), E
  `test -z "$(git diff --diff-filter=A --name-only "$(cat .jdi/phases/snippet-translation/BASELINE)" -- 'src/TranslateReader/*.cs')" && grep -qF 'e.Message == "ready"' src/TranslateReader/Pages/ReaderPage.xaml.cs`
- **Dependencies:** T-5, T-7
- **Test:** `test/TranslateReader.Tests/HybridWebViewContractTests.cs` + as duas suites completas
- **Commit:** `feat(snippet-translation): T-8 raw message bridge, snippet layer lifecycle and gates (D-2026-08-09-snippet-translation-2, -3)`
- **Status:** pending

---

## Execucao

- Total de tasks: **8** (limite respeitado). O CONTEXT sugeria T-1..T-10; consolidacao: T-1 virou
  ancoragem mecanica (a spec ja existe), Access+Model+DI fundidos (T-6), Manager+prompt+tema
  fundidos (T-7), JS separado em nucleo puro (T-3) / visual (T-4) / persistencia (T-5),
  ponte+gates numa task final (T-8).
- Waves: **6** — W1 `T-1`; W2 `T-2` + `T-6`; W3 `T-3`; W4 `T-4` + `T-7`; W5 `T-5`; W6 `T-8`.
  Speedup teorico 8/6 = 1,33x. Dentro de cada wave os `files_modified` sao disjuntos.
  **Execucao recomendada: sequencial T-1..T-8** (`D-2026-08-09-snippet-translation-6`); a
  numeracao ja e uma ordem topologica valida das dependencias.
- Cadeias independentes ate a integracao: JS (`T-2 -> T-3 -> T-4 -> T-5`) e C# Core
  (`T-6 -> T-7`); so se encontram no `T-8`. O unico acoplamento cruzado antes disso e o vetor
  dourado do hash (T-3 -> T-7), que e um literal de 8 caracteres.
- DoD por task: T-1 -> DoD 1; T-3 -> DoD 3, 4, 6; T-5 -> DoD 5; T-6 -> DoD 2;
  T-8 -> DoD 7, 8, 9, 10. Nenhum DoD fica orfao.

## Files modified (todas as tasks)
`.jdi/phases/snippet-translation/BASELINE`; `scripts/coverage-gate.sh`;
`src/TranslateReader/Resources/Raw/wwwroot/index.html`;
`src/TranslateReader/Resources/Raw/wwwroot/js/{snippets.js (novo), bridge.js}`;
`src/TranslateReader/Resources/Raw/wwwroot/fonts/{Phosphor,Inter-Regular,Inter-Medium}.ttf` (novos);
`src/TranslateReader/{MauiProgram.cs, Serialization/ReaderJsonContext.cs, Pages/ReaderPage.xaml.cs,
PageModels/ReaderPageModel.cs}`;
`src/TranslateReader.Core/Models/{SnippetTranslation,SnippetRequest,SnippetToggleRequest,SnippetRemoveRequest}.cs`
(novos); `src/TranslateReader.Core/Contracts/Access/ISnippetTranslationAccess.cs` (novo);
`src/TranslateReader.Core/Access/SnippetTranslationAccess.cs` (novo);
`src/TranslateReader.Core/Contracts/Managers/{ISnippetTranslationManager.cs (novo), ISettingsManager.cs}`;
`src/TranslateReader.Core/Business/Managers/{TranslationManager,SettingsManager}.cs`;
`src/TranslateReader.Core/Contracts/Utilities/IPromptUtility.cs`;
`src/TranslateReader.Core/Utilities/PromptUtility.cs`;
`test/js/{harness.js, harness.test.js, bridge.test.js, snippets.test.js (novo)}`;
`test/TranslateReader.Tests/{SnippetTranslationAccessTests.cs (novo),
SnippetTranslationManagerTests.cs (novo), PromptUtilityTests.cs, SettingsManagerTests.cs,
HybridWebViewContractTests.cs}`.

## Test requirements
- C#: `DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release`
  (xUnit + NSubstitute contra contratos, `InMemoryDatabase`, sem disco/rede). Piso derivado de
  `main` dentro do proprio comando (DoD 9) + comparacao `comm -23` nome a nome.
- JS: `node --test --test-reporter=tap test/js/` -> `# fail 0` e `# skipped 0`.
- Build: `DOTNET_CLI_UI_LANGUAGE=en dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0`
  -> `0 Error(s)` (ler o log, nao so o exit code).
- Cobertura: `bash scripts/coverage-gate.sh` -> exit 0, `COVERAGE_JS ... files=5`, pisos 90 (C# em
  escopo pos-`4285f25`) e 85 (JS) inalterados, zero `COVERAGE_WAIVER_INVALID`.
