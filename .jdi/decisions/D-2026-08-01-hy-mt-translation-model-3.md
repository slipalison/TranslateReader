D-2026-08-01-hy-mt-translation-model-3 (2026-08-01): Divulgacao da licenca Tencent HY Community
License Agreement (achado critico fora do card, WebFetch de
`https://huggingface.co/tencent/HY-MT1.5-1.8B-GGUF/blob/main/License.txt`) fica LOCKED como decisao
explicita e visivel, nunca absorvida em silencio como detalhe de implementacao. Clausulas reais
confirmadas: territorio EXCLUI EU/Reino Unido/Coreia do Sul ("THIS LICENSE AGREEMENT DOES NOT APPLY
IN THE EUROPEAN UNION, UNITED KINGDOM AND SOUTH KOREA"); atribuicao recomendada "Powered by Tencent
HY"; exige declarar explicitamente que a Tencent NAO e afiliada/associada/patrocinadora/endossante
do produto; exige acompanhar distribuicoes de um arquivo de aviso ("Notice" text file) com a
informacao de copyright; limite de 100 milhoes de MAU antes de precisar de licenca separada da
Tencent (irrelevante na escala atual do produto, registrado por completude).
Forma minima correta ESCOLHIDA (dentre as opcoes que o achado permitia): (1) arquivo novo
`THIRD-PARTY-NOTICES.md` na raiz do repo, com o texto das clausulas acima (territorio, atribuicao,
nao-afiliacao, Notice) — satisfaz o requisito de acompanhar a licenca de forma auditavel e
versionada; (2) atribuicao ALCANCAVEL pelo usuario final dentro do proprio app: `SettingsOverlay`
ganha um label curto perto do botao do modelo HY-MT com o texto "Powered by Tencent HY" +
nao-afiliacao + ponteiro pro `THIRD-PARTY-NOTICES.md` — cumpre literalmente a opcao "o model's entry
in settings" do brief. REJEITADO: geo-gating real (bloquear o download do modelo pra usuarios em
EU/UK/Coreia do Sul) — o app CLIENTE nao tem NENHUMA infraestrutura de geolocalizacao/deteccao de
territorio hoje (sem IP geolocation, sem restricao por loja/regiao no repo); construir isso so pra
este modelo seria trabalho de infraestrutura nova, desproporcional ao pedido do card e fora do
principio YAGNI. Efeito: a decisao de NAO fazer geo-gating fica EXPLICITA aqui (nao "silenciosamente
decidida") e o risco legal residual para usuarios nesses 3 territorios vai para
`## Deferred to PR review` do CONTEXT.md desta phase — decisao de produto/legal cabe ao dono do
repositorio, nao a este fluxo automatizado.
