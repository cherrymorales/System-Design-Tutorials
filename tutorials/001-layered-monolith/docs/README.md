# Layered Monolith

## Overview

A layered monolith is a single deployable application that separates responsibilities into logical layers such as presentation, application, domain, and infrastructure.

It is still one application, one codebase, and usually one database, but the internal structure is disciplined enough to keep the system understandable as it grows.

For this tutorial, the architecture is applied to a specific internal product so the design can be understood as a practical project rather than as a vague pattern.

## Why This Tutorial Matters

Layered monoliths remain one of the most common production architectures in the industry because they are fast to build, cheaper to operate than distributed systems, and often the best starting point for products that still need to learn from real users.

For many teams, the right first architecture is not microservices. It is a well-structured monolith with clear boundaries.

## Best Used When

- the product is early or mid-stage and priorities may still shift
- the team is small to medium-sized
- the domain is business-oriented and mostly transactional
- release speed matters more than service-level independence
- operational simplicity is a priority

## Not Ideal When

- different parts of the platform must scale independently right now
- many teams need separate deployments on different cadences
- the domain is so broad that a single deployment unit creates coordination bottlenecks
- the codebase already suffers from severe coupling and lack of ownership

## Recommended Technology

- Frontend: React
- Backend: ASP.NET Core
- Database: PostgreSQL or SQL Server
- ORM: Entity Framework Core
- Hosting: single application container in production when practical

## Single-Container Guidance

This is one of the best architectures for single-container deployment. A common modern approach is:

- build the React frontend into static assets
- serve those assets from the ASP.NET Core application
- run the web application as one container
- run the database separately in managed infrastructure or a second container for local development

## Example Project

**Project idea:** Inventory and warehouse management system for a mid-sized retail distributor

Concrete scenario:

- the business sells electronics and home-office equipment
- it operates three warehouses in Brisbane, Sydney, and Melbourne
- warehouse staff receive stock from suppliers
- operations managers approve large stock adjustments
- inventory planners transfer stock between warehouses to prevent stockouts

Business objective:

- maintain accurate stock visibility across all warehouses
- reduce manual spreadsheet-based inventory tracking
- prevent stockouts on high-demand items
- make stock movements and write-offs auditable

Why this is a strong example:

- it has clear business workflows
- it is mostly CRUD plus inventory rules, approvals, and reporting
- it benefits from strong consistency
- it usually does not need independent service scaling at the start

## Project Scope

### In Scope

- product catalog management
- warehouse setup and configuration
- stock receipts from suppliers
- stock transfers between warehouses
- stock adjustments with approval rules
- low-stock reporting and reorder visibility
- user roles for operational staff and managers

### Out Of Scope For The First Implementation

- supplier portal access
- barcode-scanner device integrations
- accounting and invoicing
- public e-commerce storefront
- advanced forecasting and machine learning
- multi-tenant SaaS support

## Delivery Position

This tutorial is being documented to project-plan level before implementation starts.

This document set is now intended to be implementation-ready for V1.

Implementation should use the finalized baseline in the supporting documents, especially:

- locked V1 decisions in [Project Plan](./project-plan.md)
- workflow and state definitions in [Implementation Blueprint](./implementation-blueprint.md)
- authorization and transaction rules in [Architecture Guide](./architecture.md)
- environment and release requirements in [Deployment Guide](./deployment.md)

## Tutorial Contents

- [Learning Guide](./learning-guide.md)
- [Project Plan](./project-plan.md)
- [Architecture Guide](./architecture.md)
- [Implementation Blueprint](./implementation-blueprint.md)
- [Deployment Guide](./deployment.md)

## Intended Audience

- developers who need a concrete layered monolith reference
- technical leads deciding whether a monolith is sufficient
- stakeholders reviewing scope before build starts
- future contributors who need a shared project definition

## What You Should Learn From This Tutorial

By the end of this tutorial, a developer should understand:

- how to structure a layered monolith cleanly
- how to model a real warehouse workflow without unnecessary service decomposition
- where to place business logic and where not to place it
- how React and ASP.NET Core fit together in this design
- when a single container is enough
- what warning signs suggest the system should evolve into a modular monolith or services later

They should also be able to explain:

- why a layered monolith is often a better first architecture than microservices
- which responsibilities belong in presentation, application, domain, and infrastructure
- what kinds of business systems naturally fit this architecture

## Definition Of Complete Documentation

This tutorial should be considered documentation-complete when a developer can answer all of the following without guessing:

- what business problem the system solves
- who the users are
- which workflows exist in the MVP
- what modules the application contains
- what data the system stores
- how the application is deployed
- how success will be measured
- what risks and non-goals are explicitly accepted
- what exact baseline decisions implementation should follow

## Tradeoffs

- simple to build and operate
- easier local development than distributed systems
- easier debugging because most requests stay inside one process
- can degrade into a "big ball of mud" if boundaries are not enforced
- scaling usually happens for the whole application, not for isolated capabilities
