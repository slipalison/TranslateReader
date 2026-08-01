# Phase 18: Review (slug: translated-epub-images)

**Verdict:** APPROVED_WITH_WARNINGS

Review FINAL da phase (iter 2, re-verify da rodada de warnings do `/jdi-issue`). Regenerada do
zero e auto-suficiente: cobre o diff completo `origin/main` (`05f3670`) → HEAD (`58f341a`) na
branch `jdi/translated-epub-images` (12 commits), incluindo a iter 1 (fix + testes) e a iter 2
(fechamento do W-2 por `D-2026-08-01-translated-epub-images-10`, zero linha de `src/`/`test/`).
Toda evidencia abaixo foi reproduzida por esta revisora nesta sessao — nada e auto-reportado
pelo doer.

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0`: `0 Error(s)` (via DoD 6, rodado nesta sessao) |
| Tests | PASS | `Failed: 0, Passed: 341, Skipped: 2, Total: 343` (baseline D-2 = 167, piso derivado da base = 343, ambos atendidos); JS `node --test test/js/`: `pass 79, fail 0` |
| Coverage | PASS | Escopo new/changed: `ParsingEngine.cs` 100%, `TranslationManager.cs` 100%, `ReadingManager.cs` 100% line-rate; `ChapterContentPurpose.cs` e enum puro (sem linha executavel, ausente do report — esperado). Agregado 93.18% (contexto, nao e o gate). Threshold 90% (D-6) atendido |
| Lint | WARN | `dotnet format --verify-no-changes` exit 2: 9 erros WHITESPACE, todos em linhas do commit legado `4285f25` (provado por `git blame` — inclusive `TranslationManagerTests.cs:560-561`, arquivo tocado pela phase mas linhas NAO tocadas). D-2 exime; dono = phase `baseline-de-estilo` (W-1) |
| Security/Layer | PASS | 5.1/5.2/5.10/5.15b/5.16/5.17: zero hit. 5.3: so auto-interface. 5.11: `+=`5/`-=`4 == baseline bootstrap. 5.12: so o static legado conhecido (`TranslationEngine.cs:16`). `catch(OCE)` em `TranslationManager.cs:61` faz `throw;` (correto, e pre-existente). Empty catches em `Pages/PageModels` sao legado intocado (diff de `src/TranslateReader/` = vazio). Teste do artefato prova que nenhuma URL do app vaza pro EPUB exportado |
| Consistency | PASS | 12 commits, Conventional Commits com scope = slug, tipos adequados (`chore`/`docs`/`test`/`fix`); arquivos commitados == `files_modified` do PLAN (5 de producao + 6 de teste + `.jdi/`); 5/5 tasks completed, cada uma com teste correspondente |
| UI Validation | SKIPPED | has_frontend=false (client MAUI nativo) |
| DoD | PASS | 6/6 auto PASS (comandos extraidos por `sed` do CONTEXT.md commitado e rodados literalmente), 0 manual |

## Blockers

Nenhum.

## Warnings

- **W-1 (herdado da iter 1, nao-corrigivel nesta phase por regra):** `dotnet format` exit 2 com 9
  violacoes WHITESPACE legadas (`ThemeEngine.cs`, `ReaderPage.xaml.cs`, `ThemeEngineTests.cs`,
  `TranslationManagerTests.cs:560-561`). Blame confirma: todas do commit `4285f25` (boundary D-2),
  nenhuma em linha tocada pela phase. Dono: `baseline-de-estilo` (ROADMAP posicao 1); registro ja
  existe em `.jdi/todos/LEGACY.md`. Decisao do doer de NAO corrigir esta correta (D-2).
- **W-3 (NOVO, desta review):** a medicao do cenario (b) registrada em
  `D-2026-08-01-translated-epub-images-10` para o item 5 ANTIGO esta **errada**. A decisao afirma
  "item 5 ANTIGO -> exit 0 VAZIO ... sem emitir um unico erro". Reproduzido nesta sessao em clone
  fresco sem `main` local: o comando ANTIGO sai **exit 1**, nao 0 — a derivacao degrada oca como
  descrito (`B=0`, `S=0`, `names_main=0`, `comm` trivialmente vazio, e 3x `fatal: unable to
  resolve revision: main` SAO emitidos no stderr) e o `dotnet test` roda verde, mas o awk final
  reprova em `Skipped(2) <= S(0)`. Ou seja: o gate ANTIGO falhava FECHADO por acidente (a suite
  ter 2 testes `Skip =`), nao aberto. O risco de passe oco era real mas CONDICIONAL (so com suite
  de 0 skips). Nada disso muda o veredito: a motivacao da troca permanece ((a)/(a')/(b) reprovam
  codigo correto; a janela de oco condicional existia) e os comandos NOVOS foram verificados
  independentemente (tabela abaixo). Recomendacao: uma linha de correcao de prosa em
  `D-...-11` append-only — o mesmo tratamento que a `-10` deu ao "309 nomes" da `-9`. Nao
  bloqueia ship.

## Verificacao cetica da iter 2 (evidencia propria, clone descartavel no scratchpad)

Clone `git clone -b jdi/translated-epub-images` do repo local (sem `main` local — estado exato de
CI); repo real NUNCA mutado (`git status` final: so o `.gitignore` local pre-existente do usuario).
OLD = `Verify:` dos itens 5/6 no CONTEXT.md de `141c63c` (iter 1); NEW = HEAD.

| # | Verificacao | Resultado |
|---|---|---|
| a | Zero diff de codigo na iter 2 | `git diff --stat 141c63c..HEAD -- src/ test/` **vazio**; iter 2 = `D-...-10.md` (A) + `CONTEXT.md` + `SUMMARY.md` |
| b | Regressao de gate (4 mutacoes, `main == origin/main`, melhor caso do OLD) | Arquivo a mais em `src/TranslateReader/`: OLD6=1/NEW6=1. Arquivo a mais no Core fora da lista fechada: OLD6=1/NEW6=1. Metodo de teste DELETADO: OLD5=1/NEW5=1 (`comm -23` nao-vazio). RENOMEADO: OLD5=1/NEW5=1. **Nenhum caso OLD=1/NEW=0** |
| c1 | `main` local ATRASADO (`9e07c83`) | OLD6 exit 1 arrastando `.../js/translation.js` (PR #17); OLD5 afrouxa (`B=337`, `names_main=308`). NEW6 exit 0, `BASE=05f3670`. Confere com D-...-10(a) |
| c2 | `main` local AUSENTE (clone de CI) | OLD6 exit 1 (`fatal: bad revision 'main'`); OLD5 exit **1** (ver W-3 — a D-...-10 alega 0); NEW5 exit 0 FULL RUN no clone (`BASE=05f3670`, `B=338`, `names_base=309`, suite 343) e NEW6 exit 0 |
| c3 | Base AVANCA com PR aberto (commit de terceiro em `origin/main`) | OLD6 exit 1; NEW6 exit 0, `merge-base` fixo em `05f3670`. Confere com D-...-10(a') |
| d | Guardas anti-oco | Sem `origin/main` no clone: `BASE=$(git merge-base origin/main HEAD)` sai exit **128** e o `&&` corta (NEW5=128, NEW6=128 — medido). `test "$B" -gt 0` e `test -s names-base.txt` fecham a janela residual de base ilegivel. Nenhum caso de exit 0 com `origin/main` ausente encontrado |
| e | Append-only | `D-...-10.md` e arquivo NOVO (`A`); `-1..-10` todas `A` no diff vs `origin/main`, zero `M`/`D` em `.jdi/decisions/`; diff iter 2 do CONTEXT.md toca SO as linhas dos itens 5/6 (comando + prosa da ancora + `Source:`) |
| f | Fix continua provado | DoD 1/3 verdes no repo real; no clone, remover as 2 linhas do early-return de `Export` (`ParsingEngine.cs:63-64`) derruba `..._ForExport_MatchesRawZipEntryForEveryChapter` E `..._NoEntryContainsTheAppHost`: `Failed: 2, Passed: 0`. O endurecimento nao mascarou nada |
| g | Escopo | `.gitignore` em ZERO commits (`git log origin/main..HEAD -- .gitignore` vazio; a modificacao local do usuario segue fora); `git diff origin/main -- src/TranslateReader/` vazio |

## Review do codigo (iter 1, diff de producao)

- `ChapterContentPurpose` (enum novo em `Models/`, com `<summary>`) como 4o parametro OBRIGATORIO
  de `ExtractChapterContentAsync` — `IParsingEngine` permanece com 6 operacoes (D-...-2),
  `<summary>` atualizado no contrato. Conforme The Method: enum em Models (zero dependencia),
  nada sobe de camada.
- `Export` = early return de `item.Content` cru (nem `RewriteImagePaths` nem `InlineCssLinks` —
  fecha `<img>` E `<link>` na mesma correcao, D-...-3); `Display` = comportamento de hoje + guarda
  fail-fast `imagesDirectory` vazio → `InvalidOperationException` ao lado das 2 guardas existentes
  (csharp.md §1). Comentario unico e um WHY legitimo.
- Call sites conforme D-...-4: `TranslationManager` 3x `Export`/0x `Display`;
  `ReadingManager` 1x `Display`/0x `Export` — verificado por grep E por teste NSubstitute
  (`Received(2)`+`DidNotReceive`, `Received(1)`).
- Testes novos de qualidade real: comparacao BYTE-A-BYTE contra a entrada crua do zip (todos os
  capitulos), guarda, e teste de ARTEFATO reproduzindo o caminho de producao
  (`RebuildEveryChapterAsync` → `CreateTranslatedEpubAsync` → reabre o zip), com forma DIFERENCIAL
  de `https://` (D-...-9(B)) e `epub-images` absoluto; `StringComparison.Ordinal`; cleanup em
  `finally`. I/O de disco so em `ParsingEngineTests.cs` (excecao nomeada D-2026-07-31-coverage-90-3).
- RED→GREEN integro: assercao do teste do artefato identica antes/depois do fix (unica linha
  alterada no corpo = o 4o argumento), conferido no diff `d001a75`→`036b2b6`.

## DoD Checklist (gate 8)

Comandos extraidos do CONTEXT.md commitado (`sed -n 's/^ *\*\*Verify:\*\* ...`) e executados
literalmente nesta sessao. `.jdi/PROJECT.md` nao possui secao `## Definition of Done` — o DoD da
phase (CONTEXT.md) governa.

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | `Export` == entrada crua do zip, todos os capitulos | CONTEXT (D-...-2/-3) | Auto | PASS | exit 0; `Failed: 0, Passed: 1` (`TestResults/dod1.log`) |
| 2 | Guarda `Display`+dir vazio lanca; `Display` real segue reescrevendo | CONTEXT (D-...-3) | Auto | PASS | exit 0; `Failed: 0, Passed: 2` (`dod2.log`) |
| 3 | Artefato sem `epub-images` (absoluto) e sem `https://` GANHO (diferencial) | CONTEXT (D-...-2/-3/-4) | Auto | PASS | exit 0; `Failed: 0, Passed: 1` (`dod3.log`) |
| 4 | Wiring: 3x `Export` no TranslationManager, 1x `Display` no ReadingManager | CONTEXT (D-...-4) | Auto | PASS | exit 0; greps estruturais + `Failed: 0, Passed: 3` (`dod4.log`) |
| 5 | Suite inteira: `Failed: 0`, `Total >= B+5`, `Skipped <= S`, `comm -23` vazio, ancora merge-base | CONTEXT (D-...-9(A), D-...-10) | Auto | PASS | exit 0; `B=338 S=2` derivados de `BASE=05f3670`; `343 >= 343`; `names_base=309`/`names_head=314`, `comm` vazio (`dod5.log`) |
| 6 | Escopo fechado: app intocado, Core = 5 arquivos, app builda | CONTEXT (D-...-4/-7, D-...-10) | Auto | PASS | exit 0; diff app vazio; lista fechada confere; `0 Error(s)` (`dod6.log`) |

**Totals:** 6 items | Auto: 6 (6 PASS, 0 FAIL) | Manual: 0 pending

## Estado final da phase

- Bug raiz (URL do virtual host `epub-images` gravada dentro do EPUB traduzido) corrigido na
  GERACAO, provado no ARTEFATO, e o estado que o causava virou inalcancavel (guarda fail-fast).
- Suite 343/343 (`Failed: 0`), 5 testes novos, zero teste deletado/renomeado/pulado; JS 79/79.
- W-2 da iter 1 FECHADO de verdade: ancora `merge-base origin/main HEAD` + guardas anti-oco,
  provada superior ao literal `main` nos 3 cenarios e sem regressao de deteccao (4/4 mutacoes).
- Pendencias deliberadas registradas: regexes de forma e percent-encoding
  (`.jdi/todos/2026-08-01-translated-epub-images.md`, D-...-6), lint legado
  (`.jdi/todos/LEGACY.md`, W-1), migracao de livros quebrados NAO construida (D-...-7, YAGNI).
- Warnings em aberto: W-1 (legado, dono e outra phase) e W-3 (prosa de medicao na D-...-10 —
  correcao de uma linha, append-only, pode ser feita no proprio PR ou numa D-...-11).

## Para o revisor humano do PR

1. **Livros ja traduzidos ANTES da correcao continuam quebrados por decisao (D-...-7):** o
   artefato e derivado e descartavel; o caminho do usuario e apagar da biblioteca e retraduzir
   (TranslationCache quente). Decisao de produto/UX pendente: ONDE/COMO comunicar isso.
2. **Confirmacao visual em device/WebView real** de que o EPUB traduzido abre com imagens — sem
   harness neste ambiente (mesmo limite das phases anteriores).
3. **SonarCloud** sem issue nova nos 5 arquivos tocados — so verificavel apos push+CI.
4. **CI precisa de `fetch-depth: 0`** (ou `git fetch origin main`) para os `Verify:` dos itens
   5/6 — pre-requisito novo da D-...-10; sem `origin/main` os gates falham FECHADOS (exit 128).
5. **W-3:** ao ler a `D-...-10`, saiba que o sub-claim "(b) item 5 ANTIGO exit 0 VAZIO" nao
   reproduz (o exit real e 1 pelo piso de `Skipped`); o resto da decisao foi reproduzido 1:1.
6. O card pediu "mais pontos de vista": os 6 foram endereçados (1 e 3 corrigidos, 2 refutado com
   evidencia, 4 e 6 confirmados e adiados com registro, 5 decidido explicitamente).

## Recommendation

Ship. Nenhum blocker; os 2 warnings tem dono e registro. Sugestao barata antes do merge: D-...-11
de uma linha corrigindo a prosa da medicao (b) da D-...-10 (mesmo padrao que a -10 usou para a -9).

**Verdict:** APPROVED_WITH_WARNINGS
