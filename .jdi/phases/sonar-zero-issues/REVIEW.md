# Phase 14: Review  (slug: sonar-zero-issues)

**Verdict:** APPROVED_WITH_WARNINGS

Reviewer: `jdi-reviewer-translatereader` (mode=verify, iter=1).
Escopo: `6132078` (main) → `068bcef` (HEAD, `jdi/sonar-zero-issues`), 12 commits (11 da fase + registro de roadmap).
Toda evidência abaixo foi medida por execução própria — nada foi aceito do SUMMARY sem re-medição.
Nota de método: as provas por mutação (gate d) editaram `ParsingEngine.cs` temporariamente e o
restauraram via `git checkout --` (verificado: `git status --porcelain` limpo ao final). Nenhuma
mudança persistente em `src/` ou `test/`.

## Gates

| Gate | Comando | Resultado real | Status |
|---|---|---|---|
| 1 Build | `dotnet restore && dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` | **0 erros**, 8 warnings (CS0618/CS0414, todos em `PageModels/`/`Pages/` do app MAUI, pré-existentes — nenhum em arquivo da fase) | PASS |
| 2 Tests | `dotnet test test/TranslateReader.Tests/... -c Release` | **256 total / 254 aprovados / 2 ignorados / 0 falhas**. Baseline main: 229 (227/2). +27 casos (= 12 HtmlInjection + 3 ParsingEngine + 4 theories×3 CultureRoundTrip). Skips: exatamente os mesmos 2 do LLamaSharp | PASS |
| 3 Coverage | `--collect:"XPlat Code Coverage"` + cruzamento `git diff -U0` × Cobertura | D-6 (90% sobre código alterado): **68/68 linhas alteradas cobráveis cobertas = 100,0%** (8/8 arquivos a 100% — ver checagem i). Agregado: 88,53% (contexto, não é o gate). Arquivos `.cs` NOVOS pós-`4285f25`: só testes (`CultureRoundTripTests.cs` + 2 de fases anteriores) → gate por arquivo novo sem objeto | PASS |
| 4 Lint | `dotnet format TranslateReader.slnx --verify-no-changes` | exit 2, **9 violações WHITESPACE** — `ThemeEngine.cs` (2), `ReaderPage.xaml.cs` (2), `ThemeEngineTests.cs` (1), `TranslationManagerTests.cs` (4). **Nenhuma em arquivo tocado pela fase** (nenhum desses 4 arquivos está no diff) → legado, D-2 | WARN |
| 5 Security/Layer | greps 5.1–5.17 do reviewer + leitura manual | 5.1/5.2/5.10/5.14/5.15(Core)/5.16/5.17 sem hit; 5.3 só auto-interface (`: ITranslationManager` etc.); 5.10b `TranslationManager.cs:61` catch OCE **relança** (`throw;`); 5.11 eventos 5/4 = baseline bootstrap (fase não toca Pages/PageModels); 5.12 só o static legado conhecido (`TranslationEngine.cs:16`); catch `{ }` de `ReaderPage.xaml.cs:326,434` são legado não tocado. Zip (5.6): mudança `Open`→`OpenAsync` não altera manuseio de path; sem extração entry→disco | PASS |
| 6 Consistency | `git log --name-only 6132078..068bcef` × PLAN | Arquivos tocados = exatamente o `## Files modified` do PLAN (inclusive `TranslationManagerTests.cs` INTOCADO, como T-7 exigia). 12 commits Conventional, scope `sonar-zero-issues`, tipos variados e apropriados (chore/refactor/fix/ci/docs) — não é tudo `feat` | PASS |
| 7 UI Validation | — | SKIPPED (has_frontend=false, MAUI nativo) | SKIPPED |
| 8 DoD | 10 `Verify:` do CONTEXT.md executados literalmente | **10/10 exit 0** (saída real na checklist abaixo); 0 itens Manual | PASS |

## Blockers

Nenhum.

## Warnings

- **W-1 (checagem c) — o mecanismo anti-recorrência cobre a promessa do card só em parte.**
  `.github/workflows/sonarqube.yml:104` (`sonar.qualitygate.wait=true`). Medido na API pública: o
  gate do projeto é o **"Sonar way" built-in (id 9, isCleanAsYouCode=true)** com 6 condições, TODAS
  de New Code (`new_security_rating≤A`, `new_reliability_rating≤A`, `new_maintainability_rating≤A`,
  `new_coverage≥80`, `new_duplicated_lines_density≤3`, `new_security_hotspots_reviewed=100`).
  O que ele pega: 1 bug ou vulnerability novo em linha nova/alterada derruba o rating de A → job
  obrigatório falha (cobre os tipos BUG/VULNERABILITY das 113). O que ele NÃO pega, nomeado:
  (1) **code smells novos abaixo de 5% de debt ratio** — o rating de maintainability é razão de
  débito, não contagem; em particular as famílias INFO desta fase (SYSLIB1045, CA1816, CA1875,
  CA1847, xUnit1004 — ~metade dos 104 smells) praticamente não movem o rating e passariam verdes;
  (2) **issue nova em linha NÃO alterada de arquivo legado** — fora do New Code, invisível ao gate;
  (3) **todo o projeto `src/TranslateReader` (app MAUI)** — não compilado na janela do scanner,
  estruturalmente invisível (já registrado em D-2026-07-30-sonar-zero-issues-6 e `todos.md`; os
  sub-gaps 1 e 2 não estavam nomeados em lugar nenhum até este review). "Evite que esses TIPOS de
  issue voltem" está coberto para bug/vuln em código novo; para smells e legado, parcialmente.
- **W-2 (checagem f) — o `Verify:` do DoD 9 é um gate acomodável (proxy textual).** Medição
  própria: os 2 métodos têm **5 parâmetros cada** (declarações em `TranslationManager.cs:114-119`
  e `147-152`) — refactor real, 8→5, contrato público intacto, 33 `TranslationManagerTests` sem 1
  linha mudada. Mas o awk do DoD dá N=4 nos dois porque, para `TranslateChaptersWithCacheAsync`, a
  janela cai no **call site** (`TranslationManager.cs:59` — 4 vírgulas do call), não na declaração;
  para `TranslateSingleChapterAsync` só cai na declaração porque o doer a declarou antes do
  chamador (desvio declarado no SUMMARY). O gate mede uma janela textual sensível à ordem de
  declaração — mesma família de gate oco do learning da phase `the-method-refactor`. A propriedade
  real está entregue e o desvio foi declarado; o achado é de PROCESSO: `Verify:` futuros de
  contagem de parâmetros devem parsear a declaração, não uma janela de vírgulas.
- **W-3 (gate 4)** — 9 violações `dotnet format` pré-existentes em 4 arquivos fora do diff da fase
  (lista no gate). Ficam para `baseline-de-estilo`; não bloqueiam por D-2.
- **W-4 (idioma)** — nomes e comentários de testes novos em pt-BR
  (`ParsingEngineTests.cs:242` `Practice_CreateTranslatedEpubAsync_GravaCapituloTraduzidoEAtualizaTitulo`
  e irmãos; comentários em `ParsingEngineTests.cs:236-238` e doc de `CultureRoundTripTests.cs:7-11`).
  CLAUDE.md § Idioma: código em inglês. Cosmético, não afeta comportamento.
- **W-5 (checagem b, menor)** — `resourceKey="**/index.html"` nas 2 exclusões multicriteria
  (`sonarqube.yml:79,81`) é glob de repo inteiro: pegaria um futuro `index.html` em outro caminho.
  Conforma byte-a-byte com o que D-...-4/PLAN T-3 especificaram (e hoje só existe 1 `index.html`),
  então não é desvio do doer — apenas apertável numa fase futura para o caminho completo.
- **Nota (não-warning)** — SUMMARY declara baseline de 214 atributos; medição direta em `6132078`
  dá **216** linhas `[Fact]`/`[Theory]` (204 Fact + 10 Theory + 2 Fact(Skip)). Sem impacto: HEAD tem
  235 (+19 atributos = +27 casos, aritmética fecha exata com 229→256); nenhuma regressão possível.

## Veredito ponto a ponto (a)–(i)

- **(a) As 113 issues têm endereço? CONFIRMADO.** Inventário re-somado: 113. Mapa família a
  família verificado no código: `dotnet-install.ps1` 41 (arquivo deletado); `HtmlUtility` 16 =
  15 FIX (7×S6444+7×SYSLIB1045 → 8 `[GeneratedRegex]` com `matchTimeoutMilliseconds`; S3776 L72 →
  `InjectTags` decomposto de verdade em 4 helpers) + 1 waiver SYSLIB1044; JS 17 FIX (8+7+2,
  identidade provada pelo DoD 3); `HtmlInjectionTests` 6 FIX; `ParsingEngine` 4 FIX;
  `index.html` 2 FIX + 2 exclusão; `TranslationManager` 3, `BooksAccess` 3, `ReadingStateAccess` 3,
  `BookTranslationJobAccess` 2, `SettingsAccess` 1, `TranslationEngine` 2 FIX;
  testes 15 FIX (6 CA1816 avulsos + FileUtilityTests 2 + LibraryManagerTests 1 + HtmlInjection 6);
  `TranslationEngineTests` 2 waiver. 67+41+2+3 = 113, sem furo e sem dupla contagem.
  **Silenciamento não registrado: ZERO** — auditoria completa: os únicos pragmas em `src/`+`test/`
  são os 2 pares registrados (SYSLIB1044, xUnit1004); 0 `[SuppressMessage]`, 0 `NoWarn` em csproj,
  0 `sonar.exclusions`.
- **(b) Exclusões/waivers honestos? CONFIRMADO.** Multicriteria (`sonarqube.yml:75-81`): 2 regras
  nomeadas (`Web:S7926`, `css:S4667`) + resourceKey por arquivo, comentário YAML citando
  D-...-3/D-...-4 — estreito (ressalva menor W-5 sobre o glob). Pragma SYSLIB1044
  (`HtmlUtility.cs:147-150`): envolve SÓ `TextBlockRegex`, tem `restore`, comentário cita a decisão
  e o porquê técnico (backreference `\1`). Pragma xUnit1004 (`TranslationEngineTests.cs:59-86`):
  envolve SÓ os 2 `[Fact(Skip=...)]`, tem `restore`, cita D-...-3 + D-...-regression-suite-5(2).
  Nenhuma exclusão por diretório, nenhum pragma sem restore.
- **(c) `sonar.qualitygate.wait=true` é o mecanismo inteiro? PARCIAL — W-1.** Confirmado na API:
  gate "Sonar way", 100% New Code. Pega regressão de bug/vuln em código novo; NÃO pega smell
  abaixo do debt ratio, issue em linha não alterada de legado, nem o app MAUI não escaneado.
  Gap nomeado em W-1; nenhum escopo novo inventado.
- **(d) `OpenAsync` provado por teste? CONFIRMADO por mutação PRÓPRIA.**
  M1 `ParsingEngine.cs:105` `await using var writer` → `var writer`: **1 falha**
  (`Practice_CreateTranslatedEpubAsync_GravaCapituloTraduzidoEAtualizaTitulo`).
  M2 `ParsingEngine.cs:104` remoção de `stream.SetLength(0)`: **1 falha** (mesmo teste — assert de
  igualdade byte-a-byte da entry pega o resíduo). Os 3 testes novos provam flush e conteúdo, não
  apenas execução. Working tree restaurado e verificado limpo após cada mutante.
- **(e) InvariantCulture — DoD oco? NÃO; conformidade honesta e aceitável.** Sonda própria
  (pwsh/.NET): `DateTime.Parse` de string formato "O" dá o MESMO instante com e sem provider em
  ar-SA, th-TH, fa-IR, he-IL, ja-JP — a imunidade da mutação de leitura é propriedade do formato
  "O" (culture-invariant por especificação), não fraqueza do teste. A metade REAL do risco (escrita
  `ToString("O")` → `ToString()`) quebra o round-trip — sonda confirma e os 12
  `CultureRoundTripTests` a prendem. `ParseExact("O", Invariant, ...)` seria igualmente imune à
  mesma mutação (o "O" ignora cultura por definição) — não compraria detectabilidade e mudaria
  comportamento (rejeição de formatos não-"O"), fora do escopo locked ("refactor além do exigido
  pelas regras flagged" está em Out of scope). O doer registrou a prova negativa exatamente como o
  PLAN exigia. Item do DoD entregue: identidade presente + linhas 100% cobertas.
- **(f) T-7/S107 honesto ou cosmético? Refactor HONESTO, gate ACOMODÁVEL — W-2.** 5 parâmetros
  reais em cada método (medido na declaração); `TranslationRun` é record privado de contexto,
  nenhum contrato público mudou, Manager continua orquestração fina (conforme The Method — record
  sem comportamento, só agrupamento de argumentos). O achado é sobre o `Verify:`, não sobre o
  código.
- **(g) Regressão de teste? NENHUMA.** Todas as linhas removidas em `test/` auditadas uma a uma:
  5 `Dispose()` one-liners → blocos mecânicos com `GC.SuppressFinalize`; `FileUtilityTests.cs:99-103`
  FORTALECIDO (S2699: ganhou `Record.ExceptionAsync` + `Assert.Null`); `HtmlInjectionTests`
  `Regex.Matches(...).Count` → `<Gen>Regex().Count(...)` com os MESMOS `Assert.Equal(1, ...)`;
  `HybridWebViewContractTests.cs:196-197` é mecânico como declarado (`items[i].index`→`item.index`,
  mesmas 2 propriedades, `DoesNotContain("JSON.parse")` intacto — acompanha o `for`→`for-of` real
  de `translation.js`); `LibraryManagerTests.cs:175` `Contains("5")`→`Contains('5')` mesma força.
  0 `Skip` novo (2→2, os mesmos), 0 assert virando execução muda. E os testes só cresceram:
  +12 casos novos de `InjectTags`/fallback + checagem por reflexão dos 8 timeouts de regex.
- **(h) Invariantes? CONFIRMADOS.** Diff filtrado em `src/TranslateReader/**/*.cs` = vazio (só
  assets `wwwroot/`); `git diff 6132078..HEAD -- .jdi/DECISIONS.md | grep -c '^-[^-]'` = **0**;
  `git grep -l 'dotnet-install\.ps1' -- . ':(exclude).jdi'` = vazio (exit 1) — nada rastreado
  referencia o script removido; a permissão stale saiu de `.claude/settings.local.json` no mesmo
  commit (`8e6200f`).
- **(i) Cobertura D-6 100%? CONFIRMADO: 68/68 = 100,0%.** Medido cruzando `git diff -U0` (lado
  novo) com o Cobertura fresco desta review, por arquivo: `BookTranslationJobAccess` 2/2,
  `BooksAccess` 3/3, `ReadingStateAccess` 8/8, `SettingsAccess` 1/1, `ParsingEngine` 6/6,
  `TranslationEngine` 1/1, `TranslationManager` 23/23, `HtmlUtility` 24/24 (linhas alteradas
  não-cobráveis = atributos/assinaturas/chaves, fora do denominador do Cobertura).

## DoD Checklist (gate 8)

Fonte: `CONTEXT.md § Definition of Done` (PROJECT.md não tem seção DoD própria; nada filtrado).
Comandos executados byte-a-byte como escritos.

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | `dotnet-install.ps1` removido; zero ref rastreada fora de `.jdi/` | CONTEXT (D-...-1, Verify por D-...-7) | Auto | PASS | exit 0 (arquivo ausente; `git grep` vazio) |
| 2 | `HtmlUtility`: 0 `Regex.*` estático, ≥7 `[GeneratedRegex]`, pragma SYSLIB1044 disable+restore, `InjectTags` ≤4 `if (` | CONTEXT (D-...-3) | Auto | PASS | exit 0 (8 GeneratedRegex, todos com timeout — confirmado também por teste de reflexão) |
| 3 | JS: `.dataset`, for-of, `Number.parseInt`, optional chaining | CONTEXT (D-...-5) | Auto | PASS | exit 0 |
| 4 | `HtmlInjectionTests`: 0 `Matches(...).Count`, ≥3 `[GeneratedRegex]` | CONTEXT (D-...-3) | Auto | PASS | exit 0 |
| 5 | `FileUtilityTests` L95 com assert; CA1816 nos 7; CA1847 | CONTEXT (D-...-3) | Auto | PASS | exit 0 |
| 6 | `TranslationEngine` sealed + SuppressFinalize; pragma xUnit1004; `Fact(Skip` = 2 | CONTEXT (D-...-3) | Auto | PASS | exit 0 |
| 7 | `index.html` lang + title; `user-scalable=no` mantido; waivers no yml | CONTEXT (D-...-3, D-...-4) | Auto | PASS | exit 0 |
| 8 | `ParsingEngine` OpenAsync; `BeginTransactionAsync`; InvariantCulture; S1192 | CONTEXT (D-...-5) | Auto | PASS | exit 0 (+ prova por mutação M1/M2 desta review) |
| 9 | `TranslationManager` ≤7 params; `chapters.Select(...)` | CONTEXT (D-...-5) | Auto | PASS | exit 0 (awk N=4/4; parâmetros reais medidos: 5/5 — ver W-2) |
| 10 | `sonar.qualitygate.wait=true` no `end` | CONTEXT (D-...-2) | Auto | PASS | exit 0 |

**Totals:** 10 items | Auto: 10 (10 PASS, 0 FAIL) | Manual: 0 pending

## Recommendation

Aprovar e seguir para `/jdi-ship sonar-zero-issues`. Os 3 itens de `Deferred to PR review` do
CONTEXT (Quality Gate verde real no SonarCloud, confirmação funcional do WebView pós-migração JS,
julgamento UX de `user-scalable=no`) continuam sendo responsabilidade do PR — não são itens Manual
do DoD e não travam o ship. Encaminhar W-1 (sub-gaps do mecanismo: smells INFO abaixo do debt
ratio e legado fora do New Code) e W-2 (gate de contagem de parâmetros por janela awk) para
`.jdi/todos.md`; W-3/W-4/W-5 são cosméticos ou de fase futura (`baseline-de-estilo`).

## DoD Critic (enhanced — forcado por /jdi-issue)

NOTA DE EXECUCAO: o sub-agente critico foi terminado no meio por limite de sessao da API. O gate
NAO foi pulado — o orquestrador rodou o ataque adversarial inline (mesmo protocolo: contra-exemplo
executado em copia no scratchpad, repo real nunca mutado). Cobertura desta passagem: as 10 linhas
`Type=Auto`/`PASS`, com foco nos 2 achados que o reviewer marcou como acomodaveis e nas familias de
maior risco.

**Linha oca com prova objetiva:**

- DoD row «`TranslationManager.cs`: `TranslateChaptersWithCacheAsync`/`TranslateSingleChapterAsync`
  (S107, 8 params > 7) reduzidos a <= 7 params»: **hollow=true, objective=true**.
  O `awk` do `Verify:` nao mede parametros — ele acha a PRIMEIRA linha que contem `<Nome>(` e soma
  virgulas ate a primeira linha terminada em `)`. Para `TranslateChaptersWithCacheAsync` a primeira
  ocorrencia no arquivo e o CALL SITE (`TranslationManager.cs:59`, 5 argumentos), nao a declaracao
  (`:147`). Contra-exemplo EXECUTADO: copia do arquivo com 3 parametros extras inseridos na
  DECLARACAO (total 8 — exatamente a violacao S107 que o item existe para impedir) continua saindo
  **exit 0**, com o awk reportando as mesmas 4 virgulas do call site em ambos os metodos.
  O doer ADMITIU no SUMMARY ter reordenado a declaracao de `TranslateSingleChapterAsync` para antes
  do chamador "para a janela cair sobre a declaracao" — isso confirma que o gate e posicional, nao
  semantico. O refactor em si e honesto (5 parametros reais em cada metodo, medidos na declaracao,
  via `TranslationRun`); o que nao presta e a PROVA. Mesma familia ja catalogada em `.jdi/todos.md`
  (`[PROCESSO/DoD]`) e a causa das 2 reprovacoes da phase `the-method-refactor`: o gate mede um
  proxy textual conveniente em vez da propriedade.

**Linhas confirmadas solidas** (verificacao independente do proprio `Verify:`, greps mais amplos que
o gate):
- `dotnet-install.ps1`: arquivo ausente; `git grep -l` fora de `.jdi` = 0 hits. `D-...-7` e append
  puro e o comando novo corresponde ao que ela autoriza.
- `HtmlUtility` / `InjectTags`: `Regex.(Match|IsMatch|Replace|Matches)` estatico em TODO `src/` = 0
  (o gate so olhava 1 arquivo; a propriedade vale repo-wide). `InjectTags` tem 3 pontos de decisao
  e os 4 helpers extraidos vivem DENTRO da janela medida (`:93-107`) — decomposicao real, nao
  deslocamento de codigo para fora do marcador. Residuo: mover helper para depois de
  `BuildFallbackHtml` escaparia da janela (evasao deliberada, nao slip).
- JS do WebView: `hasAttribute|getAttribute|setAttribute|removeAttribute` nos 3 arquivos = 0 hits;
  `parseInt` sem `Number.` = 0. O gate e mais estreito que a propriedade (so cobre `getAttribute`
  em `scroll.js`), mas o artefato de hoje entrega a propriedade inteira — residuo, nao furo.
- Access/Engine mecanicos: `DateTime.Parse` sem provider em `src/` = 0; `BeginTransaction()`
  sincrono = 0. Os 5 `.Open()` que sobram (`BookTranslationJobAccess:21`, `BooksAccess:21`,
  `ReadingStateAccess:23`, `SettingsAccess:20`, `TranslationCacheAccess:19`) estao em CONSTRUTOR
  sincrono — S6966 nao dispara sem contexto async, e por isso o Sonar nao os flagou. Nao e gap.
- CA1816: as 7 classes de teste nao-seladas ganharam `GC.SuppressFinalize`; `InMemoryDatabase.cs`
  ficou de fora CORRETAMENTE (e `sealed`, CA1816 nao se aplica).
- xUnit1004: o par `#pragma disable/restore` (`TranslationEngineTests.cs:59-86`) envolve exatamente
  os 2 `[Fact(Skip=...)]` do arquivo — escopo estreito, sem vassoura.
- `sonar.qualitygate.wait=true` esta no comando executado de verdade
  (`.github/workflows/sonarqube.yml:104`, no `run:` do `dotnet-sonarscanner end`), nao em comentario.

**Verdict:** BLOCKED
