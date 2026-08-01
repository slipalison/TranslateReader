D-2026-08-01-translated-epub-images-6 (pontos de vista 4 e 6 do card — CONFIRMADOS como defeitos
reais e medidos, DELIBERADAMENTE fora de escopo desta fase, registrados em `.jdi/todos/`): dois
gaps de cobertura de FORMA existem HOJE independentemente de traducao (afetam tambem livros NAO
traduzidos, sempre que `Purpose.Display` roda) — explicam "ALGUMAS imagens quebradas", nao todas,
mas por um mecanismo ORTOGONAL ao vazamento de URL do app pro artefato:

(a) Cobertura de forma dos 3 regexes de imagem — `ImgSrcRegex`, `SvgImageXlinkHrefRegex`,
`SvgImageHrefRegex` (`ParsingEngine.cs:352-359`) so casam atributo com ASPAS DUPLAS
(`\bsrc\s*=\s*""([^""]+)""` / equivalente para `xlink:href`/`href`). `<img src='...'>` (aspas
simples), `<img src=foo.png>` (sem aspas), `srcset`, `<picture><source>` e `background-image` em
`style="..."` inline NUNCA sao casados por nenhum dos 3 padroes — permanecem com o path relativo
ORIGINAL, que nao resolve contra a raiz estatica do WebView do app (quebrado NA LEITURA, dentro do
app, independente de traducao).

(b) Normalizacao de `FindImage`/`FindCss` (`ParsingEngine.cs:272-279`, `230-237`) compara so por
`OrdinalIgnoreCase` + sufixo de barra — nao decodifica `%XX` nem normaliza alem de `..`/`.`
(`NormalizePath`, `ParsingEngine.cs:287-303`). Um `src` percent-encoded
(`Images/My%20Book.png`) contra um `FilePath` de manifesto nao codificado (`Images/My Book.png`)
falha o match; `FindImage`/`FindCss` retornam `null`, `ReplaceImageRef` devolve `match.Value`
inalterado (`ParsingEngine.cs:263-264`) — mesmo efeito de (a): path original preservado.

Por que ficam FORA de escopo: (i) o card e explicitamente sobre livros TRADUZIDOS ficarem
quebrados, nao sobre robustez geral de leitura de qualquer livro; (ii) nenhum dos 3 fixtures reais
do repo (Practice/Righting/Wardley) exercita aspas simples/sem aspas nem path percent-encoded nos
testes ja escritos — corrigir sem um caso reproduzivel seria trabalho especulativo (YAGNI); (iii)
como NENHUM dos dois casos aciona `ReplaceImageRef` de fato, nenhum dos dois pode produzir
`https://epub-images/...` no artefato exportado — o texto original relativo permanece, que E valido
dentro de um EPUB portavel (nao piora o defeito desta fase, so nao o corrige para leitura no app);
(iv) a correcao de (a) sozinha exige decidir escopo de `srcset`/`<picture>`/CSS inline — trabalho de
robustez de `HtmlUtility`/`ParsingEngine` maior que este bugfix, candidato a fase propria.
Registrado em `.jdi/todos/2026-08-01-translated-epub-images.md` com os `file:line` acima.
