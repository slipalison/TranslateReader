# Phase 7: Pipeline CI/CD com seguranca + correcao do .slnx — Summary  (slug: ci-seguranca)

**Status:** executed
**Tasks:** 8/8 completed, 0 blocked

## Tasks executadas

| Task | Commit | Files |
|---|---|---|
| T-1 slnx fix (remove `/.idea/`) | `d1f607f` | `TranslateReader.slnx` |
| T-2 CI (test Linux + build Windows) | `34f3a75` | `.github/workflows/ci.yml` |
| T-3 CodeQL (csharp, security-extended) | `5ac2698` | `.github/workflows/codeql.yml` |
| T-4 Dependabot + dependency-review | `386de3f` | `.github/dependabot.yml`, `.github/workflows/dependency-review.yml` |
| T-5 Scorecard + gitleaks + badge | `8c125b8` | `.github/workflows/scorecard.yml`, `.github/workflows/secret-scan.yml`, `README.md` |
| T-6 Release por tag `v*` | `bc29f7f` | `.github/workflows/release.yml` |
| T-7 SonarQube Cloud (dotnet-sonarscanner) | `67e52c9` | `.github/workflows/sonarqube.yml` |
| T-8 Auditoria de hardening | _(sem commit — nada a corrigir)_ | — |

## SHA pins usados (resolvidos via `gh api` + conferidos com `git ls-remote` peeled tags)

```
actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1                    # v7.0.1
actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68                # v6.0.0
actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a             # v7.0.1
actions/setup-java@03ad4de0992f5dab5e18fcb136590ce7c4a0ac95                  # v5.6.0
step-security/harden-runner@bf7454d06d71f1098171f2acdf0cd4708d7b5920         # v2.20.0
github/codeql-action/{init,analyze,upload-sarif}@e4fba868fa4b1b91e1fdab776edc8cfbe6e9fb81 # v4.37.3
actions/dependency-review-action@a1d282b36b6f3519aa1f3fc636f609c47dddb294    # v5.0.0
ossf/scorecard-action@2d1146689b8cda280b9bc96326124645441f03bc               # v2.4.4
gitleaks/gitleaks-action@e0c47f4f8be36e29cdc102c57e68cb5cbf0e8d1e            # v3.0.0
softprops/action-gh-release@3d0d9888cb7fd7b750713d6e236d1fcb99157228         # v3.0.2
```

## Evidencia da auditoria T-8

### Pin coverage (100%)

```
uses: total ................. 28
uses: pinados por SHA-40 .... 28
refs @vN restantes .......... 0 (grep sem match)
```

### Convencoes de hardening

```
permissions: top-level ...... 7/7 workflows
concurrency: ................ 7/7 workflows
harden-runner ............... 6/6 jobs ubuntu-latest
  (release.yml e Windows-only — harden-runner nao suporta Windows; skip correto)
persist-credentials: false .. 8/8 checkouts
```

### Validacao YAML (comando usado — actionlint indisponivel na maquina)

`python -c "import yaml,sys;yaml.safe_load(open(sys.argv[1],encoding='utf-8'))" <file>`

```
OK .github/dependabot.yml
OK .github/workflows/ci.yml
OK .github/workflows/codeql.yml
OK .github/workflows/dependency-review.yml
OK .github/workflows/release.yml
OK .github/workflows/scorecard.yml
OK .github/workflows/secret-scan.yml
OK .github/workflows/sonarqube.yml
```

### Os 10 Verify do CONTEXT.md (executados verbatim em bash)

```
DoD-1  slnx sem .idea + projetos preservados ............ PASS
DoD-2  CI build Windows TFM + test Linux + XPlat ........ PASS
DoD-3  CodeQL csharp + security-extended ................ PASS
DoD-4  dependabot.yml nuget + github-actions ............ PASS
DoD-5  dependency-review-action em pull_request ......... PASS
DoD-6  ossf/scorecard-action + schedule ................. PASS
DoD-7  gitleaks|trufflehog presente ..................... PASS
DoD-8  SHA pin + permissions + harden-runner + concurrency PASS
DoD-9  release em tag v* + action-gh-release ............ PASS
DoD-10 sonarscanner + SONAR_TOKEN ....................... PASS
```

## Files modified

- `TranslateReader.slnx`
- `README.md`
- `.github/dependabot.yml`
- `.github/workflows/ci.yml`
- `.github/workflows/codeql.yml`
- `.github/workflows/dependency-review.yml`
- `.github/workflows/scorecard.yml`
- `.github/workflows/secret-scan.yml`
- `.github/workflows/release.yml`
- `.github/workflows/sonarqube.yml`

## Tests

- Total: 171 (169 aprovados, 2 ignorados — skips pre-existentes de integracao LLM)
- Falhas: 0 — baseline preservado (acima dos 167 do D-2, sem regressao)
- `dotnet restore TranslateReader.slnx` OK apos o fix do T-1
- Phase infra-only: nenhum `.cs` novo -> Gate 3 (cobertura 90%) reporta SKIPPED, esperado

## Desvios

1. **gitleaks-action v3.0.0** (nao v2): v3 e o major estavel atual — mesma interface do v2,
   apenas migra o runtime pra Node 24 (v2 para de funcionar em runners GitHub em 2026-09).
   Repo pessoal publico -> sem necessidade de `GITLEAKS_LICENSE`.
2. **`GITLEAKS_ENABLE_COMMENTS: "false"`** no secret-scan: o job roda com `contents: read`
   (locked no PLAN) e comentario em PR exigiria `pull-requests: write` — desabilitado pra
   evitar 403 ruidoso; findings continuam falhando o job e gerando artifact SARIF.
3. **Versoes das actions**: pinadas nas latest stable no momento da resolucao (checkout v7.0.1,
   setup-dotnet v6.0.0, codeql-action v4.37.3 etc.) — o PLAN exemplificava v5.0.0 do checkout,
   mas a regra locked e "latest stable tag do major + SHA".
4. **Sonar `/k:`/`/o:`**: preenchidos com `slipalison_TranslateReader` / `slipalison` (padrao
   SonarCloud pro repo `github.com/slipalison/TranslateReader`); execucao real deferida
   (D-2026-07-28-ci-seguranca-6) — ajustavel quando a org for criada.
5. **T-8 sem commit**: auditoria nao encontrou nada pra corrigir (conforme nota do PLAN,
   evidencia registrada aqui).
