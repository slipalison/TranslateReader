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

### Phase 11: Zip-slip e bound de descompressao no EPUB
- **Slug:** epub-zip-slip
- **Goal:** containment de path e limite de tamanho descomprimido na extracao de EPUB (input nao confiavel) + corrigir a regra Semgrep que hoje nao cobre o caminho real — duas entregas, senao o defeito volta invisivel

### Phase 10: README completo com badges
- **Slug:** readme
- **Goal:** README preciso e bem explicado — badges de pipeline/CodeQL/Sonar/Scorecard/licenca, feature de traducao offline documentada, tabela de componentes completa (4 Manager/3 Engine/6 Access/3 Utility), estrutura de pastas real (Core vs App), comandos de build corrigidos e licenca Apache 2.0

### Phase 9: Pipeline unificada (orquestrador reusable)
- **Slug:** pipeline-unificada
- **Goal:** consolidar os fluxos de push/PR num orquestrador unico `pipeline.yml` via `workflow_call` — um run graph com build, testes, CodeQL, Semgrep, SCA, secrets, Sonar, dependency-review e SBOM; `scorecard.yml` (requisito OSSF) e `release.yml` (trigger de tag) permanecem isolados; branch protection re-mapeada para os novos nomes de check

### Phase 12: Rede de testes de regressao
- **Slug:** regression-suite
- **Goal:** fixar o comportamento observavel de hoje em testes de caracterizacao, para que qualquer alteracao futura (em especial o refactor da phase `the-method-refactor`) quebre um teste em vez de quebrar o app — cobrindo os caminhos do Core hoje sem teste, e decidindo explicitamente o que fazer com as 1516 linhas do projeto MAUI que o test project atual nao alcanca

### Phase 13: Refactor The Method + memoria/CPU mobile
- **Slug:** the-method-refactor
- **Goal:** eliminar as violacoes concretas de The Method (CLAUDE.md) e de `.claude/rules/csharp.md` e os hotspots reais de memoria/CPU (ParsingEngine, HtmlUtility, TranslationEngine, loops de paragrafo/token, LOH), cada mudanca justificada por uma violacao nomeada e protegida pela rede da phase `regression-suite` — finding-driven, nao rewrite amplo

### Phase 14: Zerar as issues do SonarQube e travar a regressao
- **Slug:** sonar-zero-issues
- **Goal:** resolver as 113 issues abertas do SonarQube em `main` (2 bugs, 7 vulnerabilities, 104 code smells) e instalar o mecanismo que impede a classe delas de voltar — cada issue resolvida no codigo, excluida da analise com justificativa registrada, ou suprimida com waiver auditavel; nunca silenciada sem registro

### Phase 15: Cobertura de 90% no SonarQube sem issues novas
- **Slug:** coverage-90
- **Goal:** escrever os testes que faltam ate a cobertura medida pelo SonarQube atingir 90%, sem introduzir nenhuma issue nova — partindo de 75,9% em `main` (1428 lines to cover, 329 descobertas, das quais 195 sao JS do WebView sem harness nenhum no repo)

### Phase 17: Traducao cega a paragrafo em `<div>` (EPUB de calibre)
- **Slug:** div-paragraph-translation
- **Goal:** traduzir o texto de EPUBs cujos paragrafos sao `<div>` e nao `<p>` — hoje `HtmlUtility.ExtractTextBlocks` casa so `p|h1-h6|li` e, num livro real do usuario, enxergou 360 de 2.274 blocos (11,2% do texto), gerou o EPUB "traduzido" com 48 de 53 documentos ainda em ingles e nao avisou nada; entrega tambem o sinal de cobertura de traducao, para que um livro de formato inesperado deixe de falhar em silencio
### Phase 16: Validacao funcional e performance da conversao
- **Slug:** conversion-performance
- **Goal:** provar por teste que conversao de livro, extracao de imagens e download de modelo funcionam de ponta a ponta em livro CURTO e em livro GRANDE (fixtures de 1,7 MB / 27 capitulos e 32 MB / 256 imagens ja no repo), e corrigir os gargalos nomeados que a validacao expuser — comecando pelo `ExtractAllImagesAsync`, que hoje materializa 44 MB de imagens num unico dicionario (229 alocacoes na LOH), contra `.claude/rules/csharp.md` §2.3

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
