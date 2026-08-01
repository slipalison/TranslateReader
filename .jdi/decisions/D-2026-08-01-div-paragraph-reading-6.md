D-2026-08-01-div-paragraph-reading-6 (SUPERSEDE dos `Verify:` dos itens 1, 2, 3, 4 e 6 do DoD desta
phase): os CRITERIOS do `## Definition of Done` do CONTEXT.md ficam INTACTOS — muda so o COMANDO que
os prova. Os itens 5 e 7 ficam intocados (o DoD critic os confirmou solidos: piso `B+1` derivado de
`main`, nome exato assertado, e pathspec de escopo conferido contra `origin/main`/`ls-remote`).
Nenhuma linha de `src/` ou `test/` muda por causa desta decisao: o fix de iter 1 passou os 8 gates
do reviewer (JS 73/73, C# Failed 0 / Passed 336 / Total 338, build 0 erros, RED-first reproduzido
pelo proprio reviewer em worktree). O que estava quebrado era a PROVA, nao o codigo.

MOTIVO (contra-exemplos EXECUTADOS pelo DoD critic, todos reproduzidos nesta sessao em copia
descartavel do repo — `git clone --local` no scratchpad, repo real nunca mutado):
- Itens 2 e 3 NAO PODEM sair verdes como escritos: grepam `^# fail 0` (TAP) e o Node 24
  (`v24.14.0`, mesma major pinada no CI via `setup-node`) usa o reporter `spec` por padrao mesmo com
  stdout redirecionado para arquivo. Medido: os dois comandos LITERAIS saem exit 1 com a suite 100%
  verde. O "PASS" registrado na iter 1 veio de um comando RE-AUTORADO na hora pelo reviewer
  (variante com `--test-reporter=tap`), nao do comando escrito no CONTEXT — um `Verify:` que so
  passa quando alguem reescreve o comando na hora de rodar nao certifica nada. Precedente do
  projeto: a phase `coverage-90` ja pina `--test-reporter=tap` nos `Verify:` dela.
- Item 2 continuava PROXY mesmo com TAP: 4 stubs `test('calibre stub N', () => {})` sem assert
  nenhum dao `N=4`/`# pass 4`/`# fail 0` e saem exit 0 com ZERO round-trip. Reproduzido: exit 0 com
  os 6 testes calibre reais renomeados para fora do filtro.
- Item 3 nao media regressao: o piso `P >= B+4` passa com 3 testes da era de `main` DELETADOS
  (`17 >= 17`). Reproduzido: exit 0 na variante TAP com 3 testes de `main` fora do arquivo. O que
  prova nao-regressao e a comparacao NOME A NOME — que o reviewer rodou FORA do comando (`comm`).
- Item 1 era grep de FORMA: (CE-1) filtro de letra invertido em `translation.js:15` (fix
  efetivamente ausente, 6 testes vermelhos) sai exit 0; (CE-2) `applyTranslations` desviado para
  `querySelectorAll('[data-original], p, div')` com um COMENTARIO contendo `_translatableCandidates(`
  repoe a contagem `>= 4` e sai exit 0 com so 2 das 3 funcoes usando o helper — o grep conta TEXTO,
  inclusive comentario.
- Item 4: `grep -q "HtmlUtility.ExtractTextBlocks"` casa nas linhas 124 e 195 (outros metodos), entao
  desviar a linha 244 para `HtmlUtility.LegacyParagraphExtract(bodyContent)` (o extrator defeituoso
  apenas RENOMEADO) sai exit 0; e o grep de ausencia so olhava `HtmlUtility.cs`, nunca o repo.
- Item 6: o piso `321/323` esta 15 testes ABAIXO do baseline REAL de `main` (`335/2/337`, medido em
  worktree pelo reviewer), ou seja aceitava regressao de ate 15 testes. Reproduzido: log sintetico
  `Passed: 321, Total: 323` sai exit 0; e uma copia do HEAD com 3 testes C# REALMENTE deletados mais
  o log honesto correspondente (`333/335`) tambem sai exit 0.

CONTINGENCIA PREVISTA PELO PROPRIO PLAN, e ela DISPAROU. O PLAN (`## Aritmetica do DoD item 6`)
proibia criar `D-...-6` para a aritmetica de contagem e admitia a excecao "so se a corrida real
contradisser a medicao". A corrida real CONTRADISSE: o PLAN projetava `main` em Passed 320 / Total
322 (herdado de `D-2026-08-01-div-paragraph-translation-9` item 8) e piso exato 321/323 com "margem
zero"; a medicao real de `main` e **Failed 0, Passed 335, Skipped 2, Total 337**, e o HEAD entrega
**336/2/338**. O piso do PLAN nao era margem zero: era folga de 15 testes. Fica registrado que a
premissa numerica do PLAN estava errada, e por isso o caminho append-only (arquivo NOVO de decisao,
depois CONTEXT.md, depois `npx -y jdi-cli render`) e o correto aqui. Decisoes D-...-1 a D-...-5
seguem intocadas.

REGRAS DE AUTORIA DE `Verify:` que passam a valer nesta phase (e viram precedente para as
proximas):
1. Todo `Verify:` que parseie `node --test` PINA `--test-reporter=tap` (irmao de
   `DOTNET_CLI_UI_LANGUAGE=en` no `dotnet test`, `D-2026-08-01-div-paragraph-translation-9` regra 1).
2. Gate de suite nao se contenta com CONTAGEM: amarra o criterio aos testes REAIS por NOME EXATO,
   exigindo `ok N - <nome>` no TAP.
3. Nao-regressao se prova NOME A NOME contra `main` (`comm -23` entre a lista de `main` e os nomes
   verdes do HEAD), nunca por piso aritmetico sozinho.
4. Gate de forma sobre codigo remove COMENTARIO antes de grepar e AMARRA o achado ao CORPO da funcao
   /metodo (range de `awk`), nunca ao arquivo inteiro.
5. Grep de AUSENCIA de API varre o repo tracked inteiro (`git grep` — nunca `grep -r`, que leria
   `obj/` e pegaria o codigo gerado por `[GeneratedRegex]`), nao so o arquivo onde a API morava.
6. Piso de suite e DERIVADO de `main` dentro do proprio comando, nunca cravado a mao.

COMANDOS NOVOS (substituem os `Verify:` dos itens 1, 2, 3, 4 e 6 no CONTEXT.md):
- Item 1: comentarios removidos (`//` e `/* */` de uma linha) antes de qualquer grep; o helper tem de
  ser lido em LINHA DE CODIGO DENTRO do corpo de cada uma das 3 funcoes (`awk` de
  `window.<fn> = function` ate `^};`); o corpo do helper tem de ter as duas guardas na polaridade
  certa (`if (<sem !>...querySelector(` para folha e `if (!<X>.test(` para letra). A prova de
  COMPORTAMENTO do corpo fica DELEGADA aos itens 2 e 3, agora endurecidos — item 1 e, por desenho,
  gate estrutural de fonte unica.
- Item 2: TAP pinado + os 3 testes de round-trip exigidos por NOME EXATO como `ok N - <nome>`
  (`getVisibleParagraphs returns every calibre leaf div that holds letters`,
  `applyTranslations writes into the calibre div the reported index points at`,
  `clearTranslations restores a translated calibre div and drops the marker`). A contagem `N >= 4`
  fica, agora como piso adicional, nao como prova.
- Item 3: TAP pinado + `# skipped 0` + `comm -23` entre os nomes de `git show main:` e os nomes
  VERDES do TAP do HEAD (vazio obrigatorio) + o piso `B+4` mantido.
- Item 4: ausencia por `git grep` em `src/*.cs`/`test/*.cs` (repo tracked inteiro) + presenca de
  `HtmlUtility.ExtractTextBlocks(bodyContent)` DENTRO do range de `awk` do corpo de
  `TranslateChapterAsync`, e exatamente UMA atribuicao com argumento `bodyContent` nesse corpo (mata
  tanto o rename quanto o "legado adicionado ao lado").
- Item 6: piso DERIVADO de `main` no proprio comando — `B` = `[Fact]` + `[InlineData]` contados em
  `main` (medido: 288 + 49 = **337**, bate exatamente com o `Total: 337` da corrida real de `main`),
  `Total >= B+1`, `Skipped <= ` contagem de `Skip=` em `main` (2), `Failed == 0` e coerencia
  `Passed+Skipped+Failed == Total` (mata log sintetico incoerente) + `comm -23` nome a nome dos
  metodos de teste C# de `main` contra o HEAD.

BARRA DE PROVA CUMPRIDA (matriz de mutacao nos dois sentidos, em copia; numeros no SUMMARY.md
`## Iter 2`): cada comando NOVO sai exit 1 em todos os contra-exemplos do critico enquanto o ANTIGO
sai exit 0; cada comando NOVO sai exit 0 no repo REAL sem mutacao (zero falso positivo); e cada
comando NOVO continua saindo exit 1 em tudo que o ANTIGO ja pegava (sete casos de regressao de gate
verificados). REJEITADO afrouxar qualquer criterio para fechar a conta, e REJEITADO tocar em `src/`
ou `test/`: o codigo esta certo, a prova e que estava oca.
