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
- **[DoD/gate] Os gates TEXTUAIS dos itens 1, 4 e 6 desta phase sao contornaveis por construcao
  — divida de gate, nao de codigo** (W-2 e W-3 do REVIEW da iter 3). Fechar por texto exigiria um
  tokenizador de JS e de C#; o que fecha a classe inteira e o gate COMPORTAMENTAL, e ele existe e
  foi provado por mutacao. Nao corrigir avulso: quem reescrever DoD de JS/C# assume.
  - Item 1 (`sed`/`grep` linha a linha sobre `translation.js`): cego a comentario de BLOCO
    multi-linha e a helper duplicado por substring. Medido: sob o mutante M-E o item 1 sai exit 0.
    Backstop provado: os itens 2 e 3 saem exit 1 sob o MESMO mutante (2 testes JS novos da iter 3).
    Sobreviventes restantes sao mutantes EQUIVALENTES (copia identica do helper; `clear` via
    `[data-original]`, marker que so o `apply` escreve e so em candidato) — zero bug de usuario.
  - Item 4 (`git grep` de `ExtractParagraphs`/`ParagraphRegex`): nao pega desvio renomeado nem
    escondido em comentario. Backstop provado por execucao: desviando `TranslationManager.cs:244`
    para um extrator so-`<p>`, `TranslateChapterAsync_ForCalibreStyleBody_TranslatesLeafDivParagraphs`
    falha (`Expected: 3 / Actual: 0`) e o `Verify:` do item 5 sai exit 1.
  - Item 6 (piso `B+1` derivado de `[Fact]`+`[InlineData]` de `main` + `comm` de metodos publicos):
    o lado HEAD vem de grep ESTATICO, entao um metodo que perde o `[Fact]` continua no arquivo e o
    `comm` nao acusa; o `Total` cai abaixo do piso e o gate falha, MENOS na compensacao simultanea
    (+N testes novos / -N desativados). Direcao de erro futura (`MemberData`/`ClassData`) e
    SUBcontagem — piso mais frouxo, nunca falso bloqueio. Acao para a proxima phase que reescrever
    DoD de C#: derivar o lado HEAD de `dotnet test --list-tests` em vez de grep estatico.
