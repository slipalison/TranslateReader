D-2026-08-08-cobertura-e-ci-6 (2026-08-08): guardrail de escopo e ordem — registrado a pedido
explicito do orquestrador na mesma invocacao que fechou D-...-1 a D-...-5.

**Enquadramento corrigido da fase.** O goal no ROADMAP diz "threshold de cobertura no coverlet +
workflow de CI com build e testes". A segunda metade **ja existe**: `ci-seguranca`,
`sast-sca-sbom` e `pipeline-unificada` rodaram fora da ordem do roadmap e deixaram 12 workflows,
com orquestrador `pipeline.yml`. O que sobra para esta fase e o **gate**, exatamente como
D-2026-07-28-ci-seguranca-1 e -5 ja tinham alocado ("o GATE de 90% que falha o build fica para a
phase `cobertura-e-ci` — aqui so nasce a coleta"). A primeira metade tambem muda de forma: nao ha
"threshold no coverlet" possivel (D-...-1, fato tecnico do `coverlet.collector`).

**Ordem obrigatoria de execucao** (o planner pode agrupar em waves, nao pode reordenar):
1. `scripts/coverage-gate.sh` + `.jdi/coverage-waivers.txt` — o medidor, verificavel 100% local;
2. `coverage.yml` + caller em `pipeline.yml` + retirada da coleta de `ci.yml` + reportgenerator;
3. sincronizacao de processo: Gate 3 do reviewer chamando o script, e o `coverlet.collector 8.0.1`
   desatualizado em `.jdi/PROJECT.md:27`, `.jdi/registry/LEGACY.md:26`,
   `.jdi/agents/jdi-doer-translatereader.md:68,70` e `.jdi/agents/jdi-reviewer-translatereader.md:64`
   (o repo usa **10.0.1** desde antes desta fase);
4. snapshot de branch protection + `branch-protection-remap.md`;
5. W-2 do Android — **por ultimo, e so na PR**: o resultado depende de um runner com SDK Android,
   nao existe medicao local possivel, e a resposta pode exigir decisao nova.

**Risco de tamanho, declarado e nao escondido.** Sao 5 superficies novas
(script, workflow novo, edicao de 2 workflows existentes, 4 arquivos de processo, 2 artefatos de
branch protection) mais um item que so resolve em CI. Se o planner nao couber em 8 tasks sem
empilhar responsabilidades, a linha de corte **ja esta escolhida**: itens 1-3 ficam nesta fase (o
gate completo, ponta a ponta, com valor sozinho) e itens 4-5 viram phase propria de CI. Cortar em
qualquer outro ponto entrega gate sem CI ou CI sem gate — os dois inuteis. Nao dividir e o default;
dividir exige registrar a decisao aqui.
