# TranslateReader - Leitor de EPUB Multiplataforma

Leitor de livros EPUB construido com .NET MAUI, projetado para Windows, Android e iOS (iPhone/iPad).

> **Nota:** .NET MAUI nao possui suporte oficial para Linux. Para suporte Linux, considere integrar com [Avalonia UI](https://avaloniaui.net/) ou aguardar suporte oficial da Microsoft.

## Funcionalidades

- Importar e organizar livros EPUB na biblioteca local
- Leitura de EPUB 2 e EPUB 3 com renderizacao HTML via WebView
- Persistencia automatica da posicao de leitura (SQLite local)
- Retomar leitura exatamente de onde parou
- Navegacao por capitulos com indice interativo
- Bookmarks para marcar trechos importantes
- Suporte a temas claro/escuro

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

- **.NET 10** com **MAUI** (Multi-platform App UI)
- **SQLite** via Microsoft.Data.Sqlite para persistencia local
- **VersOne.Epub** para parsing de arquivos EPUB
- **CommunityToolkit.Mvvm** para padrao MVVM
- **CommunityToolkit.Maui** para componentes UI extras
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
