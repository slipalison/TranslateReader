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
- **Goal:** threshold de cobertura no coverlet (80% pos-boundary D-2) + workflow de CI com build e testes

## Candidatos detectados (NAO aprovados)

Lacunas encontradas na varredura do repositorio, com evidencia. Servem so como ponto de
partida para a conversa — nenhuma foi escolhida pelo usuario e nenhuma e uma phase.

| Candidato | Evidencia no repo |
|---|---|
| Bookmarks ponta a ponta | `Bookmark` + 3 operacoes em `IReadingStateAccess` existem, mas `IReadingManager` nao expoe nenhuma e nao ha UI; README anuncia a feature |
| Backend nativo LLM em Android/iOS | `LLamaSharp.Backend.*` so referenciado sob `Condition ... == 'windows'`; Fase 7 de `docs/translation-feature-plan.md` nao implementada |
| Tela de detalhe do livro | README documenta `BookDetailPage` + `BookDetailPageModel`; arquivos nao existem no repo |
| Gate de cobertura + CI | `coverlet.collector` referenciado mas sem threshold; nenhum workflow de CI no repo |
| Busca dentro do livro | `ILibraryManager.SearchBooksAsync` busca so na biblioteca; nao ha full-text no conteudo do capitulo |
| Baseline de estilo | sem `.editorconfig`, sem `.gitattributes`, sem analyzers configurados |

## Convencoes de phase

- Slug canonico, sem prefixo numerico (`NN-` e so posicao de exibicao)
- Status de phase e derivado dos artefatos da pasta da phase
  (`SHIPPED.md` -> done, `REVIEW` -> verified, `SUMMARY` -> executed,
  `PLAN` -> planned, `CONTEXT` -> discussed), nunca escrito aqui
- Sem ponteiro de phase atual neste arquivo — evita conflito entre branches paralelas
