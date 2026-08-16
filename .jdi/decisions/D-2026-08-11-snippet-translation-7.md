D-2026-08-11-snippet-translation-7 (2026-08-11): A traducao do trecho tenta SEM contexto primeiro —
supersede empiricamente a parte "PROMPT = TRECHO + PARAGRAFO COMO CONTEXTO" de
D-2026-08-09-snippet-translation-5, LOCKED.

D-2026-08-09-snippet-translation-5 decidiu mandar o paragrafo inteiro como contexto para o modelo
nao perder sujeito e referencia de pronome. A premissa (contexto melhora a traducao) vale para um
modelo grande; para o modelo local pequeno deste app ela se inverteu na pratica: em CINCO rodadas de
teste real do usuario, o modelo copiou o contexto para dentro da resposta em vez de usa-lo como
referencia — traduziu o paragrafo inteiro no lugar do trecho pedido.

O iter 10 ja tinha estreitado o contexto de "paragrafo inteiro" para JANELA (periodo anterior +
trecho + periodo seguinte) e endurecido o prompt com delimitadores e instrucao explicita. Nao
bastou: o caso medido no 8o feedback tinha o trecho no periodo 0 (sem anterior), entao a janela era
`P0 + P1` e a resposta persistida trouxe os dois periodos traduzidos — 134 chars de original viraram
392 chars de traducao com 3 sentencas onde o original tem 1.

DECISAO: a PRIMEIRA tentativa de inferencia usa o prompt SEM contexto algum. Sem paragrafo no
prompt nao existe material para o modelo copiar — a classe inteira de vazamento morre por
construcao no caminho feliz, em vez de depender de o modelo obedecer a instrucao. A janela de
contexto sobrevive como SEGUNDA tentativa, disparada so quando a primeira resposta reprova na
validacao (`SnippetValidationUtility`): ali o contexto ainda pode salvar um trecho que o modelo nao
soube traduzir isolado, e um vazamento na segunda tentativa e barrado pela mesma validacao antes de
persistir ou aparecer na tela.

CUSTO ACEITO: um periodo isolado cujo sujeito vive no periodo anterior ("Ela disse que...") pode ser
traduzido com pronome ambiguo na primeira tentativa e ser aceito, porque uma traducao ambigua e
plausivel — a validacao mede escopo, nao qualidade semantica. Trocamos qualidade marginal de
desambiguacao por escopo correto e deterministico. O usuario reportou vazamento cinco vezes e
ambiguidade zero vezes; a troca segue a prioridade de CLAUDE.md (corretude antes de refinamento).

`IPromptUtility` mantem as DUAS assinaturas de `BuildSnippetTranslationMessages` (com e sem
contexto) — o contrato nao muda, so a ORDEM em que `TranslationManager` as usa.

O resto de D-2026-08-09-snippet-translation-5 permanece VIGENTE e intocado: cache em
`TranslationCache` com `OriginalHash = ComputeHash(salt + trecho, src, dst)`, sobreposicao
destrutiva por run contiguo, segundo contrato `ISnippetTranslationManager` na mesma classe
`TranslationManager`, e o fluxo assincrono com `CancellationToken` ponta a ponta.
