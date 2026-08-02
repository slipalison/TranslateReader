## De `app-redesign` (2026-08-02)

- **[FEATURE, rejeitada com razao, precisa de mockup novo]** Toggle grid/list na Biblioteca. O par de
  botoes de `desktop-library.jpg` nao e portado (D-2026-08-02-app-redesign-6): no prototipo ele e
  decorativo e nao existe NENHUM screenshot de list view pra replicar. Pra fazer de verdade:
  desenhar a list view primeiro, depois implementar.

- **[FEATURE, adiada]** "Recentes" como destino de navegacao real (rota + pagina) e o write path de
  `Book.LastOpenedAt`. Achado: a coluna existe no schema, e lida e ordenada por
  `FetchAllBooksAsync`, mas NUNCA e escrita por codigo de producao — esta morta desde a adocao.
  Nesta phase "Recentes" virou filtro na propria `LibraryPage` sobre `ReadingProgress.UpdatedAt`
  (dado real), ver D-2026-08-02-app-redesign-5. Se um dia quiser separar "aberto" de "lido", ai sim
  vale criar `IBooksAccess.TouchLastOpenedAsync` + chamada em `ReadingManager.OpenBookAsync` + testes.

- **[FEATURE, adiada]** TOC de capitulos no mobile. O painel entregue aqui aparece so no idiom
  Desktop porque `mobile-reader.jpg` nao tem hamburguer (D-2026-08-02-app-redesign-4). Precisa de
  decisao de design pra onde encaixar o gatilho na barra mobile (4 elementos ja ocupados).

- **[LIMPEZA]** `ILibraryManager.SearchBooksAsync(query)` fica redundante depois desta phase
  (`ListBookSummariesAsync(query)` cobre o caso da UI). Nao foi removida porque e baseline coberta
  por `LibraryManagerTests.SearchBooksAsync_FiltersCorrectly`. Candidata natural a ser absorvida pela
  phase `busca-no-livro`.

- **[REFACTOR]** `ITranslationManager` vai a 9 operacoes com o `GetSelectedModelStatusAsync`
  (D-2026-08-02-app-redesign-9) — ja estava acima do "3-5 ideal" de CLAUDE.md antes desta phase.
  Candidato a split de contrato (traducao vs ciclo de vida do modelo) na phase `the-method-refactor`.

- **[INFRA DE TESTE]** O test project (`net10.0`) referencia so o Core, entao NADA em
  `src/TranslateReader` (Pages/Controls/PageModels/converters) tem teste de unidade ou cobertura de
  linha (D-2026-08-02-app-redesign-10). Esta phase compensa com verificacao estrutural sobre os XAML
  (`DesignSystemTests`), o que NAO e teste de comportamento. Harness real (UI test instrumentado ou
  projeto de teste multi-TFM com MAUI hosting) e trabalho proprio, provavelmente junto de
  `regression-suite`/`coverage-90`.

- **[PRODUTO/DESIGN]** A chrome do app fica dark-only (`UserAppTheme = AppTheme.Dark`,
  D-2026-08-02-app-redesign-3) porque os dois mockups so existem em dark. Tema claro da chrome exige
  um mockup light antes. (O tema de LEITURA Claro/Escuro/Sepia do conteudo continua funcionando
  normalmente — sao coisas diferentes.)

- **[I18N]** Todas as strings de UI continuam pt-BR hardcoded no XAML/code-behind (o mockup tambem e
  pt-BR). Fora do escopo desta phase, mas continua sendo divida.
