# Layered Monolith Deployment Guide

## Deployment Philosophy

The layered monolith should stay operationally simple.

For this tutorial, the preferred deployment model is:

- one container for the ASP.NET Core application
- React built into static assets and served by the same application
- database hosted separately in production

Implementation should not begin until the deployment model is accepted, because deployment assumptions influence project structure, configuration, and operational risk.

## Why Single Container Works Well Here

- there is only one deployable application
- request handling is synchronous and local to the process
- operational overhead stays low
- debugging and rollback are simpler than in a multi-service setup

## Learning Focus

When reading this document, focus on:

- why deployment simplicity is a valid design goal
- how environment strategy affects project structure
- why a monolith can still need serious operational planning

## Recommended Production Shape

```text
[ React static files + ASP.NET Core app ] -> single container
[ PostgreSQL ] -> managed database service
```

This keeps the application deployment simple while avoiding data loss risks that come from bundling the database into the app container.

## Environment Strategy

Recommended environments:

- `local`: developer machine with app container and PostgreSQL container
- `dev`: shared integration environment for ongoing feature work
- `test`: pre-release validation environment
- `prod`: internal production environment for business users

Each environment should use the same deployment shape, with differences limited to configuration, scale, and managed services.

## Local Development Shape

For local development, two containers are perfectly reasonable:

- app container
- PostgreSQL container

This still preserves the layered monolith design because the application remains a single deployable unit.

Suggested local setup:

- one Dockerfile for the ASP.NET Core host plus built React assets
- one PostgreSQL container
- seed data for products, warehouses, and example inventory

## Container Notes

Recommended approach:

- build React during the application build pipeline
- copy the built frontend assets into the ASP.NET Core host
- expose one HTTP port
- keep environment-specific settings outside the image

Suggested image responsibilities:

- host the API
- serve the React SPA
- expose health endpoints
- emit structured logs to stdout

## Environment Configuration

Typical configuration values:

- database connection string
- ASP.NET Core Identity settings
- logging level
- allowed origins if frontend and backend are separated in development
- approval threshold configuration for inventory adjustments
- warehouse assignment seeding for local and test environments
- feature flags for incomplete modules

Secrets should never be stored in source control.

## Observability

Even a monolith should be observable.

Recommended minimum:

- structured application logs
- request tracing
- error tracking
- health check endpoint

Recommended domain-focused telemetry:

- count of stock adjustments created
- count of pending approvals
- transfer failures caused by insufficient stock
- low-stock report generation duration
- per-endpoint response times

## Security And Access

Minimum operational controls:

- authenticated access only
- role-based authorization on write operations
- audit logging for inventory-changing actions
- HTTPS in all non-local environments
- database backups enabled for production

V1 security baseline:

- use ASP.NET Core Identity for local user and role management
- seed initial roles for `WarehouseOperator`, `InventoryPlanner`, `PurchasingOfficer`, and `OperationsManager`
- seed at least one assigned warehouse user for local testing

## CI/CD Expectations

A reasonable pipeline for this architecture:

1. run backend tests
2. run frontend tests
3. build React assets
4. build the ASP.NET Core application
5. create container image
6. deploy application

Release gates before production:

1. automated tests pass
2. database migrations are validated
3. smoke tests pass in test environment
4. low-stock reporting and adjustment approval flows are verified
5. rollback plan is ready
6. transfer reservation, dispatch, and receipt flows are verified end-to-end

## Database Migration Strategy

- keep schema changes in version-controlled migrations
- apply migrations in a controlled deployment step
- avoid manual production schema edits
- ensure new migrations are backward-compatible when possible

## Backup And Recovery

Minimum recovery plan:

- scheduled PostgreSQL backups
- restore procedure documented and tested
- recovery point objective aligned with business tolerance for inventory data loss
- recovery steps for failed application deployment

## Operational Risks

- schema changes that break inventory balances
- missing audit records for stock-changing operations
- accidental over-coupling between catalog, inventory, and reporting code
- configuration drift across environments

## Readiness Checklist

Before implementation starts, confirm:

- environment list is agreed
- container strategy is agreed
- auth approach is agreed
- logging and monitoring requirements are agreed
- backup expectations are agreed
- release gates are agreed

## Warning Signs The Deployment Model Is Breaking Down

- deployments become slow because unrelated changes always ship together
- startup time grows significantly
- one feature area needs very different scaling
- incidents are hard to isolate because the application has become too entangled

When that happens, the next step is usually better internal modularity before service decomposition.
