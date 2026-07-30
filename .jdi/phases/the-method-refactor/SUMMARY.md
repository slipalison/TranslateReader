# Phase 13: Refactor The Method + memoria/CPU mobile — Summary  (slug: the-method-refactor)

**Status:** complete | **Tasks:** 4/4, 0 blocked | **Base:** `a390eb9` (main, PR #10)

## Commits (1 achado = 1 commit atomico) — prefixo `refactor(the-method-refactor): `

| SHA | Subject |
|---|---|
| `508933b` | route extracted-images check through IFileUtility |
| `a5f7b44` | compile the 7 ParsingEngine regexes at build time |
| `3c5a8cf` | move HTML block handling into HtmlUtility |
| _(este)_ | `docs(...)`: aggregate evidence + gaps |

## T-1 — achado #1 (`508933b`)

`IFileUtility` ganhou `bool DirectoryHasContent(string)` (7o metodo, com `<summary>`);
`FileUtility` implementa com o predicado original verbatim; `ReadingManager:53-54` virou
`if (fileUtility.DirectoryHasContent(imagesDir)) return;`. `ReadingManager.cs:59-60` e
`WriteFileAsync` **nao tocados** (zip-slip, de `epub-zip-slip`). Testes: `FileUtilityTests`
9 -> 13 (arquivo / so-subpasta / vazio / inexistente), `ReadingManagerTests` 7 -> 8 (skip provado
com `DidNotReceive` em `ExtractAllImagesAsync` **e** `WriteFileAsync`, zero I/O). Ajuste
**MECANICO**: `.Returns(false)` explicito nos 2 testes legados de `LoadChapterContentAsync` —
assercoes intactas, nada deletado ou afrouxado. Excecao/cancelamento **N/A**: metodo sincrono,
sem `CancellationToken`.

**Mutacao — 3 rodadas, nada commitado, 2/2 previstos em cada uma:**

| Mutacao | FALHARAM |
|---|---|
| A (disjuntos): guard do Manager removido **e** `Length > 0` -> `>= 0` | `..._SkipsExtractionEntirely`, `..._ForEmptyDirectory` |
| B: `Exists(p) && GetFileSystemEntries(p)` -> `GetFiles(p)` | `..._ForMissingDirectory`, `..._OnlyASubdirectory` |
| C: `DirectoryHasContent` => `false` | `..._ContainingAFile`, `..._OnlyASubdirectory` |

Restaurado -> verde.

## T-2 — achado #3 (`a5f7b44`)

`public partial class ParsingEngine`; 7 padroes transcritos **verbatim**. Armadilha do PLAN
honrada: o `RegexOptions` que era 4o arg de `Regex.Replace` / 3o de `Match`/`IsMatch` foi para
dentro do atributo nos 7 — **os 7 com `IgnoreCase`**, L126 tambem `Singleline`.

| Linha antiga | Metodo gerado | Pattern |
|---|---|---|
| L126 Replace | `OpfTitleRegex` | `(<dc:title[^>]*>)(.*?)(</dc:title>)` |
| L196 Replace | `LinkTagRegex` | `<link\b([^>]*?)/?>` |
| L199 IsMatch | `StylesheetRelRegex` | `\brel\s*=\s*"stylesheet"` |
| L202 Match | `StylesheetHrefRegex` | `\bhref\s*=\s*"([^"]+)"` |
| L228 Replace | `ImgSrcRegex` | `(<img\b[^>]*?\bsrc\s*=\s*")([^"]+)(")` |
| L232 Replace | `SvgImageXlinkHrefRegex` | `(<image\b[^>]*?\bxlink:href\s*=\s*")([^"]+)(")` |
| L236 Replace | `SvgImageHrefRegex` | `(<image\b[^>]*?\bhref\s*=\s*")([^"]+)(")` |

`cultureName` no default nos 7 (precedente de `TranslationManager`). Zero metodo extraido, zero
logica alterada; `using System.Text.RegularExpressions` mantido (`Regex`/`Match` em uso).

**Mutacao — desvio do PLAN.** O PLAN dizia "protegido por 19 `ParsingEngineTests`". Medido,
**so 1 dos 7 padroes e discriminado sozinho**:

| Mutacao (pattern quebrado) | Falhou |
|---|---|
| `ImgSrcRegex` sozinho | **2**: `Practice_..._RewritesImagePathsToVirtualHostUrl`, `Practice_..._NaoDeveConterRefsRelativas...` |
| `StylesheetRelRegex` sozinho | **0** |
| `SvgImageHrefRegex` sozinho | **0** |
| 6 dos 7 quebrados, `ImgSrcRegex` intacto | **1**: `WardleyMaps_SvgCoverChapter_...` |

Leitura: os 2 padroes de `<image>` so mordem JUNTOS (o fixture tem `href` e `xlink:href`, um
cobre o outro); `OpfTitleRegex` + os 3 de `InlineCssLinks` nao sao cobertos por nada. Nao criei
fixture nem relaxei secao 6 — registrado em `.jdi/todos.md` (mesmo motivo tecnico da
`regression-suite` > Lacuna 4).

## T-3 — achado #2 (`3c5a8cf`)

`HtmlUtility` virou `public static partial class` (obrigatorio para hospedar `[GeneratedRegex]`)
e recebeu `ExtractParagraphs`, `ExtractTextBlocks`, `ReplaceTextBlocksInHtml`, `StripHtmlTags`
como `public static` (corpo/assinatura inalterados) + os 3 regex como `private static partial`.
`TranslationManager` perdeu as 4 definicoes, os 3 regex, o `partial` e o `using
System.Text.RegularExpressions` (orfaos), mantendo `System.Text` e `System.Security.Cryptography`;
as 4 chamadas passam por `HtmlUtility.X(...)`. Zero teste novo (D-...-4). Sem colisao com
`ExtractBodyContent`/`InjectTags`/`BuildContinuousScrollHtml`.

**Mutacao (3 rodadas):**

| Mutacao em `HtmlUtility` | Falhou |
|---|---|
| A (disjuntos): `StripHtmlTags` => `html` **e** `WebUtility.HtmlEncode` removido | **1**: `TranslateBookAsync_HtmlEncodesTranslatedText...` |
| B: `StripHtmlTags` => `html` sozinho | **0** — identidade nao e discriminada (blocos dos fixtures tem texto plano) |
| C: `StripHtmlTags` => `string.Empty` | **12** `TranslationManagerTests` |

C e a prova pedida: o MOVE esta no caminho vivo, nao e copia morta. B fica registrado como limite
honesto da rede existente.

## Gates (numeros reais, escopo de cada medicao declarado)

- `dotnet build TranslateReader.slnx -c Release` (**escopo: solucao, 3 projetos, TFMs
  windows/ios/maccatalyst/android**): `0 Erro(s)`, `64 Aviso(s)` — **0 no Core**; os 64 sao
  `MVVMTK0045` legados do app MAUI, intocado.
- `dotnet test .../TranslateReader.Tests.csproj -c Release` (**escopo: unico test project, TFM
  `net10.0`**): **201 aprovados / 2 ignorados / 203 total**, 0 falhas, ~4 s. Baseline medido nesta
  sessao com o mesmo comando: 196/2/198 (+5 = +4 `FileUtilityTests` +1 `ReadingManagerTests`). Os
  2 ignorados seguem sendo os 2 `[Fact(Skip=...)]` de `TranslationEngineTests`.
- Atributos `[Fact]`/`[Theory]` (`grep -rhoE`, **escopo: `test/TranslateReader.Tests`, 18 `.cs`**):
  192 -> **197**.
- `dotnet format --verify-no-changes` (**escopo: SOLUCAO, `core.longpaths=true`**): **11**
  violacoes WHITESPACE, todas legadas — `ThemeEngine.cs`(12,24)(14,11), `ReaderPage.xaml.cs`
  (122,103)(124,72), `HtmlInjectionTests.cs`(25,1)(42,1), `ThemeEngineTests.cs`(12,33),
  `TranslationManagerTests.cs`(528,21)(528,33)(528,61)(529,31) — a lista de `regression-suite`
  (12) menos `ReadingManager.cs(55,1)`, que caiu no hunk de T-1 e foi limpa. **Zero violacao NOVA
  nos 8 arquivos tocados** (reconfirmado com `--include` por arquivo em T-2/T-3).

## Definition of Done — 5 `Verify:` rodados, todos `exit=0`

| # | Verify (comando do CONTEXT, rodado literal) | Medido |
|---|---|---|
| 1 | contrato+impl `DirectoryHasContent`; 0 `Directory.(Exists\|GetFileSystemEntries)` no Manager | 0, chamada presente |
| 2 | `DirectoryHasContent` nos 2 test files; `ReadingManagerTests` >= 8 attrs | 8 attrs |
| 3 | 0 `private static` dos 4 nomes no Manager; **exatamente 4** `public static` em `HtmlUtility` | 0 e 4 |
| 4 | 0 `Regex.(Replace\|Match\|IsMatch)(`; `public partial class`; >= 7 `[GeneratedRegex` | 0 e 7 |
| 5 | 0 arquivo em `src/TranslateReader/`; 0 csproj BenchmarkDotNet; attrs >= 193 | 0 / 0 / 197 |

## Arquivos modificados

Core: `Contracts/Utilities/IFileUtility.cs`, `Utilities/{FileUtility,HtmlUtility}.cs`,
`Business/Managers/{ReadingManager,TranslationManager}.cs`, `Business/Engines/ParsingEngine.cs`.
Test: `{FileUtility,ReadingManager}Tests.cs`. JDI: `todos.md`, `phases/.../{PLAN,SUMMARY}.md`.
**Zero diff em `src/TranslateReader/`.**

## Desvios do PLAN

1. **T-2, protecao superestimada** (acima): a suite discrimina 1 de 7 padroes, nao "6 dos 7". Nao
   inventei escopo — MOVE verbatim + medicao em `.jdi/todos.md`, que absorve o gap de
   `UpdateOpfTitleAsync` citado pelo PLAN.
2. **T-1, limpeza de 1 violacao legada de formato** (`ReadingManager.cs(55,1)`): caiu no hunk
   editado. Baseline 12 -> 11; nao e reformatacao de legado fora de escopo.
3. **T-1, `<summary>` no metodo novo do contrato** (csharp.md secao 7 exige em `Contracts/`),
   embora os 6 legados de `IFileUtility` nao tenham — legado nao tocado (D-2).

## Fora de escopo (declarado, nao esquecido)

Zip-slip (`ReadingManager:59-60`, `WriteFileAsync`) -> `epub-zip-slip`. Seam de
`TranslationEngine`/LLamaSharp -> `llm-mobile` (D-...-6). Infra de medicao nao criada (D-...-2 (B))
— **nenhum ganho de memoria/CPU foi MEDIDO aqui**, so conformidade de regra por inspecao. Teste
novo para `InlineCssLinks`/`UpdateOpfTitleAsync` (I/O de disco, secao 6). Auditoria nao exaustiva.
