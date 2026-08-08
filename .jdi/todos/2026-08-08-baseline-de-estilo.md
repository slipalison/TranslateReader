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
