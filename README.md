# System Design Tutorials

This repository is a practical learning library for modern system design patterns used in real software projects.

The goal is to make each architecture easy to study, compare, and eventually implement through a concrete example system.

## What This Repository Contains

Each numbered tutorial focuses on one system design architecture.

Current tutorials:

1. `001-layered-monolith`
2. `002-modular-monolith`
3. `003-client-server-spa-api`
4. `004-microservices`
5. `005-event-driven-architecture`
6. `006-cqrs`
7. `007-saga-distributed-transactions`
8. `008-multitenant-saas`
9. `009-serverless-architecture`
10. `010-web-queue-worker`
11. `011-event-sourcing`

## Folder Structure

Each tutorial follows the same structure:

```text
tutorials/
  001-system-design-name/
    README.md
    docs/
      README.md
      learning-guide.md
      project-plan.md
      architecture.md
      implementation-blueprint.md
      deployment.md
    implementation/
      README.md
      src/
      tests/
      infra/
      docker/
```

Meaning:

- `README.md`: entry point for that system design
- `docs/`: learning and specification documents
- `implementation/`: buildable example implementation

## Technology Direction

Default preferences across tutorials:

- Frontend: React
- Backend: ASP.NET Core
- Database: PostgreSQL or SQL Server depending on the architecture
- Deployment: prefer a single container when that fits the design

These defaults can change if a different technology is a better fit for a specific architecture.

## How To Use This Repository

1. Start with the tutorial folder `README.md`.
2. Read the documents in `docs/` to understand the architecture and what it teaches.
3. Use `implementation/` when the tutorial is ready to be turned into a working example.

## Current Status

The most developed tutorial so far is:

- [001-layered-monolith](./tutorials/001-layered-monolith/README.md)

Its documentation includes:

- learning objectives
- project planning detail
- architecture guidance
- implementation blueprint
- deployment guidance

## Project Context

The repository-level conventions and goals are defined in:

- [PROJECT_CONTEXT.md](./PROJECT_CONTEXT.md)
