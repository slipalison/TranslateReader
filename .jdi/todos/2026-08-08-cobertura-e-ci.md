# Todos — sessao de discuss `cobertura-e-ci` (2026-08-08)

Itens levantados na captura de decisoes e conscientemente empurrados para fora do escopo
(ver `## Out of scope` em `.jdi/phases/cobertura-e-ci/CONTEXT.md`).

- **[CI/GATE] A guarda do app MAUI vai bater em `detalhe-livro`.** D-2026-08-08-cobertura-e-ci-4
  faz o gate falhar (exit 2) quando um `.cs` NOVO aparece em `src/TranslateReader/`, porque nenhum
  teste alcanca esse diretorio (D-2026-07-30-regression-suite-2, opcao "c"). A phase `detalhe-livro`
  cria `BookDetailPage.xaml.cs` + `BookDetailPageModel.cs` por definicao: ela tera de escolher entre
  fechar a lacuna de teste do app MAUI (test project com TFM de MAUI, hoje proibido por
  D-2026-07-30-regression-suite-6) ou registrar o waiver em `.jdi/coverage-waivers.txt` com decisao
  propria. **O ponto e que a escolha fica visivel** — nao existe mais o default de herdar cegueira.

- **[CI/GATE] Diff coverage por LINHA (o que o Sonar chama New Code) nao foi implementado.**
  D-2026-08-08-cobertura-e-ci-2 escolheu `--diff-filter=AM` (arquivo inteiro), que puxa o arquivo
  legado completo para o denominador quando editado — mais duro que a metrica que a PR realmente
  controla. Implementar diff coverage de verdade (cruzar as linhas adicionadas/alteradas do patch
  contra o Cobertura) e a evolucao natural do script, e evitaria a valvula de waiver na maioria dos
  casos. Custo: parse de patch + mapa de linhas por arquivo.

- **[CI/GATE] Sem twin em PowerShell do `coverage-gate.sh`.** Rejeitado em
  D-2026-08-08-cobertura-e-ci-1: duas implementacoes da mesma regra viram duas fontes de verdade.
  Consequencia: quem estiver em Windows sem Git Bash nao roda o gate localmente (o Git for Windows
  o instala; `CLAUDE.md` ja assume isso para os hooks). Se um dia isso doer, a saida certa e um
  wrapper `.ps1` que invoca o `.sh`, nunca uma reimplementacao.

- **[CI/COBERTURA] O piso e agregado, nao por arquivo.** O gate exige `>=90%` sobre a soma das
  linhas em escopo; um arquivo novo a 40% pode passar carregado por outro a 100%. Um piso adicional
  por arquivo (ex.: nenhum arquivo em escopo abaixo de 70%) foi considerado e nao entrou — mais um
  numero para calibrar sem evidencia de que o modo de falha e real hoje.

- **[CI/JS] O harness JS continua sem `package.json`, lockfile ou linter.** Depois desta fase ele
  roda em dois lugares (`sonarqube.yml` e `coverage.yml`) com a mesma linha de comando duplicada em
  YAML. Se surgir um terceiro consumidor, extrair para `scripts/` junto do gate. O linter/formatter
  de JS continua no todo de `baseline-de-estilo`.

- **[CI/CUSTO] Nenhuma medicao de tempo de PR foi feita.** A coleta saiu do `ci.yml` e nasceu em
  `coverage.yml` (job paralelo), entao o esperado e empate — mas o job novo instala
  `dotnet-reportgenerator-globaltool` a cada run, sem cache. Se o tempo incomodar, cache de tool ou
  `dotnet tool restore` com manifest sao as saidas, e nenhuma delas foi decidida aqui.
