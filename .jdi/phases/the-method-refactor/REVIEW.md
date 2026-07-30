# Phase 13: Review — iter 3  (slug: the-method-refactor)

**Verdict:** APPROVED_WITH_WARNINGS

Reviewer: `jdi-reviewer-translatereader` (Fable 5, xhigh — D-7). Diff revisado: `a390eb9` (main)
ate `499194a`, branch `jdi/the-method-refactor`, 13 commits (9 de trabalho + 2 de review + context/plan).
REVIEW.md regenerado do zero (iter 3); nada herdado das iters 1-2. Toda mutacao de gate rodou em
copia no scratchpad — repo real nunca mutado.

## Gates

| Gate | Comando | Resultado real | Status |
|---|---|---|---|
| 1 Build | `dotnet restore && dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` | **0 Erro(s)**, 40 avisos (todos `MVVMTK0045`, app MAUI legado) | PASS |
| 2 Tests | `dotnet test` | **227 aprovados / 2 ignorados / 229 total, 0 falhas** — identico ao fechamento da iter 2; baselines: fase 196p/2s, D-2 167 | PASS |
| 3 Coverage | `dotnet test --collect:"XPlat Code Coverage"` + Cobertura | Linhas ALTERADAS da fase em `src/`: **46/49 = 93,88%** (>= 90%, D-6). Agregado Core (contexto, nao-gate): 82,15%. Descobertas: `ParsingEngine.cs:126` (W-1), `HtmlUtility.cs:44,46` (guardas movidas verbatim, edge legado). Arquivos NOVOS pos-`4285f25` = so testes (fora do report de producao) | PASS |
| 4 Lint | `dotnet format --verify-no-changes` (solucao, `core.longpaths=true`) | **11 WHITESPACE**, as MESMAS 11 legadas da iter 2 (ThemeEngine.cs x2, ReaderPage.xaml.cs x2, HtmlInjectionTests x2, ThemeEngineTests x1, TranslationManagerTests x4). **Zero** nos 9 arquivos tocados pela fase | WARN (legado, D-2) |
| 5 Security/Layer | bateria 5.1-5.17 (abaixo) | 5.1=0; 5.2=0; 5.3: 1 hit/Manager (interface propria); 5.6: 2 hits baseline `ParsingEngine.cs:93,115` (escrita em entry do zip, nao extracao a caminho de disco; vetor real deferido a `epub-zip-slip` por D-...-3); 5.7=0; 5.8: 10 calls, todas via `JsStr(...)`/`*Json`; unica interpolacao crua `ReaderPage.xaml.cs:474` usa constante interna (`"loadScrollContent"`/`"loadChapter"`), nao derivada de livro; 5.9=0; 5.10=0; OCE: 4 catches, `TranslationManager.cs:61-64` faz `throw;` (persiste pause e rethrow, §1 ok), 3 no app = legado; 5.11: 5+=/4-= (baseline bootstrap, zero novo); 5.12: 1 = `TranslationEngine.cs:16` (baseline conhecido); 5.13: so `obj/` gerado; 5.14=0 (`new Regex` = **0** no src inteiro); 5.15: 5 catches vazios, todos app legado; 5.16=0; 5.17: 0 mock de concreto, I/O real so nos 4 arquivos de padrao pre-existente (FileUtilityTests com temp dir e sancionado por D-...-3/PLAN T-1) | PASS (WARNs legados) |
| 6 Consistency | `git show --name-only` por commit vs PLAN | T-1 `508933b`, T-2 `a5f7b44`, T-3 `3c5a8cf`, T-4 `b9ec38a` batem 1:1 com `files_modified` do PLAN; 13/13 commits Conventional com scope `the-method-refactor` e tipos corretos (`refactor`/`test`/`docs`, nada cegamente `feat`) — D-4 | PASS |
| 7 UI Validation | — | SKIPPED (has_frontend=false, cliente MAUI nativo) | SKIPPED |
| 8 DoD | 5 comandos `Verify:` extraidos LITERALMENTE do CONTEXT.md vigente e executados | **5/5 exit 0**; 0 itens Manual (dod=auto_only). Numeros: item 2 -> `ReadingManagerTests` 8 attrs; item 4 -> 0 regex estatico / partial / 7 attrs / 14 nomes / 7 pares / 7 nomes conformes por nome; item 5 -> 0 diff app MAUI / 0 BenchmarkDotNet no repo todo / 214 attrs VIVOS (vivo == textual == 214, sem falso positivo) | PASS |

## Blockers

Nenhum.

## Warnings

- **W-1 (residuo, carregado da iter 2)** — wiring end-to-end de `InlineCssLinks`/`UpdateOpfTitleAsync`
  sobre EPUB real segue sem assercao; `ParsingEngine.cs:126` com **0 hits** (Cobertura:
  `<UpdateOpfTitleAsync>d__6` = 0% em 13 linhas; `CreateTranslatedEpubAsync` 0% em 22). Regra:
  `.claude/rules/csharp.md` §6 (proibe I/O de disco em teste novo) impede a fixture que provaria a
  EXECUCAO; identidade byte-a-byte dos patterns provada pelo DoD 4 e a semantica pelos 26 casos de
  `ParsingEngineRegexTests.cs`. Roteado em `.jdi/todos.md:151-165`. Julgamento em (g) abaixo: ACEITAVEL.
- **W-2 (limite do gate DoD 4, evidencia executada)** — 3 evasoes ADVERSARIAIS passam o `Verify:`
  novo com codigo errado: call site dentro de string literal (`ParsingEngine.cs:196` mutado para
  `if (!"StylesheetRelRegex()".Equals(attrs))` -> OLD=0 NEW=0), call site vivo so no texto dentro de
  `#if SIMBOLO_INDEFINIDO` (OLD=0 NEW=0) e call de nome lookalike PREFIXADO (`MyStylesheetRelRegex()`,
  casado por substring no AWK `index()` -> OLD=0 NEW=0). Julgamento completo em (a) abaixo: nenhuma
  tem caminho ACIDENTAL (0 `#if` no Core; nenhum dos 7 nomes reais e sufixo de outro; string com o
  nome exato da factory nao surge de refactor) — classe distinta dos blockers das iters 1-2, que eram
  slips plausiveis. WARN, nao blocker; racional registrado.
- **W-3 (precisao do claim de containment da D-8, item 5)** — a afirmacao formal "NEW `exit 0`
  implica OLD `exit 0`" e falsificada no canto bin/obj: csproj com BenchmarkDotNet dentro de
  `src/**/obj/` ou `src/**/bin/` da OLD=1 / NEW=0 (executado, S4/S5). E divergencia DELIBERADA e
  CORRETA (artefato de build nao e declaracao de pacote; o proprio dispatch a exige), e nenhuma
  protecao sobre declaracao REAL foi perdida (S1-S3: teste-csproj, `Directory.Packages.props` CPM e
  `Directory.Build.targets` todos 0->1). Mas a frase da D-8/SUMMARY "find . podado e superconjunto de
  find src" nao e literalmente verdadeira — registro para nao virar precedente de prova.
- **W-4 (workspace)** — `TestResults/` untracked e **fora do `.gitignore`** (0 matches). Verificado:
  **nenhum** path `TestResults` em nenhum dos 13 commits da fase. Fix trivial (1 linha no
  `.gitignore`), fora do escopo desta fase e desta review read-only — recomendo no proximo commit de
  housekeeping, nao bloqueia ship.
- **W-5 (residuos da contagem viva, DoD 5)** — executado: `[ Fact ]` com espacos nao conta (attr
  VIVO invisivel a medida — direcao fail-closed, subconta) e `[Fact(...)]` idem (consistente com o
  baseline, que nunca os contou); string literal com `"[Fact]"` e attr sob `#if false` CONTAM
  (sobreconta adversarial-only). Piso `-ge 193` com medida real 214 deixa folga de 21 — regressao de
  ate 21 attrs passaria o DoD 5, mas o Gate 2 (comparacao com 227 aprovados) cobre essa janela.
  Sugestao para futura phase: ratchet do piso ao fechar cada fase.
- **Legado (baseline D-2, fase nao tocou; sem acao)** — OCE engolido no boundary de UI
  (`LibraryPageModel.cs:183`, `ReaderPageModel.cs:222`, `ReaderPage.xaml.cs:308`) + `catch { }`
  (`ReaderPage.xaml.cs:326,434`); eventos 5+=/4-= no app; static mutavel unico
  (`TranslationEngine.cs:16`). Identicos ao baseline do bootstrap.

## Ceticismo dirigido (a)-(i) — evidencia propria, executada

**(a) Ataque ao `Verify:` novo do DoD 4 — SUSTENTA (com residuo W-2).**
Harness em scratchpad (`attack.sh`), comandos NEW e OLD extraidos por sed do CONTEXT.md vigente e de
`bc4f1c6`; 17 mutantes, cada linha = `OLD/NEW` exit:
pristino 0/0 (sem falso positivo); **M5 do critic** (call `:196` `StylesheetRelRegex`->`StylesheetHrefRegex`,
agregado mantem 14) **0/1 — fechado**; orfao compensado de **cada um dos 7 nomes** (OpfTitle, LinkTag,
StylesheetRel, StylesheetHref, ImgSrc, SvgImageXlinkHref, SvgImageHref — 1 token trocado no call site,
agregado sempre 14) **0/1 nos SETE**; call comentado `//` 0/1; call em `/// <summary>` 0/1 (o AWK
descarta `///` por prefixo `//`); `nameof(StylesheetRelRegex)` sem parenteses 1/1; declaracao duplicada
1/1; pattern corrompido + `IgnoreCase` removido (M1 da iter 2) 1/1 — o endurecimento da D-...-7
sobrevive intacto. Evasoes NOVAS que passaram: **E1 string literal 0/0, E2 `#if` 0/0, E5 lookalike
prefixado 0/0** (W-2). Julgamento sobre a regra do dispatch ("qualquer evasao que passe = BLOCKER"):
nao aplicada literalmente, por tres motivos registrados com transparencia total para revisao humana:
(1) o mapeamento de veredito do reviewer (`.jdi/agents/jdi-reviewer-translatereader.md` § rules) so
alcanca BLOCKED por falha de gate 1-3, check BLOCK-class do gate 5 ou Auto FAIL no gate 8 — os 5
itens do DoD dao `exit 0` no repo real e o codigo real SATISFAZ genuinamente cada criterio (conferido
por leitura direta de `ParsingEngine.cs`, nao pelo SUMMARY); (2) os blockers das iters 1-2 foram
estados errados ALCANCAVEIS POR ACIDENTE (slip de migracao nomeado como risco #1 no PLAN; lookalike
entre nomes EXISTENTES; pacote no csproj NATURAL de benchmark) — as 3 evasoes restantes exigem
construcao deliberada sem caminho acidental (0 `#if` em todo o Core; nenhum nome real e sufixo de
outro; `nameof` nao produz `()`), e um doer adversarial poderia simplesmente editar a linha `Verify:`;
(3) exigir grep a prova de adversario e criterio insatisfazivel por qualquer gate textual (AWK nunca
vai parsear C#; `#if` exige o build graph) — o fixpoint dessa politica e "nenhum DoD passa nunca",
contradizendo a propria D-8, que fixa a MEDIDA do criterio, nao promete parser. O backstop para
codigo adversarial e o PR review humano (estatuto do /jdi-issue).

**(b) Ataque ao `Verify:` novo do DoD 5 — SUSTENTA.**
Pacote: BenchmarkDotNet no csproj de TESTE 0->1 (contra-exemplo do critic, fechado);
`<PackageVersion>` em `Directory.Packages.props` (CPM) 0->1; `<PackageReference Update=` em
`Directory.Build.targets` 0->1; em `src/**/obj/` e `src/**/bin/` NEW=0 — **ignorado corretamente**
(prune funciona). Contagem viva: `[Fact]` na MESMA linha do metodo conta (215); indentado com TAB
conta (215); 25 attrs comentados `// [Fact]` -> vivo=189, **exit 1** (contra-exemplo do critic,
fechado); `[ Fact ]` com espacos NAO conta (subconta, fail-closed — W-5); string/`#if false`
sobrecontam (adversarial-only — W-5). **Falso positivo no repo real: nenhum** — 5/5 `exit 0`,
vivo == textual == 214 (214 = 192 do baseline + 5 da T-1 + 17 de `ParsingEngineRegexTests`; os 2
`[Fact(Skip=...)]` de integracao nunca entraram na medida, em nenhuma versao).

**(c) Containment clausula a clausula — CONTIDO (com a ressalva W-3).**
Item 4: OLD tem 11 clausulas (`&&`-split); **11/11 contidas LITERALMENTE** no NEW (13 clausulas =
11 antigas + AWK por-nome + inalteradas), verificado mecanicamente por comparacao de substring —
NEW `exit 0` implica OLD `exit 0`, nada afrouxado. Item 5, clausula a clausula: (1) `git diff` do
app MAUI — **identica byte a byte**; (2) busca de pacote — superconjunto em TIPOS de arquivo
(csproj+props+targets+packages.config vs so csproj) e em DIRETORIOS (repo todo vs `src`), EXCETO o
canto bin/obj/.git podado (W-3: divergencia intencional, correta, sem perda de protecao sobre
declaracao real — provado por S1-S3); (3) contagem — vivo <= textual sempre, logo NEW `-ge 193`
implica OLD `-ge 193`. Nenhuma protecao antiga sumiu para estado de codigo real.

**(d) Caminho JDI-legal da mudanca de DoD — LIMPO.**
`git diff a390eb9 HEAD -- .jdi/DECISIONS.md` = **181 insercoes, 0 delecoes** (append puro na fase
inteira); `D-2026-07-30-the-method-refactor-8` comeca na linha 579 e e a ULTIMA decisao do arquivo;
D-...-5 e D-...-7 intactas (zero delecao implica intactas). `git diff efc3e8f HEAD -- CONTEXT.md` =
**1 hunk unico** sobre os itens 4 e 5 (texto do criterio + `Verify:` + nota `Source:`); itens 1, 2 e
3 byte-identicos. As linhas novas correspondem exatamente ao que a D-8 autoriza (checagem POR NOME
com descarte de comentario; `find .` podado sobre os 4 tipos de arquivo; contagem viva; piso mantido
em 193) e os textos dos criterios so ENDURECEM ("nenhuma das 7 factories orfa", "QUALQUER
csproj/props/targets/packages.config", "VIVOS (nao comentados)") — nada afrouxado.

**(e) Producao intocada na iter 3 — CONFIRMADO.**
`git diff efc3e8f HEAD --stat -- src/ test/` = **vazio**. Fase inteira (`a390eb9..HEAD`):
exatamente 6 arquivos em `src/`, todos no Core (ParsingEngine, ReadingManager, TranslationManager,
IFileUtility, FileUtility, HtmlUtility) + 3 de teste — bate com a iter 1.

**(f) Regressao de teste — NENHUMA.**
`git diff a390eb9 HEAD -- test/` tem **0 linhas deletadas** (34+182+18 = 234 so adicoes): nenhum
assert removido, nenhuma assercao virou execucao muda, nenhum `[Fact(Skip=...)]` novo (contagem de
`[Fact(` = 2, os 2 de integracao legados). Adicoes sao load-bearing: skip do Manager provado com
`DidNotReceive` em `ExtractAllImagesAsync` E `WriteFileAsync`; os 2 `.Returns(false)` em testes
legados sao MECANICOS (default de `bool` ja e false), assercoes originais intactas; os 26 casos de
`ParsingEngineRegexTests.cs` assertam captura/replace/nao-casamento concretos por factory.

**(g) Residuo W-1 — WARNING ACEITAVEL, nao blocker.**
A unica propriedade nao provada e a EXECUCAO do wiring (`ParsingEngine.cs:126`, 0 hits) — prova-la
exige fixture EPUB com I/O de disco, vedada para teste novo por `.claude/rules/csharp.md` §6, com
recusa precedente registrada (`regression-suite` SUMMARY > Lacuna 4). O que e provavel sem violar §6
ja esta provado em dupla camada: texto byte-identico + nao-orfandade por nome (DoD 4) e semantica dos
7 patterns (26 casos). Codigo em questao e legado (pre-boundary, D-2), comportamento inalterado por
MOVE. Roteado em `.jdi/todos.md:151-165` com dono claro. Coerente com o estatuto finding-driven.

**(h) `TestResults/` — LIMPO nos commits; WARN de housekeeping (W-4).**
`git log --name-only a390eb9..HEAD | grep -i testresults` = **0**. `.gitignore` nao tem a entrada.
Nada vazou; fix de 1 linha recomendado fora desta fase (review e read-only). Nao bloqueia ship.

**(i) Estatuto finding-driven (D-...-1/-2) — HONRADO.**
Iters 2-3 tocaram somente: `ParsingEngineRegexTests.cs` (teste pareado EXIGIDO pelo critic),
`.jdi/{DECISIONS,todos}.md` e `phases/the-method-refactor/{CONTEXT,SUMMARY,LOOP,REVIEW}.md`. Zero
escopo novo, zero arquivo de producao, **zero arquivo do app MAUI em qualquer dos 13 commits**
(DoD 5 clausula 1 `exit 0` + inspecao por commit). Deferimentos (zip-slip -> `epub-zip-slip`, seam
LLamaSharp -> `llm-mobile`, infra de medicao -> todos.md) permanecem como decidido; nenhum ganho de
memoria/CPU foi declarado sem medida (D-...-2(B)).

## DoD Checklist (gate 8)

| # | Criterio | Source | Type | Status | Evidencia |
|---|---|---|---|---|---|
| 1 | `IFileUtility.DirectoryHasContent` existe; `ReadingManager` roteia por ele, zero `Directory.*` direto | CONTEXT | Auto | PASS | exit 0; `IFileUtility.cs:13` (com `<summary>`), `FileUtility.cs:42`, `ReadingManager.cs:53` |
| 2 | Gap regression-suite-5(1): teste mockado do branch "ja extraido" + caso real em FileUtilityTests | CONTEXT | Auto | PASS | exit 0; `ReadingManagerTests` 8 attrs (skip com 2x `DidNotReceive`); `FileUtilityTests` +4 casos `DirectoryHasContent` |
| 3 | 4 metodos HTML fora de `TranslationManager` (privados=0) e publicos/estaticos em `HtmlUtility` (=4) | CONTEXT | Auto | PASS | exit 0; `HtmlUtility` e `public static partial class` (linha 6) |
| 4 | 7 `[GeneratedRegex]` byte-identicos, pareados por adjacencia, nenhum orfao (por NOME, so linha viva) | CONTEXT | Auto | PASS | exit 0; 0 regex estatico / partial / 7 attrs / 14 nomes / 7 pares / 7 conformes; gate validado por 17 mutacoes (M5 e os 7 orfaos compensados agora falham) |
| 5 | Guardrail: 0 diff app MAUI; 0 BenchmarkDotNet em qualquer csproj/props/targets/packages.config; >= 193 attrs VIVOS | CONTEXT | Auto | PASS | exit 0; 0 arquivos de diff / 0 pacotes / 214 vivos (== textual) |

**Totals:** 5 itens | Auto: 5 (5 PASS, 0 FAIL) | Manual: 0 pendente
(PROJECT.md nao declara `## Definition of Done` — baseline da fase e o CONTEXT.md, dod=auto_only.)

## Recommendation

Aprovar e seguir para `/jdi-ship the-method-refactor`. Os 3 achados de producao estao corretos,
atomicos e protegidos (227p/2s, 93,88% nas linhas alteradas, zero regressao, zero linha de teste
deletada na fase inteira); os dois blockers do DoD critic da iter 2 estao comprovadamente fechados
por contra-exemplo re-executado (M5 0->1; 25 `// [Fact]` 0->1). Antes do merge, no PR review humano:
(1) avaliar W-2 — se o time quiser gate ainda mais duro, word-boundary no `index()` do AWK e
strip de string literal fecham E1/E5 a custo baixo (E2/`#if` so o build fecha); (2) 1 linha de
`.gitignore` para `TestResults/` (W-4); (3) ratchet futuro do piso 193 (W-5). Nenhum desses bloqueia.

**Verdict:** APPROVED_WITH_WARNINGS

## DoD Critic (enhanced — forcado por /jdi-issue)

Terceiro re-ataque das 5 linhas `Type=Auto`/`PASS`, com harness proprio em scratchpad (repo intocado).
Resultado: **nenhuma linha oca**. Detalhe por linha:

- Itens 1, 2 e 3: `git diff efc3e8f HEAD -- .../CONTEXT.md` mostra 1 hunk unico (so itens 4/5), entao
  as tres linhas estao byte-identicas desde a iter 2 e a re-checagem foi leve. Confirmadas com grep
  MAIS amplo que o proprio `Verify:` (`Directory\.` sem restricao de metodo em `ReadingManager.cs`
  = 0 hits) e com as assercoes load-bearing localizadas (`ReadingManagerTests.cs:149-150`,
  `HtmlUtility.cs:18,27,36,54`, roteamento real em `TranslationManager.cs:140,185,188,216-217`).

- Item 4 (`[GeneratedRegex]` por nome): **nao oco**. O slip PLAUSIVEL da familia prefixo — rename
  consistente `StylesheetRelRegex` -> `CssStylesheetRelRegex` (declaracao + call site juntos, como um
  rename de IDE produz) — sai **exit 1**, pego por DUAS clausulas independentes (o par `grep -A1 -F` e
  o `d[n]==1` do AWK). Lookalike SUFIXADO (`StylesheetRelRegex2`, familia natural de typo) tambem sai
  exit 1. As 3 evasoes que o reviewer catalogou como W-2 foram reconfirmadas como exit 0, mas nenhuma
  tem caminho acidental: a de prefixo so compila declarando o lookalike de proposito (senao o gate 1
  de build barra), a de `#if` exige introduzir a primeira diretiva de compilacao condicional de um
  Core que hoje tem zero, e a de string literal exige escrever o texto exato da invocacao dentro de
  uma string E remover a chamada real. Diferenca decisiva em relacao a queda da iter 2: a promessa de
  `D-2026-07-30-the-method-refactor-8` e QUALIFICADA ("call site VIVO — comentario nao conta como
  chamada", com o passe AWK locked como a medida), enquanto `D-...-7` prometia fechar orfaos sem
  qualificador e nao entregava. O comando entrega exatamente o escopo que a decisao promete.

- Item 5 (guardrail): **nao oco**. Contagem viva medida por 3 metodos independentes converge em 214
  (viva == textual == atributos em inicio de linha) — zero atributo comentado, zero contaminacao por
  string, zero falso negativo; os unicos nao contados sao os 2 `[Fact(Skip=...)]` que o baseline 192
  tambem nunca contou (semantica consistente). O pinhole `bin`/`obj` (W-3) foi enumerado sem prune:
  o repo tem exatamente 3 `.csproj` reais e nenhum `.props`/`.targets`/`packages.config` fora de
  `obj/`; o que a poda esconde sao apenas os `*.nuget.g.props/targets` GERADOS pelo restore, que so
  refletem csproj ja cobertos. Nao esconde caminho real.

Classificacao W-2 do reviewer: **sustentada** — as tres evasoes exigem construcao deliberada, e os
slips plausiveis da mesma familia caem com exit 1 comprovado.

**Verdict:** APPROVED
