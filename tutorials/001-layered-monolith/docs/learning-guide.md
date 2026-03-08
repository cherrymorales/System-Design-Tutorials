# Layered Monolith Learning Guide

## Purpose Of This Guide

This document explains what a learner should take away from the layered monolith tutorial.

The goal is not only to understand the inventory system, but also to understand why this system design works well for it and where the current implementation is intentionally simplified.

## Learning Objectives

After studying this tutorial, the learner should be able to:

- define a layered monolith in practical terms
- explain the difference between application structure and deployment structure
- identify which kinds of systems fit a layered monolith well
- describe the responsibility of each layer
- explain why strong consistency and a single relational database fit this example
- explain why auth and authorization rules are architectural concerns, not only UI concerns
- recognize when a layered monolith should evolve into a modular monolith

## What To Focus On

### 1. One Deployable Unit Does Not Mean Bad Design

A single deployable application can still have strong internal structure.

The lesson is not that a monolith is automatically clean. The lesson is that a monolith can remain clean if boundaries are explicit and enforced.

### 2. Layering Is About Responsibility

Focus on what each layer is responsible for:

- Presentation handles HTTP, routes, sessions, and UI-facing concerns
- Application should coordinate workflows
- Domain enforces business rules
- Infrastructure deals with technical implementation details

The current implementation partially simplifies this by keeping some orchestration in endpoint modules. That is useful to notice, because it helps learners distinguish architectural intent from current implementation maturity.

### 3. Choose Architecture Based On Current Reality

This system uses a layered monolith because:

- the product is internal
- the workflows are transactional
- the domain is understandable and bounded
- strong consistency matters
- separate service deployment would add cost without current benefit

This is the core lesson: do not optimize for imagined scale before solving a current problem.

### 4. Planning Matters Before Coding

Another lesson in this tutorial is that architecture is not just folder structure.

A serious project definition also needs:

- scope
- non-goals
- workflows
- user roles
- data boundaries
- deployment assumptions
- readiness gates

### 5. Identity And Authorization Shape The Architecture

This tutorial now includes a practical auth layer.

That matters because learners can see that:

- write operations should use authenticated server-side identity
- warehouse visibility rules affect both API filtering and UI behavior
- authorization belongs to the system design, not only to a login screen

## Common Misunderstandings

### "Monolith" Means Poor Engineering

Incorrect. A monolith only describes the deployment model. Poor engineering comes from weak boundaries.

### Every Business Capability Needs Its Own Service

Incorrect. Most internal systems do not benefit from service decomposition in their first versions.

### Layered Architecture Means The Application Layer Is Always Fully Realized Immediately

Incorrect. Teams often start with an incomplete application layer and refine it as complexity grows. This tutorial currently shows that transitional state.

### A Database Per Concern Is Always Better

Incorrect for this case. The inventory system benefits from a single source of truth and strong transactional integrity.

## What This Tutorial Teaches Better Than A Toy Example

The inventory scenario is concrete enough to expose real design issues:

- stock movements require transactional clarity
- adjustments require approval rules
- reports depend on trustworthy operational data
- auditability matters
- multiple user roles create meaningful authorization boundaries

## Comparison Lens

### Layered Monolith Vs Modular Monolith

- layered monolith focuses on layer separation
- modular monolith adds stronger business-module boundaries
- the most likely next step for this tutorial is better modularity, not service decomposition

### Layered Monolith Vs Microservices

- layered monolith optimizes for simplicity and consistency
- microservices optimize for independent deployment and scaling
- for this inventory example, microservices would add operational cost too early

### Layered Monolith Vs Event-Driven Architecture

- layered monolith handles the core transactional workflow directly
- event-driven architecture becomes more valuable when many systems need asynchronous reactions
- this inventory example does not need that complexity for MVP

## Review Questions

- Why is this inventory system a better fit for a layered monolith than for microservices?
- Which responsibilities belong in the application layer versus the domain layer?
- Why is a single relational database appropriate here?
- Why should workflow actors come from the authenticated session rather than the request body?
- What warning signs would justify evolving this design later?

## Practical Exercise

A useful learning exercise is to trace one workflow through the current implementation and compare it to the ideal layered design.

Suggested workflow:

1. a warehouse operator signs in
2. the frontend submits a stock receipt request
3. the endpoint validates auth, role, and warehouse assignment
4. domain rules validate the operation
5. infrastructure persists the workflow and inventory change
6. the actor is recorded from the authenticated server-side identity

If the learner can explain both the current implementation path and the next refactor that would move orchestration into application services, they understand the tutorial well.

## Completion Check

The learner should consider this tutorial understood when they can:

- describe the architecture without generic buzzwords
- explain why this example fits the design
- identify what is implemented already versus what remains future work
- explain the tradeoff between tutorial simplicity and architectural purity
- explain why authorization and warehouse scoping are part of the architecture itself
