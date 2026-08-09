D-2026-08-08-cobertura-e-ci-1 (2026-08-08): o gate de 90% (D-6) nasce como **script versionado no
repo** + **segunda camada no SonarCloud**, nunca como threshold de pacote.

**Fato tecnico que corta a alternativa obvia:** `coverlet.collector` (10.0.1 no
`test/TranslateReader.Tests.csproj`) **nao** suporta `Threshold`/`ThresholdType` — enforcement
nativo so existe em `coverlet.msbuild` (`/p:Threshold=90`), e mesmo la o numero e o **agregado por
assembly**, que D-2 manda ignorar (dominado por legado isento). Adicionar `coverlet.msbuild` foi
REJEITADO: entregaria o numero errado com aparencia de rigor.

**(a) Camada local/CI — `scripts/coverage-gate.sh`** (bash; Git Bash no Windows, `ubuntu-latest` no
CI). Twin em PowerShell REJEITADO: duas implementacoes da mesma regra viram duas fontes de verdade,
e todo `Verify:` deste repo ja e bash. Contrato locked:
- **O script executa a medicao**, nao le artefato de terceiros. Aprendizado direto de
  `coverage-90` (SHIPPED, learnings 2 e 3): gate que le relatorio de execucao ANTERIOR passa verde
  com a suite reprovando, e `find | sort | tail -1` sobre diretorio GUID do VSTest escolhe por
  ordem lexicografica, nao por tempo.
- Diretorio proprio, **apagado a cada execucao**: `TestResults/coverage-gate/`. Nao toca no literal
  `TestResults/js-lcov.info` que `sonarqube.yml` pina em duas linhas (D-2026-07-31-coverage-90-9).
- Cobertura **ponderada por linhas**: `sum(linhas cobertas) / sum(linhas validas)` sobre os
  arquivos em escopo, deduplicando por `filename + line number` (um `.cs` pode render varios
  `<class>` no Cobertura; classe parcial conta duas vezes se nao deduplicar). **Corrige defeito
  real:** o Gate 3 do reviewer hoje (`jdi-reviewer-translatereader.md:266`) tira media
  NAO-PONDERADA de `line-rate` — um arquivo de 4 linhas pesa igual a um de 400.
- Saida legivel por maquina, uma linha por bloco:
  `COVERAGE_SCOPE covered=<int> valid=<int> pct=<float> files=<int>`,
  `COVERAGE_JS covered=<int> valid=<int> pct=<float> files=<int>`,
  `COVERAGE_GUARD new_app_cs=<int> waived=<int>`.
- Thresholds por env com default pinado no proprio script: `COVERAGE_MIN=${COVERAGE_MIN:-90}`
  (D-6) e `COVERAGE_JS_MIN=${COVERAGE_JS_MIN:-85}` (herdado de D-2026-07-31-coverage-90-1). O env
  existe para o **teste adversarial do DoD**, e o YAML de CI tem PROIBIDO defini-lo — senao o gate
  se afrouxa sozinho num commit de workflow.
- Exit codes: `0` pass, `1` abaixo do piso, `2` violacao da guarda do app MAUI (D-...-4), `3` falha
  de medicao (suite vermelha, relatorio ausente).
- **Gate 3 do reviewer passa a CHAMAR o script**; a implementacao em prosa (bash + PowerShell) sai
  do agent. Uma regra, um lugar.

**(b) Segunda camada — SonarCloud Quality Gate New Code de 80% para 90%.** Complementa e nao
substitui (a): mede metrica diferente (New Code por linhas do diff, D-2026-07-31-coverage-90-7) e
ja bloqueia PR por `sonar.qualitygate.wait=true`. **Mora fora do repo** (UI/API do SonarCloud, sem
arquivo versionado, morre sem `SONAR_TOKEN`) — por isso a mudanca em si vai para
`## Deferred to PR review`, e NENHUM `Verify:` desta fase alega prova-la localmente.
