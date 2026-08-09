D-2026-08-08-cobertura-e-ci-5 (2026-08-08): superficie de `.github/` desta fase = escopo amplo
(escolha explicita do usuario). Quatro entregas, cada uma com sua regra:

**(1) Reusable dedicado `.github/workflows/coverage.yml`.** `on: workflow_call:` puro — sem
`push:`/`pull_request:` (D-2026-07-28-pipeline-unificada-2 tirou esses triggers de todo reusable) e
sem `workflow_dispatch:` (mesmo tratamento dado a `ci.yml`: o orquestrador e o unico jeito de
disparar). Sem bloco `concurrency:` proprio — ele pertence ao `pipeline.yml`
(D-2026-07-28-pipeline-unificada-1(e)). Hardening obrigatorio de D-2026-07-28-ci-seguranca-4:
`step-security/harden-runner` no job `ubuntu-latest`, 100% das actions de terceiro pinadas por SHA
de 40 hex, `permissions: contents: read` no topo. Caller em `pipeline.yml`: job `coverage` com
`name: Coverage`, `permissions: contents: read`, **sem** `secrets:` (o gate nao fala com servico
externo) e **sem** `if:` (roda em push e em PR, D-...-3).

**(2) Relatorio.** `dotnet-reportgenerator-globaltool` gerando `MarkdownSummaryGithub` no
`$GITHUB_STEP_SUMMARY` (numero visivel sem baixar artifact) + `Html` como artifact com
`if-no-files-found: error`. Nome do artifact prefixado pelo job e unico entre TODOS os workflows —
apos `pipeline-unificada` todos compartilham o mesmo `run_id`, entao nome repetido colide
(D-2026-07-28-pipeline-unificada-6(a)).

**(3) Fechar o W-2 de `baseline-de-estilo`** (`REVIEW.md:176-190`): `TreatWarningsAsErrors=true`
vive no `Directory.Build.props` da raiz e vale para TODO TFM, inclusive o job `build-android`, que
nunca foi medido em maquina nenhuma (nao ha Android SDK local) — IDs que o build Windows nunca
acende (`CA1416` de platform-compatibility, `XA*` do SDK Android) hoje seriam **erro duro**. A fase
mede no CI e registra o resultado em `.jdi/phases/cobertura-e-ci/android-warnings.md`.
**Ordem de preferencia locked, para o doer nao pegar o atalho:** (i) ID novo entra no `<NoWarn>`
com comentario proprio e marca `RISCO:` quando for bug potencial, seguindo exatamente a convencao
de D-2026-08-08-baseline-de-estilo-6(4), com roteamento para todos; (ii) desligar
`TreatWarningsAsErrors` **so** no TFM `net10.0-android` (`Condition` explicita) e permitido apenas
se os IDs vierem do toolchain Android e nao forem enumeraveis, e obriga comentario citando esta
decisao. Desligar `TreatWarningsAsErrors` de forma ampla e PROIBIDO — seria desfazer a entrega da
fase anterior para nao ler um log.

**(4) Remap de branch protection — a parte com precedente de incidente.** Job novo = check name
novo (`Pipeline / Coverage`), e required context com nome errado ja travou 100% dos PRs uma vez
(D-2026-07-28-pipeline-unificada-1(d)). Protocolo obrigatorio, na ordem:
(a) ANTES de qualquer edicao, snapshot `gh api repos/:owner/:repo/branches/main/protection` em
`.jdi/phases/cobertura-e-ci/branch-protection-before.json` (commitado) — mesmo mecanismo de
D-2026-07-28-pipeline-unificada-6(b), que ja produziu `branch-protection-remap.md` em
`pipeline-unificada`;
(b) o nome do check e **derivado do YAML**, nunca digitado de memoria: `name:` do orquestrador +
` / ` + `name:` do job caller;
(c) `.jdi/phases/cobertura-e-ci/branch-protection-remap.md` registra before/after e o literal
esperado;
(d) a **mutacao** da protection (adicionar o required context) exige token de admin e so pode ser
feita **depois** de o job existir e ter rodado uma vez — vai para `## Deferred to PR review`.
Adicionar um required context que ainda nao produziu check e exatamente como se trava o proprio
repositorio.
