D-2026-08-01-div-paragraph-reading-4 (caminho C# morto): `HtmlUtility.ExtractParagraphs` /
`ParagraphRegex()` (unico chamador: `TranslateChapterAsync`, `TranslationManager.cs:244` —
confirmado sem chamador de UI em `src/TranslateReader/` por `git grep`, so `ITranslationManager.cs`
e `TranslationManagerTests.cs`) tem o MESMO defeito de classe (so `<p>`) e fica REMOVIDO;
`TranslateChapterAsync` passa a chamar `HtmlUtility.ExtractTextBlocks` (ja corrigido por
`div-paragraph-translation`, D-2026-08-01-div-paragraph-translation-7/8). Compativel com os 6 testes
`TranslateChapterAsync_*` existentes sem alteracao: o ramo `p|h|li` de `ExtractTextBlocks` usa o
MESMO filtro `!string.IsNullOrWhiteSpace` que `ExtractParagraphs` ja usava, e todos os corpos desses
testes so tem `<p>` — resultado identico, zero churn nos testes existentes.
REJEITADO remover `TranslateChapterAsync`/o membro de `ITranslationManager`: e contrato publico
testado (6 caracterizacoes protegendo comportamento), remocao seria decisao de API-surface alem de
"resolva o problema" do card, nao pedida nem sugerida por ninguem — trocar a extracao fecha o
defeito de classe com uma mudanca de uma linha, sem esse risco.
