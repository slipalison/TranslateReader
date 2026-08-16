# Translation model licenses

Every GGUF model offered in `SettingsOverlay` ("Modelo local") and resolvable by
`TranslationManager.ResolveModel`, with the license that actually governs it. Verified by reading
the license text at the source, not inferred from the model card summary
(`.jdi/decisions/D-2026-08-16-llm-mobile-2.md`).

## Hy-MT2-1.8B (default for new installs)

- **Name in registry:** `hy-mt2-1.8b`
- **File:** `Hy-MT2-1.8B-Q4_K_M.gguf` (1,133,080,448 bytes)
- **Source:** https://huggingface.co/tencent/Hy-MT2-1.8B-GGUF/resolve/main/Hy-MT2-1.8B-Q4_K_M.gguf
- **License: Apache-2.0.** No usage cap, no territorial exclusion, no restriction on training other
  models with its outputs.
- Same architecture family as HY-MT1.5 below (`hunyuan_v1_dense`, 32 layers, hidden 2048, vocab
  120818) — a drop-in replacement in this app's GGUF pipeline, at equal-or-better quality per the
  upstream model card, without HY-MT1.5's licensing restrictions. This is why it became the default
  for new installs: it is the only offered model with no strings attached.

## HY-MT1.5-1.8B (selectable, not the default)

- **Name in registry:** `hy-mt1.5-1.8b`
- **File:** `HY-MT1.5-1.8B-Q4_K_M.gguf`
- **Source:** https://huggingface.co/tencent/HY-MT1.5-1.8B-GGUF/resolve/main/HY-MT1.5-1.8B-Q4_K_M.gguf
- **License: Tencent HY Community License**
  (https://huggingface.co/tencent/HY-MT1.5-1.8B/raw/main/License.txt). Notable terms:
  - States explicitly: **"THIS LICENSE AGREEMENT DOES NOT APPLY IN THE EUROPEAN UNION, UNITED
    KINGDOM AND SOUTH KOREA"** — the app must not present this model as available/default for users
    in those regions.
  - Caps commercial use at 100M monthly active users.
  - Prohibits using this model's outputs to train other (non-Tencent-derived) models.
- Kept in the registry and selectable **only** because removing it would break the saved selection
  of anyone who already downloaded the 1.06 GB file — not because the license is unproblematic. No
  new install is steered toward it (`hy-mt2-1.8b` is the default; see above).

## Gemma 2 2B (legacy default, still selectable)

- **Name in registry:** `gemma-2-2b`
- **File:** `gemma-2-2b-it-Q4_K_M.gguf` (1,629,413,888 bytes)
- **Source:** https://huggingface.co/bartowski/gemma-2-2b-it-GGUF/resolve/main/gemma-2-2b-it-Q4_K_M.gguf
- **License: Gemma Terms of Use** (Google). Permissive for this app's use case (local, on-device
  inference, no redistribution of model weights), but not OSI-approved — it carries Google-specific
  use restrictions (see the Gemma Prohibited Use Policy) that Apache-2.0 does not.
- Was the default for new installs before this phase (`ReadingSettings.cs`, `SettingsAccess.cs`
  both defaulted `TranslationModelName` to `gemma-2-2b`). Kept in the registry, and kept as
  `TranslationManager.ResolveModel`'s fallback target for any unrecognized/legacy settings value
  (`qwen-2.5-3b`, `phi-3.5`, or anything else the UI ever wrote), so existing installs never get
  silently redirected to a different multi-gigabyte download.

## Not real downloads yet

`qwen-2.5-3b` and `phi-3.5` are UI placeholders in `SettingsOverlay` with no entry in
`TranslationManager`'s model registry — selecting them in the UI does not change what actually gets
downloaded; `ResolveModel` falls back to `gemma-2-2b` for any name it does not recognize. Tracked
pre-existing gap, not something this phase introduces or fixes.
