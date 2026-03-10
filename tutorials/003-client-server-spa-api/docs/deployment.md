# Client-Server SPA + API Deployment Guide

## Deployment Philosophy

This tutorial should stay simple to run and simple to explain.

The preferred deployment model is:

- one React SPA
- one ASP.NET Core backend
- one PostgreSQL database
- same-origin hosting in production

The browser and API are separate runtime concerns, but they do not need separate production hosts in the MVP.

## Why Single-Application-First Fits Here

This architecture is about client-server separation, not operational sprawl.

A SPA + API system can still be deployed simply when:

- one product team owns both client and server
- there is one primary client
- auth is web-first
- the scale and team size do not justify extra hosting layers yet

## Local Development Shape

Local development should use:

- Vite dev server for the SPA
- ASP.NET Core API host
- PostgreSQL container or local instance

Recommended local run shape:

```text
React SPA dev server     -> http://localhost:517x
ASP.NET Core API         -> http://localhost:808x
PostgreSQL               -> localhost:543x
```

Development notes:

- local SPA and API may run separately
- local API should allow the SPA origin used during development
- local data can be recreated from a clean seed for tutorial workflows

## Production Direction

Recommended production direction for the tutorial:

- build the SPA assets during CI
- copy the built assets into the backend host image
- serve the SPA and API from the same application host
- run PostgreSQL separately

Production shape:

```text
Browser
  -> ASP.NET Core host
      -> serves SPA assets
      -> exposes /api/*
      -> connects to PostgreSQL
```

This keeps cookies, routing, and deployment easier than introducing separate frontend hosting on day one.

## Container Strategy

### Development

- API container optional during inner-loop development
- PostgreSQL container recommended
- SPA usually runs with Vite outside Docker for faster feedback

### Tutorial Production Or Demo Deployment

- one container for the ASP.NET Core host with built SPA assets
- one PostgreSQL container or managed database

This still respects the repo principle of minimizing container count while keeping the database isolated.

## Environment Strategy

Suggested environments:

- local
- dev or demo
- production-like tutorial environment

Environment variables should cover:

- database connection string
- auth and cookie settings
- seed-data toggle
- logging level
- base URL settings if needed for absolute links

## Authentication And Security

V1 auth model:

- same-origin cookie auth
- secure cookies outside local development
- server-side authorization on all protected endpoints

Security expectations:

- do not trust client-side hidden controls as authorization
- use CSRF protection appropriate to the cookie strategy
- protect admin and membership management routes explicitly
- avoid exposing internal-only diagnostic data to the SPA

## Data Strategy

For tutorial development and demo environments, a clean recreated database is acceptable.

Recommended approach:

- startup database creation for clean tutorial runs
- deterministic seed data for roles, users, projects, and tasks
- repeatable demo dataset for smoke testing

If this tutorial is ever extended into a persistent production system, add a proper schema migration pipeline before retaining real user data.

## CI/CD Expectations

CI should:

- build the backend
- run backend tests
- build the frontend
- run frontend tests
- run smoke-test validation against a built app
- build the deployable application image

CD should:

- deploy the application host
- provision or connect the database
- apply environment-specific configuration
- run post-deploy smoke checks

## Observability Baseline

The MVP should include at least:

- structured backend logs
- request logging for API failures
- client-visible error states with correlation-friendly messages
- health endpoint for application readiness

Useful future additions:

- request tracing
- browser error capture
- dashboard metrics for API latency and failed workflow actions

## Operational Risks

- serving the SPA and API from different origins too early adds avoidable auth complexity
- large dashboard payloads can create slow initial loads if projections are poorly shaped
- missing post-deploy smoke tests can hide broken auth or broken route fallbacks

## Readiness Checklist

Before deployment is considered ready:

- SPA builds successfully
- backend builds successfully
- automated tests pass
- smoke path covers sign-in, dashboard load, project open, task update, and comment creation
- production host serves SPA fallback routes correctly
- cookies behave correctly in the target environment
- database configuration is stable and seeded as expected

## Warning Signs This Model Needs To Evolve

Revisit the deployment model if:

- a second independent client becomes primary
- the frontend and backend need independent release cadences that materially conflict
- auth requirements force a different token and hosting model
- dashboard query load becomes heavy enough to justify specialized read infrastructure
