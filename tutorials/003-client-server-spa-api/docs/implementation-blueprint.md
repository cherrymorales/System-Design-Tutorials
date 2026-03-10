# Client-Server SPA + API Implementation Blueprint

## Example System

The example system for this tutorial is a project delivery and team collaboration platform for internal teams.

The platform replaces spreadsheets, chat-thread task tracking, and ad hoc status meetings with one authenticated web application.

The system allows users to:

- sign in through the SPA
- view assigned projects and dashboard summaries
- create and manage projects
- create, assign, and track tasks
- add task comments and review task activity
- filter work by project, assignee, and status

## Product Goals

- centralize project and task coordination in one application
- provide a responsive browser-based experience for daily workflow use
- keep business rules on the backend
- define explicit API contracts that match real UI workflows
- demonstrate how a SPA and API should be designed together rather than independently

## Learning Focus

This blueprint should teach:

- how to pick screen boundaries first, then shape API contracts to support them
- how to keep the backend authoritative without making the UI awkward
- how to express workflow actions through explicit API endpoints
- how to keep the initial deployment simple while preserving a clean client-server split

## Locked V1 Decisions

- frontend is a React SPA
- backend is an ASP.NET Core API
- database is PostgreSQL
- authentication is same-origin cookie auth
- production direction is one application host serving SPA assets plus API routes
- project access is membership-based
- task assignment is single-assignee in V1
- timeline activity is append-only
- server-side workflow validation is mandatory

## MVP Scope

### Included In V1

- login, logout, and session restore
- dashboard summary screen
- project list and project detail screens
- project creation and update
- project membership management
- task creation, update, and assignment
- task status workflow actions
- task comments and activity history
- filtered task list views
- role-based authorization
- automated tests across backend, API, frontend, and smoke paths

### Not Included In V1

- realtime collaboration or websocket updates
- file uploads and document storage
- recurring tasks
- subtasks and dependency graphs
- public API consumers
- mobile-specific UX

## Primary User Roles

- `WorkspaceAdmin`
  Full workspace visibility and administrative control.
- `ProjectManager`
  Creates and manages owned projects, members, tasks, and assignments.
- `Contributor`
  Updates assigned tasks and comments within accessible projects.
- `Viewer`
  Read-only access to assigned projects and dashboards.

## Primary Screens

- Login
- Dashboard
- Project list
- Project detail
- Project membership panel
- Task detail
- Task filter view
- Settings or workspace admin area

## Suggested Backend Structure

```text
src/backend/
  SystemDesignTutorials.ClientServerSpaApi.Web/
    Endpoints/
    Contracts/
    Auth/
  SystemDesignTutorials.ClientServerSpaApi.Application/
    Services/
    UseCases/
    Queries/
    Validation/
  SystemDesignTutorials.ClientServerSpaApi.Domain/
    Entities/
    Enums/
    Rules/
  SystemDesignTutorials.ClientServerSpaApi.Infrastructure/
    Persistence/
    Identity/
    Seeding/
```

## Suggested Frontend Structure

```text
src/frontend/src/
  app/
  routes/
  features/
    auth/
    dashboard/
    projects/
    tasks/
    comments/
  shared/
    api/
    components/
    models/
    utils/
```

## Domain Model Starting Point

### Project

Core fields:

- `Id`
- `Name`
- `Code`
- `Description`
- `Status`
- `OwnerUserId`
- `StartDate`
- `TargetDate`
- `CompletedAt`
- `CreatedAt`
- `UpdatedAt`

### ProjectMember

Core fields:

- `Id`
- `ProjectId`
- `UserId`
- `RoleInProject`
- `JoinedAt`

### Task

Core fields:

- `Id`
- `ProjectId`
- `Title`
- `Description`
- `Status`
- `Priority`
- `AssigneeUserId`
- `CreatedByUserId`
- `BlockerNote`
- `DueDate`
- `CompletedAt`
- `CreatedAt`
- `UpdatedAt`

### TaskComment

Core fields:

- `Id`
- `TaskId`
- `AuthorUserId`
- `Body`
- `CreatedAt`
- `UpdatedAt`

### TaskActivity

Core fields:

- `Id`
- `TaskId`
- `Type`
- `ActorUserId`
- `Summary`
- `CreatedAt`

### DashboardSummary

This is a read model, not necessarily a stored entity.

Expected fields:

- `ActiveProjectCount`
- `AtRiskProjectCount`
- `OverdueTaskCount`
- `TasksInReviewCount`
- `MyOpenTaskCount`

## State Machines

### Project State Machine

Allowed states:

1. `Planned`
2. `Active`
3. `AtRisk`
4. `Completed`
5. `Archived`

Rules:

- new projects start as `Planned`
- only `ProjectManager` for the project or `WorkspaceAdmin` can change project status
- tasks may only be created when project status is `Active` or `AtRisk`
- `Archived` is read-only

### Task State Machine

Allowed states:

1. `Backlog`
2. `InProgress`
3. `Blocked`
4. `InReview`
5. `Done`
6. `Cancelled`

Rules:

- new tasks start as `Backlog`
- only valid transitions documented in `project-plan.md` are allowed
- entering `Blocked` requires `BlockerNote`
- entering `Done` records `CompletedAt`
- `Cancelled` is terminal

## End-To-End Workflow Example

### Assign And Complete A Task

1. A project manager opens the project detail screen.
2. The SPA loads the project, membership list, and task summary from API endpoints.
3. The manager creates a task and assigns it to a contributor who is already a project member.
4. The contributor opens the task detail page.
5. The SPA calls `POST /api/tasks/{taskId}/start`.
6. The API validates membership, role, and current status.
7. The contributor adds a comment explaining work progress.
8. The contributor submits the task for review.
9. The project manager reviews and marks the task as done.
10. The dashboard summary reflects the updated counts after the server confirms the state transition.

## API Surface

### Auth

```text
POST /api/auth/login
POST /api/auth/logout
GET /api/auth/me
```

Example login request:

```json
{
  "email": "manager@clientserverspa.local",
  "password": "Password123!"
}
```

### Dashboard

```text
GET /api/dashboard/summary
GET /api/dashboard/my-work
```

### Projects

```text
GET /api/projects
POST /api/projects
GET /api/projects/{projectId}
PUT /api/projects/{projectId}
POST /api/projects/{projectId}/activate
POST /api/projects/{projectId}/mark-at-risk
POST /api/projects/{projectId}/complete
POST /api/projects/{projectId}/archive
```

Example create project request:

```json
{
  "name": "Apollo Client Portal",
  "code": "APOLLO-PORTAL",
  "description": "Internal delivery project for the new client-facing workspace.",
  "targetDate": "2026-06-30"
}
```

### Project Membership

```text
GET /api/projects/{projectId}/members
POST /api/projects/{projectId}/members
DELETE /api/projects/{projectId}/members/{memberId}
```

Example add member request:

```json
{
  "userId": "4f0d2a87-8b37-4d0c-b593-71a07f2d2a30",
  "roleInProject": "Contributor"
}
```

### Tasks

```text
GET /api/tasks
POST /api/tasks
GET /api/tasks/{taskId}
PUT /api/tasks/{taskId}
POST /api/tasks/{taskId}/start
POST /api/tasks/{taskId}/block
POST /api/tasks/{taskId}/submit-review
POST /api/tasks/{taskId}/complete
POST /api/tasks/{taskId}/cancel
```

Example create task request:

```json
{
  "projectId": "9f12c9eb-7f46-4c14-b286-5422f8f1f79a",
  "title": "Implement dashboard filters",
  "description": "Add project status and assignee filters to dashboard views.",
  "assigneeUserId": "2ecf13f1-78a0-4b58-a875-2fe43649e442",
  "priority": "High",
  "dueDate": "2026-04-03"
}
```

Example block task request:

```json
{
  "blockerNote": "Waiting on API contract approval for summary endpoints."
}
```

### Comments

```text
GET /api/tasks/{taskId}/comments
POST /api/tasks/{taskId}/comments
PUT /api/tasks/{taskId}/comments/{commentId}
```

Example add comment request:

```json
{
  "body": "The filter state handling is complete. Starting API wiring next."
}
```

### Users

```text
GET /api/users/assignable
GET /api/users/me/projects
```

## Backend Rules

- the server validates every status transition
- the server validates that assignees are current project members
- the server validates role permissions for project and task changes
- the server records audit activity for status changes and comments
- the server prevents writes to archived projects
- the server prevents contributors from editing tasks not assigned to them unless they are also project managers

## Frontend Rules

- the SPA restores the current session before showing authenticated routes
- route guards are user-experience helpers only; the API remains authoritative
- task and dashboard screens should request projections shaped for those views
- the SPA should not compute authoritative dashboard totals from stale local data
- filter state belongs in the client, but filtered results are returned from the API

## Acceptance Criteria

The system is implementation-ready for MVP when the future implementation can satisfy all of the following:

- a signed-in user can view only projects they are allowed to access
- a project manager can create a project and add members
- a project manager can create and assign tasks
- a contributor can move an assigned task through valid states
- a contributor can add comments to accessible tasks
- dashboard summaries reflect backend-confirmed state
- archived projects are read-only
- automated tests cover business rules, API contracts, frontend workflows, and smoke paths

## Testing Baseline

The detailed testing plan lives in [testing-strategy.md](./testing-strategy.md).

The MVP test baseline requires:

- backend domain and application tests
- API integration tests
- frontend component and workflow tests
- smoke tests for the main sign-in and task lifecycle path

## Keep Simple In The First Build

- use same-origin hosting for production
- keep one primary web client
- keep the task model single-assignee
- keep comments text-only
- avoid realtime and background-event complexity

## Evolution Path

After the MVP, sensible expansions include:

- realtime task update notifications
- richer reporting projections
- file attachments
- mobile clients reusing the API
- explicit API versioning if additional clients create pressure for it
