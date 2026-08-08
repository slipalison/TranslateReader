D-2026-08-08-baseline-de-estilo-1 (2026-08-08): Escopo do legado = SO whitespace, num commit proprio,
provadamente semantico-zero. LOCKED.
(1) A normalizacao do codigo legado desta phase e `dotnet format whitespace` no repo inteiro — NAO o
`dotnet format` completo. Style fixes (`var` vs tipo explicito, `this.`, ordem de `using`, chaves,
expression-bodied) e analyzer fixes NAO sao aplicados retroativamente ao legado: seriam churn semantico
em ~70 arquivos que D-2 (fronteira `4285f25`) isenta explicitamente. O `.editorconfig` desta phase pode
declarar essas preferencias de style; elas so passam a valer para codigo novo/tocado.
(2) Isso mata definitivamente o warning recorrente (as violacoes WHITESPACE em `ThemeEngine.cs:12,14`,
`ReaderPage.xaml.cs:122,124`, `ThemeEngineTests.cs:12`, `TranslationManagerTests.cs:528-529`) que
apareceu em TODA REVIEW.md desde `ci-seguranca` e que `.jdi/todos/LEGACY.md:367-378` roteou para ca.
(3) ORDEM OBRIGATORIA dos commits (inverter refaz trabalho): 1) `.gitattributes` + renormalize (D-...-4),
2) `.editorconfig`, 3) `Directory.Build.props` + analyzers (D-...-2, D-...-3), 4) `dotnet format
whitespace`, 5) docs/reviewer/registry/todos (D-...-5). Commits atomicos, 1 assunto por commit.
(4) O diff da phase em `*.cs`/`*.xaml`/`*.js` tem de ser semantico-zero: `git diff --ignore-all-space
--ignore-blank-lines --ignore-cr-at-eol` contra o commit ancora VAZIO. Se o executor precisar mudar
uma linha de codigo de verdade para fazer algum gate passar, a task para com BLOCKED — nao inventa.
