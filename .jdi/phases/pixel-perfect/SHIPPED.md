shipped_at: 2026-08-02T19:11:23Z
verdict: APPROVED_WITH_WARNINGS
by: Alison Amorim

## Learnings
- Reviewer "impossible in MAUI" claims need a doc/source check before being accepted as final — 2 items in this phase (Setter.TargetName for VisualStates, BackButtonBehavior.IconOverride) were wrongly declared limitations in earlier reviews and turned out to be real, documented APIs.
- A `Border.StrokeThickness="a,b,c,d"` CSS-style value silently parses as a wrong single double (NumberStyles.Any treats commas as thousand separators) instead of failing the build — grep for comma-separated values on any MAUI `double`-typed XAML attribute during redesigns.
- Implicit Button/Entry styles' Padding and MinimumHeightRequest/MinimumWidthRequest floors can silently clip or overflow a control resized smaller in a specific screen — always re-check inherited style defaults when shrinking a styled control's footprint.
- Live screenshots (build+launch+PowerShell GDI+ capture scoped to the app window) find real bugs mockup/code review can't (e.g. text overlay illegible on real photo content vs. only-ever-tested placeholder gradients) — worth the setup cost on visually-driven phases. Guard window-rect capture against maximize/multi-monitor DWM edge cases before using it.
- Some warnings are structurally unfixable within a phase's own locked scope boundary (Core-untouched here) — don't chase zero-warning by silently violating the boundary; surface the tradeoff explicitly and let the human decide.
