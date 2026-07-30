# Phase 14: Zerar as issues do SonarQube e travar a regressao — Summary  (slug: sonar-zero-issues)

**Status:** complete
**Tasks:** 8/8 completas, 0 blocked
**Base:** `6132078` (main, pos PR #11) — branch `jdi/sonar-zero-issues`

## Tasks e commits (1 task = 1 commit)

Escopo `(sonar-zero-issues)` em todos os subjects, omitido na tabela.

| Task | Commit | Subject |
|---|---|---|
| T-1 | `8e6200f` | `chore: remove vendored dotnet-install.ps1` |
| T-2 | `294d316` | `refactor: modernize WebView DOM access in JS` |
| T-3 | `ddda060` | `fix: add lang and title to index.html, register waivers` |
| T-4 | `2bbdaee` | `refactor: move HtmlUtility regexes to GeneratedRegex and split InjectTags` |
| T-5 | `d84f3e9` | `fix: conform the dispose pattern and clean up test-project smells` |
| T-6 | `df53909` | `fix: use async I/O, an invariant date parser and a named parameter constant` |
| T-7 | `229fe5d` | `refactor: group the per-book translation context and simplify the rebuild loop` |
| T-8 | `6cc6200` | `ci: fail the build when the SonarCloud Quality Gate fails` |

## Destino das 113 issues (taxonomia D-...-3 — nenhuma silenciada sem registro)

| Estado | Qtd | Onde |
|---|---|---|
| FIX no codigo | 67 | 15 `HtmlUtility`, 17 JS, 2 BUG `index.html`, 4 `ParsingEngine`, 3 `TranslationManager`, 3 `BooksAccess`, 3 `ReadingStateAccess`, 2 `BookTranslationJobAccess`, 1 `SettingsAccess`, 2 `TranslationEngine`, 15 no projeto de teste |
| REMOCAO do arquivo | 41 | `dotnet-install.ps1` deletado (`powershelldre:*`) |
| EXCLUSAO multicriteria | 2 | `Web:S7926` + `css:S4667` em `sonarqube.yml` (D-...-4) |
| WAIVER `#pragma` | 3 | `SYSLIB1044` (`TextBlockRegex`) + 2x `xUnit1004` (LLamaSharp) |
| **Total** | **113** | |

## Gates (numeros reais)

- `dotnet build TranslateReader.slnx -c Release` -> **0 erros**, 64 avisos (todos `MVVMTK0045`
  pre-existentes do app MAUI; 0 aviso novo, 0 `SYSLIB*`/`CA*` no Core).
- `dotnet test -c Release` -> **256 total / 254 aprovados / 2 ignorados / 0 falhas**.
  Baseline era 229 (227/2): **+27 testes, 0 deletado, 0 afrouxado**, os 2 `Skip` seguem exatamente 2.
- Atributos `[Fact]`/`[Theory]` VIVOS (AWK descartando comentarios): **235** (baseline 214).
- Cobertura D-6 sobre linhas ALTERADAS de producao: **68/68 = 100,0%** (100% em cada um dos 8
  arquivos: `ParsingEngine`, 4x `*Access`, `TranslationEngine`, `TranslationManager`, `HtmlUtility`).
- `dotnet format --verify-no-changes`: **0 violacao em qualquer arquivo tocado pela fase**.
  DESVIO declarado: o comando de solucao sai exit 2 tambem em `main`, por 9 violacoes
  pre-existentes em `ThemeEngine.cs`, `ReaderPage.xaml.cs`, `ThemeEngineTests.cs` e
  `TranslationManagerTests.cs` — `ReaderPage.xaml.cs` e C# do app MAUI (proibido nesta fase) e os
  demais nao pertencem a task nenhuma. Unica correcao: whitespace em `HtmlInjectionTests.cs` (T-4).

## Definition of Done — 10/10 `Verify:` extraidos LITERALMENTE do CONTEXT.md vigente

Todos exit **0**:
1 `dotnet-install.ps1` removido, zero ref rastreada fora de `.jdi/` · 2 `HtmlUtility`
`[GeneratedRegex]` + pragma SYSLIB1044 + `InjectTags` decomposto · 3 JS `.dataset`/`for-of`/
`Number.parseInt`/optional chaining · 4 `HtmlInjectionTests` sem `Matches(...).Count` e >=3
`[GeneratedRegex]` · 5 `FileUtilityTests` L95 com assert + CA1816 nos 7 + CA1847 · 6
`TranslationEngine` sealed + SuppressFinalize + pragma xUnit1004 · 7 `index.html` `lang`/`<title>`,
`user-scalable=no` mantido, waivers no yml · 8 `ParsingEngine` OpenAsync, `BeginTransactionAsync`,
InvariantCulture, S1192 · 9 `TranslationManager` <=7 params e `chapters.Select(chapter =>
chapter.HRef)` · 10 `sonar.qualitygate.wait=true` no `end`.

## Matriz de mutacao (executada; working tree restaurado apos cada mutante)

**T-1 (`Verify:` novo)** — registrada em `D-2026-07-30-sonar-zero-issues-7`: antes da entrega exit
1; depois exit 0; permissao readicionada -> exit 1; `dotnet-install.ps1` restaurado -> exit 1.

**T-6 (caminho async: `CreateTranslatedEpubAsync` estava SEM cobertura; 3 casos novos):**

| Mutante | Resultado |
|---|---|
| `await using var writer` -> `var writer` (flush async perdido) | **PEGO** (1 falha) |
| `stream.SetLength(0)` removido na entry do capitulo | **PEGO** (1 falha) |
| `OpfTitleRegex().Replace(...)` removido | **PEGO** (2 falhas) |
| `CommitAsync()` removido (`BooksAccess`+`SettingsAccess`) | **PEGO** (6 falhas, suites existentes) |

**T-6 (`CultureInfo.InvariantCulture`) — PROVA NEGATIVA, registrada como o PLAN exige:**

| Mutante | Resultado |
|---|---|
| `CultureInfo.InvariantCulture` removido dos 3 caminhos de leitura | **NAO PEGO** (12/12 passam) |
| lado de ESCRITA `ToString("O")` -> `ToString()` | **PEGO** (6 falhas) |

Probe .NET 10 (`ar-SA`/`th-TH`/`fa-IR`/`he-IL`/`ja-JP`): `DateTime.Parse` do formato round-trip
`"O"` da o MESMO instante com e sem format provider, em UmAlQura, ThaiBuddhist e Persian. Conclusao
honesta: aqui `CultureInfo.InvariantCulture` e **conformidade de regra (S6580), nao correcao de
comportamento**. `CultureRoundTripTests.cs` (12 casos) fica mesmo assim: prende a metade
culture-sensivel REAL do round-trip — trocar a escrita `"O"` por `ToString()` derruba o teste.

## Desvios do PLAN (com evidencia)

1. **T-7 — ordem dos 2 helpers privados.** O `awk` do DoD 9 nao mede parametros: soma virgulas de
   uma JANELA que comeca na PRIMEIRA ocorrencia textual de `<Metodo>(` e termina na proxima linha
   terminada em `)`. Medido no arquivo original: `TranslateSingleChapterAsync` = **16 virgulas**
   para 8 parametros (a janela engolia call site + `UpdateJobProgressAsync` + declaracao). Com o
   refactor honesto (8 -> 5 params via `TranslationRun`) e ordem caller-first ainda dava 10.
   Declarar `TranslateSingleChapterAsync` ANTES do chamador poe a janela sobre a declaracao: **4**.
   Nada afrouxado — a propriedade real foi medida a parte por parser de declaracao: **5 parametros
   em cada um dos 2 metodos**. Zero contrato publico alterado; os 33 `TranslationManagerTests`
   passam sem 1 linha mudada.
2. **T-6 — testes novos com I/O de disco.** `csharp.md` §6 proibe disco em teste novo; o PLAN
   autoriza aqui ("fixture em disco ja padrao dessa classe, sem infra nova", D-...-5): provar o
   flush assincrono do zip exige um `.epub` real. Usam a fixture `TestData/` ja existente e limpam
   o temp em `finally`. Zero infra nova.
3. **T-6 — assert do `<dc:title>`.** O `.opf` da fixture usa `<dc:title id="t1" xml:lang="en">`,
   entao o assert e sobre o texto interno + ausencia do titulo original.
4. **Ajuste MECANICO em teste existente (declarado):** `HybridWebViewContractTests.cs:196-197`
   (T-2) trocou `items[i].index`/`.translated` por `item.index`/`.translated`, acompanhando
   `for` -> `for-of` em `translation.js`. Mesma forca, mesmas 2 propriedades.
5. **T-5 — `GC.SuppressFinalize(this)` como PRIMEIRA instrucao** em `TranslationEngine.Dispose()`
   (o guard `if (_disposed) return;` sai antes das 3 linhas que o `Verify:` le). Idempotente: sem
   finalizador. `ModelAccessTests` extraiu `DisposeFixtures()` para manter a ordem idiomatica.

## Fora de escopo / nao fechado

- **Quality Gate real no SonarCloud** — so existe apos push+CI; `Deferred to PR review` (D-...-6).
  Os `Verify:` provam identidade local, nao o resultado do scan remoto.
- **Confirmacao FUNCIONAL do WebView** (zoom, scroll-sync, overlay) apos a migracao de
  `translation.js`/`scroll.js`/`bridge.js` — sem harness JS no repo (D-...-5), fica no PR review.
- **Acessibilidade de `user-scalable=no`** mantido (D-...-4) — chamada de produto/UX.
- **Gap estrutural do scan:** o job Sonar nao compila `src/TranslateReader`, entao `Pages/`,
  `PageModels/`, `Platforms/`, `MauiProgram.cs` e `Utilities/*Converter.cs` seguem invisiveis ao
  analisador C#. "0 issues" vale para o que o Sonar escaneia (D-...-6, em `todos.md`). **Zero C# do
  app MAUI alterado** — diff da fase filtrado em `src/TranslateReader` retorna vazio.
- **9 violacoes pre-existentes de `dotnet format`** fora do escopo das tasks (ver Gates).

## Integridade do registro

`.jdi/DECISIONS.md` append-only preservado: o diff da fase tem **0** linhas removidas.
