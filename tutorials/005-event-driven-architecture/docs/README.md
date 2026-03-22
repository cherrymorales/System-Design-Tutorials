# Event-Driven Architecture

## Overview

Event-driven architecture coordinates system behavior through events instead of tight request chaining.

One component publishes that something happened, and one or more other components react asynchronously. This makes it a strong fit for workflows where many downstream actions must happen independently, at different speeds, and with different scaling characteristics.

For this tutorial, the architecture is applied to a concrete system: a digital asset ingestion and processing platform for a media operations team.

## Why This Tutorial Matters

Event-driven design is widely used in industry, but it is often discussed only at a slogan level.

This tutorial matters because it shows:

- what an event-driven workflow actually looks like
- when asynchronous fan-out is a better fit than request chaining
- how retries, idempotency, dead-lettering, and eventual consistency affect design
- how to test a system whose behavior unfolds across multiple handlers

## Best Used When

- one business action must trigger multiple downstream reactions
- work is asynchronous by nature
- independent consumers need to scale separately
- the system must stay responsive while background processing continues
- eventual consistency is acceptable and visible to operators

## Not Ideal When

- the workflow is mostly synchronous and simple
- one database transaction already solves the business problem
- the team is not ready to operate messaging, retries, and observability
- the UI cannot tolerate delayed downstream completion

## Recommended Technology

Recommended tutorial baseline:

- Frontend: React operations console
- Backend API: ASP.NET Core
- Background processing: ASP.NET Core worker services
- Messaging: RabbitMQ for tutorial-scale messaging
- Transactional storage: PostgreSQL
- Blob or object storage: local object store emulator or cloud object storage in production
- Reliability patterns: outbox, idempotent consumers, dead-letter queues

## Example Project

**Project idea:** Digital asset ingestion and processing platform

Concrete scenario:

- media operators upload raw video and image assets
- a submit action triggers multiple background reactions
- the system scans files, extracts metadata, generates derivatives, updates processing status, and notifies operators
- the React console is used to monitor processing state and intervene when failures occur

Core workflow in scope:

- asset registered
- upload completed
- scan completed
- metadata extracted
- thumbnails generated
- transcode completed
- asset marked ready
- notification requested

## Scope

### In Scope

- authenticated operations console
- asset registration and upload initiation
- asynchronous processing pipeline
- fan-out event handling
- status projection for operations users
- retry and failure visibility
- automated testing across API, handlers, contracts, frontend, and smoke flow

### Out Of Scope For The First Implementation

- public content delivery
- external CDN integration
- human moderation workflows
- AI enrichment and tagging
- multi-region disaster recovery
- advanced workflow designer tooling

## Implementation Status

This tutorial is now implemented through Phases 1 to 4.

Implemented now:

- API, worker, shared contracts/core, and React operations console
- RabbitMQ-backed local runtime with PostgreSQL persistence
- asset registration, upload-complete initiation, asynchronous fan-out processing, readiness, and failure projection handling
- backend tests, frontend tests, contract tests, and smoke validation of the main ready-state path

Remaining future evolution:

- richer retry/dead-letter operator tooling
- more advanced observability and replay workflows
- packaging changes if the tutorial later needs hosted SPA assets rather than Vite-based local development

## MVP Testing Position

For this tutorial, testing is part of the architecture baseline.

The MVP is only considered complete when it includes:

- handler and domain tests for core processing rules
- API integration tests for asset submission and status APIs
- contract tests for published and consumed events
- frontend workflow tests for the operations console
- smoke validation of the end-to-end processing pipeline

## Tutorial Contents

- [Learning Guide](./learning-guide.md)
- [Project Plan](./project-plan.md)
- [Architecture Guide](./architecture.md)
- [Implementation Blueprint](./implementation-blueprint.md)
- [Deployment Guide](./deployment.md)
- [Testing Strategy](./testing-strategy.md)

## Intended Audience

- developers learning when asynchronous event handling is justified
- architects comparing request-driven and event-driven designs
- contributors who need a concrete baseline before extending the implementation

## What You Should Learn From This Tutorial

By the end of this tutorial, a developer should understand:

- when an event is a better integration boundary than a synchronous call
- how fan-out changes system behavior and testing needs
- why idempotency and retries are not optional in event-driven systems
- how status projections support operational visibility
- when event-driven architecture is a good fit and when it adds unnecessary complexity

## Definition Of Documentation Accuracy

This tutorial documentation is accurate when a reader can answer all of the following without guessing:

- what business problem the system solves
- which events matter and why
- which components publish and consume those events
- how failures and retries are handled
- how operators observe pipeline progress
- how the MVP should be tested
- how the system is intended to run locally and beyond local development
