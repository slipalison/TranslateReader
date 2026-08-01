D-2026-08-01-translated-epub-images-9 (SUPERSEDE os pisos ocos do DoD desta fase — itens 5 e 3 —
por MEDICAO real com shell; `-1` a `-8` ficam INTOCADAS): a sessao do asker rodou sem shell
(D-2026-08-01-translated-epub-images-8) e fixou os pisos por contagem estatica. Esta sessao do doer
TEM shell e mediu. Duas correcoes, ambas append-only:

**(A) Item 5 do DoD — o piso `304` estava 34 testes ABAIXO do real, e contagem nao prova
nao-regressao.** Corrida LIMPA da suite no branch `jdi/translated-epub-images` ANTES de qualquer
mudanca de codigo (arvore identica a `main` em `src/` e `test/`; log em
`TestResults/baseline-main.log`):

    Passed!  - Failed:     0, Passed:   336, Skipped:     2, Total:   338, Duration: 4 s

`304` aceitaria perder 34 testes sem reprovar. Derivacao estatica reconferida com o MESMO grep do
gate sobre `main` (`05f3670`): `[Fact` + `[InlineData` = **338**, `Skip =` = **2** — bate 1:1 com a
corrida real, confirmando que nao ha `MemberData`/`ClassData` e que a formula e exata.

Mas cravar `338` repetiria o erro em outra forma (numero que envelhece a cada phase) e, pior,
CONTAGEM nao prova nao-regressao: o learning de `div-paragraph-reading` (`SHIPPED.md`, `## Learnings`)
diz que um piso por contagem aceita stub sem assert e delecao compensada — quem prova e comparar
NOME A NOME contra `main`. Logo o item 5 passa a DERIVAR `B` e `S` de `main` dentro do proprio
comando e a exigir, alem do piso numerico, que nenhum nome de metodo publico de teste presente em
`main` esteja ausente no HEAD (`comm -23`, superset conservador: falha fechado, nunca aberto).

`+5` = os 5 testes novos desta fase (3 em `ParsingEngineTests.cs`, 2 em `TranslationManagerTests.cs`,
nomes ja prescritos em `## Notes` do CONTEXT). Comando novo do item 5 (substitui o antigo por
inteiro; `304` sai de cena):

    mkdir -p TestResults && B=$(git grep -hoE '\[(Fact|InlineData)' main -- 'test/TranslateReader.Tests/*.cs' | wc -l) && S=$(git grep -hoE 'Skip[[:space:]]*=' main -- 'test/TranslateReader.Tests/*.cs' | wc -l) && git grep -hoE 'public[[:space:]]+(async[[:space:]]+)?(Task|void)[[:space:]]+[A-Za-z0-9_]+' main -- 'test/TranslateReader.Tests/*.cs' | awk '{print $NF}' | sort -u > TestResults/names-main.txt && git grep -hoE 'public[[:space:]]+(async[[:space:]]+)?(Task|void)[[:space:]]+[A-Za-z0-9_]+' HEAD -- 'test/TranslateReader.Tests/*.cs' | awk '{print $NF}' | sort -u > TestResults/names-head.txt && test -z "$(comm -23 TestResults/names-main.txt TestResults/names-head.txt)" && DOTNET_CLI_UI_LANGUAGE=en dotnet test test/TranslateReader.Tests/TranslateReader.Tests.csproj -c Release > TestResults/dod5.log 2>&1 && grep -q "Passed!" TestResults/dod5.log && awk -v b="$B" -v s="$S" '/Passed!/{ok=1;for(i=1;i<=NF;i++){if($i=="Failed:")f=$(i+1);if($i=="Passed:")p=$(i+1);if($i=="Skipped:")k=$(i+1);if($i=="Total:")t=$(i+1)}} END{exit (ok && f+0==0 && t+0>=b+5 && k+0<=s+0 && p+0+k+0+f+0==t+0)?0:1}' TestResults/dod5.log

Valores medidos hoje na derivacao: `B=338`, `S=2`, piso efetivo `Total >= 343`, `comm -23` VAZIO
(309 nomes em `main`, 309 no HEAD). PRE-REQUISITO do comando: o ref LOCAL `main` tem de apontar pro
`origin/main` real. Nesta sessao ele estava PARADO em `9e07c83` (PR #16) enquanto `origin/main` ja
era `05f3670` (PR #17) — com o ref velho, `git diff --name-only main -- src/TranslateReader/` do item
6 devolveria `Resources/Raw/wwwroot/js/translation.js` (arquivo do PR #17, nao desta fase) e o item 6
reprovaria codigo correto. Corrigido por fast-forward puro do ref (`git update-ref refs/heads/main
origin/main`; `9e07c83` e ancestral de `05f3670`, verificado com `git merge-base --is-ancestor`) —
nenhum checkout, rebase ou merge, arvore de trabalho intocada.

**(B) Item 3 do DoD — a forma ABSOLUTA de `https://` reprovaria codigo correto; vira DIFERENCIAL.**
Sonda do fixture Practice (`[IO.Compression.ZipFile]::OpenRead` + `StreamReader` por entrada, 45
entradas lidas): **1 entrada tem `https://` NATIVO** — `ops/styles/1266002537.css`, ocorrencia
`https://opensource.org/licenses/MIT` (URL de licenca em comentario de CSS). `epub-images`: **0
entradas**. Consequencias:
- exigir "nenhuma entrada do zip com `https://`" REPROVA o artefato mesmo depois da correcao, porque
  `CreateTranslatedEpubAsync` so reescreve as entradas de capitulo e o `.opf` — a entrada de CSS
  chega ao artefato intacta, com o `https://` que ja vinha do EPUB-fonte;
- o mesmo `https://` de licenca e justamente o que HOJE vaza para DENTRO das entradas de capitulo,
  porque `InlineCssLinks` inlina o CSS no HTML — ou seja, a forma diferencial ainda pega o defeito, e
  pega DOIS de uma vez (a URL do virtual host vinda de `RewriteImagePaths` e o `https://` da licenca
  vindo de `InlineCssLinks`), exatamente o que D-2026-08-01-translated-epub-images-3 previu.

Item 3 passa a exigir, no artefato gerado com capitulos em `Purpose.Export`:
1. **ABSOLUTO:** NENHUMA entrada do zip, de nenhum tipo, contem o literal `epub-images` (medido: 0 no
   original, entao qualquer ocorrencia no artefato e vazamento do app, sem excecao);
2. **DIFERENCIAL:** nenhuma entrada do zip GANHA `https://` — para toda entrada, se o artefato a
   contem com `https://`, a MESMA entrada do EPUB original ja continha `https://`. Comparacao entrada
   a entrada por `FullName`, nao agregada.

O comando `Verify:` do item 3 NAO muda (ele so casa o nome do teste e roda `dotnet test --filter`) —
muda a PROSA e, com ela, a assercao que o teste
`Practice_TranslatedEpubArtifact_ForExportedChapters_NoEntryContainsTheAppHost` implementa. A
assercao e escrita ja na forma diferencial em T-2 (RED) e fica INTOCADA em T-3 (GREEN), que so
acrescenta o 4o argumento `ChapterContentPurpose.Export`.

Nada mais do DoD, do CONTEXT ou das decisoes `-1` a `-8` e alterado por esta decisao.
