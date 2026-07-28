---
name: csharp
description: "usar quando for gerar codigos em csharp"
---

# C# Rule — TranslateReader (MANDATORY for all AI-generated C#)

Applies to EVERY C# change in this repo (Copilot, Claude Code, Gemini, OpenCode, any assistant).
This is a .NET MAUI app (Windows/Android/iOS), not a server. The hot paths are: EPUB parsing
(`ParsingEngine`), HTML manipulation (`HtmlUtility`), LLM inference and token streaming
(`TranslationEngine`), chapter/paragraph translation loops, and SQLite row loops. UI code,
DI wiring, and tests optimize for readability, not allocation.
Priority when rules conflict: **1) Security 2) Performance 3) Clean Code / SOLID / DRY / YAGNI**.
Complements `CLAUDE.md` — The Method layer rules apply to every change and are not restated here.

## 1. Flow control — fail fast with exceptions

- Per `CLAUDE.md`: **fail fast, use exceptions, never return null** to signal failure. Do not
  introduce Result/Try patterns for error flow — that contradicts the locked convention.
- Exceptions signal *errors*, never expected control flow: don't throw-and-catch inside your own
  call chain to make a decision, don't branch logic in a `catch`, never `catch { }` (log or comment).
- Exactly ONE boundary converts exceptions to user-facing state: the PageModel `[RelayCommand]`
  method. It catches, sets a friendly error message/state, and never leaks stack traces to the UI.
  Managers/Engines/Access let exceptions propagate.
- `OperationCanceledException` always flows — never swallow it, never convert it to an error state.

## 2. Allocation, memory & GC — parsing and inference paths

Three principles, in priority order: **① don't allocate if you can avoid it ② reuse memory over
allocating new ③ stack beats heap.** Applies to per-chapter/per-paragraph/per-token code.
**Measure before optimizing** (BenchmarkDotNet, `dotnet-counters`, `dotnet-gcdump`); don't
micro-optimize UI glue on intuition (YAGNI).

### 2.1 Strings — EPUB/HTML processing is string-heavy

- Compare without allocating: `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)`, never
  `ToLower()==`. `Ordinal` for hrefs/keys/hashes, `OrdinalIgnoreCase` for tags/attributes;
  culture-aware only for user-facing sort.
- Test prefixes/suffixes on spans: `path.AsSpan().StartsWith("file:", StringComparison.Ordinal)`,
  `value.StartsWith('[')` (char overload). No `Substring` just to compare.
- Slice/parse chapter content with `ReadOnlySpan<char>`; avoid `Substring`/`Split` inside parsing
  loops (.NET 8+ `AsSpan().Split(',')` enumerates zero-alloc). Spans cannot cross `await`, be
  returned, or stored — use `ReadOnlyMemory<char>` for async lifetime.
- Building HTML/prompts: pre-sized `StringBuilder` for loops or 5+ parts; a single `$"{a}-{b}"`
  is fine. Never `+=` a string in a loop over paragraphs/tokens.
- Repeated literal 3+ times → `const`. Fixed lookup sets → `static readonly FrozenSet`/`FrozenDictionary`.
  Compile-time-known regex → `[GeneratedRegex]` partial method, never `new Regex(...)` per call.
  Never `string.Intern()`.

### 2.2 Reduce GC pressure

- Small, short-lived, immutable data → `readonly record struct` (~≤16–24 bytes); pass large structs
  by `in`. Never box a struct in a loop (no `object`/interface calls on it).
- No finalizers. `IDisposable` + `using`; `SafeHandle` for native handles (LLamaSharp contexts are
  already wrapped — dispose them deterministically).
- Pool transient large buffers: `ArrayPool<byte>.Shared.Rent/Return` in `try/finally` for EPUB entry
  and image buffers. `RecyclableMemoryStream` (or streaming) over `new MemoryStream()` for zip/image
  payloads.
- No capturing closures in per-token/per-paragraph loops: `static` lambdas, cached `static readonly`
  delegates.

### 2.3 Large Object Heap — EPUBs, images, GGUF models

Any single allocation ≥ 85,000 bytes (byte[], big string ≥ ~42,500 chars, grown `List<T>`/`StringBuilder`
backing array) lands on the LOH: collected only by full GC, never compacted by default → fragmentation
→ `OutOfMemoryException` on mobile. Rules:

- **Never materialize a GGUF model, full EPUB, or image into one `byte[]`/`string`.** Stream:
  `Stream.CopyToAsync` with pooled buffers for model downloads, per-entry streams for EPUB zip,
  `JsonSerializer.DeserializeAsync(stream, …)` for JSON.
- Chapter HTML can exceed the string LOH threshold — process per-node/per-paragraph, don't
  concatenate a whole translated book into one string; write to the output EPUB entry stream.
- Pre-size collections (`new List<T>(n)`, `new Dictionary<K,V>(n)`, `new StringBuilder(n)`) so growth
  doubling never emits large intermediate arrays; segment inherently huge data.

### 2.4 Memory leaks — reachability (MAUI's #1 failure mode)

- **Every `+=` needs a `-=`.** A page/control subscribing to a long-lived publisher (Manager event,
  `WeakReferenceMessenger`, timer, static event) roots the page after navigation. Subscribe in
  `OnAppearing`/ctor, unsubscribe in `OnDisappearing`/`Dispose`, same delegate instance. Prefer
  messenger unregister or `Channel<T>` over raw events across DI lifetimes.
- `static` mutable state is a process-lifetime GC root — statics are `static readonly` and immutable.
- Every in-memory cache is bounded (size cap + eviction/TTL) or it is a leak. The SQLite
  `TranslationCache` is fine; any in-memory dictionary cache must not only ever `Add`.
- Dispose what you own: streams, DB connections, `SemaphoreSlim`, `CancellationTokenSource`
  (and `token.Register(...)` registrations on long-lived tokens), timers. Never dispose what DI owns.
- Diagnose: heap climbing across GCs that never drops = leak; diff two `dotnet-gcdump` snapshots.

## 3. Concurrency — background work + one UI thread

- **UI state changes only on the main thread.** `[ObservableProperty]` setters bound to XAML fire
  `PropertyChanged`; from background work (translation jobs, downloads) marshal via
  `MainThread.BeginInvokeOnMainThread` (or ensure the continuation is on the UI context).
- **Never sync-over-async** (`.Result`, `.Wait()`, `GetAwaiter().GetResult()`) — on MAUI this
  deadlocks the UI thread outright. `async` end-to-end; `CancellationToken` flows
  PageModel → Manager → Engine → Access.
- One-time expensive init (GGUF model load) → `SemaphoreSlim(1,1)` with `WaitAsync`/`Release` in
  `try/finally`, or `Lazy<Task<T>>`. Never `await` inside a `lock`.
- Shared progress/counters → `Interlocked.Increment/Add/Exchange`; multi-field shared state → build
  an immutable snapshot and publish with `Interlocked.Exchange`, don't mutate in place.
- Prefer ready-made primitives: `ConcurrentDictionary` (side-effect-free factories or `Lazy<T>`
  values), `Channel<T>` for producer/consumer (token streaming → UI). Ambient per-flow context via
  `AsyncLocal<T>`, never `[ThreadStatic]`.

## 4. Security — priority 1, overrides everything

- **EPUB files are untrusted input.** Extract zip entries defensively: reject entry paths that
  escape the target directory (`..`, absolute paths, drive letters) — zip-slip. Bound decompressed
  sizes. Parse XML with DTD processing disabled (`DtdProcessing.Prohibit`, no `XmlResolver`) — XXE.
- **Book HTML rendered in WebView is untrusted.** Never inject book-derived strings into JS without
  encoding; keep the WebView bridge surface minimal; virtual-host URLs only for local content.
- No user data (book titles/content, file paths under the user profile, reading history) in logs or
  exception messages beyond what debugging needs; never in test fixtures copied from real users.
- Validate external input at the boundary (type, length, range, whitelist); reject, don't
  sanitize-and-continue. User-facing errors are generic; no stack traces or internals.
- No secrets committed, logged, or defaulted. Model download URLs are constants, verified by
  size/hash where available. `RandomNumberGenerator` for anything security-sensitive.

## 5. Logging — only what is consumed

- Log decision points and failures, not play-by-play. Never log per paragraph/token inside
  translation or parsing loops.
- Message templates over interpolation where the logging API supports it; no book content in logs.

## 6. Tests — 90% on new/changed code (D-2 boundary)

- New/changed code after commit `4285f25` ships unit tests in the same PR: ≥90% line coverage,
  covering success, failure/exception, and cancellation/edge paths. Legacy code is exempt
  (`.jdi/DECISIONS.md` D-2); the 167 existing tests are baseline and must not regress.
- Isolated: no network/disk/real SQLite in unit tests. NSubstitute for contracts (interfaces only —
  never mock concretes), xUnit patterns already in `test/TranslateReader.Tests`.
- A bugfix starts with a failing test reproducing it.

## 7. Style — priority 3, never traded against 1–2

- Per `CLAUDE.md`: functions ≤ 20 lines preferred; CQS (a method changes state OR returns data,
  never both); no deodorant comments — if it needs a comment, refactor; a single WHY line for a
  non-obvious constraint is acceptable. No commented-out code. No TODO without a work item.
- Public contracts (`Contracts/`) get `<summary>`. Guard clauses over nesting; ≤7 ctor params.
- Match surrounding code (naming, layout, patterns) and run `dotnet format` before commit.
