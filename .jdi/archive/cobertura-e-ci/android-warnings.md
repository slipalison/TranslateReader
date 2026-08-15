# W-2 — Android warning measurement (`Build (Android)`, `TreatWarningsAsErrors=true`)

Measured, not guessed (D-2026-08-08-cobertura-e-ci-5(3)): this is the FIRST real run of the
`Build (Android)` job in this repository's CI — there is no local Android SDK, so a PR run was
the only way to get this number.

- Run: https://github.com/slipalison/TranslateReader/actions/runs/31317291066
- Job: https://github.com/slipalison/TranslateReader/actions/runs/31317291066/job/93254482136
- PR: https://github.com/slipalison/TranslateReader/pull/28
- Head SHA measured: `337e10ae32ad9f75037c062a5cf6d180fa61c8f9`
- Job conclusion: **success**

## Result

The job passed, but "job passed" is not the same question as "zero warning IDs fired" —
`TreatWarningsAsErrors=true` governs the C# compiler and analyzers, not every MSBuild task in
the Android packaging pipeline. Reading the actual job log (`gh run view ... --log`) surfaced one
Android-toolchain warning that a green build alone would have hidden:

- XA4301 (2x: `arm64-v8a` and `x86_64`) — "APK already contains the item
  lib/<abi>/libe_sqlite3.so; ignoring." Emitted by the APK packaging step (not the C# compiler,
  which is why `TreatWarningsAsErrors=true` did not turn it into a build failure).

Not RISCO: `Microsoft.Data.Sqlite.Core` and the explicit `SQLitePCLRaw.bundle_green` reference
both ship the same native `e_sqlite3` binary; the Android linker keeps one copy and ignores the
duplicate at pack time. Nothing is missing from the APK and no behavior changes.

Preference order applied (D-2026-08-08-cobertura-e-ci-5(3), option (i) — single `<NoWarn>` with
its own comment line, no `TreatWarningsAsErrors=false` needed since the ID never made the build
fail in the first place): `XA4301` added to the existing closed, per-ID `<NoWarn>` in
`Directory.Build.props`, with its own comment block. `TreatWarningsAsErrors` stays `true` at the
root, unconditioned, unchanged.

## Invariants preserved (from `baseline-de-estilo`)

- Single `<NoWarn>` element in `Directory.Build.props` — no second list, no wildcard.
- `XA4301` appears in >= 2 lines of the file (the `<NoWarn>` value and its own comment entry).
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` unchanged at the root.
