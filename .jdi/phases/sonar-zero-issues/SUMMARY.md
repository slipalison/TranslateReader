# Phase 14: Zerar as issues do SonarQube e travar a regressao — Summary  (slug: sonar-zero-issues)

**Status:** complete · **Tasks:** 8/8, 0 blocked · **Base:** `6132078` (main) — branch `jdi/sonar-zero-issues`

## Iter 1 — entrega das 8 tasks

Commits (1 task = 1 commit, escopo `(sonar-zero-issues)`, tipos variados): `8e6200f` T-1 chore
remove vendored dotnet-install.ps1 · `294d316` T-2 refactor modernize WebView DOM access in JS ·
`ddda060` T-3 fix lang+title in index.html + waivers · `2bbdaee` T-4 refactor HtmlUtility →
GeneratedRegex + split InjectTags · `d84f3e9` T-5 fix dispose pattern + test-project smells ·
`df53909` T-6 fix async I/O, invariant date parser, named parameter constant · `229fe5d` T-7
refactor group per-book translation context + simplify rebuild loop · `6cc6200` T-8 ci fail the
build when the SonarCloud Quality Gate fails.

**Destino das 113 issues** (taxonomia D-...-3, nenhuma silenciada sem registro): **67 FIX**
(15 `HtmlUtility`, 17 JS, 2 BUG `index.html`, 4 `ParsingEngine`, 3 `TranslationManager`, 9 nos 4
`*Access`, 2 `TranslationEngine`, 15 em testes) · **41 REMOCAO** (`dotnet-install.ps1` deletado) ·
**2 EXCLUSAO** multicriteria (`Web:S7926`+`css:S4667`, D-...-4) · **3 WAIVER** `#pragma`
(`SYSLIB1044` + 2x `xUnit1004`). Soma exata: 113.

**Gates:** build **0 erros** / 64 avisos (todos `MVVMTK0045` pre-existentes do app MAUI); testes
**256 (254p / 2s / 0f)** vs baseline 229 (227/2) = **+27, 0 deletado, 0 afrouxado**; cobertura D-6
sobre linhas ALTERADAS de producao **68/68 = 100,0%**; `dotnet format` 9 violacoes, **todas fora do
diff da fase** (legado, D-2). DoD: 10/10 `Verify:` exit 0.

**Mutacao T-1/T-6** (executada, tree restaurado a cada mutante): `Verify:` novo do item 1 registrado
em `D-...-7` (permissao readicionada -> exit 1; script restaurado -> exit 1) · `await using var
writer`→`var writer` **PEGO** (1 falha) · `stream.SetLength(0)` removido **PEGO** (1) ·
`OpfTitleRegex().Replace` removido **PEGO** (2) · `CommitAsync()` removido **PEGO** (6).
**Prova negativa declarada:** remover `CultureInfo.InvariantCulture` dos 3 caminhos de LEITURA
**NAO e pego** (12/12 passam) — o formato "O" e culture-invariant por especificacao (probe em
ar-SA/th-TH/fa-IR/he-IL/ja-JP); a metade REAL do risco (escrita `ToString("O")`→`ToString()`)
**e pega** (6 falhas), presa por `CultureRoundTripTests.cs`. Aqui InvariantCulture e conformidade
de regra (S6580), nao correcao de comportamento.

**Desvios declarados:** T-6 usa I/O de disco em 3 testes novos (autorizado pelo PLAN — fixture
`TestData/` ja existente, sem infra nova, limpeza em `finally`); assert do `<dc:title>` sobre texto
interno; `HybridWebViewContractTests.cs:196-197` ajuste mecanico `items[i].x`→`item.x` acompanhando
`for`→`for-of`; `GC.SuppressFinalize(this)` como 1a instrucao em `TranslationEngine.Dispose()`;
T-7 declarou `TranslateSingleChapterAsync` antes do chamador para a janela do `awk` do DoD 9 cair
sobre a declaracao — **este ultimo virou o blocker do DoD critic, tratado na iter 2**.

**Fora de escopo (inalterado):** Quality Gate real no SonarCloud, confirmacao funcional do WebView e
julgamento UX de `user-scalable=no` seguem em `Deferred to PR review`; o job Sonar nao compila
`src/TranslateReader`, entao o C# do app segue invisivel ao scan (D-...-6, `todos.md`).

## Iter 2 — fix do blocker do DoD critic

**Blocker (unico, objetivo):** o `Verify:` do item 9 do DoD nao media parametros. O `awk` antigo
achava a PRIMEIRA linha com `<Nome>(`, concatenava ate a proxima terminada em `)` e contava as
virgulas dessa janela. Para `TranslateChaptersWithCacheAsync` a primeira ocorrencia e o CALL SITE
(`:59`), nao a declaracao (`:147`) — o gate media o chamador. O critico executou o contra-exemplo:
copia com 3 params extras na DECLARACAO (8 no total, a violacao S107 que o item existe para
impedir) saia **exit 0**. O desvio #1 da iter 1 confirmava: o gate era POSICIONAL, nao semantico.

**Fix: o gate, nao o codigo. Zero linha de producao ou teste alterada nesta iter** —
`git diff HEAD -- src test` vazio. A iter 1 ja entrega 5 parametros reais em cada metodo via
`TranslationRun`; o quebrado era a PROVA.

Caminho seguido (o mesmo de `D-...-7`): decisao NOVA append-only
**`D-2026-07-30-sonar-zero-issues-8`**, citando o contra-exemplo do critico e supersedendo APENAS o
`Verify:`; so depois a linha do item 9 do `CONTEXT.md` foi trocada. Nenhuma decisao reescrita —
`git diff .jdi/DECISIONS.md | grep -c '^-[^-]'` = **0** (858→944 linhas, 1 unico hunk no fim). O
comando gravado nos 2 arquivos foi conferido byte-identico por `cmp`.

**O que o comando novo mede** (3 mudancas de natureza, nao de limiar): (1) ancora em
`^[[:space:]]*private async Task <Nome>(`, exigida EXATAMENTE 1 vez — call site nunca casa esse
prefixo; (2) varre caractere a caractere ate o `)` que fecha a assinatura e conta SEPARADORES de
parametro (virgulas em profundidade de parenteses 1, com `<>`/`[]`/`{}` em profundidade 0),
params = separadores + 1; (3) limiar `-le 7` PARAMETROS (o antigo era `-le 6` VIRGULAS).

**Matriz de mutacao** (repo real NUNCA mutado — copias em `/tmp/s107/<var>/`, `git status
--porcelain` limpo ao final; `Nc/Ns` = params medidos em `...ChaptersWithCache`/`...SingleChapter`):

| Var | Mutante | NEW | OLD | Nc/Ns |
|---|---|---|---|---|
| m0 | copia intacta do arquivo entregue | `exit 0` | `exit 0` | 5/5 |
| m1 | **contra-exemplo do critico**: 8 params na DECL de `...ChaptersWithCache` | **`exit 1`** | `exit 0` | 8/5 |
| m2 | 8 params na DECL de `...SingleChapter` | **`exit 1`** | `exit 1` | 5/8 |
| m3 | **REORDER puro**: as 2 decls movidas para DEPOIS dos chamadores | `exit 0` | **`exit 1`** | 5/5 |
| m4 | m3 + 8 params na DECL de `...SingleChapter` | **`exit 1`** | `exit 1` | 5/8 |
| m11 | a mesma violacao de 8 params colapsada em UMA linha | **`exit 1`** | `exit 0` | 8/5 |
| m12 | fronteira: exatamente 7 params | `exit 0` | `exit 0` | 7/5 |
| m5 | 6o param `Dictionary<string, int>` (virgula de generico) | `exit 0` | `exit 0` | 6/5 |
| m7 | 6o param com default `1 > 0 ? 1 : 0` (`>` sem par) | `exit 0` | `exit 0` | 6/5 |
| m10 | 6o param com default `1 < 2 ? 1 : 0` (`<` sem par) | `exit 0` | `exit 0` | 6/5 |
| m8 | clausula S3267 revertida para `foreach (var chapter in chapters)` | `exit 1` | `exit 1` | 5/5 |
| m9 | declaracao renomeada (ancora ausente) | `exit 1` | `exit 1` | — |

m1/m11 fecham o furo (OLD dava falso PASS). m3 prova que o gate deixou de ser posicional:
reordenacao pura — mesmo multiset de linhas, conferido por `sort`+`cmp` — NAO muda o NEW e DERRUBA
o OLD. m8/m9 provam **zero regressao de gate**: a clausula `chapters.Select(chapter => chapter.HRef)`
ficou BYTE-IDENTICA e continua presa. O desvio #1 da iter 1 **nao foi revertido** — apenas deixou
de importar, como m3 demonstra.

**Residuos DECLARADOS** (nenhum ocorre nos 2 metodos de hoje, que nao tem default nem literal na
assinatura): virgula dentro de literal de string num valor default (`string x = "a,b"`) conta como
separador — medido em m6: 6 params reais reportados como **7**; direcao SEGURA (superestima): so
causa reprovacao falsa, nunca aprovacao falsa; idem virgula em comentario `//`. `<` colado a
identificador dentro de um default (`1<2`, sem espaco) subestimaria — as 2 variantes com formatacao
normal foram fechadas e medidas (`>` por clamp `if(a>0)a--`, m7 = 6; `<` pela guarda `if(pc!=" ")`,
m10 = 6). A ancora fixa a forma `private async Task <Nome>(`: mudar tipo de retorno ou visibilidade
faz o gate REPROVAR — falha ruidosa deliberada, obriga revisitar o item do DoD.

**Gates re-rodados (numeros reais, iter 2):** `dotnet build TranslateReader.slnx -c Release` →
**0 erros**, 64 avisos (identico a iter 1) · `dotnet test -c Release` → **256 total / 254 aprovados /
2 ignorados / 0 falhas** · atributos `[Fact]`/`[Theory]` vivos = **235**, `Fact(Skip` = **2**
(baseline preservado, nada deletado ou afrouxado) · `dotnet format --verify-no-changes` → as mesmas
**9** violacoes pre-existentes fora do diff da fase · **10/10 `Verify:` do DoD extraidos LITERALMENTE
do `CONTEXT.md` vigente saem exit 0** (item 9 agora com o comando novo).
