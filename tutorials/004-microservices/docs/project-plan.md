# Microservices Project Plan

## Project Summary

This tutorial defines an omnichannel commerce operations platform for a national retailer that now requires multiple independently deployable services.

The system will be built using:

- React for the internal operations frontend
- ASP.NET Core for the gateway and services
- RabbitMQ for asynchronous workflows
- PostgreSQL with database-per-service ownership

The goal is to provide a realistic tutorial that demonstrates when microservices are appropriate, how to define service boundaries, and how distributed workflows should be planned, implemented, deployed, and tested.

## Problem Statement

The retailer needs a centralized way to:

- manage orders across catalog, inventory, payments, and fulfillment
- avoid one shared codebase and one shared schema becoming a coordination bottleneck
- allow separate teams to release and scale parts of the system independently
- support asynchronous order processing without losing operational visibility
- expose one stable frontend entry point while keeping services independently owned
- prove through tests that the distributed workflow behaves correctly under success and failure paths

The current monolithic direction is no longer sufficient because team ownership, release cadence, and operational demands have already diverged by domain.

## Project Goals

- define clear service boundaries based on business capability
- centralize browser access through one gateway or BFF
- keep service-owned data separate
- support order submission through asynchronous distributed workflows
- make compensation and eventual consistency explicit
- make service, contract, and end-to-end testing part of the MVP baseline

## Learning Value

From a learning perspective, this project demonstrates:

- how microservices differ from modular monoliths in both code and operations
- how browser access should be shaped through a gateway instead of direct browser-to-service sprawl
- how event-driven workflows fit inside a microservices system
- why data ownership, observability, and testing are first-order architectural concerns
- how to keep a distributed system teachable without pretending it is operationally cheap

## Success Metrics

- operators can create and track orders through one browser application
- order submission coordinates payment and inventory through explicit service interaction
- fulfillment progression is visible without direct cross-service database writes
- dashboard views are built from service-owned data or projections instead of ad hoc joins
- services can be reasoned about independently by team ownership
- the MVP has automated coverage for service rules, contracts, event flows, frontend workflows, and smoke paths

## Stakeholders

- catalog managers
- order operations agents
- inventory coordinators
- finance reviewers
- fulfillment operators
- operations managers
- internal platform team

## Assumptions

- the first release is for internal authenticated staff only
- a single React operations console is the only browser client in V1
- a gateway or BFF is the only public web entry point in V1
- one message broker is acceptable for the tutorial architecture
- local development may use one PostgreSQL server hosting multiple service-owned databases
- multiple containers are required and accepted for this architecture

## Locked Decisions For V1

The following decisions are finalized for the first implementation:

- public browser access goes only through `GatewayBff`
- service set: `Identity`, `Catalog`, `Orders`, `Inventory`, `Payments`, `Fulfillment`, `Notifications`, and `OperationsQuery`
- communication rule: user-facing request flows may use synchronous HTTP where immediate response is required, while cross-service workflow progression uses messaging
- messaging rule: RabbitMQ is the V1 broker for asynchronous integration
- persistence rule: each service owns its own database schema or database; no direct operational writes across services
- query rule: `OperationsQuery` is read-only and builds projections from service events or approved service reads
- auth rule: the gateway handles browser session concerns; services enforce authorization based on forwarded user context or internal tokens
- workflow rule: `Orders` owns the order lifecycle and orchestrates the order-submission saga for V1
- consistency rule: eventual consistency is accepted for dashboard and downstream workflow views
- testing rule: MVP is not complete without service tests, HTTP integration tests, event/contract tests, frontend tests, and smoke tests

## Scope

### MVP In Scope

- gateway-authenticated operations console
- role-based access for catalog, order, inventory, finance, fulfillment, and manager roles
- product lookup and availability view
- order creation and order submission workflow
- stock reservation and reservation release
- payment authorization simulation and failure handling
- fulfillment progression and shipment-status visibility
- notification request emission
- dashboard and order-detail projections
- automated testing for the distributed workflow baseline

### Explicitly Out Of Scope

- customer-facing storefront
- external payment provider integration
- carrier label generation
- warehouse robotics integration
- multi-region disaster recovery
- complex workflow engine adoption
- full customer support ticketing

## Recommended Delivery Phases

Current status:

- Phase 1 implemented
- Phase 2 implemented
- Phase 3 implemented
- Phase 4 implemented

### Phase 1: Foundation And Platform Skeleton

Implemented deliverables:

- gateway skeleton
- service skeletons for all in-scope services
- broker and database setup
- auth baseline and seeded users
- shared tracing, health, and local run conventions
- test project setup and service run conventions

### Phase 2: Core Service Ownership

Implemented deliverables:

- `Identity` service baseline
- `Catalog` service
- `Orders` service
- `Inventory` service baseline
- service-owned persistence boundaries
- service-level domain and application tests

### Phase 3: Distributed Workflow Baseline

Implemented deliverables:

- order submission saga orchestration
- payment authorization path
- stock reservation path
- compensation behavior for failed submissions
- `Fulfillment` and `Notifications` integration points
- `OperationsQuery` read models for dashboard and order tracking
- contract and event-flow tests

### Phase 4: Hardening And Release Readiness

Implemented deliverables:

- authorization refinement across the gateway and services
- gateway composition cleanup
- frontend workflow tests
- smoke tests for the main operational path
- deployment packaging baseline
- release readiness validation

## Milestones

1. Scope and document approval
2. Service boundary approval
3. communication and data ownership approval
4. deployment and testing model approval
5. MVP implementation start
6. core services complete
7. distributed workflow baseline complete
8. full test suite baseline complete
9. release readiness review

## Functional Requirements

- users can sign in through the gateway-backed operations console
- users can search products and see product availability
- order operations agents can create and submit orders
- the system can reserve stock for submitted orders
- the system can authorize payments through a simulated payment path
- fulfillment can progress shipment state for eligible orders
- notifications can be requested from workflow events
- managers can view operational dashboard projections
- services must not write directly to another service's operational data store
- automated tests must prove the main success and failure flows

## Workflow Decisions

### Order Lifecycle

The order lifecycle for V1 is:

1. `Draft`
2. `Submitted`
3. `AwaitingDependencies`
4. `ReadyForFulfillment`
5. `FulfillmentInProgress`
6. `Completed`
7. `Cancelled`
8. `Failed`

Behavior rules:

- an order starts as `Draft`
- submission creates a snapshot of requested items and payment intent
- `Orders` emits events that trigger inventory reservation and payment authorization
- the order remains `AwaitingDependencies` until both dependencies succeed or one fails
- if both inventory and payment succeed, the order becomes `ReadyForFulfillment`
- if either dependency fails, the order becomes `Failed` and compensation steps are triggered
- cancelled orders release live reservation and fulfillment intent where applicable

### Payment Lifecycle

The payment lifecycle for V1 is:

1. `Pending`
2. `Authorized`
3. `Captured`
4. `Failed`
5. `Voided`

Behavior rules:

- payment authorization is simulated in V1
- `Payments` owns all payment state
- `Orders` does not write payment records directly
- failed or cancelled order flows can trigger payment void behavior where applicable

### Reservation Lifecycle

The inventory reservation lifecycle for V1 is:

1. `Pending`
2. `Reserved`
3. `Rejected`
4. `Released`

Behavior rules:

- reservation is owned by `Inventory`
- reservation must succeed only if sufficient stock exists
- release happens as part of compensation or cancellation
- `Orders` consumes reservation outcomes through events, not direct data access

### Shipment Lifecycle

The fulfillment lifecycle for V1 is:

1. `Pending`
2. `Picking`
3. `Packed`
4. `Shipped`
5. `Delivered`
6. `Cancelled`

Behavior rules:

- fulfillment begins only after an order is `ReadyForFulfillment`
- `Fulfillment` owns shipment progression
- delivered shipment updates the order through an explicit event path

## Non-Functional Requirements

- independently deployable services
- service-owned data boundaries
- traceable distributed workflows
- resilient handling of asynchronous failure paths
- clear operational observability
- test coverage strong enough to demonstrate distributed correctness

## Major Risks

- services are defined in docs but still coupled through database shortcuts
- gateway becomes a hidden monolith if business logic drifts into it
- asynchronous flows become hard to debug without tracing and correlation
- eventual consistency surprises the UI and operators
- the implementation overuses synchronous service calls and loses resilience
- tests verify individual services but not real distributed behavior

## Risk Mitigations

- lock service ownership before implementation starts
- keep orchestration logic explicit and traceable
- use outbox and idempotency patterns in the eventing baseline
- design the UI to reflect pending and eventually consistent states clearly
- centralize browser access through one gateway instead of exposing many browser-visible services
- include event-flow and smoke tests in the MVP baseline

## Readiness Gates

Implementation may start only when:

- the README, architecture, implementation, deployment, learning, and testing documents are complete
- the service list and ownership rules are accepted
- the main distributed workflow and compensation rules are accepted
- the deployment model is accepted
- the testing strategy is accepted as part of the MVP baseline

## Definition Of MVP Complete

MVP is complete when:

- the documented in-scope services are implemented
- the gateway exposes the required operational workflows
- the order submission saga works through success and failure paths
- dashboard projections reflect service-owned state correctly
- the application runs locally in a multi-container setup
- automated tests cover service rules, HTTP contracts, event flows, frontend workflows, and a smoke-tested main path

## Recommendation

Use this document as the implementation baseline and as the reference for evaluating whether future changes preserve the intended service boundaries and workflow behavior.
