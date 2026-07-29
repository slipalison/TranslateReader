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
+-------------------------------------------------+
|  CLIENT LAYER          |  UTILITIES (vertical)  |
|  Pages / PageModels    |  FileUtility           |
|  (MAUI Shell + Views)  |                        |
+------------------------+                        |
|  BUSINESS LOGIC LAYER  |                        |
|  ReadingManager        |                        |
|  LibraryManager        |                        |
|  ParsingEngine         |                        |
+------------------------+                        |
|  RESOURCE ACCESS LAYER |                        |
|  BooksAccess           |                        |
|  ReadingStateAccess    |                        |
+------------------------+                        |
|  RESOURCE LAYER        |                        |
|  SQLite DB             |                        |
|  File System (EPUBs)   |                        |
+-------------------------------------------------+
```

### Componentes

| Componente | Tipo | Volatilidade Encapsulada |
|---|---|---|
| `ReadingManager` | Manager | Sequencia de atividades de leitura (abrir, navegar, retomar) |
| `LibraryManager` | Manager | Sequencia de gestao da biblioteca (importar, listar, remover) |
| `ParsingEngine` | Engine | Formato do livro (EPUB 2/3, futuros formatos) |
| `BooksAccess` | ResourceAccess | Mecanismo de armazenamento de metadados de livros |
| `ReadingStateAccess` | ResourceAccess | Mecanismo de armazenamento de progresso e bookmarks |
| `FileUtility` | Utility | Operacoes de arquivo (cross-cutting) |

### Casos de Uso Principais

1. **Importar Livro** - `LibraryPage -> LibraryManager -> ParsingEngine -> BooksAccess`
2. **Ler Livro** - `ReaderPage -> ReadingManager -> ParsingEngine + ReadingStateAccess`
3. **Retomar Leitura** - `LibraryPage -> ReadingManager -> ReadingStateAccess -> ParsingEngine`
4. **Gerenciar Biblioteca** - `LibraryPage -> LibraryManager -> BooksAccess`

### Modelos de Dados

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
```

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

```
TranslateReader.slnx
+-- src/
|   +-- TranslateReader/          (MAUI App)
|       +-- Contracts/
|       |   +-- Managers/         IReadingManager, ILibraryManager
|       |   +-- Engines/          IParsingEngine
|       |   +-- Access/           IBooksAccess, IReadingStateAccess
|       |   +-- Utilities/        IFileUtility
|       +-- Business/
|       |   +-- Managers/         ReadingManager, LibraryManager
|       |   +-- Engines/          ParsingEngine
|       +-- Access/
|       |   +-- BooksAccess.cs
|       |   +-- ReadingStateAccess.cs
|       +-- Utilities/
|       |   +-- FileUtility.cs
|       +-- Models/
|       |   +-- Book.cs, Chapter.cs
|       |   +-- ReadingProgress.cs, Bookmark.cs
|       +-- Pages/                (Client Layer)
|       |   +-- LibraryPage.xaml
|       |   +-- ReaderPage.xaml
|       |   +-- BookDetailPage.xaml
|       |   +-- Controls/
|       +-- PageModels/
|       |   +-- LibraryPageModel.cs
|       |   +-- ReaderPageModel.cs
|       |   +-- BookDetailPageModel.cs
|       +-- Resources/
|       +-- Platforms/
+-- test/                         (Projetos de teste)
+-- .claude/                      (Claude Code config e skills)
+-- .idea/                        (Rider config)
```

## Build e Execucao

```bash
# Restaurar dependencias
dotnet restore

# Build para Windows
dotnet build -f net10.0-windows10.0.19041.0

# Build para Android
dotnet build -f net10.0-android

# Build para iOS
dotnet build -f net10.0-ios

# Executar (Windows)
dotnet run -f net10.0-windows10.0.19041.0
```

## Licenca

Projeto privado.
