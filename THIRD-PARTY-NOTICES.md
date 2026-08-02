# Third-Party Notices

TranslateReader can download and run translation models that are not developed or distributed
by this repository. The model itself is never bundled with the app or the source tree; it is
downloaded to the device by the user at runtime, on demand, from the vendor's own hosting.
This file lists the license obligations that apply to each such model.

## Tencent HY (hy-mt1.5-1.8b)

- **Artifact:** [`tencent/HY-MT1.5-1.8B-GGUF`](https://huggingface.co/tencent/HY-MT1.5-1.8B-GGUF)
  repository on Hugging Face, file `HY-MT1.5-1.8B-Q4_K_M.gguf`.
- **License:** [Tencent HY Community License Agreement](https://huggingface.co/tencent/HY-MT1.5-1.8B-GGUF/blob/main/License.txt),
  Copyright (c) 2025 Tencent. This app does not redistribute the model file; it only points the
  user's device at the vendor's download URL. The clauses below are the ones that apply to a
  downstream application that lets a user download and run the model.

### Territorial exclusion

> THIS LICENSE AGREEMENT DOES NOT APPLY IN THE EUROPEAN UNION, UNITED KINGDOM AND SOUTH KOREA

The Tencent HY Community License Agreement does not grant rights to use, reproduce, modify,
distribute, or display the Tencent HY works to recipients located in the **European Union**,
**United Kingdom**, or **South Korea**. TranslateReader does not implement geo-gating for this
model (no location infrastructure exists in the app today); users in those territories who
select the hy-mt1.5-1.8b model are outside the license grant. See `.jdi/todos/` for the tracked
follow-up on this residual risk.

### Attribution

Per the license, downstream products built with Tencent HY works are encouraged to mark
themselves as **"Powered by Tencent HY"**. TranslateReader displays this attribution next to the
hy-mt1.5-1.8b model option in Settings.

### Non-affiliation

TranslateReader is **not affiliated** with, associated with, sponsored by, or endorsed by
Tencent. TranslateReader is the actual provider of the reading and translation features in this
app; Tencent HY is only the underlying model that the user may choose to download.

### License copy and notice file

Per the license, any distribution of the Tencent HY works (or works built with them) must be
accompanied by a copy of the license agreement and a notice file containing:

> Tencent HY is licensed under the Tencent HY Community License Agreement, Copyright (c) 2025 Tencent.

This file (`THIRD-PARTY-NOTICES.md`), together with the linked
[License.txt](https://huggingface.co/tencent/HY-MT1.5-1.8B-GGUF/blob/main/License.txt), serves
that purpose for this repository.
