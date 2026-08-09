D-2026-08-08-baseline-de-estilo-3 (2026-08-08): `TreatWarningsAsErrors=true` com lista `NoWarn` fechada,
por ID e comentada. LOCKED.
(1) `TreatWarningsAsErrors=true` no `Directory.Build.props` (vale para os 3 projetos): warning NOVO
quebra o build. E o mecanismo que impede a divida de estilo de voltar a acumular.
(2) O legado fica CONGELADO por supressao explicita, nao por correcao: cada warning pre-existente que
sobreviver entra em `<NoWarn>` por ID exato (ex.: `CS0618` do `DisplayAlert` obsoleto, `CS0414` do
`_needsInjection`, e o que `CA*`/`MA*` acender sobre codigo anterior a `4285f25`). Corrigir esses
warnings seria refatorar legado por estilo — proibido por D-2 e fora do escopo desta phase.
(3) Regras da lista, verificaveis: (a) so IDs concretos, NENHUM curinga/prefixo de categoria (`CA`,
`MA0*`, `IDE*` ou `$(NoWarn)` puro nao contam como item); (b) teto de 12 IDs — passar disso significa
que a decisao de ligar `latest-recommended` precisa ser revisitada, nao que a lista cresce;
(c) CADA ID aparece pelo menos duas vezes no `Directory.Build.props`: uma na propriedade e outra numa
LINHA de comentario separada com o motivo e onde ele ocorre. Comentario na mesma linha do valor nao
conta.
(4) A lista vive no `Directory.Build.props` da raiz, nunca espalhada nos csproj, nunca em `#pragma
warning disable` novo no codigo.
(5) Codigo NOVO nao usa `NoWarn`: se um analyzer acender em codigo escrito depois de `4285f25`, o
codigo muda, a supressao nao.
