shipped_at: 2026-07-29T11:45:32Z
verdict: APPROVED_WITH_WARNINGS
by: Alison Amorim

## Learnings
- Migracao para reusable renomeia todo check do Actions para `<caller job> / <job interno>`; checks de GitHub App (CodeQL agregado, Semgrep OSS, SonarCloud) mantem o nome. Sempre capturar os nomes REAIS via `gh api .../check-runs` DEPOIS do primeiro run e so entao aplicar o PATCH — ordem invertida trava toda PR em "Expected — waiting for status".
- PATCH de required checks usa `checks[]` com `app_id`, nunca `contexts[]` (grava `app_id: null` e afrouxa o pin de app). `strict` e boolean: `-F strict=true`, `-f` retorna 422.
- Job que precisa de `contents: write` (dependency-snapshot do SBOM) fica gated em `push` — dar write a job disparado por PR e risco real e falha em PR de fork. A cobertura do lado do PR e do `dependency-review-action`, nao do SBOM.
- Reusable convertido tem que perder `push`/`pull_request` (double-run) e o proprio `concurrency` (nested cancel) — concurrency vive so no orquestrador.
- Quando um item de DoD vira proxy fraco do objetivo real (grep de `secrets: inherit` vs least-privilege de fato), o objetivo ganha: amendar a decisao (append-only) e reescrever o DoD, provando com teste de discriminacao que o criterio novo e estritamente mais forte.
