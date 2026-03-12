# 004 Microservices Implementation

This folder is reserved for the future buildable `004-microservices` example implementation.

The implementation has not started yet.

Planned implementation baseline:

- React SPA operations console
- ASP.NET Core gateway and service set
- RabbitMQ for asynchronous events
- PostgreSQL with database-per-service ownership
- multiple containers for local and non-local runtime
- service, contract, frontend, and smoke-test coverage as part of the MVP

Suggested future structure:

```text
implementation/
  docker/
  infra/
  src/
    gateway/
    services/
      identity/
      catalog/
      orders/
      inventory/
      payments/
      fulfillment/
      notifications/
      operations-query/
    frontend/
  tests/
    services/
    contracts/
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

Implementation should not start until those documents are accepted as the baseline.
