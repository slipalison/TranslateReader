# TranslateReader — Roadmap (adotado)

## Status

adopted: true

## Contexto

Projeto adotado em 2026-07-28 sobre codigo pre-existente. O codigo ja implementado
**nao** entra neste roadmap — ele e contexto (ver `PROJECT.md > Existing assets`).
Aqui entram apenas features NOVAS a serem construidas via JDI.

## Phases

### Phase 1: Baseline de estilo
- **Slug:** baseline-de-estilo
- **Goal:** editorconfig, gitattributes e analyzers configurados na raiz

### Phase 2: Cobertura e CI
- **Slug:** cobertura-e-ci
- **Goal:** threshold de cobertura no coverlet (90% pos-boundary D-2, ver D-6) + workflow de CI com build e testes

### Phase 3: Bookmarks
- **Slug:** bookmarks
- **Goal:** completar o vertical morto — expor bookmarks em IReadingManager e entregar UI de criar/listar/remover

### Phase 4: Tela de detalhe do livro
- **Slug:** detalhe-livro
- **Goal:** BookDetailPage + BookDetailPageModel documentados no README passam a existir

### Phase 5: Busca dentro do livro
- **Slug:** busca-no-livro
- **Goal:** busca full-text no conteudo dos capitulos do livro aberto

### Phase 6: LLM em Android/iOS
- **Slug:** llm-mobile
- **Goal:** backends nativos LLamaSharp em Android/iOS — traducao offline roda em mobile (Fase 7 do translation-feature-plan)

### Phase 7: Pipeline CI/CD com seguranca + correcao do .slnx
- **Slug:** ci-seguranca
- **Goal:** corrigir TranslateReader.slnx (referencias a .idea/ gitignored) + GitHub Actions rigoroso: CodeQL, dependency review, secret scanning, OSSF Scorecard, build + testes, SonarQube e release automatizado

### Phase 8: Suplemento SAST/SCA/SBOM (paridade simulator-ccb)
- **Slug:** sast-sca-sbom
- **Goal:** Semgrep SAST com regras custom (zip-slip/XXE/WebView), gate SCA nativo dotnet (bloqueia CVE HIGH/CRITICAL), bump SQLitePCLRaw (GHSA-2m69-gcr7-jv3q), TruffleHog verified, SBOM Syft e SECURITY.md

### Phase 9: Pipeline unificada (orquestrador reusable)
- **Slug:** pipeline-unificada
- **Goal:** consolidar os fluxos de push/PR num orquestrador unico `pipeline.yml` via `workflow_call` — um run graph com build, testes, CodeQL, Semgrep, SCA, secrets, Sonar, dependency-review e SBOM; `scorecard.yml` (requisito OSSF) e `release.yml` (trigger de tag) permanecem isolados; branch protection re-mapeada para os novos nomes de check

## Evidencias de origem das phases

Todas as 6 phases vieram de lacunas detectadas na varredura da adocao e foram
aprovadas pelo usuario em 2026-07-28 (ver D-5 em DECISIONS.md):

| Phase (slug) | Evidencia no repo |
|---|---|
| `bookmarks` | `Bookmark` + 3 operacoes em `IReadingStateAccess` existem, mas `IReadingManager` nao expoe nenhuma e nao ha UI; README anuncia a feature |
| `llm-mobile` | `LLamaSharp.Backend.*` so referenciado sob `Condition ... == 'windows'`; Fase 7 de `docs/translation-feature-plan.md` nao implementada |
| `detalhe-livro` | README documenta `BookDetailPage` + `BookDetailPageModel`; arquivos nao existem no repo |
| `cobertura-e-ci` | `coverlet.collector` referenciado mas sem threshold; nenhum workflow de CI no repo |
| `busca-no-livro` | `ILibraryManager.SearchBooksAsync` busca so na biblioteca; nao ha full-text no conteudo do capitulo |
| `baseline-de-estilo` | sem `.editorconfig`, sem `.gitattributes`, sem analyzers configurados |

## Convencoes de phase

- Slug canonico, sem prefixo numerico (`NN-` e so posicao de exibicao)
- Status de phase e derivado dos artefatos da pasta da phase
  (`SHIPPED.md` -> done, `REVIEW` -> verified, `SUMMARY` -> executed,
  `PLAN` -> planned, `CONTEXT` -> discussed), nunca escrito aqui
- Sem ponteiro de phase atual neste arquivo — evita conflito entre branches paralelas
