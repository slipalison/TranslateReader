# Phase 15: Cobertura de 90% no SonarQube sem issues novas — Summary  (slug: coverage-90)

**Status:** complete · **Tasks:** 8/8, 0 bloqueadas · **Branch:** `jdi/coverage-90` (base `main`
@ `1af3a51`) · **Linhas de `src/` tocadas: 0** em TODAS as iters.

## Iter 1 — as 8 tasks (1 task = 1 commit)

T-1 `950cb5b` harness `node:vm`+DOM falso, zero dep nova, `paginated.js` 105/105 L · T-2 `f90ba32`
`FileUtility`/`HtmlUtility` line+branch-rate = 1 · T-3 `9e8aa48` `SettingsAccess` branch-rate
0,607 -> 1 · T-4 `7c6111a` `bridge.js` 95/95 L, 35/35 BR · T-5 `86545b3` `ModelAccess.cs` 0,39 ->
line/branch-rate 1, sem rede (handler fake) · T-6 `be06291` `scroll.js`+`translation.js`, os 4
scripts 287/287 L e 98/98 BR · T-7 `ce61ac3` `ParsingEngine` 87 -> 6 unidades descobertas
(**+81**, alvo >=55) · T-8 `59e2130` `setup-node` SHA-pinned + step lcov + `lcov.reportPaths` +
`sonar.exclusions`.

**Aritmetica (Sonar: linhas+condicoes).** Proxy validado contra o remoto: `main` local
1336/1760 = 75,91% vs SonarCloud 1339/1764 = 75,90% (delta 0,01pp). Fechado: JS +195,
`ParsingEngine` +81, `ModelAccess` +39, `SettingsAccess` +12, `HtmlUtility` +7, `FileUtility` +3;
`sum(BRF)=sum(BRH)=98` (nos dois lados). `1771/1858` -> **95,32%**, piso 90% = 1673 ->
**margem +98**; reviewer refez a conta, divergencia 0,00pp. Sem BRF/BRH do JS: 95,06%. UNICO
cenario que reprova: lcov do JS nao consumido (83,98%) — por isso T-8 e load-bearing.

**Desvios do PLAN.** (1) `test/js/index.js`: Node >= 24 trata o positional de `--test` como
**glob** (rodava 0 testes); o agregador preserva o comando LITERAL do CONTEXT e descobre
`*.test.js` dinamicamente. (2) `mkdir -p TestResults` antes do lcov no CI.
(3) `sonar.exclusions=test/js/**` exclui TESTE, nunca producao. (4) T-7 fechou 81 com meta >=55.

**Mutacao (reviewer):** 6 mutacoes de PRODUCAO, 6 MORTAS. Cobertura JS atribuida a producao — os 4
`SF:` resolvem em `src/.../wwwroot/js/*.js` via `vm.Script(code,{filename})`.
**Achados de producao** (nao corrigidos, `src/` fechado por escopo): zip handle aberto em
`ReadEpubSafeAsync`; `byte[0]` em vez de `null` em `FindCoverInManifest` — em `.jdi/todos.md`
§ `De coverage-90` com file:line.

## Iter 2 — fix dos 3 gates ocos (so `.jdi/` mudou)

O DoD critic nao questionou o TRABALHO: o que nao prestava era a **PROVA** — **o gate lia artefato
de medicao ja em disco em vez de exigir sucesso da medicao DESTA execucao**. Dois defeitos,
reproduzidos por medicao propria: (i) `;` entre runner e leitor **descarta o exit code** — com
`node` fora do `PATH` e um `js-lcov.info` valido de 5399 bytes em disco o item 2 ANTIGO saiu
**exit 0** sem rodar 1 teste, e com `FileUtilityTests.cs:81` invertido (suite REPROVANDO) os itens
3/4 ANTIGOS sairam **exit 0**; (ii) `find|sort|tail -1` ordena **GUIDs do VSTest
lexicograficamente** — escolheu mtime 07:49:59 tendo 07:53:32 em disco.

**Mecanismo** (D-2026-07-31-coverage-90-8, append-only; CONTEXT.md editado SO DEPOIS): destino
LIMPO por execucao + `&&` do runner ate a assercao. Itens 3/4 em `TestResults/dod3`/`dod4`
(`rm -rf` antes), exigindo **exatamente 1** relatorio; item 2 apaga o lcov (`rm -f`) e exige
`test -s`, **mantendo o caminho do CI**. **Endurecimentos:** item 2 exige os **4** arquivos de
producao distintos (`seen[f]`/`n==4`); itens 3/4 comparam `line-rate` como NUMERO **por classe**
(o antigo montava `R` MULTILINHA e o `awk` degradava para comparacao de STRING); item 1 exige
`# pass > 1` (suite vazia ainda reporta `# pass 1`); item 5 extrai
`reportPaths=`/`--test-reporter-destination=` do YAML e exige igualdade + `setup-node` SHA-pinned.
**Nenhum piso mudou** (85% / 0,90 / 0,99).

| Cenario | ANTIGO | NOVO |
|---|---|---|
| repo real, 5 itens, 2 rodadas | 0 | **0** (zero falso positivo) |
| `node` fora do `PATH` + lcov valido em disco | item 2 = **0** | item 2 = **127** |
| suite JS esvaziada; so `scroll.test.js` esvaziado | itens 1/2 = **0** | **1** |
| assercao C# invertida (suite reprovando) | itens 3/4 = **0** | itens 3/4 = **1** |
| `reportPaths` divergindo do destino do reporter | item 5 = **0** | item 5 = **1** |
| pisos elevados (JS 101%; item 3 -> `TranslationEngine`; item 4 1,01) | — | **1, 1, 1** |

*Honestidade:* o contra-exemplo LITERAL do critico (`throw` num `.test.js`) nao reproduz identico —
o reporter lcov trunca o destino para um stub de 4 bytes (`TN:`), entao o comando antigo falhava
por **acidente**; o defeito estrutural segue provado pelos dois casos acima. *Nao endurecido de
proposito:* o ratchet NUMERICO `# pass >= 60` — `[PROCESSO/DoD]` de `.jdi/todos.md` ja decidiu que
piso de contagem sobe na VIRADA da phase.

## Iter 3 — rodada de warnings (/jdi-issue)

Loop ja convergido (APPROVED_WITH_WARNINGS, iter 2); rodada so dos 3 warnings da REVIEW. 3 commits,
**`src/` intocado** (`git diff -- src/` vazio).

**W-1 (mutante sobrevivente) — FECHADO** (`ad9c30c`). `ModelAccessTests.cs:146-160` assertava teto,
monotonicidade e o `1.0` final, mas **nenhum piso de contagem**: o mutante `||` -> `&&` em
`ModelAccess.cs:41` suprime todo report intermediario e sobrevivia 15/15.
`Assert.True(reports.Count < 334, ...)` -> `Assert.InRange(reports.Count, 100, 333)` — o teto e
LITERALMENTE o mesmo (`< 334` == `<= 333`) e o piso e novo, entao a assercao so ficou mais forte.
**Prova por mutacao nesta iter** (`&&` aplicado em `ModelAccess.cs:41`): teste ANTIGO +
mutante = **15/15 aprovado** (sobrevivencia reproduzida); teste NOVO + mutante = **1 falha**,
`Assert.InRange() Failure ... Range: (100 - 333) / Actual: 1` — o `Actual: 1` e exatamente o
"so o report final" que o mutante produz; teste NOVO + producao intacta = **15/15 aprovado**.
Producao restaurada; `git status --porcelain` limpo depois.

**W-2 (lint legado) — NAO FECHADO, por regra.** 9 erros WHITESPACE do `dotnet format` em 4 arquivos
legados (`ThemeEngine.cs`, `ReaderPage.xaml.cs`, `ThemeEngineTests.cs`, `TranslationManagerTests.cs`),
nenhum no diff desta phase. **D-2** isenta codigo pre-`4285f25` e o specialist proibe
refatorar legado por estilo; o conserto pertence a `baseline-de-estilo`, que shipa o `.editorconfig`
e vira o gate para BLOCK-on-new. Confirmado e seguido.

**W-3 (residuo do item 5) — FECHADO** (`a24f084` + `e3c01dc`): custo de um `test`, zero falso
positivo. O item 5 provava `P = D` (Sonar le == reporter escreve), fechando a divergencia
UNILATERAL, mas nao pinava o literal — rename COORDENADO dos dois lados mantinha o gate verde
enquanto o item 2 media o caminho hardcoded. Inserido
`&& test "$P" = "TestResults/js-lcov.info"` logo apos `&& test "$P" = "$D"`: puramente ADITIVO,
todo check anterior permanece literal.

| Cenario | D-...-8 | D-...-9 |
|---|---|---|
| repo real, 2 rodadas | 0 | **0** |
| rename COORDENADO dos 2 lados (`sonarqube.yml:107` e `:137`) | **0** (o furo) | **1** |
| divergencia unilateral (so `:107`) | 1 | **1** |
| `setup-node` por tag em vez de SHA | 1 | **1** |

Ordem JDI-legal: `a24f084` registra **D-2026-07-31-coverage-90-9** (append-only conferido: **0**
remocoes / 25 insercoes em `.jdi/DECISIONS.md`) ANTES de `e3c01dc` editar o CONTEXT.md, onde so as
linhas `**Verify:**`/`**Source:**` do item 5 mudaram — **zero** linha de criterio `- [ ]` e **zero**
piso tocados.

## Gates finais (iter 3, numeros reais)

- `dotnet build TranslateReader.slnx -c Release` -> **0 Erro(s)**, 64 avisos (`MVVMTK0045` legados).
- `dotnet test TranslateReader.slnx -c Release` -> **304 testes: 302 aprovados, 2 ignorados, 0
  falhas** — baseline preservado, nada afrouxado.
- `node --test test/js/` -> **60 testes, 60 pass, 0 fail**.
- 5 `Verify:` extraidos LITERALMENTE do CONTEXT.md vigente, 2 rodadas -> **0 0 0 0 0** / **0 0 0 0
  0**. Medido: JS **287/287 = 100,00%** em 4 arquivos; `ModelAccess` line-rate `1 1`.
- `dotnet format --verify-no-changes` no arquivo alterado: **exit 0**; `git status --porcelain`
  limpo apos os gates e apos cada mutacao.

## O que ficou de fora

- `TranslationEngine.cs` (67 unidades) — **locked-deferido** por D-...-4; a margem e +98 sem ele.
- Confirmacao remota (`## Deferred to PR review`, inalterado): Overall >= 90% e "zero issues novas"
  so existem pos-push (D-...-6/-7).
