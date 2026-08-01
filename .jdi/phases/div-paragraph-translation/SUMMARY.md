# Phase 17: Traducao cega a paragrafo em `<div>` (EPUB de calibre) - Summary  (slug: div-paragraph-translation)

**Status:** complete
**Tasks:** 7/7 completas, 0 blocked
**Iter:** 1 (ralph_loop)

## Commits (7, atomicos, base `main` @ ad607ac)

| SHA | Subject |
|---|---|
| `393a8de` | docs: supersede the fallback trigger with a disjoint-union selection rule |
| `57c3143` | test: add failing selection tests for calibre div paragraphs |
| `0d5bef9` | test: characterize block extraction on the three real EPUB fixtures |
| `88f7c9a` | fix: translate calibre paragraphs wrapped in leaf divs |
| `4acdabf` | feat: report how much of the book the translation actually covered |
| `c01c81d` | test: cover the CoveredTextRatio signal on every branch |
| `2ff9d07` | refactor: read the EPUB path from the translation result |

## Tasks executadas

- **T-1** (`393a8de`) - `D-...-7` (uniao disjunta numa unica regex, supersede SO o gatilho de
  `D-...-1`) e `D-...-8` (simetria extracao/substituicao) anexadas ao fim de `.jdi/DECISIONS.md`.
  `CONTEXT.md`: item 3 do DoD reescrito para o invariante estrutural (nome de teste mantido),
  `D-...-1` ganhou nota de supersessao, `D-...-7`/`-8` listadas. Nenhum dos 7 `Verify:` mudou de
  comando ou de nome de teste. **Delecoes em `.jdi/DECISIONS.md` = 0** (grep de linhas `-`).
- **T-2** (`57c3143`) - 10 testes novos em `HtmlUtilityTests.cs`, strings literais (`D-...-5`),
  zero I/O: Fixture A (3 blocos na ordem), Fixture B (1 bloco), anti-regressao `p|h|li` com
  assercao de nao-duplicacao, guarda de letra, round-trip (ordem + divs sem letra byte-identicos),
  2 de ReDoS, 2 de borda (`<div/>`, `<br/>`).
- **T-3** (`0d5bef9`) - `ExtractTextBlocksBaselineTests.cs` novo: 3 testes
  `*_PreservesBaselineBlockCount` (padrao `FindEpub` de `ParsingEngineTests.cs`). Cada um fixa
  contagem de blocos **e** soma de chars nao-espaco. Verdes desde a primeira corrida. O EPUB do
  usuario nao e citado em arquivo nenhum.
- **T-4** (`88f7c9a`) - `TextBlockRegex` vira alternacao unica: branch `p|h[1-6]|li` primeiro,
  branch de div-folha depois (token temperado que exclui `<div`, `</div`, `<p`, `<h[1-6]` e `<li`),
  grupos nomeados `tag`/`text`, `IgnoreCase|Singleline` + `RegexTimeoutMilliseconds`.
  `ExtractTextBlocks` e `ReplaceTextBlocksInHtml` passam a compartilhar a regex E o predicado
  `IsTranslatableBlock` (`ContainsLetter` no branch de div, `IsNullOrWhiteSpace` no branch
  `p|h|li`). Nenhuma fabrica de `Regex` nova, entao o `Assert.Equal(8, factories.Count)` de
  `HtmlInjectionTests.cs:304` **nao precisou mudar** (arquivo intocado). Waiver `SYSLIB1044`
  mantido, comentario atualizado (backreference agora e `\k<tag>`).
- **T-5** (`4acdabf`) - `BookTranslationResult(string EpubPath, double CoveredTextRatio)` novo;
  `ITranslationManager.TranslateBookAsync` com `<summary>`; agregacao dentro de
  `RebuildAllTranslatedChaptersAsync` via record privado `RebuiltBook` (zero I/O novo);
  `HtmlUtility.CountTextChars` guarda a contagem; o Manager so acumula e divide (total 0 => 1.0).
  Nunca lanca por cobertura baixa. 1 assercao de retorno ajustada (linha 334).
- **T-6** (`c01c81d`) - 4 testes de cobertura: Fixture A abaixo de 1.0 (valor exato 106/113),
  Fixture B igual a 1.0, capitulo de cobertura zero (0.0, `DeleteJobAsync` chamado, zero excecao)
  e ramo de corpo sem texto (1.0).
- **T-7** (`2ff9d07`) - `LibraryPageModel.cs` le `translation.EpubPath` (2 usos). Zero
  `DisplayAlert`/`ShowPopupAsync`/`Popup` novo.

## Transcript VERMELHO (`D-...-6`, antes de T-4)

Corrida em `57c3143`, `dotnet test`:

```
Com falha ...HtmlUtilityTests.ReplaceTextBlocksInHtml_ForCalibreStyleBody_WritesEachTranslationIntoItsOwnDiv
Com falha ...HtmlUtilityTests.ExtractTextBlocks_ForLargeCalibreBody_ExtractsEveryBlockUnderOneSecond
Com falha ...HtmlUtilityTests.ExtractTextBlocks_ForLeafDivWithoutAnyLetter_SkipsTheBlock
Com falha ...HtmlUtilityTests.ExtractTextBlocks_ForLeafDivWithLineBreaks_KeepsItAsOneBlock
Com falha ...HtmlUtilityTests.ExtractTextBlocks_ForFullyCoveredCalibreBody_ExtractsTheSingleLeafDiv
Com falha ...HtmlUtilityTests.ExtractTextBlocks_ForCalibreStyleBody_ExtractsLeafDivsWithLetters
Com falha ...HtmlUtilityTests.ExtractTextBlocks_ForSelfClosingDiv_DoesNotSwallowTheFollowingDivs
Com falha! - Com falha: 7, Aprovado: 305, Ignorado: 2, Total: 314
```

`Failed: 7` (exigido >= 5).

## PROVA OBRIGATORIA - antes vs depois, com numero

Medicao real: a selecao foi temporariamente estreitada de volta a forma pre-fix (branch de div
removido da alternacao), a suite rodada, e o arquivo restaurado com `git checkout --`.

| Corpo | Blocos ANTES | Blocos DEPOIS | CoveredTextRatio ANTES | DEPOIS |
|---|---|---|---|---|
| Fixture A (calibre) | **0** | **3** | **0,0** (0/113) | **0,93805** (106/113) |
| Fixture B (calibre) | **0** | **1** | **0,0** (0/39) | **1,0** (39/39) |
| 5.000 div-folha (~250 KB) | **0** | **5.000** | - | - |

Os 3 fixtures reais **nao mudam de contagem** (os mesmos literais passam nos dois estados):

| Fixture | Blocos | Chars nao-espaco |
|---|---|---|
| Wardley Maps | 2124 | 678242 |
| Righting software | 1329 | 292254 |
| Practice Makes Perfect | 6102 | 239075 |

## Gates (numeros reais)

- `dotnet test TranslateReader.slnx -c Release` -> **Com falha: 0, Aprovado: 319, Ignorado: 2,
  Total: 321**.
- `node --test test/js/` -> **tests 60, pass 60, fail 0** (intocados).
- `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f
  net10.0-windows10.0.19041.0` -> **0 Erro(s), 40 Aviso(s)** - todos `MVVMTK0045` pre-existentes.
- Cobertura (`--collect:"XPlat Code Coverage"`, cobertura.xml): `HtmlUtility` **113/113 linhas
  (100%) e 60/60 branches (100%)**; `TranslationManager` **37/37 linhas (100%) e 6/6 branches
  (100%)**; `BookTranslationResult` **1/1**. Piso D-6 = 90%.
- `dotnet format --verify-no-changes` nos arquivos tocados: limpo. As 4 violacoes WHITESPACE
  restantes em `TranslationManagerTests.cs` sao as legadas das linhas 528-529 (baseline, D-2).
- `.jdi/DECISIONS.md`: **0 linhas deletadas**. `.gitignore` (alteracao local do usuario) fora de
  todos os 7 commits.

## DoD - os 7 `Verify:` com resultado real

| # | Item | Resultado |
|---|---|---|
| 1 | 3 testes `PreservesBaselineBlockCount` | **PASS** |
| 2 | Fixture A + guarda `IsLetter` | **PASS** |
| 3 | Invariante: div que contem p/h#/li nunca vira bloco | **PASS** |
| 4 | `BookTranslationResult` + contrato + teste de ratio | **PASS** |
| 5 | Cobertura zero nao lanca | **PASS** |
| 6 | Toda `[GeneratedRegex]` com timeout (N=8 T=9 D=1) | **PASS** |
| 7 | `src/TranslateReader/` so em `LibraryPageModel.cs`, sem UI nova | **PASS** |

## Divergencias registradas (nao sao falhas, sao correcoes de numero)

- **Piso de teste do PLAN (Total >= 331) partia de um baseline errado.** O baseline real desta
  branch e **304** (302 aprovados / 2 ignorados), medido antes de qualquer alteracao e ja
  registrado em `.jdi/phases/coverage-90/SUMMARY.md:113` - nao 319. Aplicando a intencao do plano
  (baseline + >= 12 novos), o piso correto e **>= 316**; entregue **321** (17 testes novos).
- **T-6, capitulo de cobertura zero:** o PLAN descreve "capitulo so com `<img>` -> ratio 0.0". Um
  corpo so com `<img>` tem ZERO chars nao-espaco e por definicao cai no ramo total 0 => 1.0. Para
  obter 0.0 de verdade o fixture usa `<img>` + texto solto fora de qualquer bloco; o ramo do corpo
  vazio ganhou teste proprio (`..._IsOneWhenTheBodyHasNoTextAtAll`). Os dois estao verdes.

## Fora de escopo (mantido)

- `ExtractParagraphs`/`TranslateChapterAsync` (traducao interativa) seguem so com `<p>` - mesmo
  defeito de classe, ja registrado em `.jdi/todos.md`.
- Aviso visual de `CoveredTextRatio` baixo e Quality Gate do SonarCloud: `Deferred to PR review`.
- Custo/tempo (R3): o livro-origem passa de ~360 para ~2,2k blocos; corrida muito mais longa,
  consequencia esperada da correcao.

## Arquivos modificados

`.jdi/DECISIONS.md`, `CONTEXT.md`, `PLAN.md`, `SUMMARY.md`, `HtmlUtility.cs`,
`BookTranslationResult.cs` (novo), `ITranslationManager.cs`, `TranslationManager.cs`,
`LibraryPageModel.cs`, `HtmlUtilityTests.cs`, `TranslationManagerTests.cs`,
`ExtractTextBlocksBaselineTests.cs` (novo).
