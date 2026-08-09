D-2026-08-08-baseline-de-estilo-5 (2026-08-08): Gate 4 do reviewer vira BLOCK escopado aos arquivos da
phase; NENHUM job de lint no CI. LOCKED.
(1) `.jdi/agents/jdi-reviewer-translatereader.md` (Gate 4, hoje linhas 280-298) e reescrito: lint deixa
de ser "WARN only". Passa a BLOCK **apenas** sobre os arquivos tocados pela phase em review (o proprio
gate ja calcula essa lista); drift em arquivo fora do diff continua no maximo WARN, coerente com D-2.
Some do arquivo o texto "Tighten to BLOCK-on-new-files once the `baseline-de-estilo` phase ships".
(2) O comando do gate acompanha D-...-1: e o subcomando `whitespace` escopado, nao o `dotnet format`
completo — o gate nao pode exigir do executor mais do que a decisao mandou fazer.
(3) NENHUM job novo em `.github/workflows/ci.yml`. Nem repo-wide, nem limitado a Core+Tests. Motivos:
o job de teste roda em `ubuntu-latest` sem workload MAUI (nao consegue carregar o csproj do app), e o
`dotnet format` com MSBuild workspace custa minutos por PR sem cobrir o app. `.github/` fica com git
diff VAZIO nesta phase. Se um dia isso mudar, e outra phase e outra decisao.
(4) Ficam alinhados na mesma passada, porque hoje afirmam o contrario: `.jdi/registry/LEGACY.md:30`,
`.jdi/registry/LEGACY-reviewers.md:33-34`, `.jdi/agents/jdi-doer-translatereader.md:71` ("no
`.editorconfig` or custom analyzers exist yet") e a entrada `.jdi/todos/LEGACY.md:367-378`, que e
marcada com o texto literal `RESOLVIDO em baseline-de-estilo`.
