# Specialists — routing do doer

Cada linha mapeia um stack para o agent executor que o atende. O `/jdi-do` resolve o doer
por este arquivo: escolhe a linha cujo `File glob` casa com os `files_modified` da task.

Schema v2 — a coluna `File glob` existe para projetos multi-stack (um par doer/reviewer por
stack). Projeto single-stack usa o catch-all `**/*`.

| Stack | Agent | File glob | Trigger |
|---|---|---|---|
| C# / .NET 10 + MAUI 10.0.51 (Windows/Android/iOS/MacCatalyst) | jdi-doer-translatereader | `**/*` | executor for files matching glob |

## Notas

- **Single-stack.** Os 4 TFMs (`net10.0-windows10.0.19041.0`, `net10.0-android`, `net10.0-ios`,
  `net10.0-maccatalyst`) sao multi-targets da MESMA arvore de codigo, nao codebases separados.
  Nao ha split backend/frontend nem por plataforma — por isso um unico par doer/reviewer.
- Code design locked: **The Method** (D-1, confirmado em D-5). O doer carrega a skill `the-method`.
  As skills genericas de arquitetura do JDI estao proibidas por D-3.
- Adocao brownfield: boundary `4285f25` (D-2). O doer nao refatora codigo legado por estilo.
