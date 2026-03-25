# Plano de Implementacao — Traducao Offline com LLM Local

## Visao Geral

Adicionar traducao offline de ingles para portugues brasileiro usando LLamaSharp (llama.cpp bindings para .NET). O modelo GGUF roda localmente no dispositivo, sem necessidade de internet apos o download inicial.

---

## Mapeamento para Arquitetura The Method

### Componentes Novos

```
Client Layer
  ReaderPageModel ──────> ITranslationManager (novo)

Business Layer
  TranslationManager ───> ITranslationEngine (novo)
                       ───> IModelAccess (novo)
                       ───> ITranslationCacheAccess (novo)

  TranslationEngine ────> IModelAccess (novo)
                       ───> IPromptUtility (novo)

Resource Access Layer
  ModelAccess ──────────> FileSystem (download/cache modelo GGUF)
  TranslationCacheAccess > SQLite (cache de traducoes)

Utilities
  PromptUtility ────────> Nenhuma dependencia interna
```

### Tabela de Componentes

| Componente | Tipo | Responsabilidade |
|---|---|---|
| `TranslationManager` | Manager | Orquestra: verificar modelo, inicializar engine, traduzir com contexto |
| `TranslationEngine` | Engine | Carrega LLamaSharp, faz inferencia, limpa output, gerencia context window |
| `ModelAccess` | ResourceAccess | Download, armazenamento e exclusao de arquivos GGUF |
| `TranslationCacheAccess` | ResourceAccess | CRUD de traducoes cacheadas no SQLite |
| `PromptUtility` | Utility | Monta prompts por modelo (Gemma, Qwen, Phi), injeta contexto |

### Contratos (Interfaces)

#### `ITranslationManager` (3 operacoes)
```
Task<bool> EnsureModelReadyAsync(IProgress<double>? progress, CancellationToken ct)
IAsyncEnumerable<TranslatedParagraph> TranslateChapterAsync(int bookId, string chapterHRef, CancellationToken ct)
Task DeleteModelAsync()
```

#### `ITranslationEngine` (4 operacoes)
```
Task InitializeAsync(string modelPath, CancellationToken ct)
bool IsReady { get }
IAsyncEnumerable<string> GenerateStreamingAsync(string prompt, float temperature, int maxTokens, CancellationToken ct)
Task<string> GenerateAsync(string prompt, float temperature, int maxTokens, CancellationToken ct)
void Dispose()
```

#### `IModelAccess` (4 operacoes)
```
Task DownloadModelAsync(string url, IProgress<double>? progress, CancellationToken ct)
bool IsModelAvailable()
string GetModelPath()
Task DeleteModelAsync()
```

#### `ITranslationCacheAccess` (3 operacoes)
```
Task<string?> FetchTranslationAsync(int bookId, string chapterHRef, string originalHash)
Task SaveTranslationAsync(int bookId, string chapterHRef, string originalHash, string translatedText)
Task RemoveTranslationsForBookAsync(int bookId)
```

#### `IPromptUtility` (1 operacao)
```
string BuildTranslationPrompt(string text, string modelTemplate, string? bookTitle, string? chapterTitle, string? previousParagraph)
```

### Modelos de Dados Novos

```
TranslatedParagraph:  Original, Translated, Index, TotalParagraphs, Progress
ModelInfo:            Name, FileName, DownloadUrl, SizeBytes, ModelTemplate
```

### Schema SQLite (tabela nova)

```sql
TranslationCache (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    BookId          INTEGER NOT NULL,
    ChapterHRef     TEXT NOT NULL,
    OriginalHash    TEXT NOT NULL,
    TranslatedText  TEXT NOT NULL,
    CreatedAt       TEXT NOT NULL,
    UNIQUE(BookId, ChapterHRef, OriginalHash)
)
```

---

## Dependencias NuGet

### TranslateReader.Core.csproj
```xml
<PackageReference Include="LLamaSharp" Version="0.18.0" />
```

### TranslateReader.csproj (backends por plataforma)
```xml
<PackageReference Include="LLamaSharp.Backend.Cpu" Version="0.18.0"
    Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'" />
```

> Para Android/iOS: compilar llama.cpp nativo (ver secao de compilacao nativa).

---

## Atividades de Implementacao

### Fase 1 — Modelos e Infraestrutura

**1.1 Criar modelos de dominio**
- Arquivo: `src/TranslateReader.Core/Models/TranslatedParagraph.cs`
- Arquivo: `src/TranslateReader.Core/Models/ModelInfo.cs`
- Records/classes simples, sem dependencias

**1.2 Criar IModelAccess + ModelAccess**
- Contrato: `src/TranslateReader.Core/Contracts/Access/IModelAccess.cs`
- Implementacao: `src/TranslateReader.Core/Access/ModelAccess.cs`
- Responsabilidade: download com progresso, verificar existencia, obter path, deletar
- Download usa HttpClient com streaming e arquivo .tmp (rename atomico apos conclusao)
- Diretorio: `FileSystem.AppDataDirectory/models/`

**1.3 Criar ITranslationCacheAccess + TranslationCacheAccess**
- Contrato: `src/TranslateReader.Core/Contracts/Access/ITranslationCacheAccess.cs`
- Implementacao: `src/TranslateReader.Core/Access/TranslationCacheAccess.cs`
- Schema: tabela TranslationCache com indice unico (BookId, ChapterHRef, OriginalHash)
- Hash: SHA256 truncado do texto original (primeiros 16 chars hex)
- Integrar com `LibraryManager.DeleteBookAsync` para limpar cache ao excluir livro

**1.4 Testes para ModelAccess e TranslationCacheAccess**

### Fase 2 — Engine e Utility

**2.1 Criar IPromptUtility + PromptUtility**
- Contrato: `src/TranslateReader.Core/Contracts/Utilities/IPromptUtility.cs`
- Implementacao: `src/TranslateReader.Core/Utilities/PromptUtility.cs`
- Templates por modelo: Gemma (`<start_of_turn>`), Qwen (`<|im_start|>`), Phi (`<|system|>`)
- Regras no prompt: traduzir naturalmente, manter nomes proprios, PT-BR, sem explicacoes
- Recebe contexto opcional: titulo do livro, titulo do capitulo, paragrafo anterior

**2.2 Criar ITranslationEngine + TranslationEngine**
- Contrato: `src/TranslateReader.Core/Contracts/Engines/ITranslationEngine.cs`
- Implementacao: `src/TranslateReader.Core/Business/Engines/TranslationEngine.cs`
- Wrapper sobre LLamaSharp: LLamaWeights, LLamaContext, StatelessExecutor
- `InitializeAsync`: carrega modelo com parametros otimizados
  - ContextSize: 2048
  - GpuLayerCount: 0 (CPU only para compatibilidade)
  - Threads: metade dos cores em mobile, cores-2 em desktop
  - UseMemorymap: true
  - BatchSize: 512
- `GenerateAsync` / `GenerateStreamingAsync`: inferencia com AntiPrompts
- `Dispose`: libera LLamaWeights e LLamaContext
- Implementar IDisposable corretamente

**2.3 Testes para PromptUtility e TranslationEngine**
- PromptUtility: verificar formato de prompt por modelo, inclusao de contexto
- TranslationEngine: testes de integracao (requerem modelo GGUF, marcar como [Trait("Category", "Integration")])

### Fase 3 — Manager

**3.1 Criar ITranslationManager + TranslationManager**
- Contrato: `src/TranslateReader.Core/Contracts/Managers/ITranslationManager.cs`
- Implementacao: `src/TranslateReader.Core/Business/Managers/TranslationManager.cs`
- Dependencias: ITranslationEngine, IModelAccess, ITranslationCacheAccess, IPromptUtility, IBooksAccess, IParsingEngine
- `EnsureModelReadyAsync`: verifica se modelo existe, baixa se necessario, inicializa engine
- `TranslateChapterAsync`:
  1. Obter HTML do capitulo via IParsingEngine
  2. Extrair texto dos paragrafos (strip HTML)
  3. Para cada paragrafo:
     a. Verificar cache (TranslationCacheAccess)
     b. Se nao cacheado: montar prompt com contexto (PromptUtility), gerar traducao (TranslationEngine)
     c. Limpar output (remover prefixos, aspas extras)
     d. Salvar no cache
     e. Yield TranslatedParagraph
  4. Passar paragrafo anterior como contexto para o proximo
- `DeleteModelAsync`: delega para ModelAccess

**3.2 Integrar limpeza de cache na exclusao de livro**
- Atualizar `LibraryManager.DeleteBookAsync` para chamar `translationCacheAccess.RemoveTranslationsForBookAsync(bookId)`

**3.3 Testes para TranslationManager**
- Mocks de todas as dependencias (NSubstitute)
- Testar fluxo de orquestracao
- Testar cache hit/miss
- Testar passagem de contexto entre paragrafos

### Fase 4 — UI do Leitor

**4.1 Adicionar propriedades de traducao ao ReaderPageModel**
- `IsTranslating` (bool)
- `TranslationProgress` (double, 0-1)
- `TranslatedContent` (string, HTML traduzido)
- `IsTranslationVisible` (bool)
- `IsModelDownloading` (bool)
- `ModelDownloadProgress` (double)
- Commands: TranslateCommand, CancelTranslationCommand, ToggleTranslationCommand

**4.2 Implementar fluxo de traducao no ReaderPageModel**
- `TranslateCommand`:
  1. Chamar `translationManager.EnsureModelReadyAsync` (mostra download se necessario)
  2. Chamar `translationManager.TranslateChapterAsync` (streaming de paragrafos)
  3. Montar HTML traduzido progressivamente
  4. Exibir no WebView (substituir ou alternar)
- `CancelTranslationCommand`: cancelar via CancellationTokenSource
- `ToggleTranslationCommand`: alternar entre original e traduzido

**4.3 Adicionar botao de traducao na ReaderPage**
- Botao na toolbar (ao lado do botao de configuracoes)
- Indicador de progresso durante traducao
- Icone de toggle original/traduzido

**4.4 Overlay de download do modelo**
- Exibir quando modelo nao esta baixado e usuario aciona traducao
- Barra de progresso do download
- Info do tamanho (~500 MB)
- Botao cancelar

### Fase 5 — Configuracoes

**5.1 Adicionar configuracao de modelo ao ReadingSettings**
- `TranslationModelName` (string, default: "gemma-2-2b")
- `TranslationTemperature` (double, default: 0.1)
- Persistir via SettingsAccess

**5.2 Adicionar secao de traducao no SettingsOverlay**
- Selector de modelo (Gemma 2B, Qwen 3B, Phi 3.5)
- Info do modelo baixado (tamanho, status)
- Botao para deletar modelo (liberar espaco)

### Fase 6 — Registro DI e Integracao

**6.1 Adicionar NuGets ao .csproj**
- LLamaSharp no TranslateReader.Core.csproj
- LLamaSharp.Backend.Cpu no TranslateReader.csproj (condicional por plataforma)

**6.2 Registrar servicos no MauiProgram.cs**
```
Singleton:  IModelAccess, ITranslationCacheAccess
Singleton:  ITranslationEngine (mantém modelo carregado em memoria)
Transient:  IPromptUtility
Transient:  ITranslationManager
```

**6.3 Atualizar ReaderPageModel e ReaderPage**
- Injetar ITranslationManager no ReaderPageModel
- Conectar eventos de UI

### Fase 7 — Compilacao Nativa Mobile (futura)

**7.1 Backend Android**
- Compilar llama.cpp com Android NDK (arm64-v8a)
- Gerar libllama.so
- Adicionar como AndroidNativeLibrary no .csproj

**7.2 Backend iOS**
- Compilar llama.cpp com Xcode (arm64)
- Habilitar Metal para GPU acceleration
- Gerar libllama.a (estatico)
- Adicionar como NativeReference no .csproj

---

## Decisoes Tecnicas

### Modelo recomendado para comecar
**Gemma 2 2B Q4_K_M** (~520 MB, ~1.5 GB RAM)
- Menor e mais rapido
- Boa qualidade em PT-BR
- Se insuficiente, migrar para Qwen2.5 3B

### Estrategia de traducao
- Traduzir por paragrafo (nao por pagina inteira)
- Passar paragrafo anterior como contexto
- Passar titulo do livro e capitulo como contexto
- Temperature: 0.1 (fidelidade, nao criatividade)
- MaxTokens: 3x o tamanho do texto original (PT-BR tende a ser mais longo)
- Cache agressivo: uma vez traduzido, nao retraduzir

### Exibicao da traducao
- Modo toggle: usuario alterna entre original e traduzido
- HTML traduzido substitui o HTML original no WebView
- Indicador visual de que esta vendo traducao (ex: barra colorida no topo)
- Traducao por capitulo (nao por pagina/scroll position)

### Performance
- Download do modelo: sob demanda na primeira traducao
- Warm-up do modelo: inferencia dummy apos carregamento
- Inferencia: Task.Run para nao bloquear UI thread
- Cache: SHA256 truncado do texto original como chave
- ContextSize: 2048 (suficiente para 1 paragrafo + contexto)

### Limpeza de dados
- Excluir livro: limpar cache de traducoes do livro
- Opcao para deletar modelo GGUF (liberar ~500 MB)
- Cache de traducoes usa mesma connection string do SQLite existente

---

## Diagrama de Dependencias

```
┌─────────────────────────────────────────────────────────────────┐
│ Client Layer                                                     │
│   ReaderPageModel ─────┬───> ITranslationManager                │
│                         └───> IReadingManager (existente)        │
├─────────────────────────────────────────────────────────────────┤
│ Business Layer                                                   │
│   TranslationManager ──┬───> ITranslationEngine                 │
│                         ├───> IModelAccess                       │
│                         ├───> ITranslationCacheAccess            │
│                         ├───> IPromptUtility                     │
│                         ├───> IBooksAccess (existente)           │
│                         └───> IParsingEngine (existente)         │
│                                                                  │
│   TranslationEngine ───┬───> LLamaSharp (NuGet)                │
│                         └───> (sem dependencias internas)        │
├─────────────────────────────────────────────────────────────────┤
│ Resource Access Layer                                            │
│   ModelAccess ──────────> FileSystem (HttpClient + disco)        │
│   TranslationCacheAccess > SQLite                                │
├─────────────────────────────────────────────────────────────────┤
│ Utilities                                                        │
│   PromptUtility ────────> Nenhuma dependencia                    │
└─────────────────────────────────────────────────────────────────┘
```

---

## Estrutura de Pastas (novos arquivos)

```
src/TranslateReader.Core/
  Contracts/Access/        IModelAccess.cs, ITranslationCacheAccess.cs
  Contracts/Engines/       ITranslationEngine.cs
  Contracts/Managers/      ITranslationManager.cs
  Contracts/Utilities/     IPromptUtility.cs
  Access/                  ModelAccess.cs, TranslationCacheAccess.cs
  Business/Engines/        TranslationEngine.cs
  Business/Managers/       TranslationManager.cs
  Utilities/               PromptUtility.cs
  Models/                  TranslatedParagraph.cs, ModelInfo.cs

src/TranslateReader/
  PageModels/              (editar ReaderPageModel.cs)
  Pages/                   (editar ReaderPage.xaml, ReaderPage.xaml.cs)
  Pages/Controls/          (editar SettingsOverlay.xaml, .xaml.cs)
  MauiProgram.cs           (registrar novos servicos)

test/TranslateReader.Tests/
  ModelAccessTests.cs
  TranslationCacheAccessTests.cs
  PromptUtilityTests.cs
  TranslationManagerTests.cs
```

---

## Ordem de Execucao Recomendada

| Ordem | Fase | Atividade | Dependencia |
|-------|------|-----------|-------------|
| 1 | 1.1 | Modelos de dominio | — |
| 2 | 1.2 | ModelAccess | 1 |
| 3 | 1.3 | TranslationCacheAccess | 1 |
| 4 | 1.4 | Testes Access | 2, 3 |
| 5 | 2.1 | PromptUtility | 1 |
| 6 | 6.1 | NuGets LLamaSharp | — |
| 7 | 2.2 | TranslationEngine | 5, 6 |
| 8 | 2.3 | Testes Engine/Utility | 5, 7 |
| 9 | 3.1 | TranslationManager | 2, 3, 5, 7 |
| 10 | 3.2 | Integrar cache na exclusao | 3, 9 |
| 11 | 3.3 | Testes Manager | 9 |
| 12 | 4.1-4.4 | UI do leitor | 9 |
| 13 | 5.1-5.2 | Configuracoes | 12 |
| 14 | 6.2-6.3 | Registro DI | 9, 12 |
| 15 | 7.1-7.2 | Compilacao nativa mobile | 14 |

---

## Riscos e Mitigacoes

| Risco | Impacto | Mitigacao |
|-------|---------|-----------|
| Modelo GGUF muito grande para mobile | Alto | Comecar com Gemma 2B (520 MB), menor disponivel |
| RAM insuficiente em dispositivos low-end | Alto | GpuLayerCount=0, UseMemorymap=true, ContextSize menor |
| Traducao lenta em mobile (~3-5 tok/s) | Medio | Streaming visual (token por token), cache agressivo |
| LLamaSharp incompativel com net10.0 | Alto | Verificar versao mais recente, considerar fork se necessario |
| Qualidade de traducao insuficiente | Medio | Abordagem hibrida: trocar modelo (Gemma → Qwen → Phi) |
| Download interrompido | Baixo | Arquivo .tmp com rename atomico, retomar download |
