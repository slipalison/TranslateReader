# Phase 13: Refactor The Method + memoria/CPU mobile — Summary  (slug: the-method-refactor)

**Status:** complete | **Tasks:** 4/4, 0 blocked | **Base:** `a390eb9` (main, PR #10)
Commits atomicos, scope `the-method-refactor`: `508933b` T-1 · `a5f7b44` T-2 · `3c5a8cf` T-3 ·
`b9ec38a` T-4 · iter 2 `3f68deb`+`3010268` · iter 3 `446ee63`+`499194a` · iter 4 `dd0e97d`+
`efa0605`+`1061e5c`.

## T-1..T-3 — os 3 achados (iter 1; 1 achado = 1 commit)

**T-1** `IFileUtility.DirectoryHasContent(string)` novo (7o metodo, com `<summary>`), predicado
verbatim em `FileUtility`, `ReadingManager:53-54` roteando por ele; L59-60 e `WriteFileAsync` **nao
tocadas** (zip-slip). `FileUtilityTests` 9->13, `ReadingManagerTests` 7->8 (skip provado com
`DidNotReceive` em `ExtractAllImagesAsync` **e** `WriteFileAsync`, zero I/O); `.Returns(false)` em
2 legados e MECANICO, nada deletado. **Mutacao: 3 rodadas, 2/2 previstos.**

**T-2** `public partial class ParsingEngine`, os 7 padroes **verbatim** com `RegexOptions` dentro do
atributo (`OpfTitleRegex` era L126, `IgnoreCase|Singleline`; os outros 6 so `IgnoreCase`). Zero
metodo extraido. **Mutacao — desvio do PLAN** (dizia "protegido por 19 testes"): so **1 dos
7** mordia sozinho (`ImgSrcRegex`); os 2 de `<image>` so juntos; `OpfTitleRegex` + os 3 de
`InlineCssLinks` sem cobertura. **Fechado na iter 2.**

**T-3** `HtmlUtility` virou `public static partial class` e recebeu os 4 metodos HTML como
`public static` (corpo/assinatura inalterados) + os 3 regex `private static partial`;
`TranslationManager` perdeu as 4 definicoes, os 3 regex, o `partial` e o `using` orfao. Zero teste
novo. **Mutacao:** `=> string.Empty` -> **12** falhas (o MOVE esta no caminho vivo); mas
`StripHtmlTags => html` sozinho -> **0** (limite honesto: fixtures sao texto plano).

## Iters 2-3 — DoD 4 e 5 eram ocos (blockers do critic). **Zero linha de producao**

**Iter 2 (D-...-7):** o `Verify:` contava `[GeneratedRegex` sem checar QUAIS patterns — o critic
corrompeu `StylesheetRelRegex` (`stylsheet` + sem `IgnoreCase`) e saiu `exit 0`; a rede nao
acusava. Duas metades: `ParsingEngineRegexTests.cs` (26 casos nas factories privadas por reflection
— a API publica pede `filePath` = I/O, vedado por §6; **zero diff em producao**) e `Verify:` com
`-eq 7`, `-eq 14` e cada atributo byte-a-byte (`grep -F`) ligado por adjacencia (`grep -A1`) a sua
assinatura. Prova: 5 mutacoes, antigo `exit 0` / novo `exit 1` nas 5. Rede: 7 patterns
corrompidos -> **16** falhas; `IgnoreCase` fora -> **7**; `Singleline` fora -> **1**.

**Iter 3 (D-...-8):** (1) orfao COMPENSADO passava (1 token trocado no call site `:196` mantem o
agregado `-eq 14`) -> clausula NOVA por NOME (`declaracoes==1` **e** `call sites>=1`, so linha
VIVA). (2) `find src` deixava BenchmarkDotNet no csproj de TESTE passar e `grep -rhoE` contava
TEXTO (25 `[Fact]` comentados = 214 medidos, 189 ativos) -> varredura do repo todo sobre
csproj/props/targets/packages.config e contagem so de atributo VIVO; piso segue `-ge 193` (corrige
a MEDIDA, nao o limiar). Mutacao: M5 0->1; os **7** orfaos compensados 0->1 nos SETE; call
comentado e em `/* */` 0->1; BenchmarkDotNet em teste/CPM/props 0->1; 25 attrs comentados 0->1;
os ja pegos 1/1; repo real 0/0.

## Iter 4 — rodada de warnings (`/jdi-issue`)

Loop ja convergido na iter 3; rodada para tentar limpar os 5 warnings. **Zero linha de producao,
zero teste tocado.** 2 fechados, 1 em parte, 2 nao fechados com motivo.

**W-4 `TestResults/` — FECHADO** (`dd0e97d`). Conferido antes: 0 entrada equivalente, 0 arquivo
`TestResults` rastreado. `**/TestResults/` na secao "Build artifacts .NET",
no estilo de `**/bin/`; `git check-ignore -v` confirma raiz **e** aninhado.

**W-2/E5 (lookalike PREFIXADO) — FECHADO** (`efa0605`, **D-...-9** append-only, supersede so o
`Verify:` do item 4; D-...-5/-7/-8 intactas). O AWK casava o nome por SUBSTRING (`index()`), entao
`MyStylesheetRelRegex()` no call site mantinha `StylesheetRelRegex` "chamado" com o agregado 14
intacto -> `exit 0`. Agora exige FRONTEIRA de identificador a esquerda. **Containment:** 12/13
clausulas byte-identicas (`&&`-split); token e subconjunto de substring, e a contagem de DECLARACAO
e identica (o literal `"partial Regex "` ja embute a fronteira; medido: pristino `d=1/c=1` nos 7
nomes nas duas versoes, declaracao duplicada `d=2` nas duas) -> `k_novo <= k_velho`, logo NEW
`exit 0` implica OLD `exit 0`. **Mutacao (25 mutantes em scratchpad, repo nunca mutado; OLD por sed
de `7a4081a`):** alvo novo `MyStylesheetRelRegex()` **OLD 0 / NEW 1** e
`CachedImgSrcRegex()` **OLD 0 / NEW 1** — nesse mutante `-eq 14` ainda le 14 e `[GeneratedRegex`
ainda le 7, quem pega e so o AWK novo. **Zero regressao:** os 17 ja pegos seguem **1/1** (7 orfaos
compensados, `//` `/* */` `///`, orfao simples, pattern/options corrompidos, rename consistente,
`nameof`, declaracao duplicada, lookalike SUFIXADO). **Zero falso positivo:** pristino
e as 3 formas legitimas de call site (membro, coluna 1, TAB) 0/0.

**W-2/E1 (string literal) e E2 (`#if` indefinido) — NAO FECHADAS.** Exigem parsear C# e o build
graph, fora do alcance de AWK/grep; meia-solucao heuristica introduz falso positivo em codigo
legitimo, a falha cara num gate. Nenhuma tem caminho ACIDENTAL (Core tem zero `#if`; a string
exige o texto exato **e** remover a chamada real). Backstop: PR review humano.

**W-3 frase de containment da D-...-8 — FECHADO** (`efa0605`, nota append-only na D-...-9; D-...-8
**nao** reescrita). **CORRECAO:** "`find .` podado e superconjunto de `find src`" e FALSA no canto
`bin`/`obj` (csproj com BenchmarkDotNet em `src/**/obj/` da OLD 1 / NEW 0). A divergencia e
deliberada e correta; errada estava a PALAVRA. Correto: "superconjunto sobre todo arquivo de
declaracao REAL, com `bin`/`obj`/`.git` excluidos". Comando e medida inalterados.

**W-5 contagem viva — NAO FECHADO; ratchet roteado** (`1061e5c`). `[ Fact ]` com espacos nao conta:
FAIL-CLOSED (subconta, so derruba o gate) e identico ao baseline 192 — "corrigir" AFROUXA a medida.
`"[Fact]"` em string literal: mesma classe de E1. Ratchet do piso 193 -> 214 nao e correcao de
MEDIDA e sim mudanca de CRITERIO; apertar o proprio criterio no fim da corrida, sabendo que passa,
e movimento de trave — e a folga de 21 attrs ja e coberta pelo Gate 2 (227/229). Roteado em
`.jdi/todos.md`.

**W-1 wiring end-to-end — NAO FECHADO, por regra (esperado).** Exige fixture EPUB com I/O de disco,
vedado a teste NOVO por §6; ja julgado ACEITAVEL pelo reviewer e pelo critic. Roteamento
**conferido**: `[TESTE] ParsingEngine` em `.jdi/todos.md:146-167`, residuo em 164-167 com candidato
de dono (phase de integracao, ou API por stream). A REVIEW cita `151-165`, faixa anterior a
appends; conteudo la e correto.

## Gates (numeros reais, re-rodados na iter 4)

- Build `TranslateReader.slnx -c Release` (4 TFMs): **0 Erro(s)**, 64 avisos, **0 no Core**.
- `dotnet test -c Release`: **227p / 2s / 229t**, 0 falhas — identico as iters 2-3 (iter 1
  201/2/203; baseline 196/2/198; D-2 167). Zero teste deletado ou afrouxado.
- Attrs `[Fact]`/`[Theory]`: 192 -> 197 -> **214**, todos VIVOS (vivo == textual).
  `dotnet format --verify-no-changes`: **11** WHITESPACE, as mesmas 11 legadas.
- Cobertura: iters 2-4 nao alteraram producao -> linhas alteradas seguem **93,88%** (piso D-6 90%).
- **DoD 5/5 `exit 0`**, comandos extraidos LITERALMENTE do CONTEXT.md: (1) 0 + chamada; (2) 8
  attrs; (3) 0 e 4; (4) 0 / partial / 7 / 14 / **7 nomes por TOKEN EXATO**; (5) 0 diff no app MAUI
  / 0 BenchmarkDotNet / 214 attrs vivos.

## Arquivos, desvios, limites

Iter 1: 5 do Core + 2 de teste. Iters 2-4: so `.jdi/*` + `ParsingEngineRegexTests.cs`
(novo, iter 2) + `.gitignore` (iter 4). **Zero diff em `src/` nas iters 2-4; zero diff em
`src/TranslateReader/` na fase inteira.** Desvios: (1) protecao de T-2 superestimada pelo PLAN,
medida e fechada; (2) 1 violacao legada de formato caiu no hunk de T-1 (12->11); (3) `<summary>` no
metodo novo do contrato (§7), que os 6 legados nao tem (D-2). Fora de escopo: zip-slip, seam
LLamaSharp e infra de medicao (D-...-2(B)) — **nenhum ganho de memoria/CPU foi MEDIDO**, so
conformidade de regra; auditoria nao exaustiva.
