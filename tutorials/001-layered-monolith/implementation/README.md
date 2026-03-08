# 001 Layered Monolith Implementation

This folder contains the working implementation for the `001-layered-monolith` tutorial.

## Current Phase

Phase 2 is in progress.

Included now:

- backend solution scaffold with seeded startup data
- React frontend for product and warehouse management
- PostgreSQL-backed infrastructure baseline
- Docker support for the API and database
- domain tests for inventory, product, and warehouse behavior
- automatic schema creation on application startup for clean local databases
- CRUD endpoints for products and warehouses
- inventory summary endpoint wired to seeded stock positions

## Structure

```text
implementation/
  src/
    backend/
    frontend/
  tests/
    backend/
  infra/
  docker/
```

## Run Locally

### Backend

```powershell
cd src/backend/SystemDesignTutorials.LayeredMonolith.Web
dotnet run
```

Available endpoints:

- `GET /health`
- `GET /api/health`
- `GET /api/products`
- `GET /api/products/{id}`
- `POST /api/products`
- `PUT /api/products/{id}`
- `POST /api/products/{id}/archive`
- `GET /api/warehouses`
- `GET /api/warehouses/{id}`
- `POST /api/warehouses`
- `PUT /api/warehouses/{id}`
- `POST /api/warehouses/{id}/deactivate`
- `GET /api/inventory/summary`

Seeded users:

- `manager@layeredmonolith.local`
- `planner@layeredmonolith.local`
- `purchasing@layeredmonolith.local`
- `operator.brisbane@layeredmonolith.local`
- password for all seeded users: `Password123!`

### Frontend

```powershell
cd src/frontend
npm install
npm run dev
```

The Vite dev server is configured for `http://localhost:5174` and proxies `/api` requests to `http://localhost:8081`.
If `5174` is already in use, Vite automatically starts on the next available port.

### Docker Compose

```powershell
cd docker
docker compose up --build
```

This starts:

- PostgreSQL on `localhost:5433`
- API on `localhost:8081`

The database container is intentionally ephemeral:

- no persistent Docker volume is used
- schema is created automatically with `EnsureCreated`
- each fresh container starts from an empty database state
- this local workflow does not use EF Core migrations

## Next Implementation Targets

- add transfer and adjustment workflows
- add authentication screens and protected routes
- add reporting screens and role-based filtering
- expand API tests beyond current domain coverage
