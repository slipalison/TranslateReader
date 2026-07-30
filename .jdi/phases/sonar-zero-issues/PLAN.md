# Phase 14: Zerar as issues do SonarQube e travar a regressao — Plan  (slug: sonar-zero-issues)

## Goal
Resolver as 113 issues abertas do SonarQube em `main` e instalar mecanismo anti-recorrencia —
cada issue termina em fix, exclusao ou waiver auditavel, nunca silenciada sem registro.

## Locked decisions (CONTEXT.md)
D-...-1 remover `dotnet-install.ps1` | D-...-2 `sonar.qualitygate.wait=true` | D-...-3 taxonomia
FIX/EXCLUSAO(multicriteria)/WAIVER(`#pragma`) | D-...-4 `user-scalable=no` MANTIDO + waivers
`Web:S7926`/`css:S4667` | D-...-5 esta fase E a "phase explicita" da D-2, JS sem harness |
D-...-6 `Verify:` provam identidade LOCAL, Quality Gate remoto em Deferred to PR review.
D-6 global: 90% sobre codigo ALTERADO — toda task que mexe em `.cs` de producao nomeia abaixo o
teste que cobre a linha alterada.

## Defeito conhecido no DoD (tratado por T-1, nao ignorado)
O `Verify:` do item 1 usa `grep -rl 'dotnet-install\.ps1' --exclude-dir=.git .` e e
auto-derrotante: bate em `.jdi/DECISIONS.md` e no proprio `CONTEXT.md` (que CITAM o nome por
design — sao registro de auditoria) e em `.idea/.idea.TranslateReader/.idea/workspace.xml`,
untracked/ignorado (estado local de IDE, nao removivel pela phase). Medido:
`git grep -l 'dotnet-install\.ps1' -- .` fora de `.jdi/` retorna exatamente
`.claude/settings.local.json` (TRACKED, permissao stale) e `dotnet-install.ps1`. A propriedade
REAL a provar e **"o arquivo nao existe mais E nenhum arquivo RASTREADO de codigo/config o
referencia"**, nao "nenhum byte do repo menciona a string". Correcao pelo caminho append-only ja
estabelecido: decisao NOVA `D-...-7` supersedendo, depois a linha do DoD. NUNCA reescrever D-...-1.

## Serializacao declarada (nao ha paralelismo a fingir)
`.github/workflows/sonarqube.yml` e tocado por **T-3** (args do `begin`: multicriteria) e **T-8**
(arg do `end`: `qualitygate.wait`). Estao em waves diferentes (1 e 4) de proposito. Nenhuma outra
task toca esse arquivo. Ordem geral por risco crescente: wave 1 remocao/config/asset (zero `.cs`
de producao), wave 2 utility + hygiene de teste, wave 3 producao com risco de comportamento REAL,
wave 4 ativa o gate bloqueante.

## Tasks

### Wave 1 — remocao e configuracao (paralelo, arquivos disjuntos)

#### T-1: remover `dotnet-install.ps1` e corrigir o `Verify:` auto-derrotante → DoD 1
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `dotnet-install.ps1` (DELETE), `.claude/settings.local.json`,
  `.jdi/DECISIONS.md`, `.jdi/phases/sonar-zero-issues/CONTEXT.md`
- **Acceptance:**
  - `D-2026-07-30-sonar-zero-issues-7` APENDADA ao fim de `DECISIONS.md`, supersedendo APENAS o
    `Verify:` do item 1 (nao a D-...-1): registra que `grep -r` bate no proprio registro de
    auditoria (`.jdi/`) e em `.idea/**` untracked, e que a nova forma usa `git grep` (so
    rastreados) excluindo `.jdi/`. `git diff .jdi/DECISIONS.md | grep -c '^-[^-]'` == 0.
  - Linha do DoD item 1 em `CONTEXT.md` reescrita para:
    `test ! -e dotnet-install.ps1 && test -z "$(git grep -l 'dotnet-install\.ps1' -- . ':(exclude).jdi' 2>/dev/null)"`
  - `dotnet-install.ps1` deletado (41 issues `powershelldre:*` fecham por remocao) E a permissao
    stale `.claude/settings.local.json:38` removida no MESMO commit (D-...-1 exige).
  - Matriz de mutacao do novo `Verify:` provada nos 2 sentidos: repo limpo -> exit 0; readicionar
    a linha de permissao -> exit != 0.
- **Dependencies:** none
- **Test:** N/A (sem `.cs`). Gate = o `Verify:` corrigido + a matriz de mutacao.
- **Status:** pending

#### T-2: WebView JS — `.dataset`, `for-of`, `Number.parseInt`, optional chaining → DoD 3
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader/Resources/Raw/wwwroot/js/{translation,scroll,bridge}.js`
- **Acceptance:**
  - `translation.js`: 0 `has/get/set/removeAttribute`; >=4 `.dataset.`; os 3 `for` indexados viram
    `for-of`. `data-original` -> `dataset.original`, `delete el.dataset.original` no clear. O
    seletor `p[data-original]` (L48) continua valido — `dataset` reflete no atributo.
  - `scroll.js`: 0 `getAttribute`; `dataset.chapterHref`/`dataset.chapterIndex` (camelCase casa com
    `data-chapter-href`/`data-chapter-index` emitidos por `HtmlUtility.BuildContinuousScrollHtml`,
    que NAO muda); >=2 `Number.parseInt(`.
  - `bridge.js:77,79` (S6582): `window.chrome?.webview` e
    `window.webkit?.messageHandlers?.webwindowinterop`.
  - Sem harness JS (D-...-5): a confirmacao funcional de zoom/scroll-sync/overlay ja esta em
    Deferred to PR review — a task NAO pode inventar um gate que finja prova funcional.
- **Dependencies:** none
- **Test:** nenhum (sem harness JS). Gate = `Verify:` do DoD item 3 (identidade textual).
- **Status:** pending

#### T-3: `index.html` — 2 BUGs + waivers multicriteria em `sonarqube.yml` → DoD 7
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader/Resources/Raw/wwwroot/index.html`,
  `.github/workflows/sonarqube.yml`
- **Acceptance:**
  - `<html lang="pt-BR">` (Web:S5254) e `<title>` no `<head>` (Web:PageWithoutTitleCheck) — 2 BUGs.
  - `user-scalable=no` MANTIDO byte-a-byte (D-...-4); `<style id="reader-theme"></style>` continua
    vazio (populado em runtime pelo ThemeEngine). Remover qualquer um dos dois reprova.
  - Nos args do `dotnet-sonarscanner begin`: `sonar.issue.ignore.multicriteria=e1,e2` com
    `e1.ruleKey=Web:S7926`, `e2.ruleKey=css:S4667`, ambos `resourceKey=**/index.html`, comentario
    YAML citando D-...-4. Nao existe `sonar-project.properties` — config vive nos args.
  - Serializa com T-8 no mesmo arquivo; T-8 so comeca depois desta.
- **Dependencies:** none
- **Test:** `HybridWebViewContractTests` (20 testes) e o unico que le assets do wwwroot — rodar
  para garantir que nada quebrou; nao ha (nem se inventa) assert sobre `<title>`/`lang`.
- **Status:** pending

### Wave 2 — utility + hygiene de teste

#### T-4: `HtmlUtility` regex + `InjectTags`; `HtmlInjectionTests` CA1875 → DoD 2, DoD 4
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Utilities/HtmlUtility.cs`,
  `test/TranslateReader.Tests/HtmlInjectionTests.cs`
- **Acceptance:**
  - As 7 chamadas estaticas (L11, 90, 111, 126, 130, 134, 139 — S6444 + SYSLIB1045) viram
    `[GeneratedRegex]`: `BodyOpenTag`, `HeadOpenTag`, `HtmlOpenTag`, `HtmlTagPresence`,
    `XmlDeclaration` (5 novos + 3 existentes = >=7). Cada atributo novo leva
    `matchTimeoutMilliseconds` (fecha S6444) **e** os `RegexOptions` originais (`IgnoreCase`,
    `Singleline` onde havia) — option perdido nao quebra build nem teste (learning phase 13).
  - `TextBlockRegex` (SYSLIB1044, backreference `\1`): `#pragma warning disable/restore SYSLIB1044`
    citando D-...-3. Nao mudar o pattern — mudaria comportamento.
  - `InjectTags` decomposto: <=4 `if (` entre `public static string InjectTags` e
    `private static string BuildFallbackHtml`. Decomposicao REAL (extrair `InjectBaseTag`/
    `InjectCss`, fecha S3776 L72) — mover codigo para depois do marcador do `awk` sem reduzir
    pontos de decisao e burlar o gate e sera reprovado.
  - `HtmlInjectionTests` vira `partial`; os 3 `Regex.Matches(...).Count` (L23, L64, L143) viram
    `<Gen>Regex().Count(result)` sobre `[GeneratedRegex]` (>=3) — CA1875 + SYSLIB1045 juntos.
- **Dependencies:** none
- **Test (D-6):** `HtmlInjectionTests` ja cobre `InjectTags` (8 chamadas), `ExtractBodyContent` (4)
  e `BuildContinuousScrollHtml` (3) — cobre L11/L90/L111. Para L126/130/134/139
  (`BuildFallbackHtml`) o doer DEVE confirmar que cada branch (sem `<head>`, so `<body>`, so
  `<?xml`, nenhum) e atingida; branch descoberta ganha caso novo de `InjectTags` no mesmo arquivo.
- **Status:** pending

#### T-5: dispose pattern (S3881/CA1816) + hygiene do projeto de teste → DoD 5, DoD 6
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Business/Engines/TranslationEngine.cs`,
  `test/TranslateReader.Tests/{FileUtilityTests,BookTranslationJobAccessTests,ModelAccessTests,
  TranslationCacheAccessTests,BooksAccessTests,ReadingStateAccessTests,SettingsAccessTests,
  LibraryManagerTests,TranslationEngineTests}.cs`
- **Acceptance:**
  - `TranslationEngine`: `public sealed class` + `GC.SuppressFinalize(this)` no `Dispose()`.
    Verificado: zero subclasses no repo (`MauiProgram` registra por interface; os testes
    instanciam o concreto) — `sealed` nao quebra ninguem.
  - CA1816 nos 7 arquivos de teste com `Dispose()`: `GC.SuppressFinalize(this)` — ajuste MECANICO,
    nenhum assert alterado.
  - `FileUtilityTests` L95 (BLOCKER S2699): `DeleteDirectoryAsync_DoesNotThrowForNonExistentDirectory`
    ganha assercao load-bearing no padrao do teste irmao —
    `var ex = await Record.ExceptionAsync(() => sut.DeleteDirectoryAsync(dir)); Assert.Null(ex);`.
  - `LibraryManagerTests:175` (CA1847): `p.Contains('5')` (char); `p.Contains("images")` intacto.
  - `TranslationEngineTests`: `#pragma warning disable/restore xUnit1004` ao redor dos 2
    `[Fact(Skip=...)]`, citando D-...-3 + D-2026-07-30-regression-suite-5(2). Contagem `Fact(Skip`
    permanece exatamente 2 — desskipar quebra CI sem fixture `.gguf`.
- **Dependencies:** none
- **Test (D-6):** `sealed`/`SuppressFinalize` provados pelos 7 `TranslationEngineTests` existentes
  (inclui `Dispose`/`ObjectDisposedException`). O resto e o proprio projeto de teste.
- **Status:** pending

### Wave 3 — producao com risco de comportamento real

#### T-6: Access + ParsingEngine mecanicos (async I/O, cultura, S1192) → DoD 8
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Business/Engines/ParsingEngine.cs`,
  `src/TranslateReader.Core/Access/{BooksAccess,SettingsAccess,ReadingStateAccess,
  BookTranslationJobAccess}.cs`, `test/TranslateReader.Tests/ParsingEngineTests.cs`,
  `test/TranslateReader.Tests/CultureRoundTripTests.cs` (NOVO)
- **Acceptance:**
  - `ParsingEngine` (4x S6966): `ZipFile.Open` -> `await ZipFile.OpenAsync(...)` (`await using`,
    API nativa .NET 10) e os 3 `entry.Open()`/`opfEntry.Open()` -> `await ...OpenAsync()`;
    `StreamWriter` com `await using` para o flush ser assincrono. 0 `.Open()`, >=4 `.OpenAsync(`.
  - `BooksAccess:100` e `SettingsAccess:65`: `BeginTransaction()` -> `await BeginTransactionAsync()`.
    ATENCAO: `DbConnection.BeginTransactionAsync` devolve `DbTransaction` e
    `SqliteCommand.Transaction` e tipado `SqliteTransaction` — cast explicito (ou trocar o tipo do
    parametro de `SettingsAccess.UpsertValueAsync`). Preferir `await using` na transacao.
  - `DateTime.Parse` com `CultureInfo.InvariantCulture`: `BooksAccess` L138/139,
    `ReadingStateAccess` L142/152, `BookTranslationJobAccess` L63/64 (>=2 por arquivo).
  - `ReadingStateAccess` S1192: `private const string BookIdParameter = "$bookId";` nas 6
    `AddWithValue` — sobra no maximo 1 linha com o literal.
- **Dependencies:** none — mas NAO pode editar os 9 arquivos de teste de T-5; testes novos vao em
  arquivo novo ou em `ParsingEngineTests.cs`, que T-5 nao toca.
- **Test (D-6) — risco REAL nomeado um a um:**
  - `OpenAsync`: **HOJE SEM COBERTURA** — `CreateTranslatedEpubAsync` so aparece mockado em
    `TranslationManagerTests`; os 19 `ParsingEngineTests` nao o exercitam. Teste NOVO em
    `ParsingEngineTests.cs`: copia um EPUB de `TestData/` para temp, chama
    `CreateTranslatedEpubAsync` com 1 capitulo traduzido e assere (a) arquivo de saida existe,
    (b) a entry do capitulo contem o texto traduzido, (c) `<dc:title>` do `.opf` atualizado. E o
    unico jeito de provar que o caminho async faz flush. Usa a fixture em disco ja padrao dessa
    classe (sem infra nova — D-...-5).
  - `BeginTransactionAsync`: coberto por `BooksAccessTests.SaveChaptersAsync_PersistsChapters` e
    pelos 7 `SettingsAccessTests.SaveSettingsAsync_*` — todos leem de volta APOS o commit, entao
    transacao quebrada reprova.
  - `CultureInfo.InvariantCulture`: teste NOVO `CultureRoundTripTests.cs` fixa
    `CultureInfo.CurrentCulture` em cultura de calendario nao-gregoriano (ex.: `ar-SA`) e faz
    round-trip de `Book`, `ReadingProgress`, `Bookmark` e `BookTranslationJob` via
    `InMemoryDatabase`, assegurando as datas. **Prova por mutacao obrigatoria:** o teste tem que
    FALHAR se o format provider for retirado. Se nao falhar (formato `"O"` se provar
    culture-independent nesse runtime), o doer registra isso no SUMMARY e cita os round-trips
    existentes — o proibido e afirmar cobertura sem a matriz.
  - S1192: sem mudanca de comportamento; rede = os 7 `ReadingStateAccessTests`.
- **Status:** pending

#### T-7: `TranslationManager` — S107 (8 params) e S3267 → DoD 9
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `src/TranslateReader.Core/Business/Managers/TranslationManager.cs`
- **Acceptance:**
  - `TranslateChaptersWithCacheAsync` e `TranslateSingleChapterAsync` com <=7 params cada, via
    objeto de contexto PRIVADO agrupando `Book`+`sourceLanguage`+`targetLanguage`+`progress`.
    Nenhuma assinatura publica muda, nenhum contrato em `Contracts/` muda, Manager continua fino.
  - `RebuildAllTranslatedChaptersAsync` L182 (S3267): `foreach (var href in
    chapters.Select(chapter => chapter.HRef))`, com os usos de `chapter.HRef` trocados por `href`.
  - Escopo travado: nada de reshuffle amplo do `TranslationManager` (Out of scope do CONTEXT).
- **Dependencies:** none (arquivo disjunto de T-6)
- **Test (D-6):** os 33 `TranslationManagerTests` sao a rede — cobrem `TranslateBookAsync`,
  retomada por `LastCompletedChapterIndex`, cache hit/miss, cancelamento e as 4 chamadas a
  `CreateTranslatedEpubAsync` (que passam por `RebuildAllTranslatedChaptersAsync`). Refactor
  puramente interno: se algum teste precisar mudar, isso e sinal de mudanca de comportamento e
  reprova.
- **Status:** pending

### Wave 4 — ativa o gate bloqueante (por ultimo, de proposito)

#### T-8: `sonar.qualitygate.wait=true` no `dotnet-sonarscanner end` → DoD 10
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `.github/workflows/sonarqube.yml`
- **Acceptance:**
  - `dotnet-sonarscanner end /d:sonar.token="$SONAR_TOKEN" /d:sonar.qualitygate.wait=true`, com
    comentario citando D-...-2.
  - E a ULTIMA task por decisao de ordem: `qualitygate.wait=true` torna o job `sonarqube` (ja check
    obrigatorio via `pipeline.yml:54`) BLOQUEANTE. Ativar antes de T-1..T-7 deixaria o PR desta
    propria phase vermelho pelas 113 issues que ela ainda nao fechou.
  - **Risco residual declarado e ACEITO:** mesmo em ultimo, o gate "Sonar way" mede New Code e
    inclui cobertura sobre linhas novas. As linhas novas de maior risco sao as de T-6
    (`CreateTranslatedEpubAsync`), hoje descobertas — por isso T-6 traz teste novo obrigatorio.
    O resultado remoto so existe apos push+CI e ja esta em Deferred to PR review (D-...-6).
- **Dependencies:** T-1..T-7 (ordem de risco + serializacao em `sonarqube.yml` com T-3)
- **Test:** N/A. Gate = `Verify:` do DoD item 10 + CI verde no PR.
- **Status:** pending

## Execution
- Tasks: 8, cobrindo os 10 itens do DoD. Mapa: 1->T-1 | 2->T-4 | 3->T-2 | 4->T-4 | 5->T-5 |
  6->T-5 | 7->T-3 | 8->T-6 | 9->T-7 | 10->T-8
- Waves: 4 (3 || 2 || 2 || 1). Speedup real BAIXO: a phase e majoritariamente serial por risco, e
  `sonarqube.yml` serializa T-3 com T-8. Nao ha paralelismo a inventar.
- 1 familia de issue = 1 commit atomico; scope = `sonar-zero-issues`.

## Files modified (todas as tasks)
`dotnet-install.ps1`(DEL) · `.claude/settings.local.json` · `.jdi/DECISIONS.md` ·
`.jdi/phases/sonar-zero-issues/CONTEXT.md` · `.github/workflows/sonarqube.yml`(T-3+T-8) ·
`wwwroot/index.html` · `wwwroot/js/{translation,scroll,bridge}.js` · `Core/Utilities/HtmlUtility.cs` ·
`Core/Business/Engines/{ParsingEngine,TranslationEngine}.cs` ·
`Core/Business/Managers/TranslationManager.cs` ·
`Core/Access/{BooksAccess,SettingsAccess,ReadingStateAccess,BookTranslationJobAccess}.cs` ·
`Tests/{HtmlInjectionTests,FileUtilityTests,ModelAccessTests,BooksAccessTests,ReadingStateAccessTests,
SettingsAccessTests,TranslationCacheAccessTests,BookTranslationJobAccessTests,LibraryManagerTests,
TranslationEngineTests,ParsingEngineTests}.cs` · `Tests/CultureRoundTripTests.cs`(NOVO)
Fora de escopo, nenhuma task toca: C# do app MAUI (`Pages/`, `PageModels/`, `Platforms/`).

## Test requirements
- `dotnet build src/TranslateReader.Core/TranslateReader.Core.csproj -c Release` sem warning novo
- `dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release` — **229 testes,
  0 falhas, 0 deletados, 0 afrouxados, 0 `Skip` novo** (os 2 `Skip` do LLamaSharp seguem 2).
  Ajuste MECANICO em teste existente e permitido, mas tem que ser declarado como tal no SUMMARY.
- Cobertura: 90% sobre codigo ALTERADO (D-6); legado pre-`4285f25` isento (D-2)
- `dotnet format --verify-no-changes` em escopo de solucao antes de cada commit
- Todo `Verify:` novo/alterado (T-1) exige matriz de mutacao nos DOIS sentidos: pega o mutante
  realista E continua exit 0 no repo limpo (learning phase 13 — sem isso o gate so prova que o
  comando roda)
