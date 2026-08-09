D-2026-08-09-snippet-translation-2 (2026-08-09): Superficie da UI no WebView, ponte por raw message
e arquivo `js/snippets.js` novo, LOCKED.

TODA a UI de trecho (spans de periodo, blob de vidro, pill flutuante, hint de primeira vez, chip de
idioma) e DOM dentro do WebView, injetado em `document.body` FORA do `#_viewport` (paginado) para
nao entrar no fluxo de colunas do pager. Motivo: `backdrop-filter: blur(26px) saturate(190%)` e
`clip-path: path(...)` por linha de texto nao tem equivalente nativo em .NET MAUI. Requisito 3 do
usuario (pixel-perfect INEGOCIAVEL) so e alcancavel em CSS.

REJEITADO pill/hint em XAML nativo por cima do WebView: entregaria a tipografia Inter+Phosphor ja
pixel-perfect da phase 22, mas sem `backdrop-filter` (delta visual assumido no componente mais
visivel da feature) e com dois idiomas de UI para um componente so.

ARQUIVO NOVO `src/TranslateReader/Resources/Raw/wwwroot/js/snippets.js`, carregado em `index.html`
DEPOIS de `translation.js` (a ordem importa: `snippets.js` consome o global
`_translatableCandidates` declarado la). REJEITADO empilhar tudo dentro de `translation.js`: o
arquivo sairia de 83 para ~400 linhas misturando duas features.

CONSEQUENCIA OBRIGATORIA NO GATE: `scripts/coverage-gate.sh` hoje itera
`for name in bridge paginated scroll translation` (linha ~246) e aborta com `exit 3` se
`JS_FILES -ne 4` (linha ~262). As duas linhas passam a listar/exigir 5 arquivos
(`bridge paginated scroll snippets translation`). Piso `COVERAGE_JS_MIN` continua 85 — nao mexer.
Sem essa alteracao o gate falha e o executor nao entende por que.

PONTE JS -> C#: o botao "Traduzir trecho" vive no DOM, entao o pedido sobe pelo canal de raw
message que JA EXISTE. `ReaderPage.OnHybridMessageReceived` hoje compara `e.Message == "ready"` e
ignora o resto (`ReaderPage.xaml.cs:74-82`); passa a despachar por prefixo:

    "ready"                      -> comportamento atual, intocado
    "snip|{json}"                -> pedido de traducao de N runs contiguos
    "snip-toggle|{json}"         -> alternou original/traducao (persistir ShowingOriginal)
    "snip-remove|{json}"         -> descartou a traducao do trecho

O payload e JSON desserializado com `JsonTypeInfo` no `ReaderJsonContext` (AOT-safe, padrao ja
usado por `VisibleParagraph`/`PageInfo`), NUNCA com reflexao. C# -> JS continua por
`EvaluateJavaScriptAsync` com os argumentos serializados por `JsStr`/`JsonSerializer` — nenhuma
string derivada do livro entra em JS por concatenacao (`.claude/rules/csharp.md` §4).

`ReaderPage.xaml.cs` e EDITADO, nao criado — nenhum `.cs` NOVO nasce em `src/TranslateReader/`,
entao a guarda `COVERAGE_GUARD` do gate (exit 2) nao dispara e `.jdi/coverage-waivers.txt`
permanece com zero entradas vivas. Se o plano quiser um `.cs` novo no projeto MAUI, precisa de
waiver com decisao propria — nao ha default de herdar cegueira.
