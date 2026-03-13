# Web Queue Worker

## What It Is

A web-queue-worker architecture separates interactive web requests from asynchronous background processing.

The web application handles user requests quickly, places longer work onto a queue, and one or more worker processes handle the background tasks.

## Best Used When

- user actions trigger long-running or heavy work
- request latency must stay low even when processing takes time
- work can be retried safely
- the system needs to smooth traffic spikes

## Not Ideal When

- all work is quick and synchronous
- background tasks are tightly coupled to in-request transaction state
- the team cannot operate a queue and worker lifecycle reliably

## Why It Is Common

This is one of the most common real-world patterns for exports, report generation, media processing, notifications, and bulk back-office operations.

It is simpler than full microservices while still teaching asynchronous boundaries clearly.

## Recommended Technology

- Frontend: React
- Web app: ASP.NET Core
- Worker: ASP.NET Core background service or dedicated worker service
- Queue: RabbitMQ, Azure Storage Queues, Azure Service Bus, or Redis streams depending on scale
- Database: PostgreSQL for operational data

## Single-Container Guidance

A small learning example can package web and worker in one container, but the cleaner tutorial shape is usually multiple processes or containers so queue-driven separation is explicit.

## Example Project

**Project idea:** Reporting and export generation platform

Example flow:

- user requests a financial export
- web app stores the request and places a job on the queue
- worker builds the export file
- status is updated
- user downloads the completed file later

## Suggested Solution Shape

- React frontend for request submission and job tracking
- ASP.NET Core API for job intake and status
- worker process for export generation
- queue for background job dispatch
- PostgreSQL for job metadata and audit trail

## Tradeoffs

- lower request latency and better workload smoothing
- simpler than full service decomposition
- easier scaling of background processing separately
- queue visibility, retries, and idempotency become part of the design
