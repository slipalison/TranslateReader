# Phase 15: Cobertura de 90% no SonarQube sem issues novas — Summary  (slug: coverage-90)

**Status:** complete · **Tasks:** 8/8, 0 bloqueadas · **Branch:** `jdi/coverage-90` (base `main` @
`1af3a51`) · **Arquivos de `src/` tocados: 0** (em todas as iters)

## Iter 1 — as 8 tasks (1 task = 1 commit)

| Task | Commit | Resultado |
|---|---|---|
| T-1 | `950cb5b` | harness `node:vm` + DOM falso, zero dependencia nova; `paginated.js` 105/105 L |
| T-2 | `f90ba32` | `FileUtility.cs` e `HtmlUtility.cs` com `line-rate=1` **e** `branch-rate=1` |
| T-3 | `9e8aa48` | `SettingsAccess`: 12 condicoes fechadas, `branch-rate` 0,607 -> 1 |
| T-4 | `7c6111a` | `bridge.js` 95/95 L, 35/35 BR |
| T-5 | `86545b3` | `ModelAccess.cs` 0,39 -> `line-rate=1`/`branch-rate=1`, sem rede (handler fake) |
| T-6 | `be06291` | `scroll.js`+`translation.js`; os 4 scripts a 287/287 L e 98/98 BR |
| T-7 | `ce61ac3` | `ParsingEngine`: 87 -> 6 unidades descobertas (**+81**, alvo era >=55) |
| T-8 | `59e2130` | `setup-node` SHA-pinned + step lcov + `lcov.reportPaths` + `sonar.exclusions` |

**Aritmetica (Sonar: linhas + condicoes).** Modelo validado contra o remoto: `main` local =
**1336/1760 = 75,91%** vs SonarCloud **1339/1764 = 75,90%** (delta 0,01pp — o proxy reproduz a
metrica). Fechado: JS +195, `ParsingEngine` +81, `ModelAccess` +39, `SettingsAccess` +12,
`HtmlUtility` +7, `FileUtility` +3. Ficam descobertos `TranslationEngine` 67 (deferido, D-...-4) e
~10 unidades espalhadas. `sum(BRF)` = `sum(BRH)` = **98** (entra nos dois lados).
`D_final = 1760+98 = 1858` · `N_final = 1336+337+98 = 1771` -> **projecao 1771/1858 = 95,32%**
(piso de 90% = 1673 -> **margem +98 unidades**); o reviewer refez a conta de forma independente,
**divergencia 0,00pp**. Sensibilidade: sem `BRF/BRH` do JS, 95,06%. O UNICO cenario que reprova e
o lcov do JS nao ser consumido (83,98%) — por isso T-8 e load-bearing.

**Desvios do PLAN.** (1) Arquivo extra `test/js/index.js`: Node >= 24 trata o argumento posicional
de `--test` como **glob**, nao diretorio, entao `node --test test/js/` tentava executar o diretorio
como modulo com **0 testes rodados**; o agregador restaura o comando LITERAL do CONTEXT e descobre
`*.test.js` dinamicamente, entao nenhum arquivo pode ser omitido em silencio. (2) `mkdir -p
TestResults` antes do lcov no CI — o reporter do node nao cria o destino. (3)
`sonar.exclusions=test/js/**`: exclui TESTE, nunca o JS de producao (nao contradiz D-...-1).
(4) Alvos superados (T-7 fechou 81 com meta >=55; 4 arquivos a 100% linha **e** branch).

**Qualidade provada por mutacao (reviewer, iter 1):** 6 mutacoes em PRODUCAO, 6 mortas, 16 falhas
(`paginated.js` `_applyLayout`; `bridge.js` `flushChunk`; `ModelAccess.cs:50`;
`SettingsAccess.cs:49`; `HtmlUtility.cs:102`; `ParsingEngine.cs:277`). Cobertura do JS atribuida
aos arquivos de producao: os 4 `SF:` do lcov resolvem em `src/.../wwwroot/js/*.js` e o harness
carrega a fonte real via `vm.Script(code, {filename})`, nao copia inline.

**Achados de producao (nao corrigidos — `src/` fechado por escopo):** handle de zip aberto em
`ReadEpubSafeAsync` e `byte[0]` em vez de `null` em `FindCoverInManifest`. **Registrados em
`.jdi/todos.md` § `De coverage-90` na iter 2** (Warning 1 da REVIEW), com file:line e evidencia.
As 6 unidades residuais de `ParsingEngine` exigem estado que `SkipInvalidManifestItems=true` remove
antes do engine ver — praticamente inalcancavel pela API publica.

## Iter 2 — fix dos 3 gates ocos

O DoD critic derrubou a iter 1 sem questionar o TRABALHO: o que nao prestava era a **PROVA**. Tres
`Verify:` ocos e dois com residuo, todos da mesma familia — **o gate lia artefato de medicao ja em
disco em vez de exigir que a medicao DESTA execucao tivesse sucesso**. So `.jdi/` mudou nesta iter.

**Os dois defeitos, reproduzidos por medicao propria:**
- `;` entre runner e leitor **descarta o exit code**. Com o `node` fora do `PATH` (regressao
  plausivel: o step `actions/setup-node` nasceu na T-8) e um `js-lcov.info` valido de 5399 bytes em
  disco, o item 2 ANTIGO saiu **exit 0** sem rodar 1 teste. Invertendo `FileUtilityTests.cs:81`
  (`".epub"` -> `".MUTANT"`), com a suite REPROVANDO, os itens 3/4 ANTIGOS sairam **exit 0**.
- `find ... | sort | tail -1` ordena **GUIDs do VSTest lexicograficamente**, sem relacao com tempo.
  Medido com 4 relatorios em disco: escolheu `9a248056-...` (mtime 07:49:59) enquanto o mais
  recente era `3e886ce2-...` (07:53:32).

**Mecanismo** (D-2026-07-31-coverage-90-8, append-only; CONTEXT.md editado SO DEPOIS): destino
LIMPO por execucao + `&&` do runner ate a assercao. Itens 3/4 escrevem em `TestResults/dod3`/`dod4`
(`rm -rf` antes) e exigem **exatamente 1** relatorio — com diretorio limpo e 1 projeto de teste a
selecao deixa de ser heuristica e passa a ser provada. Item 2 apaga o lcov com `rm -f` e exige
`test -s` depois, **mantendo o caminho que o CI usa** para o item 5 continuar aferindo a mesma
string. Selecao por mtime rejeitada: `find -printf "%T@"` nao existe em todo ambiente.

**Endurecimentos** (todos com contra-exemplo executado): item 2 exige os **4** arquivos de producao
como records distintos no lcov (`seen[f]`/`n==4`); itens 3/4 comparam `line-rate` como NUMERO por
classe (o antigo montava um `R` MULTILINHA — `ModelAccess` tem 2 classes, `FileUtility` 3 — e o
`awk` degradava para comparacao de STRING); item 1 exige `# pass > 1` (com os 4 `.test.js`
esvaziados o Node ainda reporta `# pass 1`, contando o proprio arquivo, entao `> 0` seria vacuo);
item 5 EXTRAI `reportPaths=` e `--test-reporter-destination=` do YAML e exige igualdade, mais
`setup-node` SHA-pinned. **Nenhum piso mudou** (85% / 0,90 / 0,99).

**Matriz de mutacao — comandos extraidos LITERALMENTE do CONTEXT.md vigente:**

| Cenario | ANTIGO | NOVO |
|---|---|---|
| repo real, sem mutacao (5 itens) | 0 | **0** (zero falso positivo) |
| `node` fora do `PATH` + lcov valido em disco | item 2 = **0** | item 2 = **127** |
| suite JS inteira esvaziada | item 1 = **0** | itens 1 e 2 = **1** |
| so `scroll.test.js` esvaziado | item 2 = **0** | item 2 = **1** (`n==4` falha) |
| `throw` em `paginated.test.js` | item 2 = 1 (acidente, ver nota) | itens 1 e 2 = **1** |
| assercao C# invertida (suite reprovando) | itens 3/4 = **0** | itens 3/4 = **1** |
| 11 relatorios velhos em disco + run atual falhando | — | itens 3/4 = **1** |
| `reportPaths` divergindo do destino do reporter | item 5 = **0** | item 5 = **1** |
| pisos elevados: JS 101%; item 3 -> `TranslationEngine` (0,21/0,4/0,2); item 4 1,01 | — | **1, 1, 1** |

*Nota de honestidade:* o contra-exemplo LITERAL do critico para o item 2 (`throw` num `.test.js`)
nao reproduz identico neste runtime — o reporter lcov do Node trunca o destino para um stub de 4
bytes (`TN:`), entao o comando antigo falhava por **acidente**, nao por design. O defeito
estrutural e real e provado pelos dois casos acima.

**Nao endurecido de proposito:** o ratchet NUMERICO de contagem de teste JS (`# pass >= 60`) — o
item `[PROCESSO/DoD]` de `.jdi/todos.md` ja decidiu que piso de contagem se ergue na VIRADA da
phase, com o numero publicado. O piso `> 1` adotado nao e ratchet: so nega a suite vazia.

## Gates finais (numeros reais, iter 2)

- `dotnet build TranslateReader.slnx -c Release` -> **0 Erro(s)**, 64 Aviso(s) (todos
  `MVVMTK0045` pre-existentes, `src/` intocado).
- `dotnet test TranslateReader.slnx -c Release` -> **304 testes: 302 aprovados, 2 ignorados, 0
  falhas** (baseline 256 = 254p/2s; nada removido ou afrouxado).
- `node --test test/js/` -> **60 testes, 60 pass, 0 fail**.
- 5 `Verify:` extraidos LITERALMENTE do CONTEXT.md vigente -> **exit 0, 0, 0, 0, 0**. Medido:
  JS **287/287 = 100,00%** sobre os 4 arquivos; `ModelAccess` line-rate `1 1`;
  `FileUtility`/`HtmlUtility` line-rate `1 1 1 1`.
- `git status --porcelain` limpo apos cada mutacao — o repo real nunca ficou mutado.

## O que ficou de fora

- `TranslationEngine.cs` (67 unidades) — **locked-deferido** por D-...-4; a margem e +98 sem ele.
- Confirmacao remota (`## Deferred to PR review`, inalterado): cobertura Overall >= 90% no
  SonarCloud e "zero issues novas" so existem pos-push (D-...-6/-7).
