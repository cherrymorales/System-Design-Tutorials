# Client-Server SPA + API Project Plan

## Project Summary

This tutorial defines a project delivery and team collaboration platform for internal teams managing projects, tasks, comments, and delivery status in one web application.

The system will be built using:

- React for the frontend SPA
- ASP.NET Core for the backend API
- PostgreSQL for the database

The goal is to provide a modern web-first application that demonstrates how a client-server SPA + API architecture should be planned, implemented, deployed, and tested.

## Problem Statement

The organization needs a centralized way to:

- manage active projects and milestones
- assign tasks and due dates clearly
- track project progress and overdue work in one place
- capture task comments and status history
- provide an app-like workflow UI rather than relying on server-rendered forms or spreadsheets
- expose business behavior through stable APIs rather than putting all logic directly into the frontend

Without a clear client-server design, the frontend risks becoming tightly coupled to backend internals, API contracts become inconsistent, and critical workflow rules are duplicated between client and server.

## Project Goals

- provide a responsive SPA for project and task workflows
- keep business logic and persistence on the server
- define stable backend API contracts for the main use cases
- support role-based authenticated access
- keep deployment simple enough for one product team
- make testing part of the implementation baseline from the start

## Learning Value

From a learning perspective, this project demonstrates:

- how to separate client responsibilities from server responsibilities
- how to shape APIs for an interactive SPA rather than for server-rendered pages
- how a same-origin SPA + API system can stay simple to deploy
- how frontend, backend, and testing concerns fit together in this architecture

## Success Metrics

- users can create and manage projects without spreadsheet coordination
- users can create, assign, update, and comment on tasks in one application
- project dashboards show overdue and in-progress work clearly
- API contracts remain stable enough that the SPA does not depend on backend implementation details
- the MVP has automated coverage for backend rules, API behavior, frontend workflows, and smoke-path validation

## Stakeholders

- workspace admins
- project managers
- contributors
- viewers
- internal technical team
- product owner for delivery operations

## Assumptions

- the first release is for authenticated internal users only
- one web client is the primary consumer of the API in V1
- a single backend application and single database are sufficient for MVP
- same-origin hosting is acceptable for the first release
- real-time collaboration is not required for the first implementation
- a clean recreated local database is acceptable for tutorial local development

## Locked Decisions For V1

The following decisions are finalized for the first implementation:

- frontend: React SPA with route-based navigation
- backend: ASP.NET Core API with explicit route contracts
- auth: same-origin cookie authentication for web-first delivery
- API style: REST-style JSON endpoints for the main workflows
- deployment model: single-application-first by serving built SPA assets from the backend host when practical
- local development model: Vite dev server plus API backend is acceptable during development
- state rule: the server is the source of truth for project, task, and comment state
- validation rule: the backend owns business validation; the frontend may add UX validation but must not become the authority
- testing rule: MVP is not complete without backend, API, frontend, and smoke-test coverage
- membership rule: project access is limited to project members plus workspace admins
- assignee rule: a task has one assignee in V1
- comment rule: comments are append-only in V1 except for author edits within a short grace period
- audit rule: status changes and comments create timeline entries visible in task detail

## Scope

### MVP In Scope

- user login and session management
- project CRUD
- project membership management
- task CRUD within projects
- task assignment and status tracking
- task comments and basic activity history
- dashboard views for project progress and overdue items
- filtering by project, assignee, and task status
- role-based access for workspace admins, project managers, contributors, and viewers
- automated tests for key workflows

### Explicitly Out Of Scope

- real-time multi-user presence or collaborative editing
- external notification channels such as Slack or Teams
- native mobile applications
- advanced analytics and forecasting
- document storage for large attachments
- multi-tenant SaaS isolation

## Recommended Delivery Phases

### Phase 1: Foundation

Planned deliverables:

- solution and project structure
- auth baseline and seeded roles/users
- React SPA shell and navigation scaffold
- backend host, database setup, and API conventions
- test project setup and run conventions

### Phase 2: Core Project And Task Management

Planned deliverables:

- project CRUD
- task CRUD
- assignment and due-date handling
- status transitions
- backend domain and application tests for projects and tasks
- frontend pages for projects and tasks

### Phase 3: Comments, Dashboards, And Filtering

Planned deliverables:

- task comments
- activity timeline or audit trail baseline
- project and dashboard summaries
- filtered task and project views
- API integration tests for filters, auth, and workflow endpoints
- frontend tests for dashboard and task-detail flows

### Phase 4: Hardening And Release Readiness

Planned deliverables:

- authorization refinement by role
- error-handling consistency
- end-to-end smoke tests
- deployment packaging baseline
- release readiness validation

## Milestones

1. Scope and document approval
2. Client-server responsibility approval
3. API shape approval
4. Deployment and auth model approval
5. MVP implementation start
6. Core workflows complete
7. Test baseline complete
8. Release readiness review

## Functional Requirements

- users can sign in and maintain a session
- users can create and manage projects
- users can manage project membership for valid roles
- users can create and manage tasks within projects
- users can assign tasks to team members
- users can move tasks through valid states
- users can add comments to tasks
- users can view dashboard summaries and filtered task lists
- the server enforces the project and task workflow rules
- the frontend consumes stable API contracts rather than backend internals
- automated tests prove the main user and API flows

## Workflow Decisions

### Task Lifecycle

The task lifecycle for V1 is:

1. `Backlog`
2. `InProgress`
3. `Blocked`
4. `InReview`
5. `Done`
6. `Cancelled`

Behavior rules:

- a new task starts as `Backlog`
- a task can move from `Backlog` to `InProgress` or `Cancelled`
- a task can move from `InProgress` to `Blocked`, `InReview`, or `Cancelled`
- a task can move from `Blocked` back to `InProgress`
- a task can move from `InReview` to `Done`, `InProgress`, or `Blocked`
- blocked tasks require a blocker note in V1
- cancelled tasks remain visible in history but no longer accept operational updates
- completed tasks remain commentable for audit context but cannot return to active work in V1

### Project Lifecycle

The project lifecycle for V1 is:

1. `Planned`
2. `Active`
3. `AtRisk`
4. `Completed`
5. `Archived`

Behavior rules:

- tasks may only be created for active or at-risk projects
- planned projects may be edited but not worked actively
- archived projects are read-only in V1
- dashboard summaries are derived from project and task state together
- completed projects may accept comment-only follow-up but no new tasks in V1

## Authorization Rules

- `WorkspaceAdmin` can manage all users, all projects, and all tasks
- `ProjectManager` can create projects, manage projects they own, manage membership, and manage task assignment within those projects
- `Contributor` can view projects where they are a member, update tasks assigned to them, and add comments on projects they can access
- `Viewer` can view projects where they are a member and dashboard summaries, but cannot mutate project or task state

These rules are part of the implementation baseline and should not be deferred to later design work.

## Non-Functional Requirements

- web-first user experience with SPA navigation
- clear API contracts for the main workflows
- role-based authorization
- maintainable client and server separation
- simple single-application-first deployment model
- automated coverage strong enough to demonstrate the architecture properly

## Major Risks

- the SPA becomes too dependent on backend implementation details
- the API becomes chatty and inefficient for the main screens
- frontend and backend validation rules drift apart
- auth and authorization responsibilities become unclear across client and server
- testing focuses only on unit tests and misses real user flows

## Risk Mitigations

- lock the API shape before implementation starts
- define clear server-owned validation rules in the blueprint
- keep same-origin auth simple for V1
- treat frontend tests and API integration tests as part of the MVP baseline
- review the app from workflow and contract perspectives, not only from component or controller perspectives

## Readiness Gates

Implementation may start only when:

- the architecture, implementation, deployment, and testing docs are complete
- the example system and workflow states are accepted
- the auth model is accepted
- the single-container-first deployment model is accepted
- the testing strategy is accepted as part of the MVP baseline

## Definition Of MVP Complete

MVP is complete when:

- users can sign in and access role-appropriate areas
- projects, tasks, and comments work through the documented workflows
- dashboards and filters support the core project-management path
- the React client consumes the documented API shape cleanly
- the app runs locally and has a production-direction deployment plan
- automated tests cover backend rules, API behavior, frontend workflow paths, and a smoke-tested main flow

## Recommendation

Proceed with implementation only after this document set is accepted as the locked baseline for the first build.
