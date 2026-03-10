# Frontend

This is the React SPA for `003-client-server-spa-api`.

Implemented pages:

- login
- dashboard
- project portfolio
- project detail
- task list
- task detail

Key files:

- app shell and routing: `src/App.tsx`
- shared layout: `src/layout/AppShell.tsx`
- API client: `src/api.ts`
- view models: `src/models.ts`
- page components: `src/pages/`
- frontend tests: `src/App.test.tsx`

## Commands

Install dependencies:

```powershell
npm install
```

Run the SPA in development:

```powershell
npm run dev
```

Run the frontend tests:

```powershell
npm test
```

Create a production build:

```powershell
npm run build
```

## API Proxy

The Vite dev server proxies `/api` and `/health` requests to the backend.

Default proxy target:

- `http://localhost:8083`

You can override it when needed:

```powershell
$env:VITE_API_PROXY_TARGET='http://127.0.0.1:8084'
npm run dev
```

That override is used by the Playwright smoke test so the smoke browser can target its own isolated backend instance.
