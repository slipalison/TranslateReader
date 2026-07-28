shipped_at: 2026-07-28T13:32:31Z
verdict: APPROVED_WITH_WARNINGS
by: Alison Amorim

## Learnings
- Sonar coverage com coverlet.collector exige `--collect:"XPlat Code Coverage;Format=opencover"` — props `-p:CollectCoverage` sao do coverlet.msbuild e falham silenciosamente (W-2).
- NU1903 HIGH em SQLitePCLRaw 2.1.11 segue aberto — Dependabot (entregue nesta phase) vai propor o bump; validar restore nos 4 TFMs ao aceitar.
- harden-runner esta em `egress-policy: audit` — promover a `block` numa phase futura, depois de aprender o baseline de trafego dos runs reais.
- `dotnet build -f <TFM>` a nivel de solution quebra com NETSDK1005 (projetos net10.0-only) — sempre apontar o csproj do app explicitamente.
- Nunca interpolar `${{ }}` dentro de `run:` — sempre indirection via `env:` (tag injection, W-3); auditoria pegou 1 caso mesmo com convencao escrita.
