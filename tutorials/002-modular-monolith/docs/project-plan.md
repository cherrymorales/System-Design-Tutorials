# Modular Monolith Project Plan

## Project Summary

This tutorial defines a B2B wholesale operations platform for a growing distributor serving business customers across sales, warehouse, finance, and operations teams.

The system will be built as a modular monolith using:

- React for the frontend
- ASP.NET Core for the backend
- PostgreSQL for the database

The project replaces disconnected spreadsheets and separate internal tools with one application that has strong internal module boundaries and a first-class testing strategy.

## Problem Statement

The business currently lacks a reliable centralized way to:

- manage customer accounts and sales orders in one system
- reserve and fulfill inventory consistently against customer demand
- issue invoices and track invoice status without duplicating order data across tools
- keep business areas separated in code while still running one application
- produce reporting across departments without creating service sprawl too early
- prove through tests that module boundaries and cross-module workflows behave correctly

Manual and fragmented processes create inconsistent data, weak ownership boundaries, and a codebase that would become tightly coupled if built as a generic monolith.

## Project Goals

- centralize wholesale operations in one application
- preserve strong boundaries between business modules
- support cross-module workflows without distributed infrastructure
- make order, inventory, and billing workflows auditable
- make module boundaries testable and visible
- keep future extraction options open for modules that may later need separate deployment

## Learning Value

From a learning perspective, this project demonstrates:

- how a modular monolith differs from a layered monolith
- how business capabilities become module boundaries
- how one application can use strong module ownership without becoming microservices
- how shared deployment and separate ownership can coexist
- how a modular monolith should be tested beyond basic unit tests

## Success Metrics

- customer orders can be created and tracked without spreadsheet coordination
- inventory reservations are linked to order workflows consistently
- invoice creation follows order and fulfillment state clearly
- reporting can read across modules without allowing uncontrolled write coupling
- contributors can identify which module owns which behavior without guessing
- the MVP has automated coverage for domain rules, cross-module workflows, API behavior, and key frontend paths

## Stakeholders

- sales coordinators
- warehouse operators
- finance officers
- operations managers
- internal technical team

## Assumptions

- the first release is for internal staff only
- one shared database is acceptable if module ownership is enforced clearly
- real external payment processing is not required for MVP
- deployment simplicity remains more important than independent deployment at this stage
- a clean recreated local database is acceptable for the tutorial implementation workflow

## Locked Decisions For V1

The following decisions are finalized for the first implementation:

- authentication: use ASP.NET Core Identity for local user and role management
- module set: `Catalog`, `Customers`, `Orders`, `Inventory`, `Billing`, `Reporting`, and `Identity`
- deployment model: one backend application and one shared database, with module boundaries enforced in code rather than by separate services
- module persistence rule: each module owns its tables and write logic; cross-module writes are not allowed directly
- cross-module integration rule: modules interact through explicit in-process application contracts and orchestrated workflows, not by direct table updates in another module
- reporting rule: reporting may read across modules, but reporting does not own operational writes
- local data workflow: the tutorial implementation may recreate and seed the local database rather than requiring migration-based local setup
- testing rule: MVP is not considered complete without automated tests for domain rules, module workflows, API behavior, and key frontend flows

## Scope

### MVP In Scope

- customer account management
- product catalog management
- order creation and order status tracking
- inventory reservation against orders
- fulfillment-ready inventory views
- invoice generation and invoice status tracking
- role-based internal access
- cross-module reporting for order, inventory, and billing summaries
- automated tests for the module and workflow baseline

### Explicitly Out Of Scope

- public customer portal
- external payment processor integration
- shipment carrier integration
- ERP synchronization
- advanced customer pricing engine
- tenant-isolated SaaS model

## Recommended Delivery Phases

### Phase 1: Foundation And Module Skeleton

Planned deliverables:

- solution and project structure
- module skeletons for the core business capabilities
- shared host, auth, and database setup
- seeded local users and baseline reference data
- initial test project setup and conventions

### Phase 2: Core Business Modules

Planned deliverables:

- `Customers` module
- `Catalog` module
- `Orders` module
- `Inventory` module baseline
- module ownership rules enforced in code structure
- domain tests for `Customers`, `Catalog`, `Orders`, and `Inventory`
- module application tests for create, update, and validation paths

### Phase 3: Cross-Module Workflows

Planned deliverables:

- order creation with inventory reservation
- fulfillment readiness views
- invoice creation from order lifecycle
- reporting read models across the operational modules
- application and contract tests for cross-module workflows
- test fixtures that prove module-owned writes are not bypassed

### Phase 4: Hardening And Boundary Enforcement

Planned deliverables:

- authorization refinement by module and role
- internal contract cleanup
- API integration tests
- frontend workflow tests
- end-to-end smoke tests for the main wholesale flow
- release readiness validation

## Milestones

1. Scope and document approval
2. Module boundary approval
3. Data ownership approval
4. Deployment and testing model approval
5. MVP implementation start
6. Core modules complete
7. Cross-module workflow baseline complete
8. Test suite baseline complete
9. UAT and release readiness review

## Functional Requirements

- users can manage customer records
- users can manage product records
- users can create sales orders
- the system can reserve inventory against orders
- users can view fulfillment readiness by order and warehouse context
- finance users can create invoices from eligible order states
- managers can view reporting summaries across modules
- module-owned operational data cannot be mutated directly by another module
- the system includes automated tests that prove these behaviors and boundaries
- the system includes a documented test matrix that explains what is tested at domain, module, API, UI, and smoke-test levels

## Workflow Decisions

### Order Lifecycle

The order lifecycle for V1 is:

1. `Draft`
2. `Submitted`
3. `Reserved`
4. `ReadyForInvoicing`
5. `Invoiced`
6. `Completed`
7. `Cancelled`

Behavior rules:

- an order starts as `Draft`
- submission validates customer and line-item data
- reservation links the order to available inventory
- an order becomes `ReadyForInvoicing` only after the required inventory state is satisfied
- invoicing creates a billing record without transferring billing ownership to the orders module
- cancellation must release any active reservation if invoicing has not progressed too far

### Invoice Lifecycle

The invoice lifecycle for V1 is:

1. `Draft`
2. `Issued`
3. `Paid`
4. `Voided`

Behavior rules:

- billing owns invoice creation and state transitions
- orders may request invoice creation through an explicit contract, but do not own invoice data
- payment state is tracked internally only for MVP, without external gateway integration

## Non-Functional Requirements

- clear module ownership boundaries
- straightforward local development and operational support
- maintainable code structure as the domain grows
- explicit authorization model by role and module capability
- deployment simplicity over premature distribution
- automated coverage that is strong enough to demonstrate how the architecture should be tested

## Major Risks

- module boundaries are defined in docs but not enforced in code
- the host project becomes a coordination bottleneck if modules leak responsibilities
- reporting reads turn into hidden backdoor coupling between modules
- implementation shortcuts blur module ownership too early
- the team starts extracting services before module boundaries are stable inside the monolith
- tests focus only on units and fail to prove cross-module behavior

## Risk Mitigations

- define module ownership explicitly before coding starts
- keep module APIs and internal persistence boundaries visible in both docs and code
- allow cross-module reads only through approved reporting or orchestration paths
- treat service extraction as a later decision, not as an implementation assumption
- include module-boundary checks in code review expectations
- make the testing strategy part of the MVP baseline rather than a later hardening step

## Readiness Gates

Implementation may start only when:

- the README, architecture, implementation, deployment, and testing documents are complete
- the module list is accepted
- data ownership boundaries are accepted
- core workflow lifecycles are agreed
- the deployment model is accepted
- the locked V1 decisions in this document are used as the implementation baseline

## Definition Of MVP Complete

MVP is complete when:

- the documented in-scope modules are implemented
- cross-module workflows function through explicit contracts
- module ownership boundaries are visible in the solution structure
- core operational flows work without spreadsheet dependency
- the application can run locally and in a non-local environment as one deployment unit
- automated tests cover domain rules, workflow orchestration, API behavior, and key frontend flows
- a repeatable smoke-test path proves the full order-to-invoice flow on seeded data

## Recommendation

Proceed with implementation only after this document set and the supporting testing strategy are accepted as the baseline for V1.
