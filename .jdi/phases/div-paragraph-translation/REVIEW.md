# Phase 17: Review  (slug: div-paragraph-translation)

**Verdict:** APPROVED_WITH_WARNINGS

Revisao independente (iter 1, mode=verify) do diff `main` (ad607ac) -> HEAD (`9c56c36`), 11 commits.
Todos os numeros abaixo foram medidos pelo reviewer nesta maquina — nada foi aceito por
auto-declaracao do doer. Probes sinteticos rodaram num harness descartavel FORA do repo, com copia
byte-identica do `HtmlUtility.cs` de HEAD e de main lado a lado; os fixtures reais foram medidos
num worktree temporario em `0d5bef9` (pre-fix), removido apos o uso.

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` -> 0 erros. Avisos: MVVMTK0045 + CS0618/CS0414, todos em codigo legado (ver W-6) |
| Tests | PASS | **319 aprovados, 2 ignorados, 0 falhas, Total 321** (`-c Release`). Baseline 304 (302+2, coverage-90/SUMMARY.md:113) preservado; piso corrigido >= 316 atendido. JS: **60/60 pass** intocados |
| Coverage | PASS | Escopo novo-arquivo (adopted, D-2/D-6): `BookTranslationResult.cs` 1/1 = **100%**. Tocados: `HtmlUtility` **84/84 linhas, branch-rate 1.0**; `TranslationManager` **100% linhas**, `TranslateBookAsync` 18/18, `RebuildAllTranslatedChaptersAsync` 14/14 branch 1.0. Agregado (contexto): 92,64% linha / 80,08% branch |
| Lint | WARN | `dotnet format --verify-no-changes` exit 2 — SO linhas legadas (ThemeEngine.cs:12,14; ReaderPage.xaml.cs:122,124; ThemeEngineTests.cs:12; TranslationManagerTests.cs:528-529 — fora dos hunks da phase, D-2). Nenhuma violacao em linha tocada |
| Security/Layer | PASS | 5.1/5.2/5.6-5.9/5.10/5.14/5.15/5.17: limpos no codigo novo. Regex nova com timeout runtime-verificado por `EveryHtmlUtilityRegex_IsBoundedByAMatchTimeout`. Warns legados em W-4/W-5 |
| Consistency | PASS | 7 commits de codigo/teste + 4 docs, Conventional Commits com scope `div-paragraph-translation`, tipos corretos (test/fix/feat/refactor/docs). `files_modified` do PLAN = exatamente o diff. `.gitignore` e o EPUB do usuario fora de todos os commits |
| UI Validation | SKIPPED | has_frontend=false (cliente MAUI nativo) |
| DoD | PASS | 7/7 auto PASS, 0 manual. PROJECT.md nao declara DoD baseline; o do CONTEXT.md cobre a phase |

## Blockers

Nenhum.

## Warnings

- **W-1 (correcao/robustez, baixa):** `CoveredTextRatio` pode exceder 1.0 com `<` cru nao escapado
  dentro de div-folha. Provado no harness: corpo `<div class="c">a < b</div>` -> covered=3,
  total=1, **ratio=3.0**. Causa: numerador re-aplica `StripHtmlTags` ao bloco ja stripped
  (`TranslationManager.cs:207-213` -> `HtmlUtility.cs:87`), denominador stripa o body inteiro, e o
  `<` cru pareia com o `>` do `</div>` no denominador. Em XHTML valido (EPUB spec) o `<` vem
  escapado (`&#8226;`/`&lt;` conferidos simetricos) e nao ha consumidor do valor hoje (UX
  deferida). Sugestao para quando a UX consumir: clamp `Math.Min(1.0, ...)` em
  `TranslationManager.cs:215` ou contar chars do bloco sem re-strip. Nunca lanca — csharp.md §1 ok.
- **W-2 (perf, baixa, csharp.md §2/5.14):** `CountBlockChars` (`TranslationManager.cs:207-213`)
  roda `StripHtmlTags` de novo em blocos ja stripped — uma passada extra de regex + alocacao O(texto
  do livro) por rebuild. Irrelevante frente a inferencia LLM; registrar, nao corrigir agora.
- **W-3 (legado, D-2):** violacoes WHITESPACE do gate 4 listadas acima — todas em linhas legadas,
  nenhuma em linha tocada pela phase. Destravamento previsto na phase `baseline-de-estilo`.
- **W-4 (legado, CLAUDE.md regra 1 / gate 5.4):** `LibraryPageModel.TranslateBookAsync` usa 2
  Managers no mesmo `[RelayCommand]` (`translationManager` + `libraryManager`,
  `LibraryPageModel.cs:171-176`) — pre-existente em main; a phase so renomeou a variavel do retorno.
- **W-5 (legado, csharp.md §1 / gate 5.10):** `catch (OperationCanceledException) { }` em
  `LibraryPageModel.cs:183`, `ReaderPageModel.cs:222`, `ReaderPage.xaml.cs:308` e `catch { }` em
  `ReaderPage.xaml.cs:326/434` — todos pre-existentes, nenhum tocado pela phase.
- **W-6 (doc):** SUMMARY afirma "40 Aviso(s) - todos MVVMTK0045 pre-existentes"; o log de build
  tambem contem CS0618 (`DisplayAlert` obsoleto) e CS0414 — igualmente legados. Inexatidao de
  relato, sem impacto de codigo.
- **W-7 (Sonar, risco baixo):** literais repetidos nos testes novos ("/dest/out.epub" x4, fixture
  calibre duplicada entre `HtmlUtilityTests.cs` e `TranslationManagerTests.cs`). Aceitavel por
  legibilidade de teste (DRY: test setup legivel > helper magico); S1192 e regra de main-code no
  perfil padrao. Confirmacao real do Quality Gate so pos-push (ja em `Deferred to PR review`).

## Veredito do ceticismo obrigatorio (a)-(j)

| Item | Veredito | Evidencia propria |
|---|---|---|
| (a) Disjuncao por construcao | **CONFIRMADO** | Harness, 11 probes: div-folha com `<p>` -> so os `<p>` viram blocos, sem duplicata; div aninhado 3 niveis -> so o mais interno, 1x; `<p>` dentro de `<li>` -> 1 bloco (o `li` engole o `p`); corpo misto h2+div+p+div-com-p+div -> 5 blocos em ordem, zero dupla; spans dos matches sem sobreposicao em todos os corpos (uniao numa unica `Matches` = disjunta por construcao do engine). `<pre>/<hr>/<link>` nao quebram div-folha (tempering com `\b` correto); `<DIV>` maiusculo casa (IgnoreCase) |
| (b) Extracao == substituicao (D-...-8) | **CONFIRMADO** | Round-trip com sentinelas: Fixture A em html completo (head+style) -> 3 extraidos, 3 sentinelas escritas exatamente 1x, em ordem, originais removidos, div de imagem/bullet/head byte-identicos. Corpo misto de 225 blocos (200 div + p/h3/img intercalados) -> 225/225 sentinelas, ordem preservada. Mesma regex + mesmo predicado `IsTranslatableBlock` compartilhados no codigo (`HtmlUtility.cs:38-76`) |
| (c) Baseline dos 3 fixtures | **CONFIRMADO** | Worktree em `0d5bef9` (codigo de SELECAO ainda de main): 3/3 `PreservesBaselineBlockCount` verdes; HEAD: mesmos literais verdes na suite completa; `git diff 0d5bef9..HEAD` no arquivo = vazio. Wardley 2124/678242, Righting 1329/292254, Practice 6102/239075 — identicos nos dois estados |
| (d) ReDoS | **CONFIRMADO** | Harness: 496 KB realistas (6.000 div) -> 16 ms; 30k `<div` sem fechamento -> retorna em 213 ms; adversarial proprio `<div>` + 300 KB de ruido `a<b ` -> 57 ms; 20k `<h2 ` sem fechamento -> 23 ms (igual ao codigo de main: classe pre-existente, nao regressao); 30k `<div >x` -> 18 ms. Timeout 1000 ms presente em todas as 9 usinas (`N=8 T=9 D=1`) e verificado em runtime pelo teste de reflexao |
| (e) CoveredTextRatio | **CONFIRMADO com ressalva W-1** | Aritmetica reproduzida: Fixture A covered=106 total=113 ratio=0,938053097 (bullet `&#8226;` = os 7 chars nao cobertos); Fixture B 39/39=1,0; corpo img+texto solto 0/28=0,0; corpo so-img total=0 -> ramo 1.0; covered <= total em todos os corpos BEM-FORMADOS. Nunca lanca (ramo unico `total==0 ? 1.0 : divisao`, 100% de branch coverage). Ressalva: `<` cru -> ratio>1.0 (W-1) |
| (f) Contrato publico | **CONFIRMADO** | 3 call sites exatos (contrato, Manager, `LibraryPageModel.cs:171`); diff do PageModel = 3 linhas mecanicas (`translatedEpubPath` -> `translation.EpubPath`), zero UI nova (DoD 7 PASS); build `net10.0-windows` 0 erros |
| (g) Vermelho primeiro (D-...-6) | **CONFIRMADO** | Ordem no historico: `57c3143` (testes) < `0d5bef9` (baseline) < `88f7c9a` (fix). Rodei a suite no worktree em `0d5bef9`: **exatamente os 7 testes nomeados no SUMMARY falham** (Com falha: 7, Aprovado: 11 no filtro HtmlUtilityTests) — transcript do doer verificado, nao so lido |
| (h) Regressao de teste | **CONFIRMADO** | `git diff --numstat -- test/`: 72+0, 153+0, 83+1 — a UNICA linha removida e o assert mecanico da linha 334 (`result` -> `result.EpubPath`). `HtmlInjectionTests.cs` fora do diff; linha 304 segue `Assert.Equal(8, factories.Count)` com verificacao de `MatchTimeout` em todas |
| (i) Cobertura D-6 + padroes Sonar | **CONFIRMADO** | Linhas novas/alteradas 100% (tabela de gates). Sem CA1826/CA1816/S2699/S3776/xUnit1004 no codigo novo; sem `new Regex(`; sem mock de concreto; sem I/O novo em teste (exceto os 3 de T-3, excecao autorizada). Unico residuo: W-7 (S1192 em teste, risco baixo) |
| (j) Correcoes de numero do doer | **PROCEDEM as duas** | (1) Piso: baseline real da branch = 304 (302+2), registrado em coverage-90/SUMMARY.md:113 e conferido pelas minhas corridas (314 em 57c3143 = 304+10; 317 em 0d5bef9 = +3); piso correto 304+12=316, entregue 321 — o "319" do PLAN era o passed FINAL, nao baseline. (2) Corpo so-`<img>` tem 0 chars nao-espaco -> cai por definicao no ramo `total==0 -> 1.0` (provado no harness, probe e4): "ratio 0.0" era aritmeticamente impossivel; o fixture substituto (img + texto solto -> 0.0) e o correto e o ramo vazio ganhou teste proprio. Nenhuma das duas e conveniencia |

## Cobertura de extracao (numeros do reviewer)

Sinteticos (harness fora do repo, codigo de main vs HEAD byte-identicos):

| Corpo | Blocos main | Blocos HEAD | Ratio HEAD |
|---|---|---|---|
| Fixture A (calibre, 5 divs) | **0** | **3** | **106/113 = 0,93805** |
| Fixture B (1 div-folha) | **0** | **1** | **39/39 = 1,0** |
| img + texto solto | 0 | 0 | **0/28 = 0,0** |
| so `<img>` | 0 | 0 | total=0 -> **1,0** |
| p/h/li puro (forma dos fixtures reais) | N | **N (identico a main)** | — |
| 6.000 div-folha, 496 KB | 0 | **6.000** (16 ms) | 1,0 |
| misto 200 div + p/h3/img | so os p/h | **225**, round-trip 225/225 | — |

Fixtures reais (worktree `0d5bef9` = selecao de main; HEAD = selecao nova — mesmos literais verdes
nos dois estados):

| Fixture | Blocos | Chars nao-espaco |
|---|---|---|
| Wardley Maps | 2124 | 678242 |
| Righting software | 1329 | 292254 |
| Practice Makes Perfect | 6102 | 239075 |

Ancora do orquestrador (EPUB real do usuario, probe externo): 1.910 blocos (era 360), cobertura
100,0%, 1 capitulo sem blocos — consistente com o comportamento medido aqui: main da 0 blocos em
corpo calibre e HEAD cobre todo texto em div-folha; capitulo sem letra (capa/imagem) da 0 blocos e
cai no ramo neutro. Nenhuma divergencia a investigar.

## DoD Checklist (gate 8)

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | 3 testes `PreservesBaselineBlockCount` (1 por fixture real) | CONTEXT | Auto | PASS | grep count = 3, exit 0; verdes em 0d5bef9 e em HEAD |
| 2 | Fixture A extraida com guarda de letra Unicode | CONTEXT | Auto | PASS | testes presentes + `IsLetter` em `HtmlUtility.cs:82`; exit 0 |
| 3 | Invariante estrutural: div contendo p/h#/li nunca vira bloco | CONTEXT | Auto | PASS | teste presente; disjuncao provada no item (a) |
| 4 | `BookTranslationResult.CoveredTextRatio` no contrato + testes de ratio | CONTEXT | Auto | PASS | record + contrato + testes; exit 0 |
| 5 | Cobertura zero/baixa nao lanca | CONTEXT | Auto | PASS | teste presente; ramo 100% coberto; harness confirma |
| 6 | Toda `[GeneratedRegex]` com `RegexTimeoutMilliseconds` | CONTEXT | Auto | PASS | N=8 T=9 D=1, (T-D)>=N; runtime-check via reflexao segue no teste |
| 7 | `src/TranslateReader/` so `LibraryPageModel.cs`, sem UI nova | CONTEXT | Auto | PASS | diff name-only = exatamente o arquivo; 0 DisplayAlert/Popup novos |

**Totals:** 7 items | Auto: 7 (7 PASS, 0 FAIL) | Manual: 0 pending

## Recommendation

Aprovar e seguir para `/jdi-ship div-paragraph-translation`. As duas unicas observacoes de codigo
novo (W-1 clamp do ratio, W-2 re-strip) sao de baixa severidade, sem consumidor afetado hoje, e
cabem como nota no PR — a UX de cobertura baixa ja esta em `Deferred to PR review` e e o momento
natural de resolver W-1 junto. Nada a corrigir antes do ship.

## DoD Critic (enhanced — forcado por /jdi-issue)

NOTA DE EXECUCAO: rodado inline pelo orquestrador, exit code REAL, gates extraidos por parser
restrito a secao `## Definition of Done`. Mutacoes em copia com restauracao conferida
(`git status --porcelain src/` = 0 apos cada uma).

**OS 7 GATES SAO OCOS — prova objetiva, executada.**

Construi um mutante que COMPILA LIMPO e reintroduz exatamente o defeito que esta phase existe para
corrigir: removi o branch de div da alternacao de `TextBlockRegex`
(`HtmlUtility.cs:193-194`), deixando o regex identico ao de `main` (`p|h[1-6]|li` apenas), com a
virgula ajustada para a sintaxe seguir valida.

| Medida | Resultado |
|---|---|
| `dotnet build src/TranslateReader.Core -c Release` | **0 erros** (o mutante e codigo valido) |
| Os 7 `Verify:` do DoD | **exit 0 nos 7** |
| `dotnet test` | **9 falhas** (`Com falha: 9, Aprovado: 310, Total: 321`) |

Ou seja: com o bug de volta no lugar, o DoD inteiro aprova e so a suite reprova — e **nenhum dos 7
gates roda a suite**. Eles verificam: contagem de ocorrencias de um NOME de teste (`test $(grep -rho
"PreservesBaselineBlockCount" ... | wc -l) -eq 3`), presenca de outros nomes de teste, presenca do
literal `IsLetter`, presenca de `record BookTranslationResult` e da assinatura, contagem de
`[GeneratedRegex`/`RegexTimeoutMilliseconds`, e o escopo do diff em `src/TranslateReader/`.
Todos sao propriedades de FORMA do arquivo; nenhum e propriedade de COMPORTAMENTO.

Constatacao adicional da mesma familia: numa tentativa anterior deixei `HtmlUtility.cs`
sintaticamente QUEBRADO (2 erros de compilacao) e os 7 gates continuaram exit 0 — o DoD nem sequer
exige que o projeto compile.

Isto nao questiona o TRABALHO: os testes existem, mordem (9 falhas contra o mutante), o vermelho-
primeiro foi verificado pela reviewer rodando a suite pre-fix, e o probe do orquestrador contra o
EPUB real do usuario mede 1.910 blocos e 100,0% de cobertura (era 360 e 12,6%). O que nao presta e a
PROVA: do jeito que os gates estao escritos, uma regressao futura no seletor passa verde pelo DoD.
E a mesma familia ja catalogada em `.jdi/todos.md` (`[PROCESSO/DoD]`) e que bloqueou
`the-method-refactor` (2x), `sonar-zero-issues` e `coverage-90` — com o agravante de que aqui
NENHUM gate executa teste, enquanto naquelas phases ao menos um executava.

Correcao esperada: pelo menos um gate precisa EXECUTAR a suite com piso de testes casados e
`Failed: 0` (o padrao que `coverage-90` e `conversion-performance` ja usam), de modo que o mutante
acima seja reprovado pelo DoD e nao so pela suite rodada a mao pela reviewer.

**Verdict:** BLOCKED
