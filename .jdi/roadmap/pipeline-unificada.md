---
order: 9
name: Pipeline unificada (orquestrador reusable)
---
- **Slug:** pipeline-unificada
- **Goal:** consolidar os fluxos de push/PR num orquestrador unico `pipeline.yml` via `workflow_call` — um run graph com build, testes, CodeQL, Semgrep, SCA, secrets, Sonar, dependency-review e SBOM; `scorecard.yml` (requisito OSSF) e `release.yml` (trigger de tag) permanecem isolados; branch protection re-mapeada para os novos nomes de check
