# Modular Monolith Deployment Guide

## Deployment Philosophy

A modular monolith should stay operationally simple even while its internal module boundaries become stronger.

For this tutorial, the preferred deployment model is:

- one backend application host that loads all modules
- one frontend application
- one relational database
- single-container-first for the application runtime when practical

## Why Single Container Still Works Here

A modular monolith is still one application.

The modules are internal boundaries, not separate deployable services.

That means a strong default production shape is still:

```text
[ React static files + ASP.NET Core host + internal modules ] -> single container
[ PostgreSQL ] -> managed database service or second container locally
```

## Learning Focus

When reading this document, focus on:

- why stronger modularity does not require more deployment units
- how module boundaries affect code ownership more than runtime topology
- how to avoid introducing microservice-style infrastructure too early

## Recommended Runtime Shape

### Local Development

Recommended local shape:

- application host container or local host process
- PostgreSQL container
- optional separate frontend dev server during development

Tutorial-local preference:

- a clean recreated local database is acceptable
- local seeding should provide representative data for each module

### Production Direction

Recommended future production shape:

- build the React frontend into static assets
- serve those assets from the ASP.NET Core host
- run the host and all modules in one container
- keep PostgreSQL separate

## Environment Strategy

Recommended environments:

- `local`: full developer workflow
- `dev`: shared integration environment
- `test`: release validation environment
- `prod`: internal production environment

All environments should keep the same core shape:

- one application runtime
- one database
- configuration differences only where necessary

## Container Notes

Suggested image responsibilities:

- host all backend modules
- serve the React SPA
- expose health endpoints
- emit structured logs to stdout

Do not introduce separate per-module containers in V1.

## Environment Configuration

Typical configuration values:

- database connection string
- ASP.NET Core Identity settings
- module feature flags for incomplete areas
- logging level
- local seed-data settings
- reporting refresh settings if background jobs are added later

Secrets should never be committed to source control.

## Observability

Even one application with internal modules should be observable.

Recommended minimum:

- structured application logs
- health checks
- request tracing
- error tracking

Recommended modular telemetry:

- order submission failures
- reservation failures caused by insufficient stock
- invoice issuance counts
- reporting query durations
- per-module endpoint response times

## Security And Access

Minimum operational controls:

- authenticated access only
- role-based authorization by module capability
- audit logging for operationally important actions
- HTTPS in all non-local environments
- database backups enabled for production

V1 security baseline:

- ASP.NET Core Identity for user and role management
- seed initial roles for sales, warehouse, finance, and operations users
- keep module authorization rules explicit in the host and module boundaries

## CI/CD Expectations

A reasonable pipeline for this architecture:

1. run backend tests
2. run frontend tests
3. run end-to-end smoke checks against a seeded environment
4. build frontend assets
5. build the backend host and modules
6. create container image
7. deploy application

Release gates before production:

1. automated tests pass
2. module-boundary rules are validated in review and test coverage
3. smoke tests pass in test environment
4. core cross-module workflows are verified end to end
5. rollback plan is ready
6. no release proceeds with only unit-test coverage; module, API, and UI evidence is required for the MVP path

## Data Evolution Strategy

Tutorial-local baseline:

- clean recreated local database is acceptable
- seed representative module data for predictable learning and testing

Recommended future production strategy:

- introduce version-controlled schema evolution
- keep module-owned tables clearly separated
- avoid ad hoc schema edits across modules

## Backup And Recovery

Minimum recovery plan:

- scheduled PostgreSQL backups
- documented restore procedure
- recovery expectations aligned with operational tolerance for order, inventory, and billing data loss

## Operational Risks

- introducing deployment complexity that the architecture does not require
- weakening module ownership by sharing too much configuration or persistence setup blindly
- letting reporting queries become hidden operational dependencies
- blurring local tutorial convenience with production deployment guidance

## Readiness Checklist

Before implementation starts, confirm:

- environment list is agreed
- application container strategy is agreed
- auth approach is agreed
- module-boundary and data-ownership rules are agreed
- observability expectations are agreed
- backup expectations are agreed
- test stages and release gates are agreed

## Warning Signs The Deployment Model Is Breaking Down

- one or two modules need clearly different scaling characteristics for sustained periods
- deployment coordination becomes dominated by a few unrelated capabilities
- incidents become impossible to isolate even with strong module boundaries
- the organization is ready for service-level operations and has a clear reason to split

If those conditions are not real yet, keep the system as a modular monolith.
