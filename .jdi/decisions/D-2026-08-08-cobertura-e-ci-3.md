D-2026-08-08-cobertura-e-ci-3 (2026-08-08): o gate **bloqueia nos dois lugares** — falha o comando
local (`bash scripts/coverage-gate.sh` com exit != 0) e falha o CI. Nada de modo informativo,
nada de periodo de calibracao.

**Reconciliacao explicita de duas respostas desta sessao** (a pergunta 3 dizia "step no job `test`
do `ci.yml`", a pergunta 5 escolheu job dedicado): o local do bloqueio no CI e o **job dedicado
`Coverage`** de D-...-5, nao um step em `ci.yml`. A pergunta 5 supersede o detalhe de localizacao
da pergunta 3; o que a pergunta 3 travou — bloqueia, e nos dois ambientes — vale integralmente.

Consequencia obrigatoria, para nao existir medicao duplicada: `ci.yml` **perde**
`--collect:"XPlat Code Coverage"` e o upload do artifact `coverage`; os dois migram inteiros para
`coverage.yml`. O job `test` de `ci.yml` continua rodando a suite (feedback rapido, check
`Pipeline / CI` inalterado); a cobertura passa a ter uma unica origem no pipeline. Rodar `dotnet
test` duas vezes por PR so para ter dois relatorios seria custo de CI sem sinal novo.

Sem excecao por evento: bloqueia em `pull_request` E em `push` para `main` (o orquestrador dispara
nos dois). Um main vermelho por cobertura e informacao, nao ruido — e o unico jeito de o piso ser
piso.
