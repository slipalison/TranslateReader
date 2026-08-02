D-2026-08-01-hy-mt-translation-model-6 (2026-08-01): Preocupacao do card sobre "HY-MT nao tem
system_prompt default, entao a instrucao deveria ir no turno de usuario, nao no de sistema" fica
DELIBERADAMENTE fora de escopo nesta phase, nao reescrita em `PromptUtility`. Motivo: o app NUNCA
depende do system prompt default embutido no GGUF de nenhum modelo — `PromptUtility.
BuildTranslationMessages` (`PromptUtility.cs:7-17`) sempre CONSTROI seu proprio `systemMessage`
explicito, passado como `StatelessExecutor.SystemMessage` (`TranslationEngine.cs:103-107`), pra
TODOS os modelos, gemma incluido. Ou seja: a ausencia de system_prompt default do HY-MT nao quebra
nada estruturalmente hoje — a preocupacao do card nao se aplica como RISCO TECNICO, so como possivel
AJUSTE FINO de qualidade (o HY-MT pode responder melhor com a instrucao no turno de usuario em vez
do de sistema, por nao ter sido fine-tunado pra obedecer um system prompt custom da mesma forma que
um modelo com system_prompt default documentado). Essa e uma preocupacao MODEL-AGNOSTIC — afetaria
`BuildTranslationMessages` pra QUALQUER modelo que se comporte assim, nao so hy-mt — reescrever o
contrato compartilhado de prompt e mudanca arquitetural maior que "adicionar modelo pro download",
fora do estatuto desta phase. Registrado em `## Deferred to PR review` do CONTEXT.md e como todo
para phase futura (possivelmente junto com D-...-5, sampling por modelo, ja que ambos sao ajuste
fino de qualidade de traducao por modelo, nao bugs estruturais).
