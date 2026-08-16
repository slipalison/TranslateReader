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

- **[READER] RESOLVIDO (iter 8, 2026-08-10).** ~~Paragrafo com filhos elemento (`<em>`/`<a>`/`<img>`)
  virava UM periodo unico~~ (derivacao D do `PLAN.md`, marcada la como "a evolucao — split
  preservando markup no nivel de text node — e phase futura, nao debito escondido"). Entregue: uso
  real revelou que EPUBs tem markup inline em quase todo paragrafo, tornando a limitacao visivel de
  imediato (5o feedback do usuario, "nao consigo selecionar um periodo, esta selecionando o paragrafo
  inteiro"). `_wrapParagraph`/`_wrapMarkupParagraph` (`snippets.js`) agora localizam toda fronteira de
  periodo real no texto achatado do paragrafo (mesma regex de `_splitSentences`, via
  `_sentenceBoundaryMatches`) e movem nodes/fracoes de `Text.splitText` para dentro de cada
  `span.tr-sent` — nunca serializam/reparseiam HTML. Um elemento inline continua atomico: uma
  fronteira que cairia dentro dele e descartada, entao o periodo que a conteria simplesmente
  continua ate a proxima fronteira em texto livre (o `<em>` nunca e cortado). O caso "1 periodo so"
  do mockup (`onlySentence`) continua alcancavel — e so o que sobra quando o paragrafo genuinamente
  nao tem nenhuma fronteira real fora de markup. UNDO com markup: `setSnippetLoading` agora guarda os
  NODES originais do range (nao so o texto) num `Map` (`_snipOriginalNodes`), consumido por
  `_spliceSpanBackToPeriods` ao restaurar um snip removido ou um loading cancelado — fallback para
  texto (sem markup) quando a sessao foi restaurada do banco (`restoreSnippets` nunca populou o Map).
  Mapa limpo em `unmountSnippetLayer` (sem leak entre capitulos). Efeito colateral corrigido de
  brinde: `_originalParagraphText` contribuia so o `childNodes[0]` de um periodo, truncando um
  periodo com markup no meio do contexto enviado ao modelo (W-13 do `REVIEW.md`) — agora usa
  `textContent` completo. Ancoras `SnippetTranslations` persistidas ANTES deste fix, sobre um
  paragrafo que era 1 periodo e agora vira N, terao hash divergente na proxima abertura do livro —
  descarte silencioso ja existente (`restoreSnippets`), SEM purge (ancora invalida != registro
  podre); o usuario re-traduz esses trechos manualmente.
