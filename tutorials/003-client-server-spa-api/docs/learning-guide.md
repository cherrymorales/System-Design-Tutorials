# Client-Server SPA + API Learning Guide

## Purpose

This tutorial is intended to teach how to build a modern authenticated business web application with a clean separation between browser client and backend API.

The main lesson is not just "use React with an API". The main lesson is how to define the boundary correctly so the product remains maintainable and testable.

## Learning Objectives

By the end of this tutorial, you should be able to:

- explain what responsibilities belong to the SPA and what responsibilities belong to the server
- describe why same-origin cookie auth is often the simplest correct starting point for internal web apps
- design API endpoints around workflows instead of database tables
- explain how filtered dashboards and detail screens should influence API shape
- describe how a SPA + API system should be tested across multiple layers

## Core Concepts To Focus On

### 1. The Browser Is Not The Business Authority

The browser is responsible for user experience, not business truth.

This means:

- the SPA can guide users
- the SPA can pre-validate forms
- the SPA can hide unavailable actions
- the server still makes the real decision

### 2. Screen Design And API Design Must Fit Together

A common failure mode is designing screens and APIs independently.

For this architecture, the correct question is:

- what does the screen need to do efficiently and safely

Then design the API around that workflow.

### 3. Same-Origin Hosting Simplifies The First Release

A client-server architecture does not require operational complexity from day one.

Same-origin hosting keeps:

- auth simpler
- cookies simpler
- CORS simpler
- deployment easier to explain

### 4. UI Validation Is Helpful But Not Sufficient

The UI should help users enter valid data, but it should never become the only place where business rules live.

The server must still validate:

- task transitions
- membership rules
- assignment rules
- archived project restrictions

### 5. Tests Must Reflect User And Contract Reality

Do not stop at unit tests.

This architecture is only properly tested when:

- backend rules are tested
- API contracts are tested
- frontend workflow behavior is tested
- the sign-in to task-update smoke path is tested

## Common Misunderstandings

### "If The SPA Hides The Button, The Action Is Secure"

False.

The API must enforce authorization even if the UI already hides the action.

### "SPA + API Means The Frontend Should Handle More Business Logic"

False.

A richer frontend experience does not justify moving authority into the browser.

### "REST Means Everything Must Be Generic CRUD"

False.

Workflow-specific endpoints are often clearer and safer for real business applications.

### "Separate Client And Server Means Separate Production Hosts"

Not necessarily.

Runtime concern separation and deployment concern separation are different decisions.

## Why This Example Works Well

The project delivery example is useful because it combines:

- authenticated internal users
- multiple roles
- status workflows
- list and detail screens
- dashboards and filters
- comment-driven collaboration

That makes it realistic enough to teach the architecture without forcing microservices or realtime complexity.

## Comparison Lens

### Compared To A Server-Rendered Web App

This architecture gives:

- richer client interactions
- clearer API reuse potential
- more explicit client-server contract design

It costs:

- more frontend state management
- more contract discipline

### Compared To A Layered Or Modular Monolith

Those tutorials emphasize backend internal organization.

This tutorial emphasizes:

- client-server boundary
- contract design
- authenticated browser workflow behavior

### Compared To Microservices

This architecture stays much simpler operationally.

Use it when one product can still be owned as one system and one database without forced service boundaries.

## Review Questions

- Why should task status validation stay on the server?
- Why is `/api/auth/me` useful for a SPA?
- Why might `POST /api/tasks/{id}/start` be better than a generic full-object update for this system?
- Why should the dashboard summary be served from the API instead of fully computed in the browser?
- Why is project membership an important server-side authorization boundary?

## Practical Exercise

Take the task status workflow and answer these questions:

1. What part of the flow belongs in the SPA?
2. What part belongs in the API?
3. What data does the task detail page need after a successful transition?
4. What tests prove the transition is implemented correctly?

## Completion Criteria

You should consider the learning goals complete when you can:

- explain the architecture without confusing hosting model and responsibility model
- describe the main API routes and why they exist
- describe the role and membership authorization rules
- explain the full testing strategy without skipping frontend or API coverage
- start implementation without unresolved architecture questions
