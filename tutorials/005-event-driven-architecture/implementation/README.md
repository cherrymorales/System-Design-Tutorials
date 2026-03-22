# Implementation

This folder contains the buildable `005-event-driven-architecture` example implementation.

Current implementation:

- React operations console in `src/frontend`
- ASP.NET Core API in `src/api`
- ASP.NET Core worker consumers in `src/workers`
- RabbitMQ-backed asynchronous processing
- PostgreSQL persistence for asset state, projections, notifications, and outbox records
- automated backend, frontend, contract, and smoke tests

## Local Runtime

From the repository root:

```powershell
docker compose -f tutorials/005-event-driven-architecture/implementation/docker/docker-compose.yml up -d --build
```

Then start the frontend:

```powershell
cd tutorials/005-event-driven-architecture/implementation/src/frontend
npm install
npm run dev
```

URLs:

- frontend: `http://127.0.0.1:5179`
- API health: `http://127.0.0.1:8085/api/health`
- PostgreSQL: `localhost:5437`
- RabbitMQ AMQP: `localhost:5674`
- RabbitMQ management UI: `http://localhost:15674`

Seeded users:

- `coordinator@eventdriven.local`
- `manager@eventdriven.local`

Password for all seeded accounts:

- `Password123!`

## Test Commands

Backend and contract tests:

```powershell
dotnet test tutorials/005-event-driven-architecture/implementation/src/sln/SystemDesignTutorials.EventDriven.slnx
```

Frontend build and tests:

```powershell
cd tutorials/005-event-driven-architecture/implementation/src/frontend
npm run build
npm test
```

Smoke test:

```powershell
cd tutorials/005-event-driven-architecture/implementation/tests/smoke
npm install
npm test
```

## Notes

- local development uses Vite for the frontend and Docker for the API, worker, PostgreSQL, and RabbitMQ
- the main documentation baseline remains in:
  - `../docs/README.md`
  - `../docs/project-plan.md`
  - `../docs/architecture.md`
  - `../docs/implementation-blueprint.md`
  - `../docs/deployment.md`
  - `../docs/learning-guide.md`
  - `../docs/testing-strategy.md`
