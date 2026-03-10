# Client-Server SPA + API Testing Strategy

## Purpose

This document defines how the `003` tutorial should be tested once implementation starts.

Testing is part of the MVP. It is not a later optional hardening step.

## Testing Philosophy

The goal is to prove that:

- server-side business rules work
- API contracts behave correctly
- the SPA supports the documented workflows
- the system works through the main user path from sign-in to task completion

The testing strategy must reflect how users actually use the product, not only how individual classes behave in isolation.

## Test Layers

### 1. Backend Domain And Application Tests

Purpose:

- validate task and project rules quickly
- validate authorization-sensitive workflow guards where possible
- validate membership and assignment rules

Recommended examples:

- project cannot accept new tasks when archived
- task cannot move to `Blocked` without a blocker note
- task assignee must be a project member
- contributor cannot update a task outside allowed ownership rules

Recommended tooling:

- xUnit
- FluentAssertions if the repo adopts it

### 2. API Integration Tests

Purpose:

- validate route behavior, auth behavior, status codes, and contract shapes
- prove that the real HTTP boundary behaves correctly

Recommended examples:

- unauthenticated access to protected endpoints returns `401`
- viewer cannot mutate project or task state
- project manager can add project members
- contributor can comment on accessible tasks
- invalid task transition returns the expected error response
- dashboard filtering returns correct results

Recommended tooling:

- xUnit
- `WebApplicationFactory`
- test database seeded for deterministic scenarios

### 3. Frontend Tests

Purpose:

- validate route-level SPA behavior
- validate form and interaction logic
- validate loading, error, and success states

Recommended examples:

- login form submits credentials and handles failure
- project detail screen renders task list from API data
- task detail screen disables unavailable actions based on returned state
- filter controls call the correct query path and update the screen
- comment form posts and refreshes the thread

Recommended tooling:

- Vitest
- React Testing Library
- Mock Service Worker or equivalent API mocking approach

### 4. Smoke Tests

Purpose:

- prove the main path works in a built environment

Recommended smoke path:

1. sign in
2. open dashboard
3. open project detail
4. create or update a task
5. add a comment
6. verify updated status appears

Recommended tooling:

- Playwright

## Test Matrix

### Auth And Session

- valid login succeeds
- invalid login fails with expected message
- signed-in session can be restored through `/api/auth/me`
- logout clears the session
- protected routes require authentication

### Projects

- project manager can create a project
- project manager can edit owned project
- archived project rejects write attempts
- viewer cannot create or edit projects
- project list only shows accessible projects for a non-admin user

### Membership

- project manager can add a contributor to a project
- non-manager cannot change membership
- removed members no longer appear in assignable-user lists
- historical activity remains visible after member removal

### Tasks

- task creation requires an active or at-risk project
- assignee must be a current project member
- valid status transitions succeed
- invalid status transitions fail
- contributor cannot modify tasks they are not allowed to manage

### Comments And Activity

- comment creation succeeds for authorized users
- empty comment body fails validation
- comment edit is limited to allowed grace-period behavior
- status changes create activity entries

### Dashboard And Filters

- dashboard summary counts match seeded scenario data
- assignee filter reduces visible tasks correctly
- overdue filter returns only overdue work
- project status filter excludes archived or completed work as expected

## UI And Contract Alignment Checks

The SPA and API must be tested together conceptually even when tests are separate.

Explicit checks:

- frontend assumptions about task states match backend enum values
- frontend action availability matches backend authorization and workflow rules
- validation messages map cleanly from API responses into the UI
- dashboard requests use the documented query contract instead of hidden client-only calculations

## Test Data Strategy

Use deterministic seeded data for:

- one workspace admin
- one project manager
- two contributors
- one viewer
- at least two projects with different statuses
- at least one overdue task
- at least one blocked task
- at least one completed task

This dataset should support all main test layers without rewriting fixtures per test file.

## Recommended Test Project Layout

```text
implementation/
  tests/
    backend/
      SystemDesignTutorials.ClientServerSpaApi.Tests/
        Domain/
        Api/
    frontend/
      src/
        auth/
        dashboard/
        projects/
        tasks/
    smoke/
      playwright/
```

## Execution Order

Recommended local order:

1. backend tests
2. frontend tests
3. smoke tests when feature behavior changes materially

Recommended CI order:

1. backend build
2. backend tests
3. frontend build
4. frontend tests
5. application packaging
6. smoke tests against the built app

## MVP Test Completion Criteria

The MVP testing strategy is implemented only when:

- backend rules for projects, tasks, comments, and membership are covered
- API authorization and workflow routes are covered
- frontend tests cover login, dashboard, project detail, and task detail behavior
- smoke tests cover the main sign-in and task-update path

## What To Avoid

- relying only on unit tests
- mocking away the HTTP boundary for every meaningful API behavior
- putting business-rule assertions only in frontend tests
- skipping role-based authorization scenarios
- building smoke tests that are so broad they become the only safety net
