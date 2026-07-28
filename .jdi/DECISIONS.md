# Decisoes locked do projeto

Append-only. Nunca editar decisao existente — adicionar uma nova que a supersede.

D-1 (2026-07-28): Code design = **The Method** (Juval Lowy, decomposicao baseada em
volatilidade). Camadas fechadas Client -> Managers -> Engines -> ResourceAccess -> Resources,
com Utilities vertical. Nao foi escolhido nesta adocao: ja estava locked e documentado pelo
usuario em `CLAUDE.md` ("Regras de Camada (OBRIGATORIAS)") e em `README.md`, e confirmado
pela estrutura fisica e pelo naming do repositorio (4 Manager, 3 Engine, 6 Access, 3 Utility).
As regras PROIBIDO de CLAUDE.md (sem Manager -> Manager sincrono, sem pular camadas, sem
regra de negocio em Manager ou PageModel) valem como criterio de review.

D-2 (2026-07-28): Adocao brownfield. Boundary do legado = commit `4285f25`
(`4285f25c308f6aeb0877202bb4aabf66523f7c1e`). A partir daqui:
- gate de cobertura de 80% vale **somente** para arquivos criados/alterados depois desse commit;
- codigo pre-existente fica isento e nao deve ser refatorado sem uma phase explicita;
- os 167 testes existentes sao baseline: podem ser mantidos, nao devem regredir.

D-3 (2026-07-28): As skills de arquitetura genericas do JDI (`clean-architecture`, `ddd`,
`hexagonal`, `onion`, `vertical-slice`) **nao** se aplicam a este projeto e nao podem
redecidir a arquitetura. O JDI e camada de processo; a arquitetura vigente e The Method (D-1).
Origem: secao "JDI — Workflow de Desenvolvimento" do `CLAUDE.md`.

D-4 (2026-07-28): Conventional commits passam a valer para commits novos (escopo = slug da
phase). O historico legado tem 0/10 commits no padrao e nao sera reescrito.

D-5 (2026-07-28): D-1 confirmada explicitamente pelo usuario (a adocao havia registrado o
design por inferencia de CLAUDE.md/README, sem canal interativo). The Method esta LOCKED.
Na mesma confirmacao o usuario aprovou os 6 candidatos detectados como phases do roadmap
e orquestracao `enhanced` em `config.json`.

D-6 (2026-07-28): Gate de cobertura sobe de 80% para 90% em codigo novo/alterado pos-boundary
`4285f25`. Origem: o usuario elevou o threshold em `.claude/rules/csharp.md` §6 no mesmo dia
do bootstrap. Supersede o numero de D-2; o boundary, a isencao do legado e o baseline de 167
testes permanecem inalterados. Sincronizado em `config.json` (`coverage_min`), `PROJECT.md`,
`ROADMAP.md` (goal da phase `cobertura-e-ci`), `reviewers.md`, `registry.md` e nos specialists
doer/reviewer.
