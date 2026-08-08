D-2026-08-02-pixel-perfect-7 (2026-08-02): Menu de contexto continua MenuFlyout nativo, LOCKED.
O visual do menu aberto no mockup (160w, bg surface, r8, item 28h, "Excluir" em ColorDanger) NAO
sera replicado: `MenuFlyout` e nativo por plataforma (WinUI/Android) e nao aceita styling MAUI
confiavel. Replica-lo com overlay custom duplicaria a superficie de input (right-click ja abre o
flyout nativo) e criaria dois menus divergentes. O que E obrigatorio: o botao ⋮ visivel em cada
card do grid (28x28, CoverScrim) e em cada row da list (30x30), abrindo o MESMO flyout via
`FlyoutBase.ShowAttachedFlyout` no code-behind. Delta consciente documentado no PIXEL-SPEC
("Diferencas intencionais mantidas" item 1) e reportado no PR.
