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

---

## Fix round 1

Iteracao 2, `mode=fix_blockers`. Entrada: `REVIEW.md` com verdict **BLOCKED**, 3 blockers.
Commit unico de correcao: `211a00b` (`fix(readme): remove three false claims about code, tests and
CI`). Nenhum `.cs`, `.csproj`, `.github/**` ou `TranslateReader.slnx` tocado.

**Causa raiz comum aos 3 blockers.** A iteracao 1 leu `.claude/rules/csharp.md` e
`.semgrep/dotnet-security.yml` e converteu **regras** (o que o codigo *deve* fazer) em
**descricoes** (o que o codigo *ja faz*). E exatamente o defeito que esta phase existe para
eliminar. O metodo desta iteracao foi o inverso: para cada frase escrita ou mantida nas secoes
tocadas, verificar a afirmacao contra o repositorio antes de commitar, preferindo fato observavel
(arquivo que existe, comando que roda, contagem medida) a restatement de politica; e, quando a
politica e citada, nomear o arquivo-fonte e marca-la como convencao.

### B-1 — over-claim de seguranca (o serio)

| | |
|---|---|
| **O que era falso** | "O proprio codigo trata como entrada nao confiavel os arquivos EPUB (extracao de zip **valida path escape e limita tamanho descomprimido**)". Nao existe esse controle em lugar nenhum do repo. |
| **Evidencia** | `ReadingManager.cs:59-60` monta `Path.Combine(imagesDir, relativePath.Replace(...))` com `relativePath` vindo de `epub.Content.Images.Local` (`ParsingEngine.cs:62-64`) e chama `fileUtility.WriteFileAsync`; `FileUtility.cs:31-32` faz `Directory.CreateDirectory` + `File.WriteAllBytesAsync` sem validar. Grep em `src/` por `GetFullPath\|ExtractToFile\|ExtractToDirectory\|entry.FullName` -> **zero** resultados; grep por `maxSize\|maxBytes\|uncompressed\|sizeLimit` -> **zero** resultados. |
| **O que diz agora** | Que EPUB/HTML sao entrada nao confiavel e que as regras de tratamento estao em `.claude/rules/csharp.md` secao 4, **normativas para codigo novo, nao descricao do que ja esta implementado**. E que quem as cobra hoje e a CI: as 4 regras proprias de `.semgrep/dotnet-security.yml` (`translatereader-zip-slip`, `-xxe`, `-webview-js-injection`, `-insecure-deserialization`) rodando via `semgrep scan --config .semgrep/ --severity ERROR --error`, enquadradas como **deteccao em CI, nao defesa em runtime**. |
| **Extra** | O claim de WebView ("todo valor derivado do livro e codificado antes de chegar em JavaScript") foi **removido junto**, mesmo tendo sido verificado como verdadeiro pelo reviewer: a propria regra de Semgrep e `WARNING` em vez de `ERROR` porque dois call sites legados nao passam por `JsStr(...)`. Afirmacao blanket de hardening em runtime saiu inteira da secao. |

### B-2 — over-claim sobre a suite de testes

| | |
|---|---|
| **O que era falso** | "Sao xUnit + NSubstitute, isolados: **sem rede, sem disco e sem SQLite real**." Duas das tres clausulas erradas. |
| **Evidencia** | Disco real: `FileUtilityTests.cs:18,24,31,43,63,86`, `ModelAccessTests.cs:34,43,66,77,78`, `HybridWebViewContractTests.cs:18,212,231`, e `ParsingEngineTests.cs:17` usa `Path.GetTempPath()`. SQLite real: `InMemoryDatabase.cs:19` -> `new SqliteConnection(...)` com provider `Microsoft.Data.Sqlite` (in-memory, mas motor real). |
| **O que diz agora** | Descreve a suite como medida: 171 testes, 169 passando, 2 ignorados (os dois de `TranslationEngineTests` com `Skip = "Requires GGUF model file for local development"` — string lida do arquivo). Diz que nao acessa rede, e que onde a unidade sob teste **e** o acesso a recurso ela usa o recurso de verdade em ambiente descartavel (SQLite in-memory + `Path.GetTempPath()`), nomeando os arquivos. A regra de isolamento fica num paragrafo separado, atribuida a `.claude/rules/csharp.md` **secao 6** e explicitamente valida para codigo/testes **novos**, com a ressalva de que a suite legada nao a cumpre. |

### B-3 — topologia de CI falsa

| | |
|---|---|
| **O que era falso** | "que dispara os workflows **reusaveis abaixo**" seguido de tabela que incluia `scorecard.yml`. `pipeline.yml` nao despacha Scorecard, e `scorecard.yml` nem e reusavel. A frase tambem generalizava "todo push e todo pull request" sobre `dependency-review` (so PR) e `sbom` (so push). |
| **Evidencia** | `pipeline.yml:16-75` lista exatamente 8 jobs: `ci`, `codeql`, `semgrep`, `sca`, `secret-scan`, `sonarqube`, `dependency-review` (`if: github.event_name == 'pull_request'`), `sbom` (`if: github.event_name == 'push'`). `grep -l workflow_call .github/workflows/*.yml` devolve exatamente esses 8 arquivos — `scorecard.yml` e `release.yml` ficam de fora. `scorecard.yml:3-8`: `schedule: cron "30 2 * * 6"` + `push: branches: [main]` + `workflow_dispatch`, com `publish_results: true` na linha 42. `release.yml:3-5`: `push: tags: ["v*"]`. |
| **O que diz agora** | Duas tabelas separadas. A primeira, "8 jobs despachados por `pipeline.yml`", com coluna **Disparo** explicitando `somente PR` para dependency-review e `somente push` para sbom, e `push e PR, mais cron semanal proprio` para o CodeQL (hibrido `workflow_call`+`schedule`). A segunda, "workflows fora do orquestrador", com `scorecard.yml` e `release.yml` e o motivo de cada um ficar separado. |
| **Bonus achado nesta iteracao** | A linha do `ci.yml` que eu mesmo escrevi dizia "build e suite de testes no Linux" — errado: `ci.yml` tem **dois** jobs, `test` (ubuntu-latest, com `--collect:"XPlat Code Coverage"`) e `build` (windows-latest, app MAUI). Corrigido antes do commit. Mesma classe de defeito, pego pela propria disciplina de verificar cada frase. |

### W-1 (corrigido junto) — comando de build Android que falha

`dotnet build ... -f net10.0-android` retornava `NETSDK1005` nesta maquina porque
`TranslateReader.csproj:7` so acrescenta o TFM android **no Windows** se achar
`%LocalAppData%\Android\Sdk` / `$ANDROID_HOME` / `$ANDROID_SDK_ROOT`. Confirmado por
`dotnet msbuild src/TranslateReader/TranslateReader.csproj -getProperty:TargetFrameworks` ->
`net10.0-windows10.0.19041.0;net10.0-ios;net10.0-maccatalyst` (sem android). Agora: a linha do
Android carrega o pre-requisito inline, ganhou callout proprio **`NETSDK1005` no build de
Android** citando `csproj:7` e as 3 variaveis, o callout pre-existente virou **`NETSDK1005` a
partir da raiz** (o leitor que topa com o erro no comando Android nao e mais mandado para a
explicacao errada), e o bloco ganhou o comando `-getProperty:TargetFrameworks` para listar os TFMs
reais da maquina. A ordem iOS/Android foi trocada para o comando incondicional vir antes do
condicional.

### Validacao (rodada apos as edicoes, antes do commit)

```
DoD (10 Verify: do CONTEXT.md, verbatim)     10/10 PASS
Badges (GET, curl -sL -o /dev/null -w %{http_code})
  pipeline.yml/badge.svg .................. 200
  codeql.yml/badge.svg ................... 200
  api.scorecard.dev/.../badge ............ 200
  sonarcloud .../metric=alert_status ..... 200
  sonarcloud .../metric=coverage ......... 200
  img.shields.io/.../License-Apache_2.0 .. 200
Acentos (scan Python, todo code point > 127)
  non-ascii total: 25 — 25/25 EM DASH (U+2014), letras acentuadas: 0
  contraprova: LC_ALL=C.UTF-8 grep -nP "[\x{00C0}-\x{00FF}]" README.md -> vazio, exit 1
dotnet test -c Release ... Com falha: 0, Aprovado: 169, Ignorado: 2, Total: 171
```

Nota sobre os em-dash: eram 17 na iteracao 1, agora sao 25 — as 8 novas ocorrencias vieram da
prosa reescrita. A restricao locked e **sem acentos**, nao sem em-dash; o scan confirma 0 letras
acentuadas. Metodo escolhido de proposito por nao poder falsear passe (enumera todos os code
points, nao depende de locale do `grep -P`).

### Warnings do REVIEW que **nao** foram tocados (fora do escopo desta rodada)

W-3 (nomes de tabela vs nomes de modelo), W-4 (`CLAUDE.md` com 15/16 servicos e 6/7 tabelas),
W-5 (linha 10 cita 3 de 4 plataformas), W-6 (preambulo do Roadmap), W-7 (arvore omite `docs/`,
`.semgrep/`, `Properties/`), W-8 (recomendacao de DoD para futuras phases de doc), W-9
(imprecisao de contagem no SUMMARY da iteracao 1). Nenhum e blocker.

### Achado escalado para fora da phase

W-2 do REVIEW deixou de ser observacao e virou registro formal em
[`.jdi/todos.md`](../../todos.md), secao "De `readme` (2026-07-29)": a ausencia de containment de
path (zip-slip) e de bound de tamanho descomprimido (zip-bomb) na extracao de imagens do EPUB e
**divida de codigo real** contra `.claude/rules/csharp.md` secao 4, com file:line de evidencia e a
correcao esperada. Registrado tambem que a regra `translatereader-zip-slip` do Semgrep **nao pega
este caso** (ela casa `Path.Combine($DEST, $ENTRY.FullName)`, e o codigo real usa uma variavel
intermediaria vinda da API do VersOne.Epub) — ou seja, o gate de CI da falsa sensacao de cobertura
aqui. Candidato a phase de hardening.
