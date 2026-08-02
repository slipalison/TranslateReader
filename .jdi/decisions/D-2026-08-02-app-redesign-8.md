D-2026-08-02-app-redesign-8 (2026-08-02): Gap 5 (Settings = painel lateral direito no desktop vs
bottom sheet no mobile) — ACEITO, com UM UNICO `SettingsOverlay.xaml`. Proibido criar um segundo
controle. LOCKED.
Hoje `SettingsOverlay.xaml` implementa so o bottom sheet (Border com `VerticalOptions="End"`,
`CornerRadius="16,16,0,0"`) e ele e usado nos dois idioms. O mockup pede painel ancorado a direita,
altura cheia, no desktop (`desktop-reader-settings-panel.jpg`) e o sheet no mobile
(`mobile-reader-settings-sheet.jpg`).
Forma LOCKED: so as PROPRIEDADES DE LAYOUT do `Border` externo viram `OnIdiom`
(`Desktop` -> `VerticalOptions=Fill`, `HorizontalOptions=End`, `WidthRequest~350`, cantos so do lado
esquerdo; default -> o sheet de hoje). Tudo o resto — todos os `x:Name`, todos os handlers
(`OnLightThemeClicked`, `OnFontSizeChanged`, `OnHyMtClicked`, ...), o `ApplySettings`, os 3 eventos
publicos (`CloseRequested`, `SettingsChanged`, `DeleteModelRequested`) e o consumo em
`ReaderPage.xaml.cs` — fica IDENTICO. Zero duplicacao de code-behind, zero `if (DeviceInfo.Idiom)`
em C# decidindo layout.
REJEITADO: dois ContentViews (SettingsPanel + SettingsSheet) ou dois blocos XAML irmaos com
`IsVisible` por idiom — duplicaria ~250 linhas de XAML e os ~20 handlers, e a proxima mudanca de
settings sairia aplicada so em um dos dois.
Conteudo interno segue o screenshot: secoes com rotulo caixa-alta (TEMA / MODO DE LEITURA /
TRADUCAO), tema como 3 cards "Aa", modo como segmented control de 2, os 4 sliders com o valor
alinhado a direita do rotulo (ja e o comportamento dos `*Label` atuais). O bloco de modelos
(4 botoes + label de atribuicao Tencent + status + excluir), entregue pela phase
`hy-mt-translation-model`, e PRESERVADO — o mockup foi desenhado antes dele existir e nao o mostra;
remover seria regressao funcional e apagaria a atribuicao de licenca exigida por
D-2026-08-01-hy-mt-translation-model-3.
