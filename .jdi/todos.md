# Todos — scope creep registrado

Append-only. Itens fora do escopo de uma phase discutida, candidatos a phase futura ou acao
manual do usuario. Nunca vira phase automaticamente — precisa ser promovido via
`/jdi-add-phase`.

## De `ci-seguranca` (2026-07-28)

- Build + testes de Android/iOS no pipeline de CI — exigiria workload MAUI mobile instalado no
  runner (e possivelmente emulador/simulador). Nao pedido explicitamente pelo card.
- Assinatura e publicacao em lojas (Google Play Console, Apple App Store Connect/TestFlight) no
  workflow de release — exige certificados/secrets inexistentes hoje.
- `zizmor` (linter estatico de workflows do GitHub Actions) — reforco opcional de rigor, nao
  obrigatorio no card; considerar se quiser elevar ainda mais a regua de supply-chain.
- SonarQube self-hosted (servidor proprio) — descartado a favor do SonarQube Cloud
  (D-2026-07-28-ci-seguranca-6); revisitar so se o projeto ganhar backend/infra propria.
