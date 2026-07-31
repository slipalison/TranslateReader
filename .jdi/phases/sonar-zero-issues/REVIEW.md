# Phase 14: Review  (slug: sonar-zero-issues)

**Verdict:** APPROVED_WITH_WARNINGS

Review FINAL da phase (iter 2). Regenerada do zero, sem herdar texto da iter 1. Escopo revisado:
diff `6132078` (main) -> `bd2c3a2` (HEAD, branch `jdi/sonar-zero-issues`), 15 commits (8 de task +
7 de docs/gate). Historico: iter 1 entregou as 8 tasks e foi BLOCKED pelo DoD critic (o `Verify:`
do item 9 media o CALL SITE, nao a declaracao — contra-exemplo executado); iter 2 consertou SO o
gate via `D-2026-07-30-sonar-zero-issues-8` (append-only), sem tocar producao nem teste. Toda
evidencia abaixo foi produzida por esta review (comandos executados agora), nao copiada do SUMMARY.

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `dotnet restore` + `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0`: **0 erros**, 40 avisos (todos `MVVMTK0045` pre-existentes do app MAUI) |
| Tests | PASS | `dotnet test -c Release`: **256 total / 254 aprovados / 2 ignorados / 0 falhas**. Baseline 167 (D-2) e piso 229 do PLAN superados (+27 novos). Atributos `[Fact]`/`[Theory]` = **235**; `Fact(Skip` = **2** (inalterado) |
| Coverage | PASS | 0 arquivo `.cs` NOVO em `src/` pos-boundary `4285f25` (gate por arquivo novo = N/A). D-6 sobre linhas ALTERADAS de producao, medicao propria (git diff -U0 x cobertura.xml): **68 linhas cobertaveis alteradas, 68 cobertas = 100,0%**. Agregado Core 88,53% (contexto, nao e o gate) |
| Lint | WARN | `dotnet format TranslateReader.slnx --verify-no-changes`: **9 erros**, todos em 4 arquivos LEGADOS fora do diff da phase (`ThemeEngine.cs`, `ReaderPage.xaml.cs`, `ThemeEngineTests.cs`, `TranslationManagerTests.cs`) — D-2, aguarda `baseline-de-estilo` |
| Security/Layer | PASS | 5.1 Client->Access/Engine: limpo. 5.2 storage em contratos: limpo. 5.3 Manager->Manager: so auto-referencia de interface. 5.10 sync-over-async: limpo; `catch (OperationCanceledException)` em `TranslationManager.cs:61` persiste "Paused" e **rethrows** (conforme). 5.14/5.16/5.17: limpos. Legados conhecidos nao tocados em W-5 |
| Consistency | PASS | 8 tasks = 8 commits atomicos + docs; Conventional Commits com escopo `sonar-zero-issues` e tipos variados (chore/refactor/fix/ci/docs — D-4). Todos os `files_modified` do PLAN aparecem nos commits. Nota menor: o comentario do pragma `xUnit1004` cita D-2026-07-30-regression-suite-5(2) mas nao cita D-...-3 literalmente (acceptance do T-5 pedia ambos) — cosmetico, mecanismo e decisao estao citados |
| UI Validation | SKIPPED | has_frontend=false (cliente MAUI nativo; WebView coberto pelo 5.8 — `JsStr(...)` intacto, phase nao tocou `ReaderPage.xaml.cs`) |
| DoD | PASS | **10/10 Auto PASS** (comandos extraidos literalmente do CONTEXT.md vigente), 0 Manual |

## Blockers

Nenhum.

## Warnings

- **W-1 (gate do item 9 — residuo fail-open NAO declarado pelo doer, medido por esta review):**
  comentario `//` com `)` desbalanceado dentro da assinatura encerra a varredura do awk cedo.
  Medido: 5 params reais + `// ver nota 2)` -> N=3 (subconta); COMPOSICAO adversarial (mesmo
  comentario + 8 params reais na declaracao) -> **exit 0** (falso PASS). Nao vira blocker por tres
  motivos objetivos: (1) nenhuma das 5 formas legitimas testadas produz veredito errado sozinha
  (tabela em (b) abaixo — o furo exige comentario anomalo E violacao S107 SIMULTANEOS, sem caminho
  acidental: hoje as 2 assinaturas nao tem comentario algum); (2) o backstop semantico e exatamente
  o mecanismo que esta phase instala — o S107 do proprio SonarCloud (analisador Roslyn, imune a
  truque textual) com `qualitygate.wait=true` em New Code; (3) jurisprudencia locked do repo
  (D-2026-07-30-the-method-refactor-9: evasoes que exigem parser C# sao residuo declarado +
  PR review humano, nao meia-solucao heuristica). Recomendacao roteavel a `todos.md`: descartar
  `//...` na varredura do awk — a MESMA tecnica ja usada pelo gate do item 4 de `the-method-refactor`
  (D-...-8 daquela phase), custo trivial, zero falso positivo novo.
- **W-2 (Quality Gate remoto inobservavel localmente):** a API do SonarCloud da 404 para
  `branch=jdi/sonar-zero-issues` (sem analise ainda — resultado so existe apos push+CI, como
  D-...-6 preve). Risco de o PR ficar vermelho pelo proprio mecanismo do T-8: **baixo,
  quantificado** — ver item (g) da auditoria abaixo. `main` hoje: gate OK, `new_coverage` 88,7%
  vs limiar 80%.
- **W-3 (limites nomeados do mecanismo anti-recorrencia — gap estrutural, nao defeito da phase):**
  o gate "Sonar way" mede SO New Code. Ele NAO pega: (a) issue nova levantada em linha LEGADA nao
  alterada (upgrade de regra/analisador flagra codigo velho e o gate segue verde); (b) smell de
  New Code abaixo do debt ratio do rating A (~5% de technical debt ratio) — smells pequenos
  acumulam sem reprovar; (c) o C# do app MAUI (`src/TranslateReader`): o job Sonar compila apenas
  Core+testes, `PageModels/`, `Pages/*.xaml.cs`, `Platforms/`, `MauiProgram.cs` sao invisiveis ao
  scan (D-...-6, registrado em `todos.md`); (d) `if: env.SONAR_TOKEN != ''` em todos os steps —
  sem o secret (fork/clone), o mecanismo inteiro vira no-op SILENCIOSO, nao falha.
- **W-4 (lint legado):** as 9 violacoes de `dotnet format` pre-existentes (mesmo numero da iter 1),
  todas fora do diff — nada introduzido pela phase.
- **W-5 (legados do gate 5, nenhum tocado pela phase, todos pre-existentes em `main`):**
  `catch { }` em `ReaderPage.xaml.cs:326,434`; `catch (OperationCanceledException) { }` sem rethrow
  em `LibraryPageModel.cs:183`, `ReaderPageModel.cs:222`, `ReaderPage.xaml.cs:308` (conversao de
  cancelamento no boundary de UI); static mutavel `TranslationEngine.cs:16` (baseline conhecido do
  bootstrap); eventos `+=`/`-=` = 5/4 (desequilibrio baseline). Candidatos a phases futuras, nao
  bloqueiam sob D-2.

## Auditoria cetica da iter 2 (evidencia propria, itens (a)-(h) do dispatch)

**(a) Regressao de gate — matriz executada por esta review** (copias em `/tmp/tmp.p8f6KPccpM/`,
repo real NUNCA mutado — `git status --porcelain -- src/ test/` vazio ao final; Nc/Ns = params
medidos em `TranslateChaptersWithCacheAsync`/`TranslateSingleChapterAsync`):

| Caso | Estado | gate (NOVO) | Nc/Ns | Veredito |
|---|---|---|---|---|
| m0 | repo real + copia intacta | exit 0 | 5/5 | correto |
| m1 | 8 params na DECL de `...ChaptersWithCache` (contra-exemplo do critic) | **exit 1** | 8/5 | furo FECHADO |
| m2 | 8 params na DECL de `...SingleChapter` | **exit 1** | 5/8 | correto |
| m3 | REORDER puro (decls movidas para depois dos chamadores) | exit 0 | 5/5 | gate deixou de ser posicional |
| m4 | m3 + 8 params | **exit 1** | 5/8 | correto |
| m8 | clausula S3267 revertida (`foreach (var chapter in chapters)`) | **exit 1** | 5/5 | protecao antiga preservada |
| m9 | declaracao renomeada (ancora ausente) | **exit 1** | — | falha ruidosa deliberada |

m3 provado como permutacao pura de linhas: `sort`+`cmp` identicos modulo CR (o arquivo original e
CRLF; o sed do harness normaliza — 317 bytes de diferenca = exatamente 317 CRs, conteudo identico).
A clausula S3267 e byte-identica entre comando velho e novo (visivel no diff do CONTEXT.md).
Nenhum estado ERRADO passou; nenhuma protecao do comando antigo se perdeu (m2/m8/m9 seguem presos).

**(b) Falso positivo — 5 formas legitimas testadas:**

| Forma | N medido (reais) | Veredito do gate | Direcao |
|---|---|---|---|
| default `string extra = "a,b"` (6 reais) | 7 | exit 0 | superestima: **fail-closed** (reprovaria falso so em 7 reais + virgula em string) |
| generico `IReadOnlyDictionary<string, int>` (6 reais) | 6 | exit 0 | exato |
| atributo `[EnumeratorCancellation]` (6 reais) | 6 | exit 0 | exato |
| assinatura em 1 linha (5 reais) | 5 | exit 0 | exato |
| comentario `// capitulo, ja resolvido` (5 reais) | 6 | exit 0 | superestima: **fail-closed** |
| comentario `// ver nota 2)` (5 reais) | 3 | exit 0 | subconta: **fail-open em composicao** -> W-1 |

Nenhuma forma legitima produz falso positivo hoje; os residuos DECLARADOS pelo doer (virgula em
string default; `<`/`>` desbalanceado) foram confirmados na direcao declarada. O unico residuo
fail-open encontrado (parenteses desbalanceado em comentario) NAO estava declarado -> W-1.

**(c) Caminho JDI-legal:** `git diff 6132078..HEAD -- .jdi/DECISIONS.md | grep -c '^-[^-]'` = **0**
(append puro; D-...-1 a D-...-7 intactas, D-...-8 apendada ao fim). `git diff 1e0a3f8 HEAD --
CONTEXT.md` = **1 unico hunk**, restrito ao bloco do item 9 (linha `Verify:` + linha `Source:`);
os outros 9 itens byte-identicos. Comando do item 9 conferido **byte-identico** (755 bytes) entre
DECISIONS.md e CONTEXT.md via `cmp`.

**(d) Producao intocada na iter 2:** `git diff 1e0a3f8 HEAD -- src/ test/` = **vazio** (0 linhas).
A iter 2 tocou apenas `.jdi/DECISIONS.md` (+86), `CONTEXT.md` (2 linhas) e `SUMMARY.md`.

**(e) As 113 issues — cruzamento item a item contra o inventario** (`sonar-main-inventory.md`,
soma por arquivo conferida = 113): **67 FIX** = 15 `HtmlUtility` (7 S6444 + 7 SYSLIB1045 + 1 S3776)
+ 17 JS (8 `translation.js` + 7 `scroll.js` + 2 `bridge.js`) + 2 BUG `index.html` (S5254 +
PageWithoutTitleCheck) + 4 `ParsingEngine` (S6966) + 3 `TranslationManager` (2 S107 + S3267) + 9
nos 4 `*Access` (3+3+2+1) + 2 `TranslationEngine` (S3881 + CA1816) + 15 em testes (7 CA1816 + 3
CA1875 + 3 SYSLIB1045 + S2699 + CA1847) · **41 REMOCAO** (`dotnet-install.ps1`) · **2 EXCLUSAO**
multicriteria (`Web:S7926` + `css:S4667`, comentadas no yml citando D-...-3/4) · **3 WAIVER**
pragma (1 SYSLIB1044 + 2 xUnit1004). **67+41+2+3 = 113** — nenhuma orfa, nenhuma dupla contagem.
Silenciamento fora da taxonomia: `git diff` da phase inteira contem exatamente 2 pares
`#pragma disable/restore` (SYSLIB1044, xUnit1004) e zero `NoWarn`/`SuppressMessage`/`sonar.exclusions`.

**(f) Mecanismo anti-recorrencia:** `sonarqube.yml:104` — `dotnet-sonarscanner end
/d:sonar.token="$SONAR_TOKEN" /d:sonar.qualitygate.wait=true` no comando REAL executado, com
comentario citando D-...-2; job `sonarqube` chamado por `pipeline.yml:50-60`. DoD 10 exit 0.
Gaps remanescentes nomeados em W-3.

**(g) Risco pratico do T-8 para o proprio PR:** consultei a API publica — a branch ainda nao tem
analise (404), `main` esta OK com as 6 condicoes do "Sonar way" verdes (`new_coverage` 88,7% vs 80).
Avaliacao: risco **baixo**. As linhas C# novas/alteradas estao 100% cobertas (68/68, medicao
propria); `new_duplicated_lines_density` ~0; hotspots 0. Residuo de incerteza real e duplo:
(1) a metrica `new_coverage` do Sonar e blended (linhas+conditions) — condition coverage das
linhas alteradas nao e medivel por esta review, mas os branches novos (`BuildFallbackHtml`,
`InjectTags` decomposto, parsers de data) tem teste dedicado; (2) as linhas JS alteradas nao tem
cobertura importada (sem harness JS) — no comportamento do SonarQube/Cloud pos-6.2, arquivo sem
dado de cobertura fica FORA da metrica (nao conta como 0%), entao nao deve arrastar o numero. Se o
gate reprovar mesmo assim, a causa provavel e uma dessas duas — diagnostico ja encaminhado no
`Deferred to PR review`.

**(h) Regressao de teste:** diff completo de `test/` na phase (13 arquivos, +437/-19): **0 assert
removido sem substituto mais forte** — `items[i].index/translated` -> `item.index/translated`
(mecanico, acompanha `for`->`for-of`); `p.Contains("5")` -> `p.Contains('5')` mantendo as duas
condicoes do `Arg.Is`; `DeleteDirectoryAsync_DoesNotThrowForNonExistentDirectory` GANHOU assert
(`Record.ExceptionAsync` + `Assert.Null` — fecha o BLOCKER S2699 de verdade); 7 `Dispose()`
expandidos so com `GC.SuppressFinalize(this)`. **0 `Skip` novo, 0 removido** (segue 2). +27 testes
novos (CultureRoundTripTests 130 linhas; 3 `CreateTranslatedEpubAsync` em ParsingEngineTests com
asserts load-bearing: entry traduzida, `<dc:title>`, original intocado; casos novos em
HtmlInjectionTests). Nenhuma assercao virou execucao muda.

## DoD Checklist (gate 8)

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | `dotnet-install.ps1` removido; nenhum rastreado fora de `.jdi/` referencia | CONTEXT (D-...-1/-7) | Auto | PASS | exit 0; unica referencia era a permissao stale de `settings.local.json`, removida no mesmo commit `8e6200f` |
| 2 | `HtmlUtility`: 0 Regex estatico, >=7 `[GeneratedRegex]`, pragma SYSLIB1044, `InjectTags` <=4 `if` | CONTEXT (D-...-3) | Auto | PASS | exit 0; 8 `[GeneratedRegex]`, todos com timeout (const `RegexTimeoutMilliseconds=1000`, 3o arg posicional — S6444 fechado); pragma com WHY citando D-...-3(c); decomposicao real (`InjectCss` extraido, contado DENTRO da janela do awk) |
| 3 | WebView JS: `.dataset`, `for-of`, `Number.parseInt`, optional chaining | CONTEXT (D-...-5) | Auto | PASS | exit 0 |
| 4 | `HtmlInjectionTests`: 0 `Matches().Count`, >=3 `[GeneratedRegex]` | CONTEXT (D-...-3) | Auto | PASS | exit 0 |
| 5 | Assert real no teste S2699 + CA1816 nos 7 arquivos + CA1847 | CONTEXT (D-...-3) | Auto | PASS | exit 0; assert confirmado load-bearing no diff |
| 6 | `TranslationEngine` sealed + SuppressFinalize; pragma xUnit1004; `Fact(Skip` == 2 | CONTEXT (D-...-3) | Auto | PASS | exit 0 |
| 7 | `index.html` lang+title; `user-scalable=no` mantido; waivers no yml | CONTEXT (D-...-3/-4) | Auto | PASS | exit 0; multicriteria e1/e2 nos args reais do `begin` |
| 8 | Access+Engine mecanicos (OpenAsync, BeginTransactionAsync, InvariantCulture, S1192) | CONTEXT (D-...-5) | Auto | PASS | exit 0 |
| 9 | `TranslationManager` S107 <=7 params declarados + S3267 | CONTEXT (D-...-5/-8) | Auto | PASS | exit 0 no repo real (5/5 params); gate validado por matriz propria — secoes (a)/(b) |
| 10 | `sonar.qualitygate.wait=true` no `dotnet-sonarscanner end` | CONTEXT (D-...-2) | Auto | PASS | exit 0; `sonarqube.yml:104` |

**Totals:** 10 items | Auto: 10 (10 PASS, 0 FAIL) | Manual: 0 pending

## Estado final da phase

**O que mudou em PRODUCAO (tudo na iter 1):** `HtmlUtility.cs` (8 `[GeneratedRegex]` com timeout,
`InjectTags` decomposto, pragma SYSLIB1044), `ParsingEngine.cs` (4x `OpenAsync`, `await using`),
`TranslationEngine.cs` (sealed + `GC.SuppressFinalize`), `TranslationManager.cs` (record privado
`TranslationRun` reduz os 2 helpers de 8->5 params; loop S3267 via `Select`), 4 `*Access`
(`BeginTransactionAsync`, `DateTime.Parse` invariante, const `$bookId`), `index.html`
(`lang="pt-BR"` + `<title>`), 3 JS do WebView (`.dataset`, `for-of`, `Number.parseInt`, optional
chaining). Testes: +27 (CultureRoundTrip, 3 CreateTranslatedEpub, casos HtmlInjection) e hygiene
mecanica em 10 arquivos.

**O que mudou SO em gate/doc (iter 2):** `D-2026-07-30-sonar-zero-issues-8` apendada; `Verify:` do
item 9 do CONTEXT.md trocado (mede parametros DECLARADOS, ancora + varredura char a char);
SUMMARY.md atualizado. Zero producao, zero teste.

**Numeros finais:** build 0 erros · 256 testes (254p/2s/0f) · 235 atributos vivos · cobertura das
linhas alteradas 68/68 = 100,0% (agregado Core 88,53%) · format 9 violacoes (todas legadas) ·
DoD 10/10 · SonarCloud main: gate OK.

**Placar das 113 issues:** 67 FIX · 41 REMOCAO · 2 EXCLUSAO · 3 WAIVER = 113 (cruzamento item a
item na secao (e); nenhuma silenciada fora da taxonomia D-...-3).

## Para o revisor humano do PR

O que o gate automatizado NAO prova — decisoes de 1 minuto:

1. **Remocao do `dotnet-install.ps1` (41 das 113):** script vendored da Microsoft, 1573 linhas,
   zero referencia em codigo/workflow (unica era uma permissao stale de tooling local, removida
   junto), re-obtenivel em dotnet.microsoft.com. Se alguem do time depende dele em maquina local,
   e so baixar de novo — confirme que ninguem o usa em script proprio fora do repo.
2. **`user-scalable=no` MANTIDO (waiver D-...-4):** chamada de PRODUTO, nao tecnica — paginacao
   exige viewport fixo; WCAG 1.4.4 atendido por tipografia configuravel; precedente
   Kindle/Apple Books. Se discordar do argumento de acessibilidade, o veto e aqui.
3. **Quality Gate real so existe DEPOIS do push:** hoje a branch nem tem analise (API da 404).
   Apos o CI: job `sonarqube` verde = mecanismo funcionou; se vermelho por `new_coverage`, as duas
   causas provaveis ja estao diagnosticadas em W-2/(g). Confirme tambem que `sonarqube` consta nos
   required checks da branch protection (vive nas Settings do GitHub, invisivel ao repo).
4. **WebView sem harness JS:** zoom, scroll-sync e overlay de traducao apos a migracao mecanica de
   `translation.js`/`scroll.js`/`bridge.js` so tem prova FUNCIONAL manual (abrir um EPUB e usar).
   O contrato textual JS<->C# esta preso por `HybridWebViewContractTests`, comportamento nao.
5. **Gate textual do item 9 e evadivel por construcao anomala** (W-1): comentario com `)`
   desbalanceado dentro da assinatura + violacao simultanea. O S107 real do SonarCloud cobre isso
   semanticamente em New Code; o PR review humano e o backstop declarado para codigo adversarial.

## Recommendation

APPROVED_WITH_WARNINGS — nenhum blocker; as 8 tasks entregues e provadas; o unico defeito da iter 1
(gate posicional do item 9) esta objetivamente fechado (m1/m11 do doer + matriz propria desta
review) sem regressao de gate e sem tocar producao. Encaminhar ao `/jdi-ship`; rotear a W-1
(endurecimento `//` no awk do item 9) e os itens (a)-(d) da W-3 para `.jdi/todos.md`. Os 3 itens de
`Deferred to PR review` do CONTEXT.md seguem com o revisor humano (secao acima).

## DoD Critic (enhanced — forcado por /jdi-issue)

NOTA DE EXECUCAO: rodado inline pelo orquestrador (o sub-agente critico foi terminado por limite de
sessao da API na iter 1 e o protocolo foi mantido: contra-exemplo executado em copia sob
`$CLAUDE_JOB_DIR/tmp`, repo real nunca mutado). Foco: a linha 9, unica que mudou desde a passagem
anterior (o reviewer confirmou hunk unico no CONTEXT.md); as outras 9 seguem com o julgamento da
iter 1, onde foram confirmadas por grep mais amplo que o proprio gate.

**Linha 9 (S107) — furo da iter 1 FECHADO, sem regressao e sem falso positivo:**

| Mutante | Resultado |
|---|---|
| repo real, sem mutacao | exit 0 (sem falso positivo) |
| 8 params na DECLARACAO de `TranslateChaptersWithCacheAsync` (contra-exemplo que derrubou a iter 1) | **exit 1 — pego** |
| 8 params na DECLARACAO de `TranslateSingleChapterAsync` | **exit 1 — pego** |
| clausula S3267 revertida (`chapter => chapter.HRef` -> `c => c.HRef`) | **exit 1 — pego** (sem regressao de gate) |
| assinatura legitima com generico `IReadOnlyDictionary<string, int>` + atributo `[CallerMemberName]` + default (7 params) | exit 0 — **sem falso positivo** |

O gate deixou de ser posicional: ancora em `^[[:space:]]*private async Task <Nome>(` (exige
exatamente 1 ocorrencia — call site nao casa) e conta separadores de parametro no nivel 1 de
parenteses, ignorando virgulas dentro de `<>`/`[]`/`{}`. A propriedade medida passou a ser
"parametros DECLARADOS", nao "virgulas de uma janela textual".

**Residuo confirmado (W-1 do reviewer, reproduzido de forma independente):** um comentario `//` com
`)` desbalanceado DENTRO da lista de parametros faz o contador fechar cedo; combinado com 8
parametros reais, sai exit 0. Julgamento: residuo, nao furo — nao ha caminho acidental (exige
escrever um comentario malformado dentro da assinatura no mesmo commit em que se adiciona 3
parametros), e e a mesma familia que a jurisprudencia desta base ja classificou como residuo na
phase `the-method-refactor` (evasoes que so morrem com parser C# de verdade: string literal, `#if`).
Fix barato existe (descartar tudo apos `//` antes de contar) e cabe na rodada de warnings.

Nenhuma outra linha `Type=Auto`/`PASS` mostrou-se oca nesta passagem.

**Verdict:** APPROVED
