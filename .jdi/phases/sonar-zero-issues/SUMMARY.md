# Phase 14: Zerar as issues do SonarQube e travar a regressao — Summary  (slug: sonar-zero-issues)

**Status:** complete · **Tasks:** 8/8 · **Base:** `6132078` (main) · branch `jdi/sonar-zero-issues`

## Iter 1 — entrega das 8 tasks

8 commits, 1 task = 1 commit, escopo `(sonar-zero-issues)`, tipo por natureza (D-4): `8e6200f` T-1
chore · `294d316` T-2 JS · `ddda060` T-3 index.html · `2bbdaee` T-4 HtmlUtility · `d84f3e9` T-5
dispose · `df53909` T-6 async I/O · `229fe5d` T-7 TranslationRun · `6cc6200` T-8 ci.

**Destino das 113 issues** (taxonomia D-...-3; cruzamento item a item na secao (e) da REVIEW):
**67 FIX** · **41 REMOCAO** (`dotnet-install.ps1`) · **2 EXCLUSAO** multicriteria (`Web:S7926` +
`css:S4667`, D-...-4) · **3 WAIVER** `#pragma` = **113**. O diff tem exatamente 2 pares `#pragma
disable/restore` e zero `NoWarn`/`SuppressMessage`/`sonar.exclusions`.

**Gates:** build **0 erros** / 64 avisos (`MVVMTK0045` pre-existentes do app MAUI); testes **256
(254p/2s/0f)** vs baseline 229 (227/2) = **+27, 0 deletado, 0 afrouxado**; cobertura D-6 das linhas
ALTERADAS de producao **68/68 = 100,0%**; `dotnet format` 9 violacoes **todas fora do diff** (D-2);
DoD 10/10.

**Mutacao T-1/T-6:** `await using`→`var` writer, `stream.SetLength(0)`, `OpfTitleRegex().Replace` e
`CommitAsync()` — os 4 **PEGOS**. **Prova negativa declarada:** remover `InvariantCulture` dos 3
caminhos de LEITURA **NAO e pego** (o formato "O" e culture-invariant por especificacao; probe em 5
locales); a metade REAL do risco (escrita `ToString("O")`→`ToString()`) **e pega** (6 falhas).

**Desvio load-bearing:** T-7 declarou `TranslateSingleChapterAsync` antes do chamador para a janela
do `awk` do DoD 9 cair sobre a declaracao — **virou o blocker do critic, tratado na iter 2**.

## Iter 2 — fix do blocker do DoD critic (item 9, S107)

**Blocker:** o `awk` do item 9 nao media parametros — achava a PRIMEIRA linha com `<Nome>(` e
contava virgulas ate a proxima terminada em `)`. Para `TranslateChaptersWithCacheAsync` a primeira
ocorrencia e o CALL SITE (`:59`), nao a declaracao (`:147`): o gate media o chamador. Contra-exemplo
executado pelo critico — 8 params na DECLARACAO — saia **exit 0**.

**Fix: o gate, nao o codigo — zero linha de producao ou teste alterada.** Decisao NOVA append-only
`D-2026-07-30-sonar-zero-issues-8`, so depois a linha do CONTEXT.md; comando byte-identico nos 2
arquivos. O novo ancora em `^[[:space:]]*private async Task <Nome>(` exigida 1x (call site
nunca casa) e conta PARAMETROS declarados — virgula em profundidade 1 de parenteses, com
`<>`/`[]`/`{}` em 0 — contra `-le 7`, nao mais virgulas de uma janela textual contra `-le 6`.

**Matriz (NEW/OLD):** intacto 0/0 · **m1 contra-exemplo do critico 1/0** · m11 a mesma violacao em
UMA linha **1/0** · **m3 REORDER puro 0/1** (gate deixou de ser posicional; permutacao provada por
`sort`+`cmp`) · m2/m4 1/1 · m12 fronteira 7 params 0/0 · m5/m7/m10 formas legitimas 0/0 · m8 S3267
revertido 1/1 · m9 decl renomeada 1/1 — os 2 ultimos provam zero regressao (a clausula
`chapters.Select(chapter => chapter.HRef)` ficou BYTE-IDENTICA).

## Iter 3 — rodada de warnings (/jdi-issue)

Loop ja convergido na iter 2; esta rodada ataca os 5 warnings da REVIEW. **Diff em `src/` e `test/`:
VAZIO** (`git diff HEAD~3 HEAD -- src/ test/` = 0 linhas) — so gate, CI e doc. Commits: `54cbacc` fix
(gate item 9) · `d31a05c` ci (guard do token) · `af28696` docs (todos).

**W-1 — FECHADO.** Furo: `)` dentro de comentario na assinatura fechava a profundidade de parenteses
e encerrava a varredura cedo; com 8 params reais saia **exit 0** (falso PASS). Fix
(`D-2026-07-30-sonar-zero-issues-9`, append-only, supersede so o `Verify:` do item 9): descartar
comentario de linha e de bloco antes de contar — MESMA tecnica do item 4 de `the-method-refactor` —
mais guarda de PARIDADE DE ASPAS, sem a qual um default `string url = "https://x"` perderia a
virgula do parametro (fail-open novo; com a guarda mede 6, igual ao comando velho).
**20 mutantes, repo real nunca mutado.** Alvo: evasao exata do reviewer (`// ver nota 2)` + 8
params) OLD **0** / NEW **1**; a mesma no OUTRO metodo 0/1; bloco `/* ... ) ... */` de 1 e de 2
linhas 0/1. Nao-regressao: m1/m2/m8/m9 seguem NEW 1. Zero falso positivo em 8 formas legitimas
(generico, `[CallerMemberName]`, default vazio, assinatura em 1 linha, doc `///` e bloco acima da
decl, `//` em literal de string, repo real) — 8/8 exit 0. **Containment medido, nao alegado:** 2 das
3 clausulas ficam byte-identicas e, nos 14 mutantes SEM comentario na assinatura, o N e IDENTICO nos
dois comandos (14/14) — nesse dominio inteiro sao a mesma funcao. A unica divergencia em que NEW
passa e OLD reprova (7 params reais + `// nota, com virgula`) e REPROVACAO FALSA do OLD, residuo ja
declarado por escrito em D-...-8. Zero linha de producao.

**W-2 — NAO FECHADO (nao fechavel localmente).** O Quality Gate real so existe apos push+CI; a API
do SonarCloud da 404 para a branch. Confirmado no `## Deferred to PR review` do CONTEXT.md. Nao ha
como simular sem inventar evidencia.

**W-3(a)/(b) — NAO FECHADO (fora do repo).** "Sonar way" so mede New Code: issue nova em linha
legada nao alterada e smell abaixo do debt ratio nao reprovam. As duas condicoes vivem no Quality
Gate do SonarCloud — config de projeto, nao versionavel, inalcancavel por commit ou `Verify:`. Mudar
e politica com custo real (todo o legado reprovaria de uma vez, exatamente o que D-2 isenta) e cabe
ao dono do projeto. Registrado em `todos.md`.

**W-3(c) — NAO FECHADO (D-...-6).** Job Sonar em `windows-latest` com workload MAUI e infra nova, ja
fora de escopo. Registro em `todos.md` conferido.

**W-3(d) — FECHADO.** `if: env.SONAR_TOKEN != ''` nos 7 steps fazia o mecanismo virar no-op
SILENCIOSO atras de um required check — verde sem escanear e pior que check nenhum. Guard
(`D-2026-07-30-sonar-zero-issues-10`), gated em `== ''`: **falha** onde o secret DEVE existir (repo
de origem: push, PR do proprio repo, `workflow_dispatch`) e emite `::warning` alto onde a ausencia e
legitima e nao ha o que consertar — PR de fork e Dependabot, a quem o GitHub deliberadamente nao
entrega secrets do repo (`.github/dependabot.yml` tem 2 ecossistemas semanais). Falhar neles
deixaria todo PR externo e todo bump semanal vermelho para sempre — dai o carve-out, ele proprio
preso pelo gate. Item 10 do DoD endurecido: o comando anterior e **prefixo literal** do novo, entao
NEW exit 0 implica OLD exit 0 por construcao. **9 mutantes do yml** — guard deletado, `exit 1`
virando `echo`, `if:` invertido, `TOKEN_EXPECTED` fixo em `false`, carve-outs de fork e de
Dependabot removidos, guard duplicado: os 7 OLD **0** / NEW **1**; `sonar.qualitygate.wait=true`
removido 1/1 (protecao original intacta); intacto 0/0.

**W-4 — NAO FECHADO (D-2).** As 9 violacoes de `dotnet format` seguem 9, nos mesmos 4 arquivos
legados fora do diff. Endereco ja existe: a phase `baseline-de-estilo` do ROADMAP, dona do
`.editorconfig`. Consertar aqui e refactor de legado por estilo, proibido por D-2.

**W-5 — NAO FECHADO (D-2), agora ENUMERADO.** `catch { }` em `ReaderPage.xaml.cs:326,434`;
`catch (OperationCanceledException) { }` sem rethrow em `LibraryPageModel.cs:183`,
`ReaderPageModel.cs:222`, `ReaderPage.xaml.cs:308`; static mutavel em `TranslationEngine.cs:16`;
eventos `+=`/`-=` 5/4. Todos pre-existentes em `main`, fora do diff. So existiam no corpo da review
— foram para `todos.md` com file:line e com o motivo de nao serem tocados: vivem no head MAUI, fora
da rede de testes (D-2026-07-30-regression-suite-2), e mexer sem rede troca cheiro conhecido por bug
desconhecido — o "rewrite amplo" que o escopo das phases deste repo proibe.

**Gates re-rodados (iter 3):** build `-c Release` **0 erros**, 64 avisos (identico as iters 1-2) ·
`dotnet test -c Release` **256 / 254p / 2s / 0f** · atributos vivos 233 + 2 `Fact(Skip` = **235**
(baseline intacto — diff de `test/` vazio) · `dotnet format --verify-no-changes` as mesmas **9**
legadas · **10/10 `Verify:` extraidos LITERALMENTE do CONTEXT.md vigente saem exit 0** (itens 9 e 10
com os comandos novos) · `.jdi/DECISIONS.md` append-only: **0 linhas removidas** nos 2 appends.
