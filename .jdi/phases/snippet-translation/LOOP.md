---
phase_slug: snippet-translation
phase_position: 23
iter: 1
total_resets: 2
status: converged
max_iter_per_round: 5
max_resets: 3
created_at: 2026-08-09T14:00:00-03:00
---

## History

- iter 1: BLOCKED, hash=e41ad3c3c445, commit=b7df369, ts=2026-08-09T15:30:00-03:00
- iter 2: APPROVED_WITH_WARNINGS, hash=2cf9f669c2ff, commit=deb5864, ts=2026-08-09T16:15:00-03:00
--- RESUMED from converged at 2026-08-09T17:00:00-03:00 (reset 1/3, user feedback pre-ship: blob some pos-traducao, selecao opaca sobre o texto, blob segmentado por linha vs contorno continuo do mockup) ---
- iter 1: BLOCKED, hash=945c33b2b869, commit=48cae53, ts=2026-08-09T18:05:00-03:00
- iter 2: APPROVED_WITH_WARNINGS, hash=7e45c21685b2, commit=942800a, ts=2026-08-09T18:50:00-03:00
--- RESUMED from converged at 2026-08-09T19:20:00-03:00 (reset 2/3, user feedback pre-ship com screenshot: pill quebra em multiplas linhas no app real — tip/botao sem nowrap + shrink-to-fit de 50vw; fonte da pill/chip nao e Inter no app) ---
- iter 1: APPROVED_WITH_WARNINGS, hash=5a30c91453a6, commit=1f5688f, ts=2026-08-09T20:40:00-03:00 (cobre 2 fixes: pill fit+fonte b3005e1; contexto poluido+salt de cache 25ef3f3 — segundo defeito reportado pelo user mid-round)
--- POST-LOOP fix round at 2026-08-10 (autorizado pelo usuario, 3o feedback com screenshots; resets ja em 2/3 e a regra literal de resume mataria o loop, entao rodou como ciclo doer->verify fora do envelope /jdi-loop) ---
- post-loop: APPROVED_WITH_WARNINGS, commits=044870b+daf11a7+37b5a21 (D-A guarda de proporcao + retry sem contexto + purga de linhas envenenadas; D-B fonts.ready + ResizeObserver + refreshSnippetBlobs + contorno por coluna), review sobrescrito no REVIEW.md
--- POST-LOOP fix round 2 at 2026-08-10 (4o feedback do usuario com screenshots: vidro some quando o paragrafo fragmenta entre paginas no resize + bolha fantasma na troca de pagina) ---
- post-loop-2: APPROVED_WITH_WARNINGS, commit=76e9dac (blobs re-ancorados em .tr-blob-layer por raiz — ancora por paragrafo era estruturalmente invalida sob fragmentacao multi-column; coords root-relative, um contorno por coluna no lugar certo, fantasma eliminado), review sobrescrito no REVIEW.md
--- POST-LOOP fix round 3 at 2026-08-10 (5o feedback do usuario: paragrafo com markup inline virava periodo unico — derivacao D caiu; loading orfao eterno quando o apply nao aplicava) ---
- post-loop-3: APPROVED_WITH_WARNINGS apos 3 ciclos dev<->review. Commits: 6edb678 (split preservando markup via splitText, derivacao D entregue, W-13 resolvida) + 634e307/0b4be77 (loading nunca-orfao, matching de ancora frouxo) + 89c5fe1/02be25b (blockers B-1 crash IndexSizeError e B-2 data-si duplicado, achados pelo reviewer) + ce835e7 (B-3 comment nodes dessincronizando o walk, gate por capability + clamp) + docs. JS 204/204 (99.45%), C# 416 intocado. REVIEW.md final sobrescrito.
--- POST-LOOP fix round 4 at 2026-08-10 (6o feedback do usuario: paragrafo titulo+corpo com o corpo inteiro dentro de UM elemento inline ainda virava periodo unico — a regra "elemento atomico" adiava todos os boundaries internos) ---
- post-loop-4: APPROVED_WITH_WARNINGS, commits=016ef1e+e67be75 (split recursivo: boundary CONTIDO num elemento divide o elemento em clones rasos cloneNode(false) propagando por todos os ancestrais; so boundary CRUZANDO borda segue adiado — B-1/B-2/B-3 preservados e re-pinados). JS 210/210 (99.46%), C# 416 intocado. REVIEW.md iter 9 sobrescrito.
--- POST-LOOP fix round 5 at 2026-08-10/11 (7o feedback do usuario: recusa do modelo persistida como traducao + vazamento parcial de periodos vizinhos em trecho longo) ---
- post-loop-5: APPROVED_WITH_WARNINGS apos 3 ciclos dev<->review. Commits: 1b89bb3 (SnippetValidationUtility: proporcao + blocklist de recusa + stopword-ratio do destino; purga na carga; resolve W-16) + 2b84504 (contexto em JANELA: periodo anterior+trecho+seguinte no lugar do paragrafo inteiro) + 0feaafc (B-4 do reviewer: blocklist exige co-ocorrencia com meta-vocabulario whole-word; purga da carga nao le settings — trocar idioma nunca deleta acervo) + 371e7af (B-5 do reviewer: tabelas EN/ES/PT enriquecidas com pronomes/auxiliares — dialogo comum nunca reprova; fixture verbatim como const apos 2 ocorrencias de stand-in adulterado pelo doer, corrigidas) + docs. C# 443/445 (95.23%, utility 100%), JS 215/215 (99.26%). REVIEW.md iter 10 final sobrescrito.
--- POST-LOOP fix round 6 at 2026-08-11 (8o feedback do usuario: a JANELA de contexto do iter 10 vazava o periodo vizinho — trecho de 134 chars/1 sentenca voltou traduzido com 392 chars/3 sentencas, e nenhuma das guardas existentes pegou: 375 < 134*3+120, PT-BR legitimo, sem frase de recusa) ---
- post-loop-6: APPROVED_WITH_WARNINGS, commits=3978ac8+0b5d477+4ddbb2d. Inferencia 1 passa a ser SEM contexto (D-2026-08-11-snippet-translation-7 supersede empiricamente a parte de contexto da D-2026-08-09-snippet-translation-5; janela vira retry); nova camada de contagem de sentencas (traducao <= original+1); proporcao 3x+120 -> 1.8x+100; restore JS purga linha inflada; cross-pin de constantes C#<->JS resolve W-15. C# 455/457 (95.25%, utility 73/73), JS 218/218 (99.27%). W-21 nova (menor): cross-pin do regex de fronteira ainda so por comentario. REVIEW.md iter 11 sobrescrito.
