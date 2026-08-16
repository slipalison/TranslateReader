D-2026-08-16-llm-mobile-9 (2026-08-16): O XCFramework do llama.cpp NUNCA entra no git. E obtido em
build por script versionado, pinado por TAG de release, validado por SHA-256 fail-closed, com modo
`--verify-only` testavel sem rede, LOCKED.

NAO COMMITAR: o maior arquivo versionado deste repo hoje tem 31 MB (um EPUB de fixture) e o repo nao
usa Git LFS para nada. O XCFramework oficial e substancialmente maior e nao pode entrar no historico
— historico de git e irreversivel na pratica. O caminho de destino vai para `.gitignore` e o DoD
prova que nenhum arquivo com `xcframework` no nome esta rastreado.

FORMA LOCKED:
- `scripts/fetch-llama-xcframework.sh` — baixa o asset `llama-<tag>-xcframework.zip` do release
  PINADO, confere SHA-256 e so entao extrai; cache local para nao rebaixar a cada build.
- Propriedades MSBuild no csproj do app com os pins como LITERAIS: `LlamaCppRelease` (tag `bNNNN`,
  nunca `latest`/`master`/`main`) e `LlamaCppXcframeworkSha256` (64 hex).
- Target `FetchLlamaXcframework`, condicionado a TFM iOS, roda antes do link. Em Windows/Android o
  target nunca executa.
- Guarda anti-no-op: `<Error Condition="!Exists(...)">` sobre o caminho final. `NativeReference` usa
  caminho LITERAL, jamais glob.

POR QUE O `--verify-only`: um download de build sem checksum verificado e cadeia de suprimento nao
confiavel — o checksum e obrigatorio, nao opcional. Mas "o checksum e conferido" so vale se a
conferencia FALHAR quando deve. `--verify-only <arquivo> <sha256>` permite provar isso em segundos,
sem rede e sem baixar centenas de MB: o DoD gera um arquivo temporario, roda com o hash certo
(espera exit 0) e com um hash errado (espera exit != 0). Sem esse modo, "tem checksum" seria um grep
— exatamente o hollow PASS que o DoD critic caca.

ARMADILHA QUE ISSO EVITA (modo de falha REAL observado): `liqngliz/My.Private.Ai` referencia
`runtimes/ios-arm64/native/*.dylib` de um pacote onde esse caminho nao existe; o glob casa ZERO
arquivos e o build passa VERDE sem embarcar nada. Build verde nao e prova de binario embarcado; por
isso a combinacao caminho-literal + `<Error Condition="!Exists(...)">` + checksum e obrigatoria.

CUSTO ACEITO: (a) o build iOS depende de rede na primeira execucao de cada maquina/runner — mitigado
por cache local e aceitavel porque so o job de CI macOS compila iOS; (b) subir de release do
llama.cpp passa a exigir atualizar DOIS literais (tag e sha) e reconferir os P/Invoke — friccao
deliberada: binario de terceiro entrando no app tem que ter origem, versao e hash registrados, e
`latest` e proibido.
