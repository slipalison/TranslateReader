D-2026-08-02-pixel-perfect-2 (2026-08-02): Tipografia Inter, LOCKED.
O mockup usa Inter com SO dois pesos: 400 (Regular) e 500 (Medium) — nao existe bold(700) em
nenhuma medida do PIXEL-SPEC. Entram `Inter-Regular.ttf` e `Inter-Medium.ttf` (release oficial
rsms/inter, licenca OFL 1.1, notice adicionado a `THIRD-PARTY-NOTICES.md`), registrados como
`InterRegular`/`InterMedium` no `MauiProgram`. `Styles.xaml` troca o default `OpenSansRegular` ->
`InterRegular`. Nos XAML redesenhados, `FontAttributes="Bold"` e SUBSTITUIDO por
`FontFamily="InterMedium"` (peso 500) onde a spec pede 500 — nunca manter bold "por seguranca".
Os arquivos OpenSans permanecem no repo (paginas fora do escopo continuam funcionando).
Excecao consciente: o "Aa" serifado dos cards de tema do Settings usa Georgia no mockup; Georgia
nao e portavel cross-platform em MAUI — o "Aa" fica em `InterRegular` (glyph de 20x19px, delta
minimo, registrado como delta consciente; alternativa OnPlatform rejeitada por complexidade sem
ganho perceptivel).
