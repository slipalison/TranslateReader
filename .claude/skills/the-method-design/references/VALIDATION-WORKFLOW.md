# Validation & Review Workflow

> Read this when performing architecture reviews, code reviews, or PR validations.

---

## 1. ARCHITECTURE VALIDATION WORKFLOW

Execute in this order (design decision tree). Stop and fix before advancing.

### Phase 1: Decomposition Audit

```
1. List all services/modules in the system
2. For EACH service, answer:
   ├─ What VOLATILITY does it encapsulate?
   │   ├─ If answer is a feature/function → RED FLAG: functional decomposition
   │   ├─ If answer is a domain entity → RED FLAG: domain decomposition
   │   └─ If answer is "it could change because..." → ✅ volatility identified
   │
   ├─ Does the service name match a requirement or feature?
   │   └─ YES → RED FLAG (e.g., "ReportingService", "BillingService")
   │
   └─ Can you replace the implementation without touching other services?
       └─ NO → coupling detected, decomposition failure
```

### Phase 2: Taxonomy Classification

```
For EACH service, classify:
┌─────────────────────────────────────────────────────────┐
│ Does it orchestrate a sequence of activities?           │
│  YES → MANAGER. Verify:                                │
│   - No business rules inside (delegate to Engines)      │
│   - No direct DB access (delegate to ResourceAccess)    │
│   - Name: [Noun]Manager                                 │
│   - Encapsulates a FAMILY of use cases                  │
├─────────────────────────────────────────────────────────┤
│ Does it execute a volatile business rule/algorithm?     │
│  YES → ENGINE. Verify:                                 │
│   - Does not orchestrate other services                 │
│   - Can be shared across Managers                       │
│   - Name: [Verb/Rule]Engine                             │
│   - Strategy pattern applicable                         │
├─────────────────────────────────────────────────────────┤
│ Does it abstract access to a resource (DB/API/file)?   │
│  YES → RESOURCEACCESS. Verify:                         │
│   - Interface reveals NO storage technology             │
│   - Name: [Resource]Access                              │
│   - No business logic inside                            │
├─────────────────────────────────────────────────────────┤
│ Is it cross-cutting infrastructure?                     │
│  YES → UTILITY. Verify:                                │
│   - Passes cappuccino-machine test                      │
│   - No domain-specific logic                            │
│   - Name: [Concern]Utility or standard name             │
├─────────────────────────────────────────────────────────┤
│ None of the above?                                      │
│  → Investigation needed. Likely misclassified or        │
│    functional decomposition in disguise.                │
└─────────────────────────────────────────────────────────┘
```

### Phase 3: Cardinality Check

| Check | Threshold | Action if Violated |
|---|---|---|
| Managers in system | ≤5 (no subsystems) | Split into subsystems or investigate functional decomp |
| Managers per subsystem | ≤3 | Merge related Managers or extract subsystem |
| Engines : Managers ratio | ≈ (M-1):1 | Too many Engines = functional decomp; too few = rules leaked to Managers |
| Total components | ~10–20 | Too few = monolith; too many = over-decomposition |
| Ops per contract | 3–5 ideal; ≥12 refactor | Factor down/sideways/up |
| Contracts per service | ≤2 | Service may be too large; split |

### Phase 4: Layer & Interaction Audit

```
For EACH dependency/call in the codebase:

1. Map caller and callee to their layer
2. Check against interaction matrix:

   Client → Manager:          ✅ (max 1 Manager per use case)
   Client → Engine:           ❌ VIOLATION
   Client → ResourceAccess:   ❌ VIOLATION
   Client → Utility:          ✅
   Manager → Engine:          ✅
   Manager → ResourceAccess:  ✅
   Manager → Utility:         ✅
   Manager → Manager (sync):  ❌ VIOLATION
   Manager → Manager (queue): ✅ (max 1 per use case)
   Engine → ResourceAccess:   ✅
   Engine → Utility:          ✅
   Engine → Engine:           ❌ VIOLATION
   Engine → Manager:          ❌ VIOLATION
   ResourceAccess → Utility:  ✅
   ResourceAccess → Resource: ✅
   Any → layer above:         ❌ CRITICAL VIOLATION

3. For each violation, prescribe fix:
   - Up-call       → Pub/Sub Utility or callback contract
   - Sync sideways → Queue or Pub/Sub
   - Layer skip    → Introduce intermediary or restructure
```

### Phase 5: Composition Validation

```
1. Identify 2–6 core use cases
2. For EACH core use case:
   a. Draw call chain: Client → Manager → Engine(s) → ResourceAccess → Resource
   b. Verify ALL components in the chain exist in the architecture
   c. Verify NO new components are needed (if so, decomposition is incomplete)
   d. Verify the chain respects closed architecture
3. For 2–3 hypothetical FUTURE use cases:
   a. Verify they can be satisfied by DIFFERENT interactions
      between EXISTING components
   b. If new components would be needed → decomposition may be incomplete
      OR it may be a change to the nature of the business (validate which)
```

---

## 2. CODE REVIEW WORKFLOW (Per PR/Changeset)

### Step 1: Structural Check (30 seconds)

```
□ Files modified are in the correct layer directory
□ No imports from upper layers
□ No circular dependencies introduced
□ New service follows naming convention: [Noun][Type]
```

### Step 2: SOLID Scan (2 minutes)

```
□ SRP: Does the changed class have exactly ONE reason to change?
  - Multiple unrelated methods added? → Split
  - Class name is vague ("Helper", "Utils", "Handler")? → Rename or split

□ OCP: Is behavior extended via new implementation, NOT modification?
  - Modified existing switch/if for new case? → Extract strategy (Engine)
  - Changed contract interface? → Should be adding, not changing

□ LSP: Any new subtype or implementation?
  - Can it substitute base type in ALL callers without surprise? → Verify

□ ISP: New or modified interface?
  - Any method that some implementors don't need? → Factor sideways

□ DIP: Dependencies injected via interface?
  - Constructor receives concrete class? → Inject interface instead
  - `new ConcreteService()` inside business logic? → DI violation
```

### Step 3: Clean Code Scan (2 minutes)

```
□ Functions ≤20 lines; one level of abstraction each
□ Names are intention-revealing (can understand without reading body)
□ No side effects in query methods (CQS)
□ Error handling: exceptions, not nulls or error codes
□ No commented-out code
□ No TODO/FIXME without tracked issue
□ No magic numbers/strings — use named constants
```

### Step 4: DRY / YAGNI Scan (1 minute)

```
□ DRY: Search for duplicated logic in the changeset and nearby code
  - Same business rule in 2+ places? → Extract to Engine
  - Same data pattern in 2+ places? → Extract to ResourceAccess
  - Same util logic? → Extract to Utility
  - Similar-looking code, DIFFERENT domain concept? → Leave separate

□ YAGNI: Scan for speculative additions
  - Unused parameters, methods, or classes? → Remove
  - Feature flags for unplanned features? → Remove
  - "Just in case" abstractions with single implementation? → Simplify
  - Comment says "for future use"? → Remove
```

### Step 5: The Method Compliance (1 minute)

```
□ If new service: classified correctly (Manager/Engine/ResAccess/Utility)?
□ If Manager changed: still contains NO business rules? Only sequence?
□ If Engine changed: does NOT orchestrate other services?
□ If ResourceAccess changed: interface still technology-agnostic?
□ If contract changed: still 3–5 ops? Still cohesive? Still independent?
□ No client orchestrating multiple Managers in same flow?
```

---

## 3. SEVERITY CLASSIFICATION

Use this to prioritize review findings:

| Severity | Category | Examples | Action |
|---|---|---|---|
| **🔴 CRITICAL** | Architecture violation | Functional decomposition; open architecture (up-call); client orchestrating services; Manager with business rules | Block merge. Redesign required. |
| **🟠 HIGH** | Structural violation | Sync Manager→Manager; layer skip; contract >12 ops; god service; ResourceAccess leaking tech | Block merge. Refactor before merge. |
| **🟡 MEDIUM** | Principle violation | DRY violation (3+ occurrences); SOLID violation; missing error handling; CQS violation | Merge with fix-forward ticket (must fix in next sprint). |
| **🟢 LOW** | Style/practice | Naming convention miss; function slightly too long; missing test; minor YAGNI | Merge. Track for cleanup. |

---

## 4. COMMON REVIEW CONVERSATIONS

### "But the feature works, why change the architecture?"

The cost of wrong architecture is nonlinear. A service 2x too large is 4x more complex and potentially 20x more expensive to maintain. Getting to the area of minimum cost NOW prevents exponential cost growth.

### "This is just a simple CRUD — we don't need Managers and Engines"

If it's truly simple CRUD with no business rules and no workflow, a single Manager + ResourceAccess is fine. But verify: if there are ANY conditional paths, validation rules, or orchestration → you need the layering.

### "We'll refactor later"

Changing architecture after implementation is orders of magnitude more expensive than getting it right during design. The Method enables design validation in days, not months. The investment pays back immediately.

### "We need this feature urgently — let's skip the layering"

Skipping the layering means the change affects multiple services (functional decomposition behavior). This makes the "urgent" feature take LONGER, not shorter. Proper layering contains the change.

### "My service doesn't fit any category (Manager/Engine/ResourceAccess/Utility)"

Then either: (a) it's a functional decomposition artifact that shouldn't exist as a separate service, or (b) it combines multiple categories and should be split. Every legitimate service fits exactly one category.

---

## 5. AUTOMATED CHECKS (Linting Rules)

Suggested rules for CI/CD enforcement:

```yaml
architecture-lint:
  # Layer violation: no imports from upper layers
  - rule: no-upward-imports
    access-cannot-import: [business, clients]
    business-cannot-import: [clients]

  # No concrete dependencies in business layer
  - rule: business-layer-depends-on-contracts-only
    path: src/business/**
    forbidden-imports: [src/access/*, src/clients/*]

  # Contract size
  - rule: max-interface-methods
    warn: 9
    error: 12
    reject: 20

  # Function size
  - rule: max-function-lines
    warn: 20
    error: 40

  # Cyclomatic complexity
  - rule: max-cyclomatic-complexity
    warn: 10
    error: 15

  # No property-like methods on service contracts
  - rule: no-getter-setter-on-contracts
    path: src/contracts/**
    pattern: "^(get|set|is)[A-Z]"
```
