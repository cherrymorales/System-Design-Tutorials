# Microservices Learning Guide

## Purpose

This tutorial is meant to teach microservices as an engineering tradeoff, not as a prestige architecture.

The main lesson is not just "split the system into services". The main lesson is how to split a system only when ownership, deployment, and workflow realities justify it.

## Learning Objectives

By the end of this tutorial, a learner should be able to:

- explain what makes a service boundary real
- identify when a gateway or BFF is necessary
- describe why data ownership is the core rule of microservices
- explain when to use HTTP versus messaging between services
- describe eventual consistency in terms that match product behavior
- outline a defensible testing strategy for a microservices system

## What To Focus On

When studying this tutorial, focus on:

- service ownership
- workflow orchestration
- event flow and compensation
- query projections
- observability and tracing
- testing beyond unit scope

## Architectural Comparison

### Compared With 001 Layered Monolith

`001` keeps one deployment unit and one database.

`004` introduces:

- multiple deployable services
- asynchronous integration
- separate service data ownership
- operational overhead that a monolith does not have

### Compared With 002 Modular Monolith

`002` proves business boundaries inside one application.

`004` assumes those boundaries are already strong enough that independent runtime ownership now matters.

The key difference is:

- modular monolith boundaries are in-process
- microservices boundaries are networked and operational

### Compared With 003 Client-Server SPA + API

`003` is mostly about the browser-to-server boundary.

`004` keeps that browser boundary but adds several internal service boundaries behind the gateway.

## What Usually Goes Wrong

Common microservices mistakes:

- splitting services before team and domain boundaries are stable
- sharing one operational database across services
- exposing many services directly to the browser
- building only synchronous chains and calling the system resilient
- ignoring tracing and correlation until production incidents arrive
- writing tests that cover classes but not workflows

## Core Principles To Remember

### Principle 1: Data Ownership Is Non-Negotiable

If multiple services can write the same operational tables directly, the architecture is not teaching real microservices.

### Principle 2: The Gateway Is A Boundary Tool, Not A Domain Owner

The gateway should simplify browser access and session concerns.

It should not become the place where order, payment, and inventory rules are reimplemented.

### Principle 3: Messaging Is For Workflow Decoupling, Not Just Fashion

Use messaging when the workflow crosses services and immediate in-request completion is not required.

Do not add messaging everywhere just to look distributed.

### Principle 4: Eventual Consistency Must Be Visible To Users

The UI must show pending and in-progress states honestly.

If the workflow is asynchronous, the browser cannot pretend every downstream result is immediate.

### Principle 5: Tests Must Follow The Real System Shape

Do not stop at service unit tests.

This architecture is only properly tested when:

- service rules are tested
- HTTP contracts are tested
- event flows are tested
- frontend workflows through the gateway are tested
- the main end-to-end path is smoke tested

## Questions A Learner Should Be Able To Answer

1. Why is `Orders` the V1 saga orchestrator instead of the gateway?
2. Why does `OperationsQuery` remain read-only?
3. Why should the browser not call every service directly?
4. What state is eventually consistent in this system?
5. Which failures require compensation and which do not?
6. What tests prove the distributed workflow works?

## Review Prompts

Use these prompts to test understanding:

- explain the difference between synchronous command handling and asynchronous workflow completion in this system
- explain why service boundaries fail when reporting or gateway code starts owning operational writes
- explain why microservices require more than just multiple repositories or multiple Dockerfiles
- explain how the order-submission saga completes or fails
- explain how the UI should represent a workflow that is still awaiting service outcomes

## Practical Exercise

Describe how you would add a `Returns` capability to this platform.

The answer should cover:

- whether `Returns` deserves its own service
- what data it owns
- which services it interacts with
- which interactions should be HTTP versus events
- how the gateway and `OperationsQuery` would change
- what new tests are required

## Completion Criteria

A learner has understood this tutorial when they can:

- describe the service boundaries and defend them
- describe the main distributed workflow without hand-waving
- explain the difference between operational writes and read projections
- explain the deployment shape without pretending it is cheap
- explain the full testing strategy without skipping event and smoke coverage
