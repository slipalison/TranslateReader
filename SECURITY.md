# Security Policy

## Supported Versions

TranslateReader is under active development. Security fixes are applied to the
`main` branch only; there are no maintained release branches.

| Version        | Supported |
| -------------- | --------- |
| `main` (latest) | Yes      |
| Anything else  | No        |

## Reporting a Vulnerability

Please report vulnerabilities **privately** through GitHub Security Advisories:

1. Open the repository's **Security** tab.
2. Choose **Advisories** → **Report a vulnerability** (new draft advisory), or go
   directly to
   <https://github.com/slipalison/TranslateReader/security/advisories/new>.
3. Fill in the draft with the details below.

**Do not open a public issue for security problems.** Public issues expose users
before a fix exists; privately reported advisories allow coordinated disclosure.

### What to include

- A description of the vulnerability and its impact.
- Steps to reproduce (a proof-of-concept EPUB file or code snippet helps a lot).
- Affected component if known (EPUB parsing, WebView bridge, model download,
  local database, CI/build pipeline).
- Any suggested remediation.

### Response targets

| Stage                              | Target        |
| ---------------------------------- | ------------- |
| Acknowledgement of the report      | within 72 hours |
| Initial triage and severity assessment | within 7 days |
| Fix or mitigation plan communicated | within 30 days |
| Coordinated disclosure             | after a fix is available, agreed with the reporter |

## Scope

Reports are especially welcome for the areas this application treats as
security-sensitive:

- **EPUB files as untrusted input** — zip extraction (path traversal, resource
  exhaustion) and XML parsing (XXE) of attacker-crafted books.
- **WebView JavaScript bridge** — injection of book-derived content into the
  reader WebView.
- **GGUF model download** — integrity of downloaded translation models.
- **Local data** — reading state and settings stored in the local SQLite
  database.

## Out of scope

- There is **no bug bounty program**; reports are handled on a best-effort basis.
- Vulnerabilities in third-party dependencies should be reported upstream to the
  affected project; a report here is still useful when this application's usage
  of the dependency is what makes it exploitable.
- Findings that require a compromised device or physical access to the user's
  machine.
