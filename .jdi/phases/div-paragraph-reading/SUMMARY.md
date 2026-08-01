# Phase 18: Traducao interativa cega a paragrafo em `<div>` (leitura) — Summary  (slug: div-paragraph-reading)

**Status:** complete · **Tasks:** 6/6 · **Blocked:** 0 · **Iter:** 3 (ralph)

Iter 1 entregou o codigo (secoes abaixo). Iter 2 nao tocou em `src/` nem `test/` — consertou os
`Verify:` ocos do DoD apontados pelo `## DoD Critic` do REVIEW.md (`# Iter 2` no meio deste
arquivo). Iter 3 nao tocou em `src/` nem no CONTEXT.md — fechou por COMPORTAMENTO o mutante M-E que
o DoD critic da iter 2 deixou passar (`## Iter 3` no fim deste arquivo).

# Iter 1 — o fix

## Tasks

- **T-1** `harness.js`: `parseSelector` virou lista (`splitSelectorGroup` + `parseSimpleSelector`) e
  `matchDescendants` faz UMA passada por `descendantElements` casando qualquer parte
  (`matchesAnyPart`) — ordem de DOCUMENTO, sem laco externo por seletor. Split de virgula ignora
  `[...]` e valor entre aspas (`scroll.js:32` monta `[data-chapter-href="<href>"]` com href de EPUB,
  input nao confiavel, `csharp.md` §4). 6 testes novos em `harness.test.js`.
- **T-4** `TranslateChapterAsync_ForCalibreStyleBody_TranslatesLeafDivParagraphs` reusando
  `CalibreFixtures.PartiallyCoveredBody`: 3 paragrafos na ordem; wrapper, div-`<img>` e div-`&#8226;`
  fora. NSubstitute sobre `Contracts/`, zero I/O.
- **T-2** 7 testes novos em `translation.test.js` (6 com `calibre` no nome, `test('` na coluna 0):
  3 div-folha visiveis; ordem de documento com `p`/div intercalados; `applyTranslations` no elemento
  do indice devolvido; round-trip apply->clear com `dataset.original === undefined`; wrapper/img/
  bullet nunca candidatos; `console.warn` so quando ha texto e zero candidato. 13 existentes intocados.
- **T-5** `ExtractParagraphs` + `[GeneratedRegex] ParagraphRegex()` DELETADOS;
  `TranslationManager.cs:244` -> `HtmlUtility.ExtractTextBlocks`; `HtmlInjectionTests.cs:304`
  `Assert.Equal(8->7, factories.Count)` no MESMO commit. Zero teste deletado/renomeado/pulado.
- **T-3** `_translatableCandidates(pg)` interno (nao em `window`): `querySelectorAll('p, h1..h6, li,
  div')`, `DIV` so se `querySelector('div, p, h1..h6, li') === null` E `/\p{L}/u`; demais tags mantem
  o filtro de texto nao-vazio. As 3 funcoes leem SO essa lista; `clearTranslations` filtra por
  `dataset.original !== undefined` (fim do `'p[data-original]'`). `console.warn` de `D-...-5`.
- **T-6** gates finais + escopo de diff (abaixo).

## Commits

| sha | subject |
|---|---|
| `a72c4a2` | test(div-paragraph-reading): support selector groups in the JS DOM harness |
| `a3eee90` | test(div-paragraph-reading): cover calibre body in TranslateChapterAsync |
| `1b13648` | fix(div-paragraph-reading): extract text blocks in TranslateChapterAsync |
| `537e595` | test(div-paragraph-reading): add failing calibre cases for paragraph selection |
| `fd5f177` | test(div-paragraph-reading): compare vm-realm results in the caller realm |
| `e00c066` | fix(div-paragraph-reading): select calibre leaf divs as translatable paragraphs |

`.gitignore` (alteracao local do usuario) **fora de todos os commits** — confirmado:
`git log --name-only main..HEAD | grep -c "^.gitignore"` = **0**.

## RED -> GREEN

**T-2 (JS)** — `node --test test/js/translation.test.js`

- ANTES (producao ainda com `querySelectorAll('p')`): `# tests 20 / # pass 13 / # fail 7`.
  Falharam os 7 novos: `returns every calibre leaf div that holds letters`, `leaves the calibre
  wrapper, the image div and the bullet div out`, `indexes p and calibre div elements in document
  order`, `applyTranslations writes into the calibre div the reported index points at`,
  `clearTranslations restores a translated calibre div and drops the marker`, `warns when the page
  has text but no translatable paragraph`, `stays quiet when a calibre div is translatable`.
- DEPOIS (T-3): `# tests 20 / # pass 20 / # fail 0`.

**T-4 (C#)** — `dotnet test ... --filter "FullyQualifiedName~TranslateChapterAsync"`

- ANTES: `Failed!  - Failed: 1, Passed: 6, Skipped: 0, Total: 7` —
  `TranslateChapterAsync_ForCalibreStyleBody_TranslatesLeafDivParagraphs`:
  `Assert.Equal() Failure: Values differ / Expected: 3 / Actual: 0`.
- DEPOIS (T-5): `Passed!  - Failed: 0, Passed: 7, Skipped: 0, Total: 7`.

## Gates (saida real)

| Gate | Resultado |
|---|---|
| `node --test test/js/` | `# tests 73 / # pass 73 / # fail 0` (B_js medido em `main` = **60**; piso `B_js+9` = 69) |
| Cobertura JS `--experimental-test-coverage` | `translation.js 100.00 linha / 100.00 branch / 100.00 funcs` (os 4 arquivos 100%) |
| `dotnet build TranslateReader.slnx -c Release` | `0 Error(s)`, `64 Warning(s)` — **zero** warning em `HtmlUtility.cs`/`TranslationManager.cs`/`HtmlInjectionTests.cs` (so CS0414/CS0618/MVVMTK0045 pre-existentes) |
| `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` | `Build succeeded. 0 Error(s)` |
| `dotnet test ... -c Release` | `Passed!  - Failed: 0, Passed: 336, Skipped: 2, Total: 338` |
| `dotnet format --verify-no-changes` | limpo nos arquivos tocados (restam `ThemeEngine.cs` e `TranslationManagerTests.cs:560` — legado byte-identico a `main`, D-2) |

## Os 7 `Verify:` do CONTEXT (iter 1 — comandos ANTIGOS, 5 deles SUPERSEDIDOS na iter 2)

`1,4,5,6,7` exit **0**; `2,3` exit **1 por defeito do comando** (reporter — desvio D-1), criterio
atendido com a variante TAP (`N=6 P=6`; `B=13 P=20 piso 17`). O DoD critic depois provou que 5 dos 7
comandos (1,2,3,4,6) eram ocos; substituidos na iter 2 por `D-2026-08-01-div-paragraph-reading-6` —
matriz de mutacao e resultados reais em `## Iter 2`.

## Desvios do PLAN

- **D-1 — itens 2 e 3 do DoD saem exit 1 por causa do REPORTER, nao por teste vermelho.** O Node 24
  (`v24.14.0`) usa o reporter `spec` por padrao mesmo com stdout redirecionado; `grep -qE "^# fail
  [[:space:]]+0$"` nunca casa. Os MESMOS comandos + `--test-reporter=tap` saem **exit 0**. Na iter 1
  registrei so em `.jdi/todos/` (CONTEXT.md imutavel para o doer); **resolvido na iter 2** via
  decisao nova + edicao do CONTEXT.md.
- **D-2 — baseline C# real desmente a projecao do PLAN, PARA CIMA.** O PLAN projetava `main` em
  320/322 e piso exato 321/323 ("margem zero"). A corrida real mediu **Failed 0, Passed 335,
  Skipped 2, Total 337** ANTES de qualquer mudanca minha, e **336/338** depois (+1 = o teste de
  T-4). O item 6 usa `>=`, entao FECHA com folga. Nenhum teste foi deletado, renomeado, pulado ou
  marcado: `+1` liquido, exatamente como o PLAN mandava.
- **D-3 — commit extra `fd5f177`, fora dos 6 do PLAN.** Dois asserts de T-2 usavam
  `deepStrictEqual` contra valores produzidos DENTRO do `node:vm` do harness; o Node rejeita por
  prototipo (`same structure but not reference-equal`), nao por conteudo. `Array.from` reconstroi no
  realm do teste. Commit separado para T-3 ficar so com `translation.js`. O RED foi RE-MEDIDO com
  os asserts corrigidos e a producao antiga: continua `# pass 13 / # fail 7`.
- **D-4 — cobertura de `TranslateChapterAsync`: 100% linha, 83,3% branch** (o PLAN pedia >= 90% em
  ambos). O unico branch parcial e `chapter?.Title` (`TranslationManager.cs:265`, 1/2): linha NAO
  tocada por esta phase (`git diff main` mostra so a linha 244) e o irmao intocado
  `TranslateParagraphsAsync` tem o mesmo `chapter?.Title` e o mesmo branch-rate `0,8333` — divida
  pre-existente, nao regressao. NAO adicionei teste fora de escopo so para mover a metrica; anotado
  em `.jdi/todos/`. A linha efetivamente alterada (244) tem 100% de cobertura.

## Fora de escopo (mantido)

`ReaderPage.xaml.cs` / `ReaderPageModel.cs` intocados (D-...-5); membro `TranslateChapterAsync` de
`ITranslationManager` mantido (D-...-4); modo Scroll intocado; `:has()`/combinadores nao entraram no
harness; nenhum parser de HTML novo; nenhuma dependencia npm. Aviso VISUAL ao usuario e validacao em
WebView real seguem em `## Deferred to PR review`.

# Iter 2 — fix dos gates do DoD

**Escopo:** ZERO linha de `src/` ou `test/`. `git diff --stat -- src/ test/` = **vazio**. O codigo da
iter 1 passou os 8 gates do reviewer (JS 73/73, C# Failed 0 / Passed 336 / Total 338, build 0 erros,
RED-first reproduzido pelo reviewer em worktree) — o que estava quebrado era a PROVA. Consertei a
prova.

## O que mudou

| Arquivo | Mudanca |
|---|---|
| `.jdi/decisions/D-2026-08-01-div-paragraph-reading-6.md` | **NOVO** — supersede os `Verify:` dos itens 1, 2, 3, 4 e 6. D-...-1..5 **intocadas** |
| `.jdi/phases/div-paragraph-reading/CONTEXT.md` | 5 linhas `**Verify:**` trocadas + `**Source:**` de cada uma citando o motivo. Os **criterios** (texto do `- [ ]`) nao mudaram, exceto o do item 6, que passou a dizer "piso DERIVADO de `main`" |
| `.jdi/DECISIONS.md`, `.jdi/todos.md` | regerados por `npx -y jdi-cli render` (views gitignored) |

Itens **5 e 7 nao foram tocados** — o critic os confirmou solidos. Provado por `diff` entre o
`Verify:` extraido do CONTEXT.md de hoje e o da iter 1: **byte-identicos**.

**Contingencia do PLAN acionada.** O PLAN proibia `D-...-6` para a aritmetica de contagem, com a
excecao "so se a corrida real contradisser a medicao". Contradisse: o PLAN projetava `main` em
Passed 320 / Total 322 e chamava `321/323` de "margem zero"; o baseline REAL de `main` e
**Failed 0, Passed 335, Skipped 2, Total 337**. O piso do PLAN tinha folga de **15 testes**, nao
zero. Registrado na decisao.

## Matriz de mutacao — repo real NUNCA mutado

Lab: `git clone --local` do repo em scratchpad, `main` local criada de `origin/main` (`9e07c83`).
Cada caso: `git checkout -- . && git clean -fd` -> aplica mutacao -> roda ANTIGO e NOVO -> restaura.
`OLD-TAP` = o comando ANTIGO com `--test-reporter=tap` acrescentado, i.e. exatamente a variante
RE-AUTORADA que o reviewer rodou na iter 1 — e a unica comparacao justa de CAPACIDADE para os itens
2 e 3, ja que o comando literal e um reprovador constante.

### (a) Contra-exemplos do critico: NOVO pega (exit 1), ANTIGO nao (exit 0)

| Caso | Mutacao aplicada | Item | ANTIGO | NOVO |
|---|---|---|---|---|
| CE-1 | filtro de letra invertido (`if (!_LETTER_RE.test(` -> `if (_LETTER_RE.test(`), fix efetivamente AUSENTE | 1 | **0** | **1** |
| CE-2 | `applyTranslations` desviado para `querySelectorAll('[data-original], p, div')` + COMENTARIO `// _translatableCandidates(pg)` repondo a contagem | 1 | **0** | **1** |
| CE-3 | 3 testes da era de `main` DELETADOS de `translation.test.js` (fica `17 >= 17`) | 3 | **0** (OLD-TAP) | **1** |
| CE-4 | linha 244 -> `HtmlUtility.LegacyParagraphExtract(bodyContent)` (extrator defeituoso so RENOMEADO) | 4 | **0** | **1** |
| CE-5 | 6 testes calibre reais renomeados para fora do filtro + 4 stubs `test('calibre stub N', () => {})` sem assert | 2 | **0** (OLD-TAP) | **1** |
| CE-6 | log sintetico `Passed! - Failed: 0, Passed: 321, Skipped: 2, Total: 323` | 6 | **0** | **1** |
| CE-6b | 3 testes C# REALMENTE deletados (`TranslationManagerTests.cs`, chaves balanceadas, `[Fact]` 289->286) + log honesto `333/335` | 6 | **0** | **1** |

Fidelidade das mutacoes conferida por execucao, nao assumida:
CE-3 -> `# tests 17 / # pass 17 / # fail 0` (o piso `B+4=17` fecha com 3 testes de `main` fora);
CE-5 -> `--test-name-pattern="calibre"` casa **so** `ok 1..4 - calibre stub 1..4`, `# pass 4`, zero
round-trip; CE-1 -> a suite acusa 6 falhas enquanto o item 1 ANTIGO sai 0.

### (b) Zero falso positivo: NOVO sai exit 0 no repo REAL sem mutacao

| Item | 1 | 2 | 3 | 4 | 5 | 6 | 7 |
|---|---|---|---|---|---|---|---|
| NOVO no repo real | **0** | **0** | **0** | **0** | **0** (intocado) | **0** | **0** (intocado) |

Rodados a partir do `CONTEXT.md` de hoje, extraidos por `sed` da linha `**Verify:**` — **nao
digitados de memoria**. Os 7 extraidos foram `diff`-ados contra os arquivos que a matriz executou:
**byte-identicos** (1,2,3,4,6 = NOVO; 5,7 = os da iter 1).

### (c) Sem regressao de gate: NOVO continua pegando tudo que o ANTIGO pegava

| Caso | Mutacao | Item | ANTIGO | NOVO |
|---|---|---|---|---|
| R1 | `querySelectorAll('p')` de volta em `getVisibleParagraphs` | 1 | 1 | **1** |
| R2 | helper renomeado (`function _translatableCandidates` some) | 1 | 1 | **1** |
| R3 | so 3 testes calibre (abaixo do piso `N>=4`) | 2 | 1 | **1** |
| R4 | `assert.strictEqual(1, 2)` injetado no round-trip `applyTranslations ... calibre` | 2 e 3 | 1 | **1** |
| R5 | `ExtractParagraphs` de volta em `HtmlUtility.cs` | 4 | 1 | **1** |
| R6 | nenhum `HtmlUtility.ExtractTextBlocks` em `TranslationManager.cs` | 4 | 1 | **1** |
| R7 | teste da era de `main` (`clearTranslations restores every original...`) ficou vermelho | 3 | 1 | **1** |
| R8 | log `Passed: 300, Total: 302` (abaixo ate do piso ANTIGO) | 6 | 1 | **1** |
| R9 | log `Failed! - Failed: 1` | 6 | 1 | **1** |

### Baseline do lab (prova do defeito que o critic apontou)

| Comando | exit no repo 100% verde |
|---|---|
| item 2 ANTIGO (literal do CONTEXT) | **1** — nao pode sair 0 no Node 24 |
| item 3 ANTIGO (literal do CONTEXT) | **1** — idem |
| itens 2 / 3 OLD-TAP (variante re-autorada pelo reviewer) | 0 |
| itens 1, 4 ANTIGOS | 0 |

Metodo para o item 6: a parte estatica (`git grep`/`comm`) rodou contra mutacoes REAIS do codigo; a
parte de parse do sumario rodou com logs sinteticos, substituindo **apenas** a invocacao
`dotnet test ... > TestResults/dod6.log 2>&1` por `cp <log> TestResults/dod6.log` — o resto do
comando (incluindo o `awk`) e byte-identico, gerado por substituicao de string sobre o proprio
arquivo do comando. O comando COMPLETO, com `dotnet test` de verdade, rodou no repo real: exit 0.

## Por que cada comando novo fecha o buraco

- **Item 1** — comentarios (`//` e `/* */` de uma linha) removidos ANTES do grep; o helper tem de ser
  lido em linha de CODIGO dentro do corpo de cada uma das 3 funcoes (range `awk` de
  `window.<fn> = function` ate `^};`); as 2 guardas do corpo do helper checadas na polaridade certa
  (sem `!` na de folha, com `!` na de letra). Continua sendo, por desenho, gate ESTRUTURAL de fonte
  unica — a prova de COMPORTAMENTO do corpo esta DELEGADA aos itens 2 e 3, agora endurecidos, e isso
  esta escrito no `**Source:**` da linha.
- **Item 2** — TAP pinado (sem isso o comando e um reprovador constante) + os 3 testes de round-trip
  get/apply/clear exigidos por NOME EXATO como `^ok <n> - <nome>$`. Stub sem assert nao produz esses
  nomes. `N >= 4` fica como piso adicional, nao como prova.
- **Item 3** — TAP pinado + `# skipped 0` + `comm -23` entre os nomes de
  `git show main:test/js/translation.test.js` e os nomes VERDES do TAP do HEAD (vazio obrigatorio).
  Teste deletado, renomeado, pulado OU vermelho some da lista de `ok` e o `comm` acusa. Piso `B+4`
  mantido por cima.
- **Item 4** — ausencia varrida no repo tracked inteiro via `git grep` em `src/*.cs`/`test/*.cs`
  (`grep -r` leria `obj/`, onde o source generator de `[GeneratedRegex]` emite `ParagraphRegex` — era
  falso positivo garantido) + presenca amarrada ao CORPO de `TranslateChapterAsync` (range `awk`) e
  exatamente UMA atribuicao a partir de `bodyContent` nesse corpo (mata o rename E o "legado
  adicionado ao lado"). Conferido que a linha 195 (`= HtmlUtility.ExtractTextBlocks(bodyContent)`,
  outro metodo) fica FORA do range.
- **Item 6** — piso derivado de `main` no proprio comando: `B` = `[Fact]` + `[InlineData]` contados em
  `main` = **288 + 49 = 337**, que bate 1:1 com o `Total: 337` da corrida real de `main` (0 uso de
  `MemberData`/`ClassData` no projeto — conferido). Exige `Total >= B+1`, `Skipped <=` contagem de
  `Skip=` em `main` (**2**), `Failed == 0`, coerencia `Passed+Skipped+Failed == Total` (mata log
  sintetico incoerente) e `comm -23` nome a nome dos metodos publicos de teste C# de `main` contra o
  HEAD.

## Gates finais (saida real, repo real)

| Gate | Resultado |
|---|---|
| `node --test --test-reporter=tap test/js/` | `# tests 73 / # pass 73 / # fail 0 / # skipped 0` |
| `DOTNET_CLI_UI_LANGUAGE=en dotnet test ... -c Release` | `Passed! - Failed: 0, Passed: 336, Skipped: 2, Total: 338` |
| 7 `Verify:` extraidos por `sed` do CONTEXT.md | **7/7 exit 0** |
| Item 6, evidencia interna | `dod6-base.txt` 308 nomes, `dod6-head.txt` 309, `comm -23` = **vazio**; B=337, piso Total 338 |
| `git diff --stat -- src/ test/` | **vazio** — nenhum teste deletado, renomeado ou pulado |
| `.gitignore` | alteracao local do usuario, **fora do commit** |

## Debitos que seguem abertos (nao regridem, nao bloqueiam)

W-2 (harness falha ABERTO para aspas dentro de valor de atributo, `harness.js:315-333`, pre-existente
de `main`) e W-3 (branch `chapter?.Title`, `TranslationManager.cs:265`, 83,3% de branch, divida
pre-existente — o irmao intocado `TranslateParagraphsAsync` tem o mesmo 0,8333) seguem em
`.jdi/todos/`, como na iter 1. Nenhum e alcancado por esta iter, que nao tocou codigo.

## Iter 3 — fecha M-E por comportamento

**Escopo:** 1 commit, 1 arquivo, `+53` linhas em `test/js/translation.test.js`. **ZERO linha de
`src/`** — `translation.js` esta correto, M-E e um mutante hipotetico. **ZERO linha de
`CONTEXT.md`/`.jdi/decisions/`** — nenhuma decisao nova foi necessaria (justificativa abaixo).
Blocker unico do `## DoD Critic (enhanced)`: a suite tinha `getVisibleParagraphs` sobre
`CALIBRE_BODY` e `clearTranslations` sobre div unico, mas **nao tinha `applyTranslations` sobre um
corpo capaz de DESSINCRONIZAR**. O teste de apply existente
(`applyTranslations writes into the calibre div the reported index points at`) roda sobre
`<p>one</p><div>two</div><p>three</p>`, forma em que TODO elemento e paragrafo — qualquer seletor
ingenuo coincide com o helper, entao o teste nao discrimina.

### Os 2 testes novos (ambos com `calibre` no nome, `test('` na coluna 0)

| Teste | O que amarra |
|---|---|
| `applyTranslations writes each calibre index into the element getVisibleParagraphs read it from` | Paridade elemento a elemento sobre `CALIBRE_BODY`: cada indice devolvido pelo read escreve no MESMO elemento (`textContent` + `dataset.original` dos 5 blocos do capitulo, na ordem), e os 2 blocos nunca reportados (div-`<img>`, div-`&#8226;`) ficam intactos |
| `applyTranslations leaves the calibre wrapper alone instead of collapsing the chapter` | O wrapper nao recebe `dataset.original`, o capitulo mantem seus 5 blocos, e a lista relida DEPOIS da escrita e identica a lida ANTES (paridade read↔write) |

Helper `elementChildren(node)` (filtra `nodeType === 1`) porque o fixture e `join('\n')` e o wrapper
tambem carrega text nodes.

### Matriz RED-first (`.claude/rules/csharp.md` §6)

Lab **descartavel** em scratchpad (`lab-me`: copia dos 4 scripts de producao + `test/js`, estrutura
de pastas preservada porque `harness.js` resolve `SCRIPT_DIR` por caminho relativo). **Repo real
nunca mutado** — `git status` durante toda a iter: so `test/js/translation.test.js` (+ `.gitignore`
do usuario, fora de commit). Mutante M-E aplicado SO no lab: em `applyTranslations`,
`var ps = _translatableCandidates(pg);` -> `var ps = pg.querySelectorAll('[data-original], p, div');`
precedido de comentario de BLOCO multi-linha contendo `_translatableCandidates(pg)`, para repor a
contagem do grep estrutural.

| Codigo | Suite | `node --test test/js/` | DoD item 1 | DoD item 2 |
|---|---|---|---|---|
| **M-E (lab)** | ANTIGA (`HEAD~1`) | `# tests 73 / # pass 73 / # fail 0` — **exit 0** | **exit 0** | **exit 0** |
| **M-E (lab)** | NOVA | `# tests 75 / # pass 73 / # fail 2` — **exit 1** | exit 0 | **exit 1** |
| **REAL (repo)** | NOVA | `# tests 75 / # pass 75 / # fail 0` — **exit 0** | exit 0 | exit 0 |

As 2 falhas sob M-E sao EXATAMENTE os 2 testes novos — nenhum teste da era anterior quebra sob
mutante (o mutante coincide com o helper nos corpos antigos, por isso ele passava). O assert que
disparou nomeia o bug de usuario: `the wrapper div itself was translated` /
`actual: 'First calibre paragraph...\nSecond calibre paragraph...\n&#8226;\nThird paragraph...'` —
i.e. o capitulo inteiro colapsado em um bloco, exatamente o `CHAPTER_COLLAPSED: true` do critico.

Reproducao confirmada da premissa do critico (linha 1 da matriz): M-E realmente saia exit 0 nos
gates e verde na suite. Depois do fix, **os itens 2 e 3 do DoD deixam de ser ocos para M-E** — item
2 vai de exit 0 para exit 1 sob o mutante (item 3 roda a suite inteira com `# fail 0`, mesma
consequencia). A mitigacao do W-2 da review ("os itens 2/3 pegam o desvio real"), que o critico
falsificou, passa a ser verdadeira **por teste**, nao por redacao.

### Item 1 do DoD: por que NAO mexi (argumento, nao omissao)

O `sed -e 's://.*::' -e 's:/\*.*\*/::'` do item 1 e por LINHA e nunca cobrira comentario de bloco
multi-linha nem string literal — fechar isso de verdade exige um tokenizador de JS, nao mais um
`sed`; qualquer `sed` multi-linha seria outra aproximacao com o proximo bypass do lado de fora.
O `**Source:**` do item 1 ja declara, desde a iter 2, que ele e gate **ESTRUTURAL** de fonte unica e
que a prova de **COMPORTAMENTO** e delegada aos itens 2 e 3. Ate a iter 2 essa delegacao era falsa
(M-E provou); com os 2 testes novos ela e verdadeira e **medida** (tabela acima). Endurecer o grep
depois disso seria trocar prova comportamental por aproximacao textual — custo de uma decisao nova
+ edicao de CONTEXT + re-rodada dos 7 gates, sem fechar nenhum mutante que os itens 2/3 ja nao
peguem. Por isso: `.jdi/decisions/` **inalterado nesta iter** (zero A/M/D), CONTEXT.md
byte-identico, caminho append-only nao acionado. O proprio critico pediu isso: "Endurecimento
minimo para fechar: 1 teste JS de `applyTranslations` sobre `CALIBRE_BODY`".

### Itens 4 e 6: seguem WARNING com backstop nomeado (registrado, nao fechado)

- **Item 4** — desvio para extrator renomeado com a linha genuina em `/* */` sai exit 0, mas o
  extrator defeituoso teria de existir para compilar e o **item 5** (`dotnet test` real do filtro
  `TranslateChapterAsync`, com o teste calibre provado RED-first na iter 1 contra exatamente esse
  extrator) fica VERMELHO. Backstop comportamental real, ja executado nesta phase.
- **Item 6** — o `comm` do lado HEAD vem de grep ESTATICO (metodo que perde o `[Fact]` continua no
  arquivo e nunca mais roda). Assimetria conhecida e documentada; direcao do erro futuro
  (`MemberData`/`ClassData`) e SUBcontagem, i.e. piso frouxo, nunca gate impossivel. Fechar exige
  trocar a fonte do lado HEAD por lista de testes EXECUTADOS (`--list-tests`), o que muda o comando
  de novo — desproporcional para uma phase que ja gastou 2 iters em prova, e sem mutante aberto
  contra ela nesta phase. Fica anotado aqui como divida de gate para a proxima phase que reescrever
  DoD de C#.

### Gates finais (saida real, repo real, iter 3)

| Gate | Resultado |
|---|---|
| `node --test test/js/` | `# tests 75 / # pass 75 / # fail 0 / # skipped 0` (era 73/73; **+2**, zero deletado/renomeado/pulado) |
| `DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release` | `Passed! - Failed: 0, Passed: 336, Skipped: 2, Total: 338` — identico ao baseline da phase |
| 7 `Verify:` extraidos por `sed` do `CONTEXT.md` COMMITADO | **7/7 exit 0** |
| Evidencia interna | item 2: `# tests 8 / # pass 8` (`N` calibre = 8, piso 4); item 3: `# tests 22 / # pass 22 / # fail 0 / # skipped 0`, `comm -23` vs `main` vazio, piso `B+4 = 17` |
| `git diff --stat main..HEAD -- src/` | inalterado desde a iter 1 (so `Resources/Raw/wwwroot/js/translation.js` + Core) |
| `.gitignore` | alteracao local do usuario, **fora do commit** |

### Commit

| sha | subject |
|---|---|
| `ec037f6` | test(div-paragraph-reading): close the apply/read desync gap on calibre bodies |

---

## Iter 4 — rodada de warnings

Rodada de warnings do `/jdi-issue` sobre o `## Warnings` do REVIEW da iter 3 (o loop ja tinha
convergido `APPROVED_WITH_WARNINGS`). **1 de 6 fechado.** Producao (`src/`) intocada: nenhum dos 6
warnings pedia mudanca de codigo de producao.

### W-5 — harness falha ABERTO com aspas no valor do atributo → **FECHADO**

Unico com fix barato e real, e o de maior relevancia de seguranca: `scroll.js:32` monta
`'[data-chapter-href="' + href + '"]'` com `href` vindo do EPUB — input NAO confiavel
(`.claude/rules/csharp.md` secao 4).

**Defeito medido antes do fix** (`test/js/harness.js`, `parseSimpleSelector`): o loop de
`SELECTOR_PART_RE` era `exec` global e PULAVA o texto que nao conseguia ler. Quando nada casava,
sobrava `{ tag: null, classes: [], attributes: [] }` — e matcher vazio casa com TODO elemento:

| selector construido pelo shape do `scroll.js:32` | antes | depois |
|---|---|---|
| `[data-chapter-href="ch"1"]` (href com aspas) | **`BODY,DIV,SPAN`** (casou o documento inteiro) | `SyntaxError` |
| `[data-chapter-href="ch"1.xhtml"]` | `(none)` — `.xhtml` virou "classe" | `SyntaxError` |
| `]]garbage((` | **`BODY,DIV,SPAN`** | `SyntaxError` |
| `[data-chapter-href="a,b].xhtml"]` (virgula + `]` no valor) | `alvo` (ja correto) | `alvo` (preservado) |
| `[data-chapter-href="ok.xhtml"]` | `DIV` | `DIV` |

Uma WebView real REJEITA o seletor invalido; o harness aprovava. Isso e falha ABERTA: teste verde
podia assinar embaixo de codigo que na WebView seleciona o capitulo errado (ou o documento inteiro).

**Fix (comportamento, nao redacao):** `parseSimpleSelector` agora presta contas de CADA caractere
da parte (`match.index === cursor` + `cursor === rest.length`) e lanca `SyntaxError` quando nao
consegue — o mesmo contrato do DOM real. `querySelector` tambem lanca em vez de devolver `null`,
senao o `if (!ch) return;` do `scrollToChapter` engoliria o seletor invalido como no-op silencioso.

**RED-first (transcript real):**

```
# ANTES do fix — node --test test/js/harness.test.js
✔ querySelectorAll keeps an attribute value holding a comma and a bracket in one selector
✖ querySelectorAll refuses an unparseable selector instead of matching every element
✖ querySelector refuses an unparseable selector instead of reporting not-found
✖ querySelectorAll refuses an empty part of a selector group
ℹ tests 10 / ℹ pass 7 / ℹ fail 3        (AssertionError: Missing expected exception (SyntaxError))

# DEPOIS do fix — node --test test/js/  (suite JS INTEIRA)
ℹ tests 79 / ℹ pass 79 / ℹ fail 0 / ℹ cancelled 0 / ℹ skipped 0 / ℹ todo 0   -> exit 0
```

**Sem regressao, nome a nome:** os 75 nomes de teste do HEAD anterior comparados por `comm -23`
contra a lista de `✔` do HEAD novo → **0 ausentes**; os 4 extras sao os 4 testes novos. Nenhum
teste deletado, renomeado ou pulado. Escopo do fix: so `test/js/` (bridge/paginated/scroll/
translation seguem verdes com o harness novo).

Commit: `a57b916` — `fix(div-paragraph-reading): make the JS harness reject unparseable selectors`.

### W-1 — 9 WHITESPACE do `dotnet format` (legado) → **NAO FECHADO**

Dono e D-2 + a phase `baseline-de-estilo`. **Registro confirmado**: `.jdi/todos/LEGACY.md:367-377`
ja descreve os hits e a regra ("Nao corrigir avulso — a phase de estilo deve rodar `dotnet format`
uma vez, no escopo dela"). Nada a acrescentar; seguido.

### W-2 / W-3 — limite dos gates textuais dos itens 1 / 4 / 6 → **NAO FECHADO (registrado)**

O DoD critic ja confirmou que os sobreviventes sao mutantes EQUIVALENTES (item 1) ou tem backstop
comportamental provado por execucao (itens 4/5 e 6). Fechar por texto exigiria tokenizador de JS e
de C# — cada aproximacao `sed`/`grep` adicional teria o proximo bypass do lado de fora. Nao estava
registrado em `.jdi/todos/`; **registrado agora** com a acao concreta para o item 6 (derivar o lado
HEAD de `dotnet test --list-tests` em vez de grep estatico).

Commit: `e6a5b46` — `docs(div-paragraph-reading): register the textual DoD gate debt`.

### W-4 — branch 83,33% em `chapter?.Title` → **NAO FECHADO**

Divida pre-existente, `TranslationManager.cs:265` — linha que esta phase NAO tocou (o diff toca so
a 244), e o irmao intocado `TranslateParagraphsAsync` tem o mesmo `0,8333`. D-2 exime legado. Ja
registrado em `.jdi/todos/2026-08-01-div-paragraph-reading.md` com o custo de fechar (1 teste).

### W-6 — `console.warn` invisivel ao usuario → **NAO FECHADO**

Decisao de UX (toast/badge) DEFERIDA ao PR review por `D-2026-08-01-div-paragraph-reading-5`
(`## Deferred to PR review`). E escolha do humano, nao do doer.

### Gates finais (saida real, repo real, iter 4)

| Gate | Resultado |
|---|---|
| `node --test test/js/` | `ℹ tests 79 / ℹ pass 79 / ℹ fail 0 / ℹ skipped 0` → exit 0 (era 75/75; **+4**, zero perdido) |
| `DOTNET_CLI_UI_LANGUAGE=en dotnet test ... -c Release` | `Passed! - Failed: 0, Passed: 336, Skipped: 2, Total: 338` → exit 0 (identico ao baseline) |
| 7 `Verify:` extraidos por `sed` do `CONTEXT.md` COMMITADO | **7/7 exit 0** |
| `git diff --name-only 8eff13c..HEAD -- src/` | **vazio** — zero diff de producao nesta iter |
| `.gitignore` | alteracao local do usuario, **fora de todo commit** |

### Commits da iter 4

| sha | subject |
|---|---|
| `a57b916` | fix(div-paragraph-reading): make the JS harness reject unparseable selectors |
| `e6a5b46` | docs(div-paragraph-reading): register the textual DoD gate debt |
