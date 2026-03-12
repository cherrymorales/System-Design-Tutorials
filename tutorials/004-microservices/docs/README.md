# Microservices

## Overview

Microservices split a product into multiple independently deployable services, each responsible for a focused business capability and its own operational lifecycle.

Each service owns its logic and data, while the full product emerges from explicit synchronous contracts, asynchronous events, and operational tooling around the distributed system.

For this tutorial, the architecture is applied to a concrete system: an omnichannel commerce operations platform for a national retailer that manages catalog, orders, inventory, payments, fulfillment, notifications, and dashboard projections across separate service teams.

## Why This Tutorial Matters

Microservices are common in industry, but they are also one of the architectures most often adopted too early or explained too vaguely.

This tutorial matters because it shows:

- when microservices are justified
- what real service boundaries look like
- why gateway, messaging, data ownership, and observability are part of the architecture, not optional extras
- how a microservices system should be tested beyond simple unit tests

## Best Used When

- the business domain already has multiple stable bounded contexts
- multiple teams need independent deployment and ownership
- some parts of the system need different scaling or release cadence
- asynchronous workflows and eventual consistency are acceptable and understood
- the organization can support distributed system operations

## Not Ideal When

- the team is small
- the product is early-stage
- deployment and observability maturity are low
- a modular monolith would solve the problem more simply

## Recommended Technology

Recommended tutorial baseline:

- Frontend: React SPA for the internal operations console
- Gateway/BFF: ASP.NET Core with YARP or equivalent reverse proxy
- Backend services: ASP.NET Core by default
- Messaging: RabbitMQ for tutorial-scale asynchronous workflows
- Data: PostgreSQL with database-per-service ownership
- Observability: OpenTelemetry, centralized logs, and health endpoints

Recommended service shape:

- one public gateway or BFF
- multiple independently deployed services
- one message broker
- separate service-owned databases
- one read-model service for cross-service dashboard queries

## Example Project

**Project idea:** Omnichannel commerce operations platform

Concrete scenario:

- the retailer sells consumer products across web, mobile, and customer-support-assisted channels
- a React internal operations console is used by catalog, order operations, finance, inventory, and fulfillment teams
- orders require coordination across several services instead of one shared application
- the company now needs independent service ownership, service-level scaling, and asynchronous workflow recovery

Core services in scope:

- `GatewayBff`
- `Identity`
- `Catalog`
- `Orders`
- `Inventory`
- `Payments`
- `Fulfillment`
- `Notifications`
- `OperationsQuery`

Business objective:

- replace a tightly coupled commerce backend with explicit service boundaries
- support order submission, payment authorization, stock reservation, shipment progression, and notifications through service collaboration
- keep browser interactions simple by exposing one stable gateway while preserving independent service ownership behind it
- teach how distributed workflows, eventual consistency, and multi-layer testing actually work in practice

## Project Scope

### In Scope

- gateway-based browser access to the microservices system
- role-based authenticated internal operations console
- catalog and product availability views
- order creation and order-status progression
- stock reservation and release
- payment authorization and failure handling
- fulfillment creation and shipment progression
- notification request generation
- dashboard and projection views built from service events
- automated tests covering service rules, HTTP contracts, event flows, UI workflows, and smoke paths

### Out Of Scope For The First Implementation

- public storefront UX
- real external payment provider integration
- warehouse robotics or carrier integration
- multi-region active-active deployment
- recommendation engine and search indexing
- full-blown workflow engine adoption

## Implementation Status

This tutorial is currently documentation-first.

Implemented now:

- folder structure for `docs/` and `implementation/`
- a complete implementation-ready documentation baseline for service boundaries, workflow decisions, deployment, and testing

Not started yet:

- the buildable `004` implementation

The purpose of this document set is to let implementation begin without further architectural ambiguity.

## MVP Testing Position

For this tutorial, testing is part of the architecture baseline, not a later hardening phase.

The MVP is only considered complete when it includes:

- service-level domain and application tests
- HTTP API integration tests
- event and contract tests across services
- frontend workflow tests through the gateway
- end-to-end smoke validation of the main order workflow

## Tutorial Contents

- [Learning Guide](./learning-guide.md)
- [Project Plan](./project-plan.md)
- [Architecture Guide](./architecture.md)
- [Implementation Blueprint](./implementation-blueprint.md)
- [Deployment Guide](./deployment.md)
- [Testing Strategy](./testing-strategy.md)

## Intended Audience

- developers learning when microservices are justified beyond theory
- technical leads deciding whether service decomposition is worth the operational cost
- contributors who need a precise service and workflow baseline before implementation begins

## What You Should Learn From This Tutorial

By the end of this tutorial, a developer should understand:

- how to choose service boundaries based on business ownership
- why gateway, messaging, and observability are mandatory parts of a microservices architecture
- how distributed workflows differ from in-process modular workflows
- how eventual consistency changes API and UI expectations
- how a microservices system should be tested across service, contract, UI, and smoke layers
- when this architecture is justified and when it is not

## Definition Of Documentation Accuracy

This tutorial documentation is accurate when a reader can answer all of the following without guessing:

- what business problem the system solves
- what services exist and why
- which service owns which data and workflows
- how services communicate
- what the main distributed workflow looks like
- how the MVP should be tested
- how the system is intended to be deployed locally and beyond local development
- what remains future work

## Tradeoffs

- strong service ownership and deployment independence
- better scaling and release isolation for large domains
- clearer fit for asynchronous and independently evolving capabilities
- much higher complexity in deployment, debugging, consistency, testing, and operational support
