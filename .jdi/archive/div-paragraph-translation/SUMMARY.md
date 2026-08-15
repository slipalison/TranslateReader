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

## Iter 3 — rodada de warnings (/jdi-issue)

O loop JA convergiu na iter 2 (`APPROVED_WITH_WARNINGS`, reviewer: "nenhum pede acao antes do
ship"). Esta rodada existe porque o modo autonomo nao aceita "ship assim mesmo" sem tentar limpar.
**Zero linha de producao tocada.** Veredito de `D-2026-08-01-div-paragraph-translation-10`
(append-only: `git diff main..HEAD -- .jdi/DECISIONS.md | grep -c '^-[^-]'` = **0**).

### W-2 (perf, `CountBlockChars` re-strip) — NAO FECHADO, agora com numero

`.claude/rules/csharp.md` §2 exige medir antes de otimizar. Medi, em vez de opinar: probe
descartavel FORA do repo contra `TranslateReader.Core.dll` Release, corpo sintetico de
**534.890 chars / 2.000 blocos-folha** (ordem de grandeza do livro do bug: 1.910 blocos), media de
20 corridas apos 3 de aquecimento, alocacao por `GC.GetAllocatedBytesForCurrentThread()`:

| Variante | ms / rebuild de livro | bytes / rebuild |
|---|---|---|
| atual (`CountTextChars` -> `StripHtmlTags` por bloco) | **2,154 ms** | **2 B** |
| hipotetica sem re-strip (conta nao-espaco direto) | 0,266 ms | 2 B |

Tres motivos para nao mexer, nesta ordem:
1. **O ganho e ~1,9 ms por livro inteiro**, contra 2.000 blocos de inferencia LLM local (minutos a
   horas — a propria phase mede ~8x de aumento na duracao da corrida). Otimizar isso e ruido.
2. **A premissa de alocacao do warning esta errada:** `Regex.Replace` devolve a MESMA instancia
   quando o padrao nao casa nada (`ReferenceEquals` = True, medido) e bloco ja stripped nao tem
   `<...>` sobrando. A "alocacao O(texto do livro)" nao existe — sao ~0 B nas duas variantes. O
   warning superestimava o proprio custo.
3. **A variante barata nao e equivalente:** ela diverge exatamente em HTML malformado com `<` cru,
   que e a classe de entrada que produziu o W-1 e hoje depende do clamp. Trocar o caminho de
   contagem de um sinal que ja teve defeito de borda, por 1,9 ms, e trade ruim.

Registrado em `.jdi/todos.md` com o numero junto e com o aviso para quem for mexer
(`TranslateBookAsync_CoveredTextRatio_IsNeverAboveOneOnMalformedHtml` tem de continuar verde).

### W-7 (literais/fixture duplicada em teste) — FECHADO na parte que era risco real

A metade que importava era a **fixture calibre duplicada**: `HtmlUtilityTests.cs` asserta QUAIS
blocos saem dela, `TranslationManagerTests.cs` asserta a razao **106/113** derivada dos MESMOS
caracteres. Editar uma copia deixava a outra verde sobre markup obsoleto, em silencio — divergencia
que nao falha, so mente. Agora ha **uma copia so**, em `test/TranslateReader.Tests/CalibreFixtures.cs`;
os documentos de capitulo sao compostos por concatenacao de `const`
(`"<html><body>" + ... + "</body></html>"`), **byte-identicos** aos literais anteriores.
Zero assercao alterada, zero nome de teste alterado (os `Verify:` grepam nome literal e contam
ocorrencia), zero `[Fact]` a mais ou a menos. `dotnet format` nao acusa nada nos 3 arquivos.

**NAO fechado:** `"/dest/out.epub"`. Numeros medidos (a REVIEW dizia "x5"; o split real e outro —
ver `D-...-10-CORRECAO`): **10** ocorrencias ja existiam em `main`, HEAD tem **17**, e **7** linhas
com o literal foram adicionadas/tocadas pela phase (as 2 a mais que os 5 da review sao as linhas
mecanicas `result` -> `result.EpubPath`). Extrair exigiria editar as **10** linhas legadas
anteriores a `4285f25` — proibido por `D-2` — e
S1192 e regra de escopo MAIN: o projeto de teste e auto-detectado como test pelo scanner .NET,
entao `csharpsquid` de escopo MAIN nao dispara la. (O precedente de issue do Sonar em teste, as 2
`CA1826` do PR #12, veio do importador `external_roslyn`, que le diagnostico do log do MSBuild —
outro pipeline, outra regra.)

### W-3, W-4, W-5 (legados, D-2) — NAO FECHADOS, por regra; os 3 agora registrados com file:line

Corrigir aqui seria refactor de legado no head MAUI, que esta fora da rede de testes
(`D-2026-07-30-regression-suite-2`): trocar cheiro conhecido por bug desconhecido, sem rede.

- **W-3 (whitespace do `dotnet format`)** — lista RE-MEDIDA nesta iter, identica a da review:
  `ThemeEngine.cs:12,14`; `ReaderPage.xaml.cs:122,124`; `ThemeEngineTests.cs:12`;
  `TranslationManagerTests.cs:528-529`. **Nao estava em `.jdi/todos.md`** (so no corpo da review) —
  registrado agora com file:line e com o encaminhamento: a phase `baseline-de-estilo` (ROADMAP 1)
  roda `dotnet format` uma vez no repo inteiro, depois do `.editorconfig`; corrigir avulso e churn
  sem criterio locked. Confirmado que meus 3 arquivos nao somam violacao nova.
- **W-4 (2 Managers no mesmo `[RelayCommand]`)** — **nao estava em `.jdi/todos.md`**; registrado
  agora, com os file:line CONFERIDOS no arquivo (a review nao trazia numero):
  `LibraryPageModel.cs:105-106` (`[RelayCommand] TranslateBookAsync`), `:111` e `:171`
  (`translationManager`), `:175` (`libraryManager.ImportBookAsync`) e `:45` via `LoadBooksAsync`.
- **W-5 (`catch` que engolem cancelamento)** — **ja estava** registrado, no item `[LEGADO/D-2]` de
  `## De sonar-zero-issues`, com os 5 file:line (`ReaderPage.xaml.cs:326,434,308`,
  `LibraryPageModel.cs:183`, `ReaderPageModel.cs:222`). Nada a fazer.

### Nota de processo — CONFIRMADA, nao tocada

`LOOP.md` da phase le hoje `iter: 2 / status: converged` (o orquestrador ja corrigiu o
`iter: 1 / status: running` que a reviewer viu). Arquivo nao editado por mim.

### Gates da iter 3 (medidos apos as mudancas)

- `dotnet build TranslateReader.slnx -c Release`: **0 Error(s)**, 32 warnings legados
  (MVVMTK0045/CS0618/CS0414 — sao mais que os 8 da review porque a slnx builda os 4 TFMs, nao so o
  de Windows)
- `dotnet test -c Release`: **Failed: 0, Passed: 320, Skipped: 2, Total: 322** (baseline mantido)
- `node --test test/js/`: **tests 60, pass 60, fail 0** (intocados)
- **DoD 8/8 PASS**, comandos extraidos LITERALMENTE de `## Definition of Done` do CONTEXT vigente
- `dotnet format --verify-no-changes`: so as 9 violacoes legadas do W-3, nenhuma em linha tocada
- `.gitignore` (mod local do usuario) fora dos 3 commits

### Commits da iter 3

`d237263` test (fixture compartilhada) · `76a25a2` docs (`D-...-10` + registros em `todos.md`) ·
`c31597b` docs (`D-...-10-CORRECAO`: split real do literal `/dest/out.epub`, medido depois de eu
ter escrito o numero errado em `D-...-10` e na mensagem de `d237263`) · este SUMMARY.
