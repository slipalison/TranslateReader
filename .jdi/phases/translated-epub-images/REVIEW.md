# Phase 18: Review  (slug: translated-epub-images)

**Verdict:** APPROVED_WITH_WARNINGS

Iter 1 (ralph loop). Diff revisado: `origin/main` (`05f3670`) → HEAD (`68f208e`),
branch `jdi/translated-epub-images`. Toda evidência abaixo foi **re-executada por esta
revisora** — nada foi aceito do SUMMARY sem prova própria.

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `dotnet build src/TranslateReader/TranslateReader.csproj -c Release -f net10.0-windows10.0.19041.0` → exit 0, `0 Erro(s)` (warnings MVVMTK0045 pré-existentes) |
| Tests | PASS | `Failed: 0, Passed: 341, Skipped: 2, Total: 343` — baseline `main` 336/2/338 (+5), baseline legado 167 preservado |
| Coverage | PASS | Agregado 93,18% (contexto). Arquivos novos pós-`4285f25`: `BookTranslationResult.cs` 100%, `ExtractedImage.cs` 100%, `ChapterContentPurpose.cs` ausente do report (enum puro, zero linhas cobríveis). Arquivos de produção ALTERADOS na fase: `ParsingEngine.cs`, `TranslationManager.cs`, `ReadingManager.cs` todos com `line-rate="1"` (100%) — acima do piso 90% (D-6) |
| Lint | WARN | `dotnet format --verify-no-changes` exit 2 — 100% legado (ver W-1); nenhuma linha tocada pela fase |
| Security/Layer | PASS | 5.1/5.2/5.10/5.15b/5.16/5.17 sem hit; 5.3 só auto-interface; 5.11 baseline 5/4 inalterado; 5.12 só o static legado conhecido; OCE-swallow em Pages/PageModels é legado não tocado pela fase |
| Consistency | PASS | 5 commits da fase atômicos, Conventional Commits com scope correto, tipos variados (`docs`/`test`/`fix`), files_modified do PLAN = commits reais |
| UI Validation | SKIPPED | has_frontend=false (cliente MAUI nativo) |
| DoD | PASS | 6/6 auto PASS (re-executados literalmente por esta revisora), 0 manual |

## Blockers

Nenhum.

## Warnings

- **W-1 (lint, legado — não bloqueia por D-2):** `dotnet format --verify-no-changes` sai 2 com
  erros `WHITESPACE` em `src/TranslateReader.Core/Business/Engines/ThemeEngine.cs:12,14`,
  `src/TranslateReader/Pages/ReaderPage.xaml.cs:122,124`,
  `test/TranslateReader.Tests/ThemeEngineTests.cs:12` e
  `test/TranslateReader.Tests/TranslationManagerTests.cs:560-561`. Verifiquei: a linha 560 existe
  VERBATIM em `origin/main` na mesma posição e nenhum hunk da fase toca essas linhas — a alegação
  do doer ("saída idêntica antes e depois") confere. Resolve-se na phase `baseline-de-estilo`.
  Regra: Gate 4 (WARN até `.editorconfig` existir).
- **W-2 (fragilidade de gate):** os `Verify:` dos itens 5 e 6 do DoD dependem do ref **LOCAL**
  `main` (`.jdi/phases/translated-epub-images/CONTEXT.md:158,164`). A sessão do doer precisou de
  `git update-ref` para o gate não reprovar código correto — o fast-forward foi legítimo (ver
  veredito (f)), mas um gate que exige pré-sincronização manual de ref local falha nos DOIS
  sentidos: ref velho reprova código correto; `main` que avance além da base do branch também.
  Recomendação para DoDs futuros: ancorar em `origin/main` ou em `git merge-base origin/main HEAD`.
  Regra: Gate 8 (robustez do `Verify:`).

## Veredito dos itens céticos (a)–(h)

- **(a) RED-first — CONFIRMADO.** Worktree descartável em `d001a75` (branch intocada):
  `dotnet test --filter ...NoEntryContainsTheAppHost` → `Failed: 1, Passed: 0`, mensagem com
  `ops/xhtml/ad.xhtml: contains 'epub-images' -> epub-images//ops/images/ad.jpg` (a barra dupla do
  `bookDir` vazio) e `gained 'https://' -> https://opensource.org/licenses/MIT`. E
  `git diff d001a75 036b2b6 -- test/` mostra que o commit de fix só fez o churn mecânico do 4º
  argumento + 2 testes novos; no teste do artefato a ÚNICA linha alterada é o 4º argumento
  `ChapterContentPurpose.Export` em `RebuildEveryChapterAsync` — o bloco `leaks`/`Assert.True` não
  aparece no diff. Asserção intocada entre RED e GREEN.
- **(b) Artefato limpo — CONFIRMADO por inspeção própria.** Sondei o EPUB-fonte Practice
  (45 entradas): **1** `https://` nativo (`ops/styles/1266002537.css`, URL de licença MIT) e **0**
  `epub-images` — exatamente o que o doer alegou. Gerei EU MESMA o artefato no worktree em HEAD
  (persistindo a saída do caminho de produção `Export` → `CreateTranslatedEpubAsync`) e escaneei o
  zip: 45 entradas, **0** com `epub-images`, `https://` presente APENAS em
  `ops/styles/1266002537.css` — a mesma entrada nativa do fonte. Nenhuma entrada GANHOU `https://`.
  A forma DIFERENCIAL do item 3 é necessária (a absoluta reprovaria código correto por causa do CSS
  nativo) e suficiente (o RED provou que ela pega o vazamento nos capítulos).
- **(c) Guarda fail-fast — NÃO quebra produção.** `git grep ExtractChapterContentAsync` em `src/`:
  4 call sites exatos. O ÚNICO com `Display` é `ReadingManager.LoadChapterContentAsync`
  (`ReadingManager.cs:28-31`), onde `imagesDir = Path.Combine(booksDirectory, "images",
  bookId.ToString())` — estruturalmente não-vazio (contém o segmento `"images"` mesmo que
  `booksDirectory` fosse vazio), e `booksDirectory` vem de `MauiProgram.cs:65`
  (`Path.Combine(FileSystem.AppDataDirectory, "books")`). Os 3 call sites com `string.Empty` usam
  `Export`, que ignora o parâmetro. Nenhum caminho legítimo alcança a exceção. Sem blocker.
- **(d) Literais de caracterização — INTOCADOS.** Grep sobre o diff completo da fase em `test/`
  por `6102|239075|1329|292254|2124|678242`: zero ocorrência (nenhum literal adicionado, removido
  ou alterado). Os 4 pares seguem em `ExtractTextBlocksBaselineTests.cs:33-52`, agora rodando sob
  `Export` — prova independente de que pular as 2 mutações não altera o texto extraído.
- **(e) Prova por mutação — REFEITA E CONFIRMADA.** No worktree em HEAD (baseline 3/3 verde):
  (a) removi o early-return de `Export` (grep antes=1/depois=0, `git diff` 2 deleções — mutação
  APLICADA) → `Failed: 2` (`ForExport_MatchesRawZipEntry...` e `NoEntryContainsTheAppHost`),
  exatamente os 2 esperados; (b) restaurei e removi a guarda (grep antes=1/depois=0) →
  `Failed: 1` (`DisplayWithEmptyImagesDirectory_Throws...`). Ambas revertidas, worktree limpo e
  removido. Os testes matam as duas mutações — o gate não é textual.
- **(f) Ref `main` local — fast-forward LEGÍTIMO.** `git merge-base --is-ancestor 9e07c83 05f3670`
  → verdadeiro; `main` == `origin/main` == `05f3670` agora; reflog consistente com update-ref sem
  mexer na árvore. A dependência do `Verify:` em ref LOCAL fica registrada como fragilidade → W-2.
- **(g) Derivação do item 5 — sólida, com lacuna residual conhecida.** `B=338` derivado de `main`
  bate 1:1 com a corrida real de `main` (336+2); piso `Total >= B+5 = 343` é JUSTO (HEAD tem
  exatamente 343 — nada de folga). `comm -23` cobre deleção E rename (nome antigo ausente do HEAD
  → falha fechada); a extração pega métodos públicos não-teste também (superset conservador, falha
  fechada). Lacuna residual: teste esvaziado mantendo o nome passaria no `comm` — mitigada porque
  os 5 testes novos da fase são pinados individualmente pelos itens 1–4 (grep de nome + corrida
  filtrada com piso numérico) e pela prova por mutação (e). Não é frouxo nem impossível hoje;
  fragilidade do ref local coberta em W-2.
- **(h) Escopo e The Method — LIMPO.** Enum novo em
  `src/TranslateReader.Core/Models/ChapterContentPurpose.cs`, namespace `TranslateReader.Models`,
  idêntico ao padrão de `ReadingMode.cs`/`Book.cs` — Models é a camada compartilhada de POCOs que
  `Contracts/` já referencia (`Book`, `Chapter`); nenhuma violação de camada. `IParsingEngine`
  permanece com 6 operações (parâmetro, não método novo — D-...-2), membro alterado com
  `<summary>`. `src/TranslateReader/` (app MAUI): ZERO mudança. `.gitignore`: em 0 commits
  (verificado no log completo da fase). `D-...-1..-8`: intocadas após seus commits de criação
  (`git diff 3271fc3..HEAD -- .jdi/decisions/` mostra SÓ a adição de `D-...-9`);
  `D-...-9` é arquivo NOVO em `0aa4320`. JS: `node --test test/js/` → 79/79 (== baseline).

## DoD Checklist (gate 8)

Todos re-executados literalmente por esta revisora (bash, `DOTNET_CLI_UI_LANGUAGE=en`).

| # | Criterion | Source | Type | Status | Evidence |
|---|---|---|---|---|---|
| 1 | `Export` == entrada crua do zip, todos os capítulos do Practice | CONTEXT (D-...-2/-3) | Auto | PASS | exit 0; `Failed: 0, Passed: 1` |
| 2 | Guarda `Display`+dir vazio lança + regressão do rewrite `Display` | CONTEXT (D-...-3) | Auto | PASS | exit 0; `Failed: 0, Passed: 2` |
| 3 | Artefato sem `epub-images` (absoluto) e sem `https://` GANHO (diferencial) | CONTEXT (D-...-2/-3/-4, D-...-9(B)) | Auto | PASS | exit 0; `Failed: 0, Passed: 1`; confirmado também por inspeção direta do zip (veredito (b)) |
| 4 | Wiring: 3×`Export` no TranslationManager, 1×`Display` no ReadingManager | CONTEXT (D-...-4) | Auto | PASS | exit 0; greps estruturais 3/0/1/0 + `Failed: 0, Passed: 3` |
| 5 | Suite sem regressão, piso derivado de `main` + `comm` nome a nome | CONTEXT (D-...-9(A)) | Auto | PASS | exit 0; `B=338 S=2`, nomes `main`=309 / HEAD=314, `comm -23` vazio; `Failed: 0, Passed: 341, Skipped: 2, Total: 343` (≥ 343) |
| 6 | Escopo de diff fechado + app compila | CONTEXT (D-...-4/-7) | Auto | PASS | exit 0; `src/TranslateReader/` vazio; Core = exatamente os 5 arquivos previstos; build `0 Error(s)` |

**Totals:** 6 items | Auto: 6 (6 PASS, 0 FAIL) | Manual: 0 pending

## Estado final da phase

- Bug corrigido na GERAÇÃO: `Export` devolve o capítulo byte-a-byte como o EPUB armazena; a causa
  raiz histórica (`Display` com diretório vazio) virou estado inalcançável com guarda fail-fast
  comprovadamente fora de qualquer caminho de produção.
- RED→GREEN genuíno (falha reproduzida por esta revisora em worktree no commit RED), asserção
  intocada entre os dois commits, mutações refeitas e mortas pelos testes.
- Suite 343/343-2 verde (+5 sobre `main`), cobertura dos arquivos tocados 100%, JS 79/79,
  escopo de diff fechado, `.gitignore` fora de todos os commits, decisões `-1..-8` intactas.
- Sem itens manuais de DoD → nada pendente para `/jdi-confirm-dod`. Pronto para `/jdi-ship`.

## Para o revisor humano do PR

1. **Livros já traduzidos ANTES do fix continuam quebrados por decisão explícita (D-...-7):** o
   artefato é derivado e descartável; caminho do usuário = apagar da biblioteca e retraduzir
   (cache `TranslationCache` sobrevive, regeneração rápida). Decisão de produto/UX de ONDE/COMO
   comunicar isso está deliberadamente em aberto (`.jdi/todos/2026-08-01-translated-epub-images.md`).
2. Confirmação visual em device/WebView real de que o livro traduzido abre sem imagem quebrada —
   sem harness neste ambiente; recomendo smoke manual no Windows antes do merge.
3. SonarCloud: só existe leitura após push+CI (limite já documentado em `sonar-zero-issues`).
4. Nota menor: `D-...-9` diz "309 nomes em `main`, 309 no HEAD" — snapshot medido ANTES da
   implementação; no HEAD final são 314 (309 + 5 novos). O gate (`comm -23`, só lado `main`) não é
   afetado; é apenas prosa que envelheceu.
5. `LOOP.md` da phase está untracked (artefato do orquestrador, não commitado) — decidir se entra
   no commit de ship ou fica fora, como nas phases anteriores.
6. W-2: considerar padronizar DoDs futuros para ancorar em `origin/main`/merge-base em vez de ref
   local `main`.

## Recommendation

Aprovar com os warnings registrados. Nenhum item exige ação antes do ship; W-1 resolve-se em
`baseline-de-estilo` e W-2 é diretriz para os próximos DoDs. Próximo passo: `/jdi-ship
translated-epub-images` (ou o PR, mantendo os itens 1–2 acima na descrição para o revisor humano).

**Verdict:** APPROVED_WITH_WARNINGS

## DoD Critic (enhanced — forcado por /jdi-issue)

Re-ataque dos 6 rows `Type=Auto`/`PASS` com worktree descartavel (repo real intocado).
**Nenhuma linha oca.** Dois ataques executados no item central (o teste do artefato):

- **Ataque 1 — vazamento em entrada NAO traduzida**: injetei `https://epub-images/leak` em
  `ops/1266002537.opf` (entrada que nao passa por `translatedChapterHtml`). O teste **reprovou**
  (`contains epub-images` + `gained https://`, `Failed: 1`) — `CollectAppUrlLeaks` varre
  `artifact.Entries` INTEIRO (toc, opf, capa, binarios via Latin1), nao so os capitulos.
- **Ataque 2 — ponto cego do diferencial**: `https://leak.example` NOVO dentro de
  `ops/styles/1266002537.css` (a unica entrada com `https://` nativo) **passa**. E ponto cego
  objetivo da forma diferencial por-entrada, mas inalcancavel da producao:
  `CreateTranslatedEpubAsync` (`ParsingEngine.cs:105-122`) so escreve entradas casadas com hrefs de
  capitulo e o `.opf`, e nenhum href de `ReadingOrder` e `.css`; alem disso qualquer vazamento do
  app carrega `epub-images`, pego pelo check absoluto em QUALQUER entrada (provado pelo ataque 1).
  Nao e furo real.

- Item da guarda fail-fast: o filtro do `Verify:` roda os DOIS lados com piso `n=2` — o teste do
  `throw` e o de regressao `RewritesImagePathsToVirtualHostUrl` (`Display` com diretorio valido
  assertando a reescrita). Prova que dispara E que nao dispara no caminho legitimo.
- Item de wiring: contagens `-eq` exatas (3x `Export` em `TranslationManager.cs:124,195,245`,
  1x `Display` em `ReadingManager.cs:31`) falham FECHADO nas duas direcoes, com backstop
  comportamental em `TranslationManagerTests.cs:888-891,905-908` e `ReadingManagerTests.cs:64-65`.
- Item do piso de suite: rename cai (`comm -23` nao-vazio), `[Fact]` removido com metodo mantido cai
  (`Total` 342 < piso 343, sem folga — o HEAD esta exatamente no piso), `Skip` novo cai
  (`Skipped 3 > S=2`). Direcao aberta e o teste PRE-EXISTENTE esvaziado mantendo o nome — residual
  explicitado no proprio texto do criterio, mitigado nos 5 testes novos pela prova por mutacao.
- Item de escopo: a fragilidade do ref `main` LOCAL erra FECHADO (ref velho ADICIONA arquivos ao
  diff e reprova — foi o que aconteceu na sessao do doer). Registrado como W-2.
- Coerencia `D-...-9`: a prosa "309 nomes" envelheceu (o HEAD tem 314 = 309+5), mas o gate consome
  so o lado `main` do `comm -23` — imprecisao de prosa, nenhum veredito muda.

**Verdict:** APPROVED
