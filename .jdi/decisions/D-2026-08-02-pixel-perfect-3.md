D-2026-08-02-pixel-perfect-3 (2026-08-02): Icones Phosphor por icon-font, LOCKED.
Os icones do mockup sao a font Phosphor (`@phosphor-icons/web`, MIT) — confirmado por inspecao do
DOM (`class="ph ph-*"`, `font-family: Phosphor`). Entram 2 TTFs: `Phosphor.ttf` (Regular) e
`Phosphor-Fill.ttf` (Fill), registrados como `Phosphor`/`PhosphorFill`. Todos os glifos usados e
seus codepoints estao tabelados no PIXEL-SPEC (24 icones, extraidos do `::before` computado do
proprio mockup — ex.: magnifying-glass `E30C`, dots-three-vertical `E208`, check-circle Fill
`E184`). Os caracteres improvisados atuais (emoji de livro, `&#9776;`, `&#8594;`, `⚙`, `✕`, `☀`,
`☾`, `☕`) saem das superficies redesenhadas. Notice MIT adicionado a `THIRD-PARTY-NOTICES.md`.
Rejeitado: SVG/Path por icone (24 geometrias coladas a mao = mais superficie de erro pra LLM
menor que 1 font + tabela de codepoints).
