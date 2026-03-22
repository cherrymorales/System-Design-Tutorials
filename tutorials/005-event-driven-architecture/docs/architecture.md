# Event-Driven Architecture Guide

## Architectural Intent

The system exists to keep upload initiation responsive while allowing multiple independent processing steps to happen asynchronously.

The architecture is designed so that one asset event can trigger several reactions without the API needing to call every downstream processor directly.

## Core Components

### 1. Operations API

Responsibilities:

- authenticate operators
- register assets
- mark uploads as complete
- persist transactional asset records
- publish workflow-starting events

### 2. Event Broker

Responsibilities:

- distribute events to subscribed consumers
- isolate producers from downstream processing detail
- support retry and dead-letter routing

RabbitMQ is the tutorial broker because it is practical for local development and clear enough for learning.

### 3. Processing Workers

Separate workers react to asset events:

- scan worker
- metadata worker
- thumbnail worker
- transcode worker
- notification worker

Each worker should own one focused reaction rather than becoming a general workflow controller.

### 4. Projection Updater

Responsibilities:

- consume processing outcome events
- maintain operator-visible status
- support the React console with read-friendly data

This projection is critical because event-driven systems need a stable read model for the UI.

## Communication Model

### Synchronous

Use synchronous HTTP for:

- sign in
- asset registration
- upload completion acknowledgement
- operator reads from the projection API

### Asynchronous

Use events for:

- beginning processing
- tracking scan, metadata, thumbnails, and transcode outcomes
- requesting notifications
- updating projection state

The rule is simple:

- user-facing transactional actions are synchronous
- downstream workflow progression is asynchronous

## Event Flow

Typical happy path:

1. operator registers an asset
2. upload completes
3. API publishes `AssetUploadCompleted`
4. scan worker reacts
5. metadata worker reacts
6. thumbnail worker reacts
7. transcode worker reacts
8. projection updater consumes the resulting events
9. when required steps complete, the system publishes `AssetReady`
10. notification worker reacts to `AssetReady`

This is fan-out rather than direct orchestration.

## Data Ownership

The system should keep two broad data concerns separate:

- transactional asset records
- projected operator-facing status views

Workers should not directly mutate each other's internal state.

Instead:

- the API owns core asset record creation
- workers emit outcome events
- projection logic builds operator-visible status

## Reliability Design

### Outbox

The API should not publish workflow events without ensuring the initial asset transaction is durable.

The implementation baseline should therefore include an outbox or an equivalent reliable publish pattern.

### Idempotency

Every consumer must safely handle repeat delivery.

Examples:

- metadata extraction event arrives twice
- thumbnail generation completion is replayed
- projection receives duplicate ready-state messages

### Dead-Lettering

If a consumer cannot process a message after retry policy is exhausted, the message must move to a dead-letter path and the failure must be visible to operators or support personnel.

### Ordering

The architecture should not depend on perfect global ordering.

Where ordering matters, it should be defined per asset or per processing step, not assumed across the entire system.

## UI Implications

The React console should not assume immediate final state after upload completion.

It must display:

- queued or processing status
- partial completion
- failed stages
- ready state

This is a core part of the architecture, not a frontend detail.

## Security Boundaries

The first release is internal-only.

Security expectations:

- authenticated operations users only
- role-based access to registration and monitoring actions
- no direct worker exposure to the browser
- internal event handling components remain behind the API boundary

## Observability Requirements

The system should emit enough telemetry to answer:

- which asset is being processed
- which step succeeded or failed
- how long each stage took
- whether a message was retried
- whether a message reached dead-letter handling

Without this, event-driven systems become difficult to operate.

## Why This Is Event-Driven Rather Than Just Background Jobs

This architecture is not merely "API plus one worker."

It is event-driven because:

- one event triggers multiple independent consumers
- the producer does not coordinate all downstream work directly
- projections and notifications are downstream reactions
- system behavior emerges from event publication and subscription

## Architectural Tradeoffs

- better decoupling between the initial API transaction and downstream work
- easier fan-out to multiple handlers
- better fit for long-running processing
- more complexity in reliability, debugging, observability, and testing
