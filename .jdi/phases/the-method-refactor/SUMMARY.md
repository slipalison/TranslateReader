# Phase 13: Refactor The Method + memoria/CPU mobile — Summary  (slug: the-method-refactor)

**Status:** complete | **Tasks:** 4/4, 0 blocked | **Base:** `a390eb9` (main, PR #10)

Commits atomicos, scope `the-method-refactor`: `508933b` T-1 `refactor` route via IFileUtility ·
`a5f7b44` T-2 `refactor` 7 regexes -> `[GeneratedRegex]` · `3c5a8cf` T-3 `refactor` move para
HtmlUtility · `b9ec38a` T-4 `docs` · iter 2 `3f68deb` `test` + `3010268` `docs` · iter 3 `446ee63`
`docs` DoD 4+5 medidos por item.

## T-1 — achado #1 (`508933b`)

`IFileUtility` ganhou `bool DirectoryHasContent(string)` (7o metodo, com `<summary>`);
`FileUtility` implementa o predicado original verbatim; `ReadingManager:53-54` roteia por ele.
Linhas 59-60 e `WriteFileAsync` **nao tocadas** (zip-slip, de `epub-zip-slip`). `FileUtilityTests`
9->13, `ReadingManagerTests` 7->8 (skip provado com `DidNotReceive` em `ExtractAllImagesAsync` **e**
`WriteFileAsync`, zero I/O). Ajuste MECANICO: `.Returns(false)` em 2 testes legados — assercoes
intactas, nada deletado.
**Mutacao (3 rodadas, 2/2 previstos em cada):** (A) guard removido **e** `Length > 0`->`>= 0`;
(B) `Exists && GetFileSystemEntries`->`GetFiles`; (C) `DirectoryHasContent => false`.

## T-2 — achado #3 (`a5f7b44`)

`public partial class ParsingEngine`; os 7 padroes transcritos **verbatim**, com o `RegexOptions`
(4o arg de `Replace`, 3o de `Match`/`IsMatch`) movido para dentro do atributo: `OpfTitleRegex`
(era L126, `IgnoreCase|Singleline`) e `LinkTag`/`StylesheetRel`/`StylesheetHref`/`ImgSrc`/
`SvgImageXlinkHref`/`SvgImageHref` (L196/199/202/228/232/236, so `IgnoreCase`). Zero metodo
extraido, zero logica alterada.
**Mutacao — desvio do PLAN** ("protegido por 19 `ParsingEngineTests`"): so **1 dos 7** era
discriminado sozinho — `ImgSrcRegex` -> 2 falhas; `StylesheetRelRegex` e `SvgImageHrefRegex` ->
**0**; os 2 de `<image>` so mordiam juntos; `OpfTitleRegex` + os 3 de `InlineCssLinks` sem
cobertura nenhuma. **Fechado na iter 2.**

## T-3 — achado #2 (`3c5a8cf`)

`HtmlUtility` virou `public static partial class` e recebeu `ExtractParagraphs`,
`ExtractTextBlocks`, `ReplaceTextBlocksInHtml`, `StripHtmlTags` como `public static` (corpo e
assinatura inalterados) + os 3 regex como `private static partial`. `TranslationManager` perdeu as
4 definicoes, os 3 regex, o `partial` e o `using` orfao. Zero teste novo (D-...-4).
**Mutacao (3 rodadas):** (A) `StripHtmlTags => html` **e** `WebUtility.HtmlEncode` removido -> 1
falha; (B) `StripHtmlTags => html` sozinho -> **0** (limite honesto: blocos dos fixtures sao texto
plano); (C) `=> string.Empty` -> **12** falhas — o MOVE esta no caminho vivo, nao e copia morta.

## Iter 2 — DoD 4 oco (1o blocker do critic)

O `Verify:` contava `[GeneratedRegex` (`-ge 7`) e nunca checava QUAIS patterns/options: o critico
corrompeu `StylesheetRelRegex` (`stylesheet`->`stylsheet` + `IgnoreCase` fora) e saiu `exit 0`; a
rede tambem nao acusava (medido em T-2). Entreguei as duas metades. **(1)**
`ParsingEngineRegexTests.cs`, 26 casos sobre as factories privadas via reflection (a API publica
pede `filePath` = I/O, vedado por §6; abrir a API so para teste vazaria detalhe no contrato) —
**zero diff em producao**. **(2)** `Verify:` endurecido via **D-...-7** (append-only): `-eq 7`
exato, `-eq 14` linhas dos 7 nomes, e cada linha de atributo conferida byte-a-byte (`grep -F`)
ligada por adjacencia (`grep -A1`) a sua assinatura. **Prova:** 5 mutacoes em copia — antigo
`exit 0` nas 5, novo `exit 1` nas 5. Rede: 7 patterns corrompidos -> **16** falhas; `IgnoreCase`
fora dos 7 -> **7** (1 por regex, zero dos 203 antigos); `Singleline` fora -> **1**;
contra-exemplo do critico -> **2** (antes 0). Honestidade: a 1a mutacao de `OpfTitleRegex` era
equivalente (`[^>]*` absorve a letra) e nao derrubou nada; refeita -> 5 falhas.

## Iter 3 — fix dos 2 blockers do DoD critic

**Zero linha de producao** (o codigo estava certo; o gate e que nao provava). Entregue **D-...-8**
(append-only, supersede so os `Verify:` dos itens 4 e 5 — D-...-5 e D-...-7 **intactas**) + as 2
linhas do CONTEXT.md; itens 1, 2 e 3 (solidos pelo critico) byte-identicos.

**Blocker 1 (item 4) — orfao COMPENSADO passava:** trocar 1 token no call site
`ParsingEngine.cs:196` (`StylesheetRelRegex`->`StylesheetHrefRegex`, lookalike) deixa o regex
declarado e nunca chamado MANTENDO o agregado `-eq 14` -> `exit 0`, enquanto D-...-7 promete fechar
"declarado e nunca chamado" sem qualificador. Fix: clausula NOVA (nada removido — `-eq 14` e os 7
pares seguem) exigindo, POR NOME, `declaracoes == 1` **e** `call sites >= 1`, contando so linha
VIVA (AWK descarta `//` e `/* */`, com estado entre linhas); nomes conformes tem de dar 7.

**Blocker 2 (item 5) — 2 proxies errados:** (a) o criterio diz "nenhum pacote BenchmarkDotNet" mas
o comando rodava `find src`, entao pacote no csproj de TESTE saia `exit 0` -> a busca passa a
cobrir `*.csproj`/`*.props`/`*.targets`/`packages.config` de TODO o repo (`bin`/`obj`/`.git`
podados), sem depender de `Directory.*.props` existir hoje; (b) `grep -rhoE` contava TEXTO, entao
25 `[Fact]` comentados mantinham 214 com 189 ativos -> mesma varredura AWK, so atributo VIVO. Piso
segue `-ge 193` (o criterio locka o baseline 192: corrigi a MEDIDA, nao o limiar).

**Containment:** o item 4 contem o comando anterior literalmente; no item 5 `find .` cobre
`find src` e vivo <= texto, logo NEW `exit 0` implica OLD `exit 0` — nada afrouxado.

**Prova por mutacao** (clone em scratchpad, repo real nunca mutado; OLD = comando extraido por sed
do CONTEXT.md em `bc4f1c6`). Item 4: pristino 0/0; M5 do critico **0->1**; os **7** orfaos
compensados (1 por nome, agregado sempre 14) **0->1 nos SETE**; call site comentado com `// ` 0->1;
call site dentro de `/* */` 0->1; orfao simples (linha deletada) 1/1; M1/M3/M4 da iter 2 1/1 (o
endurecimento de D-...-7 sobrevive). Item 5: BenchmarkDotNet no csproj de TESTE **0->1**, em
`Directory.Build.props` 0->1, em `Directory.Packages.props` (CPM) 0->1, no csproj de `src` 1/1; 25
attrs `// [Fact]` **0->1** (texto 214, vivo 189); 25 em `/* */` de 1 linha 0->1; 25 em bloco
multi-linha 0->1; 25 attrs DELETADOS 1/1. **Sem falso positivo:** repo real sem mutacao -> `exit 0`
nos dois, vivo == texto (214 = 214).

## Gates (numeros reais)

- Build `TranslateReader.slnx -c Release` (4 TFMs): **0 Erro(s)**, 64 avisos — **0 no Core**
  (todos `MVVMTK0045`, app MAUI intocado).
- `dotnet test -c Release`: **227p / 2s / 229t**, 0 falhas. Iter 1 fechou 201/2/203; baseline da
  fase 196/2/198; D-2 167. Zero teste deletado ou afrouxado.
- Attrs `[Fact]`/`[Theory]`: 192 -> 197 -> **214**, todos VIVOS (medida nova = antiga em codigo
  limpo). `dotnet format --verify-no-changes`: **11** WHITESPACE, as mesmas 11 legadas.
- Cobertura: iters 2-3 nao alteraram producao -> agregado alterado segue **93,9%** (piso D-6).
- **DoD 5/5 `exit 0`**, os 5 comandos extraidos LITERALMENTE do CONTEXT.md vigente e eval-ados:
  (1) 0 + chamada presente; (2) 8 attrs; (3) 0 e 4; (4) 0 / partial / 7 / 14 / 7 pares
  atributo<->assinatura / **7 nomes com call site VIVO**; (5) 0 arquivo de diff no app MAUI /
  **0 BenchmarkDotNet em todo o repo** / **214 attrs vivos**.

## Arquivos, desvios, limites

Iter 3: `.jdi/DECISIONS.md` + `phases/.../{CONTEXT,SUMMARY}.md`. Iter 2:
`ParsingEngineRegexTests.cs` (novo) + `.jdi/*`. Iter 1: 5 arquivos do Core + 2 de teste. **Zero
diff em `src/` nas iters 2-3; zero diff em `src/TranslateReader/` (app MAUI) na fase inteira.**
Desvios: (1) protecao de T-2 superestimada pelo PLAN, medida e fechada; (2) 1 violacao legada de
formato caiu no hunk de T-1 (12->11); (3) `<summary>` no metodo novo do contrato (§7), que os 6
legados nao tem (D-2). **Residuo (W-1):** o wiring end-to-end de `InlineCssLinks`/
`UpdateOpfTitleAsync` sobre EPUB real segue sem assercao (`ParsingEngine.cs:126`, 0 hits) — exige
fixture com I/O (§6), roteado em `.jdi/todos.md`: o gate ja pega orfanamento e comentario, mas nao
prova EXECUCAO. Fora de escopo: zip-slip, seam LLamaSharp e infra de medicao (D-...-2(B)) —
**nenhum ganho de memoria/CPU foi MEDIDO**, so conformidade de regra; auditoria nao exaustiva.
