# Phase 18: Review  (slug: div-paragraph-reading)

**Verdict:** APPROVED_WITH_WARNINGS

Review iter 1 (`mode=verify`), branch `jdi/div-paragraph-reading`, diff `main` (`9e07c83`) ->
HEAD (`c79580d`), 10 commits (6 de codigo, 4 de artefatos `.jdi/`). Toda evidencia abaixo foi
executada nesta sessao pelo reviewer — nada foi aceito do SUMMARY sem reproducao.

## Gates

| Gate | Comando | Resultado real | Status |
|---|---|---|---|
| 1. Build | `dotnet restore && dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` | `0 Erro(s)`, 40 warnings (MVVMTK0045 etc., pre-existentes; zero nos arquivos tocados) | PASS |
| 2. Tests C# | `DOTNET_CLI_UI_LANGUAGE=en dotnet test ... -c Release` | `Failed: 0, Passed: 336, Skipped: 2, Total: 338` — baseline `main` medido pelo reviewer em worktree: `335/2/337`; +1 liquido; >> baseline 167 (D-2) | PASS |
| 2b. Tests JS | `node --test --test-reporter=tap test/js/` | `# tests 73 / # pass 73 / # fail 0 / # skipped 0` — baseline `main` medido: `60/60/0`; os 60 nomes de `main` presentes 1:1 no HEAD (comm = 0 ausentes), +13 novos | PASS |
| 3. Coverage | `dotnet test --collect:"XPlat Code Coverage"` + parse Cobertura; `node --test --experimental-test-coverage test/js/` | 0 arquivos `.cs` novos nesta phase -> gate adopted-mode formalmente SKIPPED; medido mesmo assim: agregado 93,15%; `HtmlUtility` 100/100; `TranslateChapterAsync` **100% linha / 83,33% branch** (ver W-3); `translation.js` **100,00 / 100,00 / 100,00** (4 arquivos JS em 100%) | PASS (nota W-3) |
| 4. Lint | `dotnet format --verify-no-changes` | exit 2 — hits em `ThemeEngine.cs:12,14`, `ReaderPage.xaml.cs:122,124`, `ThemeEngineTests.cs:12`, `TranslationManagerTests.cs:560-561`; TODOS byte-identicos a `main` (regiao 560 = `main:528`; `git diff main` nesses arquivos = 0 linhas). Nenhum drift novo | WARN (legado, D-2) |
| 5. Security/Layer | greps 5.1-5.17 + leitura manual | 5.1/5.2/5.10(sync-async)/5.15b/5.16/5.17 limpos; 5.3 so auto-interface; 5.10b: 4 catches de OCE identicos em `main` (e `TranslationManager.cs:61` faz `throw;`); 5.11 `+=5/-=4` = baseline bootstrap; 5.12 = 1 hit baseline; `catch { }` `ReaderPage.xaml.cs:326,434` pre-existentes em `main`. Phase nao toca C# de UI; o `console.warn` novo e string constante (sem interpolacao, 5.8 ok) | PASS |
| 6. Consistency | `git log --name-only main..HEAD -- src/ test/` | 1 task = 1 commit atomico; arquivos por commit = `files_modified` do PLAN; Conventional Commits com scope `div-paragraph-reading`, tipos corretos (`test`/`fix`/`docs`/`chore`); commit extra `fd5f177` documentado como desvio D-3 do SUMMARY | PASS |
| 7. UI Validation | — | SKIPPED (has_frontend=false, cliente MAUI nativo) | SKIPPED |
| 8. DoD | 7 itens Auto executados um a um (abaixo) | 5/7 exit 0 como escritos; itens 2 e 3 exit 1 **por defeito do comando** (reporter), criterio provado verde com TAP — ver W-1 e veredito (f) | PASS c/ defeito de comando (W-1) |

## Blockers

_Nenhum._

## Warnings

- **W-1 — `Verify:` dos itens 2 e 3 do DoD nao provam o criterio como escritos**
  (`.jdi/phases/div-paragraph-reading/CONTEXT.md:64,68`). O Node 24 (`v24.14.0`) usa reporter
  `spec` mesmo com stdout redirecionado; o sumario sai `ℹ pass 20 / ℹ fail 0` e o
  `grep -qE "^# fail[[:space:]]+0$"` nunca casa -> exit 1 com suite 100% verde. Reproduzido pelo
  reviewer nos dois sentidos (ver veredito (f)). Julgo **WARNING**, nao blocker — argumento em (f).
  Acao: proximo `/jdi-discuss` deve pinar `--test-reporter=tap` em todo `Verify:` que parseie
  `node --test` (precedente: phase `coverage-90`; ja registrado pelo doer em
  `.jdi/todos/2026-08-01-div-paragraph-reading.md`).
- **W-2 — harness falha ABERTO para seletor invalido com aspas no valor**
  (`test/js/harness.js:315-333`, `SELECTOR_PART_RE`). `[data-chapter-href="a"b"]` (href de EPUB
  contendo `"`) nao parseia nenhum atributo e vira seletor sem restricao -> **casou 3 elementos
  (BODY,DIV,SPAN)** no teste do reviewer, enquanto um DOM real lanca `SyntaxError` (falha fechada).
  Comportamento pre-existente do `parseSelector` de `main` (a phase nao o piorou; virgula e `]`
  entre aspas estao CORRETOS — ver (d)); risco e de fidelidade do harness mascarar teste futuro,
  nao defeito do produto. Relacionado: `scroll.js:32` nao usa `CSS.escape` no href (pre-existente,
  intocado, csharp.md §4) — candidato a item de phase futura.
- **W-3 — branch parcial pre-existente em `TranslateChapterAsync`**
  (`src/TranslateReader.Core/Business/Managers/TranslationManager.cs:265-270`, `chapter?.Title`,
  condition-coverage 50% 1/2). Linha NAO alterada pela phase (diff = so `@@ -244 +244 @@`) e o
  irmao intocado `TranslateParagraphsAsync` tem o MESMO 0,8333 na linha 313 — divida pre-existente
  (D-2), nao regressao. Fechamento (1 teste: `ExtractChaptersAsync` sem o href pedido) ja anotado
  em `.jdi/todos/`. Piso D-6 sobre codigo ALTERADO: atendido (ver (g)).
- **W-4 — lint legado** (`ThemeEngine.cs`, `ReaderPage.xaml.cs`, `ThemeEngineTests.cs`,
  `TranslationManagerTests.cs:560-561`): drift de whitespace byte-identico a `main`, exempt por
  D-2. Vira BLOCK-on-new-files quando a phase `baseline-de-estilo` shipar `.editorconfig`.

## Veredito ponto a ponto (a)-(i)

- **(a) RED-first — CONFIRMADO como TDD genuino, por execucao propria em worktree descartavel**
  (branch intocada):
  - JS em `537e595`: `# tests 20 / # pass 13 / # fail 7` — falham exatamente os 7 testes novos
    (nomes conferidos no TAP). Re-medido em `fd5f177` (asserts de realm corrigidos, producao ainda
    antiga): continua `13/7`. Em `e00c066` (fix): `73/73/0`.
  - C# em `a3eee90`: filtro `~TranslateChapterAsync` -> `Failed: 1, Passed: 6, Total: 7`, falha
    `TranslateChapterAsync_ForCalibreStyleBody_TranslatesLeafDivParagraphs`
    (`Assert.Equal() Failure`). Em `1b13648` (fix): `Failed: 0, Passed: 7, Total: 7`.
- **(b) Ordem de documento e estabilidade do pareamento — CONFIRMADO por testes adversariais do
  reviewer (8/8)** alem dos testes commitados: DOM com `h2`/`p`/div-folha/`li`/div-`<img>`/bullet/
  div-aninhado INTERCALADOS devolve indices `[0..5]` em ordem de documento; ciclo
  get->apply->get->clear->get mantem membership, ordem e texto original; cada traducao caiu no
  elemento cujo indice a reportou (`textContent === 'T:' + dataset.original` para os 6); div
  traduzido para texto SEM letra continua candidato (o `el.dataset.original ?? el.textContent` de
  `translation.js:15` resolve o ramo div) e `p` traduzido para string vazia continua candidato
  (`_paragraphText`, `translation.js:24-26`: candidato `p` exige `textContent.trim()` nao-vazio
  antes do apply, logo `dataset.original` gravado nunca e vazio). Os dois ramos sao suficientes.
- **(c) Regressao do harness — NENHUMA.** Suite JS de `main` medida em worktree: `60/60/0`.
  Comparacao nome a nome (TAP `ok N - <nome>`, `comm -23`): **0 testes de `main` ausentes no
  HEAD**, 13 novos (6 `harness.test.js` + 7 `translation.test.js`), `# skipped 0`, nenhum rename.
- **(d) Split de virgula — CORRETO para o input nao confiavel testado:** virgula no valor
  (`[data-chapter-href="a,b.xhtml"]` -> 1 seletor, 1 match), `]` no valor entre aspas
  (`"a]b.xhtml"` -> match exato) e `],` combinados (`"a],b.xhtml"` -> match exato) — todos
  verificados pelo reviewer alem do teste commitado. Aspas DENTRO do valor: falha aberta no
  harness vs falha fechada no DOM real — pre-existente, produto nao afetado (W-2).
- **(e) Remocao de API — ACOMPANHADA, sem afrouxamento:** `HtmlInjectionTests.cs:304` trocou
  literal por literal (`Assert.Equal(8` -> `Assert.Equal(7`, `Assert.Equal` mantido, sem `>=`);
  diff de `test/`: **+1 `[Fact]`, 0 removidos, 0 `Skip` novo**, unico assert removido e o proprio
  literal 8 (numstat `HtmlInjectionTests.cs 1+/1-`, `TranslationManagerTests.cs 32+/0-`);
  `grep -rn "ExtractParagraphs\|ParagraphRegex" src/ test/` = **0 hits**.
- **(f) Defeito de gate (reporter) — CONFIRMADO independentemente; julgo WARNING, nao BLOCKER.**
  Reproducao do reviewer: item 2 como escrito exit **1**; mesmo comando + `--test-reporter=tap`
  exit **0** (`N=6`, `# pass 6 / # fail 0`). Item 3 como escrito exit **1** (log spec mostra
  `ℹ pass 20 / ℹ fail 0` — sem `#`); com TAP exit **0** (`B=13`, piso 17, `# pass 20`). A causa e
  o reporter, nao teste vermelho. Argumento para WARNING: (1) o CRITERIO de cada item ("suite
  passa, sem regressao, piso N") esta objetivamente provado — o mesmo comando, alterado apenas no
  formato de saida, passa; (2) o defeito mora no comando do `Verify:` (autorado no discuss em modo
  auto), nao na entrega — bloquear puniria o codigo por um bug do gate; (3) ha precedente no
  projeto de que a forma correta do comando pina o reporter (phase `coverage-90`), ou seja, isso e
  correcao de tooling ja reconhecida, e o doer seguiu o processo certo: nao editou o CONTEXT
  (imutavel) e registrou em `.jdi/todos/`. O que impediria o APPROVED seria um `# fail != 0` — que
  nao existe. Condicao da aprovacao: a correcao dos `Verify:` futuros fica registrada (ja esta).
- **(g) Cobertura D-6 — piso vale sobre o CODIGO ALTERADO, e passa.** Medido pelo reviewer no
  Cobertura (`TestResults/0360504f-.../coverage.cobertura.xml`): `<TranslateChapterAsync>d__26`
  line-rate 1.0 / branch-rate 0,8333; unico branch parcial na linha **265** (statement
  `BuildTranslationMessages(...)` cujo argumento `chapter?.Title` esta na linha 270), 50% (1/2).
  O diff da phase nesse arquivo e exatamente `@@ -244 +244 @@` — a linha alterada tem 100% de
  cobertura e zero branches proprios. Evidencia de divida pre-existente: o irmao INTOCADO
  `TranslateParagraphsAsync` (`d__27`) tem o mesmo padrao na linha 313 com o mesmo 0,8333.
  Interpretar o piso sobre o metodo inteiro cobraria desta phase um teste para linha que D-2
  exime; classifico como divida pre-existente documentada (W-3). JS: `translation.js` 100% linha /
  100% branch / 100% funcs, medido com `--experimental-test-coverage`.
- **(h) Escopo — LIMPO.** `git diff --name-only main -- src/TranslateReader/
  ':(exclude)...wwwroot/js/'` = vazio (exit 0); unico arquivo tocado em `src/TranslateReader/` e
  `Resources/Raw/wwwroot/js/translation.js`; `git log --stat main..HEAD | grep -c gitignore` =
  **0** — a alteracao local de `.gitignore` esta fora de todos os commits.
- **(i) `console.warn` (D-...-5) — os dois lados testados:** `translation.test.js:263` prova que
  dispara com texto e zero candidato (`<span>so span</span>`, `env.logged('warn', ...)` true) e
  `translation.test.js:273` prova que NAO dispara havendo candidato (`logged(...) === false`).
  Pagina sem texto nenhum nao avisa (`translation.js:36` exige `pg.textContent.trim()`), entao nao
  ha falso positivo em capitulo vazio. Mesmo canal e prefixo `[JS]` de `paginated.js:27`. Mensagem
  constante — sem dado do livro no log (csharp.md §5) e sem superficie de injecao (§4).

## DoD Checklist (gate 8)

Fonte: apenas `CONTEXT.md` (o `.jdi/PROJECT.md` nao tem secao `## Definition of Done`;
`dod=auto_only`, 0 itens manuais).

| # | Criterio | Source | Type | Status | Evidencia |
|---|---|---|---|---|---|
| 1 | `_translatableCandidates` fonte unica; seletores antigos ausentes | CONTEXT | Auto | PASS | exit 0 — helper 1x, chamadas 4x, `querySelectorAll('p')`/`('p[data-original]')` 0x |
| 2 | >= 4 testes `calibre` verdes (round-trip real) | CONTEXT | Auto | PASS* | como escrito exit 1 (defeito de reporter, W-1); com `--test-reporter=tap` exit 0: `N=6`, `# pass 6 / # fail 0` |
| 3 | Suite `translation.js` inteira sem regressao de `main` | CONTEXT | Auto | PASS* | como escrito exit 1 (W-1); com TAP exit 0: `B=13`, piso 17, `# pass 20 / # fail 0` |
| 4 | `ExtractParagraphs`/`ParagraphRegex` removidos; `ExtractTextBlocks` em uso | CONTEXT | Auto | PASS | exit 0; 0 refs em `src/` e `test/` |
| 5 | Teste calibre de `TranslateChapterAsync` + existentes verdes | CONTEXT | Auto | PASS | exit 0 — `Failed: 0, Passed: 7, Total: 7` (piso B+1 = 7) |
| 6 | Suite C# inteira acima do piso | CONTEXT | Auto | PASS | exit 0 — `Failed: 0, Passed: 336, Total: 338` (piso 321/323) |
| 7 | `Pages`/`PageModels` intocados; fix so em `wwwroot/js` + Core | CONTEXT | Auto | PASS | exit 0 — pathspec vazio |

**Totals:** 7 itens | Auto: 7 (7 PASS — 2 deles via comando com reporter pinado, W-1; 0 FAIL) | Manual: 0 pendentes

\* Status semantico. O exit code registrado do comando literal e 1; a causa (reporter `spec` do
Node 24) foi isolada e o criterio provado verde — fundamentacao completa no veredito (f).

## Recommendation

Aprovar e seguir para `/jdi-ship div-paragraph-reading`. Antes da proxima phase que parseie
`node --test`, converter o registro de `.jdi/todos/2026-08-01-div-paragraph-reading.md` em regra
de autoria de `Verify:` (pinar `--test-reporter=tap`, como `coverage-90` ja faz). Os dois debitos
apontados (branch `chapter?.Title` e `CSS.escape` no `scroll.js:32`) sao candidatos naturais a
itens de phase futura — nenhum bloqueia esta.

**Verdict:** APPROVED_WITH_WARNINGS

## DoD Critic (enhanced — forcado por /jdi-issue)

Re-ataque das 7 linhas `Type=Auto`/`PASS` com contra-exemplos EXECUTADOS em copia (repo intocado).
Resultado: **5 linhas ocas**, todas com prova objetiva. As duas que se sustentam sao os itens 5 e 7.

- Item 2 («>= 4 testes `calibre` verdes, round-trip get/apply/clear») — **hollow=true, objective=true**.
  O comando LITERAL de `CONTEXT.md:64` sai **exit 1 com a suite 100% verde**: Node v24.14.0 emite o
  reporter `spec` (`i pass 6 / i fail 0`), e o `grep -qE "^# fail"` nunca casa. Um `Verify:` que nao
  pode sair 0 nao prova criterio nenhum — o PASS registrado veio de um comando RE-AUTORADO pelo
  reviewer (variante `--test-reporter=tap`), nao do comando escrito. E o inverso exato do gate oco,
  com o mesmo efeito: a linha do DoD nao certifica nada. Agravante medido: mesmo a variante TAP e
  proxy de contagem — 4 stubs `test('calibre stub N', () => {})` SEM assert dao `N=4/pass=4` e
  saem exit 0 com ZERO round-trip. O round-trip real existe
  (`test/js/translation.test.js:237-261`, com asserts sobre `dataset.original`), mas quem o prova e
  leitura/execucao externa, nao o gate.
- Item 3 («suite de `translation.js` sem regressao de `main`, piso `B+4`») — **hollow=true,
  objective=true**. Mesmo defeito de reporter, e o piso nao mede regressao: deletando 3 testes da
  era de `main` da copia do HEAD a contagem fica `17 >= 17` e a variante TAP sai **exit 0 com 3
  testes de `main` ausentes**; `main` + 4 stubs vazios da o mesmo `17 >= 17`. A prova real de
  nao-regressao foi o `comm` nome-a-nome que o reviewer rodou FORA do comando.
- Item 1 («`_translatableCandidates` fonte unica») — **hollow=true, objective=true**. Grep de forma:
  (CE-1) filtro de letra invertido em `translation.js:15` — fix efetivamente AUSENTE, div calibre
  com letra excluida — sai **exit 0** enquanto a suite acusa 6 falhas; (CE-2) `applyTranslations`
  trocado para `querySelectorAll('[data-original], p, div')` com um COMENTARIO contendo
  `_translatableCandidates(` repondo a contagem `>= 4` tambem sai exit 0, com so 2 das 3 funcoes
  usando o helper. O grep conta TEXTO, inclusive comentario.
- Item 4 («`ExtractParagraphs`/`ParagraphRegex` removidos; `TranslateChapterAsync` usa
  `ExtractTextBlocks`») — **hollow=true, objective=true**. (CE-4) desviando a linha 244 para um
  `LegacyParagraphExtract(bodyContent)` (o extrator defeituoso apenas RENOMEADO) o comando sai
  **exit 0**, porque o `grep -q "HtmlUtility.ExtractTextBlocks"` casa nas linhas 124 e 195 (outros
  metodos) — a presenca nao esta amarrada ao corpo de `TranslateChapterAsync`, e o grep de ausencia
  so olha `HtmlUtility.cs`, entao um rename que preserve o defeito escapa.
- Item 6 («suite C# nao regride do baseline», piso `321/323`) — **hollow=true, objective=true**. Log
  sintetico `Failed: 0, Passed: 321, Total: 323` passa o `awk` de `CONTEXT.md:81`. O baseline REAL de
  `main` e 335/337 (medido em worktree pela review primaria e corroborado por contagem independente
  de atributos), ou seja o gate aceita **regressao de ate 15 testes**. A entrega real esta em
  baseline+1 (336/338), entao nao houve regressao de fato — o furo e do criterio, nao da entrega.

Linhas que se sustentam: item 5 (piso `B+1` derivado de `main`, sem folga, teste nomeado assertando
o triple `Original/Translated/Index`, red-first reproduzido em worktree) e item 7 (`main` local
conferido == `origin/main` == `ls-remote` = 9e07c83; unico path divergente sob
`src/TranslateReader/` e `Resources/Raw/wwwroot/js/translation.js`).

Familia ja catalogada em `.jdi/todos/` (`[PROCESSO/DoD]`): o gate mede um proxy conveniente — aqui
com um agravante novo, o gate que reprova codigo CORRETO e so "passa" quando alguem reescreve o
comando na hora de rodar.

**Verdict:** BLOCKED
