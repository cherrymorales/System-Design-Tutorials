# Modular Monolith

## What It Is

A modular monolith is a single deployable application that is intentionally split into strong internal modules with clear boundaries.

It keeps monolith deployment simplicity while reducing coupling inside the codebase.

## Best Used When

- the product needs a clean architecture but not the operational cost of microservices
- the team expects the system to grow significantly
- different business capabilities should stay isolated in code
- future extraction into services may be needed later

## Not Ideal When

- teams already need independent deployment cycles right now
- modules need completely different runtime or scaling models

## Why It Is Common

Many teams now prefer modular monoliths as a modern default because they preserve simplicity while avoiding the chaos of a poorly structured monolith.

## Recommended Technology

- Frontend: React
- Backend: ASP.NET Core with projects or folders organized by business module
- Database: PostgreSQL or SQL Server
- Hosting: single application container in most cases

## Single-Container Guidance

This design strongly supports single-container deployment. It is often the best modern compromise between speed, structure, and operational simplicity.

## Example Project

**Project idea:** Multi-department retail management platform

Example modules:

- catalog
- inventory
- orders
- billing
- reporting

## Suggested Solution Shape

- React frontend with module-aligned feature areas
- ASP.NET Core backend organized by domain modules
- per-module application services and domain models
- shared relational database with carefully managed schema ownership

## Tradeoffs

- better maintainability than a traditional monolith
- easier to evolve than a tightly coupled application
- still deployed as one unit
- requires discipline to prevent modules from becoming tangled
