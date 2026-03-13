# Implementation

This folder is reserved for the buildable `005-event-driven-architecture` example implementation.

The implementation has not started yet.

Planned implementation baseline:

- React operations console
- ASP.NET Core API and worker services
- RabbitMQ for asset processing events
- PostgreSQL for asset state and projections
- multiple containers for local runtime because the broker is part of the architecture

Suggested future structure:

```text
implementation/
  docker/
  infra/
  src/
    api/
    workers/
    frontend/
  tests/
    backend/
    frontend/
    smoke/
```

Before implementation starts, the following documents should be treated as the locked baseline:

- `../docs/README.md`
- `../docs/project-plan.md`
- `../docs/architecture.md`
- `../docs/implementation-blueprint.md`
- `../docs/deployment.md`
- `../docs/learning-guide.md`
- `../docs/testing-strategy.md`
