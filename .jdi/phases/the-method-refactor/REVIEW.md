# Phase 13: Review — FINAL (slug: the-method-refactor, iter 4)

**Verdict:** APPROVED_WITH_WARNINGS

Review final da phase, regenerada do zero na iter 4 (rodada de warnings do `/jdi-issue`).
Auto-suficiente: cobre as 4 iteracoes, os 8 gates re-rodados com numeros desta execucao, o ataque
de mutacao ao gate DoD 4 (3a versao do comando), e o julgamento individual dos warnings que o doer
declarou NAO fechados. Diff revisado: `a390eb9` (main) ate `62aa92b`, branch `jdi/the-method-refactor`,
18 commits. Producao e testes estao IMUTAVEIS desde `7a4081a` (fim da iter 3): a iter 4 tocou apenas
`.gitignore`, `.jdi/DECISIONS.md`, `CONTEXT.md`, `SUMMARY.md` e `.jdi/todos.md` — provado por
`git diff 7a4081a HEAD -- src/ test/` **vazio**.

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0`: **0 Erro(s)**, 40 avisos (todos MVVMTK0045 legados do app MAUI, intocado pela fase) |
| Tests | PASS | **227 aprovados / 2 ignorados / 229 total, 0 falhas** — identico ao baseline das iters 2-3; os 2 ignorados sao os `[Fact(Skip=...)]` de integracao legados de `TranslationEngineTests`; sem regressao vs 196/2/198 (pre-fase) nem vs 167 (D-2) |
| Coverage | PASS | Linhas ALTERADAS pela fase (D-6, 90%): **93,88%** (49 linhas executaveis alteradas, 46 cobertas), medido POR ESTA REVIEW cruzando `git diff -U0 a390eb9 HEAD` com o Cobertura fresco (`TestResults/e932adb4-*/coverage.cobertura.xml`) — nao auto-reportado. Agregado 82,15% (contexto apenas; adopted=true, D-2). Por arquivo: ReadingManager 1/1, TranslationManager 5/5, FileUtility 1/1, ParsingEngine 10/11, HtmlUtility 29/31 |
| Lint | WARN (por design) | `dotnet format --verify-no-changes` (solucao, `core.longpaths=true`): **11 violacoes WHITESPACE**, todas nas MESMAS localizacoes legadas das iters anteriores (`ThemeEngine.cs` 2, `ReaderPage.xaml.cs` 2, `HtmlInjectionTests.cs` 2, `ThemeEngineTests.cs` 1, `TranslationManagerTests.cs` 4) — **zero violacao nova** em arquivo tocado pela fase |
| Security/Layer | PASS (legado = WARN baseline) | 5.1/5.2/5.10 (sync-over-async)/5.15b (Result)/5.17 (mock de concreto)/5.16 (TODO): **zero hits**. 5.3: so auto-referencia de interface. 5.7 XXE: zero parsing XML direto. 5.9 secrets/PII: limpo. 5.12: 1 static mutavel legado (`TranslationEngine.cs:16`, baseline bootstrap). 5.11: 5+=/4-= — identico ao baseline. OCE em `TranslationManager.cs:61` **re-lancado** (`throw` apos persistir Paused). Legados D-2 inalterados: OCE engolido no boundary de UI (3), `catch { }` (2) — app MAUI, zero diff na fase |
| Consistency | PASS | 18/18 commits Conventional Commits com scope `the-method-refactor`, tipos adequados (refactor/test/docs/chore — nao tudo `feat`, D-4); files_modified do PLAN batem com os commits; 1 achado = 1 commit atomico (508933b/a5f7b44/3c5a8cf) |
| UI Validation | SKIPPED | has_frontend=false (cliente MAUI nativo) — por design, nunca bloqueia |
| DoD | PASS | **5/5 Auto `exit 0`**, comandos extraidos LITERALMENTE do CONTEXT.md e executados por esta review; 0 itens Manual (dod=auto_only do /jdi-issue) |

## Blockers

Nenhum.

## Warnings

Remanescentes declarados NAO fechados pelo doer na iter 4 — julgados um a um em (f) abaixo; os
quatro se sustentam:

- **W-1 (wiring end-to-end)** — `UpdateOpfTitleAsync` (`ParsingEngine.cs:115-126`) e o wiring real
  de `InlineCssLinks` sobre EPUB de verdade seguem sem execucao por teste (Cobertura:
  `<UpdateOpfTitleAsync>d__6` = 0%, `CreateTranslatedEpubAsync` = 0%). Prova-los exige fixture com
  I/O de disco, vedada a teste NOVO por `.claude/rules/csharp.md` §6. Mitigado em dupla camada:
  identidade byte-a-byte dos 7 patterns (DoD 4) + semantica em 26 casos de
  `ParsingEngineRegexTests.cs`. Roteado em `.jdi/todos.md:146-167` com candidato de dono.
- **W-2/E1 (call site substituido por string literal)** — evasao adversarial que o gate textual nao
  pega. Nao fechada: exige parser de C#; heuristica de aspas quebraria no proprio arquivo (as linhas
  de `[GeneratedRegex]` sao densas em `""` de verbatim string). Sem caminho acidental. Backstop: PR
  review humano.
- **W-2/E2 (call site vivo so sob `#if SIMBOLO_INDEFINIDO`)** — idem, exige build graph. Sem caminho
  acidental: **zero `#if` em todo o Core hoje** (verificado por esta review). Backstop: PR review
  humano.
- **W-5 (residuos da contagem viva do DoD 5)** — `[ Fact ]` com espacos subconta (fail-closed,
  consistente com o baseline 192); `"[Fact]"` em string sobreconta (classe E1). Piso segue
  `-ge 193` com medida real 214 — folga de 21 attrs coberta pelo Gate 2 (227/229); ratchet roteado
  para a proxima phase em `.jdi/todos.md` (julgamento em (f)).
- **Legado (baseline D-2, fase nao tocou; sem acao)** — 11 violacoes de format; OCE engolido em
  `LibraryPageModel.cs:183`, `ReaderPageModel.cs:222`, `ReaderPage.xaml.cs:308` + `catch { }` em
  `ReaderPage.xaml.cs:326,434`; eventos 5+=/4-=; static mutavel `TranslationEngine.cs:16`.
  Identicos ao bootstrap.

## Ceticismo dirigido (a)-(h) — evidencia propria, executada nesta review

**(a) REGRESSAO DE GATE (risco #1 da rodada) — ZERO REGRESSAO, provado por mutacao.**
Harness proprio em scratchpad (`dod4/harness.sh`); NEW extraido por sed do CONTEXT.md em HEAD, OLD
do CONTEXT.md em `7a4081a` (versao da iter 3); 20 mutantes, repo real nunca mutado. Matriz
`OLD/NEW` (exit):

| mutante | OLD | NEW |
|---|---|---|
| pristino | 0 | 0 |
| M5 do critic (call `:196` `StylesheetRelRegex`->`StylesheetHrefRegex`) | 1 | **1** |
| orfao compensado de CADA um dos 7 nomes (um mutante por nome) | 1 | **1** (nos 7) |
| pattern corrompido (`stylsheet`) | 1 | **1** |
| `IgnoreCase` removido (`ImgSrcRegex`) | 1 | **1** |
| `Singleline` removido (`OpfTitleRegex`) | 1 | **1** |
| factory renomeada (decl+call consistentes) | 1 | **1** |
| call site comentado `//` | 1 | **1** |
| call site em bloco `/* */` | 1 | **1** |

**14/14 mutantes que as versoes anteriores pegavam continuam pegos.** Nenhum caso `OLD=1 / NEW=0`
existe na matriz — a condicao de blocker do dispatch nao ocorreu. Containment mecanico: split das
clausulas por ` && ` da **13 = 13**, e o `diff` acusa diferenca em UMA unica clausula (o passe AWK);
as outras 12 sao byte-identicas — confirma a alegacao "12/13" da SUMMARY/D-...-9.

**(b) O fix novo mata a evasao de prefixo sem falso positivo — SIM.**
Alvos novos: `MyStylesheetRelRegex()` no call site -> **OLD 0 / NEW 1**; `CachedImgSrcRegex()`
(2o nome) -> **OLD 0 / NEW 1**. Nesses mutantes o agregado `-eq 14` segue lendo 14 e
`[GeneratedRegex` segue 7 — quem pega e exclusivamente a fronteira de identificador nova.
Suite de falso positivo (todas as formas legitimas exigidas pelo dispatch): call site na **coluna 1**
0, indentado com **TAB** 0, apos `.` (**`ParsingEngine.StylesheetHrefRegex()`**) 0, **dentro de
parenteses** (`!(StylesheetRelRegex().IsMatch(attrs))`) 0, **apos operador** (`content=OpfTitleRegex()`,
alem do `!` ja presente no pristino em `:196`) 0. **Repo real: NEW `exit 0`.** Zero falso positivo.

**(c) Trilha JDI-legal — LIMPA.**
`git diff 7a4081a HEAD -- .jdi/DECISIONS.md`: **zero linha deletada** (+87) — D-...-9 e append puro,
ultima decisao do arquivo; D-...-5/-7/-8 intactas por consequencia (delecao zero). CONTEXT.md na
iter 4: **1 hunk unico** (`@@ -71,15 +71,19 @@`) cobrindo SO o item 4 do DoD (texto do criterio
ganhou a clausula TOKEN EXATO, `Verify:` trocou apenas o passe AWK, `Source:` registra a 3a
supersessao); itens 1, 2, 3 e 5 **byte-identicos** (fora do hunk).

**(d) `.gitignore` — FECHADO DE VERDADE.**
Entrada `**/TestResults/` na secao "Build artifacts .NET", no estilo de `**/bin/`/`**/obj/`.
`git check-ignore -v`: pega raiz (`TestResults/x/...` -> `.gitignore:18`) **e** aninhado
(`test/TranslateReader.Tests/TestResults/x/...` -> mesma regra). `git ls-files | git check-ignore
--stdin`: **nenhum arquivo rastreado passou a ser ignorado**. `git ls-files | grep bin|obj|TestResults`:
**nenhum artefato de build rastreado**. Nenhum path `TestResults` em commit algum da fase.

**(e) Iter 4 nao tocou codigo — CONFIRMADO.**
`git diff 7a4081a HEAD -- src/ test/` = **vazio**. `git diff a390eb9 HEAD -- src/TranslateReader/` =
**vazio** (fase inteira, DoD 5 clausula 1). Iter 4 mudou exatamente 5 arquivos:
`.gitignore` (+1), `.jdi/DECISIONS.md` (+87), `CONTEXT.md`, `SUMMARY.md`, `.jdi/todos.md` (+15).

**(f) Os 4 warnings NAO fechados — julgados um a um; nenhum era fechavel barato:**
- **W-1**: fechar exige violar §6 (I/O de disco em teste novo) ou refactor de API (path->stream) —
  escopo novo, contra o estatuto finding-driven (D-...-1). Recusa correta, roteamento conferido
  (`todos.md:146-167`, residuo em 164-167 com candidato de dono). SUSTENTA.
- **W-2/E1**: heuristica de descartar string literal em AWK e concretamente perigosa NESTE arquivo —
  as proprias linhas de declaracao carregam `""` de verbatim strings (`@"\brel\s*=\s*""stylesheet"""`);
  um stripper ingenuo de aspas corromperia a contagem de DECLARACAO no codigo pristino, e falso
  positivo e a falha cara num gate. Sem caminho acidental (exige escrever o texto exato da invocacao
  numa string E remover a chamada real). SUSTENTA.
- **W-2/E2**: resolver `#if` exige o build graph. Confirmei **zero `#if` no Core** — sem caminho
  acidental hoje. (Opcao barata existiria — falhar o gate se QUALQUER `#if` aparecer no arquivo —
  mas seria criterio NOVO, nao correcao de medida, e um `#if` de plataforma legitimo futuro viraria
  falso positivo; nao exijo.) SUSTENTA.
- **W-5 ratchet `-ge 193` -> 214**: **CONCORDO com a recusa, nao e esquiva.** Tres razoes: (1) o
  criterio locka o baseline 192; subir o piso muda o CRITERIO, nao a medida — exigiria nova
  supersessao formal, e apertar o proprio criterio no fim da corrida, ja sabendo que passa, nao
  compra protecao NESTA fase (a iter 4 nao mudou codigo e nao ha proxima iteracao: a janela que o
  piso 214 protegeria aqui e vazia); (2) a folga de 21 attrs esta coberta AGORA pelo Gate 2 desta
  review (227 aprovados comparados ao baseline); (3) o valor real do piso 214 e para a PROXIMA phase
  — exatamente onde foi roteado (`todos.md`: "o piso do guardrail nasce igual a medida fechada da
  anterior"). A recusa vem acompanhada de politica escrita, nao de silencio.

**(g) Estatuto finding-driven (D-...-1/-2) nas 18 commits — HONRADO.**
Auditoria commit a commit (`git show --name-only` nos 18): producao aparece SOMENTE nos 3 commits
de refactor da iter 1 (`508933b` 3 arquivos + 2 testes; `a5f7b44` 1; `3c5a8cf` 2) — exatamente os
6 arquivos do Core dos 3 achados nomeados; teste novo so `ParsingEngineRegexTests.cs` (`3f68deb`,
exigido pelo critic da iter 2); todo o resto e `.jdi/*`, `LOOP/REVIEW` e `.gitignore` (housekeeping
do W-4, 1 linha). **Zero escopo novo.** Deferimentos preservados: zip-slip (`ReadingManager.cs:59-60`)
-> `epub-zip-slip`; seam LLamaSharp -> `llm-mobile`; infra de medicao -> todos.md (D-...-2(B)) —
nenhum ganho de memoria/CPU declarado sem medida.

**(h) Pronta para PR — SIM.**
Working tree limpa (unica entrada: regeneracao desta REVIEW.md, deletada pelo orquestrador para a
re-verify); nenhum artefato de build rastreado; 18/18 mensagens em Conventional Commits com scope
correto (D-4); `TestResults/` agora ignorado.

## DoD Checklist (gate 8)

Comandos extraidos literalmente de `CONTEXT.md` (HEAD) e executados nesta review:

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | Achado #1: `IFileUtility.DirectoryHasContent` + `ReadingManager` roteia por ele | CONTEXT | Auto | PASS | exit 0 (contrato + impl + zero `Directory.*` direto + chamada presente) |
| 2 | Gap regression-suite-5(1): teste mockado do branch "ja extraido" + caso real | CONTEXT | Auto | PASS | exit 0 (`ReadingManagerTests` 8 attrs, `FileUtilityTests` cita `DirectoryHasContent`) |
| 3 | Achado #2: 4 metodos HTML saem privados do Manager, viram `public static` em `HtmlUtility` | CONTEXT | Auto | PASS | exit 0 (0 privados no Manager, exatamente 4 publicos na Utility) |
| 4 | Achado #3: 7 `[GeneratedRegex]` byte-identicos, zero orfa, call site por TOKEN EXATO | CONTEXT | Auto | PASS | exit 0 (0 `Regex.*` estatico / partial / 7 attrs / 14 linhas / AWK k=7 / 7 pares attr-assinatura) — comando na 3a versao (D-...-9), atacado por mutacao em (a)/(b) |
| 5 | Guardrail: zero diff app MAUI, zero BenchmarkDotNet, >= 193 attrs VIVOS | CONTEXT | Auto | PASS | exit 0 (diff vazio / 0 pacotes / **214 vivos**, standalone confirma 214 e vivo == textual) |

**Totals:** 5 items | Auto: 5 (5 PASS, 0 FAIL) | Manual: 0 pending

## Estado final da phase

**O que mudou em producao (iter 1, unica que tocou `src/`; 6 arquivos do Core, 3 achados):**
1. **Achado #1 (camada)** — `ReadingManager.ExtractImagesIfNeededAsync` deixou de chamar
   `Directory.Exists`/`GetFileSystemEntries` direto (Business tocando Resource) e roteia por
   `IFileUtility.DirectoryHasContent` novo (7o metodo do contrato, com `<summary>`); o branch
   "ja extraido" virou testavel com mock (fecha D-...-regression-suite-5(1)).
2. **Achado #2 (responsabilidade)** — os 4 metodos privados de HTML por regex de
   `TranslationManager` (`ExtractParagraphs`/`ExtractTextBlocks`/`ReplaceTextBlocksInHtml`/
   `StripHtmlTags` + 3 `[GeneratedRegex]`) foram MOVIDOS verbatim para `HtmlUtility`
   (`public static partial class`), onde CLAUDE.md ja atribui essa responsabilidade.
3. **Achado #3 (csharp.md §2.1)** — `ParsingEngine` virou `public partial class` e os 7 regex
   inline de caminho por-capitulo viram `[GeneratedRegex]` compile-time, patterns e options
   byte-identicos aos originais.

**O que mudou so em teste:** `FileUtilityTests` 9->13, `ReadingManagerTests` 7->8 (iter 1, T-1);
`ParsingEngineRegexTests.cs` novo com 17 attrs/26 casos (iter 2, exigido pelo critic — as
factories provadas por comportamento, sem I/O). Zero teste deletado ou afrouxado na fase.

**O que mudou so em gate/doc (iters 2-4):** o `Verify:` do DoD 4 endurecido 3x, sempre por
contra-exemplo executado (D-...-7 identidade de pattern/options; D-...-8 checagem POR NOME com
descarte de comentario + escopo total de declaracao de pacote + contagem viva no DoD 5; D-...-9
fronteira de identificador contra lookalike prefixado); nota de correcao da frase de containment
da D-...-8 (W-3); `**/TestResults/` no `.gitignore` (W-4); ratchet de piso roteado a proxima phase
(W-5). Zero linha de producao nas 3 iteracoes — o codigo ja estava correto; o gate e que nao provava.

**Numeros finais:** testes 196/2/198 -> **227/2/229** (0 falhas); attrs vivos 192 -> **214**
(vivo == textual, zero comentado); cobertura das linhas alteradas **93,88%** (piso 90%, D-6);
build Windows Release **0 erros**; lint **11 violacoes**, todas legadas (12 no inicio da fase —
1 caiu no hunk de T-1); **zero diff em `src/TranslateReader/`** na fase inteira.

## Para o revisor humano do PR

O que o gate automatizado NAO prova — decisoes de 1 minuto para um humano:

1. **Ninguem executa `UpdateOpfTitleAsync`/`InlineCssLinks` de ponta a ponta** (W-1). Os patterns
   estao provados byte-a-byte e por semantica, mas nenhum teste abre um EPUB real e afirma o
   resultado inteiro — fazer isso exigiria I/O de disco, proibido para teste novo pela regra do
   repo. Se voce quer essa prova, a rota registrada e uma phase de integracao ou API por stream
   (`.jdi/todos.md:146-167`). Decida se aceita o residuo por ora.
2. **O gate do DoD 4 e textual** (W-2/E1/E2). Um autor MAL-INTENCIONADO ainda engana o grep/AWK
   escondendo a invocacao numa string literal ou sob `#if` indefinido — nao ha caminho acidental
   (zero `#if` no Core; a string exige remover a chamada real de proposito), e fechar isso exige
   parser de C#. O backstop contra adversario e exatamente VOCE, agora.
3. **O piso de atributos de teste ficou em 193 com medida real 214** (W-5). A folga so importa se
   uma phase futura deletar testes; a politica registrada e a proxima phase nascer com piso 214.
   Confirme que concorda com "ratchet na virada, nunca no meio".
4. **Julgamento subjetivo do MOVE do achado #2** (Deferred no CONTEXT): os nomes/estilo dos 4
   metodos movidos ficaram coerentes com o `HtmlUtility` existente? Grep nao mede gosto.
5. **A auditoria foi finding-driven, nao varredura exaustiva certificada**: 3 achados fechados,
   2 deferidos com dono (`epub-zip-slip`, `llm-mobile`). Zip-slip real segue ABERTO de proposito
   em `ReadingManager.cs:59-60`/`FileUtility.cs:31-32` — e da phase 11, nao desta.
6. **Nenhum ganho de memoria/CPU foi MEDIDO** — a fase entregou conformidade de regra provavel
   por inspecao (D-...-2(B)); se o card te prometeu numeros de bateria, eles nao existem aqui.

## Recommendation

Phase pronta para `/jdi-ship` e PR. Os 3 achados estao fechados com rede verde e cobertura acima
do piso; as 3 rodadas de endurecimento de gate deixaram o DoD 4/5 provando o que dizem provar
(atacado por mutacao nesta review, zero regressao de gate); os warnings remanescentes sao
estruturalmente nao-fechaveis por gate textual ou pertencem a proxima phase, todos roteados com
dono. Levar ao PR a secao "Para o revisor humano" acima.

**Verdict:** APPROVED_WITH_WARNINGS

## DoD Critic (enhanced — forcado por /jdi-issue, passe final antes do ship)

O comando do item 4 mudou pela TERCEIRA vez na iter 4 (fronteira de identificador a esquerda,
`D-2026-07-30-the-method-refactor-9`), entao a aprovacao do critico da iter 3 nao o cobria. Re-ataque
completo das 5 linhas com matriz de mutacao propria (repo nunca mutado; versao OLD extraida por sed
de `7a4081a`). **Nenhuma linha oca.**

- Itens 1, 2, 3 e 5: byte-identicos desde `7a4081a` (o diff do CONTEXT.md tem hunk unico, so no item
  4). Re-checagem leve confirmou que cada um mede o artefato real, nao so exit 0 —
  `IFileUtility.cs:13` + `FileUtility.cs:42-43` + `ReadingManager.cs:53` com zero `Directory.*`
  direto; `ReadingManagerTests.cs:137-152` load-bearing (`DidNotReceive` em `ExtractAllImagesAsync`
  E `WriteFileAsync`); `TranslationManager.cs:140,185,188,217` roteando de verdade por `HtmlUtility`
  (nao e move morto); item 5 com viva == textual == 214 e `git diff 7a4081a HEAD -- src/ test/` vazio.

- Item 4: **zero regressao de gate**. Os 7 mutantes de regressao amostrados (orfao compensado,
  pattern corrompido, `IgnoreCase` removido, call comentado com `//` e com `/* */`, orfao simples,
  rename consistente) saem OLD=1/NEW=1 — nenhum caso OLD=1/NEW=0. O fix novo pega os lookalikes
  prefixados (`MyStylesheetRelRegex` 0->1, `CachedImgSrcRegex` 0->1) e tambem `_StylesheetRelRegex`
  (variante com underscore que o doer nao chegou a testar). Split por `&&`: 13 = 13 clausulas, 12
  byte-identicas, so o passe AWK difere — containment confirmado mecanicamente. Suite de falso
  positivo limpa: acesso por membro, `Regex opf = OpfTitleRegex();`, call dentro de interpolacao
  `$"{...}"`, coluna 1, parenteses extras — todos exit 0. A promessa textual de `D-...-9`
  corresponde ao entregue (foi assim que `D-...-7` caiu na iter 2, e desta vez nao cai).
  Nota nao-bloqueante: call quebrado em duas linhas (nome, depois `()`) falharia — rigidez
  PRE-existente desde `D-...-8` (o token inclui `()`), ja imposta pela clausula agregada `-eq 14`
  desde a primeira versao; nao e regressao da iter 4 e falha ALTO (fail-closed), nunca em silencio.

**Verdict:** APPROVED
