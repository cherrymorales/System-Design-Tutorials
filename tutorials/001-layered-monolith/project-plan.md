# Layered Monolith Project Plan

## Project Summary

This project defines an internal inventory and warehouse management system for a mid-sized retail distributor operating warehouses in Brisbane, Sydney, and Melbourne.

The system will be built as a layered monolith using:

- React for the frontend
- ASP.NET Core for the backend
- PostgreSQL for the relational database

The system is intended to replace spreadsheet-driven stock tracking with a single application that supports controlled workflows, auditability, and operational visibility.

## Problem Statement

The business currently lacks a reliable, centralized way to:

- track stock across multiple warehouses
- record supplier receipts consistently
- transfer stock between locations with clear controls
- approve and audit stock adjustments
- identify low-stock conditions early

Manual processes create data inconsistency, weak auditability, and delayed operational decisions.

## Project Goals

- centralize inventory data in one system
- provide accurate stock visibility by warehouse
- make stock-changing actions auditable
- reduce operational friction for warehouse staff
- provide managers with clear approval and reporting workflows

## Learning Value

From a learning perspective, this project is useful because it demonstrates:

- how a layered monolith can support a realistic business workflow
- how project scope should be defined before implementation starts
- how architecture decisions connect to operational simplicity
- how to separate MVP requirements from future enhancements

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

## Locked Decisions For V1

The following decisions are finalized for the first implementation:

- authentication: use ASP.NET Core Identity for the first release
- supplier scope: keep only `supplierCode` and `supplierReference` fields in MVP, no supplier entity management yet
- transfer inventory behavior: creating a transfer reserves source inventory, dispatching decrements source on-hand, receiving increments destination on-hand
- adjustment approval threshold: adjustments with absolute quantity delta greater than `10` units or absolute inventory value greater than `AUD 1,000` require manager approval
- warehouse visibility: warehouse operators can only view and act on assigned warehouses, while inventory planners, purchasing officers, and operations managers can view all warehouses

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

## Recommended Delivery Phases

### Phase 1: Foundation

- project skeleton and solution structure
- authentication and role model
- product catalog module
- warehouse module
- baseline inventory schema

### Phase 2: Core Inventory Workflows

- stock receipt workflow
- inventory view by warehouse
- transfer workflow
- low-stock rule storage

### Phase 3: Controls And Reporting

- stock adjustment workflow
- approval queue
- low-stock dashboard
- movement and adjustment reporting

### Phase 4: Hardening

- audit validation
- performance tuning
- operational dashboards
- release readiness validation

## Milestones

1. Scope and document approval
2. Architecture approval
3. Data model approval
4. Deployment model approval
5. MVP implementation start
6. MVP feature complete
7. UAT and operational validation
8. Production release

## Functional Requirements

- users can create and update products
- users can create and update warehouses
- warehouse operators can record stock receipts
- inventory planners can create stock transfers
- the system validates stock before transfer creation
- users can submit stock adjustments with reason codes
- managers can approve high-value or threshold-based adjustments
- users can view low-stock items by warehouse
- all inventory-changing actions must be auditable

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
- cancelled transfers release any reserved quantity if dispatch has not occurred

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

## Major Risks

- business rules remain incomplete before coding starts
- approval thresholds are not agreed early
- data model grows inconsistently without module ownership
- reporting needs expand beyond MVP scope
- implementation starts before workflow decisions are finalized

## Risk Mitigations

- require sign-off on the document set before implementation
- define approval thresholds explicitly before coding
- keep module boundaries visible in both code and documentation
- separate MVP reporting from advanced analytics
- use acceptance criteria per workflow before development begins

## Readiness Gates

Implementation may start only when:

- the README, architecture, implementation, and deployment documents are complete
- the project scope is accepted
- the primary workflows are agreed
- the data model outline is accepted
- the deployment model is accepted
- the locked V1 decisions in this document are used as the implementation baseline

## Definition Of MVP Complete

MVP is complete when:

- the documented in-scope workflows are implemented
- acceptance criteria are met
- operational logging and health checks are in place
- deployment to a non-local environment succeeds
- users can perform core inventory tasks without external spreadsheets

## Recommendation

Proceed with implementation using this document set as the baseline project plan for V1.
