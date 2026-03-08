# 001 Layered Monolith Implementation

This folder contains the working implementation for the `001-layered-monolith` tutorial.

## Current Phase

Phase 1 is in progress.

Included now:

- backend solution scaffold
- React frontend scaffold
- PostgreSQL-backed infrastructure baseline
- Docker support for the API and database
- first domain test for inventory reservation behavior
- automatic schema creation on application startup for clean local databases

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

API endpoints available initially:

- `GET /health`
- `GET /api/health`
- `GET /api/products`
- `GET /api/warehouses`
- `GET /api/inventory/summary`

### Frontend

```powershell
cd src/frontend
npm install
npm run dev
```

The Vite dev server runs on `http://localhost:5174` and proxies `/api` requests to `http://localhost:8081`.

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

- seed roles and baseline users
- implement products and warehouses CRUD
- add transfer and adjustment workflows
- add authentication screens and protected routes
