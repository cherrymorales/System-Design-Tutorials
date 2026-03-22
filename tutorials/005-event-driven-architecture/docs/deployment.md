# Event-Driven Architecture Deployment Guide

## Deployment Goal

The goal of deployment for this tutorial is to make the event-driven processing model visible and testable without overcomplicating the first implementation.

## Local Development Shape

Recommended local runtime:

- React frontend
- ASP.NET Core API
- one or more ASP.NET Core workers
- RabbitMQ
- PostgreSQL

This usually means multiple containers because the message broker is part of the architecture itself.

## Why Single-Container Is Usually Not The Right Fit

A single container can host the API and worker logic for learning, but it hides too much of the real event-driven shape.

For this tutorial, multiple containers are recommended because they keep the broker and processing separation explicit.

## Environment Expectations

### Local

- docker compose based startup
- seeded users and sample assets
- local broker and database
- logs visible per component
- Vite frontend running separately during development and smoke validation

### Higher Environments

- separate API and worker deployment units if scaling requires it
- managed broker or messaging service where appropriate
- durable storage for projections and transactional state
- centralized logs and tracing

## Runtime Responsibilities

### API

- handles authentication
- registers assets
- acknowledges upload completion
- publishes workflow-starting events reliably
- serves projected read data to the frontend

### Workers

- consume events
- perform processing steps
- publish outcome events
- retry transient failures

### Broker

- routes events
- supports queue durability, retries, and dead-letter handling

### Database

- stores core asset records
- stores operator-facing projections
- stores outbox or equivalent reliability state if used

## Observability Requirements

The deployment should support:

- per-step logs
- correlation IDs across handlers
- message retry visibility
- dead-letter visibility
- processing latency monitoring
- handler success and failure counts

## Security Expectations

- internal authenticated users only in V1
- secrets and connection strings kept out of code
- workers not exposed publicly
- browser traffic only reaches the API

## Release Gates

The system should not be considered ready for release unless:

- API and worker health can be observed
- failure paths are visible
- dead-letter handling exists
- smoke tests pass against the deployed local stack
- operators can see status progression in the console

## Current Tutorial Runtime

The implemented tutorial runs locally with:

- API container on `8085`
- worker container connected to the same broker and database
- PostgreSQL on `5437`
- RabbitMQ on `5674`
- RabbitMQ management UI on `15674`
- React frontend through Vite on `5179`

## Recommended Next Evolution After MVP

- split workers by scaling needs
- move to managed messaging in cloud deployment
- add richer tracing and alerting
- add replay and reprocessing tools for failed assets
