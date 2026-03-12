# Client-Server SPA + API

## Overview

A client-server SPA + API architecture separates the interactive web client from the backend application by using a single-page application for the frontend and HTTP APIs for the server.

The client owns rendering, navigation, local interaction state, and user workflow presentation. The server owns authentication, business rules, persistence, authorization, and API contracts.

For this tutorial, the architecture is applied to a concrete system: a project delivery and team collaboration platform used by internal teams to manage projects, tasks, comments, and progress dashboards.

## Why This Tutorial Matters

This is one of the most common modern web application architectures in industry.

It is practical because it gives teams:

- a rich, app-like frontend experience
- clear separation between UI and backend concerns
- reusable API contracts for future clients
- a deployment shape that can remain simple when the system is still one product

## Best Used When

- the product needs a rich interactive web UI
- the frontend has meaningful workflow complexity
- server capabilities should be exposed through clear HTTP contracts
- the team wants independent frontend and backend iteration without committing to microservices
- the application is primarily web-first and authenticated

## Not Ideal When

- the product is mostly static or document-style content
- SEO-heavy public pages are the primary concern and app-like interactions are minimal
- the system is almost entirely asynchronous or event-driven from the start
- the frontend has so little behavior that a full SPA is unnecessary complexity

## Recommended Technology

Recommended tutorial baseline:

- Frontend: React
- Backend: ASP.NET Core Web API
- Database: PostgreSQL or SQL Server
- ORM: Entity Framework Core
- Hosting: keep the SPA and API in one product boundary; local development uses a Vite SPA with an ASP.NET Core API, and backend-served SPA packaging is an optional future deployment step

Recommended client-server shape:

- React SPA with route-based navigation
- ASP.NET Core API for business workflows and auth
- one relational database for transactional data
- same-origin cookie authentication for the first implementation

## Example Project

**Project idea:** Project delivery and team collaboration platform

Concrete scenario:

- internal teams manage client and internal delivery projects
- project managers create projects, milestones, and tasks
- contributors update task status, due dates, and comments
- workspace admins manage users and project access
- team leads need dashboard views for project progress and overdue work

Business objective:

- replace disconnected spreadsheets and chat-thread coordination
- provide a responsive task and project workflow UI
- centralize project status, assignment, and comments in one web app
- keep the backend API clear enough that future mobile or external clients could reuse it later

## Project Scope

### In Scope

- login and session handling
- project and task management
- task comments and activity history
- dashboard and filter-driven views
- role-based access within the application
- API contracts for all major workflows
- automated testing strategy covering backend, API, frontend, and end-to-end smoke paths
- project membership and assignee selection for the MVP workflow

### Out Of Scope For The First Implementation

- public marketing site or SEO-focused content system
- native mobile clients
- offline-first synchronization
- real-time collaborative editing
- file storage pipeline for large attachments
- external calendar or chat integrations

## Implementation Status

This tutorial now includes a buildable MVP implementation.

Implemented now:

- folder structure for `docs/` and `implementation/`
- React SPA pages for login, dashboard, projects, and tasks
- ASP.NET Core API endpoints for auth, projects, tasks, comments, dashboard data, and membership
- PostgreSQL-backed local runtime through Docker Compose
- backend tests, frontend tests, and Playwright smoke coverage

Current runtime shape:

- Vite SPA during local development
- ASP.NET Core API as the server authority
- PostgreSQL as the main local database
- isolated SQLite runtime for smoke and integration tests

Optional future extension:

- serve built SPA assets directly from the backend host if packaged same-origin deployment becomes part of the tutorial goal

## MVP Testing Position

For this tutorial, testing is part of the implementation baseline, not a later hardening phase.

The MVP is only considered complete when it includes:

- backend domain and application tests for business rules
- API integration tests for contracts and authorization
- frontend tests for critical user workflows
- end-to-end smoke tests for the main project-and-task flow

## Tutorial Contents

- [Learning Guide](./learning-guide.md)
- [Project Plan](./project-plan.md)
- [Architecture Guide](./architecture.md)
- [Implementation Blueprint](./implementation-blueprint.md)
- [Deployment Guide](./deployment.md)
- [Testing Strategy](./testing-strategy.md)

## Intended Audience

- developers learning how to structure a modern SPA with a clean backend API
- technical leads deciding when a SPA + API architecture is the right web delivery model
- contributors who need the architecture and testing baseline agreed before coding starts

## What You Should Learn From This Tutorial

By the end of this tutorial, a developer should understand:

- how the client and server divide responsibilities in a SPA + API system
- how API contracts shape frontend behavior and backend design together
- how authenticated browser sessions can remain simple with same-origin-friendly cookie auth and aligned client/server routing
- why authenticated business web apps often fit this pattern well
- how the architecture should be deployed simply before adding unnecessary runtime complexity
- how to test both the client and the server in a way that reflects real user workflows

## Definition Of Documentation Accuracy

This tutorial documentation is accurate when a reader can answer all of the following without guessing:

- what business problem the system solves
- what the client is responsible for
- what the server is responsible for
- what API shape is intended
- what workflows the MVP includes
- how the system is intended to be tested
- how the app should be deployed locally and in production
- what remains future work

## Tradeoffs

- excellent fit for rich interactive web applications
- clean separation between UI and backend responsibilities
- easy to reuse APIs for future clients
- requires discipline to avoid chatty or poorly shaped APIs
- frontend and backend can drift if contracts and validation rules are not kept aligned
- authentication and authorization must be handled deliberately across both the SPA and API layers
- screens and API contracts should be designed together or the experience will become awkward quickly
