D-2026-08-16-llm-mobile-7 (2026-08-16): `net10.0-maccatalyst` NAO ganha backend de inferencia nesta
phase. A limitacao e registrada explicitamente, o TFM continua compilando, e o app degrada com
mensagem clara em vez de estourar, LOCKED.

ESTADO REAL: o csproj declara `net10.0-maccatalyst` e esse TFM sofre HOJE exatamente o mesmo bloqueio
do iOS — `SystemInfo.Get()` do LLamaSharp so conhece Windows/Linux/OSX e o MacCatalyst nao e
reconhecido como OSX pelo caminho que o loader usa, terminando em `PlatformNotSupportedException`
disparada do static ctor. Isso ja era verdade antes desta phase; a phase nao pode deixar passar em
silencio (AC14), mas tambem nao pode fingir que resolveu.

POR QUE NAO CORRIGIR JUNTO: nao ha como verificar nada de MacCatalyst neste loop — exige um Mac para
compilar e um Mac para executar, e nenhum dos dois existe aqui. Estender a engine iOS para Catalyst
significaria uma slice adicional do XCFramework, outro conjunto de frameworks linkados e outro job de
CI, tudo entregue as cegas. Empilhar isso no Bloco 2 — que ja e o bloco com maior chance de nao
fechar (D-2026-08-16-llm-mobile-1) — troca risco por nada.

O QUE A PHASE ENTREGA PARA CATALYST:
- `NativeBackendPlan.For(TranslationPlatform.MacCatalyst)` declara o backend gerenciado como NAO
  suportado, com teste nomeado (D-2026-08-16-llm-mobile-3);
- degradacao graciosa obrigatoria (D-2026-08-16-llm-mobile-8): traducao indisponivel com mensagem
  tratada, todo o resto do app — biblioteca, leitura, temas, progresso, marcadores — intacto;
- linha `PLATFORM maccatalyst STATUS UNSUPPORTED` em `docs/NATIVE-BACKENDS.md` apontando para esta
  decisao;
- item em `.jdi/todos/2026-08-16-llm-mobile.md` para uma phase futura.

CUSTO ACEITO: usuario de macOS via Catalyst tem um leitor de EPUB sem traducao offline. E pior do que
"tudo funciona" e melhor do que as duas alternativas reais: crash nao tratado (o estado de hoje) ou
uma implementacao nao verificada declarada como pronta.
