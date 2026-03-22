# Event-Driven Architecture Project Plan

## Project Summary

This tutorial defines a digital asset ingestion and processing platform for a media operations team.

The system will be built using:

- React for the operations frontend
- ASP.NET Core for the API and workers
- RabbitMQ for asynchronous events
- PostgreSQL for transactional and projection data

The goal is to provide a realistic tutorial that demonstrates when event-driven architecture is appropriate, how to define event boundaries, and how asynchronous processing should be planned, tested, and deployed.

## Problem Statement

The media team needs a centralized way to:

- register and track uploaded assets
- avoid long synchronous request chains during processing
- trigger multiple downstream processing steps from one upload event
- keep operators informed while background work is still running
- retry or investigate failed processing stages without losing traceability

The system should remain responsive even when processing work takes minutes rather than milliseconds.

## Project Goals

- define clear producer and consumer responsibilities
- make asynchronous fan-out explicit
- keep the upload API responsive
- support status tracking through projections
- make retry, dead-letter, and idempotency part of the baseline design
- make automated testing part of the MVP baseline

## Learning Value

From a learning perspective, this project demonstrates:

- when events are a better coordination boundary than direct HTTP calls
- how asynchronous workflows affect UI and operator expectations
- how to design a processing pipeline without hiding complexity
- why read models and status projections matter in asynchronous systems

## Success Metrics

- operators can register an asset and monitor its processing lifecycle
- an upload event fans out into multiple background reactions
- failures are visible without blocking the original upload request
- the dashboard reflects processing state through a projection model
- the MVP has automated coverage for handlers, contracts, frontend workflows, and smoke paths

## Stakeholders

- content operations coordinators
- media processing engineers
- support operators
- operations managers
- internal platform team

## Assumptions

- the first release is for internal authenticated staff only
- a single React operations console is the only browser client in V1
- RabbitMQ is acceptable for the tutorial messaging baseline
- PostgreSQL is sufficient for transactional state and projections in V1
- multiple containers are acceptable because the broker is part of the architecture

## Locked Decisions For V1

The following decisions are finalized for the first implementation:

- one API registers assets and publishes workflow-starting events
- processing steps run asynchronously in workers
- workflow coordination happens through events, not chained HTTP calls
- RabbitMQ is the V1 broker
- PostgreSQL stores asset records, processing status, and operator-visible projections
- the UI reads projected processing state rather than polling every worker directly
- the system uses idempotent consumer behavior for all workflow handlers
- dead-letter handling is required for failed, non-recoverable messages
- testing is part of the MVP baseline

## Scope

### MVP In Scope

- authenticated operations console
- asset registration
- upload completion event
- malware or safety scan step
- metadata extraction step
- thumbnail generation step
- transcoding step
- status projection for operators
- notification request when an asset becomes ready
- automated testing of the asynchronous baseline

### Explicitly Out Of Scope

- public media delivery
- moderation review queues
- AI tagging and enrichment
- cross-region replication
- DRM and licensing workflows
- external studio integrations

## Recommended Delivery Phases

### Phase 1: Foundation And Messaging Baseline

Planned deliverables:

- API skeleton
- worker skeletons
- broker and database setup
- seeded users and auth baseline
- local run conventions
- test project setup

### Phase 2: Core Asset Registration And Projection Baseline

Planned deliverables:

- asset registration API
- asset store record model
- upload completion event
- operations status projection baseline
- API and frontend skeleton coverage

### Phase 3: Processing Fan-Out Workflow

Planned deliverables:

- scan, metadata, thumbnail, and transcode consumers
- processing status updates
- ready-state notification trigger
- retry and failure-state modeling
- contract and handler tests

### Phase 4: Hardening And Release Readiness

Planned deliverables:

- authorization refinement
- dead-letter and failure visibility
- frontend workflow tests
- smoke tests for the main upload-to-ready path
- deployment packaging baseline

## Functional Requirements

- users can sign in through the operations console
- users can register a new asset
- the system can record upload completion
- upload completion triggers background processing steps
- operators can see processing progress without waiting on the request thread
- the system can show failed stages clearly
- the system can mark an asset ready only after required processing steps succeed
- the system can request a notification after readiness
- automated tests must prove the main success path and failure visibility path

## Workflow Decisions

### Asset Lifecycle

The asset lifecycle for V1 is:

1. `Registered`
2. `UploadPending`
3. `Uploaded`
4. `Processing`
5. `Ready`
6. `Failed`

Behavior rules:

- asset registration creates the initial record
- upload completion begins asynchronous processing
- processing status is derived from downstream event outcomes
- the asset becomes `Ready` only after required processing steps succeed
- non-recoverable failure moves the asset to `Failed`

### Event Set For V1

Core events:

- `AssetRegistered`
- `AssetUploadCompleted`
- `AssetScanCompleted`
- `MetadataExtracted`
- `ThumbnailGenerationCompleted`
- `TranscodeCompleted`
- `AssetReady`
- `AssetProcessingFailed`
- `NotificationRequested`

### Reliability Rules

- consumers must be idempotent
- retries are allowed for transient failures
- permanent failures go to dead-letter handling
- projections must tolerate duplicate event delivery

## Non-Functional Requirements

- responsive upload initiation
- observable asynchronous workflow progression
- clear operator-visible status
- reliable retry behavior
- traceable event flow
- test coverage strong enough to demonstrate asynchronous correctness

## Major Risks

- event contracts are unclear or too technical
- duplicate delivery breaks state updates
- operators cannot understand partial progress
- too much business logic drifts into one consumer
- the system hides failures instead of surfacing them

## Risk Mitigations

- keep events business-meaningful
- require idempotency in every handler
- build an explicit projection model for the UI
- define dead-letter and retry rules up front
- test duplicate and failure behavior, not only success behavior

## Readiness Gates

Implementation may start only when:

- the README, architecture, implementation, deployment, learning, and testing documents are complete
- the event set is accepted
- the processing lifecycle is accepted
- retry and dead-letter expectations are accepted
- the testing strategy is accepted as part of the MVP baseline

## Definition Of MVP Complete

MVP is complete when:

- the documented processing pipeline is implemented
- the UI shows asset progress through projected state
- the ready-state path works end to end
- failed processing is visible in operator views
- the system runs locally with a broker and worker set
- automated tests cover handlers, API behavior, projections, frontend workflows, and a smoke-tested main path
