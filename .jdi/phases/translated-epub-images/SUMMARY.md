# Phase 18: Imagem quebrada em livro traduzido — Summary  (slug: translated-epub-images)

**Status:** complete | **Tasks:** 5/5 completas, 0 blocked | **Iter:** 1 (ralph loop)

## Commits

| SHA | Subject |
|---|---|
| `0aa4320` | `docs(translated-epub-images): derive the suite floor from main (D-...-9)` |
| `d001a75` | `test(translated-epub-images): prove the app host leaks into the translated epub` |
| `036b2b6` | `fix(translated-epub-images): extract chapters for export without app-host rewrites` |
| `476640e` | `test(translated-epub-images): pin the chapter content purpose of every call site` |
| _este_ | `docs(translated-epub-images): record gate evidence and mutation proof` |

`.gitignore` (alteracao local do usuario) ficou FORA de todos os 5 commits.

## O que foi feito por task

**T-1 — pisos ocos corrigidos por MEDICAO (`D-2026-08-01-translated-epub-images-9`, arquivo NOVO;
`-1..-8` intocadas).** Corrida limpa ANTES de qualquer codigo:
`Failed: 0, Passed: 336, Skipped: 2, Total: 338` — o piso `304` de `D-...-8` tolerava perder 34
testes. Item 5 passa a DERIVAR `B` (`[Fact`+`[InlineData`) e `S` (`Skip =`) de `main` no proprio
comando (`B=338`, `S=2`, piso `Total >= 343`) + `comm -23` NOME A NOME (309 nomes em `main`, 309 no
HEAD, nenhum perdido) — contagem sozinha aceita stub sem assert e delecao compensada (learning de
`div-paragraph-reading`). Sonda do fixture Practice (45 entradas): **1 `https://` NATIVO**
(`ops/styles/1266002537.css`, `https://opensource.org/licenses/MIT`) e **0 `epub-images`** -> item 3
vira DIFERENCIAL (nenhuma entrada GANHA `https://`), `epub-images` segue absoluto.

**T-2 — RED.** `Practice_TranslatedEpubArtifact_ForExportedChapters_NoEntryContainsTheAppHost`
escrito com a assinatura de HOJE (3 args), espelhando `RebuildAllTranslatedChaptersAsync` com cache
frio (traducao = original) + `CreateTranslatedEpubAsync`, reabrindo o zip.

**T-3 — fix.** `Models/ChapterContentPurpose.cs` (novo) vira 4o parametro OBRIGATORIO (sem default).
`Export` devolve `item.Content` cru (nem `RewriteImagePaths` nem `InlineCssLinks`); `Display` = hoje
+ guarda `imagesDirectory` vazio -> `InvalidOperationException`. Call sites: `TranslationManager` 3x
`Export`/0x `Display`, `ReadingManager` 1x `Display`/0x `Export`. Churn do 4o argumento nos **6**
arquivos de teste previstos. 2 testes novos (Export == entrada crua do zip para TODOS os capitulos;
guarda). `IParsingEngine` segue com **6 operacoes**.

**T-4 — wiring pinado.** `TranslateBookAsync_UsesExportPurposeForCacheExtractionAndRebuild`
(`Received(2)` Export + `DidNotReceive()` Display), `TranslateChapterAsync_UsesExportPurposeToReadChapterHtml`
(`Received(1)` Export), e `LoadChapterContentAsync_ExtractsImagesThenParsesContent` apertado de
`Arg.Any` para `ChapterContentPurpose.Display` mantendo `s.Contains("images")`.

**T-5 — evidencia.** 6 `Verify:` + build do app + escopo de diff + mutacao (abaixo).

## RED -> GREEN (assercao INTOCADA)

RED (`TestResults/red-artifact.log`, HEAD `d001a75`):
```
Failed!  - Failed: 1, Passed: 0, Skipped: 0, Total: 1
ops/xhtml/ad.xhtml: contains 'epub-images' -> epub-images//ops/images/ad.jpg"/>
ops/xhtml/ad.xhtml: gained 'https://'     -> https://opensource.org/licenses/MIT
```
A barra DUPLA e o `bookDir` vazio da causa raiz (D-...-1); o `https://` extra e o `InlineCssLinks`
inlinando o CSS no capitulo — a mesma correcao fecha os dois, como D-...-3 previu.

GREEN (`TestResults/green-artifact.log`, apos `036b2b6`): `Passed! - Failed: 0, Passed: 1, Total: 1`.
`git diff d001a75 -- ParsingEngineTests.cs` no corpo do teste mostra UMA linha alterada: o 4o
argumento `ChapterContentPurpose.Export`. O bloco `leaks`/`Assert.True` nao aparece no diff.

## Gates (numeros reais)

| Gate | Resultado |
|---|---|
| `dotnet build TranslateReader.slnx -c Release` | `0 Error(s)` (40 warnings MVVMTK0045 pre-existentes) |
| `dotnet test ... -c Release` | `Failed: 0, Passed: 341, Skipped: 2, Total: 343` (baseline `main` 336/2/338) |
| `node --test test/js/` | `pass 79, fail 0` (== baseline) |
| Literais de caracterizacao | `6102`/`239075`, `1329`/`292254`, `2124`/`678242` INALTERADOS sob `Export` |

Os 4 literais sobreviverem sob `Export` e prova INDEPENDENTE de que pular as 2 mutacoes nao altera o
texto extraido (achado 3 do PLAN). Nenhum teste deletado, renomeado ou pulado.

## Os 6 `Verify:` (extraidos por `sed` do CONTEXT, rodados literalmente)

| # | Exit | Numeros |
|---|---|---|
| 1 Export == entrada crua | 0 | `Failed: 0, Passed: 1, Total: 1` |
| 2 guarda + regressao Display | 0 | `Failed: 0, Passed: 2, Total: 2` |
| 3 artefato (diferencial) | 0 | `Failed: 0, Passed: 1, Total: 1` |
| 4 wiring dos call sites | 0 | `Failed: 0, Passed: 3, Total: 3` |
| 5 suite inteira (D-...-9) | 0 | `Failed: 0, Passed: 341, Skipped: 2, Total: 343`; `comm` VAZIO |
| 6 escopo de diff + build | 0 | `src/TranslateReader/` VAZIO; Core = os 5 arquivos previstos; `0 Error(s)` |

## Prova por mutacao (gate textual nao prova comportamento)

- **(a)** `Export` caindo no caminho de `Display` (early return removido) ->
  `..._ForExport_MatchesRawZipEntryForEveryChapter` **e** `..._NoEntryContainsTheAppHost` FALHAM
  (`Failed: 2, Passed: 0`). Revertida.
- **(b)** guarda removida -> `..._DisplayWithEmptyImagesDirectory_ThrowsInvalidOperationException`
  FALHA (`Failed: 1, Passed: 0`). Revertida.
- Apos reverter as duas: `git diff HEAD -- src/` VAZIO e suite de volta a 343.

## Desvios do PLAN (com justificativa)

1. **Ref local `main` estava PARADO em `9e07c83` (PR #16)** enquanto `origin/main` era `05f3670`
   (PR #17). Com o ref velho, o item 6 do DoD acusaria `src/TranslateReader/Resources/Raw/wwwroot/js/
   translation.js` (arquivo do PR #17) e reprovaria codigo correto. Corrigido por **fast-forward puro
   do ref** (`git update-ref refs/heads/main origin/main`; ancestralidade verificada) — sem checkout,
   rebase ou merge, arvore intocada. Registrado em `D-...-9(A)`.
2. **`dotnet format --verify-no-changes` sai 2, ANTES e DEPOIS do diff, com saida byte-identica**
   (verificado com `git stash`): erros `WHITESPACE` legados em `ThemeEngine.cs`, `ReaderPage.xaml.cs`,
   `ThemeEngineTests.cs` e `TranslationManagerTests.cs:560`. Nenhum cai em linha tocada por esta fase.
   O PLAN pedia "`dotnet format` limpo"; corrigir legado violaria D-2 (e `.editorconfig` pertence a
   `baseline-de-estilo`), entao reportei o fato em vez de mexer.
3. **A primeira tentativa da mutacao (a) NAO aplicou** (`perl -0pi` contra o arquivo) e o teste passou
   verde — o que leria como "gate fraco". Detectado conferindo `grep -c` antes de confiar no
   resultado; refeito, e a mutacao mata os 2 testes. Registrado porque mutacao que nao aplica produz
   leitura invertida do gate.

## Fora de escopo (deliberado)

- **Zero codigo de migracao/reparo de livros ja traduzidos e quebrados** (D-...-7): o EPUB traduzido
  e artefato DERIVADO, o original nunca e mutado, e o `TranslationCache` sobrevive -> apagar da
  biblioteca e retraduzir e rapido. Ja registrado em `.jdi/todos/2026-08-01-translated-epub-images.md`
  como `[PRODUTO/UX, decisao humana]` — nenhuma task nova criada.
- Forma dos regexes de imagem e percent-encoding em `FindImage`/`FindCss` (D-...-6) — mesmo todo.
- Confirmacao visual em device/WebView real e SonarCloud sem issue nova: `## Deferred to PR review`
  (sem harness neste ambiente / so existe apos push+CI).
