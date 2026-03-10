# Layered Monolith

## Overview

A layered monolith is a single deployable application with clear internal boundaries between presentation, application, domain, and infrastructure concerns.

For this tutorial, the architecture is applied to a concrete internal system: an inventory and warehouse management platform for a mid-sized retail distributor operating warehouses in Brisbane, Sydney, and Melbourne.

## Why This Tutorial Matters

This architecture remains common in industry because it usually gives teams the fastest path to a maintainable product without the operational burden of distributed systems.

For many business systems, the right first architecture is a disciplined monolith, not microservices.

## Best Used When

- the product is early or mid-stage and requirements are still evolving
- the team is small to medium-sized
- the domain is transactional and consistency-sensitive
- operational simplicity is a priority
- one deployment cadence is acceptable

## Not Ideal When

- separate domains require independent deployment immediately
- different subsystems must scale independently right now
- many teams need strict ownership isolation
- the codebase is already too entangled for simple layering to be sufficient

## Recommended Technology

Recommended tutorial baseline:

- Frontend: React
- Backend: ASP.NET Core
- Database: PostgreSQL or SQL Server
- ORM: Entity Framework Core
- Hosting: one application deployment unit where practical

Current implementation in this repository:

- Frontend: React + React Router + Vite
- Backend: ASP.NET Core minimal APIs
- Database: PostgreSQL
- Auth: ASP.NET Core Identity with cookie authentication
- Data access: Entity Framework Core

## Example Project

**Project idea:** Inventory and warehouse management system for a mid-sized retail distributor

Concrete scenario:

- the business sells electronics and home-office equipment
- it operates three warehouses in Brisbane, Sydney, and Melbourne
- warehouse staff receive stock from suppliers
- inventory planners create transfers between warehouses
- operations managers approve large stock adjustments
- purchasing officers manage replenishment-related product data

Business objective:

- maintain accurate stock visibility across all warehouses
- reduce spreadsheet-based tracking
- prevent stockouts on high-demand items
- make stock movements and write-offs auditable

## Project Scope

### In Scope

- product catalog management
- warehouse setup and configuration
- stock receipts from suppliers
- stock transfers between warehouses
- stock adjustments with approval rules
- low-stock reporting and warehouse visibility
- role-based access for internal users

### Out Of Scope For The Current Tutorial Implementation

- supplier portal access
- barcode-scanner device integrations
- accounting and invoicing
- public e-commerce storefront
- advanced forecasting and machine learning
- multi-tenant SaaS support
- browser-based end-to-end testing
- automated container smoke tests in CI

## Implementation Status

This tutorial is no longer only a planning package. It now includes a working implementation through Phase 4.

Implemented now:

- solution and project structure for React + ASP.NET Core + PostgreSQL
- seeded startup data for products, warehouses, inventory, roles, and users
- product and warehouse CRUD
- receipt, transfer, and adjustment workflows
- low-stock reporting
- login, session handling, role-based authorization, and operator warehouse scoping
- local Docker runtime for API and database
- backend domain, workflow, and API integration tests with `14` passing tests
- frontend automated tests with `2` passing tests

Still recommended as future work:

- stronger application-layer orchestration instead of endpoint-heavy workflow handling
- browser-based end-to-end workflow tests
- automated Docker smoke tests in CI
- production packaging that serves the built SPA from ASP.NET Core in a single app container

## Tutorial Contents

- [Learning Guide](./learning-guide.md)
- [Project Plan](./project-plan.md)
- [Architecture Guide](./architecture.md)
- [Implementation Blueprint](./implementation-blueprint.md)
- [Testing Strategy](./testing-strategy.md)
- [Deployment Guide](./deployment.md)

## Intended Audience

- developers learning when a layered monolith is the right choice
- technical leads evaluating whether a monolith is sufficient for a business system
- contributors who need both the design intent and the current implementation status

## What You Should Learn From This Tutorial

By the end of this tutorial, a developer should understand:

- why a layered monolith is often a better first architecture than microservices
- how to structure a transactional business system with clear boundaries
- where business rules should live and where they should not
- how authorization and warehouse scoping affect both API design and UI behavior
- how the current testing coverage supports the business model and where more automation should be added
- where this implementation is intentionally simplified and what a next refactor would improve

## Definition Of Documentation Accuracy

This tutorial documentation is accurate when a reader can answer all of the following without guessing:

- what business problem the system solves
- what the intended architecture is
- what is already implemented in this repository
- what is still only recommended or future work
- what runtime and deployment assumptions are actually being used
- what testing is already automated and what is still manual
- what tradeoffs the current implementation is making for tutorial simplicity

## Tradeoffs

- fast to build and operate compared with distributed systems
- easier local development and debugging because most work stays in one process
- strong consistency is straightforward with one relational database
- can degrade into a tightly coupled codebase if boundaries are not enforced
- current implementation keeps some orchestration in endpoint modules for simplicity, which is practical for a tutorial but not the cleanest long-term layering