# Reviewers — routing dos gates

Cada linha mapeia um reviewer aos arquivos que ele audita. O `/jdi-verify` roda todos os
reviewers cujo `File glob` intersecta os arquivos alterados na phase.

Schema v2 — a coluna `File glob` existe para projetos multi-stack. Projeto single-stack usa
o catch-all `**/*`.

| Agent | File glob | Trigger | Blocks ship? |
|---|---|---|---|
| jdi-reviewer-translatereader | `**/*` | /jdi-verify | yes, if BLOCKED |

## Gates configurados

| Gate | Comando | Falha |
|---|---|---|
| 1 Build | `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` | BLOCK |
| 2 Tests | `dotnet test` (baseline 167) | BLOCK |
| 3 Coverage | `dotnet test --collect:"XPlat Code Coverage"` -> Cobertura, 90% (D-6) | BLOCK (so em arquivos novos pos-`4285f25`) |
| 4 Lint | `dotnet format --verify-no-changes` | WARN (sem `.editorconfig`/analyzers ainda) |
| 5 Security/Layer | greps de camada, zip-slip, XXE, WebView JS, sync-over-async, leak de evento, static mutavel | BLOCK / WARN conforme o check |
| 6 Consistency | log de commits x PLAN, conventional commits (D-4) | WARN |
| 7 UI live | — | **SKIPPED permanente** (`has_frontend=false`, app MAUI nativo) |
| 8 DoD | `Verify:` de PROJECT.md + CONTEXT.md | BLOCK se algum Auto FAIL |

## Notas

- **Modelo pinado (D-7):** este reviewer roda em **Fable 5 / reasoning xhigh**
  (`runtime_overrides.claude.model: fable`, `effort: xhigh`). O doer continua herdando o default
  do ambiente. Vale para todo `/jdi-verify`, inclusive `mode=dod-critic`.
- `orchestration.mode=enhanced` em `config.json` (D-5): o `/jdi-verify` tambem dispara este
  reviewer em `mode=dod-critic` (re-check read-only das linhas Auto/PASS do gate 8).
- Gate 4 ja e BLOCK sobre os arquivos tocados pela phase em review (WARN fora do diff): a phase
  `baseline-de-estilo` entregou `.editorconfig` + `Directory.Build.props` com analyzers, e o
  comando do gate passou a ser `dotnet format whitespace --verify-no-changes`.
- Gate 3 reporta SKIPPED enquanto nao houver arquivo `.cs` novo depois de `4285f25`
  (no bootstrap havia 0). SKIPPED nao e falha.
- Prioridade em conflito: seguranca > performance > boas praticas.
