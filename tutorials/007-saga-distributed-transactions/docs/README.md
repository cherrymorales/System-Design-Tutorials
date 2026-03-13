# Saga Distributed Transactions

## What It Is

The saga pattern coordinates a business transaction across multiple services using a sequence of local transactions and compensating actions.

Instead of a single distributed database transaction, each service commits its own work and the system drives the workflow forward or backward through messages and state transitions.

## Best Used When

- one business workflow spans multiple services
- each service owns its own database
- partial failure is expected and must be handled explicitly
- rollback must be done through compensation rather than database transaction scope

## Not Ideal When

- the system is still a monolith
- a single local transaction can already solve the problem
- the team is not ready to design compensation and failure recovery clearly

## Why It Is Common

Once a system uses true service ownership, many real workflows stop fitting inside one transaction.

Saga is common because it gives distributed systems a structured way to manage multi-step workflows without pretending networked services can behave like one database transaction.

## Recommended Technology

- Frontend: React
- Backend: ASP.NET Core services
- Messaging: RabbitMQ, Azure Service Bus, or Kafka
- Persistence: PostgreSQL or another service-owned transactional store per service
- Orchestration: explicit workflow state in one coordinator service or choreography through events

## Single-Container Guidance

For learning, a very small saga demo can run in a single container, but realistic saga examples usually need multiple containers because separate services and a broker are part of the pattern itself.

## Example Project

**Project idea:** Travel booking and itinerary coordination platform

Example workflow:

- reserve flight
- reserve hotel
- authorize payment
- confirm itinerary
- if one step fails, compensate the earlier reservations

## Suggested Solution Shape

- React frontend for itinerary creation and status tracking
- ASP.NET Core services for booking, billing, and itinerary management
- message broker for workflow progression and compensations
- service-owned databases with explicit saga state tracking

## Tradeoffs

- realistic handling of cross-service business transactions
- explicit compensation and failure paths
- better fit for true service-owned data
- more workflow complexity, more states to test, and harder debugging
