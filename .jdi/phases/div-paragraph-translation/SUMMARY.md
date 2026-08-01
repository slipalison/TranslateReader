# Phase 17: Traducao cega a paragrafo em `<div>` (EPUB de calibre) - Summary  (slug: div-paragraph-translation)

**Status:** complete | **Tasks:** 7/7, 0 blocked | **Iter:** 2 (ralph_loop)

## Iter 1 - a correcao (7 commits atomicos, base `main` @ ad607ac)

`393a8de` docs (`D-...-7` uniao disjunta + `D-...-8` simetria; 0 delecoes em DECISIONS) ·
`57c3143` test (10 testes em `HtmlUtilityTests.cs`, strings literais, zero I/O: Fixture A/B,
anti-regressao `p|h|li` com assercao de nao-duplicacao, guarda de letra, round-trip, 2 de ReDoS,
2 de borda) · `0d5bef9` test (`ExtractTextBlocksBaselineTests.cs`: 3 `*_PreservesBaselineBlockCount`
no padrao `FindEpub`, cada um fixando contagem **e** chars nao-espaco) · `88f7c9a` fix
(`TextBlockRegex` vira alternacao unica - `p|h[1-6]|li` primeiro, div-folha temperado depois;
`ExtractTextBlocks` e `ReplaceTextBlocksInHtml` passam a compartilhar a regex **e** o predicado
`IsTranslatableBlock`; nenhuma fabrica de `Regex` nova, entao `HtmlInjectionTests.cs:304`
(`Assert.Equal(8, factories.Count)`) ficou intocado; waiver `SYSLIB1044` mantido) · `4acdabf` feat
(`BookTranslationResult(EpubPath, CoveredTextRatio)`, agregacao dentro de
`RebuildAllTranslatedChaptersAsync` via record privado `RebuiltBook`, zero I/O novo, nunca lanca) ·
`c01c81d` test (4 testes de cobertura) · `2ff9d07` refactor (`LibraryPageModel` le
`translation.EpubPath`, 2 usos, zero `DisplayAlert`/`Popup` novo).

**Vermelho primeiro (`D-...-6`)** - suite rodada em `57c3143`, antes do fix:
`Com falha: 7, Aprovado: 305, Ignorado: 2, Total: 314`. Os 7 sao exatamente os testes de selecao de
div-folha + o round-trip. Exigido >= 5. Verificado pela reviewer rodando a suite pre-fix, nao so
lido no transcript.

**Antes vs depois** (selecao estreitada de volta ao estado pre-fix, suite rodada, arquivo restaurado
com `git checkout --`):

| Corpo | Blocos antes | depois | Ratio antes | depois |
|---|---|---|---|---|
| Fixture A (calibre) | 0 | 3 | 0,0 (0/113) | 0,93805 (106/113) |
| Fixture B | 0 | 1 | 0,0 (0/39) | 1,0 (39/39) |
| 5.000 div-folha (~250 KB) | 0 | 5.000 | - | - |

Os 3 fixtures reais **nao mudam de contagem** nos dois estados: Wardley Maps 2124 blocos /
678242 chars, Righting software 1329 / 292254, Practice Makes Perfect 6102 / 239075.

**Divergencias de numero (procedem, confirmadas pela reviewer):** o piso do PLAN partia de baseline
errado - baseline real da branch = 304 (302+2, `coverage-90/SUMMARY.md:113`), piso correto 316,
entregue 321. E "capitulo so-`<img>` -> ratio 0.0" e aritmeticamente impossivel (0 chars nao-espaco
cai no ramo `total==0 -> 1.0`); o fixture de 0.0 usa `<img>` + texto solto, e o ramo do corpo vazio
ganhou teste proprio.

## Iter 2 - fix dos gates ocos

O codigo da iter 1 nao foi questionado (reviewer: 319/321, 0 falhas, 100% de linha/branch nos
arquivos alterados, disjuncao com 39 probes, round-trip 225/225, ReDoS medido; orquestrador no EPUB
real do usuario: 1.910 blocos e 100,0% de cobertura, contra 360 e 12,6% antes). **O que estava
quebrado era a PROVA.** O DoD critic mostrou que os 7 `Verify:` aprovavam um mutante que compila
limpo e reintroduz o defeito da phase - e aprovavam ate fonte que nao compila.

**`D-2026-08-01-div-paragraph-translation-9`** (append-only:
`git diff .jdi/DECISIONS.md | grep -c '^-[^-]'` = **0**, `--numstat` = 40 add / 0 del) supersede os
7 comandos. Cada comando novo **comeca pelo antigo, literal** - verificado por comparacao de prefixo
byte a byte, 93/194/115/325/131/223/276 chars preservados nos itens 1..7 - e segue, encadeado com
`&&` (nunca `;`), numa corrida real da suite:
`DOTNET_CLI_UI_LANGUAGE=en dotnet test ... > log && grep -q "Passed!" log && awk` com piso
`Failed: 0` e `Passed:` casado, o piso de cada item fixado antes da corrida. Item **8 e novo**:
suite inteira, sem filtro, `Failed: 0`, `Passed: >= 320`, `Total: >= 322`.

### Prova por mutacao (cada mutante restaurado; `git status --porcelain src/` = vazio apos cada uma)

| Mutacao | Compila | Suite | DoD ANTIGO | DoD NOVO |
|---|---|---|---|---|
| branch de div removido de `TextBlockRegex` | 0 erros | `Failed: 9, Passed: 310` | **7/7 exit 0** | **5 reprovam** (itens 2,3,4,6,8) |
| `ContainsLetter` sintaticamente quebrado | 2 erros CS | nao roda | **7/7 exit 0** | **8/8 reprovam** |
| teste renomeado, presente so no filtro do item 3 | 0 erros | 322 verdes | **7/7 exit 0** | **item 3 reprova** (piso 3, casou 2) |
| filtro que casa ZERO teste | - | - | - | **reprova** |
| repo real, sem mutacao | 0 erros | `Passed: 320` | - | **8/8 exit 0, 2 corridas seguidas** |

Detalhe medido que justifica exigir `grep -q "Passed!"`: com um filtro que casa zero teste o
`dotnet test` sai com **exit code 0** e sem a linha de sumario. Um gate que dependesse so do exit
code continuaria oco. Os gates gravam log em `TestResults/` (ja em `.gitignore`), entao rodar o DoD
inteiro nao suja `git status`.

### W-1 fechado (vermelho primeiro)

`CoveredTextRatio` podia passar de 1.0 com `<` cru dentro de div-folha. O teste novo
`TranslateBookAsync_CoveredTextRatio_IsNeverAboveOneOnMalformedHtml` (`22b8b50`) reproduziu
`Expected: 1 / Actual: 3` **antes** do fix; `Math.Min(1.0, ...)` em `CoveredRatio`
(`TranslationManager.cs`, `3d7a39f`) fecha. E a UNICA linha de producao tocada nesta iter. Nunca
lanca - csharp.md secao 1 intocada. O piso do item 4 subiu de 3 para 4 testes casados e o do item 8
de 319/321 para 320/322 por causa dele.

### Gates da iter 2 (numeros reais, medidos)

- `dotnet test -c Release`: **Failed: 0, Passed: 320, Skipped: 2, Total: 322**
- `node --test test/js/`: **tests 60, pass 60, fail 0** (intocados)
- `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0`:
  **0 Error(s)**
- Cobertura (`--collect:"XPlat Code Coverage"`): `HtmlUtility`, `TranslationManager` e
  `BookTranslationResult` todos **line-rate 1 / branch-rate 1**
- `dotnet format --verify-no-changes` nos arquivos tocados: nenhuma violacao em linha tocada; so as
  4 WHITESPACE legadas de `TranslationManagerTests.cs:528-529` (baseline, D-2 / W-3)
- `.gitignore` (alteracao local do usuario) fora de todos os commits das duas iters

### Commits da iter 2

`22b8b50` test (W-1 vermelho) · `3d7a39f` fix (clamp) · `3944c2a` docs (`D-...-9` + secao
`## Definition of Done` endurecida) · este SUMMARY.

## Fora de escopo (mantido)

`ExtractParagraphs`/`TranslateChapterAsync` seguem so com `<p>` - mesmo defeito de classe, ja em
`.jdi/todos.md`. Aviso visual de cobertura baixa e Quality Gate do SonarCloud: `Deferred to PR
review`. W-2 (re-strip em `CountBlockChars`) segue registrado e nao corrigido - irrelevante frente
ao custo da inferencia LLM. Custo/tempo: o livro-origem passa de ~360 para ~2,2k blocos, corrida
muito mais longa, consequencia esperada da correcao.

## Arquivos modificados

Iter 1: `.jdi/DECISIONS.md`, `CONTEXT.md`, `PLAN.md`, `SUMMARY.md`, `HtmlUtility.cs`,
`BookTranslationResult.cs` (novo), `ITranslationManager.cs`, `TranslationManager.cs`,
`LibraryPageModel.cs`, `HtmlUtilityTests.cs`, `TranslationManagerTests.cs`,
`ExtractTextBlocksBaselineTests.cs` (novo).
Iter 2: `.jdi/DECISIONS.md` (append), `CONTEXT.md` (`## Definition of Done`), `SUMMARY.md`,
`TranslationManager.cs`, `TranslationManagerTests.cs`.
