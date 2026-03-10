# Infrastructure Notes

This tutorial keeps infrastructure intentionally light.

Current runtime model:

- PostgreSQL runs through `docker/docker-compose.yml`
- the API container connects to PostgreSQL inside the compose network
- the frontend runs as a separate Vite dev server during local development

Current tutorial constraints:

- no EF migration pipeline is used for the local tutorial flow
- database schema is created and seeded on startup
- the Docker PostgreSQL container uses ephemeral storage for clean local resets
- Playwright smoke tests use a temporary SQLite database instead of PostgreSQL

If this tutorial is extended later, this folder is the place for:

- SQL helper scripts
- seed data notes
- environment overrides
- deployment-specific infrastructure assets
