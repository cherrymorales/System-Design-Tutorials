# Implementation

This folder contains the buildable `002-modular-monolith` example implementation.

Implemented phases:

- Phase 1: backend, frontend, Docker, seeded Identity users, and clean local database setup
- Phase 2: `Customers`, `Catalog`, `Orders`, and `Inventory` module baseline
- Phase 3: cross-module workflow from draft order to reservation to invoice-ready state
- Phase 4: cookie authentication, role-based authorization, billing, reporting, and automated test coverage

Current implementation shape:

- one ASP.NET Core host application
- explicit in-process module contracts and module-owned schemas
- PostgreSQL database for the running application
- SQLite-backed backend test host for fast automated tests
- React frontend for internal users
- local Docker workflow with clean seeded data

Implemented modules:

- `Customers`
- `Catalog`
- `Orders`
- `Inventory`
- `Billing`
- `Reporting`
- `Identity`

Seeded users:

- `sales@modularmonolith.local`
- `warehouse@modularmonolith.local`
- `finance@modularmonolith.local`
- `manager@modularmonolith.local`

Password for every seeded account:

- `Password123!`

Local run options:

## Docker API + DB

From [docker](C:/Users/cherr/OneDrive/Documents/GitHub/System-Design-Tutorials/tutorials/002-modular-monolith/implementation/docker):

```powershell
docker compose up -d --build
```

Runtime ports:

- API: `http://localhost:8082`
- PostgreSQL: `localhost:5434`

## Frontend dev server

From [src/frontend](C:/Users/cherr/OneDrive/Documents/GitHub/System-Design-Tutorials/tutorials/002-modular-monolith/implementation/src/frontend):

```powershell
npm install
npm run dev
```

Frontend dev URL:

- `http://localhost:5176`

Verification commands used for this implementation:

```powershell
dotnet build src/backend/sln/SystemDesignTutorials.ModularMonolith.slnx
dotnet test tests/backend/SystemDesignTutorials.ModularMonolith.Tests/SystemDesignTutorials.ModularMonolith.Tests.csproj
npm run build
npm run test
```

Verified runtime flow:

- unauthenticated API requests return `401`
- sales users are blocked from manager-only reports with `403`
- sales can create and reserve orders
- warehouse users can move reserved orders to invoice-ready state
- finance users can draft, issue, and mark invoices paid
- paid invoices allow order completion and commit stock deductions
- manager reports reflect the completed workflow
