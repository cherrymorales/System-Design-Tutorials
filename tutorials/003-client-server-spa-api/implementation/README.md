# 003 Client-Server SPA + API Implementation

This folder contains the completed Phase 1 to Phase 4 implementation for the `003-client-server-spa-api` tutorial.

Implemented scope:

- React SPA with route-based navigation
- ASP.NET Core API with cookie authentication
- PostgreSQL runtime for the main local stack
- SQLite-backed backend tests and smoke tests
- dashboard, projects, tasks, comments, and membership workflows
- backend, frontend, and browser smoke coverage

## Structure

```text
implementation/
  docker/
  infra/
  src/
    backend/
      sln/
    frontend/
  tests/
    backend/
    smoke/
```

Key locations:

- backend solution: `src/backend/sln/SystemDesignTutorials.ClientServerSpaApi.slnx`
- API entry point: `src/backend/SystemDesignTutorials.ClientServerSpaApi.Web/Program.cs`
- frontend entry point: `src/frontend/src/App.tsx`
- backend integration tests: `tests/backend/SystemDesignTutorials.ClientServerSpaApi.Tests/ApiIntegrationTests.cs`
- smoke test: `tests/smoke/playwright/smoke.spec.ts`

## Local Runtime

Recommended local runtime:

1. Start PostgreSQL and the API with Docker Compose.
2. Run the frontend SPA with Vite.

API and database:

```powershell
cd tutorials/003-client-server-spa-api/implementation/docker
docker compose up -d --build
```

Frontend:

```powershell
cd tutorials/003-client-server-spa-api/implementation/src/frontend
npm install
npm run dev
```

Default local URLs:

- frontend: `http://127.0.0.1:5177`
- API health: `http://localhost:8083/api/health`
- PostgreSQL host port: `5435`

The Vite dev server proxies `/api` and `/health` to `http://localhost:8083` by default.

If you want to run the API locally instead of through Docker, use the Docker PostgreSQL instance on `5435`:

```powershell
cd tutorials/003-client-server-spa-api/implementation
$env:ConnectionStrings__DefaultConnection='Host=localhost;Port=5435;Database=client_server_spa_api;Username=postgres;Password=postgres'
dotnet run --project src/backend/SystemDesignTutorials.ClientServerSpaApi.Web/SystemDesignTutorials.ClientServerSpaApi.Web.csproj --no-launch-profile
```

## Seeded Access

All seeded accounts use:

- password: `Password123!`

Seeded users:

- `admin@clientserverspa.local` - `WorkspaceAdmin`
- `manager@clientserverspa.local` - `ProjectManager`
- `alex@clientserverspa.local` - `Contributor`
- `sam@clientserverspa.local` - `Contributor`
- `viewer@clientserverspa.local` - `Viewer`

## Test Commands

Backend tests:

```powershell
cd tutorials/003-client-server-spa-api/implementation
dotnet test tests/backend/SystemDesignTutorials.ClientServerSpaApi.Tests/SystemDesignTutorials.ClientServerSpaApi.Tests.csproj
```

Frontend tests:

```powershell
cd tutorials/003-client-server-spa-api/implementation/src/frontend
npm test
```

Frontend production build:

```powershell
cd tutorials/003-client-server-spa-api/implementation/src/frontend
npm run build
```

Browser smoke test:

```powershell
cd tutorials/003-client-server-spa-api/implementation/tests/smoke
npm install
npx playwright install chromium
npm test
```

Smoke test notes:

- the smoke run starts its own backend and frontend servers
- it uses a temporary SQLite database file instead of PostgreSQL
- it uses isolated ports `8084` and `5178`
- it does not depend on the Docker stack

## Current Coverage

Implemented coverage:

- domain workflow tests for projects, tasks, and comments
- backend API integration tests for auth, filtering, authorization, and workflow behavior
- frontend route and page rendering tests
- Playwright smoke coverage for the primary manager workflow

## Implementation Notes

- The backend creates and seeds the database on startup through `ApplicationDataSeeder`.
- The tutorial does not use EF migrations for the local tutorial flow.
- Cookie auth is same-origin from the SPA through the Vite proxy in development.
- Project and task workflow rules are enforced on the API, not in the SPA.
