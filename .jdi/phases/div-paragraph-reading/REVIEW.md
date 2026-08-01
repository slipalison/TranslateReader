# Phase 18: Review (FINAL — iter 2)  (slug: div-paragraph-reading)

**Verdict:** APPROVED_WITH_WARNINGS

Review final auto-suficiente (o REVIEW.md da iter 1 foi deletado; nada aqui depende dele).
Diff revisado: `main` (`9e07c83`) → HEAD (`39b5c2d`), 12 commits de phase + 2 de bookkeeping.
Iter 2 = SO endurecimento de gate/doc (`D-2026-08-01-div-paragraph-reading-6`); toda evidencia
abaixo foi produzida NESTA sessao, por execucao propria — nada herdado do SUMMARY do doer.

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `dotnet restore && dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` — exit 0, `0 Erro(s)` |
| Tests | PASS | C# `Failed: 0, Passed: 336, Skipped: 2, Total: 338` (baseline 167 e baseline da phase 336/2/338 — sem regressao) · JS `# tests 73 / # pass 73 / # fail 0 / # skipped 0` |
| Coverage | PASS | Cobertura real (Cobertura XML): agregado 88,92% (so contexto, D-2); arquivos NOVOS pos-boundary `4285f25`: `Models/BookTranslationResult.cs` 100%, `Models/ExtractedImage.cs` 100% (>= 90%, D-6). Esta phase criou 0 arquivo `.cs` novo |
| Lint | WARN | `dotnet format --verify-no-changes` exit 2 com 9 findings — todos em arquivos legados byte-identicos a `main` (D-2 exime; WARN por regra do gate 4). Arquivos tocados pela phase: limpos |
| Security/Layer | PASS | 5.1/5.2/5.3/5.10/5.15 zero hits; 5.8 todas as interpolacoes de `EvaluateJavaScriptAsync` via `JsStr(...)`/`itemsJson` (legado identico a `main`); 5.9 limpo; 5.11 `+=`5/`-=`4 e 5.12 `_nativeLibraryConfigured` = exatamente o baseline legado do bootstrap, nada novo; OCE em `TranslationManager.cs:61` faz `throw;` |
| Consistency | PASS | 8 arquivos commitados em `src/`+`test/` batem 1:1 com o `files_modified` do PLAN; Conventional Commits com scope `div-paragraph-reading` (+1 `chore(jdi)` padrao), tipos adequados (test/fix/docs/chore) |
| UI Validation | SKIPPED | has_frontend=false (cliente MAUI nativo) — por desenho, nunca bloqueia |
| DoD | PASS | 7/7 Auto PASS (comandos extraidos por `sed` do CONTEXT.md COMMITADO em HEAD, rodados nesta sessao); 0 itens Manual (dod=auto_only; PROJECT.md nao declara secao DoD — a da phase governa) |

## Blockers

- _(nenhum)_

## Warnings

1. **`D-...-6` promete "os CRITERIOS ficam INTACTOS", mas o texto do criterio do item 6 tambem
   mudou** (de "piso acima do baseline conhecido ... 320/322" para "piso DERIVADO de `main` ... e
   nenhum metodo de teste de `main` some"). A mudanca e na direcao ESTRITA (o texto antigo embutia
   um piso factualmente errado, 15 testes abaixo do real) e o SUMMARY a declara honestamente —
   mas a frase de abertura da decisao e imprecisa. Sem efeito pratico; registrar como precedente
   de redacao: se o criterio muda, a decisao deve dizer que muda.
2. **Item 1 — bypass residual por comentario de BLOCO multi-linha.** O `sed` remove `//` e `/* */`
   de UMA linha; um `/* ... _translatableCandidates( ... */` atravessando linhas dentro do corpo
   de uma funcao ainda enganaria o grep estrutural. Mitigado por desenho: o proprio `Source:` do
   item 1 delega a prova de COMPORTAMENTO aos itens 2/3, e a matriz abaixo (CE-1, R-A) prova que
   eles pegam o desvio real. Nota menor: `s://.*::` truncaria linha de codigo contendo `://` em
   string (hoje inexistente em `translation.js`); o efeito seria gate MAIS estrito, nunca mais
   frouxo.
3. **Item 6 — limites conhecidos da derivacao do piso** (analise do item e do dispatch):
   (a) ancora no `main` LOCAL — hoje `main == origin/main == 9e07c83` (verificado); se o ref local
   estalar atras do remoto, o piso deriva de um baseline defasado. Rodar `git fetch` antes do gate;
   (b) a contagem estatica `[Fact]`+`[InlineData]` nao enxerga `MemberData`/`ClassData`/`TheoryData`
   — hoje ZERO ocorrencias em `main` e HEAD (verificado); se entrarem no futuro, `B` subconta e o
   comando precisa ser re-derivado; (c) o `comm` e por nome de METODO — deletar uma LINHA de
   `[InlineData]` e simultaneamente somar 2 testes novos ainda fecharia a conta aritmetica. Piso
   aritmetico + nomes de metodo e o teto do que um gate estatico entrega; regressao por caso de
   Theory fica para a suite em si. Aceitavel nesta phase (nenhum `[Theory]` foi tocado);
   `[Fact(Skip=...)]` novo e pego (`Skipped <= 2`).
   Confirmacao pedida no dispatch: `[Fact]`=288 + `[InlineData]`=49 = **337** em `main`, batendo
   1:1 com o `Total: 337` real de `main`; `Skip=`=2 = os 2 Skipped reais.
4. **Legado pre-existente, byte-identico a `main`, fora do alcance desta phase** (D-2 exime; nada
   disso e novo): `catch (OperationCanceledException) { }` em `LibraryPageModel.cs:183`,
   `ReaderPageModel.cs:222`, `ReaderPage.xaml.cs:308` + `catch { }` em `ReaderPage.xaml.cs:326,434`;
   desbalanceamento de eventos 5/4; 9 findings de `dotnet format`; I/O real em testes legados
   (`FileUtilityTests`, `ModelAccessTests`, `SettingsAccessTests`, `InMemoryDatabase`,
   `HybridWebViewContractTests`).
5. **Debitos da iter 1 que seguem abertos em `.jdi/todos/`** (nao regridem): harness com falha
   ABERTA para aspas dentro de valor de atributo (`harness.js`, pre-existente de `main`); branch
   `chapter?.Title` (`TranslationManager.cs:265`) em 83,3% — divida pre-existente, linha nao tocada.

## DoD Checklist (gate 8)

Comandos extraidos por `sed -nE 's/^ *\*\*Verify:\*\* \x60(.*)\x60$/\1/p'` de
`git show HEAD:.jdi/phases/div-paragraph-reading/CONTEXT.md` — nao digitados de memoria.

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | `translation.js` com fonte unica `_translatableCandidates`, seletores antigos ausentes, helper no corpo das 3 funcoes, guardas na polaridade certa | CONTEXT (D-...-6) | Auto | PASS | exit 0 (repo real) |
| 2 | Round-trip calibre get/apply/clear com >= 4 testes `calibre` e os 3 nomes exatos verdes em TAP | CONTEXT (D-...-6) | Auto | PASS | exit 0; `# fail 0`, 3 nomes `ok N - <nome>` presentes |
| 3 | Suite `translation.js` inteira verde, sem regressao nome-a-nome vs `main`, piso B+4 | CONTEXT (D-...-6) | Auto | PASS | exit 0; `comm -23` vazio; 20 >= 13+4 |
| 4 | `ExtractParagraphs`/`ParagraphRegex` ausentes do repo tracked; corpo de `TranslateChapterAsync` com exatamente 1 atribuicao `= HtmlUtility.ExtractTextBlocks(bodyContent)` | CONTEXT (D-...-6) | Auto | PASS | exit 0; linhas 124/195 (outros metodos) fora do range `awk` — conferido |
| 5 | Teste calibre novo em `TranslateChapterAsync_*`, filtro passa com piso B+1 | CONTEXT (iter 1, intacto) | Auto | PASS | exit 0; byte-identico ao da iter 1 (diff vazio) |
| 6 | Suite C# inteira, piso derivado de `main` (B=337), Skipped<=2, coerencia do sumario, `comm` nome-a-nome | CONTEXT (D-...-6) | Auto | PASS | exit 0; base 308 nomes / head 309, `comm -23` vazio; `Failed 0, Passed 336, Skipped 2, Total 338` |
| 7 | `src/TranslateReader/` intocado fora de `Resources/Raw/wwwroot/js/` | CONTEXT (iter 1, intacto) | Auto | PASS | exit 0; byte-identico ao da iter 1 (diff vazio) |

**Totals:** 7 items | Auto: 7 (7 PASS, 0 FAIL) | Manual: 0 pending

## Matriz de mutacao (execucao PROPRIA — lab `git clone --local` em scratchpad, repo real nunca mutado)

Lab: clone descartavel com `main` local = `origin/main` (`9e07c83`), HEAD = `39b5c2d`. `OLD-TAP` =
comando antigo + `--test-reporter=tap` (a re-autoria da iter 1 — o antigo LITERAL dos itens 2/3 e um
reprovador constante no Node 24, reproduzido: exit 1 no lab 100% verde, enquanto v2/v3 novos saem 0).
Higiene: apos detectar que `git checkout main -- <arquivo>` sujava o INDICE do lab, R-B e R-D foram
re-rodados isolados com `reset --hard` — os numeros abaixo sao das rodadas limpas.

### (a) Contra-exemplos do DoD critic — todos reproduzidos, todos caem no comando NOVO

| Caso | Mutacao (so no lab) | Item | ANTIGO | NOVO |
|---|---|---|---|---|
| CE-1 | filtro de letra invertido em `translation.js:15` (`if (!_LETTER_RE.test(` → sem `!`) | 1 | 0 | **1** (e item 2 tambem: 1) |
| CE-2 | `applyTranslations` desviado p/ `querySelectorAll('[data-original], p, div')` + comentario `// _translatableCandidates(pg)` | 1 | 0 | **1** |
| CE-3 | `TranslationManager.cs:244` → `HtmlUtility.LegacyParagraphExtract(bodyContent)` (rename da API defeituosa) | 4 | 0 | **1** |
| CE-4 | 3 testes da era de `main` deletados de `translation.test.js` (fica 17) | 3 | 0 (OLD-TAP) | **1** |
| CE-5 | 6 calibre reais renomeados p/ fora do filtro + 4 stubs `test('calibre stub N', () => {})` | 2 | 0 (OLD-TAP) | **1** |
| CE-6 | log sintetico `Passed: 321, Total: 323` (substituicao SO da invocacao dotnet por `cp`) | 6 | 0 | **1** |
| CE-6b | 3 metodos `[Fact]` REALMENTE deletados de `TranslationManagerTests.cs` (7→4 `TranslateChapterAsync_*`) | 6 | 0 (c/ log honesto 333/335) | **1** (via `comm`, antes do dotnet) |

### (b) Regressao de gate — NENHUM caso OLD=1/NEW=0 (criterio de blocker do dispatch: zero ocorrencias)

| Caso | Mutacao | Item | ANTIGO | NOVO |
|---|---|---|---|---|
| R-A | fix AUSENTE (`translation.js` de `main`) | 1, 2, 3 | 1 | **1** em todos (suite JS vermelha inclusa) |
| R-B | so 3 testes `calibre` (abaixo do piso N>=4) — isolado | 2 | 1 (OLD-TAP) | **1** |
| R-C | `ExtractParagraphs` de volta (`HtmlUtility.cs` de `main`) | 4 | 1 | **1** |
| R-D | assert falho injetado em teste da era de `main` — isolado, `# pass 19 / # fail 1` | 3 | 1 (OLD-TAP) | **1** |
| R-E | logs: vermelho `Failed: 1`; `300/302`; incoerente `339+2+0 != 338` | 6 | 1 / 1 / n.a. | **1 / 1 / 1** |
| R-F | suite C# REALMENTE vermelha no lab (assert flipado, `dotnet test` de verdade: `Failed! - 1/335/2/338`) | 6 | n.a. | **1** |

### (c) Falso positivo — os 7 comandos NOVOS no repo REAL sem mutacao

| Item | 1 | 2 | 3 | 4 | 5 | 6 | 7 |
|---|---|---|---|---|---|---|---|
| exit | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

## Checagens do dispatch (a–i)

- **(a) Zero diff de codigo na iter 2:** `git diff --stat 03b54af..HEAD -- src/ test/` = **vazio**
  (exit 0, 0 linhas). O diff completo da iter 2 toca so `.jdi/decisions/D-...-6.md` (novo),
  `CONTEXT.md` e `SUMMARY.md`. Confirmado.
- **(b)** Matriz secao (b) acima — zero caso OLD=1/NEW=0. Confirmado.
- **(c)** Matriz secao (a) — os 6 contra-exemplos do critico + CE-6b saem exit 1 no comando novo.
  Confirmado.
- **(d)** Secao (c) da matriz — 7/7 exit 0 no repo real, comandos extraidos do CONTEXT.md commitado.
  Confirmado.
- **(e)** Warning 3 acima. 288+49=337 confirma a alegacao do doer; `main==origin/main` hoje;
  limites MemberData/ref-local/linha-de-Theory documentados e aceitaveis nesta phase.
- **(f) Append-only:** `D-...-6` e arquivo NOVO (`git diff --name-status 03b54af..HEAD --
  .jdi/decisions/` = so `A D-...-6.md`); D-1..5 nasceram nesta phase e nao mudam desde a iter 1;
  `git diff main HEAD -- .jdi/decisions/` = 6 linhas `A`, zero `D`/`M`. No CONTEXT.md mudaram as
  linhas `Verify:`/`Source:` dos itens 1,2,3,4,6 e o texto do criterio do item 6 (Warning 1);
  itens 5 e 7 byte-identicos entre iters (diff dos comandos extraidos = vazio). Confirmado com a
  ressalva do Warning 1.
- **(g)** A promessa OPERACIONAL da D-...-6 (6 regras de autoria) corresponde aos comandos
  entregues — conferida regra a regra (TAP pinado; nome exato; `comm` nome-a-nome; comentario
  removido + range de corpo; `git grep` de ausencia; piso derivado). Duas ressalvas textuais:
  Warning 1 (criterio do item 6) e Warning 2 (comentario multi-linha fica fora do sed — a decisao
  promete "comentario removido" e entrega so o de uma linha, com mitigacao comportamental provada).
- **(h) O fix segue provado:** JS 73/73/0 skipped 0; C# 336/2/338; os 6 testes `calibre` verdes com
  os 3 round-trips nomeados; codigo-fonte re-lido nesta sessao (`_translatableCandidates` unico,
  guardas de folha/letra corretas, `clearTranslations` via `dataset.original`, `console.warn` so em
  `getVisibleParagraphs`; C# linha 244 → `ExtractTextBlocks`, API morta removida,
  `HtmlInjectionTests` 8→7). O endurecimento de gate nao mascarou nada.
- **(i) Escopo:** diff da phase em `src/TranslateReader/` = SO `Resources/Raw/wwwroot/js/translation.js`
  (resto no Core e em `test/`); `.gitignore` em **zero** commits (`git log main..HEAD -- .gitignore`
  vazio; a alteracao local do usuario — linha `design` — segue nao commitada e nao foi tocada).

## Estado final da phase

**Producao (muda para o usuario):** EPUBs de calibre (paragrafos em `<div class="calibreN">`)
agora funcionam na traducao interativa por paragrafo do ReaderPage — `translation.js` ganhou o
helper unico `_translatableCandidates` (div-folha com letra Unicode entra; wrapper/imagem/bullet
ficam fora; indice pareado por construcao entre get/apply/clear) e `console.warn` quando o capitulo
tem texto mas zero candidato. No Core, `TranslateChapterAsync` trocou o extrator so-`<p>` morto
(`ExtractParagraphs`/`ParagraphRegex`, removidos) por `ExtractTextBlocks`, ja corrigido na phase
irma. **So em gate/teste/doc:** harness com selector group por virgula + 6 testes; 7 testes JS de
calibre + 1 teste C#; iter 2 inteira (D-...-6 + `Verify:` endurecidos) — zero linha de `src/`/`test/`
na iter 2. **Numeros finais:** build 0 erros; JS 73/73; C# 336 passed / 2 skipped / 338 total
(+1 sobre `main` = o teste novo, nenhum removido — nome a nome); coverage de arquivos novos
pos-boundary 100%.

## Para o revisor humano do PR

O que o gate automatizado NAO prova — 1 minuto de atencao:

1. **Rendering real em WebView.** Toda a prova JS roda num DOM falso (harness). Que o paragrafo
   calibre selecionado renderiza/pagina certo em WebView2/WKWebView/Android WebView so um smoke
   manual com um EPUB de calibre mostra. (Deferido pelo CONTEXT.)
2. **UX do caso "zero candidato":** hoje e so `console.warn` — o usuario final nao ve nada quando
   um capitulo nao tem paragrafo traduzivel. Decisao de produto (toast/badge) esta explicitamente
   deferida ao PR; concordar ou abrir issue.
3. **SonarCloud** nos arquivos tocados so existe pos-push/CI (D-2026-07-30-sonar-zero-issues-12).
4. **`ITranslationManager.TranslateChapterAsync` segue no contrato sem chamador de UI** — mantido
   por decisao (D-...-4); se incomodar, e phase futura, nao este PR.
5. Os `Verify:` novos sao fortes mas nao adversarialmente perfeitos (Warnings 2–3: comentario
   multi-linha, MemberData futuro, ref `main` local). Eles provam nao-regressao e comportamento
   hoje; nao substituem revisao de diff.

## Recommendation

Aprovar e seguir para `/jdi-ship div-paragraph-reading`. Nenhum item manual de DoD pendente. As
warnings sao textuais/limites-de-gate ou legado exento por D-2 — nenhuma pede nova iteracao de
codigo. Para phases futuras: adotar as 6 regras de autoria da D-...-6 ja na escrita do primeiro
CONTEXT (a phase gastou uma iteracao inteira consertando prova, nao codigo).

**Verdict:** APPROVED_WITH_WARNINGS

## DoD Critic (enhanced — forcado por /jdi-issue)

Re-ataque dos 7 rows apos o endurecimento da iter 2 (a aprovacao do critico da iter 1 nao cobre
comandos que mudaram). Itens 3, 5 e 7 confirmados SOLIDOS; 4 e 6 caem para warning com backstop
nomeado; **itens 1 e 2 ocos, com um achado que nao e de gate e sim de TESTE FALTANDO**:

- **Mutante M-E (executado, mutacao SO em `src/`)**: `applyTranslations` desviado para
  `pg.querySelectorAll('[data-original], p, div')` com um comentario de BLOCO multi-linha contendo
  `_translatableCandidates(` para repor a contagem. Resultado: **os 7 `Verify:` saem exit 0 e a
  suite JS fica 73/73 verde** — enquanto o harness prova, no proprio fixture calibre desta phase,
  bug REAL de usuario: traduzir o paragrafo de indice 0 escreve no `div` WRAPPER e colapsa o
  capitulo inteiro (`CHAPTER_COLLAPSED: true`). Isso falsifica a mitigacao do W-2 da review
  ("os itens 2/3 pegam o desvio real" — nao pegam) e falsifica a certificacao do criterio central
  da phase (`D-...-3`: "dessincronia de indice deixa de ser possivel por construcao").
  Causa raiz da lacuna: a suite nova tem get-sobre-`CALIBRE_BODY` e clear-sobre-div-unico, mas
  **nao tem apply sobre um corpo capaz de dessincronizar** — o teste de apply
  (`test/js/translation.test.js:237-249`) usa `<p>one</p><div>two</div><p>three</p>`, forma em que
  qualquer seletor ingenuo coincide com o helper.
- Item 1 (`hollow=true, objective=true`): o `sed` de remocao de comentario so cobre `//` de UMA
  linha — comentario de bloco e string literal burlam igual. Sem backstop comportamental (ver M-E).
- Item 2 (`hollow=true, objective=true`): amarra NOME de teste, nao corpo — trocar o corpo do teste
  nomeado por `assert.ok(true)` sai exit 0.
- Item 4 (`hollow=true`, mas MITIGADO de verdade): desvio para `LegacyParagraphExtract` com a linha
  genuina escondida em `/* */` sai exit 0; porem, para compilar, o extrator defeituoso precisa
  existir, e o item 5 (byte-identico, `dotnet test` real, teste calibre provado RED-first na iter 1
  contra exatamente esse extrator) fica VERMELHO. Backstop comportamental real.
- Item 6 (`hollow=true`, assimetria documentada): o `comm` do lado HEAD vem de grep ESTATICO nos
  arquivos, nao dos testes EXECUTADOS — remover `[Fact]` de um metodo da era de `main` (o metodo
  fica no arquivo e nunca mais roda) mais 2 stubs vazios passa. O item 3 nao tem esse furo porque
  compara com os nomes VERDES do TAP. Derivacao auditada em `main`: 288 `[Fact]` + 49 `[InlineData]`
  = 337 = `Total` real, zero `MemberData`/`ClassData`, zero atributo comentado; direcao de erro
  futura e SUBcontagem (piso frouxo), nunca gate impossivel.
- Coerencia `D-...-6` <-> comandos: a frase "os CRITERIOS ficam INTACTOS" e imprecisa (o texto do
  criterio do item 6 tambem mudou), mas a direcao e estritamente mais dura, esta declarada no
  `Source`/SUMMARY, e o ledger `.jdi/decisions/` so recebeu arquivo NOVO (zero M/D) — warning de
  redacao, nao invalida o caminho append-only.

Endurecimento minimo para fechar: **1 teste JS de `applyTranslations` sobre `CALIBRE_BODY`** (ou
assert de paridade entre a lista lida por `getVisibleParagraphs` e a escrita por
`applyTranslations`), que fecha M-E por COMPORTAMENTO — independente de comentario ou string.

**Verdict:** BLOCKED
