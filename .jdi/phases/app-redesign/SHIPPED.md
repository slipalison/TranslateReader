shipped_at: 2026-08-02T05:26:52Z
verdict: APPROVED_WITH_WARNINGS
by: Alison Amorim

## Learnings
- A DoD grep Verify: is prescriptive — if a doer widens a test's exception list to pass, re-run the literal Verify: command before trusting SUMMARY.md; the fix belongs in the source token/artifact, not in the test's denylist.
- Content hexes that coincide with legacy chrome hexes (reading-theme swatches) need their own named design tokens (not literal hex) or a legacy-hex sweep produces false positives/negatives.
- Any handler that mutates observable state from an unthrottled per-keystroke command (e.g. search) needs a generation/version guard against out-of-order completion, covering both the data write and any busy/loading flag.
- Unscoped 'dotnet format' touches unrelated legacy files; scope format verification to the files the task actually changed.
- Per-project doer/reviewer specialists live in .jdi/agents/, not .claude/agents/ — this runtime does not expose them as Agent subagent_type; invoke by loading the specialist file as role context into a general-purpose agent call instead.
