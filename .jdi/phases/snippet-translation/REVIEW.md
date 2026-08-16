# Phase 23: Review  (slug: snippet-translation)

**Verdict:** APPROVED_WITH_WARNINGS

**Iter:** 11 (fix pos-aprovacao — 8o feedback do usuario, vazamento de janela medido pelo orquestrador) · Reviewer: `jdi-reviewer-translatereader` (mode=verify + dod-critic fold-in, D-5 enhanced) · Data: 2026-08-11
**HEAD revisado:** `4ddbb2d` · Fixes: `3978ac8` (A1 ordem invertida + A2 contagem de sentencas + A3 proporcao 1.8x+100), `0b5d477` (A4 purga no restore + A5 cross-pin de constantes) · Baseline da phase: `02a4c6c` · Base do round: `793763e` (iter 10 aprovado)

**As 5 entregas verificadas na fonte e por probe mecanico contra o Core real.** O caso MEDIDO (original 134 chars/1 sentenca; traducao persistida com a sentenca vizinha da janela) e reprovado pelas DUAS camadas novas independentemente; a integridade do A4 pos-acidente de `git checkout` foi confirmada NO HEAD (nao pela palavra do doer); os fixtures novos passaram na auditoria (original byte-exact, reconstrucao ROTULADA com a forma real medida, e fixtures de isolamento genuinamente discriminantes por camada).

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `net10.0-windows10.0.19041.0` Release: exit 0, `0 Aviso(s), 0 Erro(s)` |
| Tests | PASS | C#: **455 passed / 0 failed / 2 skipped / 457 total** (+12) >= baseline 167 (D-2). JS: **218/218** (+3). Numeros do doer reproduzidos; zero teste perdido vs `main`; goldens `blob geometry` com zero linha removida |
| Coverage | PASS | `bash scripts/coverage-gate.sh` **exit 0 direto**: `COVERAGE_SCOPE covered=1423 valid=1494 pct=95.25 files=27` (utility `73/73` = 100% no stdout do gate); `COVERAGE_JS covered=1906 valid=1920 pct=99.27 files=5`; `GUARD 0/0`; zero WAIVER_INVALID |
| Lint | WARN | Mesmas 2 FINALNEWLINE legadas (W-7); zero `NoWarn` novo; zero catch novo |
| Security/Layer | PASS | Guardas na Utility (camada certa), Manager orquestra; purga persisted INTOCADA (B-4 preservado — contagem/proporcao so onde o original e conhecido); verificacao cetica abaixo |
| Consistency | PASS | 3 commits atomicos (`3978ac8`, `0b5d477`, `4ddbb2d` docs), conventional (D-4); SUMMARY reproduzido. **Nota para o /jdi-ship:** a supersessao empirica da `D-2026-08-09-snippet-translation-5` (contexto vira retry, nao estrategia primaria) esta documentada no codigo e no SUMMARY §Iter 11, mas DECISIONS.md (append-only por regra propria) ainda NAO tem a D-entry que a registra — gravar no ship |
| UI Validation | SKIPPED | has_frontend=false. Payload/ordem confirmados por leitura pelo orquestrador — convergente com os testes `Received.InOrder` |
| DoD | PASS | **10/10 auto PASS, 0 manual**; comandos re-executados integralmente em HEAD `4ddbb2d` |

## Blockers

Nenhum. B-1..B-5 (iters 8-10) seguem fechados; as camadas novas do iter 11 nao os reabrem (purga persisted intocada; fixtures B-4/B-5 seguem verdes na suite).

## Verificacao cetica do iter 11 (probes re-compilados contra o Core real)

**1. Caso medido + varredura de legitimos (proporcao 1.8x+100):**
```
original medido: len=134, teto=341.2 | reconstrucao: len=392, sentencas=3 vs 1 -> REJECTED (2 camadas)
ACCEPTED  traducao PT fiel de 1 sentenca do MESMO original (126 chars, 1.03x)
ACCEPTED  curto 18->13 | 66->71 (1.08x) | quebra 1->2 sentencas (+1 slack) | 4 sentencas naturais 279->266
teto por comprimento: 30=5.13x | 80=3.05x | 134=2.55x | 200=2.30x | 400=2.05x | 800=1.93x
```
O multiplicador assintota em 1.8x, ACIMA do ~1.6x maximo linguistico EN<->PT — legitimo nao estoura em nenhum comprimento; o slack e overhead fixo que so alarga trechos curtos. **Concordo com slack=100** (vs 80 prescrito): +20 flat mantem o fixture legitimo real de 82->237 chars passando (teto 247.6) e o caso medido segue reprovado com 51 chars de margem (392 > 341.2). Ressalva honesta do probe: uma frase sintetica repetida 4x com quase zero function words reprova no ratio de STOPWORDS pre-existente (iter 10), nao nas camadas novas — texto natural longo passa (testado); nao e regressao deste iter.

**2. Contagem de sentencas C# == JS (verificado mecanicamente, 6 casos de fronteira):** aspas de fechamento retas e curvas, reticencias, abreviacao ("Mr. Smith" — ambos contam 3, consistentes entre si), parenteses e sem-fronteira — `CountSentences` (reflection) e `_splitSentences().length` (node) retornam IDENTICO em todos. A equivalencia hoje e real; o pino anti-drift do REGEX e a W-21 abaixo.

**3. Ordem invertida (A1) sem retry-que-vira-caminho-comum:** `Received.InOrder` pina sem-contexto ANTES de com-contexto; `WhenFirstAttemptSucceeds_NeverBuildsMessagesWithParagraphContext` pina que sucesso na 1a tentativa = exatamente 1 geracao e ZERO prompt com contexto construido — nenhum teste finge sucesso na primeira; o retry so dispara em reprovacao (testes de too-long/idioma-errado/estouro-de-sentencas cobrem os 3 gatilhos). Falha dupla lanca e nao persiste nada (`DidNotReceive` nos 2 saves).

**4. A4 integro no HEAD (acidente de checkout auditado):** `restoreSnippets` roda `_isSnippetTranslationTooLong || _hasTooManySentences` e purga via `snip-remove` (fonte lida em HEAD, nao no relato); constantes nomeadas `_LENGTH_RATIO_MULTIPLIER/_SLACK/_MAX_EXTRA_SENTENCES` presentes e greppaveis; os 3 testes JS novos existem e passam (218/218), incluindo o assert do payload exato do `snip-remove`.

**5. Auditoria de fixtures (historico de 2 stand-ins):** `MeasuredLeakOriginal` byte-exact contra o texto do report; a traducao vazada e ROTULADA como reconstrucao representativa E carrega as propriedades reais medidas (392 chars >= ~375-399 do report; 3 sentencas vs 1). Os 2 fixtures de isolamento sao genuinamente discriminantes: 122 chars/3 sentencas (passa proporcao, so a contagem pega) e 363 chars/2 sentencas (passa contagem, so a proporcao pega) — cada um FALHARIA se sua camada regredisse. Sem suavizacao.

**6. Cross-pin (A5) resolve W-15:** contract test extrai as 3 constantes do JS por regex greppavel e compara por reflection com os consts privados do C# — quebrar qualquer lado falha o build (mutacao do doer coerente com a construcao; extracao falha alto se o literal sumir). **W-15 RESOLVIDA.**

## Warnings

Resolvidas: **W-13** (iter 8), **W-16** (iter 10), **W-15** (iter 11 — cross-pin de constantes entregue e verificado) — mantidas.

Nova (1, menor):

- **W-21 — O REGEX de fronteira de sentenca agora existe em DUAS linguagens sem pino proprio.** `SentenceBoundaryRegex` (C#, `[GeneratedRegex]`) espelha o `_SENTENCE_BOUNDARY_RE` do JS byte a byte, e o comentario C# AFIRMA que o contract test os pina — mas `SnippetsJs_GuardConstantsMatchSnippetValidationUtility` pina so as 3 constantes numericas; um drift no regex NAO quebraria o build. A equivalencia FOI verificada mecanicamente por este review (6 casos de fronteira identicos C#/JS), entao hoje nao ha defeito — mas o pino prometido nao existe. Fix barato: 1 assert extraindo o literal `_SENTENCE_BOUNDARY_RE = /.../;` do JS e comparando com `SentenceBoundaryRegex().ToString()`. Junto: corrigir o comentario C# ou entregar o pino. Candidata imediata de higiene.

Atualizada:

- **W-20 — Residuais da guarda heuristica (re-provados no iter 11, inalterados).** (a) RECALL: recusa sem meta-vocabulario segue passando (re-probe: "I can't do that, it would not be right for me to do so, sorry." ACCEPTED via colisao "do"/"as" na tabela PT). Nota iter 11: a contagem de sentencas NAO ajuda aqui (recusa de 1-2 sentencas passa o teto +1). (b) PRECISAO: conjuncao acidental frase+meta segue reprovando dialogo raro ("I'm sorry about my language"). Candidatas de higiene inalteradas.

Abertas (re-verificadas em HEAD `4ddbb2d`): **W-2** (hint listeners — zero `removeEventListener`, re-contado), **W-3** (ChapterHRef null), **W-4** (_APP_ACCENT), **W-5** (SnippetLabels/Theme 0% — gate agregado 95.25 com folga), **W-6** (solution test legado), **W-7** (FINALNEWLINE legado, re-confirmado), **W-8** (OCE fronteiras), **W-9** (reentrancia download), **W-10** (thread afinidade), **W-11** (multi-trecho parcial), **W-12** (sweep blobs), **W-14** (hint resize), **W-17** (CSS nao pinada), **W-18** (capitulo errado paginado), **W-19** (`_originalParagraphText` morta em producao).

## Gate 5 — detalhe

| Check | Resultado |
|---|---|
| 5.1-5.5 Camadas | ok — camadas novas na Utility pura; Manager segue orquestrando (ordem de tentativas e sequencia, regra nos predicados); contratos intactos |
| 5.6-5.13 | limpos — zero site novo de risco; `SentenceBoundaryRegex` e `[GeneratedRegex]` com timeout 1000ms (ReDoS bounded, padrao da casa); zero static mutavel/cache novo |
| 5.14 | contagem de sentencas roda por resposta/linha restaurada — evento discreto, fora de loop de token; aceitavel |
| 5.15 | zero catch novo; falha dupla lanca; purga persisted segue blocklist-only (B-4 intacto — verificado: `IsPlausiblePersistedSnippetTranslation` NAO ganhou camadas novas) |
| 5.16/5.17 | zero TODO; NSubstitute so interfaces; zero I/O real (o contract test le o proprio wwwroot do repo — padrao pre-existente da classe, nao I/O de teste unitario de logica); utility 73/73 = 100% |

## DoD Checklist (gate 8)

10 comandos extraidos mecanicamente do CONTEXT.md (intocado) e re-executados verbatim em HEAD `4ddbb2d` — **10/10 PASS, 0 manual** (`dod=auto_only`). Evidencias re-derivadas: build 0 Error(s); C# 457 / JS 218 com zero teste perdido vs `main`; regex da fronteira segue 1x NO snippets.js (o espelho C# vive fora do escopo do Verify e esta coberto pela W-21); goldens/frozen/literais intactos (diff vazio vs `02a4c6c` nos 3 congelados); `files=5` + `GUARD 0/0`; pisos 90/85 inalterados; pt-BR 0 no JS.

## DoD-critic (D-5 enhanced, segunda passada — mesmo reviewer, mode=dod-critic)

```json
[
  {"row": 1, "hollow": false, "evidence": "spec fora do diff; grep re-executado"},
  {"row": 2, "hollow": false, "evidence": "testes ~Snippet reais verdes; +12 nomes lidos (utility/manager/contract)"},
  {"row": 3, "hollow": false, "evidence": "regex 1x re-contado no snippets.js POS-iter-11; risco especifico checado: o espelho C# novo NAO adiciona segunda escrita no arquivo que o criterio protege, e a dupla existencia cross-language esta registrada como W-21 — nao torna o row oco (o comando prova o que o criterio pede)"},
  {"row": 4, "hollow": false, "evidence": "goldens intactos; _blobPath fora do diff"},
  {"row": 5, "hollow": false, "evidence": "4 testes de restore verdes + purga nova por contagem testada com payload exato do snip-remove; hash divergente segue skip silencioso (fonte re-lida)"},
  {"row": 6, "hollow": false, "evidence": "contagem corpo==arquivo pos-diff"},
  {"row": 7, "hollow": false, "evidence": "3 congelados diff vazio vs 02a4c6c em HEAD real"},
  {"row": 8, "hollow": false, "evidence": "gate exit 0 real; SCOPE 95.25 coerente com utility 73/73 no AM scope; JS 99.27 coerente com as linhas novas cobertas"},
  {"row": 9, "hollow": false, "evidence": "457 C# / 218 JS rodados por este review; diffs de teste aditivos; comm vazio vs main"},
  {"row": 10, "hollow": false, "evidence": "JS novo lido na integra: zero string de UI; grep pt-BR 0"}
]
```

**Nenhum row hollow.** O critic so aperta — verdito mantido.

## Recommendation

O iter 11 ataca a causa-raiz certa: com a 1a tentativa SEM contexto, nao existe material para vazar (deterministico), e as duas camadas novas — proporcao apertada 1.8x+100 e contagem de sentencas com +1 de folga — pegam o caso medido independentemente uma da outra, sem reabrir B-4 (purga persisted intocada) nem B-5 (varredura de legitimos limpa em todos os comprimentos; assintota 1.8 > 1.6 linguistico). Concordo com o slack=100 pelo raciocinio verificado (overhead fixo de trechos curtos; fixture legitimo real de 2.89x em 82 chars). A disciplina de fixtures foi exemplar desta vez (byte-exact + reconstrucao rotulada com a forma real + isolamento discriminante por camada), o A4 esta integro no HEAD apesar do acidente de checkout, e o cross-pin de constantes resolve a W-15. Sobram: W-21 (pino do regex prometido em comentario mas nao entregue — 1 assert) e a D-entry da supersessao da `D-2026-08-09-snippet-translation-5` a registrar no ship; W-20 re-provada e inalterada. Nada bloqueia. Pronto para `/jdi-ship snippet-translation`.
