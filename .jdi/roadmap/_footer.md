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
