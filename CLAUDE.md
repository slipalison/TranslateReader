# CLAUDE.md - TranslateReader

## Projeto

Leitor de EPUB multiplataforma (.NET MAUI). Persiste posicao de leitura em SQLite local.

## Arquitetura: The Method (Volatility-Based Decomposition)

### Regras de Camada (OBRIGATORIAS)

```
Client Layer (Pages/PageModels) -> apenas Managers e Utilities
Business Layer (Managers)       -> Engines, ResourceAccess, Utilities
Business Layer (Engines)        -> ResourceAccess, Utilities
Resource Access Layer            -> Resources (SQLite, FileSystem), Utilities
Utilities                        -> Nenhuma dependencia interna
```

**PROIBIDO:**
- Camada inferior chamar camada superior
- Chamadas sincronas Manager -> Manager
- Pular camadas (Client -> ResourceAccess)
- Business logic em Pages/PageModels (delegar para Manager)
- Regras de negocio em Managers (delegar para Engine)
- Tecnologia de storage exposta nas interfaces de ResourceAccess

### Componentes do Sistema

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
| `FileUtility` | Utility | Operacoes de arquivo (copiar, deletar, verificar existencia) |
| `PromptUtility` | Utility | Constroi prompts de traducao com contexto para o LLM |
| `HtmlUtility` | Utility | Parsing e manipulacao de HTML para o reader (estatico) |

### Naming Conventions

- Managers: `[Noun]Manager` (ex: `ReadingManager`)
- Engines: `[Noun]Engine` (ex: `ParsingEngine`)
- ResourceAccess: `[Resource]Access` (ex: `BooksAccess`)
- Utilities: `[Concern]Utility` (ex: `FileUtility`)
- Contracts: `I[ServiceName]` no namespace `Contracts.[Layer]`
- Models: substantivo singular Pascal-case (`Book`, `Chapter`)

### Contratos (Interfaces)

- 3-5 operacoes por contrato (ideal)
- Maximo 2 contratos por servico
- Nomes comportamentais (verbos), nao property-like
- Interfaces em `Contracts/` — implementacoes NUNCA sao referenciadas diretamente

## Build

```bash
dotnet restore
dotnet build -f net10.0-windows10.0.19041.0   # Windows
dotnet build -f net10.0-android                # Android
dotnet build -f net10.0-ios                    # iOS
```

## Dependencias Principais

- `VersOne.Epub` — parsing de EPUB
- `Microsoft.Data.Sqlite.Core` — SQLite
- `LLamaSharp` — inferencia local de LLM (traducao offline)
- `CommunityToolkit.Mvvm` — MVVM (ObservableObject, RelayCommand)
- `CommunityToolkit.Maui` — componentes MAUI extras

## Padroes de Codigo

- **MVVM** com CommunityToolkit: PageModels herdam `ObservableObject`, usam `[ObservableProperty]` e `[RelayCommand]`
- **DI** via `MauiProgram.cs`: todos os servicos registrados como singleton ou transient
- **Async/Await** em todas as operacoes de I/O
- **Funcoes pequenas** (max 20 linhas preferivel)
- **CQS**: metodos ou mudam estado ou retornam dados, nunca ambos
- **Fail fast**: usar excecoes, nao retornar null
- **Sem comentarios como desodorante**: se precisa comentar, refatorar o codigo

## Estrutura de Pastas

```
/                                  Raiz da solucao (.slnx)
  src/TranslateReader/             Projeto MAUI (Client Layer + UI)
    Pages/                         XAML views (Client Layer)
    Pages/Controls/                Controles customizados
    PageModels/                    ViewModels (Client Layer)
    Serialization/                 JSON contexts e converters
    Utilities/                     Converters XAML (FilePathToImageSource, etc.)
    Resources/                     Fonts, styles, assets
    Platforms/                     Codigo platform-specific
  src/TranslateReader.Core/        Biblioteca de logica (Business + Data)
    Contracts/Managers/            Interfaces de Managers
    Contracts/Engines/             Interfaces de Engines
    Contracts/Access/              Interfaces de ResourceAccess
    Contracts/Utilities/           Interfaces de Utilities
    Business/Managers/             Implementacoes de Managers
    Business/Engines/              Implementacoes de Engines
    Access/                        Implementacoes de ResourceAccess
    Utilities/                     Implementacoes de Utilities
    Models/                        Entidades de dominio (POCOs, records, enums)
  test/TranslateReader.Tests/      Projetos de teste (xUnit + NSubstitute)
```

## Modelos de Dados (SQLite)

```
Book:             ID, Title, Author, Publisher, Language, CoverImagePath, FilePath, TotalChapters, DateAdded, LastOpenedAt
Chapter:          ID, BookId, Title, OrderIndex, HRef
ReadingProgress:  ID, BookId, ChapterHRef, ScrollPosition, ProgressPercentage, UpdatedAt
Bookmark:         ID, BookId, ChapterHRef, Position, Label, CreatedAt
Settings:         Key (PK), Value
TranslationCache: ID, BookId, ChapterHRef, OriginalHash, TranslatedText, CreatedAt (UNIQUE: BookId+ChapterHRef+OriginalHash)
```

## Regras Importantes

1. PageModels chamam NO MAXIMO 1 Manager por caso de uso
2. Managers sao finos — apenas orquestram sequencia, sem if/else de regras de negocio
3. Toda regra de negocio vai para um Engine
4. ResourceAccess nunca expoe SQL ou detalhes de storage na interface
5. Utilities passam no "teste da maquina de cappuccino" (usaveis em qualquer sistema)
6. Dependencias sempre via interface (DIP) — nunca instanciar concretos no business layer

---

# JDI — Workflow de Desenvolvimento

Este projeto usa o JDI (Just Do It) como workflow. JDI eh a camada de **processo**; a arquitetura do projeto ja esta locked nas secoes acima e **nao** deve ser redecidida pelas skills de arquitetura que o JDI traz (`clean-architecture`, `ddd`, `hexagonal`, `onion`, `vertical-slice`). A arquitetura vigente eh The Method.

## Loop canonico

```
/jdi-new "<descricao>"   -> research + PROJECT.md + ROADMAP.md
/jdi-bootstrap           -> cria specialists per-project (doer + reviewer)
/jdi-discuss <N>         -> captura decisoes locked da phase
/jdi-plan <N>            -> decompoe em tasks com waves
/jdi-do <N>              -> executa via doer specialist
/jdi-verify <N>          -> gates de qualidade via reviewer specialist
/jdi-ship <N>            -> finaliza phase + avanca pra proxima

# Roadmap mutation (qualquer hora)
/jdi-add-phase "<name>" [--goal "<t>"] [--at <pos>]   -> adiciona phase
/jdi-remove-phase <N> [--force]                        -> remove future/pending phase

# Continuidade / snapshot
/jdi-status                                            -> resumo: phase atual + ultima acao + proximo comando
/jdi-next                                              -> auto-router: roda o proximo passo correto
```

`/jdi-create [desc]` gera novos agents/skills genericos no `core/` (rodado dentro do repo JDI, nao de projetos consumindo).

## Agents core (em `.claude/agents/`)

Genericos, shipped pelo JDI:

| Agent | Modelo | Funcao |
|---|---|---|
| `jdi-researcher` | Opus | Research pre-roadmap. Le ideia, pergunta, gera PROJECT.md + ROADMAP.md |
| `jdi-bootstrap` | Sonnet | Dispara architect modo specialist pra gerar per-project doer + reviewer |
| `jdi-asker` | Sonnet | Loop adaptativo de perguntas pra capturar decisoes locked (CONTEXT.md) |
| `jdi-planner` | Opus | Decompoe phase em tasks, agrupa em waves de paralelismo (PLAN.md) |
| `jdi-architect` | Opus | Meta-agent. 2 modos: cria agents/skills genericos no core/ OU cria specialists per-project |

## Specialists per-project (em `.jdi/agents/`)

Gerados pelo `/jdi-bootstrap` baseado em PROJECT.md:

| Agent | Funcao |
|---|---|
| `jdi-doer-{slug}` | Executor que ja conhece stack/code-design/conventions do projeto. Sem descoberta, ja sabe |
| `jdi-reviewer-{slug}` | Roda gates de qualidade definidos pra stack: build, tests, coverage, lint, security |

Routing em `.jdi/specialists.md` e `.jdi/reviewers.md`.

## Memoria — files em `.jdi/`

```
.jdi/
  PROJECT.md          <- visao + stack + code-design locked
  ROADMAP.md          <- phases + status
  DECISIONS.md        <- D-XX append-only (decisoes locked)
  STATE.md            <- current_phase + next_step
  specialists.md      <- routing pro doer
  reviewers.md        <- routing pro reviewer
  registry.md         <- audit trail dos specialists criados
  agents/             <- per-project specialists
    jdi-doer-{slug}.md
    jdi-reviewer-{slug}.md
  phases/{NN-slug}/
    CONTEXT.md        <- output do asker
    PLAN.md           <- output do planner
    SUMMARY.md        <- output do doer
    REVIEW.md         <- output do reviewer
```

## Convencoes JDI

- Conventional Commits — scope = phase slug, ex: `feat(01-setup-api): ...`
- Atomic commits — 1 task = 1 commit
- 80% cobertura minima (overridable via PROJECT.md)
- Code design locked uma vez no `/jdi-new`, nunca muda
- D-XX referenciado em commit message quando aplicavel

## Idioma

- Codigo, commits, PRs: ingles
- Discussao, docs em `.jdi/`: pt-BR

## Prioridade quando conflita

1. Seguranca
2. Performance
3. Boas praticas

## Hooks (opcional)

`.githooks/pre-commit` e `post-commit` shipped. Pra ativar:

```bash
git config core.hooksPath .githooks
```

Windows: hooks rodam via Git Bash (vem com Git for Windows). Sem ele, hooks sao silenciosamente ignorados.
