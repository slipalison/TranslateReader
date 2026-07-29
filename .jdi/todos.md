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
    `Path.Combine($DEST, $ENTRY.FullName)` e `$ENTRY.ExtractToFile(...)`, e o codigo real nao
    passa por nenhum dos dois. Ou seja, o gate de CI da falsa sensacao de cobertura aqui.
  - **CORRECAO DO DIAGNOSTICO (2026-07-29, verify round 3):** a redacao anterior dizia que a
    regra falha por causa de uma "variavel intermediaria (`relativePath`)" — isso e FRACO e
    enganoso. O reviewer provou com probe de 4 casos que a regra exige o **acesso sintatico a
    `.FullName`**; como o projeto extrai via VersOne.Epub e nunca toca `ZipArchiveEntry`, a regra
    nao pode disparar no caminho real em nenhuma forma que ele venha a assumir. Quem for
    consertar deve reescrever a regra para o padrao real, nao normalizar a variavel.
    Fonte canonica desta analise: `D-2026-07-29-epub-zip-slip-1` em `.jdi/DECISIONS.md`.
  - **Este item virou phase:** `epub-zip-slip` (posicao 11 no ROADMAP), com escopo de DUAS
    entregas — o containment/bound no codigo E a correcao da regra. Entregar so a primeira deixa
    o defeito invisivel para o CI na proxima regressao.

  Correcao esperada quando virar phase: resolver com `Path.GetFullPath` e exigir
  `StartsWith(imagesDir, StringComparison.Ordinal)` antes de escrever, rejeitando (nao sanitizando)
  o que escapar; somar um limite de bytes por entrada e por livro. Codigo legado (anterior a
  `4285f25`), entao D-2 isenta as phases atuais — mas seguranca e prioridade 1 do projeto e isso
  nao tem boundary de legado.

- [UI/CODIGO] Controles que a UI promete e o codigo nao honra (achado adjacente do verify round 3
  da phase `readme` — mesma classe de defeito que a phase passou 3 rounds tirando do README, mas
  na tela):
  - **Model picker morto:** `TranslationModelName` e escrito por 3 botoes vivos no painel de
    configuracoes, mas nao e consumido por ninguem — `DownloadModelIfNeededAsync` usa
    `DefaultModel` incondicionalmente e `GetModelPath()` devolve o primeiro `*.gguf` por
    enumeracao cega de nome. O usuario escolhe um modelo e o app ignora.
  - **`IsModelAvailable` e flag de sessao:** escrito so em `ReaderPageModel.cs:275` e `:300`,
    nunca consulta o disco, e o PageModel e `AddTransient` — reabrir o app esconde o botao
    "Excluir modelo" mesmo com 1,6 GB em disco.
  - **Download de modelo nao e retomavel:** `ModelAccess` usa `FileMode.Create` (trunca) e nao
    manda header `Range`; um `.tmp` interrompido nao conta como disponivel, entao rebaixa os
    1,6 GB do zero.
