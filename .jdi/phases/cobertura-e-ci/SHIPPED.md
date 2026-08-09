shipped_at: 2026-08-09T14:34:12Z
verdict: APPROVED_WITH_WARNINGS
by: Alison Amorim

## Learnings
- Before any decision derives a GitHub check name from workflow YAML (`name:` fields), confirm it empirically via `gh pr checks`/`gh api` — the derivation formula can diverge from what GitHub actually reports, and using an unconfirmed name in a branch-protection PATCH reproduces the pipeline-unificada incident.
- A green CI job does not prove zero warnings when `TreatWarningsAsErrors` only governs the compiler — MSBuild packaging tasks (e.g. Android APK steps) can still emit warnings; read the actual job log before declaring a measurement clean.
- When a coverage/analysis tool lacks native threshold support for the metric you actually need (e.g. `coverlet.collector` has no `Threshold`), don't force a mismatched built-in flag — a small versioned script measuring the real thing beats a wrong number with a rigorous-looking flag.
- Gate 2's documented bare `dotnet test` command is environment-fragile (fails on dev boxes without Android/iOS SDKs) — scope it to the test project like Gate 1 and `scripts/coverage-gate.sh` already do; still unfixed, flagged for whichever phase next touches that gate.
- `jdi-reviewer-translatereader.md` Gate 5.12's mutable-static baseline comment ("exactly 1 hit") is stale since `pixel-perfect` — whichever phase next touches that gate section should refresh it.
