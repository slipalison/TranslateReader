# Todos — sessao de discuss `snippet-translation` (2026-08-09)

Itens levantados na captura de decisoes e conscientemente empurrados para fora do escopo
(ver `## Out of scope` em `.jdi/phases/snippet-translation/CONTEXT.md`).

- **[READER] Liberar a traducao por paragrafo no modo Rolagem.** `D-2026-08-09-snippet-translation-3`
  mantem `ReaderPage.xaml.cs:326-330` (`DisplayAlert` "A traducao funciona apenas no modo Paginado")
  intacto: a camada de TRECHOS funciona nos dois modos, a de PARAGRAFO nao. Fica a assimetria de que
  o usuario seleciona um periodo e traduz em rolagem, mas nao consegue traduzir a pagina inteira ali.
  Custo real: `getVisibleParagraphs` e amarrado a `_pager`/`_currentPage` e precisaria da mesma raiz
  parametrizada (`_snippetRoots`) que esta phase constroi — depois desta phase o caminho ja existe e
  a phase futura vira quase so fiacao. Ficou fora porque mexe em `translation.js`, que esta protegido
  por gate estrutural da phase 18, e o requisito do usuario nao pediu isso.

- **[I18N] O app nao tem infra de i18n; os mockups v0.2.0 tem pt E en.** As chaves novas
  (`selectHint`, `extendTip`, `sentenceOne/Many`, `translateSnip`, `extendSel`, `shrinkSel`,
  `onlySentence`, `toggleSnip`, `removeSnip`) entram como strings pt-BR vindas do C# via
  `setSnippetLabels(...)` — `snippets.js` nao carrega literal pt-BR, respeitando a regra de i18n de
  `CLAUDE.md`. O conjunto `en` do mockup NAO e implementado: nao existe seletor de idioma de UI no
  app, e inventar um aqui seria feature nova. Quando i18n de verdade entrar no roadmap, essas chaves
  ja estao isoladas num unico ponto de injecao.

- **[I18N] Chaves declaradas e nao usadas no mockup: `snipTitle`, `snipLoading`, `copyTitle`,
  `copied`.** Conferido nos dois templates renderizados — nenhuma aparece no HTML. `snipTitle` e
  `snipLoading` sao redundantes (o estado de loading e comunicado pelo blob pulsando, `trPulse`, nao
  por texto). `copyTitle`/`copied` implicam uma feature de COPIAR a traducao do trecho que nao existe
  em lugar nenhum do mockup nem do app. Ficaram fora. Copiar trecho traduzido e um pedido plausivel
  de UX e vira phase propria se o usuario quiser.

- **[UX] Selecao restrita a UM paragrafo.** Regra do mockup (`sel = {p, anchor, set[]}`; tocar em
  periodo de outro paragrafo REINICIA a selecao), mantida como esta. Selecionar um trecho que
  atravessa a fronteira de paragrafos exigiria ancoragem multi-bloco e um modelo de dados diferente
  do de `D-2026-08-09-snippet-translation-1`. Nao foi pedido.

- **[UX] Sem UI para gerenciar os trechos de um livro.** Nao ha tela de "meus trechos traduzidos",
  nem contador, nem limpeza em massa. `RemoveSnippetsForBookAsync` existe no Access para o caminho de
  exclusao do livro, mas nao tem botao. Se um livro acumular centenas de trechos, so da para remove-los
  um a um pelo X do chip.

- **[PERF] Sem lazy mount dos spans em rolagem.** `D-2026-08-09-snippet-translation-3` rejeitou criar
  os spans so do capitulo visivel. Em rolagem o app pode ter varios `.chapter-content` montados, e
  cada paragrafo vira N spans. Nenhuma medicao foi feita — se doer em livro grande no Android, a saida
  ja esta nomeada (montar por `.chapter-content` sob IntersectionObserver).

- **[PERF] Re-medicao dos blobs.** O mockup remede em `componentDidUpdate` e no `resize` da janela.
  No app faltam dois gatilhos que o mockup nao tem: mudanca de fonte/tamanho/espacamento pelo
  SettingsOverlay (que reflui o texto) e troca de pagina no pager. A phase precisa cobri-los, mas
  nenhum debounce/throttle foi decidido — se a re-medicao pesar durante o drag de um slider de
  tipografia, throttle e a saida e nao foi especificada aqui.

- **[DESIGN] Comentarios apontando para caminhos que mudaram.** `DesignTokens.xaml:6` cita
  `design/DESIGN-REFERENCE.md` e a linha 86 cita `design/PIXEL-SPEC.md`; ambos foram movidos para
  `design/v0.1.0/`. Nenhum teste ou script le esses caminhos (so comentario), entao nao bloqueia nada
  — mas os comentarios estao mentindo.

- **[DESIGN] `design/v0.2.0/` nao tem DESIGN-REFERENCE.md.** A v0.1.0 tem. Esta phase gera so a
  PIXEL-SPEC (medidas dos elementos novos), nao a referencia de design completa da v0.2.0.
