# Phase 10: README completo com badges — Summary  (slug: readme)

**Status:** executed
**Tasks:** 7/7 completed, 0 blocked
**Branch:** `jdi/readme` (base `b4cc85a`, sai de `jdi/pipeline-unificada` — PR #7)
**Modelo de execucao:** single-writer sequencial (T-1 -> T-7), 1 task = 1 commit, conforme o
`## HAZARD DE ORDENACAO` do PLAN. Zero paralelismo.

## Tasks executadas

| Task | Escopo | Commit | Status |
|---|---|---|---|
| T-1 | Funcionalidades + Stack: traducao offline, ressalva Windows-only, temas Light/Dark/Sepia | `657cfe6` | completed |
| T-2 | Tabela de Componentes com os 16 servicos reais + diagrama de camadas | `ae0fb69` | completed |
| T-3 | Estrutura do Projeto (3 projetos reais, sem `.idea/`) + secao Roadmap | `f7de5e8` | completed |
| T-4 | Modelos de Dados com as 7 tabelas SQLite | `9d1fdaf` | completed |
| T-5 | Build/Execucao corrigido + Testes/Cobertura 90% + Contribuindo/JDI | `efeff06` | completed |
| T-6 | Secao Seguranca + Licenca Apache 2.0 | `d52cd13` | completed |
| T-7 | Bloco dos 6 badges na ordem locked | `54105f6` | completed |
| T-7 | Bateria de DoD + PLAN.md/SUMMARY.md | (commit final desta phase) | completed |

## Arquivos modificados

- `README.md` (T-1..T-7 — unico arquivo de producao da phase)
- `.jdi/phases/readme/PLAN.md` (statuses)
- `.jdi/phases/readme/SUMMARY.md` (este arquivo)

Nada fora disso foi tocado: nenhum `.cs`, nenhum `.csproj`, nenhum arquivo em `.github/`,
`TranslateReader.slnx` intocado.

## Evidencia do DoD (10/10 auto-verificaveis)

Rodado da raiz do repo, comandos `**Verify:**` do CONTEXT.md verbatim, apos o commit `54105f6`:

```
PASS  (a) licenca
PASS  (b) traducao offline + ressalva
PASS  (c) 16 componentes
PASS  (d) 3 projetos
PASS  (e) BookDetail nao existe
PASS  (f) build/test commands
PASS  (g) modelos de dados
PASS  (h)+(i) .idea + Sepia
PASS  badges (D-2026-07-29-readme-2)
PASS  D-2026-07-29-readme-4 (4 secoes novas)
```

Item a item:

| DoD | O que foi feito | Resultado |
|---|---|---|
| (a) | "Projeto privado" removido; secao Licenca cita Apache License 2.0 e linka `LICENSE` | PASS |
| (b) + D-...-3 | Traducao offline documentada (LLamaSharp 0.27.0, modelo GGUF, cache por hash, `BookTranslationJob` com pause/retomada, export do EPUB traduzido) + blockquote "traducao offline hoje roda somente Windows" apontando a phase `llm-mobile` | PASS |
| (c) | Tabela de Componentes passou de 6 para os 16 servicos reais (4 Manager + 3 Engine + 6 Access + 3 Utility); descricoes copiadas literalmente de `CLAUDE.md` na secao Componentes do Sistema, exceto `BookTranslationJobAccess` (ausente la — ver desvio 4) | PASS |
| (d) | Arvore reescrita com os 3 projetos reais: `src/TranslateReader.Core`, `src/TranslateReader`, `test/TranslateReader.Tests` | PASS |
| (e) | `BookDetailPage.xaml` / `BookDetailPageModel.cs` sumiram da arvore; `BookDetailPage` so aparece na tabela de Roadmap, marcado "Planejado", apontando a phase `detalhe-livro` | PASS |
| (f) | Todo `dotnet build`/`dotnet run` aponta para `src/TranslateReader/TranslateReader.csproj` (nada de `-f <TFM>` na solution, NETSDK1005); `dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj` documentado nas duas formas (simples e com coleta de cobertura), copiadas de `.github/workflows/ci.yml` | PASS |
| (g) | Modelos de Dados subiu de 4 para as 7 tabelas, com `Settings`, `TranslationCache` (incl. `UNIQUE`) e `BookTranslationJob` (incl. `LastCompletedChapterIndex`) | PASS |
| (h)+(i) | `.idea/` removido da arvore (gitignorado por `.gitignore:29`); temas agora Light/Dark/Sepia, "claro/escuro" eliminado | PASS |
| badges D-...-2 | 6 badges na ordem locked: Pipeline -> CodeQL -> Scorecard -> Sonar Quality Gate -> Sonar Coverage -> License. Ordem validada por script Python; todo `actions/workflows/*.yml` citado existe em `.github/workflows/` | PASS |
| D-...-4 | 4 secoes novas: Seguranca (`SECURITY.md` + tabela de 8 scanners + hardening por SHA pin), Testes e Cobertura (90% pos-boundary `4285f25`), Contribuindo/JDI (`CLAUDE.md` + `.claude/rules/csharp.md`), Licenca Apache 2.0 | PASS |

Checagem extra do PLAN (T-1): o README continua **pt-BR sem acentos** — a bateria de accent-grep
sai vazia (exit 1) apos cada task. Nota de ambiente: sem `LC_ALL=C.UTF-8` o `grep -P` deste Git
Bash aborta com "supports only unibyte and UTF-8 locales", e o `!` da negacao mascara o erro
como sucesso. Rodar sempre com o locale explicito.

## Resolvibilidade dos badges

| Badge | URL | HTTP | Gate |
|---|---|---|---|
| Pipeline | `.../actions/workflows/pipeline.yml/badge.svg` | 200 | warn-only |
| CodeQL | `.../actions/workflows/codeql.yml/badge.svg` | 200 | warn-only |
| OpenSSF Scorecard | `api.scorecard.dev/projects/github.com/slipalison/TranslateReader/badge` | 200 | hard |
| Sonar Quality Gate | `sonarcloud.io/api/project_badges/measure?...metric=alert_status` | 200 | hard |
| Sonar Coverage | `sonarcloud.io/api/project_badges/measure?...metric=coverage` | 200 | hard |
| License | `img.shields.io/badge/License-Apache_2.0-blue.svg` | 200 | (extra) |

Os 2 badges do Actions eram warn-only no PLAN porque `pipeline.yml` poderia ainda nao estar
registrado no remote. **Nao foi preciso emitir WARN:** o `gh api` de
`repos/slipalison/TranslateReader/actions/workflows/pipeline.yml` e o de `codeql.yml` retornaram
`.state = active`, e os dois `badge.svg` respondem 200.

## Desvios

1. **Probe de resolvibilidade dos 3 badges externos trocado de `HEAD` para `GET`.** O comando do
   PLAN era `curl -sfI` (HEAD). O SonarCloud responde **405 Method Not Allowed** a HEAD em
   `/api/project_badges/measure` — falha do metodo de sondagem, nao da URL. Em GET a mesma URL
   devolve **200** com um SVG valido. O gate continua **hard** e continua passando (3/3 via
   `curl -sfL -o /dev/null`); so a forma de sondar mudou. Nenhum hard virou warn.
2. **Bullet "Bookmarks para marcar trechos importantes" removido de Funcionalidades** (T-1). Nao
   estava na lista (a)-(i), mas viola a regra locked "nenhuma feature futura descrita como
   pronta" de D-2026-07-29-readme-1: `IReadingManager` nao expoe bookmarks e nao ha UI (a mesma
   evidencia que originou a phase `bookmarks` em `ROADMAP.md`). Migrou para a tabela de Roadmap
   como "Planejado", com a nota de que so a camada de dados existe.
3. **Coluna da tabela de Componentes renomeada** de "Volatilidade Encapsulada" para
   "Responsabilidade" (T-2), para casar com a coluna de `CLAUDE.md`, ja que o CONTEXT manda
   reusar aquele texto literalmente em vez de inventar volatilidade nova.
4. **`BookTranslationJobAccess` nao existe na tabela de `CLAUDE.md`** (ela lista 15 dos 16
   servicos). A descricao foi derivada das 4 operacoes reais de
   `Contracts/Access/IBookTranslationJobAccess.cs` (`FetchActiveJobAsync`, `SaveJobAsync`,
   `UpdateJobProgressAsync`, `DeleteJobAsync`), nao inventada. Vale registrar em `todos.md`:
   `CLAUDE.md` esta com 15 de 16 servicos na propria tabela.

## Testes / regressao

```
dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release
Aprovado!  - Com falha: 0, Aprovado: 169, Ignorado: 2, Total: 171
```

Baseline preservada (169 aprovados / 2 ignorados — os 2 `TranslationEngineTests` que exigem
modelo GGUF real). Nenhum teste novo: phase README-only, sem `.cs` novo ou alterado.

**Cobertura (D-6, 90% pos-boundary `4285f25`): SKIPPED esperado** — nao ha codigo novo ou
alterado nesta phase. Mesmo padrao de `ci-seguranca` e `pipeline-unificada`.

## Blocked

Nenhuma.

## Fica para o PR review (ja previsto no CONTEXT)

- Renderizacao visual dos 6 badges depois do merge (o do Pipeline so fica verde de fato quando
  esta branch entrar em `main`).
- Julgamento subjetivo de "bem explicado" pedido no card.
- Dashboard do SonarCloud com Quality Gate/coverage populados pos-merge.
- Leitura humana da didatica das secoes novas (Seguranca, Testes, Contribuindo).
