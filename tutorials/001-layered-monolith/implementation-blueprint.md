# Layered Monolith Implementation Blueprint

## Example System

This tutorial uses an **inventory and warehouse management system** as the example system.

Concrete business scenario:

- company: a mid-sized retail distributor
- products: laptops, monitors, keyboards, office chairs, and networking gear
- warehouses: Brisbane, Sydney, and Melbourne
- users: warehouse operators, purchasing officers, inventory planners, and operations managers

Primary capabilities:

- product catalog
- warehouse management
- stock receiving and adjustments
- stock transfers
- reporting dashboard

## Product Goals

- provide a single source of truth for stock across all warehouses
- replace manual spreadsheet updates with controlled workflows
- make stock changes auditable
- reduce avoidable stockouts by surfacing reorder risk early

## MVP Scope

The first implementation should deliver:

- product catalog CRUD
- warehouse CRUD
- inventory receipt workflow
- stock transfer workflow
- stock adjustment workflow with approval threshold
- low-stock reporting
- role-based access for the four primary user roles

The first implementation should not deliver:

- supplier self-service
- purchase order management
- shipment carrier integration
- barcode hardware integration
- predictive forecasting

## Learning Focus

When reading this document, focus on:

- how concrete workflows turn architecture into implementation scope
- how user roles and acceptance criteria shape system behavior
- how data model choices reflect business rules

## Why This Example Fits

This domain is ideal for a layered monolith because it is:

- rich in business rules
- mostly transactional
- consistency-sensitive
- easy to understand without distributed infrastructure

It also mirrors the kind of internal line-of-business software that teams commonly build first as a single application.

## Suggested Stack

- Frontend: React with React Router and TanStack Query
- Backend: ASP.NET Core Web API
- Data access: Entity Framework Core
- Database: PostgreSQL
- Auth: ASP.NET Core Identity for V1

## Locked V1 Decisions

- use ASP.NET Core Identity for authentication and role management
- do not model suppliers as a first-class entity in MVP
- reserve inventory when a transfer is created
- decrement source `quantityOnHand` only when a transfer is dispatched
- increment destination `quantityOnHand` when a transfer is received
- require manager approval when `absolute(quantityDelta) > 10` or `absolute(quantityDelta * unitCost) > AUD 1,000`
- restrict warehouse operators to assigned warehouses only

## Non-Functional Requirements

- response time target: common read operations under 500 ms in normal load
- auditability: all stock-changing actions must record actor and timestamp
- availability target: standard business-hours internal application availability
- security: authenticated access only, role-based authorization on write operations
- maintainability: code must follow clear layer boundaries and module ownership

## Suggested Backend Structure

```text
src/
  Web/
    Controllers/
    Contracts/
    Middleware/
    Program.cs
  Application/
    Products/
    Warehouses/
    Inventory/
    Transfers/
    Common/
  Domain/
    Products/
    Warehouses/
    Inventory/
    Transfers/
    Common/
  Infrastructure/
    Persistence/
    Integrations/
    Reporting/
```

## Example Use Cases

### Product And Inventory Management

- create a new SKU such as `MON-27-4K`
- update product dimensions, reorder threshold, and supplier code
- receive a supplier delivery into the Brisbane warehouse
- record a stock adjustment for damaged items found during a cycle count

### Warehouse Operations

- create and manage warehouse records for Brisbane, Sydney, and Melbourne
- transfer stock from Sydney to Melbourne when Melbourne drops below threshold
- approve or reject stock adjustments above the warehouse operator limit
- view low-stock items that require replenishment or transfer

### Reporting

- stock on hand by warehouse
- inventory movement by period
- low-stock and reorder reports
- adjustment history by approver

## Primary Screens

- login page
- dashboard with low-stock and recent-activity widgets
- products list and product detail page
- warehouses list and warehouse detail page
- inventory view filtered by warehouse and product
- stock receipt form
- stock transfer form
- stock adjustment form and approval queue
- reporting page for low-stock and movement summaries

## Primary User Roles

- `Warehouse Operator`: receives stock, performs counts, submits adjustments
- `Inventory Planner`: reviews stock levels and creates transfers
- `Purchasing Officer`: manages suppliers and incoming receipts
- `Operations Manager`: approves large adjustments and oversees warehouse health

## Authorization Rules

- warehouse operators can create receipts and adjustments only for assigned warehouses
- warehouse operators can view inventory only for assigned warehouses
- inventory planners can view all warehouses and create transfers between any active warehouses
- purchasing officers can manage product replenishment fields and create receipts across all warehouses
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

## End-To-End Example Workflow

1. A supplier delivers 250 units of `LAP-14-BLK` to the Brisbane warehouse.
2. A warehouse operator records the receipt.
3. Inventory for Brisbane is increased immediately.
4. Sydney falls below its reorder threshold for the same product.
5. An inventory planner creates a transfer of 60 units from Brisbane to Sydney.
6. The application validates available stock and reserves 60 units in Brisbane.
7. Brisbane warehouse staff dispatch the transfer, reducing Brisbane on-hand stock.
8. Sydney warehouse staff receive the transfer, increasing Sydney on-hand stock.
9. A later cycle count discovers 8 damaged units in Sydney.
10. The operator submits an adjustment request.
11. The system evaluates quantity and value thresholds to decide whether manager approval is required.

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
- `lastReceiptAt`
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
- `approvedAt`
- `approvedBy`
- `dispatchedAt`
- `receivedAt`
- `cancelledAt`
- `reason`
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
- `rejectedAt`
- `notes`
- `createdAt`

### InventoryReceipt

Key fields:

- `id`
- `warehouseId`
- `productId`
- `quantityReceived`
- `supplierReference`
- `receivedBy`
- `receivedAt`

## Example API Surface

```text
GET    /api/products
GET    /api/products/{id}
POST   /api/products
PUT    /api/products/{id}
GET    /api/warehouses/{id}/inventory
POST   /api/inventory/receipts
POST   /api/transfers
POST   /api/transfers/{id}/approve
POST   /api/transfers/{id}/dispatch
POST   /api/transfers/{id}/receive
POST   /api/transfers/{id}/cancel
POST   /api/adjustments
POST   /api/adjustments/{id}/approve
POST   /api/adjustments/{id}/reject
GET    /api/reports/low-stock
```

Example payloads:

```json
POST /api/inventory/receipts
{
  "warehouseId": "brisbane",
  "productSku": "LAP-14-BLK",
  "quantity": 250,
  "supplierReference": "PO-10428"
}
```

```json
POST /api/transfers
{
  "sourceWarehouseId": "brisbane",
  "destinationWarehouseId": "sydney",
  "productSku": "LAP-14-BLK",
  "quantity": 60,
  "reason": "Replenish low stock"
}
```

Suggested additional endpoints:

```text
GET    /api/reports/movements
GET    /api/adjustments/pending
GET    /api/transfers/{id}
PUT    /api/reorder-rules/{id}
```

## Example Domain Rules

- inventory cannot be reduced below available stock unless backorders are supported
- a stock transfer must have both source and destination warehouses
- an adjustment above a configured threshold requires approval
- archived products cannot receive new stock transactions
- each product can define a reorder threshold per warehouse
- receiving and transfer operations must be auditable by user and timestamp
- cancelling a transfer after dispatch is not allowed in V1
- warehouse operators cannot create transactions for unassigned warehouses

## React Frontend Guidance

The React app should be feature-oriented rather than page-folder-only.

Suggested shape:

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

This keeps the frontend aligned with business capabilities and makes it easier to evolve later.

Suggested route shape:

```text
/dashboard
/products
/products/:id
/warehouses
/warehouses/:id
/inventory
/receipts/new
/transfers/new
/adjustments
/adjustments/pending
/reports/low-stock
```

## Data Model Starting Point

Core tables:

- `products`
- `warehouses`
- `inventory_items`
- `stock_transfers`
- `inventory_adjustments`
- `inventory_receipts`
- `reorder_rules`
- `users`

Keep the schema relational and straightforward early on. This architecture benefits from strong consistency more than clever distribution.

Recommended table additions:

- `user_warehouse_assignments`
- `audit_log`

## Acceptance Criteria For MVP

- a user can create and update products
- a user can create and update warehouses
- a warehouse operator can record incoming stock receipts
- an inventory planner can create a transfer and the system validates available stock
- transfer creation reserves quantity at the source warehouse
- transfer dispatch reduces source on-hand stock
- transfer receipt increases destination on-hand stock
- an operator can submit a stock adjustment
- an operations manager can approve or reject adjustments above threshold
- operators cannot access warehouses outside their assignments
- low-stock items can be filtered and viewed by warehouse
- all stock-changing operations write audit metadata

## Testing Strategy

- unit tests for domain rules
- application-layer tests for use-case flows
- API integration tests for key endpoints
- frontend component and page tests for critical user workflows

Priority scenarios to test first:

- receive stock updates quantity on hand
- transfer creation reserves stock without reducing on-hand stock
- transfer dispatch reduces source on-hand stock
- transfer receipt increases destination on-hand stock
- transfer fails when source stock is insufficient
- high-value adjustment enters pending approval state
- approval updates inventory and audit trail correctly
- low-stock report reflects reorder thresholds accurately

## What To Keep Simple

- one main database
- one deployable application
- one shared auth model
- one pipeline for build and deploy

## What To Avoid Early

- splitting services by entity too soon
- adding messaging because it feels "enterprise"
- overusing generic repositories where direct use-case-oriented data access is clearer
- adding too many abstraction layers before the domain demands them

## Evolution Path

If the system grows significantly, a good next step is usually a **modular monolith**, not microservices.

Likely future module boundaries:

- catalog
- warehouse operations
- inventory control
- transfers and approvals
- reporting

## Implementation Ready Baseline

Use the following baseline without further architectural debate during V1 implementation:

- local identity and role management through ASP.NET Core Identity
- supplier references only, with no supplier management module
- reservation-based transfer workflow
- threshold-based adjustment approval using the documented quantity and value rules
- warehouse-level access restrictions enforced for operators
