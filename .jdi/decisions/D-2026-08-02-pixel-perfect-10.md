D-2026-08-02-pixel-perfect-10 (2026-08-02): Fix-round pos-convergencia (iter 2) — 8 bugs reportados
pelo usuario em teste manual, LOCKED.
O usuario testou o app ao vivo apos o loop convergir (APPROVED_WITH_WARNINGS, iter 1) e reportou 8
defeitos visuais/funcionais que nem o DoD automatico nem o reviewer conseguiam pegar sem rodar a
UI de verdade — exatamente o gap que `CONTEXT.md`'s "Deferred to PR review" ja previa. Investigacao
direta (leitura de codigo, nao suposicao) antes de despachar o doer:

1. **Borda errada (sidebar da Library E painel TOC do Reader)** — CAUSA RAIZ: `Border.StrokeThickness`
   em MAUI e `double`, nao `Thickness` (confirmado lendo o source oficial do MAUI). O XAML tinha
   `StrokeThickness="0,0,1,0"` (sintaxe CSS-like, invalida) em `LibraryPage.xaml:22` e
   `ReaderPage.xaml:183`. `double.TryParse("0,0,1,0", NumberStyles.Any, InvariantCulture)` NAO falha
   — interpreta as virgulas como separador de milhar e retorna `10` (testado e confirmado nesta
   sessao). Resultado real: borda solida de 10px nos 4 lados, em vez do hairline de 1px so na borda
   direita que o mockup pede. Token tambem estava errado: PIXEL-SPEC pede `ColorDivider` para os
   dois casos, o XAML usava `Neutral800` (solido, mais escuro — reforca o efeito "borda diferente").
2. **Campo de busca ilegivel / placeholder cortado** — CAUSA RAIZ: o estilo implicito `Entry` em
   `Styles.xaml:118` fixa `MinimumHeightRequest="44"`. `SearchEntry` em `LibraryPage.xaml` sobrescreve
   `FontSize` mas NAO `MinimumHeightRequest`; o `Border` que o contem tem `HeightRequest` 35
   (Desktop)/38 (mobile) — menor que o minimo forcado de 44 do `Entry` filho. O filho exige mais
   altura do que o pai permite → conteudo cortado verticalmente.
3. **"Biblioteca" duplicado** — CAUSA RAIZ: `LibraryPage.xaml` nao define `Shell.TitleView` nem
   `Shell.NavBarIsVisible="False"`; a barra de navegacao NATIVA do Shell mostra o `ContentPage.Title`
   ("Biblioteca") ALEM do `Label Text="Biblioteca"` que o proprio topo customizado da pagina ja
   renderiza. `ReaderPage.xaml` nao tem esse problema porque define um `Shell.TitleView` completo,
   substituindo o titulo nativo.
4. **Botoes de configuracoes/traducao cortados no Reader** — CAUSA RAIZ: o estilo implicito `Button`
   em `Styles.xaml:32` fixa `Padding="14,10"`. Os botoes `TocButton`/Aa (`OnTranslateButtonClicked`)/
   engrenagem no `Shell.TitleView` de `ReaderPage.xaml` foram encolhidos para 36x36
   (`WidthRequest`/`HeightRequest`/`Minimum*Request`) SEM sobrescrever `Padding` — o padding de
   14,10 herdado deixa so 8x16px de area util pro glifo/texto dentro de uma caixa de 36x36,
   cortando o conteudo visualmente.
5. **Fonte OpenDyslexic nao funciona** — CAUSA RAIZ: e uma fonte de CONTEUDO da area de leitura
   (aplicada via CSS dentro do WebView, nao fonte nativa MAUI). "Georgia"/"serif"/"sans-serif"/
   "monospace" resolvem para fontes de sistema do navegador; "OpenDyslexic" NAO e instalada em
   nenhuma plataforma-alvo por padrao e o projeto nao empacota o arquivo de fonte nem um
   `@font-face` em `Resources/Raw/wwwroot`. **FORA DE ESCOPO desta phase**: corrigir exigiria tocar
   `Resources/Raw/**`, proibido por D-...-1 (fronteira Client-only, Core+Raw intocados, provada pelo
   DoD 11). Registrado como todo para phase futura; a opcao permanece no picker (removê-la seria
   regressao funcional).
6. **Modo de leitura "errado" vs mockup**, **7. layout de Settings nao bate com o modelo**,
   **8. animacao de traducao automatica nao aparece** — sem prova de causa raiz unica encontrada por
   leitura estatica de codigo (a logica de `UpdateReadingModeButtonBorders`/`FontImageSource` parece
   estruturalmente correta no papel; `IsTranslationModeActive` existe e esta ligado a uma BoxView de
   3px que pode simplesmente ser sutil demais). Vao para o doer como investigacao dirigida com
   hipoteses concretas (ver PLAN.md T-10, itens F/G/H), nao como "olhe e veja o que acha".

Escopo do fix-round: itens 1-4 tem fix mecanico e preciso (aplicado pelo doer sem investigacao
adicional); item 5 e so registro de todo (nenhum arquivo de Resources/Raw ou Core e tocado); itens
6-8 exigem investigacao dirigida e podem terminar BLOCKED se a causa raiz nao for confirmavel sem
teste visual ao vivo (que nem doer nem reviewer tem capacidade de fazer nesta sessao) — nesse caso
o doer documenta a hipotese tentada e o que faria diferente com acesso a um screenshot real.
