---
name: the-method-design
description: >
  System and project design rules based on "The Method" (Juval Löwy) combined with SOLID, Clean Code, DRY, and YAGNI.
  Use this skill for: architecting new systems, decomposing services, validating architectures, reviewing code for design violations,
  identifying functional/domain decomposition anti-patterns, classifying services (Manager/Engine/ResourceAccess/Utility),
  layering enforcement, call-chain validation, and service contract design.
  Trigger on: "architecture review", "system design", "decomposition", "The Method", "volatility-based", "service taxonomy",
  "Manager/Engine pattern", "closed architecture", "design validation", "code review", "design standard", any request
  to structure, decompose, or validate a software system or codebase. Also trigger when the user asks to check SOLID,
  Clean Code, DRY, YAGNI compliance, or when reviewing pull requests and code quality.
---

# The Method — System & Project Design Standard

> **Prime Directive:** Never design against the requirements.

---

## 1. DECOMPOSITION

### 1.1 What to AVOID

| Anti-Pattern | Why it Fails |
|---|---|
| **Functional decomposition** | Couples services to requirements; any change ripples across system; precludes reuse; bloats clients with orchestration logic |
| **Domain decomposition** | Functional decomposition in disguise (Kitchen = Cooking); duplicates logic across domains; internal complexity grows; cross-domain composition is painful |
| **Designing against requirements** | Requirements are incomplete, contradictory, and will change. Coupling design to them guarantees rework |

### 1.2 Volatility-Based Decomposition (DIRECTIVE)

Decompose based on **volatility**: identify areas of potential change and encapsulate each into a service. Implement required behavior as interaction between encapsulated volatilities.

**Mental model:** each service is a vault — toss the change-grenade inside, close the door, no shrapnel outside.

#### Identifying Volatility

1. **Axes of volatility** — same customer over time; all customers at same time.
2. **Design for competitors** — if your competitor can't reuse your system, list the barriers → those are volatilities.
3. **Longevity heuristic** — things unchanged for N years will likely change again in ~N years. Encapsulate anything expected to change within system lifespan.
4. **Solutions masquerading as requirements** — "send an email" → real requirement is "notify"; "use a local DB" → real requirement is "store data". Strip the solution; encapsulate the volatility.

#### What NOT to Encapsulate

- **Nature of the business** — rare change + any attempt to encapsulate it is done poorly = don't encapsulate.
- **Speculative design** — if it violates both indicators (rare + poor encapsulation), it's speculation. Avoid.

---

## 2. STRUCTURE (Service Taxonomy)

### 2.1 Four Layers

```
┌─────────────────────────────────────────────────┐
│  CLIENT LAYER          │  UTILITIES (vertical)  │
│  Client A, Client B…   │  Security              │
├────────────────────────│  Logging               │
│  BUSINESS LOGIC LAYER  │  Diagnostics           │
│  Managers → Engines    │  Pub/Sub               │
├────────────────────────│  Message Bus           │
│  RESOURCE ACCESS LAYER │  Hosting               │
│  ResourceAccess A…     │  …                     │
├────────────────────────│                        │
│  RESOURCE LAYER        │                        │
│  DB, Queue, File…      │                        │
└─────────────────────────────────────────────────┘
```

### 2.2 Service Types

| Type | Role | Encapsulates |
|---|---|---|
| **Manager** | Orchestrates use-case sequences (workflows). Entry point for Clients into business logic. | Volatility in the **sequence** of activities |
| **Engine** | Executes a volatile business activity/rule. Shared across Managers. Strategy pattern. | Volatility in the **activity** itself |
| **ResourceAccess** | Abstracts underlying resource. Never exposes storage tech to upper layers. | Volatility in **storage/access mechanism** |
| **Utility** | Cross-cutting infrastructure. Must pass cappuccino-machine test (usable in ANY system). | Volatility in **infrastructure concerns** |

### 2.3 Naming Convention

- Two-part Pascal-case compound: `[Noun][Type]`
- Managers: noun = use-case family → `TradeManager`, `OrderManager`
- Engines: noun = business-rule verb → `PricingEngine`, `MatchingEngine`
- ResourceAccess: noun = resource → `TradesAccess`, `CustomersAccess`

### 2.4 Cardinality Rules

- ≤5 Managers per system (without subsystems); ≤3 per subsystem.
- Golden ratio Engines:Managers ≈ (M-1):1. 2M→1E, 3M→2E, 5M→3E. If 8+ Managers → decomposition failure.
- Total building blocks ≈ order of magnitude 10 (12–20 typical).
- Service contracts: 3–5 operations optimal. >12 is red flag. ≥20 reject immediately.
- ≤2 contracts per service.

---

## 3. ARCHITECTURE RULES

### 3.1 Closed Architecture (DIRECTIVE)

| Rule | Detail |
|---|---|
| **No calling UP** | Lower layers never call higher layers. Worst violation. |
| **No calling SIDEWAYS** | Except queued Manager→Manager calls. Manager→Manager sync = functional decomposition. |
| **No skipping layers** | Call only the adjacent lower layer (semi-open allowed only for extreme perf infra). |
| **Resolve violations** | Up-call → use Pub/Sub Utility. Sideways → use queued call or event. |

### 3.2 Interaction Matrix

| Caller → | Manager | Engine | ResAccess | Utility | Client | Resource |
|---|---|---|---|---|---|---|
| **Client** | ✅ (1 per use case) | ❌ | ❌ | ✅ | — | ❌ |
| **Manager** | ✅ queued only | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Engine** | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ |
| **ResAccess** | ❌ | ❌ | ❌ | ✅ | ❌ | ✅ |

### 3.3 Design Don'ts

1. Clients do NOT call multiple Managers in the same use case.
2. Managers do NOT queue to >1 Manager in the same use case (use Pub/Sub instead).
3. Engines do NOT receive queued calls.
4. ResourceAccess does NOT receive queued calls.
5. Clients, Engines, ResourceAccess, Resources do NOT publish events (only Managers publish).
6. Engines, ResourceAccess, Resources do NOT subscribe to events.
7. Never use public/external protocols (HTTP/REST) for internal service communication. Use TCP, IPC, named pipes, queues, etc.

---

## 4. COMPOSITION & VALIDATION

### 4.1 Composable Design (DIRECTIVE)

- Identify **core use cases** (2–6 max; the essence of the business).
- Find the **smallest set** of components that satisfies ALL core use cases.
- All other use cases = different interactions between the SAME components, not new components.
- **When requirements change, decomposition does NOT change** — only Manager integration code changes.

### 4.2 Validation Technique

For each core use case, produce a **call chain** or **sequence diagram** superimposed on the layered architecture. If every core use case maps to a valid interaction → design is valid.

### 4.3 Symmetry

Strive for symmetry in the architecture: similar problems handled by similar patterns. Inconsistency signals missed volatility.

---

## 5. SOLID PRINCIPLES (DIRECTIVE)

| Principle | Rule |
|---|---|
| **S — Single Responsibility** | Each class/service has ONE reason to change. Aligns with volatility encapsulation. |
| **O — Open/Closed** | Open for extension, closed for modification. Use abstractions and Strategy (Engine pattern). |
| **L — Liskov Substitution** | Subtypes must be substitutable for base types without breaking behavior. Contract factoring (up/down/sideways) must preserve this. |
| **I — Interface Segregation** | No client should depend on methods it doesn't use. Service contracts = cohesive, independent facets (3–5 ops). Factor down/sideways when violated. |
| **D — Dependency Inversion** | Depend on abstractions, not concretions. Upper layers depend on contracts (interfaces), never on implementations. |

---

## 6. CLEAN CODE (DIRECTIVE)

1. **Meaningful names** — intention-revealing; no abbreviations; domain vocabulary.
2. **Small functions** — do ONE thing; ≤20 lines preferred; one level of abstraction per function.
3. **No side effects** — functions do what the name promises, nothing else.
4. **Command-Query Separation** — methods either change state OR return data, not both.
5. **Error handling** — use exceptions, not return codes; don't return null; fail fast.
6. **No comments as deodorant** — if you need a comment, refactor the code to be self-explanatory. Only use comments for WHY, never for WHAT.
7. **Boy Scout Rule** — leave code cleaner than you found it.
8. **Tests** — fast, independent, repeatable, self-validating, timely. Test one concept per test.

---

## 7. DRY — Don't Repeat Yourself (DIRECTIVE)

- Every piece of knowledge has a single, unambiguous, authoritative representation.
- Duplication in logic → extract to shared Engine or Utility.
- Duplication in data access → extract to ResourceAccess.
- Duplication in orchestration → likely a missing Manager.
- **Caveat:** DRY applies to knowledge, not to code that looks similar but represents different concepts. Don't force abstraction where concerns are independent.

---

## 8. YAGNI — You Aren't Gonna Need It (DIRECTIVE)

- Do NOT implement speculative features.
- Do NOT encapsulate changes to the nature of the business.
- Do NOT create components without identified volatility (= functional decomposition).
- The architecture accommodates future change via composability, NOT via speculative implementation.
- If volatility is not mapped to an axis of volatility → don't create a building block for it.

---

## 9. REVIEW CHECKLISTS

### 9.1 Architecture Review (for Validator)

- [ ] No functional or domain decomposition detected
- [ ] Every service encapsulates an identified volatility
- [ ] ≤5 Managers; golden ratio Engines:Managers respected
- [ ] Total components in order-of-magnitude 10
- [ ] Closed architecture: no up-calls, no sync sideways, no layer-skipping
- [ ] Interaction matrix respected (§3.2)
- [ ] All Design Don'ts (§3.3) verified
- [ ] Core use cases identified (2–6); each validated with call chain/sequence
- [ ] Composable: requirements change ≠ decomposition change
- [ ] Symmetry check: similar problems → similar patterns
- [ ] Internal comms use appropriate protocols (not HTTP between internal services)
- [ ] Utilities pass cappuccino-machine test

### 9.2 Code Review (for Validator)

- [ ] **SOLID** — each class has single responsibility; depends on abstractions; interfaces are segregated; open for extension
- [ ] **Clean Code** — small functions; meaningful names; no side effects; CQS; proper error handling
- [ ] **DRY** — no duplicated business knowledge; shared logic extracted
- [ ] **YAGNI** — no speculative code; no unused abstractions; no over-engineering
- [ ] **Service contracts** — 3–5 ops per contract; no property-like ops; ≤2 contracts per service
- [ ] **Naming** — Pascal-case two-part compound for services; intention-revealing for classes/methods
- [ ] **Layering** — code respects layer boundaries; no imports from upper layers; no circular dependencies
- [ ] **Tests** — unit tests for Engines/ResourceAccess; integration tests for Manager workflows; no test coupling

### 9.3 Quick Smell Detection

| Smell | Likely Violation |
|---|---|
| Client orchestrating multiple services sequentially | Functional decomposition; missing Manager |
| Service name matches a requirement/feature name | Functional decomposition |
| God class / god service (>12 ops or high cyclomatic complexity) | Missing decomposition; too few services |
| Explosion of tiny services | Over-decomposition; too many services |
| Service calling up a layer | Open architecture violation |
| Manager calling Manager synchronously | Sideways call violation |
| Business logic in Client layer | Missing Manager or Engine |
| ResourceAccess exposing storage technology (SQL in interface) | Leaky abstraction |
| Identical logic in multiple services | DRY violation; missing Engine or Utility |
| Feature flag / switch for unbuilt feature | YAGNI violation |

---

## 10. SERVICE CONTRACT DESIGN

### 10.1 Attributes of Good Contracts

Contracts must be: **logically consistent**, **cohesive**, and **independent** facets → which makes them **reusable**.

### 10.2 Factoring Techniques

| Technique | When |
|---|---|
| **Factor DOWN** | Operation doesn't belong in base contract → push to derived contract |
| **Factor SIDEWAYS** | Unrelated operations in same contract → extract to new independent contract |
| **Factor UP** | Identical operations across unrelated contracts → extract to base contract |

### 10.3 Metrics

| Metric | Value |
|---|---|
| Ops per contract | 3–5 optimal; 6–9 acceptable; ≥12 refactor; ≥20 reject |
| Contracts per service | 1–2 max |
| Property-like ops | Avoid; use behavioral verbs (`DoSomething()`) |
| Single-op contracts | Red flag; investigate |

---

## 11. CONDENSED DESIGN STANDARD (from Appendix C)

### Directives (NEVER violate)

1. Avoid functional decomposition
2. Decompose based on volatility
3. Provide composable design
4. Offer features as aspects of integration, not implementation
5. Design iteratively, build incrementally
6. Design the project to build the system
7. Drive educated decisions with viable options (schedule, cost, risk)
8. Build along critical path
9. Be on time throughout

### Key System Design Guidelines

- Capture required **behavior** (use cases), not functionality
- Eliminate solutions masquerading as requirements
- Validate design supports all core use cases
- Volatility decreases top-down; reuse increases top-down
- Managers should be almost expendable (thin orchestration)
- Design should be symmetric
- Prefer closed architecture; extend via subsystems

---

## REFERENCE FILES

Read these on demand — do NOT load upfront to save context tokens.

| File | When to Read |
|---|---|
| `references/IMPLEMENTATION-PATTERNS.md` | When implementing services, writing code, or showing code examples. Contains: project structure, Manager/Engine/ResourceAccess/Utility code patterns, contract factoring examples, anti-pattern code samples, Clean Code patterns, DRY refactoring decision tree. |
| `references/VALIDATION-WORKFLOW.md` | When reviewing architecture, reviewing PRs, or validating code. Contains: 5-phase architecture validation workflow, per-PR code review checklist (5 steps), severity classification, automated linting rules, common review rebuttals. |
