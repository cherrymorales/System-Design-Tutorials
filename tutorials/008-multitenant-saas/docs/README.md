# Multitenant SaaS

## What It Is

A multitenant SaaS architecture serves multiple customers from one product while keeping tenant identity, configuration, access, and data isolation explicit.

Tenants may share infrastructure, application instances, or databases, but the architecture must always enforce clear tenant boundaries.

## Best Used When

- the product is sold to many organizations on one platform
- tenant onboarding and lifecycle management matter
- per-tenant configuration is required
- the business needs efficient shared infrastructure without losing isolation

## Not Ideal When

- the system is built for one customer only
- every customer requires fully dedicated infrastructure from day one
- tenant isolation requirements are not yet understood

## Why It Is Common

Most B2B SaaS products eventually become multitenant.

It is one of the most practical architectures to study because tenant isolation, billing, configuration, data partitioning, and operational boundaries affect nearly every part of the design.

## Recommended Technology

- Frontend: React
- Backend: ASP.NET Core
- Database: PostgreSQL with tenant-aware schema strategy, row-level partitioning, or separate databases depending on isolation needs
- Auth: tenant-aware identity provider integration
- Caching and storage: tenant-keyed patterns throughout

## Single-Container Guidance

A tutorial MVP can stay in a single container if tenant scale is small and one app instance handles all tenants.

The main concern is not container count but correct tenant isolation across auth, queries, storage, and operations.

## Example Project

**Project idea:** Project management SaaS for agencies and consulting teams

Example tenant-aware capabilities:

- tenant onboarding
- branded workspace settings
- tenant members and roles
- project and task data isolation
- tenant usage and plan reporting

## Suggested Solution Shape

- React frontend with tenant-scoped workspace navigation
- ASP.NET Core API with tenant resolution middleware
- PostgreSQL with explicit tenant partitioning strategy
- background jobs for tenant provisioning and usage reporting

## Tradeoffs

- efficient shared platform for many customers
- central product evolution across tenants
- lower infrastructure cost than fully dedicated deployments
- more complexity in isolation, noisy-neighbor control, onboarding, and operational governance
