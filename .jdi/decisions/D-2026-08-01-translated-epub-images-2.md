D-2026-08-01-translated-epub-images-2 (causa raiz reconfirmada no HEAD atual + forma da correcao
LOCKED): Reconfirmado por leitura de codigo nesta sessao (nao herdado por hipotese) que o estado de
`main` citado em `D-2026-08-01-translated-epub-images-1` nao mudou nas linhas relevantes:
`ParsingEngine.cs:47-57` (`ExtractChapterContentAsync`, chama `RewriteImagePaths` + `InlineCssLinks`
incondicionalmente), `ParsingEngine.cs:239-253` (`RewriteImagePaths`, 3 regexes: `<img src>`,
`<image xlink:href>`, `<image href>`), `ParsingEngine.cs:255-269` (`ReplaceImageRef`, monta
`bookDir = Path.GetFileName(imagesDirectory)` e `imageUrl = $"https://epub-images/{bookDir}/{resolvedPath}"`),
`TranslationManager.cs:123,193,242` (3 chamadas com `imagesDirectory: string.Empty`).

ACHADO NOVO nesta sessao (nao estava em `D-...-1`): `ReplaceImageRef` (`ParsingEngine.cs:258`) tem
uma guarda `if (src.StartsWith("data:", ...) || src.StartsWith("http", ...)) return match.Value;` —
qualquer `src` que ja comece com `http`/`https` e devolvido INALTERADO. Consequencia: uma vez que
`https://epub-images//OEBPS/...` e gravado no artefato traduzido, ele fica QUEBRADO PARA SEMPRE —
mesmo reabrindo o livro traduzido pela biblioteca (com um `BookId` novo e um `imagesDir` CORRETO em
`ReadingManager.LoadChapterContentAsync`), a segunda passagem de `RewriteImagePaths` sobre o MESMO
HTML ve um `src` que ja comeca com `https://` e pula a reescrita — a URL nunca se autocorrige. Isso
explica por que o defeito e permanente e nao um transitorio de importacao, e por que a correcao TEM
de acontecer no momento da GERACAO (`CreateTranslatedEpubAsync`/`RebuildAllTranslatedChaptersAsync`),
nunca so na leitura.

FRONTEIRAS A DECIDIR (pedido explicito do card): `ExtractChapterContentAsync` mistura produzir HTML
para o WebView (precisa de URL do virtual host) e produzir HTML para gravar no EPUB (precisa dos
caminhos ORIGINAIS, relativos, validos dentro do zip). Forma da correcao LOCKED: novo enum
`ChapterContentPurpose { Display, Export }` em `src/TranslateReader.Core/Models/ChapterContentPurpose.cs`
(mesmo padrao ja usado por `Models/ReadingMode.cs` para enum de dominio) vira um parametro OBRIGATORIO
(sem valor default) de `IParsingEngine.ExtractChapterContentAsync`.
Alternativas pesadas e REJEITADAS:
(a) Metodo novo dedicado (ex.: `ExtractChapterContentForExportAsync`) — REJEITADO porque
`IParsingEngine` JA esta em 6 operacoes (excede "3-5 operacoes por contrato (ideal)" do CLAUDE.md;
legado ja registrado e explicitamente NAO corrigido sem fase dedicada, ver
D-2026-07-30-the-method-refactor-2(b)/D-2026-07-31-conversion-performance-10(b)). Adicionar uma 7a
operacao nesta fase de bugfix pioraria ativamente uma violacao ja nomeada, em vez de conte-la.
(b) Reverter a reescrita DEPOIS de persistir (fazer o rewrite normalmente e desfazer antes de
escrever no zip) — REJEITADO: exigiria parsear/desfazer uma transformacao regex ja aplicada (com
risco de perda de informacao — o `src` original nao fica preservado em lugar nenhum apos a
reescrita), e reintroduziria exatamente a mesma classe de "guarda de idempotencia silenciosa" do
achado acima. Mais codigo, mais risco, nenhum ganho sobre nunca mutar.
(c) Um `bool` cru (`rewriteForDisplay: true/false`) — REJEITADO em favor do enum: um literal
`true`/`false` solto nos 4 call sites e boolean-trap classico (obriga o leitor a checar a assinatura
pra saber o que o literal significa); o enum documenta a intencao no proprio call site sem crescer a
contagem de operacoes do contrato (continua sendo 1 parametro, nao 1 operacao nova).
Contagem de operacoes de `IParsingEngine` permanece 6 (sem crescimento) apos esta fase.
