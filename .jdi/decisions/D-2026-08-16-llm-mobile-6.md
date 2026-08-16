D-2026-08-16-llm-mobile-6 (2026-08-16): O minimo de iOS do app sobe de 15.0 para **16.4**, alinhado ao
binario oficial do llama.cpp. Nao compilamos build proprio para baixar o minimo, LOCKED.

FATO: `build-xcframework.sh` do llama.cpp fixa `IOS_MIN_OS_VERSION=16.4`. O csproj declara hoje
`SupportedOSPlatformVersion` = 15.0 para `ios` (`TranslateReader.csproj:45`). Deixar 15.0 e embarcar
um binario 16.4 e uma mentira de manifesto: ou o link falha, ou o app e instalado num device que nao
consegue carregar o codigo nativo.

ALTERNATIVA DESCARTADA: compilar um XCFramework proprio com `IOS_MIN_OS_VERSION` menor. Descartada
porque cria e obriga a manter um pipeline de build nativo (macOS + Xcode + toolchain pinada) so para
ganhar iOS 15.x, e contraria D-2026-08-16-llm-mobile-9 (usar o artefato oficial pinado por release e
validado por checksum). Trocariamos cadeia de suprimento verificavel por cadeia caseira.

CUSTO ACEITO: o app inteiro — inclusive a LEITURA de EPUB, que nao tem nada a ver com LLM — deixa de
instalar em iOS 15.x. Aceitavel por dois motivos verificados: (1) o app nao esta publicado em loja
(sem TFM iOS em CI ate esta phase, `ApplicationId` ainda `com.companyname.translatereader`) — zero
usuarios reais perdidos, e nao ha atualizacao que possa quebrar na mao de ninguem; (2) o teto de
hardware ja e mais alto que isso: Metal em iOS exige GPU Apple7+ (A14/M1 em diante) e o footprint de
~1,5-1,8 GB (modelo 1,06 GB + KV cache) e apertado em device de 4 GB. O conjunto "roda iOS 15 mas
nao roda 16.4" e majoritariamente A9-A10 com 2 GB de RAM, que nunca executaria um 1.8B.

`SupportedOSPlatformVersion` de `maccatalyst` NAO muda nesta phase (continua 15.0) — ver
D-2026-08-16-llm-mobile-7: Catalyst nao ganha backend aqui, entao subir o minimo dele so cortaria
usuarios sem entregar nada em troca.
