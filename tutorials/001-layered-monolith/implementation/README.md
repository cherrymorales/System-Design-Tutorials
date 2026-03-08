# 001 Layered Monolith Implementation

This folder contains the working implementation for the `001-layered-monolith` tutorial.

## Current Phase

Phase 4 is in progress.

Included now:

- backend solution scaffold with seeded startup data
- React frontend with login, protected routes, and a role-aware inventory console
- PostgreSQL-backed infrastructure baseline
- Docker support for the API and database
- domain tests for inventory, product, warehouse, transfer, and adjustment behavior
- automatic schema creation on application startup for clean local databases
- ASP.NET Core Identity with seeded users and cookie-based authentication
- role-based authorization for catalog, warehouse, inventory, transfer, and adjustment workflows
- warehouse assignment filtering for operator accounts
- CRUD endpoints for products and warehouses
- inventory receipt workflow
- stock transfer workflow with request, approve, dispatch, receive, and cancel transitions
- inventory adjustment workflow with threshold-based approval
- low-stock reporting endpoint and dashboard view

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
- `POST /api/auth/login`
- `POST /api/auth/logout`
- `GET /api/auth/me`
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
- `GET /api/inventory/receipts`
- `POST /api/inventory/receipts`
- `GET /api/transfers`
- `POST /api/transfers`
- `POST /api/transfers/{id}/approve`
- `POST /api/transfers/{id}/dispatch`
- `POST /api/transfers/{id}/receive`
- `POST /api/transfers/{id}/cancel`
- `GET /api/adjustments`
- `GET /api/adjustments/pending`
- `POST /api/adjustments`
- `POST /api/adjustments/{id}/approve`
- `POST /api/adjustments/{id}/reject`
- `GET /api/reports/low-stock`

Seeded users:

- `manager@layeredmonolith.local` (`OperationsManager`)
- `planner@layeredmonolith.local` (`InventoryPlanner`)
- `purchasing@layeredmonolith.local` (`PurchasingOfficer`)
- `operator.brisbane@layeredmonolith.local` (`WarehouseOperator`)
- password for all seeded users: `Password123!`

Authorization highlights:

- all `/api/*` endpoints except `/api/health` and `/api/auth/login` require authentication
- operators are limited to their assigned warehouses
- product writes are restricted to purchasing officers and managers
- warehouse writes are restricted to managers
- transfer creation and approval are restricted to planners and managers
- transfer dispatch and receive are restricted to operators for their assigned warehouses or managers
- adjustment approval and rejection are restricted to managers

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
- each fresh container starts from a clean seeded database state
- this local workflow does not use EF Core migrations

## Verification

The current Phase 4 implementation has been verified with:

- `dotnet test` passing with 10 tests
- `npm run build` passing for the React frontend
- `docker compose up -d --build` passing
- runtime checks confirming:
  - unauthenticated API access returns `401`
  - seeded login works through `/api/auth/login`
  - `/api/auth/me` returns the authenticated session
  - manager accounts can view all three warehouses
  - the Brisbane operator only sees the assigned warehouse
  - the Brisbane operator is blocked from creating transfers
  - receipt creation records the authenticated operator identity on the server

## Next Implementation Targets

- add backend integration tests for authorization and endpoint behavior
- add frontend test coverage
- expand reporting screens beyond the low-stock dashboard
- refine operator-specific UX based on assigned warehouse defaults
