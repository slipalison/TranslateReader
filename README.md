# TranslateReader - Leitor de EPUB Multiplataforma

[![Pipeline](https://github.com/slipalison/TranslateReader/actions/workflows/pipeline.yml/badge.svg)](https://github.com/slipalison/TranslateReader/actions/workflows/pipeline.yml)
[![CodeQL](https://github.com/slipalison/TranslateReader/actions/workflows/codeql.yml/badge.svg)](https://github.com/slipalison/TranslateReader/actions/workflows/codeql.yml)
[![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/slipalison/TranslateReader/badge)](https://scorecard.dev/viewer/?uri=github.com/slipalison/TranslateReader)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=slipalison_TranslateReader&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=slipalison_TranslateReader)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=slipalison_TranslateReader&metric=coverage)](https://sonarcloud.io/summary/new_code?id=slipalison_TranslateReader)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](LICENSE)

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

## Como usar

Nao ha instalador publicado ainda: rode o app a partir do fonte, seguindo
[Build e Execucao](#build-e-execucao). O fluxo e sempre o mesmo — importar um EPUB, ler e,
opcionalmente, traduzir.

### 1. Importar um EPUB

A tela inicial e a **Biblioteca**, vazia no primeiro uso. O item **Importar** na barra de
ferramentas abre o seletor de arquivos do sistema ja filtrado por EPUB (`.epub` no Windows,
`application/epub+zip` no Android, `org.idpf.epub-container` no iOS). O arquivo escolhido e
copiado para o armazenamento do app; dele saem os metadados, a capa e a lista de capitulos, e o
livro aparece na grade com capa, titulo, autor e uma barra de progresso de leitura sobre a capa.
EPUB sem capa cai num placeholder com o titulo.

O menu de contexto de cada livro traz **Traduzir livro** e **Excluir**. Excluir pede confirmacao e
apaga o EPUB copiado, a capa, as imagens extraidas, o progresso de leitura e o cache de traducao
daquele livro.

### 2. Ler

Tocar no livro abre o **Reader**, que renderiza o capitulo num WebView. O botao de engrenagem no
topo abre as **Configuracoes de leitura**, que valem para o app inteiro (nao por livro), se
aplicam na hora e sao gravadas quando voce fecha o painel:

- **Tema** — Claro, Escuro ou Sepia
- **Modo de leitura** — **Rolagem** (todos os capitulos num scroll continuo) ou **Paginado** (uma
  pagina por vez, com botoes Anterior/Proximo e indicador de pagina). O padrao e Paginado
- **Tipografia** — fonte (Georgia, serif, sans-serif, monospace ou OpenDyslexic), tamanho da
  fonte e espacamento de linha, de letra e de palavra
- **Traducao** — idioma de origem e de destino, com `English` -> `Brazilian Portuguese (PT-BR)`
  como padrao

No modo Paginado, **Anterior**/**Proximo** viram pagina e, ao chegar no fim do capitulo, pulam
para o capitulo seguinte. A posicao de leitura e gravada ao sair da tela do leitor e restaurada na
proxima vez que voce abrir o mesmo livro — a pagina no modo Paginado, o ponto do scroll no modo
Rolagem.

### 3. Traduzir

> Vale a ressalva do inicio deste README: **a inferencia local so roda no Windows hoje**; suporte a
> Android/iOS esta na phase `llm-mobile`.

**Antes da primeira traducao o app precisa baixar um modelo GGUF** — e o passo que surpreende quem
instala. Nao ha nada para configurar: na primeira vez que voce pede uma traducao, o app baixa o
modelo padrao (`gemma-2-2b-it-Q4_K_M.gguf`, cerca de 1,6 GB, do Hugging Face) e depois o carrega
na memoria. As duas etapas aparecem como overlay com progresso ("Baixando modelo de traducao" e
"Carregando modelo") e, no leitor, podem ser canceladas. O download acontece uma vez so: o arquivo
fica em `models/`, dentro do diretorio de dados do app. Para recuperar o espaco, o painel de
configuracoes do leitor tem um botao **Excluir modelo**, visivel depois que o modelo e baixado ou
carregado na sessao atual — reabrir o app esconde o botao mesmo com o arquivo em disco
(`ReaderPageModel.IsModelAvailable` e um flag de sessao, nao uma checagem de disco).

Com o modelo baixado, ha dois caminhos:

- **Traduzir o que esta na tela** — no leitor, o botao **Aa** liga o modo de traducao e traduz os
  paragrafos visiveis da pagina atual, com barra de progresso. Tocar de novo desliga o modo e
  devolve o texto original. **So funciona no modo Paginado**: em Rolagem o app avisa e nao traduz.
- **Traduzir o livro inteiro** — na Biblioteca, **Traduzir livro** no menu de contexto. Um popup
  pede os idiomas de origem e destino e a traducao roda capitulo a capitulo em segundo plano, com
  progresso e botao **Pausar**. O trabalho e persistido como `BookTranslationJob`: se voce pausar
  ou fechar o app, na proxima vez o app pergunta se quer retomar a traducao anterior, e ela
  recomeca do capitulo seguinte ao ultimo concluido. No fim, o resultado vira um **novo EPUB**, com
  os idiomas no titulo, importado automaticamente para a biblioteca ao lado do original.

Os dois caminhos passam pelo mesmo cache: cada trecho vira um hash SHA-256 de
`origem|destino|texto` e a traducao fica em `TranslationCache`. Repetir o mesmo trecho — reler uma
pagina ja traduzida, ou retomar um livro pausado — nao gasta inferencia de novo.

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

# Build do app para iOS
dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-ios

# Build do app para Android — exige o SDK do Android instalado (ver nota abaixo)
dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-android

# Executar no Windows
dotnet run --project src/TranslateReader/TranslateReader.csproj -f net10.0-windows10.0.19041.0
```

Os TFMs disponiveis nao sao fixos: o csproj do app os monta por condicao de sistema operacional e
de SDK presente. Para ver quais existem na sua maquina antes de escolher um `-f`:

```bash
dotnet msbuild src/TranslateReader/TranslateReader.csproj -getProperty:TargetFrameworks
```

> **`NETSDK1005` no build de Android.** No Windows, `net10.0-android` so entra em
> `TargetFrameworks` quando o csproj encontra um SDK do Android — `%LocalAppData%\Android\Sdk`,
> `$ANDROID_HOME` ou `$ANDROID_SDK_ROOT` (`src/TranslateReader/TranslateReader.csproj:7`). Sem
> nenhum deles o TFM nao existe no projeto e o comando acima falha com `NETSDK1005`. Instale o SDK
> do Android (via Visual Studio ou Android Studio) ou builde a partir de Linux/macOS, onde o TFM
> android e incondicional.

> **`NETSDK1005` a partir da raiz.** Todos os comandos apontam para o csproj do app, nao para a
> solution, de proposito: passar um TFM de plataforma na raiz falha pelo mesmo erro, porque
> `TranslateReader.Core` e `TranslateReader.Tests` alvejam `net10.0` puro e nao conhecem esse TFM.

## Testes e Cobertura

Os testes ficam em `test/TranslateReader.Tests`, alvejam `net10.0` puro e nao precisam do
workload de MAUI instalado. Sao xUnit + NSubstitute: **171 testes, 169 passando e 2 ignorados** —
os dois de `TranslationEngineTests` marcados `Skip = "Requires GGUF model file for local
development"`.

A suite nao acessa a rede. Onde a unidade sob teste **e** o acesso a recurso, ela usa o recurso de
verdade num ambiente descartavel: SQLite in-memory pelo provider real `Microsoft.Data.Sqlite`
(`InMemoryDatabase.cs`) e diretorios temporarios sob `Path.GetTempPath()` (`FileUtilityTests`,
`ModelAccessTests`, `ParsingEngineTests`); `HybridWebViewContractTests` le do disco os assets de
JS/HTML do proprio repositorio. O restante isola as dependencias com substitutes das interfaces de
`Contracts/`.

Para **codigo e testes novos**, a regra da secao 6 de
[`.claude/rules/csharp.md`](.claude/rules/csharp.md) pede isolamento completo — sem rede, sem
disco e sem SQLite real, com NSubstitute apenas contra interfaces. Ela vale dali para frente; a
suite legada, anterior ao commit de boundary, nao a cumpre.

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

Todo push em `main` e todo pull request disparam o orquestrador
[`.github/workflows/pipeline.yml`](.github/workflows/pipeline.yml). Ele despacha 8 jobs, cada um
um workflow reusavel (`workflow_call`) proprio:

| Job | Workflow | Disparo | O que faz |
|---|---|---|---|
| CI | `ci.yml` | push e PR | Suite de testes com coleta de cobertura no Linux, mais build do app Windows |
| CodeQL | `codeql.yml` | push e PR, mais cron proprio (segunda, 07:26 UTC) | Analise estatica de C# com o pacote `security-extended` |
| Semgrep | `semgrep.yml` | push e PR, mais cron proprio (segunda, 06:45 UTC) | SAST com regras da registry mais as regras proprias de `.semgrep/` |
| SCA | `sca.yml` | push e PR, mais cron proprio (quarta, 05:50 UTC) | Gate de vulnerabilidade em dependencias NuGet — reprova em CVE High/Critical |
| Secret scan | `secret-scan.yml` | push e PR, mais cron proprio (domingo, 04:15 UTC) | Gitleaks sobre o historico, complementando o secret scanning nativo do GitHub |
| SonarQube Cloud | `sonarqube.yml` | push e PR | Quality Gate e cobertura (antigo SonarCloud) |
| Dependency review | `dependency-review.yml` | somente PR (`if: github.event_name == 'pull_request'`) | Diff de dependencias do PR, com resumo comentado quando reprova |
| SBOM | `sbom.yml` | push, nunca em PR (`if: github.event_name == 'push'`), mais cron proprio (terca, 03:20 UTC) | Gera SBOM SPDX com Syft e publica na Dependency Submission API |

A coluna Disparo soma duas coisas diferentes: quando o `pipeline.yml` despacha o job, e quando o
workflow roda por conta propria. Cinco dos oito — `codeql.yml`, `semgrep.yml`, `sca.yml`,
`secret-scan.yml` e `sbom.yml` — declaram `schedule` e `workflow_dispatch` alem de
`workflow_call`, entao rodam no dia e hora agendados sem push nenhum e podem ser disparados a mao
pela aba Actions. Os outros tres (`ci.yml`, `sonarqube.yml` e `dependency-review.yml`) declaram
somente `workflow_call`: fora do pipeline eles nao rodam.

Outros dois workflows rodam **fora** do orquestrador. Nenhum dos dois declara `workflow_call`,
entao `pipeline.yml` nao tem como chama-los:

| Workflow | Disparo | Por que fica separado |
|---|---|---|
| `scorecard.yml` | cron semanal (sabado, 02:30 UTC), push em `main` e dispatch manual | Publica o resultado no OpenSSF (`publish_results: true`), o que exige rodar como workflow proprio do repositorio, nao como job aninhado |
| `release.yml` | push de tag `v*` | Empacota a release do Windows; nao faz parte do ciclo de push/PR |

Hardening de supply chain aplicado a todos esses workflows: **toda action de terceiro e pinada
por commit SHA completo**, nunca por tag mutavel `@vN`; `permissions:` nega tudo no topo e cada
job eleva somente o que precisa; jobs em `ubuntu-latest` rodam sob `step-security/harden-runner`.

Arquivo EPUB e HTML de livro sao **entrada nao confiavel**. As regras obrigatorias para trata-los
— rejeitar path escape ao montar caminho a partir de entrada de zip, limitar tamanho
descomprimido, parsear XML com DTD desabilitado, codificar todo valor derivado do livro antes de
interpola-lo em JavaScript — estao escritas em
[`.claude/rules/csharp.md`](.claude/rules/csharp.md) secao 4. Sao **normativas para codigo novo**,
nao uma descricao do que ja esta implementado.

Quem cobra essas regras hoje e a CI. O arquivo `.semgrep/dotnet-security.yml` traz 4 regras
proprias que procuram exatamente esses padroes — `translatereader-zip-slip`,
`translatereader-xxe`, `translatereader-webview-js-injection` e
`translatereader-insecure-deserialization` — e o job de Semgrep as roda em todo push e PR
(`semgrep scan --config .semgrep/ --severity ERROR --error`), reprovando o build nas de severidade
ERROR. Sao regras de **deteccao em CI**, nao defesas em runtime: elas apontam o codigo que
viola a politica, quem implementa a protecao e o codigo.

Uma dessas quatro protecoes ja esta implementada no codigo, e nao so escrita na regra: **todo
valor derivado do livro e codificado antes de virar JavaScript.** Os 10 pontos de
`EvaluateJavaScriptAsync` em `src/TranslateReader/Pages/ReaderPage.xaml.cs` foram auditados um a
um — o HTML do capitulo, os chunks desse HTML e o `HRef` do capitulo passam pelo helper `JsStr`
(`:486`, que e `JsonSerializer.Serialize`), e os paragrafos traduzidos chegam como payload ja
serializado por `JsonSerializer.Serialize` (`:305-306`). O que sobra sao scripts constantes e
nomes de funcao internos, que so recebem os literais `loadScrollContent` e `loadChapter`. A regra
`translatereader-webview-js-injection` esta em `WARNING`, e nao em `ERROR`, porque o pattern nao
consegue provar os dois casos que nao passam pelo helper: a severidade menor descreve o limite do
detector, nao um furo no codigo.

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
