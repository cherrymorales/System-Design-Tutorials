# Event-Driven Architecture Implementation Blueprint

## Example System

The tutorial implementation will use a digital asset ingestion and processing platform.

Target users:

- content operations coordinators
- media processing operators
- operations managers

Primary use case:

- an operator registers an asset and marks the upload as complete
- the system performs processing asynchronously
- the operations console shows progress and failures

## Business Capabilities

### Asset Intake

Responsibilities:

- create asset records
- store metadata supplied by operators
- confirm upload completion

### Processing Pipeline

Responsibilities:

- scan files
- extract metadata
- generate thumbnails
- transcode media

### Status Projection

Responsibilities:

- show current step status
- surface failure reasons
- support list and detail views in the operations console

### Notifications

Responsibilities:

- request user or operator notifications when an asset becomes ready or fails

## User Roles

### Content Operations Coordinator

- register assets
- mark uploads complete
- track processing progress

### Operations Manager

- review the processing dashboard
- inspect failed assets
- monitor throughput and failures

## Core Data Model

### Transactional Asset Record

Suggested fields:

- `assetId`
- `assetKey`
- `title`
- `submittedBy`
- `createdAt`
- `currentLifecycleState`
- `failureReason`

### Projection Record

Suggested fields:

- `assetId`
- `title`
- `lifecycleState`
- `scanStatus`
- `metadataStatus`
- `thumbnailStatus`
- `transcodeStatus`
- `readyAt`
- `lastUpdatedAt`
- `failureReason`

## Event Contracts

The implementation should define explicit contracts for:

- `AssetUploadCompleted`
- `AssetScanCompleted`
- `MetadataExtracted`
- `ThumbnailGenerationCompleted`
- `TranscodeCompleted`
- `AssetReady`
- `AssetProcessingFailed`
- `NotificationRequested`

Each event should include:

- event identifier
- asset identifier
- occurred-at timestamp
- correlation identifier
- payload relevant to the event

## API Shape

Suggested operator-facing routes:

- `POST /api/assets`
- `POST /api/assets/{assetId}/upload-complete`
- `GET /api/assets`
- `GET /api/assets/{assetId}`
- `GET /api/dashboard`

Suggested behavior:

- create and upload-complete endpoints return quickly
- list and detail endpoints read from the projection model

## UI Shape

Suggested pages:

- asset registration page
- asset list page
- asset detail page
- operations dashboard

UI expectations:

- show in-progress states clearly
- surface failed stages and reasons
- avoid implying that every action is immediately final

## Processing Rules

### Success Path

- upload completion publishes `AssetUploadCompleted`
- scan, metadata, thumbnail, and transcode handlers react
- projection updates after each handler outcome
- once all required steps succeed, publish `AssetReady`
- request notification

### Failure Path

- if a required processing stage fails permanently, publish `AssetProcessingFailed`
- projection marks the asset failed
- operator-visible failure reason is stored
- no ready notification is sent

## Deployment Shape

Local learning deployment should use:

- API container
- worker container or several worker processes
- RabbitMQ container
- PostgreSQL container
- optionally a local object storage emulator

## Acceptance Criteria For MVP

- operators can register and submit an asset for processing
- the API returns without waiting for the full pipeline
- downstream handlers process the event flow asynchronously
- the operations console shows projected processing status
- failures are visible
- the ready path is smoke-tested end to end
