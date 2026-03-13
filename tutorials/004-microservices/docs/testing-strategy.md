# Microservices Testing Strategy

## Purpose

This document defines how the implemented `004-microservices` MVP is tested.

The goal is not only to verify business correctness, but also to prove that the distributed architecture behaves like real microservices rather than a distributed monolith.

## Testing Philosophy

The test suite must prove six things:

- each service enforces its own business rules
- browser-facing HTTP behavior is correct through the gateway
- cross-service workflows happen through explicit contracts and events
- compensation paths are correct when downstream work fails
- the frontend reflects eventual consistency and service-owned state correctly
- the main business path works end to end on a seeded running environment

Tests are part of the MVP baseline. They are not deferred to post-MVP hardening.

## Implementation Status

Implemented now:

- service workflow and business-rule tests
- gateway API tests
- contract and integration-event tests
- frontend tests
- end-to-end Playwright smoke coverage for the primary success path

Current verification commands:

- `dotnet test src/sln/SystemDesignTutorials.Microservices.slnx`
- `npm test -- --run` in `implementation/src/frontend`
- `npm run build` in `implementation/src/frontend`
- `npm test` in `implementation/tests/smoke`

## Test Layers

### 1. Service Domain Tests

Purpose:

- validate entity rules, invariants, and state transitions inside each service

Examples:

- an order cannot be submitted without valid lines
- a reservation cannot succeed when available stock is insufficient
- a payment cannot move from `Failed` to `Captured`
- a shipment cannot move from `Pending` directly to `Shipped`

### 2. Service Application Tests

Purpose:

- validate service orchestration, handlers, validation, and persistence behavior inside each service boundary

Examples:

- `Catalog` returns sellable products only
- `Orders` creates a draft order with product snapshots
- `Inventory` creates and releases reservations correctly
- `Payments` records authorization outcomes correctly
- `Fulfillment` creates a shipment only for eligible orders

### 3. Gateway And Service API Integration Tests

Purpose:

- validate route behavior, auth behavior, status codes, serialization, and service registration

Examples:

- unauthenticated gateway requests return `401`
- an order operations agent can create and submit an order
- a fulfillment operator cannot create catalog items
- the dashboard projection endpoint returns the expected shape
- invalid synchronous requests fail with clear payloads

### 4. Contract And Event-Flow Tests

Purpose:

- validate the explicit service-to-service integration behavior
- prove that the system does not rely on hidden database coupling

Examples:

- `Orders` publishes `OrderSubmitted` with the documented contract
- `Inventory` consumes reservation requests idempotently
- `Payments` publishes authorization outcomes in the expected shape
- `Orders` reacts correctly when inventory succeeds but payment fails
- `OperationsQuery` updates a projection after downstream events

### 5. Frontend Tests

Purpose:

- validate that the React SPA behaves correctly through the gateway contract

Examples:

- login flow restores a session
- order detail shows pending dependency state after submission
- dashboard shows projected status data from the gateway
- role-based navigation hides unsupported areas
- fulfillment progression updates the visible order state after refresh

### 6. End-To-End Smoke Tests

Purpose:

- validate the highest-value operational path against a seeded running environment

Required MVP smoke path:

1. sign in as `OrderOpsAgent`
2. create and submit an order
3. observe reservation and payment progression
4. sign in as `FulfillmentOperator`
5. progress shipment state
6. sign in as `OperationsManager`
7. confirm the dashboard projection reflects the workflow result

## Service Test Matrix

### GatewayBff

Required tests:

- unauthenticated access is rejected
- authenticated user context is propagated correctly
- gateway composition returns the documented response shape
- unsupported role actions are rejected

### Identity

Required tests:

- seeded users can authenticate
- roles are assigned correctly
- downstream user context contains the required claims or identifiers

### Catalog

Required tests:

- active product lookup succeeds
- archived products are excluded from sellable responses
- duplicate SKU creation is rejected

### Orders

Required tests:

- create draft order
- reject invalid submit attempts
- publish order submission event
- transition to `ReadyForFulfillment` only after required outcomes
- mark order `Failed` and emit compensation when required

### Inventory

Required tests:

- reserve stock for valid request
- reject reservation on insufficient stock
- release reservation on compensation or cancellation
- handle duplicate reservation messages idempotently

### Payments

Required tests:

- authorize payment intent
- publish failure outcome when authorization fails
- void eligible payments during compensation
- reject invalid state transitions

### Fulfillment

Required tests:

- create shipment for eligible order
- progress shipment through valid states only
- publish delivered event

### Notifications

Required tests:

- notification request is recorded or dispatched from workflow events
- duplicate event handling does not create duplicate sends

### OperationsQuery

Required tests:

- dashboard projection updates from workflow events
- order tracking projection reflects downstream service outcomes
- read model remains read-only

## Architectural Boundary Checks

The test strategy should explicitly prove:

- one service does not directly write another service's operational data
- service integration happens through HTTP contracts or events
- the gateway does not own order, payment, or inventory rules
- projections are read-only and rebuilt from events or approved reads
- event handlers are idempotent

These checks matter because the main failure mode of microservices is hidden coupling.

## Test Data Strategy

Use deterministic seeded data for:

- one user per core role
- active and archived products
- sufficient and insufficient stock cases
- successful and failed payment scenarios
- orders in multiple states
- projections that include pending, failed, and completed workflows

Keep success-path and failure-path data explicit so tests do not depend on hidden assumptions.

## Recommended Test Project Layout

```text
tests/
  services/
    Identity/
    Catalog/
    Orders/
    Inventory/
    Payments/
    Fulfillment/
    Notifications/
    OperationsQuery/
  contracts/
    Http/
    Events/
  frontend/
    auth/
    dashboard/
    orders/
    fulfillment/
  smoke/
```

## Recommended Execution Order

For local development:

1. run service domain and application tests
2. run gateway and service API integration tests
3. run contract and event-flow tests
4. run frontend tests
5. run smoke tests against a seeded running environment when workflow behavior changes materially

For CI:

1. service builds
2. service tests
3. contract and event-flow tests
4. frontend build
5. frontend tests
6. smoke tests in an isolated seeded environment
7. image packaging only after tests pass

## MVP Test Completion Criteria

The `004` MVP is not complete unless:

- each in-scope service has automated tests for its core business rules
- the main order submission and compensation path is covered by automated tests
- gateway and service API tests prove authorization and contract behavior
- frontend tests cover the main operator workflows
- at least one end-to-end smoke path passes on a seeded environment

## What To Avoid

- relying only on unit tests
- testing service interaction by reading each other's tables directly
- skipping event-flow tests because HTTP tests already pass
- treating the gateway as too thin to test
- omitting smoke tests for the main distributed workflow
