D-2026-08-08-baseline-de-estilo-6 (2026-08-08): Emenda a D-3 — teto do `NoWarn` sobe, mas so depois
de calibrar as rules que estao ERRADAS para o tipo de projeto. LOCKED.

CONTEXTO (medicao, nao estimativa). T-5 da iteracao 1 mediu o inventario real de warnings com os
analyzers de D-2 ligados: **24 IDs distintos** (CS 3, CA 10, MA 11) contra o teto de 12 fixado em
D-3(3b). O teto de 12 foi escolhido ANTES de qualquer medicao — era um palpite. `latest-recommended`
+ Meziantou num codebase que nunca teve analyzer produz cauda longa; isso nao e defeito do doer nem
do plano, e o custo real de ligar analyzers pela primeira vez.

(1) A contagem bruta de 24 IDs e ENGANOSA: duas rules sozinhas respondem por 1232 das ocorrencias e
ambas estao mal calibradas para a forma deste projeto, nao apontam divida real:
   - **CA1707** ("identificadores nao devem conter underscore"): 688 ocorrencias, TODAS no projeto de
     teste. `Metodo_Cenario_Esperado` e a convencao de nomenclatura de teste padrao da industria e a
     usada em todo `test/TranslateReader.Tests`. A rule esta certa para codigo de producao e errada
     para o projeto de teste.
   - **MA0004** ("use `Task.ConfigureAwait`"): 544 ocorrencias. Em codigo de UI do MAUI,
     `ConfigureAwait(false)` e frequentemente INCORRETO — a continuacao precisa voltar para o
     contexto da UI (ver `.claude/rules/csharp.md` §3: "UI state changes only on the main thread").
     A rule esta certa para `src/TranslateReader.Core/` (biblioteca) e errada para a camada de UI.

(2) CALIBRAR NAO E DODGE. D-1/PLAN proibiram mover CA/MA para `severity = none` **como fuga do teto**
— desligar uma rule que voce teria que corrigir, so para o numero fechar. Isso continua proibido. O
que esta autorizado aqui e diferente e tem criterio objetivo: ajustar a severidade de uma rule
**cujo pressuposto nao se aplica aquele projeto/pasta**, com o escopo mais estreito possivel
(`[test/**]`, `[src/TranslateReader/**]`) e com o motivo tecnico escrito no proprio `.editorconfig`,
em linha de comentario acima da chave. Uma rule calibrada continua valendo onde faz sentido.
Criterio de teste, aplicavel item a item: *se a rule fosse corrigida em vez de suprimida, o codigo
ficaria pior?* Se sim, e calibracao. Se o codigo ficaria melhor e so da trabalho, e divida — vai
para `NoWarn`.

(3) O que sobra depois da calibracao vai para `NoWarn`, mantendo INTACTO todo o resto de D-3: lista
unica, por ID (nunca curinga), cada ID com comentario em linha propria explicando o que e e por que
esta congelado. O teto numerico de 12 de D-3(3b) fica **revogado e substituido** por: o `NoWarn`
contem exatamente os IDs que sobraram da medicao apos a calibracao de (2) — nem um a mais. Nao ha
numero magico; ha a lista medida. Adicionar ID novo a essa lista no futuro exige decisao propria.

(4) Warnings que representam BUG POTENCIAL, e nao estilo, ficam explicitamente marcados no comentario
do `NoWarn` com o prefixo `RISCO:` — hoje sao pelo menos `CS8602` (null dereference, 14x no projeto
de teste) e `CA1001` (tipo que possui campo `IDisposable` sem implementar `IDisposable`, 6x). Eles
sao congelados aqui porque D-1 proibe tocar no legado nesta phase, NAO porque sao aceitaveis. Vira
todo para uma phase futura de correcao — registrar em `.jdi/todos/`.

(5) DoD 2 e DoD 6 do CONTEXT.md passam a ser avaliados contra esta emenda: a validacao estrutural do
`NoWarn` (elemento unico, sem curinga, cada ID em >= 2 linhas) continua valendo; a assercao de
`<= 12 IDs` sai. O `.editorconfig` passa a poder conter blocos com escopo de pasta para as rules
calibradas, cada um com o comentario tecnico de (2).
