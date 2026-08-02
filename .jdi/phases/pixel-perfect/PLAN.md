# Phase 22: Pixel-perfect da chrome vs mockups — Plan (slug: pixel-perfect)

## Leia antes de comecar (obrigatorio)

1. Voce vai executar 9 tasks EM ORDEM (T-1 -> T-9). Nao pule, nao reordene, nao junte tasks.
2. TODA medida vem de `design/PIXEL-SPEC.md`. Abra e leia a secao indicada em cada task ANTES
   de editar. Se um valor nao estiver la nem aqui, PARE a task, escreva `BLOCKED: <o que falta>`
   no `SUMMARY.md` desta phase e nao invente.
3. Cada task termina com **Criterio de sucesso**: um comando bash (Git Bash no Windows, rodar
   da raiz do repo). Exit 0 = concluida. Exit != 0 = corrija antes de seguir. Depois do
   criterio passar: `dotnet build src/TranslateReader/TranslateReader.csproj -f net10.0-windows10.0.19041.0`
   tem que compilar com 0 erros ANTES do commit.
4. Um commit por task: `style(pixel-perfect): T-N <resumo em ingles>` (T-3/T-4: `feat`, T-9:
   `test`). Nao commitar com build quebrado.
5. Regras globais de "NAO FACA" (valem em todas as tasks):
   - NAO tocar em NADA dentro de `src/TranslateReader.Core/` nem `src/TranslateReader/Resources/Raw/`.
   - NAO remover nenhum `x:Name`, handler, evento publico ou binding existente.
   - NAO usar `FontAttributes="Bold"` nas superficies editadas — peso 500 = `FontFamily="InterMedium"`.
   - NAO converter padding errado: CSS `p:16px 28px` = MAUI `Padding="28,16"`; CSS
     `p:6px 10px 6px 32px` (top,right,bottom,left) = MAUI `Padding="32,6,10,6"` (left,top,right,bottom).
   - NAO escrever hex solto em XAML/C# — sempre `{StaticResource <Token>}` (C#:
     `(Color)Application.Current!.Resources["<Token>"]`).
   - Icones: `<Label FontFamily="Phosphor" Text="&#xE30C;" ...>` (ou `PhosphorFill` quando a
     tabela do PIXEL-SPEC diz Fill). Codepoints APENAS da tabela "Icones — Phosphor".

## Goal
Chrome pixel-perfect vs mockups (Inter + Phosphor + medidas do PIXEL-SPEC), desktop e mobile,
Core intocado.

## Locked decisions (from CONTEXT.md)
D-...-1 fronteira Client-only; D-...-2 Inter 400/500; D-...-3 Phosphor; D-...-4 list view
aceita (so desktop); D-...-5 ColorDanger #E08A8A; D-...-6 grid adaptativo; D-...-7 flyout
nativo + botao ⋮; D-...-8 execucao sequencial com Verify literal.

## Frozen names (criar EXATAMENTE com estes nomes; DoD depende deles)
`ImportButton`, `GridToggleButton`, `ListToggleButton`, `BooksListCollection`,
`IsListView`, `ShowGridView`, `ShowListView` (LibraryPageModel — geram
`ShowGridViewCommand`/`ShowListViewCommand`); `ChapterSubtitleLabel`, `ReaderFooter`,
`PageProgressBar`, `ChapterSubtitle` (ReaderPageModel); `ModelsList` (SettingsOverlay).

---

## Tasks (sequencial — nao ha waves)

### T-1: Baseline da phase + fontes Inter e Phosphor
- **Files:** `.jdi/phases/pixel-perfect/BASELINE` (novo), `src/TranslateReader/Resources/Fonts/`
  (4 TTF novos), `src/TranslateReader/MauiProgram.cs`, `THIRD-PARTY-NOTICES.md`
- **Passos:**
  1. `git rev-parse HEAD > .jdi/phases/pixel-perfect/BASELINE` (ancora do DoD 11 — fazer
     ANTES de qualquer outra mudanca).
  2. Baixar Inter: `curl -L -o /tmp/inter.zip https://github.com/rsms/inter/releases/download/v4.1/Inter-4.1.zip`.
     Listar com `unzip -l /tmp/inter.zip | grep -iE 'Inter-(Regular|Medium)\.ttf'` e extrair os
     dois arquivos (estao em `extras/ttf/` no release 4.1; se o caminho for outro, use o que a
     listagem mostrar) para `src/TranslateReader/Resources/Fonts/Inter-Regular.ttf` e
     `Inter-Medium.ttf`.
  3. Baixar Phosphor:
     `curl -L -o src/TranslateReader/Resources/Fonts/Phosphor.ttf https://raw.githubusercontent.com/phosphor-icons/web/master/src/regular/Phosphor.ttf`
     `curl -L -o src/TranslateReader/Resources/Fonts/Phosphor-Fill.ttf https://raw.githubusercontent.com/phosphor-icons/web/master/src/fill/Phosphor-Fill.ttf`
     Conferir que cada TTF tem > 50KB (`wc -c`). Se um download falhar apos 3 tentativas:
     BLOCKED (nao prosseguir com font fake).
  4. Em `MauiProgram.cs`, dentro do `ConfigureFonts`, adicionar apos as linhas OpenSans:
     `fonts.AddFont("Inter-Regular.ttf", "InterRegular");`
     `fonts.AddFont("Inter-Medium.ttf", "InterMedium");`
     `fonts.AddFont("Phosphor.ttf", "Phosphor");`
     `fonts.AddFont("Phosphor-Fill.ttf", "PhosphorFill");`
  5. Em `THIRD-PARTY-NOTICES.md`, adicionar duas secoes: "Inter (SIL Open Font License 1.1)"
     com link https://github.com/rsms/inter e "Phosphor Icons (MIT)" com link
     https://github.com/phosphor-icons/web, seguindo o formato das secoes ja existentes.
- **NAO FACA:** nao apagar os OpenSans*.ttf; nao mexer em Styles.xaml ainda (e T-2).
- **Criterio de sucesso:**
  `F=src/TranslateReader/Resources/Fonts; test -f .jdi/phases/pixel-perfect/BASELINE && for f in Inter-Regular.ttf Inter-Medium.ttf Phosphor.ttf Phosphor-Fill.ttf; do test -f "$F/$f" && test "$(wc -c < "$F/$f")" -gt 50000 || exit 1; done && for a in InterRegular InterMedium Phosphor PhosphorFill; do grep -q "\"$a\"" src/TranslateReader/MauiProgram.cs || exit 1; done && grep -qi "Phosphor" THIRD-PARTY-NOTICES.md && grep -qi "Inter" THIRD-PARTY-NOTICES.md`
- **Status:** completed

### T-2: Tokens novos + default font + morte do #E53E3E
- **Files:** `Resources/Styles/DesignTokens.xaml`, `Resources/Styles/Styles.xaml`,
  `Pages/LibraryPage.xaml`, `Pages/ReaderPage.xaml`, `Pages/Controls/SettingsOverlay.xaml`
- **Passos:**
  1. Em `DesignTokens.xaml`, adicionar (secao nova "Pixel-spec extensions"):
     `<Color x:Key="ColorDanger">#E08A8A</Color>`
     `<Color x:Key="AccentTint10">#1A9184D9</Color>`
     `<Color x:Key="AccentTint08">#149184D9</Color>`
     `<Color x:Key="OverlayScrim">#80292B31</Color>`
     `<Color x:Key="CoverScrim">#A6161826</Color>`
     `<Color x:Key="TextMuted70">#B3E9E9ED</Color>`
     `<Color x:Key="TextMuted55">#8CE9E9ED</Color>`
     `<Color x:Key="TextMuted40">#66E9E9ED</Color>`
     `<Color x:Key="ProgressTrackOnCover">#59000000</Color>`
  2. Em `Styles.xaml`, trocar TODA ocorrencia de `OpenSansRegular` por `InterRegular` e de
     `OpenSansSemibold` por `InterMedium` (sao os setters de estilo implicito).
  3. Substituir TODA ocorrencia de `#E53E3E` (grep aponta LibraryPage.xaml, ReaderPage.xaml 2x,
     SettingsOverlay.xaml) por `{StaticResource ColorDanger}`.
- **NAO FACA:** nao renomear tokens existentes; nao trocar fontes dentro de
  `Resources/Raw/`.
- **Criterio de sucesso:** o comando do **DoD 2** do CONTEXT.md (copiar literal), E
  `test "$(grep -c 'OpenSansRegular' src/TranslateReader/Resources/Styles/Styles.xaml)" -eq 0`
- **Status:** completed

### T-3: Library desktop — sidebar, top bar e hero
- **Files:** `Pages/LibraryPage.xaml`, `Pages/LibraryPage.xaml.cs`, `PageModels/LibraryPageModel.cs`
- **Spec:** PIXEL-SPEC secoes "Library — sidebar", "Library — top bar", "Library — hero".
- **Passos:**
  1. Sidebar: `WidthRequest` 240 -> **232**; `Padding="16,20"`; logo vira row com chip
     32x32 (Border r8, Stroke ColorAccent) contendo icone translate `&#xE4A2;` 18 ColorAccent +
     "TranslateReader" 16 InterMedium `CharacterSpacing="-0.16"`. Nav items: altura 40,
     `Padding="10,9"`, icone + texto 14 (Biblioteca: `PhosphorFill` `&#xE758;`; Recentes:
     `Phosphor` `&#xE1A0;`); estado ativo = bg `AccentTint10` + icone/texto `ColorAccent`
     (trocar os DataTriggers atuais de `Accent900` para `AccentTint10` e adicionar setter de
     TextColor). Model card: seguir a secao (icone cpu `&#xE610;` 15 + "Modelo de traducao" 12
     Neutral400; nome 13 ColorText SEM bold; status 11 Neutral600).
  2. Top bar: titulo "Biblioteca" 20 InterMedium `CharacterSpacing="-0.3"` e `BookCountLabel`
     NA MESMA LINHA (HorizontalStackLayout, baseline; count 12 Neutral600) — nao empilhado.
     Busca: Border 260x35 (mobile mantem OnIdiom), r8, icone `&#xE30C;` 15 Neutral500 a
     esquerda (grid interno: icone coluna 0 com Margin="10,0,6,0", Entry coluna 1), Entry 13.
     Toggle segmented `GridToggleButton`/`ListToggleButton`: container Border 80x30 hairline
     r8 com Grid 2 colunas iguais; cada celula = Border transparente com icone 15
     (`&#xE464;` grid / `&#xE5A2;` list); celula ativa: Border interno Stroke ColorAccent +
     icone ColorAccent (bind em `IsListView`); TapGestureRecognizer ->
     `ShowGridViewCommand`/`ShowListViewCommand`. Chip idioma: adicionar icone globe
     `&#xE28C;` 15 antes do texto, altura 34, texto 14 InterMedium. `ImportButton` (novo, no
     lugar do ToolbarItem — REMOVER o bloco `<ContentPage.ToolbarItems>` inteiro): Border 34h
     r8 Stroke ColorAccent, icone `&#xE3D4;` 15 + "Importar" 14 InterMedium, ambos
     ColorAccent, TapGestureRecognizer -> `ImportBookCommand`.
  3. `LibraryPageModel`: adicionar `[ObservableProperty] bool isListView` e 2 RelayCommands:
     `ShowGridView()` (`IsListView = false`) e `ShowListView()` (`IsListView = true`).
  4. Hero: capa 52x76 -> **56x84** com "lombada" (BoxView 3w ColorAccent opacity 0.5 encostada
     na esquerda, dentro do Border da capa); label "CONTINUE LENDO" 10 `CharacterSpacing="1"`;
     titulo 17 InterRegular (NAO bold); meta 12 Neutral500; progress `HeightRequest="3"`
     track Neutral900; gap 20 entre colunas (`ColumnSpacing="20"`); Padding 16; botao
     Continuar: icone arrow-right `&#xE06C;` 15 apos o texto 14 InterMedium, Stroke
     ColorAccent (ja e outline — conferir r8).
- **NAO FACA:** nao remover `SearchQuery`/bindings existentes; nao mexer no grid de capas
  ainda (T-4); nao criar rota nova.
- **Criterio de sucesso:** o comando do **DoD 3** do CONTEXT.md, E
  `M=src/TranslateReader/PageModels/LibraryPageModel.cs; grep -q 'IsListView\|isListView' "$M" && grep -q 'ShowGridView' "$M" && grep -q 'ShowListView' "$M"`
- **Status:** completed

### T-4: Library desktop — grid de capas, LIST VIEW e span adaptativo
- **Files:** `Pages/LibraryPage.xaml`, `Pages/LibraryPage.xaml.cs`
- **Spec:** PIXEL-SPEC secoes "Library — grid de capas", "Library — LIST VIEW";
  screenshot `design/screenshots/desktop-library-list.jpg`.
- **Passos:**
  1. Card do grid (template existente de `BooksCollection`): capa r8 -> **r6**, stroke
     hairline Neutral800, `Padding="12,14"` interno; ADICIONAR dentro do topo da capa: titulo
     14 InterMedium ColorText (MaxLines 2) + autor 10 `TextMuted55`; lombada = BoxView 4w
     ColorAccent opacity 0.35 na borda esquerda; badge EPUB 9 `CharacterSpacing="0.72"`
     `TextMuted40`; progress track `ProgressTrackOnCover` (hoje Transparent); botao ⋮ novo:
     Border 28x28 r6 bg `CoverScrim` canto superior direito (Margin="0,6,6,0"), Label
     `&#xE208;` 16, TapGestureRecognizer com handler `OnCardMenuTapped` no code-behind
     chamando `FlyoutBase.ShowAttachedFlyout((BindableObject)sender)` — o `MenuFlyout` ja
     anexado ao card permanece como esta. Labels abaixo da capa: titulo 12 (InterRegular,
     remover bold), autor 11 Neutral600.
  2. `BooksCollection` ganha `IsVisible="{Binding IsListView, Converter={StaticResource InvertedBoolConverter}}"`
     (se o converter nao existir no projeto, usar DataTrigger — verificar
     `Utilities/` e `App.xaml` antes; existir = usar o existente).
  3. Criar `BooksListCollection` (CollectionView irma, `IsVisible="{Binding IsListView}"`,
     mesma `ItemsSource={Binding Books}`, LinearItemsLayout vertical): row = Grid 84h r10
     `Padding="10,12"` `ColumnSpacing="16"`: capa 40x60 gradiente r3; titulo 14 + autor 12
     Neutral600; direita: mini progress 96x3 (track Neutral900) + label
     `{Binding ProgressPercentage, StringFormat='{0:0}%'}` 11 Neutral500 + botao ⋮ 30x30 r8.
     Mesmo `MenuFlyout` (copiar o bloco `FlyoutBase.ContextFlyout` do card) + mesmo
     TapGestureRecognizer de abrir livro.
  4. Span adaptativo: no code-behind, handler de `SizeChanged` da pagina (assinar no
     construtor — pagina e raiz da subscription, nao precisa de unsubscribe) que faz:
     `int span = Math.Max(3, (int)((BooksCollection.Width + 20) / 187));` e aplica em
     `((GridItemsLayout)BooksCollection.ItemsLayout).Span` SO se mudou. Guardar `187` como
     `const double`. No idiom Phone nao aplicar (fica 3).
- **NAO FACA:** nao trocar `ItemsLayout` por lista em runtime (2 CollectionViews, D-...-4);
  nao duplicar os 2 `MenuFlyoutItem` com Commands diferentes dos atuais; nao esquecer
  `x:DataType="models:BookSummary"` no template novo.
- **Criterio de sucesso:** comandos dos **DoD 4 e DoD 5** do CONTEXT.md (ambos).
- **Status:** completed

### T-5: Library mobile — header compacto, busca cheia, hero compacto
- **Files:** `Pages/LibraryPage.xaml`
- **Spec:** PIXEL-SPEC secao "MOBILE / Library mobile".
- **Passos:**
  1. Via `OnIdiom` (mesmo padrao ja usado): no Default (mobile), a top bar vira o header do
     mockup mobile: chip logo 30x30 (Border r8 Stroke ColorAccent, icone `&#xE4A2;` 16) +
     "Biblioteca" 17 + count 11 inline + chip "PT" (usar `TargetLanguageChip` existente com
     36x34, texto 11 InterMedium — truncar para sigla e aceitavel manter o binding atual) +
     botao `+` 34x34 (o `ImportButton` de T-3 com OnIdiom: no mobile mostra so o icone plus
     17, sem texto).
  2. Busca no mobile: largura cheia (`WidthRequest` OnIdiom Default=-1) numa linha PROPRIA
     abaixo do header, altura 38, r10, `Padding="34,6,10,6"` com icone.
  3. Hero no mobile: altura ~90, capa 44x66, SEM linha autor/%, SEM botao Continuar — em vez
     disso icone arrow-right 17 ColorAccent na direita (OnIdiom IsVisible no botao e no
     icone); card inteiro clicavel (TapGestureRecognizer ja existe no Border? se nao, mover o
     TapGestureRecognizer do botao para o Border raiz do hero no idiom Default).
  4. Grid mobile: conferir `Span=3` e ajustar spacing OnIdiom: Default
     `HorizontalItemSpacing=14, VerticalItemSpacing=18` / Desktop `20/24` (valores do
     PIXEL-SPEC; hoje 14/18 fixo — Desktop muda).
- **NAO FACA:** nao criar segunda pagina/layout duplicado — tudo por OnIdiom no MESMO XAML;
  nao esconder a sidebar de outro jeito (ja funciona).
- **Criterio de sucesso:**
  `X=src/TranslateReader/Pages/LibraryPage.xaml; test "$(grep -c 'OnIdiom' "$X")" -ge 8 && grep -q 'E4A2' "$X" && dotnet build src/TranslateReader/TranslateReader.csproj -f net10.0-windows10.0.19041.0 -c Release --nologo -v q`
- **Status:** completed

### T-6: Reader — subtitulo, footer do mockup, TOC restyle
- **Files:** `Pages/ReaderPage.xaml`, `Pages/ReaderPage.xaml.cs`, `PageModels/ReaderPageModel.cs`
- **Spec:** PIXEL-SPEC secoes "Reader — top bar", "Reader — painel TOC", "Reader — footer".
- **Passos:**
  1. `ReaderPageModel`: adicionar `[ObservableProperty] string chapterSubtitle = "";`
     atualizado no MESMO ponto onde o capitulo atual muda (procurar onde
     `CurrentChapterIndex`/capitulo e setado apos `LoadCurrentChapterAsync`):
     desktop-formato `"Capítulo {i+1} de {total} — {Book.Author}"`; como o PageModel nao
     conhece idiom, gerar SEM o autor quando `DeviceInfo.Idiom == DeviceIdiom.Phone`
     (`"Cap. {i+1} de {total}"`). Import ja disponivel em MAUI (`Microsoft.Maui.Devices`).
  2. `Shell.TitleView`: substituir o Label unico por VerticalStackLayout: titulo 14
     InterRegular 1 linha + `ChapterSubtitleLabel` (`Text="{Binding ChapterSubtitle}"`) 11
     Neutral600. Botoes: `TocButton` texto -> `&#xE2F0;` FontFamily Phosphor 18; "Aa" fica
     texto 15 InterMedium; engrenagem -> `&#xE272;` 18. Botoes 36x36 (hoje 44 — mockup 36;
     manter HeightRequest 44 como touch target e visual 36 e aceitavel SOMENTE se mantiver o
     glyph 18 — preferir 36x36 real com margem).
  3. Footer: envolver o Grid inferior num Border `x:Name="ReaderFooter"` bg ColorBg com
     hairline superior (BoxView 1px ColorDivider no topo), `Padding="20,10"`, 54h; centro =
     VerticalStackLayout: `PageIndicatorLabel` (formato novo no code-behind:
     `$"Página {p} / {t} · Capítulo {c} de {n}"` — linha ~466) 12 Neutral500 + ProgressBar
     `x:Name="PageProgressBar"` 200x2 (track Neutral900, fill ColorAccent; atualizar
     Progress no mesmo ponto do code-behind com `(double)(p+1)/t`). `NextButton`: 34h Border
     outline hairline r8, texto "Próximo" 14 InterMedium + caret-right `&#xE13A;` 14;
     `PreviousButton` espelhado com caret-left `&#xE138;`. MANTER os 3 x:Name e handlers.
  4. TOC: item ativo (VisualState Selected) bg `Accent800` -> **`AccentTint10`** + adicionar
     Setter de TextColor ColorAccent no Label do titulo (usar DataTrigger no Border/Label se
     o VisualState nao alcancar o Label — padrao mais simples: trocar o Setter de
     BackgroundColor e adicionar um segundo VisualState Setter via TargetName NAO e
     suportado; entao: Border Selected bg AccentTint10 e aceitar titulo ColorText como
     fallback SE TextColor por estado nao for viavel sem code-behind — registrar em SUMMARY
     qual dos dois ficou). Numero 11 Neutral600, titulo 13, item `Padding="10,8"`, header
     "Capítulos" 13 InterMedium `CharacterSpacing="1"` Neutral600.
- **NAO FACA:** nao mexer em `ContentWebView`, `Resources/Raw/`, `IReadingManager`; nao
  esquecer que `PreviousButton/NextButton/PageIndicatorLabel` tem visibilidade controlada no
  code-behind — preservar a logica, so mudar visual/formato.
- **Criterio de sucesso:** comando do **DoD 6** do CONTEXT.md.
- **Status:** completed

### T-7: SettingsOverlay — painel 380, cards de tema, segmented, lista de modelos
- **Files:** `Pages/Controls/SettingsOverlay.xaml`, `Pages/Controls/SettingsOverlay.xaml.cs`
- **Spec:** PIXEL-SPEC secoes "Settings — painel desktop" e "Reader mobile" (sheet).
- **Passos:**
  1. Panel Border: `WidthRequest` Desktop 400 -> **380**; header: titulo 17 InterRegular,
     botao fechar `&#xE4F6;` 17 em 36x36 r8; corpo gap 22; section headers 13 InterMedium
     `CharacterSpacing="1"` Neutral600 (ja uppercase via TextTransform — manter).
  2. Cards de tema (MANTER x:Name e handlers `LightThemeButton` etc.): de pill p/ card —
     largura igual 1/3 (Grid 3 colunas gap 10), altura 67 (59 no mobile via OnIdiom), r10,
     conteudo vertical: "Aa" 17 + label 12 ("Claro"/"Escuro"/"Sépia" SEM emoji); cores
     `Reading*Bg/Text` como hoje; selecionado = BorderColor ColorAccent (logica de selecao do
     code-behind permanece — so garantir que o nao-selecionado usa hairline ColorDivider, nao
     transparente).
  3. Modo de leitura: virar segmented — container Border hairline r8 35h com Grid 2 colunas;
     ORDEM NO XAML: `PaginatedModeButton` PRIMEIRO (icone book-open `&#xE0E6;` 15 +
     "Paginado" 13), depois `ScrollModeButton` (arrows-vertical `&#xEB04;` 15 + "Rolagem" 13).
     Ativo = texto+icone ColorAccent (code-behind `UpdateReadingModeButtonBorders` ajustar
     para setar TextColor em vez de BorderColor).
  4. Labels de campo (Fonte, Tamanho da fonte, Espacamentos, Idiomas): 14 -> **12** com cor
     `TextMuted70`; labels de VALOR dos 4 sliders (`FontSizeLabel` etc.): 12 **ColorAccent**.
     Pickers: envolver em Border 36h hairline r8 (padrao do popup atual).
  5. Modelos: substituir o `ScrollView Orientation="Horizontal"` por VerticalStackLayout
     `x:Name="ModelsList"` (gap 8) com 4 rows; cada row = Border 53h r8 hairline
     `Padding="12,10"` MANTENDO os x:Name (`GemmaModelButton` etc. — o x:Name migra do
     Button para o Border novo) e os handlers (`OnGemmaClicked` -> TapGestureRecognizer
     `Tapped="OnGemmaClicked"`; assinatura `(object?, EventArgs)` ja e compativel). Conteudo
     da row: radio (Label `&#xE18A;` Phosphor 16 Neutral500; selecionado `&#xE184;`
     PhosphorFill ColorAccent — code-behind `UpdateModelButtonBorders` troca glyph/família e
     bg `AccentTint08` + Stroke ColorAccent), nome 13, filename 11 Neutral500 abaixo,
     tamanho 11 Neutral600 a direita. Filename/tamanho: usar os REAIS lidos de
     `src/TranslateReader.Core` (procurar o registro de modelos em
     `Business/Managers/TranslationManager.cs` — SO LEITURA) e escrever literais no XAML.
  6. Status do modelo: icone circle-dashed `&#xE602;` 14 + `ModelStatusLabel` 12 Neutral600.
     `DeleteModelButton`: bg ColorDanger, TextColor ColorBg (feito em T-2 o token; aqui o
     visual). Atribuicao Tencent intacta.
  7. Mobile (OnIdiom Default): handle de arrasto novo — BoxView 36x4 r2 Neutral700 centrado
     no topo do sheet; corner radius topo 16 -> **18**.
- **NAO FACA:** nao remover nenhum dos 18 x:Name nem os 3 eventos publicos; nao tocar nos
  valores/logica de settings (so visual); nao esquecer que `Color.FromArgb` no code-behind
  vira leitura de resource.
- **Criterio de sucesso:** comando do **DoD 7** do CONTEXT.md.
- **Status:** completed

### T-8: TranslateBookPopup — 440w, banner depois dos pickers, botoes outline
- **Files:** `Pages/Controls/TranslateBookPopup.xaml`
- **Spec:** PIXEL-SPEC secao "Library — modal Traduzir livro".
- **Passos:**
  1. Border raiz: `WidthRequest` 340 -> **440**; r14 mantem; Padding 24 -> 12+conteudo
     conforme spec (header/gaps ~8-12; nao precisa bater 11.2 exato — usar 12).
  2. MOVER o bloco `OfflineBanner` para DEPOIS do Grid dos pickers (antes dos botoes).
  3. Banner: bg `Accent900` -> **`ColorBg`**, Stroke hairline ColorDivider, icone
     shield-check `&#xE40C;` 15 Neutral500, texto 12 Neutral500 (nao Accent200).
  4. Book row: capa 34x51 com lombada 2px; `BookMetaLabel` formatos: titulo 13 ColorText,
     meta 11 Neutral500 (`Author · N capítulos` — o code-behind ja monta; so estilo).
  5. Pickers: seta entre eles vira icone arrow-right `&#xE06C;` 15 ColorAccent; labels 12
     `TextMuted70`.
  6. Botoes 29-34h outline: "Cancelar" Border hairline + texto ColorText; "Traduzir" Border
     ColorAccent + icone translate `&#xE4A2;` 15 + texto, ambos ColorAccent, bg
     transparente (HOJE e solido — trocar). Manter `Clicked`/handlers.
- **NAO FACA:** nao mudar o ctor nem o contrato de retorno `(source, target)`; nao renomear
  `BookMetaLabel`/`OfflineBanner`/`SourcePicker`/`TargetPicker`.
- **Criterio de sucesso:** comando do **DoD 8** do CONTEXT.md.
- **Status:** completed

### T-9: Testes estruturais PixelSpecTests + atualizacao do DesignSystemTests
- **Files:** `test/TranslateReader.Tests/PixelSpecTests.cs` (novo),
  `test/TranslateReader.Tests/DesignSystemTests.cs`
- **Passos:**
  1. Criar `PixelSpecTests.cs` no MESMO padrao de leitura de arquivos do
     `DesignSystemTests.cs` (le XAML/cs do disco via caminho relativo ao repo — copiar o
     helper de resolucao de caminho existente). 10 `[Fact]` com EXATAMENTE estes nomes:
     - `DesignTokens_ExposeThePixelSpecExtensions` — os 9 tokens novos existem no
       DesignTokens.xaml e `#E08A8A` esta la.
     - `Fonts_InterAndPhosphorAreRegistered` — os 4 TTF existem em Resources/Fonts e os 4
       aliases estao no MauiProgram.cs.
     - `Chrome_UsesNoLegacyDangerRed` — `#E53E3E` ausente em todos os .xaml/.xaml.cs de
       `src/TranslateReader/Pages/` e em `PageModels/`.
     - `LibraryPage_HasTheListViewAndToggle` — `BooksListCollection`, `GridToggleButton`,
       `ListToggleButton` no XAML; `IsListView`, `ShowGridView`, `ShowListView` no PageModel.
     - `LibraryPage_ImportButtonLivesInTheTopBar` — `ImportButton` presente e `<ToolbarItem`
       ausente.
     - `LibraryPage_GridSpanIsAdaptive` — `SizeChanged` e `187` presentes no code-behind.
     - `ReaderPage_HasChapterSubtitleAndStyledFooter` — `ChapterSubtitleLabel`,
       `ReaderFooter`, `PageProgressBar` no XAML; `ChapterSubtitle` no PageModel.
     - `SettingsOverlay_ModelsAreAVerticalRadioList` — `ModelsList` presente,
       `Orientation="Horizontal"` ausente, 4 x:Name de modelo presentes.
     - `SettingsOverlay_UsesPhosphorGlyphsNotAsciiArt` — `FontFamily="Phosphor` presente no
       SettingsOverlay.xaml e nenhum dos chars `☀`/`☾`/`☕`/`✕` presente.
     - `TranslateBookPopup_BannerFollowsThePickers` — indice da linha de `OfflineBanner` >
       indice da linha de `SourcePicker` no arquivo.
  2. Em `DesignSystemTests.cs`: no teste `RedesignedXaml_HasNoLegacyChromeHex`, MOVER
     `#E53E3E` da lista de permitidos para a denylist (D-...-5). Rodar a suite; se algum
     teste legado falhar por causa das mudancas de T-2..T-8 (ex.: hex, estrutura), corrigir o
     APP para o valor da spec — so ajustar o TESTE se ele contradisser a spec (registrar no
     SUMMARY qual foi).
  3. Rodar a suite completa e o build (DoD 9 e DoD 10).
- **NAO FACA:** nao usar `[Theory]`/`[InlineData]` (piso conta com `[Fact]`); nao deletar
  nem renomear teste existente; nao mockar concretos.
- **Criterio de sucesso:** comandos dos **DoD 9, DoD 10 e DoD 11** do CONTEXT.md (os tres).
- **Status:** pending

---

## Encerramento da phase (apos T-9)
1. Rodar os 11 DoD do CONTEXT.md em sequencia; todos exit 0.
2. Escrever `SUMMARY.md` da phase (o que mudou por task, deltas conscientes, BLOCKED se houver).
3. Abrir PR de `feat/pixel-perfect` para a branch alvo do momento (se o PR #20 de
   `feat/app-redesign` ja tiver sido mergeado, alvo = `main`; senao, alvo =
   `feat/app-redesign`), corpo com: DoD 1-11 com resultado, secao "Deferred to PR review" do
   CONTEXT.md copiada, e os screenshots de `design/screenshots/` referenciados.
