# Event-Driven Architecture Testing Strategy

## Purpose

This document defines how the `005-event-driven-architecture` MVP should be tested.

The goal is not only to verify API behavior, but to prove that asynchronous handlers, projections, retries, and final operator-visible state behave correctly.

## Testing Philosophy

The test suite must prove five things:

- the API produces the right events from the right actions
- consumers process events correctly and idempotently
- projections reflect asynchronous progress accurately
- failures remain visible rather than disappearing into the broker
- the end-to-end processing path works from asset submission to ready state

## Test Layers

### 1. Domain And Handler Tests

Purpose:

- validate processing rules, state transitions, and handler behavior

Examples:

- an asset cannot become `Ready` until all required stages complete
- duplicate completion events do not double-apply state
- a failure event marks the projection failed

### 2. API Integration Tests

Purpose:

- validate route behavior, auth behavior, and event publication boundaries

Examples:

- unauthenticated requests return `401`
- asset registration succeeds for authorized users
- upload-complete publishing path returns quickly and records the event transaction correctly

### 3. Contract Tests

Purpose:

- validate event schema shape and compatibility expectations

Examples:

- `AssetUploadCompleted` contains correlation and asset identifiers
- `AssetReady` includes the required projection payload
- consumers reject malformed payloads cleanly

### 4. Projection Tests

Purpose:

- validate the operator-facing status model

Examples:

- scan completion updates the projection
- a failed transcode updates failure reason and lifecycle state
- ready state appears only after all required stages complete

### 5. Frontend Tests

Purpose:

- validate operations console workflows against the projected API

Examples:

- asset registration form submits correctly
- list view shows in-progress status
- detail view shows failed or ready outcomes correctly

### 6. End-To-End Smoke Tests

Purpose:

- validate the highest-value operational path against a seeded running environment

Required MVP smoke path:

1. sign in
2. register an asset
3. mark upload complete
4. observe asynchronous processing progression
5. confirm the asset reaches `Ready`

## Reliability Checks

The testing strategy should explicitly prove:

- duplicate messages do not corrupt state
- transient failures are retried
- non-recoverable failures are visible
- the UI can represent eventual consistency without confusion

## Recommended Execution Order

For local development:

1. run domain and handler tests
2. run API integration tests
3. run contract and projection tests
4. run frontend tests
5. run smoke tests when workflow behavior changes materially

For CI:

1. build
2. run backend tests
3. run frontend build and tests
4. run smoke tests against a seeded composed environment

## MVP Test Completion Criteria

The `005` MVP is not complete unless:

- the main upload-to-ready path is automated
- failure-state visibility is automated
- contract tests cover event payload expectations
- frontend tests cover the core operator path
- smoke tests prove the end-to-end asynchronous behavior
