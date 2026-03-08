# Microservices

## What It Is

Microservices split a system into multiple independently deployable services, each responsible for a focused business capability.

Each service owns its logic and often its own data store, while the full product emerges from collaboration between services.

## Best Used When

- the business domain is large and clearly divided into bounded contexts
- multiple teams need to work and deploy independently
- some parts of the platform require different scaling behavior
- platform maturity is high enough to support distributed systems operations

## Not Ideal When

- the team is small
- the product is early-stage
- deployment and observability maturity are low
- a modular monolith would solve the problem more simply

## Why It Is Common

This is a widely used architecture in larger organizations because it helps scale teams and systems independently, but it comes with real operational cost.

## Recommended Technology

- Frontend: React
- Backend: ASP.NET Core services by default
- Communication: REST for simple interactions, messaging for asynchronous flows
- Data: database per service where possible
- Hosting: multiple containers are usually required

## Single-Container Guidance

This architecture is usually not a good fit for single-container deployment. Multiple containers are normally the right choice because service independence is part of the design.

## Example Project

**Project idea:** Large e-commerce platform

Possible services:

- product catalog
- cart
- checkout
- payment
- shipping
- notification

## Suggested Solution Shape

- React storefront and admin portal
- ASP.NET Core services per domain capability
- API gateway for client access
- RabbitMQ or Kafka for asynchronous communication
- separate persistence per service

## Tradeoffs

- strong team and deployment independence
- flexible scaling by service
- better alignment with large domains
- higher complexity in deployment, observability, testing, and data consistency
