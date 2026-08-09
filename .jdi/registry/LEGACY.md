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
- Test framework: xUnit 2.9.3 + NSubstitute 5.3.0 + coverlet.collector 10.0.1 (baseline 167 testes)
- Build: `dotnet build -f net10.0-windows10.0.19041.0` (TFM Windows e o alvo de verificacao — backends LLamaSharp Cpu/Cuda12 so existem para Windows hoje)
- Test: `dotnet test` (projeto de teste em `net10.0` puro, sem workload mobile)
- Coverage: `bash scripts/coverage-gate.sh` (D-2026-08-08-cobertura-e-ci-1) -> mede a propria execucao,
  ponderado por linha, escopo `AM` (arquivos criados OU modificados) depois de `4285f25`, 90% C# /
  85% JS (D-6, D-2026-08-08-cobertura-e-ci-4)
- Lint: `dotnet format whitespace --verify-no-changes` — `.editorconfig` + `Directory.Build.props` (analyzers built-in `latest-recommended` + `EnforceCodeStyleInBuild` + `Meziantou.Analyzer`) estao na raiz desde a phase `baseline-de-estilo`. Gate 4 = BLOCK nos arquivos tocados pela phase em review, WARN fora do diff (legado isento por D-2). `TreatWarningsAsErrors` esta LIGADO: warning `CS/CA/MA` novo quebra o build. Dos 24 IDs medidos, 4 foram calibrados por escopo de pasta no `.editorconfig` e 21 congelados num `<NoWarn>` fechado, por ID, comentado — o teto de 12 de D-2026-08-08-baseline-de-estilo-3 foi revogado por D-2026-08-08-baseline-de-estilo-6
- Gate 7 (UI live): SKIPPED permanente — `has_frontend=false`, app MAUI nativo sem dev server. Skills `frontend-rules`/`frontend-validator` NAO carregadas
- Modelo LLM: nenhum pinado em nenhum runtime — PROJECT.md declara "usar o default do ambiente"

**Notas:**
- Skills genericas de arquitetura do JDI (`clean-architecture`, `ddd`, `hexagonal`, `onion`, `vertical-slice`) explicitamente NAO carregadas — proibidas por D-3
- Comandos de gate gerados em bash e PowerShell, ambos validados contra este repo no bootstrap
- 2026-07-28 (pos-bootstrap): threshold de cobertura atualizado 80% -> 90% nos dois agents por D-6
  (usuario elevou o gate em `.claude/rules/csharp.md` §6). A linha "Coverage" acima reflete o
  valor no momento da geracao; o vigente e 90%
- 2026-08-09 (phase `cobertura-e-ci`): a prosa de cobertura nos dois agents virou chamada a
  `scripts/coverage-gate.sh` (D-2026-08-08-cobertura-e-ci-1). Escopo passou de "so arquivos
  novos" (`--diff-filter=A`) para `AM` (criados OU modificados); "SKIPPED quando nao ha arquivo
  novo" nao existe mais — o gate sempre mede o `AM` scope. `coverlet.collector` atualizado
  8.0.1 -> 10.0.1 (versao real do repo)
- 2026-07-28 (fix round ci-seguranca): comando de build do Gate 1 corrigido para
  `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0`
  em `jdi-reviewer-translatereader.md` e `reviewers.md` — `-f` a nivel de solution falha com
  NETSDK1005 nos projetos Core/Tests (`net10.0`-only), REVIEW W-5. A linha "Build" acima reflete
  o valor no momento da geracao; o vigente e o comando com csproj explicito
- 2026-07-30 (D-7): modelo do reviewer pinado em **Fable 5 / reasoning xhigh**
  (`runtime_overrides.claude: {model: fable, effort: xhigh}`). Supersede a linha
  "Modelo LLM: nenhum pinado em nenhum runtime" acima, SO para o reviewer — o doer
  segue herdando o default do ambiente. Pedido explicito do usuario.
