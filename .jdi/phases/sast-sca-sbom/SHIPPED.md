shipped_at: 2026-07-28T14:52:13Z
verdict: APPROVED_WITH_WARNINGS
by: Alison Amorim

## Learnings
- Trivy FS e cego para NuGet sem packages.lock.json — o gate SCA idiomatico em .NET e `dotnet list package --vulnerable --include-transitive` com parser fail-closed (provado: red/green fixtures).
- GHSA-2m69-gcr7-jv3q sem `first_patched` upstream: override nearest-wins `SQLitePCLRaw.lib.e_sqlite3` 3.53.3 no Core.csproj — remover quando sair bundle_green corrigido (alternativa de delta menor: lib 2.1.12 stable ja existe in-band).
- DAST/ZAP nao se aplica a app cliente sem superficie HTTP; Trivy Image/Dockle/Checkov idem sem Docker/IaC — decisao auditada em D-2026-07-28-sast-sca-sbom-1, nao re-levantar.
- Regra Semgrep custom so entra como ERROR depois de provada limpa no codebase real (fixture + scan); WebView rule ficou WARNING por 2 call-sites legados sem `JsStr()` em ReaderPage.xaml.cs:306/:474 — ao tocar esse arquivo em phase futura, envolver com JsStr e promover a ERROR.
- `pattern-not` com `$"...{JsStr(...)}..."` exclui limpo os call-sites encodados — padrao reutilizavel pra regras de injection em C# interpolado.
