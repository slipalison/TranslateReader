# Registry — audit trail dos agents criados pelo JDI

Append-only. Cada entrada registra o que foi gerado, quando e a partir de que contexto.
ID deterministico `R-{YYYY-MM-DD}-{slug}` — dois devs fazendo bootstrap de stacks diferentes
em branches paralelos nunca colidem.

## R-2026-07-28-translatereader (2026-07-28)

**Type:** specialist (doer + reviewer)
**Slug:** `translatereader`
**Stack:** C# / .NET 10 (`net10.0`) + .NET MAUI 10.0.51 — TFMs `net10.0-windows10.0.19041.0`, `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`
**Code design:** The Method (Juval Lowy) — LOCKED por D-1, confirmado por D-5. Skill carregada: `the-method`
**Adopted:** true — boundary `4285f25` (D-2)
**Specialist count:** 1 (single-stack; os 4 TFMs sao multi-targets da mesma arvore de codigo)
**Created by:** /jdi-bootstrap -> jdi-architect (mode=specialist), execucao nao-interativa

**Files:**
- `.jdi/agents/jdi-doer-translatereader.md`
- `.jdi/agents/jdi-reviewer-translatereader.md`

**Integration:**
- `.jdi/specialists.md` (criado)
- `.jdi/reviewers.md` (criado)

**Config aplicada:**
- Test framework: xUnit 2.9.3 + NSubstitute 5.3.0 + coverlet.collector 8.0.1 (baseline 167 testes)
- Build: `dotnet build -f net10.0-windows10.0.19041.0` (TFM Windows e o alvo de verificacao — backends LLamaSharp Cpu/Cuda12 so existem para Windows hoje)
- Test: `dotnet test` (projeto de teste em `net10.0` puro, sem workload mobile)
- Coverage: `dotnet test --collect:"XPlat Code Coverage"` -> parse do Cobertura, 80% **apenas** em arquivos criados depois de `4285f25`
- Lint: `dotnet format --verify-no-changes` — WARN-only enquanto nao existir `.editorconfig`/analyzers (phase `baseline-de-estilo`)
- Gate 7 (UI live): SKIPPED permanente — `has_frontend=false`, app MAUI nativo sem dev server. Skills `frontend-rules`/`frontend-validator` NAO carregadas
- Modelo LLM: nenhum pinado em nenhum runtime — PROJECT.md declara "usar o default do ambiente"

**Notas:**
- Skills genericas de arquitetura do JDI (`clean-architecture`, `ddd`, `hexagonal`, `onion`, `vertical-slice`) explicitamente NAO carregadas — proibidas por D-3
- Comandos de gate gerados em bash e PowerShell, ambos validados contra este repo no bootstrap
