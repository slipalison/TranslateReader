# Phase 14: Review (slug: sonar-zero-issues)

**Verdict:** APPROVED_WITH_WARNINGS

REVIEW FINAL da phase — iter 3 (re-verify unica da rodada de warnings do /jdi-issue).
Diff revisado: `main` (`6132078`) ate HEAD (`1f64a8d`), branch `jdi/sonar-zero-issues`,
**20 commits** (o dispatch citou 17; a contagem real via `git rev-list --count` e 20 — os 3
primeiros sao docs de registro/context/plan, anteriores a execucao). Iters 1 e 2 ja haviam sido
aprovadas; a iter 3 fechou W-1 (evasao de comentario no gate do item 9, `D-...-9`) e W-3(d)
(guarda de token no job Sonar, `D-...-10`) — **zero linha de producao ou teste mudou nas iters 2
e 3** (provado abaixo, item e).

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `net10.0-windows10.0.19041.0` Release: **0 erros** (avisos = familia `MVVMTK0045` pre-existente do app MAUI) |
| Tests | PASS | **256 total / 254 aprovados / 2 skip / 0 falha** — identico a baseline da phase (256); >= 167 (D-2). Atributos `[Fact]/[Theory]`: **235** (19 adicionados na phase, **0 removidos**) |
| Coverage | PASS | Escopo D-6 (linhas de producao ALTERADAS): **68/68 instrumentadas cobertas = 100,0%** (threshold 90%). Agregado 88,53% (contexto, nao e o gate). 0 arquivo novo de producao; teste novo `CultureRoundTripTests.cs` |
| Lint | WARN | `dotnet format --verify-no-changes`: **9 violacoes**, nos mesmos 4 arquivos legados (`ThemeEngine.cs` 2, `ReaderPage.xaml.cs` 2, `ThemeEngineTests.cs` 1, `TranslationManagerTests.cs` 4) — os 4 com **0 linhas no diff da phase** (D-2; endereco = phase `baseline-de-estilo`) |
| Security/Layer | PASS (warns legados) | 5.1/5.2/5.7/5.9/5.10/5.17 limpos; 5.3 = 4 hits, todos self-interface (ok); OCE em `TranslationManager.cs:61` faz `throw;` (conforme); 5.11 eventos 5/4, 5.12 static 1, 5.15 catches vazios — tudo baseline legado do head MAUI, fora do diff, enumerado em `todos.md` (W-5) |
| Consistency | PASS | 20/20 commits Conventional com scope `sonar-zero-issues`, tipos variados por natureza (D-4): chore/refactor/fix/ci/docs. `files_modified` do PLAN todos presentes no log. 8/8 tasks completed |
| UI Validation | SKIPPED | has_frontend=false (cliente MAUI nativo) — por design, nunca bloqueia |
| DoD | PASS | **10/10 Auto PASS, 0 Manual** — comandos extraidos LITERALMENTE do CONTEXT.md vigente (itens 9 e 10 nas versoes `D-...-9`/`D-...-10`). PROJECT.md nao tem secao DoD propria; os 10 itens vem do CONTEXT.md |

## Blockers

Nenhum.

## Warnings

1. **W-A — Quality Gate real so e observavel apos push+CI.** Os `Verify:` provam identidade
   local (D-...-6); o resultado do scan remoto esta em `## Deferred to PR review` do CONTEXT.md.
   Nao fechavel em repo.
2. **W-B — "Sonar way" so mede New Code.** Issue nova flagrada em linha legada nao alterada e
   smell abaixo do debt ratio nao reprovam o gate. Config do Quality Gate vive no SonarCloud
   (fora do repo, nao versionavel). Registrado em `todos.md` `[CI/QUALITY-GATE]` como decisao de
   politica do dono do projeto.
3. **W-C — C# do app MAUI e estruturalmente invisivel ao scan** (o job so compila Core+testes,
   D-...-6). Fechar exigiria job Sonar em `windows-latest` com workload MAUI — infra nova, fora
   de escopo, registrado em `todos.md`.
4. **W-D — 9 violacoes de `dotnet format` legadas** (4 arquivos fora do diff). Dona: phase
   `baseline-de-estilo`.
5. **W-E — achados legados do head MAUI** (catch vazio `ReaderPage.xaml.cs:326,434`; OCE
   engolido `LibraryPageModel.cs:183`, `ReaderPageModel.cs:222`, `ReaderPage.xaml.cs:308`;
   static mutavel `TranslationEngine.cs:16`; eventos 5/4) — pre-existentes em `main`, fora do
   diff e fora da rede de testes (D-2026-07-30-regression-suite-2); enumerados com file:line em
   `todos.md`.
6. **W-F — carve-out de fork/Dependabot e um bypass DELIBERADO do required check** (julgamento
   do item c do ceticismo, detalhe em "Para o revisor humano"). Aceito com mitigacoes; precisa
   ficar visivel a quem revisa PR externo.
7. **W-G — pior caso do proprio mecanismo no primeiro CI run** (item g): se o SonarCloud
   contabilizar as ~20 linhas JS alteradas como 0% de cobertura (nenhum report JS e importado),
   coverage on New Code cairia a ~68/88 = 77% < 80% e o proprio PR da phase ficaria vermelho.
   Cenario provavel e o oposto (sem propriedade de cobertura JS configurada, o sensor JS nao
   contribui e as linhas C# alteradas estao 100% cobertas pelo MESMO run opencover) — mas so o
   push decide. Ver "Para o revisor humano".

## Evidencia do ceticismo (verificada por esta review, nao herdada)

### (a) Gate do item 9 (S107) — reescrito 2x, provado sem regressao e sem falso positivo novo

Harness proprio: 13 mutantes de `TranslationManager.cs` em `/tmp/rev3/s107` (repo real intocado,
`git status --porcelain` limpo ao final), comando NOVO extraido literalmente do CONTEXT.md:

| Mutante | Esperado | Medido |
|---|---|---|
| m0 copia intacta | exit 0 | exit 0 |
| m1 8 params na DECL de `TranslateChaptersWithCacheAsync` | exit 1 | **exit 1** |
| m2 8 params na DECL de `TranslateSingleChapterAsync` | exit 1 | **exit 1** |
| m8 S3267 revertido | exit 1 | **exit 1** |
| m9 declaracao renomeada | exit 1 | **exit 1** |
| w1a evasao `// ver nota 2)` + 8 params (metodo 1) | exit 1 | **exit 1** |
| w1f mesma evasao no metodo 2 | exit 1 | **exit 1** |
| w1b bloco `/* ... ) ... */` 1 linha + 8 params | exit 1 | **exit 1** |
| w1c bloco quebrado em 2 linhas + 8 params | exit 1 | **exit 1** |
| res default `string url = "https://x"` (6 params reais) | exit 0 | exit 0 |
| lit default `"a // b, c"` (`//` e virgula DENTRO de literal) | exit 0 | exit 0 |
| xmldoc `///` acima da declaracao | exit 0 | exit 0 |
| blockabove `/* */` acima da declaracao | exit 0 | exit 0 |

**Guarda de paridade de aspas provada load-bearing** (alegacao do doer testada por mim): variante
ingenua do awk (corte incondicional no primeiro `//`) mede `res` = **5** — subconta, um 8o
parametro poderia se esconder = fail-OPEN real; o comando entregue mede **6** (correto).
Desvio encontrado: em `lit`, o entregue mede **7** com 6 params reais (virgula dentro de literal
conta como separador) — **fail-closed** (so reprovacao falsa, nunca aprovacao falsa) e ja
DECLARADO como residuo em `D-...-8`. Nenhum fail-open novo → nenhum blocker.

### (b) Gate do item 10 — prefixo literal confirmado + 9 mutantes do yml

Prefixo: comando velho (106 chars) e prefixo byte-a-byte do novo (510 chars), seguido de ` && ` —
`NEW exit 0 => OLD exit 0` por construcao. Mutantes (em `/tmp/rev3/yml`, cada um conferido
diferente do original):

| Mutante | Medido |
|---|---|
| intacto | exit 0 |
| guard deletado | **exit 1** |
| `exit 1` → `echo` | **exit 1** |
| `if:` invertido (`!= ''`) | **exit 1** |
| `TOKEN_EXPECTED` hardcoded `'false'` | **exit 1** |
| carve-out de fork removido | **exit 1** |
| carve-out de Dependabot removido | **exit 1** |
| guard duplicado | **exit 1** |
| `sonar.qualitygate.wait=true` removido | **exit 1** (protecao original intacta) |

Semantica do `if:`/expressao conferida contra o comportamento documentado do GitHub Actions:
dentro de reusable workflow o contexto `github` e o do caller (evento original), `&&`
curto-circuita antes de desreferenciar `github.event.pull_request` em push, `github.actor` de PR
do Dependabot e `dependabot[bot]`. `pipeline.yml:50-60` passa `SONAR_TOKEN` explicitamente
(D-2026-07-28-pipeline-unificada-7).

### (c) O carve-out abriu buraco? — SIM, e um bypass; julgado risco aceitavel COM registro

Hoje, um PR de fork: secret ausente → guard emite `::warning` e sai 0 → os 7 steps uteis sao
pulados → job `sonarqube` (required check) fica **VERDE sem escanear**. Isso e um bypass do
mecanismo anti-recorrencia via fork. Julgamento: **aceitavel**, porque (1) o GitHub nao entrega
secrets a PR de fork por design — nao existe forma segura de rodar o scanner CLI com token em
fork PR (`pull_request_target` com checkout de codigo de fork e um anti-padrao de seguranca
conhecido); (2) falhar nesses contextos deixaria TODO PR externo e todo bump semanal vermelho
para sempre (pior); (3) mitigacoes reais: branch protection exige review humano, CodeQL/Semgrep
rodam em PR de fork sem secret, e o primeiro `push` em `main` apos o merge RODA o gate com token
— regressao contrabandeada por fork falha o pipeline de `main` imediatamente apos o merge
(deteccao ruidosa, nao prevencao). O `::warning` do guard e o sinal para o revisor humano tratar
PR de fork com rigor extra. Registrado como W-F.

### (d) Caminho JDI-legal — limpo

`git diff 6132078..HEAD -- .jdi/DECISIONS.md | grep -c '^-[^-]'` = **0**. Mais forte: as 944
linhas do DECISIONS.md em `bd2c3a2` sao **prefixo byte-identico** do arquivo em HEAD (`cmp`
limpo) — D-...-0..8 (incluindo -5/-7/-8) intactas por construcao; -9 e -10 sao append puro.
CONTEXT.md desde `bd2c3a2`: o diff toca SOMENTE `Verify:`/`Source:` do item 9 e
texto/`Verify:`/`Source:` do item 10 — os outros 8 itens byte-identicos.

### (e) Producao e teste intocados nas iters 2 e 3

`git diff bd2c3a2 HEAD -- src/ test/` = **0 linhas**; `git diff 1e0a3f8 HEAD -- src/ test/` =
**0 linhas**.

### (f) As 113 issues — placar reconferido contra o inventario canonico

Soma por arquivo do inventario (`sonar-main-inventory.md`): 41+16+8+7+6+4+4+3+3+3+2+2+2+2+2+
1+1+1+1+1+1+1+1 = **113**. Destino: **41 REMOCAO** (`dotnet-install.ps1`, todas `powershelldre:*`)
· **2 EXCLUSAO** multicriteria (`Web:S7926` + `css:S4667`, presentes nos args do `begin`) ·
**3 WAIVER** `#pragma` (SYSLIB1044 em `HtmlUtility` + 2x xUnit1004 em `TranslationEngineTests`)
· **67 FIX** = 113. No diff de `src/`+`test/`+`.github/`: exatamente **2 pares**
`#pragma disable/restore` e **0** `NoWarn`/`SuppressMessage`/`sonar.exclusions` (as 5 mencoes no
diff total sao prosa de registro em `.jdi/`).

### (g) O que acontece quando o CI rodar

- **Mais provavel:** PR same-repo → token presente → guard nem roda → scan + Quality Gate sobre
  New Code do PR. Linhas C# alteradas 100% cobertas pelo MESMO run opencover que o job importa;
  as correcoes removem exatamente as regras flagradas, sem issue nova esperada → **verde**.
- **Pior caso 1 (W-G):** SonarCloud contar as ~20 linhas JS alteradas como 0% de cobertura →
  coverage on New Code ~77% < 80% → QG reprova e o proprio PR da phase fica vermelho. Correcao,
  se ocorrer, e config (`sonar.coverage.exclusions` para `wwwroot/js/**`) — decisao pos-push.
- **Pior caso 2 (by design):** `SONAR_TOKEN` sumido/rotacionado no repo de origem → o guard NOVO
  falha o job com `::error` — e exatamente o comportamento que D-...-10 quer.

### (h) Regressao de teste na phase inteira

`git diff 6132078 HEAD -- test/`: **19 atributos adicionados, 0 removidos**; 2 unicas linhas de
`Assert` removidas foram atualizacao MECANICA no mesmo commit da migracao JS
(`items[i].index` → `item.index`, T-2/`294d316`) — mesma forca, variavel renomeada. Run real:
256/254/2/0.

## DoD Checklist (gate 8)

Comandos extraidos literalmente do CONTEXT.md vigente e executados nesta review:

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | `dotnet-install.ps1` removido; nenhum rastreado fora de `.jdi/` referencia | CONTEXT (D-...-1/-7) | Auto | PASS | exit 0 |
| 2 | `HtmlUtility`: 7+ `[GeneratedRegex]`, pragma SYSLIB1044, `InjectTags` <=4 `if` | CONTEXT (D-...-3) | Auto | PASS | exit 0 |
| 3 | WebView JS: `.dataset`, `for-of`, `Number.parseInt`, optional chaining | CONTEXT (D-...-5) | Auto | PASS | exit 0 |
| 4 | `HtmlInjectionTests`: 0 `Matches().Count`, >=3 `[GeneratedRegex]` | CONTEXT (D-...-3) | Auto | PASS | exit 0 |
| 5 | `FileUtilityTests` assert real; CA1816 nos 7 Dispose; CA1847 | CONTEXT (D-...-3) | Auto | PASS | exit 0 |
| 6 | `TranslationEngine` sealed+SuppressFinalize; xUnit1004 pragma; 2 Skip | CONTEXT (D-...-3) | Auto | PASS | exit 0 |
| 7 | `index.html` lang+title; `user-scalable=no` mantido; waivers no yml | CONTEXT (D-...-3/-4) | Auto | PASS | exit 0 |
| 8 | Access+Engine: `OpenAsync`, `BeginTransactionAsync`, `InvariantCulture`, S1192 | CONTEXT (D-...-5) | Auto | PASS | exit 0 |
| 9 | S107 <=7 params DECLARADOS (comentarios descartados) + S3267 | CONTEXT (D-...-9) | Auto | PASS | exit 0; matriz 13 mutantes acima |
| 10 | `qualitygate.wait=true` + guard de token com carve-out fork/Dependabot | CONTEXT (D-...-10) | Auto | PASS | exit 0; matriz 9 mutantes acima |

**Totals:** 10 items | Auto: 10 (10 PASS, 0 FAIL) | Manual: 0 pending

## Estado final da phase

**Placar das 113 issues** (inventario canonico reconferido): **67 FIX** em codigo · **41
REMOCAO** (`dotnet-install.ps1` APAGADO do repo) · **2 EXCLUSAO** auditavel via
`sonar.issue.ignore.multicriteria` (`Web:S7926`, `css:S4667` — `sonarqube.yml:95-99`) · **3
WAIVER** `#pragma` com comentario citando decisao. Zero issue silenciada sem registro.

**O que mudou em PRODUCAO** (tudo na iter 1, commits `8e6200f..6cc6200`): `HtmlUtility.cs`
(GeneratedRegex + decomposicao de `InjectTags`), `TranslationEngine.cs` (sealed + dispose),
`ParsingEngine.cs` (I/O async), `BooksAccess/SettingsAccess/ReadingStateAccess/
BookTranslationJobAccess.cs` (transacao async, `InvariantCulture`, S1192), `TranslationManager.cs`
(objeto de contexto `TranslationRun`, S3267), 3 JS do WebView + `index.html`, e DELETE de
`dotnet-install.ps1`. **O que mudou so em gate/CI/doc** (iters 2 e 3, `cbefcbf..1f64a8d`):
`Verify:` dos itens 9 (2x) e 10, guard de token em `sonarqube.yml`, DECISIONS/CONTEXT/SUMMARY/
todos.md. Diff de `src/`+`test/` nas iters 2-3: **vazio**.

**Numeros finais:** build 0 erros · testes 256 (254p/2s/0f), 235 atributos, 0 removido ·
cobertura das 68 linhas alteradas de producao 100% (agregado 88,53%) · format 9 violacoes, todas
legadas fora do diff · DoD 10/10 · DECISIONS.md append-only (0 remocoes; prefixo de 944 linhas
byte-identico).

## Para o revisor humano do PR

O que o gate automatizado NAO prova — leia isto antes de aprovar:

1. **Um arquivo foi APAGADO do repo:** `dotnet-install.ps1` (1573 linhas, script vendored da
   Microsoft, zero referencia rastreada, re-obtenivel em dotnet.microsoft.com). 41 das 113
   issues fecham por essa remocao. Confirme que voce concorda com remocao (nao exclusao de scan).
2. **Waiver de acessibilidade:** `user-scalable=no` foi MANTIDO em `index.html` por decisao de
   produto (paginacao exige viewport fixo; tipografia configuravel cobre WCAG 1.4.4;
   precedente Kindle/Apple Books). E chamada de produto/UX — julgue-a; o comando so prova que a
   linha esta la.
3. **Carve-out de fork/Dependabot:** PR de fork e PR do Dependabot rodam SEM o scan do Sonar e o
   required check `sonarqube` fica VERDE com um `::warning` (GitHub nao entrega secrets nesses
   contextos). E um bypass deliberado e registrado do gate: em PR externo, o seu review E o
   gate; a regressao so seria pega pelo pipeline de `main` apos o merge.
4. **O Quality Gate real so existe apos o push.** Todos os `Verify:` locais passam, mas o scan
   remoto pode divergir — pior caso mapeado: cobertura de New Code contando as ~20 linhas JS
   sem report como 0% (ficaria ~77% < 80% e o PR vermelho). Se ocorrer, e config de scan, nao
   defeito de codigo.
5. **Confirmacao funcional do WebView** (zoom, scroll-sync, overlay de traducao) apos a migracao
   mecanica de `translation.js`/`scroll.js`/`bridge.js` — sem harness JS no repo (D-...-5),
   ninguem alem de um humano abre o reader e olha.

## Recommendation

APPROVED_WITH_WARNINGS. Nenhum blocker; os 7 warnings sao (i) limites estruturais fora do
alcance do repo, todos registrados em `todos.md`/Deferred, ou (ii) riscos pos-push mapeados com
plano de correcao. Prosseguir para `/jdi-ship sonar-zero-issues` e abrir o PR; a primeira rodada
de CI decide W-A/W-G, e os itens 1-5 acima sao a pauta do revisor humano.

**Verdict:** APPROVED_WITH_WARNINGS

## DoD Critic (enhanced — forcado por /jdi-issue, passe final antes do ship)

NOTA DE EXECUCAO: rodado inline pelo orquestrador (o sub-agente critico foi terminado por limite de
sessao da API na iter 1; o protocolo foi mantido — comandos extraidos por parser do CONTEXT.md
vigente e executados contra copias sob `$CLAUDE_JOB_DIR/tmp`, repo real nunca mutado). Os itens 9 e
10 mudaram na rodada de warnings, entao a aprovacao anterior nao os cobria; os outros 8 seguem
byte-identicos desde `bd2c3a2` (hunk unico confirmado no diff do CONTEXT.md) e mantem o julgamento
das passagens anteriores.

**Item 9 (S107, terceira versao do comando) — nao oco, sem regressao, sem falso positivo:**

| Mutante | Resultado |
|---|---|
| repo real | exit 0 |
| `// ver nota 2)` na assinatura + 8 params declarados (a evasao W-1) | **exit 1 — fechada** |
| `/* nota ) fim */` na assinatura + 8 params declarados | **exit 1 — fechada** |
| 8 params na declaracao de `TranslateSingleChapterAsync` | **exit 1** (sem regressao) |
| clausula S3267 revertida | **exit 1** (sem regressao) |
| default legitimo `string url = "https://x"` (6 params reais) | exit 0 — **sem falso positivo** |

O ultimo caso e o que a guarda de paridade de aspas existe para proteger: o strip ingenuo de `//`
comeria a virgula seguinte e subcontaria. Confirmado que a guarda e load-bearing.

**Item 10 (`qualitygate.wait` + guarda de token) — nao oco:**

| Mutante no `.github/workflows/sonarqube.yml` | Resultado |
|---|---|
| real | exit 0 |
| `exit 1` da guarda virando `echo skip` | **exit 1** |
| step de guarda deletado inteiro | **exit 1** |
| `if: env.SONAR_TOKEN == ''` invertido para `!= ''` | **exit 1** |
| `/d:sonar.qualitygate.wait=true` removido | **exit 1** (protecao original intacta) |

**Achado que o critico confirma e encaminha ao humano (nao e blocker):** o carve-out de fork
significa que um PR vindo de fork deixa o check `sonarqube` VERDE sem escanear. Isso e consequencia
do design do GitHub (secrets nao vao para fork), nao defeito do commit — mas e um bypass real do
mecanismo anti-recorrencia e precisa estar visivel para quem revisa, nao enterrado numa decisao.
Esta na pauta do revisor humano.

**Verdict:** APPROVED
