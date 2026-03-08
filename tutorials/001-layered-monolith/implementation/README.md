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

The Vite dev server proxies `/api` requests to `http://localhost:8080`.

### Docker Compose

```powershell
cd docker
docker compose up --build
```

This starts:

- PostgreSQL on `localhost:5432`
- API on `localhost:8080`

## Next Implementation Targets

- add EF Core migrations
- seed roles and baseline users
- implement products and warehouses CRUD
- add transfer and adjustment workflows
- add authentication screens and protected routes
