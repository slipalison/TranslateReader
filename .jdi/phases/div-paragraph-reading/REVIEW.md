# Phase 18: Review (slug: div-paragraph-reading)

**Verdict:** APPROVED_WITH_WARNINGS

Review FINAL da phase (iter 3), regenerada do zero — auto-suficiente. Range revisado:
`main` (`9e07c83`) ate HEAD (`8eff13c`), 15 commits. Toda medicao abaixo foi executada por esta
review nesta maquina (nao copiada do SUMMARY); mutacoes rodaram SOMENTE em labs descartaveis no
scratchpad — o repo real nunca foi mutado (`git status` durante a review: so `.gitignore` M do
usuario, fora de commit, e este REVIEW.md).

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `dotnet restore` + `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` — exit 0 |
| Tests | PASS | C# `Passed! - Failed: 0, Passed: 336, Skipped: 2, Total: 338` (baseline 167 de D-2 superado; identico ao baseline da phase). JS `# tests 75 / # pass 75 / # fail 0 / # skipped 0` (era 73 na iter 2; +2, zero perdido) |
| Coverage | PASS | Arquivos tocados pela phase: `HtmlUtility.cs` 100% linha / 100% branch; `TranslationManager.cs` 100% linha (`TranslateChapterAsync` branch 83,33% — divida pre-existente, ver W-4). JS: `translation.js` 100/100/100 (os 4 arquivos 100%). Aggregate C# 93,15% (contexto). Novos `.cs` desta phase: nenhum (so edicao de arquivos existentes) |
| Lint | WARN | `dotnet format --verify-no-changes` exit 2, 9 erros WHITESPACE — TODOS fora dos hunks da phase (ver W-1) |
| Security/Layer | PASS | 5.1/5.2/5.3/5.10/5.12/5.15/5.17 sem hit novo; detalhes abaixo |
| Consistency | PASS | 8 arquivos commitados em `src/`+`test/` = 1:1 com `## Files modified` do PLAN; Conventional Commits com scope `div-paragraph-reading`, tipos variados (test/fix/docs/chore) |
| UI Validation | SKIPPED | has_frontend=false (native MAUI client) — por desenho, nunca bloqueia |
| DoD | PASS | 7/7 Auto PASS (exit 0, comandos extraidos por `sed` do CONTEXT.md COMMITADO em HEAD), 0 Manual |

## Verificacao adversarial (evidencia propria desta review)

### (a) Mutante M-E agora e pego — CONFIRMADO
Lab descartavel em scratchpad (`lab-me`: copia dos 4 js de producao + `test/js`, estrutura
preservada). Mutacao aplicada SO no lab: em `applyTranslations`,
`var ps = _translatableCandidates(pg);` → `var ps = pg.querySelectorAll('[data-original], p, div');`
precedido de comentario de BLOCO multi-linha contendo `_translatableCandidates(pg)`.

| Medicao sob M-E | Resultado |
|---|---|
| `node --test test/js/` (suite nova) | **exit 1** — falham EXATAMENTE os 2 testes novos: `applyTranslations writes each calibre index into the element getVisibleParagraphs read it from` e `applyTranslations leaves the calibre wrapper alone instead of collapsing the chapter` |
| `Verify:` item 2 do DoD | **exit 1** |
| `Verify:` item 1 do DoD | exit 0 — o grep estrutural continua cego a comentario de bloco (como o proprio `Source:` do item admite; ver julgamento em (e)) |

### (b) Os 2 testes novos assertam de verdade — CONFIRMADO
Duas inversoes alternativas da producao, cada uma sobre lab restaurado:
- **off-by-one** (`applyTranslations` escrevendo em `ps[idx + 1]`): exit 1; caem 5 testes, entre
  eles o novo `writes each calibre index into the element getVisibleParagraphs read it from`.
- **helper em ordem invertida** (`return candidates.reverse();`): exit 1; caem 6 testes, incluindo
  o mesmo teste novo.
Nenhum dos 2 testes novos e ruido: ambos discriminam producao correta de producao invertida.

### (c) Zero diff de producao na iter 3 — CONFIRMADO
`git diff --stat 48725c8..HEAD -- src/` = **vazio**. A iter 3 tocou exatamente 2 arquivos:
`test/js/translation.test.js` (+53) e `.jdi/phases/div-paragraph-reading/SUMMARY.md`.

### (d) Zero regressao — CONFIRMADO
- JS nome a nome: os **73** nomes de teste da iter 2 (`48725c8`, todos os `test/js/*.test.js`)
  comparados via `comm -23` contra a lista de `ok` do TAP do HEAD: **0 ausentes**; os 2 nomes
  extras sao os 2 testes novos. Os **13** de `main:test/js/translation.test.js` tambem 100%
  presentes e verdes. `# skipped 0`.
- C#: `Failed: 0, Passed: 336, Skipped: 2, Total: 338` — identico ao baseline da phase; nenhum
  metodo de teste de `main` sumiu (`comm` do Verify item 6 vazio, executado nesta review).

### (e) Item 1 do DoD mantido como esta — julgamento: DEFENSAVEL, nao esquiva
O `Source:` do item 1 (commitado desde a iter 2) declara textualmente: "Gate ESTRUTURAL de fonte
unica — a prova de COMPORTAMENTO do corpo e dos itens 2 e 3". Ate a iter 2 essa delegacao era
falsa (M-E passava em tudo — reproduzi a premissa: item 1 exit 0 sob M-E). Com os 2 testes novos
ela e verdadeira e **medida** nesta review ((a) acima: item 2 exit 1, suite exit 1). O argumento
tecnico do doer procede: o `sed` do item 1 e por linha e nunca cobrira comentario de bloco
multi-linha ou string literal; fechar isso por texto exige tokenizador de JS, e cada aproximacao
`sed` adicional teria o proximo bypass do lado de fora. Mutantes textuais sao infinitos; o gate
comportamental (itens 2/3) e o que fecha a classe inteira. Aceito com warning W-2.

### (f) Itens 4 e 6 — backstops confirmados de fato
- **Item 4**: lab C# descartavel (`git clone --no-hardlinks` em scratchpad), linha 244 de
  `TranslationManager.cs` desviada para um extrator so-`<p>` (regex Singleline — o desvio que o
  item 4 nao pega quando renomeado/escondido em comentario). Resultado:
  `TranslateChapterAsync_ForCalibreStyleBody_TranslatesLeafDivParagraphs [FAIL]` —
  `Expected: 3 / Actual: 0`, `Failed! - Failed: 1, Passed: 6` no MESMO filtro
  (`FullyQualifiedName~TranslateChapterAsync`) que o `Verify:` do item 5 roda → item 5 sai
  **exit 1**. Backstop comportamental real, provado por execucao.
- **Item 6**: assimetria confirmada por analise — o lado HEAD do `comm` vem de grep ESTATICO de
  metodos publicos; um metodo que perde o `[Fact]` continua no arquivo e o `comm` nao acusa,
  porem o `Total` cai abaixo do piso `B+1` e o gate falha; o buraco residual e apenas a
  COMPENSACAO simultanea (+N testes novos / -N desativados). A direcao futura
  (`MemberData`/`ClassData`) faz `B` SUBcontar — piso mais frouxo, nunca gate impossivel (zero
  falso bloqueio). E mesmo subcontagem; warning W-3, divida anotada para a proxima phase que
  reescrever DoD de C#.

### (g) Escopo — CONFIRMADO
- `git diff --name-only 9e07c83..HEAD -- src/TranslateReader/ ':(exclude).../wwwroot/js/'` = vazio
  (`PageModels`/`Pages` intocados, D-...-5).
- `.gitignore` em **0** dos 15 commits (mudanca local do usuario, fora de commit).
- Iter 3: `CONTEXT.md`, `.jdi/decisions/` e `.jdi/DECISIONS.md` com diff **vazio** vs `48725c8` —
  nenhuma `D-...-7` criada, como alegado.

### (h) Os 7 `Verify:` do CONTEXT commitado — 7/7 exit 0
Extraidos por `sed -n 's/^ *\*\*Verify:\*\* \x60\(.*\)\x60$/\1/p'` de
`git show HEAD:.jdi/phases/div-paragraph-reading/CONTEXT.md` (nao digitados de memoria) e
executados um a um no repo real: itens 1–7 todos **exit 0**.

## Gate 5 — detalhe

- 5.1 Client→Access/Engine: 0 hits. 5.2 storage em contrato: 0. 5.3 Manager→Manager: 0 (apos
  filtrar auto-referencia). 5.10 sync-over-async: 0.
- 5.10 OCE: `TranslationManager.cs:61` faz `throw;` apos persistir "Paused" — correto. Os 3
  `catch (OperationCanceledException) { }` em `LibraryPageModel.cs:183`, `ReaderPageModel.cs:222`,
  `ReaderPage.xaml.cs:308` sao boundary de UI pre-existentes (arquivos NAO tocados pela phase) —
  legado, fora do escopo.
- 5.12 static mutavel: 1 hit = baseline documentado (`TranslationEngine.cs:16`).
- 5.15 `catch { }`: `ReaderPage.xaml.cs:326,434` — pre-existentes, arquivo intocado (legado, D-2).
- 5.17: 0 mock de concreto; 0 I/O real nos 2 arquivos de teste C# tocados pela phase
  (`TranslationManagerTests.cs`, `HtmlInjectionTests.cs`); os 25 hits de I/O em `test/` sao todos
  em arquivos de phases anteriores.
- WebView/injecao (5.8): a phase nao tocou `EvaluateJavaScriptAsync`; `translation.js` escreve so
  via `textContent`/`dataset` (nunca `innerHTML` com dado do livro) e o seletor do helper e
  constante. Sem superficie nova.

## Blockers

_(nenhum)_

## Warnings

- **W-1 (Lint, legado)** — 9 erros WHITESPACE do `dotnet format`: `ThemeEngine.cs:12,14`,
  `ThemeEngineTests.cs:12`, `ReaderPage.xaml.cs:122,124`, `TranslationManagerTests.cs:560-561`.
  Todos fora dos hunks da phase (unico hunk de `TranslationManagerTests.cs` e `@@ -189 +189,38`;
  `ReaderPage.xaml.cs`/`ThemeEngine*` byte-identicos a `main`). D-2 exime; vira BLOCK quando
  `baseline-de-estilo` shippar `.editorconfig`.
- **W-2 (gate textual, item 1)** — o item 1 e comprovadamente cego a comentario de bloco
  multi-linha (M-E: exit 0). Mitigado POR COMPORTAMENTO nos itens 2/3 — mitigacao provada por
  mutacao nesta review, nao por redacao.
- **W-3 (gate textual, item 6)** — compensacao simultanea (+N testes novos / -N `[Fact]`
  removidos) passa no gate; so subcontagem, nunca falso bloqueio. Divida de gate anotada para a
  proxima phase que reescrever DoD de C#.
- **W-4 (coverage, pre-existente)** — `TranslateChapterAsync` branch 83,33%: unico branch parcial
  e `chapter?.Title` (`TranslationManager.cs:265`), linha NAO tocada pela phase; o irmao intocado
  `TranslateParagraphsAsync` tem o mesmo 0,8333. Em `.jdi/todos/`.
- **W-5 (pre-existente de `main`)** — harness falha ABERTO para aspas dentro de valor de atributo
  (`harness.js:315-333`); em `.jdi/todos/`.
- **W-6 (UX deferida)** — capitulo sem paragrafo traduzivel gera so `console.warn`, invisivel ao
  usuario; decisao de toast/badge esta em `## Deferred to PR review` (D-...-5).

## DoD Checklist (gate 8)

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | `translation.js` com fonte unica `_translatableCandidates` nas 3 funcoes; seletores antigos ausentes | CONTEXT | Auto | PASS | exit 0 (gate estrutural; comportamento delegado aos itens 2/3 — delegacao provada em (a)) |
| 2 | Round-trip get/apply/clear calibre com >= 4 testes `calibre`, 3 nomes exatos exigidos | CONTEXT | Auto | PASS | exit 0; N=8 testes calibre; sob mutante M-E este comando sai exit 1 (provado) |
| 3 | Suite JS inteira verde, sem regressao nome a nome vs `main` | CONTEXT | Auto | PASS | exit 0; `# fail 0 / # skipped 0`, `comm -23` vazio, 22 >= 13+4 |
| 4 | `ExtractParagraphs`/`ParagraphRegex` removidos; `TranslateChapterAsync` usa `ExtractTextBlocks` | CONTEXT | Auto | PASS | exit 0; `git grep` limpo no repo tracked; backstop comportamental = item 5 (provado vermelho contra desvio em (f)) |
| 5 | Teste calibre de `TranslateChapterAsync` + existentes passam | CONTEXT | Auto | PASS | exit 0; `Failed: 0, Passed: 7`; contra extrator so-`<p>` fica `Failed: 1` (Expected 3/Actual 0) |
| 6 | Suite C# inteira, piso derivado de `main` (B=337), nenhum metodo de `main` some | CONTEXT | Auto | PASS | exit 0; `Total: 338 >= 338`, `Skipped 2 <= 2`, `comm` vazio, soma coerente |
| 7 | `PageModels`/`Pages` intocados fora de `wwwroot/js` | CONTEXT | Auto | PASS | exit 0; diff vazio |

**Totals:** 7 items | Auto: 7 (7 PASS, 0 FAIL) | Manual: 0 pending

(`.jdi/PROJECT.md` nao contem secao `## Definition of Done`; todos os itens vem do CONTEXT.md — dod=auto_only.)

## Estado final da phase

**Producao (`src/`), 3 arquivos — inalterados desde a iter 1:**
- `src/TranslateReader/Resources/Raw/wwwroot/js/translation.js` — helper interno
  `_translatableCandidates(pg)` (`p,h1..h6,li,div`; `div` so folha + `\p{L}`) vira a UNICA fonte
  de selecao das 3 funcoes (`getVisibleParagraphs`/`applyTranslations`/`clearTranslations`);
  `console.warn` quando ha texto e zero candidato. Fix do defeito: EPUB calibre
  (`<div class="calibreN">`) agora e visivel, traduzivel e limpavel na leitura interativa.
- `src/TranslateReader.Core/Business/Managers/TranslationManager.cs` — 1 linha:
  `TranslateChapterAsync` passa a usar `HtmlUtility.ExtractTextBlocks`.
- `src/TranslateReader.Core/Utilities/HtmlUtility.cs` — `ExtractParagraphs` + `ParagraphRegex`
  (caminho morto com o mesmo defeito de classe) DELETADOS (-12 linhas).

**So teste/gate/doc:**
- `test/js/harness.js` — selector groups por virgula (iter 1); `test/js/harness.test.js` novo
  (6 testes); `test/js/translation.test.js` +9 testes (7 na iter 1, 2 na iter 3 fechando M-E);
  `test/TranslateReader.Tests/` +1 teste calibre (RED-first) + ajuste de contagem em
  `HtmlInjectionTests.cs`.
- DoD endurecido na iter 2 (`D-2026-08-01-div-paragraph-reading-6`, itens 1,2,3,4,6) apos 2
  derrubadas do DoD critic; iter 3 fechou o mutante M-E por comportamento com 2 testes JS.
- `.jdi/`: CONTEXT/PLAN/SUMMARY/decisions da phase.

**Numeros finais:** JS 75/75/0 skipped 0 (main: 60; translation.test.js 13→22), 100% de cobertura
nos 4 js; C# `Failed 0 / Passed 336 / Skipped 2 / Total 338` (main: 335/337; +1 = teste calibre);
build Windows TFM 0 erros; 7/7 DoD exit 0.

## Para o revisor humano do PR

O que os gates NAO provam — em 1 minuto:

1. **Nada aqui rodou numa WebView real.** A suite JS roda sobre um DOM falso (`node:vm`); prova
   comportamento de funcao (selecao/indices/round-trip), nao layout, paginacao visual nem
   `offsetLeft` real. Vale abrir um EPUB de calibre no app e traduzir uma pagina.
2. **Capitulo sem paragrafo traduzivel falha quase em silencio**: só `console.warn` no WebView —
   o usuario nao ve nada. Decisao de toast/badge foi deferida (D-...-5).
3. **Os gates textuais do DoD (itens 1 e 4) sao contornaveis por construcao** (comentario de
   bloco/rename). A protecao real sao os testes comportamentais — esta review provou por mutacao
   que eles ficam vermelhos nos desvios conhecidos (M-E, off-by-one, ordem invertida, extrator
   so-`<p>`). Mutantes textuais novos continuam possiveis; testes sao o gate que importa.
4. **Item 6 tem um buraco estreito**: remover um `[Fact]` de `main` e ao mesmo tempo adicionar um
   teste novo passa no gate (o metodo continua no arquivo, a contagem compensa).
5. **SonarCloud** (analisador `javascript`) so roda apos push+CI — issue nova nos arquivos tocados
   ainda nao foi descartada.
6. `ITranslationManager.TranslateChapterAsync` segue no contrato SEM chamador de UI (mantido por
   D-...-4 — fora do pedido do card removê-lo).

## Recommendation

Aprovar e seguir para `/jdi-ship div-paragraph-reading`. Os warnings sao legado (W-1, W-4, W-5),
limitacao documentada de gate textual com backstop comportamental provado (W-2, W-3) ou decisao de
produto deferida ao PR (W-6) — nenhum exige nova iter do doer. A divida do item 6 (lado HEAD via
`--list-tests`) fica para a proxima phase que reescrever DoD de C#.

**Verdict:** APPROVED_WITH_WARNINGS

## DoD Critic (enhanced — forcado por /jdi-issue)

Terceiro re-ataque, com lab descartavel no scratchpad (repo real nunca mutado). **Nenhuma linha oca.**

- Item 2 e 3: **M-E morre por comportamento** — reproduzido em lab, a suite sai exit 1 com
  EXATAMENTE os 2 testes novos vermelhos, e o `Verify:` do item 2 sai exit 1. Os testes novos nao
  sao tautologicos: apply no-op derruba 5, off-by-one e ordem invertida tambem derrubam. Residuais
  nomeados e estreitos: (i) guarda do helper sem `dataset.original ??` so quebra se a traducao sair
  sem nenhuma letra Unicode; (ii) mutante fixture-aware que fatia os candidatos antes do primeiro
  `<p>`. Nenhum dos dois e implementacao que alguem escreveria — nao-blocker.
- Item 1: o gate textual segue cego POR DESENHO (comentario de bloco e helper duplicado por
  substring passam), mas a delegacao aos itens 2/3 que o `Source:` declara agora e VERDADEIRA por
  medicao, cobrindo toda a familia comportamentalmente visivel: reroute do get (5 red), do apply
  (M-E 2 red; fallback-p red; sem filtro de letra red; `slice(1)` e `slice(0,-1)` red) e do clear
  (red). Os unicos sobreviventes sao EQUIVALENTES (copia identica do helper; clear via
  `[data-original]`, marker que so o apply escreve e so em candidatos) — zero bug de usuario por
  construcao.
- Item 4/5: backstop confirmado por execucao PROPRIA do critico — desviando a linha 244 para um
  extrator so-`<p>`, `TranslateChapterAsync_ForCalibreStyleBody_TranslatesLeafDivParagraphs` falha
  com `Expected: 3 / Actual: 0` e o comando do item 5 sai exit 1.
- Item 6: derivacao recomputada — `[Fact]`=288 + `[InlineData]`=49 = 337 = `Total` real de `main`,
  com zero `MemberData`/`ClassData`. Direcao de erro futura confirmada como SUBcontagem (piso mais
  frouxo), nunca falso bloqueio.
- Item 7: `Verify:` byte-identico desde a iter 2; `git diff 48725c8..HEAD -- src/` vazio (a iter 3
  foi so `test/js/translation.test.js` +53 e `SUMMARY.md`); `CONTEXT.md` e `.jdi/decisions/`
  inalterados — nenhuma `D-...-7`, como o doer alegou.

**Verdict:** APPROVED
