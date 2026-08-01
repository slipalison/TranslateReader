# Phase 17: Review (slug: div-paragraph-translation)

**Verdict:** APPROVED_WITH_WARNINGS

REVIEW FINAL da phase — iter 3 (re-verify única da rodada de warnings do `/jdi-issue`), regenerada
do zero e auto-suficiente. Diff revisado: `main` (`ad607ac`) → HEAD (`dc3b7f3`), branch
`jdi/div-paragraph-translation`, 21 commits, todos Conventional Commits com scope
`div-paragraph-translation` (0 fora do padrão). Reviewer: `jdi-reviewer-translatereader`
(Fable 5, xhigh). Toda evidência abaixo foi medida NESTA re-verificação — nada foi herdado do
transcript do doer sem re-execução.

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0`: 0 Error(s), 8 warnings legados (CS0618/CS0414/MVVMTK0045) |
| Tests | PASS | **Failed: 0, Passed: 320, Skipped: 2, Total: 322** (baseline D-2 = 167; piso da phase = 320/322) + JS `node --test test/js/`: 60/60 |
| Coverage | PASS | Arquivo novo pós-`4285f25` (`BookTranslationResult.cs`): **line 100% / branch 100%**. Tocados: `HtmlUtility.cs` 100/100, `TranslationManager.cs` 100/100 (ramos legados `TranslateChapterAsync`/`TranslateParagraphsAsync` 83,3% branch — fora do escopo, D-2). Agregado 88,45% linha (contexto, não é o gate). Threshold 90% (D-6): atendido |
| Lint | WARN | `dotnet format --verify-no-changes`: 9 violações WHITESPACE, TODAS legadas (W-3, lista idêntica à conhecida); **0 em linha tocada pela phase** — `TranslationManagerTests.cs:528-529` está fora dos hunks da phase (hunks: `@@ -331`, `@@ -744`) |
| Security/Layer | PASS | 5.1/5.2/5.3/5.7/5.9/5.10/5.14/5.15(Result)/5.16/5.17: 0 hits. `TranslationManager.cs:61` `catch (OperationCanceledException)` persiste "Paused" e **`throw;`** — OCE flui (csharp.md §1). Baselines legados inalterados: zip 2 hits (ParsingEngine, intocado), eventos 5+=/4-=, 1 static mutável (TranslationEngine:16), 5 catches legados (W-5, registrados) |
| Consistency | PASS | 21/21 commits conformes; `files_modified` do PLAN todos no log; extras justificados (CalibreFixtures.cs → D-...-10; todos.md/LOOP.md/ROADMAP.md → processo); `.gitignore` em **0** commits da phase |
| UI Validation | SKIPPED | has_frontend=false (cliente MAUI nativo) — por design, nunca bloqueia |
| DoD | PASS | **8/8 Auto PASS, 2 corridas completas, exit code real, 0 Manual** (dod=auto_only). Comandos extraídos LITERALMENTE do CONTEXT via `sed` — não re-digitados |

## Iter 3 — o que esta re-verificação provou (ceticismo a–h)

**(a) A extração da fixture compartilhada NÃO enfraqueceu nada.** Provado por três vias
independentes:
- (i) O diff `24b473c..HEAD -- test/` altera SOMENTE declarações de `const` (3 arquivos); nenhuma
  linha de asserção tocada.
- (ii) Nomes de método de teste extraídos das duas revisões e comparados por `diff`: **idênticos,
  304 métodos** — nenhum rename silencioso; os `Verify:` que grepam nome literal continuam medindo
  os mesmos testes.
- (iii) Contagem de `[Fact]`/`[Theory]` POR ARQUIVO idêntica entre `24b473c` e HEAD (271 `[Fact]`
  + 18 `[Theory]`).
- Probe próprio (reflexão sobre o assembly de teste compilado, fora do repo): a extração devolve
  os MESMOS números nos dois estados — Fixture A: 3 blocos, 106/113 = 0,93805 tanto no literal
  antigo reconstruído quanto no `CalibreFixtures.PartiallyCoveredBody` atual.
- **Ressalva de precisão (não de comportamento):** a alegação "byte-idêntico" do doer é imprecisa
  em dois pontos, ambos provados no probe e ambos neutros para toda asserção da suite:
  `CalibreChapterHtml` composto perdeu os **2 newlines** que o raw literal antigo tinha em volta do
  corpo (`<html><body>\n...\n</body></html>` → `<html><body><div...`); e `CalibreFixtures.cs` está
  LF no worktree enquanto os arquivos antigos estão CRLF, então o newline EMBUTIDO no literal
  compilado mudou de CRLF para LF nesta máquina (vs blob commitado, LF, é idêntico). Caracteres
  não-espaço: **idênticos** nas duas formas (probe: `NonSpace(chapA)==NonSpace(oldChap)` = True);
  blocos, covered, total e ratio: idênticos. Nenhum gate afrouxou (ver b). Ver Warnings W-N1.

**(b) O DoD continua mordendo.** Mutante do critic reaplicado por mim (branch de div removido da
alternação de `TextBlockRegex`, sintaxe válida, compila): itens **2, 3, 4, 6 e 8 reprovam**
(exit 1), itens 1/5/7 passam — **exatamente o mesmo perfil 5/8 da iter 2**. A extração da fixture
não afrouxou gate algum. Arquivo restaurado via `git checkout --`; `git status --porcelain src/
test/` vazio depois.

**(c) W-2 — julgamento da medição do doer.** A medição é honesta e a decisão de NÃO otimizar está
correta, com uma correção de registro em cada direção:
- **Meu warning original estava impreciso**: a premissa de alocação ("alocação O(texto do livro)")
  estava ERRADA. Confirmei no probe: `Regex.Replace` devolve a MESMA instância quando o padrão não
  casa (`ReferenceEquals` = True), e bloco já stripped não casa nada. O custo real é só a
  varredura do regex (~0,4–1,9 ms por livro inteiro; meu probe com 2.000 divs / 552.000 chars:
  re-strip 2,250 ms vs direto 1,835 ms — mesma ordem de grandeza da medição do doer). Irrelevante
  frente a minutos/horas de inferência LLM. Registro de auditoria, não concessão.
- **O argumento nº 3 do doer também não se sustenta**: "a variante direta diverge em HTML
  malformado com `<` cru" não vale para ESTE caminho. Fuzz de 300.000 corpos aleatórios
  (`<>ab /"=x`): `StripHtmlTags` é idempotente (0 falhas) e a contagem direta sobre blocos JÁ
  extraídos (que saem de `BlockText` = já stripped) coincide com o re-strip em 100% dos casos
  (0 divergências). A divergência que ele descreve existe no nível do CORPO (é o W-1), não no
  nível dos blocos. A decisão fica de pé pelos motivos nº 1 e nº 2; o nº 3 é inválido. Ver W-N2.

**(d) Auto-correção do doer — caminho append-only confirmado.**
`git diff main..HEAD -- .jdi/DECISIONS.md | grep -c '^-[^-]'` = **0** (e 0 também no recorte
`24b473c..HEAD`). Números do registro final re-medidos por mim e corretos: `"/dest/out.epub"` em
`main` = **10**, em HEAD = **17**, linhas com o literal adicionadas/tocadas pela phase = **7**
(10 + 7 = 17 ✓). A mensagem do commit `d237263` de fato carrega a versão errada ("12 of its 17")
e NÃO foi reescrita — a correção vive em `D-...-10-CORRECAO`, que é o lugar certo (histórico
imutável, registro append-only).

**(e) Os 8 `Verify:` no repo limpo.** Executados 2x (antes e depois da rodada de mutação), exit
code real, comandos extraídos literalmente de `## Definition of Done` do CONTEXT: **8/8 exit 0 nas
duas corridas**. Logs em `TestResults/` (ignorado); `git status --porcelain src/ test/ TestResults/`
vazio ao final.

**(f) Regressão de teste — nenhuma.** `git diff ad607ac..HEAD -- test/`: **0 nomes removidos** do
baseline, **18 adicionados**; nenhuma asserção existente afrouxada;
`HtmlInjectionTests.cs:304` segue `Assert.Equal(8, factories.Count)` (contagem EXATA, não `>=`).

**(g) Cobertura D-6 + Sonar nas linhas da iter 3.** Iter 3 tocou só teste/doc — cobertura de
produção inalterada (100/100 nos tocados). Nos 3 arquivos de teste alterados: `dotnet format`
limpo, nenhum padrão Sonar novo (S1192 é escopo MAIN e o projeto é auto-detectado como test —
verificação do doer confere com o precedente das 2 CA1826 do PR #12, que vieram do importador
`external_roslyn`, pipeline distinto).

**(h) Produção intocada na iter 3.** `git diff 24b473c..HEAD -- src/` = **vazio** (medido).

## Blockers

Nenhum.

## Warnings

Legados (D-2, deliberadamente não corrigidos — todos agora registrados em `.jdi/todos.md` com
`file:line`, nenhum pede ação antes do ship):
- **W-3 (estilo):** 9 violações WHITESPACE do `dotnet format` — `ThemeEngine.cs:12,14`,
  `ReaderPage.xaml.cs:122,124`, `ThemeEngineTests.cs:12`, `TranslationManagerTests.cs:528-529`.
  Encaminhamento: phase `baseline-de-estilo` (`.editorconfig` + format único no repo).
- **W-4 (The Method):** 2 Managers no mesmo `[RelayCommand]` — `LibraryPageModel.cs:105-106`
  (`TranslateBookAsync`), `:111/:171` (`translationManager`), `:175` (`libraryManager`) e `:45`
  via `LoadBooksAsync`. File:lines conferidos no arquivo. Padrão pré-existente; a phase só trocou
  a leitura do retorno.
- **W-5 (cancelamento):** 5 `catch` legados que engolem OCE/exceção —
  `ReaderPage.xaml.cs:308,326,434`, `LibraryPageModel.cs:183`, `ReaderPageModel.cs:222`. Já
  registrados. O único catch de OCE em código tocado pela phase (`TranslationManager.cs:61`)
  **re-lança** — conforme.
- **W-2 (perf, encerrado como "não fazer"):** re-strip em `CountBlockChars` custa ~0,4–1,9 ms por
  livro (medido 2x, por mim e pelo doer) — não pagar. Registrado em todos.md com número.

Novos desta re-verificação (registro de auditoria; nenhum bloqueia nem pede ação antes do ship):
- **W-N1 (precisão de documentação):** a alegação "byte-idêntico" de `D-...-10`/SUMMARY é
  imprecisa para os documentos de capítulo compostos (2 newlines perdidos em `CalibreChapterHtml`)
  e para o EOL embutido (CRLF→LF no worktree desta máquina). Provadamente neutro para todas as
  asserções (non-space chars, blocos e ratio idênticos — probe próprio). Se algum dia uma asserção
  depender de newline DENTRO da fixture, este registro é o aviso.
- **W-N2 (registro de auditoria do W-2):** as duas partes escreveram uma justificativa parcialmente
  errada — o meu warning original superestimou alocação (premissa refutada por `ReferenceEquals`),
  e o argumento nº 3 do doer (não-equivalência em HTML malformado) não vale no caminho de blocos
  (fuzz 300k: 0 divergências; `StripHtmlTags` é idempotente). A DECISÃO (não otimizar) permanece
  correta pelos motivos nº 1 e nº 2. Quem for mexer nisso no futuro: o teste
  `TranslateBookAsync_CoveredTextRatio_IsNeverAboveOneOnMalformedHtml` tem de continuar verde.

## DoD Checklist (gate 8)

Comandos extraídos literalmente do CONTEXT (`sed` sobre as linhas `**Verify:**`), executados 2x no
repo limpo. "Morde?" = comportamento sob o mutante do critic (item b acima).

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | Caracterização dos 3 fixtures reais (3x `PreservesBaselineBlockCount`, contagem + chars não-espaço pinados) passa de verdade | CONTEXT | Auto | PASS | exit 0 (2x); `Passed: 3, Failed: 0` exigidos pelo awk; sob mutante: PASS (esperado — fixtures não têm div-folha) |
| 2 | Fallback de div-folha extrai Fixture A (container/imagem/sem-letra ficam fora), 3 testes executados | CONTEXT | Auto | PASS | exit 0 (2x); **sob mutante: exit 1** |
| 3 | Invariante D-...-7 (disjunção) + simetria D-...-8 (round-trip) executados | CONTEXT | Auto | PASS | exit 0 (2x); **sob mutante: exit 1** |
| 4 | `BookTranslationResult.CoveredTextRatio` (A < 1.0, B == 1.0, nunca > 1.0 — W-1) — 4 testes de ratio passam | CONTEXT | Auto | PASS | exit 0 (2x); **sob mutante: exit 1** |
| 5 | Cobertura zero/baixa não lança (csharp.md §1), teste executado | CONTEXT | Auto | PASS | exit 0 (2x); sob mutante: PASS (esperado) |
| 6 | Toda `[GeneratedRegex]` com `RegexTimeoutMilliseconds` + reflexão runtime + 2 corpos adversariais (ReDoS) | CONTEXT | Auto | PASS | exit 0 (2x); **sob mutante: exit 1** |
| 7 | `src/TranslateReader/` só muda em `LibraryPageModel.cs`, sem UI nova; MAUI compila | CONTEXT | Auto | PASS | exit 0 (2x); diff-scope conferido também à mão; sob mutante: PASS (esperado — mutante compila) |
| 8 | Suite INTEIRA sem filtro: `Failed: 0`, `Passed >= 320`, `Total >= 322` | CONTEXT (D-...-9) | Auto | PASS | exit 0 (2x); medido: 320/2/322; **sob mutante: exit 1** |

**Totals:** 8 items | Auto: 8 (8 PASS, 0 FAIL) | Manual: 0 pending (dod=auto_only)

## Cobertura de extração (números meus, re-medidos nesta verificação)

| Corpo | Blocos | Chars cobertos / total (não-espaço) | Ratio |
|---|---|---|---|
| Fixture A (calibre, sintético) | 3 | 106 / 113 | 0,93805 |
| Fixture A — literal ANTIGO reconstruído (prova de equivalência) | 3 | 106 / 113 | 0,93805 |
| Fixture B (sintético) | 1 | 39 / 39 | 1,00000 |
| Sintético 2.000 div-folha (552.000 chars, probe) | 2.000 | 418.000 / 418.000 | 1,0 |
| Sintético 5.000 div-folha (~250 KB) | 5.000 em < 1 s | — | (teste ReDoS do DoD 6, verde 2x) |
| Wardley Maps (EPUB real) | 2.124 | 678.242 extraídos | baseline preservado (verde 2x) |
| Righting software (EPUB real) | 1.329 | 292.254 extraídos | baseline preservado (verde 2x) |
| Practice Makes Perfect (EPUB real) | 6.102 | 239.075 extraídos | baseline preservado (verde 2x) |

Âncora do bug real: o livro do usuário extrai **1.910 blocos com 100,0% de cobertura** com o
código desta branch (medido pelo orquestrador, fora desta review) — contra **360 blocos / 12,6%**
antes da correção. Os 3 EPUBs reais do repo não mudam de contagem: neles não existe div-folha fora
de `p|h1-6|li`, e a caracterização (contagem E soma de chars) é o cadeado contra regressão.

## Estado final da phase

**Produção (tudo da iter 1 + 1 linha da iter 2; iter 3 = zero produção, medido):**
- `src/TranslateReader.Core/Utilities/HtmlUtility.cs` — `TextBlockRegex` vira união disjunta
  (`p|h[1-6]|li` primeiro; div-folha só sem bloco clássico dentro), predicado `IsTranslatableBlock`
  compartilhado entre `ExtractTextBlocks` e `ReplaceTextBlocksInHtml` (D-...-7/D-...-8),
  `CountTextChars` novo; toda regex com timeout de 1 s.
- `src/TranslateReader.Core/Models/BookTranslationResult.cs` (novo) — `record (EpubPath,
  CoveredTextRatio)`.
- `src/TranslateReader.Core/Contracts/Managers/ITranslationManager.cs` — **mudança de contrato
  público**: `TranslateBookAsync` devolve `Task<BookTranslationResult>` (antes `Task<string>`).
- `src/TranslateReader.Core/Business/Managers/TranslationManager.cs` — agregação do ratio dentro
  do rebuild (zero I/O novo), clamp `Math.Min(1.0, ...)` (W-1), nunca lança por cobertura baixa.
- `src/TranslateReader/PageModels/LibraryPageModel.cs` — ÚNICO arquivo MAUI tocado, ajuste
  mecânico (2 leituras `translation.EpubPath`), zero UI nova.

**Só teste/doc:** +18 testes (0 removidos, 0 renomeados), `CalibreFixtures.cs` (fixture única
compartilhada, iter 3), `ExtractTextBlocksBaselineTests.cs` (caracterização com I/O autorizado),
DoD endurecido para 8 itens comportamentais (D-...-9), decisões D-...-0..-10 + CORRECAO
append-only (0 deleções, medido), warnings registrados em `.jdi/todos.md` com file:line.

**Números finais:** suite 320 passed / 2 skipped / 322 total (piso 320/322; baseline D-2 167);
JS 60/60; build Windows Release 0 erros; cobertura 100% linha+branch em `HtmlUtility`,
`TranslationManager` e `BookTranslationResult`; DoD 8/8 em 2 corridas; mutante do critic reprova
em 5/8 gates; 21 commits conformes; `.gitignore` fora de todos.

## Para o revisor humano do PR

Em 1 minuto:
- **Origem:** bug report real — o usuário converteu um EPUB gerado pelo calibre e só **12,6%** do
  texto foi traduzido, sem qualquer aviso (calibre envolve cada parágrafo em `<div class="calibreN">`
  e o seletor antigo só via `p|h1-6|li`). A phase corrige a seleção E cria o sinal que impede o
  silêncio: `CoveredTextRatio`.
- **Mudança de contrato público (breaking para consumidores do Core):**
  `ITranslationManager.TranslateBookAsync` agora devolve `BookTranslationResult(EpubPath,
  CoveredTextRatio)` em vez de `string`. Único call site no app: `LibraryPageModel.cs` (ajuste
  mecânico, único arquivo MAUI tocado — conferido por diff-scope no DoD 7).
- **O que o gate NÃO prova:** (1) UI/aviso ao usuário quando `CoveredTextRatio` for baixo — o
  sinal existe e é testado, mas mostrar/threshold é decisão de produto deferida a você; (2)
  execução em device real (a rede de testes cobre o Core; o head MAUI só compila); (3) Quality
  Gate do SonarCloud — só existe após push+CI; (4) fidelidade das Fixtures A/B à forma calibre
  real — leitura humana deferida (a âncora de 1.910 blocos/100,0% no livro real mitiga).
- **Custo esperado:** o livro-origem passa de ~360 para ~2,2k blocos traduzidos — a corrida de
  tradução fica ~8x mais longa. É consequência da correção (o texto agora é visto), não defeito.

## Recommendation

Ship. Nenhum blocker; os warnings abertos são legados cobertos por D-2, registrados com file:line
e com encaminhamento (phase `baseline-de-estilo` / backlog), e os dois registros novos (W-N1/W-N2)
são de precisão de documentação/auditoria, provadamente sem efeito em comportamento ou em gate.
O DoD desta phase reprova o mutante que reintroduz o defeito — o critério de saída da iter 1 está
satisfeito e re-provado nesta re-verificação. Próximo passo: `/jdi-ship div-paragraph-translation`
(--pr), com os itens de `## Deferred to PR review` no corpo do PR.

**Verdict:** APPROVED_WITH_WARNINGS

## DoD Critic (enhanced — forcado por /jdi-issue, passe final antes do ship)

NOTA DE EXECUCAO: rodado inline pelo orquestrador. `git diff 24b473c..HEAD -- .../CONTEXT.md` e
VAZIO — nenhuma linha do DoD mudou desde a aprovacao do critico na iter 2, entao aquele julgamento
permanece valido e nao precisa ser refeito do zero. A rodada de warnings mexeu so em teste
(extracao da fixture compartilhada), producao intocada, e a reviewer reaplicou o mutante do critico
de forma independente, obtendo o MESMO perfil: itens 2, 3, 4, 6 e 8 reprovam.

O ponto que a iter 3 poderia ter estragado — e nao estragou — era a extracao da fixture calibre para
`CalibreFixtures.cs`: varios `Verify:` grepam NOMES literais de teste e CONTAM ocorrencias, entao um
rename silencioso reprovaria o gate ou, pior, passaria medindo outra coisa. Verificado: 304 nomes de
metodo identicos entre `24b473c` e HEAD, `[Fact]`/`[Theory]` identicos por arquivo, e a extracao
devolvendo os mesmos 3 blocos / 106 de 113 chars / ratio 0,93805 nos dois estados.

Registro de auditoria que a propria reviewer levantou contra si mesma: a alegacao do doer de corpos
"byte-identicos" e imprecisa (2 newlines a menos e EOL CRLF->LF), e o warning original W-2 dela
tinha premissa errada sobre alocacao. Nenhum dos dois muda veredito — mas ficam escritos, que e o
que mantem o rastro honesto.

Nenhuma linha `Type=Auto`/`PASS` mostrou-se oca.

**Verdict:** APPROVED
