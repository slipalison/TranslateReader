# Phase 15: Cobertura de 90% no SonarQube sem issues novas — Summary  (slug: coverage-90)

**Status:** complete
**Tasks:** 8/8 completas, 0 bloqueadas
**Branch:** `jdi/coverage-90` (base `main` @ `1af3a51`) · **Arquivos em `src/` tocados: 0**

## Tasks executadas (1 task = 1 commit)

| Task | Commit | Resultado |
|---|---|---|
| T-1 | `950cb5b` `test(coverage-90): add JS harness and paginated.js coverage` | harness `node:vm` + DOM falso, sem dependencia nova; `paginated.js` 105/105 linhas |
| T-2 | `f90ba32` `test(coverage-90): cover FileUtility and HtmlUtility to 100%` | os dois arquivos com `line-rate=1` **e** `branch-rate=1` |
| T-3 | `9e8aa48` `test(coverage-90): close the uncovered SettingsAccess fallbacks` | 12 condicoes fechadas; `branch-rate` 0,607 -> 1 |
| T-4 | `7c6111a` `test(coverage-90): cover bridge.js host detection and chunk buffering` | `bridge.js` 95/95 linhas, 35/35 branches |
| T-5 | `86545b3` `test(coverage-90): cover ModelAccess.DownloadModelAsync end to end` | `ModelAccess.cs` 0,39 -> `line-rate=1` / `branch-rate=1`, sem rede |
| T-6 | `be06291` `test(coverage-90): cover scroll.js and translation.js` | 4 scripts a 287/287 linhas e 98/98 branches |
| T-7 | `ce61ac3` `test(coverage-90): cover the ParsingEngine edge cases the real fixtures miss` | 87 -> 6 unidades descobertas (**+81**, alvo era >=55) |
| T-8 | `59e2130` `ci(coverage-90): feed the WebView lcov report to SonarQube` | `setup-node` SHA-pinned + step lcov + `lcov.reportPaths` + `sonar.exclusions` |

## Gates (numeros reais)

- `dotnet build TranslateReader.slnx -c Release` -> **0 Erro(s)**, 64 Aviso(s) (todos pre-existentes, `MVVMTK0045`).
- `dotnet test ... -c Release` -> **304 testes: 302 aprovados, 2 ignorados, 0 falhas** (baseline 256 = 254p/2s; +48 testes, nenhum existente removido ou afrouxado).
- `node --test test/js/` -> **60 testes, 60 pass, 0 fail**.
- `dotnet format --verify-no-changes` em cada arquivo novo/alterado -> exit 0.

## DoD — `Verify:` rodados LITERALMENTE do CONTEXT.md

| # | Item | exit |
|---|---|---|
| 1 | harness dos 4 scripts existe e `node --test test/js/` passa | **0** |
| 2 | agregado lcov dos 4 JS >= 85% -> medido **287/287 = 100,00%** | **0** |
| 3 | `ModelAccess.cs` >= 90% -> medido `line-rate=1` | **0** |
| 4 | `FileUtility.cs` e `HtmlUtility.cs` = 100% -> ambos `line-rate=1` | **0** |
| 5 | wiring de CI (setup-node + lcov + reportPaths) | **0** |

## Aritmetica final (formato Sonar: linhas + condicoes)

Modelo local validado contra o baseline remoto: `main` local = **1336/1760 = 75,91%** vs
SonarCloud **1339/1764 = 75,9%** (delta de 4 condicoes, 0,01 pp) — o proxy reproduz a metrica.

| Alvo | descoberto no baseline | descoberto agora | fechado |
|---|---|---|---|
| 4 JS do WebView | 195 (linhas) | 0 | **+195** |
| `ParsingEngine.cs` | 87 | 6 | **+81** |
| `ModelAccess.cs` | 39 | 0 | **+39** |
| `SettingsAccess.cs` | 12 | 0 | **+12** |
| `HtmlUtility.cs` | 7 | 0 | **+7** |
| `FileUtility.cs` | 3 | 0 | **+3** |
| `TranslationEngine.cs` | 67 | 67 | 0 (deferido, D-...-4) |
| resto (models, ThemeEngine, Managers, Access) | 10 | 10 | 0 |

- `sum(BRF)` do lcov = **98**, `sum(BRH)` = **98** -> entram nos dois lados.
- `D_final = 1760 + 98 = 1858` · `N_final = 1336 + 337 + 98 = 1771`.
- **Projecao: 1771/1858 = 95,32%.** Piso de 90% exige 1673 -> **margem +98 unidades**.

Sensibilidade: mesmo se o Sonar ignorasse por completo o `BRF/BRH` do JS, daria
1673/1760 = **95,06%**. O cenario em que 90% NAO fecha e apenas o de o lcov do JS nao ser
consumido (83,98%) — por isso T-8 e load-bearing, nao cosmetico.

## Desvios do PLAN (com justificativa)

1. **Arquivo extra `test/js/index.js`** (T-1 previa so `harness.js` + `paginated.test.js`).
   Node >= 24 trata todo argumento posicional de `--test` como **glob**, nao como diretorio —
   confirmado na fonte embutida (`internal/test_runner/runner.js`: `createTestFileList` monta um
   `Glob` com os patterns, sem expansao de diretorio). Resultado: `node --test test/js/` casava o
   proprio diretorio e tentava executa-lo como modulo (`Cannot find module '...\test\js'`), com
   **0 testes rodados** — os `Verify:` de DoD 1 e 2 estavam quebrados contra o runtime pinado. O
   agregador restaura o comando LITERAL do CONTEXT e descobre `*.test.js` dinamicamente, entao
   nenhum arquivo de teste pode ser silenciosamente deixado de fora (learning #3 da phase anterior).
2. **`mkdir -p TestResults`** antes do comando exato de lcov no CI: o reporter do node **nao** cria
   o diretorio de destino (verificado; sem ele o step aborta e o lcov nunca existe).
3. **`sonar.exclusions`** de `test/js/**` somado ao `lcov.reportPaths` (previsto no PLAN; registrado
   aqui por explicitar o porque no proprio YAML). Nao contradiz D-...-1: o excluido e o codigo de
   TESTE, nunca o JS de producao.
4. **Alvos superados**: `ParsingEngine` fechou 81 (meta >=55) e `ModelAccess`/`SettingsAccess`/
   `HtmlUtility`/`FileUtility` chegaram a 100% linha **e** branch (meta era 90-100% so de linha).
5. `.jdi/DECISIONS.md` **nao foi tocado** — nenhuma decisao nova foi necessaria (`git diff` vazio).

## Achados (nao corrigidos — `src/` e off-limits nesta phase)

- `ReadEpubSafeAsync`: quando a leitura estrita lanca `EpubPackageException`, o VersOne.Epub deixa o
  handle do zip **aberto**; o arquivo segue travado depois do fallback. Visivel so porque o teste
  apaga o temp no `Dispose` (limpeza best-effort com `catch (IOException)` comentado). Candidato a
  work item futuro.
- `ExtractCoverImageAsync` devolve `byte[0]`, nao `null`, quando o item de manifesto
  `properties="cover-image"` aponta para arquivo ausente do zip — `IgnoreMissingFileError` cria um
  placeholder vazio. Comportamento fixado por teste, producao inalterada.
- As 6 unidades residuais de `ParsingEngine` (1 linha + 5 condicoes) exigem um item de manifesto
  **sem `media-type`**, que `SkipInvalidManifestItems=true` remove antes do engine ver, e um
  `Content.Cover` nao-vazio com `CoverImage` vazio (os dois vem do mesmo leitor). Praticamente
  inalcancaveis pela API publica.

## O que ficou de fora

- `TranslationEngine.cs` (67 unidades) — **locked-deferido** por D-2026-07-31-coverage-90-4. Nao
  reaberto; so o humano pode reabrir. Nao foi preciso: a margem e +98 sem ele.
- ~10 unidades espalhadas (`Models/*`, `ThemeEngine`, `TranslationManager`, 4 `Access`) — fora do
  plano, sem impacto na meta.
- Confirmacao remota (Deferred to PR review, inalterado): cobertura Overall >= 90% no SonarCloud e
  "zero issues novas" so existem pos-push; nenhum analisador do Sonar roda local (D-...-6/-7).
