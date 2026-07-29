# TranslateReader - Leitor de EPUB Multiplataforma

[![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/slipalison/TranslateReader/badge)](https://scorecard.dev/viewer/?uri=github.com/slipalison/TranslateReader)

Leitor de livros EPUB construido com .NET MAUI, projetado para Windows, Android e iOS (iPhone/iPad).

> **Nota:** .NET MAUI nao possui suporte oficial para Linux. Para suporte Linux, considere integrar com [Avalonia UI](https://avaloniaui.net/) ou aguardar suporte oficial da Microsoft.

## Funcionalidades

- Importar e organizar livros EPUB na biblioteca local
- Leitura de EPUB 2 e EPUB 3 com renderizacao HTML via WebView
- Persistencia automatica da posicao de leitura (SQLite local)
- Retomar leitura exatamente de onde parou
- Navegacao por capitulos com indice interativo
- Temas de leitura Light, Dark e Sepia, com fonte, tamanho e espacamento configuraveis
- **Traducao offline EN -> PT-BR** com LLM local via LLamaSharp: traduz o paragrafo visivel,
  o capitulo atual ou o livro inteiro sem enviar texto para nenhum servico externo
- Download e gerenciamento do modelo GGUF usado pela traducao, direto pelo app
- Cache de traducao por hash do texto original: retraduzir o mesmo trecho nao gasta inferencia
- Traducao de livro completo roda como job persistido (`BookTranslationJob`) — pode ser pausada,
  o app fechado, e retomada depois a partir do ultimo capitulo concluido
- Exportacao do livro traduzido como um novo EPUB

> **Ressalva — traducao offline hoje roda somente Windows.** Os backends nativos do LLamaSharp
> (`LLamaSharp.Backend.Cpu` e `LLamaSharp.Backend.Cuda12`) so sao referenciados no TFM Windows
> do app. O leitor em si roda nas 4 plataformas; a inferencia local, nao. Suporte a Android/iOS
> esta planejado na phase `llm-mobile` — ver [Roadmap](#roadmap).

## Plataformas Suportadas

| Plataforma | Status |
|---|---|
| Windows | Suportado |
| Android | Suportado |
| iOS (iPhone/iPad) | Suportado |
| macOS (Mac Catalyst) | Suportado |
| Linux | Nao suportado oficialmente pelo MAUI |

## Arquitetura

O projeto segue **The Method** (Decomposicao Baseada em Volatilidade) com arquitetura em camadas fechadas.

### Camadas

```
+-------------------------------------------------------------------+
|  CLIENT LAYER                            |  UTILITIES (vertical)  |
|  Pages / PageModels                      |  FileUtility           |
|  (MAUI Shell + Views + WebView)          |  PromptUtility         |
+------------------------------------------+  HtmlUtility           |
|  BUSINESS LAYER - Managers               |                        |
|  ReadingManager, LibraryManager          |                        |
|  TranslationManager, SettingsManager     |                        |
+------------------------------------------+                        |
|  BUSINESS LAYER - Engines                |                        |
|  ParsingEngine, TranslationEngine        |                        |
|  ThemeEngine                             |                        |
+------------------------------------------+                        |
|  RESOURCE ACCESS LAYER                   |                        |
|  BooksAccess, ReadingStateAccess         |                        |
|  SettingsAccess, TranslationCacheAccess  |                        |
|  ModelAccess, BookTranslationJobAccess   |                        |
+------------------------------------------+                        |
|  RESOURCE LAYER                          |                        |
|  SQLite DB, File System (EPUBs)          |                        |
|  Modelo GGUF local                       |                        |
+-------------------------------------------------------------------+
```

Regras de chamada (fechadas, verificadas em review):

```
Client (Pages/PageModels)  -> apenas Managers e Utilities
Managers                   -> Engines, ResourceAccess, Utilities
Engines                    -> ResourceAccess, Utilities
ResourceAccess             -> Resources (SQLite, FileSystem), Utilities
Utilities                  -> nenhuma dependencia interna
```

Nao e permitido pular camadas, chamar de baixo para cima, nem Manager chamar Manager de forma
sincrona. Detalhes completos em [`CLAUDE.md`](CLAUDE.md).

### Componentes

Os 16 servicos do sistema. Managers, Engines e ResourceAccess vivem em
`src/TranslateReader.Core`; as Pages/PageModels que os consomem vivem em `src/TranslateReader`.

| Componente | Tipo | Responsabilidade |
|---|---|---|
| `ReadingManager` | Manager | Orquestra leitura: abrir livro, salvar/carregar progresso, navegar |
| `LibraryManager` | Manager | Orquestra biblioteca: importar, listar, deletar, buscar |
| `TranslationManager` | Manager | Orquestra traducao: download de modelo, traduzir capitulos/paragrafos/livro completo, cache |
| `SettingsManager` | Manager | Orquestra configuracoes: carregar/salvar settings, gerar CSS de tema |
| `ParsingEngine` | Engine | Parseia EPUB (2/3), extrai metadados, capitulos, conteudo HTML, imagens; cria EPUB traduzido |
| `TranslationEngine` | Engine | Inferencia local com LLamaSharp: inicializar modelo, gerar texto (streaming/batch) |
| `ThemeEngine` | Engine | Gera CSS de temas de leitura (Light, Dark, Sepia) |
| `BooksAccess` | ResourceAccess | CRUD de Book e Chapter no SQLite |
| `ReadingStateAccess` | ResourceAccess | CRUD de ReadingProgress e Bookmark no SQLite |
| `SettingsAccess` | ResourceAccess | CRUD de Settings (key-value) no SQLite |
| `TranslationCacheAccess` | ResourceAccess | Cache de traducoes por hash no SQLite |
| `ModelAccess` | ResourceAccess | Download e gerenciamento de arquivos GGUF de modelo |
| `BookTranslationJobAccess` | ResourceAccess | Estado do job de traducao de livro completo: buscar job ativo, salvar, atualizar progresso, remover |
| `FileUtility` | Utility | Operacoes de arquivo (copiar, deletar, verificar existencia) |
| `PromptUtility` | Utility | Constroi prompts de traducao com contexto para o LLM |
| `HtmlUtility` | Utility | Parsing e manipulacao de HTML para o reader (estatico) |

### Casos de Uso Principais

1. **Importar Livro** - `LibraryPage -> LibraryManager -> ParsingEngine -> BooksAccess`
2. **Ler Livro** - `ReaderPage -> ReadingManager -> ParsingEngine + ReadingStateAccess`
3. **Retomar Leitura** - `LibraryPage -> ReadingManager -> ReadingStateAccess -> ParsingEngine`
4. **Gerenciar Biblioteca** - `LibraryPage -> LibraryManager -> BooksAccess`
5. **Ajustar Leitura** - `ReaderPage -> SettingsManager -> ThemeEngine + SettingsAccess`
6. **Traduzir Paragrafos Visiveis** - `ReaderPage -> TranslationManager -> TranslationEngine + TranslationCacheAccess`
7. **Traduzir Livro Completo** - `LibraryPage -> TranslationManager -> ParsingEngine + TranslationEngine + BookTranslationJobAccess`

### Modelos de Dados

As 7 tabelas do SQLite local (criadas sob demanda pelos respectivos ResourceAccess):

```
Book
  ID, Title, Author, Publisher, Language, CoverImagePath,
  FilePath, TotalChapters, DateAdded, LastOpenedAt

Chapter
  ID, BookId, Title, OrderIndex, HRef

ReadingProgress
  ID, BookId, ChapterHRef, ScrollPosition,
  ProgressPercentage, UpdatedAt

Bookmark
  ID, BookId, ChapterHRef, Position, Label, CreatedAt

Settings
  Key (PK), Value

TranslationCache
  ID, BookId, ChapterHRef, OriginalHash, TranslatedText, CreatedAt
  UNIQUE (BookId + ChapterHRef + OriginalHash)

BookTranslationJob
  Id, BookId, SourceLanguage, TargetLanguage, Status,
  LastCompletedChapterIndex, CreatedAt, UpdatedAt
```

`TranslationCache` e a chave da economia de inferencia: o texto original vira `OriginalHash` e a
restricao `UNIQUE` garante uma traducao por trecho por capitulo. `BookTranslationJob` guarda
`LastCompletedChapterIndex`, que e o ponto de retomada quando um livro completo e pausado.
`Bookmark` existe na camada de dados, mas ainda nao tem UI — ver [Roadmap](#roadmap).

## Stack Tecnologica

- **.NET 10** com **MAUI** (Multi-platform App UI) — Windows, Android, iOS e Mac Catalyst
- **Microsoft.Data.Sqlite.Core** 10.0.10 (+ `SQLitePCLRaw.bundle_green` 2.1.11) para persistencia local
- **VersOne.Epub** 3.3.6 para parsing de arquivos EPUB
- **LLamaSharp** 0.27.0 para inferencia local do LLM de traducao. Os backends nativos
  (`LLamaSharp.Backend.Cpu` e `LLamaSharp.Backend.Cuda12`, mesma versao 0.27.0) estao sob
  condicao de plataforma `windows` no csproj do app — dai a ressalva acima e a phase `llm-mobile`
- **CommunityToolkit.Mvvm** 8.4.2 para padrao MVVM
- **CommunityToolkit.Maui** 14.2.2 para componentes UI extras
- **WebView** para renderizacao de conteudo HTML do EPUB

## Estrutura do Projeto

A solution tem **3 projetos**: a biblioteca de logica (`TranslateReader.Core`), o app MAUI
(`TranslateReader`) e os testes (`TranslateReader.Tests`).

```
TranslateReader.slnx
+-- src/
|   +-- TranslateReader.Core/     (Business + Data — TFM net10.0, sem MAUI)
|   |   +-- Contracts/
|   |   |   +-- Managers/         IReadingManager, ILibraryManager,
|   |   |   |                     ITranslationManager, ISettingsManager
|   |   |   +-- Engines/          IParsingEngine, ITranslationEngine, IThemeEngine
|   |   |   +-- Access/           IBooksAccess, IReadingStateAccess, ISettingsAccess,
|   |   |   |                     ITranslationCacheAccess, IModelAccess,
|   |   |   |                     IBookTranslationJobAccess
|   |   |   +-- Utilities/        IFileUtility, IPromptUtility
|   |   +-- Business/
|   |   |   +-- Managers/         ReadingManager, LibraryManager,
|   |   |   |                     TranslationManager, SettingsManager
|   |   |   +-- Engines/          ParsingEngine, TranslationEngine, ThemeEngine
|   |   +-- Access/               BooksAccess, ReadingStateAccess, SettingsAccess,
|   |   |                         TranslationCacheAccess, ModelAccess,
|   |   |                         BookTranslationJobAccess
|   |   +-- Utilities/            FileUtility, PromptUtility, HtmlUtility
|   |   +-- Models/               Book, Chapter, ReadingProgress, Bookmark,
|   |                             ReadingSettings, BookTranslationJob, ...
|   +-- TranslateReader/          (App MAUI — Client Layer)
|       +-- Pages/                LibraryPage.xaml, ReaderPage.xaml
|       |   +-- Controls/         SettingsOverlay.xaml, TranslateBookPopup.xaml
|       +-- PageModels/           LibraryPageModel.cs, ReaderPageModel.cs
|       +-- Serialization/        JSON contexts e converters
|       +-- Utilities/            Converters de XAML
|       +-- Resources/            Fontes, estilos, assets
|       +-- Platforms/            Codigo platform-specific
+-- test/TranslateReader.Tests/   (xUnit + NSubstitute + coverlet — TFM net10.0)
+-- .github/workflows/            (pipeline, CI, scanners de seguranca, release)
+-- .claude/                      (Claude Code config, rules e skills)
+-- .jdi/                         (workflow JDI: roadmap, phases, decisoes)
```

`TranslateReader.Core` nao referencia MAUI: e uma biblioteca `net10.0` pura, o que permite
rodar os testes sem workload de MAUI instalado.

## Build e Execucao

Pre-requisitos: SDK do .NET 10 e o workload de MAUI (`dotnet workload install maui`).

```bash
# Restaurar dependencias
dotnet restore

# Build do app para Windows
dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0

# Build do app para Android
dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-android

# Build do app para iOS
dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-ios

# Executar no Windows
dotnet run --project src/TranslateReader/TranslateReader.csproj -f net10.0-windows10.0.19041.0
```

> Os comandos apontam para o csproj do app, e nao para a solution. Passar um TFM de plataforma
> na raiz falha com `NETSDK1005`: `TranslateReader.Core` e `TranslateReader.Tests` alvejam
> `net10.0` puro e nao conhecem esse TFM.

## Testes e Cobertura

Os testes ficam em `test/TranslateReader.Tests`, alvejam `net10.0` puro e nao precisam do
workload de MAUI instalado. Sao xUnit + NSubstitute, isolados: sem rede, sem disco e sem SQLite
real.

```bash
# Rodar a suite
dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release

# Rodar coletando cobertura (mesmo comando usado na CI)
dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --collect:"XPlat Code Coverage"
```

Regra de cobertura: **90% de linha em codigo novo ou alterado** a partir do commit de boundary
`4285f25`. Codigo anterior a esse commit e legado, fica isento do limite e nao deve ser
refatorado so para subir numero.

Hoje esse limite e cobrado na revisao: a CI **coleta** a cobertura, mas ainda **nao reprova** o
build por ela. O gate automatico que falha abaixo de 90% esta planejado na phase
`cobertura-e-ci` — ver [Roadmap](#roadmap).

## Seguranca

Para reportar uma vulnerabilidade, siga a politica em [`SECURITY.md`](SECURITY.md) — o report e
privado, via GitHub Security Advisories. **Nao abra issue publica para falha de seguranca.**

Todo push e todo pull request passam pelo orquestrador
[`.github/workflows/pipeline.yml`](.github/workflows/pipeline.yml), que dispara os workflows
reusaveis abaixo:

| Verificacao | Workflow | O que faz |
|---|---|---|
| CodeQL | `codeql.yml` | Analise estatica de C# com o pacote `security-extended`, mais um run agendado semanal |
| Semgrep | `semgrep.yml` | SAST com regras da registry e regras proprias em `.semgrep/` (zip-slip, XXE, injecao no WebView) |
| SCA | `sca.yml` | Gate de vulnerabilidade em dependencias NuGet — reprova em CVE High/Critical |
| Secret scan | `secret-scan.yml` | Gitleaks sobre o historico, complementando o secret scanning nativo do GitHub |
| Dependency review | `dependency-review.yml` | Diff de dependencias em pull request, com resumo comentado no PR |
| SBOM | `sbom.yml` | Gera SBOM SPDX com Syft e publica na Dependency Submission API |
| OpenSSF Scorecard | `scorecard.yml` | Score de postura de supply chain do repositorio |
| SonarQube Cloud | `sonarqube.yml` | Quality Gate e cobertura (antigo SonarCloud) |

Hardening de supply chain aplicado a todos esses workflows: **toda action de terceiro e pinada
por commit SHA completo**, nunca por tag mutavel `@vN`; `permissions:` nega tudo no topo e cada
job eleva somente o que precisa; jobs em `ubuntu-latest` rodam sob `step-security/harden-runner`.

O proprio codigo trata como **entrada nao confiavel** os arquivos EPUB (extracao de zip valida
path escape e limita tamanho descomprimido) e o HTML dos livros renderizado no WebView (todo
valor derivado do livro e codificado antes de chegar em JavaScript). Detalhes das regras
obrigatorias em [`.claude/rules/csharp.md`](.claude/rules/csharp.md).

## Roadmap

Tudo abaixo esta **planejado, nao construido** — nao existe no repositorio hoje. Cada item e
uma phase do roadmap JDI (`.jdi/ROADMAP.md`), na ordem em que sera atacada.

| Phase (slug) | O que entra | Situacao |
|---|---|---|
| `baseline-de-estilo` | `.editorconfig`, `.gitattributes` e analyzers configurados na raiz | Planejado |
| `cobertura-e-ci` | Threshold de cobertura que reprova o build abaixo de 90% em codigo novo | Planejado |
| `bookmarks` | Expor bookmarks em `IReadingManager` e entregar a UI de criar/listar/remover. Hoje so existe a camada de dados (`Bookmark` + operacoes em `IReadingStateAccess`) — nao ha UI | Planejado |
| `detalhe-livro` | Tela de detalhe do livro (`BookDetailPage` + `BookDetailPageModel`). Nao existem no repositorio — versoes antigas deste README as documentavam como se existissem | Planejado |
| `busca-no-livro` | Busca full-text no conteudo dos capitulos do livro aberto. Hoje `ILibraryManager.SearchBooksAsync` busca apenas na biblioteca | Planejado |
| `llm-mobile` | Backends nativos do LLamaSharp em Android/iOS, para a traducao offline sair do Windows | Planejado |

## Contribuindo

O desenvolvimento segue o **JDI** (Just Do It), um workflow de phases versionado em `.jdi/`:
cada entrega passa por discuss -> plan -> do -> verify -> ship, e toda decisao travada fica
registrada em `.jdi/DECISIONS.md`.

Antes de abrir um PR, leia [`CLAUDE.md`](CLAUDE.md): ele traz as regras de arquitetura (The
Method — camadas fechadas e o que cada Manager/Engine/ResourceAccess/Utility pode chamar) e a
secao "JDI — Workflow de Desenvolvimento" com o loop de comandos. As regras obrigatorias de C#
(seguranca, alocacao, concorrencia, testes, estilo) estao em
[`.claude/rules/csharp.md`](.claude/rules/csharp.md).

Convencoes: Conventional Commits com escopo igual ao slug da phase, commits atomicos (1 task =
1 commit), codigo e mensagens de commit em ingles, documentacao de processo em pt-BR.

## Licenca

Distribuido sob a **Apache License 2.0**. O texto completo esta em [`LICENSE`](LICENSE).
