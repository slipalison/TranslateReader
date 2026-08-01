D-2026-08-01-translated-epub-images-10 (SUPERSEDE a ANCORA dos `Verify:` dos itens 5 e 6 do DoD
desta phase: ref LOCAL `main` -> `$(git merge-base origin/main HEAD)`; `-1` a `-9` ficam
INTOCADAS): W-2 da REVIEW iter 1 registrou que os dois unicos itens do DoD que comparam contra
outra revisao usam o literal `main`, um ref **LOCAL**. Ref local nao e propriedade do codigo — e
estado da maquina de quem roda o gate. Esta sessao mediu as tres formas de o gate mentir, todas em
CLONE DESCARTAVEL no scratchpad (repo real intocado, nenhuma mutacao):

**(a) ref local `main` ATRASADO — reprova codigo correto (o incidente real desta phase).** Com
`main` = `9e07c83` e `origin/main` = `05f3670` (exatamente o estado da sessao do doer, ver
`D-...-9`): item 6 ANTIGO -> **exit 1**, arrastando `src/TranslateReader/Resources/Raw/wwwroot/js/
translation.js` (arquivo do PR #17, de outra phase) para dentro do diff; item 6 NOVO -> **exit 0**.
No item 5 o ref velho erra na direcao OPOSTA e mais perigosa: `B=337` (piso cai de 343 para 342) e
`names_main=308` em vez de 309 — um teste de folga, gate **frouxo**, silenciosamente.

**(b) ref local `main` AUSENTE — o gate vira oco.** Clone fresco com `git clone -b
jdi/translated-epub-images` (nenhum `main` local, exatamente o que CI faz): item 6 ANTIGO ->
**exit 1** com `fatal: bad revision 'main'`; item 5 ANTIGO -> **exit 0 VAZIO**: `B=0`, `S=0`, piso
`Total >= 5`, `names_main=0` linhas, `comm -23` trivialmente vazio — o item 5 degrada para "a suite
roda e tem pelo menos 5 testes" sem emitir um unico erro. Item 5/6 NOVOS -> **exit 0** com os
valores corretos (`BASE=05f3670`, `B=338`, `S=2`, piso 343, 309 nomes na base).

**(a') `main` AVANCA enquanto o PR esta aberto — reprova codigo correto tambem.** Simulando um
merge de terceiro em `origin/main` (commit novo tocando `src/TranslateReader/`): item 6 ANTIGO ->
**exit 1** (o arquivo do outro merge entra no diff como se fosse desta phase); item 6 NOVO ->
**exit 0**, porque `merge-base` continua em `05f3670`. `merge-base` nao so conserta o ref velho:
imuniza o gate contra a base se mover durante a vida do PR, que e o comportamento normal de `main`.

**Sem regressao de gate (obrigatorio: o NOVO tem de pegar tudo que o ANTIGO pegava).** Com
`main == origin/main` (melhor caso possivel para o ANTIGO), os dois produzem valores IDENTICOS
(`B=338 S=2 piso=343 nomes=309`) e ANTIGO e NOVO reprovam JUNTOS as 4 mutacoes testadas:
arquivo a mais em `src/TranslateReader/` (1/1), arquivo a mais em `src/TranslateReader.Core/`
fora da lista fechada (1/1), metodo de teste DELETADO (1/1, `comm -23` nao-vazio) e metodo de teste
RENOMEADO (1/1). Nenhuma direcao de deteccao foi perdida.

**Comandos NOVOS (substituem por inteiro os dos itens 5 e 6; o literal `main` sai de cena).**
Item 5:

    mkdir -p TestResults && BASE=$(git merge-base origin/main HEAD) && B=$(git grep -hoE '\[(Fact|InlineData)' "$BASE" -- 'test/TranslateReader.Tests/*.cs' | wc -l) && S=$(git grep -hoE 'Skip[[:space:]]*=' "$BASE" -- 'test/TranslateReader.Tests/*.cs' | wc -l) && test "$B" -gt 0 && git grep -hoE 'public[[:space:]]+(async[[:space:]]+)?(Task|void)[[:space:]]+[A-Za-z0-9_]+' "$BASE" -- 'test/TranslateReader.Tests/*.cs' | awk '{print $NF}' | sort -u > TestResults/names-base.txt && git grep -hoE 'public[[:space:]]+(async[[:space:]]+)?(Task|void)[[:space:]]+[A-Za-z0-9_]+' HEAD -- 'test/TranslateReader.Tests/*.cs' | awk '{print $NF}' | sort -u > TestResults/names-head.txt && test -s TestResults/names-base.txt && test -z "$(comm -23 TestResults/names-base.txt TestResults/names-head.txt)" && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release > TestResults/dod5.log 2>&1 && grep -q "Passed!" TestResults/dod5.log && awk -v b="$B" -v s="$S" '/Passed!/{ok=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1);if($i=="Skipped:")k=$(i+1);if($i=="Total:")t=$(i+1)}} END{exit (ok && f+0==0 && t+0>=b+5 && k+0<=s+0 && p+0+k+0+f+0==t+0)?0:1}' TestResults/dod5.log

Item 6:

    BASE=$(git merge-base origin/main HEAD) && test -z "$(git diff --name-only "$BASE" -- src/TranslateReader/)" && test "$(git diff --name-only "$BASE" -- src/TranslateReader.Core/ | sort | tr '\n' ',')" = "src/TranslateReader.Core/Business/Engines/ParsingEngine.cs,src/TranslateReader.Core/Business/Managers/ReadingManager.cs,src/TranslateReader.Core/Business/Managers/TranslationManager.cs,src/TranslateReader.Core/Contracts/Engines/IParsingEngine.cs,src/TranslateReader.Core/Models/ChapterContentPurpose.cs," && test -f src/TranslateReader.Core/Models/ChapterContentPurpose.cs && mkdir -p TestResults && DOTNET_CLI_UI_LANGUAGE=en dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0 > TestResults/dod6.log 2>&1 && grep -qE "^ *0 Error\(s\)" TestResults/dod6.log

Duas guardas ANTI-OCO acrescentadas ao item 5 junto com a troca de ancora, motivadas diretamente
pelo achado (b): `test "$B" -gt 0` e `test -s TestResults/names-base.txt`. Elas transformam "nao
consegui ler a base" em **reprovacao** em vez de aprovacao vazia — sem elas o item 5 continuaria
podendo passar com base ilegivel, so que por outro caminho. A ancora nova ja falha FECHADO por
construcao quando nem `origin/main` existe (clone raso): `BASE=$(git merge-base origin/main HEAD)`
sai `fatal: Not a valid object name origin/main`, exit **128**, e o `&&` corta a cadeia (medido).

PRE-REQUISITO NOVO (substitui "o ref local `main` tem de apontar pro `origin/main` real" de
`D-...-9`): o clone precisa ter `origin/main` e historia suficiente para o `merge-base` — em CI,
`actions/checkout` com `fetch-depth: 0` (ou um `git fetch origin main` antes do gate). Nao ha mais
nenhum pre-requisito sobre branch LOCAL: o gate roda em clone fresco sem `main` local.

DIRETRIZ para os proximos DoDs desta base de codigo (era a recomendacao final de W-2): todo
`Verify:` que compare contra outra revisao ancora em `$(git merge-base origin/main HEAD)` — nunca
no literal `main`, nunca em `origin/main` cru (que se move com o PR aberto).

CORRECAO DE PROSA de `D-...-9` (append-only: a `-9` NAO foi editada). Onde a `-9` diz "`comm -23`
VAZIO (309 nomes em `main`, 309 no HEAD)", o numero do HEAD e um snapshot tirado ANTES da
implementacao. O HEAD final da phase tem **314** nomes (309 herdados de `main` + 5 testes novos);
a base continua com 309 e o `comm -23` continua VAZIO. O gate so consome o lado da base do
`comm -23`, entao nenhum veredito de `D-...-9` muda — a imprecisao e so de prosa e fica corrigida
aqui.
