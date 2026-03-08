# Layered Monolith Learning Guide

## Purpose Of This Guide

This document explains what a learner should take away from the layered monolith tutorial.

The goal is not only to understand the example inventory system, but to understand why this system design works well for it.

## Learning Objectives

After studying this tutorial, the learner should be able to:

- define a layered monolith in practical terms
- explain the difference between application structure and deployment structure
- identify which kinds of systems fit a layered monolith well
- describe the responsibility of each layer
- explain why strong consistency and a single relational database fit this example
- recognize when a layered monolith should evolve into a modular monolith

## What To Focus On

### 1. One Deployable Unit Does Not Mean Bad Design

The main lesson is that a single deployable application can still have strong internal structure.

The mistake many teams make is assuming "monolith" automatically means "messy". This tutorial shows the opposite: the deployment unit can be singular while the code remains disciplined.

### 2. Layering Is About Responsibility

Focus on what each layer is responsible for:

- Presentation handles HTTP and UI-facing concerns
- Application coordinates workflows
- Domain enforces business rules
- Infrastructure deals with technical implementation details

If these responsibilities blur, the architecture degrades quickly.

### 3. Choose Architecture Based On Current Reality

This system uses a layered monolith because:

- the product is internal
- the workflows are transactional
- the domain is understandable and bounded
- strong consistency matters
- separate service deployment would add cost without immediate benefit

This is the real architectural lesson: do not optimize for imagined scale before solving a current problem.

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

Without those, implementation starts with hidden ambiguity.

## Common Misunderstandings

### "Monolith" Means Poor Engineering

Incorrect. A monolith is only the deployment model. Poor engineering comes from weak boundaries, not from being a single deployable unit.

### Every Business Capability Needs Its Own Service

Incorrect. Most internal systems do not benefit from service decomposition in their first versions.

### Layered Architecture Means Business Logic Goes Everywhere

Incorrect. Business logic should concentrate in the domain and application layers, not in controllers, UI components, or raw data access code.

### A Database Per Concern Is Always Better

Incorrect for this case. The inventory system benefits from a single source of truth and strong transactional integrity.

## What This Tutorial Teaches Better Than A Toy Example

The inventory scenario is intentionally concrete because it exposes real design issues:

- stock movements require transactional clarity
- adjustments require approval rules
- reports depend on trustworthy operational data
- auditability matters
- multiple user roles create authorization boundaries

This makes the layered monolith decision easier to evaluate than with a generic CRUD demo.

## Comparison Lens

### Layered Monolith Vs Modular Monolith

- layered monolith focuses on layer separation
- modular monolith adds stronger business-module boundaries
- a layered monolith is often the earlier step

### Layered Monolith Vs Microservices

- layered monolith optimizes for simplicity and consistency
- microservices optimize for independent deployment and scaling
- for this inventory example, microservices would add operational cost too early

### Layered Monolith Vs Event-Driven Architecture

- layered monolith handles the core transactional workflow directly
- event-driven architecture becomes more valuable when many systems need to react asynchronously
- this inventory example does not need that complexity for MVP

## Review Questions

- Why is this inventory system a better fit for a layered monolith than for microservices?
- Which responsibilities belong in the application layer versus the domain layer?
- Why is a single relational database appropriate here?
- What business rules should never live in controllers?
- What warning signs would justify evolving this design later?

## Practical Exercise

A useful learning exercise is to take one workflow and map it through the layers.

Suggested workflow:

1. stock receipt is submitted by a warehouse operator
2. controller validates the request format
3. application service coordinates the use case
4. domain rules validate the product and quantity
5. infrastructure persists the result in PostgreSQL

If the learner can explain that path clearly, they understand the architecture more concretely.

## Completion Check

The learner should consider this tutorial understood when they can:

- describe the architecture without generic buzzwords
- explain why this example fits the design
- identify what is intentionally not implemented yet
- explain the tradeoff between simplicity and future modularity
