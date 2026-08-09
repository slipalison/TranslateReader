# Todos — sessao de discuss `baseline-de-estilo` (2026-08-08)

Itens levantados na captura de decisoes e conscientemente empurrados para fora do escopo da phase
(ver `## Out of scope` em `.jdi/phases/baseline-de-estilo/CONTEXT.md`).

- **[ESTILO/JS] Nenhum linter/formatter cobre o JavaScript do repo.** Existem 4 scripts de producao
  em `src/TranslateReader/Resources/Raw/wwwroot/js/` (`bridge.js`, `paginated.js`, `scroll.js`,
  `translation.js`) e 6 arquivos de teste em `test/js/` (rodados por `node --test`, sem
  `package.json` no repo). `dotnet format` nao toca em `.js`, entao o `.editorconfig` desta phase
  vale so como dica de editor para eles. Adicionar eslint/prettier implicaria criar toolchain Node
  (package.json + lockfile + job) — decisao propria, phase propria.

- **[ESTILO/XAML] Nenhum formatador cobre os `.xaml`.** `dotnet format` ignora XAML; os 4
  XAML de `Pages/`/`Pages/Controls/` + `Resources/Styles/*.xaml` seguem sem verificacao mecanica.
  Ferramenta candidata: `XamlStyler` (dotnet tool + `.xamlstyler.json`).

- **[ESTILO/MSBUILD] Indentacao dos csproj inconsistente.** `src/TranslateReader/TranslateReader.csproj`
  usa 4 espacos; `src/TranslateReader.Core/*.csproj` e `test/TranslateReader.Tests/*.csproj` usam 2.
  O `.editorconfig` pode declarar a preferencia, mas nada no toolchain aplica em XML — unificar seria
  edicao manual, churn sem gate que segure a regressao.

- **[CI] Gate de lint no pipeline.** Rejeitado em D-2026-08-08-baseline-de-estilo-5: o job de teste
  roda em `ubuntu-latest` sem workload MAUI (nao carrega o csproj do app) e o `dotnet format` com
  MSBuild workspace custa minutos por PR. Reavaliar se um dia houver runner Windows dedicado.

- **[GIT] `.githooks/` nao existe neste repo.** `CLAUDE.md` documenta `git config core.hooksPath
  .githooks` como opcional, mas nao ha hook nenhum commitado. Um `pre-commit` rodando
  `dotnet format whitespace --verify-no-changes` escopado ao staged seria o par natural desta phase.

## Warnings congelados no `NoWarn` (T-5, 2026-08-08) — candidatos a phase de correcao

D-2026-08-08-baseline-de-estilo-6(4) manda registrar aqui os warnings que a phase congelou no
`<NoWarn>` do `Directory.Build.props`. Eles estao suprimidos porque **D-1 proibe tocar em codigo
legado nesta phase**, nao porque sejam aceitaveis. Cada correcao remove o ID do `NoWarn` — o
`NoWarn` so encolhe, nunca cresce sem decisao propria.

### Prioridade 1 — `RISCO:` (bug potencial, nao estilo)

- **`CA1001` (3x)** — `LibraryPageModel`, `ReaderPageModel` e `ReaderPage` possuem
  `_translationCts` (`CancellationTokenSource`, `IDisposable`) sem implementar `IDisposable`.
  Violacao direta de `.claude/rules/csharp.md` §2.4 ("Dispose what you own: ... `CancellationTokenSource`");
  em MAUI, PageModel nao descartado e o modo #1 de vazamento.
- **`CA1305` + `MA0011` (10x + 15x)** — conversao sensivel a cultura em
  `Core/Access` (`SettingsAccess` na frente), `Managers` e `Utilities`. **O caso concreto:**
  `ReadingSettings` guarda `double` (`FontSize`, `LineSpacing`, `LetterSpacing`, `WordSpacing`) como
  string no SQLite via `ToString()`/`TryParse` sem `IFormatProvider`. Numa maquina pt-BR isso grava
  `"1,6"`; qualquer seed/default invariante (`"1.6"`) deixa de fazer round-trip e o valor cai no
  default silenciosamente. Correcao: `CultureInfo.InvariantCulture` em toda a serializacao de
  settings.
- **`CS8602` (7x, `test/TranslateReader.Tests`)** — dereference de referencia possivelmente nula na
  suite. Quando dispara, o teste morre com `NullReferenceException` em vez de reportar a assercao.
- **`MA0009` (18x, `ParsingEngine` + `test/`)** — regex sem timeout sobre HTML de EPUB, que
  `.claude/rules/csharp.md` §4 classifica como **entrada nao confiavel**. ReDoS. Prioridade 1 do
  projeto e seguranca; e o item mais urgente desta lista.
- **`CS0414` (1x, `ReaderPage.xaml.cs:21`)** — `_needsInjection` e escrito nas linhas 114 e 125 e
  **nunca lido**: um guard de reinjecao do WebView que parou de guardar. Ou o guard volta a ser
  usado, ou o campo sai.

### Prioridade 2 — divida de qualidade (o codigo melhora se corrigido)

- `MA0004` (175x fora da camada de UI: 165x em `Core`, 10x em `test/`) — `ConfigureAwait(false)`
  ausente onde ele **esta certo** (biblioteca). Quando esta divida for paga e `MA0004` sair do
  `NoWarn`, o `.editorconfig` continua desligando a rule em `src/TranslateReader/**` — la
  `ConfigureAwait(false)` e defeito, nao divida (csharp.md §3).
- `MA0074` (138x, `test/`) — `Contains`/`StartsWith`/`EndsWith` sem `StringComparison`.
  `StartsWith`/`EndsWith` sao culture-sensitive por default, entao nao e so cerimonia.
- `MA0002` (11x), `MA0006` (15x) — comparacao/lookup de string sem comparer ou sem
  `StringComparison` explicito (csharp.md §2.1 pede `Ordinal`).
- `MA0023` (12x) — regex sem `RegexOptions.ExplicitCapture` (mesmos regexes de `MA0009`; corrigir
  junto).
- `CS0618` (7x) — `Page.DisplayAlert` obsoleto em MAUI 10; trocar por `DisplayAlertAsync`.
- `CA1869` (1x) — `JsonSerializerOptions` novo a cada serializacao no caminho da ponte do WebView
  (csharp.md §2.2 manda cachear).
- `MA0051` (2x) — `LibraryPageModel.cs:193` (91 linhas) e `ReaderPage.xaml.cs:110` (76 linhas)
  contra o limite de 60 do Meziantou e o de 20 de csharp.md §7.
- `CA1822`, `CA1826`, `CA1852`, `CA1859`, `MA0016`, `MA0046`, `MA0048` (1-2x cada) — itens
  pontuais. **`CA1859` e `MA0016` se contradizem** (tipo concreto por performance vs. abstracao de
  colecao): quem pegar a phase decide qual das duas vale neste repo e desliga a outra por decisao,
  em vez de manter as duas congeladas.
