# Phase 10: Review  (slug: readme)

**Verdict:** APPROVED_WITH_WARNINGS

**Round:** 3 (`mode=verify`, iter=3). HEAD `1f4c53a`, branch `jdi/readme`. Commits novos desde o
round 2: `82b3628` (`fix(readme)`: W-10 + W-11), `2f3da79` (`docs(readme)`: secao **Como usar**),
`e791b43` (`docs(readme)`: `## Fix round 2` no SUMMARY), `1f4c53a` (`chore(jdi)`: registro da
phase `epub-zip-slip`).

Escopo real confirmado por `git diff --name-only 02bd8eb..HEAD -- src/ test/ .github/ .semgrep/
*.slnx` -> **vazio**. A phase segue README-only nas tres rodadas.

Esta rodada concentrou o esforco onde risco novo foi introduzido: a secao **Como usar** e prosa
inteiramente nova descrevendo comportamento de produto — a superficie de over-claim mais perigosa
que esta phase produziu. Verifiquei **cada capacidade descrita** contra o codigo real, sem aceitar
o SUMMARY do doer como evidencia. **Zero blockers.** Um claim com condicao mais larga que a
realidade (W-12) e um artefato defasado (W-13) entram abertos; W-10 e W-11 estao fechados.

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` -> `0 Erro(s)`, 40 avisos (todos `MVVMTK0045` em `ReaderPageModel.cs`, legado — mesma baseline dos rounds 1 e 2) |
| Tests | PASS | `Aprovado! - Com falha: 0, Aprovado: 169, Ignorado: 2, Total: 171`. Baseline 169+2 preservada |
| Coverage | SKIPPED | `git log --diff-filter=A ... 4285f25..HEAD \| grep '\.cs$'` -> 0 arquivos. Esperado (D-2/D-6) |
| Lint | PASS | `dotnet format --verify-no-changes` -> exit 0, zero diff |
| Security/Layer | PASS (com warnings) | Greps 5.1/5.2/5.10/5.17 limpos; 5.12 com o unico hit legado conhecido (`TranslationEngine.cs:16`); 5.15 com os 5 catch vazios legados. Nenhum `.cs` tocado — baseline identica |
| Consistency | PASS | 4 commits novos, atomicos, tipo correto (`fix` no de correcao, `docs` nos de prosa/artefato, `chore(jdi)` na mutacao de roadmap), escopo `readme`/`jdi`, trailer de sessao em 4/4 |
| UI Validation | SKIPPED | `has_frontend=false` (cliente MAUI nativo) — SKIP permanente por design |
| DoD | PASS (10/10 auto, 0 manual) | Os 10 `Verify:` do CONTEXT rodados verbatim, 10/10 exit 0 |

## Blockers

**Nenhum.**

---

## Auditoria da secao "Como usar" (`README.md:45-110`)

Item 1 do dispatch, e o de maior peso. **Cada frase da secao foi conferida contra o fonte.**
Resultado: **nenhuma capacidade inventada, nenhuma tecnologia atribuida errado, nenhum numero
falso.** Uma unica imprecisao de escopo (W-12) e um nit.

### 1. Importar (`:51-62`)

| Afirmacao do README | Verificacao no codigo | Veredito |
|---|---|---|
| "A tela inicial e a **Biblioteca**" | `AppShell.xaml:10-13` — unico `ShellContent`, `ContentTemplate` = `LibraryPage`, `Route="library"` | OK |
| "vazia no primeiro uso" | `LibraryPage.xaml:26` `EmptyView="Nenhum livro importado. Toque em Importar para adicionar um EPUB."` | OK |
| "O item **Importar** na barra de ferramentas" | `LibraryPage.xaml:11-12` `<ToolbarItem Text="Importar" Command="{Binding ImportBookCommand}" />` | OK |
| "seletor de arquivos do sistema ja filtrado por EPUB (`.epub` no Windows, `application/epub+zip` no Android, `org.idpf.epub-container` no iOS)" | `LibraryPageModel.cs:56-66` `FilePicker.Default.PickAsync` com `WinUI: [".epub"]`, `Android: ["application/epub+zip"]`, `iOS: ["org.idpf.epub-container"]` — **as tres strings batem literalmente** | OK (ver nota W-5) |
| "O arquivo escolhido e copiado para o armazenamento do app" | `LibraryManager.cs:42` `fileUtility.CopyFileAsync(filePath, booksDirectory)`; `MauiProgram.cs:65` `booksDirectory = Path.Combine(FileSystem.AppDataDirectory, "books")` | OK |
| "dele saem os metadados, a capa e a lista de capitulos" | `LibraryManager.cs:43/45/48` `ExtractMetadataAsync` + `SaveCoverImageAsync` + `ExtractChaptersAsync` | OK |
| "o livro aparece na grade com capa, titulo, autor e uma barra de progresso de leitura sobre a capa" | `LibraryPage.xaml:28-31` `GridItemsLayout Span="3"`; `:97` Image capa; `:112`/`:122` Title/Author; `:102-107` `ProgressBar VerticalOptions="End"` dentro do `Border` da capa | OK |
| "EPUB sem capa cai num placeholder com o titulo" | `LibraryPage.xaml:75-93` Grid visivel via `StringIsNullOrEmptyConverter` sobre `CoverImagePath`, com `Label Text="{Binding Title}"` | OK |
| "O menu de contexto de cada livro traz **Traduzir livro** e **Excluir**" | `LibraryPage.xaml:38-46` `MenuFlyout` com exatamente esses 2 `MenuFlyoutItem` | OK |
| "Excluir pede confirmacao" | `LibraryPageModel.cs:86-90` `DisplayAlert("Excluir livro", ..., "Excluir", "Cancelar")` com early-return se negado | OK |
| "e apaga o EPUB copiado, a capa, as imagens extraidas, o progresso de leitura e o cache de traducao daquele livro" | `LibraryManager.cs:60-70` — os **cinco**, um a um: `RemoveTranslationsForBookAsync`, `RemoveStateForBookAsync`, `RemoveBookAsync`, `DeleteFileAsync(book.FilePath)`, `DeleteFileAsync(book.CoverImagePath)`, `DeleteDirectoryAsync(.../images/{id})` | OK |

### 2. Ler (`:64-81`)

| Afirmacao do README | Verificacao no codigo | Veredito |
|---|---|---|
| "Tocar no livro abre o **Reader**" | `LibraryPage.xaml:49-51` `TapGestureRecognizer -> OpenBookCommand`; `LibraryPageModel.cs:102` `GoToAsync($"reader?bookId={book.Id}")`; `AppShell.xaml.cs:10` `RegisterRoute("reader", typeof(ReaderPage))` | OK |
| "renderiza o capitulo num WebView" | `ReaderPage.xaml:55-59` `HybridWebView` | OK |
| "O botao de engrenagem no topo abre as **Configuracoes de leitura**" | `ReaderPage.xaml:27-33` botao `⚙` no `Shell.TitleView` -> `OnSettingsButtonClicked`; `SettingsOverlay.xaml:29` header `"Configuracoes de leitura"` | OK |
| "valem para o app inteiro (nao por livro)" | `SettingsManager.LoadSettingsAsync/SaveSettingsAsync` -> `ISettingsAccess` **sem parametro `bookId`**; tabela `Settings` e key-value global | OK |
| "se aplicam na hora" | `ReaderPage.xaml.cs:345-346` `SettingsChanged -> ApplySettingsAsync` a cada mudanca; `SettingsOverlay.xaml.cs:77-81` `NotifySettingsChanged` em todo handler | OK |
| "sao gravadas quando voce fecha o painel" | `ReaderPage.xaml.cs:348-352` `OnSettingsCloseRequested -> SaveCurrentSettingsAsync()`. `CloseRequested` dispara tanto do `✕` quanto do backdrop (`SettingsOverlay.xaml.cs:83-87`) | OK |
| "**Tema** — Claro, Escuro ou Sepia" | `SettingsOverlay.xaml:50-79` os 3 botoes; `ThemeType` = Light/Dark/Sepia | OK |
| "**Rolagem** (todos os capitulos num scroll continuo)" | `ReaderPageModel.cs:127-142` `LoadScrollContentAsync` itera **todos** os `Chapters` e monta `BuildContinuousScrollHtml` | OK |
| "**Paginado** (uma pagina por vez, com botoes Anterior/Proximo e indicador de pagina)" | `ReaderPage.xaml:64-79` `PreviousButton`/`NextButton`/`PageIndicatorLabel`; `:410-424` indicador `"{_currentPage + 1} / {_totalPages}"` | OK |
| "O padrao e Paginado" | `ReadingSettings.cs:11` `ReadingMode = ReadingMode.Paginated` | OK |
| "fonte (Georgia, serif, sans-serif, monospace ou OpenDyslexic)" | `SettingsOverlay.xaml.cs:7` `FontOptions = ["Georgia", "serif", "sans-serif", "monospace", "OpenDyslexic"]` — **os 5, na mesma ordem** | OK |
| "tamanho da fonte e espacamento de linha, de letra e de palavra" | `SettingsOverlay.xaml:124/136/148/160` os 4 sliders | OK |
| "idioma de origem e de destino, com `English` -> `Brazilian Portuguese (PT-BR)` como padrao" | `ReadingSettings.cs:14-15` `SourceLanguage = "English"`, `TargetLanguage = "Brazilian Portuguese (PT-BR)"` — **strings identicas** | OK |
| "**Anterior**/**Proximo** viram pagina e, ao chegar no fim do capitulo, pulam para o capitulo seguinte" | `ReaderPage.xaml.cs:238-257` `OnNextButtonClicked`: `_currentPage < _totalPages-1` -> `NextPageAsync()`, senao `HasNextChapter` -> `NavigateNextCommand`. Simetrico em `:216-236` (com `_goToLastPageOnLoad = true` para cair na ultima pagina do capitulo anterior) | OK |
| "A posicao de leitura e gravada ao sair da tela do leitor" | `ReaderPage.xaml.cs:51-68` `OnDisappearing` -> `SaveScrollInfoAsync()` (Rolagem) ou `SaveProgressAsync(...)` (Paginado). **Nao ha save periodico** — o README diz exatamente "ao sair", nao "continuamente" | OK |
| "restaurada na proxima vez que voce abrir o mesmo livro — a pagina no modo Paginado, o ponto do scroll no modo Rolagem" | `ReaderPageModel.cs:81-86` recarrega `progress` e posiciona `CurrentChapterIndex` por `ChapterHRef`; `ReaderPage.xaml.cs:147-165` — Rolagem -> `RestoreScrollPositionAsync()`, Paginado -> `GoToPageAsync((int)SavedScrollPosition)` | OK |

### 3. Traduzir (`:83-110`)

| Afirmacao do README | Verificacao no codigo | Veredito |
|---|---|---|
| "**Antes da primeira traducao o app precisa baixar um modelo GGUF**" | `TranslationManager.cs:32-36` `DownloadModelIfNeededAsync` roda antes de qualquer traducao, em ambos os fluxos (`ReaderPageModel.cs:219`, `LibraryPageModel.cs:161`) | OK |
| "Nao ha nada para configurar: na primeira vez que voce pede uma traducao, o app baixa o modelo padrao" | `TranslationManager.cs:34-35` `if (!modelAccess.IsModelAvailable()) DownloadModelAsync(DefaultModel.DownloadUrl, ...)` — sem input do usuario | OK |
| "`gemma-2-2b-it-Q4_K_M.gguf`" | `TranslationManager.cs:25` `FileName: "gemma-2-2b-it-Q4_K_M.gguf"` — **string identica** | OK |
| "cerca de 1,6 GB" | `TranslationManager.cs:27` `SizeBytes: 1_629_413_888` = 1,63 GB. O overlay do app diz `~1.6 GB` (`ReaderPage.xaml:105`) | OK |
| "do Hugging Face" | `TranslationManager.cs:26` `https://huggingface.co/bartowski/gemma-2-2b-it-GGUF/resolve/main/...` | OK |
| "e depois o carrega na memoria" | `TranslationManager.cs:38-42` `InitializeEngineIfNeededAsync -> translationEngine.InitializeAsync(modelAccess.GetModelPath(), ct)` | OK |
| "As duas etapas aparecem como overlay ... e, no leitor, podem ser canceladas" | `ReaderPage.xaml:83-119` (download) e `:122-156` (load), **ambos com botao `Cancelar`** -> `OnCancelDownloadClicked` -> `CancelTranslationCommand`. A qualificacao "no leitor" e precisa: os overlays equivalentes da Biblioteca (`LibraryPage.xaml:172-197`) **nao** tem Cancelar | OK — qualificacao correta |
| "O download acontece uma vez so" | `ModelAccess.cs:53-59` `IsModelAvailable()` = existe qualquer `*.gguf`; `TranslationManager.cs:34` so baixa se falso | OK |
| "o arquivo fica em `models/`, dentro do diretorio de dados do app" | `MauiProgram.cs:67` `modelsDirectory = Path.Combine(FileSystem.AppDataDirectory, "models")` | OK |
| "o painel de configuracoes do leitor ganha um botao **Excluir modelo** assim que o modelo esta pronto" | Botao existe (`SettingsOverlay.xaml:231-241`) e aparece via `UpdateModelStatus()`. **Mas a condicao real e mais estreita que "o modelo esta pronto"** | **W-12** |
| "o botao **Aa** liga o modo de traducao" | `ReaderPage.xaml:20-26` botao `Aa` -> `OnTranslateButtonClicked` -> `TranslateCommand` -> `IsTranslationModeActive = true` | OK |
| "traduz os paragrafos visiveis da pagina atual, com barra de progresso" | `ReaderPage.xaml.cs:295` `getVisibleParagraphs()`; `translation.js:1-22` filtra por `offsetLeft` dentro de `[left, right)` da pagina corrente; `ReaderPage.xaml:49-53` ProgressBar ligada a `TranslationProgress`/`IsTranslating` | OK |
| "Tocar de novo desliga o modo e devolve o texto original" | `ReaderPage.xaml.cs:267-273` -> `ClearTranslationsAsync()` -> `translation.js:45-54` `clearTranslations()` restaura `data-original` em cada `<p>` e remove o atributo | OK |
| "**So funciona no modo Paginado**: em Rolagem o app avisa e nao traduz" | `ReaderPage.xaml.cs:261-265` `if (IsScrollMode()) { await DisplayAlert("Traducao", "A traducao funciona apenas no modo Paginado...", "OK"); return; }`. **O app realmente avisa** — nao e um early-return silencioso | OK |
| "na Biblioteca, **Traduzir livro** no menu de contexto. Um popup pede os idiomas de origem e destino" | `TranslateBookPopup.xaml:27-33` `SourcePicker` + `TargetPicker`; `.xaml.cs:40-45` devolve `(source, target)` | OK |
| "roda capitulo a capitulo em segundo plano, com progresso e botao **Pausar**" | `TranslationManager.cs:117-127` loop por capitulo; `LibraryPageModel.cs:170-171` `Task.Run(...)`; `LibraryPage.xaml:156-164` botao `Pausar` -> `PauseBookTranslationCommand` | OK |
| "O trabalho e persistido como `BookTranslationJob`" | `TranslationManager.cs:88-105` `GetOrCreateJobAsync` -> `bookTranslationJobAccess.SaveJobAsync`; tabela `BookTranslationJobs` | OK |
| "se voce pausar **ou fechar o app**, na proxima vez o app pergunta se quer retomar" | `LibraryPageModel.cs:111-118` `GetActiveTranslationJobAsync` -> `DisplayAlert("Traducao pendente", "Deseja retomar a traducao anterior?", "Retomar", "Nova traducao")`. O "ou fechar o app" **se sustenta**: `BookTranslationJobAccess.FetchActiveJobAsync` filtra `Status IN ('Pending','InProgress','Paused')` — um job deixado em `InProgress` por fechamento abrupto ainda e recuperado | OK |
| "recomeca do capitulo seguinte ao ultimo concluido" | `TranslationManager.cs:55` `var startChapterIndex = job.LastCompletedChapterIndex + 1` | OK |
| "o resultado vira um **novo EPUB**, com os idiomas no titulo" | `TranslationManager.cs:73-75` `$"{book.Title} [{sourceLanguage} → {targetLanguage}]"` passado a `CreateTranslatedEpubAsync` | OK |
| "importado automaticamente para a biblioteca ao lado do original" | `LibraryPageModel.cs:175-176` `libraryManager.ImportBookAsync(translatedEpubPath)` + `LoadBooksAsync()` | OK |
| "cada trecho vira um hash SHA-256 de `origem|destino|texto`" | `TranslationManager.cs:343-348` `$"{sourceLanguage}\|{targetLanguage}\|{text}"` -> `SHA256.HashData` -> `Convert.ToHexString(...)[..16]`. **A ordem dos campos bate exatamente** | OK |
| "Repetir o mesmo trecho ... nao gasta inferencia de novo" | Cache consultado antes de `GenerateAsync` nos **tres** caminhos: `:149`, `:231`, `:279` | OK |

### As duas omissoes deliberadas — **ambas foram a decisao certa**

O doer declarou ter omitido dois itens por falta de lastro no codigo. Verifiquei os dois; ele
esta certo nos dois, e omitir foi o unico caminho honesto.

**Omissao 1 — o seletor de modelo do painel de configuracoes. CORRETA, e o achado e mais forte
do que o doer registrou.** O painel expoe 3 botoes reais — `Gemma 2B`, `Qwen 3B`, `Phi 3.5`
(`SettingsOverlay.xaml:193-222`) — que gravam `_settings.TranslationModelName` (`:180-199`) e
persistem via `SaveSettingsAsync`. **Mas nada consome esse valor.** `TranslationManager.cs:23-27`
tem um `private static readonly ModelInfo DefaultModel` e `DownloadModelIfNeededAsync` (`:34-35`)
usa `DefaultModel.DownloadUrl` **incondicionalmente**; `InitializeEngineIfNeededAsync` (`:41`) usa
`modelAccess.GetModelPath()`, que retorna o **primeiro `*.gguf` do diretorio**
(`ModelAccess.cs:66`), sem olhar nome. Grep de controle: `TranslationModelName` aparece em
`ReadingSettings.cs`, `SettingsOverlay.xaml.cs` e no `SettingsAccess` — **em nenhum ponto do
caminho de traducao**. Escolher "Qwen 3B" grava a preferencia e baixa Gemma. Documentar esse
seletor teria sido a quarta afirmacao falsa da phase. **Omitir foi certo** — e o seletor morto e
divida de codigo (ver Recomendacao).

**Omissao 2 — download retomavel. CORRETA.** `ModelAccess.DownloadModelAsync` (`:13-51`) abre
`new FileStream(tmpPath, FileMode.Create, ...)` — `Create` **trunca** —, copia em buffer de 80 KB
e no fim faz `File.Move(tmpPath, finalPath, overwrite: true)`. Nao ha header `Range`, nao ha
leitura do tamanho parcial, nao ha retomada. Pior: como `IsModelAvailable()` so conta `*.gguf`, um
`.tmp` interrompido **nao** conta, e o proximo pedido rebaixa 1,6 GB do zero. O README nao promete
retomada em lugar nenhum — conferi a secao inteira. **Omitir foi certo.**

### Nit (nao e warning)

`:91` diz "As duas etapas aparecem como overlay **com progresso**". O overlay de download tem
ProgressBar + percentual + tamanho; o de carregamento tem `ActivityIndicator` indeterminado
(`ReaderPage.xaml:139-141`). "Com progresso" cobre os dois de forma frouxa. Nao afirma capacidade
inexistente e nao induz decisao errada — registro so para nao passar em branco.

### Ordem e navegabilidade

"Como usar" ficou em `:45`, **antes** de Arquitetura e ~240 linhas antes de "Build e Execucao",
que ela referencia via ancora. Chequei as ancoras: `## Build e Execucao` -> `#build-e-execucao` e
`## Roadmap` -> `#roadmap`, ambas resolvem. Colocar uso antes de internals e a escolha certa para
o leitor recem-chegado, e a dependencia ("nao ha instalador, rode do fonte") esta declarada na
primeira linha da secao em vez de escondida. Confirmei tambem que "Nao ha instalador publicado
ainda" se sustenta: `git tag` -> **vazio**, e `release.yml` so dispara em tag `v*`.

---

## W-10 — RULING: RESOLVIDO, e corretamente

Li o bloco `on:` dos 8 reusaveis por conta propria, arquivo por arquivo. A tabela extraida:

| Workflow | `on:` real | cron | README `:367-374` | Veredito |
|---|---|---|---|---|
| `ci.yml` | `workflow_call` | nenhum | "push e PR" | OK |
| `codeql.yml` | `workflow_call` + `schedule` + `workflow_dispatch` | `26 7 * * 1` | "mais cron proprio (segunda, 07:26 UTC)" | OK |
| `semgrep.yml` | `workflow_call` + `schedule` + `workflow_dispatch` | `45 6 * * 1` | "(segunda, 06:45 UTC)" | OK |
| `sca.yml` | `workflow_call` + `workflow_dispatch` + `schedule` | `50 5 * * 3` | "(quarta, 05:50 UTC)" | OK |
| `secret-scan.yml` | `workflow_call` + `schedule` + `workflow_dispatch` | `15 4 * * 0` | "(domingo, 04:15 UTC)" | OK |
| `sonarqube.yml` | `workflow_call` (com `inputs` + `secrets`) | nenhum | "push e PR" | OK |
| `dependency-review.yml` | `workflow_call` | nenhum | "somente PR" | OK |
| `sbom.yml` | `workflow_call` + `workflow_dispatch` + `schedule` | `20 3 * * 2` | "push, nunca em PR ... mais cron proprio (terca, 03:20 UTC)" | OK |

Traducao dos 5 crons conferida campo a campo (`min hora * * dow`; `dow` 0=domingo, 1=segunda,
2=terca, 3=quarta): **5/5 corretos**, incluindo o horario UTC.

- **"somente push" do SBOM SUMIU.** Virou "push, nunca em PR (`if: github.event_name == 'push'`),
  mais cron proprio (terca, 03:20 UTC)" — que e a descricao exata do comportamento.
- **O contraste falso do CodeQL acabou:** os 5 que tem cron agora dizem que tem, os 3 que nao tem
  nao mencionam.
- O paragrafo novo (`:376-381`) declara explicitamente que a coluna soma duas coisas — despacho
  pelo `pipeline.yml` e execucao propria — e lista os 5 e os 3 nominalmente. **Conferi os dois
  conjuntos: corretos.** Isso resolve a causa-raiz da W-10, nao so o sintoma.
- **Condicoes citadas:** `pipeline.yml:64` e literalmente `if: github.event_name ==
  'pull_request'` (job `dependency-review`, declarado em `:62`) e `pipeline.yml:72` e literalmente
  `if: github.event_name == 'push'` (job `sbom`, declarado em `:70`). **Ambas exatas**, ate o
  numero da linha.

Nenhuma over-correction: a tabela nao ganhou nada que os arquivos nao digam.

## W-11 — RULING: RESOLVIDO, no calibre certo

O paragrafo restaurado (`:410-419`) foi lido clausula a clausula contra a minha propria auditoria
dos 10 call sites (round 2):

- "**todo valor derivado do livro e codificado antes de virar JavaScript**" — verdadeiro; e o
  mesmo claim que eu havia verificado e que a rodada 1 removeu por engano.
- "Os 10 pontos de `EvaluateJavaScriptAsync` ... foram auditados um a um" — sao 10 de fato.
- "o HTML do capitulo, os chunks desse HTML e o `HRef` do capitulo passam pelo helper `JsStr`
  (`:486`, que e `JsonSerializer.Serialize`)" — `:456`, `:467`, `:444-445`; `:486-487`
  `JsStr(value) => JsonSerializer.Serialize(value ?? string.Empty)`. Confere, inclusive a linha.
- "os paragrafos traduzidos chegam como payload ja serializado por `JsonSerializer.Serialize`
  (`:305-306`)" — confere.
- "O que sobra sao scripts constantes e nomes de funcao internos, que so recebem os literais
  `loadScrollContent` e `loadChapter`" — `:128` e `:132`. Confere.
- "A regra ... esta em `WARNING`, e nao em `ERROR`, porque o pattern nao consegue provar os dois
  casos que nao passam pelo helper: **a severidade menor descreve o limite do detector, nao um
  furo no codigo**" — e exatamente a conclusao que eu emiti, e esta enunciada como fato sobre o
  detector, nao como promessa de seguranca.

**Calibre correto: nao houve swing de volta para over-claim.** O que retornou foi uma afirmacao
pontual, ancorada em `file:line`, sobre **uma** das quatro protecoes — e o texto diz "Uma dessas
quatro protecoes ja esta implementada no codigo", o que implicitamente mantem as outras tres como
normativas. A distincao deteccao-em-CI vs defesa-em-runtime (o coracao do B-1) segue intacta em
`:407-408`.

## Registro da phase `epub-zip-slip` — RULING: captura fiel, escopo preservado

`D-2026-07-29-epub-zip-slip-1` (`.jdi/DECISIONS.md:106-128`) foi lido inteiro contra o meu achado
do round 2. **Captura tudo que importa, e em dois pontos captura melhor do que `todos.md`:**

- Evidencia com `file:line`: `ReadingManager.cs:59-60` -> `FileUtility.cs:31-32`, e a origem nao
  confiavel (`epub.Content.Images.Local`). Confere.
- Greps de confirmacao (`GetFullPath|ExtractToFile|ExtractToDirectory|entry.FullName` = zero;
  `maxSize|maxBytes|uncompressed|sizeLimit` = zero). Reconferi: continuam zero.
- A regra violada citada nominalmente (`.claude/rules/csharp.md` §4), com o texto.
- **O agravante esta la, com a conclusao forte e nao a fraca:** "A regra exige o acesso sintatico
  a `.FullName`; como o projeto extrai via VersOne.Epub e nunca toca `ZipArchiveEntry`, a regra
  nao pode disparar no unico vetor de zip-slip do produto **em nenhuma forma que ele venha a
  assumir**." Era exatamente o ponto do meu caso D — e o registro incorporou, em vez de repetir a
  hipotese errada da "variavel intermediaria".
- **O escopo de duas entregas esta LOCKED e justificado:** "(1) o containment de path
  (`Path.GetFullPath` + verificacao de prefixo do diretorio destino) e o bound de tamanho
  descomprimido; (2) a correcao da regra Semgrep para casar o padrao real, com fixture provando
  red antes e green depois. **Entregar so (1) deixa o defeito invisivel para o CI na proxima
  regressao.**" E a exigencia (b) que faltava.
- Extra que eu nao havia pedido e esta certo: "Codigo tocado e pos-boundary, entao vale o gate de
  90% de cobertura (D-6) e o teste comeca falhando".
- `ROADMAP.md:47-49` registra a phase com goal coerente com o escopo duplo.

**Nada material faltando.** A unica lacuna e de higiene de artefato, nao de escopo: `todos.md`
segue com o diagnostico superseded (W-13).

---

## Warnings

### Novos nesta rodada

- **W-12 — "Excluir modelo aparece assim que o modelo esta pronto" e mais largo do que o codigo
  entrega (`README.md:94`).** O botao existe e funciona, mas so fica visivel **na sessao de leitor
  que acabou de baixar/carregar o modelo**. Cadeia verificada:
  `SettingsOverlay.UpdateModelStatus()` faz `DeleteModelButton.IsVisible = _isModelAvailable`, que
  vem de `ReaderPage.xaml.cs:341` `SettingsOverlay.ApplySettings(..., _pageModel.IsModelAvailable)`.
  E `ReaderPageModel.IsModelAvailable` (`:67`) e um **flag de sessao**: grep em todo `src/` mostra
  que ele so e escrito em `:275` (dentro de `EnsureModelDownloadedAsync`) e `:300` (delete). Ele
  **nunca** consulta o disco — `ITranslationManager` sequer expoe um `IsModelAvailable`; o unico
  que sabe a verdade e `ModelAccess.IsModelAvailable()` (`:53`), que a camada Client nao alcanca.
  Como `ReaderPageModel` e `AddTransient` (`MauiProgram.cs:99`), **cada** abertura do leitor
  comeca com `false`. Efeito pratico: com o modelo de 1,6 GB no disco, o usuario que reabre o app
  e vai as configuracoes para recuperar espaco **nao encontra o botao** — que e literalmente o
  cenario que a frase do README oferece ("Para recuperar o espaco...").

  **Nao e blocker.** Os blockers da iteracao 1 afirmavam coisas que **nao existem** (hardening
  ausente, suite isolada que nao e, workflow que o pipeline nao dispara). Aqui a capacidade e real
  e o botao aparece — o que esta largo demais e a **condicao**. E, na minha leitura, o defeito
  primario e de **codigo**, nao de doc: o flag deveria vir de `modelAccess.IsModelAvailable()`
  atraves do Manager. Duas saidas: (a) estreitar a frase ("assim que o modelo termina de baixar,
  ainda nesta sessao do leitor"), ou (b) corrigir o codigo e deixar a frase como esta. (b) e
  melhor produto.

- **W-13 — `.jdi/todos.md` ficou com o diagnostico superseded do zip-slip, e ele esta errado.**
  Agora que `epub-zip-slip` e phase, o item de `todos.md` (secao "De `readme` (2026-07-29)")
  duplica o achado **com a causa-raiz antiga**: "ela casa `Path.Combine($DEST, $ENTRY.FullName)`
  ... e o codigo real usa **uma variavel intermediaria (`relativePath`)**". Meu probe do round 2
  (caso D: `Path.Combine(imagesDir, relativePath)`, variavel simples, sem `.Replace`) **tambem nao
  foi detectado** — logo a causa nao e a variavel intermediaria, e sim a exigencia do acesso
  sintatico a `.FullName`. `DECISIONS.md` tem a versao certa; `todos.md` tem a errada e nao exige
  a entrega (2). Risco concreto: um doer que abrir `todos.md` em vez de `DECISIONS.md` escreve uma
  regra Semgrep que passa a casar variaveis intermediarias e **continua cega** ao caminho real,
  fechando a phase com o gate de CI ainda dando falso conforto — precisamente o que o escopo duplo
  existe para impedir. `todos.md` e append-only, entao a correcao e **append**: uma linha
  apontando para `D-2026-07-29-epub-zip-slip-1` como fonte da verdade e marcando o item como
  promovido.

### Carregados das iteracoes 1-2 (reconfirmados abertos)

- **W-3 — "7 tabelas do SQLite" lista nomes de modelo, nao de tabela (`:190-219`).** Os nomes reais
  no DDL sao `Books`, `Chapters`, `Bookmarks`, `BookTranslationJobs` (plural) e `ReadingProgress`,
  `Settings`, `TranslationCache` (singular). Colunas conferem 1:1.
- **W-4 — `CLAUDE.md` esta defasado em relacao ao README.** A tabela de Componentes lista 15 de 16
  servicos (falta `BookTranslationJobAccess`) e Modelos de Dados lista 6 de 7 tabelas (falta
  `BookTranslationJob`). O README segue mais correto que a fonte que ele cita.
- **W-5 — inconsistencia interna de plataformas.** `:10` diz "Windows, Android e iOS" (3); a tabela
  `:37-43` e a Stack `:228` dizem 4 (com Mac Catalyst); o csproj confirma 4. **Reforcado nesta
  rodada:** o novo `:55` lista os filtros de Windows/Android/iOS e omite o `MacCatalyst: ["epub"]`
  que existe em `LibraryPageModel.cs:64`. Erro por omissao, coerente com `:10` e incoerente com a
  tabela — a mesma contradicao agora aparece em dois lugares.
- **W-6 — preambulo do Roadmap se contradiz (`:423`).** "Tudo abaixo esta planejado ... nao existe
  no repositorio hoje", mas a linha de `bookmarks` (`:430`) diz "Hoje so existe a camada de dados".
- **W-7 — arvore de estrutura omite caminhos existentes.** Faltam `docs/` (contem
  `docs/translation-feature-plan.md`, ainda **nao linkado de lugar nenhum**, apesar de ser o plano
  detalhado da feature que a nova secao "Como usar" explica), `.semgrep/` e
  `src/TranslateReader/Properties/`. Erra por omissao, nunca por invencao.
- **W-8 — o DoD desta phase e verificavel sem ser verdadeiro.** Terceira confirmacao: os 10
  `Verify:` deram 10/10 PASS **nas tres rodadas** — com 3 blockers presentes, sem eles, e agora com
  uma secao inteira nova. Sao greps de presenca; nenhum avalia veracidade. **Gate 8 verde nao e
  evidencia de README correto nesta classe de phase**; o que fecha esta phase e a re-verificacao
  factual acima. Recomendacao para `/jdi-discuss` de futuras phases de doc: ao menos um item que
  cruze afirmacao com artefato.
- **W-9 — imprecisao de contagem no SUMMARY da iteracao 1** (2 URLs do SonarCloud em 405-HEAD, nao
  3). Nao corrigido; detalhe de artefato, nao de produto.

### Changelog dos warnings resolvidos nesta rodada

| ID | Titulo | Como fechou |
|---|---|---|
| **W-10** | Coluna "Disparo" omitia o cron de 4 dos 8 jobs; "somente push" do SBOM enganoso | Tabela reescrita com dia+hora UTC dos 5 crons (5/5 corretos), "somente push" removido, e paragrafo novo explicando que a coluna soma despacho + execucao propria. Causa-raiz tratada, nao so o sintoma |
| **W-11** | Remocao do claim de WebView foi overcorrection | Frase propria em `:410-419`, ancorada em `file:line`, escopada a "uma dessas quatro protecoes", com nota honesta de que o `WARNING` do Semgrep e limite do detector. Sem swing para over-claim |
| — | Lacuna "como usar" / primeiro uso (apontada em `## Leitura de ponta a ponta`, rounds 1 e 2) | Secao **Como usar** (`:45-110`) em 3 passos. Auditada capacidade a capacidade acima: 1 imprecisao de escopo (W-12), 0 invencoes |

**Contagem:** 2 warnings numerados resolvidos + 1 lacuna de didatica fechada; 2 novos (W-12, W-13);
7 carregados. **Total aberto: 9.** Nenhum bloqueia o merge.

---

## Veredito final de "bem explicado"

Reli o README inteiro do zero pela terceira vez, como recem-chegado. **O card esta cumprido:
"preciso" ja estava; "bem explicado" fecha agora.**

A ordem final e a certa para quem chega sem contexto: identidade -> o que faz -> onde roda ->
**como usar** -> como e por dentro -> como buildar -> como testar -> seguranca -> o que falta ->
como contribuir -> licenca. Colocar o uso **antes** da arquitetura foi a decisao editorial mais
importante da rodada: o leitor descobre o que o app faz por ele antes de ser apresentado a
Managers e Engines, e quem so quer usar nunca precisa passar por The Method.

O que "Como usar" resolve, e que eu vinha cobrando desde o round 1: o download obrigatorio de
**1,6 GB** aparece em negrito, com nome do arquivo, origem, onde fica no disco, como cancelar e
como apagar depois. Deixou de ser um bullet perdido em Funcionalidades ("Download e gerenciamento
do modelo GGUF ... direto pelo app", `:24`), que parecia recurso, e virou o passo obrigatorio que
e. A frase "e o passo que surpreende quem instala" e desarmante no bom sentido — antecipa a duvida
numero 1 em vez de esperar o usuario tropecar nela.

Tres detalhes que separam este texto de um manual generico, e que so existem porque foram
verificados: (1) "**So funciona no modo Paginado**: em Rolagem o app avisa e nao traduz" — e uma
limitacao real, chata de admitir, e o app de fato exibe o alerta; (2) a ressalva Windows-only
repetida no inicio da secao de traducao, em vez de so no topo do arquivo, onde o leitor que pulou
direto para "Traduzir" a perderia; (3) a distincao entre os dois caminhos de traducao (`Aa` na
pagina visivel vs livro inteiro em job persistido) com o cache explicado como o que liga os dois.

E o que o documento **nao** diz continua sendo o melhor sinal de saude: o seletor de modelo
(Gemma/Qwen/Phi) esta na tela e ficou fora do texto porque nada o consome; retomada de download
ficou de fora porque nao existe. Um README que resiste a descrever a UI que ve, e descreve o
comportamento que o codigo tem, e um README em que da para confiar. Depois de tres rodadas — 3
blockers de over-claim na primeira, 2 warnings na segunda, e agora uma secao inteiramente nova com
**zero** capacidades inventadas — a disciplina virou habito.

Ressalvas honestas: nao ha screenshot nem GIF (opcional, mas para um leitor de EPUB uma imagem da
Biblioteca e do Reader pagaria bem); `docs/translation-feature-plan.md` continua orfao (W-7),
justamente agora que existe uma secao que se beneficiaria de linka-lo; e W-5/W-6 deixam duas
contradicoes internas de uma linha cada, que um leitor atento nota.

**Veredito: bem explicado. Cumpre para entender, construir E usar.**

## DoD Checklist (gate 8)

Os 10 `Verify:` do CONTEXT.md rodados verbatim da raiz do repo, no HEAD `1f4c53a`:

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | (a) Licenca: sem "Projeto privado", cita Apache 2.0 + `LICENSE` | CONTEXT | Auto | PASS | exit 0; `LICENSE` presente; secao `:450-452` |
| 2 | (b)+readme-3 Traducao offline documentada + ressalva Windows-only -> `llm-mobile` | CONTEXT | Auto | PASS | exit 0; `:22-33` e agora tambem `:83-110` |
| 3 | (c) 16 servicos reais na tabela | CONTEXT | Auto | PASS | exit 0; 16/16 |
| 4 | (d) 3 projetos reais na estrutura | CONTEXT | Auto | PASS | exit 0; `:240-277` |
| 5 | (e) `BookDetailPage`/`BookDetailPageModel` so em roadmap | CONTEXT | Auto | PASS | exit 0 |
| 6 | (f) Build aponta o csproj (sem `-f` bare); `dotnet test` presente | CONTEXT | Auto | PASS | exit 0 |
| 7 | (g) Modelos de Dados com `TranslationCache` + `BookTranslationJob` + `OriginalHash` | CONTEXT | Auto | PASS | exit 0 |
| 8 | (h)+(i) `.idea/` fora da arvore; temas Light/Dark/Sepia | CONTEXT | Auto | PASS | exit 0 |
| 9 | Badges D-2026-07-29-readme-2: 6 badges, URL real e resolvivel, ordem locked | CONTEXT | Auto | PASS | exit 0; **6/6 GET 200 re-sondados** (Pipeline, CodeQL, Scorecard, alert_status, coverage, shields); ordem locked confirmada em `:3-8` |
| 10 | D-2026-07-29-readme-4: 4 secoes novas | CONTEXT | Auto | PASS | exit 0 |

**Totals:** 10 items | Auto: 10 (10 PASS, 0 FAIL) | Manual: 0 pending

Registro para o dod-critic (inalterado, e o ponto e esse): estes 10 comandos deram 10/10 PASS nas
**tres** rodadas, inclusive com os 3 blockers presentes. Ver W-8.

## Verificacoes complementares

### Restricao de acentos — PASS (metodo que nao consegue passar em falso)

Scan em Python que enumera **todo** code point > 127 e testa acento por decomposicao NFD
(`unicodedata.combining`), sem depender de locale do `grep -P`:

```
total non-ascii code points: 39
  U+2014 EM DASH  x39
ACCENTED LETTERS: 0  []
LETTERS (any non-ascii alphabetic): []
```

Subiu de 25 para 39 em-dashes com a prosa nova. A restricao locked e **sem acentos**, nao sem
em-dash. **Zero letras acentuadas, zero caracteres alfabeticos nao-ASCII.**

### Gate 5 — greps estruturais

| Check | Resultado |
|---|---|
| 5.1 Client -> Access/Engines | vazio (OK) |
| 5.2 storage tech em `Contracts/Access/` | vazio (OK) |
| 5.10 sync-over-async | vazio (OK) |
| 5.12 static mutavel | 1 hit, `TranslationEngine.cs:16` — baseline legada conhecida |
| 5.15 catch vazio | 5 hits, todos legados |
| 5.17 substitutes em concretos | vazio (OK) |

Nenhum `.cs` foi alterado nas tres rodadas — baseline identica a da iteracao 1. Nada aqui e
atribuivel a esta entrega.

### Gate 6 — consistencia dos 4 commits novos

| Commit | Tipo | Escopo | Arquivos | Atomico? |
|---|---|---|---|---|
| `82b3628` | `fix` | readme | `README.md` | sim — so as correcoes W-10/W-11 |
| `2f3da79` | `docs` | readme | `README.md` | sim — so a secao nova |
| `e791b43` | `docs` | readme | `.jdi/phases/readme/SUMMARY.md` | sim — so o artefato |
| `1f4c53a` | `chore` | jdi | `.jdi/DECISIONS.md`, `.jdi/ROADMAP.md` | sim — mutacao de roadmap |

Tipos corretos (`fix` na correcao, `docs` na prosa/artefato, `chore(jdi)` na mutacao de roadmap —
a convencao do `/jdi-add-phase`). Trailer de sessao em 4/4. **1 assunto = 1 commit** respeitado.

## Recommendation

**Aprovado com warnings — pode seguir para `/jdi-ship`.** Os 2 warnings do round 2 estao fechados
com evidencia re-verificada de forma independente, a lacuna de "como usar" foi fechada melhor do
que o pedido, e a auditoria capacidade-a-capacidade da secao nova nao encontrou **nenhuma
capacidade inventada**. Gates 1-4 verdes, baseline de 169+2 testes preservada, 10/10 DoD, 6/6
badges 200, zero acento.

Ordem de retorno se quiser mais uma passada barata antes do merge:

1. **W-13** — uma linha appendada em `.jdi/todos.md` apontando o item de zip-slip para
   `D-2026-07-29-epub-zip-slip-1` e marcando-o como promovido. **E o item de maior risco desta
   lista**, porque um diagnostico errado deixado no lugar mais provavel de ser lido pode fazer a
   phase `epub-zip-slip` entregar uma regra Semgrep que continua cega.
2. **W-5** e **W-6** — duas contradicoes internas de uma linha cada (3 vs 4 plataformas — agora em
   dois pontos; preambulo absoluto do Roadmap).
3. **W-7** — linkar `docs/translation-feature-plan.md` a partir da secao de traducao, que e
   exatamente o leitor que quer esse documento.

**Fora do escopo desta phase, e o item mais consequente que ela produziu, agora corretamente
escalado:** a phase `epub-zip-slip` esta registrada com o escopo duplo locked. Recomendo priorizar
**antes** das phases de estilo/cobertura — `CLAUDE.md` poe Seguranca em 1o lugar, e o gate de CI
hoje da falso conforto sobre o unico vetor de zip-slip do produto.

Achado adjacente que **nao** e desta phase e ainda nao tem dono, registrado aqui porque so
apareceu na auditoria de hoje: o **seletor de modelo morto** (`SettingsOverlay` grava
`TranslationModelName`, `TranslationManager` sempre usa `DefaultModel`) e o **flag de sessao
`IsModelAvailable`** da W-12. Sao UI que promete ao usuario um controle que o codigo nao honra —
a mesma classe de defeito que esta phase passou tres rodadas expurgando do README, so que na tela
em vez do documento. Vale um `/jdi-add-phase` proprio ou, no minimo, `todos.md` **com** o
apontamento de que W-13 pede.

W-3, W-4, W-8 e W-9 seguem como backlog de manutencao — nenhum segura o merge.
