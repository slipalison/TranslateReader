D-2026-08-16-llm-mobile-4 (2026-08-16): Android usa o pacote OFICIAL `LLamaSharp.Backend.Cpu.Android`
0.27.0; o `minSdk` do app sobe de 21.0 para 23.0; a presenca e o alinhamento do `.so` sao provados
por script versionado, nao por comando ad hoc, LOCKED.

PACOTE: `LLamaSharp.Backend.Cpu.Android` 0.27.0 (publicado 2026-04-26) — exatamente a versao de
`LLamaSharp` que o Core ja referencia. Entra em `src/TranslateReader/TranslateReader.csproj` sob
`Condition ...GetTargetPlatformIdentifier(...) == 'android'`, espelhando o ItemGroup que hoje so
existe para `'windows'` (Cuda12 + Cpu). Nao compilar llama.cpp proprio para Android enquanto o
pacote oficial atender (versao do backend SEMPRE casada com a do LLamaSharp — o ABI nativo e
version-locked ao loader gerenciado, como o proprio comentario do csproj ja documenta).

minSdk 21.0 -> 23.0: o backend oficial e compilado com `ANDROID_PLATFORM=android-23` no CI do
LLamaSharp. Manter `SupportedOSPlatformVersion` = 21.0 significa declarar suporte a API 21/22
enquanto se embarca um `.so` linkado contra API 23 — falha de `dlopen` em runtime, no device do
usuario, sem nenhum sinal em build. Subir e a unica opcao honesta.
CUSTO ACEITO: corta Android 5.0/5.1 (API 21-22). Aceitavel por duas razoes independentes: (1) o app
nao esta publicado em loja nenhuma (`ApplicationId` ainda `com.companyname.translatereader`,
version 1.0/1) — zero usuarios reais cortados; (2) device de API 21-22 tipicamente tem 1 GB de RAM e
nao roda um modelo 1.8B de jeito nenhum, entao o corte coincide com o piso de hardware do recurso.

VERIFICACAO POR SCRIPT (`scripts/check-android-so.sh`) em vez de one-liner no DoD:
o brief mediu o APK atual com `unzip -l` e achou 26 `.so` e ZERO de llama/ggml — esse e o baseline
NEGATIVO do AC4. Provar a inversao exige (a) abrir o APK e (b) ler os program headers ELF. `unzip`
nao vem no Git Bash por padrao e `readelf`/`objdump` so existem com NDK; um one-liner que falhe por
ferramenta ausente vira ou falso-negativo ou tentacao de disjuncao hollow. O script versionado
resolve fallback de extracao (unzip -> PowerShell/.NET ZipFile), imprime uma linha por artefato e
FALHA FECHADO: sai != 0 se nao achar APK, se nao achar nenhum `.so` de llama/ggml, ou se o `--check-doc`
divergir do medido.

CONTRATO DO SCRIPT (o DoD depende destes tokens, nao renomear):
- `SO_FOUND <caminho-dentro-do-apk>` — uma linha por `.so` de llama/ggml encontrado
- `SO_ALIGN <caminho> align=<n>` — alinhamento do maior LOAD do ELF, uma linha por `.so`
- `SO_COUNT <n>` — total encontrado; `0` e falha, nunca sucesso vazio
- `--check-doc <arquivo>` — compara CADA linha `SO_ALIGN` medida agora com a registrada no doc e
  falha em qualquer divergencia (impede registro obsoleto)

16 KB PAGE SIZE (exigencia do Google Play para apps que targetam Android 15+ desde 2025-11-01): a
regra locked e MEDIR E REGISTRAR A VERDADE, nao passar um threshold que nao controlamos. Os valores
medidos entram em `docs/NATIVE-BACKENDS.md`; se algum `align` for < 16384, o doc PRECISA conter uma
linha `MITIGATION:` nomeando o `.so` e o caminho de correcao, senao o script falha.
CUSTO ACEITO: se o backend oficial 0.27.0 nao estiver alinhado a 16 KB, a phase entrega Android
funcional COM uma limitacao registrada de publicacao em Play — nao um gate verde mentindo sobre
prontidao de loja. O alinhamento e fato upstream; o que esta sob nosso controle e nao esconde-lo.
