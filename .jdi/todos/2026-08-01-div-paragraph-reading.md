- **[DoD/tooling] Os `Verify:` dos itens 2 e 3 desta phase assumem o reporter TAP do `node --test`,
  mas o Node 24 (versao pinada no CI, `setup-node`; local medido `v24.14.0`) usa o reporter `spec`
  por padrao mesmo com stdout redirecionado para arquivo. O sumario sai como `ℹ fail 0` / `ℹ pass N`,
  entao o `grep -qE "^# fail[[:space:]]+0$"` nunca casa e os dois itens reprovam com a suite 100%
  verde. Medido nesta sessao: item 2 exit 1 e item 3 exit 1 com `# fail 0` ausente; os MESMOS
  comandos com `--test-reporter=tap` acrescentado saem exit 0 (item 2 `N=6 P=6`, item 3 `B=13 P=20`,
  piso 17). A phase `coverage-90` ja tinha acertado isso — o `Verify:` do item 1 dela passa
  `--test-reporter=tap` explicitamente. Acao: todo `Verify:` futuro que parseie sumario de
  `node --test` tem de fixar o reporter (`--test-reporter=tap`), do mesmo jeito que
  `DOTNET_CLI_UI_LANGUAGE=en` e obrigatorio no `dotnet test`
  (`D-2026-08-01-div-paragraph-translation-9` regra 1). Nao corrigido aqui porque o CONTEXT.md da
  phase e imutavel para o doer.
- **[Cobertura] `TranslateChapterAsync` fica em 100% de linha e 83,3% de branch** (coverlet,
  `TestResults/cov/*/coverage.cobertura.xml`). O unico branch parcial e o `chapter?.Title`
  (`TranslationManager.cs:265`, 50% 1/2): nenhum teste exercita o caso "capitulo nao encontrado na
  lista", porque `SetupBookAndChapter` sempre devolve o capitulo. Linha NAO alterada por esta phase
  (o diff toca so a linha 244) e o irmao intocado `TranslateParagraphsAsync` tem o mesmo
  `chapter?.Title` e o mesmo branch-rate 0,8333 — e divida pre-existente, nao regressao. Fechar
  vale 1 teste (`ExtractChaptersAsync` devolvendo lista sem o href pedido).
