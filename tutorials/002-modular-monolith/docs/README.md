# Modular Monolith

## Overview

A modular monolith is a single deployable application that is intentionally divided into strong business modules with explicit boundaries.

It keeps monolith deployment simplicity while reducing coupling through module ownership, internal contracts, disciplined data access, and testable boundaries.

For this tutorial, the architecture is applied to a concrete system: a B2B wholesale operations platform for a growing distributor that serves business customers across sales, warehouse, and finance teams.

## Why This Tutorial Matters

A modular monolith is one of the most practical modern architectures for business systems that are too broad for a simple monolith but do not yet justify microservices.

It gives teams:

- clearer business ownership than a loosely structured monolith
- lower operational cost than microservices
- a realistic evolution path when the system grows further
- a better place to prove module boundaries before any service extraction

## Best Used When

- the product has multiple meaningful business capabilities
- the team wants strong internal boundaries without service deployment overhead
- independent code ownership matters before independent deployment does
- the system is expected to grow significantly over time
- operational simplicity is still a priority

## Not Ideal When

- separate teams already require independent deployment cycles immediately
- modules need materially different runtime or scaling characteristics right now
- the organization can already support the cost of distributed systems and has a clear reason to do so

## Recommended Technology

Recommended tutorial baseline:

- Frontend: React
- Backend: ASP.NET Core
- Database: PostgreSQL or SQL Server
- ORM: Entity Framework Core
- Hosting: one application deployment unit where practical

Recommended modular backend structure:

- one host application
- one shared runtime
- internal modules organized by business capability
- one relational database with module ownership boundaries

## Example Project

**Project idea:** B2B wholesale operations platform

Concrete scenario:

- the company sells office technology and business equipment to business customers
- sales coordinators create customer orders
- warehouse teams reserve and fulfill stock
- finance officers issue invoices and track payment state
- operations managers need cross-module reporting without moving to microservices too early

Core modules in scope:

- `Catalog`
- `Customers`
- `Orders`
- `Inventory`
- `Billing`
- `Reporting`
- `Identity`

Business objective:

- manage customer accounts, products, orders, inventory, and invoices in one application
- preserve clear ownership boundaries inside the codebase
- avoid the tight coupling typical of an unstructured monolith
- keep future extraction options open if a few modules later need to split out

## Project Scope

### In Scope

- business-customer account management
- product catalog management
- order creation and lifecycle tracking
- inventory reservation and fulfillment support
- invoice generation and payment-state tracking
- role-based internal access
- reporting views across modules
- explicit automated testing strategy for module, workflow, and UI boundaries
- a documented MVP test suite plan covering backend, frontend, and end-to-end validation

### Out Of Scope For The First Implementation

- public storefront or self-service customer portal
- real payment gateway processing
- external ERP integration
- advanced forecasting and recommendation engines
- tenant-per-customer SaaS isolation

## Implementation Status

This tutorial is currently documentation-first.

Implemented now:

- folder structure for `docs/` and `implementation/`
- a complete documentation baseline for MVP scope, architecture, deployment, and testing

Not started yet:

- the buildable `002` implementation

The goal of this document set is to make `002` implementation-ready before coding begins, including a clear definition of how the system should be tested.

## MVP Testing Position

For this tutorial, testing is part of the architecture baseline, not a later hardening task.

The MVP is only considered complete when it includes:

- domain tests inside each module
- application and orchestration tests for cross-module workflows
- API integration tests for the host application
- frontend tests for critical operator workflows
- end-to-end smoke checks for the main wholesale business path

## Tutorial Contents

- [Learning Guide](./learning-guide.md)
- [Project Plan](./project-plan.md)
- [Architecture Guide](./architecture.md)
- [Implementation Blueprint](./implementation-blueprint.md)
- [Deployment Guide](./deployment.md)
- [Testing Strategy](./testing-strategy.md)

## Intended Audience

- developers learning how modular monoliths differ from both layered monoliths and microservices
- technical leads deciding whether stronger internal modularity is needed before service decomposition
- contributors who need a shared project definition before implementation begins

## What You Should Learn From This Tutorial

By the end of this tutorial, a developer should understand:

- why a modular monolith is often the right next step after a simple monolith
- how to divide one deployable application into business-owned modules
- how module boundaries affect code structure, database ownership, and API design
- how internal module contracts differ from service-to-service communication
- how a modular monolith should be tested at domain, module, API, and UI levels
- when this architecture should remain a monolith and when it may need further evolution later

## Definition Of Documentation Accuracy

This tutorial documentation is accurate when a reader can answer all of the following without guessing:

- what business problem the system solves
- what modules exist and why
- what data each module owns
- how modules are allowed to interact
- what is planned for the first implementation
- how the MVP should be tested
- what remains future work
- how the application is intended to be deployed

## Tradeoffs

- better internal maintainability than a loosely structured monolith
- easier evolution path than a tightly coupled monolith
- lower operational burden than microservices
- still one deployment unit, so some changes still ship together
- requires stronger discipline than a layered monolith because module boundaries must be enforced explicitly in both code and tests
