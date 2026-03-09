# Modular Monolith Testing Strategy

## Purpose

This document defines how the `002-modular-monolith` MVP should be tested.

The goal is not only to verify business correctness, but also to prove that the architecture behaves like a modular monolith rather than a loosely structured monolith.

## Testing Philosophy

The test suite must prove five things:

- each module enforces its own business rules
- cross-module workflows happen through explicit contracts
- the host application composes modules correctly
- role-based access rules are enforced at runtime
- the main business workflow works end to end on realistic seeded data

Tests are part of the MVP baseline. They are not deferred to post-MVP hardening.

## Test Layers

### 1. Domain Tests

Purpose:

- validate entity rules, invariants, and state transitions inside each module

Examples:

- a customer cannot move to an invalid status
- an archived product cannot be sold
- an order cannot leave `Draft` without valid lines
- an invoice cannot move from `Draft` directly to `Paid`

### 2. Module Application Tests

Purpose:

- validate use-case handlers, service orchestration, and module-level validation

Examples:

- `Customers` creates a business account with required contacts
- `Catalog` rejects duplicate SKU creation
- `Orders` rejects submission for inactive customers
- `Inventory` rejects reservation when stock is insufficient
- `Billing` creates an invoice only for invoice-eligible orders

### 3. Cross-Module Contract Tests

Purpose:

- validate approved in-process module interactions without allowing persistence shortcuts

Examples:

- `Orders` requests stock reservation through `Inventory`
- `Orders` requests invoice creation through `Billing`
- `Reporting` reads order, inventory, and invoice summaries without taking operational write ownership

The test should prove contract behavior, not internal table access.

### 4. API Integration Tests

Purpose:

- validate the composed host application, routing, authorization, serialization, and module registration

Examples:

- authenticated sales users can create orders
- warehouse users cannot issue invoices
- finance users can issue invoices for eligible orders
- managers can view reporting endpoints
- invalid cross-module workflow requests fail with clear responses

### 5. Frontend Tests

Purpose:

- validate that the user-facing workflow reflects module boundaries and host behavior correctly

Examples:

- create a customer
- create and submit an order
- view reservation result
- issue an invoice from an eligible order
- view reporting summary
- confirm role-based navigation or action restrictions

### 6. End-To-End Smoke Tests

Purpose:

- validate the highest-value business path against a seeded running environment

Required MVP smoke path:

1. sign in as `SalesCoordinator`
2. create a customer
3. create and submit an order
4. reserve inventory
5. confirm the order becomes invoice-eligible
6. sign in as `FinanceOfficer`
7. issue the invoice
8. sign in as `OperationsManager`
9. confirm the reporting summary reflects the new operational state

## Module Test Matrix

### Customers

Required tests:

- create customer with valid business details
- reject invalid account data
- reject duplicate account code
- prevent operational use of inactive customer accounts

### Catalog

Required tests:

- create product with valid SKU and price
- reject duplicate SKU
- reject invalid pricing
- prevent order usage of archived products

### Orders

Required tests:

- create draft order
- reject order without valid customer
- reject order line with archived product
- submit valid order
- cancel eligible order
- prevent invalid state transitions

### Inventory

Required tests:

- reserve stock for valid submitted order
- reject reservation when available stock is insufficient
- release reservation when eligible order is cancelled
- expose fulfillment-ready information without taking order ownership

### Billing

Required tests:

- create invoice only for eligible order state
- reject invoice creation before order readiness
- issue invoice
- mark invoice paid
- prevent invalid invoice transitions

### Reporting

Required tests:

- read combined order, reservation, and invoice state
- ensure reporting paths do not mutate operational state
- confirm reporting remains read-only even when read models are introduced

### Identity

Required tests:

- authenticate seeded users
- enforce role restrictions by endpoint
- enforce module-specific actions by role

## Architectural Boundary Checks

The test strategy should explicitly prove:

- one module does not directly write another module's operational tables
- orchestration logic uses explicit module contracts
- reporting is read-only
- authorization rules follow business roles rather than ad hoc endpoint exceptions

These checks matter because the main failure mode of a modular monolith is silent boundary erosion.

## Test Data Strategy

Use seeded reference data for predictable execution:

- active and inactive customers
- active and archived products
- enough stock for success paths
- insufficient stock for failure paths
- seeded roles and users for sales, warehouse, finance, and operations

Keep success-path and failure-path data explicit so tests do not depend on hidden assumptions.

## Recommended Test Project Layout

```text
tests/
  backend/
    Domain/
    Application/
    Contracts/
    Api/
  frontend/
    features/
    workflows/
  smoke/
```

The exact tooling may vary, but the separation of test intent should remain visible.

## Recommended Execution Order

For local development:

1. run domain and application tests first
2. run API integration tests
3. run frontend tests
4. run smoke tests against a seeded running environment

For CI:

1. backend tests
2. frontend tests
3. smoke tests in an isolated seeded environment
4. build artifacts only after tests pass

## MVP Test Completion Criteria

The `002` MVP is not complete unless:

- each in-scope module has automated tests for its core business rules
- the main `Orders -> Inventory -> Billing -> Reporting` workflow is covered by automated tests
- API tests prove composition and authorization behavior
- frontend tests cover the main internal user workflows
- at least one end-to-end smoke path passes on a seeded environment

## What To Avoid

- relying only on unit tests
- testing cross-module behavior by bypassing public module contracts
- treating the host layer as too thin to test
- skipping frontend coverage because the backend is modular
- omitting smoke tests for the main business workflow
