D-2026-08-08-baseline-de-estilo-2 (2026-08-08): Conjunto de analyzers = built-in + Meziantou.Analyzer,
centralizado num `Directory.Build.props` na raiz. LOCKED.
(1) Ligados para os 3 projetos: `EnableNETAnalyzers=true`, `AnalysisLevel=latest-recommended`,
`EnforceCodeStyleInBuild=true` (faz as regras IDExxxx do `.editorconfig` reportarem em build, nao so
no `dotnet format`).
(2) MAIS o pacote `Meziantou.Analyzer` (`PrivateAssets=all`, `IncludeAssets=runtime;build;native;
contentfiles;analyzers;buildtransitive`). Escolhido por cobrir mecanicamente as regras que ja estao
locked em `.claude/rules/csharp.md`: `StringComparison` explicito (§2.1), sync-over-async e
`ConfigureAwait` (§3), `CancellationToken` propagado (§3), alocacao/closure em loop (§2.2),
`catch {}` vazio (§1). REJEITADOS: StyleCop.Analyzers (naming/doc, ruido puro sobre legado isento por
D-2) e SonarAnalyzer.CSharp (duplicaria o SonarQube que ja roda no `pipeline.yml`).
(3) `Directory.Build.props` na RAIZ e a unica fonte: `TargetFramework`/`Nullable`/`ImplicitUsings`
hoje duplicados nos 3 csproj podem ser centralizados, mas o requisito e que as propriedades de
analise cheguem aos 3 projetos por heranca MSBuild — provado por `-getProperty:`/`-getItem:`, nao
por grep no arquivo.
(4) Nao se cria `.globalconfig` nem `Directory.Packages.props` (central package management) nesta
phase — YAGNI, e mudaria o gerenciamento de versao de pacote de todo o repo.
