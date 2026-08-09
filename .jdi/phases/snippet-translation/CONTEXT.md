# Phase 23: Traducao de trechos por selecao de periodos — Context (slug: snippet-translation)

Gerado em 2026-08-09. Brief primario = pedido do usuario + mockups novos em `design/v0.2.0/`
(2 bundles HTML de ~5 MB, base64+gzip). Os bundles JA FORAM decodificados e diffados contra
v0.1.0 nesta sessao; **todos os valores que o plano precisa estao COPIADOS abaixo** — o executor
nao precisa (e nao deve) re-extrair os bundles. Executor previsto: Sonnet (high), ver
`D-2026-08-09-snippet-translation-6`.

## Goal
O leitor seleciona um ou mais periodos (sentencas) dentro de um paragrafo e traduz apenas esses
trechos — UX liquid-glass pixel-perfect aos mockups v0.2.0, traducao PERSISTENTE por livro,
alternancia original/traducao por trecho, funcionando em modo paginado E rolagem, desktop E mobile.

## Requisitos inegociaveis do usuario
1. Trecho traduzido PERSISTE — fechar e reabrir o livro mantem os trechos visiveis.
2. O usuario alterna cada trecho entre texto original e texto traduzido.
3. Pixel-perfect vs mockups e INEGOCIAVEL.
4. Funciona em TODOS os modos de leitura: paginado E rolagem.
5. CONTEXT/PLAN montados para execucao por LLM menor (Sonnet high).

## Locked decisions
- **D-2026-08-09-snippet-translation-1** — Tabela nova `SnippetTranslations`; ancora = indice do
  paragrafo (`_translatableCandidates`) + range `a..b`; `OriginalHash` e GUARDA: hash divergente
  descarta o trecho EM SILENCIO (preferivel perder do que colar no periodo errado). Estado do
  toggle persiste (`ShowingOriginal`). `TranslationCache` segue so cache de inferencia. Contrato
  novo `ISnippetTranslationAccess`, sem SQL/SQLite vazando na interface.
- **D-2026-08-09-snippet-translation-2** — Toda a UI e DOM no WebView (unica forma de ter
  `backdrop-filter` e `clip-path` reais). Arquivo novo `js/snippets.js`, carregado DEPOIS de
  `translation.js`. `scripts/coverage-gate.sh` passa de 4 para 5 arquivos JS (2 linhas).
  Ponte pelo canal de raw message que ja existe: `snip|`, `snip-toggle|`, `snip-remove|`.
  Nenhum `.cs` NOVO em `src/TranslateReader/` (so edicao de `ReaderPage.xaml.cs`).
- **D-2026-08-09-snippet-translation-3** — Camada independente do modo via `_snippetRoots()`
  (paginado: `#_pager`; rolagem: um item por `.chapter-content` com seu `data-chapter-href`).
  `translation.js` com diff VAZIO. Coexistencia com a traducao por paragrafo orquestrada no C#
  (`unmountSnippetLayer()` antes de `applyTranslations`, `mountSnippetLayer()` depois de
  `clearTranslations`) — proibido monkey-patch. Traducao por paragrafo segue paginado-only.
- **D-2026-08-09-snippet-translation-4** — T-1 produz `design/v0.2.0/PIXEL-SPEC.md` + screenshots
  ANTES de qualquer codigo. `_blobPath` ganha teste de path SVG DOURADO (literal, caractere a
  caractere). Desktop vs mobile por `data-idiom` (nao media query — tablet usa layout desktop).
  `bottom` da pill e RE-DERIVADO (no app o footer e XAML fora do WebView).
- **D-2026-08-09-snippet-translation-5** — Prompt = trecho + paragrafo como contexto
  (`IPromptUtility.BuildSnippetTranslationMessages`, 2a operacao). Cache em `TranslationCache` com
  `OriginalHash = ComputeHash(trecho, src, dst)`. Sobreposicao e destrutiva (`!(o.b < a || o.a > b)`),
  no DOM e no banco. Contrato novo `ISnippetTranslationManager` implementado pela MESMA classe
  `TranslationManager` (2 contratos por servico; `ITranslationManager` ja tem 9 operacoes).
- **D-2026-08-09-snippet-translation-6** — Plano sequencial T-1..T-N, passos imperativos com valores
  literais, criterio de sucesso = comando bash por task, bloco "NAO FACA" por task, commit atomico,
  piso de testes DERIVADO de `main` no proprio comando + `comm -23` nome a nome.

## Canonical refs
- `design/v0.2.0/TranslateReader Desktop.html`, `design/v0.2.0/TranslateReader Mobile.html` (bundles).
- `design/v0.2.0/PIXEL-SPEC.md` + `design/v0.2.0/screenshots/` — **criados por T-1 desta phase**.
- `.jdi/decisions/D-2026-08-09-snippet-translation-1..6.md`.
- Precedentes de formato: `.jdi/phases/pixel-perfect/CONTEXT.md` (spec-first para LLM menor),
  `.jdi/phases/div-paragraph-reading/CONTEXT.md` (gates estruturais de JS, piso derivado de `main`).
- Superficies: `src/TranslateReader/Resources/Raw/wwwroot/js/snippets.js` (novo), `index.html`,
  `src/TranslateReader/Pages/ReaderPage.xaml.cs`, `PageModels/ReaderPageModel.cs`,
  `src/TranslateReader/Serialization/ReaderJsonContext.cs`, `MauiProgram.cs`,
  `src/TranslateReader.Core/{Models,Contracts/Access,Contracts/Managers,Access,Business/Managers,Utilities}`,
  `scripts/coverage-gate.sh`, `test/js/{harness,snippets.test}.js`, `test/TranslateReader.Tests/`.
- Regras: `CLAUDE.md` (camadas The Method, 1 Manager por caso de uso, contratos 3-5 ops),
  `.claude/rules/csharp.md` §1 (excecao so pra erro), §2 (alocacao em hot path), §3 (UI thread,
  sem sync-over-async), §4 (HTML de livro e input NAO confiavel), §6 (90% em codigo novo).

## Out of scope
- Conjunto `en` do i18n do mockup (o app nao tem seletor de idioma de UI) e as chaves declaradas e
  nao usadas nos dois templates: `snipTitle`, `snipLoading`, `copyTitle`, `copied`.
- Copiar a traducao do trecho para a area de transferencia (implicado por `copyTitle`/`copied`,
  ausente do HTML de ambos os mockups).
- Liberar a traducao por paragrafo no modo Rolagem (`scrollWarn` / `DisplayAlert` fica intacto).
- Selecao atravessando fronteira de paragrafo (mockup reinicia a selecao ao tocar em outro paragrafo).
- Tela de gerenciamento de trechos do livro; lazy mount de spans em rolagem.
- `design/v0.2.0/DESIGN-REFERENCE.md` (so a PIXEL-SPEC nasce aqui).
- Qualquer alteracao em `translation.js`, `paginated.js`, `scroll.js`.
Todos registrados em `.jdi/todos/2026-08-09-snippet-translation.md`.

## Medidas do mockup (ground truth ja extraida — T-1 confirma e formaliza na PIXEL-SPEC)

**Split de periodos** (identico nos dois mockups, `_splitS`):
`/(?<=[.!?…]["”’»)\]]?)\s+(?=[A-ZÀ-Þ"“«'(])/` -> `.map(s => s.trim()).filter(Boolean)`

**Span de periodo** (desktop e mobile): `position: relative; cursor: pointer; user-select: none;
-webkit-user-select: none; border-radius: 8px; padding: 0.1em 0.24em; margin: 0 -0.24em;
box-decoration-break: clone` (+ `-webkit-`). Desktop tem `transition: background 0.22s ease` e hover
`rgba(127,127,168,0.14)` (hover so quando o periodo NAO esta selecionado). Mobile nao tem hover.

**Blob de vidro** — geometria: rects via `getClientRects()`, filtrando `w>1 && h>1`; ordenacao
`top`, depois `left`; agrupamento em linha por `Math.abs(L.cy - cy) < r.height * 0.6`; constantes
`OFF = 8`, `padX = 5`, `padY = 1.5`; bandas adjacentes se encontram no ponto medio
`mid = (bands[i].y2 + bands[i+1].y1) / 2`; raio `r = 10` limitado por
`Math.min(r, (x2-x1)/2, (y2-y1)/2)`; caixa `w = ceil(parRect.width) + 16`, `h = ceil(parRect.height) + 16`;
posicao `left: -8px; top: -8px`.
Render: `<span>` com `clip-path: path('<d>')`, `backdrop-filter: blur(9px) saturate(180%)` (+`-webkit-`),
`pointer-events: none`; e `<svg>` irmao com `<path fill="none" stroke-width="1.25"
style="filter: drop-shadow(0 6px 16px <glow>)">`, `overflow: visible`.
Cores derivadas do accent do tema (`AC` = "r,g,b" do accent):
- fill dark: `linear-gradient(180deg, rgba(255,255,255,0.18), rgba(255,255,255,0.07))`
- fill claro/sepia: `linear-gradient(180deg, rgba(AC,0.17), rgba(AC,0.07))`
- stroke: `rgba(AC, 0.45)` dark / `rgba(AC, 0.34)` claro; glow: `rgba(AC, 0.3)`
Animacao: `trGlassIn 0.25s ease` ao aparecer; `trPulse 1.1s ease-in-out infinite` durante loading.
`@keyframes trGlassIn { from { opacity: 0; transform: scale(0.985); } to { opacity: 1; transform: scale(1); } }`
`@keyframes trPulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.45; } }`
`@keyframes trFadeUp { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: translateY(0); } }`

**Pill desktop**: `position: fixed; left: 50%; transform: translateX(-50%); z-index: 35;
bottom: 78px` (paginado) / `32px` (rolagem) — **RE-DERIVAR, ver D-...-4**; `display: flex;
align-items: center; gap: 10px; padding: 7px 8px 7px 16px; border-radius: 999px;
background: rgba(28,30,48,0.58); backdrop-filter: blur(26px) saturate(190%);
box-shadow: inset 0 1px 0 rgba(255,255,255,0.18), inset 0 -1px 0 rgba(0,0,0,0.35),
0 16px 40px -12px rgba(0,0,0,0.75); color: #e9e9ed; animation: trFadeUp 0.22s ease`.
Conteudo, na ordem: `<i class="ph ph-text-align-left">` 15px cor accent; contador 12px; dica
`· {extendTip}` 11px `rgba(233,233,237,0.55)` (so quando 1 periodo selecionado e o paragrafo tem >1);
grupo −/+ (`gap: 2px; padding: 2px; border-radius: 999px; background: rgba(255,255,255,0.07)`,
botoes 26x26, `ph-minus`/`ph-plus` 13px, opacidade `1` habilitado / `0.35` desabilitado);
`{onlySentence}` 11px `rgba(233,233,237,0.5)` quando o paragrafo tem 1 periodo so; divisor
`1px x 20px rgba(255,255,255,0.16)`; botao primario `min-height: 32px; border-radius: 999px` com
`ph-translate` 15px + `{translateSnip}`; botao X 28x28 com `ph-x` 14px.

**Pill mobile**: `position: absolute; left: 10px; right: 10px; bottom: 102px; z-index: 30;
gap: 6px; padding: 6px 6px 6px 12px; border-radius: 999px; background: rgba(28,30,48,0.6);
backdrop-filter: blur(26px) saturate(190%); box-shadow: inset 0 1px 0 rgba(255,255,255,0.18),
inset 0 -1px 0 rgba(0,0,0,0.35), 0 16px 40px -14px rgba(0,0,0,0.8)`. Contador 11px, espacador
`flex: 1`, botoes −/+ 28x28, botao primario `height: 32px; font-size: 12px` com `ph-translate` 14px,
botao X 30x30. Sem dica, sem `onlySentence`, sem divisor.

**Hint de primeira vez** (ate a primeira selecao): desktop `fixed; left: 50%; bottom: <mesmo da pill>;
z-index: 34; gap: 9px; padding: 8px 16px; radius 999px; background: rgba(28,30,48,0.5);
backdrop-filter: blur(20px) saturate(180%); box-shadow: inset 0 1px 0 rgba(255,255,255,0.14),
0 12px 30px -14px rgba(0,0,0,0.8); color: rgba(233,233,237,0.82); font-size: 12px;
animation: trFadeUp 0.4s ease` + `ph-cursor-text` 15px cor accent.
Mobile: `absolute; left: 50%; bottom: 104px; z-index: 29; gap: 8px; padding: 7px 14px;
background: rgba(28,30,48,0.55); box-shadow ... -16px rgba(0,0,0,0.85); font-size: 11px`,
icone 14px.

**Chip de idioma no trecho pronto**: `display: inline-flex; align-items: center; gap: 5px` (mobile 4);
`vertical-align: 0.08em; margin-left: 7px` (mobile 6); `padding: 2px 8px` (mobile `2px 7px`);
`border-radius: 999px; font-family: var(--font-body); font-size: 0.6em; font-weight: 500;
letter-spacing: 0.07em; color: <accent>; background: rgba(AC,0.13);
box-shadow: 0 0 0 1px rgba(AC,0.38); white-space: nowrap`. Conteudo: `ph-arrows-left-right` 1.25em,
label curto, `ph-x` 1.15em `opacity: 0.65`. Label = destino quando mostrando traducao, origem quando
mostrando original. Mapa: English->EN, Brazilian Portuguese (PT-BR)->PT-BR, Spanish->ES, French->FR,
German->DE, Italian->IT, Japanese->JA, Korean->KO, Chinese (Simplified)->ZH, Russian->RU; fallback
`s.slice(0,2).toUpperCase()`.

**Span do trecho traduzido**: mesmo padding/margin do periodo, `border-radius` NAO aplicado no span
(o visual vem do blob), `cursor: pointer; user-select: none`.

**Interacao**: `sel = { p, anchor, set[] }` restrita a UM paragrafo. Tap alterna o periodo no `set`
(esvaziou -> `sel = null`); tap em periodo de OUTRO paragrafo reinicia com `{p, anchor: j, set: [j]}`.
Drag = `pointerdown` marca `_dragging`, `pointermove` usa `document.elementFromPoint` +
`closest('[data-si]')` e chama `extendSent` (preenche `anchor..j` contiguo); `pointerup` no
`document` encerra. `Escape` limpa (desktop). Clique fora do texto limpa. Troca de pagina e troca de
capitulo limpam (`sel: null`). `+` estende ao proximo periodo; `−` remove o ultimo do `set`.

**Textos (pt-BR, unica lingua no escopo)**: `selectHint` desktop "Toque em um período; toque em outro
para estender a seleção" / mobile "Toque em um período; outro toque adiciona"; `extendTip` "toque em
outro período para estender"; `sentenceOne`/`sentenceMany` desktop "período selecionado"/"períodos
selecionados", mobile "período"/"períodos"; `translateSnip` desktop "Traduzir trecho" / mobile
"Traduzir"; `extendSel` "Estender ao próximo período"; `shrinkSel` "Reduzir seleção";
`onlySentence` "único período deste parágrafo"; `toggleSnip` "Alternar original / tradução";
`removeSnip` "Descartar tradução".

## Definition of Done

> `dod=auto_only`. Comandos em bash (Git Bash no Windows), executados da RAIZ do repo.
> `DOTNET_CLI_UI_LANGUAGE=en` porque o sumario local sai em pt-BR. Logs em `TestResults/`
> (gitignored). `PHASE_BASE` = commit gravado por T-1 em `.jdi/phases/snippet-translation/BASELINE`.
> Reporter TAP pinado em todo `node --test` (sem isso o comando nao sai 0 no Node 24 —
> `D-2026-08-01-div-paragraph-reading-6`).

### Auto-verifiable
- [ ] **DoD 1 — Ground truth v0.2.0 existe e e medida.** `design/v0.2.0/PIXEL-SPEC.md` traz os
      valores literais dos elementos novos e ha >= 4 screenshots dos estados novos
      **Verify:** `S=design/v0.2.0/PIXEL-SPEC.md; test -f "$S" && for k in "blur(9px) saturate(180%)" "blur(26px) saturate(190%)" "blur(20px) saturate(180%)" "border-radius: 8px" "0.1em 0.24em" "box-decoration-break" "stroke-width" "1.25" "trGlassIn" "trPulse" "rgba(28,30,48,0.58)" "rgba(28,30,48,0.6)" "bottom: 102px" "data-idiom"; do grep -qF "$k" "$S" || { echo "MISSING $k"; exit 1; }; done && test "$(ls design/v0.2.0/screenshots/*.jpg 2>/dev/null | wc -l)" -ge 4`
      **Source:** D-...-4
- [ ] **DoD 2 — Tabela, Model e Access novos, com storage invisivel no contrato e round-trip
      testado.** `SnippetTranslations` com as 9 colunas e a UNIQUE da ancora; `ISnippetTranslationAccess`
      sem uma palavra de SQL/SQLite; testes reusando `InMemoryDatabase`
      **Verify:** `A=src/TranslateReader.Core/Access/SnippetTranslationAccess.cs; I=src/TranslateReader.Core/Contracts/Access/ISnippetTranslationAccess.cs; M=src/TranslateReader.Core/Models/SnippetTranslation.cs; T=test/TranslateReader.Tests/SnippetTranslationAccessTests.cs; test -f "$A" && test -f "$I" && test -f "$M" && test -f "$T" && grep -q "CREATE TABLE IF NOT EXISTS SnippetTranslations" "$A" && for c in BookId ChapterHRef ParagraphIndex SentenceStart SentenceEnd OriginalHash TranslatedText ShowingOriginal CreatedAt; do grep -q "$c" "$A" || exit 1; done && grep -q "UNIQUE(BookId, ChapterHRef, ParagraphIndex, SentenceStart, SentenceEnd)" "$A" && test "$(grep -ciE 'select |insert |update |delete |sqlite|connectionstring' "$I")" -eq 0 && grep -q "InMemoryDatabase" "$T" && grep -q "MauiProgram" <(git diff --name-only "$(cat .jdi/phases/snippet-translation/BASELINE)" | sed 's:.*/::') && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --filter "FullyQualifiedName~Snippet" > TestResults/snip-dod2.log 2>&1 && grep -q "Passed!" TestResults/snip-dod2.log && awk '/Passed!/{for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1)}} END{exit (f+0==0 && p+0>=12)?0:1}' TestResults/snip-dod2.log`
      **Source:** D-...-1
- [ ] **DoD 3 — Split de periodos: UMA funcao, a regex literal do mockup, fronteiras testadas.**
      `_splitSentences` e a unica fonte; a regex aparece exatamente uma vez
      **Verify:** `F=src/TranslateReader/Resources/Raw/wwwroot/js/snippets.js; test -f "$F" && C=$(sed -e 's://.*::' "$F") && test "$(printf '%s\n' "$C" | grep -cE '^function _splitSentences\(')" -eq 1 && test "$(printf '%s\n' "$C" | grep -cF '(?<=[.!?…]["”’»)\]]?)')" -eq 1 && test "$(printf '%s\n' "$C" | grep -cF '(?=[A-ZÀ-Þ')" -eq 1 && mkdir -p TestResults && node --test --test-reporter=tap --test-name-pattern="splitSentences" test/js/snippets.test.js > TestResults/snip-dod3.log 2>&1 && grep -qE "^# fail 0$" TestResults/snip-dod3.log && P=$(awk '/^# pass /{print $3}' TestResults/snip-dod3.log) && test "$P" -ge 5`
      **Source:** D-...-4, mockup `_splitS`
- [ ] **DoD 4 — Geometria dourada do blob.** `_blobPath` e funcao pura e os 4 testes de path
      LITERAL passam por nome exato; as constantes do mockup estao no arquivo
      **Verify:** `F=src/TranslateReader/Resources/Raw/wwwroot/js/snippets.js; C=$(sed -e 's://.*::' "$F"); printf '%s\n' "$C" | grep -qE 'OFF *= *8' && printf '%s\n' "$C" | grep -qE 'padX *= *5' && printf '%s\n' "$C" | grep -qE 'padY *= *1\.5' && printf '%s\n' "$C" | grep -qF '_blobPath(bands, 10)' && grep -qE "'M [0-9]+\.[0-9] .*Q .*Z'" test/js/snippets.test.js && mkdir -p TestResults && node --test --test-reporter=tap --test-name-pattern="blob geometry" test/js/snippets.test.js > TestResults/snip-dod4.log 2>&1 && grep -qE "^# fail 0$" TestResults/snip-dod4.log && for t in "blob geometry: a single line yields one rounded band" "blob geometry: two lines join at the midpoint between them" "blob geometry: the radius never exceeds half the band" "blob geometry: rects thinner than one pixel are ignored"; do grep -qE "^ok [0-9]+ - $t$" TestResults/snip-dod4.log || exit 1; done`
      **Source:** D-...-4
- [ ] **DoD 5 — Persistencia: restaura, respeita o toggle e DESCARTA quando o hash diverge.**
      Round-trip real no harness, com os 4 testes exigidos por nome
      **Verify:** `mkdir -p TestResults && node --test --test-reporter=tap --test-name-pattern="restore|toggle|remove" test/js/snippets.test.js > TestResults/snip-dod5.log 2>&1 && grep -qE "^# fail 0$" TestResults/snip-dod5.log && for t in "restore: a snippet whose hash matches renders the translated text" "restore: a snippet whose hash diverges is dropped and the paragraph is untouched" "restore: a snippet saved showing the original comes back showing the original" "toggle: switching a snippet swaps the text and flips the chip label"; do grep -qE "^ok [0-9]+ - $t$" TestResults/snip-dod5.log || exit 1; done && grep -q "ShowingOriginal" src/TranslateReader.Core/Models/SnippetTranslation.cs`
      **Source:** D-...-1
- [ ] **DoD 6 — Independente do modo: UMA funcao resolve a raiz, e os dois modos tem teste.**
      `_pager` e `chapter-content` aparecem so dentro de `_snippetRoots`
      **Verify:** `F=src/TranslateReader/Resources/Raw/wwwroot/js/snippets.js; C=$(sed -e 's://.*::' "$F"); test "$(printf '%s\n' "$C" | grep -cE '^function _snippetRoots\(')" -eq 1 && H=$(printf '%s\n' "$C" | awk '/^function _snippetRoots\(/,/^}$/') && printf '%s\n' "$H" | grep -qF "_pager" && printf '%s\n' "$H" | grep -qF "chapter-content" && test "$(printf '%s\n' "$C" | grep -cF '_pager')" -eq "$(printf '%s\n' "$H" | grep -cF '_pager')" && test "$(printf '%s\n' "$C" | grep -cF 'chapter-content')" -eq "$(printf '%s\n' "$H" | grep -cF 'chapter-content')" && mkdir -p TestResults && node --test --test-reporter=tap --test-name-pattern="root" test/js/snippets.test.js > TestResults/snip-dod6.log 2>&1 && grep -qE "^# fail 0$" TestResults/snip-dod6.log && for t in "root: paginated mode resolves the pager as the single root" "root: scroll mode resolves one root per chapter with its own href"; do grep -qE "^ok [0-9]+ - $t$" TestResults/snip-dod6.log || exit 1; done`
      **Source:** D-...-3
- [ ] **DoD 7 — Fronteira: `translation.js` intocado, seletor nao duplicado, ordem de carga
      correta.** `snippets.js` consome `_translatableCandidates` e nunca reimplementa a selecao de
      blocos; a phase 18 nao regride
      **Verify:** `PHASE_BASE=$(cat .jdi/phases/snippet-translation/BASELINE) && test -n "$PHASE_BASE" && test -z "$(git diff --name-only "$PHASE_BASE" -- src/TranslateReader/Resources/Raw/wwwroot/js/translation.js src/TranslateReader/Resources/Raw/wwwroot/js/paginated.js src/TranslateReader/Resources/Raw/wwwroot/js/scroll.js)" && S=src/TranslateReader/Resources/Raw/wwwroot/js/snippets.js && CS=$(sed -e 's://.*::' "$S") && printf '%s\n' "$CS" | grep -qF '_translatableCandidates(' && test "$(printf '%s\n' "$CS" | grep -cE "querySelectorAll\('[^']*(p|h1|li|div)")" -eq 0 && test "$(printf '%s\n' "$CS" | grep -cE 'window\.(applyTranslations|clearTranslations|getVisibleParagraphs) *=')" -eq 0 && I=src/TranslateReader/Resources/Raw/wwwroot/index.html && T=$(grep -n 'js/translation.js' "$I" | cut -d: -f1) && N=$(grep -n 'js/snippets.js' "$I" | cut -d: -f1) && test "$N" -gt "$T"`
      **Source:** D-...-2, D-...-3
- [ ] **DoD 8 — Gate de cobertura verde com 5 arquivos JS e sem `.cs` novo no app MAUI.**
      As duas linhas de `scripts/coverage-gate.sh` atualizadas; pisos 90/85 inalterados
      **Verify:** `G=scripts/coverage-gate.sh; grep -qF 'for name in bridge paginated scroll snippets translation' "$G" && grep -qE 'JS_FILES"? -ne 5' "$G" && grep -qE 'COVERAGE_MIN:-90' "$G" && grep -qE 'COVERAGE_JS_MIN:-85' "$G" && PHASE_BASE=$(cat .jdi/phases/snippet-translation/BASELINE) && test -z "$(git diff --diff-filter=A --name-only "$PHASE_BASE" -- 'src/TranslateReader/*.cs')" && mkdir -p TestResults && bash "$G" > TestResults/snip-gate.log 2>&1 && grep -qE '^COVERAGE_JS .*files=5$' TestResults/snip-gate.log && grep -qE '^COVERAGE_SCOPE ' TestResults/snip-gate.log && test "$(grep -c 'COVERAGE_WAIVER_INVALID' TestResults/snip-gate.log)" -eq 0`
      **Source:** D-...-2
- [ ] **DoD 9 — Build limpo e as DUAS suites verdes, com piso derivado de `main` e sem perder
      nenhum teste que existe hoje.** Windows Release 0 erros; C# e JS sem regressao nome a nome
      **Verify:** `mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0 > TestResults/snip-build.log 2>&1 && grep -qE "^ *0 Error\(s\)" TestResults/snip-build.log && B=$(( $(git grep -cE '^[[:space:]]*\[Fact' main -- 'test/TranslateReader.Tests/*.cs' | awk -F: '{s+=$NF} END{print s+0}') + $(git grep -cE '^[[:space:]]*\[InlineData' main -- 'test/TranslateReader.Tests/*.cs' | awk -F: '{s+=$NF} END{print s+0}') )) && git grep -hoE 'public (async Task|void) [A-Za-z0-9_]+\(' main -- 'test/TranslateReader.Tests/*.cs' | sed -E 's/^public (async Task|void) //; s/\($//' | sort -u > TestResults/snip-base.txt && git grep -hoE 'public (async Task|void) [A-Za-z0-9_]+\(' -- 'test/TranslateReader.Tests/*.cs' | sed -E 's/^public (async Task|void) //; s/\($//' | sort -u > TestResults/snip-head.txt && test -z "$(comm -23 TestResults/snip-base.txt TestResults/snip-head.txt)" && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release > TestResults/snip-suite.log 2>&1 && grep -q "Passed!" TestResults/snip-suite.log && awk -v tn=$((B+12)) '/Passed!/{k=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1);if($i=="Skipped:")s=$(i+1);if($i=="Total:")t=$(i+1)}} END{exit (k&&f+0==0&&t+0>=tn&&p+0+s+0+f+0==t+0)?0:1}' TestResults/snip-suite.log && git show main:test/js/index.js > /dev/null && for f in bridge paginated scroll translation; do git show "main:test/js/$f.test.js" | awk -F"'" '/^test\(/{print $2}'; done | sort -u > TestResults/snip-js-base.txt && node --test --test-reporter=tap test/js/ > TestResults/snip-js.log 2>&1 && grep -qE "^# fail 0$" TestResults/snip-js.log && grep -qE "^# skipped 0$" TestResults/snip-js.log && grep -E "^ok [0-9]+ - " TestResults/snip-js.log | sed -E "s/^ok [0-9]+ - //" | sort -u > TestResults/snip-js-head.txt && test -z "$(comm -23 TestResults/snip-js-base.txt TestResults/snip-js-head.txt)"`
      **Source:** D-...-6
- [ ] **DoD 10 — Valores literais batem com a spec e nenhuma string pt-BR mora no JS.** Cada
      constante visual existe em `snippets.js` E na PIXEL-SPEC; os rotulos chegam do C# por
      `setSnippetLabels`
      **Verify:** `J=src/TranslateReader/Resources/Raw/wwwroot/js/snippets.js; S=design/v0.2.0/PIXEL-SPEC.md; for v in "blur(9px) saturate(180%)" "blur(26px) saturate(190%)" "blur(20px) saturate(180%)" "border-radius: 8px" "0.1em 0.24em" "0 -0.24em" "box-decoration-break: clone" "999px" "rgba(28,30,48,0.58)" "rgba(28,30,48,0.6)" "trGlassIn 0.25s" "trPulse 1.1s" "ph-translate" "ph-cursor-text" "ph-arrows-left-right"; do grep -qF "$v" "$J" || { echo "JS MISSING $v"; exit 1; }; grep -qF "$v" "$S" || { echo "SPEC MISSING $v"; exit 1; }; done && grep -qE '^window\.setSnippetLabels = function' "$J" && test "$(grep -cE 'período|Traduzir|Toque em|Alternar|Descartar|Estender|Reduzir|seleção|tradução' "$J")" -eq 0 && X=src/TranslateReader/Pages/ReaderPage.xaml.cs && for s in "período" "períodos" "Traduzir trecho" "Toque em um período" "Alternar original / tradução" "Descartar tradução" "Estender ao próximo período" "Reduzir seleção" "único período deste parágrafo"; do grep -qF "$s" "$X" || { echo "C# MISSING $s"; exit 1; }; done && grep -qF 'dataset.idiom' "$J"`
      **Source:** D-...-2, D-...-4

### Manual
- _(none — `dod=auto_only`; itens humanos foram para `## Deferred to PR review`)_

## Deferred to PR review
- **Paridade visual REAL** contra `design/v0.2.0/screenshots/*.jpg` — humano compara lado a lado o
  blob de vidro, a pill e o chip num device. Nenhum grep prova "parece igual".
- **Blur em device real.** `backdrop-filter` tem suporte desigual entre WebView2 (Windows), Android
  System WebView e WKWebView; a decisao de fallback (fundo solido) so pode ser tomada olhando.
- **Ergonomia do drag em toque real** (mobile): `elementFromPoint` durante `pointermove` sobre texto
  que rola. So se avalia com o dedo.
- **Qualidade linguistica da traducao de trecho com contexto de paragrafo** — se o modelo devolve so
  o trecho ou vaza o paragrafo inteiro. Exige rodar o GGUF; nao ha assert possivel.
- **Posicao final da pill** apos a re-derivacao do `bottom` (D-...-4): o numero derivado precisa de
  conferencia visual em paginado e em rolagem, desktop e mobile.
- **Custo de re-medicao dos blobs** ao arrastar sliders de tipografia no SettingsOverlay.
- **SonarCloud** sem issue nova nos arquivos tocados — so existe apos push+CI
  (`D-2026-07-30-sonar-zero-issues-12`).

## Notes

### Achados no codigo real (verificados nesta sessao — nao inferidos do mockup)
1. **`scripts/coverage-gate.sh` esta travado em 4 arquivos JS**: `for name in bridge paginated
   scroll translation` (~linha 246) e `if [[ "$JS_FILES" -ne 4 ]]; then ... exit 3` (~linha 262).
   `snippets.js` sem essas 2 linhas atualizadas = gate quebrado com mensagem que nao explica nada.
2. **`.cs` NOVO em `src/TranslateReader/` quebra o gate** (`COVERAGE_GUARD`, exit 2) sem linha em
   `.jdi/coverage-waivers.txt` citando um `D-`. O arquivo tem HOJE zero entradas vivas — manter assim.
3. **A ponte JS->C# so entende `"ready"`**: `ReaderPage.OnHybridMessageReceived`
   (`ReaderPage.xaml.cs:74-82`) compara `e.Message == "ready"` e ignora o resto. O despacho por
   prefixo e novo.
4. **Modo rolagem nao tem `_pager`**: `getVisibleParagraphs`/`applyTranslations`/`clearTranslations`
   dependem de `getElementById('_pager')` + `_currentPage`, que so existem em paginado. Em rolagem o
   conteudo vai para `#chapter-container` e pode ter VARIOS `.chapter-content[data-chapter-href]`.
5. **`_translatableCandidates` (`translation.js:10`) ja e a fonte unica de blocos**, imposta por
   `D-2026-08-01-div-paragraph-reading-3` e por um gate de DoD que faz grep estrutural. E um
   `function` de topo de script classico -> global visivel para `snippets.js` sem export.
6. **`_blobPath`/`_blobFromEls` sao funcoes PURAS** (retangulos -> string SVG) — testaveis sem
   WebView. O `test/js/harness.js` hoje NAO tem `getClientRects()`, `closest()` nem
   `elementFromPoint`; os tres precisam nascer la (o `getBoundingClientRect` ja existe via
   `element.rect`). Regra do harness: ele falha FECHADO — seletor que ele nao entende lanca
   `SyntaxError`, nunca casa tudo.
7. **`design/v0.2.0/` so tem os 2 bundles** (sem PIXEL-SPEC, sem screenshots), ao contrario de
   `design/v0.1.0/`. `design/PIXEL-SPEC.md` foi movido para `design/v0.1.0/PIXEL-SPEC.md`; nenhum
   teste/script le esse caminho (so comentarios em `DesignTokens.xaml:6,86`).
8. **`TranslationCache` e cache puro** (`BookId+ChapterHRef+OriginalHash`, sem coluna de "ativo") —
   nao responde "quais trechos estao traduzidos neste livro". Dai a tabela nova.

### Nomes prescritos (o DoD depende deles — nao renomear)
JS `snippets.js`: `_splitSentences(text)`, `_snippetRoots()`, `_runsOf(set)`, `_blobFromEls(els)`,
`_blobPath(bands, r)`, `window.mountSnippetLayer()`, `window.unmountSnippetLayer()`,
`window.setSnippetLabels(labels)`, `window.restoreSnippets(list)`,
`window.applySnippetTranslation(items)`, `window.setSnippetLoading(keys)`.
Atributos DOM: `data-pi`, `data-si`, `data-snip` (chave `chapterHRef:paragraphIndex:a:b`),
`document.documentElement.dataset.idiom`.
C#: `Models/SnippetTranslation.cs`, `Contracts/Access/ISnippetTranslationAccess.cs`,
`Access/SnippetTranslationAccess.cs`, `Contracts/Managers/ISnippetTranslationManager.cs`
(implementado por `TranslationManager`), `IPromptUtility.BuildSnippetTranslationMessages`.
Testes: `test/js/snippets.test.js`, `test/TranslateReader.Tests/SnippetTranslationAccessTests.cs`.

### Padroes do repo a REUSAR (nao inventar)
- Teste de Access: `test/TranslateReader.Tests/InMemoryDatabase.cs`
  (`Data Source={guid};Mode=Memory;Cache=Shared`, conexao ancora aberta). Copiar a forma de
  `TranslationCacheAccessTests.cs`. Nao ha SQLite em disco em teste.
- Contrato do payload JS<->C#: `test/TranslateReader.Tests/HybridWebViewContractTests.cs` ja testa
  camelCase de `PageInfo`/`ScrollInfo` e ate le os arquivos JS — os tipos novos entram la.
- Serializacao AOT-safe: `src/TranslateReader/Serialization/ReaderJsonContext.cs` com
  `JsonTypeInfo`; `ReaderPage.EvalJsAsync<T>` ja recebe o `JsonTypeInfo`. Nunca reflexao.
- DDL inline `CREATE TABLE IF NOT EXISTS` no proprio Access (o repo nao tem migration framework).
- Injecao de string em JS sempre por `JsStr(...)`/`JsonSerializer.Serialize` — HTML de livro e
  input NAO confiavel (`.claude/rules/csharp.md` §4).

### Sequencia sugerida ao planner
T-1 spec+screenshots+BASELINE -> T-2 harness (`getClientRects`/`closest`/`elementFromPoint`) ->
T-3 `_splitSentences` + `_runsOf` -> T-4 geometria dourada (`_blobFromEls`/`_blobPath`) ->
T-5 camada e raizes (`_snippetRoots`, mount/unmount) -> T-6 selecao, pill e hint ->
T-7 Core (Model, Access, contratos, prompt, cache) -> T-8 ponte e orquestracao no `ReaderPage` ->
T-9 persistencia/restore fim a fim -> T-10 gate de cobertura + suites.
