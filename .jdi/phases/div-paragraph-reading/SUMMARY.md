# Phase 18: Traducao interativa cega a paragrafo em `<div>` (leitura) — Summary  (slug: div-paragraph-reading)

**Status:** complete · **Tasks:** 6/6 · **Blocked:** 0 · **Iter:** 1 (ralph)

## Tasks

- **T-1** `harness.js`: `parseSelector` virou lista (`splitSelectorGroup` + `parseSimpleSelector`) e
  `matchDescendants` faz UMA passada por `descendantElements` casando qualquer parte
  (`matchesAnyPart`) — ordem de DOCUMENTO, sem laco externo por seletor. Split de virgula ignora
  `[...]` e valor entre aspas (`scroll.js:32` monta `[data-chapter-href="<href>"]` com href de EPUB,
  input nao confiavel, `csharp.md` §4). 6 testes novos em `harness.test.js`.
- **T-4** `TranslateChapterAsync_ForCalibreStyleBody_TranslatesLeafDivParagraphs` reusando
  `CalibreFixtures.PartiallyCoveredBody`: 3 paragrafos na ordem; wrapper, div-`<img>` e div-`&#8226;`
  fora. NSubstitute sobre `Contracts/`, zero I/O.
- **T-2** 7 testes novos em `translation.test.js` (6 com `calibre` no nome, `test('` na coluna 0):
  3 div-folha visiveis; ordem de documento com `p`/div intercalados; `applyTranslations` no elemento
  do indice devolvido; round-trip apply->clear com `dataset.original === undefined`; wrapper/img/
  bullet nunca candidatos; `console.warn` so quando ha texto e zero candidato. 13 existentes intocados.
- **T-5** `ExtractParagraphs` + `[GeneratedRegex] ParagraphRegex()` DELETADOS;
  `TranslationManager.cs:244` -> `HtmlUtility.ExtractTextBlocks`; `HtmlInjectionTests.cs:304`
  `Assert.Equal(8->7, factories.Count)` no MESMO commit. Zero teste deletado/renomeado/pulado.
- **T-3** `_translatableCandidates(pg)` interno (nao em `window`): `querySelectorAll('p, h1..h6, li,
  div')`, `DIV` so se `querySelector('div, p, h1..h6, li') === null` E `/\p{L}/u`; demais tags mantem
  o filtro de texto nao-vazio. As 3 funcoes leem SO essa lista; `clearTranslations` filtra por
  `dataset.original !== undefined` (fim do `'p[data-original]'`). `console.warn` de `D-...-5`.
- **T-6** gates finais + escopo de diff (abaixo).

## Commits

| sha | subject |
|---|---|
| `a72c4a2` | test(div-paragraph-reading): support selector groups in the JS DOM harness |
| `a3eee90` | test(div-paragraph-reading): cover calibre body in TranslateChapterAsync |
| `1b13648` | fix(div-paragraph-reading): extract text blocks in TranslateChapterAsync |
| `537e595` | test(div-paragraph-reading): add failing calibre cases for paragraph selection |
| `fd5f177` | test(div-paragraph-reading): compare vm-realm results in the caller realm |
| `e00c066` | fix(div-paragraph-reading): select calibre leaf divs as translatable paragraphs |

`.gitignore` (alteracao local do usuario) **fora de todos os commits** — confirmado:
`git log --name-only main..HEAD | grep -c "^.gitignore"` = **0**.

## RED -> GREEN

**T-2 (JS)** — `node --test test/js/translation.test.js`

- ANTES (producao ainda com `querySelectorAll('p')`): `# tests 20 / # pass 13 / # fail 7`.
  Falharam os 7 novos: `returns every calibre leaf div that holds letters`, `leaves the calibre
  wrapper, the image div and the bullet div out`, `indexes p and calibre div elements in document
  order`, `applyTranslations writes into the calibre div the reported index points at`,
  `clearTranslations restores a translated calibre div and drops the marker`, `warns when the page
  has text but no translatable paragraph`, `stays quiet when a calibre div is translatable`.
- DEPOIS (T-3): `# tests 20 / # pass 20 / # fail 0`.

**T-4 (C#)** — `dotnet test ... --filter "FullyQualifiedName~TranslateChapterAsync"`

- ANTES: `Failed!  - Failed: 1, Passed: 6, Skipped: 0, Total: 7` —
  `TranslateChapterAsync_ForCalibreStyleBody_TranslatesLeafDivParagraphs`:
  `Assert.Equal() Failure: Values differ / Expected: 3 / Actual: 0`.
- DEPOIS (T-5): `Passed!  - Failed: 0, Passed: 7, Skipped: 0, Total: 7`.

## Gates (saida real)

| Gate | Resultado |
|---|---|
| `node --test test/js/` | `# tests 73 / # pass 73 / # fail 0` (B_js medido em `main` = **60**; piso `B_js+9` = 69) |
| Cobertura JS `--experimental-test-coverage` | `translation.js 100.00 linha / 100.00 branch / 100.00 funcs` (os 4 arquivos 100%) |
| `dotnet build TranslateReader.slnx -c Release` | `0 Error(s)`, `64 Warning(s)` — **zero** warning em `HtmlUtility.cs`/`TranslationManager.cs`/`HtmlInjectionTests.cs` (so CS0414/CS0618/MVVMTK0045 pre-existentes) |
| `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` | `Build succeeded. 0 Error(s)` |
| `dotnet test ... -c Release` | `Passed!  - Failed: 0, Passed: 336, Skipped: 2, Total: 338` |
| `dotnet format --verify-no-changes` | limpo nos arquivos tocados (restam `ThemeEngine.cs` e `TranslationManagerTests.cs:560` — legado byte-identico a `main`, D-2) |

## Os 7 `Verify:` do CONTEXT

| # | exit | evidencia |
|---|---|---|
| 1 | **0** | `function _translatableCandidates` = 1; `_translatableCandidates(` = 4; `querySelectorAll('p')`/`('p[data-original]')` = 0 |
| 2 | **1 (ver desvio D-1)** | criterio ATENDIDO: `N=6 >= 4`, suite filtrada `pass 6 / fail 0`, `P=6 >= N` |
| 3 | **1 (ver desvio D-1)** | criterio ATENDIDO: `B=13`, piso `17`, `pass 20 / fail 0`, `P=20 >= 17` |
| 4 | **0** | `ExtractParagraphs` = 0, `ParagraphRegex` = 0 em `HtmlUtility.cs`; `HtmlUtility.ExtractTextBlocks` presente |
| 5 | **0** | `Passed! - Failed: 0, Passed: 7, Total: 7` (piso `B+1` = 7) |
| 6 | **0** | `Passed! - Failed: 0, Passed: 336, Skipped: 2, Total: 338` (piso `321`/`323`) |
| 7 | **0** | pathspec VAZIO; unico arquivo tocado em `src/TranslateReader/` e `Resources/Raw/wwwroot/js/translation.js` |

## Desvios do PLAN

- **D-1 — itens 2 e 3 do DoD saem exit 1 por causa do REPORTER, nao por teste vermelho.** O Node 24
  (`v24.14.0`) usa o reporter `spec` por padrao mesmo com stdout redirecionado, entao o sumario sai
  com prefixo de info em vez de `#`, e o `grep -qE "^# fail[[:space:]]+0$"` dos dois comandos nunca
  casa. Os MESMOS comandos, byte-identicos exceto por `--test-reporter=tap`, saem **exit 0**
  (`N=6 P=6`; `B=13 P=20 floor=17`) — logs `TestResults/js-dod2-tap.log` e `js-dod3-tap.log`. A
  phase `coverage-90` ja fixava o reporter no `Verify:` dela. Nao editei `CONTEXT.md` (imutavel para
  o doer) e nao criei decisao; registrei em `.jdi/todos/2026-08-01-div-paragraph-reading.md` +
  `npx -y jdi-cli render`.
- **D-2 — baseline C# real desmente a projecao do PLAN, PARA CIMA.** O PLAN projetava `main` em
  320/322 e piso exato 321/323 ("margem zero"). A corrida real mediu **Failed 0, Passed 335,
  Skipped 2, Total 337** ANTES de qualquer mudanca minha, e **336/338** depois (+1 = o teste de
  T-4). O item 6 usa `>=`, entao FECHA com folga. Nenhum teste foi deletado, renomeado, pulado ou
  marcado: `+1` liquido, exatamente como o PLAN mandava.
- **D-3 — commit extra `fd5f177`, fora dos 6 do PLAN.** Dois asserts de T-2 usavam
  `deepStrictEqual` contra valores produzidos DENTRO do `node:vm` do harness; o Node rejeita por
  prototipo (`same structure but not reference-equal`), nao por conteudo. `Array.from` reconstroi no
  realm do teste. Commit separado para T-3 ficar so com `translation.js`. O RED foi RE-MEDIDO com
  os asserts corrigidos e a producao antiga: continua `# pass 13 / # fail 7`.
- **D-4 — cobertura de `TranslateChapterAsync`: 100% linha, 83,3% branch** (o PLAN pedia >= 90% em
  ambos). O unico branch parcial e `chapter?.Title` (`TranslationManager.cs:265`, 1/2): linha NAO
  tocada por esta phase (`git diff main` mostra so a linha 244) e o irmao intocado
  `TranslateParagraphsAsync` tem o mesmo `chapter?.Title` e o mesmo branch-rate `0,8333` — divida
  pre-existente, nao regressao. NAO adicionei teste fora de escopo so para mover a metrica; anotado
  em `.jdi/todos/`. A linha efetivamente alterada (244) tem 100% de cobertura.

## Fora de escopo (mantido)

`ReaderPage.xaml.cs` / `ReaderPageModel.cs` intocados (D-...-5); membro `TranslateChapterAsync` de
`ITranslationManager` mantido (D-...-4); modo Scroll intocado; `:has()`/combinadores nao entraram no
harness; nenhum parser de HTML novo; nenhuma dependencia npm. Aviso VISUAL ao usuario e validacao em
WebView real seguem em `## Deferred to PR review`.
