# Microservices Implementation Blueprint

## Example System

The example system for this tutorial is an omnichannel commerce operations platform for a national retailer.

The platform gives internal staff one browser application for operational workflows while the backend is divided into multiple independently deployable services.

The system allows users to:

- sign in through the gateway-backed SPA
- search product and availability data
- create and submit orders
- track payment and reservation outcomes
- progress fulfillment states
- view dashboard and order projections

## Product Goals

- support service-level ownership for a large commerce domain
- keep browser access coherent behind one gateway
- make distributed workflows explicit rather than hidden in a monolith
- preserve independent deployability and data ownership
- demonstrate realistic microservice testing and operational expectations

## Learning Focus

This blueprint should teach:

- how to translate business domains into deployable services
- how to decide what belongs in the gateway and what belongs in services
- how event-driven workflows coordinate cross-service actions
- how read models help the frontend without breaking service ownership

## Locked V1 Decisions

- frontend is a React SPA for internal operations users
- browser traffic goes through one `GatewayBff`
- services are ASP.NET Core applications
- messaging is RabbitMQ
- databases are PostgreSQL with database-per-service ownership
- `Orders` orchestrates the order-submission saga in V1
- `OperationsQuery` is read-only
- external providers are simulated where needed for MVP

## MVP Scope

### Included In V1

- login, logout, and session restore through the gateway
- product and availability lookup
- order draft creation and order submission
- inventory reservation and release
- payment authorization simulation
- fulfillment progression
- notification request emission
- dashboard and order-detail projections
- service, contract, frontend, and smoke tests

### Not Included In V1

- customer storefront
- external payment gateway
- returns workflow
- supplier replenishment
- search indexing
- workflow engine replacement of the saga baseline

## Primary User Roles

- `CatalogManager`
  Manages products and product operational status.
- `OrderOpsAgent`
  Creates and submits orders.
- `InventoryCoordinator`
  Reviews reservation and stock outcomes.
- `FinanceReviewer`
  Reviews payment state.
- `FulfillmentOperator`
  Progresses shipment state.
- `OperationsManager`
  Views projections and cross-service operational summaries.

## Primary Screens

- Login
- Dashboard
- Product lookup
- Order list
- Order detail
- Payment and reservation status panel
- Fulfillment progress panel
- Operations projection views

## Service List

### GatewayBff

Responsibilities:

- public browser entry point
- session restore
- route-level API surface for the SPA
- composition of user-facing responses where necessary

### Identity

Responsibilities:

- users
- roles
- authentication support
- user context for downstream authorization

### Catalog

Responsibilities:

- product master data
- SKU status and pricing
- order-entry product lookup

### Orders

Responsibilities:

- order aggregate
- order lines
- order lifecycle
- orchestration of order submission

### Inventory

Responsibilities:

- stock availability
- reservation records
- reservation release

### Payments

Responsibilities:

- payment authorization state
- capture or void simulation
- payment failure and compensation state

### Fulfillment

Responsibilities:

- shipment records
- shipment-state progression

### Notifications

Responsibilities:

- notification requests derived from workflow events
- send-attempt tracking if retained in V1

### OperationsQuery

Responsibilities:

- dashboard projections
- order tracking views
- service-crossing operational summaries

## Suggested Solution Structure

```text
implementation/
  src/
    gateway/
      GatewayBff/
    services/
      Identity/
      Catalog/
      Orders/
      Inventory/
      Payments/
      Fulfillment/
      Notifications/
      OperationsQuery/
    frontend/
  tests/
    services/
    contracts/
    frontend/
    smoke/
  docker/
  infra/
```

## Domain Model Starting Point

### Orders

Core fields:

- `OrderId`
- `OrderNumber`
- `Status`
- `CustomerReference`
- `Currency`
- `TotalAmount`
- `CreatedByUserId`
- `CreatedAt`
- `UpdatedAt`

Order line fields:

- `OrderLineId`
- `Sku`
- `ProductNameSnapshot`
- `Quantity`
- `UnitPrice`

### Inventory

Core fields:

- `StockItemId`
- `Sku`
- `WarehouseCode`
- `AvailableQuantity`
- `ReservedQuantity`

Reservation fields:

- `ReservationId`
- `OrderId`
- `Status`
- `CreatedAt`
- `ReleasedAt`

### Payments

Core fields:

- `PaymentId`
- `OrderId`
- `Status`
- `Amount`
- `AuthorizationReference`
- `CreatedAt`
- `UpdatedAt`

### Fulfillment

Core fields:

- `ShipmentId`
- `OrderId`
- `Status`
- `WarehouseCode`
- `TrackingReference`
- `CreatedAt`
- `UpdatedAt`

### OperationsQuery

This is a read model service, not an operational owner.

Expected projections:

- dashboard summary
- order processing summary
- failed or delayed workflow view
- fulfillment status view

## State Machines

### Order State Machine

Allowed states:

1. `Draft`
2. `Submitted`
3. `AwaitingDependencies`
4. `ReadyForFulfillment`
5. `FulfillmentInProgress`
6. `Completed`
7. `Cancelled`
8. `Failed`

Rules:

- new orders start as `Draft`
- only valid orders can be submitted
- submission creates downstream workflow requests
- only successful reservation and payment outcomes can move the order to `ReadyForFulfillment`
- delivered fulfillment completes the order
- compensation or operator cancellation can move the order to `Cancelled` or `Failed`

### Payment State Machine

Allowed states:

1. `Pending`
2. `Authorized`
3. `Captured`
4. `Failed`
5. `Voided`

### Reservation State Machine

Allowed states:

1. `Pending`
2. `Reserved`
3. `Rejected`
4. `Released`

### Shipment State Machine

Allowed states:

1. `Pending`
2. `Picking`
3. `Packed`
4. `Shipped`
5. `Delivered`
6. `Cancelled`

## End-To-End Workflow Example

### Submit Order Through Distributed Services

1. An order operations agent opens the React SPA.
2. The SPA calls the gateway to create a draft order.
3. The gateway forwards the request to `Orders`.
4. The agent submits the order.
5. `Orders` validates the draft and emits `OrderSubmitted`.
6. `Inventory` receives the event and attempts reservation.
7. `Payments` receives the event and attempts authorization.
8. `Orders` consumes both outcomes.
9. If both succeed, `Orders` emits `OrderReadyForFulfillment`.
10. `Fulfillment` creates a shipment record.
11. `Notifications` emits customer or internal notification requests.
12. `OperationsQuery` updates dashboard and order-tracking projections.

## API Surface

The SPA talks to the gateway, not to every service directly.

### Auth

```text
POST /api/auth/login
POST /api/auth/logout
GET /api/auth/me
```

### Catalog

```text
GET /api/catalog/products
GET /api/catalog/products/{sku}
GET /api/catalog/availability?sku={sku}
```

### Orders

```text
GET /api/orders
POST /api/orders
GET /api/orders/{orderId}
POST /api/orders/{orderId}/submit
POST /api/orders/{orderId}/cancel
```

Example create order request:

```json
{
  "customerReference": "CSR-ORDER-10042",
  "currency": "AUD",
  "lines": [
    {
      "sku": "SKU-HEADSET-001",
      "quantity": 2
    },
    {
      "sku": "SKU-MOUSE-002",
      "quantity": 1
    }
  ]
}
```

### Fulfillment

```text
GET /api/fulfillment/shipments/{shipmentId}
POST /api/fulfillment/shipments/{shipmentId}/pick
POST /api/fulfillment/shipments/{shipmentId}/pack
POST /api/fulfillment/shipments/{shipmentId}/ship
POST /api/fulfillment/shipments/{shipmentId}/deliver
```

### Dashboard And Projections

```text
GET /api/operations/dashboard
GET /api/operations/orders/{orderId}
GET /api/operations/orders?status={status}
```

## Integration Events

Required V1 events:

- `OrderSubmitted`
- `InventoryReservationRequested`
- `InventoryReserved`
- `InventoryReservationRejected`
- `PaymentAuthorizationRequested`
- `PaymentAuthorized`
- `PaymentAuthorizationFailed`
- `OrderReadyForFulfillment`
- `OrderSubmissionFailed`
- `ReservationReleaseRequested`
- `PaymentVoidRequested`
- `ShipmentCreated`
- `ShipmentDelivered`
- `NotificationRequested`

## Backend Rules

- each service validates and persists its own state
- services publish integration events only from their owned workflow results
- `Orders` never assumes downstream success without receiving explicit outcomes
- `OperationsQuery` remains read-only
- the gateway does not bypass service authorization
- failed distributed workflow steps must be compensatable in V1

## Frontend Rules

- the SPA restores the current session before showing protected routes
- the SPA never assumes distributed workflow completion until the gateway returns updated projection data
- pending and failed distributed states must be visible in the UI
- browser calls go only through the gateway API surface

## Acceptance Criteria

The system is implementation-ready for MVP when the future implementation can satisfy all of the following:

- a signed-in user can perform only role-appropriate actions
- an order operations agent can create and submit an order
- inventory and payment outcomes change the order through explicit service collaboration
- failed order submission triggers visible compensation behavior
- fulfillment progression updates the order projection correctly
- dashboard views reflect event-driven projections rather than browser-side data stitching
- automated tests cover service rules, contracts, frontend workflows, and smoke paths

## Testing Baseline

The detailed testing plan lives in [testing-strategy.md](./testing-strategy.md).

The MVP test baseline requires:

- service domain and application tests
- gateway and service API integration tests
- event and contract tests
- frontend workflow tests
- smoke tests for the main operational path

## Keep Simple In The First Build

- keep one public gateway
- keep one internal React SPA
- simulate external providers where practical
- keep saga orchestration in `Orders` instead of introducing a workflow engine
- keep projections focused on the main operational screens

## Evolution Path

After the MVP, sensible expansions include:

- dedicated workflow orchestration tooling
- search and indexing services
- external payment and carrier integrations
- more specialized read-model services
- regional service deployment strategies
