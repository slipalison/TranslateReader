# Phase 10: README completo com badges — Plan  (slug: readme)

## Goal
README preciso e bem explicado — badges de pipeline/CodeQL/Sonar/Scorecard/licenca, feature de
traducao offline documentada, tabela de componentes completa (16 servicos), estrutura de pastas
real (Core vs App vs Tests), comandos de build corrigidos e licenca Apache 2.0.

## Locked decisions (from CONTEXT.md)
- D-2026-07-29-readme-1: 9 defeitos factuais (a)-(i); pt-BR SEM acentos; nada de feature futura
  descrita como pronta; badge so p/ workflow existente.
- D-2026-07-29-readme-2: 6 badges, ordem locked — Pipeline, CodeQL, Scorecard, Sonar Quality
  Gate, Sonar Coverage, License.
- D-2026-07-29-readme-3: ressalva "traducao offline: hoje somente Windows" apontando `llm-mobile`;
  a tabela de Plataformas Suportadas NAO muda.
- D-2026-07-29-readme-4: 4 secoes novas — Seguranca, Testes+cobertura 90% (D-6), Contributing/JDI,
  Licenca Apache 2.0.
- D-6: 90% de cobertura em codigo novo/alterado pos-boundary `4285f25`.

## HAZARD DE ORDENACAO — leia antes de executar
**Todas as tasks editam o MESMO arquivo (`README.md`). NAO EXISTE PARALELISMO nesta phase.**
Modelo: **single-writer sequencial** — T-1 -> T-2 -> ... -> T-7, um commit por task, cada task
faz `git add README.md && git commit` antes da proxima comecar. Rodar duas tasks em paralelo
gera conflito de escrita e perda silenciosa de secao. As waves abaixo sao 1:1 com as tasks
justamente para tornar isso explicito, nao para sugerir concorrencia.

Ordem herdada do CONTEXT: (1) defeitos (a)-(i) -> (2) secoes novas -> (3) badges por ultimo.

## Fatos verificados no repo (usar, nao re-descobrir)
- LLamaSharp **0.27.0** (Core.csproj:19; backends Cpu/Cuda12 0.27.0 no app csproj:85-86, sob
  `ItemGroup Condition ... == 'windows'` — origem da ressalva D-...-3). VersOne.Epub 3.3.6,
  Microsoft.Data.Sqlite.Core 10.0.10, CommunityToolkit.Mvvm 8.4.2, CommunityToolkit.Maui 14.2.2.
- Comandos canonicos (copiados de `.github/workflows/ci.yml:30,57`):
  `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0`
  `dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release --collect:"XPlat Code Coverage"`
- `.github/workflows/` tem 11 workflows: pipeline, ci, codeql, semgrep, sca, sbom, secret-scan,
  dependency-review, scorecard, sonarqube, release. `LICENSE` e `SECURITY.md` existem na raiz.
- Estrutura real: `src/TranslateReader.Core/{Contracts/{Managers,Engines,Access,Utilities},
  Business/{Managers,Engines},Access,Utilities,Models}`, `src/TranslateReader/{Pages,Pages/Controls,
  PageModels,Serialization,Utilities,Resources,Platforms}`, `test/TranslateReader.Tests/`.
  Pages reais: LibraryPage, ReaderPage, Controls/{SettingsOverlay,TranslateBookPopup}. **Nao existe
  BookDetailPage.** `BookTranslationJob` = Id, BookId, SourceLanguage, TargetLanguage, Status,
  LastCompletedChapterIndex, CreatedAt, UpdatedAt.
- Fonte das 16 descricoes de componente: tabela de `CLAUDE.md` § Componentes do Sistema —
  **reusar o texto literal**, nao inventar volatilidade.

## Tasks

### Wave 1

#### T-1: Funcionalidades + Stack com traducao offline, ressalva Windows-only e temas Sepia
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `README.md`
- **Acceptance:**
  - `grep -qi "LLamaSharp" README.md && grep -qi "GGUF" README.md && grep -qi "cache" README.md && grep -Eqi "pause|retom" README.md && grep -q "llm-mobile" README.md && grep -q "0.27.0" README.md`
  - Ressalva + temas: `grep -q "somente Windows" README.md && grep -qi "Sepia" README.md && test "$(grep -c "claro/escuro" README.md)" = "0"`
  - Sem acento no texto novo: `! grep -nP "[\x{00C0}-\x{00FF}]" README.md` (LGTM se vazio)
- **Dependencies:** none
- **Test:** DoD (b) + D-...-3 + parte de (i); grep battery acima
- **DoD:** (b)+readme-3, (i)
- **Status:** pending

### Wave 2

#### T-2: Tabela de Componentes com os 16 servicos reais + diagrama de camadas atualizado
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `README.md`
- **Acceptance:**
  - Os 16 nomes presentes em code-span: `for s in ReadingManager LibraryManager TranslationManager SettingsManager ParsingEngine TranslationEngine ThemeEngine BooksAccess ReadingStateAccess SettingsAccess TranslationCacheAccess ModelAccess BookTranslationJobAccess FileUtility PromptUtility HtmlUtility; do grep -q "\`$s\`" README.md || { echo "FALTA $s"; exit 1; }; done`
  - Exatamente 16 linhas de tabela: ``test "$(grep -cE '^\| `[A-Za-z]+(Manager|Engine|Access|Utility)` \|' README.md)" = "16"``
  - Cada servico existe no codigo: `for s in <mesma lista>; do test -f "src/TranslateReader.Core/Business/Managers/$s.cs" -o -f "src/TranslateReader.Core/Business/Engines/$s.cs" -o -f "src/TranslateReader.Core/Access/$s.cs" -o -f "src/TranslateReader.Core/Utilities/$s.cs" || exit 1; done`
- **Dependencies:** T-1
- **Test:** DoD (c); loop de 16 nomes + contagem de linhas
- **DoD:** (c)
- **Status:** pending

### Wave 3

#### T-3: Estrutura do Projeto real (3 projetos, sem `.idea/`) + secao Roadmap
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `README.md`
- **Acceptance:**
  - `grep -q "src/TranslateReader.Core" README.md && grep -q "test/TranslateReader.Tests" README.md && grep -B15 "Contracts/" README.md | grep -q "TranslateReader.Core" && test "$(grep -c "\.idea" README.md)" = "0"`
  - Nada inexistente na arvore: `test "$(grep -c "BookDetailPage.xaml" README.md)" = "0" && test "$(grep -c "BookDetailPageModel.cs" README.md)" = "0"`; e as pastas citadas existem: `test -d src/TranslateReader.Core/Contracts && test -d src/TranslateReader/PageModels && test -d src/TranslateReader/Pages/Controls && test -f test/TranslateReader.Tests/TranslateReader.Tests.csproj`
  - Roadmap marca o nao-construido como PLANEJADO: `grep -qi "planejad" README.md && for p in detalhe-livro bookmarks busca-no-livro llm-mobile baseline-de-estilo cobertura-e-ci; do grep -q "$p" README.md || exit 1; done`
- **Dependencies:** T-2
- **Test:** DoD (d), (e), (h); greps + assercoes de existencia de pasta
- **DoD:** (d), (e), (h)
- **Status:** pending

### Wave 4

#### T-4: Modelos de Dados com as 7 tabelas (Settings, TranslationCache, BookTranslationJob)
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `README.md`
- **Acceptance:**
  - `for m in Book Chapter ReadingProgress Bookmark Settings TranslationCache BookTranslationJob; do grep -q "$m" README.md || exit 1; done`
  - Campos-chave conferem com a fonte: `grep -q "OriginalHash" README.md && grep -q "LastCompletedChapterIndex" README.md && grep -q "UNIQUE" README.md`
  - Nomes batem com `CLAUDE.md` § Modelos de Dados e `src/TranslateReader.Core/Models/BookTranslationJob.cs` (sem campo inventado)
- **Dependencies:** T-3
- **Test:** DoD (g); loop de 7 modelos + campos
- **DoD:** (g)
- **Status:** pending

### Wave 5

#### T-5: Build/Execucao corrigido + secao Testes/Cobertura 90% + ponteiro Contributing/JDI
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `README.md`
- **Acceptance:**
  - Sem `-f` bare (NETSDK1005, learning ci-seguranca W-5): `test "$(grep -c "dotnet build -f " README.md)" = "0" && grep -q "src/TranslateReader/TranslateReader.csproj" README.md`
  - `grep -q "dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj" README.md && grep -q "90%" README.md && grep -q "CLAUDE.md" README.md && grep -qi "JDI" README.md`
  - Todo csproj citado existe + threshold automatico descrito como PLANEJADO (nao existe gate hoje): `for p in $(grep -oE "(src|test)/[A-Za-z./]+\.csproj" README.md | sort -u); do test -f "$p" || exit 1; done && grep -q "cobertura-e-ci" README.md`
- **Dependencies:** T-4
- **Test:** DoD (f) + D-...-4 itens (2) e (3)
- **DoD:** (f), D-...-4(2), D-...-4(3)
- **Status:** pending

### Wave 6

#### T-6: Secao Seguranca + Licenca Apache 2.0 (mata "Projeto privado")
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `README.md`
- **Acceptance:**
  - `test "$(grep -c "Projeto privado" README.md)" = "0" && grep -qi "apache" README.md && grep -q "LICENSE" README.md && test -f LICENSE`
  - `grep -q "SECURITY.md" README.md && test -f SECURITY.md && grep -qi "CodeQL" README.md && grep -qi "Semgrep" README.md && grep -Eqi "SonarCloud|SonarQube" README.md && grep -qi "Scorecard" README.md && grep -qi "SHA" README.md`
  - Todo scanner citado tem workflow real: `for w in codeql semgrep sca secret-scan sbom dependency-review scorecard sonarqube; do test -f ".github/workflows/$w.yml" || exit 1; done`
- **Dependencies:** T-5
- **Test:** DoD (a) + D-...-4 itens (1) e (4)
- **DoD:** (a), D-...-4(1), D-...-4(4)
- **Status:** pending

### Wave 7

#### T-7: Bloco de 6 badges na ordem locked + bateria completa de DoD + SUMMARY.md
- **Specialist:** jdi-doer-translatereader
- **Files modified:** `README.md`, `.jdi/phases/readme/SUMMARY.md`
- **Acceptance:**
  - Os 6 badges presentes: `grep -q "actions/workflows/pipeline.yml/badge.svg" README.md && grep -q "actions/workflows/codeql.yml/badge.svg" README.md && grep -q "api.scorecard.dev/projects/github.com/slipalison/TranslateReader/badge" README.md && grep -q "sonarcloud.io/api/project_badges/measure?project=slipalison_TranslateReader&metric=alert_status" README.md && grep -q "sonarcloud.io/api/project_badges/measure?project=slipalison_TranslateReader&metric=coverage" README.md && grep -q "shields.io" README.md`
  - Ordem locked + nenhum badge para workflow ausente: `python -c "import sys;t=open('README.md',encoding='utf-8').read();o=['workflows/pipeline.yml/badge.svg','workflows/codeql.yml/badge.svg','api.scorecard.dev','metric=alert_status','metric=coverage','shields.io'];p=[t.find(x) for x in o];sys.exit(0 if all(i>=0 for i in p) and p==sorted(p) else 1)"` **e** `for f in $(grep -oE "actions/workflows/[A-Za-z0-9_.-]+\.yml" README.md | sed -E "s#actions/workflows/##" | sort -u); do test -f ".github/workflows/$f" || exit 1; done`
  - Resolvibilidade: hard-fail nos 3 externos `curl -sfI -o /dev/null "<scorecard>" && curl -sfI -o /dev/null "<sonar alert_status>" && curl -sfI -o /dev/null "<sonar coverage>"`; warn-only nos 2 do Actions `for f in pipeline codeql; do gh api "repos/slipalison/TranslateReader/actions/workflows/$f.yml" --jq .state >/dev/null || echo "WARN: $f.yml ainda nao registrado no remote (branch nao mergeada — Deferred to PR review)"; done`
  - Bateria final: rodar os 10 comandos `**Verify:**` do CONTEXT.md em sequencia, todos verdes; `SUMMARY.md` escrito com o resultado item a item
- **Dependencies:** T-6
- **Test:** DoD de badges (D-...-2) + re-run dos 10 itens auto-verificaveis
- **DoD:** badges (D-...-2) + revalidacao de (a)-(i) e D-...-4
- **Status:** pending

## Execution
- Total tasks: 7
- Waves: 7
- Estimated parallel speedup: **1x — paralelismo ZERO por design** (7 tasks, 1 unico arquivo de
  producao). Waves 1:1 com tasks para forcar o modelo single-writer.

| Wave | Task | Escopo da secao | DoD coberto |
|---|---|---|---|
| 1 | T-1 | Funcionalidades + Stack + ressalva Windows-only + Sepia | (b)+readme-3, (i) |
| 2 | T-2 | Componentes (16) + diagrama de camadas | (c) |
| 3 | T-3 | Estrutura do Projeto + Roadmap | (d), (e), (h) |
| 4 | T-4 | Modelos de Dados (7 tabelas) | (g) |
| 5 | T-5 | Build/Execucao + Testes/Cobertura + Contributing | (f), D-4(2)(3) |
| 6 | T-6 | Seguranca + Licenca | (a), D-4(1)(4) |
| 7 | T-7 | Badges + bateria de DoD + SUMMARY.md | badges D-2 + re-run total |

## Files modified (all tasks)
- `README.md` (T-1..T-7 — arquivo unico de producao, escrita serializada)
- `.jdi/phases/readme/SUMMARY.md` (T-7)

## Test requirements
- Tipo: verificacao documental por grep/`test -f`/`curl`/`gh api` — nao ha `.cs` novo.
- Comando de regressao (deve seguir verde apos a phase):
  `dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release` (baseline 167 testes)
- Cobertura (D-6, 90% pos-boundary): **SKIPPED esperado** — phase README-only, sem codigo
  novo/alterado. Mesmo padrao de `ci-seguranca`/`pipeline-unificada`.
- Commits: Conventional Commits, escopo `readme`, mensagem em ingles, 1 task = 1 commit.
- `npx jdi-cli` esta quebrado neste ambiente Windows — nenhum passo pode depender dele.
