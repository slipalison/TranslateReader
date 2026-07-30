# Phase 13: Review  (slug: the-method-refactor)

**Verdict:** APPROVED_WITH_WARNINGS

Review iter 2 (regenerado do zero, sem herdar texto da iter 1). Escopo do diff: `a390eb9` (main)
ate `efc3e8f` na branch `jdi/the-method-refactor`. O range contem 10 commits: os 7 nomeados no
dispatch (508933b, a5f7b44, 3c5a8cf, b9ec38a, 3f68deb, 3010268, efc3e8f) + 3 artefatos do
orquestrador (25874fa context, b98a9be plan, 435e184 review iter 1) — consistente, nada fora do
esperado. A iter 1 foi BLOCKED pelo DoD critic (Verify do DoD 4 oco); esta review verificou o fix
com ataque proprio por mutacao, nao herdou aprovacao.

## Gates

| Gate | Comando | Resultado real | Status |
|---|---|---|---|
| 1 Build | `dotnet restore && dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` | **0 Erro(s)**, 40 avisos (todos `MVVMTK0045` em `ReaderPageModel.cs`, app MAUI legado com zero diff na fase), 8,2s | PASS |
| 2 Tests | `dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj` | **227 aprovados / 2 ignorados / 229 total, 0 falhas** (4s). Baselines superados: 167 (D-2), 196p/2s (inicio da fase), 201p (iter 1). Os 2 skips sao os `TranslationEngineTests` de integracao GGUF pre-existentes, inalterados | PASS |
| 3 Coverage | `dotnet test --collect:"XPlat Code Coverage"` + parse do Cobertura (`TestResults/f434d1e1-*/coverage.cobertura.xml`) | Agregado **82,15%** (contexto apenas — dominado por legado isento D-2). Arquivos NOVOS pos-`4285f25`: 2, ambos de TESTE (`BookTranslationJobAccessTests.cs`, `ParsingEngineRegexTests.cs`) — nao aparecem no report por definicao; zero arquivo novo de producao. Escopo new/changed (D-6, 90%): media dos 5 arquivos de producao alterados = **93,9%** (ReadingManager 100 / FileUtility 100 / TranslationManager 100 / HtmlUtility 89,42 / ParsingEngine 80,19 — os dois ultimos puxados por metodos LEGADOS sem hit). Por LINHA alterada pela fase: descobertas apenas `ParsingEngine.cs:126` (W-1) e `HtmlUtility.cs:44,46` (W-5); as 7 declaracoes `[GeneratedRegex]` (L322-341) e os call sites de `InlineCssLinks`/`RewriteImagePaths` tem hits > 0 | PASS |
| 4 Lint | `dotnet format --verify-no-changes` (solucao) | exit 2, **11 `error WHITESPACE`** — ThemeEngine.cs(12,14), ReaderPage.xaml.cs(122,124), HtmlInjectionTests.cs(25,42), ThemeEngineTests.cs(12), TranslationManagerTests.cs(528x3,529). **Mesmas 11 legadas da iter 1; zero em `ParsingEngineRegexTests.cs`** | WARN (legado, D-2) |
| 5 Security/Layer | 17 greps 5.1-5.17 (saida integral abaixo em Warnings/notas) | 5.1 Client->Access/Engine: **0 hits**. 5.2 storage em contrato: **0**. 5.3 Manager->Manager: so auto-interface (`: IXManager`), **0 cruzado**. 5.6 zip: baseline `ParsingEngine.cs:93,115` (escrita em entry de archive proprio, sem extract-to-path; vetor real e de `epub-zip-slip`, intocado). 5.7 XXE: **0**. 5.8 WebView: 10 hits `ReaderPage.xaml.cs` legado, zero diff na fase. 5.9 secrets/PII em log: **0**. 5.10 sync-over-async: **0**; `catch (OperationCanceledException)` em `TranslationManager.cs:61` **faz `throw;`** (L64, persiste "Paused" antes — conforme §1); os 3 swallows de OCE restantes sao app MAUI legado. 5.11 eventos: subscribe=5 unsubscribe=4 — **igual ao baseline do bootstrap**, nenhum `+=` novo. 5.12 static mutavel: so o baseline `TranslationEngine.cs:16`. 5.15 catch vazio: 5 hits, todos app legado. 5.15b Result pattern: **0**. 5.16 TODO: **0**. 5.17 mock de concreto: **0**; I/O real so nos 4 arquivos de teste pre-existentes (padrao estabelecido) — `ParsingEngineRegexTests.cs` tem **zero I/O e zero mock** | PASS |
| 6 Consistency | `git show --name-only` por commit + `git log --pretty=%s` | Arquivos por commit batem 1:1 com `files_modified` do PLAN (T-1: 5 arquivos, T-2: 1, T-3: 2, T-4: docs; iter 2: teste novo + `.jdi/`). Conventional Commits com scope `the-method-refactor` nos 10; tipos adequados (`refactor`/`test`/`docs`, nao tudo `feat`) — D-4. 1 achado = 1 commit atomico, producao+teste juntos em T-1 | PASS |
| 7 UI Validation | — | SKIPPED (has_frontend=false, cliente MAUI nativo) | SKIPPED |
| 8 DoD | 5 `Verify:` extraidos por sed do CONTEXT.md e executados **literalmente** via eval | **5/5 exit 0**. PROJECT.md nao tem secao `## Definition of Done` (brownfield) — itens vem so do CONTEXT.md. 0 itens manuais (dod=auto_only) | PASS |

## Blockers

Nenhum.

## Warnings

- **W-1 (herdada da iter 1, parcial — segue aberta):** o wiring end-to-end de
  `InlineCssLinks`/`UpdateOpfTitleAsync` sobre EPUB real continua sem assercao —
  `src/TranslateReader.Core/Business/Engines/ParsingEngine.cs:126` com **hits=0 medidos** no
  Cobertura (metodo `UpdateOpfTitleAsync` inteiro 0%, `CreateTranslatedEpubAsync` 0% — ambos
  legados pre-boundary, ja sem cobertura antes da fase). Fechar exigiria fixture EPUB com I/O de
  disco, proibido em teste novo por `.claude/rules/csharp.md` §6 e recusado por precedente
  (`regression-suite` SUMMARY > Lacuna 4). Registrado em `.jdi/todos.md`. Ver ponto (g).
- **W-2 (nova, achado deste review):** o `Verify:` endurecido do DoD 4 ainda tem 1 furo residual,
  provado por mutacao propria (M5): trocar o call site `StylesheetRelRegex().IsMatch` por
  `StylesheetHrefRegex().IsMatch` orfana um regex COMPENSANDO a contagem (`-eq 14` se mantem) e o
  comando sai `exit 0`; a rede de testes tambem nao acusa (os 26 casos testam os regexes isolados,
  e a medicao da iter 1 mostrou `StylesheetRelRegex` sozinho -> 0 falhas). A clausula de D-7
  "fechando o caso regex declarado e nunca chamado" vale so para orfanamento SIMPLES (contagem cai
  -> pega, provado em M4); orfanamento compensado escapa. NAO e hollow-pass do criterio — o
  criterio do DoD 4 fala de identidade de pattern/options e ligacao atributo<->nome, e isso o
  comando prova integralmente; o que escapa e regressao de COMPORTAMENTO de call site, que e
  exatamente o residuo W-1 visto por outro angulo. Fechar W-1 (fixture end-to-end em fase futura)
  fecha isso junto. Cita: `.jdi/DECISIONS.md` D-2026-07-30-the-method-refactor-7;
  `ParsingEngine.cs:196`.
- **W-3:** `dotnet format` — 11 violacoes WHITESPACE legadas (mesmo conjunto da iter 1, lista no
  Gate 4). D-2 isenta; vira BLOCK-on-new apos a fase `baseline-de-estilo`. Zero violacao nova.
- **W-4 (workspace, ponto h):** `TestResults/` esta untracked e **nao esta no `.gitignore`**
  (grep por `test|trx|coverage` no `.gitignore` = 0 hits). Nenhum arquivo dela vazou para commit
  algum (`git log --all --name-only -- 'TestResults/*'` = vazio), mas um `git add .` futuro
  arrastaria XMLs de cobertura para o repo. Recomendacao: adicionar `TestResults/` ao
  `.gitignore` num commit de hygiene futuro (fora do escopo desta review, que e read-only).
- **W-5 (informativa):** `src/TranslateReader.Core/Utilities/HtmlUtility.cs:44,46` — os 2 guard
  branches de `ReplaceTextBlocksInHtml` (bloco whitespace-only; `translations` esgotadas) foram
  movidos sem cobertura, igual ao estado na origem (`TranslationManager`) — limite honesto ja
  declarado na SUMMARY (mutacao B de T-3). Bloco movido: 37/39 linhas cobertas (94,9%).

Notas legadas (baseline D-2, sem mudanca na fase, nao contam como warning novo): swallows de OCE e
`catch { }` no app MAUI (`LibraryPageModel.cs:183`, `ReaderPageModel.cs:222`,
`ReaderPage.xaml.cs:308,326,434`), desbalanco de eventos 5/4, static mutavel
`TranslationEngine.cs:16`.

## Veredito dos pontos (a)-(h) do dispatch

**(a) `ParsingEngineRegexTests.cs` por reflection — design de teste: ACEITO.**
(i) *Rename silencioso:* NAO passa vazio. `Pattern(name)` tem null-guard explicito
(`ParsingEngineRegexTests.cs:15-16`) — `GetMethod` nulo lanca `InvalidOperationException` e as 26
execucoes falham com mensagem nomeada. Rede dupla: a mutacao M4 (rename `SvgImageHrefRegex` ->
`SvgImageHrefRx`) tambem derruba o Verify do DoD 4 (`exit 1`, contagem 14 cai e par
atributo<->assinatura some). Rename e RUIDOSO nos dois gates.
(ii) *Acoplamento a membro privado:* pesado contra as alternativas, e o menor mal — expor as
factories na API do Engine vazaria detalhe de implementacao no contrato (The Method) e seria
superficie especulativa (YAGNI, sem 2o consumidor); teste via API publica exige `filePath` real =
I/O de disco, vedado a teste novo por §6 e por precedente locked. Reflection manteve **zero diff
em producao** (provado no ponto e). §6 nao e violado: sem rede, sem disco, sem SQLite, sem mock de
concreto (nao ha mock algum). Registro: se algum dia edicao de producao entrar em escopo,
`InternalsVisibleTo` + factories `internal` seria a rota mais limpa que reflection — fica como
opcao, nao como pendencia.
(iii) *Comportamento vs existencia:* os 26 casos assertam COMPORTAMENTO — match E no-match por
regex, valor de grupo capturado (`Groups[1]`/`Groups[2]`), templates de replace (`$1New$3`,
inclusive o template exato usado em `ParsingEngine.cs:126`), case folding com InlineData uppercase
para **cada um dos 7** (derrubar `IgnoreCase` de qualquer um falha >= 1 caso), `Singleline` (titulo
com `\n`), lazy quantifier (2 `<dc:title>` substituidos separadamente) e delimitacao (`<linkage`,
`<image src`, `<img href`, href vazio, `rel="stylesheets"`). Nenhum caso e assercao de mera
existencia. Executados de verdade: 227p inclui os 26.

**(b) `Verify:` novo do DoD 4 — CONFIRMADO com 1 residuo (W-2).** Rodado literalmente (extraido do
CONTEXT.md por sed, eval): `exit 0` com contagens reais 0 regex estaticos / 7 attrs exatos / 14
linhas de nome / 7 pares atributo<->assinatura. Atacado com 5 mutacoes proprias em copia no
scratchpad (`attack_dod4.sh`, repo intocado), comando velho (extraido de `b9ec38a`) vs novo:

| Mutacao | OLD | NEW |
|---|---|---|
| P0 pristino (sanidade) | exit 0 | exit 0 |
| M1 contra-exemplo do critico (`stylsheet` + sem `IgnoreCase`) | **exit 0** | **exit 1** |
| M2 patterns trocados entre `StylesheetRel` e `StylesheetHref` | exit 0 | exit 1 |
| M3 `Singleline` removido de `OpfTitleRegex` | exit 0 | exit 1 |
| M4 factory renomeada (`SvgImageHrefRegex` -> `SvgImageHrefRx`) | exit 0 | exit 1 |
| M5 orfao compensado (call site Rel->Href, contagem mantida) | exit 0 | **exit 0** |

O blocker da iter 1 esta objetivamente fechado (M1: OLD passava, NEW derruba). M5 e o furo
residual — classificado WARN e nao BLOCKER porque nao e hollow-pass do criterio locked (patterns e
ligacao seguem provados; o que escapa e wiring de call site, residuo W-1 ja declarado e roteado).

**(c) Caminho JDI-legal — CONFIRMADO.** `git diff b9ec38a HEAD -- .jdi/DECISIONS.md`: **1 hunk,
36 linhas `+`, 0 linhas `-`**, posicao `@@ -539,3 +539,39 @@` = append puro no fim do arquivo
(577 linhas). D-...-5 intocada (zero delecao no arquivo inteiro). A linha nova do
`## Definition of Done` implementa exatamente as 4 propriedades que D-...-7 autoriza — (1) zero
`Regex.(Replace|Match|IsMatch)(`; (2) `public partial class`; (3) `-eq 7` EXATO (endurecido vs
`-ge 7`); (4) `-eq 14` + 7 pares `grep -A1 -F` literais — e o comando antigo esta contido no novo
(nenhuma clausula afrouxada).

**(d) Outros 4 itens do DoD intactos — CONFIRMADO.** `git diff b9ec38a HEAD -- CONTEXT.md`:
**1 hunk unico** (7+/3-), restrito ao bloco do item 4 (achado #3). Itens 1, 2, 3 e 5 byte-identicos.

**(e) Zero producao na iter 2 — CONFIRMADO.** `git diff 3f68deb^ HEAD --stat -- src/` = saida
vazia. Iter 2 = `ParsingEngineRegexTests.cs` (novo, +182) + `.jdi/*`.

**(f) Zero regressao de teste — CONFIRMADO.** `git diff b9ec38a HEAD -- test/`: **0 linhas
removidas** (unico change = arquivo novo, 182 insercoes). Nenhum `[Fact(Skip=...)]` novo (os 2
skips do repo sao os GGUF pre-existentes em `TranslationEngineTests.cs:56,69`). Nenhuma assercao
alterada. Attrs 197 -> 214 (+17 do arquivo novo: 9 `[Fact]` + 8 `[Theory]` = 26 casos).

**(g) Residuo W-1 parcial — ACEITAVEL COMO WARNING.** `ParsingEngine.cs:126` hits=0 confirmado no
Cobertura desta review. Tres razoes para nao ser blocker: (1) o metodo era legado JA sem cobertura
antes da fase — a fase nao piorou nada, apenas trocou a forma da chamada, e a SEMANTICA do regex
daquela linha agora tem 5 assercoes dedicadas (inclusive o template `$1{titulo}$3`); (2) fechar
exige I/O vedado por regra locked (§6) com precedente de recusa registrado — exigir aqui
contradiria decisao do projeto; (3) o risco residual esta nomeado, registrado em `.jdi/todos.md` e
delimitado (W-2 mostra o vetor exato). Continua warning ate uma fase decidir criar fixture
end-to-end deliberadamente.

**(h) Workspace — WARN (W-4).** `TestResults/` untracked, **fora do `.gitignore`**, e **nenhum**
arquivo dela em commit algum do historico (`git log --all` vazio). Sem vazamento hoje; risco de
`git add .` futuro. Alem dela, o unico item de workspace e a delecao proposital do REVIEW.md da
iter 1 (regenerado por este arquivo).

## DoD Checklist (gate 8)

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | Achado #1: `IFileUtility.DirectoryHasContent` + `ReadingManager` roteia por ele | CONTEXT | Auto | PASS | exit 0 — metodo no contrato e na impl; 0 `Directory.(Exists\|GetFileSystemEntries)` no Manager; chamada `fileUtility.DirectoryHasContent` presente |
| 2 | Gap regression-suite-5(1): teste mockado do branch "ja extraido" + caso real em FileUtilityTests | CONTEXT | Auto | PASS | exit 0 — `DirectoryHasContent` citado nos 2 arquivos; `ReadingManagerTests` com 8 attrs (>= 8) |
| 3 | Achado #2: 4 metodos HTML saem do Manager, entram `public static` em `HtmlUtility` | CONTEXT | Auto | PASS | exit 0 — 0 definicoes privadas restantes; exatamente 4 `public static` |
| 4 | Achado #3: 7 `[GeneratedRegex]` com pattern/options byte-identicos ligados ao nome, zero regex estatico (Verify endurecido por D-...-7) | CONTEXT | Auto | PASS | exit 0 — 0 / partial presente / 7 exatos / 14 nomes / 7 pares literais; gate validado por mutacao propria (tabela no ponto b): pega M1-M4, residuo M5 = W-2 |
| 5 | Guardrail: zero diff app MAUI, zero BenchmarkDotNet, attrs >= 193 | CONTEXT | Auto | PASS | exit 0 — 0 arquivos de diff em `src/TranslateReader/`; 0 csproj com BenchmarkDotNet; 214 attrs |

**Totals:** 5 items | Auto: 5 (5 PASS, 0 FAIL) | Manual: 0 pending

(PROJECT.md nao declara secao `## Definition of Done` — brownfield; itens exclusivamente do
CONTEXT.md, `dod=auto_only`.)

## Recommendation

Blocker da iter 1 fechado com prova objetiva e verificado adversarialmente por este review — o fix
e real, nao cosmetico: o contra-exemplo exato do critico agora derruba o gate, e o par
teste-semantico + gate-textual cobre pattern, options, ligacao nome<->atributo e rename. Nenhum
gate 1-8 bloqueia; 0 itens manuais. Recomendo **ship**. Para fases futuras, tres encaminhamentos
ja roteados: (1) fixture end-to-end de `CreateTranslatedEpubAsync`/`InlineCssLinks` fecharia W-1 e
W-2 de uma vez (decisao deliberada de I/O em fase propria); (2) `TestResults/` no `.gitignore`
(W-4, 1 linha); (3) as 11 WHITESPACE morrem na fase `baseline-de-estilo` (W-3).

## DoD Critic (enhanced — forcado por /jdi-issue)

Re-ataque das 5 linhas `Type=Auto`/`PASS` (o critico so pode endurecer o veredito). Linhas 1, 2 e 3
confirmadas SOLIDAS por inspecao independente + contra-exemplos executados: o `-ge 8` do item 2 e
escopado a `ReadingManagerTests.cs` (7 attrs em `a390eb9`, 8 hoje — deletar o teste do branch derruba
o gate; os +17 attrs novos vivem em outro arquivo e nao inflam a medida), e o item 3 pega o leftover
realista (`private static ...` residual -> exit 1). Duas linhas caem:

- DoD row «Achado #3: `ParsingEngine` ... 7 `[GeneratedRegex]` com pattern/options byte-identicos
  ligados ao nome (Verify endurecido por D-...-7)»: **hollow=true, objective=true**. Contra-exemplo
  M5 executado em copia: trocar UM token no call site `ParsingEngine.cs:196`
  (`StylesheetRelRegex` -> `StylesheetHrefRegex`, nomes lookalike adjacentes — slip plausivel num
  refactor, nao evasao deliberada) sai **exit 0** com `StylesheetRelRegex` declarado e nunca chamado;
  o orfao SIMPLES (M6) sai exit 1. Enquadramento: e furo DO CRITERIO, nao wiring fora dele —
  `D-2026-07-30-the-method-refactor-7` (`DECISIONS.md:563-568`) locka textualmente que a clausula
  `-eq 14` fecha "regex declarado e nunca chamado" e que "orfanar um regex derruba o gate", sem
  qualificador; o comando entrega essa promessa so para o orfao simples. A causa e a mesma classe do
  blocker da iter 1: `-eq 14` mede contagem agregada conveniente em vez da propriedade por-nome
  ("cada uma das 7 factories tem >= 1 call site" = 7 checks). A rede pareada nao compensa POR
  CONSTRUCAO: os 26 casos de `ParsingEngineRegexTests.cs:13-18` invocam as factories direto por
  reflection (nunca passam pelo wiring de producao) e `ParsingEngineTests` tem ZERO referencia a
  `stylesheet`/`css`/`<link` (grep = 0 hits) — M5 passa o `Verify:` E a suite inteira.
  O resto do endurecimento de D-7 e solido e foi confirmado: os pares `-A1 -F` byte-exatos amarram
  pattern+options ao nome certo, pattern trocado entre factories quebra o par, rename quebra
  `-eq 14` + par + o null-guard de `ParsingEngineRegexTests.cs:15-16` (o teste por reflection lanca,
  nao passa vazio).

- DoD row «Guardrail agregado: zero diff em `src/TranslateReader/`, nenhum pacote BenchmarkDotNet,
  contagem `[Fact]`/`[Theory]` nao regride do baseline 192»: **hollow=true, objective=true**. Duas
  clausulas nao provam o que o criterio afirma, ambas executadas em copia:
  (1) o criterio diz "nenhum pacote BenchmarkDotNet" sem escopo, mas o comando roda `find src` — com
  `<PackageReference Include="BenchmarkDotNet"/>` em `test/TranslateReader.Tests/
  TranslateReader.Tests.csproj` (o lugar NATURAL de infra de benchmark, exatamente o que
  `D-...-2(B)` quer barrar) a clausula sai **exit 0**, e nenhum outro gate greppa BenchmarkDotNet.
  (2) o criterio diz "a contagem nao regride", mas `grep -rhoE` conta TEXTO, incluindo comentario:
  comentando 25 atributos reais (`// [Fact]`) a contagem medida permanece 214 e a clausula sai
  **exit 0** com atributos ativos = 189 < 192 — regressao real passa o guardrail (e passa o gate 2
  tambem, cujo piso locked e 167). Sustentam-se: `git merge-base main HEAD` resolve (a390eb9) e a
  clausula de diff do app MAUI compara base contra working tree (pega commitado e nao-commitado).

Ambos os furos sao a mesma familia ja catalogada em `.jdi/todos.md` (`[PROCESSO/DoD]`): o gate mede
um proxy agregado conveniente em vez da propriedade por-item.

**Verdict:** BLOCKED
