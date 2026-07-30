# Phase 13: Review  (slug: the-method-refactor)

**Verdict:** APPROVED_WITH_WARNINGS

Reviewer: `jdi-reviewer-translatereader` (Fable 5 / xhigh, D-7) | mode=verify, iter=1 | 2026-07-30
Escopo: `jdi/the-method-refactor` vs `main` (base `a390eb9` = merge-base real, verificado).
Commits: `508933b` (T-1), `a5f7b44` (T-2), `3c5a8cf` (T-3), `b9ec38a` (T-4) — 1:1 com o PLAN.

## Gates

| Gate | Comando | Resultado real | Status |
|---|---|---|---|
| 1 Build | `dotnet restore && dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` | `0 Erro(s)`, 40 avisos (todos `MVVMTK0045`, legado do app MAUI intocado), 10.3 s | PASS |
| 2 Tests | `dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj` | **201 aprovados / 2 ignorados / 203 total, 0 falhas**, ~4 s. Baseline da fase 196/2/198 (+5 = +4 `FileUtilityTests` +1 `ReadingManagerTests`); baseline D-2 167 — sem regressao. Os 2 ignorados seguem sendo os `[Fact(Skip=...)]` de `TranslationEngineTests` | PASS |
| 3 Coverage | `dotnet test --collect:"XPlat Code Coverage"` -> Cobertura XML, intersecao das linhas ADICIONADAS no diff com linhas cobriveis (D-6: 90% em codigo novo/alterado pos-`4285f25`) | Por arquivo tocado (cobriveis alteradas): `ParsingEngine.cs` **90,9%** (10/11; unica descoberta: L126, call site de `OpfTitleRegex` — gap declarado), `ReadingManager.cs` **100%** (1/1), `TranslationManager.cs` **100%** (5/5), `FileUtility.cs` **100%** (1/1), `HtmlUtility.cs` **93,5%** (29/31; L44/L46 = guards de early-return movidos verbatim). Agregado alterado: **46/49 = 93,9%**. Agregado da solucao (contexto, nao e o gate): 75,7%. Unico `.cs` NOVO pos-boundary: `BookTranslationJobAccessTests.cs` (fase anterior, test file) | PASS |
| 4 Lint | `dotnet format --verify-no-changes` (escopo SOLUCAO, `core.longpaths=true`) | **11 violacoes WHITESPACE**, todas legadas: `ThemeEngine.cs(12,24)(14,11)`, `ReaderPage.xaml.cs(122,103)(124,72)`, `HtmlInjectionTests.cs(25,1)(42,1)`, `ThemeEngineTests.cs(12,33)`, `TranslationManagerTests.cs(528,21)(528,33)(528,61)(529,31)`. Baseline era 12; `ReadingManager.cs(55,1)` foi limpa no hunk de T-1. **Zero violacao nos 8 arquivos tocados** | WARN (legado, D-2) |
| 5 Security/Layer | Bateria 5.1-5.17 (greps + julgamento manual, detalhado abaixo) | 5.1 Client->Access/Engine: vazio. 5.2 storage tech em `Contracts/Access`: vazio; `Contracts/Utilities`: vazio (contrato novo limpo). 5.3 Manager->Manager: 4 hits, todos `: I<Proprio>Manager` (implementacao propria — ok). 5.10 sync-over-async: vazio. 5.10b `catch OCE`: `TranslationManager.cs:61` faz `throw;` (rethrow correto); 3 swallows restantes sao legado do app MAUI intocado. 5.12 static mutavel: 1 hit = baseline legado (`TranslationEngine.cs:16`). 5.11 eventos: subscribe=5/unsubscribe=4 = baseline do bootstrap, 0 `+=` novos. 5.14 `new Regex(`: vazio; `Substring/ToLower==` em Engines+Utilities: vazio. 5.15 catch vazio: 5 hits, todos legado app MAUI. 5.16 TODO sem ticket: vazio. 5.6 zip: 2 hits baseline em `ParsingEngine` (vetor real e propriedade da phase `epub-zip-slip`; linhas 59-60/31-32 NAO tocadas — confirmado no diff). 5.7 XXE: vazio. 5.9 secrets/PII: vazio | PASS (warns legados pre-existentes) |
| 6 Consistency | `git log --name-only a390eb9..HEAD` x PLAN `files_modified`; subjects x D-4 | Match 1:1 exato: T-1=`508933b` (5 arquivos), T-2=`a5f7b44` (1), T-3=`3c5a8cf` (2), T-4=`b9ec38a` (docs). Conventional commits com scope `the-method-refactor`, tipos `refactor`/`docs` apropriados (nao tudo `feat`). T-2/T-3 sem teste novo por decisao locked (D-...-4/5) — consistente com acceptance do PLAN | PASS |
| 7 UI live | — | SKIPPED permanente (`has_frontend=false`, app MAUI nativo) | SKIPPED |
| 8 DoD | 5 `Verify:` do CONTEXT.md rodados LITERALMENTE (bash) | **5/5 Auto `exit=0`**, 0 Manual. Medidas cruas: 7 `[GeneratedRegex]` em `ParsingEngine`, exatamente 4 `public static` dos 4 nomes em `HtmlUtility`, 8 attrs em `ReadingManagerTests`, 13 em `FileUtilityTests`, 197 attrs totais, 0 arquivos de `src/TranslateReader/` no diff, 0 csproj com BenchmarkDotNet. PROJECT.md nao tem secao DoD (fonte unica = CONTEXT.md; INCONCLUSIVE exigiria ausencia em AMBOS) | PASS |

## Auditoria cetica (pontos exigidos pelo dispatch)

**(a) Os 7 regex de `ParsingEngine` — risco #1 da fase: CONFIRMADO preservado, padrao a padrao.**
Diff `a390eb9..HEAD` inspecionado ocorrencia por ocorrencia. Os 7 patterns sao byte-identicos aos
inline antigos e o `RegexOptions` migrou para dentro do atributo em TODOS: `OpfTitleRegex`
`IgnoreCase|Singleline` (era 4o arg do `Replace`), `LinkTagRegex` `IgnoreCase` (era 4o arg, apos o
MatchEvaluator), `StylesheetRelRegex`/`StylesheetHrefRegex` `IgnoreCase` (era 3o arg de
`IsMatch`/`Match`), `ImgSrcRegex`/`SvgImageXlinkHrefRegex`/`SvgImageHrefRegex` `IgnoreCase` (era 4o
arg). Replacement strings e evaluators intactos. Nenhum option perdido. Nuance registrada em W-5.

**(b) MOVE dos 4 metodos HTML: CONFIRMADO MOVE puro, linha a linha.**
`git diff a5f7b44 3c5a8cf`: corpos de `ExtractParagraphs`/`ExtractTextBlocks`/
`ReplaceTextBlocksInHtml`/`StripHtmlTags` byte-identicos (unica mudanca: `private`->`public`);
os 3 `[GeneratedRegex]` (`ParagraphRegex`, `TextBlockRegex`, `HtmlTagRegex`) com pattern e options
identicos; 4 call sites roteados por `HtmlUtility.X(...)` (L140/L185/L188/L217); `TranslationManager`
perdeu `partial` e o using orfao; `HtmlUtility` virou `public static partial class`. Grep confirma
zero residuo de `Regex`/definicao privada no Manager. Sem colisao de nomes. Utility continua
passando no teste da maquina de cappuccino (manipulacao generica de HTML, sem regra de traducao).

**(c) Gap de discriminacao dos regex (1 de 7): ACEITAVEL para esta fase — vira W-1, nao blocker.**
O claim do doer e coerente com a evidencia de cobertura que eu mesma medi: L126 (`OpfTitleRegex`)
tem **0 hits** (nunca executa — indiscriminavel por definicao); em `InlineCssLinks` o caminho feliz
executa (L193/196/199 cobertas) mas os early-returns L197/201/206 nao — execucao sem assercao, exatamente
o que faz mutacao de `StylesheetRelRegex` passar em silencio. Julgamento: (1) a prova de equivalencia
locked para esta fase e inspecao (D-...-2 (B)), e eu a refiz independentemente no item (a) —
transcricao verbatim confirmada; (2) fechar o gap exigiria fixture nova com I/O de disco em teste
novo, proibido por `.claude/rules/csharp.md` §6, ou refactor da API para stream (fora do estatuto
finding-driven); (3) o gap foi MEDIDO por mutacao, confessado na SUMMARY (corrigindo a
superestimativa do PLAN) e registrado em `.jdi/todos.md` com caminho de saida nomeado. Esconder
teria sido blocker; medir e declarar e o comportamento certo.

**(d) Probes do DoD provam o criterio, nao so exit 0.**
DoD 3: `-eq 4` exato pega tanto ausencia quanto duplicacao acidental, pareado com `-eq 0` dos
`private static` no Manager; o loophole restante (mover sem rotear) e fechado por build+testes
(Gates 1-2). DoD 5: `git merge-base main HEAD` resolve para `a390eb9` (verificado — fork point
real), e o diff de `src/TranslateReader/` e genuinamente vazio (0 arquivos, cruzado com
`git diff --stat`). DoD 4: par presenca-positiva (`>=7 [GeneratedRegex]`, `partial class`) /
ausencia-negativa (`Regex.(Replace|Match|IsMatch)( == 0`), com `new Regex(` vazio no gate 5.14
fechando a rota de bypass. DoD 1/2: `grep -q` poderia casar comentario, mas os arquivos foram
LIDOS — a chamada e real e as assercoes `DidNotReceive` sao load-bearing. Fraqueza teorica
registrada (nao ocorreu aqui): probes no formato `test $(cmd | wc -l) -eq 0` dao falso PASS se a
ref/comando falhar com stdout vazio — licao ja catalogada em todos.md `[PROCESSO/DoD]`, reforco
para phases futuras: validar a ref antes do count.

**(e) `IFileUtility` com 7o metodo: OK.** Continua **1 contrato so** (dentro do "max 2 por
servico"); `bool DirectoryHasContent(string directoryPath)` e comportamental, com `<summary>`
(csharp.md §7), e **nao vaza tecnologia de storage** — grep de `Sqlite|System.IO|Directory.|File.`
em `Contracts/Utilities/` vazio; `Directory.*` existe so na implementacao (`FileUtility.cs:42-43`).
O estouro do "3-5 ideal" fica em W-3 (ja aceito no CONTEXT > Notes).

**(f) Cobertura de codigo ALTERADO, por arquivo:** ver Gate 3 — 90,9% / 100% / 100% / 100% /
93,5%, agregado 93,9%, piso 90% respeitado em todos.

## Blockers

_(nenhum)_

## Warnings

- **W-1 [teste/regressao futura]** `src/TranslateReader.Core/Business/Engines/ParsingEngine.cs:321-341` —
  4 dos 7 `[GeneratedRegex]` (`OpfTitleRegex`, `LinkTagRegex`, `StylesheetRelRegex`,
  `StylesheetHrefRegex`) nao sao discriminados por teste algum, e os 2 de `<image>` so mordem
  juntos (medido por mutacao pelo doer, coerente com a cobertura que eu medi: `ParsingEngine.cs:126`
  0 hits; early-returns de `InlineCssLinks` L197/201/206 descobertos). Regra em tensao:
  `.claude/rules/csharp.md` §6 (caminhos de falha cobertos) vs §6 (proibicao de I/O de disco em
  teste novo) — a segunda vence nesta fase (D-...-2 (B)). Registrado em `.jdi/todos.md` § `[TESTE]`;
  proxima regressao nesses 4 padroes passa em silencio ate a phase de integracao/stream-API.
- **W-2 [regra vs decisao locked]** `test/TranslateReader.Tests/FileUtilityTests.cs:102-134` — os 4
  testes novos de `DirectoryHasContent` fazem I/O real em temp dir, em tensao literal com
  `.claude/rules/csharp.md` §6 ("no disk in unit tests" pos-boundary). Aceito porque: exigido
  textualmente pelo DoD 2 do CONTEXT, locked em D-2026-07-30-the-method-refactor-3, padrao
  pre-existente do arquivo, e `FileUtility` E o wrapper de filesystem — mocka-lo testaria nada.
  Desvio documentado, nao precedente geral.
- **W-3 [contrato acima do ideal]** `src/TranslateReader.Core/Contracts/Utilities/IFileUtility.cs:13` —
  7a operacao no contrato, acima do "3-5 operacoes por contrato (ideal)" de CLAUDE.md; segue 1
  contrato unico. Aceito no CONTEXT > Notes; se um 8o metodo aparecer, considerar split.
- **W-4 [lint legado]** 11 violacoes WHITESPACE legadas (lista no Gate 4), isentas por D-2; melhora
  de 12 -> 11. Vira BLOCK-on-new-files quando `baseline-de-estilo` entregar `.editorconfig`.
- **W-5 [informativo, sem acao]** Os 7 regex migrados trocam o case-folding de cultura CORRENTE
  (`Regex` estatico + `IgnoreCase`) para INVARIANTE (default de `[GeneratedRegex]` sem
  `cultureName`). Observavel apenas em culturas tipo tr-TR/az, e na direcao de MAIOR robustez para
  tags/atributos HTML — alinhado ao espirito de csharp.md §2.1 (ordinal para tags). Mesmo default
  ja usado pelos `[GeneratedRegex]` pre-existentes de `TranslationManager`/`HtmlUtility`.

### Baseline legado pre-existente (inalterado por esta fase, ja conhecido — nao conta como warning novo)

`catch (OperationCanceledException) { }` em `LibraryPageModel.cs:183`, `ReaderPageModel.cs:222`,
`ReaderPage.xaml.cs:308` + `catch { }` em `ReaderPage.xaml.cs:326,434` (app MAUI, zero diff nesta
fase); desequilibrio de eventos 5 `+=` / 4 `-=` (baseline do bootstrap); 1 static mutavel legado
(`TranslationEngine.cs:16`); zip-slip em `ReadingManager.cs:59-60`/`FileUtility.cs:31-32` —
propriedade exclusiva da phase `epub-zip-slip` (D-2026-07-29-epub-zip-slip-1), confirmado nao
tocado no diff.

## DoD Checklist (gate 8)

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | `IFileUtility.DirectoryHasContent` existe; `ReadingManager` roteia por ele; zero `Directory.(Exists\|GetFileSystemEntries)` no Manager | CONTEXT | Auto | PASS | exit=0; chamada real em `ReadingManager.cs:53`, contrato + impl presentes |
| 2 | Gap regression-suite-5(1) fechado: teste mockado do branch "ja extraido" + caso real em `FileUtilityTests` | CONTEXT | Auto | PASS | exit=0; 8 attrs em `ReadingManagerTests` (skip provado com `DidNotReceive` duplo), 13 em `FileUtilityTests` |
| 3 | 4 metodos HTML fora do `TranslationManager`, publicos/estaticos em `HtmlUtility` | CONTEXT | Auto | PASS | exit=0; 0 `private static` dos nomes no Manager, **exatamente 4** `public static` em `HtmlUtility` |
| 4 | `ParsingEngine` partial + 7 `[GeneratedRegex]` + zero `Regex.Replace/Match/IsMatch` estaticos | CONTEXT | Auto | PASS | exit=0; contagens reais 0 / presente / 7 |
| 5 | Guardrail: zero diff em `src/TranslateReader/`, zero BenchmarkDotNet, attrs >= 193 | CONTEXT | Auto | PASS | exit=0; 0 arquivos / 0 csproj / 197 attrs |

**Totals:** 5 items | Auto: 5 (5 PASS, 0 FAIL) | Manual: 0 pending

Nota: `.jdi/PROJECT.md` nao contem secao `## Definition of Done`; o DoD desta fase vem
integralmente do CONTEXT.md (INCONCLUSIVE exigiria ausencia em ambos — nao e o caso).

## Deferred to PR review (itens humanos do CONTEXT — fora do verdict automatico)

- Leitura humana: o MOVE do achado #2 manteve nomes/estilo coerentes com `HtmlUtility` existente
  (julgamento subjetivo; a evidencia objetiva — corpos identicos, mesmo padrao de chamada de
  `ExtractBodyContent` — esta no item (b) acima).
- Revalidar o deferimento do seam `TranslationEngine`/LLamaSharp (D-...-6, YAGNI) quando
  `llm-mobile` comecar.
- Confirmar que a auditoria finding-driven (3 fechados, 2 deferidos) nao deixou violacao obvia
  adicional no Core — varredura exaustiva nao foi escopo.

## Recommendation

Aprovar e seguir para `/jdi-ship the-method-refactor`. Os 3 achados foram fechados exatamente como
locked (MOVE/ROUTE verbatim, sem mudanca de logica — verificado por inspecao independente, nao por
confianca na SUMMARY), a rede nao regrediu (201/2/203 vs 196/2/198) e o codigo alterado esta acima
do piso de 90%. Os warnings sao de rastreamento (W-1 e o unico com risco real futuro e ja esta em
`.jdi/todos.md`); nenhum exige retrabalho nesta fase. Recomendo que a phase de integracao futura
(ou `epub-zip-slip`, que ja vai criar fixtures de EPUB malicioso) absorva assercoes para
`InlineCssLinks`/`UpdateOpfTitleAsync` — fechar W-1 no mesmo esforco de fixture.

**Verdict:** APPROVED_WITH_WARNINGS

## DoD Critic (enhanced — forcado por /jdi-issue)

Re-exame read-only das 5 linhas `Type=Auto` com `Status=PASS`. O critico so pode tornar o veredito
mais estrito, nunca mais frouxo. Resultado: 4 de 5 linhas com PASS genuino (artefato inspecionado
independentemente do `Verify:`, com backstop de mutacao nas linhas 1-3 e precondicoes de `merge-base`
resolvidas na linha 5). Uma linha oca com prova objetiva:

- DoD row «Achado #3: `ParsingEngine` vira `partial class`, os 7 padroes regex inline viram
  `[GeneratedRegex]`, zero `Regex.Replace/Match/IsMatch` estaticos restantes»: **hollow=true,
  objective=true**. O `Verify:` conta `[GeneratedRegex` >= 7 e nunca checa QUAIS patterns/options —
  ao contrario do DoD 3, que nomeia os 4 metodos. Contra-exemplo EXECUTADO (copia em scratchpad, repo
  intocado): pattern de `StylesheetRelRegex` corrompido (`stylesheet` -> `stylsheet`) E
  `RegexOptions.IgnoreCase` removido — exatamente a armadilha de migracao que o PLAN nomeou como risco
  #1 — e o `Verify:` literal do DoD 4 saiu com `exit 0`. A rede de testes nao fecha o furo: o proprio
  doer mediu (`SUMMARY` T-2) que `StylesheetRelRegex`, `OpfTitleRegex`, `LinkTagRegex` e
  `StylesheetHrefRegex` sozinhos produzem **0** falhas; a cobertura confirma `ParsingEngine.cs:126`
  com 0 hits e os early-returns de `InlineCssLinks` descobertos. Ou seja: `Verify:` verde + 203 testes
  verdes coexistem com codigo comportamentalmente errado em 4 dos 7 padroes. O que provou a
  conformidade de fato foi a inspecao manual byte-a-byte do reviewer (item (a) acima) — prova que vive
  FORA do gate automatizado, e portanto nao sobrevive a proxima phase.

Mesma classe de defeito ja catalogada em `.jdi/todos.md` (`[PROCESSO/DoD]` e a regra Semgrep
`translatereader-zip-slip` que nunca podia disparar no caminho real): o gate mede um proxy conveniente
em vez da propriedade real.

**Verdict:** BLOCKED
