# Layered Monolith Testing Strategy

## Purpose

This document explains how the `001-layered-monolith` tutorial should be tested and what is actually covered in the current implementation.

The goal is not only to verify correctness, but also to teach how a layered monolith should separate test responsibilities across domain logic, API behavior, and UI behavior.

## Testing Objectives

The current testing strategy is designed to prove the following:

- inventory state changes follow business rules
- transfer and adjustment workflows enforce the intended state machines
- catalog and warehouse entities preserve key invariants
- protected API endpoints enforce authentication and warehouse-scoped authorization
- the frontend preserves the seeded-user login flow and critical UI behavior
- the implementation can be validated quickly during local development
- the tutorial makes clear which test layers are already implemented and which still need deeper coverage

## Current Coverage In This Repository

Implemented now:

- backend domain and workflow tests with `xUnit`
- backend API integration tests with `WebApplicationFactory` and SQLite-backed test hosting
- frontend automated tests with `Vitest` and React Testing Library
- verification of inventory reservation and dispatch behavior
- verification of product and warehouse entity mutations
- verification of transfer lifecycle rules
- verification of adjustment approval threshold behavior
- verification of authentication, authorization, and warehouse-scoped API reads
- verification of core login-screen behavior in the frontend

Still not implemented:

- browser-based end-to-end tests
- container-level smoke tests executed automatically in CI

## Test Layers

### 1. Domain Tests

This is still the strongest automated layer in the current `001` implementation.

These tests validate the business model without requiring the web host or the database.

Current files:

- `../implementation/tests/backend/SystemDesignTutorials.LayeredMonolith.Tests/InventoryItemTests.cs`
- `../implementation/tests/backend/SystemDesignTutorials.LayeredMonolith.Tests/ProductAndWarehouseTests.cs`
- `../implementation/tests/backend/SystemDesignTutorials.LayeredMonolith.Tests/WorkflowTests.cs`
- `../implementation/tests/backend/SystemDesignTutorials.LayeredMonolith.Tests/ApiIntegrationTests.cs`

What these tests currently prove:

- reserving stock increases `QuantityReserved` without changing `QuantityOnHand`
- dispatching reserved stock decreases `QuantityOnHand` and clears reservations
- product updates replace mutable catalog details correctly
- product archiving changes status correctly
- warehouse updates and deactivation behave correctly
- transfers move through `Requested -> Approved -> Dispatched -> Received`
- transfers can be cancelled before dispatch
- small adjustments auto-approve
- large adjustments move into pending approval

Why this layer matters:

- it is fast
- it isolates business rules from infrastructure concerns
- it teaches where core behavior should live in a layered monolith

### 2. API Integration Tests

This layer is now implemented at a baseline level in `001`.

Current file:

- `../implementation/tests/backend/SystemDesignTutorials.LayeredMonolith.Tests/ApiIntegrationTests.cs`

What these tests currently prove:

- unauthenticated requests to protected catalog endpoints return `401`
- seeded manager login succeeds through the real auth endpoint
- managers can retrieve all warehouses
- Brisbane operators only see their assigned warehouse and inventory rows
- Brisbane operators are blocked from transfer creation with `403`

Recommended next coverage:

- successful receipt, transfer, and adjustment progression entirely through HTTP endpoints
- actor attribution assertions on persisted workflow records
- error-contract coverage for invalid stock, invalid transitions, and bad request payloads

### 3. Frontend Tests

This layer is also implemented at a baseline level in `001`.

Current file:

- `../implementation/src/frontend/src/App.test.tsx`

What these tests currently prove:

- the application falls back to the login screen when session restoration returns `401`
- clicking a seeded-user card populates the login email field correctly

Recommended next coverage:

- protected route behavior after successful sign-in
- role-aware rendering for restricted actions
- form submission flows for receipts, transfers, and adjustments
- low-stock dashboard rendering and failure-state handling

### 4. Manual Runtime Verification

Even with automated tests in place, manual runtime checks are still part of the tutorial workflow.

Current manual checks that should remain easy to perform:

- start Docker and verify `/api/health`
- log in with a seeded user
- verify manager access across all warehouses
- verify operator restriction to assigned warehouses
- create a receipt and confirm the authenticated actor is recorded
- confirm operators cannot create transfers if the role does not allow it

## How To Run Current Automated Tests

From the repository root:

```powershell
dotnet test tutorials/001-layered-monolith/implementation/src/backend/sln/SystemDesignTutorials.LayeredMonolith.slnx
cd tutorials/001-layered-monolith/implementation/src/frontend
npm run test
```

Current verified result:

- `14` backend tests passing
- `2` frontend tests passing

## Recommended Test Matrix For This Tutorial

Minimum bar for a good layered monolith tutorial:

- domain tests for business rules
- API integration tests for auth and workflow behavior
- frontend tests for critical screens
- a small set of manual smoke checks for the full local runtime

Recommended evolution order:

1. keep domain tests strong as entities and workflows change
2. extend API integration coverage for workflow transitions and error contracts
3. extend frontend tests around protected actions, reporting, and failure states
4. add automated Docker smoke checks if the tutorial later adds CI

## What This Teaches About Layered Monoliths

A layered monolith should not put all confidence in controller or endpoint tests.

The preferred testing emphasis is:

- domain tests protect business rules first
- integration tests prove the web and auth boundary second
- frontend tests prove the user workflow third

That pattern matches the architecture itself: stable business rules at the center, infrastructure and UI tested around them.

## Current Testing Gaps

The main gaps in `001` are now explicit:

- automated coverage still focuses on representative scenarios rather than exhaustive business flows
- there is no automated end-to-end workflow test running through the browser
- there is no automated container smoke test for the Docker runtime

These gaps do not block the tutorial from being understandable, but they do limit how strongly `001` can be presented as a fully hardened testing example compared with `002`.

## Completion Criteria For Testing Maturity

For `001` to match the stronger testing standard demonstrated in `002`, it should continue expanding toward:

- domain tests
- backend API integration tests
- frontend automated tests
- browser-level end-to-end tests
- documented smoke-test commands for the Docker runtime

At the current stage, the tutorial already satisfies the first three layers at a baseline level and documents the next gaps clearly.