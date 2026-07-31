# Phase 14: Zerar as issues do SonarQube e travar a regressao — Context (slug: sonar-zero-issues)

## Goal
Resolver as 113 issues abertas do SonarQube em `main` e instalar mecanismo anti-recorrencia —
cada issue termina em fix, exclusao ou waiver auditavel, nunca silenciada sem registro.

## Locked decisions
- D-2026-07-30-sonar-zero-issues-0: registro da phase, baseline 113 issues (2 bugs, 7 vuln, 104 smells).
- D-2026-07-30-sonar-zero-issues-1: `dotnet-install.ps1` REMOVIDO (41 issues, vendored MS, sem
  referencia real, redownloadable) — nao exclusao via `sonar.exclusions`, nao fix de terceiro.
- D-2026-07-30-sonar-zero-issues-2: mecanismo anti-recorrencia = `sonar.qualitygate.wait=true`
  em `dotnet-sonarscanner end` (`sonarqube.yml`) — falha o job obrigatorio do pipeline se o
  Quality Gate (New Code) reprovar. Sem overlap com `baseline-de-estilo` (local/generico vs
  CI/SonarQube-especifico).
- D-2026-07-30-sonar-zero-issues-3: taxonomia — toda issue termina em (a) FIX, (b) EXCLUSAO via
  `sonar.issue.ignore.multicriteria` (rule+resourceKey, versionado em `sonarqube.yml`), ou
  (c) WAIVER via `#pragma warning disable <ID>`/`restore` com comentario citando esta decisao.
- D-2026-07-30-sonar-zero-issues-4: `user-scalable=no` MANTIDO (`index.html:5`) — waiver
  argumentado (paginacao exige viewport fixo; tipografia configuravel ja cobre WCAG 1.4.4);
  suprimido via multicriteria (`Web:S7926`). Mesmo mecanismo suprime `css:S4667` (style vazio
  por design, populado em runtime pelo ThemeEngine).
- D-2026-07-30-sonar-zero-issues-5: esta fase E a "phase explicita" (D-2) para tocar os arquivos
  legados listados abaixo; toda mudanca protegida por testes ja existentes (sem infra nova).
  JS do WebView sem harness — confirmacao funcional vai para Deferred to PR review.
- D-2026-07-30-sonar-zero-issues-6: `Verify:` provam identidade local (grep por arquivo/regra);
  confirmacao real do Quality Gate no SonarCloud fica em Deferred to PR review. LIMITE: o job
  Sonar so compila `TranslateReader.Core` + testes — App C# (`src/TranslateReader`) e
  estruturalmente invisivel ao scan hoje; achado registrado em `todos.md`, fora de escopo.

## Canonical refs
- Inventario completo: `.../scratchpad/sonar-main-inventory.md` (+ `sonar-main-raw.json`)
- `.github/workflows/sonarqube.yml`, `pipeline.yml`

## Out of scope
- Cobertura de scan do Sonar sobre `src/TranslateReader` (App/MAUI C#) — job nao compila esse
  projeto hoje; exigiria runner Windows com workload MAUI. Registrado em `todos.md`.
- `baseline-de-estilo` (editorconfig/gitattributes/analyzers genericos) — fase separada, intocada.
- Refactor alem do exigido pelas regras flagged (ex.: reshuffle amplo de TranslationManager).

## Definition of Done

### Auto-verifiable
- [ ] `dotnet-install.ps1` removido; nenhum arquivo RASTREADO fora de `.jdi/` (registro de
      auditoria append-only) o referencia. Historico git e `.jdi/` fora do universo por design.
      **Verify:** `test ! -e dotnet-install.ps1 && test -z "$(git grep -l 'dotnet-install\.ps1' -- . ':(exclude).jdi' 2>/dev/null)"`
      **Source:** CONTEXT (D-...-1, `Verify:` superseded por D-2026-07-30-sonar-zero-issues-7)

- [ ] `HtmlUtility.cs`: as 7 chamadas estaticas `Regex.Match/IsMatch` (S6444+SYSLIB1045) viram
      `[GeneratedRegex]` com `matchTimeoutMilliseconds`; `TextBlockRegex` (SYSLIB1044) tem pragma
      disable/restore justificado; `InjectTags` decomposto (menos pontos de decisao).
      **Verify:** `F=src/TranslateReader.Core/Utilities/HtmlUtility.cs; test $(grep -cE "Regex\.(Match|IsMatch|Replace)\(" "$F") -eq 0 && test $(grep -c "\[GeneratedRegex(" "$F") -ge 7 && grep -q "#pragma warning disable SYSLIB1044" "$F" && grep -q "#pragma warning restore SYSLIB1044" "$F" && test $(awk "/public static string InjectTags/,/private static string BuildFallbackHtml/" "$F" | grep -cE "\bif \(") -le 4`
      **Source:** CONTEXT (D-...-3)

- [ ] WebView JS (`translation.js`, `scroll.js`, `bridge.js`): `.dataset` no lugar de
      get/set/has/removeAttribute, `for-of` no lugar de `for` indexado, `Number.parseInt`,
      optional chaining nas 2 cadeias `&&` de `bridge.js:77,79`.
      **Verify:** `T=src/TranslateReader/Resources/Raw/wwwroot/js/translation.js; S=src/TranslateReader/Resources/Raw/wwwroot/js/scroll.js; B=src/TranslateReader/Resources/Raw/wwwroot/js/bridge.js; test $(grep -cE "\.(hasAttribute|getAttribute|setAttribute|removeAttribute)\(" "$T") -eq 0 && test $(grep -c "\.dataset\." "$T") -ge 4 && test $(grep -cE "\.getAttribute\(" "$S") -eq 0 && test $(grep -c "Number\.parseInt(" "$S") -ge 2 && grep -q "window.chrome?.webview" "$B" && grep -q "window.webkit?.messageHandlers?.webwindowinterop" "$B"`
      **Source:** CONTEXT (D-...-5)

- [ ] `HtmlInjectionTests.cs`: as 3 ocorrencias de `Regex.Matches(...).Count` (CA1875) viram
      `.Count(...)` sobre regex gerado (SYSLIB1045 fechado junto).
      **Verify:** `F=test/TranslateReader.Tests/HtmlInjectionTests.cs; test $(grep -cE "\.Matches\([^)]*\)\.Count" "$F") -eq 0 && test $(grep -c "\[GeneratedRegex(" "$F") -ge 3`
      **Source:** CONTEXT (D-...-3)

- [ ] `FileUtilityTests.cs` L95 (BLOCKER S2699, teste sem assert real): ganha assercao load-bearing
      (padrao `Record.ExceptionAsync`+`Assert.Null`, igual ao teste irmao de `DeleteFileAsync`).
      Familia CA1816 (7 arquivos de teste, `Dispose()` sem `GC.SuppressFinalize`) + CA1847
      (`LibraryManagerTests.cs:175`) fechada junto.
      **Verify:** `grep -A4 "DeleteDirectoryAsync_DoesNotThrowForNonExistentDirectory" test/TranslateReader.Tests/FileUtilityTests.cs | grep -q "Assert\." && for f in FileUtilityTests BookTranslationJobAccessTests ModelAccessTests TranslationCacheAccessTests BooksAccessTests ReadingStateAccessTests SettingsAccessTests; do grep -A3 "public void Dispose()" "test/TranslateReader.Tests/$f.cs" | grep -q "GC.SuppressFinalize" || exit 1; done && grep -q "Contains('" test/TranslateReader.Tests/LibraryManagerTests.cs`
      **Source:** CONTEXT (D-...-3, blocker verificado — teste real sem I/O e sem assert)

- [ ] `TranslationEngine.cs`: dispose pattern conforme (S3881+CA1816) — classe `sealed` +
      `GC.SuppressFinalize(this)` em `Dispose()`. `TranslationEngineTests.cs`: os 2
      `[Fact(Skip=...)]` de integracao (xUnit1004) ganham pragma disable/restore justificado.
      **Verify:** `F=src/TranslateReader.Core/Business/Engines/TranslationEngine.cs; grep -q "public sealed class TranslationEngine" "$F" && grep -A3 "public void Dispose()" "$F" | grep -q "GC.SuppressFinalize" && T=test/TranslateReader.Tests/TranslationEngineTests.cs; grep -q "#pragma warning disable xUnit1004" "$T" && grep -q "#pragma warning restore xUnit1004" "$T" && test $(grep -c "Fact(Skip" "$T") -eq 2`
      **Source:** CONTEXT (D-...-3)

- [ ] `index.html`: `lang="pt-BR"` no `<html>` + `<title>` presentes (2 BUGs fixados);
      `user-scalable=no` MANTIDO (D-...-4); waivers `Web:S7926`+`css:S4667` registrados em
      `sonarqube.yml`.
      **Verify:** `F=src/TranslateReader/Resources/Raw/wwwroot/index.html; grep -qE "<html[^>]* lang=\"pt-BR\"" "$F" && grep -qi "<title>" "$F" && grep -q "user-scalable=no" "$F" && grep -q "Web:S7926" .github/workflows/sonarqube.yml && grep -q "css:S4667" .github/workflows/sonarqube.yml`
      **Source:** CONTEXT (D-...-3, D-...-4)

- [ ] Access+Engine mecanicos: `ParsingEngine.cs` (4x S6966, `.Open()`->`.OpenAsync()`, API
      nativa .NET 10); `BooksAccess.cs`/`SettingsAccess.cs` (`BeginTransaction()`->
      `BeginTransactionAsync()`); `BooksAccess.cs`/`ReadingStateAccess.cs`/
      `BookTranslationJobAccess.cs` (`DateTime.Parse` com `CultureInfo.InvariantCulture`);
      `ReadingStateAccess.cs` (S1192, literal `"$bookId"` -> `const string`).
      **Verify:** `P=src/TranslateReader.Core/Business/Engines/ParsingEngine.cs; test $(grep -cE "\.Open\(\)" "$P") -eq 0 && test $(grep -c "\.OpenAsync(" "$P") -ge 4; B=src/TranslateReader.Core/Access/BooksAccess.cs; test $(grep -c "connection.BeginTransaction()" "$B") -eq 0 && grep -q "BeginTransactionAsync()" "$B" && test $(grep -c "CultureInfo.InvariantCulture" "$B") -ge 2; S=src/TranslateReader.Core/Access/SettingsAccess.cs; test $(grep -c "connection.BeginTransaction()" "$S") -eq 0; R=src/TranslateReader.Core/Access/ReadingStateAccess.cs; test $(grep -c "CultureInfo.InvariantCulture" "$R") -ge 2 && test $(grep -oc "\"\\\$bookId\"" "$R") -le 1; J=src/TranslateReader.Core/Access/BookTranslationJobAccess.cs; test $(grep -c "CultureInfo.InvariantCulture" "$J") -ge 2`
      **Source:** CONTEXT (D-...-5)

- [ ] `TranslationManager.cs`: `TranslateChaptersWithCacheAsync`/`TranslateSingleChapterAsync`
      (S107, 8 params>7) reduzidos via objeto de contexto privado (<=7 params cada);
      loop de `chapters` (S3267, L182) vira `.Select(chapter => chapter.HRef)`.
      **Verify:** `F=src/TranslateReader.Core/Business/Managers/TranslationManager.cs; for M in TranslateChaptersWithCacheAsync TranslateSingleChapterAsync; do test $(grep -cE "^[[:space:]]*private async Task $M\(" "$F") -eq 1 || exit 1; N=$(awk -v m="private async Task $M(" '{l=$0; if(g){i=index(l,"*/"); if(i){l=substr(l,i+2); g=0} else next} o=0; while((z=index(substr(l,o+1),"//"))>0){z+=o; pre=substr(l,1,z-1); if(gsub(/"/,"&",pre)%2==0){l=substr(l,1,z-1); break} o=z+1} while(i=index(l,"/*")){r=substr(l,i+2); j=index(r,"*/"); if(j){l=substr(l,1,i-1) substr(r,j+2)} else {l=substr(l,1,i-1); g=1; break}} if(index(l,m))f=1; if(f){for(i=1;i<=length(l);i++){h=substr(l,i,1); if(h=="("){p++} else if(h==")"){p--; if(p==0){print (s?k+1:0); exit}} else if(h=="<"){if(pc!=" ")a++} else if(h==">"){if(a>0)a--} else if(h=="["){b++} else if(h=="]"){if(b>0)b--} else if(h=="{"){c++} else if(h=="}"){if(c>0)c--} else if(h==","){if(p==1&&a==0&&b==0&&c==0)k++} else if(p>=1&&h!=" "&&h!="\t"){s=1} pc=h}}}' "$F"); test -n "$N" && test "$N" -le 7 || exit 1; done && grep -q "chapters.Select(chapter => chapter.HRef)" "$F"`
      **Source:** CONTEXT (D-...-5, `Verify:` superseded por D-2026-07-30-sonar-zero-issues-9, que por sua vez supersede -8)

- [ ] Mecanismo anti-recorrencia ativo: `dotnet-sonarscanner end` roda com
      `sonar.qualitygate.wait=true`, e o job nao pode virar no-op SILENCIOSO sem o secret:
      guard falha o job onde o token DEVE existir (repo de origem) e avisa alto onde a
      ausencia e legitima (fork/Dependabot).
      **Verify:** `grep -A3 "dotnet-sonarscanner end" .github/workflows/sonarqube.yml | grep -q "sonar.qualitygate.wait=true" && W=.github/workflows/sonarqube.yml && test $(grep -c "if: env.SONAR_TOKEN == ''" "$W") -eq 1 && G=$(awk "/if: env\.SONAR_TOKEN == ''/{f=1;next} f&&/^      - name:/{exit} f" "$W") && printf '%s' "$G" | grep -qE "^ +exit 1$" && printf '%s' "$G" | grep -q "github.repository == 'slipalison/TranslateReader'" && printf '%s' "$G" | grep -q "head.repo.fork" && printf '%s' "$G" | grep -q "dependabot\[bot\]"`
      **Source:** CONTEXT (D-...-2, `Verify:` superseded por D-2026-07-30-sonar-zero-issues-10)

### Manual
- _(none)_

## Deferred to PR review
- Confirmacao real de que o SonarCloud Quality Gate fica verde na branch/PR (so existe apos
  push+CI; os `Verify:` acima provam identidade local, nao o resultado do scan remoto).
- Confirmacao FUNCIONAL/visual de que zoom, scroll-sync e overlay de traducao no WebView
  continuam corretos apos a migracao mecanica de `translation.js`/`scroll.js`/`bridge.js`
  (sem harness JS no repo — D-...-5).
- Julgamento de acessibilidade sobre `user-scalable=no` mantido (D-...-4) — argumento
  registrado, mas e chamada de produto/UX, nao verificavel por comando.

## Notes
Distribuicao original das 113 issues e racional completo de cada familia: ver
`D-2026-07-30-sonar-zero-issues-0..6` em `.jdi/DECISIONS.md`. Gap de cobertura de scan do Sonar
sobre o projeto App (MAUI) registrado em `.jdi/todos.md` § `sonar-zero-issues` — nao e bug desta
fase, e limite estrutural do pipeline atual.
