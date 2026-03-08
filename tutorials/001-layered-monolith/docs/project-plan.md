# Layered Monolith Project Plan

## Project Summary

This tutorial defines and implements an internal inventory and warehouse management system for a mid-sized retail distributor operating warehouses in Brisbane, Sydney, and Melbourne.

The system uses:

- React for the frontend
- ASP.NET Core for the backend
- PostgreSQL for the database

The project replaces spreadsheet-driven stock tracking with one application that supports controlled workflows, auditability, and operational visibility.

## Problem Statement

The business needs a reliable centralized way to:

- track stock across multiple warehouses
- record supplier receipts consistently
- transfer stock between locations with clear controls
- approve and audit stock adjustments
- identify low-stock conditions early

Manual processes create inconsistent data, weak auditability, and delayed operational decisions.

## Project Goals

- centralize inventory data in one system
- provide accurate stock visibility by warehouse
- make stock-changing actions auditable
- reduce operational friction for warehouse staff
- provide managers with clear approval and reporting workflows

## Learning Value

From a learning perspective, this project demonstrates:

- how a layered monolith can support a realistic business workflow
- how planning decisions translate into implementation phases
- how authorization rules shape both backend and frontend behavior
- how a tutorial can distinguish architecture intent from current implementation status

## Success Metrics

- stock receipts can be recorded without spreadsheet dependency
- transfers between warehouses are tracked end-to-end
- adjustment approvals are auditable by user and timestamp
- low-stock reports are available without manual consolidation
- operational staff can complete common workflows inside one application

## Stakeholders

- warehouse operators
- inventory planners
- purchasing officers
- operations managers
- internal technical team

## Assumptions

- the first release is for internal staff only
- warehouse count is small and stable
- a single relational database is sufficient
- real-time external integrations are not required for MVP
- the business accepts a phased delivery approach
- this tutorial prefers a clean recreated local database over migration-driven local evolution

## Locked Decisions For V1

The following decisions are finalized and already reflected in the implementation:

- authentication: ASP.NET Core Identity with cookie authentication
- supplier scope: keep only `supplierCode` and `supplierReference` in MVP, no supplier management module yet
- transfer behavior: creating a transfer reserves source inventory, dispatching decrements source on-hand, receiving increments destination on-hand
- adjustment approval threshold: adjustments with absolute quantity delta greater than `10` units or absolute inventory value greater than `AUD 1,000` require manager approval
- warehouse visibility: warehouse operators can only view and act on assigned warehouses, while inventory planners, purchasing officers, and operations managers can view all warehouses
- local database workflow: schema is created from the model on startup and seeded against a clean local database state

## Scope

### MVP In Scope

- product catalog management
- warehouse management
- inventory receipt recording
- stock transfer workflow
- stock adjustment workflow with approval threshold
- low-stock reporting
- role-based access for internal users
- audit trail for inventory-changing operations

### Explicitly Out Of Scope

- public supplier portal
- e-commerce order fulfillment integration
- barcode hardware support
- procurement lifecycle management
- advanced forecasting
- multi-tenant architecture
- frontend automated test suite
- backend integration test suite for authorization and API flows

## Delivery Phases

### Phase 1: Foundation

Implemented.

Delivered:

- solution skeleton and project structure
- Docker runtime for API and PostgreSQL
- baseline domain entities and tests
- clean local database creation and seeding

### Phase 2: Catalog And Warehouse Management

Implemented.

Delivered:

- product CRUD endpoints and UI
- warehouse CRUD endpoints and UI
- seeded catalog, warehouses, and starting inventory

### Phase 3: Core Inventory Workflows

Implemented.

Delivered:

- stock receipt workflow
- transfer workflow
- inventory adjustment workflow
- low-stock dashboard and workflow console

### Phase 4: Authentication And Authorization Hardening

Implemented.

Delivered:

- login, logout, and session endpoints
- React login flow and protected routes
- role-based API authorization
- operator warehouse assignment filtering
- server-side actor attribution for inventory-changing actions

## Remaining Work After Phase 4

Recommended next steps:

- backend integration tests for endpoint and authorization behavior
- frontend automated tests
- production-focused packaging of the built SPA into the ASP.NET Core app
- reporting screens beyond low-stock views
- moving workflow orchestration from endpoint modules into explicit application services

## Milestone Status

1. Scope and document approval: completed
2. Architecture approval: completed
3. Data model approval: completed
4. Deployment model approval for tutorial local workflow: completed
5. MVP implementation start: completed
6. Phase 1 to Phase 4 implementation baseline: completed
7. Test hardening and production packaging: pending

## Functional Requirements

Implemented now:

- users can create and update products
- users can create and update warehouses
- warehouse operators can record stock receipts for assigned warehouses
- inventory planners can create stock transfers
- the system validates stock before transfer creation
- users can submit stock adjustments with reason codes
- managers can approve high-value or threshold-based adjustments
- users can view low-stock items by warehouse
- all inventory-changing actions record actor and timestamp

Still incomplete:

- backend endpoint-level automated integration tests
- frontend automated workflow coverage
- production-oriented single-container packaging of backend plus built SPA

## Workflow Decisions

### Transfer Workflow

The transfer lifecycle for V1 is:

1. `Requested`
2. `Approved`
3. `Dispatched`
4. `Received`
5. `Cancelled`

Behavior rules:

- creating a transfer reserves quantity in the source warehouse
- approving a transfer confirms operational intent but does not yet move stock
- dispatching a transfer reduces source `quantityOnHand` and releases the reservation
- receiving a transfer increases destination `quantityOnHand`
- cancelled transfers release reserved quantity if dispatch has not occurred

### Adjustment Workflow

The adjustment lifecycle for V1 is:

1. `Draft`
2. `PendingApproval`
3. `Approved`
4. `Rejected`

Behavior rules:

- adjustments at or below threshold can move directly to `Approved`
- adjustments above threshold must enter `PendingApproval`
- only operations managers can approve or reject pending adjustments
- inventory changes are applied only when the adjustment reaches `Approved`

## Non-Functional Requirements

- internal-business-grade reliability
- clear role-based authorization
- straightforward operational support
- maintainable code boundaries
- deployment simplicity over early-scale optimization

## Current Risks

- endpoint modules still contain more orchestration than an ideal application layer would
- deployment docs and implementation currently differ from the desired long-term single-container production shape
- frontend and backend test coverage are still shallow for a full MVP claim
- local `EnsureCreated` workflow is convenient for the tutorial but not a production migration strategy

## Risk Mitigations

- keep architecture intent explicit in the docs even where the tutorial implementation is simplified
- add integration tests before presenting the system as production-ready
- document the difference between tutorial-local runtime and recommended production deployment
- treat the current implementation as a strong teaching baseline, not as a finished production template

## Readiness Gates

The tutorial is implementation-ready for current scope. It is not yet production-ready.

Production-oriented work should start only after:

- endpoint and authorization integration tests exist
- frontend workflow tests exist
- SPA build and serving strategy is finalized
- deployment and data-evolution strategy is revisited beyond the current clean-local-db approach

## Definition Of Current Tutorial Complete

This tutorial is at a good learning baseline when:

- the documented layered monolith workflows are implemented through Phase 4
- auth and warehouse scoping are enforced
- local runtime works end to end
- the docs accurately describe both the intended architecture and the current implementation status

## Recommendation

Treat `001` as a strong completed tutorial baseline for learning layered monolith design, with a clear next phase focused on test hardening, production packaging, and deeper layering refinements.
