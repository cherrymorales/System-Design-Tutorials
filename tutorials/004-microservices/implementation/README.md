# 004 Microservices Implementation

This folder contains the runnable `004-microservices` tutorial implementation.

The delivered system includes:

- a React operations console
- an ASP.NET Core gateway/BFF
- service-owned ASP.NET Core services
- RabbitMQ-driven distributed workflow orchestration
- PostgreSQL with separate databases per service
- automated backend, frontend, contract, gateway, and smoke-test coverage

## Implemented Scope

Phase 1 to Phase 4 are implemented for the tutorial MVP.

Implemented services:

- `GatewayBff`
- `Identity`
- `Catalog`
- `Orders`
- `Inventory`
- `Payments`
- `Fulfillment`
- `Notifications`
- `OperationsQuery`

Implemented workflow:

- sign in through the gateway
- create a draft order
- submit the order
- reserve stock asynchronously
- authorize payment asynchronously
- project order state into the operations dashboard
- create a fulfillment shipment when the order is ready
- progress shipment through pick, pack, ship, and deliver

## Folder Layout

```text
implementation/
  docker/
  infra/
    postgres-init/
  src/
    frontend/
    gateway/
    services/
    shared/
    sln/
  tests/
    contracts/
    gateway/
    services/
    smoke/
```

## Local Runtime

Docker runtime:

- gateway: `http://localhost:8084`
- gateway health: `http://localhost:8084/api/health`
- postgres: `localhost:5436`
- rabbitmq AMQP: `localhost:5673`
- rabbitmq management: `http://localhost:15673`

Frontend local dev server:

- `http://127.0.0.1:5178`

The frontend proxies `/api` requests to the gateway.

## Seeded Users

All seeded users use:

- password: `Password123!`

Accounts:

- `catalog@microservices.local`
- `orders@microservices.local`
- `inventory@microservices.local`
- `finance@microservices.local`
- `fulfillment@microservices.local`
- `manager@microservices.local`

## Run Instructions

### 1. Start the distributed runtime

From `implementation/`:

```powershell
docker compose -f docker/docker-compose.yml up -d --build
```

### 2. Run the frontend locally

From `implementation/src/frontend`:

```powershell
npm install
npm run dev
```

### 3. Stop the distributed runtime

From `implementation/`:

```powershell
docker compose -f docker/docker-compose.yml down
```

## Test Commands

Backend solution tests:

```powershell
dotnet test src/sln/SystemDesignTutorials.Microservices.slnx
```

Frontend tests:

```powershell
cd src/frontend
npm test -- --run
```

Frontend production build:

```powershell
cd src/frontend
npm run build
```

Smoke tests:

```powershell
cd tests/smoke
npm test
```

The smoke suite starts a clean composed stack, waits for the gateway and service buses to become ready, runs the distributed order flow, and tears the stack down.

## Current Test Surface

Implemented now:

- service workflow and business-rule tests
- gateway API tests
- contract and integration-event tests
- frontend workflow tests
- end-to-end smoke coverage for the main distributed success path

Verified in the current branch:

- `dotnet test` passes
- `npm test -- --run` passes
- `npm run build` passes
- `tests/smoke` Playwright smoke passes from a clean stack

## Notes On Architecture

- Browser traffic goes only through the gateway.
- Service-owned data is isolated by database.
- Cross-service workflow progression happens through RabbitMQ events.
- `OperationsQuery` provides the read model used by the dashboard and order detail views.
- Eventual consistency is visible in the UI through projected order and shipment states.

## Reference Documents

Implementation was built from:

- `../docs/README.md`
- `../docs/project-plan.md`
- `../docs/architecture.md`
- `../docs/implementation-blueprint.md`
- `../docs/deployment.md`
- `../docs/learning-guide.md`
- `../docs/testing-strategy.md`
