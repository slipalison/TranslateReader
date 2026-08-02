D-2026-08-02-app-redesign-10 (2026-08-02): Como esta phase prova que nao quebrou o app, dado que ela
e majoritariamente XAML e que o harness de teste NAO alcanca o projeto MAUI. LOCKED.
ACHADO ESTRUTURAL (verificado em `test/TranslateReader.Tests/TranslateReader.Tests.csproj`): o test
project e `net10.0` e referencia SO `src/TranslateReader.Core`. Nada em `src/TranslateReader`
(Pages, Controls, PageModels, converters) pode ser instanciado, testado ou ter cobertura de linha
medida hoje. Adicionar `ProjectReference` pro projeto do app esta REJEITADO: os TFMs dele sao
`net10.0-windows*/android/ios/maccatalyst`, um projeto de teste `net10.0` nao os referencia, e
montar um projeto de teste multi-TFM com MAUI hosting e infraestrutura nova, phase inteira propria,
desproporcional a um reskin.
Consequencias LOCKED desta phase:
(1) TODA logica nova que merece teste vai pro Core (`LibraryManager`, `TranslationManager`,
    `BookSummary`) — ver D-...-7 e D-...-9. O C# que fica no projeto do app e so wiring fino:
    binding, handler, `OnIdiom`, animacao. Nada de regra de negocio, nada de calculo, nada de
    if/else de dominio em PageModel ou code-behind. A meta de 90% (D-2/D-6) e medida e cobrada nas
    classes do Core alteradas.
(2) O wiring do app e verificado por ESTRUTURA, com um arquivo de teste novo
    `test/TranslateReader.Tests/DesignSystemTests.cs` que LE os XAML/`.xaml.cs` do disco — o mesmo
    padrao que `HybridWebViewContractTests` ja usa hoje pra assertar contrato sobre os `.js` do
    WebView (caminho relativo a `AppContext.BaseDirectory`). Ele assere: tokens exatos do mockup
    presentes; nenhum hex de chrome legado sobrando; todo `Clicked=`/`Tapped=`/`ValueChanged=`/
    `SelectedIndexChanged=`/`TextChanged=` do XAML tem metodo homonimo no code-behind irmao; toda
    raiz de `{Binding ...}`/`Path=...` das Pages resolve a um membro de PageModel/BookSummary/
    Chapter (contando as convencoes do CommunityToolkit: campo `_books` -> `Books`, metodo
    `OpenBookAsync` -> `OpenBookCommand`); os elementos estruturais do mockup existem; os overlays
    usam animacao de verdade. Motivo: sem isso, "nao quebrei o app" nesta phase seria 100%
    julgamento humano — XAML compila com binding errado e falha em silencio no runtime.
(3) Build mobile: `src/TranslateReader/TranslateReader.csproj` so inclui `net10.0-android` no
    Windows se o Android SDK existir (`Exists('$(LocalAppData)\Android\Sdk')` etc.), e o CI de hoje
    (`.github/workflows/ci.yml`) SO compila o TFM Windows — ou seja, hoje nada no repo prova que o
    app ainda compila pra Android. Esta phase adiciona um job `Build (Android)` em `ci.yml`
    (ubuntu-latest + `dotnet workload install maui-android`, o mesmo passo que `sca.yml` ja usa e
    que ja funciona no repo). Localmente o gate compila Android quando o TFM resolve e nao falha a
    phase por ausencia de SDK — a garantia duravel fica no CI, e a validacao em device real vai pra
    `## Deferred to PR review`.
