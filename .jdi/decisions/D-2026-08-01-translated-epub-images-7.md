D-2026-08-01-translated-epub-images-7 (ponto de vista 5 do card — livros ja gerados e quebrados,
decisao EXPLICITA pedida pelo card): NENHUMA ferramenta de migracao/reparo automatico e construida
nesta fase (YAGNI — sem admin UI, sem job em lote, sem tabela/coluna nova, sem comando novo em
nenhum Manager). Racional medido, nao presumido:
- O arquivo EPUB traduzido QUEBRADO e um artefato DERIVADO e descartavel. O livro ORIGINAL (fonte
  da traducao) nunca e mutado pelo pipeline: `CreateTranslatedEpubAsync` faz
  `File.Copy(originalFilePath, destPath, overwrite: true)` (`ParsingEngine.cs:91`) e so entao
  escreve na COPIA (`destPath`); o `originalFilePath` do livro-fonte, referenciado por
  `Book.FilePath` na biblioteca, permanece intacto e correto o tempo todo.
- `TranslationCache` (chave = `BookId` + `ChapterHRef` + hash SHA-256 do texto ORIGINAL + idiomas,
  `TranslationManager.cs:330-334`) permanece 100% valido para o livro ORIGINAL — o `BookId` do
  livro-fonte nunca muda entre corridas de traducao. Reexecutar "Traduzir" no MESMO livro original,
  DEPOIS desta fase corrigir a geracao, reconstroi um EPUB traduzido CORRETO batendo cache em
  praticamente todos os paragrafos (rapido, sem nova chamada ao LLM para o texto ja traduzido).
- A capacidade de remover o livro traduzido quebrado da biblioteca JA EXISTE hoje
  (`LibraryManager.DeleteBookAsync`, `LibraryManager.cs:60-70` — remove registro, cache, progresso,
  arquivo e diretorio de imagens) — nenhuma acao de codigo nova e necessaria para o usuario se
  livrar da copia quebrada.
Acao esperada do usuario (produto/UX, comunicacao humana, NAO codigo desta fase): apagar da
biblioteca a copia traduzida quebrada e clicar em "Traduzir" novamente no livro original — registrado
em `## Deferred to PR review` do CONTEXT.md. Nenhum item de DoD cobre esta decisao porque nao ha
propriedade de codigo POSITIVA a verificar alem de "nenhum artefato novo de migracao foi
introduzido", ja coberto pelo guard-rail de escopo de diff do DoD desta fase (D-...-4).
