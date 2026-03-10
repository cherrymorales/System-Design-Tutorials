# Layered Monolith Implementation Blueprint

## Example System

This tutorial uses an **inventory and warehouse management system** as the example system.

Concrete business scenario:

- company: a mid-sized retail distributor
- products: laptops, monitors, office chairs, and related equipment
- warehouses: Brisbane, Sydney, and Melbourne
- users: warehouse operators, purchasing officers, inventory planners, and operations managers

Primary capabilities:

- product catalog
- warehouse management
- stock receiving and adjustments
- stock transfers
- low-stock reporting
- identity-driven authorization

## Product Goals

- provide a single source of truth for stock across all warehouses
- replace manual spreadsheet updates with controlled workflows
- make stock changes auditable
- reduce avoidable stockouts by surfacing reorder risk early

## Current Implementation Status

Implemented now in this repository:

- product catalog CRUD
- warehouse CRUD
- inventory receipt workflow
- stock transfer workflow
- stock adjustment workflow with approval threshold
- low-stock reporting
- login, logout, session retrieval, and protected routes
- role-based access for the four primary user roles
- warehouse assignment scoping for operators
- backend API integration tests for core auth and warehouse-scoping behavior
- frontend automated tests for core login-screen behavior

Not yet implemented:

- production packaging that serves built frontend assets from ASP.NET Core
- richer reporting beyond the current low-stock and workflow views
- strong application-layer use-case services beyond the current simple baseline

## Learning Focus

When reading this document, focus on:

- how concrete workflows turn architecture into implementation scope
- how user roles and acceptance criteria shape system behavior
- how a tutorial implementation may intentionally simplify internal structure while preserving the architectural lessons

## Why This Example Fits

This domain is ideal for a layered monolith because it is:

- rich in business rules
- mostly transactional
- consistency-sensitive
- easy to understand without distributed infrastructure

## Stack

Current implementation stack:

- Frontend: React, React Router, Vite, direct `fetch`
- Backend: ASP.NET Core minimal APIs
- Data access: Entity Framework Core
- Database: PostgreSQL
- Auth: ASP.NET Core Identity with cookie authentication

Recommended future refinement:

- move workflow orchestration into explicit application-layer services
- optionally adopt a client-side data library later if frontend complexity grows

## Locked V1 Decisions

- use ASP.NET Core Identity for authentication and role management
- do not model suppliers as a first-class entity in MVP
- reserve inventory when a transfer is created
- decrement source `quantityOnHand` only when a transfer is dispatched
- increment destination `quantityOnHand` when a transfer is received
- require manager approval when `absolute(quantityDelta) > 10` or `absolute(quantityDelta * unitCost) > AUD 1,000`
- restrict warehouse operators to assigned warehouses only

## Non-Functional Requirements

- common read operations should remain straightforward and low latency for internal usage
- all stock-changing actions must record actor and timestamp
- authenticated access only
- maintainable code boundaries even inside a single deployment unit
- simple local setup for teaching and experimentation

## Actual Backend Structure

```text
src/
  backend/
    SystemDesignTutorials.LayeredMonolith.Web/
      Contracts/
      Endpoints/
      Program.cs
    SystemDesignTutorials.LayeredMonolith.Application/
      DependencyInjection/
    SystemDesignTutorials.LayeredMonolith.Domain/
      Entities/
      Enums/
    SystemDesignTutorials.LayeredMonolith.Infrastructure/
      Identity/
      Persistence/
      Seeding/
```

Important note:

- the project layout follows layered boundaries
- the current implementation keeps most workflow orchestration in endpoint modules rather than dedicated application services
- this is acceptable for the current tutorial stage, but it is not the cleanest long-term layering outcome

## Primary Screens

Implemented now:

- login page
- dashboard with current role/session context
- product management view
- warehouse management view
- inventory summary view
- stock receipt form
- stock transfer form and lifecycle actions
- stock adjustment form and approval actions
- low-stock reporting view

## Primary User Roles

- `WarehouseOperator`: receives stock, submits adjustments, dispatches and receives transfers for assigned warehouses
- `InventoryPlanner`: reviews stock levels and creates or approves transfers
- `PurchasingOfficer`: manages product replenishment-related data and can record receipts
- `OperationsManager`: manages warehouses, approves adjustments, and can perform cross-warehouse operational overrides

## Authorization Rules

Implemented now:

- all `/api/*` endpoints except `/api/health` and `/api/auth/login` require authentication
- warehouse operators can create receipts and adjustments only for assigned warehouses
- warehouse operators can view inventory, receipts, transfers, and warehouse data only for assigned warehouses
- inventory planners can view all warehouses and create or approve transfers
- purchasing officers can manage product data and create receipts across warehouses
- operations managers can view all warehouses, approve or reject pending adjustments, and cancel transfers before dispatch

## Transfer State Machine

States:

- `Requested`
- `Approved`
- `Dispatched`
- `Received`
- `Cancelled`

State rules:

- a new transfer starts as `Requested`
- approving a transfer keeps stock reserved but does not change on-hand stock
- dispatching a transfer decreases source `quantityOnHand` and clears the reservation
- receiving a transfer increases destination `quantityOnHand`
- cancelling is allowed only before `Dispatched`

## Adjustment State Machine

States:

- `Draft`
- `PendingApproval`
- `Approved`
- `Rejected`

State rules:

- a new adjustment begins as `Draft`
- if it exceeds threshold, submission moves it to `PendingApproval`
- if it is within threshold, submission moves it directly to `Approved`
- only `Approved` adjustments affect inventory balances
- `Rejected` adjustments remain auditable and immutable

## Domain Model Outline

### Product

Key fields:

- `id`
- `sku`
- `name`
- `category`
- `supplierCode`
- `unitCost`
- `status`

### Warehouse

Key fields:

- `id`
- `code`
- `name`
- `city`
- `status`

### InventoryItem

Key fields:

- `productId`
- `warehouseId`
- `quantityOnHand`
- `quantityReserved`
- `reorderThreshold`
- `updatedAt`

### StockTransfer

Key fields:

- `id`
- `sourceWarehouseId`
- `destinationWarehouseId`
- `productId`
- `quantity`
- `status`
- `requestedBy`
- `approvedBy`
- `dispatchedBy`
- `receivedBy`
- `cancelledBy`
- `reason`
- `cancellationReason`
- `createdAt`

### InventoryAdjustment

Key fields:

- `id`
- `warehouseId`
- `productId`
- `quantityDelta`
- `reasonCode`
- `status`
- `submittedBy`
- `submittedAt`
- `approvedBy`
- `approvedAt`
- `rejectedBy`
- `rejectedAt`
- `notes`
- `createdAt`
- `requiresApproval`

### InventoryReceipt

Key fields:

- `id`
- `warehouseId`
- `productId`
- `quantityReceived`
- `supplierReference`
- `receivedBy`
- `receivedAt`

### UserWarehouseAssignment

Key fields:

- `userId`
- `warehouseId`
- `assignedAt`

## Current API Surface

```text
GET    /api/health
POST   /api/auth/login
POST   /api/auth/logout
GET    /api/auth/me
GET    /api/products
GET    /api/products/{id}
POST   /api/products
PUT    /api/products/{id}
POST   /api/products/{id}/archive
GET    /api/warehouses
GET    /api/warehouses/{id}
POST   /api/warehouses
PUT    /api/warehouses/{id}
POST   /api/warehouses/{id}/deactivate
GET    /api/inventory/summary
GET    /api/inventory/receipts
POST   /api/inventory/receipts
GET    /api/transfers
POST   /api/transfers
POST   /api/transfers/{id}/approve
POST   /api/transfers/{id}/dispatch
POST   /api/transfers/{id}/receive
POST   /api/transfers/{id}/cancel
GET    /api/adjustments
GET    /api/adjustments/pending
POST   /api/adjustments
POST   /api/adjustments/{id}/approve
POST   /api/adjustments/{id}/reject
GET    /api/reports/low-stock
```

## Example Domain Rules

- inventory cannot be reduced below available stock
- a stock transfer must reference distinct source and destination warehouses
- archived products cannot receive new stock transactions
- high-value adjustments require manager approval
- cancelling a transfer after dispatch is not allowed in V1
- warehouse operators cannot create transactions for unassigned warehouses
- stock-changing actions are attributed from the authenticated identity, not from client-supplied actor fields

## Frontend Structure Note

Current implementation:

- one main `App.tsx` orchestrates the tutorial console
- route protection is handled with React Router
- API calls use direct `fetch`

Recommended future refinement:

```text
frontend/
  src/
    features/
      products/
      warehouses/
      inventory/
      transfers/
      reports/
    shared/
    app/
```

That feature-oriented shape is still a good next step if the frontend grows.

## Data Model Starting Point

Core tables in practice:

- `products`
- `warehouses`
- `inventory_items`
- `stock_transfers`
- `inventory_adjustments`
- `inventory_receipts`
- `aspnetusers`
- `aspnetroles`
- `user_warehouse_assignments`

## Acceptance Criteria For Current Tutorial Baseline

- a signed-in user can create and update products when their role allows it
- a signed-in user can create and update warehouses when their role allows it
- a warehouse operator can record incoming stock receipts for assigned warehouses
- an inventory planner can create a transfer and the system validates available stock
- transfer creation reserves quantity at the source warehouse
- transfer dispatch reduces source on-hand stock
- transfer receipt increases destination on-hand stock
- an operator can submit a stock adjustment for an assigned warehouse
- an operations manager can approve or reject adjustments above threshold
- operators cannot access warehouses outside their assignments
- low-stock items can be viewed from the dashboard
- all stock-changing operations record audit metadata directly on the workflow entities

## Testing Strategy

Current coverage:

- domain-level tests for inventory, product, warehouse, transfer, and adjustment behavior
- API integration tests for auth and warehouse-scoped authorization behavior
- frontend tests for login-screen fallback and seeded-user interaction
- manual runtime verification for login, authorization, and warehouse scoping

Recommended next coverage:

- broader API integration tests for transfer and adjustment workflows
- frontend tests for core workflow screens beyond login
- browser end-to-end tests for cross-screen user flows

## What To Keep Simple

- one main database
- one deployable backend application
- one shared auth model
- one local Docker workflow

## What To Avoid Early

- splitting services by entity too soon
- adding messaging because it feels enterprise
- pretending the tutorial implementation is already production-hardened
- adding abstraction layers that are not yet earning their keep

## Evolution Path

The most likely next improvement is a cleaner modular monolith style inside the same deployment unit.

Good next boundaries:

- catalog
- warehouse operations
- inventory control
- transfers and approvals
- reporting
- identity and access

## Implementation Baseline Summary

Use the current codebase as a practical tutorial implementation, not as a finished production template.

It already demonstrates:

- strong transactional workflows
- clear role boundaries
- warehouse-scoped authorization
- a realistic React + ASP.NET Core stack

It still needs:

- deeper end-to-end and smoke-test coverage
- production packaging refinement
- cleaner use-case orchestration inside the application layer
