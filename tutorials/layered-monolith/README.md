# Layered Monolith

## What It Is

A layered monolith is a single deployable application where responsibilities are separated into logical layers such as presentation, application, domain, and data access.

Even though the code is organized into layers, the whole system is still built, tested, and deployed as one unit.

## Best Used When

- the product is new and needs fast delivery
- the team is small to medium-sized
- the domain is still changing often
- operational simplicity matters more than independent service scaling

## Not Ideal When

- different parts of the system need very different scaling patterns
- many teams need to deploy independently
- the codebase has grown so large that tight coupling slows every change

## Why It Is Common

This is one of the most common starting points in the industry because it is easy to build, easy to debug, and cheap to operate compared with distributed systems.

## Recommended Technology

- Frontend: React
- Backend: ASP.NET Core
- Database: PostgreSQL or SQL Server
- Hosting: single Docker container when the frontend is served by the backend, or one app container plus one database container for local development

## Single-Container Guidance

This design is one of the best candidates for a single-container deployment because the application is a single unit by nature.

## Example Project

**Project idea:** Internal HR and employee management portal

Why it fits:

- clear business workflows
- mostly transactional data
- moderate scale
- simple operations

## Suggested Solution Shape

- React frontend for dashboards and forms
- ASP.NET Core backend for business logic and APIs
- Entity Framework Core for persistence
- PostgreSQL for relational data

## Tradeoffs

- simple to build and operate
- easier local development than distributed systems
- can become hard to maintain if module boundaries are not enforced
- scaling happens mostly as a whole application
