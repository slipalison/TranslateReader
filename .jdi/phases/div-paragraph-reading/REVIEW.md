# Phase 18: Review (slug: div-paragraph-reading)

**Verdict:** APPROVED_WITH_WARNINGS

Review FINAL da phase — iter 4 (re-verify única da rodada de warnings do `/jdi-issue`).
Range: `main` (`9e07c83`) → HEAD (`21d7b7b`), 19 commits. Iter 4 = `a57b916` (fix do harness JS),
`e6a5b46` + `21d7b7b` (docs). Toda evidência abaixo foi re-executada NESTA review (worktree,
lab de mutação e repros próprios do reviewer) — nada aceito do SUMMARY sem reprodução.

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `dotnet restore` + `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` → `0 Erro(s)` (40 avisos MVVMTK0045/CS* pré-existentes) |
| Tests | PASS | C#: `Passed! - Failed: 0, Passed: 336, Skipped: 2, Total: 338` (baseline 167 — D-2). JS: `node --test test/js/` → `tests 79 / pass 79 / fail 0 / skipped 0`, exit 0 |
| Coverage | PASS | Escopo new-file (D-2/D-6): `BookTranslationResult` 100%, `ExtractedImage` 100% (≥90%). Esta phase criou **0** `.cs` novo. Agregado (só contexto): 88,92%. Classes tocadas: `HtmlUtility` 1,00/1,00, `TranslationManager` 1,00/1,00 (class-level, Cobertura) |
| Lint | WARN | `dotnet format --verify-no-changes`: 7 hits WHITESPACE, **todos legado byte-idêntico a `main`** (`TranslationManagerTests.cs:560-561` = `main:528-529` verbatim, conferido por `grep -F`; `ThemeEngineTests.cs:12`, `ReaderPage.xaml.cs:122,124` — arquivos fora do diff da phase). Zero hit em linha adicionada pela phase. Dono: phase `baseline-de-estilo` (W-1) |
| Security/Layer | PASS | 5.1/5.2/5.3/5.10/5.12/5.14/5.15-Result/5.16: limpos ou no baseline documentado. OCE em `TranslationManager.cs:61` **re-lança** (`throw;`). Empty-catch/OCE nos PageModels/Pages = boundary de UI legado, fora do diff. 5.11: `+=`5/`-=`4 = baseline de bootstrap. 5.17: hits de I/O só em testes legado (`FileUtilityTests`, `ModelAccessTests`, `InMemoryDatabase`) — nenhum nos 3 arquivos de teste tocados pela phase |
| Consistency | PASS | 19/19 commits Conventional com scope `div-paragraph-reading` (ou `chore(jdi)` do add-phase). Diff de produção da phase = exatamente os 3 arquivos do PLAN (`translation.js` +49/−8, `TranslationManager.cs` 1 linha, `HtmlUtility.cs` −12) |
| UI Validation | SKIPPED | has_frontend=false (cliente MAUI nativo; WebView coberta pelo gate 5.8) |
| DoD | PASS | 7/7 Auto exit 0 (extraídos por `sed` do CONTEXT.md **commitado**, `diff`-conferidos contra o que executei); 0 Manual (dod=auto_only) |

## Verificação cética da iter 4 (evidência própria)

**a) Regressão de harness — descartada.** Nomes de teste extraídos de `git show 8eff13c:test/js/*.test.js`
(75, zero duplicado) vs lista de `ok` do TAP no HEAD (79): `comm -23` = **vazio** (nenhum nome
perdido); os 4 extras são exatamente os 4 testes novos de recusa/valor-com-colchete. Suite inteira
exit 0 — nenhum seletor legítimo de `bridge/paginated/scroll/translation` passou a lançar (produção
usa 4 formas: `[data-chapter-href="…"]`, `.chapter-content`, grupos `p, h1…h6, li, div` — todas
parseiam).

**b) Fail-open morto — reproduzido com script próprio** (`rev-repro.js`, não o do doer):

| Seletor | harness 8eff13c (ANTIGO) | harness HEAD (NOVO) |
|---|---|---|
| `[data-chapter-href="ch"1"]` | `MATCHED BODY,DIV,SPAN` (documento inteiro) | `SyntaxError` |
| `]]garbage((` | `MATCHED BODY,DIV,SPAN` | `SyntaxError` |
| `[data-chapter-href="a,b].xhtml"]` (vírgula + `]` no valor) | `MATCHED DIV:alvo` | `MATCHED DIV:alvo` (preservado) |

`querySelector` também lança (não devolve `null`) — sem isso o `if (!ch) return;` do
`scrollToChapter` engoliria o seletor inválido como no-op.

**c) RED-first — provado em worktree própria em `8eff13c`** (não pelo SUMMARY): copiei o
`harness.test.js` do HEAD para a worktree e rodei `node --test` → `tests 10 / pass 7 / fail 3`;
os 3 `not ok` são exatamente os 3 testes de recusa, todos com
`Missing expected exception (SyntaxError)`. O 4º teste novo (vírgula+`]` no valor) já passava no
parser antigo — consistente com "fix de recusa, não de matching".

**d) Zero diff de produção na iter 4:** `git diff --stat 8eff13c..HEAD -- src/` = **vazio**.

**e) DoD 7/7 + mutante M-E ainda morre.** Os 7 `Verify:` saíram exit 0 nesta review (itens 5 e 6
com `dotnet test` real; item 6: base 308 nomes, head 309, `comm -23` vazio). Lab M-E próprio
(cópia com estrutura preservada; `applyTranslations` desviado para
`querySelectorAll('[data-original], p, div')` atrás de comentário de bloco contendo
`_translatableCandidates(pg)`): suite → `tests 79 / pass 77 / fail 2`, exit 1 — os 2 `not ok` são
exatamente os 2 testes da iter 3; DoD item 2 sob M-E → exit 1. A mudança do harness **não**
afrouxou o gate comportamental (o seletor do mutante é válido e parseia no harness novo; quem o
mata são os testes de paridade read↔write, como desenhado).

**f) Escopo limpo:** `.gitignore` em **0** commits da phase (mudança local do usuário, fora de
todo commit); `CONTEXT.md` e `.jdi/decisions/` **sem diff** em `8eff13c..HEAD`.

## Blockers

Nenhum.

## Warnings

Nenhum novo na iter 4. Remanescentes das iters anteriores, cada um com dono/regra (não bloqueiam):

- **W-1 (legado, gate 4):** 7 hits WHITESPACE do `dotnet format`, todos byte-idênticos a `main`
  (D-2). Dono: phase `baseline-de-estilo`; registrado em `.jdi/todos/LEGACY.md`.
- **W-2/W-3 (limite textual dos gates 1/4/6 do DoD):** os `Verify:` textuais são contornáveis por
  construção (comentário de bloco, rename com compensação) — fechá-los por texto exigiria
  tokenizador JS/C#. O backstop é COMPORTAMENTAL e foi provado por mutação (M-E morre pelos itens
  2/3; item 4 tem o item 5 como backstop RED-first; item 6 tem `comm` nome a nome + coerência de
  sumário). Débito registrado em `.jdi/todos/2026-08-01-div-paragraph-reading.md` com ação
  concreta para o item 6 (`dotnet test --list-tests` no lado HEAD).
- **W-4 (legado):** branch 83,33% no state-machine de `TranslateChapterAsync`
  (`chapter?.Title`, `TranslationManager.cs:265`) — linha NÃO tocada pela phase (diff = linha 244);
  o irmão intocado `TranslateParagraphsAsync` tem o mesmo rate. D-2 exime; registrado em todos.
- **W-6 (UX, deferido por decisão):** `console.warn` de capítulo sem candidato é invisível ao
  usuário. Decisão de toast/badge deferida ao PR review por `D-2026-08-01-div-paragraph-reading-5`.
- **W-5 — FECHADO na iter 4** (era: harness aceitava seletor imparseável e casava o documento
  inteiro — fail-open sobre href de EPUB, input não confiável, `.claude/rules/csharp.md` §4).
  Evidência do fechamento na seção acima.

## DoD Checklist (gate 8)

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | `translation.js` com fonte única `_translatableCandidates` nas 3 funções; seletores antigos ausentes | CONTEXT | Auto | PASS | exit 0 (re-executado nesta review) |
| 2 | Round-trip get/apply/clear calibre com ≥4 testes nomeados, 3 por nome exato no TAP | CONTEXT | Auto | PASS | exit 0; sob mutante M-E → exit 1 (gate não é oco) |
| 3 | Suite `translation.test.js` sem regressão nome a nome vs `main` + ≥4 novos | CONTEXT | Auto | PASS | exit 0; `comm -23` vazio |
| 4 | `ExtractParagraphs`/`ParagraphRegex` removidos; `TranslateChapterAsync` → `ExtractTextBlocks` | CONTEXT | Auto | PASS | exit 0 (`git grep` repo tracked + corpo via `awk`) |
| 5 | Teste calibre em `TranslateChapterAsync_*` (existentes+1) verdes | CONTEXT | Auto | PASS | exit 0 (`dotnet test` real, filtro `TranslateChapterAsync`) |
| 6 | Suite C# inteira, piso derivado de `main` (337) +1, nenhum método de `main` some | CONTEXT | Auto | PASS | exit 0; `Passed: 336, Skipped: 2, Total: 338`; base 308 / head 309 nomes, `comm` vazio |
| 7 | `PageModels`/`Pages` intocados (fix só em `wwwroot/js` + Core) | CONTEXT | Auto | PASS | exit 0 (`git diff --name-only main` filtrado = vazio) |

**Totals:** 7 items | Auto: 7 (7 PASS, 0 FAIL) | Manual: 0 pending

## Estado final da phase

- **Entrega (iter 1):** `_translatableCandidates` como fonte única de seleção nas 3 funções JS
  (leitura interativa deixa de devolver zero parágrafo em EPUB calibre); caminho C# morto
  (`ExtractParagraphs`/`ParagraphRegex`) removido, `TranslateChapterAsync` no extrator já corrigido;
  RED→GREEN documentado e re-verificado nas reviews (JS 13→20, C# 6+1).
- **Iter 2:** DoD des-ocado por `D-2026-08-01-div-paragraph-reading-6` (matriz de mutação com 7
  contra-exemplos + 9 regressões, zero falso positivo).
- **Iter 3:** mutante M-E fechado por comportamento (2 testes de paridade read↔write sobre
  `CALIBRE_BODY`), suite JS 73→75.
- **Iter 4:** W-5 fechado — harness fail-closed em seletor imparseável (mesmo contrato do DOM
  real), suite JS 75→79, zero diff de produção, zero mudança de CONTEXT/decisões.
- Placar final: JS **79/79** (main = 60), C# **336/2/338** (main = 335/2/337), build 0 erros,
  DoD 7/7, cobertura new-file 100%.
- Débitos remanescentes: W-1/W-2/W-3/W-4 registrados em `.jdi/todos/` com dono; W-6 deferido por
  decisão. Nenhum bloqueia ship.

## Para o revisor humano do PR

1. **UX do aviso (W-6, decisão sua):** capítulo com texto mas zero candidato traduzível hoje só
   emite `console.warn` (`translation.js`, `getVisibleParagraphs`). Decidir se merece toast/badge —
   deferido por `D-2026-08-01-div-paragraph-reading-5`.
2. **Validação em WebView real:** o harness prova comportamento sobre DOM falso; paginação/rendering
   dos `div` calibre traduzidos numa WebView de verdade (Windows/Android/iOS) é leitura humana.
3. **SonarCloud:** confirmar zero issue nova nos arquivos tocados após push+CI
   (D-2026-07-30-sonar-zero-issues-12).
4. **Contrato mais estrito que o DOM no harness:** `parseSimpleSelector` rejeita o seletor universal
   `*` (nenhum script de produção o usa hoje — suite verde prova). Se algum script futuro precisar,
   o harness lançará `SyntaxError` de cara — fail-closed proposital, só estar ciente.
5. `ITranslationManager.TranslateChapterAsync` segue sem chamador de UI (mantido por
   `D-2026-08-01-div-paragraph-reading-4` — fora do escopo do card removê-lo).

## Recommendation

Ship. Warnings remanescentes têm dono e registro; os itens humanos estão na seção acima e no
`## Deferred to PR review` do CONTEXT.md. Nenhuma ação do doer pendente.

**Verdict:** APPROVED_WITH_WARNINGS
