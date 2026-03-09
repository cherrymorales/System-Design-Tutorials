# Modular Monolith Implementation Blueprint

## Example System

This tutorial uses a **B2B wholesale operations platform** as the example system.

Concrete business scenario:

- company: a growing distributor selling office technology and business equipment
- customers: business accounts with internal billing and shipping contacts
- staff: sales coordinators, warehouse operators, finance officers, and operations managers
- locations: one central warehouse in V1, with room to expand later

Primary capabilities:

- customer management
- product catalog
- sales orders
- inventory reservation
- invoice generation
- operational reporting

## Product Goals

- provide one internal system for customer, order, inventory, and billing workflows
- avoid the tight coupling of a generic monolith
- keep the application simple to deploy while preserving module ownership
- establish boundaries that could support future extraction if required later
- make the testing approach explicit enough that the architecture can be demonstrated, not only described

## MVP Scope

The first implementation should deliver:

- customer module CRUD
- product catalog CRUD
- order creation and lifecycle tracking
- inventory reservation against order demand
- invoice generation and invoice status tracking
- reporting dashboards across orders, inventory, and billing
- role-based access for internal users
- automated tests for domain rules, cross-module workflows, API behavior, and key frontend flows

The first implementation should not deliver:

- public customer self-service
- external payment gateway integration
- shipment carrier integration
- ERP sync
- advanced pricing engine
- customer-specific contract pricing in V1

## Learning Focus

When reading this document, focus on:

- how a broad business system becomes a set of modules rather than a set of services
- how cross-module workflows remain explicit inside one deployment unit
- how data ownership and API shape follow module boundaries
- how a modular monolith should be tested beyond isolated unit tests

## Why This Example Fits

This domain is strong for a modular monolith because it has:

- multiple business capabilities that need clear ownership
- enough complexity that a basic monolith would become tangled quickly
- enough shared workflow that microservices would add cost too early
- strong value from shared deployment, transactional simplicity, and testable in-process contracts

## Suggested Stack

- Frontend: React with React Router
- Backend: ASP.NET Core host plus internal modules
- Data access: Entity Framework Core
- Database: PostgreSQL
- Auth: ASP.NET Core Identity for V1

## Locked V1 Decisions

- use ASP.NET Core Identity for authentication and role management
- organize the backend by business modules, not only by technical layers
- keep one deployable backend application and one shared relational database
- enforce module-owned writes only through the owning module
- perform cross-module workflows through explicit in-process contracts
- allow reporting to read across modules but not own operational writes
- allow a clean recreated local database workflow for the tutorial implementation baseline
- treat automated tests as part of the MVP, not as post-MVP hardening

## Non-Functional Requirements

- module ownership must be obvious in the codebase
- common operational workflows should stay easy to reason about
- authorization should align with module capabilities
- local setup should remain straightforward
- deployment should remain single-application-first
- the test suite should make module contracts and workflow behavior visible

## Suggested Backend Structure

```text
src/
  backend/
    Host/
    Modules/
      Catalog/
        Api/
        Application/
        Domain/
        Infrastructure/
      Customers/
        Api/
        Application/
        Domain/
        Infrastructure/
      Orders/
        Api/
        Application/
        Domain/
        Infrastructure/
      Inventory/
        Api/
        Application/
        Domain/
        Infrastructure/
      Billing/
        Api/
        Application/
        Domain/
        Infrastructure/
      Reporting/
        Api/
        Application/
        Domain/
        Infrastructure/
      Identity/
        Api/
        Application/
        Domain/
        Infrastructure/
    BuildingBlocks/
```

## Example Use Cases

### Sales And Customer Operations

- create a business customer account
- maintain billing and shipping contacts
- create a new wholesale order for laptops, monitors, and accessories
- review order status from `Draft` to `Completed`

### Inventory And Fulfillment

- validate whether the requested quantities are available
- reserve inventory when the order is submitted
- view fulfillment-ready orders by warehouse context
- release reservations when an order is cancelled before invoicing

### Billing And Reporting

- issue an invoice when an order becomes invoice-ready
- mark invoices as paid for internal tracking
- view reporting summaries that combine orders, inventory reservations, and invoice status

## Primary Screens

- login page
- customer list and customer detail page
- products list and product detail page
- orders dashboard and order detail page
- inventory reservation and fulfillment view
- invoice list and invoice detail page
- reporting dashboard for order pipeline, reservation health, and invoice status

## Primary User Roles

- `SalesCoordinator`: customer management, order creation, order review
- `WarehouseOperator`: inventory reservation review and fulfillment status
- `FinanceOfficer`: invoice generation and invoice status management
- `OperationsManager`: cross-module reporting and operational oversight

## Module Boundaries

### Customers

Owns:

- customer accounts
- account status
- contacts

### Catalog

Owns:

- products
- SKUs
- categories

### Orders

Owns:

- order headers
- order lines
- order lifecycle state

### Inventory

Owns:

- stock positions
- reservations
- fulfillment readiness support data

### Billing

Owns:

- invoices
- invoice state

### Reporting

Owns:

- dashboards and summary read models

### Identity

Owns:

- users
- roles
- access rules

## Order State Machine

States:

- `Draft`
- `Submitted`
- `Reserved`
- `ReadyForInvoicing`
- `Invoiced`
- `Completed`
- `Cancelled`

State rules:

- a new order starts as `Draft`
- submission validates customer and product availability assumptions
- reservation occurs through the `Inventory` module, not inside `Orders`
- invoice creation may only happen after the order reaches `ReadyForInvoicing`
- cancellation must release reservation if the order has not progressed too far

## Invoice State Machine

States:

- `Draft`
- `Issued`
- `Paid`
- `Voided`

State rules:

- invoice creation is initiated from an order workflow but owned by `Billing`
- `Billing` controls invoice transitions
- orders may store a billing reference but do not own billing state

## End-To-End Example Workflow

1. A sales coordinator creates a customer order for `12` monitors and `12` docking stations.
2. The `Orders` module validates customer and order-line structure.
3. `Orders` requests a reservation from `Inventory`.
4. `Inventory` confirms available stock and creates the reservation.
5. `Orders` moves the order from `Submitted` to `Reserved`.
6. Warehouse staff prepare the order for invoicing.
7. `Orders` requests invoice creation from `Billing`.
8. `Billing` creates the invoice and returns a reference.
9. `Orders` records invoice linkage without owning invoice data.
10. `Reporting` later reads order, reservation, and invoice states for operational summaries.

## Domain Model Outline

### Customer

Key fields:

- `id`
- `accountCode`
- `name`
- `status`
- `billingContact`
- `shippingContact`

### Product

Key fields:

- `id`
- `sku`
- `name`
- `category`
- `unitPrice`
- `status`

### Order

Key fields:

- `id`
- `customerId`
- `status`
- `submittedAt`
- `reservationReference`
- `invoiceReference`
- `createdBy`
- `createdAt`

### OrderLine

Key fields:

- `orderId`
- `productId`
- `quantity`
- `unitPrice`

### StockItem

Key fields:

- `productId`
- `warehouseId`
- `quantityOnHand`
- `quantityReserved`
- `updatedAt`

### InventoryReservation

Key fields:

- `id`
- `orderId`
- `status`
- `reservedAt`
- `releasedAt`

### Invoice

Key fields:

- `id`
- `customerId`
- `orderId`
- `status`
- `issuedAt`
- `paidAt`
- `totalAmount`

## Example API Surface

```text
GET    /api/customers
GET    /api/customers/{id}
POST   /api/customers
PUT    /api/customers/{id}
GET    /api/catalog/products
POST   /api/catalog/products
PUT    /api/catalog/products/{id}
GET    /api/orders
GET    /api/orders/{id}
POST   /api/orders
POST   /api/orders/{id}/submit
POST   /api/orders/{id}/reserve
POST   /api/orders/{id}/cancel
GET    /api/inventory/stock
GET    /api/inventory/reservations
GET    /api/billing/invoices
POST   /api/billing/invoices
POST   /api/billing/invoices/{id}/issue
POST   /api/billing/invoices/{id}/mark-paid
GET    /api/reports/order-pipeline
GET    /api/reports/reservation-health
GET    /api/reports/invoice-summary
```

## Example Domain Rules

- orders cannot reference inactive customers
- order lines cannot reference archived products
- reservations must be created only by the `Inventory` module
- one module cannot directly mutate another module's operational tables
- invoices cannot be issued before the order reaches the invoice-ready state
- reporting may summarize module data but must not write operational state

## React Frontend Guidance

The React app should be feature-oriented and aligned to business modules.

Suggested shape:

```text
frontend/
  src/
    features/
      customers/
      catalog/
      orders/
      inventory/
      billing/
      reports/
    shared/
    app/
```

Suggested route shape:

```text
/dashboard
/customers
/customers/:id
/products
/products/:id
/orders
/orders/:id
/inventory
/invoices
/invoices/:id
/reports
```

## Data Model Starting Point

Core tables:

- `customers.customers`
- `catalog.products`
- `orders.orders`
- `orders.order_lines`
- `inventory.stock_items`
- `inventory.reservations`
- `billing.invoices`
- `identity.users`
- `identity.roles`

Optional reporting read tables later:

- `reporting.order_pipeline_view`
- `reporting.invoice_summary_view`

## Acceptance Criteria For MVP

- a user can create and update customer records
- a user can create and update product records
- a sales coordinator can create and submit orders
- an order can request inventory reservation through the `Inventory` module
- reservation ownership remains inside `Inventory`
- a finance officer can issue and update invoice status
- managers can view cross-module reporting summaries
- code structure makes module ownership visible
- modules do not directly write each other's operational tables
- automated tests prove domain rules, cross-module workflows, API behavior, and key frontend paths

## Testing Strategy

Testing is part of the MVP.

Coverage should include:

- unit tests for domain rules inside each module
- application tests for module workflows
- contract tests for cross-module interaction paths
- API integration tests for module endpoints
- frontend tests for critical order and invoice workflows
- seeded-data smoke tests for the full order -> reservation -> invoice path

Priority scenarios to test first:

- order submission fails for inactive customer
- inventory reservation fails for insufficient stock
- invoice creation fails before order readiness
- reservation release occurs on eligible cancellation
- reporting queries do not require write-side coupling
- role boundaries are enforced for sales, warehouse, finance, and manager flows

Expected first test layers:

- `Domain tests`: validate entity rules and state transitions inside each module
- `Application tests`: validate use cases and orchestration within a module boundary
- `Contract tests`: validate in-process module interactions such as `Orders -> Inventory` and `Orders -> Billing`
- `API integration tests`: validate host-level routing, auth, serialization, and module composition
- `Frontend tests`: validate the operator workflows that show the architecture from the user side
- `Smoke tests`: validate the seeded end-to-end business flow through the running application

See [Testing Strategy](./testing-strategy.md) for the detailed test matrix.

## What To Keep Simple

- one deployable application
- one database
- one host application composing modules
- one local Docker workflow

## What To Avoid Early

- turning every module into a service before boundaries are proven in-process
- creating a giant shared project that hides module ownership
- allowing modules to share persistence shortcuts freely
- building reporting as a write path
- treating the tests as optional documentation rather than part of the architecture baseline

## Evolution Path

If the system grows further, good future options are:

- stronger module isolation inside the monolith first
- selective service extraction for a few modules only if deployment pressure becomes real

Likely candidates for future extraction if genuinely needed later:

- `Billing`
- `Reporting`
- `Orders`

## Implementation Ready Baseline

Use the following baseline without further architectural debate during V1 implementation:

- one backend host application
- clear business-module structure
- one relational database with module-owned writes
- explicit in-process contracts for cross-module workflows
- reporting as read-only cross-module aggregation
- single-application-first deployment model
- automated test coverage included as part of the MVP baseline
