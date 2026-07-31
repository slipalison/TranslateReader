# Phase 10: README completo com badges — Context (slug: readme)

## Goal
README preciso e bem explicado — badges de pipeline/CodeQL/Sonar/Scorecard/licenca, feature de
traducao offline documentada, tabela de componentes completa (16 servicos), estrutura de pastas
real (Core vs App vs Tests), comandos de build corrigidos e licenca Apache 2.0.

## Locked decisions
(texto completo de cada uma em `.jdi/DECISIONS.md`)
- D-2026-07-29-readme-1: escopo geral do card + os 9 defeitos factuais (a)-(i) do README atual
  (privado=falso; feature de traducao ausente; tabela de 6/16 servicos; estrutura de 1 projeto
  em vez de 3; BookDetailPage documentado como existente; build `-f` sem csproj; Modelos de
  Dados incompletos; `.idea/` gitignorado na arvore; temas sem Sepia). Nenhuma feature futura
  descrita como pronta; pt-BR sem acentos (padrao do arquivo); badges so p/ workflow existente.
- D-2026-07-29-readme-2: conjunto e ordem final de badges — Pipeline, CodeQL (badge PROPRIO,
  nao dobrado no Pipeline — `codeql.yml` e hibrido `workflow_call`+`schedule`+`workflow_dispatch`
  e tem execucao independente no cron semanal), OpenSSF Scorecard (mantido), SonarCloud Quality
  Gate, SonarCloud Coverage, License (Apache 2.0). URLs reais, project key `slipalison_
  TranslateReader` / org `slipalison` confirmados em `sonarqube.yml`.
- D-2026-07-29-readme-3 (defeito NOVO, achado nesta fase, fora da lista a-i): tabela
  "Plataformas Suportadas" fica enganosa por omissao ao entrar a feature de traducao — ela so
  roda em Windows hoje (`LLamaSharp` backends condicionados a `'windows'`). Mitigacao: ressalva
  explicita junto da descricao da feature de traducao, referenciando a phase `llm-mobile`; a
  tabela de plataformas em si nao muda.
- D-2026-07-29-readme-4: 4 conteudos novos exigidos alem dos defeitos (a)-(i): secao de
  Seguranca (SECURITY.md + scanners + SHA pin), como rodar testes + regra de cobertura 90% em
  codigo novo (D-6), ponteiro Contributing/JDI (`CLAUDE.md`), licenca Apache 2.0.

## Canonical refs
- Card colado via `/jdi-issue` (sem URL externo) — texto integral em D-2026-07-29-readme-1.
- `README.md` atual (160 linhas), lido por completo — todo defeito (a)-(i) confere com o texto
  real nas linhas citadas em D-2026-07-29-readme-1.
- `CLAUDE.md` § "Arquitetura: The Method" > tabela de Componentes — fonte das 16 descricoes
  reais de servico (reusar texto, nao inventar).
- `PROJECT.md` § Stack e § Existing assets — inventario real de 16 servicos, 7 tabelas SQLite,
  capacidades ja entregues (inclui traducao offline completa).
- `.github/workflows/{pipeline,codeql,sonarqube}.yml` — lidos por completo: confirmam triggers
  (`pipeline.yml`: push main/pull_request/workflow_dispatch; `codeql.yml`: workflow_call+
  schedule+workflow_dispatch), project key/org do Sonar (`slipalison_TranslateReader` /
  `slipalison`).
- `SECURITY.md`, `LICENSE` (Apache 2.0 confirmado no cabecalho), `.gitignore:29` (`.idea/`),
  `TranslateReader.slnx` (3 projetos, sem referencia a `.idea/` — ja corrigido em `ci-seguranca`).
- SonarCloud badge format confirmado por pesquisa web (1/2): `sonarcloud.io/api/
  project_badges/measure?project=<key>&metric=<metric>` (metrics `alert_status`, `coverage`).

## Out of scope
- Qualquer mudanca em codigo `.cs`, workflows `.yml` ou `.slnx` — esta phase e README-only.
- Construir `BookDetailPage`/`BookDetailPageModel` — pertence a phase `detalhe-livro`; aqui so
  vira secao de roadmap.
- Backends LLamaSharp mobile (Android/iOS) — pertence a phase `llm-mobile`; aqui so vira ressalva
  no texto (D-2026-07-29-readme-3).
- Execucao real de scanners/badges (primeira run verde, decoracao de PR do Sonar) — ja e
  `## Deferred to PR review` das phases `ci-seguranca`/`pipeline-unificada`, nao se repete aqui.
- Renomear ou reordenar workflows — fora do escopo README.

## Definition of Done

### Auto-verifiable
- [ ] Defeito (a): secao Licenca nao afirma mais "Projeto privado" (FALSO — repo publico com
      `LICENSE` Apache 2.0); menciona Apache 2.0 e o arquivo `LICENSE`
      **Verify:** `test "$(grep -c "Projeto privado" README.md)" = "0" && grep -qi "apache" README.md && grep -q "LICENSE" README.md && test -f LICENSE`
      **Source:** CONTEXT
- [ ] Defeito (b) + defeito novo D-2026-07-29-readme-3: feature de traducao offline documentada
      (LLamaSharp, modelo GGUF, cache por hash, job com pause/resume) com a ressalva de que hoje
      so roda em Windows, referenciando a phase `llm-mobile`
      **Verify:** `grep -qi "LLamaSharp" README.md && grep -qi "GGUF" README.md && grep -qi "cache" README.md && grep -Eqi "pause|retom" README.md && grep -q "llm-mobile" README.md`
      **Source:** CONTEXT
- [ ] Defeito (c): tabela de Componentes lista os 16 servicos reais (4 Manager + 3 Engine +
      6 Access + 3 Utility), nao mais 6
      **Verify:** `for s in ReadingManager LibraryManager TranslationManager SettingsManager ParsingEngine TranslationEngine ThemeEngine BooksAccess ReadingStateAccess SettingsAccess TranslationCacheAccess ModelAccess BookTranslationJobAccess FileUtility PromptUtility HtmlUtility; do grep -q "$s" README.md || exit 1; done`
      **Source:** CONTEXT
- [ ] Defeito (d): Estrutura do Projeto reflete os 3 projetos reais — `TranslateReader.Core`
      (Contracts/Business/Access/Utilities/Models), `TranslateReader` (app MAUI) e
      `TranslateReader.Tests`, nao mais 1 projeto so
      **Verify:** `grep -q "src/TranslateReader.Core" README.md && grep -q "test/TranslateReader.Tests" README.md && grep -B15 "Contracts/" README.md | grep -q "TranslateReader.Core"`
      **Source:** CONTEXT
- [ ] Defeito (e): `BookDetailPage.xaml`/`BookDetailPageModel.cs` nao aparecem como arquivos
      existentes (nao existem no repo); se citados, so numa secao de roadmap referenciando a
      phase `detalhe-livro`
      **Verify:** `test "$(grep -c "BookDetailPage.xaml" README.md)" = "0" && test "$(grep -c "BookDetailPageModel.cs" README.md)" = "0" && grep -q "detalhe-livro" README.md`
      **Source:** CONTEXT
- [ ] Defeito (f): comandos de build referenciam o csproj do app (nao mais `-f <TFM>` bare a
      nivel de solution, que falha NETSDK1005); `dotnet test` presente para rodar os testes
      **Verify:** `test "$(grep -c "dotnet build -f " README.md)" = "0" && grep -q "TranslateReader.csproj" README.md && grep -q "dotnet test" README.md`
      **Source:** CONTEXT
- [ ] Defeito (g): Modelos de Dados inclui `Settings`, `TranslationCache` e `BookTranslationJob`
      (faltavam por completo)
      **Verify:** `grep -q "TranslationCache" README.md && grep -q "BookTranslationJob" README.md && grep -q "OriginalHash" README.md`
      **Source:** CONTEXT
- [ ] Defeitos (h) + (i): `.idea/` removido da arvore de Estrutura do Projeto (gitignorado);
      temas listam Light/Dark/**Sepia**, nao so "claro/escuro"
      **Verify:** `test "$(grep -c "\.idea" README.md)" = "0" && grep -qi "Sepia" README.md`
      **Source:** CONTEXT
- [ ] Badges (D-2026-07-29-readme-2): os 6 badges presentes com URL real e resolvivel (Pipeline,
      CodeQL, OpenSSF Scorecard, SonarCloud Quality Gate, SonarCloud Coverage, License); nenhuma
      badge referencia arquivo de workflow ausente de `.github/workflows/`
      **Verify:** `grep -q "actions/workflows/pipeline.yml/badge.svg" README.md && grep -q "actions/workflows/codeql.yml/badge.svg" README.md && grep -q "api.scorecard.dev/projects/github.com/slipalison/TranslateReader/badge" README.md && grep -q "sonarcloud.io/api/project_badges/measure?project=slipalison_TranslateReader&metric=alert_status" README.md && grep -q "sonarcloud.io/api/project_badges/measure?project=slipalison_TranslateReader&metric=coverage" README.md && grep -qi "apache" README.md && for f in $(grep -oE "actions/workflows/[A-Za-z0-9_.-]+\.yml" README.md | sed -E "s#actions/workflows/##" | sort -u); do test -f ".github/workflows/$f" || exit 1; done`
      **Source:** CONTEXT
- [ ] D-2026-07-29-readme-4: 4 secoes novas presentes — Seguranca (`SECURITY.md` + CodeQL +
      Semgrep + SonarCloud/SonarQube + Scorecard + SHA pin), como rodar testes + regra de
      cobertura 90% (D-6), ponteiro Contributing/JDI
      **Verify:** `grep -q "SECURITY.md" README.md && grep -qi "CodeQL" README.md && grep -qi "Semgrep" README.md && grep -Eqi "SonarCloud|SonarQube" README.md && grep -qi "Scorecard" README.md && grep -qi "SHA" README.md && grep -q "90%" README.md && grep -qi "JDI" README.md`
      **Source:** CONTEXT

### Manual
- _(none)_

## Deferred to PR review
- Renderizacao visual dos 6 badges (o badge do Pipeline so fica "verde" de verdade depois do
  merge desta branch, que sai de `jdi/pipeline-unificada` PR #7; CodeQL depende do primeiro
  run agendado ou de PR real).
- Julgamento subjetivo de "bem explicado" pedido no card — nao e medivel por grep.
- Dashboard real do SonarCloud mostrando Quality Gate/coverage com dados pos-merge.
- Leitura humana de que a ordem/redacao das novas secoes (Seguranca, Testes, Contributing) fica
  didatica e nao so tecnicamente correta.

## Notes
- Fase README-only: sem `.cs`/`.yml` novo ou alterado -> Gate de cobertura (D-6) do reviewer
  reporta SKIPPED, esperado (mesmo padrao de `ci-seguranca`/`pipeline-unificada`).
- Reusar literalmente as descricoes de componente da tabela de `CLAUDE.md` para os 10 servicos
  que faltam — nao inventar volatilidade encapsulada nova.
- Ordem sugerida ao planner: (1) corrigir os 9 defeitos (a)-(i) primeiro, sao os mais objetivos;
  (2) adicionar as 4 secoes novas (D-2026-07-29-readme-4); (3) badges por ultimo (dependem do
  texto de Seguranca/Build já estar correto pra fazer sentido no contexto).
  Cada bullet do DoD acima e escopo de checagem, nao ordem de execucao.
- `npx jdi-cli` quebrado neste ambiente Windows — nenhum passo do doer/reviewer deve depender
  dele.
