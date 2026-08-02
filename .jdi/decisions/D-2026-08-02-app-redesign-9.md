D-2026-08-02-app-redesign-9 (2026-08-02): Os dois elementos do mockup que precisam de dado que a UI
nao tem hoje — card "Modelo de traducao" no rodape da sidebar e chip de idioma no header. LOCKED.
(1) CARD DE MODELO (`desktop-library.jpg`, rodape da sidebar: "Modelo de traducao / Gemma 2 2B -
1.6 GB / Modelo nao baixado"). `ITranslationManager` NAO expoe nada disso hoje (verificado: 8
operacoes, nenhuma de status). Hardcodar "Gemma 2 2B - 1.6 GB" no XAML esta REJEITADO: viraria
mentira assim que o usuario escolhesse HY-MT, e a phase anterior (`hy-mt-translation-model`) acabou
de existir justamente pra consertar essa classe de bug (UI dizendo um modelo, codigo usando outro).
Remover o card da sidebar tambem esta rejeitado (buraco visual sem motivo, o dado existe).
ACEITO, forma minima: `record TranslationModelStatus(string Name, string DisplayName, long SizeBytes,
bool IsDownloaded)` em `Models/` + `Task<TranslationModelStatus> GetSelectedModelStatusAsync()` em
`ITranslationManager`, implementado reusando o que ja existe: `ISettingsAccess` (ja injetado no
`TranslationManager` desde D-2026-08-01-hy-mt-translation-model-4) le `TranslationModelName`,
`ResolveModel` devolve o `ModelInfo` do registry, `IModelAccess.IsModelAvailable(fileName)` (ja
filename-aware desde a mesma phase) diz se esta em disco. Consulta pura, CQS respeitado, testavel em
`TranslationManagerTests` com NSubstitute. `ITranslationManager` vai a 9 operacoes — ja estava acima
do ideal de CLAUDE.md antes desta phase; anotado como todo de refactor, nao resolvido aqui.
(2) CHIP DE IDIOMA ("PT-BR" no desktop, "PT" no mobile). E o `ReadingSettings.TargetLanguage`, lido
via `ISettingsManager.LoadSettingsAsync()`. `LibraryPageModel` passa a receber `ISettingsManager`
(3o manager no construtor — permitido: a regra de CLAUDE.md e 1 Manager por CASO DE USO, e a classe
ja usava 2 pra casos diferentes; Manager->Manager sincrono continua nao acontecendo).
O chip e SO EXIBICAO, nao abre picker: o prototipo estatico nao mostra nenhum menu saindo dele, e o
lugar real de trocar idioma ja e o `SettingsOverlay`. Inventar um seletor aqui seria fluxo novo.
