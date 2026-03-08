# Layered Monolith Deployment Guide

## Deployment Philosophy

The layered monolith should stay operationally simple.

This tutorial currently optimizes for a clean local learning workflow first, while still documenting a sensible production direction.

## Current Tutorial Runtime

What is implemented now in this repository:

- React runs as a Vite dev server during development
- ASP.NET Core runs as a separate backend process or container
- PostgreSQL runs as a separate local container
- Docker Compose starts the API and database for local use
- the local database is recreated and seeded from the model for a clean tutorial environment

Important implication:

- the current implementation is not yet packaged as a single app container serving the built SPA
- the current local workflow does not use EF Core migrations

## Why This Is Acceptable For The Tutorial

For learning, this keeps iteration simple:

- frontend and backend can be worked on independently
- the database state is predictable on each fresh container start
- the user can focus on architecture and workflow behavior instead of deployment complexity

## Recommended Production Shape

Recommended future production-oriented shape:

```text
[ React static files + ASP.NET Core app ] -> single container
[ PostgreSQL ] -> managed database service
```

This remains the preferred long-term direction for this architecture.

## Learning Focus

When reading this document, focus on:

- the difference between the current tutorial runtime and the recommended production shape
- why a monolith can still benefit from simple deployment packaging
- why local-learning shortcuts should be documented explicitly instead of being treated as production defaults

## Environment Strategy

Current environments effectively supported by the tutorial:

- `local`: fully supported
- `dev/test/prod`: documented as future direction, not fully packaged in the repository yet

Recommended non-local environments for future work:

- `dev`: shared integration environment
- `test`: pre-release validation environment
- `prod`: internal production environment

## Local Development Shape

Current local setup:

- one API container
- one PostgreSQL container
- one Vite dev server for the frontend

This still preserves the layered monolith design because the application remains one backend deployment unit with one database.

## Container Notes

Current container responsibilities:

- API container hosts the ASP.NET Core backend
- DB container hosts PostgreSQL
- frontend is run separately in development

Recommended future image responsibilities:

- host the API
- serve the built React SPA
- expose health endpoints
- emit structured logs to stdout

## Environment Configuration

Current configuration concerns:

- database connection string
- ASP.NET Core Identity settings
- API and frontend development ports
- approval threshold configuration in code and domain rules
- warehouse assignment seeding for local users

Secrets should never be stored in source control.

## Observability

Current baseline:

- health endpoints exist
- runtime verification is possible through Docker and API checks

Still recommended:

- structured application logs
- request tracing
- error tracking
- domain-focused metrics for transfer failures, pending approvals, and low-stock generation

## Security And Access

Current operational baseline:

- authenticated access for protected API routes
- role-based authorization on write operations
- operator warehouse scoping
- audit metadata recorded on inventory-changing workflow entities
- seeded local users and roles for tutorial testing

## CI/CD Expectations

Current verified pipeline steps:

1. run backend tests
2. build frontend assets
3. build API container
4. run Docker Compose locally

Recommended future release gates:

1. backend integration tests pass
2. frontend tests pass
3. built SPA is packaged with the backend
4. smoke tests pass in a non-local environment
5. data-evolution strategy is defined beyond the current clean-local-db workflow

## Data Evolution Strategy

Current tutorial strategy:

- use `EnsureCreated` against a clean recreated local database
- avoid EF Core migrations for the local teaching workflow
- rely on seeding to create a known starting state

Recommended future production strategy:

- introduce version-controlled migrations
- apply schema changes in a controlled deployment step
- stop using destructive recreation outside local learning scenarios

## Backup And Recovery

Current tutorial implementation does not provide production backup logic.

Recommended production baseline:

- scheduled PostgreSQL backups
- documented restore procedure
- recovery expectations aligned with inventory data criticality

## Operational Risks

Current risks:

- local database recreation is destructive by design
- frontend and backend are not yet packaged into one production-style app container
- deployment guidance can be misread as production-ready if the tutorial/runtime distinction is not explicit

## Readiness Status

Current status:

- local tutorial runtime is ready and verified
- production deployment shape is only partially implemented

## Recommended Next Deployment Step

When the tutorial moves beyond the current learning baseline, the next deployment improvement should be:

- build the React frontend into static assets
- serve those assets from ASP.NET Core
- keep PostgreSQL separate
- revisit data evolution with migrations for non-local environments

## Warning Signs The Deployment Model Is Breaking Down

- the backend and frontend require different release cadences in practice
- startup time grows significantly
- one capability needs materially different scaling
- local convenience assumptions leak into production expectations

If that happens, improve modularity first before considering service decomposition.
