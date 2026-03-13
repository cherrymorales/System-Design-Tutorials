# Event Sourcing

## What It Is

Event sourcing stores state changes as a sequence of events rather than persisting only the latest snapshot.

Current state is reconstructed from the event stream or from projections and snapshots built from those events.

## Best Used When

- auditability and change history are central requirements
- the domain benefits from replay, reconstruction, or temporal analysis
- business actions are naturally modeled as meaningful domain events
- downstream projections need to be derived from the same event history

## Not Ideal When

- the domain is simple CRUD
- the team does not want the added complexity of event versioning and replay
- audit history can be handled more simply with regular tables and change logs

## Why It Is Common

Event sourcing is common in domains like finance, ledgers, workflow history, and systems where replayability and exact change tracking matter more than simple current-state persistence.

It is less universal than CRUD or CQRS, but very important in the right domain.

## Recommended Technology

- Frontend: React
- Backend: ASP.NET Core
- Event store: dedicated event store, PostgreSQL event tables, or Kafka-backed patterns depending on needs
- Projections: PostgreSQL read models, search indexes, or analytics stores
- Messaging: often paired with CQRS and event-driven projections

## Single-Container Guidance

A learning implementation can stay small, but event sourcing usually becomes more realistic with separate projection workers or read-model processes.

## Example Project

**Project idea:** Accounting ledger and audit-trail platform

Example event stream:

- account opened
- debit posted
- credit posted
- adjustment recorded
- statement projection updated

## Suggested Solution Shape

- React frontend for ledger operations and audit views
- ASP.NET Core command API for posting financial actions
- append-only event storage
- projection handlers for balances, statements, and reporting views

## Tradeoffs

- excellent auditability and temporal reconstruction
- strong fit for domains where history is the real source of truth
- replay and projection flexibility
- more complexity in event design, versioning, debugging, and operational tooling
