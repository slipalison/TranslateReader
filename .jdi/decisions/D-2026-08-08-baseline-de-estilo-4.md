D-2026-08-08-baseline-de-estilo-4 (2026-08-08): `.gitattributes` com `* text=auto eol=lf` + um unico
`git add --renormalize .`. LOCKED.
(1) LF passa a ser o fim de linha tanto no index quanto na working tree, em todas as plataformas.
Isso mata de vez o `warning: LF will be replaced by CRLF` que aparece em praticamente todo commit
deste repo (o `core.autocrlf` da maquina deixa de mandar — `.gitattributes` tem precedencia).
Risco baixo: o index ja armazena LF hoje, entao o renormalize e quase no-op de conteudo; o
`.editorconfig` da phase declara `end_of_line = lf` para casar com isso.
(2) Binarios sao declarados explicitamente `binary` (equivalente a `-text -diff`), no minimo:
`*.ttf`, `*.epub` (as 3 fixtures em `test/TranslateReader.Tests/TestData/`), `*.png`, `*.jpg`,
`*.jpeg`, `*.ico`, `*.zip`, `*.gguf`, `*.pfx`. `*.svg` continua texto (sao 4 assets editaveis em
`src/TranslateReader/Resources/`).
(3) O renormalize e o PRIMEIRO commit da phase e vem sozinho (`chore(baseline-de-estilo): normalize
line endings`), antes do `.editorconfig` e do `dotnet format whitespace` — na ordem inversa o format
seria refeito.
(4) Prova executavel: `git ls-files --eol` sem nenhum `i/crlf` nem `i/mixed`.
(5) Nao se ativa `core.hooksPath`/`.githooks` nesta phase (nao existem no repo hoje) — fica em todos.
