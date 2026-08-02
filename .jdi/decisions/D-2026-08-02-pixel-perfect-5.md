D-2026-08-02-pixel-perfect-5 (2026-08-02): Cor destrutiva do design system e `#E08A8A`, LOCKED.
A phase app-redesign permitiu `#E53E3E` como excecao porque "a paleta do mockup nao define
vermelho — nao inventar um". Medicao direta desta sessao: o mockup DEFINE vermelho — o item
"Excluir" do menu de contexto renderiza `color: #e08a8a`. Nasce o token `ColorDanger` (#E08A8A)
em `DesignTokens.xaml`; TODOS os `#E53E3E` de `src/TranslateReader/` (botoes Pausar/Cancelar
download, DeleteModelButton) viram `{StaticResource ColorDanger}`. O teste
`RedesignedXaml_HasNoLegacyChromeHex` remove `#E53E3E` da lista de permitidos e o adiciona a
denylist. Botao "Excluir modelo" (solido) usa bg ColorDanger + texto ColorBg.
