# Microservices Deployment Guide

## Deployment Philosophy

This tutorial should not pretend microservices can be deployed like a single simple application.

The preferred deployment model is:

- one React SPA
- one public gateway or BFF
- multiple backend services
- one message broker
- multiple service-owned databases or schemas
- centralized observability

This is explicitly a multi-container architecture.

## Why Multiple Containers Are Correct Here

Microservices are about independent ownership and deployment.

That means the tutorial should preserve:

- independent service runtime boundaries
- service-specific health and logs
- broker-backed asynchronous integration
- failure isolation between gateway, services, and data stores

Trying to force this into a single container would teach the wrong lesson.

## Local Development Shape

Local development should use:

- Vite dev server for the React SPA
- one container for the gateway
- one container per service
- one RabbitMQ container
- one PostgreSQL container hosting multiple service-owned databases, or separate PostgreSQL containers if preferred later

Recommended local run shape:

```text
React SPA dev server      -> http://localhost:517x
Gateway/BFF               -> http://localhost:808x
Service containers        -> internal compose network
RabbitMQ                  -> internal compose network
PostgreSQL                -> localhost:543x
```

Development notes:

- the SPA may run outside Docker for faster iteration
- browser traffic should go only to the gateway
- services should communicate through internal service names and broker routing
- local data may be recreated from seeded startup data for tutorial workflows

## Non-Local Environment Direction

Recommended non-local direction for the tutorial:

- package the SPA with its own lightweight host or keep it behind the gateway-facing web tier
- deploy the gateway separately from backend services
- keep broker and database infrastructure external to application containers
- use environment-specific configuration for service endpoints, connection strings, and secrets

This architecture can still stay tutorial-friendly without pretending the deployment is as simple as a monolith.

## Container Strategy

### Development

- React SPA usually runs through Vite outside Docker
- gateway and services run in Docker
- RabbitMQ and PostgreSQL run in Docker

### Tutorial Demo Or Staging

- SPA hosted in a simple web container or behind the gateway-facing web host
- gateway container
- service containers
- broker container
- PostgreSQL container or managed PostgreSQL instance

## Environment Strategy

Suggested environments:

- local
- dev or demo
- staging
- production-like tutorial environment

Environment variables should cover:

- database connection string per service
- broker connection settings
- gateway upstream service URLs
- auth signing or validation settings
- tracing exporter configuration
- feature flags for seeded data

## Authentication And Security

V1 auth model:

- browser sign-in through the gateway
- secure session handling at the gateway edge
- downstream authorization enforced inside services

Security expectations:

- do not expose every service directly to the browser
- do not trust gateway-hidden UI states as authorization
- secure service-to-service and gateway-to-service identity propagation
- protect broker and database access from direct public exposure
- keep secret material out of repository configuration

## Data Strategy

For tutorial development and demo environments:

- a clean recreated local environment is acceptable
- seeded reference users, products, orders, and workflow events are recommended
- one PostgreSQL server with separate service-owned databases is acceptable locally

For a stronger non-local environment:

- keep separate database ownership even if infrastructure is shared
- avoid cross-service schema coupling
- add migration or schema deployment discipline before retaining real user data

## Messaging Strategy

Required messaging baseline:

- one RabbitMQ broker for the tutorial
- explicit exchanges and routing keys by event type
- dead-letter handling for failed messages
- outbox publishing from services that emit events
- idempotent consumers for retried message delivery

## CI/CD Expectations

CI should:

- build the gateway and each service
- run service tests
- run contract and event-flow tests
- build the frontend
- run frontend tests
- run smoke-test validation against an isolated seeded environment
- build deployable images

CD should:

- deploy the gateway
- deploy services
- provision or connect broker and database infrastructure
- apply environment-specific configuration
- run post-deploy smoke checks

## Observability Baseline

The MVP should include at least:

- structured logs for gateway and services
- correlation IDs across HTTP and broker messages
- distributed tracing through the main workflow
- health endpoints for gateway and services
- broker visibility
- read-model or projection health visibility

Useful future additions:

- per-service SLO dashboards
- alerting on dead-letter accumulation
- replay or reprocessing tooling for projection rebuilds

## Operational Risks

- exposing services directly to the browser too early creates unnecessary client coupling
- missing correlation IDs makes distributed incidents expensive to debug
- eventual consistency can confuse operators if the UI does not show pending states clearly
- weak broker handling can hide failed workflow steps
- incomplete smoke coverage can miss integration failures that unit tests cannot see

## Readiness Checklist

Before deployment is considered ready:

- the SPA builds successfully
- gateway and services build successfully
- automated tests pass
- smoke path covers sign-in, order submission, payment/reservation coordination, and shipment progression
- correlation and tracing are visible across the main workflow
- gateway routing is stable
- service configuration is environment-safe
- seeded demo data behaves as documented

## Warning Signs This Model Needs To Evolve

Revisit the deployment model if:

- the gateway becomes too heavy and starts owning domain logic
- one broker becomes a bottleneck
- read-model rebuilds or event replay become operationally necessary
- certain services need materially different runtime platforms
- the team count or regional footprint demands stronger platform automation
