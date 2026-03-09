# Implementation

This folder is reserved for the buildable `002-modular-monolith` example implementation.

Implementation has not started yet.

Planned baseline for the future build:

- one ASP.NET Core host application loading internal modules
- React frontend aligned to module-oriented features
- PostgreSQL database with explicit module ownership boundaries
- local Docker workflow with clean seeded data
- single-application-first deployment model

Planned MVP test baseline for the future build:

- domain tests per module for business rules and state transitions
- module application tests for use-case flows
- contract tests for `Orders`, `Inventory`, `Billing`, and `Reporting` interactions
- API integration tests for host composition, auth, and key routes
- frontend tests for order, reservation, invoice, and reporting paths
- smoke tests for seeded end-to-end execution of the main wholesale workflow

Implementation should start only after the `docs/` package is reviewed and accepted as the baseline for V1.
