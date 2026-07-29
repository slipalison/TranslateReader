# Todos — scope creep registrado

Append-only. Itens fora do escopo de uma phase discutida, candidatos a phase futura ou acao
manual do usuario. Nunca vira phase automaticamente — precisa ser promovido via
`/jdi-add-phase`.

## De `ci-seguranca` (2026-07-28)

- Build + testes de Android/iOS no pipeline de CI — exigiria workload MAUI mobile instalado no
  runner (e possivelmente emulador/simulador). Nao pedido explicitamente pelo card.
- Assinatura e publicacao em lojas (Google Play Console, Apple App Store Connect/TestFlight) no
  workflow de release — exige certificados/secrets inexistentes hoje.
- `zizmor` (linter estatico de workflows do GitHub Actions) — reforco opcional de rigor, nao
  obrigatorio no card; considerar se quiser elevar ainda mais a regua de supply-chain.
- SonarQube self-hosted (servidor proprio) — descartado a favor do SonarQube Cloud
  (D-2026-07-28-ci-seguranca-6); revisitar so se o projeto ganhar backend/infra propria.

## De `readme` (2026-07-29)

- **[SEGURANCA] Extracao de imagem de EPUB nao tem containment de path (zip-slip) nem bound de
  tamanho descomprimido (zip-bomb).** Achado colateral do review da phase `readme` (B-1): o README
  afirmava esse controle, e a verificacao mostrou que ele nao existe. Nao e defeito de doc — e
  divida de codigo real contra `.claude/rules/csharp.md` secao 4 ("EPUB files are untrusted input.
  Extract zip entries defensively: reject entry paths that escape the target directory ... Bound
  decompressed sizes").

  Evidencia:
  - `src/TranslateReader.Core/Business/Managers/ReadingManager.cs:59-60` monta o caminho de saida
    direto da entrada nao confiavel:
    `Path.Combine(imagesDir, relativePath.Replace('/', Path.DirectorySeparatorChar))` seguido de
    `fileUtility.WriteFileAsync(outputPath, content)`. `relativePath` vem de
    `epub.Content.Images.Local` (`ParsingEngine.cs:62-64`), ou seja, dos caminhos internos do EPUB.
  - `src/TranslateReader.Core/Utilities/FileUtility.cs:31-32` escreve sem validar:
    `Directory.CreateDirectory(Path.GetDirectoryName(filePath)!)` +
    `File.WriteAllBytesAsync(filePath, content)`. Um EPUB com entrada `../../evil.png` escreve
    fora de `imagesDir`.
  - Nao ha `Path.GetFullPath` + checagem de containment em lugar nenhum de `src/` (grep por
    `GetFullPath|ExtractToFile|ExtractToDirectory|entry.FullName` -> zero resultados), nem
    qualquer limite de bytes por entrada ou por livro (grep por `maxSize|maxBytes|uncompressed|
    sizeLimit` -> zero resultados).
  - A regra de Semgrep `translatereader-zip-slip` **nao pega este caso**: ela casa
    `Path.Combine($DEST, $ENTRY.FullName)` e `$ENTRY.ExtractToFile(...)`, e o codigo real usa uma
    variavel intermediaria (`relativePath`) vinda da API do VersOne.Epub. Ou seja, o gate de CI da
    falsa sensacao de cobertura aqui.

  Correcao esperada quando virar phase: resolver com `Path.GetFullPath` e exigir
  `StartsWith(imagesDir, StringComparison.Ordinal)` antes de escrever, rejeitando (nao sanitizando)
  o que escapar; somar um limite de bytes por entrada e por livro. Codigo legado (anterior a
  `4285f25`), entao D-2 isenta as phases atuais — mas seguranca e prioridade 1 do projeto e isso
  nao tem boundary de legado.
