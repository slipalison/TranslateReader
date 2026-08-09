D-2026-08-08-cobertura-e-ci-4 (2026-08-08): denominador do gate = **C# do Core + JS do WebView +
guarda explicita do app MAUI**.

**(a) C#** — sai do Cobertura da execucao do proprio script. Na pratica so `src/TranslateReader.Core`
e alcancavel: `test/TranslateReader.Tests.csproj` e `net10.0` com um unico `ProjectReference` para o
Core (D-2026-07-30-regression-suite-6 proibiu multi-target e segundo test project).

**(b) JS** — os 4 scripts de producao em `src/TranslateReader/Resources/Raw/wwwroot/js/`
(`bridge`, `paginated`, `scroll`, `translation`) tem harness `node --test` desde `coverage-90`, mas
ele so roda **dentro de `sonarqube.yml`**, ou seja: some junto com `SONAR_TOKEN` (fork, Dependabot,
secret revogado). Passa a rodar tambem no gate, com piso proprio de **85% agregado sobre os 4
arquivos de producao** (numero herdado de D-2026-07-31-coverage-90-1, deliberadamente abaixo de 100
para nao forcar ramo puramente defensivo). Comando:
`node --test --experimental-test-coverage --test-reporter=lcov --test-reporter-destination=TestResults/coverage-gate/js-lcov.info test/js/`.
O destino e o diretorio proprio do gate: o literal `TestResults/js-lcov.info` que `sonarqube.yml`
usa nas linhas 107 e 137 fica **intocado** (D-2026-07-31-coverage-90-9 pina esse literal).

**(c) Guarda do app MAUI** — `src/TranslateReader/` (~1516 linhas) e estruturalmente inalcancavel
por teste; a lacuna esta aceita por decisao (D-2026-07-30-regression-suite-2, opcao "c"). O gate
NAO tenta medir esse diretorio, mas **falha (exit 2) se um `.cs` NOVO aparecer la** — criado apos
`4285f25` (`git log --diff-filter=A`) **ou** ainda nao commitado
(`git ls-files --others --exclude-standard`), para o sinal chegar antes do commit e nao depois do
push. Sem a guarda, escrever codigo novo no app MAUI e a forma mais facil de o gate ficar verde sem
significar nada: linha nao instrumentada nao aparece em denominador nenhum.

**Valvula auditavel, nao bypass:** `.jdi/coverage-waivers.txt`, uma linha por path, cada linha
exigindo referencia `# D-...` a uma decisao que justifique. Path sem referencia de decisao nao e
waiver valido. A phase `detalhe-livro` (BookDetailPage/BookDetailPageModel) vai bater nesta guarda
por construcao — e esse e o ponto: ela tera de escolher entre fechar a lacuna de teste do app ou
registrar a isencao por escrito, em vez de herdar cegueira por default.
