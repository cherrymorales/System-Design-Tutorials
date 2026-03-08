# Event-Driven Architecture

## What It Is

An event-driven architecture coordinates system behavior through events. One part of the system publishes an event, and other parts react to it asynchronously.

This reduces direct coupling and works well for workflows that span multiple capabilities.

## Best Used When

- business processes are naturally asynchronous
- multiple systems need to react to the same business action
- auditability and decoupling are important
- the platform needs responsive workflows without tight request chaining

## Not Ideal When

- the domain is simple and synchronous request-response flows are enough
- the team is not ready for eventual consistency and asynchronous debugging

## Why It Is Common

Modern systems frequently use event-driven patterns for integrations, background processing, notifications, and scalable business workflows.

## Recommended Technology

- Frontend: React
- Backend: ASP.NET Core
- Messaging: RabbitMQ for simpler setups, Kafka for high-throughput event streaming
- Database: PostgreSQL plus outbox pattern when reliability matters
- Hosting: usually multiple containers when a message broker is involved

## Single-Container Guidance

The application code may stay simple, but true event-driven solutions often need at least a separate broker container. Single-container deployment is possible only for learning or lightweight demos.

## Example Project

**Project idea:** Online order processing and notification platform

Example event flow:

- order created
- payment confirmed
- inventory reserved
- shipment scheduled
- customer notified

## Suggested Solution Shape

- React frontend for order and operations views
- ASP.NET Core APIs and workers
- RabbitMQ for domain events and background workflows
- PostgreSQL for transactional state

## Tradeoffs

- good decoupling between business capabilities
- scalable asynchronous processing
- easier fan-out to multiple consumers
- harder debugging, replay, monitoring, and consistency management
