# Implementation Patterns Reference

> Read this when implementing services following The Method. Code examples are language-agnostic pseudocode.

---

## 1. LAYERED PROJECT STRUCTURE

```
src/
├── clients/                    # Client Layer
│   ├── web-app/
│   ├── mobile-app/
│   └── api-gateway/
├── business/                   # Business Logic Layer
│   ├── managers/
│   │   ├── OrderManager/
│   │   └── FulfillmentManager/
│   ├── engines/
│   │   ├── PricingEngine/
│   │   └── RoutingEngine/
├── access/                     # Resource Access Layer
│   ├── OrdersAccess/
│   ├── CustomersAccess/
│   └── InventoryAccess/
├── utilities/                  # Utilities (vertical bar)
│   ├── SecurityUtility/
│   ├── LoggingUtility/
│   └── PubSubUtility/
├── contracts/                  # Shared interfaces/contracts
│   ├── managers/
│   ├── engines/
│   ├── access/
│   └── utilities/
└── tests/
    ├── unit/                   # Engines, ResourceAccess
    ├── integration/            # Manager workflows
    └── e2e/                    # Core use case call chains
```

### Import Rules (ENFORCE)

```
# ✅ ALLOWED dependency direction
clients     → contracts/managers, contracts/utilities
managers    → contracts/engines, contracts/access, contracts/utilities
engines     → contracts/access, contracts/utilities
access      → contracts/utilities
utilities   → (no project deps, only external libs)

# ❌ FORBIDDEN
managers    → contracts/managers     (sideways — except queued)
engines     → contracts/managers     (calling up)
access      → contracts/engines      (calling up)
clients     → contracts/engines      (skipping layer)
clients     → contracts/access       (skipping layer)
ANY         → concrete implementations (depend on contracts only)
```

---

## 2. SERVICE IMPLEMENTATION PATTERNS

### 2.1 Manager Pattern

Managers orchestrate workflows. They are THIN — contain no business rules, only sequence.

```pseudo
class OrderManager implements IOrderManager {

    // Dependencies: injected contracts only
    private pricingEngine: IPricingEngine
    private routingEngine: IRoutingEngine
    private ordersAccess: IOrdersAccess
    private pubSub: IPubSubUtility

    // USE CASE: Place Order (core use case)
    function placeOrder(request: PlaceOrderRequest): OrderResult {
        // Step 1: validate & price
        pricing = pricingEngine.calculatePrice(request.items)

        // Step 2: determine fulfillment route
        route = routingEngine.determineRoute(request.destination)

        // Step 3: persist
        order = ordersAccess.store(Order.from(request, pricing, route))

        // Step 4: notify (async event, NOT direct call to another Manager)
        pubSub.publish("order.placed", OrderPlacedEvent(order.id))

        return OrderResult.success(order)
    }

    // USE CASE: Cancel Order (same components, different sequence)
    function cancelOrder(orderId: string): CancelResult {
        order = ordersAccess.retrieve(orderId)
        refund = pricingEngine.calculateRefund(order)
        ordersAccess.updateStatus(orderId, CANCELLED)
        pubSub.publish("order.cancelled", OrderCancelledEvent(orderId, refund))
        return CancelResult.success(refund)
    }
}
```

**Key rules:**
- Manager never contains `if/else` business rules → delegate to Engine
- Manager never does raw DB queries → delegate to ResourceAccess
- Manager never calls another Manager synchronously → queue or Pub/Sub
- Manager is "almost expendable" — if you delete it and rewrite the sequence, nothing else breaks

### 2.2 Engine Pattern (Strategy)

Engines encapsulate volatile business RULES. They are the Strategy pattern.

```pseudo
// Contract: multiple implementations possible
interface IPricingEngine {
    calculatePrice(items: Item[]): PricingResult
    calculateRefund(order: Order): RefundResult
}

// Implementation A: standard pricing
class StandardPricingEngine implements IPricingEngine {
    private ratesAccess: IRatesAccess

    function calculatePrice(items: Item[]): PricingResult {
        // Business rules HERE — tax, discounts, tiers
        subtotal = items.sum(i => i.basePrice * i.quantity)
        tax = applyTaxRules(subtotal)          // encapsulated volatility
        discount = applyDiscountRules(items)    // encapsulated volatility
        return PricingResult(subtotal, tax, discount)
    }
}

// Implementation B: promo pricing (different volatility instance)
class PromoPricingEngine implements IPricingEngine {
    // completely different rules, same contract
}
```

**Key rules:**
- Engine never orchestrates other Engines (no sideways)
- Engine never calls a Manager (no calling up)
- Engine CAN call ResourceAccess (same layer)
- Engine is shareable across Managers

### 2.3 ResourceAccess Pattern

Abstracts all storage volatility. Upper layers NEVER know what's behind it.

```pseudo
interface IOrdersAccess {
    store(order: Order): StoredOrder
    retrieve(id: string): Order
    updateStatus(id: string, status: Status): void
    findByCustomer(customerId: string): Order[]
}

class OrdersAccess implements IOrdersAccess {
    // Implementation detail: could be SQL, NoSQL, cache, cloud, file
    // NOTHING in the interface reveals this
    private db: DatabaseConnection  // or cache, or API, or file

    function store(order: Order): StoredOrder {
        // Encapsulated: HOW storage works
        return db.insert("orders", order.toRow())
    }
}
```

**Key rules:**
- Name is `[Resource]Access`, never `[Resource]Repository` with domain logic
- Interface uses domain terms, never tech terms (no `executeSql`, no `getFromRedis`)
- Can access multiple Resources if necessary (but prefer 1:1)
- Never receives queued calls
- Never publishes events

### 2.4 Utility Pattern

Cross-cutting, system-agnostic. Cappuccino-machine test: would a smart coffee machine use this?

```pseudo
interface ILoggingUtility {
    info(message: string, context: Map): void
    warn(message: string, context: Map): void
    error(message: string, error: Error, context: Map): void
}

// ✅ PASSES cappuccino test: any system logs
class LoggingUtility implements ILoggingUtility { ... }

// ❌ FAILS cappuccino test: business-specific
class MortgageCalculatorUtility { ... }  // This is an Engine!
```

---

## 3. CONTRACT DESIGN EXAMPLES

### 3.1 Factoring Sideways (ISP compliance)

```pseudo
// ❌ BAD: mixed concerns in one contract
interface IDeviceAccess {
    readCode(): long
    adjustBeam(): void
    openPort(): void
    closePort(): void
}

// ✅ GOOD: factored into independent facets
interface ICommunicationDevice {
    openPort(): void
    closePort(): void
}

interface IReaderAccess {
    readCode(): long
}

interface IScannerAccess extends IReaderAccess {
    adjustBeam(): void
}

// Each service supports relevant facets
class BarcodeScanner implements IScannerAccess, ICommunicationDevice { }
class KeypadReader implements IReaderAccess, ICommunicationDevice { }
class ConveyerBelt implements IBeltAccess, ICommunicationDevice { }
```

### 3.2 Factoring Up (shared base)

```pseudo
// Common operations across device types → factor UP
interface IDeviceControl {
    abort(): void
    runDiagnostics(): DiagResult
}

interface IReaderAccess extends IDeviceControl {
    readCode(): long
}

interface IBeltAccess extends IDeviceControl {
    start(): void
    stop(): void
}
```

---

## 4. ANTI-PATTERN DETECTION (Code Examples)

### 4.1 Client Orchestrating Services (FORBIDDEN)

```pseudo
// ❌ Client stitching services = functional decomposition
class OrderPage {
    function submit() {
        invoice = invoicingService.create(data)      // call 1
        billing = billingService.charge(invoice)      // call 2
        shipping = shippingService.ship(invoice)      // call 3
        notification.send(billing, shipping)          // call 4
    }
}

// ✅ Client calls ONE Manager
class OrderPage {
    function submit() {
        result = orderManager.placeOrder(data)  // single entry point
    }
}
```

### 4.2 Manager Containing Business Rules (FORBIDDEN)

```pseudo
// ❌ Business logic inside Manager
class OrderManager {
    function placeOrder(req) {
        if (req.customer.tier == GOLD && req.total > 1000) {
            discount = 0.15  // Business rule leaked into Manager!
        }
    }
}

// ✅ Delegate to Engine
class OrderManager {
    function placeOrder(req) {
        pricing = pricingEngine.calculatePrice(req) // Engine owns the rule
    }
}
```

### 4.3 ResourceAccess Exposing Technology (FORBIDDEN)

```pseudo
// ❌ Leaky abstraction
interface IOrdersAccess {
    executeSql(query: string): ResultSet      // exposes SQL
    getFromRedis(key: string): string         // exposes Redis
}

// ✅ Domain-level abstraction
interface IOrdersAccess {
    store(order: Order): StoredOrder
    retrieve(id: string): Order
}
```

### 4.4 Sync Manager-to-Manager Call (FORBIDDEN)

```pseudo
// ❌ Sideways synchronous call
class OrderManager {
    function placeOrder(req) {
        // ...
        fulfillmentManager.startFulfillment(order)  // SYNC sideways!
    }
}

// ✅ Queued or event-driven
class OrderManager {
    function placeOrder(req) {
        // ...
        pubSub.publish("order.placed", event)       // decoupled
        // OR
        messageQueue.enqueue("fulfillment", message) // queued
    }
}
```

---

## 5. CLEAN CODE QUICK PATTERNS

### 5.1 Function Size & Naming

```pseudo
// ❌ BAD
function proc(d, f) {
    // 80 lines of mixed concerns
}

// ✅ GOOD
function calculateShippingCost(destination: Address, items: Item[]): Money {
    weight = calculateTotalWeight(items)
    zone = determineShippingZone(destination)
    return applyZoneRate(zone, weight)
}
```

### 5.2 CQS (Command-Query Separation)

```pseudo
// ❌ BAD: query with side effect
function getNextOrder(): Order {
    order = queue.dequeue()   // side effect!
    return order
}

// ✅ GOOD: separate command and query
function peekNextOrder(): Order { return queue.peek() }
function dequeueOrder(): void { queue.dequeue() }
```

### 5.3 Error Handling

```pseudo
// ❌ BAD: returning null, swallowing errors
function findCustomer(id): Customer? {
    try { return access.retrieve(id) }
    catch { return null }
}

// ✅ GOOD: explicit, fail fast
function findCustomer(id): Customer {
    customer = access.retrieve(id)
    if (customer == null) throw CustomerNotFound(id)
    return customer
}
```

---

## 6. DRY REFACTORING DECISION TREE

```
Duplicated code detected
    │
    ├─ Same business RULE in multiple places?
    │   └─ Extract to ENGINE
    │
    ├─ Same data access pattern in multiple places?
    │   └─ Extract to RESOURCEACCESS
    │
    ├─ Same orchestration sequence in multiple places?
    │   └─ Likely a missing MANAGER (or a use case not captured)
    │
    ├─ Same cross-cutting concern (logging, auth, serialization)?
    │   └─ Extract to UTILITY
    │
    └─ Code LOOKS similar but represents DIFFERENT domain concepts?
        └─ DO NOT merge. Different volatilities = different services.
            Merging would create functional coupling.
```
