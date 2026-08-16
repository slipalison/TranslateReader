D-2026-08-16-llm-mobile-1 (2026-08-16): A phase entrega em DOIS BLOCOS sequenciais e "nao quebrar
nada" e requisito de PRIMEIRA CLASSE, medido contra baselines gravados, LOCKED.

Bloco 1 (base + Android) roda ANTES do Bloco 2 (iOS). Bloco 1: configuracao nativa por plataforma,
modelo Apache-2.0 no registry, gating de memoria e degradacao graciosa, backend Android, `.so` no
APK, gates do reviewer corrigidos. Bloco 2: engine iOS com P/Invoke proprio, XCFramework pinado,
job de CI em runner macOS, MacCatalyst tratado.

MOTIVO: os dois blocos tem custo e risco radicalmente diferentes. Tudo do Bloco 1 e verificavel
NESTA maquina (Windows, sem workload `maui-ios`); nada do Bloco 2 e — iOS exige macOS para compilar
e device fisico para executar. Se o Bloco 2 travar, o Bloco 1 continua sendo entrega completa e
comprovavel. O inverso nao existe: comecar pelo iOS arrisca terminar a phase sem NADA verificado.

BASELINES QUE A PHASE PROTEGE (medidos em 2026-08-16 na branch `feat/llm-mobile`; regressao = falha):
- `dotnet test` = 455 passed / 2 skipped / 0 failed. Os 2 skips sao `TranslationEngineTests` que
  exigem GGUF real — pre-existentes, nao mexer, nao "consertar", nao converter em falha.
- Build Android Debug `net10.0-android` = 0 warnings / 0 errors.
- Build Windows Release `net10.0-windows10.0.19041.0` = 0 errors.
- Nenhum nome de teste que existe no commit base pode DESAPARECER (checagem `comm -23` nome a nome,
  nao contagem — contagem pode ser mascarada por testes novos).

REGRA DE HONESTIDADE: se o Bloco 2 nao fechar, a saida CORRETA e registrar o estado real (o que foi
entregue, onde parou, qual a evidencia) e deixar o iOS para uma phase seguinte. E PROIBIDO inventar
um `Verify:` que passa sem provar, declarar iOS funcionando sem prova, ou repetir estimativa de
pesquisa (tokens/s) como se fosse medicao observada.

CUSTO ACEITO: a phase pode terminar entregando so o Bloco 1, deixando o objetivo declarado
("iOS tambem") parcialmente aberto. Preferimos entrega parcial verdadeira a entrega total declarada
e nao verificada — o app hoje ja nao roda LLM em mobile nenhum, entao Android sozinho ja e ganho
liquido, e um iOS "verde no papel" seria regressao de confianca, nao progresso.
