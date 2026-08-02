D-2026-08-02-app-redesign-6 (2026-08-02): Gap 3 (toggle grid/list na Biblioteca) — REJEITADO. Os dois
botoes de icone nao sao portados pro app. LOCKED.
Razoes, nesta ordem: (1) no proprio mockup o toggle e DECORATIVO — clicar nele no prototipo nao muda
o layout (constatado ao operar o HTML renderizado, registrado em
`design/DESIGN-REFERENCE.md` gap #3); (2) nao existe NENHUM screenshot de uma "list view", entao
implementar uma seria inventar layout novo, que e literalmente o que o card proibe; (3) portar o
botao sem funcao seria pior que nao portar — um controle morto na UI real e um defeito que chega no
usuario, e o app de hoje so tem grid, entao nada regride ao nao ter o toggle.
Efeito visual aceito e explicito: o header do desktop fica sem o par de botoes que aparece em
`design/screenshots/desktop-library.jpg` entre a busca e o chip de idioma; o espaco vai pro campo de
busca. Delta consciente contra o screenshot, registrado em `## Deferred to PR review` do CONTEXT.md
pro humano confirmar, e como todo caso ele queira a list view de verdade (a que precisaria de um
mockup novo antes).
Observacao de simetria: o mockup MOBILE (`mobile-library.jpg`) tambem nao tem o toggle — so o
desktop tem. Nao porta-lo alinha as duas plataformas.
