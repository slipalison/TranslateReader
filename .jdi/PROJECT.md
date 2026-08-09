# TranslateReader

## Visao

Leitor de EPUB multiplataforma (.NET MAUI) com traducao offline via LLM local (LLamaSharp).
Core de leitura, biblioteca e traducao de livro completo ja implementados.

## Tipo

App cliente multiplataforma (desktop + mobile): Windows, Android, iOS, MacCatalyst.
Sem backend proprio — toda persistencia e inferencia rodam no dispositivo.

## Status

**Adotado (brownfield)** em 2026-07-28. Codigo pre-existente; JDI adicionado depois.
Boundary legado: commit `4285f25` (ver D-2).

## Stack (detectada no repo + documentada em CLAUDE.md)

- Linguagem: C# / .NET 10 (`net10.0`), `Nullable` e `ImplicitUsings` habilitados
- UI: .NET MAUI 10.0.51 (`Microsoft.Maui.Controls`), XAML com `MauiXamlInflator=SourceGen`
- TFMs do app: `net10.0-windows10.0.19041.0`, `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`
- MVVM: `CommunityToolkit.Mvvm` 8.4.2 + `CommunityToolkit.Maui` 14.0.1
- EPUB: `VersOne.Epub` 3.3.6
- Persistencia: `Microsoft.Data.Sqlite.Core` 10.0.5 + `SQLitePCLRaw.bundle_green` 2.1.11
- LLM local: `LLamaSharp` 0.26.0 (+ backends `Cpu` e `Cuda12`, hoje so no target Windows)
- Testes: xUnit 2.9.3 + NSubstitute 5.3.0 + coverlet.collector 10.0.1
- Solution: `TranslateReader.slnx` (3 projetos)
- Linter/formatter: nenhum configurado (sem `.editorconfig`, sem analyzers custom)
- Conventional commits: **nao** em uso (0 de 10 commits seguem o padrao)

## Code Design

**LOCKED: The Method (Juval Lowy — decomposicao baseada em volatilidade)**

Camadas fechadas: `Client (Pages/PageModels) -> Managers -> Engines -> ResourceAccess -> Resources`,
com `Utilities` vertical e sem dependencias internas.

Evidencia detectada no repo (design ja documentado pelo proprio usuario, nao inferido):
- `CLAUDE.md` secao "Arquitetura: The Method" com regras de camada OBRIGATORIAS e tabela de componentes
- `README.md` secao "Arquitetura" com diagrama de camadas e volatilidade encapsulada
- Skill dedicada `.claude/skills/the-method-design/` referenciada no `.slnx`
- Estrutura fisica: `Business/Managers/`, `Business/Engines/`, `Access/`, `Utilities/`, `Contracts/{Managers,Engines,Access,Utilities}/`
- Naming 100% aderente: 4 `*Manager`, 3 `*Engine`, 6 `*Access`, 3 `*Utility`

CLAUDE.md explicita que as skills de arquitetura do JDI (`clean-architecture`, `ddd`,
`hexagonal`, `onion`, `vertical-slice`) **nao** devem redecidir a arquitetura (ver D-3).

## Slug

`translatereader`

## Existing assets (snapshot 2026-07-28)

Contexto para o planner. **Nao e TODO** — nada aqui deve virar phase.

`src/TranslateReader.Core` — 48 arquivos .cs, ~2.227 linhas:
- `Contracts/` — 15 interfaces (Managers 4, Engines 3, Access 6, Utilities 2)
- `Business/Managers/` — 4: `Reading`, `Library`, `Translation`, `Settings`
- `Business/Engines/` — 3: `Parsing` (EPUB 2/3 + geracao de EPUB traduzido), `Translation` (LLamaSharp), `Theme`
- `Access/` — 6: `Books`, `ReadingState`, `Settings`, `TranslationCache`, `Model` (GGUF), `BookTranslationJob`
- `Utilities/` — 3: `File`, `Prompt`, `Html`
- `Models/` — 17 POCOs/records/enums

`src/TranslateReader` (app MAUI) — 23 arquivos .cs, ~1.647 linhas:
- `Pages/` — 8 arquivos: `LibraryPage`, `ReaderPage`, `Controls/SettingsOverlay`, `Controls/TranslateBookPopup`
- `PageModels/` — 2: `LibraryPageModel`, `ReaderPageModel`
- `Utilities/` — 5 converters XAML
- `Platforms/` — 8 arquivos (Android, iOS, MacCatalyst, Windows)
- `Resources/Raw/wwwroot/js/` — 4 scripts do WebView: `bridge`, `paginated`, `scroll`, `translation`
- `MauiProgram.cs` — DI de todos os servicos + virtual host mapping `epub-images` (Windows)

Capacidades ja entregues: importar/listar/buscar/remover livros; leitura EPUB 2/3 com imagens;
progresso persistido; modos Scroll e Paginated; temas Light/Dark/Sepia e tipografia configuravel;
download de modelo GGUF; traducao de paragrafos visiveis e de capitulo; cache de traducao por hash;
traducao de livro completo com job persistido (pause/resume) e geracao de EPUB traduzido.

Schema/migrations: sem framework de migration — DDL inline `CREATE TABLE IF NOT EXISTS` nos Access.
7 tabelas: `Books`, `Chapters`, `ReadingProgress`, `Bookmarks`, `Settings`, `TranslationCache`, `BookTranslationJobs`.

Rotas/endpoints: N/A (app cliente, sem API HTTP).

Testes existentes: xUnit, 17 arquivos, ~2.526 linhas, **167** `[Fact]`/`[Theory]`.
Gate de cobertura versionado em `scripts/coverage-gate.sh` (D-2026-08-08-cobertura-e-ci-1):
escopo `AM` pos-`4285f25`, ponderado por linha, medido em 2026-08-09 em 95.28% (1090/1144) no
C# do Core e 100% (318/318) nos 4 scripts JS do WebView — ambos acima dos pisos de 90%/85%.

## Restricoes globais

- Cobertura minima 90% **apenas em codigo novo** (legado isento — ver D-2; threshold elevado por D-6)
- Conventional commits a partir de agora (nao usado no historico legado)
- Commits atomicos por task
- Idioma: codigo, commits e PRs em ingles; discussao e docs em `.jdi/` em pt-BR
- Regras de camada de CLAUDE.md sao obrigatorias e prevalecem sobre skills genericas do JDI
- Prioridade em conflito: seguranca > performance > boas praticas

## LLM config

Provider: nao definido nesta adocao — usar o default do ambiente (Anthropic Claude).
Ajustar aqui caso o usuario prefira Ollama/OpenAI/custom.
