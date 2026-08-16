# Phase 6: Review  (slug: llm-mobile, iter 3)

**Verdict:** APPROVED_WITH_WARNINGS

Revisado em 2026-08-16 por `jdi-reviewer-translatereader` (chain autonoma `/jdi-issue`, mode=verify,
iter=3). Diff sob revisao: `166b3da..29af388` (15 commits; 1 novo desde a iteracao 2: `29af388`,
que toca EXATAMENTE 1 linha de `.jdi/phases/llm-mobile/CONTEXT.md` — zero `.cs`, zero `.csproj`,
zero script, zero workflow). Todos os comandos abaixo foram EXECUTADOS nesta maquina nesta sessao;
nenhum numero e auto-reportado — o claim "10 PASS, 0 FAIL" do orquestrador foi re-verificado
comando a comando e CONFIRMADO de forma independente.

**B-2 esta RESOLVIDO.** O DoD 8 emendado exita 0 quando executado literalmente, e a emenda foi
julgada LEGITIMA, nao um afrouxamento (analise adversarial abaixo, com mutantes executados).

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | Windows Release `net10.0-windows10.0.19041.0`: 0W/0E. Android Release `net10.0-android`: 0W/0E (primeira classe por D-2026-08-16-llm-mobile-10). Android Debug (DoD 5): 0 Error(s) |
| Tests | PASS | 488 passed / 2 skipped / 0 failed; ZERO nome perdido (`comm -23` vazio), +33 nomes novos vs BASELINE `166b3da`; baselines 455 (phase) e 167 (D-2) respeitados |
| Coverage | PASS | AM scope (`scripts/coverage-gate.sh`) exit 0: `COVERAGE_SCOPE covered=1541 valid=1617 pct=95.30 files=34` (piso 90, D-6), `COVERAGE_JS covered=1906 valid=1920 pct=99.27 files=5` (piso 85), `COVERAGE_GUARD new_app_cs=1 waived=1`, zero waiver invalido |
| Lint | WARN | `dotnet format whitespace --verify-no-changes` exit 2: FINALNEWLINE em `Platforms/Android/MainActivity.cs` e `MainApplication.cs` — ambos FORA do diff da phase (legado, D-2) = WARN, identico as iters 1 e 2 |
| Security/Layer | PASS (com warns) | ZERO `.cs` tocado desde a iter 2 (`git diff --name-only 827b0d6..HEAD` = so CONTEXT.md); achados 5.x da iter 2 carregam sem mudanca; DoD 9 re-executou em HEAD os greps de camada, sync-over-async e static mutavel (HEAD=BASELINE) — todos limpos |
| Consistency | PASS (warn menor) | `29af388` conforme (Conventional Commits, escopo `llm-mobile`, tipo `chore` adequado, mensagem com trilha completa da emenda). T-8 `partial` no PLAN.md segue refletindo a realidade. Residuo W-7 (nota ausente em D-13) abaixo |
| UI Validation | SKIPPED | has_frontend=false (client MAUI nativo) |
| DoD | PASS | 10/10 auto PASS, 0 manual — cada `Verify:` extraido LITERALMENTE do CONTEXT.md pos-`29af388` e executado da raiz do repo |

## Julgamento da emenda do DoD 8 (`29af388`) — legitima, nao afrouxamento

O diff e cirurgico: exatamente os 3 sub-checks que o B-2 isolou, nada mais (verificado por diff
token a token do Verify antigo vs novo). Analise:

1. **Contagem de pins amarrada a `JOBS` e igual-ou-mais-forte que o literal antigo.** O antigo
   `-ge 4` era um PISO; o novo exige `checkout@SHA == JOBS` e `setup-dotnet@SHA == JOBS` — pareamento
   EXATO de um pin por job, nos dois mundos (3 ou 4 jobs). O check de tag flutuante
   (`@v*/main/master` = 0) permanece intacto e obrigatorio.
2. **Provado por execucao contra 3 mutantes de `ci.yml`** (copias no scratchpad, comando emendado
   apontado para cada uma):
   - Mutante A — `build-android` removido (a pergunta "e se eu tivesse removido mais um job?"):
     **exit 1** (`JOBS=2 < 3`, e o grep obrigatorio do comando android tambem reprova — protecao dupla).
   - Mutante B — um checkout despinado para `@v4`: **exit 1** (contagem 2 != JOBS=3 E tag flutuante
     detectada — protecao dupla).
   - Mutante C — `build-ios` ressuscitado malformado (ubuntu, sem pins, sem workload): **exit 1**
     (branch de presenca exige macos + workload + build iOS; branch de ausencia exige contagem zero).
3. **Ponto cego remanescente identico ao original**: um job novo FORA da lista fechada
   (`test|build|build-android|build-ios`) com action pinada em SHA DIFERENTE nao seria distinguido —
   mas o `-ge 4` antigo tinha exatamente a mesma cegueira (era piso, nao pareamento). Nada foi perdido;
   o pareamento exato e estritamente mais informativo.
4. **Grep do agent file re-apontado para literal que EXISTE e prova o mesmo**: `'iOS is never a
   local gate'` esta na linha 182 de `.jdi/agents/jdi-reviewer-translatereader.md` e carrega a mesma
   semantica do literal apagado por `c029cb3`. Os demais greps do agent (Android BLOCK, comando
   canonico, ausencia do texto WARN antigo) seguem obrigatorios e passando.
5. **A semantica de D-13 e honrada**: "actions pinadas por SHA, zero tag flutuante, tres jobs
   intactos" — tudo continua mecanicamente exigido nas duas saidas. A emenda operacionaliza D-13,
   nao a contorna.

**Conclusao: a correcao e real e a protecao original esta preservada (e num aspecto, reforcada).**

## Blockers

Nenhum.

## Warnings

Mapa consolidado (numeracao da iter 2 mantida):

- **W-1 (persiste) — Alinhamento dos `.so` = 4096 < 16384 (Google Play, Android 15+).** Re-medido
  NESTA sessao via DoD 5 completo (rebuild APK Debug + parsing ELF): 10/10 `.so` em align=4096,
  linhas SO_ALIGN verbatim em `docs/NATIVE-BACKENDS.md`. Conduta correta por D-2026-08-16-llm-mobile-4
  (medir e registrar). Bloqueia submissao futura na Play Store, nao o build. **Deve constar no PR.**
- **W-2 (persiste) — `ILlamaNativeAccess`: 1 contrato x 10 operacoes** vs ideal 3-5 (CLAUDE.md).
  Inalterado (zero `.cs` tocado — correto). O redesenho previsto em D-12 para fechar o gap do shim e
  o momento natural do split lifecycle/geracao. **Deve constar no PR.**
- **W-3 (persiste, ACEITO) — `int[]`/`string` no contrato nativo em vez de Span.** Trade-off exigido
  pela mockabilidade LOCKED de D-5; gatilho de reavaliacao registrado no SUMMARY. Mencao breve no PR.
- **W-4 (persiste) — `LlamaCppTranslationEngine.InitializeAsync` sem guarda de concorrencia**
  (csharp.md secao 3). Mitigado hoje pela ausencia de await antes do set de estado; enderecar se o
  engine for chamado de background threads. **Deve constar no PR.**
- **W-5 (persiste) — build/test "cru" no nivel da solucao falha nesta maquina** (CA1711 nos
  `AppDelegate.cs` legados quando TFMs sem workload compilam analyzers). Pre-existente ao baseline;
  comandos canonicos imunes. A phase futura que recriar o build iOS vai reencontra-lo. **Deve constar
  no PR** (aviso a quem for fechar o gap de D-12).
- **W-6 (persiste, menor) — `grep -qF 'build-ios'` como "confissao"** casa qualquer mencao; a forca
  real vem dos outros operandos (D-12 + matriz UNSUPPORTED + contagem zero). Historico apenas.
- **W-7 (novo, menor) — trilha da emenda so no commit.** A iter 2 pediu emenda "com trilha na
  decisao existente ou nota nela"; `29af388` traz mensagem de commit exemplar (causa raiz, forma
  nova, confirmacao de re-execucao), mas D-13 nao ganhou nota apontando a forma final dos
  sub-checks. Como a emenda IMPLEMENTA a semantica que D-13 ja declara, e residuo de processo, nao
  defeito. Nao precisa de nova iteracao.
- Lint: 2 FINALNEWLINE legadas fora do diff (gate 4) seguem WARN por D-2, inalteradas desde o bootstrap.
- Melhorias registradas (iter 2, sem mudanca): DoD 9 compara contagem-vs-contagem (forma nominal
  seria imune a par remove+adiciona); exclusao do DoD 7 por diretorio (sufixo real seria mais justo).

Itens de `## Deferred to PR review` do CONTEXT.md (build iOS verde, inferencia real em device,
tokens/s medidos, issue #1224, 16 KB pela ferramenta do Play, aceitacao nas lojas, SonarCloud) nao
sao pendencia desta review — o chain os expoe no corpo do PR, como previsto.

## Regressoes entre 166b3da e HEAD

Nenhuma. Desde a iter 2 a unica mudanca e 1 linha de CONTEXT.md (`29af388`). Suite, builds e
cobertura re-medidos IDENTICOS a iter 2: 488/2/0 com zero nome perdido, 0W/0E nos dois TFMs,
95.30 C# / 99.27 JS. Baseline D-2 (167 testes) e baseline da phase (455) respeitados. Working tree:
`LOOP.md` (estado do loop do orquestrador) e `.claude/settings.local.json` modificados, ambos fora
do produto.

## DoD Checklist (gate 8)

Todos os `Verify:` extraidos literalmente do CONTEXT.md pos-`29af388` e executados nesta sessao.

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | Nada quebrou: suite verde, nenhum teste perdido nome a nome, Windows Release intacto | CONTEXT | Auto | PASS | exit 0 — 488/2/0, `comm -23` vazio, +33 nomes novos, llm-win.log 0 Warning(s)/0 Error(s) |
| 2 | Config nativa e dado puro por plataforma; Windows byte-identico; 4 plataformas testadas | CONTEXT | Auto | PASS | exit 0 — zero literal Windows no engine, 4 testes prescritos presentes e passando |
| 3 | Modelo Apache-2.0 default de instalacao nova; licencas documentadas; settings legado intacto | CONTEXT | Auto | PASS | exit 0 — rede REAL: content-length 1133080448 == SizeBytes; fallback `: GemmaModel;` intacto; 3 testes passam |
| 4 | Android Release compila com backend oficial na Condition certa, minSdk 23, 0E e 0W | CONTEXT | Auto | PASS | exit 0 — Conditions por posicao de ItemGroup, 21.0 ausente, build 0 Warning(s)/0 Error(s) |
| 5 | libllama.so arm64-v8a no APK; alinhamento de CADA .so medido e registrado sem divergencia | CONTEXT | Auto | PASS | exit 0 — APK Debug rebuildado AGORA, SO_FOUND lib/arm64-v8a/libllama.so, SO_COUNT 10, 10 SO_ALIGN (todas align=4096) verbatim no doc |
| 6 | Plataforma sem backend e memoria insuficiente recusam com erro tratado; matriz escrita | CONTEXT | Auto | PASS | exit 0 — 3 testes de disponibilidade passam; matriz com windows/android SUPPORTED, ios UNSUPPORTED, maccatalyst UNSUPPORTED |
| 7 | Referencia nativa iOS nao e no-op; cadeia de suprimento fail-closed PROVADA | CONTEXT | Auto | PASS | exit 0 — checksum provado por execucao nos 2 sentidos (CHECKSUM_OK + rejeicao de hash errado); zero P/Invoke no Core; gap de simbolos declarado (D-12), nao mascarado |
| 8 | Job de CI iOS bem formado OU ausencia confessada (D-13); jobs intactos com pins pareados; Gate 1 do reviewer corrigido | CONTEXT | Auto | PASS | **exit 0 — B-2 resolvido por `29af388`.** Branch da ausencia confessada passa (job ausente + D-12 + mencao no ci.yml + matriz UNSUPPORTED); JOBS=3, checkout@SHA=3, setup-dotnet@SHA=3, zero tag flutuante; emenda validada contra 3 mutantes adversariais (todos reprovam) |
| 9 | The Method preservado; fora-de-escopo byte a byte igual; nenhum static mutavel novo | CONTEXT | Auto | PASS | exit 0 — HEAD=BASELINE em statics mutaveis; camadas limpas; 12 arquivos fora de escopo byte-identicos; zero sync-over-async |
| 10 | Gate de cobertura verde com waiver disciplinado | CONTEXT | Auto | PASS | exit 0 — 95.30 C# / 99.27 JS, files=5, guard 1/1, waiver unico valido citando D-2026-08-16-llm-mobile-5 e apontando arquivo existente |

**Totals:** 10 items | Auto: 10 (10 PASS, 0 FAIL) | Manual: 0 pending

`.jdi/PROJECT.md` nao contem secao Definition of Done (re-confirmado); os 10 itens vem de
CONTEXT.md (`dod=auto_only`). Nenhuma confirmacao manual pendente.

## Recommendation

Aprovar e seguir para o ship/PR. O corpo do PR deve conter:

1. Os itens de `## Deferred to PR review` do CONTEXT.md, na integra, como limitacoes declaradas —
   em especial: build iOS nunca provado verde (job removido por D-12, matriz `ios UNSUPPORTED`),
   nenhum numero de tokens/s medido (estimativas nao sao resultados), e alinhamento 16 KB medido
   apenas pelo proxy ELF.
2. Warnings W-1 (align 4096 vs Play Store), W-2 (contrato de 10 ops, split previsto no fechamento
   de D-12), W-4 (guarda de concorrencia do InitializeAsync) e W-5 (CA1711 latente que o build iOS
   futuro reencontra).
3. O escopo final honesto: Bloco 1 completo e provado; T-7 completo; T-8 parcial com o gap do shim
   C registrado em D-12 e aceito pelo DoD 8 via D-13.

Nada resta para doer ou orquestrador nesta phase. As tres iteracoes convergiram exatamente como o
processo desenha: iter 1 pegou um vermelho deterministico, iter 2 pegou uma emenda nao re-executada,
iter 3 confirma que a correcao minima foi feita, re-executada e resiste a mutacao adversarial.
