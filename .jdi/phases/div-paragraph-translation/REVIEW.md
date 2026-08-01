# Phase 17: Review  (slug: div-paragraph-translation)

**Verdict:** APPROVED_WITH_WARNINGS

REVIEW FINAL (iter 2, mode=verify), auto-suficiente — regenerada do zero apos o BLOCK da iter 1.
Diff revisado: `main` (ad607ac) -> HEAD (`1397b3e`), branch `jdi/div-paragraph-translation`,
16 commits. Todos os numeros abaixo foram medidos pela reviewer nesta maquina, nesta iteracao —
nada foi aceito por auto-declaracao do doer nem reaproveitado da iter 1 sem re-medicao. Mutacoes
rodaram num worktree git descartavel (`git worktree add` em `$TEMP`, removido ao fim); `src/` e
`test/` da arvore principal nunca foram tocados (`git status --porcelain src/ test/` = vazio do
inicio ao fim).

## Historico da phase (para leitura isolada)

- **Iter 1** entregou as 7 tasks do PLAN: selecao por uniao disjunta numa unica `TextBlockRegex`
  (branch `p|h[1-6]|li` + branch de div-folha temperado), predicado compartilhado entre extracao e
  substituicao (`D-...-8`), `BookTranslationResult(EpubPath, CoveredTextRatio)` no contrato, ajuste
  mecanico em `LibraryPageModel`. Reviewer aprovou com 7 warnings apos verificacao propria; o
  orquestrador validou contra o EPUB real do usuario (1.910 blocos, 100,0% de cobertura, contra 360
  blocos e 12,6% antes do fix).
- **O DoD critic derrubou a iter 1**: um mutante que COMPILA (branch de div removido da regex,
  reintroduzindo o bug da phase) passava nos 7 gates `Verify:` enquanto a suite acusava falhas —
  nenhum gate executava teste. Verdict BLOCKED.
- **Iter 2** endureceu o DoD (`D-2026-08-01-div-paragraph-translation-9`, append-only,
  DECISIONS.md:1502): 8 itens, cada um comecando pelo comando antigo literal e seguindo com `&&`
  numa execucao real da suite com piso de testes casados. E fechou o W-1 da iter 1 (clamp
  `Math.Min(1.0, ...)` no ratio) — a UNICA linha de producao da iter 2.

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `dotnet restore && dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` -> **0 erros**, 8 avisos (MVVMTK0045/CS0618/CS0414, todos legados) |
| Tests | PASS | **Failed: 0, Passed: 320, Skipped: 2, Total: 322** (`-c Release`, log dod8, 2 corridas identicas). Baseline 167 (D-2) e baseline da branch 304 preservados; os 2 skips sao os GGUF legados de `TranslationEngineTests` |
| Coverage | PASS | Escopo novo/tocado (adopted, D-2/D-6): `HtmlUtility` **line 1.0 / branch 1.0**; `TranslationManager` (incl. `RebuiltBook`, `TranslateBookAsync`, `RebuildAllTranslatedChaptersAsync`) **1.0 / 1.0**; `BookTranslationResult` **1.0 / 1.0**. Agregado (contexto, nao e o gate): 92,82% linha / 80,60% branch |
| Lint | WARN | `dotnet format --verify-no-changes` exit 2 — SO linhas legadas (ThemeEngine.cs:12,14; ReaderPage.xaml.cs:122,124; ThemeEngineTests.cs:12; TranslationManagerTests.cs:528-529 — fora dos hunks da phase, conferido por numstat). Nenhuma violacao em linha tocada (W-3) |
| Security/Layer | PASS | 5.1/5.2/5.3/5.9/5.10/5.12/5.14/5.15/5.17 limpos no codigo novo. `TranslationManager.cs:61` `catch (OperationCanceledException)` pausa o job e **rethrows** (compliant csharp.md §1). Sem `new Regex(`, sem mock de concreto, sem I/O novo em teste (excecao autorizada de T-3 mantida). Warns legados em W-5 |
| Consistency | PASS | 16/16 commits Conventional Commits com scope `div-paragraph-translation`, tipos corretos (docs/test/fix/feat/refactor). `files_modified` do PLAN = exatamente o diff de codigo. DECISIONS.md append-only: **0 linhas deletadas** no diff da branch inteira. `.gitignore` (mod local do usuario) fora de todos os 16 commits |
| UI Validation | SKIPPED | has_frontend=false (cliente MAUI nativo) |
| DoD | PASS | **8/8 auto PASS, exit code real, 2 corridas consecutivas**, 0 manual. Anti-oco provado por 5 mutacoes (ver ceticismo) |

## Blockers

Nenhum.

## Warnings

- **W-2 (perf, baixa — carry-over da iter 1, segue valido):** `CountBlockChars`
  (`TranslationManager.cs`) re-aplica `StripHtmlTags` (via `HtmlUtility.CountTextChars`) a blocos
  ja stripped — passada extra de regex + alocacao O(texto do livro) por rebuild. E a mesma raiz que
  produzia o W-1; o clamp agora limita o sintoma, a ineficiencia fica. Irrelevante frente ao custo
  da inferencia LLM; registrar, nao corrigir agora.
- **W-3 (legado, D-2):** violacoes WHITESPACE do gate 4 (lista na tabela) — todas em linhas
  legadas, nenhuma tocada pela phase. Destrava na phase `baseline-de-estilo`.
- **W-4 (legado, CLAUDE.md regra 1):** `LibraryPageModel.TranslateBookAsync` usa 2 Managers no
  mesmo `[RelayCommand]` (`translationManager` + `libraryManager`) — pre-existente em main; a phase
  so trocou a leitura do retorno.
- **W-5 (legado, csharp.md §1):** `catch (OperationCanceledException) { }` em
  `LibraryPageModel.cs:183`, `ReaderPageModel.cs:222`, `ReaderPage.xaml.cs:308` e `catch { }` em
  `ReaderPage.xaml.cs:326/434` — pre-existentes, nao tocados.
- **W-7 (Sonar, risco baixo):** literais repetidos em teste (`"/dest/out.epub"` agora x5 com o
  teste do clamp; fixture calibre duplicada entre `HtmlUtilityTests.cs` e
  `TranslationManagerTests.cs`). S1192 e regra de main-code no perfil padrao; legibilidade de teste
  vence. Confirmacao real do Quality Gate so pos-push (ja em `Deferred to PR review`).
- **Nota (nao e defeito):** a clausula "branch `p|h[1-6]|li` primeiro" de `D-...-7` nao e
  load-bearing — inverti a ordem da alternacao e a suite inteira segue 322/322 verde (mutante
  equivalente: os dois branches abrem com tags disjuntas, a ordem nunca decide qual casa). O
  comentario em `HtmlUtility.cs` que descreve a ordem e descritivo, nao um invariante.
- **Nota (processo):** `LOOP.md` da phase ainda registra `iter: 1 / status: running` — artefato do
  orquestrador, sem impacto de codigo.

Resolvidos desde a iter 1: **W-1 fechado** (clamp, provado por mutacao — ver ceticismo a);
**W-6 caducou** (a inexatidao de relato de warnings era do texto antigo do SUMMARY, reescrito na
iter 2 sem a afirmacao).

## Veredito do ceticismo obrigatorio (a)-(i)

| Item | Veredito | Evidencia propria (iter 2) |
|---|---|---|
| (a) Clamp W-1 | **CONFIRMADO nas 4 pontas** | (i) fecha o caso que eu provei na iter 1: rodei o teste `IsNeverAboveOneOnMalformedHtml` no worktree em `22b8b50` (teste commitado, fix ainda nao) -> **`Expected: 1 / Actual: 3`** — o mesmo ratio 3.0 do meu probe `<div class="c">a < b</div>`. (ii) nao mascara cobertura parcial legitima: `IsBelowOneWhenTextEscapesEveryBlock` assert `Assert.Equal(106d/113d, ratio, 10)` passa (dod4, 2 corridas) — `Math.Min(1.0, x)` e identidade para x<1. (iii) o teste MORDE: mutei HEAD removendo o `Math.Min` -> teste vermelho (`Expected: 1 / Actual: 3`), restaurado limpo. (iv) vermelho-primeiro real: ordem de commits `22b8b50` (test) < `3d7a9f`* (fix) e a corrida em (i) e a prova executada. *`3d7a39f` |
| (b) Regressao de gate | **CONFIRMADO — nenhum piso afrouxado** | Comparacao programatica de prefixo byte a byte dos 7 `Verify:` antigos (extraidos de `3944c2a^`) contra os novos: **7/7 prefixo preservado, 93/194/115/325/131/223/276 chars** — exatamente os numeros alegados pelo doer. Toda continuacao comeca com ` && ` (aperto monotonico: `A && B` implica `A`); nenhuma clausula antiga removida ou enfraquecida. Pisos so subiram: item 4 de 3 para 4 testes, item 8 novo com 320/322 (>= piso da phase). Item 1 mantem igualdade exata `== 3` |
| (c) Falso positivo | **CONFIRMADO** | 8 comandos executados na integra DUAS vezes seguidas no repo limpo, exit code real capturado por corrida: **16/16 exit 0**. Apos as corridas, `git status --porcelain` = so a mod local pre-existente de `.gitignore` e a delecao do REVIEW.md antigo (feita pelo orquestrador). Logs em `TestResults/`, coberto por `**/TestResults/` no `.gitignore` commitado (linha 18) |
| (d) `grep -q "Passed!"` load-bearing | **CONFIRMADO por medicao propria** | `dotnet test --filter "FullyQualifiedName~NoSuchTestNameZzz9"` -> **exit code 0** e **zero** ocorrencias de `Passed!` no log (VSTest imprime `No test matches...` e sai 0). Exit code sozinho aprovaria um filtro que nao casa teste nenhum; o `grep -q` e o que fecha essa porta, junto com o `awk` que exige `Passed:` casado com o piso |
| (e) Reataque ao DoD (5 mutacoes) | **NENHUM FAIL-OPEN** | **M0** (mutante do critic: branch de div removido) — compila com 0 erros, item 1 passa (correto: baselines nao tem div-folha), **itens 2 e 8 reprovam** (suite: `Failed: 10, Passed: 310`); consistente com o 5-de-8 medido pelo orquestrador. **M2** (letter guard -> `text.Length > 0`, refactor plausivel de simplificacao) — **itens 2, 3 e 4 reprovam**; vermelhos exatamente `SkipsTheBlock` e `ExtractsLeafDivsWithLetters`. **M3** (predicado assimetrico: `ReplaceTextBlocksInHtml` volta ao filtro whitespace-only — a classe de falha exata de `D-...-8`) — **item 3 reprova**; vermelhos os 2 round-trips. **M4** (`RegexTimeoutMilliseconds` removido da `TextBlockRegex`) — **item 6 reprova** (aritmetica T-D>=N ja corta antes da suite). **M5** (ordem da alternacao invertida) — 322/322 verde: mutante EQUIVALENTE (tags de abertura disjuntas), DoD verde e o resultado CORRETO, nao fail-open. Cada mutante restaurado com `git status --porcelain src/` vazio conferido |
| (f) `LibraryPageModel.cs` | **CONFIRMADO mecanico** | Unico arquivo de `src/TranslateReader/` no diff (clausula 1 do dod7, `git diff --name-only`); mudanca = 3 linhas: `translatedEpubPath` -> `translation` + 2 leituras `translation.EpubPath` (import e `File.Delete`); zero `DisplayAlert`/`ShowPopupAsync`/`Popup` novo (clausula 2 do dod7 = 0 hits); build `net10.0-windows10.0.19041.0` **0 erros** (gate 1 + dod7, corridos separadamente). Sem teste no projeto MAUI (D-2026-07-30-regression-suite-2) — o build e o detector, e passou |
| (g) Regressao de teste | **CONFIRMADO — nada deletado/afrouxado** | `git diff --numstat ad607ac HEAD -- test/`: `72+/0-`, `153+/0-`, `101+/1-` — a UNICA linha removida na pasta test/ e o assert mecanico da linha 334 (`result` -> `result.EpubPath`, mesma igualdade). `HtmlInjectionTests.cs` fora do diff; linha 304 segue `Assert.Equal(8, factories.Count)` com `Assert.NotEqual(Regex.InfiniteMatchTimeout, ...)` em todas as fabricas |
| (h) Cobertura D-6 + Sonar | **CONFIRMADO** | Cobertura re-medida nesta iter (Cobertura XML): 100% linha e branch em `HtmlUtility`, `TranslationManager` e `BookTranslationResult` (tabela de gates). Padroes: sem CA1826/CA1816/S2699/S3776/xUnit1004 no codigo novo (todos os testes novos assertam; metodos novos <= ~10 linhas; skips = 2 legados com reason); S1192 so em literal de teste (W-7). Sem `new Regex(`, sem `Substring`/`ToLower==` em hot path |
| (i) Warnings da iter 1 | **Reavaliados um a um** | W-1 FECHADO (item a). W-2, W-3, W-4, W-5, W-7 seguem validos (re-conferidos por grep/format/numstat nesta iter, detalhes acima). W-6 caducou (texto do SUMMARY reescrito na iter 2 sem a afirmacao inexata) |

## Cobertura de extracao (numeros da reviewer)

Sinteticos — nesta iter validados pela execucao real dos testes que os fixam (dod2/dod3/dod4,
2 corridas); os probes de harness da iter 1 (39 disjuncao, round-trip 225/225, ReDoS medido em
16-213 ms) permanecem validos porque `HtmlUtility.cs` nao mudou entre `9c56c36` e HEAD (diff da
iter 2 em producao = 1 linha em `TranslationManager.cs`):

| Corpo | Blocos main | Blocos HEAD | Ratio HEAD |
|---|---|---|---|
| Fixture A (calibre, 5 divs) | 0 | **3** | **106/113 = 0,93805** (bullet `&#8226;` = os 7 chars nao cobertos) |
| Fixture B (1 div-folha) | 0 | **1** | **39/39 = 1,0** |
| img + texto solto | 0 | 0 | **0/28 = 0,0** (nunca lanca) |
| so `<img>` (corpo sem texto) | 0 | 0 | total=0 -> **1,0** |
| `<div class="c">a &lt; b</div>` (malformado) | — | 1 | **clamp -> 1,0** (era 3,0 sem clamp — mutacao provada) |
| 5.000 div-folha (~250 KB) | 0 | **5.000** | < 1 s (teste dod6) |
| degenerado: 20.000 `<div` sem fechar | — | retorna/timeout | < 5 s, nunca pendura (teste dod6) |

Fixtures reais (caracterizacao executada nas 2 corridas do dod1 — mesmos literais da medicao
pre-fix da iter 1, ou seja, a selecao nova nao mudou NADA nos livros que ja funcionavam):

| Fixture | Blocos | Chars nao-espaco |
|---|---|---|
| Wardley Maps | 2.124 | 678.242 |
| Righting software | 1.329 | 292.254 |
| Practice Makes Perfect | 6.102 | 239.075 |

Ancora externa (orquestrador, EPUB real do usuario que originou o bug report): **1.910 blocos e
100,0% de cobertura**, contra **360 blocos e 12,6%** antes do fix. Consistente com tudo acima:
main da 0 blocos em corpo calibre; HEAD cobre todo texto em div-folha; capitulo de capa/imagem cai
no ramo neutro `total==0 -> 1.0`.

## DoD Checklist (gate 8)

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | 3 testes `PreservesBaselineBlockCount` E os 3 passam de verdade | CONTEXT | Auto | PASS | exit 0 nas 2 corridas; awk exige `Failed: 0` e `Passed: == 3` |
| 2 | Fixture A extraida com guarda de letra, provado RODANDO os 3 testes de selecao | CONTEXT | Auto | PASS | exit 0 x2; mutantes M0 e M2 reprovam este item |
| 3 | Invariante `D-...-7` (disjuncao) + simetria `D-...-8` (round-trip) executados | CONTEXT | Auto | PASS | exit 0 x2; mutantes M0, M2 e M3 reprovam este item |
| 4 | `CoveredTextRatio` < 1.0 / == 1.0 / nunca > 1.0 (W-1) — 4 testes de ratio rodam | CONTEXT | Auto | PASS | exit 0 x2; piso subiu 3 -> 4 com o teste do clamp |
| 5 | Cobertura zero/baixa nao lanca — teste executado | CONTEXT | Auto | PASS | exit 0 x2 |
| 6 | Toda `[GeneratedRegex]` com timeout: aritmetica + reflexao runtime + 2 adversariais | CONTEXT | Auto | PASS | exit 0 x2; mutante M4 reprova este item |
| 7 | `src/TranslateReader/` = so `LibraryPageModel.cs`, sem UI nova, e o MAUI compila | CONTEXT | Auto | PASS | exit 0 x2; `0 Error(s)` no log |
| 8 | Suite INTEIRA sem filtro: `Failed: 0`, `Passed >= 320`, `Total >= 322` (D-...-9) | CONTEXT | Auto | PASS | exit 0 x2; `Failed: 0, Passed: 320, Total: 322`; reprova M0 (Failed: 10) |

**Totals:** 8 items | Auto: 8 (8 PASS, 0 FAIL) | Manual: 0 pending

PROJECT.md nao declara `## Definition of Done` baseline; o DoD do CONTEXT.md cobre a phase
(mesma constatacao da iter 1 — INCONCLUSIVE nao se aplica pois o CONTEXT declara).

## Estado final da phase

**Producao (o que muda no app):**
- `HtmlUtility.cs` — `TextBlockRegex` vira alternacao unica (`p|h[1-6]|li` + div-folha temperado,
  fontes disjuntas por construcao); `ExtractTextBlocks` e `ReplaceTextBlocksInHtml` compartilham
  regex e predicado `IsTranslatableBlock` (guarda `char.IsLetter` so no branch de div); novo
  `CountTextChars`. Nenhuma fabrica de regex nova (8 mantidas), todas com timeout de 1 s.
- `TranslationManager.cs` — agrega `covered/total` dentro do rebuild existente (zero I/O novo) e
  devolve `BookTranslationResult`; ratio clampado em `Math.Min(1.0, ...)` (unica linha de producao
  da iter 2); nunca lanca por cobertura baixa.
- `ITranslationManager.cs` — `TranslateBookAsync` passa de `Task<string>` para
  `Task<BookTranslationResult>` (com `<summary>`). **Mudanca de contrato publico.**
- `BookTranslationResult.cs` (novo) — `record BookTranslationResult(string EpubPath, double CoveredTextRatio)`.
- `LibraryPageModel.cs` — 3 linhas mecanicas lendo `translation.EpubPath`. Unico arquivo MAUI.

**So teste/doc:** +16 testes (10 selecao/ReDoS/round-trip, 3 caracterizacao com I/O autorizado,
5 de ratio incluindo o do clamp, 1 assert mecanico ajustado); `D-...-7/-8/-9` em DECISIONS.md
(append-only, 0 delecoes); CONTEXT.md com DoD endurecido (8 itens); PLAN/SUMMARY/LOOP/todos/ROADMAP.

**Numeros finais:** suite 322 (320 pass, 2 skip GGUF legados, 0 fail) + 60 JS intocados; cobertura
100% linha/branch em todo arquivo tocado; agregado 92,82%/80,60%; build Windows 0 erros; 16 commits
Conventional Commits; DoD 8/8 em 2 corridas; 5 mutacoes fail-closed, 1 equivalente, 0 fail-open.

## Para o revisor humano do PR

1 minuto de contexto: o defeito veio de **bug report real do usuario** — um EPUB exportado pelo
calibre (paragrafos em `<div class="calibreN">`, sem `<p>`) saia da traducao de livro inteiro com
**12,6% do texto traduzido e nenhum aviso** (360 blocos de 88 mil palavras); o app reportava
sucesso. A correcao (1) amplia a selecao de blocos para div-folha com letra, disjunta por
construcao do branch `p|h|li` — o mesmo livro passa a 1.910 blocos e 100,0% — e (2) faz
`TranslateBookAsync` devolver **`BookTranslationResult(EpubPath, CoveredTextRatio)`** em vez de
`string` — **mudanca de contrato publico** para o sinal de cobertura nunca mais ficar em silencio.
O unico arquivo do app MAUI tocado e `LibraryPageModel.cs` (3 linhas mecanicas). O que o gate NAO
prova e fica com voce: **(a)** UI/UX de aviso quando `CoveredTextRatio` for baixo (threshold,
wording, toast vs badge — hoje ninguem consome o valor); **(b)** comportamento em device real
(gates rodam no TFM Windows; Android/iOS so buildam em CI); **(c)** Quality Gate do SonarCloud
(so existe pos-push); **(d)** fidelidade das fixtures A/B a forma calibre real (leitura humana).
Residuos conhecidos e aceitos: `ExtractParagraphs`/`TranslateChapterAsync` (traducao interativa)
seguem so com `<p>` — mesma classe de defeito, registrada em `.jdi/todos.md`; corrida de traducao
fica ~8x mais longa porque agora traduz o livro de verdade.

## Recommendation

Aprovar e seguir para `/jdi-ship div-paragraph-translation`. A iter 2 fez exatamente o que o BLOCK
pediu — o DoD agora executa comportamento (provado por 5 mutacoes, incluindo o mutante original do
critic, todas fail-closed) — e fechou o W-1 com teste que morde. Os warnings restantes sao legados
(W-3/W-4/W-5), micro-perf reconhecida (W-2) ou risco baixo de Sonar em teste (W-7); nenhum pede
acao antes do ship. A UX de cobertura baixa e o consumo do `CoveredTextRatio` sao a continuacao
natural no PR review.

**Verdict:** APPROVED_WITH_WARNINGS

## DoD Critic (enhanced — forcado por /jdi-issue)

NOTA DE EXECUCAO: rodado inline pelo orquestrador, exit code REAL, comandos extraidos por parser
restrito a `## Definition of Done`. Repo restaurado e conferido apos cada mutacao.

**O furo da iter 1 esta fechado.** Reapliquei o MESMO mutante que derrubou aquela passagem — branch
de div removido da alternacao de `TextBlockRegex`, com a virgula ajustada para a sintaxe seguir
valida, ou seja, o bug original de volta em codigo que compila:

| Medida | iter 1 (7 gates) | iter 2 (8 gates) |
|---|---|---|
| `dotnet build` do mutante | 0 erros | 0 erros |
| gates que REPROVAM o mutante | **0 de 7** | **5 de 8** (itens 2, 3, 4, 6, 8) |
| repo limpo | 7/7 exit 0 | **8/8 exit 0** |

Cinco itens independentes pegam o mutante, entre eles o item 8 (suite inteira, sem filtro,
`Failed: 0` com piso de aprovados), que e o que faltava: agora existe pelo menos um gate que EXECUTA
o codigo em vez de descrever o arquivo. A reviewer confirmou por medicao propria o detalhe que
sustenta o desenho — um `--filter` que casa ZERO teste faz `dotnet test` sair com exit 0 e sem a
linha de sumario, entao o `grep -q "Passed!"` e load-bearing e nao decoracao.

Containment verificado: os 7 comandos antigos seguem como PREFIXO byte-a-byte dos novos
(93/194/115/325/131/223/276 chars), com a continuacao sempre em `&&` — aperto monotonico, nenhum
piso afrouxado. As checagens estruturais nao sumiram; deixaram de ser a unica prova.

Residuo registrado (nao oco): a clausula de `D-...-7` que fixa "branch `p|h|li` primeiro" na
alternacao nao e load-bearing — inverter a ordem produz mutante EQUIVALENTE e a suite segue verde,
corretamente. E precisao de decisao, nao furo de gate.

Nenhuma linha `Type=Auto`/`PASS` mostrou-se oca nesta passagem.

**Verdict:** APPROVED
