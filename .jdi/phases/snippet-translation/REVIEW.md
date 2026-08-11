# Phase 23: Review  (slug: snippet-translation)

**Verdict:** APPROVED_WITH_WARNINGS

**Iter:** 10, re-verify final (pos-fix B-5) · Reviewer: `jdi-reviewer-translatereader` (mode=verify + dod-critic fold-in, D-5 enhanced) · Data: 2026-08-11
**HEAD revisado:** `52fc671` · Fixes do iter 10: `1b89bb3`+`2b84504` (D-A/D-B), `0feaafc` (B-4), `371e7af` (B-5) · Baseline da phase: `02a4c6c` · Base do round: `4950262` (iter 9 aprovado)

**B-4 e B-5 RESOLVIDOS e verificados por probe re-compilado contra o Core real.** A correcao de processo tambem foi entregue: o stand-in alongado sumiu dos 3 sites e o fixture #3 vive numa const BYTE-IDENTICA ao texto deste reviewer (`ReviewerFixtureThree`), pinada nos DOIS entry points com um comentario que inverte o onus ("se esta literal falhar, o codigo esta errado — nao o fixture").

## Gates

| Gate | Status | Details |
|---|---|---|
| Build | PASS | `net10.0-windows10.0.19041.0` Release: exit 0, `0 Aviso(s), 0 Erro(s)` |
| Tests | PASS | C#: **443 passed / 0 failed / 2 skipped / 445 total** (+4 Facts B-5, -2 InlineData do stand-in removido = +2 liquido — contagem re-derivada) >= baseline 167 (D-2). JS: **215/215** (diff JS do sub-round = 0 linhas). Numeros do doer reproduzidos |
| Coverage | PASS | `bash scripts/coverage-gate.sh` **exit 0 direto**: `COVERAGE_SCOPE covered=1419 valid=1490 pct=95.23 files=27` (utility `69/69` = 100% no proprio stdout do gate); `COVERAGE_JS covered=1887 valid=1901 pct=99.26 files=5`; `GUARD 0/0`; zero WAIVER_INVALID |
| Lint | WARN | Mesmas 2 FINALNEWLINE legadas (W-7); zero `NoWarn` novo |
| Security/Layer | PASS | B-4/B-5 fechados; camadas The Method corretas (Utility pura, Manager orquestra); verificacao cetica abaixo |
| Consistency | PASS | 2 commits atomicos (`371e7af` fix, `52fc671` docs), conventional (D-4); numeros do SUMMARY reproduzidos; a ressalva de processo do round anterior foi CORRIGIDA de fato (const byte-identica + remocao do stand-in em todos os sites, verificado por grep no source — so binarios stale de bin/obj ainda casam) |
| UI Validation | SKIPPED | has_frontend=false |
| DoD | PASS | **10/10 auto PASS, 0 manual**; comandos re-executados integralmente em HEAD `52fc671` |

## Blockers

Nenhum aberto. B-1..B-3 (iters 8-9) e B-4/B-5 (iter 10) todos resolvidos e verificados.

## Verificacao dos fixes B-4/B-5 (probe re-compilado contra o Core real, ambos entry points)

```
FRESH:     ACCEPTED  os 4 fixtures de dialogo VERBATIM (incl. #3: "I can't breathe," she whispered,
                     afraid of everything around her.)                       [B-5 fechado]
FRESH:     ACCEPTED  "He nodded slowly and walked away without a word."      [narracao EN 40-90 chars]
FRESH:     ACCEPTED  "— Não sei — disse ele, olhando para o chão."           [dialogo curto PT-BR]
FRESH:     ACCEPTED  dialogo curto ES ("Yo no sé nada", dijo ella...)        [tabela ES enriquecida tambem]
FRESH:     REJECTED  recusa do screenshot, destino PT-BR (blocklist E ratio)
FRESH:     REJECTED  recusa do screenshot, destino EN — ratio passa (texto EN), a BLOCKLIST sozinha pega
FRESH:     REJECTED  texto PT com destino EN — a tabela EN enriquecida AINDA detecta idioma errado
                     (o enriquecimento nao diluiu o ratio: pronomes EN nao colidem com PT)
PERSISTED: ACCEPTED  os 4 fixtures + fixture #3 verbatim + linha PT com destino trocado p/ Spanish
PERSISTED: REJECTED  recusa do screenshot
WHOLE-WORD: "i cannot"+"against" nao conta ("ai" nunca casa como substring); "TRADUÇÃO" maiusculo-acentuado conta
```

1. **Tabelas enriquecidas (B-5):** EN +27 (pronomes/auxiliares — "a" deliberadamente FORA, evitando colisao PT/ES), ES +13, PT-BR +15; limiar/comprimento inalterados. O fixture #3 verbatim passa agora com folga (i, you...her, my — multiplos hits) e o wrong-language continua reprovando (probe acima).
2. **Const byte-identica:** `ReviewerFixtureThree` conferida caractere a caractere contra o fixture original deste reviewer; usada no fresh (`..._IsAcceptedInTheFreshPath`), no persisted (`..._IsAccepted`) e no teste de purga do Manager (string exata inline). Grep no source: zero stand-in remanescente.
3. **Regressoes discriminantes:** os 4 Facts novos cobrem exatamente as falhas provadas (fixture #3 fresh, narracao EN, dialogo PT-BR curto, fixture #3 persisted); a recusa classica segue pinada nos dois entry points e pontua 0 hits no ratio (as adicoes EN nao aparecem nela... a deteccao dela e blocklist-meta, intocada).

## Warnings

Resolvidas: **W-13** (iter 8), **W-16** (iter 10) — mantidas.

- **W-20 (ATUALIZADA) — Residuais da guarda heuristica de recusa/idioma (documentados por probe; nenhum destroi dado bom em volume).** (a) RECALL: recusa curta sem meta-vocabulario ("I cannot help with that.", <40 chars) passa tudo; recusa EN >= 40 sem meta passa o ratio PT via colisao lexical ("do"/"as" na tabela PT — persiste pos-enriquecimento, re-provado). Sintoma original pode ressurgir para recusas frouxas — raro (recusas de traducao costumam nomear a tarefa) e barato (falso negativo re-aparece e o usuario descarta como hoje). (b) PRECISAO: dialogo legitimo com conjuncao acidental frase+meta na janela de 160 ("I'm sorry about my language," he said...) segue reprovado nos DOIS entry points — no persisted isso PURGA uma linha legitima; banda estreita (exige a conjuncao) mas real. Candidatas de higiene: remover "do"/"as" da tabela PT (ambiguidade EN) e enxugar meta-palavras largas ("language", "text", "provide") ou exigir 2 hits de meta.

Abertas (re-verificadas em HEAD `52fc671`): **W-2** (hint listeners), **W-3** (ChapterHRef null), **W-4** (_APP_ACCENT), **W-5** (SnippetLabels/Theme 0%), **W-6** (solution test legado), **W-7** (FINALNEWLINE legado), **W-8** (OCE fronteiras), **W-9** (reentrancia download), **W-10** (thread afinidade), **W-11** (multi-trecho parcial), **W-12** (sweep blobs), **W-14** (hint resize), **W-15** (formula `*3+120` C#/JS sem cross-pin), **W-17** (CSS nao pinada), **W-18** (capitulo errado paginado), **W-19** (`_originalParagraphText` morta em producao).

## Gate 5 — detalhe

| Check | Resultado |
|---|---|
| 5.1-5.5 Camadas | ok — Utility estatica pura (teste da maquina de cappuccino), Manager orquestra; contratos intactos; W-16 segue resolvida |
| 5.6-5.13 | limpos — zero site novo de zip/XML/EvaluateJavaScriptAsync/segredo/evento/static-mutavel/cache; `FrozenSet`s static readonly imutaveis |
| 5.14 | tokenizacao por resposta (evento discreto) — aceitavel, fora dos hot paths |
| 5.15 | zero catch novo; falha da dupla tentativa lanca e o PageModel converte na fronteira unica |
| 5.16/5.17 | zero TODO; NSubstitute so interfaces; zero I/O real; utility 69/69 = 100%; fixtures agora VERBATIM com const anti-regressao |

## DoD Checklist (gate 8)

10 comandos extraidos mecanicamente do CONTEXT.md (intocado no round inteiro) e re-executados verbatim em HEAD `52fc671` — **10/10 PASS, 0 manual** (`dod=auto_only`). Evidencias re-derivadas: build 0 Error(s); C# 445 / JS 215 com zero teste perdido vs `main`; `~Snippet` verde com folga sobre o piso 12; regex da fronteira 1x; goldens `blob geometry` e 3 JS congelados intactos (diff vazio vs `02a4c6c`); `COVERAGE_JS files=5` + `GUARD 0/0`; pisos 90/85 inalterados; pt-BR 0 no JS.

Nota: `.jdi/PROJECT.md` nao possui secao `## Definition of Done`; o conjunto do CONTEXT.md e o DoD integral. Itens humanos vivem em `## Deferred to PR review` (paridade visual em device, blur, drag em toque, qualidade linguistica, posicao da pill, custo de sliders, SonarCloud pos-push).

## DoD-critic (D-5 enhanced, segunda passada — mesmo reviewer, mode=dod-critic)

```json
[
  {"row": 1, "hollow": false, "evidence": "spec fora do diff; grep re-executado"},
  {"row": 2, "hollow": false, "evidence": "testes ~Snippet reais verdes; nomes novos lidos; contagem +4/-2 re-derivada"},
  {"row": 3, "hollow": false, "evidence": "regex 1x; split intocado desde o iter 9"},
  {"row": 4, "hollow": false, "evidence": "goldens intactos; _blobPath fora do diff"},
  {"row": 5, "hollow": false, "evidence": "restore verde; purga blocklist-only verificada na fonte e por probe (linha PT sobrevive a settings hostis)"},
  {"row": 6, "hollow": false, "evidence": "contagem corpo==arquivo pos-diff"},
  {"row": 7, "hollow": false, "evidence": "3 congelados diff vazio vs 02a4c6c em HEAD real"},
  {"row": 8, "hollow": false, "evidence": "gate exit 0 real; SCOPE 95.23 coerente com utility 69/69 no AM scope; JS estavel em 99.26 (JS intocado)"},
  {"row": 9, "hollow": false, "evidence": "445 C# / 215 JS rodados por este review; a remocao dos 2 InlineData e a troca do stand-in pelo fixture verbatim TORNAM os testes mais fortes, nao mais fracos — verificado nome a nome"},
  {"row": 10, "hollow": false, "evidence": "zero pt-BR novo no JS (intocado)"}
]
```

**Nenhum row hollow.** O critic so aperta — verdito mantido.

## Recommendation

O iter 10 fecha completo: D-A (recusa/idioma com co-ocorrencia whole-word e purga segura por construcao), D-B (janela de contexto), B-4 e B-5 mortos na causa e verificados por probe mecanico contra o Core real nos DOIS entry points — incluindo recall preservado nos tres caminhos da recusa do screenshot e wrong-language intacto pos-enriquecimento. A correcao de processo pedida foi entregue de verdade: fixture #3 byte a byte numa const anti-regressao usada em todos os sites, stand-ins eliminados do source. Todos os gates e os 10 DoD passam por execucao real; numeros do doer reproduzidos integralmente. W-20 atualizada com os dois residuais da heuristica (FN raro + FP raro por conjuncao — candidatas de higiene mapeadas); W-2..W-19 seguem para a phase de higiene (nenhuma bloqueia; W-13/W-16 resolvidas). Pronto para `/jdi-ship snippet-translation`.
