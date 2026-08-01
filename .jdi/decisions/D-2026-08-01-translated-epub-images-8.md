D-2026-08-01-translated-epub-images-8 (limite de ambiente desta sessao do asker — origem do piso
numerico do DoD): esta sessao rodou em modo `auto` (dispatch `mode=auto dod=auto_only`) SEM acesso a
shell/terminal — as ferramentas disponiveis foram leitura/escrita de arquivo, grep, glob e busca
web; nenhuma execucao de comando foi possivel. O padrao estabelecido nas 2 fases mais recentes do
projeto (`conversion-performance` D-2026-07-31-conversion-performance-8, `div-paragraph-translation`
D-2026-08-01-div-paragraph-translation-9) fixa o piso numerico do DoD a partir de uma corrida REAL de
`dotnet test` (linhas `Passed:`/`Total:` do sumario). Sem shell, o piso desta fase foi fixado por
CONTAGEM ESTATICA de atributos `[Fact]`/`[Theory]` via grep sobre `test/TranslateReader.Tests/*.cs`
nesta sessao: **304** ocorrencias em 24 arquivos (medido nesta sessao, estado atual do worktree).
Esse numero e um PISO SEGURO, nao inflado nem decorativo: contagem de atributo e sempre <= contagem
de casos em runtime (um `[Theory]` com N `[InlineData]` produz N casos de teste, nunca menos) —
`Total: >= 304` no `dotnet test` real nunca reprova por causa da diferenca atributo/caso, so por
regressao de verdade.
INSTRUCAO para quem executar esta fase (doer/reviewer, com shell disponivel): antes de implementar,
capturar o `Total:`/`Passed:`/`Failed:` reais de uma corrida limpa da suite (`git stash` se
necessario) e usar esse numero como piso SE for maior que 304 — o ratchet nunca desce, so sobe
(mesmo mecanismo de D-2026-07-31-conversion-performance-8 e
D-2026-08-01-div-paragraph-translation-9 item 3). Repetido em `## Notes` do CONTEXT.md para nao
depender so desta decisao.
