shipped_at: 2026-08-09T00:30:04Z
verdict: APPROVED_WITH_WARNINGS
by: Alison Amorim

## Learnings
- Don't set a numeric cap (e.g. "NoWarn <= 12") before measuring — a warning-ID inventory on a codebase that never had analyzers produces a long tail; the cap becomes an arbitrary blocker instead of a real signal. Measure first, decide the number after.
- A high raw warning count can be mostly two mis-calibrated rules (here: 1232 of 24 IDs' occurrences from 2 rules) rather than real debt. Before suppressing, ask per-ID: "if fixed instead of suppressed, would the code get worse?" — that separates calibration from dodging.
- When a task runs sequentially and one step blocks on a locked-decision conflict (not a code defect), later steps that depend on its outcome (process docs describing the new gate) can ship correct-per-DoD but describing the wrong end state if the blocked step gets unblocked out of band. Re-sync process docs after resolving a mid-phase decision amendment, don't just trust the earlier commit's prose.
- CRLF→LF renormalization is safe to verify empirically (diff --ignore-all-space/--ignore-cr-at-eol) rather than assumed — this phase touched ~118 verbatim/raw string literals across 12 files and stayed semantics-zero.
- `dotnet_diagnostic` severity in .editorconfig can be scoped narrowly by folder glob (e.g. `[test/**.cs]`) — use that instead of a repo-wide override when a rule's premise only fails in one project.
