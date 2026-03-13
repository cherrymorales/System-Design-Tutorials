# CQRS

## What It Is

Command Query Responsibility Segregation separates write operations from read operations.

Instead of forcing one model to handle both transactional updates and rich queries, CQRS allows the system to use different models, APIs, and storage strategies for commands and queries.

## Best Used When

- write behavior is complex and governed by business rules
- read behavior needs different shaping, projection, or performance characteristics
- the system has heavy dashboards, reporting, or search views
- the team needs clearer separation between operational workflows and read models

## Not Ideal When

- the system is small and CRUD-heavy
- one transactional model is already simple and sufficient
- the team would add CQRS without any real read or write complexity

## Why It Is Common

CQRS is common in systems where transactional writes and operational reporting pull the data model in different directions.

It is especially useful in platforms with approval flows, dashboards, audit needs, and specialized query views.

## Recommended Technology

- Frontend: React
- Backend: ASP.NET Core Web API
- Command side: ASP.NET Core with PostgreSQL
- Query side: PostgreSQL read models, Elasticsearch, or a projection store depending on query needs
- Messaging: optional but often useful when projections are updated asynchronously

## Single-Container Guidance

CQRS can stay in a single container for a learning project if command and query concerns are still part of one deployable application.

Once separate projection workers or read stores are introduced, multiple containers become more realistic.

## Example Project

**Project idea:** Procurement and spend approvals platform

Example split:

- command side handles purchase requests, approval rules, and supplier commitments
- query side serves approval queues, spend dashboards, buyer reports, and department summaries

## Suggested Solution Shape

- React frontend for operational forms and reporting views
- ASP.NET Core command API for transactional workflows
- ASP.NET Core query API or read endpoints for dashboards and filtered views
- PostgreSQL for transactional writes plus read-model tables for queries

## Tradeoffs

- cleaner separation of business writes and optimized reads
- easier scaling of query workloads
- more flexible reporting and projections
- more complexity in data synchronization, testing, and consistency handling
