# Phase 13: Refactor The Method + memoria/CPU mobile — Summary  (slug: the-method-refactor)

**Status:** complete | **Tasks:** 4/4, 0 blocked | **Base:** `a390eb9` (main, PR #10)

Commits atomicos, scope `the-method-refactor`: `508933b` T-1 `refactor` route via IFileUtility ·
`a5f7b44` T-2 `refactor` 7 regexes -> `[GeneratedRegex]` · `3c5a8cf` T-3 `refactor` move para
HtmlUtility · `b9ec38a` T-4 `docs` · `3f68deb` iter 2 `test` regex lock · `3010268` iter 2 `docs`
DoD 4 endurecido.

## T-1 — achado #1 (`508933b`)

`IFileUtility` ganhou `bool DirectoryHasContent(string)` (7o metodo, com `<summary>`);
`FileUtility` implementa o predicado original verbatim; `ReadingManager:53-54` virou
`if (fileUtility.DirectoryHasContent(imagesDir)) return;`. Linhas 59-60 e `WriteFileAsync` **nao
tocadas** (zip-slip, de `epub-zip-slip`). `FileUtilityTests` 9->13, `ReadingManagerTests` 7->8
(skip provado com `DidNotReceive` em `ExtractAllImagesAsync` **e** `WriteFileAsync`, zero I/O).
Ajuste **MECANICO**: `.Returns(false)` nos 2 testes legados de `LoadChapterContentAsync` —
assercoes intactas, nada deletado. Excecao/cancelamento **N/A** (metodo sincrono).

**Mutacao (3 rodadas, nada commitado, 2/2 previstos em cada):** (A) guard do Manager removido
**e** `Length > 0`->`>= 0` -> falham `..._SkipsExtractionEntirely` + `..._ForEmptyDirectory`;
(B) `Exists && GetFileSystemEntries`->`GetFiles` -> `..._ForMissingDirectory` +
`..._OnlyASubdirectory`; (C) `DirectoryHasContent => false` -> `..._ContainingAFile` +
`..._OnlyASubdirectory`. Restaurado -> verde.

## T-2 — achado #3 (`a5f7b44`)

`public partial class ParsingEngine`; os 7 padroes transcritos **verbatim**, com o `RegexOptions`
que era 4o arg de `Replace` / 3o de `Match`/`IsMatch` movido para dentro do atributo:
`OpfTitleRegex` (era L126, `IgnoreCase|Singleline`) e `LinkTag`/`StylesheetRel`/`StylesheetHref`/
`ImgSrc`/`SvgImageXlinkHref`/`SvgImageHref` (L196/199/202/228/232/236, so `IgnoreCase`). Os
patterns literais completos vivem agora no `Verify:` do DoD 4 e em `ParsingEngineRegexTests`.
`cultureName` default nos 7. Zero metodo extraido, zero logica alterada.

**Mutacao — desvio do PLAN** ("protegido por 19 `ParsingEngineTests`"): medido, so **1 dos 7** era
discriminado sozinho — `ImgSrcRegex` -> 2 falhas; `StylesheetRelRegex` -> **0**;
`SvgImageHrefRegex` -> **0**; 6 quebrados com `ImgSrcRegex` intacto -> 1 falha. Os 2 de `<image>`
so mordiam JUNTOS; `OpfTitleRegex` + os 3 de `InlineCssLinks` nao tinham cobertura nenhuma.
**Fechado na iter 2.**

## T-3 — achado #2 (`3c5a8cf`)

`HtmlUtility` virou `public static partial class` e recebeu `ExtractParagraphs`,
`ExtractTextBlocks`, `ReplaceTextBlocksInHtml`, `StripHtmlTags` como `public static` (corpo e
assinatura inalterados) + os 3 regex como `private static partial`. `TranslationManager` perdeu as
4 definicoes, os 3 regex, o `partial` e o `using System.Text.RegularExpressions` (orfaos); as 4
chamadas passam por `HtmlUtility.X(...)`. Zero teste novo (D-...-4).

**Mutacao (3 rodadas):** (A) `StripHtmlTags => html` **e** `WebUtility.HtmlEncode` removido -> 1
falha (`TranslateBookAsync_HtmlEncodesTranslatedText...`); (B) `StripHtmlTags => html` sozinho ->
**0** (blocos dos fixtures tem texto plano — limite honesto da rede); (C) `=> string.Empty` ->
**12** falhas em `TranslationManagerTests`: o MOVE esta no caminho vivo, nao e copia morta.

## Iter 2 — fix do blocker do DoD critic

**Blocker:** o `Verify:` do DoD 4 contava `[GeneratedRegex` (`-ge 7`) e nunca checava QUAIS
patterns/options. O critico corrompeu `StylesheetRelRegex` (`stylesheet`->`stylsheet` + remocao de
`RegexOptions.IgnoreCase`) e o comando saiu `exit 0`; a rede de 203 testes tambem nao acusava
(medido em T-2). A conformidade real so era provada por inspecao manual — prova que nao sobrevive
a proxima phase. Entreguei **as duas** opcoes do dispatch, que cobrem propriedades distintas: o
teste prova a SEMANTICA de casamento, o `Verify:` prova que o TEXTO que a gera nao mudou.

**Opcao 1 (preferida) — `test/TranslateReader.Tests/ParsingEngineRegexTests.cs`, 26 casos.** As
factories `[GeneratedRegex]` sao alcancadas por reflection (`BindingFlags.NonPublic | Static`) e
cada um dos 7 padroes recebe assercao de match, no-match, grupos capturados e case folding.
Justificativa da rota: os metodos sao privados e toda API publica de `ParsingEngine` recebe
`filePath`, entao cobrir `InlineCssLinks`/`UpdateOpfTitleAsync` por fora exigiria I/O de disco,
proibido em teste novo por `.claude/rules/csharp.md` §6. Abrir API de producao so para teste
vazaria detalhe do Engine no contrato (The Method) e seria abstracao especulativa (YAGNI);
reflection mantem **zero diff em producao** e ainda assim asserta comportamento, nao sintaxe. Sem
I/O, sem mock de concreto, 0 warning de analyzer (`Assert.Matches/DoesNotMatch`).

**Opcao 2 — `Verify:` do DoD 4 endurecido, via `D-2026-07-30-the-method-refactor-7`** (append-only;
D-...-5 segue intacta no QUE fazer — mudou so COMO o DoD prova). O comando novo CONTEM o antigo e
acrescenta: `-eq 7` exato de `[GeneratedRegex`; `-eq 14` linhas dos 7 nomes (7 declaracoes + 7 call
sites — pega regex orfao); e, para cada um dos 7, a linha de atributo conferida por literal
byte-a-byte (`grep -F`, pattern **e** options) ligada por adjacencia (`grep -A1`) a assinatura
`partial Regex <Nome>()` — o que tambem pega troca de patterns entre metodos.

**Prova por mutacao.** *Gate textual* (5 mutacoes em copia no scratchpad, repo intocado, comando
extraido LITERALMENTE do CONTEXT.md): contra-exemplo do critico / so pattern / so `IgnoreCase` /
`src`->`scr` em `ImgSrcRegex` / `IgnoreCase` fora de `ImgSrcRegex` — **antigo `exit 0` nas 5, novo
`exit 1` nas 5**. *Rede de testes* (4 rodadas mutando o arquivo real, `git checkout` a cada uma):
7 patterns corrompidos -> **16 falhas** (13 do arquivo novo, 1+ por regex, + 3 dos
`ParsingEngineTests` legados); `IgnoreCase` removido dos 7 -> **7 falhas, exatamente 1 por regex**
(zero dos 203 testes antigos acusa); `Singleline` fora de `OpfTitleRegex` -> **1**; contra-exemplo
exato do critico -> **2** (antes: 0). Honestidade: a 1a mutacao de `OpfTitleRegex`
(`<dc:title`->`<dc:titl`) era **equivalente** (`[^>]*` absorve a letra) e nao derrubou nada;
refeita em `</dc:title>`->`</dc:titel>` -> 5 falhas.

## Gates (numeros reais)

- Build `TranslateReader.slnx -c Release` (4 TFMs): **0 Erro(s)**, 64 avisos — **0 no Core**
  (todos `MVVMTK0045`, app MAUI intocado).
- `dotnet test -c Release` (`net10.0`): **227p / 2s / 229t**, 0 falhas. Iter 1 fechou 201/2/203;
  baseline da fase 196/2/198; D-2 167. Zero teste deletado ou afrouxado.
- Attrs `[Fact]`/`[Theory]`: 192 -> 197 -> **214**. `dotnet format --verify-no-changes` (solucao):
  **11** WHITESPACE, **as mesmas 11 legadas da iter 1**, zero no arquivo novo.
- Cobertura: iter 2 nao alterou linha de producao -> agregado alterado segue **93,9%** (piso D-6).
- **DoD 5/5 `exit=0`**, comandos extraidos do CONTEXT.md e rodados literais: (1) 0 e chamada
  presente; (2) 8 attrs; (3) 0 e 4; (4) **endurecido** — 0 / 7 / 14 / 7 pares
  atributo<->assinatura; (5) 0 / 0 / 214 attrs.

## Arquivos, desvios, limites

Iter 2: **novo** `test/.../ParsingEngineRegexTests.cs` + `.jdi/{DECISIONS,todos}.md` +
`phases/.../{CONTEXT,SUMMARY}.md` — **zero diff em `src/`**. Iter 1: 5 arquivos do Core + 2 de
teste. **Zero diff em `src/TranslateReader/` na fase inteira.** Desvios: (1) protecao de T-2 superestimada pelo PLAN, medida e fechada; (2) 1 violacao legada de
formato caiu no hunk de T-1 (12->11); (3) `<summary>` no metodo novo do contrato (§7), que os 6
legados nao tem (D-2). **Residuo (W-1 parcial):** o wiring end-to-end de `InlineCssLinks`/
`UpdateOpfTitleAsync` sobre EPUB real segue sem assercao (`ParsingEngine.cs:126`, 0 hits) — exige
fixture com I/O (§6); o que morreu foi "pattern/options corrompidos passam em silencio". Fora de
escopo: zip-slip -> `epub-zip-slip`; seam LLamaSharp -> `llm-mobile`; infra de medicao nao criada
(D-...-2 (B)) — **nenhum ganho de memoria/CPU foi MEDIDO**, so conformidade de regra; auditoria do
Core nao exaustiva.
