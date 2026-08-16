D-2026-08-16-llm-mobile-2 (2026-08-16): `Hy-MT2-1.8B` (Apache-2.0) entra no `ModelRegistry` e passa a
ser o default de INSTALACAO NOVA; `gemma-2-2b` e `hy-mt1.5-1.8b` continuam selecionaveis; o fallback
de `ResolveModel` NAO muda, LOCKED.

CORRECAO DE FATO (o brief estava impreciso, o codigo manda): o default de traducao HOJE **nao** e o
HY-MT1.5. E `gemma-2-2b`, declarado em DOIS lugares — `Models/ReadingSettings.cs:12`
(`TranslationModelName { get; set; } = "gemma-2-2b"`) e `Access/SettingsAccess.cs:54`
(`values.GetValueOrDefault("TranslationModelName") ?? "gemma-2-2b"`). O HY-MT1.5 e apenas
SELECIONAVEL no `SettingsOverlay`. A troca de default acontece nesses dois pontos, nao no registry.

MOTIVO: a "Tencent HY Community License" do HY-MT1.5 declara textualmente
"THIS LICENSE AGREEMENT DOES NOT APPLY IN THE EUROPEAN UNION, UNITED KINGDOM AND SOUTH KOREA"
(https://huggingface.co/tencent/HY-MT1.5-1.8B/raw/main/License.txt, verificado 2026-08-16), alem de
cap de 100M MAU e proibicao de usar outputs para treinar modelos. `tencent/Hy-MT2-1.8B` e Apache-2.0,
MESMA arquitetura (`hunyuan_v1_dense`, 32 layers, hidden 2048, vocab 120818 — troca drop-in no
pipeline GGUF), mesmo tamanho de quantizacao e qualidade igual ou superior. Valores literais medidos
por HTTP em 2026-08-16, a usar sem re-pesquisa:
- URL: https://huggingface.co/tencent/Hy-MT2-1.8B-GGUF/resolve/main/Hy-MT2-1.8B-Q4_K_M.gguf
- FileName: `Hy-MT2-1.8B-Q4_K_M.gguf`
- SizeBytes: `1_133_080_448` (content-length medido)

O QUE **NAO** MUDA (guarda anti-regressao, risco 5 do brief): `ResolveModel` continua
`ModelRegistry.TryGetValue(name, out var m) ? m : GemmaModel`. Trocar o alvo do fallback para o
Hy-MT2 faria um usuario com `qwen-2.5-3b`/`phi-3.5` salvo (valores que o `SettingsOverlay` grava e
que NAO estao no registry) passar a resolver para um arquivo diferente do que ja tem em disco,
disparando 1,06 GB de download novo sem ele pedir. Nada disso e melhoria: e quebra silenciosa.
`gemma-2-2b` e `hy-mt1.5-1.8b` permanecem no registry pelo mesmo motivo — o arquivo pode ja estar
baixado.

Licencas documentadas em `docs/MODEL-LICENSES.md`, citando Apache-2.0 para Hy-MT2, os termos Gemma
para o gemma-2-2b e a EXCLUSAO TERRITORIAL do HY-MT1.5 enquanto ele continuar selecionavel.
O `SettingsOverlay` ganha UMA linha nova (`HyMt2ModelButton`), espelhando o padrao das existentes; a
lista de nomes de `PixelSpecTests.ModelRowNames` acompanha.

CUSTO ACEITO: (a) instalacoes novas passam a baixar 1,06 GB de Hy-MT2 em vez de 1,63 GB de Gemma —
menos trafego, mas modelo diferente do que a documentacao antiga descrevia; (b) o app continua
OFERECENDO um modelo com licenca territorialmente restrita (HY-MT1.5). Remove-lo quebraria a selecao
salva de quem ja o baixou, o que custa mais do que documentar a restricao — e o default deixa de
apontar para ele, que era o problema real de distribuicao em loja.
